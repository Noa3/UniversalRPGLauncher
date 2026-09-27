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
                parameters.Add(item.Kind == MzKind.Number
                    // A number the game wrote as a float is read as the whole
                    // number the engine would compare it as, not as a formatted
                    // string with a decimal point in it.
                    ? System.Math.Round(item.Number).ToString(CultureInfo.InvariantCulture)
                    : item.Text);
            }
        }
        return new MzCommandEntry(
            pCommand.Member("code")?.IntOr(0) ?? 0,
            parameters,
            pCommand.Member("indent")?.IntOr(0) ?? 0);
    }
}
