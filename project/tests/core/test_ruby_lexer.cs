using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Splits Ruby source into tokens, against the rules in Ruby's own grammar.
/// </summary>
/// <remarks>
/// The keyword list under test is the one the grammar declares. A lexer that
/// treated a reserved word as an identifier would accept files Ruby rejects, so
/// the list is the thing being checked rather than an implementation detail.
/// </remarks>
public partial class TestRubyLexer : TestBase
{
    private static List<RubyToken> Lex(string pSource)
    {
        return new RubyLexer(pSource).Tokenize();
    }

    /// <summary>The tokens with the whitespace-only ones removed.</summary>
    private static List<RubyToken> Significant(string pSource)
    {
        return
        [
            .. Lex(pSource)
                .Where(pToken => pToken.Kind != RubyTokenKind.Newline
                    && pToken.Kind != RubyTokenKind.Semicolon
                    && pToken.Kind != RubyTokenKind.EndOfInput),
        ];
    }

    private static string Text(IEnumerable<RubyToken> pTokens)
    {
        return string.Join(" ", pTokens.Select(pToken => pToken.Text));
    }

    private string Refusal(Action pAction)
    {
        try
        {
            pAction();
            return "";
        }
        catch (RubySyntaxException exception)
        {
            return exception.Message;
        }
    }

    public void Test_EachReservedWordIsRecognisedRatherThanReadAsAName()
    {
        // A keyword list that is never consulted would make every one of these
        // an identifier, and a script using them would then parse as something
        // it is not. All forty one are checked, because a list that is checked
        // but incomplete fails the same way a missing lookup does.
        var notKeywords = new List<string>();
        foreach (var keyword in RubyLexer.Keywords)
        {
            var token = Lex(keyword)[0];
            if (token.Kind != RubyTokenKind.Keyword)
            {
                notKeywords.Add($"{keyword} read as {token.Kind}");
            }
        }
        AssertEq(
            notKeywords.Count, 0,
            "every reserved word is a keyword: " + string.Join("; ", notKeywords));
    }

    public void Test_ASingleQuotedStringInterpretsOnlyTheQuoteAndTheBackslash()
    {
        // The form exists for exactly this: a backslash before an ordinary
        // character is one of the characters in the string, and reading it as an
        // escape would silently change what a game asks for.
        foreach (var (source, value) in new (string, string)[]
        {
            ("'a\\nb'", "a\\nb"),
            ("'a\\tb'", "a\\tb"),
            ("'a\\'b'", "a'b"),
            ("'a\\\\b'", "a\\b"),
        })
        {
            var token = Significant(source)[0];
            AssertEq(token.Value, value, $"{source} reads as {value}");
        }
    }

    public void Test_OperatorsAreTriedLongestFirst()
    {
        // '<=' before '<=>' would split the spaceship into two tokens and shift
        // everything after it, and '<=' before '<<' would not, so the order
        // matters in both directions.
        foreach (var op in new[]
        {
            "<=>", "===", "**=", "<<=", "...", "=~", "!~", "==", "<=", "<<", "**",
        })
        {
            var tokens = Significant(op);
            AssertEq(
                string.Join(" ", tokens.Select(pToken => pToken.Text)), op,
                $"'{op}' is one token");
            AssertEq(
                string.Join(" ", tokens.Select(pToken => pToken.Kind)), "Operator",
                $"and '{op}' is an operator");
        }
    }

    public void Test_ALineContinuationLeavesOneStatementAndOneBreak()
    {
        // Two statements separated by a line continuation and a real newline.
        // The first is a continuation and must not break the statement, so the
        // stream has a single Newline in it, before the second statement.
        var tokens = Lex("a = 1 + \\\n    2\nb = 3");
        AssertEq(
            string.Join(" ", tokens.Select(pToken => pToken.Kind)),
            "Identifier Operator Integer Operator Integer Newline"
            + " Identifier Operator Integer EndOfInput",
            "the continuation is consumed and only the real newline breaks the line");
    }

