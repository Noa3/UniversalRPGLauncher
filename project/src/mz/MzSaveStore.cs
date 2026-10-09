using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace UniversalRPG.Web;

/// <summary>
/// Bounded storage for the launcher's current MZ-shaped snapshots.
/// </summary>
/// <remarks>
/// Payloads are UTF-8 JSON in a checksum-verified zlib stream. The current
/// native snapshot is incomplete and its interoperability with original
/// RPG Maker save files is not established. File-name compatibility alone
/// must not be treated as full save-format compatibility.
/// The caller owns the selected directory; imported game directories must
/// not be used as a writable default in the production launcher.
/// Reparse-point checks are defense in depth for an application-owned directory,
/// not protection against a local actor concurrently replacing path components.
/// Cloud placeholder directories are intentionally refused by this conservative
/// policy. Writes/removals acquire a bounded per-file named mutex across
/// processes in the same OS session; the in-process gate also serializes callers.
/// The zlib checksum detects corruption, not forgery.
/// </remarks>
public static class MzSaveStore
{
    /// <summary>
    /// How many slots the engine offers, which is
    /// <c>DataManager.maxSavefiles</c>.
    /// </summary>
    public const int MaxSavefiles = 20;

    /// <summary>
    /// The engine's own suffix, which is
    /// <c>StorageManager.filePath</c>'s <c>".rmmzsave"</c>.
    /// </summary>
    public const string Suffix = ".rmmzsave";

    /// <summary>
    /// The file name of a slot, which is
    /// <c>DataManager.makeSavename</c>'s <c>"file%1".format(id)</c>.
    /// </summary>
    public static string SlotName(int pSavefileId) => "file" + pSavefileId;

    /// <summary>
    /// The name of the global info file, which is what
    /// <c>DataManager.saveGlobalInfo</c> writes.
    /// </summary>
    public const string GlobalName = "global";

    public const int MaxFileBytes = 16 * 1024 * 1024;
    public const int MaxJsonBytes = 16 * 1024 * 1024;
    public const int MaxNodes = 250000;
    public const int MaxSaveDepth = 100;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly object FileGate = new();

    /// <summary>Resolves a single portable file name within the selected directory.</summary>
    public static string PathOf(string pDirectory, string pName)
    {
        if (string.IsNullOrWhiteSpace(pDirectory))
        {
            throw new ArgumentException("A save directory is required.", nameof(pDirectory));
        }
        if (string.IsNullOrEmpty(pName) || pName.Length > 64
            || !IsPortableName(pName))
        {
            throw new ArgumentException("A save name must be a non-reserved ASCII leaf name containing only letters, digits, '-' or '_'.", nameof(pName));
        }
        return Path.Combine(Path.GetFullPath(pDirectory), pName + Suffix);
    }

