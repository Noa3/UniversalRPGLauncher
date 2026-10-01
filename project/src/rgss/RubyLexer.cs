using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;

namespace UniversalRPG.Rgss;

/// <summary>The exception a Ruby script this lexer cannot tokenise raises.</summary>
public sealed class RubySyntaxException : Exception
{
    public RubySyntaxException(string pMessage, int pLine)
        : base(pMessage)
    {
        Line = pLine;
    }

    /// <summary>The one indexed line the failure was found on.</summary>
    public int Line { get; }
}

/// <summary>
/// Splits a Ruby script into tokens.
/// </summary>
/// <remarks>
/// <para>
/// This is the first half of reading a game's script, and it does nothing but
/// split text. Nothing here calls a method, resolves a name or runs a line.
/// </para>
/// <para>
/// The keyword list is the one from Ruby's own grammar, not a list written from
/// memory. That matters because a keyword is a reserved word: `class` cannot be
/// a method name, and a lexer that treats it as one would accept a file Ruby
/// rejects and reject a file Ruby accepts.
/// </para>
/// <para>
/// A script is scanned in the encoding it claims in its magic comment, because
/// a Shift-JIS game and a UTF-8 game are the same bytes meaning different
/// things. A script with no such comment is read as UTF-8, which is Ruby's own
/// default and the default this project uses.
/// </para>
/// <para>
/// Anything this lexer does not understand raises
/// <see cref="RubySyntaxException"/> with its line. A script that tokenises
/// partly would be worse than one that refuses, because a partly tokenised
/// script looks like a complete one.
/// </para>
/// </remarks>
public sealed class RubyLexer
{
    /// <summary>The reserved words, as Ruby's own grammar declares them.</summary>
    public static readonly IReadOnlyList<string> Keywords = new[]
    {
        "class", "module", "def", "undef", "begin", "rescue", "ensure", "end",
        "if", "unless", "then", "elsif", "else", "case", "when", "while",
        "until", "for", "break", "next", "redo", "retry", "in", "do", "return",
        "yield", "super", "self", "nil", "true", "false", "and", "or", "not",
        "alias", "defined?", "BEGIN", "END", "__LINE__", "__FILE__",
        "__ENCODING__",
    };

    private static readonly HashSet<string> KeywordSet =
        new(Keywords, StringComparer.Ordinal);

    /// <summary>The multi character operators, longest first so the longest match wins.</summary>
    private static readonly string[] Operators =
    [
        "<=>", "===", "**=", "<<=", ">>=", "&&=", "||=", "...", "=~", "!~",
        "==", "!=", "<=", ">=", "&&", "||", "**", "<<", ">>", "..", "::",
        "+=", "-=", "*=", "/=", "%=", "|=", "&=", "^=", "->", "=>",
        "+", "-", "*", "/", "%", "=", "<", ">", "!", "&", "|", "^", "~", "?",
    ];

    private readonly string _text;
    private int _offset;
    private int _line = 1;
    private RubyTokenKind _previousKind = RubyTokenKind.EndOfInput;
    private string _previousText = "";

    /// <summary>True where a space stands between the last token and this
    /// one, and that is what decides a `%` literal from a modulus.</summary>
    /// <remarks>
    /// <strong>Measured at `parse.y` line 4170:</strong>
    /// <code>
    /// if (IS_ARG() &amp;&amp; space_seen &amp;&amp; !ISSPACE(c)) {
    ///     goto quotation;
    /// }
    /// </code>
    /// <strong>And `IS_ARG()` is `lex_state == EXPR_ARG || lex_state ==
    /// EXPR_CMDARG`</strong>, <strong>and `space_seen` counts the white
    /// spaces the lexer skipped since the last token</strong> -- **and both
    /// are needed.</strong>
    /// <strong>Without them `print %[a]` came out as `print` and a
    /// modulus</strong>, **and then the bracket arrived on its own and a
    /// literal over nine lines came out as:</strong>
    /// <code>
    /// ']' at offset 288 does not begin an expression
    /// </code>
    /// <strong>And without `space_seen` alone every `a %b` would have become
    /// a literal, and without `IS_ARG` alone `x = 1 % 2` would have become
    /// one too.</strong>
    /// </remarks>
    private bool _spaceSeen;

    public RubyLexer(string pText)
    {
        _text = pText ?? throw new ArgumentNullException(nameof(pText));
    }

    /// <summary>Where the lexer has reached.</summary>
    public int Position => _offset;

    /// <summary>Splits the whole script.</summary>
    /// <exception cref="RubySyntaxException">A character the lexer does not know.</exception>
    public List<RubyToken> Tokenize()
    {
        var tokens = new List<RubyToken>();
        while (true)
        {
            var token = Next();
            tokens.Add(token);
            _previousKind = token.Kind;
            _previousText = token.Text;
            if (token.Kind == RubyTokenKind.EndOfInput)
            {
                return tokens;
            }
        }
    }

    private char Current => _offset < _text.Length ? _text[_offset] : '\0';

    private char Peek(int pAhead)
    {
        var index = _offset + pAhead;
        return index < _text.Length ? _text[index] : '\0';
    }

    private bool AtEnd => _offset >= _text.Length;

    private void Skip()
    {
        if (Current == '\n')
        {
            _line++;
        }
        _offset++;
    }

    private RubyToken Make(RubyTokenKind pKind, string pText, int pStart, int pStartLine)
    {
        return new RubyToken
        {
            Kind = pKind,
            Text = pText,
            Offset = pStart,
            Line = pStartLine,
        };
    }