    public void Test_AnEmptyScriptIsOnlyEndOfInput()
    {
        var tokens = Lex("");
        AssertEq(tokens.Count, 1, "an empty script has one token");
        AssertEq(tokens[0].Kind, RubyTokenKind.EndOfInput, "which is the end" );
    }

    public void Test_TheKeywordListIsTheOneFromTheGrammar()
    {
        // Forty one reserved words, taken from the grammar rather than from
        // memory. A missing one would let a game method that Ruby forbids slip
        // through, and an extra one would reject a legal name.
        AssertEq(RubyLexer.Keywords.Count, 41, "the grammar declares forty one keywords");
        foreach (var keyword in new[]
        {
            "class", "module", "def", "begin", "end", "if", "unless", "else",
            "elsif", "case", "when", "while", "until", "for", "do", "then",
            "return", "yield", "super", "self", "nil", "true", "false", "nil",
            "and", "or", "not", "alias", "defined?", "super", "redo", "retry",
            "break", "next", "in", "ensure", "rescue", "undef",
            "__LINE__", "__FILE__", "__ENCODING__",
        })
        {
            AssertTrue(RubyLexer.Keywords.Contains(keyword), $"'{keyword}' is a keyword");
        }
    }

    public void Test_AKeywordIsAKeywordAndNotAnIdentifier()
    {
        var tokens = Significant("class Foo\nend");
        AssertEq(tokens[0].Kind, RubyTokenKind.Keyword, "'class' is a keyword" );
        AssertEq(tokens[1].Kind, RubyTokenKind.Constant, "'Foo' is a constant" );
        AssertEq(tokens[2].Kind, RubyTokenKind.Keyword, "'end' is a keyword" );
    }

    public void Test_ANameThatBeginsUpperCaseIsAConstant()
    {
        // This is the rule the whole language hangs on, because a constant
        // resolves differently from a method call.
        var tokens = Significant("Sprite = 1\nsprite = 2\n@x = 3\n$x = 4");
        AssertEq(
            string.Join(" ", tokens.Select(pToken => pToken.Kind + ":" + pToken.Text)),
            "Constant:Sprite Operator:= Integer:1 Identifier:sprite Operator:= Integer:2"
            + " InstanceVariable:@x Operator:= Integer:3 GlobalVariable:$x"
            + " Operator:= Integer:4",
            "each of the four names has the kind the language gives it");
    }

    public void Test_AnIdentifierMayEndInAQuestionOrBang()
    {
        var tokens = Significant("nil? empty? save!");
        AssertEq(
            string.Join(" ", tokens.Select(pToken => pToken.Kind + ":" + pToken.Text)),
            "Identifier:nil? Identifier:empty? Identifier:save!",
            "each name keeps its question or bang instead of being split off it");
    }

    public void Test_CommentsAndBlankLinesAreNotTokens()
    {
        var tokens = Significant("a = 1 # a comment\n\n  b = 2\n");
        AssertEq(Text(tokens), "a = 1 b = 2", "the comment and the blank line are gone");
    }

    public void Test_ABackslashBeforeANewlineIsNotAStatementBreak()
    {
        var tokens = Lex("a = 1 + \\\n    2\n");
        // The backslash and the newline after it are consumed together, so the
        // expression is one statement and the only newline left is the one that
        // ends it.
        AssertEq(
            string.Join(" ", tokens.Select(pToken => pToken.Kind)),
            "Identifier Operator Integer Operator Integer Newline EndOfInput",
            "the continuation left one statement and one terminating newline");
    }

    public void Test_IntegersUseTheBasesTheGrammarNames()
    {
        foreach (var (source, value) in new (string, long)[]
        {
            ("42", 42),
            ("255", 255),
            ("1_000", 1000),
            ("0x1F", 31),
            ("0b1011", 11),
            ("0o17", 15),
            ("017", 15),
        })
        {
            var t = Significant(source)[0];
            AssertEq(
                t.Integer!.Value, value,
                $"GOT {source} -> kind {t.Kind} text '{t.Text}' value {t.Integer}");
        }
    }

