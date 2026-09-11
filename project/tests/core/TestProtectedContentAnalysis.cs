using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.SdkHost;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestProtectedContentAnalysis : TestBase
{
    private const string Root = "user://protected_content_analysis";

    public override void Setup()
    {
        Cleanup();
        Directory.CreateDirectory(ProjectSettings.GlobalizePath(Root));
    }

    public override void Teardown() => Cleanup();

    public void Test_LibraryAnalysisReportsReadableMvEncryption()
    {
        var root = ProjectSettings.GlobalizePath(Root.PathJoin("mv"));
        CreateMvGame(root, encrypted: true);
        var library = new UniversalRpgLibraryAdapter();

        var analysis = library.Analyze(root);

        AssertEq(analysis.EngineId, EnginePluginIds.RpgMakerMv);
        AssertTrue(analysis.HasProtectedContent);
        AssertEq(analysis.ProtectedContent.Count, 1);
        var status = analysis.ProtectedContent[0];
        AssertEq(status.Descriptor.SchemeId, ProtectedContentSchemes.RpgMakerMvAssets);
        AssertTrue(status.RuntimeReadable, "MV built-in encryption has a trusted transparent provider");
        AssertEq(status.ProviderId, "builtin-rpg-maker-mv-mz-assets");
    }

    public void Test_LibraryCanOpenDescriptorReturnedByAnalysis()
    {
        var root = ProjectSettings.GlobalizePath(Root.PathJoin("mv_open"));
        CreateMvGame(root, encrypted: true);
        var plain = Enumerable.Range(1, 32).Select(pValue => (byte)pValue).ToArray();
        File.WriteAllBytes(Path.Combine(root, "img", "Test.rpgmvp"), EncryptMv(plain));
        var library = new UniversalRpgLibraryAdapter();
        var analysis = library.Analyze(root);

        var opened = library.OpenProtectedContent(analysis.ProtectedContent[0].Descriptor);

        AssertTrue(opened.Success, opened.Result.ErrorMessage);
        AssertTrue(opened.Source != null);
        if (opened.Source == null) return;
        using var source = opened.Source;
        var read = source.Read("img/Test.png");
        AssertTrue(read.Success, read.ErrorMessage);
        AssertTrue(read.Data.Span.SequenceEqual(plain));
        AssertFalse(File.Exists(Path.Combine(root, "img", "Test.png")),
            "public SDK content mount remains read-only/in-memory");
    }

    public void Test_RgssEncryptedArchiveIsReportedButNotPretendedReadable()
    {
        var root = ProjectSettings.GlobalizePath(Root.PathJoin("xp"));
        Directory.CreateDirectory(Path.Combine(root, "Data"));
        File.WriteAllText(Path.Combine(root, "Game.ini"), "[Game]\nTitle=EncryptedXP\nLibrary=RGSS102A.dll\n");
        File.WriteAllBytes(Path.Combine(root, "RGSS102A.dll"), new byte[] { 1, 2, 3 });
        File.WriteAllBytes(Path.Combine(root, "Game.rgssad"), new byte[] { 1, 2, 3, 4 });
        var library = new UniversalRpgLibraryAdapter();

        var analysis = library.Analyze(root);

        AssertEq(analysis.EngineId, EnginePluginIds.RpgMakerXp);
        AssertTrue(analysis.HasProtectedContent);
        var status = analysis.ProtectedContent.First(pItem => pItem.Descriptor.SchemeId == ProtectedContentSchemes.Rgss1Archive);
        AssertFalse(status.RuntimeReadable,
            "RGSS archive is detected but no built-in provider is claimed before its legal/compatibility implementation is approved");
    }

    public void Test_WolfProtectedArchiveBlocksRuntimeSession()
    {
        var root = ProjectSettings.GlobalizePath(Root.PathJoin("wolf"));
        Directory.CreateDirectory(Path.Combine(root, "BasicData"));
        File.WriteAllText(Path.Combine(root, "Game.dat"), "WOLF");
        File.WriteAllBytes(Path.Combine(root, "Data.wolf"), new byte[] { 1, 2, 3, 4 });
        var library = new UniversalRpgLibraryAdapter();
        var analysis = library.Analyze(root);

        AssertEq(analysis.EngineId, EnginePluginIds.WolfRpg);
        AssertTrue(analysis.HasProtectedContent);
        AssertFalse(analysis.ProtectedContent[0].RuntimeReadable);

        var session = library.CreateSession(analysis);
        AssertFalse(session.Success);
        AssertEq(session.Result.ErrorCode, "session.protected-content-unavailable");
    }

    public void Test_PlainMvGameDoesNotReportProtectedContent()
    {
        var root = ProjectSettings.GlobalizePath(Root.PathJoin("mv_plain"));
        CreateMvGame(root, encrypted: false);
        var library = new UniversalRpgLibraryAdapter();

        var analysis = library.Analyze(root);

        AssertEq(analysis.EngineId, EnginePluginIds.RpgMakerMv);
        AssertFalse(analysis.HasProtectedContent);
    }

    private static void CreateMvGame(string pRoot, bool encrypted)
    {
        Directory.CreateDirectory(Path.Combine(pRoot, "data"));
        Directory.CreateDirectory(Path.Combine(pRoot, "js", "plugins"));
        Directory.CreateDirectory(Path.Combine(pRoot, "img"));
        File.WriteAllText(Path.Combine(pRoot, "index.html"), "<!DOCTYPE html><html><body></body></html>");
        File.WriteAllText(Path.Combine(pRoot, "package.json"), "{\"name\":\"rmmv\",\"version\":\"1.6.2\"}");
        File.WriteAllText(Path.Combine(pRoot, "js", "rpg_core.js"), "// rpg_core.js v1.6.2\n");
        File.WriteAllText(Path.Combine(pRoot, "js", "plugins.js"), "var $plugins = [];\n");
        File.WriteAllText(
            Path.Combine(pRoot, "data", "System.json"),
            encrypted
                ? "{\"gameTitle\":\"Encrypted MV\",\"hasEncryptedImages\":true,\"hasEncryptedAudio\":false,\"encryptionKey\":\"00112233445566778899aabbccddeeff\"}"
                : "{\"gameTitle\":\"Plain MV\",\"hasEncryptedImages\":false,\"hasEncryptedAudio\":false,\"encryptionKey\":\"\"}");
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
        for (var index = 0; index < Math.Min(16, payload.Length); index++) payload[index] ^= key[index];
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
