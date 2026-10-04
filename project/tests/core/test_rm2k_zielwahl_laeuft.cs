using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the target list names the game's own monsters.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the launcher passed a turn order where a target
/// belongs</strong>, -- <strong>and
/// <c>Rm2kZugfolge.Naechster</c> answers who acts next, not who is
/// struck</strong>.
/// </para>
/// <para>
/// <strong>And the target list is what the window shows</strong>, --
/// <strong>so its content has to be checkable without a window</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kZielwahlLaeuft : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the list carries the troop's own names and hit points.
    /// </summary>
    public void Test_DieZiellisteNenntDieEigenenGegner()
    {
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var start = host.Start(new PluginGameInfo
        {
            GameDirectory = Spiel,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        });
        if (!start.Success)
        {
            AssertTrue(false, "**and the game starts**");
            return;
        }

        var lauf = (Rm2kEngineRuntime)host.Runtime!;
        var state = lauf.Simulation;

        if (!Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 4, out var truppe, out var grund))
        {
            AssertTrue(false, "**and a troop is built** -- " + grund);
            return;
        }

        foreach (var mitglied in truppe)
        {
            state.TroopMembers.Add(mitglied);
        }

        state.IsBattleActive = true;
        var ziele = Rm2kZielwahl.MoeglicheZiele(state);
        Console.WriteLine("Truppe: " + state.TroopMembers.Count
            + "  Ziele: " + ziele.Count);
        foreach (var index in ziele)
        {
            Console.WriteLine("  " + index + " "
                + state.TroopMembers[index]["name"].AsString()
                + "  " + state.MonsterHp(index) + "/"
                + state.MonsterMaxHp(index));
        }

        AssertTrue(ziele.Count > 0,
            "**and a real troop has targets**");

        // **Und  der  erste  Knopf  traegt  den  Namen  des  ersten
        //  Gegners.**
        AssertTrue(state.TroopMembers[ziele[0]]["name"].AsString().Length
                > 0,
            "**and the target names the monster** -- and the"
                + " bank gives every enemy a name, and a list of"
                + " numbers is not something a player can aim");

        // **Und  jetzt  das  Ziel  setzen.**
        var gewaehlt = lauf.AktuellesZiel.WaehleGegner(
            state, ziele[0], out var zielFehler);
        Console.WriteLine("gewaehlt: " + gewaehlt + " -> "
            + zielFehler + "  Gegner "
            + lauf.AktuellesZiel.GegnerIndex);
        AssertTrue(gewaehlt,
            "**and the target is chosen**");

        AssertTrue(lauf.AktuellesZiel.HatGegner,
            "**and it says it is a monster**");

        AssertEq(ziele[0], lauf.AktuellesZiel.GegnerIndex,
            "**and it is the troop's own index** -- and a sorted"
                + " list would change which monster a battle"
                + " branch names");

        // **Und  jetzt  ein  Held  als  Ziel  --  und  das  muss
        //  scheitern,  weil  die  Zielwahl  sich  nicht  umstellen
        //  laesst,  ohne  dass  es  gesagt  wird.**
        var alsHeld = lauf.AktuellesZiel.WaehleHeld(state, 1,
            out var heldFehler);
        Console.WriteLine("als Held: " + alsHeld + " -> " + heldFehler);
        AssertTrue(alsHeld,
            "**and a hero can be aimed at too** -- because the"
                + " scope decides the side and the party has a"
                + " hero in it");

        AssertTrue(!lauf.AktuellesZiel.HatGegner,
            "**and the aim no longer claims a monster**");

        // **Und  jetzt  zurueck  auf  den  Gegner.**
        lauf.AktuellesZiel.WaehleGegner(state, ziele[0], out _);
        AssertTrue(lauf.AktuellesZiel.HatGegner,
            "**and aiming at a monster again says so**");
    }
}
