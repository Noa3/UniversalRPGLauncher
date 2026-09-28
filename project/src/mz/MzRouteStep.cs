using System;
using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Web;

/// <summary>One step of a move route, as the game wrote it.</summary>
/// <remarks>
/// A move route is a list of these, and <b>the list is nested inside the 205's
/// second parameter</b> — <c>{list: [...], repeat, skippable, wait}</c>, where
/// every entry is <c>{code, parameters, indent}</c>. Not a flat list of
/// numbers, which is what a first reading of a move route expects.
/// </remarks>
public readonly record struct MzRouteStep(int Code, IReadOnlyList<string> Parameters)
{
    /// <summary>The three flags <c>command205</c> reads, as the game wrote
    /// them.</summary>
    public readonly record struct Route(
        IReadOnlyList<MzRouteStep> List, bool Repeat, bool Skippable, bool Wait);

    /// <summary>
    /// Reads a route out of a parameter this reader already flattened.
    /// </summary>
    /// <remarks>
    /// <b>This is a repair, and the reason it is here is worth stating.</b>
    /// <c>MzCommandEntry.From</c> keeps a command's parameters as strings,
    /// and it turns anything that is not a number or a boolean into
    /// <c>item.Text</c> — which for a nested object is the empty string. A
    /// 205's second parameter is exactly such an object, so **the whole route
    /// arrived as one empty string and ninety-six routes came back empty.**
    ///
    /// Writing the value out again costs one serializer, and it keeps the rule
    /// that matters: <b>a reader never throws away a shape it does not
    /// understand.</b> A route this reader cannot act on is named; a route
    /// this reader never saw is a lie about the game.
    /// </remarks>
    public static Route ReadFromParameter(
        string pParameter, IReadOnlyList<string>? pFallback = null)
    {
        if (string.IsNullOrEmpty(pParameter))
        {
            return new Route(
                Array.Empty<MzRouteStep>(), false, false, false);
        }

        var top = MzJson.TryParse(pParameter, out var wert, out _)
            ? wert
            : null;
        return top == null
            ? new Route(Array.Empty<MzRouteStep>(), false, false, false)
            : Read(top, pFallback);
    }

    /// <summary>
    /// Reads one route, keeping the three flags exactly as they are.
    /// </summary>
    /// <remarks>
    /// <b>All three are read, and only one of them is acted on today.</b>
    /// <c>command205</c> is
    /// <c>character.forceMoveRoute(params[1]); if (params[1].wait)
    /// setWaitMode("route");</c> — so <c>wait</c> holds the event page, and
    /// <c>repeat</c> and <c>skippable</c> are read by <c>advanceMoveRouteIndex
    /// </c> and by the input side. A reader that invents behaviour for
    /// <c>repeat</c> without <c>advanceMoveRouteIndex</c> would loop a route
    /// the engine loops only while the character is moving.
    /// </remarks>
    public static Route Read(MzValue pValue, IReadOnlyList<string>? pFallback = null)
    {
        if (pValue.Kind != MzKind.Object)
        {
            return new Route(
                pFallback is null
                    ? Array.Empty<MzRouteStep>()
                    : Array.Empty<MzRouteStep>(),
                false, false, false);
        }

        var list = new List<MzRouteStep>();
        foreach (var item in pValue.Member("list")?.Items ?? new List<MzValue>())
        {
            list.Add(From(item));
        }

        return new Route(
            list,
            Truth(pValue.Member("repeat")),
            Truth(pValue.Member("skippable")),
            Truth(pValue.Member("wait")));
    }

    /// <summary>Reads one entry of a route list.</summary>
    public static MzRouteStep From(MzValue pValue)
    {
        // **A number the parser read is in `.Number`, not in `.Text`.** A
        // first draft read `Text`, which is empty for a number, so **every
        // route code came back 0** — which is END — and all ninety-six routes
        // did nothing at all while looking perfectly plausible: five steps
        // read, five ENDs run, every character standing still.
        //
        // The same trap is in `parameters` below, and it is why a route's
        // CHANGE_SPEED came back as nothing.
        var code = pValue.Member("code")?.IntOr(0) ?? 0;

        // **A step with no `parameters` key is not a fault and not an empty
        // list of faults.** MZ writes `{"code":3,"indent":null}` for a plain
        // step, with no `parameters` at all, and a reader that required the
        // key would have called every plain step in a game a broken one.
        var parameters = new List<string>();
        foreach (var item in pValue.Member("parameters")?.Items ?? new List<MzValue>())
        {
            parameters.Add(item.Kind switch
            {
                MzKind.Number => System.Math.Round(item.Number)
                    .ToString(CultureInfo.InvariantCulture),
                MzKind.Bool => item.Boolean ? "true" : "false",
                _ => item.Text,
            });
        }

        return new MzRouteStep(code, parameters);
    }

    /// <summary>
    /// A flag as the engine reads it: <c>!!</c> and not <c>"anything but
    /// empty"</c>.
    /// </summary>
    private static bool Truth(MzValue? pValue)
    {
        if (pValue == null)
        {
            return false;
        }
        // **A JSON boolean arrives in `.Boolean`, not in `.Text`.** A first
        // draft read `Text`, which is the empty string for a boolean, so
        // **all three flags of all ninety-six routes came back false** and
        // not one page was ever held by its route.
        return pValue.Kind == MzKind.Bool
            ? pValue.Boolean
            : pValue.Text is { Length: > 0 } && pValue.Text != "0";
    }
}
