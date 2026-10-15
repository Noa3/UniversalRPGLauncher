using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The screen tone as it reaches the pixels of a running game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the end of the path the tint command was missing.</strong>
/// The unit tests next door prove the arithmetic; this one proves the runtime
/// paints it: the map is rendered, a tone of zero is asked for, and the painted
/// frame has to lose every colour -- and get them back when the tone returns to
/// neutral.
/// </para>
/// <para>
/// <strong>And the tone is asked for directly rather than through an event</strong>,
/// so the assertion measures the render pass and not whether some page happened
/// to run in the first frames.
/// </para>
/// </remarks>
public partial class TestRm2kRuntimeRendering
{
    /// <summary>
    /// A tone of zero leaves the painted screen black, and neutral restores it.
    /// </summary>
    public void Test_DerBildschirmtonFaerbtDasGemalteBild()
    {
        var gameDir = CopyRealGame(nameof(Test_DerBildschirmtonFaerbtDasGemalteBild));
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

            var coloursBefore = CountColouredPixels(runtime.RenderedMap.Pixels);
            AssertTrue(coloursBefore > 0,
                "**and the map really painted something** -- " + coloursBefore
                    + " pixels carry a colour");

            // **And the tone is asked for with no duration**, so it is in effect
            // for the very next frame.
            AssertTrue(runtime.Presentation.TintScreen(0, 0, 0, 100, 0),
                "**and the tint of zero is accepted**");
            runtime.Update(1.0 / 60.0);
            var coloursUnderBlack = CountColouredPixels(runtime.RenderedMap!.Pixels);
            AssertEq(coloursUnderBlack, 0,
                "**and a tone of zero leaves no colour in the painted frame**,"
                    + " because the hard-light table at zero is zero for every"
                    + " channel; " + coloursUnderBlack + " coloured pixels remain");

            AssertTrue(runtime.Presentation.TintScreen(100, 100, 100, 100, 0),
                "**and the neutral tint is accepted**");
            runtime.Update(1.0 / 60.0);
            var coloursAfter = CountColouredPixels(runtime.RenderedMap!.Pixels);
            AssertTrue(coloursAfter > 0,
                "**and the neutral tone brings the colours back**, because 100 in"
                    + " the command's scale is 128 in the tone's and 128 is the"
                    + " value that changes nothing; " + coloursAfter
                    + " coloured pixels are back");
            created.Value.Host.Stop();
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>Counts the pixels whose colour is not black, ignoring alpha.</summary>
    /// <param name="pPixels">The frame's bytes.</param>
    /// <returns>The count.</returns>
    private static int CountColouredPixels(byte[] pPixels)
    {
        var count = 0;
        for (var index = 0; index + 3 < pPixels.Length; index += 4)
        {
            if (pPixels[index] != 0 || pPixels[index + 1] != 0 || pPixels[index + 2] != 0)
            {
                count++;
            }
        }
        return count;
    }
}
