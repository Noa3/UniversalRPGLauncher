using System;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The camera pan as it reaches the pixels of a running game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the end of the path the pan command was missing.</strong> The
/// pan's state, its absolute finish, its step and its wait were all in the simulation,
/// and nothing outside the interpreter, the state and the save file read any of it —
/// so a game whose cutscene pans the camera across a landscape showed a camera that
/// never moved. Measured demand: 43 pan commands in one owned game alone.
/// </para>
/// <para>
/// <strong>And the second half is the lock</strong>: <c>11060</c>'s lock holds the
/// camera, which in the reference means <c>Game_Player::UpdateScroll</c> stops
/// following the player. Freezing the whole camera instead would also stop the pan a
/// locked cutscene is usually waiting for, so the test checks both: the follow stops
/// and the pan still runs.
/// </para>
/// </remarks>
public partial class TestRm2kRuntimeRendering
{
    /// <summary>
    /// A pan moves the camera, and a lock holds the follow while the pan runs on.
    /// </summary>
    public void Test_EinPanVerschiebtDieKameraUndDieSperreHaeltDenFollow()
    {
        var gameDir = CopyRealGame(
            nameof(Test_EinPanVerschiebtDieKameraUndDieSperreHaeltDenFollow));
        if (gameDir == null)
        {
            return;
        }
        try
        {
            var created = CreateRuntime(gameDir);
            if (created == null)
            {
                return;
            }
            var runtime = created.Value.Runtime;
            AssertTrue(runtime.RenderedMap != null,
                $"the map rendered: {runtime.RenderDiagnostic}");
            if (runtime.RenderedMap == null)
            {
                created.Value.Host.Stop();
                return;
            }

            // **Und der Spieler steht in der Mitte**, weil die Kamera an den
            // Kartenrändern klemmt und ein Pan von dort aus nichts bewegen kann.
            runtime.Simulation.MapX = runtime.Simulation.MapWidth / 2;
            runtime.Simulation.MapY = runtime.Simulation.MapHeight / 2;

            // Zwei Kacheln nach rechts bei Tempo drei: 512 Untereinheiten,
            // Schritt 16, also 32 Frames.
            runtime.Simulation.StartPan(
                GameSimulationState.PanDirection.Right, 2, 3);
            AssertEq(runtime.Simulation.PanSpeed, 16,
                "**and the pan's step is the reference's 2 << 3**");
            AssertEq(runtime.Simulation.PanFramesLeft, 0,
                "and a pan started outside the interpreter carries no wait of its own");

            // **Und der erste Pan-Frame ist der Bezugspunkt und nicht der davor**:
            // die Spielerkachel direkt zu setzen macht den Frame nicht dirty -- in
            // einem echten Spiel bewegt sie sich, weil der Spieler gegangen ist,
            // und das tut es. Beide verglichenen Frames sind also mit Pan
            // komponiert.
            runtime.Update(1.0 / 60.0);
            var nachEinem = runtime.AppliedCameraOffsetX;

            runtime.Update(1.0 / 60.0);
            AssertEq(runtime.AppliedCameraOffsetX, nachEinem + 1,
                "**and one frame of the pan moves the camera one pixel**, because the"
                    + " step is sixteen sub-units and a pixel is sixteen of them");

            for (var frame = 0; frame < 30; frame++)
            {
                runtime.Update(1.0 / 60.0);
            }
            AssertEq(runtime.AppliedCameraOffsetX, nachEinem + 31,
                "**and two tiles of pan is thirty two pixels**, counted from the first"
                    + " frame the pan was composed in");
            AssertEq(runtime.Simulation.IsPanActive, false,
                "**and the pan is over**, having landed on its finish");

            // **Und die Sperre hält den Follow und nicht den Pan.**
            runtime.Simulation.LockPan();
            runtime.Simulation.MapX += 1;
            runtime.Update(1.0 / 60.0);
            AssertEq(runtime.AppliedCameraOffsetX, nachEinem + 31,
                "**and a locked camera does not follow the player**, which is the"
                    + " reference's UpdateScroll returning at once when the pan is"
                    + " locked");

            // Und mit der Sperre laeuft ein Pan trotzdem.
            runtime.Simulation.StartPan(
                GameSimulationState.PanDirection.Right, 1, 3);
            runtime.Update(1.0 / 60.0);
            AssertEq(runtime.AppliedCameraOffsetX, nachEinem + 32,
                "**and the pan still moves the locked camera**");

            // Und nach dem Entsperren folgt sie wieder: die Kachel, die der
            // Spieler waehrend der Sperre gegangen ist, plus der naechste
            // Pan-Schritt.
            runtime.Simulation.UnlockPan();
            runtime.Update(1.0 / 60.0);
            AssertEq(runtime.AppliedCameraOffsetX, nachEinem + 32 + 16 + 1,
                "**and unlocking lets the camera follow again**, one tile behind the"
                    + " player it was held away from, with the pan still stepping");
            created.Value.Host.Stop();
        }
        finally
        {
            Cleanup(gameDir);
        }
    }
}
