using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UniversalRPG.Rgss;

/// <summary>The type byte of a marshalled value.</summary>
public static class MarshalType
{
    public const byte Nil = (byte)'0';
    public const byte True = (byte)'T';
    public const byte False = (byte)'F';
    public const byte Integer = (byte)'i';
    public const byte Float = (byte)'f';
    public const byte Symbol = (byte)':';
    public const byte SymbolLink = (byte)';';
    public const byte String = (byte)'"';
    public const byte ObjectLink = (byte)'@';
    public const byte Array = (byte)'[';
    public const byte Hash = (byte)'{';
    public const byte HashWithDefault = (byte)'}';
    public const byte Object = (byte)'o';
    public const byte UserMarshal = (byte)'U';
    public const byte UserDefined = (byte)'u';
    public const byte Struct = (byte)'S';
    public const byte UserClass = (byte)'C';
    public const byte Regexp = (byte)'/';
    public const byte InstanceVariables = (byte)'I';
    public const byte Extended = (byte)'e';
    public const byte Bignum = (byte)'l';
    public const byte Class = (byte)'c';
    public const byte Module = (byte)'m';
}

/// <summary>A marshalled object reference, kept as a link rather than resolved.</summary>
public sealed class MarshalLink
{
    public required string Kind { get; init; }
    public required int Index { get; init; }
    public override string ToString() => $"{Kind} link to {Index}";
}

/// <summary>
/// A marshalled value, as data.
/// </summary>
/// <remarks>
/// The reader produces this instead of a live object, on purpose. A game
/// database is full of instances of classes the reader has never heard of, and
/// turning a stream into live objects would mean either running the game's Ruby
/// to resolve them or inventing classes that do not exist. A value tree can be
/// inspected, compared and used to build a model without either.
/// </remarks>
public sealed class MarshalValue
{
    /// <summary>What kind of value this is, from the type byte table.</summary>
    public required string Kind { get; init; }

    /// <summary>The value, for the scalar kinds.</summary>
    public long? Integer { get; init; }

    /// <summary>The value, for floats.</summary>
    public double? Real { get; init; }

    /// <summary>The bytes, for strings, symbols and user defined payloads.</summary>
    public byte[]? Bytes { get; init; }

    /// <summary>The text, when the bytes are known to be text.</summary>
    public string? Text { get; init; }

    /// <summary>The class or module name, for the types that carry one.</summary>
    public string? ClassName { get; init; }

    /// <summary>The elements, for arrays, hashes, structs and instance variables.</summary>
    public IReadOnlyList<MarshalValue> Items { get; init; } = Array.Empty<MarshalValue>();

    /// <summary>The instance variable names, paired with <see cref="Items"/>.</summary>
    public IReadOnlyList<string> Keys { get; init; } = Array.Empty<string>();

    /// <summary>The link, when this value is a reference to an earlier one.</summary>
    public MarshalLink? Link { get; init; }

    /// <summary>Whether this value is a link to an object defined earlier in the stream.</summary>
    public bool IsLink => Link != null;

    public override string ToString()
    {
        if (Link != null)
        {
            return Link.ToString()!;
        }
        if (Text != null)
        {
            return $"{Kind} \"{Text}\"";
        }
        if (Integer.HasValue)
        {
            return $"{Kind} {Integer.Value}";
        }
        if (Real.HasValue)
        {
            return $"{Kind} {Real.Value.ToString(CultureInfo.InvariantCulture)}";
        }
        return $"{Kind} with {Items.Count} items";
    }
}

/// <summary>The exception a marshalled stream that cannot be read honestly raises.</summary>
public sealed class MarshalFormatException : Exception
{
    public MarshalFormatException(string pMessage)
        : base(pMessage)
    {
    }
}
