using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The screen shake as it reaches the pixels of a running game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the end of the path the shake command was missing.</strong>
/// The unit tests next door prove the offset's arithmetic; this one proves the
/// runtime draws with it -- and, just as importantly, that it stays a **drawing**
/// offset: the camera moves and the map the player is standing on does not.
/// </para>
/// <para>
/// <strong>And the frame has to come back byte for byte</strong>, because a shake
/// that leaves the map shifted is a map the player keeps. The fixture's map has no
/// hero graphic (the pinned LDB has no starting party), so nothing else animates the
/// frame and the comparison is the shake and only the shake.
/// </para>
/// </remarks>
public partial class TestRm2kRuntimeRendering
{
    /// <summary>
    /// A shake moves the drawn map, leaves the simulation alone, and gives the
    /// frame back.
    /// </summary>
    public void Test_EinShakeVerschiebtDasGemalteBildUndGibtEsZurueck()
    {
        var gameDir = CopyRealGame(nameof(Test_EinShakeVerschiebtDasGemalteBildUndGibtEsZurueck));
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

            var before = (byte[])runtime.RenderedMap.Pixels.Clone();
            var cameraBefore = runtime.AppliedCameraOffsetX;
            var mapXBefore = runtime.Simulation.MapX;
            var mapYBefore = runtime.Simulation.MapY;

            AssertTrue(runtime.Presentation.ShakeOnce(4, 4, 30),
                "**and the shake is accepted**");
            runtime.Update(1.0 / 60.0);
            AssertTrue(runtime.Presentation.ShakePositionX != 0,
                "**and the shake has moved the screen**");
            AssertEq(runtime.AppliedCameraOffsetX, cameraBefore + runtime.Presentation.ShakePositionX,
                "**and the drawn camera carries the offset**, which is what"
                    + " Game_Map::GetDisplayX does with GetShakeOffsetX");

            var moved = runtime.RenderedMap!.Pixels;
            var changed = 0;
            for (var index = 0; index + 3 < moved.Length; index += 4)
            {
                if (moved[index] != before[index]
                    || moved[index + 1] != before[index + 1]
                    || moved[index + 2] != before[index + 2])
                {
                    changed++;
                }
            }
            AssertTrue(changed > 0,
                "**and the painted frame moved with it**; " + changed + " pixels differ");

            // **And the map itself did not move**: this is a drawing offset, which is
            // the boundary the renderer's own comment was protecting.
            AssertEq(runtime.Simulation.MapX, mapXBefore,
                "**and the simulation's map position is untouched**");
            AssertEq(runtime.Simulation.MapY, mapYBefore,
                "**and so is the vertical one**, because a shake moves the drawing"
                    + " and not the world");

            for (var frame = 0; frame < 30; frame++)
            {
                runtime.Update(1.0 / 60.0);
            }
            AssertEq(runtime.Presentation.IsShakeActive, false,
                "**and the shake is over after its frames**");
            AssertEq(runtime.Presentation.ShakePositionX, 0,
                "**and the screen is back at rest**");

            var after = runtime.RenderedMap!.Pixels;
            var remaining = 0;
            for (var index = 0; index + 3 < after.Length; index += 4)
            {
                if (after[index] != before[index]
                    || after[index + 1] != before[index + 1]
                    || after[index + 2] != before[index + 2])
                {
                    remaining++;
                }
            }
            AssertEq(remaining, 0,
                "**and the frame is byte for byte what it was**, because a shake that"
                    + " leaves the map shifted is a map the player keeps; "
                    + remaining + " pixels are still different");
            created.Value.Host.Stop();
        }
        finally
        {
            Cleanup(gameDir);
        }
    }
}
