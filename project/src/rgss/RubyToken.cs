using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UniversalRPG.Rgss;

/// <summary>The kind of a token in a Ruby script.</summary>
public enum RubyTokenKind
{
    EndOfInput,
    Keyword,
    Identifier,
    Constant,
    InstanceVariable,
    GlobalVariable,
    Integer,
    Float,
    String,
    Symbol,
    Regexp,
    Operator,
    Delimiter,
    Newline,
    Semicolon,
}

/// <summary>One token, with the place it came from.</summary>
public sealed class RubyToken
{
    public required RubyTokenKind Kind { get; init; }
    public required string Text { get; init; }
    public required int Offset { get; init; }
    public required int Line { get; init; }

    /// <summary>For integers, the value; null for everything else.</summary>
    public long? Integer { get; init; }

    /// <summary>For floats, the value; null for everything else.</summary>
    public double? Real { get; init; }

    /// <summary>For a string, its bytes, which are not necessarily text.</summary>
    public byte[]? Bytes { get; init; }

    /// <summary>For a string, its text, when the encoding is one this reader knows.</summary>
    public string? Value { get; init; }

    /// <summary>
    /// For a string or regexp, the parts between the escapes, so a reader can
    /// rebuild it without having to re-interpret the escapes.
    /// </summary>
    public IReadOnlyList<RubyStringPart> Parts { get; init; } = Array.Empty<RubyStringPart>();

    public override string ToString() => $"{Kind} {Text}";
}

/// <summary>One piece of a string literal, either literal text or an escape.</summary>
public sealed class RubyStringPart
{
    /// <summary>True when this part is an escape sequence rather than literal text.</summary>
    public required bool IsEscape { get; init; }

    /// <summary>The characters, or the escape without its backslash.</summary>
    public required string Text { get; init; }

    /// <summary>For an escape, what it produces, where the reader knows.</summary>
    public string? Resolved { get; init; }
}
