using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The weather as it reaches the pixels of a running game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the end of the path the weather command was missing.</strong> The
/// unit tests next door pin the tables, the four rules and the drawing arithmetic; this
/// one proves the runtime paints it: rain is turned on, the painted frame has to change,
/// and turning it off has to give the frame back **byte for byte** -- because a game that
/// stops the rain and keeps a wet screen is a game with a stain on it.
/// </para>
/// <para>
/// <strong>And the fixture's map has no hero graphic</strong> (the pinned LDB has no
/// starting party), so nothing else animates the frame and the comparison is the weather
/// and only the weather.
/// </para>
/// </remarks>
public partial class TestRm2kRuntimeRendering
{
    /// <summary>
    /// Rain changes the painted frame and gives it back when it stops.
    /// </summary>
    public void Test_RegenFaerbtDasGemalteBildUndGibtEsZurueck()
    {
        var gameDir = CopyRealGame(nameof(Test_RegenFaerbtDasGemalteBildUndGibtEsZurueck));
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

            AssertTrue(runtime.Presentation.SetWeather(Rm2kWeatherState.TypeRain, 2),
                "**and the rain is accepted**");
            AssertEq(runtime.Presentation.Weather.Particles.Count, 100,
                "**and the sky holds a hundred particles**");
            for (var frame = 0; frame < 3; frame++)
            {
                runtime.Update(1.0 / 60.0);
            }

            var raining = runtime.RenderedMap!.Pixels;
            var changed = 0;
            for (var index = 0; index + 3 < raining.Length; index += 4)
            {
                if (raining[index] != before[index]
                    || raining[index + 1] != before[index + 1]
                    || raining[index + 2] != before[index + 2])
                {
                    changed++;
                }
            }
            AssertTrue(changed > 0,
                "**and the painted frame has rain on it**; " + changed + " pixels differ");

            AssertTrue(runtime.Presentation.SetWeather(Rm2kWeatherState.TypeNone, 0),
                "**and the rain can be turned off**");
            runtime.Update(1.0 / 60.0);
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
                "**and the frame is byte for byte what it was**, because a game that stops"
                    + " the rain and keeps a wet screen has a stain on it; "
                    + remaining + " pixels are still different");
            created.Value.Host.Stop();
        }
        finally
        {
            Cleanup(gameDir);
        }
    }
}
