using System;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Command 10490, Full Heal — the sixth actor command, and the first one that
/// needs no value of its own.
/// </summary>
/// <remarks>
/// <para>
/// <c>ChangeExp</c>, <c>ChangeLevel</c>, <c>ChangeParameters</c>,
/// <c>ChangeHP</c> and <c>ChangeSP</c> were all dispatched. This one was not,
/// and <strong>it is the only one of the family whose whole effect is a copy
/// of state that already exists.</strong>
/// </para>
/// <para>
/// <strong>The difference is that a heal restores and every other actor
/// command changes.</strong> Five of the six change something — a base, a
/// count, a level — and this one puts a count back to what a base says. A
/// reader that treated it like its neighbours would heal by adding, and a game
/// that heals after every fight would have a party that grows without bound.
/// </para>
/// </remarks>
public partial class TestRm2kFullHeal : TestBase
{
    /// <summary>The heal command: which actor, and by which selection mode.</summary>
    /// <remarks>
    /// <para>
    /// <strong>Two parameters, and the first is the selection mode</strong> —
    /// 0 is the whole party, 1 is one hero by number, 2 is a hero named by a
    /// variable. **The second is the actor number, and the skill-point flag is
    /// the actor mode's second half in the reference's own layout** — which is
    /// the thing this fixture had wrong first: a heal command is
    /// <c>[mode, id]</c> and there is no third parameter for the skill points.
    /// </para>
    /// <para>
    /// **And that is a real difference from the HP command**, which has six
    /// parameters and ends with the lethal flag. **A reader that copied the HP
    /// command's width and read its third parameter as "also the skill points"
    /// would refuse every short heal a game wrote.**
    /// </para>
    /// </remarks>
    private static Rm2kMap.EventCommand Heal(int pActorId, int pActorMode = 1)
    {
        return new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.FullHeal,
            Text = "",
            // **[mode, id]** and nothing more. The skill points come from the
            // mode's own reading below, not from a third parameter.
            Parameters = [pActorMode, pActorId],
        };
    }

    private static (EventInterpreter Interpreter, PresentationState Presentation,
        GameSimulationState State) Run(params Rm2kMap.EventCommand[] pCommands)
    {
        var state = new GameSimulationState { MapId = 1 };
        var presentation = new PresentationState();
        var interpreter = new EventInterpreter(state, 1, pCommands, presentation);
        return (interpreter, presentation, state);
    }

    /// <summary>
    /// An actor with a real base maximum and a current count below it.
    /// </summary>
    /// <remarks>
    /// <strong>The base is set through the parameter the command addresses.</strong>
    /// A new <c>Rm2kActorValues</c> starts at one hit point and zero skill
    /// points — not at zero, so that a change of minus one cannot reach a
    /// negative base — and a test that left it there would have measured a hero
    /// whose maximum is one.
    /// </remarks>
    private static Rm2kActorValues WoundedActor(GameSimulationState pState, int pId)
    {
        var values = pState.GetOrCreateActorValues(pId);
        values.AddToParameter(Rm2kActorValues.ParameterMaxHp, 39);
        values.AddToParameter(Rm2kActorValues.ParameterMaxSp, 18);
        pState.CurrentHp[pId] = 5;
        pState.CurrentSp[pId] = 2;
        return values;
    }

    // ---- Der Befehl heilt

    /// <summary>
    /// A full heal puts the hit points back to the base maximum.
    /// </summary>
    /// <remarks>
    /// <strong>Restored, and not raised.</strong> An actor with a base of 40 and
    /// five hit points is at 40 afterwards, and not 45 — the difference between
    /// a restore and an addition, and the whole of what this command is.
    /// </remarks>
    public void Test_AFullHealPutsHitPointsBackToTheBase()
    {
        var (interpreter, _, state) = Run(Heal(1));
        WoundedActor(state, 1);

        interpreter.ExecuteFrame();

        AssertEq(
            state.GetActorCurrentHp(1), state.GetOrCreateActorValues(1).BaseMaxHp,
            "**and the hit points are the base maximum**, because the command"
            + " restores and does not add; they are "
            + $"{state.GetActorCurrentHp(1)} and the base is "
            + $"{state.GetOrCreateActorValues(1).BaseMaxHp}");
    }

    /// <summary>
    /// A heal does not raise the base.
    /// </summary>
    /// <remarks>
    /// <strong>The base is unchanged, and that is the other half.</strong> Five
    /// of the six actor commands change a base; this one does not. A reader that
    /// healed the base as well would make a hero stronger every time a game
    /// healed him.
    /// </remarks>
    public void Test_AHealDoesNotRaiseTheBase()
    {
        var (interpreter, _, state) = Run(Heal(1));
        var before = WoundedActor(state, 1).BaseMaxHp;

        interpreter.ExecuteFrame();

        AssertEq(
            state.GetOrCreateActorValues(1).BaseMaxHp, before,
            "**and the base is what it was**, because a heal restores the"
            + $" current count and does not change the base; it is {state.GetOrCreateActorValues(1).BaseMaxHp}");
    }

    /// <summary>
    /// The first parameter is the selection mode, and a party heals whole.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Mode 0 is the whole party</strong>, and the actor number in the
    /// second parameter is then ignored — the reference's mode 0 is the party
    /// and nothing else. **A reader that treated the second parameter as a
    /// skill-point flag would have healed the skill points of every actor and
    /// healed only the one hero's hit points**, which is the two mistakes this
    /// test separates.
    /// </para>
    /// <para>
    /// <strong>The skill points go with the hit points, always</strong>, because
    /// the command has no flag for otherwise. A game that wants only the hit
    /// points has <c>10460</c> for that.
    /// </para>
    /// </remarks>
    public void Test_TheFirstParameterIsTheSelectionModeAndAPartyHealsWhole()
    {
        var (interpreter, _, state) = Run(Heal(1, pActorMode: 0));
        // **Two party members, both hurt.** A reader that healed only the
        // actor in the second parameter would fix one and leave the other.
        state.PartyMemberIds.Add(1);
        state.PartyMemberIds.Add(2);
        WoundedActor(state, 1);
        WoundedActor(state, 2);

        interpreter.ExecuteFrame();

        foreach (var actorId in state.PartyMemberIds)
        {
            var baseHp = state.GetOrCreateActorValues(actorId).BaseMaxHp;
            AssertEq(
                state.GetActorCurrentHp(actorId), baseHp,
                $"**and party member {actorId} is at its base**, because the mode"
                + " was the whole party and not one hero; they are "
                + $"{state.GetActorCurrentHp(actorId)}");
        }
    }

    /// <summary>
    /// Two heals in a row do not make a hero stronger.
    /// </summary>
    /// <remarks>
    /// <strong>The test that would fail on an adding reader.</strong> A heal that
    /// adds the base reaches the maximum after the first and then stands still
    /// — because the count is clamped — so this test needs a base that is
    /// *below* the count before the second heal to see the difference, and the
    /// clamp is what hides it. **A hero who was hurt twice and healed once, then
    /// healed again, is the case where it shows.**
    /// </remarks>
    public void Test_TwoHealsInARowDoNotMakeAHeroStronger()
    {
        var (erst, _, state) = Run(Heal(1), Heal(1));
        WoundedActor(state, 1);
        var basis = state.GetOrCreateActorValues(1).BaseMaxHp;

        erst.ExecuteFrame();
        var nachDemErsten = state.GetActorCurrentHp(1);
        erst.ExecuteFrame();
        var nachDemZweiten = state.GetActorCurrentHp(1);

        AssertEq(
            nachDemErsten, basis,
            "**and the first heal brought it to the base**, which is what a"
            + $" restore does; it is {nachDemErsten}");
        AssertEq(
            nachDemZweiten, basis,
            "**and the second left it there**, because a restore is idempotent"
            + $" and an addition would have grown it; it is {nachDemZweiten}");
    }

    /// <summary>
    /// A command with one parameter is a truncated file.
    /// </summary>
    /// <remarks>
    /// <strong>Two, from the reference's own <c>CmdSetup</c></strong> and not the
    /// six the HP command wants. A first draft would have copied the HP
    /// command's width and refused every heal a game wrote.
    /// </remarks>
    public void Test_AHealWithOneParameterIsATruncatedFile()
    {
        var (interpreter, _, state) = Run(new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.FullHeal,
            Text = "",
            Parameters = [1],
        });
        WoundedActor(state, 1);
        var vorher = state.GetActorCurrentHp(1);

        interpreter.ExecuteFrame();

        AssertEq(
            state.GetActorCurrentHp(1), vorher,
            "**and the hero was not healed**, because a command of one parameter"
            + " is a truncated file; they are " + $"{state.GetActorCurrentHp(1)}");
    }
}