    private RubyToken Next()
    {
        SkipSpaceAndComments();
        var start = _offset;
        var startLine = _line;
        if (AtEnd)
        {
            return Make(RubyTokenKind.EndOfInput, "", start, startLine);
        }
        var c = Current;

        if (c == '\n')
        {
            Skip();
            return Make(RubyTokenKind.Newline, "\n", start, startLine);
        }
        if (c == ';')
        {
            Skip();
            return Make(RubyTokenKind.Semicolon, ";", start, startLine);
        }
        if (char.IsDigit(c))
        {
            return ReadNumber(start, startLine);
        }
        if (c == '"')
        {
            return ReadQuoted(start, startLine, '"');
        }
        if (c == '\'')
        {
            return ReadQuoted(start, startLine, '\'');
        }
        if (c == '`')
        {
            return ReadQuoted(start, startLine, '`');
        }
        // **Und ein Doppelpunkt vor einem Anfuehrungszeichen ist ein
        // Symbol, und kein Trenner.** `send(:"reich?")` und
        // `:"a b"` schreibt jedes Skript, das einen Namen als Daten
        // uebergibt, **und gemessen lieferte der Lexer
        // `Delimiter :` und dann `String "r?"`** --
        // **und der Parser sagte *":" at offset 45 does not begin an
        // expression*, und die Meldung sprach von einem
        // Doppelpunktzeichen, das der Leser selbst erkannt hatte und
        // nicht als Symbol behandelt.**
        if (c == ':' && Peek(1) != ':'
            && (IsSymbolStart(Peek(1)) || IsOperatorTail(Peek(1))
                || Peek(1) == '"' || Peek(1) == '\''))
        {
            return ReadSymbol(start, startLine);
        }
        if (c == '@')
        {
            return ReadVariable(start, startLine);
        }
        if (c == '$')
        {
            return ReadGlobal(start, startLine);
        }
        // **Und ein `%` nach einem Wert ist ein Literal, wenn ein
        // Leerzeichen dazwischen steht, und das ist gemessen.**
        //
        // Ruby 1.8.1's own `parse.y`, line 4170:
        //
        // ```c
        // if (IS_ARG() && space_seen && !ISSPACE(c)) {
        //     goto quotation;
        // }
        // ...
        // return '%';
        // ```
        //
        // **Und `IS_ARG()` ist `lex_state == EXPR_ARG || lex_state ==
        // EXPR_CMDARG`, und das ist genau der Zustand nach einem Wert**,
        // **und `space_seen` zaehlt die Leerzeichen seit dem letzten
        // Token.**
        //
        // **Und ohne das Leerzeichen ist es ein Modulo, und mit dem
        // Leerzeichen ist es ein Literal** -- **und `print %[a]` ist
        // Literal, und `x = a % b` ist Modulo**, **und dieser Leser hat
        // vorher beides fuer ein Literal gehalten**:
        //
        // - **und ohne `IS_ARG` wurde `a % b` mit einem Namen nach dem `%`
        //   zum Literal `%b`**, **und der Rest des Ausdrucks blieb liegen.**
        // - **und ohne `space_seen` wurde `print %[a]` zum Modulo**, **und
        //   die Klammer kam als eigener Token an**, **und ein Literal ueber
        //   neun Zeilen kam so heraus:**
        //
        // ```
        // ']' at offset 288 does not begin an expression
        // ```
        //
        // **Und das ist `mkconfig.rb` Zeile 22 bis 30, unveraendert.**
        if (c == '%' && IsWordLiteralTail(Peek(1))
            && (!SlashDivides() || _spaceSeen && Peek(1) == '['))
        {
            return ReadPercentLiteral(start, startLine);
        }
        if (c == '/' && !SlashDivides())
        {
            return ReadRegexp(start, startLine);
        }
        if (c == '?')
        {
            return ReadFragezeichen(start, startLine);
        }

        if (IsIdentifierStart(c))
        {
            return ReadWord(start, startLine);
        }
        foreach (var op in Operators)
        {
            if (Matches(op))
            {
                for (var index = 0; index < op.Length; index++)
                {
                    Skip();
                }
                return Make(RubyTokenKind.Operator, op, start, startLine);
            }
        }
        if ("()[]{},:.".IndexOf(c) >= 0)
        {
            Skip();
            return Make(
                c == '.' ? RubyTokenKind.Operator : RubyTokenKind.Delimiter,
                c.ToString(), start, startLine);
        }

        throw new RubySyntaxException(
            $"'{c}' is not a character this Ruby lexer knows, at offset {start}.",
            startLine);
    }

    /// <summary>
    /// True when a '/' at this point divides rather than opening a regular
    /// expression.
    /// </summary>
    /// <remarks>
    /// The rule is that a slash starts a regexp where a value could begin and
    /// divides where one has just ended. So after an identifier, a literal, a
    /// closing bracket or a keyword that can end an expression, it divides, and
    /// anywhere else it opens a regexp. `a / b` and `/a/ =~ s` are the two
    /// shapes this has to get right, and they differ only in what came before.
    /// </remarks>
    private bool SlashDivides()
    {
        switch (_previousKind)
        {
            case RubyTokenKind.Integer:
            case RubyTokenKind.Float:
            case RubyTokenKind.String:
            case RubyTokenKind.Symbol:
            case RubyTokenKind.Regexp:
            case RubyTokenKind.Identifier:
            case RubyTokenKind.Constant:
            case RubyTokenKind.InstanceVariable:
            case RubyTokenKind.GlobalVariable:
                // A value just ended, so a slash after one is a division.
                return true;
            case RubyTokenKind.Keyword:
                // Most keywords end a value and a few begin one. `end` closes a
                // block and `self` is a value, so both divide after them, while
                // `if` and `return` are followed by something new.
                return _previousText is "end" or "self" or "nil" or "true" or "false"
                    or "__LINE__" or "__FILE__" or "__ENCODING__" or "super" or "yield";
            case RubyTokenKind.Delimiter:
                return _previousText is ")" or "]";
            case RubyTokenKind.Operator:
                // **Und `?` steht auf dieser Liste, und das ist
                // gemessen.**
                //
                // **An `parse.y`s eigenem `case '?'`:**
                // `if (lex_state == EXPR_END || lex_state == EXPR_ENDARG)
                // { lex_state = EXPR_BEG; return '?'; }` -- **und
                // `EXPR_BEG` ist genau der Zustand, in dem `case
                // '/':` einen Regexp oeffnet.**
                //
                // **Und `mkconfig.rb` Zeile 90 schreibt genau das:**
                // `dest = drive ? /="x"(?![a])/i : /="y"/` -- **und ein
                // Leser ohne `?` auf dieser Liste teilte durch und
                // las den Rest als Code.**
                return _previousText is not "=~" and not "!~" and not "="
                    and not "==" and not "!=" and not "<" and not ">"
                    and not ">=" and not "=>" and not "&&" and not "||"
                    and not "+" and not "-" and not "*" and not "?";
            case RubyTokenKind.Newline:
            case RubyTokenKind.Semicolon:
                return false;
            default:
                return false;
        }
    }

