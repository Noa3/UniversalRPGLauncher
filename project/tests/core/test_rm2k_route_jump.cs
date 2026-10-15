using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Route commands 24 and 25: the jump.
/// </summary>
/// <remarks>
/// <strong>And the jump is not a step.</strong> A step asks whether the tile is
/// passable; a jump does not, because jumping over an obstacle is what a jump is for.
/// And it is not one command either: <c>begin_jump</c> starts a scan that reads the
/// commands up to <c>end_jump</c>, turns as it goes and adds up the distance, and only
/// then does the event leave the ground. All of that is pinned in the KANBAN card from
/// the reference source; these tests hold the runtime to it.
/// </remarks>
public partial class TestRm2kActiveEventGraphic
{
    private static Rm2kMap.MoveCommand Move(int pCommandId) => new(pCommandId);

    /// <summary>
    /// A closed jump travels the whole distance between the two commands, in one move.
    /// </summary>
    public void Test_DerSprungSpringtDieGesammelteStrecke()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, true,
            Move(Rm2kMoveRoute.BeginJump),
            Move(Rm2kMoveRoute.MoveRight),
            Move(Rm2kMoveRoute.MoveRight),
            Move(Rm2kMoveRoute.EndJump)));
        AssertEq(_event.X, 8, "the event starts where the page places it");
        RouteTicks(1);
        AssertEq(_event.X, 10,
            "**the two tiles between begin_jump and end_jump are jumped in one move**, not"
                + " walked one after the other");
        AssertEq(_event.Y, 8, "and the jump is horizontal");
        AssertEq(_event.Direction, 6, "**and the direction is right**, from the larger delta");
    }

    /// <summary>
    /// A jump lands on an impassable tile -- that is what a jump is for.
    /// </summary>
    /// <remarks>
    /// <strong>And the contrast is the test:</strong> the same two tiles walked by two
    /// ordinary steps are refused, and jumped they are taken. A runtime that reused the
    /// step's passability check for the jump would fail this, which is why both halves
    /// are asserted rather than only the landing.
    /// </remarks>
    public void Test_DerSprungLandetAufEinerUnpassierbarenKachel()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, true,
            Move(Rm2kMoveRoute.BeginJump),
            Move(Rm2kMoveRoute.MoveRight),
            Move(Rm2kMoveRoute.MoveRight),
            Move(Rm2kMoveRoute.EndJump)));
        // The tile two to the right is a wall: a step onto it is refused, a jump is not.
        var wall = 10 + 8 * _runtime.Simulation.MapWidth;
        _runtime.Simulation.PassabilityMasks[wall] = 0;
        RouteTicks(1);
        AssertEq(_event.X, 10, "**the jump takes the wall tile**, because a jump checks no tile");
        AssertTrue(_runtime.Simulation.Diagnostics.All(message => !message.Contains("jump")),
            "**and nothing is diagnosed as a failure**, so the jump did not quietly fall back"
                + " to a refused step");
    }

    /// <summary>
    /// <c>move_forward</c> inside a jump runs along the direction the scan just turned to.
    /// </summary>
    /// <remarks>
    /// <strong>And that is why the accumulation sits outside the turn switch:</strong> the
    /// reference turns first and adds the delta afterwards, so a forward move after a turn
    /// jumps the new way. A scan that added before turning would jump one tile up and one
    /// tile along the old direction.
    /// </remarks>
    public void Test_MoveForwardImSprungLaeuftInDieNeueRichtung()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, true,
            Move(Rm2kMoveRoute.BeginJump),
            Move(Rm2kMoveRoute.MoveUp),
            Move(Rm2kMoveRoute.MoveForward),
            Move(Rm2kMoveRoute.EndJump)));
        RouteTicks(1);
        AssertEq(_event.X, 8, "**the forward move did not keep the old horizontal direction**");
        AssertEq(_event.Y, 6, "**and the jump went two tiles up**, both along the new direction");
    }

    /// <summary>
    /// A jump with no <c>end_jump</c> is skipped and the route carries on.
    /// </summary>
    public void Test_EinSprungOhneEndJumpFaelltAus()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, true,
            Move(Rm2kMoveRoute.BeginJump),
            Move(Rm2kMoveRoute.MoveRight),
            Move(Rm2kMoveRoute.MoveRight)));
        RouteTicks(1);
        AssertEq(_event.X, 8,
            "**the event stays on its tile**: the scan ran to the end of the route without an"
                + " end_jump, so the jump is skipped rather than half-executed");
        AssertTrue(_runtime.Simulation.Diagnostics.All(
                message => !message.Contains("walked past unhandled command 24")),
            "**and begin_jump is no longer an unhandled command**, which is what the"
                + " diagnostic channel said before it was implemented");
    }

    /// <summary>
    /// A diagonal jump is drawn as a straight diagonal.
    /// </summary>
    /// <remarks>
    /// <strong>And both axes move at once</strong>, which is the difference from an ordinary
    /// step: the cardinal sprite offset could only ever move one axis, so the jump needs the
    /// begin tile. This is the test that would fail if the jump reused the step's sprite
    /// path -- and it is the reason the event sprite offset had to be fixed first.
    /// </remarks>
    public void Test_DerBogenWirdDiagonalGezeichnet()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, true,
            Move(Rm2kMoveRoute.BeginJump),
            Move(Rm2kMoveRoute.MoveRight),
            Move(Rm2kMoveRoute.MoveUp),
            Move(Rm2kMoveRoute.EndJump)));
        RouteTicks(1);
        _runtime.MarkFrameDirtyForTest();
        _runtime.RefreshFrameForTest();
        var sprite = _runtime.FirstComposedCharacterSprite;
        AssertTrue(sprite != null, "the jumping event is composed into the frame");
        AssertEq(sprite!.MapX, 9, "the event already occupies the tile it jumped onto");
        AssertEq(sprite.MapY, 7, "on both axes");
        AssertTrue(sprite.PixelOffsetX != 0,
            "**the sprite is drawn off the tile horizontally while the jump is in the air**");
        AssertTrue(sprite.PixelOffsetY != 0,
            "**and vertically as well**, which is the straight diagonal a step cannot draw");
        AssertEq(sprite.PixelOffsetX, -sprite.PixelOffsetY * 1,
            "**and both axes carry the same fraction of their own tile**, which is what makes"
                + " the arc straight rather than a staircase");
    }
}
