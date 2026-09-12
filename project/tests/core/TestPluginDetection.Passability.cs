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
        // Use DIFFERENT lower tiles so source.Right and target.Left can be
        // varied independently. Reversing an edge checks the same two flags;
        // retaining Left on the source alone does not make reverse travel safe.
        var map = PassabilityMap(2, 1, 1, new[] { 2000, 1000 }, new[] { 10000, 10000 });
        for (var sourceMask = 0; sourceMask < 16; sourceMask++)
        {
            for (var targetMask = 0; targetMask < 16; targetMask++)
            {
                var lower = new byte[Rm2kPassabilityMap.LowerPassageCount];
                var upper = new byte[Rm2kPassabilityMap.UpperPassageCount];
                Array.Fill(lower, (byte)0x0F);
                Array.Fill(upper, (byte)0x1F);
                lower[2] = (byte)sourceMask;
                lower[1] = (byte)targetMask;
                var database = PassabilityDatabase(1, lower, upper);
                AssertTrue(Rm2kPassabilityMap.TryCreate(database, map, out var passage, out var error), error);
                AssertTrue(passage != null);
                if (passage == null) return;
                var expected = (sourceMask & Rm2kPassabilityMap.Right) != 0
                    && (targetMask & Rm2kPassabilityMap.Left) != 0;
                AssertEq(passage.CanMove(0, 0, 1, 0), expected,
                    $"source.Right and target.Left must both allow the edge ({sourceMask:X}/{targetMask:X})");
                AssertEq(passage.CanMove(1, 0, 0, 0), expected,
                    "reverse movement checks the same adjacent edge flags");
            }
        }
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

    public void Test_Rm2kPassabilityAcceptsLegacyRawPassageFieldsDuringParserMigration()
    {
        var lower = new byte[Rm2kPassabilityMap.LowerPassageCount];
        Array.Fill(lower, (byte)0x0F);
        lower[2] = (byte)(Rm2kPassabilityMap.Down | Rm2kPassabilityMap.Left | Rm2kPassabilityMap.Up);

        var database = LegacyRawPassabilityDatabase(1, lower, null);
        var map = PassabilityMap(
            2, 1, 1,
            new[] { 2000, 2000 },
            new[] { 10000, 10000 });

        AssertTrue(Rm2kPassabilityMap.TryCreate(database, map, out var passability, out var error), error);
        AssertTrue(passability != null);
        AssertFalse(passability!.CanMove(0, 0, 1, 0),
            "legacy unknown-field chipset data retains the same directional semantics");
    }

    public void Test_Rm2kPassabilityRejectsInvalidMapBoundsBeforeAllocation()
    {
        var database = PassabilityDatabase(1);
        var map = PassabilityMap(
            501, 1, 1,
            Array.Empty<int>(),
            Array.Empty<int>());

        AssertFalse(Rm2kPassabilityMap.TryCreate(database, map, out _, out var error));
        AssertTrue(error.Contains("dimensions", StringComparison.OrdinalIgnoreCase));
    }

    private static Godot.Collections.Dictionary PassabilityDatabase(
        int pChipsetId,
        byte[]? pLower = null,
        byte[]? pUpper = null)
    {
        var chipset = new Godot.Collections.Dictionary
        {
            { "id", pChipsetId },
            { "unknown_fields", new Godot.Collections.Array<Godot.Collections.Dictionary>() },
        };
        if (pLower != null)
        {
            chipset["passable_data_lower"] = pLower;
        }
        if (pUpper != null)
        {
            chipset["passable_data_upper"] = pUpper;
        }

        var chipsets = new Godot.Collections.Array<Godot.Collections.Dictionary> { chipset };
        return new Godot.Collections.Dictionary { { "chipsets", chipsets } };
    }

    private static Godot.Collections.Dictionary LegacyRawPassabilityDatabase(
        int pChipsetId,
        byte[]? pLower,
        byte[]? pUpper)
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
