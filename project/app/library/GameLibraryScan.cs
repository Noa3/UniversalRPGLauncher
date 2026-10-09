using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalRPG.App.Library;

public sealed record LibraryScanProgress(string CurrentPath, int DirectoriesScanned, int GamesFound,
    int UnreadableDirectories, bool LimitReached);

/// <summary>One isolated, bounded scan; callers publish its results on the UI thread.</summary>
public sealed class GameLibraryScan : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private LibraryScanProgress _progress;
    private int _disposed;

    internal GameLibraryScan(string rootPath, long generation, Func<string, GameLibrary.GameEntry> inspect)
    {
        RootPath = rootPath;
        Generation = generation;
        _progress = new(rootPath, 0, 0, 0, false);
        Completion = Task.Run(() => Run(inspect), _cancellation.Token);
    }

    public string RootPath { get; }
    internal long Generation { get; }
    public Task<IReadOnlyList<GameLibrary.GameEntry>> Completion { get; }
    public LibraryScanProgress Progress => Volatile.Read(ref _progress);
    public bool IsCancellationRequested => _cancellation.IsCancellationRequested;

    public void Cancel() => _cancellation.Cancel();

    private IReadOnlyList<GameLibrary.GameEntry> Run(Func<string, GameLibrary.GameEntry> inspect)
    {
        var games = new List<GameLibrary.GameEntry>();
        var scanned = 0;
        var unreadable = 0;
        var limited = false;
        var token = _cancellation.Token;

        void Publish(string path) => Volatile.Write(ref _progress,
            new LibraryScanProgress(path, scanned, games.Count, unreadable, limited));

        void Visit(string path, int depth)
        {
            token.ThrowIfCancellationRequested();
            if (scanned >= GameLibrary.MaxScanDirectories) { limited = true; Publish(path); return; }
            // Preserve explicit collection-root selection; never follow child links.
            FileAttributes attributes;
            try { attributes = File.GetAttributes(path); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            { unreadable++; Publish(path); return; }
            if (depth > 0 && (attributes & FileAttributes.ReparsePoint) != 0) { return; }
            scanned++;
            Publish(path); // Show the current folder before a potentially slow inspection.
            if (GameLibrary.LooksLikeGameRoot(path))
            {
                token.ThrowIfCancellationRequested();
                var entry = inspect(path);
                token.ThrowIfCancellationRequested();
                if (GameLibrary.IsRecognized(entry.Detection))
                {
                    games.Add(entry);
                    Publish(path);
                    return; // Never traverse a recognized game's assets as more games.
                }
            }
            if (depth >= GameLibrary.MaxScanDepth) { return; }
            try
            {
                foreach (var child in Directory.EnumerateDirectories(path))
                {
                    token.ThrowIfCancellationRequested();
                    if (GameLibrary.ShouldSkipDirectory(Path.GetFileName(child))) { continue; }
                    if (scanned >= GameLibrary.MaxScanDirectories) { limited = true; Publish(path); break; }
                    Visit(child.Replace('\\', '/'), depth + 1);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            { unreadable++; Publish(path); }
        }

        if (Directory.Exists(RootPath)) { Visit(RootPath, 0); }
        token.ThrowIfCancellationRequested();
        games.Sort((left, right) => string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase));
        return games;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) { return; }
        _cancellation.Cancel();
        if (Completion.IsCompleted) { _cancellation.Dispose(); }
        else
        {
            // Scene teardown must not block on IO or leave a fault unobserved.
            _ = Completion.ContinueWith(task =>
            {
                _ = task.Exception;
                _cancellation.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
