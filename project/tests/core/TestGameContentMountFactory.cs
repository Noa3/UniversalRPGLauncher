using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.SdkHost;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestGameContentMountFactory : TestBase
{
    private const string Root = "user://game_content_mount_factory";

    public override void Setup()
    {
        Cleanup();
        Directory.CreateDirectory(ProjectSettings.GlobalizePath(Root));
    }

    public override void Teardown() => Cleanup();

    public void Test_PlainDirectoryMountUsesCaseInsensitiveLogicalPaths()
    {
        var root = ProjectSettings.GlobalizePath(Root.PathJoin("plain"));
        Directory.CreateDirectory(Path.Combine(root, "Data"));
        File.WriteAllText(Path.Combine(root, "Data", "Hello.TXT"), "hello");
        var library = new UniversalRpgLibraryAdapter();
        var analysis = new GameAnalysis
        {
            GameDirectory = root,
            EngineId = EnginePluginIds.RpgMaker2000,
            SupportLevel = EngineSupportLevel.PartialRuntime,
        };

        var opened = library.OpenGameContent(analysis);

        AssertTrue(opened.Success, opened.Result.ErrorMessage);
        AssertTrue(opened.Source != null);
        if (opened.Source == null) return;
        using var source = opened.Source;
        var read = source.Read("data/hello.txt");
        AssertTrue(read.Success, read.ErrorMessage);
        AssertEq(System.Text.Encoding.UTF8.GetString(read.Data.Span), "hello");
    }

    public void Test_EncryptedMvAnalysisOpensTransparentContentRoot()
    {
        var root = ProjectSettings.GlobalizePath(Root.PathJoin("mv"));
        Directory.CreateDirectory(Path.Combine(root, "data"));
        Directory.CreateDirectory(Path.Combine(root, "js", "plugins"));
        Directory.CreateDirectory(Path.Combine(root, "img"));
        File.WriteAllText(Path.Combine(root, "index.html"), "<!doctype html>");
        File.WriteAllText(Path.Combine(root, "package.json"), "{\"name\":\"rmmv\",\"version\":\"1.6.2\"}");
        File.WriteAllText(Path.Combine(root, "js", "rpg_core.js"), "// rpg_core.js v1.6.2");
        File.WriteAllText(Path.Combine(root, "js", "plugins.js"), "var $plugins = [];");
        File.WriteAllText(Path.Combine(root, "data", "System.json"),
            "{\"gameTitle\":\"Encrypted MV\",\"hasEncryptedImages\":true,\"hasEncryptedAudio\":false,\"encryptionKey\":\"00112233445566778899aabbccddeeff\"}");
        var plain = Enumerable.Range(1, 24).Select(pValue => (byte)pValue).ToArray();
        File.WriteAllBytes(Path.Combine(root, "img", "Hero.rpgmvp"), EncryptMv(plain));

        var library = new UniversalRpgLibraryAdapter();
        var analysis = library.Analyze(root);
        var opened = library.OpenGameContent(analysis);

        AssertTrue(opened.Success, opened.Result.ErrorMessage);
        AssertTrue(opened.Source != null);
        if (opened.Source == null) return;
        using var source = opened.Source;
        var read = source.Read("IMG/HERO.PNG");
        AssertTrue(read.Success, read.ErrorMessage);
        AssertTrue(read.Data.Span.SequenceEqual(plain));
        AssertFalse(File.Exists(Path.Combine(root, "img", "Hero.png")));
    }

    public void Test_ZipAnalysisPathOpensBoundedArchiveMount()
    {
        var archivePath = ProjectSettings.GlobalizePath(Root.PathJoin("game.zip"));
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("Data/Test.txt", CompressionLevel.Fastest);
            using var writer = new StreamWriter(entry.Open());
            writer.Write("zip-data");
        }
        var library = new UniversalRpgLibraryAdapter();
        var analysis = new GameAnalysis
        {
            GameDirectory = archivePath,
            EngineId = EnginePluginIds.RpgMakerMv,
            SupportLevel = EngineSupportLevel.ParsingOnly,
        };

        var opened = library.OpenGameContent(analysis);

        AssertTrue(opened.Success, opened.Result.ErrorMessage);
        AssertTrue(opened.Source != null);
        if (opened.Source == null) return;
        using var source = opened.Source;
        var read = source.Read("data/test.txt");
        AssertTrue(read.Success, read.ErrorMessage);
        AssertEq(System.Text.Encoding.UTF8.GetString(read.Data.Span), "zip-data");
    }

    private static byte[] EncryptMv(byte[] pPlain)
    {
        byte[] header =
        {
            0x52, 0x50, 0x47, 0x4d, 0x56, 0x00, 0x00, 0x00,
            0x00, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        byte[] key =
        {
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
            0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
        };
        var payload = (byte[])pPlain.Clone();
        for (var index = 0; index < Math.Min(key.Length, payload.Length); index++) payload[index] ^= key[index];
        var result = new byte[header.Length + payload.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(payload, 0, result, header.Length, payload.Length);
        return result;
    }

    private static void Cleanup()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
