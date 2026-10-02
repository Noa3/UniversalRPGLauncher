using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The thirteen commands that write into <c>$gameSystem</c>, into
/// <c>$gameMap.vehicle</c> and into the actors.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>136</c> is not <c>137</c> with another field.</strong>
/// <strong>It calls one thing more.</strong>
/// </para>
/// <code>
/// command136 = function() {
///     if (this._params[0] === 0) {
///         $gameSystem.disableEncounter();
///     } else {
///         $gameSystem.enableEncounter();
///     }
///     $gamePlayer.makeEncounterCount();
///     return true;
/// };
/// </code>
/// <code>
/// command137 = function() {
///     if (this._params[0] === 0) {
///         $gameSystem.disableFormation();
///     } else {
///         $gameSystem.enableFormation();
///     }
///     return true;
/// };
/// </code>
/// <para>
/// <strong>And the third line of <c>136</c> is the whole difference.</strong>
/// <strong><c>makeEncounterCount()</c> throws the counter back, so the next
/// fight does not start one tile after the player was told there will be
/// none.</strong> <strong>A reader that only flips the flag makes the next
/// battle come immediately.</strong>
/// </para>
/// <para>
/// <strong>And all three vehicle commands begin with the same two
/// lines</strong> -- <code>var vehicle = $gameMap.vehicle(
/// this._params[0]); if (vehicle) {</code> -- <strong>and that
/// <c>if</c> is why a ship nobody has come near is not an error.</strong>
/// </para>
/// <para>
/// <strong>And <c>206</c> takes no parameter at all</strong> -- <strong>
/// <c>$gamePlayer.getOnOffVehicle();</c></strong> -- <strong>because the
/// engine finds the vehicle under the player itself.</strong>
/// </para>
/// </remarks>
public partial class TestMzSystemAndVehicles : TestBase
{
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

    /// <summary>
    /// And the encounters start on and the counter is thrown back.
    /// </summary>
    public void Test_DerBegegnungszaehlerWirdZurueckgeworfen()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeEncounter, "0"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        fakten.Begegnungszaehler = 57;
        var aktionen = new List<MzAction>();
        interp.Run(aktionen, fakten);

