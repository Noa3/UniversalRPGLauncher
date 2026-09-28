using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for 10440 Change Skills, 10450 Change Equipment and 10480 Change
/// Condition — from liblcf's <c>eventcommand.h</c> and EasyRPG's
/// <c>Game_Interpreter::CommandChangeSkills</c>,
/// <c>Game_Interpreter::CommandChangeEquipment</c> and
/// <c>Game_Interpreter::CommandChangeCondition</c>.
/// </summary>
/// <remarks>
/// <para>
/// All three are <c>CmdSetup</c> commands with widths of 5, 5 and 4, all three
/// start with the reference's own <c>GetActors(mode, id)</c>, <strong>and all
/// three end in <c>CheckGameOver()</c></strong> — which is a fact about a game
/// over screen that a test on the skill list alone cannot see.
/// </para>
/// </remarks>
public partial class TestRm2kActorCommands : TestBase
{
    private const int Party = EventInterpreter.ActorSelectParty;
    private const int Hero = EventInterpreter.ActorSelectHero;

    private static Rm2kMap.EventCommand Cmd(int pCode, params int[] pParameters)
    {
        return new Rm2kMap.EventCommand
        {
            Code = pCode,
            Text = "",
            Parameters = new List<int>(pParameters),
        };
    }

    private static GameSimulationState PartyWith(int pHeroId, int pHp)
    {
        var state = new GameSimulationState { MapId = 1 };
        state.PartyMemberIds.Add(pHeroId);
        state.CurrentHp[pHeroId] = pHp;
        return state;
    }

    private static (EventInterpreter Interpreter, GameSimulationState State) Start(
        GameSimulationState pState,
        params Rm2kMap.EventCommand[] pCommands)
    {
        var interpreter = new EventInterpreter(
            pState, 1, pCommands, new PresentationState());
        return (interpreter, pState);
    }

    private static (EventInterpreter Interpreter, GameSimulationState State) Run(
        GameSimulationState pState,
        params Rm2kMap.EventCommand[] pCommands)
    {
        var (interpreter, state) = Start(pState, pCommands);
        for (var i = 0; i < pCommands.Length; i++)
        {
            interpreter.ExecuteFrame();
        }

        return (interpreter, state);
    }

    // ---- 10440 Change Skills

    /// <summary>
    /// A zero in the third parameter learns, and a truth unlearns.
    /// </summary>
    /// <remarks>
    /// <strong>The flag is the third parameter and it means remove.</strong> The
    /// reference reads <c>bool remove = com.parameters[2] != 0</c> — so a
    /// reader that read it as "add" would have taught a skill to a hero whose
    /// command meant to take it away.
    /// </remarks>
    public void Test_TheThirdParameterIsRemoveAndNotAdd()
    {
        var lernend = PartyWith(1, 100);
        Run(lernend, Cmd(EventInterpreter.ChangeSkills, Party, 0, 0, 0, 7));
        AssertEq(lernend.SkillsOf(1).Contains(7), true,
            "**a zero in the third parameter learns the skill**");

        var lernend2 = PartyWith(1, 100);
        Run(lernend2, Cmd(EventInterpreter.ChangeSkills, Party, 0, 1, 0, 7));
        AssertEq(lernend2.SkillsOf(1).Contains(7), false,
            "**and a truth unlearns it** — the reference's own variable is "
                + "named remove");

        // Und in die Gegenrichtung, damit es kein Zufall ist.
        var wieder = PartyWith(1, 100);
        Run(wieder, Cmd(EventInterpreter.ChangeSkills, Party, 0, 1, 0, 7));
        Run(wieder, Cmd(EventInterpreter.ChangeSkills, Party, 0, 0, 0, 7));
        AssertEq(wieder.SkillsOf(1).Contains(7), true,
            "**and the two directions are each other's opposite**");
    }

    /// <summary>
    /// The skill id is a value or a variable, in the reference's own order.
    /// </summary>
    public void Test_TheSkillIdIsAValueOrAVariable()
    {
        var state = PartyWith(1, 100);
        // **Das Array wird mit Add gefuellt, und die Referenz 20 zeigt auf
        // Position 19** -- GetVariable liest `Variables[id - 1]`, und ein
        // Array mit Laenge 0 haette den Index geworfen.
        for (var i = 0; i < 20; i++)
        {
            state.Variables.Add(0);
        }

        state.Variables[19] = 42;
        Run(state, Cmd(EventInterpreter.ChangeSkills, Party, 0, 0, 1, 20));

        AssertEq(state.SkillsOf(1).Contains(42), true,
            "**the skill came out of the variable** — the reference reads "
                + "ValueOrVariable(parameters[3], parameters[4]), so the mode "
                + "is the fourth and the value the fifth");
        AssertEq(state.SkillsOf(1).Contains(20), false,
            "and the variable's own number is not the skill id");
    }

