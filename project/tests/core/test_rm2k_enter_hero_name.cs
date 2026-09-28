using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for 10740 Enter Hero Name and the name the save file keeps — from
/// liblcf's <c>Code::EnterHeroName</c> and EasyRPG's
/// <c>Game_Interpreter_Map::CommandEnterHeroName</c> and
/// <c>Game_Actor::SetName</c>.
/// </summary>
public partial class TestRm2kEnterHeroName : TestBase
{
    private static Rm2kMap.EventCommand Cmd(int pCode, params int[] pParameters)
    {
        return new Rm2kMap.EventCommand
        {
            Code = pCode,
            Text = "",
            Parameters = new List<int>(pParameters),
        };
    }

    /// <summary>
    /// A state with one hero, so the command has somebody to name.
    /// </summary>
    private static GameSimulationState WithHero(int pActorId = 1)
    {
        var state = new GameSimulationState { MapId = 1 };
        state.ActorValues[pActorId] = new Rm2kActorValues();
        return state;
    }

    // ---- The command

    /// <summary>
    /// The three parameters are the hero, the face index and the flag.
    /// </summary>
    /// <remarks>
    /// <strong>The second is an index and not a file name.</strong> The
    /// reference's <c>Scene_Name(*actor, charset, use_default_name)</c> takes
    /// an <c>int</c>, because a face is a position in the actor's own face
    /// set — <strong>and a reader that read it as a file name would have
    /// looked for a charset called "2" and found nothing.</strong>
    /// </remarks>
    public void Test_TheSecondParameterIsAFaceIndexAndNotAFileName()
    {
        var state = WithHero(1);
        var gesehen = -1;
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.EnterHeroName, 1, 2, 1) },
            new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: null,
            eventPlaceReader: null,
            eventPlaceMover: null,
            heroNameEntry: (id, charset, _) => gesehen = charset);
        interpreter.ExecuteFrame();

        AssertEq(gesehen, 2,
            "**the face index arrived as the number 2** — the reference's own "
                + "scene takes an int, and a reader that treated it as a "
                + "charset name would have shown no face at all");
    }

    /// <summary>
    /// The third parameter is a flag, and both of its values reach the
    /// screen.
    /// </summary>
    /// <remarks>
    /// <strong>The reference names it <c>use_default_name</c> and hands it to
    /// the scene</strong> — <strong>and a reader that treated it as part of a
    /// name would have shown every hero a name ending in a digit.</strong>
    /// Zero withholds the database name and one offers it.
    /// </remarks>
    public void Test_TheThirdParameterIsAFlagAndNotText()
    {
        foreach (var (parameter, erwartet) in new[] { (0, false), (1, true) })
        {
            var state = WithHero(1);
            var flag = !erwartet;
            var interpreter = new EventInterpreter(
                state, 1,
                new[] { Cmd(EventInterpreter.EnterHeroName, 1, 2, parameter) },
                new PresentationState(),
                moveRouteStarter: null,
                vehicleBoardToggle: null,
                spriteFlasher: null,
                eventPlaceReader: null,
                eventPlaceMover: null,
                heroNameEntry: (_, _, useDefault) => flag = useDefault);
            interpreter.ExecuteFrame();

            AssertEq(flag, erwartet,
                "**a third parameter of " + parameter + " means "
                    + (erwartet ? "offer" : "withhold")
                    + " the database name** — the reference's own "
                    + "use_default_name, and a reader that treated it as "
                    + "text would have shown a name ending in a digit");
        }
    }

    /// <summary>
    /// The command itself writes no name, and says so by not writing one.
    /// </summary>
    /// <remarks>
    /// <strong>The reference builds a scene and the scene writes the name when
    /// the player is done.</strong> <strong>A reader that stored the name here
    /// would have renamed a hero with no prompt at all</strong> — which is a
    /// different game, and a worse one.
    /// </remarks>
    public void Test_TheCommandWritesNoNameOfItsOwn()
    {
        var state = WithHero(1);
        var werte = state.ActorValues[1];
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.EnterHeroName, 1, 2, 1) },
            new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: null,
            eventPlaceReader: null,
            eventPlaceMover: null,
            heroNameEntry: (_, _, _) => { });
        interpreter.ExecuteFrame();

        AssertEq(werte.Name, Rm2kActorValues.UnchangedName,
            "**the hero still answers to the database's name** — the command "
                + "opened a screen and the screen has not been used yet, and a "
                + "reader that stored a name here would have renamed a hero "
                + "with no prompt at all");
    }

    /// <summary>
    /// A hero that does not exist shows no screen, and says so.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's own
    /// <c>Output::Warning("EnterHeroName: Invalid actor ID {}")</c></strong> —
    /// the same shape <c>11320</c> and <c>11330</c> have, and the same
    /// distinction: a warning, and the page still moves.
    /// </remarks>
    public void Test_AHeroThatDoesNotExistShowsNoScreen()
    {
        var state = WithHero(1);
        var aufrufe = 0;
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.EnterHeroName, 99, 2, 1) },
            new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: null,
            eventPlaceReader: null,
            eventPlaceMover: null,
            heroNameEntry: (_, _, _) => aufrufe += 1);
        var weiter = interpreter.ExecuteFrame();

        AssertEq(aufrufe, 0,
            "**hero 99 opens no screen** — there is nobody by that name");
        AssertEq(weiter, true,
            "**and the page still moves** — the reference returns true, which "
                + "is a warning and not a refusal");
        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("Enter hero name"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "and it says so — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// No name screen at all is a diagnostic and not a crash.
    /// </summary>
    public void Test_NoNameScreenIsADiagnostic()
    {
        var state = WithHero(1);
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.EnterHeroName, 1, 2, 1) },
            new PresentationState());
        var weiter = interpreter.ExecuteFrame();

        AssertEq(weiter, true, "an interpreter with no screen still advances");
        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("no name screen is attached"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "and it says so — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    // ---- The name the save file keeps

    /// <summary>
    /// A name that differs from the database's is kept, and one that does not
    /// is not.
    /// </summary>
    /// <remarks>
    /// <strong>This is the reference's own rule, in
    /// <c>Game_Actor::SetName</c>:</strong>
    /// <c>data.name = (new_name != dbActor-&gt;name) ? new_name :
    /// lcf::rpg::SaveActor::kEmptyName</c>. <strong>Only a name that differs
    /// from the database's is kept</strong> — and that is not a
    /// simplification. A save that carried the database name into every hero
    /// would keep the <em>old</em> name after the game was renamed in the
    /// editor, <strong>and the reference's save format has a sentinel
    /// precisely so that distinction survives.</strong>
    /// </remarks>
    public void Test_OnlyANameThatDiffersIsKept()
    {
        var werte = new Rm2kActorValues();

        werte.SetName("Held", "Alex");
        AssertEq(werte.Name, "Held",
            "**a name that differs from the database's is kept** — a save that "
                + "dropped it would have made every renamed hero anonymous");

        werte.SetName("Alex", "Alex");
        AssertEq(werte.Name, Rm2kActorValues.UnchangedName,
            "**and a name that matches the database's is not** — the reference "
                + "writes kEmptyName, and a reader that stored it would have "
                + "carried the old name into a save after the game was "
                + "renamed in the editor");
    }

    /// <summary>
    /// An empty name is a real name, and not the sentinel.
    /// </summary>
    /// <remarks>
    /// <strong>A player may call a hero nothing, and the reference stores that
    /// as a name of zero length</strong> — <strong>while the sentinel is a
    /// different value entirely.</strong> A reader that used "" for
    /// "unchanged" would have made every renamed hero nameless the moment the
    /// save was written.
    /// </remarks>
    public void Test_AnEmptyNameIsARealNameAndNotTheSentinel()
    {
        var werte = new Rm2kActorValues();
        werte.SetName("", "Alex");

        AssertEq(werte.Name, "",
            "**an empty name is stored as an empty name** — a player may call "
                + "a hero nothing, and the reference keeps that apart from the "
                + "sentinel");
        AssertTrue(werte.Name != Rm2kActorValues.UnchangedName,
            "**and it is not the sentinel** — the two are different values, and "
                + "a reader that used \"\" for \"unchanged\" would have made "
                + "every renamed hero nameless");

        AssertEq(werte.ResolveName("Alex"), "",
            "**and the hero answers to nothing** — the reference's own "
                + "GetName falls back for kEmptyName and for nothing else, and "
                + "a reader that fell back for every empty string would have "
                + "answered with the database's name");
    }

    /// <summary>
    /// A hero nobody renamed answers to the database's name.
    /// </summary>
    public void Test_AHeroNobodyRenamedAnswersToTheDatabaseName()
    {
        var werte = new Rm2kActorValues();

        AssertEq(werte.Name, Rm2kActorValues.UnchangedName,
            "**a fresh hero carries the sentinel** — and not an empty string, "
                + "which is a name");
        AssertEq(werte.ResolveName("Alex"), "Alex",
            "**and answers to the database's name** — the reference's own "
                + "GetName falls back for exactly this one value");
    }

    /// <summary>
    /// Renaming back to the database's name clears the override again.
    /// </summary>
    /// <remarks>
    /// <strong>The sentinel is a value, and the rule is a comparison, so a
    /// name that becomes the database's name again writes the sentinel
    /// again.</strong> A reader that only ever wrote a differing name would
    /// have kept the override — <strong>and a save from that game would have
    /// still carried "Held" for a hero the player had renamed back.</strong>
    /// </remarks>
    public void Test_RenamingBackToTheDatabaseNameClearsTheOverride()
    {
        var werte = new Rm2kActorValues();
        werte.SetName("Held", "Alex");
        AssertEq(werte.ResolveName("Alex"), "Held", "the hero answers to Held");

        werte.SetName("Alex", "Alex");
        AssertEq(werte.ResolveName("Alex"), "Alex",
            "**renaming back to the database's name answers to that name** — "
                + "the reference compares and writes the sentinel again, and a "
                + "reader that only ever wrote differing names would have kept "
                + "the override in the save file");
    }
}
