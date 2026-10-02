using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Godot;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The two commands the starting map of a finished MV game asks for and this
/// repository did not run: <c>214 Erase Event</c> and <c>223 Screen Tint</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And both were implemented and neither was reached.</strong>
/// <c>214</c> is the second most frequent command in the whole game — one
/// hundred and sixty-five uses — <strong>and a command outside
/// <c>HasEffect</c> is never dispatched and is reported as finished.</strong>
/// </para>
/// <para>
/// <strong>And they are not the same thing as the two commands their names
/// sit next to.</strong> <c>222</c> removes the running event until the party
/// leaves the map; <strong><c>214</c> removes it from the map's own
/// list.</strong> <c>225</c> shakes the screen and <c>223</c> washes it in a
/// colour, <strong>and the colour is a list of four numbers and not three
/// values.</strong>
/// </para>
/// </remarks>
public partial class TestMvEraseAndTint : TestBase
{
    private const string Projekt = "D:/Itch/sister/www";

    private static bool Vorhanden()
    {
        if (!File.Exists(Projekt + "/data/Map002.json"))
        {
            GD.Print("    (skipped: no " + Projekt + ")");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The game's own <c>214</c> and <c>223</c>, and their measured forms.
    /// </summary>
    public void Test_DieEigenenFormenVon214Und223()
    {
        if (!Vorhanden())
        {
            return;
        }

        var k = JsonDocument.Parse(
            File.ReadAllText(Projekt + "/data/Map002.json")).RootElement;
        AssertTrue(k.TryGetProperty("events", out var evs)
            && evs.GetArrayLength() > 0,
            "**and Map002 carries events**");

        // Und die Form, gemessen.
        foreach (var e in evs.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var s in e.GetProperty("pages").EnumerateArray())
            {
                foreach (var c in s.GetProperty("list").EnumerateArray())
                {
                    if (c.GetProperty("code").GetInt32() != 223)
                    {
                        continue;
                    }

                    var ps = c.GetProperty("parameters");
                    AssertEq(ps.GetArrayLength(), 3,
                        "**and a 223 carries three parameters**");
                    var ton = ps[0];
                    AssertEq(ton.ValueKind, JsonValueKind.Array,
                        "**and the first is a list and not a number**");
                    AssertEq(ton.GetArrayLength(), 4,
                        "**and it has four numbers where the engine reads "
                        + "three**");
                    AssertEq(ps[1].GetInt32(), 999,
                        "**and the duration is nine hundred and ninety-nine "
                        + "frames**");
                    AssertTrue(!ps[2].GetBoolean(),
                        "**and it does not wait for the tint** -- and the "
                        + "third value is a yes or no and not a length");
                    return;
                }
            }
        }

        AssertTrue(false, "**and a 223 was found on that map**");
    }

    /// <summary>
    /// <summary>
    /// The tint takes its colour from a list of four and does not arrive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>startTint</c> copies the list into
    /// <c>_toneTarget</c> and does not touch <c>_tone</c></strong> --
    /// <strong>so the screen keeps the colour it had and walks toward the
    /// new one over the frames the game asked for.</strong> <strong>And
    /// this game's tint is nine hundred and ninety-nine frames long on
    /// purpose, because it is a colour the screen keeps.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that put the target straight into the tone
    /// would skip every frame of the change</strong>, <strong>and a reader
    /// that kept three of the four numbers would drop the one
    /// <c>updateTone</c>'s own <c>for (let i = 0; i &lt; 4; i++)</c> walks
    /// over.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieEinfaerbungNimmtVierZahlenUndKommtNichtAn()
    {
        var fakten = new MzBranchFacts();
        var eintrag = Befehl(MzCommandTable.ScreenTint,
            "[-68,-68,-68,0]", "999", "false");
        var interp = new MzInterpreter(new List<MzCommandEntry> { eintrag });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(interp.Stopped, MzStep.Finished,
            "**and the run finishes**");
        AssertEq(fakten.Screen.TargetTone[0], -68,
            "**and the colour is read out of the list** -- "
            + fakten.Screen.TargetTone[0] + ", and a reader that parsed the "
            + "parameter as text got a target of zero, zero, zero, which is "
            + "a target of no tint at all");
        AssertEq(fakten.Screen.TargetTone[2], -68,
            "**and the third channel**");
        AssertEq(fakten.Screen.Tone[0], 0,
            "**and the screen is not yet tinted** -- and that is the "
            + "engine's own order, because startTint writes the target and "
            + "not the tone");
        AssertEq(fakten.Screen.ToneDuration, 999,
            "**and it has the frames the game asked for**");
        AssertTrue(fakten.Screen.ToneIsMoving,
            "**and a wait would ask about it**");
    }

    /// <summary>
    /// A tint walks toward its target, and the engine's arithmetic, frame by
    /// frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the step is a fraction and not a fixed
    /// number</strong>: <c>(this._tone[i] * (d - 1) + target[i]) / d</c>,
    /// <strong>with <c>d</c> the duration that is left.</strong> <strong>So
    /// the first frame of a sixty-frame tint covers one sixtieth of the way
    /// and the last covers one part in one</strong> -- <strong>which is why
    /// it eases and why a fixed step would not.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieEinfaerbungGehtBildFuerBildZumZiel()
    {
        var bild = new MzScreen();
        bild.StarteTon(new int[] { -68, -68, -68, 0 }, 4);
        AssertEq(bild.Tone[0], 0, "**and it starts where it was**");

        // Und die vier Bilder, die die Engine selbst macht.
        var erwartet = new List<int>();
        var ton = 0;
        var dauer = 4;
        while (dauer > 0)
        {
            ton = (ton * (dauer - 1) + -68) / dauer;
            erwartet.Add(ton);
            dauer--;
        }

        var gelesen = new List<int>();
        while (bild.ToneDuration > 0 && gelesen.Count < 50)
        {
            bild.TickTon();
            gelesen.Add(bild.Tone[0]);
        }

        AssertEq(gelesen.Count, erwartet.Count,
            "**and it takes exactly the frames the game asked for** -- "
            + gelesen.Count + " against " + erwartet.Count);
        for (var i = 0; i < erwartet.Count; i++)
        {
            AssertEq(gelesen[i], erwartet[i],
                "**and frame " + i + " is the engine's own value**");
        }

        AssertEq(bild.ToneDuration, 0, "**and then it is done**");
        AssertTrue(!bild.ToneIsMoving,
            "**and a wait would no longer ask about it**");

        // **Und der vierte Kanal wird auch gelaufen, und nicht
        // uebersprungen.**
        var voll = new MzScreen();
        voll.StarteTon(new int[] { 10, 20, 30, 40 }, 2);
        voll.TickTon();
        AssertEq(voll.Tone[3], 20,
            "**and the fourth number is read by the engine** -- "
            + voll.Tone[3] + ", and `for (let i = 0; i < 4; i++)` walks over "
            + "four channels and not three");
    }

    /// <summary>
    /// A duration of zero puts the colour there at once, and that is the
    /// engine's own special case.
    /// </summary>
    public void Test_EineDauerVonNullSetztDieFarbeSofort()
    {
        var bild = new MzScreen();
        bild.StarteTon(new int[] { -68, -68, -68, 0 }, 0);
        AssertEq(bild.Tone[0], -68,
            "**and the colour is there and not one frame later** -- and "
            + "that is `if (this._toneDuration === 0)` in startTint, and a "
            + "reader without it would tint a screen over zero frames");
        AssertEq(bild.ToneDuration, 0, "**and nothing is left to wait for**");
    }

    /// <summary>
    /// <c>214</c> removes the event, and only with a map and an event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the guard is the engine's own</strong>:
    /// <c>if (this.isOnCurrentMap() &amp;&amp; this._eventId &gt; 0)</c>
    /// <strong>— and a common event has neither, so a <c>214</c> inside one
    /// is a command without a target and says so.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinEreignisWirdNurMitKarteUndNummerGeloescht()
    {
        var eintrag = Befehl(MzCommandTable.EraseEventFromMap);

        // Auf einer Karte, mit einer Ereignisnummer.
        var fakten = new MzBranchFacts();
        var interp = new MzInterpreter(new List<MzCommandEntry> { eintrag });
        interp.Setup(2, 7);
        interp.Run(new List<MzAction>(), fakten);
        AssertEq(interp.Stopped, MzStep.Finished,
            "**and the run finishes on a map**");
        AssertEq(fakten.Map.ErasedCount, 1,
            "**and the event is gone**");
        AssertTrue(fakten.Map.IsErased(7),
            "**and a page of that event cannot run again** -- and that is "
            + "the whole point of a treasure chest that has been opened");
        AssertEq(fakten.Notices.Count, 0,
            "**and nothing was refused**");

        // Ohne Karte: das gemeinsame Ereignis.
        var fakten2 = new MzBranchFacts();
        var interp2 = new MzInterpreter(new List<MzCommandEntry> { eintrag });
        interp2.Setup(0, 0);
        interp2.Run(new List<MzAction>(), fakten2);
        AssertEq(fakten2.Map.ErasedCount, 0,
            "**and nothing is erased without a map**");
        AssertTrue(fakten2.Notices.Count > 0,
            "**and the caller is told why** -- " + fakten2.Notices.Count
            + " notices, and the engine's guard stops there too");
        AssertTrue(string.Join(" ", fakten2.Notices).Contains("isOnCurrentMap"),
            "**and the notice names the engine's own condition**");
    }

    /// <summary>
    /// <c>222</c> and <c>214</c> are not the same command, and the first
    /// is not an erase at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test compared one name with itself for a
    /// while</strong> -- <strong>it said
    /// <c>MzCommandTable.EraseEventFromMap != MzCommandTable.EraseEventFromMap</c></strong>
    /// -- <strong>and that is false for ever and for any reason, and a
    /// test that can never pass should be deleted rather than
    /// repaired.</strong> <strong>It was here because the old name
    /// <c>EraseEvent</c> stood for <c>222</c>, and <c>222</c> is not an
    /// erase.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>222</c> is <c>Fadein Screen</c>:</strong>
    /// </para>
    /// <code>
    /// command222 = function() {
    ///     if (!$gameMessage.isBusy()) {
    ///         $gameScreen.startFadeIn(this.fadeSpeed());
    ///         this.wait(this.fadeSpeed());
    ///         this._index++;
    ///     }
    ///     return false;
    /// };
    /// </code>
    /// <para>
    /// <strong>And <c>214</c> is <c>Erase Event</c>, and it takes the
    /// event off the map for good:</strong> <strong><c>if
    /// (this.isOnCurrentMap() &amp;&amp; this._eventId &gt; 0) {
    /// $gameMap.eraseEvent(this._eventId); }</c></strong> <strong>and the
    /// first of those two lines is what keeps it from erasing something
    /// on another map.</strong>
    /// </para>
    /// </remarks>
    public void Test_ZweiundVierzehnIstNichtZweiZwoelf()
    {
        AssertTrue(MzCommandTable.FadeinScreen != MzCommandTable.EraseEventFromMap,
            "**and they are two numbers**");
        AssertEq(MzCommandTable.FadeinScreen, 222,
            "**and 222 is Fadein Screen, not an erase**");
        AssertEq(MzCommandTable.EraseEventFromMap, 214,
            "**and 214 is the one that takes it off the map**");
    }


    /// <summary>
    /// A command out of its own JSON, and the parameters are values and not
    /// text.
    /// </summary>
    /// <remarks>
    /// <strong>And the first version of this helper wrapped every
    /// parameter in a string</strong>, <strong>and <c>223</c>'s first
    /// parameter is a list of four numbers and not a
    /// text</strong> -- <strong>so the screen came out tinted to zero,
    /// zero, zero</strong>, <strong>which is a screen that is not
    /// tinted</strong>, <strong>and the test said so and was right.</strong>
    /// </remarks>
    private static MzCommandEntry Befehl(int pCode, params string[] pParameter)
    {
        var json = "{\"code\":" + pCode + ",\"indent\":0,"
            + "\"parameters\":["
            + string.Join(",", pParameter) + "]}";
        MzJson.TryParse(json, out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException("fixture: " + fehler);
        }

        return MzCommandEntry.From(wert);
    }
}