    /// <summary>
    /// A second mode picks one hero, and a third the hero a variable names.
    /// </summary>
    public void Test_TheThreeActorModesAreTheReferencesOwn()
    {
        var state = PartyWith(1, 100);
        Run(state, Cmd(EventInterpreter.ChangeSkills, Hero, 3, 0, 0, 5));
        AssertEq(state.SkillsOf(3).Contains(5), true,
            "**mode 1 names one hero by id**, and the hero need not be in the "
                + "party");
        AssertEq(state.SkillsOf(1).Contains(5), false,
            "and no other hero learned it");

        for (var i = 0; i < 30; i++)
        {
            state.Variables.Add(0);
        }

        state.Variables[29] = 4;
        Run(state, Cmd(EventInterpreter.ChangeSkills, 2, 30, 0, 0, 6));
        AssertEq(state.SkillsOf(4).Contains(6), true,
            "**mode 2 names the hero a variable holds** — the reference's own "
                + "third GetActors mode");
    }

    /// <summary>
    /// All three commands end in CheckGameOver, and a party's last hero at
    /// zero reaches the game over screen from a skill command.
    /// </summary>
    /// <remarks>
    /// <strong>This is the fact a test on a skill list cannot see.</strong> The
    /// reference's <c>CommandChangeSkills</c> ends in <c>CheckGameOver()</c>,
    /// so a game whose hero is killed by a condition and then loses a skill
    /// reaches the game over screen from the <em>skill</em> command. A reader
    /// that left the check out would have had the party walk on.
    /// </remarks>
    public void Test_AllThreeCommandsEndInCheckGameOver()
    {
        foreach (var (code, args) in new[]
                 {
                     (EventInterpreter.ChangeSkills, new[] { Party, 0, 0, 0, 7 }),
                     (EventInterpreter.ChangeEquipment, new[] { Party, 0, 1, 4, 0 }),
                     (EventInterpreter.ChangeCondition, new[] { Party, 0, 0, 2 }),
                 })
        {
            var state = PartyWith(1, 0);
            state.ItemEquipmentKinds[4] = GameSimulationState.EquipmentKind.Helmet;
            Run(state, Cmd(code, args));
            AssertEq(state.IsGameOverActive, true,
                "**command " + code + " reached a game over with a party whose "
                    + "last hero is at zero** — the reference's own last line "
                    + "in all three");
        }
    }

    // ---- 10450 Change Equipment

    /// <summary>
    /// The slot comes from the item's own type, not from the parameter.
    /// </summary>
    /// <remarks>
    /// <strong>Mode 0 reads the item and takes <c>slot = item-&gt;type</c>.</strong>
    /// The reference switches on the item's type across weapon, shield, armor,
    /// helmet and accessory — so a reader that took the slot from the command's
    /// parameter in both modes would have put a helmet where a sword goes.
    /// </remarks>
    public void Test_TheSlotComesFromTheItemsOwnType()
    {
        foreach (var (itemId, art, slot) in new[]
                 {
                     (10, GameSimulationState.EquipmentKind.Weapon,
                         GameSimulationState.EquipmentSlot.Weapon),
                     (11, GameSimulationState.EquipmentKind.Shield,
                         GameSimulationState.EquipmentSlot.Shield),
                     (12, GameSimulationState.EquipmentKind.Armor,
                         GameSimulationState.EquipmentSlot.Armor),
                     (13, GameSimulationState.EquipmentKind.Helmet,
                         GameSimulationState.EquipmentSlot.Helmet),
                     (14, GameSimulationState.EquipmentKind.Accessory,
                         GameSimulationState.EquipmentSlot.Accessory),
                 })
        {
            var state = PartyWith(1, 100);
            state.ItemEquipmentKinds[itemId] = art;
            // **Der vierte Parameter ist eine Zahl, die als Slot falsch waere.**
            Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, itemId));