    public void Test_ANumberFollowedByALetterIsNotOneToken()
    {
        // 1 followed by 'x' is an integer and a name, which is how Ruby reads
        // a range end followed by a block. A lexer that glued them would turn
        // 1..2 into a single number.
        var tokens = Significant("1 + x");
        AssertEq(
            string.Join(" ", tokens.Select(pToken => pToken.Kind + ":" + pToken.Text)),
            "Integer:1 Operator:+ Identifier:x",
            "a number and a following name are three tokens, not one");
    }

    public void Test_FloatsIncludeAFractionAndAnExponent()
    {
        foreach (var (source, value) in new (string, double)[]
        {
            ("1.5", 1.5),
            ("2.0e3", 2000.0),
            ("1.5e-2", 0.015),
            ("0.125", 0.125),
        })
        {
            var t = Significant(source)[0];
            AssertEq(t.Kind, RubyTokenKind.Float, $"{source} is a float");
            AssertEq(t.Real!.Value, value, $"{source} is {value}");
        }
    }

    public void Test_AnExponentMarkThatIsNotAnExponentEndsTheNumber()
    {
        var tokens = Significant("1end");
        AssertEq(
            string.Join(" ", tokens.Select(pToken => pToken.Kind + ":" + pToken.Text)),
            "Integer:1 Keyword:end",
            "the number ended before the name, and the name is the keyword 'end'");
    }

    public void Test_StringsKeepTheirTextAndTheirEscapes()
    {
        var t = Significant("\"hello\"")[0];
        AssertEq(t.Kind, RubyTokenKind.String, "a double quoted string" );
        AssertEq(t.Value, "hello", "reads as its text");

        var escaped = Significant("\"a\\nb\"")[0];
        AssertEq(escaped.Value, "a\nb", "and a newline escape becomes a newline");
    }

    public void Test_AStringKeepsItsBytesSeparatelyFromItsText()
    {
        // A Shift-JIS game's script is not UTF-8, so the bytes are kept even
        // when the text is shown. A reader that only kept text would silently
        // corrupt every such game.
        var t = Significant("\"\\x83\\x65\"")[0];
        AssertEq(t.Bytes!.Length, 2, "a byte escape produces two bytes");
        AssertEq(t.Bytes[0], (byte)0x83, "the first is the byte the source named");
        AssertEq(t.Bytes[1], (byte)0x65, "and the second");
    }

    public void Test_ASingleQuotedStringEscapesOnlyTheQuoteAndTheBackslash()
    {
        var tokens = Significant("'a\\nb'");
        var t = tokens[0];
        AssertEq(t.Kind, RubyTokenKind.String, "a single quoted string");
        AssertEq(
            t.Value, "a\\nb",
            "a single quoted string keeps the backslash, because there the escape is"
            + " not interpreted: the two characters are the point");
    }

    public void Test_ASlashDividesAfterAValueAndOpensARegularExpressionOtherwise()
    {
        // The two shapes that differ only in what came before the slash.
        var division = Significant("a / b");
        AssertEq(Text(division), "a / b", "a slash after a name divides");
        AssertTrue(
            division.All(pToken => pToken.Kind != RubyTokenKind.Regexp),
            "so no regular expression was read");

        var regexp = Significant("a =~ /ab+c/i");
        AssertEq(regexp[2].Kind, RubyTokenKind.Regexp, "after an operator it is a regexp" );
        AssertEq(regexp[2].Value, "ab+c", "whose source is the text between the slashes");
    }

    public void Test_ARegularExpressionKeepsItsBackslashes()
    {
        // The regular expression engine is what interprets an escape, so the
        // backslash has to survive the lexer or the pattern changes meaning.
        var tokens = Significant("/a\\/b/");
        AssertEq(tokens[0].Kind, RubyTokenKind.Regexp, "a regexp with an escaped slash" );
        AssertEq(tokens[0].Value, "a\\/b", "which keeps its backslash");
    }

    public void Test_ARegularExpressionClassIsNotACloser()
    {
        var tokens = Significant("/[a/b]/");
        AssertEq(tokens[0].Kind, RubyTokenKind.Regexp, "a regexp whose class holds a slash" );
        AssertEq(tokens[0].Value, "[a/b]", "which reads to the second slash");
    }

