using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestRm2kPassabilityImmutability : TestBase
{
    public void Test_LegacyChipsetNormalizationDoesNotMutateParsedDatabase()
    {
        var lower = Enumerable.Repeat((byte)0x0f, Rm2kPassabilityMap.LowerPassageCount).ToArray();
        var upper = Enumerable.Repeat((byte)0x0f, Rm2kPassabilityMap.UpperPassageCount).ToArray();
        upper[0] = 0x1f;

        var unknown = new Godot.Collections.Array<Godot.Collections.Dictionary>
        {
            Field(0x04, lower),
            Field(0x05, upper),
        };
        var chipset = new Godot.Collections.Dictionary
        {
            { "id", 1 },
            { "name", "Legacy" },
            { "unknown_fields", unknown },
        };
        var chipsets = new Godot.Collections.Array<Godot.Collections.Dictionary> { chipset };
        var database = new Godot.Collections.Dictionary { { "chipsets", chipsets } };
        var map = new Godot.Collections.Dictionary
        {
            { "width", 1 },
            { "height", 1 },
            { "chipset_id", 1 },
            { "lower_layer", new int[] { 5000 } },
            { "upper_layer", new int[] { 10000 } },
        };

        var created = Rm2kPassabilityMap.TryCreate(database, map, out var passability, out var error);

        AssertTrue(created, error);
        AssertTrue(passability != null);
        AssertFalse(chipset.ContainsKey("passable_data_lower"),
            "runtime normalization must not add typed passage fields to parsed chipset data");
        AssertFalse(chipset.ContainsKey("passable_data_upper"),
            "runtime normalization must not add typed passage fields to parsed chipset data");
        AssertEq(((Godot.Collections.Array<Godot.Collections.Dictionary>)chipset["unknown_fields"]).Count, 2,
            "legacy raw fields remain intact on the original parser object");
    }

    private static Godot.Collections.Dictionary Field(int pId, byte[] pData)
    {
        return new Godot.Collections.Dictionary
        {
            { "id", pId },
            { "data", pData },
        };
    }
}
