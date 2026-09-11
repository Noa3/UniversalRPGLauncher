using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UniversalRPG.Rgss;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestRgssScriptArchiveReader : TestBase
{
    public void Test_ReadsOrderedRgss3ScriptsAndDecompressesSource()
    {
        var archive = BuildArchive(
            (10, "Core", "class Core; end\n", false),
            (20, "Main", "puts 'hello'\n", true));

        var result = RgssScriptArchiveReader.Read(archive, RgssGeneration.Rgss3);

        AssertTrue(result.Success, result.Error);
        AssertEq(result.Scripts.Count, 2);
        AssertEq(result.Scripts[0].Id, 10);
        AssertEq(result.Scripts[0].Name, "Core");
        AssertEq(result.Scripts[0].ArchiveIndex, 0);
        AssertEq(Encoding.UTF8.GetString(result.Scripts[0].Source), "class Core; end\n");
        AssertEq(result.Scripts[1].Name, "Main");
        AssertEq(result.Scripts[1].Descriptor.LoadOrder, 1);
        AssertEq(result.Scripts[1].Descriptor.LanguageId, ScriptLanguageIds.Rgss3Ruby);
        AssertEq(result.Scripts[1].Descriptor.Sha256.Length, 64);
        AssertTrue(result.Scripts.All(pScript => pScript.Descriptor.Validate().Success));
    }

    public void Test_MapsGenerationToCorrectScriptLanguage()
    {
        var archive = BuildArchive((1, "Main", "nil\n", false));

        AssertEq(
            RgssScriptArchiveReader.Read(archive, RgssGeneration.Rgss1).Scripts[0].Descriptor.LanguageId,
            ScriptLanguageIds.Rgss1Ruby);
        AssertEq(
            RgssScriptArchiveReader.Read(archive, RgssGeneration.Rgss2).Scripts[0].Descriptor.LanguageId,
            ScriptLanguageIds.Rgss2Ruby);
        AssertEq(
            RgssScriptArchiveReader.Read(archive, RgssGeneration.Rgss3).Scripts[0].Descriptor.LanguageId,
            ScriptLanguageIds.Rgss3Ruby);
    }

    public void Test_RejectsInvalidMarshalHeaderAndUnsupportedTypes()
    {
        var invalidHeader = RgssScriptArchiveReader.Read(new byte[] { 4, 7, (byte)'0' }, RgssGeneration.Rgss1);
        AssertFalse(invalidHeader.Success);
        AssertTrue(invalidHeader.Error.Contains("Marshal header", StringComparison.Ordinal));

        var unsupported = new byte[] { 4, 8, (byte)'{' };
        var unsupportedResult = RgssScriptArchiveReader.Read(unsupported, RgssGeneration.Rgss1);
        AssertFalse(unsupportedResult.Success);
        AssertTrue(unsupportedResult.Error.Contains("Unsupported Ruby Marshal type", StringComparison.Ordinal));
    }

    public void Test_RejectsDecompressedScriptAboveConfiguredLimit()
    {
        var source = new string('x', 4096);
        var archive = BuildArchive((1, "Huge", source, false));
        var limits = new RgssScriptArchiveLimits
        {
            MaxScripts = 10,
            MaxArchiveBytes = 1024 * 1024,
            MaxCompressedScriptBytes = 1024 * 1024,
            MaxDecompressedScriptBytes = 1024,
            MaxTotalDecompressedBytes = 4096,
            MaxMarshalDepth = 64,
        };

        var result = RgssScriptArchiveReader.Read(archive, RgssGeneration.Rgss3, limits);

        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("decompressed", StringComparison.OrdinalIgnoreCase));
    }

    public void Test_RejectsNonScriptEntryShape()
    {
        var bytes = new List<byte> { 4, 8, (byte)'[' };
        bytes.AddRange(Fixnum(1));
        bytes.Add((byte)'[');
        bytes.AddRange(Fixnum(2));
        bytes.Add((byte)'i');
        bytes.AddRange(Fixnum(1));
        AddString(bytes, Encoding.ASCII.GetBytes("OnlyName"), ivar: false);

        var result = RgssScriptArchiveReader.Read(bytes.ToArray(), RgssGeneration.Rgss1);

        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("three-element", StringComparison.Ordinal));
    }

    private static byte[] BuildArchive(params (int Id, string Name, string Source, bool IvarName)[] pScripts)
    {
        var bytes = new List<byte> { 4, 8, (byte)'[' };
        bytes.AddRange(Fixnum(pScripts.Length));
        foreach (var script in pScripts)
        {
            bytes.Add((byte)'[');
            bytes.AddRange(Fixnum(3));

            bytes.Add((byte)'i');
            bytes.AddRange(Fixnum(script.Id));

            AddString(bytes, Encoding.UTF8.GetBytes(script.Name), script.IvarName);
            AddString(bytes, Compress(Encoding.UTF8.GetBytes(script.Source)), ivar: false);
        }
        return bytes.ToArray();
    }

    private static void AddString(List<byte> pBytes, byte[] pData, bool ivar)
    {
        if (ivar) pBytes.Add((byte)'I');
        pBytes.Add((byte)'"');
        pBytes.AddRange(Fixnum(pData.Length));
        pBytes.AddRange(pData);
        if (!ivar) return;

        pBytes.AddRange(Fixnum(1));
        pBytes.Add((byte)':');
        pBytes.AddRange(Fixnum(1));
        pBytes.Add((byte)'E');
        pBytes.Add((byte)'T');
    }

    private static byte[] Compress(byte[] pSource)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(pSource, 0, pSource.Length);
        }
        return output.ToArray();
    }

    private static byte[] Fixnum(int pValue)
    {
        if (pValue == 0) return new byte[] { 0 };
        if (pValue is >= 1 and <= 122) return new[] { (byte)(pValue + 5) };
        if (pValue is <= -1 and >= -123) return new[] { unchecked((byte)(sbyte)(pValue - 5)) };

        if (pValue > 0)
        {
            var count = 1;
            while (count < 4 && pValue >= (1 << (count * 8))) count++;
            var result = new byte[count + 1];
            result[0] = (byte)count;
            for (var index = 0; index < count; index++) result[index + 1] = (byte)(pValue >> (8 * index));
            return result;
        }

        var bytes = BitConverter.GetBytes(pValue);
        var negativeCount = 4;
        while (negativeCount > 1 && bytes[negativeCount - 1] == 0xFF && (bytes[negativeCount - 2] & 0x80) != 0)
        {
            negativeCount--;
        }
        var negativeResult = new byte[negativeCount + 1];
        negativeResult[0] = unchecked((byte)(sbyte)(-negativeCount));
        Array.Copy(bytes, 0, negativeResult, 1, negativeCount);
        return negativeResult;
    }
}
