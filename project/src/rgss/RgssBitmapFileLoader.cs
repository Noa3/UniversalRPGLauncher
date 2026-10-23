using System;
using System.IO;
using System.Threading;

namespace UniversalRPG.Rgss;

/// <summary>Read-only game-local images. Reject links and traversal; never invoke Godot or game code.</summary>
internal static class RgssBitmapFileLoader
{
    internal static (long Id, int Width, int Height) Load(RgssBitmapStore store, string? root,
        string filename, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var path = Resolve(root, filename, cancellation);
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (input.Length < 33 || input.Length > RgssPngImage.MaximumFileBytes)
            throw new InvalidDataException("PNG encoded size is outside the supported limit.");
        var bytes = new byte[(int)input.Length];
        input.ReadExactly(bytes);
        if (input.ReadByte() != -1) throw new InvalidDataException("Image changed while being read.");
        var png = new RgssPngImage(bytes, cancellation);
        // Reserve against the existing total bitmap budget before any decoded surface allocation.
        var id = store.Create(png.Width, png.Height);
        try
        {
            png.DecodeInto(store.FindSurface(id)!, cancellation);
            return (id, png.Width, png.Height);
        }
        catch
        {
            store.DisposeBitmap(id);
            throw;
        }
    }

    internal static string Resolve(string? root, string filename, CancellationToken cancellation)
    {
        if (string.IsNullOrEmpty(root)) throw new InvalidDataException("The game image root is unavailable.");
        var relative = filename.Replace('\\', '/');
        if (relative.Length == 0 || relative.Length > 4096 || relative.StartsWith('/')
            || relative.Contains(':') || relative.Contains('\0'))
            throw new InvalidDataException("Image filename must be a bounded game-relative path.");
        if (!relative.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) relative += ".png";
        var parts = relative.Split('/');
        if (parts.Length > 128) throw new InvalidDataException("Image path has too many components.");
        foreach (var part in parts)
            if (part.Length == 0 || part is "." or ".." || part.TrimEnd(' ', '.') != part)
                throw new InvalidDataException("Unsafe image path component.");
        var current = Path.GetFullPath(root);
        for (DirectoryInfo? ancestor = new(current); ancestor != null; ancestor = ancestor.Parent)
            RejectLink(ancestor.FullName);
        for (var index = 0; index < parts.Length; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            var candidate = Path.Combine(current, parts[index]);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                string? found = null;
                var count = 0;
                foreach (var entry in Directory.EnumerateFileSystemEntries(current))
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (++count > 32768) throw new InvalidDataException("Image directory lookup limit exceeded.");
                    if (!string.Equals(Path.GetFileName(entry), parts[index], StringComparison.OrdinalIgnoreCase)) continue;
                    if (found != null) throw new InvalidDataException("Ambiguous image filename casing.");
                    found = entry;
                }
                candidate = found ?? throw new FileNotFoundException("Game image not found: " + filename);
            }
            RejectLink(candidate);
            var directory = (File.GetAttributes(candidate) & FileAttributes.Directory) != 0;
            if (directory != (index < parts.Length - 1)) throw new InvalidDataException("Invalid image path type.");
            current = candidate;
        }
        return current;
    }

    private static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Linked image paths are not supported.");
    }
}
