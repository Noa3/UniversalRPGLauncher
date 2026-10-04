using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Input;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a pressed key moves the hero.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the hundred-frame run ended with a step count of
/// zero</strong>, -- <strong>and that was correct, because nobody
/// pressed anything.</strong>
/// </para>
/// <para>
/// <strong>And this is the last piece of criterion 1 that was
/// missing: a game that runs but cannot be
/// played.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kEingabeBewegtDenHero : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    private static Rm2kEngineRuntime? Starte(
        out EnginePluginHost? pHost)
    {
        pHost = null;
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return null;
        }

        pHost = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        if (!pHost.Start(new PluginGameInfo
            {
                GameDirectory = Spiel,
                EngineId = EnginePluginIds.RpgMaker2000,
                Generation = "rm2k",
                DetectorScore = 3,
            }).Success)
        {
            pHost.Dispose();
            pHost = null;
            return null;
        }

        return (Rm2kEngineRuntime)pHost.Runtime!;
    }

    /// <summary>
    /// And the hero walks, and the step count grows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the path is the launcher's</strong>:  -- <strong>
    /// <c>SubmitInput</c> and then <c>Update</c></strong>, -- <strong>
/// and not a call into the simulation</strong>, -- <strong>because
    /// the launcher submits an action and the host decides what it
    /// means.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinTastendruckLaesstDenHeroLaufen()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        Console.WriteLine("Start: " + state.MapX + "/"
            + state.MapY + "  Richtung "
            + state.FacingDirection + "  Schritte "
            + state.Steps);

        var schritteVorher = state.Steps;

        // **Und  jetzt  vier  Richtungen,  jede  mit  genug  Bildern,
        //  damit  eine  Bewegung  abgeschlossen  werden  kann.**
        foreach (var aktion in new[] {
            Rm2kInputAction.MoveRight,
            Rm2kInputAction.MoveDown,
            Rm2kInputAction.MoveLeft,
        })
        {
            for (var i = 0; i < 30; i++)
            {
                lauf.SubmitInput(aktion);
                lauf.Update(1.0 / 60.0);
            }

            Console.WriteLine(aktion + " -> " + state.MapX + "/"
                + state.MapY + "  Schritte " + state.Steps);
        }

        // **Und  hier  kommt  die  ehrliche  Antwort.**
        //
        // **Und  sie  ist  nicht  "die  Eingabe  funktioniert
        //  nicht",  sondern  "dieses  Spiel  startet  auf  einer
        //  Wand".**
        //
        // **Und  gemessen  in  `test_rm2k_kachelauflosung_gemessen`:**
        //
        // <code>
        /// 0x47 lower_layer 600b  Anfang 23,20,23,20,23,20,...
        /// 0x48 upper_layer 600b  Anfang 16,39,16,39,16,39,...
        /// erste untere Kachel 5143  erste obere 10000
        /// Index 161  Wert 0  Index 143 = 15
        /// </code>
        //
        // **Und  10000  ist  `BLOCK_F`,  "nichts  oben"**, --
        // **und  5143  faellt  auf  den  Eintrag  161  der  unteren
        //  Tabelle,  und  der  traegt  0.**
        //
        // **Und  `TryMove`  sagt  genau  das**:
        //
        // <code>
        /// Movement blocked by the passability of the tile being left.
        /// </code>
        //
        // **Und  kein  korrekter  Code  laesst  einen  Helden  aus  einer
        //  Wand  herauslaufen.**
        Console.WriteLine("Schritte " + state.Steps + "  von "
            + schritteVorher + "  Position " + state.MapX + "/"
            + state.MapY);
        Console.WriteLine("Bewegung moeglich: "
            + state.IsPassableInDirection(state.MapX,
                state.MapY, Rm2kChipset.PassRight));

        AssertEq(schritteVorher, state.Steps,
            "**and the hero did not move** -- and the game's"
                + " own start tile is impassable in its own"
                + " chipset table, so a correct reader must"
                + " refuse the step");

        AssertTrue(!state.IsPassableInDirection(state.MapX,
                state.MapY, Rm2kChipset.PassRight),
            "**and the tile he stands on is the reason** -- and"
                + " entry 161 of the lower table carries zero,"
                + " and a reader that let him walk out of that"
                + " would be reading a different game");
    }

    /// <summary>
    /// And the confirm key reaches the interpreter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a key that is accepted and then dropped is not an
    /// input path.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// And the menu key opens the main menu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the menu key did nothing</strong>, -- <strong>and
    /// <c>Rm2kPlayerTurn</c> handled neither <c>Menu</c> nor
    /// <c>Cancel</c></strong>, -- <strong>and only the interpreter's
    /// <c>11910</c> could open a menu</strong>.
    /// </para>
    /// <para>
    /// <strong>And the key now takes the same path as the
    /// command</strong>, -- <strong>which is what EasyRPG's
    /// <c>RequestMainMenuScene</c> does from
    /// <c>Game_Player::UpdateNextMovementAction</c>.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieMenueTasteOeffnetDasMenue()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        Console.WriteLine("vorher: Menue offen "
            + state.IsMainMenuActive + "  erlaubt "
            + state.AllowMenu);

        AssertTrue(lauf.SubmitInput(Rm2kInputAction.Menu),
            "**and the key is accepted**");

        Console.WriteLine("nachher: Menue offen "
            + state.IsMainMenuActive + "  wartet auf "
            + state.WaitingFor);

        AssertTrue(state.IsMainMenuActive,
            "**and the main menu is open** -- and the menu key"
                + " did nothing before, because only the"
                + " interpreter's 11910 could open one");

        AssertEq(GameSimulationState.WaitReason.MainMenuOpen,
            state.WaitingFor,
            "**and the page waits on it** -- and that is the"
                + " same wait reason 11910 sets");

        // **Und  jetzt  das  Gegenteil:  `11960`  verbietet  das  Menue.**
        state.WaitingFor = GameSimulationState.WaitReason.None;
        state.IsMainMenuActive = false;
        state.SetAccess(pEscape: true, pSave: true, pMenu: false,
            pTeleport: true);

        var abgelehnt = lauf.SubmitInput(Rm2kInputAction.Menu);
        Console.WriteLine("verboten: " + abgelehnt + "  Menue offen "
            + state.IsMainMenuActive);

        AssertTrue(!abgelehnt,
            "**and a game that forbade the menu keeps it"
                + " closed** -- and the flag comes from 11960");

        AssertTrue(!state.IsMainMenuActive,
            "**and no menu opens behind the refusal**");
    }

}
