using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UniversalRPG.App.Library;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestGameLibraryScan : TestBase
{
    private string _root = "";
    private string _settings = "";

    public override void Setup()
    {
        _root = ProjectSettings.GlobalizePath("user://scan-safety/" + Guid.NewGuid().ToString("N"));
        _settings = Path.Combine(_root, "library.cfg");
        CopyGame(Path.Combine(_root, "Alpha"));
        CopyGame(Path.Combine(_root, "Collection", "Beta"));
    }

    public override void Teardown() => Directory.Delete(_root, true);

    public void Test_WorkerResultsAndProgressStayDetachedUntilOwnerCommits()
    {
        using var library = NewLibrary();
        var previous = library.Import(Path.Combine(_root, "Alpha"), false)!;
        using var scan = library.BeginScan();
        Wait(scan);
        AssertEq(library.Games.Count, 1, "A worker never mutates the published game list");
        AssertTrue(ReferenceEquals(library.Games[0], previous), "Existing entries remain stable during scanning");
        AssertFalse(File.Exists(_settings), "The worker does not persist settings");
        AssertEq(scan.Progress.GamesFound, 2, "Progress counts recognized games rather than files");
        AssertTrue(scan.Progress.DirectoriesScanned >= 4, "Progress includes traversal folders");
        AssertTrue(library.ApplyScan(scan), "The owner explicitly commits a successful scan");
        AssertEq(library.Games.Count, 2, "The complete snapshot replaces the list once");
        AssertTrue(File.Exists(_settings), "Persistence happens only at owner-thread commit");
    }

    public void Test_CancelledResultPreservesPreviousEntriesAndSettings()
    {
        using var library = NewLibrary();
        library.Scan();
        var previous = library.Games.ToArray();
        var settings = File.ReadAllBytes(_settings);
        using var scan = library.BeginScan();
        Wait(scan);
        scan.Cancel(); // Also cover cancellation after IO but before the next UI frame.
        AssertFalse(library.ApplyScan(scan), "A late cancel still prevents publication");
        AssertTrue(library.Games.SequenceEqual(previous), "Cancellation keeps previous entry identity");
        AssertTrue(File.ReadAllBytes(_settings).SequenceEqual(settings), "Cancellation does not rewrite persisted records");
    }

    public void Test_RootChangeRejectsStaleCompletedResult()
    {
        using var library = NewLibrary();
        var previous = library.Import(Path.Combine(_root, "Alpha"), false)!;
        using var scan = library.BeginScan();
        Wait(scan);
        var other = Path.Combine(_root, "Other");
        Directory.CreateDirectory(other);
        library.SetRootPath(other, false);
        AssertFalse(library.ApplyScan(scan), "A completed result cannot overwrite a newly selected root");
        AssertTrue(ReferenceEquals(library.Games.Single(), previous), "Stale results preserve the previous library");
    }

    public void Test_NewScanRejectsOlderResultEvenAtTheSameRoot()
    {
        using var library = NewLibrary();
        using var old = library.BeginScan();
        Wait(old);
        using var current = library.BeginScan();
        Wait(current);
        AssertFalse(library.ApplyScan(old), "Root equality does not authorize an obsolete generation");
        AssertTrue(library.ApplyScan(current), "The newest completed generation may publish");
    }

    public void Test_WorkerCannotPublishOnTheWrongThread()
    {
        using var library = NewLibrary();
        using var scan = library.BeginScan();
        Wait(scan);
        var rejected = Task.Run(() =>
        {
            try { library.ApplyScan(scan); return false; }
            catch (InvalidOperationException) { return true; }
        }).GetAwaiter().GetResult();
        AssertTrue(rejected, "The new publication API enforces owner-thread commit");
        AssertEq(library.Games.Count, 0, "A rejected commit has no list mutation");
        AssertFalse(File.Exists(_settings), "A rejected commit has no settings mutation");
    }

    public void Test_AssetFoldersAndOverDepthGamesAreNotTraversed()
    {
        CopyGame(Path.Combine(_root, "img", "Hidden"));
        CopyGame(Path.Combine(_root, "node_modules", "Hidden"));
        CopyGame(Path.Combine(_root, "one", "two", "three", "four", "TooDeep"));
        using var library = NewLibrary();
        using var scan = library.BeginScan();
        Wait(scan);
        AssertEq(scan.Completion.Result.Count, 2, "Existing asset-folder and depth limits remain intact");
    }

    public void Test_SelectedWindowsJunctionRootStillScansButChildLinksDoNot()
    {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) return;
        var alias = _root + "-selected-link";
        var childLink = Path.Combine(_root, "LinkedChild");
        try
        {
            CreateJunction(alias, _root);
            CreateJunction(childLink, Path.Combine(_root, "Alpha"));
            using var library = new GameLibrary(pSettingsPath: _settings);
            library.SetRootPath(alias, false);
            using var scan = library.BeginScan();
            Wait(scan);
            AssertEq(scan.Completion.Result.Count, 2, "An explicitly selected linked collection root must not become an empty library");
            AssertFalse(scan.Completion.Result.Any(entry => entry.Path.Contains("LinkedChild", StringComparison.Ordinal)),
                "Explicit root permission does not allow recursive child-link traversal");
        }
        finally
        {
            if (Directory.Exists(childLink)) Directory.Delete(childLink);
            if (Directory.Exists(alias)) Directory.Delete(alias);
        }
    }

    public void Test_PersistedExplicitEngineChoiceSurvivesBackgroundRescan()
    {
        using var first = NewLibrary();
        var entry = first.Import(Path.Combine(_root, "Alpha"))!;
        AssertTrue(first.TrySelectEngine(entry, UniversalRPG.Plugins.EnginePluginIds.RpgMaker2000, out var error),
            $"A currently detected playable engine can be selected: {error}");
        using var restored = new GameLibrary(pSettingsPath: _settings);
        restored.LoadSettings();
        using var scan = restored.BeginScan();
        Wait(scan);
        AssertTrue(restored.ApplyScan(scan));
        var selected = restored.Games.Single(game => game.Path.EndsWith("/Alpha", StringComparison.Ordinal));
        AssertEq(selected.ExplicitPluginId, UniversalRPG.Plugins.EnginePluginIds.RpgMaker2000,
            "Background detection preserves an explicit choice instead of restoring ambiguity");
        AssertTrue(selected.LoadedFromPersistence);
    }

    private void CreateJunction(string alias, string target)
    {
        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.System), "cmd.exe"),
            UseShellExecute = false, CreateNoWindow = true,
            Arguments = $"/c mklink /J \"{alias}\" \"{target}\"",
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        using var process = System.Diagnostics.Process.Start(start)!;
        AssertTrue(process.WaitForExit(5000), "The owned junction fixture is created within a bounded time");
        AssertEq(process.ExitCode, 0, "Windows junction fixture creation succeeds without administrative symlink permission");
    }

    private GameLibrary NewLibrary()
    {
        var library = new GameLibrary(pSettingsPath: _settings);
        library.SetRootPath(_root, false);
        return library;
    }

    private void Wait(GameLibraryScan scan)
    {
        AssertTrue(System.Threading.SpinWait.SpinUntil(() => scan.Completion.IsCompleted, TimeSpan.FromSeconds(10)),
            "The bounded fixture scan completes");
        scan.Completion.GetAwaiter().GetResult();
    }

    private static void CopyGame(string destination)
    {
        Directory.CreateDirectory(destination);
        var source = ProjectSettings.GlobalizePath("res://tests/fixtures/easyrpg-testgame/rm2000");
        foreach (var file in Directory.GetFiles(source))
        {
            if (Path.GetExtension(file).ToLowerInvariant() is ".ldb" or ".lmt" or ".lmu" or ".ini")
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
    }
}
