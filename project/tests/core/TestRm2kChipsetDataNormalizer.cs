using System;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestRm2kChipsetDataNormalizer : TestBase
{
    public void Test_PromotesRawChipsetVectorsAndRetainsUnrelatedUnknownField()
    {
        var chipset = ChipsetWithUnknown(
            Field(0x03, new byte[] { 2, 0, 0xff, 0xff }),
            Field(0x04, new byte[] { 1, 2, 3 }),
            Field(0x05, new byte[] { 0x41, 0x0f }),
            Field(0x77, new byte[] { 9 }));

        AssertTrue(Rm2kChipsetDataNormalizer.TryNormalizeChipset(chipset, out var error), error);

        var terrain = ((Variant)chipset["terrain_data"]).AsInt32Array();
        var lower = ((Variant)chipset["passable_data_lower"]).AsByteArray();
        var upper = ((Variant)chipset["passable_data_upper"]).AsByteArray();
        AssertEq(terrain.Length, Rm2kChipsetDataNormalizer.TerrainCount);
        AssertEq(terrain[0], 2);
        AssertEq(terrain[1], -1);
        AssertEq(terrain[2], 1, "short terrain vector is padded with liblcf default terrain 1");
        AssertEq(lower[0], (byte)1);
        AssertEq(lower[1], (byte)2);
        AssertEq(lower[3], (byte)0x0f);
        AssertEq(upper[0], (byte)0x41);
        AssertEq(upper[1], (byte)0x0f);
        AssertEq(upper[2], (byte)0x0f);

        var unknown = (Godot.Collections.Array<Godot.Collections.Dictionary>)chipset["unknown_fields"];
        AssertEq(unknown.Count, 1);
        AssertEq(((Variant)unknown[0]["id"]).AsInt32(), 0x77);
    }

    public void Test_MissingVectorsUseVerifiedLiblcfDefaults()
    {
        var chipset = ChipsetWithUnknown();

        AssertTrue(Rm2kChipsetDataNormalizer.TryNormalizeChipset(chipset, out var error), error);

        var terrain = ((Variant)chipset["terrain_data"]).AsInt32Array();
        var lower = ((Variant)chipset["passable_data_lower"]).AsByteArray();
        var upper = ((Variant)chipset["passable_data_upper"]).AsByteArray();
        AssertTrue(Array.TrueForAll(terrain, pValue => pValue == 1));
        AssertTrue(Array.TrueForAll(lower, pValue => pValue == 0x0f));
        AssertEq(upper[0], (byte)0x1f);
        for (var index = 1; index < upper.Length; index++) AssertEq(upper[index], (byte)0x0f);
    }

    public void Test_AlreadyTypedVectorsAreIdempotentlyNormalized()
    {
        var chipset = new Godot.Collections.Dictionary
        {
            ["id"] = 1,
            ["terrain_data"] = new int[] { 7, 8 },
            ["passable_data_lower"] = new byte[] { 1, 2 },
            ["passable_data_upper"] = new byte[] { 3, 4 },
        };

        AssertTrue(Rm2kChipsetDataNormalizer.TryNormalizeChipset(chipset, out var error), error);
        AssertTrue(Rm2kChipsetDataNormalizer.TryNormalizeChipset(chipset, out error), error);

        var terrain = ((Variant)chipset["terrain_data"]).AsInt32Array();
        var lower = ((Variant)chipset["passable_data_lower"]).AsByteArray();
        var upper = ((Variant)chipset["passable_data_upper"]).AsByteArray();
        AssertEq(terrain[0], 7);
        AssertEq(terrain[1], 8);
        AssertEq(terrain[2], 1);
        AssertEq(lower[0], (byte)1);
        AssertEq(lower[2], (byte)0x0f);
        AssertEq(upper[0], (byte)3, "explicit typed first value overrides upper default 0x1f");
        AssertEq(upper[2], (byte)0x0f);
    }

    public void Test_RejectsOversizedRawPassageVector()
    {
        var chipset = ChipsetWithUnknown(
            Field(0x04, new byte[Rm2kChipsetDataNormalizer.LowerPassageCount + 1]));

        AssertFalse(Rm2kChipsetDataNormalizer.TryNormalizeChipset(chipset, out var error));
        AssertTrue(error.Contains("passable_data_lower", StringComparison.Ordinal));
    }

    public void Test_NormalizeDatabasePromotesEveryChipset()
    {
        var chipsets = new Godot.Collections.Array<Godot.Collections.Dictionary>
        {
            ChipsetWithUnknown(Field(0x04, new byte[] { 1 })),
            ChipsetWithUnknown(Field(0x05, new byte[] { 2 })),
        };
        chipsets[0]["id"] = 1;
        chipsets[1]["id"] = 2;
        var database = new Godot.Collections.Dictionary { ["chipsets"] = chipsets };

        AssertTrue(Rm2kChipsetDataNormalizer.TryNormalizeDatabase(database, out var error), error);
        AssertTrue(chipsets[0].ContainsKey("passable_data_upper"));
        AssertTrue(chipsets[1].ContainsKey("passable_data_lower"));
    }

    private static Godot.Collections.Dictionary ChipsetWithUnknown(params Godot.Collections.Dictionary[] pFields)
    {
        var unknown = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var field in pFields) unknown.Add(field);
        return new Godot.Collections.Dictionary
        {
            ["id"] = 1,
            ["unknown_fields"] = unknown,
        };
    }

    private static Godot.Collections.Dictionary Field(int pId, byte[] pData)
    {
        return new Godot.Collections.Dictionary
        {
            ["id"] = pId,
            ["data"] = pData,
        };
    }
}
