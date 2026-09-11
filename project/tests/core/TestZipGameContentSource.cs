using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Godot;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestZipGameContentSource : TestBase
{
    private const string Root = "user://zip_content_source";

    public override void Setup()
    {
        Cleanup();
        Directory.CreateDirectory(ProjectSettings.GlobalizePath(Root));
    }

    public override void Teardown() => Cleanup();

    public void Test_ReadsZipWithoutExtractionAndIgnoresCase()
    {
        var archivePath = ProjectSettings.GlobalizePath(Root.PathJoin("game.zip"));
        CreateZip(archivePath, ("Graphics/Characters/Hero.PNG", "hero"));

        using var source = new ZipGameContentSource(archivePath);
        var result = source.Read("graphics/characters/hero.png");

        AssertTrue(result.Success, result.ErrorMessage);
        AssertEq(Encoding.UTF8.GetString(result.Data.Span), "hero");
        AssertFalse(Directory.Exists(ProjectSettings.GlobalizePath(Root.PathJoin("Graphics"))),
            "read-only ZIP mount does not extract content beside the archive");
    }

    public void Test_RejectsArchiveTraversalEntryAtMountTime()
    {
        var archivePath = ProjectSettings.GlobalizePath(Root.PathJoin("unsafe.zip"));
        CreateZip(archivePath, ("../escape.txt", "bad"));

        var threw = false;
        try
        {
            using var ignored = new ZipGameContentSource(archivePath);
        }
        catch (InvalidDataException)
        {
            threw = true;
        }
        AssertTrue(threw, "unsafe archive path rejects the complete mount");
    }

    public void Test_RejectsCaseCollidingEntries()
    {
        var archivePath = ProjectSettings.GlobalizePath(Root.PathJoin("collision.zip"));
        CreateZip(
            archivePath,
            ("Data/System.json", "a"),
            ("data/system.JSON", "b"));

        var threw = false;
        try
        {
            using var ignored = new ZipGameContentSource(archivePath);
        }
        catch (InvalidDataException)
        {
            threw = true;
        }
        AssertTrue(threw, "case-colliding paths are ambiguous for Windows-origin games and fail closed");
    }

    public void Test_EntrySizeLimitRejectsOversizedArchiveEntry()
    {
        var archivePath = ProjectSettings.GlobalizePath(Root.PathJoin("large.zip"));
        CreateZip(archivePath, ("Data/Large.bin", new string('x', 256)));

        var threw = false;
        try
        {
            using var ignored = new ZipGameContentSource(
                archivePath,
                pLimits: new ZipGameContentLimits
                {
                    MaxEntryBytes = 128,
                    MaxTotalUncompressedBytes = 1024,
                    MaxExpansionRatio = 1000,
                });
        }
        catch (InvalidDataException)
        {
            threw = true;
        }
        AssertTrue(threw);
    }

    public void Test_LayeredSourceCanUseZipAsGameLayer()
    {
        var archivePath = ProjectSettings.GlobalizePath(Root.PathJoin("layered.zip"));
        CreateZip(archivePath, ("Data/value.txt", "from zip"));
        var overrideRoot = ProjectSettings.GlobalizePath(Root.PathJoin("override"));
        Directory.CreateDirectory(Path.Combine(overrideRoot, "Data"));
        File.WriteAllText(Path.Combine(overrideRoot, "Data", "other.txt"), "override-only");

        using var layered = new LayeredGameContentSource(new[]
        {
            new LayeredGameContentSource.Layer
            {
                Id = "override",
                Source = new DirectoryGameContentSource(overrideRoot, "override"),
            },
            new LayeredGameContentSource.Layer
            {
                Id = "game-archive",
                Source = new ZipGameContentSource(archivePath, "game-archive"),
            },
        });

        var result = layered.Read("data/VALUE.TXT");
        AssertTrue(result.Success);
        AssertEq(Encoding.UTF8.GetString(result.Data.Span), "from zip");
    }

    private static void CreateZip(string pPath, params (string Path, string Text)[] pEntries)
    {
        using var stream = File.Open(pPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false);
        foreach (var item in pEntries)
        {
            var entry = archive.CreateEntry(item.Path, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
            writer.Write(item.Text);
        }
    }

    private static void Cleanup()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
