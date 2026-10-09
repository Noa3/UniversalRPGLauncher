using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

public partial class TestMzSaveStoreSafety : TestBase
{
    private const int ByteLimit = 16 * 1024 * 1024;

    public void Test_PathEscapeIsRejectedWithoutCreatingFiles()
    {
        InDirectory(directory =>
        {
            foreach (var name in new[] { "../escape", "..\\escape", Path.Combine(directory, "absolute"), "file1:stream", "", ".", "../file1" })
            {
                var saved = MzSaveStore.Save(directory, name, Number(17), out var problem);
                AssertFalse(saved, $"Unsafe name '{name}' is refused: {problem}");
                AssertFalse(MzSaveStore.TryLoad(directory, name, out var loaded, out _),
                    "Unsafe names cannot be loaded either");
                AssertTrue(loaded == null, "Rejected load returns no value");
            }
            AssertEq(Directory.GetFiles(directory).Length, 0, "No save files were created");
        });
    }

    public void Test_InvalidUtf8IsRejectedInsteadOfRepaired()
    {
        InDirectory(directory =>
        {
            WriteCompressed(directory, new byte[] { 34, 0xc3, 0x28, 34 });
            AssertFalse(MzSaveStore.TryLoad(directory, "file1", out var value, out var problem),
                $"Invalid UTF-8 is refused: {problem}");
            AssertTrue(value == null, "Invalid bytes cannot become repaired game data");
        });
    }

    public void Test_TruncatedStreamIsRejected()
    {
        InDirectory(directory =>
        {
            var bytes = MzSaveStore.Zip("{\"progress\":42}");
            File.WriteAllBytes(MzSaveStore.PathOf(directory, "file1"), bytes[..^3]);
            AssertFalse(MzSaveStore.TryLoad(directory, "file1", out var value, out var problem),
                $"Incomplete checksum is refused: {problem}");
            AssertTrue(value == null, "Truncated file produces no loaded state");
        });
    }

    public void Test_ExpandedPayloadHasAHardLimit()
    {
        InDirectory(directory =>
        {
            WriteCompressed(directory, Encoding.UTF8.GetBytes("\"" + new string('x', ByteLimit) + "\""));
            AssertFalse(MzSaveStore.TryLoad(directory, "file1", out var value, out var problem),
                $"Compression bomb is refused: {problem}");
            AssertTrue(value == null, "Oversized input produces no state");
        });
    }

    public void Test_StoredFileHasAHardLimit()
    {
        InDirectory(directory =>
        {
            var small = MzSaveStore.Zip("42");
            using (var file = new FileStream(MzSaveStore.PathOf(directory, "file1"), FileMode.Create))
            {
                file.Write(small);
                file.SetLength(ByteLimit + 1L);
            }
            AssertFalse(MzSaveStore.TryLoad(directory, "file1", out var value, out var problem),
                $"Oversized stored file is refused before reading: {problem}");
            AssertTrue(value == null, "Oversized stored file produces no state");
        });
    }

    public void Test_ConcurrentWritersLeaveAWholeSave()
    {
        InDirectory(directory =>
        {
            var results = new bool[16];
            Parallel.For(0, results.Length, i =>
                results[i] = MzSaveStore.Save(directory, "file1", Number(i), out _));
            AssertTrue(results.All(x => x), "In-process concurrent callers all commit complete values");
            AssertTrue(MzSaveStore.TryLoad(directory, "file1", out var value, out var problem),
                $"Final save remains readable: {problem}");
            AssertTrue(value?.Kind == MzKind.Number && value.Number >= 0 && value.Number < results.Length,
                "Destination contains one complete submitted value");
            AssertEq(Directory.GetFiles(directory).Length, 1, "Temporary files are cleaned up");
        });
    }

