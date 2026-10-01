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

    /// <summary>
    /// That a newline token stood between the previous one and this one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the one bit that decides whether an
    /// <c>if</c> is a statement's own head or a modifier</strong> --
    /// <strong>and it is measured at <c>parse.y</c> 3338, where the lexer
    /// swallows a newline in four states and returns one in all the
    /// others:</strong>
    /// </para>
    /// <code>
    /// 3338  case '\n':
    /// 3340    case EXPR_BEG:
    /// 3341    case EXPR_FNAME:
    /// 3342    case EXPR_DOT:
    /// 3343    case EXPR_CLASS:
    /// 3344      goto retry;      /\* no tNL: the line goes on
    /// 3345    default:
    /// 3346      break;           /\* tNL: the statement ends here
    /// 3348  command_start = Qtrue;
    /// 3349  lex_state = EXPR_BEG;
    /// </code>
    /// <para>
    /// <strong>And <c>lex_state = EXPR_BEG</c> at 3349 is the half that
    /// matters here</strong>: <strong>after a newline the next keyword is
    /// <c>kIF</c> and not <c>kIF_MOD</c></strong>, measured at 4380.
    /// </para>
    /// <para>
    /// <strong>And the two shapes, and the difference is only this bit:</strong>
    /// </para>
    /// <code>
    /// z = 1 if c      Modifier:  no newline between the 1 and the if
    /// d = a.b 1
    /// if c            Statement:  a newline stands between them
    /// </code>
    /// </para>
    /// </remarks>
    public bool NewlineVorher { get; init; }

    /// <summary>
    /// That this <c>rescue</c> is the modifier on the statement in front of
    /// it, and that is measured at <c>lex.c</c> 86 and not derived from the
    /// word.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a second bit and not the first one reused,
    /// because <c>rescue</c> sits on <c>EXPR_MID</c> and the other four
    /// sit on <c>EXPR_BEG</c>:</strong>
    /// </para>
    /// <code>
    /// {"if",     {kIF,     kIF_MOD},     EXPR_BEG},
    /// {"rescue", {kRESCUE, kRESCUE_MOD}, EXPR_MID},
    /// </code>
    /// <para>
    /// <strong>And the shape that separates them, and both are in real
    /// files</strong> -- <c>StatsEdit.rb</c> line 83 of a VX Ace game on
    /// this machine for the first, and its own <c>parse.y</c> 461 and 353
    /// for the second:
    /// </para>
    /// <code>
    /// x = a rescue b         EXPR_MID,  and it is a modifier
    /// begin
    ///   a
    /// rescue =&gt; e          its own line,  and it opens a body
    /// end
    /// </code>
    /// </para>
    /// </remarks>
    public bool RescueIstModifier { get; init; }

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
/// <summary>
/// A keyword that can be either a statement's own head or a modifier on the
/// statement in front of it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And Ruby has two tokens for <c>if</c>, and this is the whole
/// subject.</strong> Measured at <c>lex.c</c> 96, in Ruby's own tree:
/// </para>
/// <code>
/// {"if", {kIF, kIF_MOD}, EXPR_BEG},
/// </code>
/// <para>
/// <strong>And the lexer picks, measured at <c>parse.y</c> 4380:</strong>
/// </para>
/// <code>
/// 4380  if (state == EXPR_BEG)
/// 4381      return kw->id[0];      kIF     -- the head of a statement
/// 4382  else {
/// 4383      if (kw->id[0] != kw->id[1])
/// 4384          lex_state = EXPR_BEG;
/// 4385      return kw->id[1];      kIF_MOD -- a modifier on what came before
/// </code>
/// <para>
/// <strong>And the parser never has to guess</strong>, because the choice was
/// made before it saw anything, <strong>and a reader that guesses instead has
/// to re-derive the state after every token</strong> -- <strong>and that is
/// the mistake this repository made eleven times in a row.</strong>
/// </para>
/// <para>
/// <strong>And the four words in question</strong>, measured at
/// <c>parse.y</c> 419, 428, 437 and 449:
/// <c>stmt kIF_MOD expr_value</c>, <c>stmt kUNLESS_MOD expr_value</c>,
/// <c>stmt kWHILE_MOD expr_value</c> and <c>stmt kUNTIL_MOD expr_value</c> --
/// <strong>and all four say <c>stmt</c> and not <c>arg</c>.</strong>
/// </para>
/// </remarks>
internal static class RubyRolle
{
    /// <summary>
    /// True when this keyword modifies the statement in front of it, and that
    /// is measured at <c>parse.y</c> 4380 rather than guessed.
    /// </summary>
    /// <param name="pToken">The token to classify.</param>
    /// <returns>
    /// True for <c>if</c>, <c>unless</c>, <c>while</c> and <c>until</c> when
    /// no newline stands in front of it.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And the bit is not enough on its own, and the four words are
    /// the second half.</strong> <c>rescue</c> and <c>ensure</c> are keywords
    /// and are never modifiers, <strong>and a reader that only tested the
    /// bit would call a bare <c>rescue</c> a modifier.</strong>
    /// </para>
    /// <para>
    /// <strong>And that <c>rescue</c> is never a modifier</strong> is
    /// measured at <c>parse.y</c> 353, where <c>bodystmt : compstmt
    /// opt_rescue opt_else opt_ensure</c>, <strong>and <c>rescue</c> is a
    /// <c>bodystmt</c> and never a modifier on a <c>stmt</c></strong>.
    /// </para>
    /// </remarks>
    public static bool IstModifier(RubyToken pToken)
    {
        if (pToken.Kind != RubyTokenKind.Keyword)
        {
            return false;
        }

        // **Und es sind fuenf und nicht vier, und das ist gemessen an
        // `lex.c` in Rubys eigenem Baum, Zeile 80 bis 120, und dort haben
        // genau fuenf Eintraege zwei verschiedene Token:**
        //
        // ```c
        // {"rescue", {kRESCUE, kRESCUE_MOD}, EXPR_MID},
        // {"if",     {kIF,     kIF_MOD},     EXPR_BEG},
        // {"until",  {kUNTIL,  kUNTIL_MOD},  EXPR_BEG},
        // {"unless", {kUNLESS, kUNLESS_MOD}, EXPR_BEG},
        // {"while",  {kWHILE,  kWHILE_MOD},  EXPR_BEG},
        // ```
        //
        // **Und alle anderen haben `{kX, kX}`** -- **`else`, `ensure`,
        // `elsif`, `def`, `module`, `class`, `case`, `begin`, `return`,
        // `break`, `next`, `nil`, `true`, `false`, `self` und die
        // uebrigen dreissig** -- **und bei denen ist die Wahl bei 4383
        // `if (kw->id[0] != kw->id[1])` falsch**, **und der Lexer gibt
        // beiden Zustaende denselben Token.**
        //
        // **Und `rescue` stand in dieser Liste in den ersten Runden nicht,
        // und `df5_bare_mit_arg_dann_rescue.rb` ist deshalb rot, und das
        // ist gemessen an `parse.y` 461:**
        //
        // ```text
        // 461  | stmt kRESCUE_MOD stmt
        // ```
        //
        // **Und `stmt kRESCUE_MOD stmt` ist ein Satz, kein Rumpf, und
        // damit ist `x = a rescue b` gueltig und `x = a` gefolgt von
        // `rescue => e` auf der naechsten Zeile ein Rumpf.**
        if (pToken.NewlineVorher)
        {
            return false;
        }

        // **Und `rescue` ist nicht wie die anderen vier, und das ist
        // gemessen an `lex.c` 86 in Rubys eigener Tabelle:**
        //
        // ```c
        // {"if",     {kIF,     kIF_MOD},     EXPR_BEG},
        // {"rescue", {kRESCUE, kRESCUE_MOD}, EXPR_MID},
        // ```
        //
        // **Und `EXPR_BEG` ist der Zustand, den `parse.y` 3349 hinter
        // *jedem* `tNL` setzt**, **und `EXPR_MID` ist der, den ein Wert
        // setzt** -- **und die Spalte rechts ist der Zustand, in dem das
        // Wort den Kopf seines eigenen Satzes eroeffnet, und nicht der,
        // in dem es ein Modifier ist.**
        //
        // **Und der Unterschied wird sichtbar an genau den zwei Formen,
        // die auseinanderfallen:**
        //
        // ```ruby
        // x = a rescue b      kRESCUE_MOD:  ein Modifier,  und das ist gueltig
        // begin
        //   a
        // rescue => e         kRESCUE:     ein Rumpf,     und das ist gueltig
        //   b
        // end
        // ```
        //
        // **Und `parse.y` 461 sagt `stmt kRESCUE_MOD stmt`, und 353 sagt
        // `bodystmt : compstmt opt_rescue opt_else opt_ensure`** -- **und
        // das sind zwei verschiedene Regeln fuer dasselbe Wort, und der
        // **Und der Unterschied ist der Zustand davor und nicht das Wort.**
        if (pToken.Text == "rescue")
        {
            return pToken.RescueIstModifier;
        }

        return pToken.Text is "if" or "unless" or "while" or "until";
    }
}

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
