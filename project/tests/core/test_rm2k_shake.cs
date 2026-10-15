using System;
using System.Collections.Generic;
using System.Linq;

using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The screen shake: the offset it moves the screen by, from the reference's
/// <c>Shake::NextPosition</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the shake was the third effect that was state only:</strong> the
/// command was accepted, bounded, counted down and saved, and the camera offset
/// was explicitly *not* applied -- the renderer said the shake "would couple a
/// cosmetic effect to the deterministic core". That worry was right and the
/// reference draws the line in the same place: <c>Game_Map::GetDisplayX</c> adds
/// the offset, and only the sprite set that draws the map reads it, while
/// <c>Game_Character::GetScreenX</c> (which decides whether a chasing event can see
/// the player) does not. So the offset is a **drawing** offset and the movement
/// logic stays unshaken.
/// </para>
/// <para>
/// <strong>And the numbers in these tests come from the reference's own formula</strong>,
/// computed once: <c>amplitude = 1 + 2 * strength</c>,
/// <c>angle = ((time_left * 4 * (speed + 2)) % 256) * PI / 128</c>,
/// <c>newpos = amplitude * sin(angle) * -1</c>, and
/// <c>cutoff = (speed * amplitude / 8) + 1</c> holding the new offset within that
/// distance of the current one.
/// </para>
/// </remarks>
public partial class TestRm2kShake : TestBase
{
    /// <summary>
    /// A shake swings through the reference's own positions and ends at rest.
    /// </summary>
    /// <remarks>
    /// <strong>And the first value shows the clamp doing its work</strong>: the sine
    /// asks for eight, and the cutoff of five holds the screen to five, because the
    /// reference never lets a shake jump. The whole sequence is pinned, so a drift in
    /// the arithmetic fails here rather than on a player's screen.
    /// </remarks>
    public void Test_DerShakeSchwingtMitDenWertenDerReferenz()
    {
        var presentation = new PresentationState();
        AssertTrue(presentation.ShakeOnce(4, 4, 30),
            "**and the shake of thirty frames is accepted**");
        AssertEq(presentation.ShakePositionX, 0,
            "**and a shake that has not run yet has not moved the screen**");

        var expected = new[] { 5, 6, 1, -3, -7, -9, -7, -3 };
        for (var frame = 0; frame < expected.Length; frame++)
        {
            presentation.Tick(1);
            AssertEq(presentation.ShakePositionX, expected[frame],
                $"**and frame {frame + 1} is the position the reference computes**");
        }

        // And the rest of the shake never leaves the amplitude.
        var amplitude = 1 + 2 * 4;
        var extreme = 0;
        for (var frame = expected.Length; frame < 30; frame++)
        {
            presentation.Tick(1);
            extreme = Math.Max(extreme, Math.Abs(presentation.ShakePositionX));
        }
        AssertTrue(extreme <= amplitude,
            $"**and no frame moves the screen further than the amplitude {amplitude}**;"
                + $" the furthest was {extreme}");
        AssertEq(presentation.ShakePositionX, 0,
            "**and the shake ends with the screen exactly at rest**");
        AssertEq(presentation.IsShakeActive, false);
    }

    /// <summary>
    /// The cutoff keeps a slow shake from jumping.
    /// </summary>
    /// <remarks>
    /// <strong>And this is what the cutoff is for</strong>: a strength and speed of
    /// one has an amplitude of three but a cutoff of one, so the screen creeps
    /// instead of jumping -- the reference's own numbers are -1, -2, -2, -2 and so
    /// on. A reader that took the sine directly would move three pixels on the first
    /// frame.
    /// </remarks>
    public void Test_DieShakeGrenzeHaeltDenSprungKlein()
    {
        var presentation = new PresentationState();
        AssertTrue(presentation.ShakeOnce(1, 1, 10));

        var previous = 0;
        var steps = new List<int>();
        for (var frame = 0; frame < 10; frame++)
        {
            presentation.Tick(1);
            steps.Add(presentation.ShakePositionX - previous);
            previous = presentation.ShakePositionX;
        }

        AssertEq(string.Join(",", steps), "-1,-1,0,0,0,0,0,1,1,0",
            "**and every step stays inside the cutoff of one**, so the screen creeps"
                + " towards the sine instead of jumping to it");
        AssertTrue(steps.All(pStep => Math.Abs(pStep) <= 1),
            "**and no single frame moves it further than the cutoff**");
    }

    /// <summary>
    /// A second shake does not throw the screen back to the middle.
    /// </summary>
    /// <remarks>
    /// <strong>And that is the reference's own remark on its <c>ShakeOnce</c></strong>:
    /// "Shake position is not reset in RPG_RT, so that multiple shakes which
    /// interrupt each other flow smoothly." A reader that zeroed the offset on every
    /// command would make a game that re-issues its shake every few frames flicker at
    /// the middle.
    /// </remarks>
    public void Test_EinZweiterShakeSetztDiePositionNichtZurueck()
    {
        var presentation = new PresentationState();
        AssertTrue(presentation.ShakeOnce(4, 4, 30));
        for (var frame = 0; frame < 4; frame++)
        {
            presentation.Tick(1);
        }
        var moved = presentation.ShakePositionX;
        AssertTrue(moved != 0, "**and the screen has moved**");

        AssertTrue(presentation.ShakeOnce(4, 4, 30));
        AssertEq(presentation.ShakePositionX, moved,
            "**and a shake that interrupts it keeps the screen where it is**");
        AssertEq(presentation.ShakeFramesRemaining, 30,
            "and it takes over the frames");
    }

    /// <summary>
    /// Ending a shake puts the screen back at rest at once.
    /// </summary>
    /// <remarks>
    /// <strong>And the reference's <c>ShakeEnd</c> does exactly this</strong>: it
    /// zeroes the position as well as the time left, because its own
    /// <c>Update</c> is guarded by <c>time_left &gt; 0</c> and would never reach the
    /// reset.
    /// </remarks>
    public void Test_ShakeEndSetztDiePositionZurueck()
    {
        var presentation = new PresentationState();
        AssertTrue(presentation.ShakeOnce(4, 4, 30));
        presentation.Tick(2);
        AssertTrue(presentation.ShakePositionX != 0, "**and the screen has moved**");

        presentation.ShakeEnd();
        AssertEq(presentation.ShakePositionX, 0,
            "**and ending the shake puts the screen back at rest**");
        AssertEq(presentation.IsShakeActive, false);
        AssertEq(presentation.ShakeFramesRemaining, 0);
    }
}
