using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Web;

/// <summary>
/// One command of an event, as the game stored it: a number, a set of
/// parameters and how deep it sits inside any branch around it.
/// </summary>
/// <param name="Code">
/// The command's own number. The engine builds a method name from it and calls
/// that method, so a number it has no method for is a command it steps over
/// without doing anything, and a reader that reported it as unknown would stop a
/// game over a command the game itself would have run past.
/// </param>
/// <param name="Parameters">The parameters, in the order the engine reads them.</param>
/// <param name="Indent">
/// How deep this command sits. The engine uses it for everything: which commands
/// belong to a branch, where a jump lands, when a loop repeats. A list read
/// without its indents is a list of commands with no structure at all.
/// </param>
public readonly record struct MzCommandEntry(
    int Code, IReadOnlyList<string> Parameters, int Indent)
{
    public static MzCommandEntry From(MzValue pCommand)
    {
        var parameters = new List<string>();
        var value = pCommand.Member("parameters");
        if (value != null)
        {
            foreach (var item in value.Items)
            {
                // **A boolean is a value, and a first draft lost it.** A 232
                // carries its "wait" in the eleventh slot as a real JSON
                // boolean, and only a Number and a Text were handled here — so
                // `true` and `false` both became the empty string, and this
                // game's four moves came back as four that never ask to wait.
                // The engine reads that slot as a truth value, so a boolean
                // has to arrive as one rather than as nothing.
                parameters.Add(item.Kind switch
                {
                    // A number the game wrote as a float is read as the whole
                    // number the engine would compare it as, not as a formatted
                    // string with a decimal point in it.
                    MzKind.Number =>
                        System.Math.Round(item.Number).ToString(CultureInfo.InvariantCulture),
                    MzKind.Bool => item.Boolean ? "true" : "false",
                    // **A nested object or array is written out again, not
                    // dropped.** A 205's second parameter is
                    // `{list: [...], repeat, skippable, wait}` — a whole
                    // object — and `_ => item.Text` turned it into the empty
                    // string, so every move route in a game came back empty
                    // and the reader could not have said why. A reader that
                    // throws away a shape it does not recognise cannot tell
                    // the difference between "this game has no routes" and
                    // "this reader cannot read routes", and those are
                    // different claims.
                    MzKind.Object or MzKind.Array => MzJson.Write(item),
                    _ => item.Text,
                });
            }
        }
        return new MzCommandEntry(
            pCommand.Member("code")?.IntOr(0) ?? 0,
            parameters,
            pCommand.Member("indent")?.IntOr(0) ?? 0);
    }
}
