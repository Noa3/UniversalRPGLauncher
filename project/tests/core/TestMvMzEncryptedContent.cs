using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestMvMzEncryptedContent : TestBase
{
    private const string Root = "user://mv_mz_encrypted_content";
    private static readonly byte[] Header =
    {
        0x52, 0x50, 0x47, 0x4d, 0x56, 0x00, 0x00, 0x00,
        0x00, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
    };
    private static readonly byte[] Key =
    {
        0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
        0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
    };

    public override void Setup()
    {
        Cleanup();
        var root = ProjectSettings.GlobalizePath(Root);
        Directory.CreateDirectory(Path.Combine(root, "data"));
        Directory.CreateDirectory(Path.Combine(root, "img"));
        File.WriteAllText(
            Path.Combine(root, "data", "System.json"),
            "{\"hasEncryptedImages\":true,\"hasEncryptedAudio\":true,\"encryptionKey\":\"00112233445566778899aabbccddeeff\"}");
    }

    public override void Teardown() => Cleanup();

    public void Test_DecryptsRpgmvpInMemoryUsingSystemKey()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        var plain = Enumerable.Range(0, 48).Select(pIndex => (byte)(pIndex + 1)).ToArray();
        File.WriteAllBytes(Path.Combine(root, "img", "Picture.rpgmvp"), Encrypt(plain));

        using var source = new MvMzGameContentSource(root);
        var result = source.Read("img/Picture.png");

        AssertTrue(result.Success, result.ErrorMessage);
        AssertEq(result.Data.ToArray(), plain);
        AssertEq(source.Protection, GameContentProtectionKind.EngineManagedEncryption);
        AssertFalse(File.Exists(Path.Combine(root, "img", "Picture.png")),
            "transparent runtime read must not extract a plaintext file");
    }

    public void Test_DecryptsMzUnderscoreExtensionVariant()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        var plain = Enumerable.Range(0, 40).Select(pIndex => (byte)(255 - pIndex)).ToArray();
        File.WriteAllBytes(Path.Combine(root, "img", "Face.png_"), Encrypt(plain));

        using var source = new MvMzGameContentSource(root);
        AssertTrue(source.Exists("img/Face.png"));
        var result = source.Read("img/Face.png");

        AssertTrue(result.Success, result.ErrorMessage);
        AssertEq(result.Data.ToArray(), plain);
    }

    public void Test_PlainAssetTakesPrecedenceOverEncryptedVariant()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        var plain = new byte[] { 1, 2, 3, 4 };
        var encryptedPlain = new byte[] { 9, 8, 7, 6 };
        File.WriteAllBytes(Path.Combine(root, "img", "Window.png"), plain);
        File.WriteAllBytes(Path.Combine(root, "img", "Window.rpgmvp"), Encrypt(encryptedPlain));

        using var source = new MvMzGameContentSource(root);
        var result = source.Read("img/Window.png");

        AssertTrue(result.Success);
        AssertEq(result.Data.ToArray(), plain);
    }

    public void Test_RejectsTraversalOutsideGameRoot()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        using var source = new MvMzGameContentSource(root);

        var result = source.Read("../secret.txt");

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "content.path-invalid");
    }

    public void Test_RejectsInvalidEncryptedHeader()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        File.WriteAllBytes(Path.Combine(root, "img", "Bad.rpgmvp"), new byte[64]);
        using var source = new MvMzGameContentSource(root);

        var result = source.Read("img/Bad.png");

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "mv-mz.header-invalid");
    }

    public void Test_MetadataRejectsMalformedKey()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        File.WriteAllText(
            Path.Combine(root, "data", "System.json"),
            "{\"hasEncryptedImages\":true,\"encryptionKey\":\"bad\"}");

        var result = MvMzEncryptedAssetCodec.ReadMetadata(Path.Combine(root, "data", "System.json"));

        AssertFalse(result.Success);
        AssertEq(result.Result.ErrorCode, "mv-mz.encryption-key-invalid");
    }

    private static byte[] Encrypt(byte[] pPlain)
    {
        var payload = (byte[])pPlain.Clone();
        for (var index = 0; index < Math.Min(Key.Length, payload.Length); index++)
        {
            payload[index] ^= Key[index];
        }
        var encrypted = new byte[Header.Length + payload.Length];
        Buffer.BlockCopy(Header, 0, encrypted, 0, Header.Length);
        Buffer.BlockCopy(payload, 0, encrypted, Header.Length, payload.Length);
        return encrypted;
    }

    private static void Cleanup()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