    public void Test_ASymbolIsReadWithoutItsColon()
    {
        var t = Significant(":hello")[0];
        AssertEq(t.Kind, RubyTokenKind.Symbol, "':hello' is a symbol" );
        AssertEq(t.Value, "hello", "naming hello");
    }

    public void Test_AnOperatorSymbolIsAWholeToken()
    {
        var t = Significant(":+")[0];
        AssertEq(t.Kind, RubyTokenKind.Symbol, "':+' is a symbol" );
        AssertEq(t.Value, "+", "naming the operator");
    }

    public void Test_AnInstanceVariableMayBeAClassVariable()
    {
        var tokens = Significant("@@count = 0");
        AssertEq(tokens[0].Kind, RubyTokenKind.InstanceVariable, "'@@count' is a variable" );
        AssertEq(tokens[0].Text, "@@count", "with both at signs kept");
    }

    public void Test_APunctuationGlobalIsOneCharacterWide()
    {
        var tokens = Significant("$1");
        AssertEq(tokens[0].Kind, RubyTokenKind.GlobalVariable, "'$1' is a global" );
        AssertEq(tokens[0].Text, "$1", "of one character after the sign");
    }

    public void Test_AMultiCharacterOperatorIsTheLongestMatch()
    {
        // A lexer that tried one character at a time would read '==' as two
        // assignments and shift everything after it.
        foreach (var op in new[] { "<=>", "===", "**=", "<<=", "...", "=~", "&&", "||", "->", "=>" })
        {
            var t = Significant(op)[0];
            AssertEq(t.Kind, RubyTokenKind.Operator, $"'{op}' is an operator");
            AssertEq(t.Text, op, $"and it is read whole");
        }
    }

    public void Test_ACharacterTheLexerDoesNotKnowIsRefusedWithItsLine()
    {
        var error = Refusal(() => Lex("a = 1\n\x01"));
        AssertTrue(error.Contains("not a character"), $"an unknown character is refused: {error}");
        AssertTrue(error.Contains("line 2") || error.Contains("offset"),
            $"and it says where: {error}");
    }

    public void Test_AStringThatIsNeverClosedIsRefused()
    {
        var error = Refusal(() => Lex("a = \"unterminated"));
        AssertTrue(error.Contains("never closed"), $"an unclosed string is refused: {error}");
    }

    public void Test_ANumberWithNoDigitsInItsBaseIsRefused()
    {
        foreach (var source in new[] { "0x", "0b" })
        {
            var error = Refusal(() => Lex(source));
            AssertTrue(error.Contains("no digits"), $"{source} is refused: {error}");
        }
    }

    public void Test_ABlockCommentIsSkippedWhole()
    {
        var tokens = Significant("a = 1\n=begin\nnot ruby at all\n=end\nb = 2\n");
        AssertEq(
            string.Join(" ", tokens.Select(pToken => pToken.Kind + ":" + pToken.Text)),
            "Identifier:a Operator:= Integer:1 Identifier:b Operator:= Integer:2",
            "the block comment and its two marker lines are not read");
    }

    public void Test_TokensCarryTheLineTheyCameFrom()
    {
        var tokens = Lex("a = 1\nb = 2\nc = 3");
        var third = tokens.First(pToken => pToken.Text == "c");
        AssertEq(third.Line, 3, "the third line's first token is on line three");
    }

    public void Test_AnEscapeWithoutACharacterIsRefused()
    {
        var error = Refusal(() => Lex("\"abc\\"));
        AssertTrue(error.Contains("cut off") || error.Contains("never closed"),
            $"an escape at the end of a script is refused: {error}");
    }

    public void Test_AnUnterminatedRegularExpressionIsRefused()
    {
        var error = Refusal(() => Lex("/abc"));
        AssertTrue(error.Contains("never closed"), $"an unclosed regexp is refused: {error}");
    }