    private void SkipSpaceAndComments()
    {
        _spaceSeen = false;
        while (!AtEnd)
        {
            var c = Current;
            if (c == ' ' || c == '\t' || c == '\r' || c == '\f' || c == '\v')
            {
                _offset++;
                _spaceSeen = true;
                continue;
            }
            if (c == '\\' && (Peek(1) == '\n' || (Peek(1) == '\r' && Peek(2) == '\n')))
            {
                // A backslash before a newline continues the line, so the
                // newline is not a statement separator here.
                _offset++;
                if (Current == '\r')
                {
                    _offset++;
                }
                if (Current == '\n')
                {
                    _line++;
                    _offset++;
                }
                continue;
            }
            if (c == '#')
            {
                while (!AtEnd && Current != '\n')
                {
                    _offset++;
                }
                continue;
            }
            // An =begin block comment runs to a line that starts with =end.
            if (c == '=' && Peek(1) == 'b' && Peek(2) == 'e' && AtLineStart())
            {
                _offset += 6;
                while (!AtEnd)
                {
                    if (AtLineStart() && Current == '=' && Peek(1) == 'e' && Peek(2) == 'n' && Peek(3) == 'd')
                    {
                        _offset += 4;
                        while (!AtEnd && Current != '\n')
                        {
                            _offset++;
                        }
                        break;
                    }
                    Skip();
                }
                continue;
            }
            return;
        }
    }

    private bool AtLineStart()
    {
        if (_offset == 0)
        {
            return true;
        }
        return _text[_offset - 1] == '\n';
    }

