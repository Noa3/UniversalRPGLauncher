using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kLsdSaveModel : TestBase
{
    private const string RootUri = "user://rm2k_lsd_model";
    private string _root = "";

    public override void Setup()
    {
        Teardown();
        _root = ProjectSettings.GlobalizePath(RootUri);
        Directory.CreateDirectory(_root);
    }

    public override void Teardown()
    {
        var root = ProjectSettings.GlobalizePath(RootUri);
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    public void Test_ReadOnlyModelPreservesChunkFramingAndUnknownPayload()
    {
        var payload = new byte[] { 0x10, 0x20, 0x30 };
        Write("Save001.rmm", Lcf(
            new[]
            {
                Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("TestGame")),
                Chunk(0x99, payload),
            }));

        var result = Rm2kLsdSaveCodec.TryReadFile(_root, "Save001.rmm", out var model, out var error);

        AssertTrue(result, error);
        AssertTrue(model != null);
        AssertEq(model!.Chunks.Count, 2);
        AssertEq(model.UnknownChunks.Count, 1);
        AssertEq(model.UnknownChunks[0].Id, 0x99);
        AssertEq(model.UnknownChunks[0].Length, 3);
        AssertEq(model.UnknownChunks[0].Offset, 22);
        AssertEq(model.UnknownChunks[0].PayloadOffset, 25);
        AssertEq(model.UnknownChunks[0].Data[2], (byte)0x30);
        AssertEq(model.UnknownChunkCount, 1);
    }

    public void Test_ReadOnlyModelRejectsTraversalAndAbsoluteSlots()
    {
        AssertFalse(Rm2kLsdSaveCodec.TryReadFile(_root, "../Save001.rmm", out _, out var traversalError));
        AssertTrue(traversalError.Contains("slot", StringComparison.OrdinalIgnoreCase));

        AssertFalse(Rm2kLsdSaveCodec.TryReadFile(_root, Path.Combine(_root, "Save001.rmm"), out _, out var absoluteError));
        AssertTrue(absoluteError.Contains("slot", StringComparison.OrdinalIgnoreCase));
    }

    public void Test_ReadOnlyModelRejectsOversizedAndTruncatedSaves()
    {
        Write("Huge.rmm", new byte[Rm2kLsdSaveCodec.MaxFileBytes + 1]);
        AssertFalse(Rm2kLsdSaveCodec.TryReadFile(_root, "Huge.rmm", out _, out var sizeError));
        AssertTrue(sizeError.Contains("limit", StringComparison.OrdinalIgnoreCase));

        Write("Broken.rmm", new byte[] { 11, (byte)'L', (byte)'c' });
        AssertFalse(Rm2kLsdSaveCodec.TryReadFile(_root, "Broken.rmm", out _, out var brokenError));
        AssertTrue(brokenError.Length > 0);

        var withoutTerminator = new List<byte> { 11 };
        withoutTerminator.AddRange(System.Text.Encoding.ASCII.GetBytes("LcfSaveData"));
        withoutTerminator.AddRange(Chunk(1, Array.Empty<byte>()));
        Write("NoTerminator.rmm", withoutTerminator.ToArray());
        AssertFalse(Rm2kLsdSaveCodec.TryReadFile(_root, "NoTerminator.rmm", out _, out var terminatorError));
        AssertTrue(terminatorError.Contains("terminator", StringComparison.OrdinalIgnoreCase));
    }

    private void Write(string pName, byte[] pData)
    {
        File.WriteAllBytes(Path.Combine(_root, pName), pData);
    }

    private static byte[] Lcf(IReadOnlyList<byte[]> pChunks)
    {
        var result = new List<byte> { 11 };
        result.AddRange(System.Text.Encoding.ASCII.GetBytes("LcfSaveData"));
        foreach (var chunk in pChunks)
        {
            result.AddRange(chunk);
        }
        result.Add(0);
        return result.ToArray();
    }

    private static byte[] Chunk(int pId, byte[] pData)
    {
        var result = new List<byte>();
        result.AddRange(Ber(pId));
        result.AddRange(Ber(pData.Length));
        result.AddRange(pData);
        return result.ToArray();
    }

    private static byte[] Ber(int pValue)
    {
        var groups = new List<byte> { (byte)(pValue & 0x7f) };
        pValue >>= 7;
        while (pValue > 0)
        {
            groups.Add((byte)((pValue & 0x7f) | 0x80));
            pValue >>= 7;
        }
        groups.Reverse();
        return groups.ToArray();
    }
}