        AssertTrue(!fakten.Spiel.BegegnungMoeglich,
            "**and a zero switches the encounters off** -- and it is "
            + fakten.Spiel.BegegnungMoeglich + ", and `command136` reads "
            + "the number as `=== 0`, so a zero disables");
        AssertEq(fakten.Begegnungszaehler, 0,
            "**and the counter was thrown back to zero** -- it was "
            + "fifty-seven, and `command136` ends in `$gamePlayer"
            + ".makeEncounterCount()` and `command137` does not, and a "
            + "reader that only flips the flag makes the next fight come "
            + "one tile later");
        AssertTrue(aktionen.Any(a => a.What.Contains(
                "thrown back", StringComparison.Ordinal)),
            "**and it says so**");
    }

    /// <summary>
    /// And <c>137</c> has no third line, and the difference is the whole
    /// point.
    /// </summary>
    public void Test_DieFormationHatKeineDritteZeile()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeFormationAccess, "0"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        fakten.Begegnungszaehler = 57;
        interp.Run(new List<MzAction>(), fakten);

        AssertTrue(!fakten.Spiel.FormationMoeglich,
            "**and rearranging is off**");
        AssertEq(fakten.Begegnungszaehler, 57,
            "**and the encounter counter did not move** -- it is still "
            + fakten.Begegnungszaehler + ", and `command137` has two "
            + "lines and `command136` has three");
        AssertTrue(fakten.Spiel.BegegnungMoeglich,
            "**and the encounters are untouched** -- and that is the "
            + "other half: 137 does not touch them at all");
    }

    /// <summary>
    /// And the three songs are three different things.
    /// </summary>
    public void Test_DreiLiederUndDreiBefehle()
    {
        // **Und `132` schreibt `Kampflied`, und `133` und `139`
        // schreiben zwei andere Felder.**
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeBattleBgm, "\"{\\\"name\\\":"
                + "\\\"Battle\\\",\\\"volume\\\":90,"
                + "\\\"pitch\\\":100,\\\"pan\\\":0}\""),
            Befehl(MzCommandTable.ChangeVictoryMe, "\"{\\\"name\\\":"
                + "\\\"Fanfare\\\",\\\"volume\\\":90,"
                + "\\\"pitch\\\":100,\\\"pan\\\":0}\""),
            Befehl(MzCommandTable.ChangeDefeatMe, "\"{\\\"name\\\":"
                + "\\\"GameOver\\\",\\\"volume\\\":90,"
                + "\\\"pitch\\\":100,\\\"pan\\\":0}\""),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        interp.Run(new List<MzAction>(), fakten);

        // **Und `132 Change Battle Bgm` legt das ganze Objekt ab**
        // -- **und `AudioManager` liest `.name` daraus.**  **Und mein
        // erster Test verlangte hier nur "Battle"**, **und der Zweig
        // schreibt zu Recht das ganze Objekt**, **weil
        // `setBattleBgm(value)` `value` ablegt und nicht
        // `value.name`.**
        AssertTrue(fakten.Spiel.Kampflied.Contains("Battle",
                StringComparison.Ordinal),
            "**and `132` wrote the battle song** -- it is '"
            + fakten.Spiel.Kampflied + "', and that is the whole object, "
            + "because `setBattleBgm(value)` stores `value` and not "
            + "`value.name`, and `AudioManager` reads `.name` out of it");
        AssertTrue(fakten.Spiel.HatKampflied,
            "**and it is marked as set**");
        AssertEq(fakten.Spiel.SiegLied, "Fanfare",
            "**and `133` writes the song after a win**");
        AssertEq(fakten.Spiel.NiederlageLied, "GameOver",
            "**and `139` writes the song after a loss**");
        AssertTrue(fakten.Spiel.SiegLiedGesetzt
            && fakten.Spiel.NiederlageLiedGesetzt,
            "**and both are marked as set**");
    }

    /// <summary>
    /// And a vehicle that is not on the map is not an error.
    /// </summary>
    public void Test_EinFahrzeugDasNichtDaIstIstKeinFehler()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeVehicleBgm, "3",
                "\"{\\\"name\\\":\\\"x\\\",\\\"volume\\\":90,"
                + "\\\"pitch\\\":100,\\\"pan\\\":0}\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        var aktionen = new List<MzAction>();
        interp.Run(aktionen, fakten);

        AssertEq(aktionen.Count, 0,
            "**and asking for a vehicle that is not there does nothing** "
            + "-- and there were " + aktionen.Count + " actions");
        AssertTrue(fakten.Notices.Any(x => x.Contains("if (vehicle)",
                StringComparison.Ordinal)),
            "**and it says why** -- it said '"
            + fakten.Notices.FirstOrDefault() + "', because "
            + "`$gameMap.vehicle(3)` finds none with that index and "
            + "`if (vehicle)` is why that is not an error");
    }

    /// <summary>
    /// And the three names an actor carries are three fields.
    /// </summary>
    public void Test_DreiNamenSindDreiFelder()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeName, "1", "\"Rin\""),
            Befehl(MzCommandTable.ChangeNickname, "1", "\"Wanderer\""),
            Befehl(MzCommandTable.ChangeProfile, "1", "\"RinFace\""),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts
        {
            PartyMembers = new HashSet<int> { 1 },
        };
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.Namen[1], "Rin",
            "**and `320` writes the name**");
        AssertEq(fakten.Spitznamen[1], "Wanderer",
            "**and `324` writes the nickname**");
        AssertEq(fakten.Profile[1], "RinFace",
            "**and `325` writes the profile**");
        AssertTrue(fakten.Namen[1] != fakten.Spitznamen[1],
            "**and they did not land in one field** -- and that is "
            + "`setName`, `setNickname` and `setProfile` as three separate "
            + "assignments, and a reader that put all three in the name "
            + "field would lose two of them");
    }

    /// <summary>
    /// And <c>326</c> is <c>gainTp</c>, with the operation at parameter
    /// two.
    /// </summary>
    public void Test_DieTaktischpunkte()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeTp, "0", "0", "0", "0", "30"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts
        {
            PartyMembers = new HashSet<int> { 1, 2 },
        };
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.Taktischpunkte[1], 30,
            "**and thirty went to the first actor** -- and it is "
            + fakten.Taktischpunkte[1] + ", and `command326` is "
            + "`operateValue(params[2], params[3], params[4])` and the "
            + "operation is the *second* parameter, not the first");
        AssertEq(fakten.Taktischpunkte[2], 30,
            "**and to the second** -- and a zero in the first parameter "
            + "means the whole party, which is `iterateActorEx`");
    }
}

