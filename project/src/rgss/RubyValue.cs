using System;
using System.Collections.Generic;

namespace UniversalRPG.Rgss;

/// <summary>What a Ruby value is, without being one.</summary>
/// <remarks>
/// <para>
/// The seven kinds below are the ones the language itself defines, and a value
/// is a fact about the data rather than a thing with behaviour. Nothing here
/// knows how to add, compare, call or convert anything; that belongs to whatever
/// would eventually use these values, and until there is one, keeping the
/// behaviour out of the data keeps the data honest.
/// </para>
/// <para>
/// A game object's own class is deliberately not in this list. A script may
/// define a class and every value of it is a <see cref="Object"/>, with the
/// class kept alongside as text. Putting the class in here would mean the data
/// layer had to know what classes exist, and it does not.
/// </para>
/// </remarks>
public enum RubyValueKind
{
    /// <summary>The absence of a value, which is a value in its own right.</summary>
    Nil,

    /// <summary>A whole number.</summary>
    /// <remarks>
    /// Ruby integers have no width, so a value this reader can hold says so
    /// rather than silently wrapping. A game may well use a number this reader
    /// refuses, and finding that out at read time is better than finding out
    /// later as an arithmetic answer that looks plausible.
    /// </remarks>
    Integer,

    /// <summary>A number with a fraction.</summary>
    Float,

    /// <summary>One of the two values the language answers with by itself.</summary>
    /// <remarks>
    /// True and false are not numbers in Ruby even though a whole number can
    /// stand in for either, so they are a kind of their own here. A reader that
    /// folded them into the whole numbers would lose the difference between the
    /// name and the number, and a game uses that difference.
    /// </remarks>
    Boolean,

    /// <summary>A sequence of characters, which is bytes and not characters.</summary>
    /// <remarks>
    /// A game's strings come from whatever encoding its author used, usually
    /// CP932 on a Japanese install. The bytes are kept as they are, and
    /// decoding is a separate decision this reader does not make.
    /// </remarks>
    String,

    /// <summary>A name, which is a thing you refer to rather than a thing you hold.</summary>
    Symbol,

    /// <summary>A pattern, which is data and is never matched by this reader.</summary>
    /// <remarks>
    /// A regular expression is kept as its source and its options. Nothing here
    /// compiles it or runs it, because running a pattern from a game is running
    /// something the game brought with it.
    /// </remarks>
    Regexp,

    /// <summary>A whole value with an identity, which anything can be asked about.</summary>
    /// <remarks>
    /// This covers numbers, strings, symbols, patterns and a game's own
    /// objects alike, because from the outside they answer the same questions.
    /// What one of them actually is lives beside it, not inside it.
    /// </remarks>
    Object,
}

/// <summary>
/// A value a Ruby script could have, held as data.
/// </summary>
/// <remarks>
/// A value here knows what it is and what is in it. It does not know what any of
/// that means: comparing two of them, adding them, or reading a field is a
/// decision the language makes, and this layer has no opinion about any of them.
/// The split is not ceremony. Once values start answering questions, the answer
/// depends on a world this data layer cannot see, and the two get tangled in a
/// way that neither can be tested on its own.
/// </remarks>
public sealed class RubyValue : IEquatable<RubyValue>
{
    private RubyValue(RubyValueKind pKind)
    {
        Kind = pKind;
    }

    /// <summary>What this value is.</summary>
    public RubyValueKind Kind { get; }

    /// <summary>The number, for a <see cref="RubyValueKind.Integer"/>.</summary>
    public long Integer { get; private init; }

    /// <summary>The number, for a <see cref="RubyValueKind.Float"/>.</summary>
    public double Real { get; private init; }

    /// <summary>The bytes, for a <see cref="RubyValueKind.String"/>.</summary>
    public byte[] Bytes { get; private init; } = [];

    /// <summary>The name, for a <see cref="RubyValueKind.Symbol"/>.</summary>
    public string? Name { get; private init; }

    /// <summary>The pattern text, for a <see cref="RubyValueKind.Regexp"/>.</summary>
    public string? Source { get; private init; }

    /// <summary>The pattern flags, for a <see cref="RubyValueKind.Regexp"/>.</summary>
    public int Options { get; private init; }

    /// <summary>Which of the two it is, for a <see cref="RubyValueKind.Boolean"/>.</summary>
    public bool Boolean { get; private init; }

    /// <summary>What is inside, for a list.</summary>
    public IReadOnlyList<RubyValue> Items { get; private init; } = Array.Empty<RubyValue>();

    /// <summary>What is inside, for a whole value.</summary>
    /// <remarks>
    /// An object's members are held as written, keyed by name. A key that is
    /// not a name yet is still accepted, because a game may index an object with
    /// a symbol this reader has no name for.
    /// </remarks>
    public IReadOnlyDictionary<RubyValue, RubyValue> Members { get; private init; }
        = new Dictionary<RubyValue, RubyValue>();

    /// <summary>
    /// The class a game's own value was declared with, as written, or null.
    /// </summary>
    /// <remarks>
    /// This is the name from the script and never a loaded type. A value that
    /// says `Sprite` says so because the file said so, and this layer has not
    /// looked up whether such a thing exists anywhere.
    /// </remarks>
    public string? ClassName { get; private init; }

