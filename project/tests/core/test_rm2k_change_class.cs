using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for 1008 Change Class — from liblcf's <c>Code::ChangeClass</c> and
/// EasyRPG's <c>Game_Interpreter::CommandChangeClass</c> and
/// <c>Game_Actor::ChangeClass</c>.
/// </summary>
public partial class TestRm2kChangeClass : TestBase
{
    private static Rm2kMap.EventCommand Cmd(params int[] pParameters)
    {
        return new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.ChangeClass,
            Text = "",
            Parameters = new List<int>(pParameters),
        };
    }

    /// <summary>
    /// A 2003 state with one hero, class 1, at level 10 with some experience.
    /// </summary>
    private static GameSimulationState WithHero(bool p2k3 = true)
    {
        var state = new GameSimulationState { MapId = 1, SupportsRpg2k3ECommands = p2k3 };
        state.ActorValues[1] = new Rm2kActorValues();
        state.SetActorLevel(1, 10);
        state.SetActorExp(1, 5000);
        state.ActorClassId[1] = 1;
        state.CurrentHp[1] = 30;
        state.CurrentSp[1] = 5;
        return state;
    }

    // ---- The 2003 gate

    /// <summary>
    /// A 2K game has no class command, and the reference's guard says so.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's first line is
    /// <c>if (!Player::IsRPG2k3Commands()) return true;</c></strong> — <strong>a
    /// reader that ran the command anyway would have changed a hero's class in
    /// a file format that has no field for it</strong>, and every later
    /// command that reads the class would have read a number RPG_RT never
    /// wrote.
    /// </remarks>
    public void Test_A2KGameHasNoClassCommand()
    {
        var state = WithHero(p2k3: false);
        var vorher = state.ActorClassId[1];
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(1, 1, 2, 1, 0, 0, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.ActorClassId[1], vorher,
            "**a 2K game's hero keeps the class it had** — the reference's own "
                + "guard returns before anything changes, and a reader that ran "
                + "it anyway would have written a field RPG_RT never writes");
        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("a 2K game has no class"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "and it says so — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    // ---- The two RPG_RT behaviours the reference comments on

    /// <summary>
    /// The whole equipment goes, even when the level does not.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's own comment is the reason:</strong>
    /// <c>// RPG_RT always removes all equipment on level change.</c>
    /// <strong>A reader that kept the equipment would have left a hero
    /// wearing the previous class's armour with the new class's
    /// statistics</strong> — and a game that checks a hero's equipment after
    /// the change would branch on a state the original never produces.
    /// </remarks>
    public void Test_TheWholeEquipmentGoesEvenWhenTheLevelDoesNot()
    {
        var state = WithHero();
        state.ChangeEquipment(1, GameSimulationState.EquipmentSlot.Weapon, 1);
        state.ChangeEquipment(1, GameSimulationState.EquipmentSlot.Shield, 2);
        var vorherLevel = state.GetActorLevel(1);
        // **Level 0: der Befehl laesst die Stufe, wo sie ist.**
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(1, 1, 2, 0, 0, 0, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.GetActorLevel(1), vorherLevel,
            "**the level stayed where it was** — parameter 3 was zero, and the "
                + "reference reads it as `level1 = parameters[3] > 0`");
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon)
                + state.GetEquipment(1, GameSimulationState.EquipmentSlot.Shield),
            0,
            "**and the equipment is gone anyway** — RPG_RT removes all of it on "
                + "a class change, and a reader that kept it would have left a "
                + "hero in the previous class's armour");
    }

    /// <summary>
    /// The experience is reset even when the level did not move.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's own comment:</strong> <c>// RPG_RT always resets
    /// EXP when class is changed, even if level unchanged.</c> <strong>A reader
    /// that only reset the experience when the level moved would have left a
    /// hero carrying the progress of a class they no longer have</strong> —
    /// and a game that checks "how far along is this hero" would see a number
    /// RPG_RT never shows.
    /// </remarks>
    public void Test_TheExperienceIsResetEvenWhenTheLevelDidNotMove()
    {
        var state = WithHero();
        AssertEq(state.GetActorExp(1), 5000,
            "the hero starts with five thousand experience");
        var level = state.GetActorLevel(1);
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(1, 1, 2, 0, 0, 0, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.GetActorLevel(1), level,
            "**the level did not move** — and that is the case the reference's "
                + "comment is about");
        AssertEq(state.GetActorExp(1), 0,
            "**and the experience is zero anyway** — RPG_RT resets it on every "
                + "class change, and a reader that tied it to the level would "
                + "have left a hero carrying another class's progress");
    }

    // ---- The parameters

    /// <summary>
    /// Class zero is "no class", and not an error.
    /// </summary>
    /// <remarks>
    /// <strong>The reference guards its warning with
    /// <c>class_id != 0</c></strong> and falls back to the actor's own database
    /// settings for the flags. <strong>A reader that refused zero would have
    /// refused the one thing a 2K3 game can do to a hero's class</strong> —
    /// and a game that removes its own classes would have a hero whose class
    /// command silently did nothing.
    /// </remarks>
    public void Test_ClassZeroIsNoClassAndNotAnError()
    {
        var state = WithHero();
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(1, 1, 0, 0, 0, 0, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.ActorClassId[1], 0,
            "**class zero removes the hero from a class** — the reference's "
                + "own `class_id != 0` guard is what makes it legal, and a "
                + "reader that refused it would have refused the only class "
                + "change a 2K3 game can undo");
        var geklagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("is outside 0 to 5000"))
            {
                geklagt = true;
            }
        }

        AssertTrue(!geklagt, "and it does not warn — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// The class is written, and the whole party changes at once.
    /// </summary>
    /// <remarks>
    /// <strong>The first parameter is the same actor mode every other actor
    /// command has</strong>, and the reference walks
    /// <c>GetActors(parameters[0], parameters[1])</c> — so mode zero means
    /// the party and not "no actor".
    /// </remarks>
    public void Test_TheWholePartyChangesAtOnce()
    {
        var state = WithHero();
        state.ActorValues[2] = new Rm2kActorValues();
        state.ActorClassId[2] = 1;
        state.PartyMemberIds.Add(1);
        state.PartyMemberIds.Add(2);
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(0, 0, 5, 0, 0, 0, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.ActorClassId[1], 5, "the first party member is in class 5");
        AssertEq(state.ActorClassId[2], 5,
            "**and the second** — the reference walks GetActors, and a reader "
                + "that took mode zero as \"no actor\" would have changed "
                + "nobody");
    }

    /// <summary>
    /// A six-wide command is refused, and says so.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's <c>CmdSetup</c> gives 1008 a width of
    /// seven</strong>, and the reference reads <c>parameters[6]</c> without a
    /// guard. A reader that accepted six would have read past the end of the
    /// list on a file that predates the message flag.
    /// </remarks>
    public void Test_ASixWideCommandIsRefused()
    {
        foreach (var breite in new[] { 0, 4, 5, 6 })
        {
            var state = WithHero();
            var parameter = new int[breite];
            for (var i = 0; i < breite; i++)
            {
                parameter[i] = i == 2 ? 5 : 0;
            }

            var interpreter = new EventInterpreter(
                state, 1, new[] { Cmd(parameter) }, new PresentationState());
            interpreter.ExecuteFrame();

            AssertEq(state.ActorClassId[1], 1,
                "**a command of " + breite + " parameters changes no class** — "
                    + "the reference's CmdSetup gives 1008 a width of seven, "
                    + "and a reader that took six would have read past the end "
                    + "of the list");
        }
    }

    /// <summary>
    /// The base values are written and not increased.
    /// </summary>
    /// <remarks>
    /// <strong>The parameter mode 3 writes nothing at all</strong> — the
    /// reference's <c>if (new_param != eParamReset)</c> guards the write. <strong>
    /// For every other mode the values are the ones secured at the top of the
    /// method</strong>, halved for mode 1 — and a reader that *added* a
    /// difference here would have doubled a hero's statistics on every class
    /// change, and a game that changes a class in a loop would have run the
    /// party's hit points away.
    /// </remarks>
    public void Test_TheBaseValuesAreWrittenAndNotIncreased()
    {
        // **Modus 3: kein Schreiben ueberhaupt.**
        var state = WithHero();
        var vorherHp = state.GetOrCreateActorValues(1).BaseMaxHp;
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(1, 1, 5, 0, 0, 3, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();
        AssertEq(state.GetOrCreateActorValues(1).BaseMaxHp, vorherHp,
            "**parameter mode 3 writes no base value** — the reference's "
                + "`if (new_param != eParamReset)` guard, and a reader that "
                + "wrote anyway would have changed a hero the command was told "
                + "to leave alone");

        // **Modus 1: haelbiert.** Und dieselbe Basis noch einmal geschrieben
        // waere ein Verdopplung.
        // **Und jetzt der Halbmodus auf einem Wert, dessen Haelfte sich von
        // seinem Ursprung unterscheidet.** Der Default eines neuen Helden ist
        // ein Trefferpunkt, und dessen Haelfte ist null -- was die Klamme
        // wieder auf eins hebt. **Ein Test, der den Default nimmt, prueft die
        // Klamme und nicht die Haalbierung.**
        var state2 = WithHero();
        state2.GetOrCreateActorValues(1).SetBaseParameter(0, 80);
        var basis = state2.GetOrCreateActorValues(1).BaseMaxHp;
        AssertEq(basis, 80, "the hero starts with eighty base hit points");
        var i2 = new EventInterpreter(
            state2, 1, new[] { Cmd(1, 1, 5, 0, 0, 1, 0) },
            new PresentationState());
        i2.ExecuteFrame();
        AssertEq(state2.GetOrCreateActorValues(1).BaseMaxHp, 40,
            "**parameter mode 1 halves the base hit points** — the reference's "
                + "own `max_hp /= 2` and nothing more, and a reader that added "
                + "a difference would have written 160");
    }

    /// <summary>
    /// A class number outside the format's range changes nothing.
    /// </summary>
    /// <remarks>
    /// <strong>The reference reads the class through
    /// <c>ReaderUtil::GetElement(lcf::Data::classes, class_id)</c> and warns
    /// when it is null and the id is not zero.</strong> <strong>A reader that
    /// accepted any number would have written a class id into the save file
    /// that no class entry has</strong>, and every later command that reads
    /// the class — the party's stat window, a class change to something else —
    /// would have looked up an entry that is not there.
    /// </remarks>
    public void Test_AClassNumberOutsideTheRangeChangesNothing()
    {
        foreach (var klasse in new[] { 5001, 10000, -1 })
        {
            var state = WithHero();
            var vorher = state.ActorClassId[1];
            var interpreter = new EventInterpreter(
                state, 1, new[] { Cmd(1, 1, klasse, 0, 0, 0, 0) },
                new PresentationState());
            interpreter.ExecuteFrame();

            AssertEq(state.ActorClassId[1], vorher,
                "**class " + klasse + " changes nothing** — the format's bound "
                    + "is 5000, and a reader that accepted any number would "
                    + "have written a class id no entry has");
            var gesagt = false;
            foreach (var d in state.Diagnostics)
            {
                if (d.Contains("is outside 0 to 5000"))
                {
                    gesagt = true;
                }
            }

            AssertTrue(gesagt, "**and it warns** — the reference's own "
                + "warning, and the diagnostics were: "
                + string.Join(" | ", state.Diagnostics));
        }
    }

    /// <summary>
    /// The level flag is read as a flag, and the two values differ.
    /// </summary>
    /// <remarks>
    /// <strong>The reference writes
    /// <c>bool level1 = com.parameters[3] &gt; 0;</c></strong> — a positive
    /// value is the drop, everything else keeps the level. <strong>A reader
    /// that read it as a sign would drop the level on a zero</strong>, and a
    /// game's class change that meant "keep the level" would have reset a hero
    /// to level one.
    /// </remarks>
    public void Test_TheLevelFlagIsReadAsAFlag()
    {
        // **Null heisst: die Stufe bleibt.**
        var state = WithHero();
        var level = state.GetActorLevel(1);
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(1, 1, 5, 0, 0, 0, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();
        AssertEq(state.GetActorLevel(1), level,
            "**parameter 3 of zero keeps the level** — the reference reads "
                + "`level1 = parameters[3] > 0`, and a reader that read it as "
                + "a sign would have dropped the hero to level one");

        // **Und eins heisst: Stufe eins.**
        var state2 = WithHero();
        var i2 = new EventInterpreter(
            state2, 1, new[] { Cmd(1, 1, 5, 1, 0, 0, 0) },
            new PresentationState());
        i2.ExecuteFrame();
        AssertEq(state2.GetActorLevel(1), 1,
            "**and parameter 3 of one drops to level one** — the reference's "
                + "own test, and the two values must differ or the flag is "
                + "not a flag");
    }

    /// <summary>
    /// Parameter mode 3 writes no base value, on a base whose half would
    /// differ from it.
    /// </summary>
    /// <remarks>
    /// <strong>The first version of this test used a fresh hero, whose base
    /// hit points are one</strong> — and one halved is zero, which the clamp
    /// puts back to one. <strong>So the test would have passed for a reader
    /// that wrote in mode 3.</strong> The base is eighty here, and eighty
    /// halved is forty, and the mode-3 run leaves it at eighty.
    /// </remarks>
    public void Test_ParameterModeThreeWritesNothing()
    {
        var state = WithHero();
        state.GetOrCreateActorValues(1).SetBaseParameter(0, 80);
        var vorher = state.GetOrCreateActorValues(1).BaseMaxHp;
        AssertEq(vorher, 80, "the hero starts with eighty base hit points");
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(1, 1, 5, 0, 0, 3, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.GetOrCreateActorValues(1).BaseMaxHp, vorher,
            "**mode 3 leaves the base at eighty** — the reference's own "
                + "`if (new_param != eParamReset)` guard, and half of eighty "
                + "is forty, so a reader that wrote in this mode would be "
                + "caught here and not by a base of one");
    }

    /// <summary>
    /// The first RPG_RT skill bug: level one and the same class removes
    /// nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The reference says it in a comment on the reset arm:</strong>
    /// "RPG_RT has a bug where if (<c>new_level == 1 &amp;&amp; new_class_id
    /// == prev_class_id</c>) no skills are removed."
    /// </para>
    /// <para>
    /// <strong>The test needs two class changes</strong>, because the bug is
    /// about the class being the <em>same</em> one — a single change to a
    /// new class has no previous class to be the same as, and a reader that
    /// repaired the bug would pass a one-shot test.
    /// </para>
    /// </remarks>
    public void Test_TheFirstRpgRtSkillBugKeepsTheSkillsAtLevelOne()
    {
        var state = WithHero();
        // **Zwei Faehigkeiten, und die erste wird der neuen Klasse zugeordnet.**
        state.SkillsOf(1).Add(10);
        state.SkillsOf(1).Add(11);
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(1, 1, 1, 1, 1, 0, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.SkillsOf(1).Count, 2,
            "**level one and the same class removes nothing** — RPG_RT's own "
                + "documented bug, and a reader that repaired it would have "
                + "emptied a hero's skill list the original keeps");
    }

    /// <summary>
    /// The level is kept or dropped, and the two are told apart.
    /// </summary>
    /// <remarks>
    /// <strong>This is the rule that replaced a mutation which was not
    /// one.</strong> The guard <c>if (new_param != ClassParamReset)</c> can be
    /// removed without changing anything: in mode 3 the reference leaves the
    /// local copies at the values it secured, so writing them back writes the
    /// same numbers — <strong>the arm removes a write whose result equals the
    /// value already there.</strong> That is the same shape as the
    /// <c>WaitForFrames(0)</c> rule and the reset rule: a survivor is a
    /// question about whether the reader can have the difference, and here the
    /// answer is no.
    ///
    /// <para>
    /// The level is the observable difference instead: the reference writes
    /// <c>SetLevel(level1 ? 1 : actor-&gt;GetLevel())</c>, and a reader that
    /// always dropped the hero to level one would have reset a game's progress
    /// on every class change that did not ask for it.
    /// </para>
    /// </remarks>
    public void Test_TheLevelIsKeptOrDroppedAndTheTwoDiffer()
    {
        // **Ohne das Flag bleibt die Stufe.**
        var state = WithHero();
        AssertEq(state.GetActorLevel(1), 10, "the hero starts at level ten");
        var interpreter = new EventInterpreter(
            state, 1, new[] { Cmd(1, 1, 5, 0, 0, 1, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();
        AssertEq(state.GetActorLevel(1), 10,
            "**parameter 3 of zero keeps level ten** — the reference's own "
                + "`level1 ? 1 : actor->GetLevel()`, and a reader that always "
                + "dropped the hero would have reset a game's progress on "
                + "every class change");

        // **Und mit dem Flag faellt sie auf eins** -- und der Halbmodus
        // aendert daran nichts, denn die Stufe wird vorher gesetzt.
        var state2 = WithHero();
        var i2 = new EventInterpreter(
            state2, 1, new[] { Cmd(1, 1, 5, 1, 1, 0, 0) },
            new PresentationState());
        i2.ExecuteFrame();
        AssertEq(state2.GetActorLevel(1), 1,
            "**parameter 3 of one drops to level one** — and the two values "
                + "must differ or the parameter is not a flag");
    }
}