            AssertEq(
                state.GetEquipment(1, slot), itemId,
                "**an item of kind " + art + " went into the " + slot
                    + " slot** — and the parameter said 3, so a reader that "
                    + "took the slot from the parameter would have put it in "
                    + "the wrong one");
        }
    }

    /// <summary>
    /// Mode 1 reads the slot from the parameter, plus one.
    /// </summary>
    public void Test_ModeOneReadsTheSlotFromTheParameterPlusOne()
    {
        var state = PartyWith(1, 100);
        // Parameter 3 = 3, also Slot + 1 = 4 = Helmet.
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 1, 3, 0));

        AssertEq(
            state.GetEquipment(
                1, GameSimulationState.EquipmentSlot.Helmet), 0,
            "**a direct slot writes an empty item and not a real one** — the "
                + "reference's mode 1 sets `item_id = 0` and only picks the "
                + "slot, so the command removes rather than equips");
        AssertEq(state.GetEquipment(1, GameSimulationState.EquipmentSlot.Armor), 0,
            "and nothing landed anywhere else");
    }

    /// <summary>
    /// A sixth slot is not a slot: it empties the whole actor.
    /// </summary>
    /// <remarks>
    /// <strong>The reference checks <c>slot == 6</c> before any of the
    /// five.</strong> A reader that treated the sixth value as a sixth slot
    /// would have had a game's "unequip everything" write a hidden entry and
    /// leave every real slot on.
    /// </remarks>
    public void Test_ASixthSlotIsNotASlotButEverything()
    {
        var state = PartyWith(1, 100);
        state.ItemEquipmentKinds[10] = GameSimulationState.EquipmentKind.Weapon;
        state.ItemEquipmentKinds[13] = GameSimulationState.EquipmentKind.Helmet;
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 10));
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 13));
        AssertEq(state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 10,
            "the actor is wearing a sword and a helmet — it has weapon "
            + state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon)
            + " and helmet "
            + state.GetEquipment(1, GameSimulationState.EquipmentSlot.Helmet)
            + ", and the diagnostics were "
            + string.Join(" | ", state.Diagnostics));

        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 1, 5, 0));
        AssertEq(state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 0,
            "**slot six emptied the weapon slot** — the reference's own "
                + "`slot == 6` branch, and it is checked before any of the "
                + "five");
        AssertEq(state.GetEquipment(1, GameSimulationState.EquipmentSlot.Helmet), 0,
            "**and the helmet with it**");
    }

    /// <summary>
    /// A sixth slot through the item's own kind empties the actor as well.
    /// </summary>
    /// <remarks>
    /// <strong>The sixth value is reachable from both modes, and the test for
    /// it needs the actor to be wearing something.</strong> A mutation that
    /// turned the <c>slot == All</c> branch off survived, because the test
    /// that covers it equips the actor through mode 0 and clears it through
    /// mode 1 — and a reader that emptied nothing in mode 0 would have had
    /// the same visible state, since the items were written under a slot that
    /// is not "everything".
    /// </remarks>
    public void Test_ASixthSlotThroughAnItemEmptiesTheActorToo()
    {
        var state = PartyWith(1, 100);
        state.ItemEquipmentKinds[10] = GameSimulationState.EquipmentKind.Weapon;
        state.ItemEquipmentKinds[13] = GameSimulationState.EquipmentKind.Helmet;
        state.ItemEquipmentKinds[11] = GameSimulationState.EquipmentKind.Shield;
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 10));
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 13));
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 11));
        AssertEq(state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 10,
            "the actor is wearing a sword");

        // **Modus 0 mit einem Item, dessen Art "alles" waere** -- und die
        // Zuordnung ist der Weg, den der Dispatch nimmt.
        state.ItemEquipmentKinds[99] = GameSimulationState.EquipmentKind.None;
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 10));
        AssertEq(state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 10,
            "**a weapon written twice stays**, and the sixth slot is not "
                + "reachable from an item's kind — which is the point: the "
                + "reference's own default arm leaves such an item alone");

        // **Und ueber Modus 1 mit der Zahl sechs, die der Referenz
        // "alles" bedeutet.**
        // **Der Helm ist der Beweis, und er muss VOR dem naechsten Befehl
        // geprueft werden.** Ohne den All-Zweig bleiben alle fuenf Slots
        // stehen; mit ihm ist auch der Helm weg -- und ein Leser, der die
        // sechste Zahl als sechsten Slot schrieb, haette den Helm behalten.
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 1, 5, 0));
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Helmet), 0,
            "**the sixth slot emptied the helmet too** — the reference checks "
                + "`slot == 6` before any of the five, and a reader that "
                + "wrote the sixth value as a sixth slot would have left the "
                + "helmet on");
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Shield), 0,
            "**and the shield**");
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 0,
            "**and the sword**");

        // **Und danach geht wieder ein Item in einen echten Slot.**
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 10));
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 10,
            "**and the next item went into the weapon slot** — a reader that "
                + "wrote the sixth value as a slot would have put the sword "
                + "somewhere the reference never looks");

        // **Und die eigentliche Probe: zwei Helden, und nur der erste wird
        // geleert.** Ein Reader, der das Waehlen des Slots verwechselt, wuerde
        // auch den zweiten leeren.
        var state2 = PartyWith(1, 100);
        state2.PartyMemberIds.Add(2);
        state2.CurrentHp[2] = 100;
        state2.ItemEquipmentKinds[10] = GameSimulationState.EquipmentKind.Weapon;
        Run(state2, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 10));
        AssertEq(
            state2.GetEquipment(2, GameSimulationState.EquipmentSlot.Weapon), 10,
            "**the party mode reached the second hero**");
        Run(state2, Cmd(EventInterpreter.ChangeEquipment, Hero, 1, 1, 5, 0));
        AssertEq(
            state2.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 0,
            "**and the sixth slot emptied the first hero**");
        AssertEq(
            state2.GetEquipment(2, GameSimulationState.EquipmentSlot.Weapon), 10,
            "**and left the second one alone** — the reference's own "
                + "RemoveWholeEquipment runs per actor, and a reader that "
                + "emptied the whole party would have taken a hero who was "
                + "not selected");
    }

    /// <summary>
    /// A third mode is refused, and a fourth and a fifth too.
    /// </summary>
    public void Test_AModeBeyondTheSecondIsRefused()
    {
        foreach (var modus in new[] { 2, 3, 7 })
        {
            var state = PartyWith(1, 100);
            state.ItemEquipmentKinds[10] = GameSimulationState.EquipmentKind.Weapon;
            Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, modus, 0, 10));

            AssertEq(state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 0,
                "**mode " + modus + " equipped nothing** — the reference's "
                    + "switch has 0 and 1 and a default that returns false, "
                    + "and it is the only one of the three commands that "
                    + "refuses instead of repairing");
            var gesagt = false;
            foreach (var d in state.Diagnostics)
            {
                if (d.Contains("is not 0 or 1"))
                {
                    gesagt = true;
                }
            }

            AssertTrue(gesagt,
                "and mode " + modus + " said so — the diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
        }
    }

    /// <summary>
    /// An item that is not equipment is left alone, and a third mode refuses.
    /// </summary>
    public void Test_AnItemThatIsNotEquipmentIsLeftAlone()
    {
        var konsumierbar = PartyWith(1, 100);
        Run(konsumierbar, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 99));
        var gewarnt = false;
        foreach (var d in konsumierbar.Diagnostics)
        {
            if (d.Contains("is not equipment"))
            {
                gewarnt = true;
            }
        }

        AssertTrue(gewarnt,
            "**an item that is not equipment is left alone and says so** — the "
                + "reference's default arm returns true and touches nothing");

        var dritter = PartyWith(1, 100);
        Run(dritter, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 7, 0, 0));
        var abgelehnt = false;
        foreach (var d in dritter.Diagnostics)
        {
            if (d.Contains("is not 0 or 1"))
            {
                abgelehnt = true;
            }
        }

        AssertTrue(abgelehnt,
            "**and a third mode is refused and says so** — it is the only "
                + "one of the three that returns false instead of repairing");
    }

    /// <summary>
    /// A two-weapon actor keeps his shield, and a second one-handed weapon
    /// goes into the second slot.
    /// </summary>
    /// <remarks>
    /// <strong>Two rules, and both are about the same actor.</strong> The
    /// reference skips a shield outright for a two-weapon actor while the
    /// shield is in hand, and puts a one-handed weapon into the second slot
    /// when the first is empty and neither weapon is two-handed.
    /// </remarks>
    public void Test_ATwoWeaponActorHasTwoRulesAndNotOne()
    {
        var state = PartyWith(1, 100);
        state.ActorHasTwoWeapons[1] = true;
        state.ItemEquipmentKinds[11] = GameSimulationState.EquipmentKind.Shield;
        state.ItemEquipmentKinds[20] = GameSimulationState.EquipmentKind.Weapon;
        state.ItemEquipmentKinds[21] = GameSimulationState.EquipmentKind.Weapon;
        state.TwoHandedWeapons.Add(21);

        // **Der Schild geht an einen Zwei-Waffen-Helden nicht.**
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 11));
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Shield), 0,
            "**a shield is skipped for a two-weapon actor** — the reference "
                + "continues before the assignment");

        // **Eine einhändige Waffe in die zweite Hand, wenn die erste leer
        // ist.**
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 20));
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 0,
            "**the first weapon stays empty when the second took the sword** — "
                + "the reference assigns `slot + 1` and continues");
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Shield), 20,
            "**and the sword is in the shield slot**, which is the reference's "
                + "own second weapon slot");

        // **Und eine zweihändige nicht.**
        Run(state, Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 21));
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 21,
            "**a two-handed weapon goes into the first slot** — the reference "
                + "checks both weapons for two-handedness before it fills the "
                + "second");
    }

    // ---- 10480 Change Condition

    /// <summary>
    /// A condition is added and removed, and the flag is the third parameter.
    /// </summary>
    public void Test_AConditionIsAddedAndRemoved()
    {
        var state = PartyWith(1, 100);
        Run(state, Cmd(EventInterpreter.ChangeCondition, Party, 0, 0, 3));
        AssertEq(state.ConditionsOf(1).Contains(3), true,
            "**a zero in the third parameter adds the condition**");

        Run(state, Cmd(EventInterpreter.ChangeCondition, Party, 0, 1, 3));
        AssertEq(state.ConditionsOf(1).Contains(3), false,
            "**and a truth removes it** — the reference's own read is "
                + "`bool remove = parameters[2] != 0`");
    }

    /// <summary>
    /// The removal does not ask where the condition came from.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's own comment records this as an RPG_RT
    /// quirk.</strong> <c>RemoveState(state_id, !IsBattleRunning())</c> — on
    /// the map it removes a state even when the actor has it from equipment.
    /// <strong>A reader that respected the equipment's own state would have
    /// left a hero permanently poisoned by a ring he never took off</strong>,
    /// in a game the reference lets him walk out of.
    /// </remarks>
    public void Test_TheRemovalDoesNotAskWhereTheConditionCameFrom()
    {
        var state = PartyWith(1, 100);
        state.ConditionsOf(1).Add(5);
        Run(state, Cmd(EventInterpreter.ChangeCondition, Party, 0, 1, 5));

        AssertEq(state.ConditionsOf(1).Count, 0,
            "**the removal took the condition and asked nothing** — the "
                + "reference's comment says the map removes states even when "
                + "the actor has them from equipment, and this reader "
                + "reproduces that");
    }

    /// <summary>
    /// An unknown actor mode changes nothing on anybody.
    /// </summary>
    public void Test_AnUnknownActorModeChangesNothingOnNobody()
    {
        var state = PartyWith(1, 100);
        Run(state, Cmd(EventInterpreter.ChangeCondition, 7, 0, 0, 3));

        AssertEq(state.ConditionsOf(1).Count, 0,
            "**an actor mode the reference does not know changes nothing** — "
                + "its own ResolveActors returns null and the caller stops");
        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("unsupported actor mode 7"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "and it says so — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// All three are reached and none of them is refused.
    /// </summary>
    public void Test_AllThreeAreReachedAndNoneIsRefused()
    {
        var state = PartyWith(1, 100);
        state.ItemEquipmentKinds[10] = GameSimulationState.EquipmentKind.Weapon;
        Run(
            state,
            Cmd(EventInterpreter.ChangeSkills, Party, 0, 0, 0, 7),
            Cmd(EventInterpreter.ChangeEquipment, Party, 0, 0, 0, 10),
            Cmd(EventInterpreter.ChangeCondition, Party, 0, 0, 2));

        foreach (var d in state.Diagnostics)
        {
            AssertTrue(!d.Contains("not wired") && !d.Contains("unsupported"),
                "a command was refused, its diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
        }

        AssertEq(state.SkillsOf(1).Contains(7), true, "the skill ran");
        AssertEq(
            state.GetEquipment(1, GameSimulationState.EquipmentSlot.Weapon), 10,
            "the equipment ran");
        AssertEq(state.ConditionsOf(1).Contains(2), true, "the condition ran");
    }
}