    private static bool IsPortableName(string name)
    {
        // Windows device names remain reserved even with a file extension.
        // Apply the same portable-name contract on every platform.
        var upper = name.ToUpperInvariant();
        if (upper is "CON" or "PRN" or "AUX" or "NUL"
            || (upper.Length == 4 && upper[3] is >= '1' and <= '9'
                && (upper.StartsWith("COM", StringComparison.Ordinal)
                    || upper.StartsWith("LPT", StringComparison.Ordinal))))
        {
            return false;
        }
        foreach (var c in name)
        {
            if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
                or >= '0' and <= '9' or '-' or '_'))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Writes an object into a slot, the way <c>saveObject</c> does.
    /// </summary>
    /// <param name="pDirectory">Where this game's saves live.</param>
    /// <param name="pName">The engine's name for the file.</param>
    /// <param name="pObject">The contents, already in the engine's shape.</param>
    /// <param name="pProblem">What went wrong, when something did.</param>
    /// <returns>Whether the file was written.</returns>
    /// <remarks>
    /// <strong>And a save that fails must not eat the save that was
    /// there.</strong> The engine's own <c>saveToLocalFile</c> moves the old
    /// file aside first and puts it back if the write throws, and this
    /// keeps that contract with the swap the platform does atomically --
    /// <strong>because a save that a crash left half-written is a save that
    /// a player loses a game to.</strong>
    /// </remarks>
    public static bool Save(
        string pDirectory,
        string pName,
        MzValue pObject,
        out string pProblem)
    {
        pProblem = "";
        string? temporary = null;
        var temporaryCreated = false;
        lock (FileGate)
        {
            try
            {
                var path = PathOf(pDirectory, pName);
                using var processLock = AcquireProcessLock(path);
                ValidateTree(pObject);
                var json = MzJson.Write(pObject);
                if (StrictUtf8.GetByteCount(json) > MaxJsonBytes)
                {
                    throw new InvalidDataException("Save exceeds the JSON byte limit.");
                }
                ValidateJson(json);
                var bytes = Zip(json);
                RejectLinks(path);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                RejectLinks(path);
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                using (var file = new FileStream(temporary, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None))
                {
                    temporaryCreated = true;
                    file.Write(bytes);
                    file.Flush(flushToDisk: true);
                }

                // Unique sibling staging files prevent cross-process writers from
                // sharing a buffer. The destination is untouched until the swap.
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path, overwrite: true);
                }
                temporaryCreated = false;
                return true;
            }
            catch (Exception exception) when (IsStorageFailure(exception))
            {
                pProblem = $"the save could not be written: {exception.Message}";
                return false;
            }
            finally
            {
                if (temporaryCreated && temporary != null)
                {
                    var cleanup = TryDelete(temporary);
                    if (cleanup.Length > 0)
                    {
                        pProblem += $"; staging cleanup failed: {cleanup}";
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reads an object back out of a slot.
    /// </summary>
    /// <param name="pDirectory">Where this game's saves live.</param>
    /// <param name="pName">The engine's name for the file.</param>
    /// <param name="pObject">The contents, when they could be read.</param>
    /// <param name="pProblem">What went wrong, when something did.</param>
    /// <returns>Whether a file was there and could be read.</returns>
    /// <remarks>
    /// <strong>And a file that is not there is a refusal and not an empty
    /// save.</strong> The engine's own <c>loadFromLocalFile</c> rejects with
    /// <c>new Error("Savefile not found")</c> -- <strong>and a reader that
    /// returned an empty object would let a game start a new one over a
    /// slot the player thought was full.</strong>
    /// </remarks>
    public static bool TryLoad(
        string pDirectory,
        string pName,
        out MzValue? pObject,
        out string pProblem)
    {
        pObject = null;
        pProblem = "";
        try
        {
            var path = PathOf(pDirectory, pName);
            RejectLinks(path);
            if (!File.Exists(path))
            {
                pProblem = $"there is no save at {path}";
                return false;
            }
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.Read | FileShare.Delete);
            if (file.Length > MaxFileBytes)
            {
                throw new InvalidDataException($"Save exceeds the {MaxFileBytes} byte file limit.");
            }
            var json = Unzip(ReadBounded(file, MaxFileBytes));
            ValidateJson(json);
            if (!MzJson.TryParse(json, out var value, out var failure))
            {
                pProblem = $"the save at {path} is not readable JSON: {failure}";
                return false;
            }
            pObject = value;
            return true;
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            pProblem = $"the save could not be read: {exception.Message}";
            return false;
        }
    }

    /// <summary>Whether a slot has a file, which is <c>savefileExists</c>.</summary>
    public static bool Exists(string pDirectory, string pName)
    {
        try
        {
            var path = PathOf(pDirectory, pName);
            RejectLinks(path);
            return File.Exists(path);
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            return false;
        }
    }

    /// <summary>Removes only a regular file within the selected save directory.</summary>
    public static bool Remove(string pDirectory, string pName)
    {
        lock (FileGate)
        {
            try
            {
                var path = PathOf(pDirectory, pName);
                using var processLock = AcquireProcessLock(path);
                RejectLinks(path);
                if (!File.Exists(path)) { return false; }
                File.Delete(path);
                return true;
            }
            catch (Exception exception) when (IsStorageFailure(exception))
            {
                return false;
            }
        }
    }

    /// <summary>
    /// The slots that have a save, which is what a load screen lists.
    /// </summary>
    /// <remarks>
    /// <strong>And it counts from one</strong>, because
    /// <c>DataManager.emptySavefileId</c> starts at 1 and slot 0 is not a
    /// slot the engine offers.
    /// </remarks>
    public static IReadOnlyList<int> Slots(string pDirectory)
    {
        var slots = new List<int>();
        for (var id = 1; id <= MaxSavefiles; id++)
        {
            if (Exists(pDirectory, SlotName(id)))
            {
                slots.Add(id);
            }
        }

        return slots;
    }

    /// <summary>
    /// zlib deflate, which is what <c>pako.deflate</c> produces.
    /// </summary>
    /// <remarks>
    /// <strong>And it is zlib and not raw deflate.</strong>
    /// <c>pako.deflate</c> writes the two-byte zlib header and its own
    /// checksum, and <c>pako.inflate</c> reads them -- <strong>so a reader
    /// that used <c>DeflateStream</c> would write a file no MZ game could
    /// open, and the first two bytes of every real save are
    /// <c>0x78</c>.</strong>
    /// </remarks>
    public static byte[] Zip(string pJson)
    {
        ArgumentNullException.ThrowIfNull(pJson);
        if (StrictUtf8.GetByteCount(pJson) > MaxJsonBytes)
        {
            throw new InvalidDataException($"Save exceeds the {MaxJsonBytes} byte JSON limit.");
        }
        using var output = new MemoryStream();
        using (var deflate = new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            deflate.Write(StrictUtf8.GetBytes(pJson));
        }
        if (output.Length > MaxFileBytes)
        {
            throw new InvalidDataException($"Save exceeds the {MaxFileBytes} byte file limit.");
        }
        return output.ToArray();
    }

    /// <summary>Reads one bounded, checksum-verified zlib stream as strict UTF-8.</summary>
    public static string Unzip(byte[] pZip)
    {
        ArgumentNullException.ThrowIfNull(pZip);
        if (pZip.Length > MaxFileBytes || pZip.Length < 6)
        {
            throw new InvalidDataException("Save has an invalid compressed length.");
        }
        if ((pZip[0] & 15) != 8 || (pZip[0] >> 4) > 7
            || ((pZip[0] << 8) + pZip[1]) % 31 != 0 || (pZip[1] & 32) != 0)
        {
            throw new InvalidDataException("Save has an unsupported zlib header.");
        }
        using var input = new MemoryStream(pZip, writable: false);
        using var inflate = new ZLibStream(input, CompressionMode.Decompress);
        var expanded = ReadBounded(inflate, MaxJsonBytes);
        // ZLibStream can return EOF for truncated input. Check the trailer
        // independently rather than accepting a successfully parsed prefix.
        var expected = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(pZip.AsSpan(pZip.Length - 4));
        if (Adler32(expanded) != expected)
        {
            throw new InvalidDataException("Save is truncated or has an invalid zlib checksum.");
        }
        return StrictUtf8.GetString(expanded);
    }

    private static byte[] ReadBounded(Stream stream, int limit)
    {
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (output.Length + count > limit)
            {
                throw new InvalidDataException($"Save exceeds the {limit} byte stream limit.");
            }
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }

    private static uint Adler32(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (var value in data)
        {
            a = (a + value) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }

    private static void ValidateJson(string json)
    {
        var reader = new System.Text.Json.Utf8JsonReader(StrictUtf8.GetBytes(json),
            new System.Text.Json.JsonReaderOptions { MaxDepth = MaxSaveDepth });
        var count = 0;
        while (reader.Read())
        {
            if (++count > MaxNodes)
            {
                throw new InvalidDataException($"Save exceeds the {MaxNodes} token limit.");
            }
        }
    }

    private static void ValidateTree(MzValue root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var active = new HashSet<MzValue>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        var nodes = 0;
        long size = 0;
        Visit(root, 0);

        void Visit(MzValue value, int depth)
        {
            if (value == null || depth > MaxSaveDepth
                || (depth == MaxSaveDepth && value.Kind is (MzKind.Object or MzKind.Array))
                || ++nodes > MaxNodes || !active.Add(value))
            {
                throw new InvalidDataException("Save tree is null, cyclic, too deep or too large.");
            }
            size += 32;
            if (value.Kind == MzKind.String)
            {
                size += StrictUtf8.GetByteCount(value.Text);
            }
            if (size > MaxJsonBytes)
            {
                throw new InvalidDataException("Save tree exceeds the JSON size budget.");
            }
            if (value.Kind == MzKind.Number && !double.IsFinite(value.Number))
            {
                throw new InvalidDataException("Save contains a non-finite number.");
            }
            if (value.Kind == MzKind.Array)
            {
                foreach (var child in value.Items) { Visit(child, depth + 1); }
            }
            else if (value.Kind == MzKind.Object)
            {
                var keys = new HashSet<string>(StringComparer.Ordinal);
                if (value.Keys.Count != value.Members.Count)
                {
                    throw new InvalidDataException("Save object has inconsistent keys.");
                }
                foreach (var key in value.Keys)
                {
                    if (key == null || !keys.Add(key) || !value.Members.TryGetValue(key, out var child))
                    {
                        throw new InvalidDataException("Save object has duplicate or missing keys.");
                    }
                    size += StrictUtf8.GetByteCount(key);
                    Visit(child, depth + 1);
                }
            }
            else if (value.Kind is not (MzKind.Null or MzKind.Bool or MzKind.Number or MzKind.String))
            {
                throw new InvalidDataException("Save contains an unknown value kind.");
            }
            active.Remove(value);
        }
    }

    private static IDisposable AcquireProcessLock(string path)
    {
        var identity = OperatingSystem.IsWindows() ? path.ToUpperInvariant() : path;
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        var mutex = new System.Threading.Mutex(false, "UniversalRPG.Save." + hash);
        try
        {
            try
            {
                if (!mutex.WaitOne(TimeSpan.FromSeconds(10)))
                {
                    throw new IOException("Timed out waiting for another save writer.");
                }
            }
            catch (System.Threading.AbandonedMutexException)
            {
                // WaitOne transfers ownership when a previous writer died.
                // The atomic destination swap and load checksum remain required.
            }
            return new ProcessLock(mutex);
        }
        catch
        {
            mutex.Dispose();
            throw;
        }
    }

    private sealed class ProcessLock : IDisposable
    {
        private readonly System.Threading.Mutex _mutex;
        public ProcessLock(System.Threading.Mutex mutex) => _mutex = mutex;
        public void Dispose()
        {
            try { _mutex.ReleaseMutex(); }
            finally { _mutex.Dispose(); }
        }
    }

    private static void RejectLinks(string path)
    {
        for (string? current = path; current != null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new IOException("Save paths may not contain symbolic links or junctions.");
                }
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }

    private static bool IsStorageFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or ArgumentException
            or InvalidDataException or System.Text.Json.JsonException or NotSupportedException
            or System.Threading.WaitHandleCannotBeOpenedException;

    private static string TryDelete(string pPath)
    {
        try
        {
            File.Delete(pPath);
            return "";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return exception.Message;
        }
    }
}
