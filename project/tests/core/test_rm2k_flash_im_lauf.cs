using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The screen flash as it reaches the pixels of a running game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the end of the path the flash command was missing.</strong>
/// The unit tests next door prove the decay and the blend; this one proves the
/// runtime paints it: a white flash is asked for, the painted frame has to change,
/// and when the flash is over the frame has to be **byte for byte** the frame it
/// was -- because a flash that leaves the screen lighter is a screen the player
/// keeps.
/// </para>
/// <para>
/// <strong>And the fixture's map has no hero graphic</strong> (the pinned LDB has
/// no starting party), so nothing else animates the frame and the comparison is
/// the flash and only the flash.
/// </para>
/// </remarks>
public partial class TestRm2kRuntimeRendering
{
    /// <summary>
    /// A white flash changes the painted frame and gives it back unchanged.
    /// </summary>
    public void Test_EinFlashFaerbtDasGemalteBildUndGibtEsZurueck()
    {
        var gameDir = CopyRealGame(nameof(Test_EinFlashFaerbtDasGemalteBildUndGibtEsZurueck));
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
            AssertTrue(CountColouredPixels(before) > 0,
                "**and the map really painted something**");

            // A white flash at full strength over four frames.
            AssertTrue(runtime.Presentation.FlashOnce(31, 31, 31, 31, 4),
                "**and the flash is accepted**");
            runtime.Update(1.0 / 60.0);
            var flashed = runtime.RenderedMap!.Pixels;
            var changed = 0;
            for (var index = 0; index + 3 < flashed.Length; index += 4)
            {
                if (flashed[index] != before[index]
                    || flashed[index + 1] != before[index + 1]
                    || flashed[index + 2] != before[index + 2])
                {
                    changed++;
                }
            }
            AssertTrue(changed > 0,
                "**and the painted frame changed**, because the flash is drawn over"
                    + " it; " + changed + " pixels differ");

            // Four frames and the flash is over.
            for (var frame = 0; frame < 4; frame++)
            {
                runtime.Update(1.0 / 60.0);
            }
            AssertEq(runtime.Presentation.IsFlashActive, false,
                "**and the flash is over after its frames**");
            AssertEq(runtime.Presentation.FlashCurrentLevel, 0.0,
                "**and its level is back to zero**");

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
                "**and the frame is byte for byte what it was**, because a flash"
                    + " that leaves the screen lighter is a screen the player keeps;"
                    + " " + remaining + " pixels are still different");
            created.Value.Host.Stop();
        }
        finally
        {
            Cleanup(gameDir);
        }
    }
}
