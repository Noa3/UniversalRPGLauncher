using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for the shop and inn family — 10720 Open Shop, 10730 Show Inn and the
/// ten handlers 20710-20713, 20720-20722, 20730-20732 — from liblcf's
/// <c>eventcommand.h</c> and EasyRPG's <c>Game_Interpreter_Map</c> and
/// <c>Game_Interpreter::CommandOptionGeneric</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Ten of the twelve have a width of zero,</strong> and the two that do
/// have parameters are the two openers. A handler is a name for a block, and a
/// reader that expected parameters to read would be reading past the end of a
/// list that is not there.
/// </para>
/// </remarks>
public partial class TestRm2kShopAndInn : TestBase
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
    /// A shop opener that stocks one good.
    /// </summary>
    private static Rm2kMap.EventCommand Shop(int pType, int pGood)
    {
        return Cmd(EventInterpreter.OpenShop, 0, pType, 0, 0, pGood);
    }

    private static (EventInterpreter Interpreter, GameSimulationState State) Start(
        params Rm2kMap.EventCommand[] pCommands)
    {
        var state = new GameSimulationState { MapId = 1 };
        var interpreter = new EventInterpreter(
            state, 1, pCommands, new PresentationState());
        return (interpreter, state);
    }

    private static (EventInterpreter Interpreter, GameSimulationState State) Run(
        params Rm2kMap.EventCommand[] pCommands)
    {
        var (interpreter, state) = Start(pCommands);
        for (var i = 0; i < pCommands.Length; i++)
        {
            interpreter.ExecuteFrame();
        }

        return (interpreter, state);
    }

    /// <summary>
    /// Runs a list with a sub-index chosen before the first command.
    /// </summary>
    private static (EventInterpreter Interpreter, GameSimulationState State) RunWith(
        int pSubIdx,
        params Rm2kMap.EventCommand[] pCommands)
    {
        var (interpreter, state) = Start(pCommands);
        state.SubcommandIndex = pSubIdx;
        for (var i = 0; i < pCommands.Length; i++)
        {
            interpreter.ExecuteFrame();
        }

        return (interpreter, state);
    }

    private static string Report(GameSimulationState pState)
    {
        return " — it read " + pState.ShopItemIds.Count + " goods and the "
            + "diagnostics were "
            + string.Join(" | ", pState.Diagnostics);
    }

    // ---- Die beiden Oeffner

    /// <summary>
    /// The first parameter of 10720 is a mode and the second is a type, and
    /// the goods start at the fourth.
    /// </summary>
    /// <remarks>
    /// <strong>The fourth parameter is the reference's own loop.</strong> It
    /// copies everything from <c>parameters.begin() + 4</c> on into the shop's
    /// list, so a width of 4 means a shop with no goods at all — and a reader
    /// that started at the third would have sold the handler flag as an item
    /// id.
    /// </remarks>
    public void Test_OpenShopTakesAModeATypeAndItsGoodsFromTheFourth()
    {
        var (_, state) = Run(
            Cmd(EventInterpreter.OpenShop, 0, 7, 0, 0, 3, 4, 5));

        AssertEq(state.IsShopOpen, true, "the shop is open");
        AssertEq(state.ShopType, 7,
            "**the second parameter is the shop's type and not a price** — "
                + "the reference names it shop_type and hands it to its own "
                + "Scene_Shop");
        AssertEq(state.ShopItemIds.Count, 3,
            "**everything from the fourth parameter on is one good per "
                + "entry, and a width of 4 means a shop with no goods at "
                + "all**" + Report(state));
        AssertEq(state.ShopItemIds[0], 3,
            "**the first good is the fourth parameter** and not the third, "
                + "so a reader that started one earlier would have stocked "
                + "the shop with the handler flag");
        AssertEq(state.ShopItemIds[2], 5, "and the third is the sixth");
    }

    /// <summary>
    /// Three modes, and a fourth buys and sells nothing.
    /// </summary>
    public void Test_TheShopModeHasThreeCasesAndAFourthDoesNothing()
    {
        var (_, beides) = Run(Cmd(EventInterpreter.OpenShop, 0, 1, 0, 0));
        AssertEq(beides.CanBuy && beides.CanSell, true, "mode 0 does both");

        var (_, nurKaufen) = Run(Cmd(EventInterpreter.OpenShop, 1, 1, 0, 0));
        AssertEq(nurKaufen.CanBuy, true, "mode 1 buys");
        AssertEq(nurKaufen.CanSell, false, "and mode 1 does not sell");

        var (_, nurVerkaufen) = Run(Cmd(EventInterpreter.OpenShop, 2, 1, 0, 0));
        AssertEq(nurVerkaufen.CanBuy, false, "mode 2 does not buy");
        AssertEq(nurVerkaufen.CanSell, true, "and mode 2 sells");

        var (_, kein) = Run(Cmd(EventInterpreter.OpenShop, 3, 1, 0, 0));
        AssertEq(kein.CanBuy || kein.CanSell, false,
            "**a mode the reference's switch does not know buys and sells "
                + "nothing** — its default arm is a bare break");
    }

    /// <summary>
    /// The price of 10730 is the second parameter and the first is the type.
    /// </summary>
    public void Test_TheInnPriceIsTheSecondParameterAndNotTheFirst()
    {
        var (_, state) = Run(Cmd(EventInterpreter.ShowInn, 2, 300, 0));

        AssertEq(state.IsInnOpen, true, "the inn is open");
        AssertEq(state.InnPrice, 300,
            "**the price is the second parameter, and the reference reads it "
                + "in the command's first two lines** — a reader that took "
                + "the type for the price would have charged a party the "
                + "inn's kind for a night's rest");
        AssertEq(state.InnType, 2,
            "and the first parameter is the inn's type");
    }

    /// <summary>
    /// A price of zero skips the prompt, in the reference's own words.
    /// </summary>
    public void Test_APriceOfZeroSkipsThePrompt()
    {
        var (_, state) = Run(Cmd(EventInterpreter.ShowInn, 1, 0, 0));

        AssertEq(state.ShouldShowInnPrice, false,
            "**a free inn does not open a window at all** — the reference "
                + "has its own branch for a zero price and its comment there "
                + "says Skip prompt");
    }

    /// <summary>
    /// A shop and an inn are not both open.
    /// </summary>
    public void Test_AShopAndAnInnAreNotBothOpen()
    {
        var (_, state) = Run(
            Cmd(EventInterpreter.OpenShop, 0, 1, 0, 0),
            Cmd(EventInterpreter.ShowInn, 0, 50, 0));

        AssertEq(state.IsShopOpen, false,
            "**the inn closed the shop** — the reference's SetMode is one "
                + "mode, and two flags would be free to disagree with it");
        AssertEq(state.IsInnOpen, true, "and the inn is open");
    }

    // ---- Die Schliesser

    /// <summary>
    /// 20722 returns true and changes nothing, and 20713 ends the battle.
    /// </summary>
    /// <remarks>
    /// <strong>Two closers that are not alike, and the first one is
    /// nothing.</strong> The reference's <c>CommandEndShop</c> is
    /// <c>return true;</c> with the parameter named away — the shop's own
    /// scene closed when the player left it, and this command only tells the
    /// interpreter to carry on. A reader that cleared the shop state here
    /// would have had a game's shop close the moment its own block ended,
    /// which is a different event.
    /// </remarks>
    public void Test_TheTwoClosersAreNotAlike()
    {
        var (_, shop) = Run(
            Cmd(EventInterpreter.OpenShop, 0, 1, 0, 0),
            Cmd(EventInterpreter.EndShop));
        AssertEq(shop.IsShopOpen, true,
            "**20722 does not close the shop** — the reference's whole "
                + "command is return true and nothing else" + Report(shop));

        var (_, battle) = Run(Cmd(EventInterpreter.EndBattle));
        AssertEq(battle.IsBattleActive, false,
            "**20713 ends the battle** — the reference clears the battle "
                + "state, and a reader that made it a plain option would have "
                + "left a game's battle running with no way out of it");
    }

    // ---- Die Handler

    /// <summary>
    /// A handler runs its block only when it is the option that was chosen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the whole of the reference's
    /// <c>CommandOptionGeneric</c> and it is one comparison.</strong> The
    /// method reads the sub-index, compares it with the option the handler is
    /// for, and then either writes the sentinel or skips to the next handler.
    /// <strong>A reader that always skipped would run a shop's "you bought
    /// nothing" branch beside its "you bought something" branch</strong>, and
    /// a reader that always ran would run both of them.
    /// </para>
    /// </remarks>
    public void Test_AHandlerRunsOnlyWhenItIsTheChosenOption()
    {
        var (_, state) = RunWith(
            0,
            Cmd(EventInterpreter.Transaction),
            // **The chosen handler's own block.**
            Shop(1, 44),
            Cmd(EventInterpreter.EndShop),
            // **And a second block the skipped handler would have run.**
            Cmd(EventInterpreter.NoTransaction),
            Shop(2, 55),
            Cmd(EventInterpreter.EndShop));

        AssertEq(state.ShopItemIds.Count, 1,
            "**only the chosen handler's block ran**" + Report(state));
        AssertEq(state.ShopItemIds[0], 44,
            "**and it is the one the chosen block named**");
    }

    /// <summary>
    /// The sentinel is written when the handler ran.
    /// </summary>
    public void Test_TheSentinelStopsTheSecondHandler()
    {
        var (_, state) = RunWith(
            0,
            Cmd(EventInterpreter.Transaction),
            Shop(1, 44),
            Cmd(EventInterpreter.EndShop),
            Cmd(EventInterpreter.NoTransaction),
            Shop(2, 55),
            Cmd(EventInterpreter.EndShop));

        AssertEq(state.SubcommandIndex,
            GameSimulationState.SubcommandSentinel,
            "**the chosen sub-index was cleared when its handler ran** — "
                + "without the sentinel the shop's second handler would have "
                + "run as well" + Report(state));
    }

    /// <summary>
    /// Nothing chosen means nothing runs.
    /// </summary>
    public void Test_NothingChosenMeansNothingRuns()
    {
        var (interpreter, state) = Start(
            Cmd(EventInterpreter.Transaction),
            Shop(1, 44),
            Cmd(EventInterpreter.EndShop));
        state.SubcommandIndex = GameSimulationState.SubcommandSentinel;
        for (var i = 0; i < 3; i++)
        {
            interpreter.ExecuteFrame();
        }

        AssertEq(state.ShopItemIds.Count, 0,
            "**with no option chosen, the handler skipped its whole block**"
                + Report(state));
    }

    /// <summary>
    /// Each handler has its own list, and the lists are different lengths.
    /// </summary>
    public void Test_EachHandlerHasItsOwnList()
    {
        // **A no-transaction ends at the end shop alone**, where a
        // transaction ends at the no-transaction or the end shop.
        var (_, state) = RunWith(
            1,
            Cmd(EventInterpreter.NoTransaction),
            Shop(1, 66),
            Cmd(EventInterpreter.EndShop),
            // **This block is the one a transaction's list would stop at.**
            Shop(2, 77),
            Cmd(EventInterpreter.EndShop));

        AssertEq(state.ShopItemIds.Count, 2,
            "**the no-transaction handler runs its block and stops at the "
                + "end shop** — the reference's list for it is one command "
                + "long" + Report(state));
    }

    /// <summary>
    /// The three battle outcomes have three lists, and the victory's is the
    /// longest.
    /// </summary>
    public void Test_TheThreeBattleOutcomesHaveThreeLists()
    {
        var (_, state) = RunWith(
            4,
            Cmd(EventInterpreter.VictoryHandler),
            Shop(1, 88),
            Cmd(EventInterpreter.DefeatHandler));

        AssertEq(state.ShopItemIds.Count, 1,
            "**the victory handler stopped at the defeat handler** — the "
                + "reference's list for a victory is escape, defeat and end "
                + "battle, and one for a defeat is end battle alone"
                + Report(state));
    }

    /// <summary>
    /// An inn's two handlers have the inn's two lists.
    /// </summary>
    public void Test_AnInnsTwoHandlersHaveTheInnsTwoLists()
    {
        var (_, state) = RunWith(
            2,
            Cmd(EventInterpreter.Stay),
            Shop(1, 99),
            Cmd(EventInterpreter.NoStay));

        AssertEq(state.ShopItemIds.Count, 1,
            "**the stay handler stopped at the no-stay** — the reference's "
                + "list for a stay is no-stay and end-inn, and a reader that "
                + "skipped to the end would have run the inn's own opening "
                + "command inside its own block" + Report(state));
    }

    /// <summary>
    /// A stay does not heal the party, and that is the reference's split.
    /// </summary>
    public void Test_AStayDoesNotHealTheParty()
    {
        var (interpreter, state) = Start(
            Cmd(EventInterpreter.ShowInn, 0, 100, 0),
            Cmd(EventInterpreter.Stay),
            Cmd(EventInterpreter.EndInn));
        state.CurrentHp[1] = 5;
        state.SubcommandIndex = 2;
        for (var i = 0; i < 3; i++)
        {
            interpreter.ExecuteFrame();
        }

        AssertEq(state.GetActorCurrentHp(1), 5,
            "**a stay leaves the party's hit points alone** — the "
                + "reference's CommandStay is the handler and the healing is "
                + "the inn's own business, the same split it makes for a "
                + "shop's trading");
    }

    /// <summary>
    /// A block with no closer says so and finishes the page.
    /// </summary>
    public void Test_ABlockWithNoCloserSaysSoAndFinishesThePage()
    {
        // **Zwei Befehle, und keiner davon ist ein Schliesser** -- dann laeuft
        // die Suche bis zur Listengrenze, und das ist der Fall, den der
        // Sprung melden muss.
        var (interpreter, state) = Start(
            Cmd(EventInterpreter.Transaction),
            Cmd(EventInterpreter.OpenShop, 0, 1, 0, 0, 0, 31));
        // **Nicht gewaehlt** -- der Handler ueberspringt, und die Suche nach
        // seinem Schliesser laeuft bis zur Listengrenze.
        state.SubcommandIndex = GameSimulationState.SubcommandSentinel;
        interpreter.ExecuteFrame();

        // **Das OpenShop im Block wurde uebersprungen** -- das ist die
        // Aussage. Der Sprung endet auf dem letzten Befehl, und der laeuft
        // danach als gewöhnlicher, so wie der Choice-Sprung es auch tut.
        var zweiten = interpreter.ExecuteFrame();
        AssertEq(zweiten, false,
            "**the block's own command never ran** — a block with no closer "
                + "runs to the end of the list, and the diagnostics were "
                + string.Join(" | ", state.Diagnostics));
        AssertEq(state.ShopItemIds.Count, 0,
            "**and so nothing was stocked**");
        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("has no closer in the list"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**and it says so** — the diagnostics were "
                + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// A handler that is not chosen does not run its own command.
    /// </summary>
    /// <remarks>
    /// <strong>This is the test that proved the skip exists.</strong> A
    /// mutation that turned the skip loop off left every state field correct
    /// and ran the skipped block anyway, so the only assertion that sees it
    /// is one about <em>what did not run</em>.
    /// </remarks>
    public void Test_AHandlerThatIsNotChosenDoesNotRunItsOwnCommand()
    {
        var (interpreter, state) = Start(
            Cmd(EventInterpreter.Transaction),
            // **Der Befehl, den der Handler ueberspringen muss.**
            Shop(1, 55),
            Cmd(EventInterpreter.EndShop));
        state.SubcommandIndex = GameSimulationState.SubcommandSentinel;
        interpreter.ExecuteFrame();
        var naechster = interpreter.ExecuteFrame();

        AssertEq(naechster, false,
            "**the skipped command never ran** — the skip loop is the only "
                + "thing that stops it, and the diagnostics were "
                + string.Join(" | ", state.Diagnostics));
        AssertEq(state.ShopItemIds.Count, 0,
            "**and so the shop was never stocked**");
    }

    /// <summary>
    /// A skipped handler stops at its own list and not at the longest one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A closing list is only ever read in the skipping arm,</strong>
    /// because a chosen handler runs its block and goes on. Two mutations
    /// lengthened the no-transaction and the defeat lists to their victory
    /// and transaction lengths, and nothing failed — because every test that
    /// named those handlers had <em>chosen</em> them, and a chosen handler
    /// never looks at its list.
    /// </para>
    /// <para>
    /// So these two tests are unchosen on purpose, and each block holds both
    /// a shorter and a longer closer: a handler that skipped to the longer
    /// one would run fewer commands than the reference runs.
    /// </para>
    /// </remarks>
    public void Test_ASkippedNoTransactionHandlerStopsAtTheEndShopAlone()
    {
        // **Der NoTransaction-Handler steht VOR dem EndShop, und genau das
        // unterscheidet die Listen.** Die des No-Transaction-Handlers ist
        // ein Eintrag lang und endet am EndShop; die des Transaction-
        // Handlers hat zwei und nennt auch das NoTransaction -- und das
        // steht hier an Position 1, also wuerde ein Transaktions-Handler
        // hier einen Befehl frueher stoppen als der eigene.
        // **Position eins traegt ein echtes NoTransaction**, denn genau das
        // ist der Code, den die laengere Liste nennt -- ein Transaction
        // waere ein anderer Code und wuerde die Liste nicht ausloesen.
        var (interpreter, state) = Start(
            Cmd(EventInterpreter.NoTransaction),
            Cmd(EventInterpreter.NoTransaction),
            Shop(1, 66),
            Cmd(EventInterpreter.EndShop),
            Shop(2, 77),
            Cmd(EventInterpreter.EndShop));
        state.SubcommandIndex = GameSimulationState.SubcommandSentinel;

        // **Frame eins springt bis auf den Schliesser, Frame zwei fuehrt
        // ihn aus, und Frame drei laeuft der Rest der Liste.** Die Aussage
        // ist, welcher Shop uebersprungen wurde: mit der laengeren Liste
        // waere der Sprung am Transaction gelandet und Shop(1, 66) waere
        // nie gelaufen.
        for (var i = 0; i < 3; i++)
        {
            interpreter.ExecuteFrame();
        }

        AssertEq(state.ShopItemIds.Count, 1,
            "**a skipped no-transaction handler ran one shop, not two** — "
                + "its list is the end shop alone, so it jumped over the "
                + "no-transaction and the shop behind it" + Report(state));
        AssertEq(state.ShopType, 2,
            "**and it is the second one** — the first never opened, which is "
                + "what a one-entry list means");
    }

    /// <summary>
    /// A skipped defeat handler stops at the end battle and not at an escape.
    /// </summary>
    public void Test_ASkippedDefeatHandlerStopsAtTheEndBattleAlone()
    {
        // **Der EscapeHandler steht VOR dem EndBattle** -- die Defeat-
        // Liste endet nur am EndBattle, die laengere Victory-Liste wuerde
        // am Escape hier einen Befehl frueher stoppen.
        var (interpreter, state) = Start(
            Cmd(EventInterpreter.DefeatHandler),
            Cmd(EventInterpreter.EscapeHandler),
            Shop(1, 101),
            Cmd(EventInterpreter.EndBattle),
            Shop(2, 102),
            Cmd(EventInterpreter.EndBattle));
        state.SubcommandIndex = GameSimulationState.SubcommandSentinel;

        for (var i = 0; i < 3; i++)
        {
            interpreter.ExecuteFrame();
        }

        AssertEq(state.ShopItemIds.Count, 1,
            "**a skipped defeat handler ran one shop, not two** — its list "
                + "is the end battle alone, and the escape before it "
                + "belongs to the victory's list and not to this one"
                + Report(state));
        AssertEq(state.ShopType, 2,
            "**and it is the second one** — the first never opened");
    }

    /// <summary>
    /// A skipped transaction handler stops at the next handler, and a skipped
    /// victory handler stops at the escape.
    /// </summary>
    /// <remarks>
    /// <strong>The two-arm lists, read in the arm that uses them.</strong> A
    /// transaction's list is the no-transaction and the end shop, and a
    /// victory's is the escape, the defeat and the end battle — so a
    /// skipped transaction stops before its own second handler runs, and a
    /// skipped victory stops before the defeat that follows it.
    /// </remarks>
    public void Test_TheTwoArmListsAreReadInTheSkippingArm()
    {
        var (t1, transaktion) = Start(
            Cmd(EventInterpreter.Transaction),
            Shop(1, 10),
            Cmd(EventInterpreter.NoTransaction),
            Shop(2, 20),
            Cmd(EventInterpreter.EndShop));
        transaktion.SubcommandIndex = GameSimulationState.SubcommandSentinel;
        t1.ExecuteFrame();
        t1.ExecuteFrame();

        AssertEq(transaktion.ShopItemIds.Count, 1,
            "**a skipped transaction stops at the no-transaction** — its "
                + "list has two commands and the no-transaction is the first"
                + Report(transaktion));

        var (t2, victory) = Start(
            Cmd(EventInterpreter.VictoryHandler),
            Shop(1, 30),
            Cmd(EventInterpreter.EscapeHandler),
            Shop(2, 40),
            Cmd(EventInterpreter.EndBattle));
        victory.SubcommandIndex = GameSimulationState.SubcommandSentinel;
        t2.ExecuteFrame();
        t2.ExecuteFrame();

        AssertEq(victory.ShopItemIds.Count, 1,
            "**a skipped victory stops at the escape handler** — its list "
                + "has three commands and the escape is the first"
                + Report(victory));
    }

    /// <summary>
    /// Every closing command reaches the dispatch, and each says its own name.
    /// </summary>
    /// <remarks>
    /// <strong>This is the test five mutations could not kill.</strong> The
    /// "all twelve" test counts the handlers' own diagnostic, and a closer
    /// that ran no code at all produces no diagnostic — so a dispatch that
    /// pointed 20732 at nothing, or a closer that had lost its state write,
    /// left the count untouched. <strong>A command that is supposed to do
    /// nothing still has to be the one that runs.</strong>
    /// </remarks>
    public void Test_EveryClosingCommandRunsAndSaysItsOwnName()
    {
        foreach (var (code, name) in new[]
                 {
                     (EventInterpreter.EndShop, "End shop"),
                     (EventInterpreter.EndInn, "End inn"),
                     (EventInterpreter.EndBattle, "End battle"),
                 })
        {
            var (_, state) = Run(Cmd(code));
            var gesagt = false;
            foreach (var d in state.Diagnostics)
            {
                if (d.Contains(name))
                {
                    gesagt = true;
                }
            }

            AssertTrue(gesagt,
                "**command " + code + " reached the dispatch and said "
                    + name + "** — the diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
        }
    }

    /// <summary>
    /// 20722 changes nothing, and 20732 and 20713 do.
    /// </summary>
    /// <remarks>
    /// <strong>Two closers of the same family and opposite effects.</strong>
    /// The reference's <c>CommandEndShop</c> is a bare <c>return true;</c> with
    /// its parameter named away, while its <c>CommandEndInn</c> and
    /// <c>CommandEndBattle</c> clear state. **A reader that gave all three the
    /// same effect would have closed a game's shop the moment its own block
    /// ended**, which is a different event from the player leaving.
    /// </remarks>
    public void Test_TheThreeClosersHaveThreeDifferentEffects()
    {
        var (_, nachShop) = Run(
            Cmd(EventInterpreter.OpenShop, 0, 1, 0, 0),
            Cmd(EventInterpreter.EndShop));
        AssertEq(nachShop.IsShopOpen, true,
            "**20722 leaves the shop open** — it is a bare return true"
                + Report(nachShop));

        var (_, nachInn) = Run(
            Cmd(EventInterpreter.ShowInn, 0, 50, 0),
            Cmd(EventInterpreter.EndInn));
        AssertEq(nachInn.IsInnOpen, false,
            "**and 20732 closes the inn** — the three closers are not alike");

        // **Ein laufender Kampf ist die Voraussetzung** -- die Mutation
        // `IsBattleActive = IsBattleActive` sieht auf einem Zustand, der nie
        // lief, genauso aus wie das loeschende Original.
        var state2 = new GameSimulationState
        {
            MapId = 1,
            IsBattleActive = true,
            ActiveBattleOption = GameSimulationState.BattleOutcome.Victory,
        };
        var k2 = new EventInterpreter(
            state2, 1, new[] { Cmd(EventInterpreter.EndBattle) },
            new PresentationState());
        k2.ExecuteFrame();
        AssertEq(state2.IsBattleActive, false,
            "**and 20713 ends a battle that was running** — the reference "
                + "clears the battle state and returns true");
        AssertEq(state2.ActiveBattleOption,
            GameSimulationState.BattleOutcome.None,
            "**and it clears the outcome the handler named** — a reader that "
                + "left it would have had the next battle think it was still "
                + "answering the last one's victory");
    }

    /// <summary>
    /// A no-transaction handler stops at the end shop and not at a
    /// no-transaction.
    /// </summary>
    public void Test_ANoTransactionHandlerStopsAtTheEndShopAlone()
    {
        // **Zwei Bloecke, und der erste endet am EndShop.** Ein Handler mit
        // der laengeren Transaktions-Liste wuerde am NoTransaction des
        // zweiten Blocks stoppen und den ersten Block nicht ausfuehren.
        var (_, state) = RunWith(
            1,
            Cmd(EventInterpreter.NoTransaction),
            Shop(1, 66),
            Cmd(EventInterpreter.EndShop));

        AssertEq(state.ShopItemIds.Count, 1,
            "**the no-transaction handler ran its own block** — its list is "
                + "the end shop alone, where a transaction's also lists the "
                + "no-transaction" + Report(state));
    }

    /// <summary>
    /// A defeat handler stops at the end battle and not at an escape.
    /// </summary>
    public void Test_ADefeatHandlerStopsAtTheEndBattleAlone()
    {
        // **Der Escape steht VOR dem EndBattle** -- mit der laengeren
        // Victory-Liste wuerde der Handler hier stoppen und seinen Block
        // nicht ausfuehren.
        var (_, state) = RunWith(
            6,
            Cmd(EventInterpreter.DefeatHandler),
            Shop(1, 101),
            Cmd(EventInterpreter.EscapeHandler),
            Shop(2, 102),
            Cmd(EventInterpreter.EndBattle));

        AssertEq(state.ShopItemIds.Count, 1,
            "**the defeat handler ran past the escape handler to the end "
                + "battle** — the reference's list for a defeat is end "
                + "battle alone, and the escape in the middle belongs to the "
                + "victory's list and not to this one" + Report(state));
        AssertEq(state.ShopItemIds[0], 101,
            "**and the good it stocked is the one from its own block**");
    }

    /// <summary>
    /// All twelve are reached and none of them is refused.
    /// </summary>
    public void Test_AllTwelveAreReachedAndNoneIsRefused()
    {
        var state = new GameSimulationState
        {
            MapId = 1,
            IsBattleActive = true,
        };
        // **Sub-Index an jedem Handler, so genau die sieben Handler ihren
        // Block ausfuehren** -- ohne das waere kein einziger davon ein
        // Beweis fuer seine Verdrahtung.
        var plan = new System.Collections.Generic.List<
            (int Code, int SubIdx)>
        {
            (EventInterpreter.OpenShop, 0),
            (EventInterpreter.EndShop, 0),
            (EventInterpreter.ShowInn, 0),
            (EventInterpreter.EndInn, 0),
            (EventInterpreter.VictoryHandler, 4),
            (EventInterpreter.EndBattle, 0),
            (EventInterpreter.EscapeHandler, 5),
            (EventInterpreter.EndBattle, 0),
            (EventInterpreter.DefeatHandler, 6),
            (EventInterpreter.EndBattle, 0),
            (EventInterpreter.Transaction, 0),
            (EventInterpreter.EndShop, 0),
            (EventInterpreter.NoTransaction, 1),
            (EventInterpreter.EndShop, 0),
            (EventInterpreter.Stay, 2),
            (EventInterpreter.EndInn, 0),
            (EventInterpreter.NoStay, 3),
            (EventInterpreter.EndInn, 0),
        };
        var befehle = new List<Rm2kMap.EventCommand>();
        foreach (var (code, _) in plan)
        {
            befehle.Add(code == EventInterpreter.OpenShop
                ? Shop(1, 10)
                : code == EventInterpreter.ShowInn
                    ? Cmd(code, 0, 100, 0)
                    : Cmd(code));
        }

        var interpreter = new EventInterpreter(
            state, 1, befehle, new PresentationState());
        for (var i = 0; i < plan.Count; i++)
        {
            state.SubcommandIndex = plan[i].SubIdx;
            interpreter.ExecuteFrame();
        }

        foreach (var d in state.Diagnostics)
        {
            AssertTrue(!d.Contains("not wired") && !d.Contains("unsupported"),
                "a command was refused, its diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
        }

        var gelaufen = 0;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("is the chosen option"))
            {
                gelaufen += 1;
            }
        }

        AssertEq(gelaufen, 7,
            "**all seven handlers were reached with their own sub-index** — "
                + "it ran " + gelaufen + " of them, and the diagnostics were "
                + string.Join(" | ", state.Diagnostics));
    }
}
