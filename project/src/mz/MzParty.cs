using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// What the party is carrying, and what a 126 does to it.
/// </summary>
/// <remarks>
/// <para>
/// K-121 to K-125 read MZ data and walked event lists, and neither needed to
/// know what a game <b>owns</b>. A 126 does: <b>Change Items</b> is
///
/// <code>
/// Game_Interpreter.prototype.command126 = function(params) {
///     const value = this.operateValue(params[1], params[2], params[3]);
///     $gameParty.gainItem($dataItems[params[0]], value);
///     return true;
/// };</code>
///
/// and this game uses it seventeen times across the two event pages that can
/// be reached here, so it is the next command with a real effect that a reader
/// can be checked against.
/// </para>
/// <para>
/// Four rules, each read out of <c>Game_Party</c> and not inferred:
///
/// <list type="number">
/// <item>
/// <b>The count is clamped to ninety-nine, not to the number the event asked
/// for.</b> <c>container[item.id] = newNumber.clamp(0, this.maxItems(item))</c>
/// and <c>maxItems</c> is <c>return 99</c>. **This game's events ask for 999
/// and for 10</b>, and an implementation that added the number as written
/// would give a player a thousand of something the engine refuses to hold.
/// </item>
/// <item>
/// <b>A count that lands on zero is deleted from the container.</b>
/// <c>if (container[item.id] === 0) { delete container[item.id]; }</c> A
/// reader that kept a zero would answer "does the party have any" differently
/// from the engine the moment a game checks it.
/// </item>
/// <item>
/// <b>Losing more than the party has clamps to zero, it does not go
/// negative.</b> The clamp is from below as well as above, and a game that
/// takes four of something you have one of simply has none.
/// </item>
/// <item>
/// <b>An index with no item behind it does nothing at all.</b>
/// <c>itemContainer</c> returns null for a missing item and
/// <c>gainItem</c> returns early, so a 126 naming an index past the end of
/// <c>Items.json</c> is not an error and not a crash. It is also not a step
/// the reader may take silently and call done, because the game asked for
/// something and did not get it — so the fact is recorded and the reason is
/// named.
/// </item>
/// </list>
/// </para>
/// <para>
/// <b>What is deliberately not here.</b> The party has members, and a 127
/// or 128 with <c>includeEquip</c> set will strip equipment off them, and a 130
/// will change who is in the party. None of that is modelled, and a caller
/// asking about a member gets an empty one rather than a guess. The
/// inventory is what 126 touches and it is modelled in full.
/// </para>
/// </remarks>
public sealed class MzParty
{
    /// <summary>The facts this party is part of — the engine's one set.</summary>
    private readonly MzBranchFacts _facts;

    /// <summary>
    /// The item ids this game stores, by their index in <c>Items.json</c>.
    /// A 126 names one of those, and an id that is not here is one the engine
    /// would look up as <c>undefined</c> and skip.
    /// </summary>
    private readonly HashSet<int> _known;

    /// <summary>What an event asked for and did not get.</summary>
    private readonly List<string> _notices = new();

    /// <param name="pFacts">
    /// The game's facts, which the party is a part of rather than an owner
    /// beside. **One set of facts, because the engine has one**: a caller that
    /// read <c>Items</c> and a 126 that wrote it must be looking at the same
    /// dictionary, or a branch asking whether the party has a potion would
    /// disagree with the event that gave it one.
    /// </param>
    /// <param name="pKnownItems">
    /// The item ids in <c>Items.json</c>, or null when the caller has no list
    /// and every id is taken at its word.
    /// </param>
    public MzParty(MzBranchFacts pFacts, IEnumerable<int> pKnownItems = null)
    {
        _facts = pFacts ?? new MzBranchFacts();
        // The facts carry the ids when a caller has read the game's file, and
        // a caller that passes its own list is saying something the facts do
        // not. **Null means nothing is known**, which is a different thing from
        // "every id exists" and is treated as such.
        _known = pKnownItems != null
            ? new HashSet<int>(pKnownItems)
            : _facts.KnownItems != null
                ? new HashSet<int>(_facts.KnownItems)
                // **Nothing known is not everything allowed.** A first draft
                // left `_known` null here and read null as "no id to object
                // to", which made a reader with no game behind it add 999 of
                // anything the event named. An item that is not in the file is
                // one the engine looks up as undefined and skips, and this
                // reader names it rather than inventing it.
                : new HashSet<int>();
    }

    /// <summary>The facts this party is part of, for a caller that has none.</summary>
    public MzBranchFacts Facts => _facts;

    /// <summary>
    /// The engine's own <c>maxItems</c>, which is <c>return 99</c> with no
    /// argument at all. **A field so a test can ask what happens at the
    /// boundary**, because the boundary is the whole of the rule and it is
    /// invisible in the middle of the range.
    /// </summary>
    public int MaxItems
    {
        get => _facts.MaxItems;
        init => _facts.MaxItems = value;
    }

