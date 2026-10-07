using System;
using System.IO;
using System.IO.Compression;
using Godot;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRtpArchiveSafety : TestBase
{
    private string _root = "";

    public override void Setup()
    {
        _root = ProjectSettings.GlobalizePath("user://archive-safety/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public override void Teardown()
    {
        Directory.Delete(_root, true);
    }

    public void Test_DirectoryTraversalCannotCreateOutsideDirectory()
    {
        var archive = MakeArchive("../outside/");
        var result = RtpEntpacker.Entpacke(archive, Path.Combine(_root, "target"));
        AssertFalse(Directory.Exists(Path.Combine(_root, "outside")), "Directory entries must be confined before creation.");
        AssertEq(result.Verweigert.Count, 1);
    }

    public void Test_SiblingWithSamePrefixIsNotInsideTarget()
    {
        var archive = MakeArchive("../target-sibling/file.txt");
        var result = RtpEntpacker.Entpacke(archive, Path.Combine(_root, "target"));
        AssertFalse(File.Exists(Path.Combine(_root, "target-sibling", "file.txt")), "A common prefix is not root containment.");
        AssertEq(result.Verweigert.Count, 1);
    }

    public void Test_ExistingFileIsNeverOverwritten()
    {
        var target = Path.Combine(_root, "target");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "file.txt"), "original");
        var result = RtpEntpacker.Entpacke(MakeArchive("file.txt"), target);
        AssertEq(File.ReadAllText(Path.Combine(target, "file.txt")), "original");
        AssertEq(result.Verweigert.Count, 1);
    }

    public void Test_DuplicateEntryCannotReplaceFirstFile()
    {
        var path = MakeArchive("file.txt");
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            using var writer = new StreamWriter(archive.CreateEntry("file.txt").Open());
            writer.Write("replacement");
        }
        var result = RtpEntpacker.Entpacke(path, Path.Combine(_root, "target"));
        AssertEq(result.Dateien, 1);
        AssertEq(File.ReadAllText(Path.Combine(_root, "target", "file.txt")), "archive content");
        AssertEq(result.Verweigert.Count, 1);
    }

    public void Test_ArchiveSymbolicLinkIsRefused()
    {
        var path = MakeArchive("link.txt");
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            archive.Entries[0].ExternalAttributes = unchecked((int)0xA1FF0000);
        }
        var result = RtpEntpacker.Entpacke(path, Path.Combine(_root, "target"));
        AssertEq(result.Dateien, 0);
        AssertEq(result.Verweigert.Count, 1);
    }

    public void Test_CompressedOversizedFileIsRefusedBeforeWriting()
    {
        var path = Path.Combine(_root, "large.zip");
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            using var stream = archive.CreateEntry("bomb.bin").Open();
            var block = new byte[1024 * 1024];
            for (var i = 0; i < 257; i++) stream.Write(block);
        }
        var result = RtpEntpacker.Entpacke(path, Path.Combine(_root, "target"));
        AssertFalse(File.Exists(Path.Combine(_root, "target", "bomb.bin")));
        AssertEq(result.Verweigert.Count, 1);
    }

    public void Test_WindowsNormalizedPathAliasesAreRefused()
    {
        foreach (var entry in new[] { "folder /file.txt", "NUL.txt", "COM1.txt" })
        {
            var path = MakeArchive(entry);
            var result = RtpEntpacker.Entpacke(path, Path.Combine(_root, "target"));
            AssertEq(result.Dateien, 0, "Windows path aliases must not reach the filesystem: " + entry);
            AssertEq(result.Verweigert.Count, 1);
            File.Delete(path);
        }
    }

    private string MakeArchive(string pEntry)
    {
        var path = Path.Combine(_root, "input.zip");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var entry = archive.CreateEntry(pEntry);
        if (!pEntry.EndsWith('/'))
        {
            using var writer = new StreamWriter(entry.Open());
            writer.Write("archive content");
        }
        return path;
    }
}
