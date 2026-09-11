using System;
using Godot;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Tests.Core;

public partial class TestPluginDetection
{
    public void Test_Rm2kPassabilityUsesDocumentedChipsetDefaults()
    {
        var database = PassabilityDatabase(1);
        var map = PassabilityMap(
            2, 1, 1,
            new[] { 2000, 2000 },
            new[] { 10000, 10000 });

        var created = Rm2kPassabilityMap.TryCreate(database, map, out var passability, out var error);

        AssertTrue(created, error);
        AssertTrue(passability != null);
        AssertTrue(passability!.CanMove(0, 0, 1, 0),
            "default chipset passage flags allow horizontal movement in both source and target directions");
        AssertTrue(passability.CanMove(1, 0, 0, 0));
        AssertFalse(passability.CanMove(0, 0, 0, 2), "non-cardinal/out-of-bounds moves remain blocked");
    }

    public void Test_Rm2kPassabilityChecksSourceAndTargetDirectionBits()
    {
        var lower = new byte[Rm2kPassabilityMap.LowerPassageCount];
        Array.Fill(lower, (byte)0x0F);
        // Raw lower tile 2000 maps to lower passage index 2. Remove Right while
        // retaining every other direction to prove the source tile is checked.
        lower[2] = (byte)(Rm2kPassabilityMap.Down | Rm2kPassabilityMap.Left | Rm2kPassabilityMap.Up);

        var database = PassabilityDatabase(1, lower, null);
        var map = PassabilityMap(
            2, 1, 1,
            new[] { 2000, 2000 },
            new[] { 10000, 10000 });

        AssertTrue(Rm2kPassabilityMap.TryCreate(database, map, out var passability, out var error), error);
        AssertTrue(passability != null);
        AssertFalse(passability!.CanMove(0, 0, 1, 0),
            "movement is blocked when the source tile disallows the outgoing direction");
        AssertTrue(passability.CanMove(1, 0, 0, 0),
            "the same passage table still permits the reverse direction because Left remains set");
    }

    public void Test_Rm2kPassabilityHonorsUpperLayerAndRejectsOversizedPassageData()
    {
        var upper = new byte[Rm2kPassabilityMap.UpperPassageCount];
        Array.Fill(upper, (byte)0x0F);
        // Without Above, the upper layer itself decides passage. Allow only
        // Left/Right on upper tile index 0.
        upper[0] = (byte)(Rm2kPassabilityMap.Left | Rm2kPassabilityMap.Right);
        var database = PassabilityDatabase(1, null, upper);
        var map = PassabilityMap(
            2, 1, 1,
            new[] { 2000, 2000 },
            new[] { 10000, 10000 });

        AssertTrue(Rm2kPassabilityMap.TryCreate(database, map, out var passability, out var error), error);
        AssertTrue(passability != null && passability.CanMove(0, 0, 1, 0));
        AssertFalse(passability!.AllowsDirection(0, 0, Rm2kPassabilityMap.Down));

        var oversized = new byte[Rm2kPassabilityMap.LowerPassageCount + 1];
        var invalidDatabase = PassabilityDatabase(1, oversized, null);
        AssertFalse(Rm2kPassabilityMap.TryCreate(invalidDatabase, map, out _, out var invalidError));
        AssertTrue(invalidError.Contains("expected at most", StringComparison.Ordinal));
    }

    private static Godot.Collections.Dictionary PassabilityDatabase(
        int pChipsetId,
        byte[]? pLower = null,
        byte[]? pUpper = null)
    {
        var unknownFields = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        if (pLower != null)
        {
            unknownFields.Add(new Godot.Collections.Dictionary
            {
                { "id", 0x04 },
                { "data", pLower },
            });
        }
        if (pUpper != null)
        {
            unknownFields.Add(new Godot.Collections.Dictionary
            {
                { "id", 0x05 },
                { "data", pUpper },
            });
        }

        var chipsets = new Godot.Collections.Array<Godot.Collections.Dictionary>
        {
            new Godot.Collections.Dictionary
            {
                { "id", pChipsetId },
                { "unknown_fields", unknownFields },
            },
        };
        return new Godot.Collections.Dictionary { { "chipsets", chipsets } };
    }

    private static Godot.Collections.Dictionary PassabilityMap(
        int pWidth,
        int pHeight,
        int pChipsetId,
        int[] pLower,
        int[] pUpper)
    {
        return new Godot.Collections.Dictionary
        {
            { "width", pWidth },
            { "height", pHeight },
            { "chipset_id", pChipsetId },
            { "lower_layer", pLower },
            { "upper_layer", pUpper },
        };
    }
}
