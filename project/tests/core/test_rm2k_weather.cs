using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The weather: its particle tables, its four per-frame rules, and the pixels.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the weather was the last of the five screen effects that were state
/// only</strong> -- the type and the strength were set, clamped and saved, and no
/// renderer file read them, so a game that turned on rain showed a dry screen. Measured
/// demand: one <c>11070</c> in the whole owned collection, which is why it is its own
/// card and its own suite rather than a by-product of the tint's.
/// </para>
/// <para>
/// <strong>And the numbers come from EasyRPG's <c>src/weather.cpp</c></strong>: the
/// tables <c>{20, 60, 100}</c> and <c>{128, 191, 255}</c>, the two fog layers'
/// opacities, the rain streak's <c>x = w - (y / 4) - 1</c>, and the alpha rule
/// <c>min((abase + strength) * t, 255)</c>.
/// </para>
/// </remarks>
public partial class TestRm2kWeather : TestBase
{
    private static Rm2kPixelBuffer Frame()
    {
        var frame = new Rm2kPixelBuffer(64, 32);
        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width; x++)
            {
                frame.TrySetPixel(x, y, 10, 20, 30, 255);
            }
        }
        return frame;
    }

    /// <summary>
    /// The particle tables are the reference's, and the drawn count is 0-based.
    /// </summary>
    /// <remarks>
    /// <strong>And the two counts differ on purpose</strong>:
    /// <c>GetMaxNumParticles</c> is the vector's size (the table's last entry) while the
    /// draw indexes <c>Clamp(strength, 0, 2)</c> -- so strength zero draws the first
    /// entry and not nothing.
    /// </remarks>
    public void Test_DiePartikeltabellenSindDieDerReferenz()
    {
        AssertEq(Rm2kWeatherState.MaxParticles(Rm2kWeatherState.TypeNone), 0,
            "**weather that is off has no particles**");
        AssertEq(Rm2kWeatherState.MaxParticles(Rm2kWeatherState.TypeRain), 100,
            "**rain's vector holds the table's last entry**, which is a hundred");
        AssertEq(Rm2kWeatherState.MaxParticles(Rm2kWeatherState.TypeSnow), 100);
        AssertEq(Rm2kWeatherState.MaxParticles(Rm2kWeatherState.TypeFog), 2,
            "and fog has two, because it is two overlay layers");
        AssertEq(Rm2kWeatherState.MaxParticles(Rm2kWeatherState.TypeSandstorm), 255);

        AssertEq(Rm2kWeatherState.DrawnParticles(Rm2kWeatherState.TypeRain, 0), 20,
            "**and strength zero draws twenty**");
        AssertEq(Rm2kWeatherState.DrawnParticles(Rm2kWeatherState.TypeRain, 1), 60);
        AssertEq(Rm2kWeatherState.DrawnParticles(Rm2kWeatherState.TypeRain, 2), 100);
        AssertEq(Rm2kWeatherState.DrawnParticles(Rm2kWeatherState.TypeRain, 3), 100,
            "**and a strength the editor cannot write is clamped and not refused**");
        AssertEq(Rm2kWeatherState.DrawnParticles(Rm2kWeatherState.TypeSnow, 2), 100,
            "snow shares rain's table");
        AssertEq(Rm2kWeatherState.DrawnParticles(Rm2kWeatherState.TypeSandstorm, 1), 191,
            "and the sandstorm has its own");
    }

    /// <summary>
    /// Seeding scatters only the particles it adds.
    /// </summary>
    /// <remarks>
    /// <strong>And that is the reference's early return</strong>: a game that switches
    /// from rain to fog keeps the hundred particles it already has, and switching from
    /// fog to rain scatters the ninety eight it adds. Re-scattering everything would
    /// make the sky jump on every weather change.
    /// </remarks>
    public void Test_DasSeedingStreutNurDieNeuenPartikel()
    {
        var weather = new Rm2kWeatherState();
        weather.Seed(Rm2kWeatherState.TypeRain);
        AssertEq(weather.Particles.Count, 100, "**rain scatters a hundred particles**");
        var ersterX = weather.Particles[0].X;
        var ersterT = weather.Particles[0].T;
        AssertTrue(ersterT >= 0 && ersterT <= 39,
            "**and a particle's life starts between zero and thirty nine frames**");
        AssertTrue(weather.Particles[0].X >= 0 && weather.Particles[0].X <= 319,
            "**and its column is inside the weather surface**, which is the screen");
        AssertTrue(weather.Particles[0].Y >= 0 && weather.Particles[0].Y <= 159,
            "**and its row is inside the surface's own height**");

        weather.Seed(Rm2kWeatherState.TypeFog);
        AssertEq(weather.Particles.Count, 100,
            "**and fog adds nothing**, because two particles fit in a hundred");
        AssertEq(weather.Particles[0].X, ersterX,
            "**and it does not re-scatter what was already there**");
        AssertEq(weather.Particles[0].T, ersterT);

        weather.Seed(Rm2kWeatherState.TypeSandstorm);
        AssertEq(weather.Particles.Count, 255, "and a sandstorm grows the sky to 255");
        AssertEq(weather.Particles[0].X, ersterX, "keeping the first hundred where they were");
    }

    /// <summary>
    /// The four per-frame rules are the reference's.
    /// </summary>
    public void Test_DieVierRegelnDerReferenz()
    {
        // Regen: vier Reihen nach unten, eine Spalte nach links.
        var rain = new Rm2kWeatherState();
        rain.Seed(Rm2kWeatherState.TypeRain);
        rain.SetParticle(0, new Rm2kWeatherParticle { T = 5, X = 100, Y = 100 });
        rain.Update(Rm2kWeatherState.TypeRain, 1);
        AssertEq(rain.Particles[0].T, 4, "**rain loses one frame of life**");
        AssertEq(rain.Particles[0].Y, 104, "**and falls four rows**");
        AssertEq(rain.Particles[0].X, 99, "**and drifts one column left**");

        // Schnee: zwei bis drei Reihen nach unten, null bis eine Spalte nach links.
        var snow = new Rm2kWeatherState();
        snow.Seed(Rm2kWeatherState.TypeSnow);
        snow.SetParticle(0, new Rm2kWeatherParticle { T = 5, X = 100, Y = 100 });
        snow.Update(Rm2kWeatherState.TypeSnow, 1);
        AssertEq(snow.Particles[0].T, 4, "**snow loses one frame of life**");
        var gefallen = snow.Particles[0].Y - 100;
        AssertTrue(gefallen == 2 || gefallen == 3,
            "**and falls two or three rows**, which is " + gefallen);
        var gedriftet = 100 - snow.Particles[0].X;
        AssertTrue(gedriftet == 0 || gedriftet == 1,
            "**and drifts at most one column left**, which is " + gedriftet);

        // Nebel: die zwei Schichten zaehlen nur hoch.
        var fog = new Rm2kWeatherState();
        fog.Seed(Rm2kWeatherState.TypeFog);
        var vorherHinten = fog.Particles[0].X;
        var vorherVorn = fog.Particles[1].X;
        fog.Update(Rm2kWeatherState.TypeFog, 1);
        AssertEq(fog.Particles[0].X, vorherHinten + 1,
            "**fog's back layer advances one frame**");
        AssertEq(fog.Particles[1].X, vorherVorn + 1,
            "**and so does its front layer**");
        AssertEq(fog.Particles.Count, 2, "and fog has no other particles");

        // Sandsturm: der Nebel plus eine echte Partikelsimulation.
        var sand = new Rm2kWeatherState();
        sand.Seed(Rm2kWeatherState.TypeSandstorm);
        sand.SetParticle(2, new Rm2kWeatherParticle
        {
            T = 5, X = 50, Y = 50, Alpha = 100, Vx = 2.0, Vy = 3.0, Ax = 1.0, Ay = 0.5,
        });
        sand.Update(Rm2kWeatherState.TypeSandstorm, 1);
        AssertEq(sand.Particles[2].T, 4, "**a sand particle loses one frame of life**");
        AssertEq(sand.Particles[2].Alpha, 102, "**and becomes two steps brighter**");
        AssertEq(sand.Particles[2].X, 52, "**and moves by its velocity**");
        AssertEq(sand.Particles[2].Y, 53);
        AssertEq(sand.Particles[2].Vx, 3.0, "**and its velocity grows by its acceleration**");
        AssertEq(sand.Particles[2].Vy, 3.5);
    }

    /// <summary>
    /// Rain is drawn, and only for the particles the strength asks for.
    /// </summary>
    /// <remarks>
    /// <strong>And the count is the table's, not the vector's</strong>: a particle at
    /// index thirty is inside the hundred the vector holds and outside the twenty a
    /// strength of zero draws.
    /// </remarks>
    public void Test_RegenWirdGezeichnetUndNurInDerAngefordertenZahl()
    {
        var weather = new Rm2kWeatherState();
        weather.Seed(Rm2kWeatherState.TypeRain);
        // Ein Partikel an einer bekannten Stelle und eines, das nicht gezeichnet wird.
        weather.SetParticle(0, new Rm2kWeatherParticle { T = 12, X = 10, Y = 0 });
        weather.SetParticle(30, new Rm2kWeatherParticle { T = 12, X = 40, Y = 0 });

        var frame = Frame();
        Rm2kWeatherPainter.Apply(
            frame, weather, Rm2kWeatherState.TypeRain, 0, 0, 0, 0, 0, 1);

        // Die Regenspur: eine Spalte je Reihe, x = 6 - (y / 4) - 1.
        // **Und die Spur liegt bei X + 5**: die erste Reihe des Streifens ist die
        // Spalte `6 - (0 / 4) - 1 = 5` **relativ zum Partikel**, also bei 15.
        var alpha = (5 + 0) * 12;
        var erwartet = (10 * (255 - alpha) + 255 * alpha) / 255;
        AssertEq(frame.Pixels[(0 * frame.Width + 15) * 4], erwartet,
            "**and the first row of the streak is one pixel at the particle's column"
                + " plus five** (alpha = (5 + 0) * 12 = 60)");
        AssertEq(frame.Pixels[(0 * frame.Width + 14) * 4], 10,
            "**and the column beside it is untouched**");

        AssertEq(frame.Pixels[(0 * frame.Width + 40) * 4], 10,
            "**and the particle at index thirty is not drawn**, because a strength of"
                + " zero draws twenty");

        // Und mit Staerke zwei wird es gezeichnet: seine Spur liegt bei 45.
        var stark = Frame();
        Rm2kWeatherPainter.Apply(
            stark, weather, Rm2kWeatherState.TypeRain, 2, 0, 0, 0, 0, 1);
        AssertTrue(stark.Pixels[(0 * stark.Width + 45) * 4] != 10,
            "**and at strength two it is**");
    }

    /// <summary>
    /// Weather that is off touches no byte, and the fog covers the screen.
    /// </summary>
    public void Test_OhneWetterKeinByteUndDerNebelDecktDenBildschirm()
    {
        var weather = new Rm2kWeatherState();
        weather.Seed(Rm2kWeatherState.TypeRain);
        var frame = Frame();
        var before = (byte[])frame.Pixels.Clone();

        Rm2kWeatherPainter.Apply(
            frame, weather, Rm2kWeatherState.TypeNone, 0, 0, 0, 0, 0, 1);
        for (var index = 0; index < before.Length; index++)
        {
            AssertEq(frame.Pixels[index], before[index],
                "**and weather that is off leaves byte " + index + " as it was**");
        }

        // Der Nebel: zwei gekachelte Schichten, also ist keine Ecke mehr die
        // Frame-Farbe.
        var fog = Frame();
        Rm2kWeatherPainter.Apply(
            fog, weather, Rm2kWeatherState.TypeFog, 2, 0, 0, 0, 0, 1);
        AssertTrue(fog.Pixels[0] != 10,
            "**and the fog changes the top left corner**");
        AssertTrue(fog.Pixels[(fog.Height - 1) * fog.Width * 4] != 10,
            "**and the bottom left one**, because it is tiled over the whole frame");
    }

    /// <summary>
    /// The fog opacities are the reference's table.
    /// </summary>
    public void Test_DieNebelopazitaetenSindDieDerReferenz()
    {
        AssertEq(Rm2kWeatherPainter.FogOpacity[0, 0], 32, "**the back layer at strength zero**");
        AssertEq(Rm2kWeatherPainter.FogOpacity[0, 1], 64);
        AssertEq(Rm2kWeatherPainter.FogOpacity[0, 2], 96);
        AssertEq(Rm2kWeatherPainter.FogOpacity[1, 0], 64, "**and the front layer**");
        AssertEq(Rm2kWeatherPainter.FogOpacity[1, 1], 80);
        AssertEq(Rm2kWeatherPainter.FogOpacity[1, 2], 160);
        AssertEq(Rm2kWeatherPainter.FogOpacity[0, 3], 0,
            "**and a strength of three shows no back layer**, which is the reference's"
                + " own note about a hacked strength");
        AssertEq(Rm2kWeatherPainter.FogOpacity[1, 3], 255);
    }
}
