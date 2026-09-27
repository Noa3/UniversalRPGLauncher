using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// The commands that move the interpreter's index, and nothing else.
/// </summary>
/// <remarks>
/// These are the commands whose whole effect is where the interpreter goes. Each
/// one was read out of <c>Game_Interpreter</c> in the engine, and each returns
/// whether the interpreter may carry on; one of them returns false, and that
/// return value is what holds the interpreter in place.
/// </remarks>
public static class MzControlFlow
{
    /// <summary>What running a control flow command did.</summary>
    public enum MzControlOutcome
    {
        /// <summary>It is not one of these, and belongs to
        /// <see cref="MzCommands"/>.</summary>
        NotControlFlow,

        /// <summary>It ran, and the index is where the command left it.</summary>
        Ran,

        /// <summary>
        /// The interpreter has stopped and has said why. The index stays where
        /// the command left it, which is what the engine's
        /// <c>if (!this[methodName](...)) return false;</c> does.
        /// </summary>
        Stopped,
    }

    /// <summary>
    /// Runs a command whose only effect is on the index. The three answers are
    /// three things: a first draft returned one bool for two of them, and a
    /// caller reading the first as the second stopped the index moving after the
    /// first command it met.
    /// </summary>
    public static MzControlOutcome TryExecute(
        MzInterpreter pInterpreter, MzCommandEntry pCommand,
        List<MzAction> pActions, MzBranchFacts pFacts)
    {
        switch (pCommand.Code)
        {
            case MzCommandTable.ShowText:
            {
                var branch = MzBranch.FromParameters(pCommand.Parameters);
                var result = MzBranchEvaluator.Evaluate(branch, pFacts);
                pActions.Add(MzAction.Branch(pCommand, result));
                pInterpreter.SetBranch(
                    pCommand.Indent,
                    result.Outcome == MzBranchOutcome.True ? true
                    : result.Outcome == MzBranchOutcome.False ? false
                    : null);

                if (result.Outcome == MzBranchOutcome.Unknown
                    || result.Outcome == MzBranchOutcome.ScriptNotRun)
                {
                    // The engine would have decided this one by running the
                    // game's own code or by asking for something it has. This
                    // reader cannot, so it stops here and says which, rather
                    // than stepping into an arm of a branch it did not decide.
                    pInterpreter.Stop(
                        MzStep.Refused,
                        result.Outcome == MzBranchOutcome.ScriptNotRun
                            ? $"a branch on the author's own script: {result.Missing}"
                            : result.Missing);
                    return MzControlOutcome.Stopped;
                }
                if (result.Outcome == MzBranchOutcome.False
                    && !pInterpreter.SkipBranch())
                {
                    return MzControlOutcome.Stopped;
                }
                return MzControlOutcome.Ran;
            }

            // The engine's else: it skips the branch when the indent does not
            // already hold false, so a branch decided true lets the else be
            // stepped over and a branch not decided at all is treated as false.
            case MzCommandTable.Else:
                if (pInterpreter.BranchAt(pCommand.Indent) != false
                    && !pInterpreter.SkipBranch())
                {
                    return MzControlOutcome.Stopped;
                }
                return MzControlOutcome.Ran;

            // Loop does nothing at all. It is a marker for break and repeat to
            // find, and the engine's method is `return true`.
            case MzCommandTable.Loop:
                return MzControlOutcome.Ran;

            // Repeat above walks back to the first command at its own indent.
            case MzCommandTable.RepeatAbove:
                return pInterpreter.JumpToRepeat()
                    ? MzControlOutcome.Ran
                    : MzControlOutcome.Stopped;

            // Break loop steps over to the repeat that closes this loop.
            case MzCommandTable.BreakLoop:
                return pInterpreter.JumpToEndOfLoop()
                    ? MzControlOutcome.Ran
                    : MzControlOutcome.Stopped;

            // Exit event processing ends the list.
            case MzCommandTable.ExitEventProcessing:
                pInterpreter.Index = pInterpreter.Commands.Count;
                return MzControlOutcome.Ran;

            // A label is a name in the list and nothing else.
            case MzCommandTable.Label:
                return MzControlOutcome.Ran;

            // Jump to label: a label that is not there leaves the index where it
            // was, which is what the engine does and is not a reason to stop.
            case MzCommandTable.JumpToLabel:
                pInterpreter.JumpToLabel(
                    pCommand.Parameters.Count > 0 ? pCommand.Parameters[0] : "");
                return MzControlOutcome.Ran;

            default:
                return MzControlOutcome.NotControlFlow;
        }
    }
}
