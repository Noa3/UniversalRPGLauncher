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

    /// <summary>
    /// For a pattern, the option letters behind its second slash.
    /// </summary>
    /// <remarks>
    /// <strong>And they were read and thrown away.</strong> The lexer
    /// collected the letters after the closing slash and put nothing
    /// anywhere, <strong>so <c>/held/i</c> and <c>/held/</c> were the same
    /// pattern</strong> — and a script that looks a name up without caring
    /// about the spelling did not find it, **and nothing said so.**
    /// </remarks>
    public int Options { get; init; }

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

/// <summary>
/// One here document that a <c>&lt;&lt;</c> has opened and whose body has not
/// been read yet.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a lexer-private state and not a token</strong>,
/// because a heredoc's body is not one token: <c>&lt;&lt;EOH</c> is one
/// token where the <c>&lt;&lt;</c> stands, and the body is a second one
/// that arrives later.
/// </para>
/// <para>
/// <strong>And the four fields are the four the grammar has</strong>,
/// measured at <c>heredoc_identifier</c> 3107ff and
/// <c>here_document</c> 3208ff: the terminator, whether <c>#{}</c> counts,
/// whether leading spaces are allowed in front of the terminator, and the
/// line the <c>&lt;&lt;</c> stood on -- <strong>and that last one is only
/// for the error message</strong>.
/// </para>
/// </remarks>
internal sealed class RubyHeredoc
{
    /// <summary>The word that ends the body, without any quote.</summary>
    public required string Terminator { get; init; }

    /// <summary>
    /// Whether <c>#{}</c> in the body is an interpolation, and that is false
    /// for <c>&lt;&lt;'X'</c> and true for everything else.
    /// </summary>
    public required bool Expand { get; init; }

    /// <summary>Whether the <c>-</c> was written, so leading spaces count.</summary>
    public required bool Einruecken { get; init; }

    /// <summary>The line the <c>&lt;&lt;</c> stood on, for the error.</summary>
    public required int BodyLine { get; init; }

    /// <summary>Set once the body has been read.</summary>
    public bool BodyRead { get; set; }
}
