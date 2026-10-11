using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Simulation;
using System.IO;
using System.Linq;
using System.Text;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kActiveEventGraphic
{
    private static readonly (int Command, int Dx, int Dy, byte Direction)[] DiagonalRouteCases =
    {
        (Rm2kMoveRoute.MoveUpRight, 1, -1, 9), (Rm2kMoveRoute.MoveDownRight, 1, 1, 3),
        (Rm2kMoveRoute.MoveDownLeft, -1, 1, 1), (Rm2kMoveRoute.MoveUpLeft, -1, -1, 7)
    };

    private void ConfigureDiagonalTerrain(int dx, int dy, bool verticalFirst, bool horizontalFirst)
    {
        for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
            _runtime.Simulation.PassabilityMasks[index] = 0;
        _runtime.Simulation.PassabilityMasks[8 + 8 * 20] = 15;
        _runtime.Simulation.PassabilityMasks[8 + dx + (8 + dy) * 20] = 15;
        _runtime.Simulation.PassabilityMasks[8 + (8 + dy) * 20] = (byte)(verticalFirst ? 15 : 0);
        _runtime.Simulation.PassabilityMasks[8 + dx + 8 * 20] = (byte)(horizontalFirst ? 15 : 0);
    }

    private static byte[] DiagonalPosePage(int command, int animationType) => TestRm2kParser.Struct(
        TestRm2kParser.Chunk(0x15, Encoding.ASCII.GetBytes("Chara1")),
        TestRm2kParser.Chunk(0x16, TestRm2kParser.Ber(3)),
        TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(0)),
        TestRm2kParser.Chunk(0x1F, TestRm2kParser.Ber(6)),
        TestRm2kParser.Chunk(0x20, TestRm2kParser.Ber(8)),
        TestRm2kParser.Chunk(0x22, TestRm2kParser.Ber(2)),
        TestRm2kParser.Chunk(0x24, TestRm2kParser.Ber(animationType)),
        TestRm2kParser.Chunk(0x29, AuthoredRoute(false, new Rm2kMap.MoveCommand(command))));

    public void Test_AllDiagonalDirectionsKeepOrReverseOnlyCardinalFacing()
    {
        // Independent pinned UpdateFacing table; initial columns are U,R,D,L.
        byte[][] expected = { new byte[] { 8, 6, 8, 6 }, new byte[] { 2, 6, 2, 6 },
            new byte[] { 2, 4, 2, 4 }, new byte[] { 8, 4, 8, 4 } };
        byte[] initial = { 8, 6, 2, 4 };
        for (var row = 0; row < DiagonalRouteCases.Length; row++)
        for (var column = 0; column < initial.Length; column++)
        {
            var (command, dx, dy, direction) = DiagonalRouteCases[row];
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(command)));
            _event.Direction = initial[column];
            _event.FacingDirection = initial[column];
            RouteTicks(1);
            AssertEq((_event.X, _event.Y), (8 + dx, 8 + dy));
            AssertEq(_event.Direction, direction);
            AssertEq(_event.FacingDirection, (byte?)expected[row][column]);
            AssertEq(EventSprite()!.FacingDirection, expected[row][column]);
            AssertEq(PageRoutes()[1].CurrentIndex, 1);
        }
    }

    public void Test_DiagonalAcceptsEitherCompleteCardinalPathButNeverCutsACorner()
    {
        foreach (var (command, dx, dy, _) in DiagonalRouteCases)
        foreach (var (vertical, horizontal) in new[] { (true, false), (false, true), (true, true), (false, false) })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(command)));
            ConfigureDiagonalTerrain(dx, dy, vertical, horizontal);
            RouteTicks(1);
            var accepted = vertical || horizontal;
            AssertEq((_event.X, _event.Y), accepted ? (8 + dx, 8 + dy) : (8, 8),
                $"Diagonal{command}: V={vertical},H={horizontal} needs a full path");
            AssertEq(PageRoutes()[1].CurrentIndex, accepted ? 1 : 0);
            AssertEq(_runtime.EventRemainingStepForTest(1), accepted ? 240 : -1);
        }
    }

    public void Test_EachDiagonalPathChecksEveryLegExitAndEntry()
    {
        foreach (var (command, dx, dy, _) in DiagonalRouteCases)
        foreach (var vertical in new[] { true, false })
        foreach (var edge in new[] { 0, 1, 2, 3 })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(command)));
            ConfigureDiagonalTerrain(dx, dy, vertical, !vertical);
            var horizontalExit = dx > 0 ? 4 : 2;
            var horizontalEntry = dx > 0 ? 2 : 4;
            var verticalExit = dy < 0 ? 8 : 1;
            var verticalEntry = dy < 0 ? 1 : 8;
            var intermediate = vertical ? 8 + (8 + dy) * 20 : 8 + dx + 8 * 20;
            var index = edge == 0 ? 8 + 8 * 20 : edge == 3 ? 8 + dx + (8 + dy) * 20 : intermediate;
            var bit = vertical
                ? (edge == 0 ? verticalExit : edge == 1 ? verticalEntry : edge == 2 ? horizontalExit : horizontalEntry)
                : (edge == 0 ? horizontalExit : edge == 1 ? horizontalEntry : edge == 2 ? verticalExit : verticalEntry);
            _runtime.Simulation.PassabilityMasks[index] = (byte)(15 & ~bit);
            RouteTicks(1);
            AssertEq((_event.X, _event.Y), (8, 8), $"Diagonal{command}, pathV={vertical}, missing edge{edge} must refuse");
            AssertEq(PageRoutes()[1].CurrentIndex, 0);
            AssertEq(_runtime.EventRemainingStepForTest(1), -1);
        }
    }

    public void Test_DiagonalFixedAndSpinningPagesKeepTheirCardinalPose()
    {
        foreach (var (command, dx, dy, direction) in DiagonalRouteCases)
        foreach (var animationType in new[] { 2, 4, 5 })
        {
            StartAuthoredRouteMap(DiagonalPosePage(command, animationType));
            RouteTicks(8);
            AssertEq((_event.X, _event.Y), (8 + dx, 8 + dy));
            AssertEq(_event.Direction, direction);
            AssertEq(_event.FacingDirection, (byte?)8);
            AssertEq(EventSprite()!.FacingDirection, (byte)8);
            AssertPinnedCellPixels(3, animationType == 2 ? 2 : 1, 0);
        }
    }

    public void Test_DiagonalRefusalRestoresOnlySkippablePosesAndNeverConsumesARetry()
    {
        foreach (var skippable in new[] { false, true })
        {
            StartAuthoredRouteMap(RoutePage(6, 7, false,
                new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUpRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveDown)));
            _event.Pages[0].MoveRouteSkippable = skippable;
            _event.Direction = 4;
            _event.FacingDirection = 4;
            Repaint(); // Arrange an actually displayed initial pose before the attempted step.
            ConfigureDiagonalTerrain(1, -1, false, false);
            RouteTicks(3);
            AssertEq((_event.X, _event.Y), (8, 8));
            AssertEq(_event.Direction, (byte)(skippable ? 4 : 9));
            AssertEq(_event.FacingDirection, (byte?)(skippable ? 4 : 6));
            AssertEq(PageRoutes()[1].CurrentIndex, skippable ? 1 : 0);
            AssertPinnedCellPixels(3, 1, skippable ? 3 : 1);
            if (skippable)
            {
                _runtime.Simulation.PassabilityMasks[8 + 9 * 20] = 15;
                RouteTicks(1);
                AssertEq(_event.Y, 8);
                RouteTicks(1);
                AssertEq((_event.X, _event.Y), (8, 9));
            }
            else
            {
                ConfigureDiagonalTerrain(1, -1, true, false);
                RouteTicks(1);
                AssertEq((_event.X, _event.Y), (9, 7));
                AssertEq(PageRoutes()[1].MoveFailureCount, 0);
            }
        }
    }

    public void Test_DiagonalUsesOneStepAndCannotRunTheNextCommandOnArrival()
    {
        StartAuthoredRouteMap(RoutePage(6, 7, false,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUpRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveDownLeft)));
        RouteTicks(2);
        AssertEq((_event.X, _event.Y), (8, 8));
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (9, 7));
        RouteTicks(15);
        AssertEq(_runtime.EventRemainingStepForTest(1), -1);
        AssertEq(PageRoutes()[1].CurrentIndex, 1);
        RouteTicks(4);
        AssertEq((_event.X, _event.Y), (9, 7));
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (8, 8));
        AssertEq(PageRoutes()[1].CurrentIndex, 2);
        AssertEq(_runtime.EventRemainingStepForTest(1), 240);
    }

    public void Test_DiagonalPageRebindDoesNotRedirectTheCurrentStep()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUpRight)),
            RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveDownLeft)));
        HigherRouteRequiresSwitch();
        RouteTicks(1);
        _runtime.Simulation.Switches[0] = true;
        RouteTicks(15);
        AssertEq((_event.X, _event.Y), (9, 7));
        AssertEq(PageRoutes()[1].CurrentIndex, 0);
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (8, 8));
    }

    public void Test_DiagonalStepCannotLeakAcrossStopDisposeOrMapReplacement()
    {
        foreach (var action in new[] { "stop", "dispose", "transfer" })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, true, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUpRight)));
            RouteTicks(1);
            var old = PageRoutes()[1];
            if (action == "stop") AssertTrue(_host.Stop().Success);
            else if (action == "dispose") _runtime.Dispose();
            else
            {
                File.WriteAllBytes(Path.Combine(_root, "Map0002.lmu"), TestRm2kEventPagePoseParser.MapWithPages(
                    RoutePage(0, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveDownLeft))));
                _runtime.Simulation.PendingMapId = 2;
                _runtime.Simulation.PendingX = 0;
                _runtime.Simulation.PendingY = 0;
                _runtime.Simulation.IsTransferPending = true;
                RouteTicks(1);
                AssertEq(_runtime.Simulation.MapId, 2);
                AssertFalse(ReferenceEquals(old, PageRoutes()[1]));
                RouteTicks(20);
            }
            AssertEq(_runtime.EventRemainingStepForTest(1), -1);
            if (action != "transfer") AssertEq(PageRoutes().Count, 0);
            else AssertEq(PageRoutes()[1].CurrentIndex, 0);
        }
    }

    public void Test_DiagonalBatchMatchesScalarStateAndExactPixels()
    {
        var page = RoutePage(6, 7, true, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUpRight),
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveDownLeft));
        StartAuthoredRouteMap(page);
        RouteTicks(60);
        var scalar = (_event.X, _event.Y, _event.Direction, _event.FacingDirection, _event.AnimationFrame,
            _event.AnimationCount, _event.StopCount, PageRoutes()[1].CurrentIndex, _runtime.EventRemainingStepForTest(1));
        var pixels = (byte[])_runtime.RenderedMap!.Pixels.Clone();
        StartAuthoredRouteMap(page);
        AssertTrue(_runtime.Update(1.0 + 1e-9).Success);
        AssertEq((_event.X, _event.Y, _event.Direction, _event.FacingDirection, _event.AnimationFrame,
            _event.AnimationCount, _event.StopCount, PageRoutes()[1].CurrentIndex, _runtime.EventRemainingStepForTest(1)), scalar);
        AssertTrue(pixels.SequenceEqual(_runtime.RenderedMap!.Pixels));
    }

    public void Test_LegacyForwardSeamResolvesTheStoredEightWayMovementDirection()
    {
        StartAuthoredRouteMap(RoutePage(0, 8, false,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUpRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveForward)));
        AssertEq(_runtime.StartEventMoveRouteForTest(1), 2);
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (9, 7));
        RouteTicks(16);
        AssertEq((_event.X, _event.Y), (10, 6), "Stored keypad direction9 must resolve to liblcf4, not an unknown direction");
        AssertEq(_event.Direction, (byte)9);
        AssertEq(_event.FacingDirection, (byte?)6);
        // Forward remains unsupported in production; this protects the existing seam.
    }

    public void Test_LegacyForwardRestoresPriorFacingOnlyAfterSuccessfulMovement()
    {
        foreach (var passable in new[] { true, false })
        {
            StartAuthoredRouteMap(RoutePage(0, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveForward)));
            _event.Direction = 9;
            _event.FacingDirection = 4;
            Repaint();
            ConfigureDiagonalTerrain(1, -1, passable, false);
            AssertEq(_runtime.StartEventMoveRouteForTest(1), 1);
            RouteTicks(1);
            AssertEq((_event.X, _event.Y), passable ? (9, 7) : (8, 8));
            AssertEq(_event.FacingDirection, (byte?)(passable ? 4 : 6),
                "Forward restores prior facing only after success; non-skippable refusal retains the attempt");
            AssertEq(EventSprite()!.FacingDirection, (byte)(passable ? 4 : 6));
            AssertPinnedCellPixels(3, 1, passable ? 3 : 1);
        }
    }

    public void Test_DiagonalCompatibleVerticalFacingReachesTheRealCharsetPixels()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUpRight)));
        _event.Direction = 8;
        _event.FacingDirection = 8;
        Repaint();
        RouteTicks(8);
        AssertEq(EventSprite()!.FacingDirection, (byte)8);
        AssertPinnedCellPixels(3, 2, 0);
    }

    public void Test_DiagonalPathsCannotWrapAcrossTheNonLoopingMovementBoundary()
    {
        foreach (var (command, dx, dy, _) in DiagonalRouteCases)
        {
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(command)));
            _event.X = dx > 0 ? 19 : 0;
            _event.Y = dy < 0 ? 0 : 14;
            var original = (_event.X, _event.Y);
            RouteTicks(1);
            AssertEq((_event.X, _event.Y), original);
            AssertEq(PageRoutes()[1].CurrentIndex, 0);
            AssertEq(_runtime.EventRemainingStepForTest(1), -1);
        }
    }

    public void Test_FileAuthoredDiagonalStartsOneStepWithIndependentCardinalFacing()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUpRight)));
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (9, 7), "Native opcode4 moves one tile on each axis");
        AssertEq(_event.Direction, (byte)9, "Runtime movement stores the eight-way keypad direction");
        AssertEq(_event.FacingDirection, (byte?)6, "A compatible cardinal facing is retained");
        AssertEq(_runtime.EventRemainingStepForTest(1), 240, "One shared256-unit step spends its first tick");
        AssertEq(PageRoutes()[1].CurrentIndex, 1);
        RouteTicks(7);
        AssertEq(EventSprite()!.Frame, 2);
        AssertPinnedCellPixels(3, 2, 1);
    }
}