    /// <summary>How many of an item the party has, as <c>numItems</c> does.</summary>
    public int NumItems(int pItemId) =>
        _facts.Items.TryGetValue(pItemId, out var count) ? count : 0;

    /// <summary>Whether the party carries any of an item, as <c>hasItem</c>
    /// does when the game does not ask about equipment.</summary>
    public bool HasItem(int pItemId) => NumItems(pItemId) > 0;

    /// <summary>Whether the party is carrying as many as it may.</summary>
    public bool HasMaxItems(int pItemId) => NumItems(pItemId) >= MaxItems;

    /// <summary>
    /// The ids the party has something of. **A copy, not the container**,
    /// because a zero count is deleted and a reader that handed out the
    /// dictionary would let a zero back into it.
    /// </summary>
    public List<int> Carried()
    {
        var ids = new List<int>();
        foreach (var pair in _facts.Items)
        {
            if (pair.Value > 0)
            {
                ids.Add(pair.Key);
            }
        }
        ids.Sort();
        return ids;
    }

    /// <summary>What an event asked for and did not get, in order.</summary>
    public IReadOnlyList<string> Notices => _notices;

    /// <summary>
    /// What a 126 does, and what it could not.
    /// </summary>
    /// <param name="pItemId">The item's index in <c>Items.json</c>.</param>
    /// <param name="pOperation">
    /// <c>operateValue</c>'s first argument: zero keeps the operand, anything
    /// else negates it. There is no third case in the engine.
    /// </param>
    /// <param name="pOperandType">
    /// Zero for a number written in the event, anything else for a variable.
    /// </param>
    /// <param name="pOperand">The number, or the variable to read.</param>
    /// <param name="pVariableValue">
    /// The variable's number, read by the caller — this class does not hold
    /// the game's variables, because the engine's <c>$gameVariables</c> is one
    /// object shared with the interpreter and two owners of it would be two
    /// truths.
    /// </param>
    /// <returns>
    /// What happened, said as a sentence. **A caller that wants to branch on
    /// it reads the count**, which is why the count is also returned.
    /// </returns>
    public string GainItem(
        int pItemId, int pOperation, int pOperandType, int pOperand,
        int pVariableValue = 0)
    {
        // `operateValue` is
        //   const value = operandType === 0 ? operand : $gameVariables.value(operand);
        //   return operation === 0 ? value : -value;
        // — the operand type is asked about **first**, and only a variable
        // operand is read from the game. A first draft read the variable even
        // for a number, which made a 126 with a constant amount depend on
        // whatever a variable happened to hold.
        var value = pOperandType == 0 ? pOperand : pVariableValue;
        if (pOperation != 0)
        {
            value = -value;
        }

        if (!_known.Contains(pItemId))
        {
            // The engine's own line is `if (container) { ... }`, and
            // `itemContainer` returns null for an item that is not there. So
            // the engine steps over this. **This reader says it did not
            // happen**, because a game that asks for an item this repository
            // cannot hand over would otherwise look like a game that had it
            // and used it.
            var note =
                $"item {pItemId} is not in this game's Items.json, so the {value}"
                + " it asked for was not added";
            _notices.Add(note);
            return note;
        }

        // `this._items[item.id] = newNumber.clamp(0, this.maxItems(item))`
        var last = NumItems(pItemId);
        var after = Clamp(last + value, 0, MaxItems);

        if (after == 0)
        {
            // `if (container[item.id] === 0) { delete container[item.id]; }` —
            // the entry goes, rather than sitting there as a zero.
            _facts.Items.Remove(pItemId);
        }
        else
        {
            _facts.Items[pItemId] = after;
        }

        // A gain and a loss are said the way the game's own author would say
        // them, and **the clamp is said when it bit**, because "you now have
        // 99 of 999 asked for" is the thing a reader of a log needs.
        if (after == MaxItems && last + value > MaxItems)
        {
            return $"item {pItemId} up to {after} of {last + value} asked for,"
                + $" which is the {MaxItems} the engine holds";
        }
        if (after == 0 && last + value < 0)
        {
            return $"item {pItemId} down to none of {last + value} asked for,"
                + " which is all there was";
        }
        return value >= 0
            ? $"item {pItemId} up from {last} to {after}"
            : $"item {pItemId} down from {last} to {after}";
    }

    /// <summary>
    /// The engine's own <c>Number.clamp</c>, which is
    /// <c>Math.min(Math.max(value, min), max)</c> — and note the order,
    /// because a clamp written the other way round answers differently when
    /// the range is the wrong way up, and a reader that got that backwards
    /// would accept a negative maximum and hand out a negative count.
    /// </summary>
    private static int Clamp(int pValue, int pMin, int pMax) =>
        System.Math.Min(System.Math.Max(pValue, pMin), pMax);
}