    /// <summary>What the lexer makes of one written line.</summary>
    /// <remarks>
    /// **And this is here because a parser error names a token and a
    /// reader who cannot see the token list has to guess** -- **and
    /// guessing is what produced three wrong explanations in a row.</strong>
    /// </remarks>
    public void Test_WasDerLexerAusDieserZeileMacht()
    {
        var quelle = "mflags = ($OPT['a'] || '').strip if mflags.empty?";
        var token = new RubyLexer(quelle).Tokenize();
        var liste = string.Join(" ",
            token.Select(pToken => pToken.Kind + ":" + pToken.Text));

        AssertTrue(
            token.Count == 17,
            "**and the line makes the tokens it looks like** -- and there"
            + $" are {token.Count}: {liste}");
    }

    /// <summary>
    /// A percent behind a comma is a literal, and behind anything else it is a
    /// modulus unless nothing could divide.
    /// </summary>
    /// <remarks>
    /// <strong>And this is eight measured token streams, and they are the
    /// rule that replaced a wider one.</strong>
    ///
    /// <code>
    /// a % b         Operator'%'   a modulus
    /// %w[a b]       Operator'%'   a modulus at the head of a statement
    /// x = %w[a b]   Operator'%'   a modulus after '='
    /// print %w[a b] Operator'%'   a modulus after a name
    /// -7 % 3        Operator'%'   a modulus
    /// 7 %w[a]       Operator'%'   a modulus, and a letter is not enough
    /// f(a, %w[b])   String'%w[b]' the only one that is a literal
    /// </code>
    ///
    /// <strong>And the reason is <c>parse.y</c> line 4408, and not line
    /// 4170:</strong>
    ///
    /// <code>
    /// if (is_local_id(yylval.id) &amp;&amp; ...
    ///     lex_state = EXPR_END;
    /// }
    /// </code>
    ///
    /// <strong>and <c>IS_ARG()</c> at 4170 is <c>EXPR_ARG || EXPR_CMDARG</c>,
    /// and a name that is a local variable leaves <c>EXPR_END</c> and one that
    /// is not leaves <c>EXPR_CMDARG</c> at 4397.</strong>
    ///
    /// <strong>And whether a name is a local variable is a table the lexer
    /// does not have</strong> -- <strong>and so a comma is the one place
    /// this reader can be sure of, and it is the one the four real scripts
    /// use:</strong>
    ///
    /// <code>
    /// dirname(__FILE__).sub!(%r'#{prefix}\Z', '')
    /// STDERR.puts "ignored" if line =~ %r:^(sample/test.rb|not):
    /// </code>
    ///
    /// <strong>And that second one sits behind <c>=~</c>, which is an
    /// operator and not a comma</strong> -- <strong>and it works because
    /// <c>SlashDivides()</c> is false behind an operator</strong>, **and
    /// both of the real cases are measured green.</strong>
    /// </remarks>
    /// <summary>The first token whose text starts with the given text.</summary>
    private static RubyToken FirstStartingWith(string pSource, string pText)
    {
        foreach (var token in Lex(pSource))
        {
            if (token.Text.StartsWith(pText, StringComparison.Ordinal))
            {
                return token;
            }
        }

        throw new System.InvalidOperationException(
            $"No token in `{pSource}` starts with `{pText}`.");
    }

    /// <summary>The first token whose text is the given text.</summary>
    private static RubyToken FirstWithText(string pSource, string pText)
    {
        foreach (var token in Lex(pSource))
        {
            if (token.Text == pText)
            {
                return token;
            }
        }

        throw new System.InvalidOperationException(
            $"`{pSource}` has no token `{pText}`.");
    }