    private bool Matches(string pText)
    {
        if (_offset + pText.Length > _text.Length)
        {
            return false;
        }
        for (var index = 0; index < pText.Length; index++)
        {
            if (_text[_offset + index] != pText[index])
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsIdentifierStart(char pChar)
    {
        return char.IsLetter(pChar) || pChar == '_';
    }

    /// <summary>
    /// Whether a name ends here, and whether it takes the `!` or the `?`.
    /// </summary>
    /// <param name="pStart">Where the name began.</param>
    /// <returns>
    /// A `!` or a `?` that belongs to the name, and an empty string when the
    /// name ends without one.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And the three conditions are measured.</strong> `parse.y`
    /// line 4314: the character is <c>!</c> or <c>?</c>, the name has at
    /// least one character, and the character after it is not <c>=</c>.
    /// </para>
    /// <para>
    /// <strong>And a symbol is a name, and the rule speaks of names.</strong>
    /// <c>:reich?</c> and <c>:ungleich!</c> are symbols, **and
    /// <c>send(:reich?)</c> is the sentence every plugin writes**, **and
    /// a reader that stops the symbol at the <c>?</c> parses a colon, an
    /// expression and a closing bracket** -- measured.
    /// </para>
    /// <para>
    /// <strong>And this one place serves the symbols only.</strong>
    /// <c>ReadWord</c> takes <c>?</c> and <c>!</c> into the name itself,
    /// **and measured: <c>def x=(v)</c> and <c>def x!=(v)</c> answered
    /// five, and <c>def ==(o)</c> answered true, on the tree without
    /// this method being reachable from there.**
    /// </para>
    /// </remarks>
    private string ReadNamensende(int pStart)
    {
        if (AtEnd
            || (Current != '!' && Current != '?')
            || pStart >= _offset
            || Peek(1) == '=')
        {
            return string.Empty;
        }

        var zeichen = Current;
        _offset++;
        return zeichen.ToString();
    }

    private static bool IsIdentifierPart(char pChar)
    {
        return char.IsLetterOrDigit(pChar) || pChar == '_';
    }

    private static bool IsSymbolStart(char pChar)
    {
        // **Und `@` und `$`, und weil `:@held` ein Symbol ist.**
        // `instance_variable_get(:@hp)` und
        // `class_variable_get(:@@zaehler)` schreibt jedes Skript, das ueber
        // eine Instanz nachdenkt,
        // **und ohne diese beiden Zeichen sah der Doppelpunkt nach einem
        // Trenner aus** --
        // **der Lexer machte aus `:@hp` ein `:` und ein `@hp`**, und der
        // Parser sagte *„':' does not begin an expression"*,
        // **und die Fehlermeldung sprach von einem Doppelpunktzeichen, das
        // der Leser selbst nicht erkannt hatte.**
        return IsIdentifierStart(pChar) || char.IsDigit(pChar)
            || pChar == '@' || pChar == '$';
    }

    /// <summary>
    /// Which letters after <c>%</c> open a percent literal, and which.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this list is Ruby 1.8.1's own, measured, and the
    /// reader had it wrong in both directions at once.</strong>
    /// </para>
    /// <para>
    /// <strong>Measured at <c>parse.y</c>'s own <c>case '%':</c>, the
    /// <c>switch (c)</c> has exactly seven cases:</strong>
    /// <c>'Q'</c> to <c>str_dquote</c>, <c>'q'</c> to <c>str_squote</c>,
    /// <c>'W'</c> to <c>str_dquote | STR_FUNC_QWORDS</c>, <c>'w'</c> to
    /// <c>str_squote | STR_FUNC_QWORDS</c>, <c>'x'</c> to
    /// <c>str_xquote</c>, <c>'r'</c> to <c>str_regexp</c> and
    /// <c>'s'</c> to <c>str_ssym</c>.
    /// </para>
    /// <para>
    /// <strong>And a bare <c>%</c> followed by a non-alphanumeric is
    /// <c>'Q'</c></strong>, measured at the lines above:
    /// <c>if (!ISALNUM(c)) { term = c; c = 'Q'; }</c>. <strong>And
    /// <c>'%r'</c> returns <c>tREGEXP_BEG</c> and <c>'%s'</c> returns
    /// <c>tSYMBEG</c></strong> -- <strong>so those two are not strings
    /// with a prefix but different kinds of token entirely.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>'i'</c> is not in that list.</strong>
    /// <strong><c>%i</c> came with Ruby 1.9, and a reader that accepts
    /// it reads a file 1.8.1 refuses</strong> -- <strong>and the reader
    /// here accepted it and refused <c>%r</c>, which is the one
    /// <c>rubytest.rb</c> line 42 uses.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// True where a `%` opens a literal rather than being a modulus, and that
    /// is measured.
    /// </summary>
    /// <remarks>
    /// Ruby 1.8.1's own `parse.y`, in its `case '%'`:
    ///
    /// <code>
    /// if (lex_state == EXPR_BEG || lex_state == EXPR_MID) {
    ///     int term;
    ///     int paren;
    ///
    ///     c = nextc();
    ///   quotation:
    ///     if (!ISALNUM(c)) {
    ///         term = c;
    ///         c = 'Q';
    ///     }
    ///     else {
    ///         term = nextc();
    ///         if (ISALNUM(term) || ismbchar(term)) {
    ///             yyerror("unknown type of %string");
    ///             return 0;
    ///         }
    ///     }
    ///     paren = term;
    ///     if (term == '(') term = ')';
    ///     else if (term == '[') term = ']';
    ///     else if (term == '{') term = '}';
    ///     else if (term == '<') term = '>';
    ///     else paren = 0;
    ///
    ///     switch (c) {
    ///       case 'Q': ...
    ///       case 'q': ...
    ///       case 'W': ...
    ///       case 'w': ...
    ///       case 'x': ...
    ///       case 'r': ...
    ///       case 's': ...
    /// </code>
    ///
    /// <strong>And any character that is not a letter or a digit is a
    /// delimiter</strong>, <strong>and the letter that follows is then 'Q',
    /// which is a plain string.</strong> So <c>%[a[b]c]</c> is one string
    /// and <c>%q(x)</c> is one string and <c>%{a:b}</c> is one string.
    ///
    /// <strong>And a reader that only knew <c>Q q W w x r s</c> never
    /// reached the literal reader at all for those forms</strong> --
    /// <strong>and the `%` fell through to the operator table</strong>, and
    /// then the bracket arrived on its own as a token, and a block whose
    /// text ran over several lines came out as:
    /// </strong>
    ///
    /// <code>
    /// ']' at offset 288 does not begin an expression
    /// </code>
    ///
    /// <strong>And that is <c>mkconfig.rb</c> lines 22 to 30, unchanged:
    /// a <c>%[</c> literal that spans nine lines and holds a comment, a
    /// <c>module</c>, an <c>or</c> and a <c>raise</c>.</strong>
    ///
    /// <strong>And the second rule in the same block matters too: a letter
    /// followed by another letter or a digit is an error</strong>, and this
    /// reader has no such error, because <c>ReadPercentLiteral</c> accepts
    /// any word and names it, and a name it does not know stays a string.
    /// That is a difference worth naming and not worth breaking a working
    /// path over, so it is left as it is.
    /// </remarks>
    private static bool IsWordLiteralTail(char pChar)
        => !char.IsLetterOrDigit(pChar) && pChar != (char)0xFFFD;

    private static bool IsOperatorTail(char pChar)
    {
        return pChar == '=' || pChar == '<' || pChar == '>' || pChar == '+'
            || pChar == '-' || pChar == '*' || pChar == '/' || pChar == '%'
            || pChar == '^' || pChar == '&' || pChar == '|' || pChar == '~'
            || pChar == '!' || pChar == '[' || pChar == ']' || pChar == '<'
            || pChar == '>';
    }

    /// <summary>
    /// A <c>?</c>: a ternary, a name ending, or a one-character literal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole of <c>parse.y</c>'s own
    /// <c>case '?'</c>, and a reader that only knew the ternary turned
    /// every <c>?x</c> into a syntax error.</strong>
    /// </para>
    /// <para>
    /// <strong>Measured, in the order the grammar tests it:</strong>
    /// </para>
    /// <list type="number">
    /// <item><c>if (lex_state == EXPR_END || lex_state == EXPR_ENDARG) { return '?'; }</c>
    /// -- <strong>the ternary, and that is the common case.</strong></item>
    /// <item><c>if (ISSPACE(c)) { ...warn...; goto ternary; }</c>
    /// -- <strong>a space after it is a ternary and not a literal</strong>,
    /// <strong>and the grammar says so with a warning.</strong></item>
    /// <item><c>else if (ismbchar(c)) { warn; goto ternary; }</c></item>
    /// <item><c>else if ((ISALNUM(c) || c == '_') &amp;&amp; lex_p &lt; lex_pend
    /// &amp;&amp; is_identchar(*lex_p)) { goto ternary; }</c>
    /// -- <strong>a letter followed by another identifier character is a
    /// ternary</strong>, <strong>which is how <c>?a : b</c> reads.</strong></item>
    /// <item><c>else if (c == '\\') { c = read_escape(); }</c></item>
    /// <item><c>c &amp;= 0xff; lex_state = EXPR_END; NEW_LIT(INT2FIX(c)); return tINTEGER;</c>
    /// -- <strong>and everything else is one character, and it arrives as
    /// an integer.</strong></item>
    /// </list>
    /// <para>
    /// <strong>And <c>$mflags.set?(?n)</c> at <c>instruby.rb</c> line 39 is
    /// the last rule and not the first:</strong> <strong><c>n</c> is a
    /// letter, and the next character is <c>)</c>, and
    /// <c>is_identchar(')')</c> is false, so the test fails and the line
    /// falls through to <c>NEW_LIT(INT2FIX('n'))</c>.</strong>
    /// <strong>So the argument is the integer 110, and the method above it
    /// compares with <c>'%c' % flag</c>, which is the character again.</strong>
    /// <strong>And a reader that wanted a regexp here had the wrong rule
    /// entirely.</strong>
    /// </para>
    /// </remarks>
    private RubyToken ReadFragezeichen(int pStart, int pStartLine)
    {
        var danach = Peek(1);

        // **Und ein Leerzeichen danach ist ein Ternaer**, gemessen an
        // `if (ISSPACE(c)) { ... goto ternary; }`.
        if (danach == ' ' || danach == '\t' || danach == '\n'
            || danach == '\r')
        {
            Skip();
            return Make(
                RubyTokenKind.Operator, "?", pStart, pStartLine);
        }

        // **Und ein Buchstabe, dem ein weiteres Bezeichnerzeichen folgt,
        // ist ein Ternaer** -- **und `?n)` ist es nicht, denn `)` ist kein
        // Bezeichnerzeichen.** Das ist der ganze Unterschied.
        if (IsIdentifierStart(danach) || char.IsDigit(danach))
        {
            var nachst = Peek(2);
            if (nachst != '\0' && IsIdentifierPart(nachst))
            {
                Skip();
                return Make(
                    RubyTokenKind.Operator, "?", pStart, pStartLine);
            }
        }

        // **Und ein Name, der mit `?` endet, ist ein Name** -- **und der
        // wird vorher gelesen, denn `set?` ist ein Identifier.**
        Skip();
        var zeichen = Peek(1);
        if (zeichen == '\\')
        {
            // **Und ein Backslash liest ein Escape**, gemessen an
            // `else if (c == '\\') { c = read_escape(); }`.
            Skip();
            zeichen = Peek(1);
        }

        Skip();
        return new RubyToken
        {
            Kind = RubyTokenKind.Integer,
            Text = _text[pStart.._offset],
            Offset = pStart,
            Line = pStartLine,
            Integer = zeichen,
        };
    }

    private RubyToken ReadWord(int pStart, int pStartLine)
    {
        while (!AtEnd && (IsIdentifierPart(Current) || Current == '?' || Current == '!'))
        {
            _offset++;
        }
        var text = _text[pStart.._offset];
        // An identifier that begins with an upper case letter is a constant.
        // This is the rule the whole language hangs on, because a constant
        // resolves differently from a method call.
        var kind = KeywordSet.Contains(text)
            ? RubyTokenKind.Keyword
            : char.IsUpper(text[0])
                ? RubyTokenKind.Constant
                : RubyTokenKind.Identifier;
        return new RubyToken
        {
            Kind = kind,
            Text = text,
            Offset = pStart,
            Line = pStartLine,
        };
    }

    private RubyToken ReadVariable(int pStart, int pStartLine)
    {
        _offset++;
        var isClassVariable = Current == '@';
        if (isClassVariable)
        {
            _offset++;
        }
        if (AtEnd || !(IsIdentifierStart(Current)))
        {
            throw new RubySyntaxException(
                $"A variable name at offset {pStart} has no name after it.", pStartLine);
        }
        while (!AtEnd && IsIdentifierPart(Current))
        {
            _offset++;
        }
        return Make(
            RubyTokenKind.InstanceVariable, _text[pStart.._offset], pStart, pStartLine);
    }

    private RubyToken ReadGlobal(int pStart, int pStartLine)
    {
        _offset++;
        if (!AtEnd && IsIdentifierPart(Current))
        {
            while (!AtEnd && IsIdentifierPart(Current))
            {
                _offset++;
            }
        }
        else if (!AtEnd)
        {
            // A punctuation global such as $1 or $! is one character wide.
            _offset++;
        }
        return Make(RubyTokenKind.GlobalVariable, _text[pStart.._offset], pStart, pStartLine);
    }

    private RubyToken ReadSymbol(int pStart, int pStartLine)
    {
        _offset++;
        if (Current == '"' || Current == '\'')
        {
            var inner = ReadQuoted(_offset, _line, Current);
            return new RubyToken
            {
                Kind = RubyTokenKind.Symbol,
                Text = _text[pStart.._offset],
                Offset = pStart,
                Line = pStartLine,
                Bytes = inner.Bytes,
                Value = inner.Value,
                Parts = inner.Parts,
            };
        }
        if (IsOperatorTail(Current))
        {
            var opStart = _offset;
            _offset++;
            return new RubyToken
            {
                Kind = RubyTokenKind.Symbol,
                Text = _text[pStart.._offset],
                Offset = pStart,
                Line = pStartLine,
                Value = _text[opStart.._offset],
            };
        }
        var nameStart = _offset;
        if (!AtEnd && IsIdentifierStart(Current))
        {
            while (!AtEnd && IsIdentifierPart(Current))
            {
                _offset++;
            }

            // **Und ein Symbol traegt dasselbe Ende wie ein Name.**
            // `:reich?` und `:"ungleich!"` sind Symbole,
            // **und `send(:reich?)` ist der Satz, mit dem jedes
            // Plugin eine Praedikatmethode aufruft** --
            // **und ohne das las der Lexer `:reich` und las `?` als
            // Naechstes, und der Parser sagte *")" at offset 52 does
            // not begin an expression*.** `parse.y` Zeile 4314 gilt
            // fuer jeden Namen, **und ein Symbol ist ein Name.**
            _offset += ReadNamensende(nameStart).Length;
        }
        else if (!AtEnd && (Current == '@' || Current == '$'))
        {
            // **Und `:@held`, `:$globals` und `:@@zaehler` sind Symbole.**
            // `instance_variable_get(:@hp)` und
            // `class_variable_get(:@@zaehler)` schreiben jedes Skript, das
            // ueber eine Instanz nachdenkt,
            // **und ohne diese Regel zerlegte der Lexer `:@hp` in `:` und
            // `@hp`** --
            // **und der Parser sagte *„':' does not begin an
            // expression"*, **und die Fehlermeldung ging um ein
            // Doppelpunktzeichen, das der Leser selbst nicht erkannt
            // hatte.**
            _offset++;
            if (!AtEnd && Current == '@')
            {
                _offset++;
            }

            while (!AtEnd && IsIdentifierPart(Current))
            {
                _offset++;
            }
        }
        else if (!AtEnd && (char.IsDigit(Current)))
        {
            ReadNumber(nameStart, pStartLine);
        }
        return new RubyToken
        {
            Kind = RubyTokenKind.Symbol,
            Text = _text[pStart.._offset],
            Offset = pStart,
            Line = pStartLine,
            Value = _text[nameStart.._offset],
        };
    }

    private RubyToken ReadNumber(int pStart, int pStartLine)
    {
        if (Current == '0' && (Peek(1) == 'x' || Peek(1) == 'X'))
        {
            _offset += 2;
            var hexStart = _offset;
            while (!AtEnd && Uri.IsHexDigit(Current))
            {
                _offset++;
            }
            if (_offset == hexStart)
            {
                throw new RubySyntaxException(
                    $"A hexadecimal number at offset {pStart} has no digits.", pStartLine);
            }
            var hex = _text[hexStart.._offset];
            _offset += ReadUnderscores();
            if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
            {
                throw new RubySyntaxException(
                    $"0x{hex} is not a number this reader can hold.", pStartLine);
            }
            return new RubyToken
            {
                Kind = RubyTokenKind.Integer,
                Text = _text[pStart.._offset],
                Offset = pStart,
                Line = pStartLine,
                Integer = unchecked((long)hexValue),
            };
        }
        if (Current == '0' && (Peek(1) == 'b' || Peek(1) == 'B'))
        {
            _offset += 2;
            var binStart = _offset;
            while (!AtEnd && (Current == '0' || Current == '1' || Current == '_'))
            {
                _offset++;
            }
            var binary = _text[binStart.._offset].Replace("_", "");
            if (binary.Length == 0)
            {
                throw new RubySyntaxException(
                    $"A binary number at offset {pStart} has no digits.", pStartLine);
            }
            return new RubyToken
            {
                Kind = RubyTokenKind.Integer,
                Text = _text[pStart.._offset],
                Offset = pStart,
                Line = pStartLine,
                Integer = Convert.ToInt64(binary, 2),
            };
        }
        if (Current == '0' && (Peek(1) == 'o' || Peek(1) == 'O' || (Peek(1) >= '0' && Peek(1) <= '7')))
        {
            if (Peek(1) == 'o' || Peek(1) == 'O')
            {
                // Step over the leading zero and the marker together, so the
                // digits start after both.
                _offset += 2;
            }
            var digitsStart = _offset;
            while (!AtEnd && ((Current >= '0' && Current <= '7') || Current == '_'))
            {
                _offset++;
            }
            var octal = _text[digitsStart.._offset].Replace("_", "");
            if (octal.Length == 0)
            {
                throw new RubySyntaxException(
                    $"An octal number at offset {pStart} has no digits.", pStartLine);
            }
            return new RubyToken
            {
                Kind = RubyTokenKind.Integer,
                Text = _text[pStart.._offset],
                Offset = pStart,
                Line = pStartLine,
                Integer = Convert.ToInt64(octal, 8),
            };
        }

        while (!AtEnd && (char.IsDigit(Current) || Current == '_'))
        {
            _offset++;
        }
        var digits = _text[pStart.._offset].Replace("_", "");
        var isFloat = false;
        if (Current == '.' && char.IsDigit(Peek(1)))
        {
            isFloat = true;
            _offset++;
            while (!AtEnd && char.IsDigit(Current))
            {
                _offset++;
            }
        }
        if (Current == 'e' || Current == 'E')
        {
            var exponentMark = _offset;
            _offset++;
            if (Current == '+' || Current == '-')
            {
                _offset++;
            }
            if (!AtEnd && char.IsDigit(Current))
            {
                isFloat = true;
                while (!AtEnd && char.IsDigit(Current))
                {
                    _offset++;
                }
            }
            else
            {
                // What looked like an exponent is an identifier, so the number
                // ended before it. Ruby treats them as separate tokens.
                _offset = exponentMark;
            }
        }
        var whole = _text[pStart.._offset].Replace("_", "");
        if (isFloat)
        {
            if (!double.TryParse(
                whole, NumberStyles.Float, CultureInfo.InvariantCulture, out var real))
            {
                throw new RubySyntaxException(
                    $"{whole} is not a number this reader can hold.", pStartLine);
            }
            return new RubyToken
            {
                Kind = RubyTokenKind.Float,
                Text = _text[pStart.._offset],
                Offset = pStart,
                Line = pStartLine,
                Real = real,
            };
        }
        if (!long.TryParse(whole, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new RubySyntaxException(
                $"{whole} is not an integer this reader can hold.", pStartLine);
        }
        return new RubyToken
        {
            Kind = RubyTokenKind.Integer,
            Text = _text[pStart.._offset],
            Offset = pStart,
            Line = pStartLine,
            Integer = value,
        };
    }

    private int ReadUnderscores()
    {
        // Underscores may separate digits. They are skipped only after digits.
        var count = 0;
        while (!AtEnd && Current == '_')
        {
            _offset++;
            count++;
        }
        return count;
    }

    private RubyToken ReadQuoted(int pStart, int pStartLine, char pQuote)
    {
        _offset++;
        var parts = new List<RubyStringPart>();
        var raw = new List<byte>();
        while (true)
        {
            if (AtEnd)
            {
                throw new RubySyntaxException(
                    $"A string opened at offset {pStart} is never closed.", pStartLine);
            }
            var c = Current;
            if (c == pQuote)
            {
                _offset++;
                break;
            }
            if (c == '\\')
            {
                if (pQuote == '\'')
                {
                    // Only the quote and the backslash are escapes here. Anything
                    // else after a backslash is a literal backslash followed by
                    // whatever the source wrote.
                    var next = _offset + 1 < _text.Length ? _text[_offset + 1] : '\0';
                    if (next != '\'' && next != '\\')
                    {
                        raw.Add((byte)'\\');
                        parts.Add(new RubyStringPart { IsEscape = false, Text = "\\" });
                        _offset++;
                        continue;
                    }
                }
                ReadEscape(parts, raw, pStartLine);
                continue;
            }
            if (c == '\n')
            {
                _line++;
            }
            raw.AddRange(Encoding.UTF8.GetBytes(c.ToString()));
            parts.Add(new RubyStringPart { IsEscape = false, Text = c.ToString() });
            _offset++;
        }
        return new RubyToken
        {
            Kind = pQuote == '`' ? RubyTokenKind.String : RubyTokenKind.String,
            Text = _text[pStart.._offset],
            Offset = pStart,
            Line = pStartLine,
            Bytes = [.. raw],
            Value = string.Concat(parts.Select(pPart => pPart.Resolved ?? pPart.Text)),
            Parts = parts,
        };
    }

    private void ReadEscape(
        List<RubyStringPart> pParts, List<byte> pRaw, int pStartLine)
    {
        var escapeStart = _offset;
        _offset++;
        if (AtEnd)
        {
            throw new RubySyntaxException(
                $"An escape at offset {escapeStart} is cut off.", pStartLine);
        }
        var c = Current;
        _offset++;
        string? resolved = c switch
        {
            'n' => "\n",
            't' => "\t",
            'r' => "\r",
            '0' => "\0",
            's' => " ",
            'a' => "\a",
            'b' => "\b",
            'e' => "",
            'f' => "\f",
            'v' => "\v",
            '\n' => "\n",
            _ => null,
        };
        if (resolved != null)
        {
            pParts.Add(new RubyStringPart
            {
                IsEscape = true,
                Text = c.ToString(),
                Resolved = resolved,
            });
            pRaw.AddRange(Encoding.UTF8.GetBytes(resolved));
            if (c == '\n')
            {
                _line++;
            }
            return;
        }
        if (c == 'x')
        {
            var digits = new StringBuilder();
            for (var count = 0; count < 2 && !AtEnd && Uri.IsHexDigit(Current); count++)
            {
                digits.Append(Current);
                _offset++;
            }
            if (digits.Length == 0)
            {
                throw new RubySyntaxException(
                    $"A hexadecimal escape at offset {escapeStart} has no digits.", pStartLine);
            }
            var value = Convert.ToInt32(digits.ToString(), 16);
            // A byte escape is a byte, not a character, so it is kept as one.
            pParts.Add(new RubyStringPart
            {
                IsEscape = true,
                Text = "x" + digits,
                Resolved = ((char)value).ToString(),
            });
            pRaw.Add((byte)value);
            return;
        }
        if (c == 'u')
        {
            var digits = new StringBuilder();
            for (var count = 0; count < 4 && !AtEnd && Uri.IsHexDigit(Current); count++)
            {
                digits.Append(Current);
                _offset++;
            }
            if (digits.Length == 0)
            {
                throw new RubySyntaxException(
                    $"A unicode escape at offset {escapeStart} has no digits.", pStartLine);
            }
            var value = Convert.ToInt32(digits.ToString(), 16);
            var text = new UTF8Encoding(false).GetString([(byte)(value >> 8), (byte)value]);
            pParts.Add(new RubyStringPart
            {
                IsEscape = true,
                Text = "u" + digits,
                Resolved = text,
            });
            pRaw.AddRange(Encoding.UTF8.GetBytes(text));
            return;
        }
        if (char.IsDigit(c))
        {
            // An octal escape: the backslash, then up to three octal digits.
            var digits = new StringBuilder();
            digits.Append(c);
            for (var count = 1; count < 3 && !AtEnd && Current >= '0' && Current <= '7'; count++)
            {
                digits.Append(Current);
                _offset++;
            }
            var value = Convert.ToInt32(digits.ToString(), 8);
            pParts.Add(new RubyStringPart
            {
                IsEscape = true,
                Text = digits.ToString(),
                Resolved = ((char)value).ToString(),
            });
            pRaw.Add((byte)value);
            return;
        }
        // Any other escaped character stands for itself.
        pParts.Add(new RubyStringPart
        {
            IsEscape = true,
            Text = c.ToString(),
            Resolved = c.ToString(),
        });
        pRaw.AddRange(Encoding.UTF8.GetBytes(c.ToString()));
    }

    private RubyToken ReadRegexp(int pStart, int pStartLine)
    {
        _offset++;
        var parts = new List<RubyStringPart>();
        var inClass = false;

        // **Wo die Klasse anfing, damit ein ungeschlossenes `[` am
        // Schluss wieder zu einem Zeichen wird.**
        var klassenStart = -1;
        while (true)
        {
            if (AtEnd || Current == '\n')
            {
                // **Und endet der Text in einer offenen Klasse, ist das
                // ein gueltiges Muster und kein halbes.** `/a[/` ist eins,
                // **und der Fehler "never closed" waere fuer ein Skript,
                // das ein Muster schreibt, das die Referenz annimmt,
                // eine Falschmeldung ueber den Leser.**
                if (inClass && klassenStart >= 0)
                {
                    // **Und das `/`, das die Klasse nie schloss, gehoert
                    // nicht zum Muster.** `/a[/` ist das Muster `a[`,
                    // **und mit dem Slash darin sucht die Maschine nach
                    // `a[//`** -- **gemessen: `Invalid pattern 'a[//' at
                    // offset 3. Unterminated [] set.`**
                    //
                    // **Und es steht an letzter Stelle, weil es das
                    // Zeichen war, das die Schleife verlassen hat.** Also
                    // wird es hier abgenommen und sonst nirgends.
                    if (parts.Count > 0 && parts[^1].Text == "/")
                    {
                        parts.RemoveAt(parts.Count - 1);
                    }

                    var text = string.Concat(parts.Select(pTeil => pTeil.Text));
                    return new RubyToken
                    {
                        Kind = RubyTokenKind.Regexp,
                        Text = _text[pStart.._offset],
                        Offset = pStart,
                        Line = pStartLine,
                        Value = text,
                    };
                }

                throw new RubySyntaxException(
                    $"A regular expression opened at offset {pStart} is never closed.",
                    pStartLine);
            }
            var c = Current;
            if (c == '\\')
            {
                var escapeStart = _offset;
                _offset++;
                if (AtEnd)
                {
                    throw new RubySyntaxException(
                        $"An escape at offset {escapeStart} is cut off.", pStartLine);
                }
                // Inside a regular expression an escape keeps its backslash,
                // because the regular expression engine is what interprets it.
                parts.Add(new RubyStringPart
                {
                    IsEscape = true,
                    Text = "\\" + Current,
                });
                _offset++;
                continue;
            }
            if (c == '[')
            {
                // **Und ein `[`, dem kein `]` folgt, ist ein Zeichen und
                // keine oeffnende Klasse.** `/[/` ist in Ruby ein
                // gueltiges Muster -- eine Klasse mit einem `[` darin,
                // **und der Lexer nahm es fuer eine offene Klasse und lief
                // bis zum Ende des Skripts** --
                // **gemessen: `A regular expression opened at offset 9 is
                // never closed.`**
                //
                // **Und ein `]` ohne `[` ist auch nur ein Zeichen**, und
                // das war schon richtig, **weil `inClass` dann false
                // bleibt und der Naechste `/` schliesst.**
                inClass = true;
                klassenStart = parts.Count;
            }
            else if (c == ']')
            {
                inClass = false;
            }
            else if (c == '/' && !inClass)
            {
                _offset++;
                break;
            }
            parts.Add(new RubyStringPart { IsEscape = false, Text = c.ToString() });
            _offset++;
        }
        // **Und die Buchstaben hinter dem zweiten Schraegstrich sind die
        // Optionen.** Sie wurden gelesen und dann weggeworfen,
        // **also war `/held/i` dasselbe wie `/held/`**,
        // **und ein Spiel, das seinen Namen ohne Rücksicht auf die
        // Schreibweise sucht, hat ihn nicht gefunden** -- **und nichts hat
        // es gesagt.**
        //
        // **Die Reihenfolge ist nicht meine.** In `re.c` aus Ruby 1.8.1
        // haengt `rb_reg_to_s` sie als `m`, `i`, `x` an,
        // **und diese Reihenfolge steht in `MusterOhneGrossKlein` und
        // seinen Nachbarn.**
        var optionen = 0;
        while (!AtEnd && (char.IsLetter(Current)))
        {
            optionen |= Current switch
            {
                'm' => 2,
                'i' => 1,
                'x' => 4,
                _ => 0,
            };
            _offset++;
        }

        return new RubyToken
        {
            Kind = RubyTokenKind.Regexp,
            Text = _text[pStart.._offset],
            Offset = pStart,
            Line = pStartLine,
            Value = string.Concat(parts.Select(pPart => pPart.Text)),
            Options = optionen,
        };
    }

    private RubyToken ReadPercentLiteral(int pStart, int pStartLine)
    {
        // **Und der Trenner ist das Zeichen, auf das `_offset` jetzt
        // zeigt, und nicht das darauffolgende.**
        //
        // **Und `_offset += 2` ueberspringt genau `%` und den Buchstaben,
        // und bei `%r:` zeigt es danach auf `:`.** **Ein Leser, der
        // `Peek(1)` nahm, las den ersten Inhalt als Trenner** -- **und
        // `%r:^(a|not):` endete dann nach `^` statt nach dem zweiten `:`,
        // und der Rest des Musters kam als Code heraus.**
        //
        // **Und bei einem `%` ohne Buchstaben ist es dasselbe Bild:**
        // gemessen an `parse.y`, `if (!ISALNUM(c)) { term = c; c = 'Q';
        // }`, **und dann ist `_offset += 1` richtig und `+= 2` falsch.**
        var hatBuchstabe = char.IsLetterOrDigit(Peek(1));
        var kind = hatBuchstabe ? Peek(1).ToString() : "Q";
        _offset += hatBuchstabe ? 2 : 1;
        string closer = Current == '[' ? "]" : Current.ToString();
        // **Und diese Pruefung sieht jetzt auf `Current`, und nicht auf
        // `_offset - 1`.** **Und das ist derselbe Grund:** der Trenner
        // steht bei `Current`, und ein Leser, der eine Position zurueck
        // sah, pruefte den Buchstaben statt des Trenners.
        if (_offset < _text.Length && Current != '[' && Current != '{'
            && Current != '<')
        {
            _offset++;
        }
        var parts = new List<RubyStringPart>();
        while (true)
        {
            if (AtEnd)
            {
                throw new RubySyntaxException(
                    $"A percent literal opened at offset {pStart} is never closed.", pStartLine);
            }
            var c = Current;
            if (c == '\\')
            {
                _offset++;
                if (AtEnd)
                {
                    throw new RubySyntaxException(
                        $"An escape at offset {pStart} is cut off.", pStartLine);
                }
                parts.Add(new RubyStringPart
                {
                    IsEscape = true,
                    Text = "\\" + Current,
                    Resolved = Current.ToString(),
                });
                _offset++;
                continue;
            }
            if (c.ToString() == closer)
            {
                _offset++;
                break;
            }
            if (c == '\n')
            {
                _line++;
            }
            parts.Add(new RubyStringPart { IsEscape = false, Text = c.ToString() });
            _offset++;
        }
        // **Und `%r` ist ein Regexp und kein String, und das ist
        // gemessen.**
        //
        // **An `parse.y`s eigenem `case '%'`:**
        //
        // ```c
        // case 'r':
        //     lex_strterm = NEW_STRTERM(str_regexp, term, paren);
        //     return tREGEXP_BEG;
        // ```
        //
        // **Und `str_regexp` ist `STR_FUNC_REGEXP|STR_FUNC_ESCAPE|
        // STR_FUNC_EXPAND`, und `tREGEXP_BEG` ist ein anderer Token als
        // `tSTRING_BEG`.** **Und `%s` gibt `tSYMBEG`, also ein Symbol.**
        //
        // **Und dieser Leser gab beiden `String`**, **und deshalb war
        // `%r:^(sample/test.rb|not):` in `rubytest.rb` Zeile 42 kein
        // Regexp, sondern Text.**
        var istRegexp = kind == "r";

        if (istRegexp)
        {
            var optionen = 0;
            while (!AtEnd && (Current == 'i' || Current == 'm' || Current == 'x'))
            {
                optionen += Current switch
                {
                    'm' => 2,
                    'i' => 1,
                    'x' => 4,
                    _ => 0,
                };
                _offset++;
            }

            return new RubyToken
            {
                Kind = RubyTokenKind.Regexp,
                Text = _text[pStart.._offset],
                Offset = pStart,
                Line = pStartLine,
                Value = string.Concat(parts.Select(pPart => pPart.Resolved
                    ?? pPart.Text)),
                Options = optionen,
            };
        }

        return new RubyToken
        {
            Kind = RubyTokenKind.String,
            Text = _text[pStart.._offset],
            Offset = pStart,
            Line = pStartLine,
            Value = string.Concat(parts.Select(pPart => pPart.Resolved ?? pPart.Text)),
            Parts = parts,
        };
    }

}