    /// <summary>
    /// Whether this value is a list rather than a whole value.
    /// </summary>
    /// <remarks>
    /// Ruby tells the two apart by asking the value, not by the shape of its
    /// contents, so the difference is kept rather than guessed from whether
    /// anything happens to be inside.
    /// </remarks>
    public bool IsList { get; private init; }

    /// <summary>Whether this value is the absence of one.</summary>
    public bool IsNil => Kind == RubyValueKind.Nil;

    public static RubyValue Nil { get; } = new(RubyValueKind.Nil);

    public static RubyValue OfBoolean(bool pValue) =>
        new(RubyValueKind.Boolean) { Boolean = pValue };

    public static RubyValue OfArray(IReadOnlyList<RubyValue> pItems) =>
        new(RubyValueKind.Object) { Items = pItems, IsList = true };

    public static RubyValue OfInteger(long pValue) =>
        new(RubyValueKind.Integer) { Integer = pValue };

    public static RubyValue OfReal(double pValue) =>
        new(RubyValueKind.Float) { Real = pValue };

    public static RubyValue OfBytes(byte[] pBytes) =>
        new(RubyValueKind.String) { Bytes = pBytes };

    public static RubyValue OfSymbol(string pName) =>
        new(RubyValueKind.Symbol) { Name = pName };

    public static RubyValue OfRegexp(string pSource, int pOptions) =>
        new(RubyValueKind.Regexp) { Source = pSource, Options = pOptions };

    public static RubyValue OfObject(
        string? pClassName, IReadOnlyDictionary<RubyValue, RubyValue> pMembers) =>
        new(RubyValueKind.Object) { ClassName = pClassName, Members = pMembers };

    public static RubyValue OfEmptyObject(string? pClassName) =>
        new(RubyValueKind.Object)
        {
            ClassName = pClassName,
            Members = new Dictionary<RubyValue, RubyValue>(),
        };

    /// <summary>
    /// Whether two values are the same thing, by what they are.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two integers with the same number are the same integer. Two strings with
    /// the same bytes are the same string. Two objects are the same object only
    /// if they are the same reference, because an object in a game is defined by
    /// its identity and not by what is inside it: two objects that happen to hold
    /// the same members are still two objects.
    /// </para>
    /// <para>
    /// This is the identity the language gives a value, not an answer to a
    /// question a script may ask. Ruby's own equality asks more than this and
    /// gives each class a say, so a script that compares two values the way Ruby
    /// does is doing something this does not do for it.
    /// </para>
    /// </remarks>
    public bool Equals(RubyValue? pOther)
    {
        if (ReferenceEquals(this, pOther))
        {
            return true;
        }
        if (pOther == null || pOther.Kind != Kind)
        {
            return false;
        }
        switch (Kind)
        {
            case RubyValueKind.Nil:
                return true;
            case RubyValueKind.Integer:
                return Integer == pOther.Integer;
            case RubyValueKind.Float:
                // A float and an integer are different kinds, and a number that
                // is the same either way is a question about converting, not
                // about whether two values are the same.
                return Real.Equals(pOther.Real);
            case RubyValueKind.String:
                return Bytes.AsSpan().SequenceEqual(pOther.Bytes);
            case RubyValueKind.Symbol:
                return string.Equals(Name, pOther.Name, StringComparison.Ordinal);
            case RubyValueKind.Regexp:
                return Options == pOther.Options
                    && string.Equals(Source, pOther.Source, StringComparison.Ordinal);
            case RubyValueKind.Boolean:
                return Boolean == pOther.Boolean;
            case RubyValueKind.Object:
                return false;
            default:
                return false;
        }
    }

    public override bool Equals(object? pOther) => Equals(pOther as RubyValue);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);
        switch (Kind)
        {
            case RubyValueKind.Integer:
                hash.Add(Integer);
                break;
            case RubyValueKind.Float:
                hash.Add(Real);
                break;
            case RubyValueKind.String:
                hash.AddBytes(Bytes);
                break;
            case RubyValueKind.Symbol:
                hash.Add(Name, StringComparer.Ordinal);
                break;
            case RubyValueKind.Regexp:
                hash.Add(Source, StringComparer.Ordinal);
                hash.Add(Options);
                break;
            case RubyValueKind.Boolean:
                hash.Add(Boolean);
                break;
            case RubyValueKind.Object:
                // A whole value is identified by who holds the reference, so its
                // hash follows that too. Two that hold the same members are two
                // values and must not be filed under one number.
                hash.Add(RuntimeHelpersGetHash());
                break;
        }
        return hash.ToHashCode();
    }

    private int RuntimeHelpersGetHash() =>
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

    public override string ToString()
    {
        return Kind switch
        {
            RubyValueKind.Nil => "nil",
            RubyValueKind.Integer => Integer.ToString(System.Globalization.CultureInfo.InvariantCulture),
            RubyValueKind.Float => Real.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            RubyValueKind.String => $"\"{Bytes.Length} bytes\"",
            RubyValueKind.Symbol => Name ?? "",
            RubyValueKind.Regexp => Source ?? "",
            RubyValueKind.Boolean => Boolean ? "true" : "false",
            RubyValueKind.Object => IsList ? $"[{Items.Count} items]" : ClassName ?? "object",
            _ => "?",
        };
    }
}
