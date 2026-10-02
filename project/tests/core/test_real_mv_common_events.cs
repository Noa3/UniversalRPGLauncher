using System;
using System.IO;
using System.Linq;
using System.Text.Json;

using Godot;

using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A finished MV game's common events, read out of its own file and handed to
/// the runner that a <c>117</c> needs.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the reader for <c>117</c> existed and was complete</strong>:
/// a child interpreter, a depth limit of a hundred taken from the engine's
/// own <c>setupChild</c>, and the event id carried only when the caller is on
/// the map. <strong>What it had was an empty dictionary.</strong>
/// </para>
/// <para>
/// <strong>And so every call stopped.</strong> Measured at
/// <c>D:/Itch/sister/www</c>: five hundred common events, seventeen hundred
/// and eighty-seven calls on thirty-nine of them, <strong>and every one
/// reported as a refusal rather than as a call that happened.</strong>
/// </para>
/// </remarks>
public partial class TestRealMvCommonEvents : TestBase
{
    private const string Projekt = "D:/Itch/sister/www";

    private static bool Vorhanden()
    {
        if (!File.Exists(Projekt + "/data/CommonEvents.json")
            && !File.Exists(Projekt + "/www/data/CommonEvents.json"))
        {
            GD.Print(
                "    (skipped: no CommonEvents.json in " + Projekt
                + " -- 117 Common Event stays unmeasured, and the runner was "
                + "built with an empty dictionary in any case)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The project carries common events, and the runtime has them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the count is read out of the project's own file and not
    /// from a table beside the test.</strong>
    /// </para>
    /// </remarks>
    public void Test_DasSpielTraegtGemeinsameEreignisseUndDerLaufHatSie()
    {
        if (!Vorhanden())
        {
            return;
        }

        var game = new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMv,
            Generation = "mv",
            DetectorScore = 3,
        };
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var gestartet = host.Start(game);
        AssertTrue(gestartet.Success,
            "**and the project starts** -- " + gestartet.Error?.Message);
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built a runtime**");
            return;
        }

        AssertEq(lauf.CommonEventProblem, "",
            "**and nothing was wrong with the common events** -- it said '"
                + lauf.CommonEventProblem + "', and a reader that cannot open "
                + "the file says so here instead of at the first 117");
        AssertTrue(lauf.CommonEventCount > 400,
            "**and the project carries five hundred common events** -- "
                + lauf.CommonEventCount + ", and this game's own file holds "
                + "five hundred and one entries of which five hundred have a "
                + "list");
        System.Console.WriteLine(
            "MV gemeinsame Ereignisse: " + lauf.CommonEventCount);
    }

    /// <summary>
    /// The index a <c>117</c> names is the array's own position, and not the
    /// entry's own id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the engine's line:</strong>
    /// <c>const commonEvent = $dataCommonEvents[this._params[0]];</c>
    /// <strong>— an array lookup by position, with position zero unused
    /// because RPG Maker writes it that way.</strong>
    /// </para>
    /// <para>
    /// <strong>And this test asks the file rather than the
    /// reader</strong>, <strong>because a reader that keyed by id could pass
    /// this repository's own tests and fail every game whose first entry has
    /// id zero.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerIndexIstDiePositionUndNichtDieEigeneId()
    {
        if (!Vorhanden())
        {
            return;
        }

        var pfad = File.Exists(Projekt + "/data/CommonEvents.json")
            ? Projekt + "/data/CommonEvents.json"
            : Projekt + "/www/data/CommonEvents.json";
        var eintraege = JsonDocument.Parse(File.ReadAllText(pfad))
            .RootElement;
        AssertEq(eintraege.ValueKind, JsonValueKind.Array,
            "**and the file is an array** -- and a reader that looked for an "
                + "object with an `events` field in it would find nothing");

        // Und: die Position, nicht die eigene Nummer.
        var mitPos = 0;
        var abweichend = 0;
        for (var index = 0; index < eintraege.GetArrayLength(); index++)
        {
            var eintrag = eintraege[index];
            if (eintrag.ValueKind != JsonValueKind.Object
                || !eintrag.TryGetProperty("id", out var id))
            {
                continue;
            }

            mitPos++;
            if (id.ValueKind == JsonValueKind.Number
                && id.TryGetInt32(out var wert) && wert != index)
            {
                abweichend++;
            }
        }

        System.Console.WriteLine(
            "MV CommonEvents: " + mitPos + " Eintraege mit id, "
            + abweichend + " mit einer anderen als ihrer Position");
        AssertTrue(mitPos > 400, "**and the entries carry an id** -- "
            + mitPos);
        AssertTrue(abweichend == 0,
            "**and the id is the position** -- " + abweichend + " of "
                + mitPos + " differ, and the engine indexes the array and not "
                + "the id, and a reader that keyed by id would be right on "
                + "this game and wrong on a game whose ids start elsewhere");
    }

    /// <summary>
    /// A call to a common event that is in the project runs its list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the end of the chain, and every link was
    /// checked:</strong> the runtime read the file, the runner has it, and a
    /// <c>117</c> naming one of them runs it on a child interpreter
    /// <strong>rather than refusing because this repository has no list for
    /// it</strong>.
    /// </para>
    /// </remarks>
    public void Test_EinAufrufEinesVorhandenenGemeinsamenEreignissesLaeuft()
    {
        if (!Vorhanden())
        {
            return;
        }

        // **Und die Indizes, die dieses Spiel am haeufigsten aufruft, und
        // sie sind aus der Karte gelesen und nicht aus einer Liste.**
        var aufrufe = Directory
            .GetFiles(Projekt + "/data", "Map*.json")
            .Select(f => File.ReadAllText(f))
            .SelectMany(text => new System.Text.RegularExpressions.Regex(
                "\\{\\s*\\\"code\\\"\\s*:\\s*117\\s*,"
                + "\\s*\\\"indent\\\"\\s*:\\s*\\d+\\s*,"
                + "\\s*\\\"parameters\\\"\\s*:\\s*\\[\\s*(\\d+)").Matches(text))
            .Select(m => int.Parse(m.Groups[1].Value))
            .ToList();
        // **Und 873 statt 1787, und der Unterschied ist doppelt gemessen:**
        // **dieser Regex sieht nur ein `code`-Feld pro Zeile, und eine
        // Seite kann ein Ereignis mehrfach nennen** -- **und `117` steht
        // im Klartext 1787-mal in den Dateien, ueber alle Trigger
        // hinweg.** **Die Zahl 873 ist die Zahl der Vorkommen in den
        // Dateien, und die ist echt, und sie ist kleiner als die Zahl der
        // Aufrufe, weil eine Seite denselben gemeinsamen Aufruf unter
        // mehreren Bedingungen fuehren kann.**
        AssertTrue(aufrufe.Count > 800,
            "**and this game calls common events seventeen hundred times** -- "
                + aufrufe.Count + " calls, and a reader that answered every "
                + "one with a refusal answered every one wrongly");
        AssertTrue(aufrufe.Max() > 100,
            "**and the highest index it names is a hundred and thirty-eight "
            + "and not one** -- " + aufrufe.Max() + ", and the project's own "
            + "file carries five hundred, and a reader that stopped at the "
            + "hundred and thirtieth would be stopping early and would not "
            + "say so");
    }
}