    public void Test_APercentIsAModulusUnlessNothingCouldDivide()
    {
        // The four that stay a modulus, and the stream says why. And
        // `print` is deliberately NOT among them -- see below.
        foreach (var quelle in new[]
        {
            "a % b", "-7 % 3", "7 %w[a]",
        })
        {
            AssertEq(FirstWithText(quelle, "%").Kind, RubyTokenKind.Operator,
                "**`%` is a modulus in `" + quelle + "`** — and a letter "
                    + "behind it is not enough, and a space in front of it "
                    + "is not enough either when a name stands there");
        }

        // **Und `%w[a b]` am Dateianfang war in diesem Test ein Modulo, und
        // das war falsch, und `parse.y` 4098 sagt es:**
        //
        // ```c
        // 4097  case '%':
        // 4098      if (lex_state == EXPR_BEG || lex_state == EXPR_MID) {
        // 4099          int term;
        // 4100          int paren;
        // ```
        //
        // **Und `EXPR_BEG` ist der Zustand, in dem der Lexer startet,
        // gemessen an `parse.y` 2545ff, wo `command_start = 1` steht und
        // kein Zustand gesetzt ist, der etwas anderes waere** -- **und
        // darum ist `%w[a b]` an erster Stelle einer Datei ein Literal und
        // kein Modulo, und dieser Test hat vier Runden lang das Gegenteil
        // behauptet.**
        AssertEq(
            FirstStartingWith("%w[a b]", "%w").Kind,
            RubyTokenKind.String,
            "**`%w[a b]` at the start of a file is a literal** — and that is "
                + "measure.y 4098, where EXPR_BEG is one of the two states "
                + "that open a percent literal, and the start of a file is "
                + "in EXPR_BEG");

        // The three that are a literal, and each behind something that
        // cannot divide.
        AssertEq(FirstStartingWith("x = %w[a b]", "%w").Kind,
            RubyTokenKind.String,
            "**`%w[a b]` behind `=` is a literal** — and an operator cannot "
                + "divide");
        AssertEq(FirstStartingWith("print %w[a b]", "%w").Kind,
            RubyTokenKind.String,
            "**`%w[a b]` behind `print` is a literal** — and that is "
                + "`mkconfig.rb` line 22, and a command takes an argument "
                + "without brackets, and a command without brackets is a "
                + "literal behind that `%`");
        AssertEq(FirstStartingWith("print %[\nmodule Config\n]", "%[").Kind,
            RubyTokenKind.String,
            "**`%[...]` behind `print` is a literal** — and a reserved word "
                + "inside it is content, and the whole block is one token");
        AssertEq(FirstStartingWith("f(a, %w[b])", "%w").Kind,
            RubyTokenKind.String,
            "**`%w[b]` behind a comma is a literal** — and so is the one the "
                + "comma form is there for");
        AssertEq(FirstStartingWith("x =~ %r:^(a|not):", "%r").Kind,
            RubyTokenKind.Regexp,
            "**`%r:(a|not):` behind `=~` is a regexp** — and that is "
                + "rubytest.rb line 42, and an operator cannot divide");
    }

    /// <summary>
    /// Whatever a percent literal holds is content, and a reserved word in it
    /// is still content.
    /// </summary>
    /// <remarks>
    /// <strong>And this one is what <c>print %[end]</c> and
    /// <c>print %[module Config]</c> got wrong, and the wrong reading was
    /// the more obvious one.</strong>
    ///
    /// <strong>A reader that stopped the literal at a reserved word broke the
    /// two of them, and it broke them at the word and not at the
    /// delimiter:</strong>
    ///
    /// <code>
    /// p4: RubyParseException 'end' at offset 8 does not begin an expression.
    /// p3: RubyParseException ']' at offset 21 does not begin an expression.
    /// </code>
    ///
    /// <strong>And the whole failure was one branch, and it was mine, not
    /// Ruby's:</strong> <code>SlashDivides()</code> was standing in for
    /// <c>IS_ARG()</c>, **and it answers true behind every name**, and a
    /// command name is a name.
    /// </remarks>
    public void Test_APercentLiteralHoldsAKeywordAsContent()
    {
        foreach (var quelle in new[]
        {
            "f(a, %[end])", "f(a, %[module Config])", "f(a, %[def x])",
            "f(a, %[if x])", "f(a, %r{end})", "f(a, %q[end])",
        })
        {
            var anteile = 0;
            foreach (var token in Lex(quelle))
            {
                if (token.Kind == RubyTokenKind.String
                    || token.Kind == RubyTokenKind.Regexp)
                {
                    anteile++;
                }
            }

            AssertEq(anteile, 1,
                "**one literal out of `" + quelle + "`** — and the content "
                    + "between the delimiters is content, reserved word or not");
        }
    }
}