    public void Test_MalformedAndExcessiveJsonIsRejected()
    {
        InDirectory(directory =>
        {
            var payloads = new[]
            {
                "{bad json}",
                new string('[', 110) + "0" + new string(']', 110),
                "[" + string.Join(",", Enumerable.Repeat("0", MzSaveStore.MaxNodes + 1)) + "]"
            };
            foreach (var json in payloads)
            {
                File.WriteAllBytes(MzSaveStore.PathOf(directory, "file1"), MzSaveStore.Zip(json));
                AssertFalse(MzSaveStore.TryLoad(directory, "file1", out var value, out var problem),
                    $"Malformed or excessive JSON is refused: {problem}");
                AssertTrue(value == null, "Rejected JSON cannot become live state");
            }
        });
    }

    public void Test_FailedCommitPreservesDestinationAndCleansStaging()
    {
        if (!OperatingSystem.IsWindows()) { return; }
        InDirectory(directory =>
        {
            AssertTrue(MzSaveStore.Save(directory, "file1", Number(111), out _), "Initial save commits");
            var path = MzSaveStore.PathOf(directory, "file1");
            var before = File.ReadAllBytes(path);
            using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                AssertFalse(MzSaveStore.Save(directory, "file1", Number(222), out var problem),
                    $"Windows sharing lock prevents replacement: {problem}");
                AssertTrue(before.SequenceEqual(File.ReadAllBytes(path)), "Failed commit leaves old bytes unchanged");
            }
            AssertTrue(MzSaveStore.TryLoad(directory, "file1", out var value, out _), "Previous save is still readable");
            AssertEq(value!.Number, 111.0, "Previous value survives a commit-stage failure");
            AssertEq(Directory.GetFiles(directory).Length, 1, "Failed commit cleans only its own staging file");
        });
    }

    public void Test_CyclicDeepAndOversizedTreesDoNotOverwrite()
    {
        InDirectory(directory =>
        {
            AssertTrue(MzSaveStore.Save(directory, "file1", Number(111), out _), "Initial save commits");
            var cycle = new MzValue(MzKind.Array);
            cycle.Items.Add(cycle);
            AssertFalse(MzSaveStore.Save(directory, "file1", cycle, out _), "Cycles are refused before recursive serialization");
            var deep = Number(0);
            for (var i = 0; i < 110; i++)
            {
                var parent = new MzValue(MzKind.Array);
                parent.Items.Add(deep);
                deep = parent;
            }
            AssertFalse(MzSaveStore.Save(directory, "file1", deep, out _), "Deep trees are bounded");
            var large = new MzValue(MzKind.String) { Text = new string('x', ByteLimit + 1) };
            AssertFalse(MzSaveStore.Save(directory, "file1", large, out _), "Output is bounded too");
            AssertTrue(MzSaveStore.TryLoad(directory, "file1", out var value, out _), "Rejected writes preserve old save");
            AssertEq(value!.Number, 111.0, "Last good value stays unchanged");
        });
    }

    public void Test_ReservedWindowsDeviceNamesAreRejectedBeforeIo()
    {
        InDirectory(directory =>
        {
            foreach (var name in new[] { "CON", "con", "NUL", "PRN", "AUX", "COM1", "com9", "LPT1", "lpt9" })
            {
                var rejected = false;
                try { MzSaveStore.PathOf(directory, name); }
                catch (ArgumentException) { rejected = true; }
                AssertTrue(rejected, $"Reserved device name {name} is rejected before file IO");
            }
            AssertTrue(MzSaveStore.PathOf(directory, "COM10").EndsWith("COM10.rmmzsave", StringComparison.Ordinal),
                "A non-reserved portable name is not rejected by prefix");
        });
    }

    public void Test_DepthLimitHasTheSameMeaningOnWriteAndRead()
    {
        InDirectory(directory =>
        {
            foreach (var depth in new[] { MzSaveStore.MaxSaveDepth - 1, MzSaveStore.MaxSaveDepth, MzSaveStore.MaxSaveDepth + 1 })
            {
                var tree = Number(0);
                for (var i = 0; i < depth; i++)
                {
                    var parent = new MzValue(MzKind.Array);
                    parent.Items.Add(tree);
                    tree = parent;
                }
                var accepted = depth <= MzSaveStore.MaxSaveDepth;
                AssertEq(MzSaveStore.Save(directory, "file1", tree, out _), accepted,
                    $"Writing allows at most {MzSaveStore.MaxSaveDepth} nested containers; depth={depth}");
                var json = new string('[', depth) + "0" + new string(']', depth);
                File.WriteAllBytes(MzSaveStore.PathOf(directory, "file1"), MzSaveStore.Zip(json));
                AssertEq(MzSaveStore.TryLoad(directory, "file1", out _, out _), accepted,
                    $"Reading uses the same container-depth convention; depth={depth}");
            }
        });
    }

    public void Test_DirectoryJunctionCannotRedirectSaveOperations()
    {
        if (!OperatingSystem.IsWindows()) { return; }
        InDirectory(directory =>
        {
            var outside = Path.Combine(Path.GetDirectoryName(directory)!, "outside");
            var linked = Path.Combine(directory, "linked");
            Directory.CreateDirectory(outside);
            AssertTrue(MzSaveStore.Save(outside, "file1", Number(111), out _), "Outside fixture is prepared explicitly");
            var target = MzSaveStore.PathOf(outside, "file1");
            var before = File.ReadAllBytes(target);
            var start = new System.Diagnostics.ProcessStartInfo("cmd.exe")
            {
                Arguments = $"/d /c mklink /J \"{linked}\" \"{outside}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(start)!;
            if (!process.WaitForExit(10000))
            {
                process.Kill();
                throw new TimeoutException("Scratch junction creation timed out.");
            }
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            AssertEq(process.ExitCode, 0, $"Scratch junction was created: {output}");
            if (process.ExitCode != 0) { return; }
            try
            {
                AssertTrue((File.GetAttributes(linked) & FileAttributes.ReparsePoint) != 0, "Fixture is a real junction");
                AssertFalse(MzSaveStore.Save(linked, "file1", Number(222), out var problem), $"Save rejects junction: {problem}");
                AssertFalse(MzSaveStore.TryLoad(linked, "file1", out var loaded, out _), "Load rejects junction");
                AssertTrue(loaded == null, "Rejected linked load produces no state");
                AssertFalse(MzSaveStore.Exists(linked, "file1"), "Linked save is not advertised");
                AssertFalse(MzSaveStore.Remove(linked, "file1"), "Remove cannot delete through a junction");
                AssertTrue(before.SequenceEqual(File.ReadAllBytes(target)), "Outside file remains byte-for-byte unchanged");
                Console.WriteLine("Save safety: real Windows junction rejected for Save, TryLoad, Exists and Remove; target unchanged.");
            }
            finally
            {
                // Delete the junction itself, not the target tree.
                Directory.Delete(linked);
            }
        });
    }

    public void Test_ValidOverwriteAndUnicodeRoundtrip()
    {
        InDirectory(directory =>
        {
            AssertTrue(MzSaveStore.Save(directory, "file1", Number(111), out _), "Initial save commits");
            var text = new MzValue(MzKind.String) { Text = "日本語 — Grüezi 🎮" };
            AssertTrue(MzSaveStore.Save(directory, "file1", text, out var problem), $"Valid overwrite commits: {problem}");
            AssertTrue(MzSaveStore.TryLoad(directory, "file1", out var value, out _), "Overwrite reads back");
            AssertEq(value!.Text, text.Text, "Unicode is preserved byte-for-byte through UTF-8");
        });
    }

    private static MzValue Number(int value) => new(MzKind.Number) { Number = value };

    private static void WriteCompressed(string directory, byte[] json)
    {
        using var file = File.Create(MzSaveStore.PathOf(directory, "file1"));
        using var zlib = new ZLibStream(file, CompressionLevel.Fastest);
        zlib.Write(json);
    }

    private static void InDirectory(Action<string> test)
    {
        var root = Environment.GetEnvironmentVariable("TMPDIR")
            ?? Path.GetTempPath();
        var sandbox = Path.Combine(root, "urpg-save-safety-" + Guid.NewGuid().ToString("N"));
        var directory = Path.Combine(sandbox, "saves");
        Directory.CreateDirectory(directory);
        try { test(directory); }
        finally { Directory.Delete(sandbox, true); }
    }
}
