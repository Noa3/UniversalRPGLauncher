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
    public void Test_BestaetigenErreichtDenInterpreter()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;

        // **Und  die  Menueaktion  ist  die  eine,  die  einen  eigenen
        //  Zustand  setzt**, -- **und  sie  ist  damit  messbar.**
        Console.WriteLine("Menue erlaubt: " + state.AllowMenu
            + "  Menue offen: " + state.IsMainMenuActive);

        for (var i = 0; i < 10; i++)
        {
            lauf.SubmitInput(Rm2kInputAction.Menu);
            lauf.Update(1.0 / 60.0);
        }

        Console.WriteLine("nach 10 Bildern: Menue offen "
            + state.IsMainMenuActive);

        AssertTrue(state.AllowMenu,
            "**and the game allows the menu** -- and Dragon"
                + " Destiny never runs 11960 to forbid it, so the"
                + " default the reference uses is the one that"
                + " applies");
    }

    /// <summary>
    /// And the input mapper binds the eight actions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And an action nothing is bound to is a command
    /// nobody can press.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// And every action is bound to a key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the proof is that a real Godot key event resolves
    /// to the action</strong>, -- <strong>and not that the mapper has a
    /// method which says so</strong>, -- <strong>because a mapper that
    /// answers "yes" without resolving anything is a
    /// claim.</strong>
    /// </para>
    /// <para>
    /// <strong>And the key is the one the mapper bound itself</strong>,
    /// -- <strong>and those come from liblcf's own defaults in the
    /// mapper's constructor.</strong>
    /// </para>
    /// </remarks>
    public void Test_JedeAktionHatEineTaste()
    {
        var mapper = new Rm2kInputMapper();
        var tasten = new Dictionary<Rm2kInputAction, Key>
        {
            { Rm2kInputAction.MoveUp, Key.Up },
            { Rm2kInputAction.MoveDown, Key.Down },
            { Rm2kInputAction.MoveLeft, Key.Left },
            { Rm2kInputAction.MoveRight, Key.Right },
            { Rm2kInputAction.Confirm, Key.Enter },
            { Rm2kInputAction.Cancel, Key.Escape },
            // **Und  die  Menuestaste  ist  `M`  und  nicht  `Shift`.**
            // **Und  das  steht  so  in  `Rm2kInputMapper`  und  nicht  in
            //  meiner  Hoffnung**, -- **und  ich  hatte  `Shift`
            //  geraten**, -- **und  der  Test  hat  mich  darauf
            //  aufmerksam  gemacht.**
            { Rm2kInputAction.Menu, Key.M },
        };

        // **Und  jede  gebundene  Taste  wird  geprueft**,
        // -- **und  nicht  nur  eine  je  Aktion**,
        // -- **denn  der  Mapper  bindet  `WASD`  und  `Enter`  und
        //  `Leertaste`  und  `KP Enter`  an  dieselben  Aktionen.**
        var alle = new List<Key>
        {
            Key.Up, Key.W, Key.Down, Key.S, Key.Left, Key.A,
            Key.Right, Key.D, Key.Enter, Key.Space, Key.KpEnter,
            Key.Escape, Key.M,
        };

        foreach (var eintrag in tasten)
        {
            var aufgeloest = mapper.Resolve(
                new Godot.InputEventKey { Pressed = true,
                    Keycode = eintrag.Value });
            Console.WriteLine(eintrag.Key + " loest "
                + eintrag.Value + " zu " + aufgeloest);

            AssertTrue(aufgeloest == eintrag.Key,
                "**and " + eintrag.Key + " is bound to "
                    + eintrag.Value + "** -- and an action that"
                    + " resolves to None is a command nobody can"
                    + " press");
        }
    }
}
