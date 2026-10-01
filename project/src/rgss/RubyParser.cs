using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Rgss;

/// <summary>The exception a Ruby script this parser cannot read raises.</summary>
public sealed class RubyParseException : Exception
{
    public RubyParseException(string pMessage, int pLine)
        : base(pMessage)
    {
        Line = pLine;
    }

    /// <summary>The one indexed line the failure was found on.</summary>
    public int Line { get; }
}

/// <summary>
/// Turns a token stream into a tree of shapes.
/// </summary>
/// <remarks>
/// <para>
/// The parser answers one question: what shape was written. It does not answer
/// what any name means, whether a call succeeds, or what a value is at run
/// time. A node that says <see cref="RubyNodeKind.Call"/> records that a call
/// was written; it does not call it, and it does not even know whether the name
/// is a method this runtime has ever heard of.
/// </para>
/// <para>
/// The operator precedence is the grammar's own, taken from the declaration
/// order in the table Ruby's parser generator reads. Getting this from the
/// grammar rather than from memory is the point: a reader that puts addition
/// below multiplication parses a game's arithmetic as a different tree, and
/// nothing about the result looks wrong.
/// </para>
/// <para>
/// A script this parser cannot read raises with its line. A tree that stopped
/// early would be worse than none, because nothing marks it as complete.
/// </para>
/// </remarks>
public sealed class RubyParser
{
    /// <summary>
    /// The operator precedence, lowest binding first, in the order the grammar
    /// declares its precedence levels.
    /// </summary>
    /// <remarks>
    /// A level listed here binds more tightly than the one above it. The
    /// grammar reads the same table from bottom to top, so the order here is the
    /// reverse of the declaration order and the tests check the two shapes that
    /// differ: `a + b * c` and `a * b + c`.
    /// </remarks>
    private static readonly string[][] Precedence =
    [
        // Declared loosest first. The grammar reads the same table from the
        // bottom up, so the order here is the reverse of its declaration order
        // and each entry binds more tightly than the one above it.
        //
        // **And the words are in here, and that is measured.** Ruby
        // 1.8.1's own parse.y line 305 says `%left kOR kAND`, line 312
        // says `%left tOROP` and line 313 says `%left tANDOP`, and line
        // 277 declares one token for all four: `%token tANDOP tOROP
        // /* && and || */`. **So `and` and `&&` are one operator to the
        // grammar and `or` and `||` are one, and the word forms bind
        // looser than the symbol forms** -- which is the opposite of what
        // a reader would guess. **And `or` binds looser than `and`**,
        // measured at those same three lines, in declaration order.
        ["or", "||"],
        ["and", "&&"],
        ["not"],
        ["modifier_rescue"],
        ["=>", ":="],
        ["..", "..."],
        ["==", "!=", "===", "=~", "!~", "<=>", ">", ">=", "<", "<="],
        ["|", "^"],
        ["&"],
        ["<<", ">>"],
        ["+", "-"],
        ["*", "/", "%"],
        ["**"],
        ["!", "~", "unary_minus", "unary_plus"],
    ];

    /// <summary>Where each operator sits in the table above.</summary>
    private static readonly Dictionary<string, int> BindingPower = BuildBindingPower();

    private static Dictionary<string, int> BuildBindingPower()
    {
        var powers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var level = 0; level < Precedence.Length; level++)
        {
            foreach (var op in Precedence[level])
            {
                powers[op] = level;
            }
        }
        return powers;
    }

    private readonly List<RubyToken> _tokens;
    private int _index;

    /// <summary>
    /// True right after a loop's condition, where a 'do' is the body's opener.
    /// </summary>
    /// <remarks>
    /// The flag is set the moment a loop's condition has been read and cleared by
    /// anything that reads a value after it, so a 'do' that really is a block
    /// opener is still read as one. Without it, `while a do b end` reads its own
    /// condition as the receiver of a block.
    /// </remarks>
    private bool AfterACondition { get; set; }

    public RubyParser(IReadOnlyList<RubyToken> pTokens)
    {
        _tokens = pTokens as List<RubyToken> ?? [.. pTokens];
    }

    /// <summary>Where the parser has reached.</summary>
    public int Position => _index;

    private RubyToken Current => _tokens[Math.Min(_index, _tokens.Count - 1)];

    /// <summary>
    /// Whether a space came before the current token.
    /// </summary>
    /// <returns>true when the two tokens are not neighbours.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>puts [1, 2]</c> is <c>puts([1, 2])</c>.</strong> Ruby
    /// reads a bracket right after a space as the start of an argument,
    /// **and a reader that always read it as an index made
    /// <c>puts [1, 2]</c> into <c>puts[1, 2]</c>** -- **measured: the
    /// node was <c>Call name=[]</c> with the receiver <c>puts</c>, and
    /// the host wrote nothing at all.**
    /// </para>
    /// <para>
    /// <strong>And <c>a[1]</c> without a space is still an index</strong>
    /// -- **and a reader that always read the bracket as an argument
    /// would break every array and hash in a game** -- **and the
    /// offset of a token against the one before it is the whole
    /// difference, and it is measured from what the lexer already
    /// knows.**
    /// </para>
    /// </remarks>
    private bool AbstandDavor
    {
        get
        {
            var jetzt = Math.Min(_index, _tokens.Count - 1);
            if (jetzt <= 0)
            {
                return false;
            }

            var vorher = _tokens[jetzt - 1];
            var hier = _tokens[jetzt];
            return hier.Offset - (vorher.Offset + vorher.Text.Length) > 0;
        }
    }

    private bool AtEnd => Current.Kind == RubyTokenKind.EndOfInput;

    private void SkipNewlines()
    {
        while (Current.Kind == RubyTokenKind.Newline || Current.Kind == RubyTokenKind.Semicolon)
        {
            _index++;
        }
    }

    private RubyToken Take()
    {
        var token = Current;
        _index++;
        return token;
    }

    private bool Is(string pText)
    {
        return Current.Text == pText
            && (Current.Kind == RubyTokenKind.Operator
                || Current.Kind == RubyTokenKind.Keyword
                || Current.Kind == RubyTokenKind.Delimiter);
    }

    private bool IsKeyword(string pText)
    {
        return Current.Kind == RubyTokenKind.Keyword && Current.Text == pText;
    }

    private void Expect(string pText)
    {
        if (!Is(pText))
        {
            throw new RubyParseException(
                $"'{pText}' was expected at offset {Current.Offset}, but '{Current.Text}' is there.",
                Current.Line);
        }
        _index++;
    }

    /// <summary>Reads every statement in the token stream.</summary>
    /// <exception cref="RubyParseException">The script is not shaped like Ruby.</exception>
    public List<RubyNode> ParseProgram()
    {
        return ParseStatements("end of input");
    }

    /// <summary>
    /// Reads statements until the closer is next, without consuming it.
    /// </summary>
    /// <remarks>
    /// A block's body is not a program, so it stops at its own closer. Reading
    /// it as a program instead ran the body to the end of the file and then
    /// complained that the closer was missing, which is a misleading message
    /// about a file that is perfectly well formed.
    /// </remarks>
    private List<RubyNode> ParseStatements(params string[] pClosers)
    {
        var statements = new List<RubyNode>();
        while (true)
        {
            SkipNewlines();
            if (AtEnd)
            {
                if (pClosers.Length == 1 && pClosers[0] == "end of input")
                {
                    return statements;
                }

                throw new RubyParseException(
                    $"'{pClosers[0]}' was expected, but the script ends first.",
                    Current.Line);
            }

            // **Einer der Schluesselwoerter genuegt.** `if` wird von `else`
            // und von `end` beendet, und **ohne diese Liste haette der
            // Parser `if a then b else c end` als Rumpf `b` gelesen und
            // `else` als den naechsten Ausdruck erwartet** -- das ist der
            // Fehler, an dem die erste Fassung des Interpreters scheiterte.
            foreach (var closer in pClosers)
            {
                if (Is(closer))
                {
                    return statements;
                }
            }

            statements.Add(ParseStatement());
            SkipNewlines();
        }
    }

    private RubyNode ParseStatement()
    {
        var node = ParseExpression();
        if (IsKeyword("if") || IsKeyword("unless") || IsKeyword("while") || IsKeyword("until"))
        {
            // A modifier keyword applies to the statement before it, so the
            // condition comes after the body rather than around it.
            var keyword = Take().Text;
            var condition = ParseExpression();
            node = new RubyNode
            {
                Kind = keyword switch
                {
                    "if" => RubyNodeKind.If,
                    "unless" => RubyNodeKind.If,
                    "while" => RubyNodeKind.While,
                    _ => RubyNodeKind.Until,
                },
                Name = keyword,
                Line = node.Line,
                Children = [node, condition],
                Role_Children =
                [
                    new() { Role = RubyNodeRole.Body, Node = node },
                    new() { Role = RubyNodeRole.Condition, Node = condition },
                ],
            };
        }
        return node;
    }

    private RubyNode ParseExpression()
    {
        return ParseAssignment();
    }

    /// <summary>The left side of an <c>=</c>, as the grammar's <c>mlhs</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a list and not a single node, and that is
    /// measured.</strong> Ruby 1.8.1's own <c>parse.y</c>, productions
    /// <c>mlhs</c>, <c>mlhs_entry</c>, <c>mlhs_basic</c>, <c>mlhs_item</c>
    /// and <c>mlhs_head</c>, every one of which ends in <c>NEW_MASGN</c>:
    /// </para>
    /// <code>
    /// mlhs       : mlhs_basic | '(' mlhs_entry ')'
    /// mlhs_entry : mlhs_basic | '(' mlhs_entry ')'
    /// mlhs_basic : mlhs_head
    ///            | mlhs_head mlhs_item
    ///            | mlhs_head tSTAR mlhs_node
    ///            | mlhs_head tSTAR
    ///            | tSTAR mlhs_node
    ///            | tSTAR
    /// mlhs_item  : mlhs_node | '(' mlhs_entry ')'
    /// mlhs_head  : mlhs_item ','
    /// </code>
    /// <para>
    /// <strong>And the two forms this reader did not have are both in
    /// that text.</strong> <strong>A second name after a comma</strong>
    /// (<c>a, b = 1, 2</c>) <strong>and a star that takes the rest</strong>
    /// (<c>a, *rest = ...</c>, whose <c>mlhs_head tSTAR mlhs_node</c>
    /// production passes <c>-1</c> as the splat and a bare
    /// <c>mlhs_head tSTAR</c> passes <c>-1</c> with no name at all).</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that read the target as one expression
    /// refused every script that unpacks a list</strong> --
    /// <strong>and <c>instruby.rb</c> line 31 does exactly that, in
    /// Ruby 1.8.1's own tree.</strong>
    /// </para>
    /// </remarks>
    private List<RubyNode> ParseAssignmentTarget(bool pZiel = true)
    {
        var liste = new List<RubyNode>();

        // **Und die Klammer ist nur dann eine Zielseite, und das ist
        // gemessen.**
        //
        // Ruby 1.8.1's own `parse.y`: `lhs : mlhs_basic | tLPAREN mlhs_entry
        // ')'` -- **und `mlhs_basic : mlhs_head | mlhs_head mlhs_item |
        // mlhs_head tSTAR mlhs_node | ...`**, **und `mlhs_head : mlhs_item
        // ','`.**
        //
        // **Also braucht `(a, b)` ein Komma und `(a)` braucht ein `=`**
        // **-- und ohne eines von beidem ist die Klammer ein geklammerter
        // Ausdruck und keine Zielseite.**
        //
        // **Und diese Bedingung fehlte, und 26 Tests brachen** -- **denn
        // `Is("(")` ist bei `[7.zero?, 0.zero?, ...]` wahr**, **und der
        // Leser las die ganze Liste als eine Klammer-Zielseite, und der
        // Punkt nach `7` kam dann an die Spitze von `ParsePrimary`.**
        if (Is("(") && KlammerIstEineZielseite())
        {
            Take();
            while (!Is(")") && !AtEnd)
            {
                SkipNewlines();
                if (Is(")") || AtEnd)
                {
                    break;
                }

                liste.AddRange(ParseAssignmentTarget());
                SkipNewlines();
                if (Is(","))
                {
                    Take();
                }
            }

            if (Is(")"))
            {
                Take();
            }

            return liste;
        }

        // **And the star may lead**, measured at `tSTAR mlhs_node`.
        if (Is("*"))
        {
            Take();
            liste.Add(new RubyNode
            {
                Kind = RubyNodeKind.Splat,
                Line = Current.Line,
            });
            if (StartsAValue())
            {
                liste.Add(ParseAssignmentTarget()[0]);
            }

            return liste;
        }

        liste.Add(ParseTernary());

        // **Und nur die linke Seite sammelt Namen.** **Und `arg` ist kein
        // `mlhs`**, **gemessen an `parse.y`: `arg : lhs '=' arg`** --
        // **und die rechte Seite von `a = 1, 2` ist ein Ausdruck und keine
        // Liste von Namen.**
        while (pZiel && Is(",") && CommaBelongsToTheTarget())
        {
            Take();
            SkipNewlines();
            if (Is("*"))
            {
                Take();
                liste.Add(new RubyNode
                {
                    Kind = RubyNodeKind.Splat,
                    Line = Current.Line,
                });
                // **Und der Name nach dem Stern, und nicht ein Wert.**
                //
                // **Gemessen an `parse.y`: `mlhs_basic : mlhs_head tSTAR
                // mlhs_node`** -- **und `mlhs_node` ist ein Name, keine
                // Liste**, **und `mlhs_basic : mlhs_head tSTAR` schreibt
                // die `-1`, und die ist der Stern ganz ohne Namen.**
                //
                // **Und `!Is("=")` war hier falsch, denn nach `Take()` zeigt
                // `Current` auf den Namen und nicht auf ein `=`.** **Also
                // las der Leser `rest` als Wert und stiess danach auf das
                // Komma, das schon gegessen war.**
                if (StartsAValue())
                {
                    liste.Add(ParseTernary());
                }
            }
            else if (StartsAValue())
            {
                liste.Add(ParseTernary());
            }
            else
            {
                // **And `mlhs_head tSTAR` has no name after the star**,
                // and a reader that demanded one stopped here.
                break;
            }
        }

        return liste;
    }

    /// <summary>
    /// Whether the bracket at the current token is a target, not a group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And measured at <c>parse.y</c>: <c>lhs : mlhs_basic |
    /// tLPAREN mlhs_entry ')'</c>, and <c>mlhs_head : mlhs_item ','</c>.</strong>
    /// <strong>So a bracket on the left of an <c>=</c> holds names, and a
    /// bracket anywhere else holds an expression.</strong>
    /// </para>
    /// <para>
    /// <strong>And the tell is what follows the closing bracket</strong> --
    /// <strong>a comma, which means more names, or an <c>=</c>, which means
    /// this whole bracket was the left side.</strong> <strong>And
    /// <c>Is("(")</c> alone is not that tell</strong>, <strong>and a
    /// reader that used it read the element list of every array as a
    /// target list**, <strong>which is where 26 tests went red.</strong>
    /// </para>
    /// </remarks>
    private bool KlammerIstEineZielseite()
    {
        var k = _index;
        var tiefe = 0;

        while (k < _tokens.Count)
        {
            var t = _tokens[k];
            if (t.Kind == RubyTokenKind.EndOfInput)
            {
                return false;
            }

            if (t.Kind == RubyTokenKind.Delimiter)
            {
                if (t.Text == "(")
                {
                    tiefe++;
                }
                else if (t.Text == ")")
                {
                    tiefe--;
                    if (tiefe == 0)
                    {
                        // **Und was hinter der schliessenden Klammer
                        // steht, entscheidet es.**
                        var j = k + 1;
                        while (j < _tokens.Count
                            && (_tokens[j].Kind == RubyTokenKind.Newline
                                || _tokens[j].Kind == RubyTokenKind.Semicolon))
                        {
                            j++;
                        }

                        return j < _tokens.Count
                            && (_tokens[j].Kind == RubyTokenKind.Operator
                                && (_tokens[j].Text == ","
                                    || _tokens[j].Text == "="));
                    }
                }
            }

            k++;
        }

        return false;
    }

    /// <summary>
    /// Whether a comma here starts another name of the same <c>=</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the decision the grammar makes with two tokens
    /// of lookahead, and without it a comma is ambiguous.</strong>
    /// </para>
    /// <para>
    /// <strong>Measured at <c>parse.y</c>: <c>arg : lhs '=' arg</c>, and
    /// <c>lhs : mlhs_basic</c>, and <c>mlhs_head : mlhs_item ','</c> --
    /// and the parser resolves the clash by trying <c>mlhs</c> and letting
    /// the <c>'='</c> further right fail if it does not fit.</strong>
    /// <strong>So a comma belongs to the target exactly when an <c>=</c>
    /// follows the names it introduces.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that swallowed every comma broke the commonest
    /// call in Ruby.</strong> <c>sprite.draw(x, y)</c> -- <strong>the comma
    /// there is between two arguments, not between two names, and this
    /// method is what tells the two apart.</strong>
    /// </para>
    /// </remarks>
    private bool CommaBelongsToTheTarget()
    {
        var k = _index;

        // **And the names may continue with stars, and the names after
        // them are still names** -- `a, *b, c = 1`.
        while (k < _tokens.Count
            && (_tokens[k].Kind == RubyTokenKind.Newline
                || _tokens[k].Kind == RubyTokenKind.Semicolon))
        {
            k++;
        }

        while (k < _tokens.Count && _tokens[k].Text == ",")
        {
            k++;
            while (k < _tokens.Count
                && (_tokens[k].Kind == RubyTokenKind.Newline
                    || _tokens[k].Kind == RubyTokenKind.Semicolon))
            {
                k++;
            }

            // **And a star is a name in its own right**, measured at
            // `mlhs_basic : mlhs_head tSTAR mlhs_node` and `... tSTAR`.
            if (k < _tokens.Count && _tokens[k].Text == "*")
            {
                k++;
                while (k < _tokens.Count
                    && (_tokens[k].Kind == RubyTokenKind.Newline
                        || _tokens[k].Kind == RubyTokenKind.Semicolon))
                {
                    k++;
                }
            }

            // **And the name after the comma, if there is one.**
            // **Und hier endet die Vorausschau, und das ist gemessen.**
            //
            // Ruby 1.8.1's own `parse.y`:
            //
            // ```c
            // f_opt : tIDENTIFIER '=' arg_value
            // f_arg : f_norm_arg | f_arg ',' f_norm_arg
            // ```
            //
            // **So `def m(a = 10, b = 20)` ist eine Parameterliste, und
            // das Komma zwischen `a = 10` und `b = 20` gehoert zu
            // `f_arg ',' f_norm_arg`** -- **und nicht zu einem `mlhs`.**
            //
            // **Und eine Vorausschau, die ueber das erste `=` hinauslaeuft,
            // sieht irgendwo weiter hinten ein `=` und glaubt, es sei
            // eine Mehrfachzuweisung** -- **und dann verschiebt sie die
            // Parameter um eine Stelle, und `def m(a = 10, b = 20)` bindet
            // die 10 an `b` und die 20 an nichts.**
            //
            // **Und das ist keine Vermutung: zwölf Fehlschlaege in zwei
            // Tests sahen genau so aus.**
            if (k < _tokens.Count
                && StartsAValueAt(k)
                && k + 1 < _tokens.Count
                && _tokens[k + 1].Text == "=")
            {
                // **Und ein Name links von `=` ist ein Parameter mit
                // Vorgabe, und keine zweite Zuweisung.**
                //
                // **Und das ist nur dann wahr, und wenn kein Stern
                // vorausging.** **Gemessen an `parse.y`:** `mlhs_basic :
                // mlhs_head tSTAR mlhs_node { $$ = NEW_MASGN($1, $3); }` --
                // **und `a, *rest = c` hat ein `=` hinter dem Namen, und
                // es ist trotzdem ein `mlhs`.**
                //
                // **Und ohne diese Unterscheidung verwarf der Leser den
                // Splat und `$make, *rest = Shellwords.shellwords($make)`
                // endete als `$make` allein, und das Komma kam an die
                // Spitze von `ParsePrimary`.**
                var stern = false;
                for (var r = _index; r < k; r++)
                {
                    if (_tokens[r].Text == "*")
                    {
                        stern = true;
                        break;
                    }
                }

                // **Und ein Name links von `=` ist der LETZTE Name der
                // Zielseite, und das `=` steht dahinter.**
                //
                // **Gemessen an `parse.y`: `arg : lhs '=' arg`** -- **und
                // das `=` gehoert zu `arg` und nicht zu `lhs`, und es steht
                // direkt hinter dem letzten Namen.**
                //
                // **Und k stand auf diesem letzten Namen**, **und die
                // Rueckgabe am Ende der Methode fragte genau den
                // falschen Token**, **und `a, *rest = x` sah deshalb aus
                // wie `sprite.draw(x, y)`.**
                if (k + 1 < _tokens.Count && _tokens[k + 1].Text == "=")
                {
                    k++;
                }

                break;
            }

            if (k < _tokens.Count
                && StartsAValueAt(k)
                && k + 1 < _tokens.Count
                && _tokens[k + 1].Text == ",")
            {
                k++;
                continue;
            }

            break;
        }

        // **Und `k` steht jetzt auf dem LETZTEN Namen, und nicht auf dem
        // `=`, und die alte Rueckgabe fragte genau den falschen Token.**
        //
        // **Gemessen an `parse.y`:** `lhs : mlhs_basic`, und
        // `mlhs_basic : mlhs_head | mlhs_head mlhs_item | ...`, und
        // `mlhs_head : mlhs_item ','` -- **und das `=` kommt in `arg`:
        // `arg : lhs '=' arg`.** **Also gehoert das `=` nicht zur
        // Zielseite und steht hinter ihr.**
        //
        // **Und eine Rueckgabe, die `k` selbst prueft, sieht den letzten
        // Namen und sagt nein** -- **und dann bekam `a, b, c = 1, 2, 3`
        // das Komma als Anweisungsanfang, und derselbe Fehler kam bei
        // `a, b = x` und bei `a, *b = x` ohne jeden Stern.**
        while (k < _tokens.Count
            && (_tokens[k].Kind == RubyTokenKind.Newline
                || _tokens[k].Kind == RubyTokenKind.Semicolon))
        {
            k++;
        }

        return k < _tokens.Count
            && _tokens[k].Kind == RubyTokenKind.Operator
            && _tokens[k].Text == "=";
    }

    /// <summary>Whether a token at an index begins a value.</summary>
    private bool StartsAValueAt(int pIndex) =>
        pIndex < _tokens.Count
        && StartsAValue(_tokens[pIndex]);

    /// <summary>
    /// The values on the right of a <c>=</c> that has several names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a separate reader and not the same one with a
    /// flag turned off, and that is measured.</strong>
    /// </para>
    /// <para>
    /// <strong>Measured at <c>parse.y</c>: <c>arg : lhs '=' arg</c>, and
    /// this second <c>arg</c> does not lead into <c>mlhs</c> -- <strong>so
    /// a reader that ran the name reader over the values treated
    /// <c>1, 2</c> as two names.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>a, b = 1, 2</c> is still <c>[1, 2]</c></strong>,
    /// <strong>because the grammar's <c>NEW_MASGN(list_append($1, $2), 0)
    /// </c> turns the right side into a list</strong> -- <strong>so the
    /// value of a multiple assignment has as many elements as the target
    /// has names, and every comma leads to the next one.</strong>
    /// </para>
    /// <para>
    /// <strong>And turning the name reader's flag off was not enough
    /// either</strong>, <strong>because then the comma of the right side
    /// was left uneaten and came to the front of <c>ParsePrimary</c>.</strong>
    /// <strong>So it needs its own loop, and that loop is here.</strong>
    /// </para>
    /// </remarks>
    private RubyNode ReadWertListe()
    {
        var werte = new List<RubyNode> { ParseTernary() };

        while (Is(","))
        {
            Take();
            SkipNewlines();
            if (!StartsAValue())
            {
                break;
            }

            werte.Add(ParseTernary());
            SkipNewlines();
        }

        return werte.Count == 1
            ? werte[0]
            : new RubyNode
            {
                Kind = RubyNodeKind.Array,
                Line = werte[0].Line,
                Children = [.. werte],
            };
    }

    /// <summary>Several names on the left of one <c>=</c>.</summary>
    /// <remarks>
    /// <strong>And the engine writes this as <c>NEW_MASGN</c>, measured at
    /// <c>parse.y</c> -- and the operator is written <c>masgn</c> here so a
    /// caller can tell "one name" from "a list of names".</strong>
    /// <strong>And a single name is not wrapped</strong>, <strong>so the
    /// shape a game writes stays the shape the tree has.</strong>
    /// </remarks>
    private static RubyNode Masgn(List<RubyNode> pNames) => new()
    {
        Kind = RubyNodeKind.Assignment,
        Operator = "masgn",
        Line = pNames.Count > 0 ? pNames[0].Line : 0,
        Children = [.. pNames],
    };

    private RubyNode ParseAssignment(bool pZiel = true)
    {
        // **Und nur die linke Seite ist ein `mlhs`, und das ist gemessen.**
        //
        // Ruby 1.8.1's own `parse.y`: `arg : lhs '=' arg` -- **und `arg`
        // ist kein `mlhs`, und nur `lhs` ist eines**, **denn
        // `lhs : mlhs_basic` und `mlhs_basic : mlhs_head`**.
        //
        // **Und ein Leser, der beide Seiten mit demselben Aufruf liest,
        // liest `a = 1, 2` als eine Zuweisung an `a, 1`** -- **und dann
        // kommt das Komma von `2` an die Spitze von `ParsePrimary` und
        // `a, b, c = 1, 2, 3` bricht bei Offset 11 ab.**
        //
        // **Also liest der Aufrufer die rechte Seite mit dem
        // Ausdrucksleser**, **und der kennt keine Komma-Schleife.**
        var links = ParseAssignmentTarget(pZiel);
        var left = links.Count == 1 ? links[0] : Masgn(links);
        if (Is("=") || Is("=>"))
        {
            // **Und `links` ist jetzt eine Liste, und das ist gemessen.**
            //
            // Ruby 1.8.1's `parse.y`: `mlhs : mlhs_basic | '(' mlhs_entry
            // ')'`, and `mlhs_basic : mlhs_head | mlhs_head mlhs_item |
            // mlhs_head tSTAR mlhs_node | mlhs_head tSTAR | tSTAR
            // mlhs_node | tSTAR`, and `mlhs_head : mlhs_item ','`.
            //
            // **So `$make, *rest = Shellwords.shellwords($make)` legal
            // ist** -- **und der Leser, der die Zielseite als einen
            // Ausdruck las, verweigerte es bei jedem echten Skript, das
            // eine Liste auspackt.** **Und `instruby.rb` Zeile 31 macht
            // genau das.**

            var op = Take().Text;

            // **Und die rechte Seite ist eine Namensliste, und kein
            // einfacher Ausdruck, und beides ist gemessen.**
            //
            // Ruby 1.8.1's own `parse.y`: `arg : lhs '=' arg` -- **und
            // dieses zweite `arg` kann kein Tupel sein**, **denn `arg`
            // fuehrt nicht in `mlhs`.**
            //
            // **Und `a, b = 1, 2` ist trotzdem `[1, 2]`**, **denn
            // `NEW_MASGN(list_append($1, $2), 0)` und die rechte Seite wird
            // in der Grammatik zu einer Liste gemacht** -- **und der Wert
            // einer Mehrfachzuweisung hat also so viele Elemente wie die
            // Zielseite Namen, und jedes Kommas fuehrt zum naechsten.**
            //
            // **Und `pZiel` durchzureichen war falsch** (**dann las der
            // Leser die rechte Seite als Namen und stiess auf das Komma**),
            // **und `pZiel` hart auf false zu setzen war auch falsch**
            // (**dann blieb das Komma der rechten Seite ungegessen und kam
            // an die Spitze von `ParsePrimary` -- und genau das war der
            // Fehler bei `a, b = 1, 2`**).
            //
            // **Also liest die rechte Seite ihre eigene Liste.**
            // **Und auch bei EINEM Namen darf der Wert ein Komma
            // tragen**, **denn `a = 1, 2` ist `[1, 2]` und nicht `1`
            // gefolgt von einem zweiten Ausdruck** -- **und Ruby 1.8.1
            // schreibt das nicht als Tupel, sondern der Wert wird von der
            // Zielliste her aufgeteilt.**
            //
            // **Und eine Kette hat beliebig viele Glieder, und nicht
            // zwei** -- **und `@name = @date = @id = nil` bei
            // `mdoc2man.rb` Zeile 53 hat drei.** **Und ein `if` liest
            // eines, und der Rest stand danach als Anweisung da.**
            //
            // **Und `a = b = c` ist eine Kette**, **und das zweite `=` ist
            // keine Liste und kein Komma** -- **gemessen an `parse.y`:
            // `arg : lhs '=' arg`, und das zweite `arg` kann wieder ein
            // `lhs '=' arg` sein**, **und `%right '=' tOP_ASGN` gibt dem
            // `=` die lockerste Bindung von allen.**
            var right = ReadWertListe();

            while (Is("=") || Is("=>"))
            {
                var op2 = Take().Text;
                var danach = ReadWertListe();
                right = new RubyNode
                {
                    Kind = RubyNodeKind.Assignment,
                    Operator = op2,
                    Line = right.Line,
                    Children = [right, danach],
                    Role_Children =
                    [
                        new() { Role = RubyNodeRole.Target, Node = right },
                        new() { Role = RubyNodeRole.Value, Node = danach },
                    ],
                };
            }
            return new RubyNode
            {
                Kind = RubyNodeKind.Assignment,
                Operator = op,
                Line = left.Line,
                Children = [left, right],
                Role_Children =
                [
                    new() { Role = RubyNodeRole.Target, Node = left },
                    new() { Role = RubyNodeRole.Value, Node = right },
                ],
            };
        }
        // **Und `||=` und `&&=` sind dabei, und das ist gemessen.**
        //
        // Ruby 1.8.1's own `parse.y`, in the `arg` production:
        //
        // ```c
        // | var_lhs tOP_ASGN arg
        //       if ($2 == tOROP)  { $$ = NEW_OP_ASGN_OR(gettable(vid), $1); }
        //   if ($2 == tANDOP) { $$ = NEW_OP_ASGN_AND(gettable(vid), $1); }
        // ```
        //
        // **And the lexer delivers `||=` and `&&=` as `tOP_ASGN` with
        // `yylval.id` set to `tOROP` or `tANDOP`** -- **so they are one
        // token with a kind inside it, and not two spellings of `||`.**
        //
        // **And they are not binary operators.** `a ||= b` writes `a` only
        // when `a` is falsey, and `a &&= b` only when `a` is truthy, and a
        // reader that treats them as `a = a || b` gets the same answer
        // here and a different one for a receiver with a side effect:
        // `x.ivar ||= 1` writes the receiver once under the engine and
        // twice under the substitution.
        //
        // **And `mkconfig.rb` line 4 opens with three of them:**
        // `$srcdir ||= nil`, `$install_name ||= nil`, `$so_name ||= nil`.
        foreach (var op in new[]
        {
            "+=", "-=", "*=", "/=", "%=", "**=", "<<=", ">>=", "|=", "&=",
            "^=", "||=", "&&=",
        })
        {
            if (Is(op))
            {
                _index++;

                // **Und auch hinter einem `+=` steht ein `arg`.**
                var right = ParseAssignment(false);
                return new RubyNode
                {
                    Kind = RubyNodeKind.OpAssignment,
                    Operator = op,
                    Line = left.Line,
                    Children = [left, right],
                };
            }
        }
        return left;
    }

    private RubyNode ParseTernary()
    {
        var condition = ParseBinary(0);
        if (Is("?"))
        {
            _index++;
            SkipNewlines();
            // The colon separates the two branches, so it is put aside while the
            // first branch is read. Without that a branch ending in a range
            // would swallow the colon and the second branch would be left with
            // nothing to read.
            var whenTrue = ParseTernaryBranch();
            SkipNewlines();
            Expect(":");
            SkipNewlines();
            var whenFalse = ParseTernary();
            return new RubyNode
            {
                Kind = RubyNodeKind.Ternary,
                Operator = "?",
                Line = condition.Line,
                Children = [condition, whenTrue, whenFalse],
                Role_Children =
                [
                    new() { Role = RubyNodeRole.Condition, Node = condition },
                    new() { Role = RubyNodeRole.WhenTrue, Node = whenTrue },
                    new() { Role = RubyNodeRole.WhenFalse, Node = whenFalse },
                ],
            };
        }
        return condition;
    }

    /// <summary>Reads the branch after a question mark, stopping at the colon.</summary>
    private RubyNode ParseTernaryBranch()
    {
        if (Is(":"))
        {
            return new RubyNode { Kind = RubyNodeKind.Nil, Line = Current.Line };
        }
        return ParseExpression();
    }

    private RubyNode ParseBinary(int pMinLevel)
    {
        var left = ParseUnary();
        while (true)
        {
            // **Und `and`, `or` und `not` sind Schluesselwoerter und
            // keine Operatoren, und die Grammatik kennt alle vier
            // Spielarten als eine.**
            //
            // **Gemessen an `parse.y` Zeile 277:**
            // `%token tANDOP tOROP /* && and || */` -- **der Kommentar
            // nennt alle vier Formen fuer ein Token, und `&&` und `||`
            // liefert der Lexer als `Operator`, `and` und `or` als
            // `Keyword`.**
            //
            // **Und die Tabelle muss an dieser Stelle beide annehmen**,
            // **denn `BindingPower` kennt jetzt `or`, `and` neben `||`
            // und `&&`** -- **und eine Pruefung, die nur auf
            // `Operator` schaut, laesst jedes `and` und `or` einer
            // echten Ruby-Datei als zwei Anweisungen fallen.**
            if (Current.Kind != RubyTokenKind.Operator
                && Current.Kind != RubyTokenKind.Keyword)
            {
                return left;
            }

            if (!BindingPower.TryGetValue(Current.Text, out var level))
            {
                return left;
            }
            // A level below the one this call may consume stops here, which is
            // what binds the outer expression to the caller instead.
            if (level < pMinLevel)
            {
                return left;
            }
            var op = Take().Text;
            SkipNewlines();
            RubyNode node;
            if (op is ".." or "...")
            {
                // The grammar makes the ranges non associative and lets the
                // start be missing, so an endless range is legal and `1..2..3`
                // is not something to guess at.
                var end = StartsAValue()
                    ? ParseBinary(level + 1)
                    : new RubyNode { Kind = RubyNodeKind.Nil, Line = Current.Line };
                node = new RubyNode
                {
                    Kind = RubyNodeKind.Range,
                    Operator = op,
                    Line = left.Line,
                    Children = [left, end],
                };
            }
            else
            {
                // Every level here is left associative, so the right side is
                // read at one level tighter. Power is the exception in Ruby, and
                // it is not in this table's left associative set.
                var right = op == "**"
                    ? ParseBinary(level)
                    : ParseBinary(level + 1);
                node = new RubyNode
                {
                    Kind = RubyNodeKind.Binary,
                    Operator = op,
                    Line = left.Line,
                    Children = [left, right],
                    Role_Children =
                    [
                        new() { Role = RubyNodeRole.Left, Node = left },
                        new() { Role = RubyNodeRole.Right, Node = right },
                    ],
                };
            }
            left = node;
        }
    }

    /// <summary>
    /// Reads a 'not', which the grammar gives a level between the logical pair
    /// and the assignment.
    /// </summary>
    /// <remarks>
    /// Reading it as a unary operator would bind it tighter than every operator
    /// in the table, which is the opposite end of the scale: `not a == b` is
    /// `not (a == b)` in Ruby and not `(not a) == b`.
    /// </remarks>
    private RubyNode ParseNot()
    {
        var op = Take();
        // The operand is everything the 'not' level may take, which is the
        // assignment below it, so `not a = b` negates the assignment.
        var operand = ParseAssignment();
        return new RubyNode
        {
            Kind = RubyNodeKind.Not,
            Operator = "not",
            Line = op.Line,
            Children = [operand],
        };
    }

    private RubyNode ParseUnary()
    {
        if (IsKeyword("not"))
        {
            return ParseNot();
        }
        if (Current.Kind == RubyTokenKind.Operator
            && (Current.Text == "!" || Current.Text == "-" || Current.Text == "+"
                || Current.Text == "~"))
        {
            var op = Take();
            var operand = ParseUnary();
            return new RubyNode
            {
                Kind = RubyNodeKind.Unary,
                Operator = op.Text,
                Line = op.Line,
                Children = [operand],
            };
        }
        return ParsePostfix(ParsePrimary());
    }

    private RubyNode ParsePostfix(RubyNode pNode)
    {
        var node = pNode;
        while (true)
        {
            if (Is(".") || Is("&.") || Is("::"))
            {
                var separator = Take().Text;
                SkipNewlines();
                var name = ReadMemberName();

                // **Und `::` ohne Klammern ist eine Konstante und kein
                // Aufruf.** `RPG::Actor` ist ein Name,
                // **und `RPG::Actor.new(1)` ist beides: erst der Name, dann
                // der Aufruf darauf.**
                //
                // **Vor dem Klammern-Zweig**, weil `RPG::Actor` keine
                // Klammern hat,
                // **und ein Leser, der hier immer einen Aufruf baute, las
                // `RPG::Actor` als "rufe `Actor` auf `RPG` auf"**,
                // **und `RPG` ist ein Modul, und Module haben keine Methode
                // `Actor`** -- **also nil, und dann `nil.new`, und dann
                // `nil.id`, und drei Fehlermeldungen ueber einen Host, der
                // nichts davon getan hat.**
                //
                // **Und `A::b` bleibt ein Aufruf**, wenn links kein Name
                // stand: `held::name` ist eine Methode auf `held`,
                // **und ein Leser, der den Namen auch aus einem Aufruf
                // baute, wuerde `p::x` zu einem Objekt mit dem Namen
                // `p::x` machen** -- **und die Suche nach diesem Namen
                // fiele in jedem Spiel immer ins Leere.**
                if (separator == "::" && !Is("("))
                {
                    node = node.Kind == RubyNodeKind.Constant
                        ? new RubyNode
                        {
                            Kind = RubyNodeKind.Constant,
                            Name = (node.Name ?? string.Empty) + "::" + name,
                            Line = node.Line,
                        }
                        : new RubyNode
                        {
                            Kind = RubyNodeKind.Call,
                            Name = name,
                            Line = node.Line,
                            Children = [node],
                            Role_Children = CallParts(node, Array.Empty<RubyNode>()),
                        };
                    continue;
                }

                if (Is("("))
                {
                    var arguments = ReadArguments();
                    node = new RubyNode
                    {
                        Kind = RubyNodeKind.Call,
                        Name = name,
                        Line = node.Line,
                        Children = [node, .. arguments],
                        Role_Children = CallParts(node, arguments),
                    };
                    continue;
                }
                node = new RubyNode
                {
                    Kind = RubyNodeKind.Call,
                    Name = name,
                    Line = node.Line,
                    Children = [node],
                    Role_Children = CallParts(node, Array.Empty<RubyNode>()),
                };
                continue;
            }
            // **Und eine Klammerliste nach einem Namen mit Leerzeichen
            // ist ein Argument, und kein Index.** `puts [1, 2]` ist
            // `puts([1, 2])` und `a[1]` ist ein Index.
            if (pNode.Kind == RubyNodeKind.Identifier
                && Is("[")
                && AbstandDavor)
            {
                _index++;
                SkipNewlines();
                var eintraege = new List<RubyNode>();
                while (!Is("]"))
                {
                    eintraege.Add(ParseExpression());
                    SkipNewlines();
                    if (Is(","))
                    {
                        _index++;
                        SkipNewlines();
                    }
                }
                Expect("]");
                var liste = new RubyNode
                {
                    Kind = RubyNodeKind.Array,
                    Line = pNode.Line,
                    Children = [.. eintraege],
                };
                node = new RubyNode
                {
                    Kind = RubyNodeKind.SelfCall,
                    Name = pNode.Name ?? string.Empty,
                    Line = pNode.Line,
                    Children = [pNode, liste],
                    Role_Children = CallParts(pNode, [liste]),
                };
                continue;
            }

            // **Und sonst ist eine Klammer ein Index.** `a[1]` und
            // `h[:a]` ohne Leerzeichen davor -- **und gemessen war
            // `puts [1, 2]` ein `Call name=[]` mit dem Empfaenger
            // `puts`, und der Host schrieb nichts.**
            if (Is("[") && (pNode.Kind != RubyNodeKind.Identifier
                || !AbstandDavor))
            {
                _index++;
                SkipNewlines();
                var arguments = new List<RubyNode>();
                while (!Is("]"))
                {
                    arguments.Add(ParseExpression());
                    SkipNewlines();
                    if (Is(","))
                    {
                        _index++;
                        SkipNewlines();
                    }
                }
                Expect("]");
                node = new RubyNode
                {
                    Kind = RubyNodeKind.Call,
                    Name = "[]",
                    Line = node.Line,
                    Children = [node, .. arguments],
                    Role_Children = CallParts(node, arguments),
                };
                continue;
            }
            // **Und ein Name mit Klammern und ohne Empfaenger ist ein
            // Aufruf auf `self`, und nicht auf den Namen.** `sprintf("%d", 5)`
            // und `raise "x"` schreiben alle Spiele,
            // **und `Call` wertet seinen ersten Kinder als Empfaenger aus** --
            // **und ein Name, den niemand gesetzt hat, ist nil**,
            // **und die Meldung lautete *„nil has no method 'sprintf' on this
            // host"***, **also ueber einen Empfänger, den der Leser selbst
            // erfunden hatte.**
            //
            // **Deshalb `SelfCall`, und der Name ist der Name.** Der
            // Interpreter weiß dann, dass `self` gemeint war,
            // **und ein Aufruf, der wirklich einen Empfaenger hat, bleibt
            // `Call`.**
            if (Is("(") && CanStartACall(node))
            {
                var arguments = ReadArguments();
                node = new RubyNode
                {
                    Kind = RubyNodeKind.SelfCall,
                    Name = node.Name ?? string.Empty,
                    Line = node.Line,
                    Children = arguments,
                    Role_Children = [.. arguments.Select(pArgument =>
                        new RubyNodePart
                        {
                            Role = RubyNodeRole.Argument,
                            Node = pArgument,
                        })],
                };
                continue;
            }
            if (IsKeyword("do") && !AfterACondition)
            {
                _index++;
                var parameters = ReadBlockParameters();
                var body = ReadBody("end");
                node = new RubyNode
                {
                    Kind = RubyNodeKind.Block,
                    Line = node.Line,
                    Children = [node, parameters, body],
                };
                continue;
            }
            if (Is("{"))
            {
                _index++;
                var parameters = ReadBlockParameters();
                var body = ReadBody("}");
                node = new RubyNode
                {
                    Kind = RubyNodeKind.Block,
                    Line = node.Line,
                    Children = [node, parameters, body],
                };
                continue;
            }
            return node;
        }
    }

    /// <summary>
    /// True when a following '(' is a call rather than a grouping.
    /// </summary>
    /// <remarks>
    /// A parenthesis right after a name with a space is a call; right after a
    /// literal it is a grouping. `puts (1)` calls, `(1 + 2)` groups, and the two
    /// only differ in what came before, which is why this is asked at all.
    /// </remarks>
    private static bool CanStartACall(RubyNode pNode)
    {
        return pNode.Kind == RubyNodeKind.Identifier;
    }

    private string ReadMemberName()
    {
        var token = Current;
        if (token.Kind is RubyTokenKind.Identifier or RubyTokenKind.Constant
            or RubyTokenKind.Keyword)
        {
            _index++;
            return token.Text;
        }

        // **Und die Vergleichs-Operatoren, denn die sind Namen.**
        // `def <=>(andere)` ist, was ein Spiel fuer eine sortierbare Klasse
        // schreibt, **und der Leser hat hier keinen Operator erwartet** --
        // **also wurde jede sortierbare Klasse in einem Syntaxfehler
        // abgelehnt, und der Fehler nannte den Operator, nicht die Stelle.**
        //
        // **Nur die vier, die Ruby als Methodennamen kennt.** `+` und `[]`
        // sind auch Namen, **aber die kommen in einem anderen Zweig und
        // werden hier nicht geraten** -- **ein Leser, der alles zulaesst,
        // macht aus `def ` + einem Tippfehler eine Methode**, die es nicht gibt.
        if (token.Kind == RubyTokenKind.Operator
            && token.Text is "<=>" or "==" or "===" or "<<" or ">>")
        {
            _index++;
            return token.Text;
        }

        throw new RubyParseException(
            $"A member name was expected at offset {token.Offset}, but '{token.Text}' is there.",
            token.Line);
    }

    /// <summary>
    /// A member name that ends in `=`, which is how a setter is written.
    /// </summary>
    /// <returns>The name with its `=`, or null when there is none.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>def hp=(v)</c> is a setter, and the <c>=</c> is part
    /// of the name.</strong> Ruby allows it, **and <c>attr_writer</c> builds
    /// exactly such a method**,
    /// **and a reader that stopped at the identifier would have read
    /// <c>hp</c> and then found an <c>=</c> where a parameter list
    /// belongs** — **and every game that writes a setter by hand would be
    /// a syntax error, while <c>attr_writer</c> in the same file worked.**
    /// </para>
    /// <para>
    /// <strong>And <c>==</c> is not a setter's tail.</strong>
    /// <c>def ==(other)</c> compares, **and a reader that took the first
    /// <c>=</c> would have named the method <c>==</c> and then complained
    /// about the second one.**
    /// </para>
    /// </remarks>
    private string? LeseSchreiberName()
    {
        // **Und `def self.x` ist kein Schreiber, und der Punkt ist kein
        // Name.** Ohne diese Grenze wuerde `LeseSchreiberName` dort eine
        // Ausnahme werfen, wo vorher der Punktpfad lief,
        // **und `def self.x=` waere genau so kaputt wie `def self.x`.**
        if (Current.Kind is not (RubyTokenKind.Identifier
            or RubyTokenKind.Constant or RubyTokenKind.Keyword)
            && !(Current.Kind == RubyTokenKind.Operator
                && Current.Text is "<=>" or "==" or "===" or "<<" or ">>"))
        {
            return null;
        }

        // **Und der Blick nach vorn, bevor der Name gelesen wird.** Der
        // Name wird hier *einmal* gelesen,
        // **und ein Leser, der ihn zuerst las und dann nach dem `=` sah,
        // haette den Namen bei `def n` ohne `=` schon verbraucht** --
        // **und der Aufrufer haette ihn noch einmal gelesen** -- **und
        // `ReadMemberName` waere auf ein `end` gelaufen, und die Meldung
        // waere *A member name was expected at offset 44*.**
        // *Der Fehler nannte den Namen, und der Name war richtig: der
        // zweite Lesevorgang hatte ihn aufgegessen.*
        //
        // **Und `def ==(other)` ist ein Vergleich und kein Schreiber.** Das
        // naechste Zeichen ist dann `==` und nicht `=`,
        // **und `Is("=")` sieht `Current.Text == "="` an** -- **und
        // `Current` ist hier `==`** -- **und der Vergleich war wahr, und
        // die Methode hiess `==`**, **und der Vergleich, den ein Spiel
        // braucht, war ein Schreiber mit einem Namen `==`.**
        if (Current.Kind == RubyTokenKind.Operator
            && Current.Text is "==" or "===" or "=>" or "<=" or ">="
                or "!=" or "=~")
        {
            return null;
        }

        var hier = _index;
        var name = ReadMemberName();
        if (!Is("=") || Is("==") || Is("=>") || Is("==="))
        {
            // **Und der Index muss zurueck, weil der Aufrufer den Namen
            // selbst liest, wenn hier keiner herauskam.**
            _index = hier;
            return null;
        }

        _index++;
        return name + "=";
    }
    /// <summary>
    /// Whether the next tokens are a bare name and a colon.
    /// </summary>
    /// <returns>true when a name is followed by a colon.</returns>
    /// <remarks>
    /// <para>
    /// <strong>A name and not a symbol.</strong> Ruby writes <c>k: 3</c> and
    /// <c>:k =&gt; 3</c> as the same thing,
    /// <strong>and a reader that only understood the second would have made
    /// every game's keyword-argument call a syntax error</strong> — and that
    /// is the spelling a person writes.
    /// </para>
    /// <para>
    /// <strong>And the colon has to be right there.</strong> <c>a ? b : c</c>
    /// also has a colon,
    /// <strong>and a reader that only looked for a colon anywhere would have
    /// taken the <c>b</c> of a ternary for a key</strong> — and a game that
    /// writes a ternary as an argument would have had its second half read as
    /// a named value.
    /// </para>
    /// <para>
    /// <strong>And a constant is a key too.</strong> <c>Sprite: 1</c> is
    /// ordinary Ruby, <strong>and a reader that only knew lower-case names
    /// would have made every game's named argument with a class name a
    /// syntax error.</strong>
    /// </para>
    /// </remarks>
    private bool StartsAKeyAndThenAColon()
    {
        if (Current.Kind is not (RubyTokenKind.Identifier or RubyTokenKind.Constant))
        {
            return false;
        }

        // **Der Nachbar, ohne ihn zu nehmen.** `_index + 1` ist der naechste
        // Token, und der Index wandert und kommt zurueck.
        if (_index + 1 >= _tokens.Count)
        {
            return false;
        }

        return _tokens[_index + 1].Kind == RubyTokenKind.Delimiter
            && _tokens[_index + 1].Text == ":";
    }




    private List<RubyNode> ReadArguments()
    {
        Expect("(");
        SkipNewlines();
        var arguments = new List<RubyNode>();
        if (Is(")"))
        {
            _index++;
            return arguments;
        }
        while (true)
        {
            SkipNewlines();

            // **`name: wert` ist ein Paar und kein Ausdruck.** `ParseExpression`
            // liest `k` als Bezeichner, **dann steht `:` da, und der
            // Ausdruck ist vorbei** -- der Aufruf wurde als Fehler gemeldet
            // und `f(k: 3)` **haette in einem echten Skript nicht
            // funktioniert**. Das ist die Form, die jeder Ruby-Schreibende
            // benutzt, **und sie war nicht lesbar.**
            //
            // **Und der Schluessel wird zum Symbol.** `k: 3` heisst
            // `:k => 3` in einem Hash, **und ein Leser, der den Bezeichner
            // nimmt, haette einen Hash mit einem Schluessel, den ein Spiel
            // nie schreibt.**
            if (StartsAKeyAndThenAColon())
            {
                var schluessel = Take();
                _index++;
                SkipNewlines();
                arguments.Add(new RubyNode
                {
                    Kind = RubyNodeKind.Binary,
                    Operator = "=>",
                    Name = schluessel.Text,
                    Line = schluessel.Line,
                    Children =
                    [
                        Literal(
                            RubyNodeKind.Symbol, schluessel.Line, null, null,
                            null, schluessel.Text),
                        ParseExpression(),
                    ],
                });
            }
            else
            {
                arguments.Add(ParseExpression());
            }

            SkipNewlines();
            if (Is(","))
            {
                _index++;
                continue;
            }
            break;
        }
        SkipNewlines();
        Expect(")");
        return arguments;
    }

    /// <summary>
    /// Reads a body up to one of several keywords, and leaves that keyword in
    /// the stream.
    /// </summary>
    /// <param name="pClosers">
    /// The keywords that end the body. <strong>More than one, because
    /// <c>if</c> is ended by <c>else</c> as well as by <c>end</c>.</strong>
    /// </param>
    /// <remarks>
    /// <strong>The closer is not consumed here.</strong> Every existing caller
    /// takes it itself — <c>ReadBody</c> is also the body of a
    /// <c>def</c>, a <c>class</c> and a <c>do</c> block, where the
    /// <c>end</c> belongs to the opening keyword's own parse. <strong>A
    /// version that consumed it would have made <c>def foo; end</c> one
    /// <c>end</c> short</strong> and handed the next statement to the wrong
    /// caller.
    /// </remarks>
    /// <summary>
    /// Reads the `rescue`, `else` and `ensure` arms after a body, and the
    /// `end` that closes them.
    /// </summary>
    /// <param name="pKoerper">The statements before the first arm.</param>
    /// <param name="pLine">Where the statement began.</param>
    /// <returns>The node, with the body first and the arms in run order.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the one place, because `begin` and `def` take the
    /// same arms.</strong> `def m; a; rescue; b; end` is
    /// <c>Kernel#load</c>'s way of writing it,
    /// <strong>and a reader with two copies would have one of them
    /// wrong</strong> — and the wrong one would be the one no test
    /// happened to write.
    /// </para>
    /// <para>
    /// <strong>And the order is the order they run in.</strong> Body,
    /// rescue, else, ensure —
    /// <strong>and <c>else</c> only runs when nothing was raised</strong>,
    /// which is its whole difference from a second <c>rescue</c> arm.
    /// </para>
    /// </remarks>
    private RubyNode ArmeSammeln(List<RubyNode> pKoerper, int pLine)
    {
        var kinder = new List<RubyNode>
        {
            new()
            {
                Kind = RubyNodeKind.Block,
                Name = "end",
                Line = pLine,
                Children = pKoerper,
            },
        };

        var letzteKlasse = 1;
        while (IsKeyword("rescue"))
        {
            _index++;
            var klassen = new List<RubyNode>();
            var name = string.Empty;

            // **Und `rescue => e` bindet den Fehler an einen Namen**, und
            // **die Klassen kommen vor dem Pfeil.**
            // `rescue A, B => e` faengt beide,
            // **und ein Leser, der nur den ersten Namen nahm, haette eine
            // game's second error class escape** — and the game would crash
            // on the error it thought it had handled.
            if (Is("=>"))
            {
                _index++;
                name = ReadMemberName();
            }
            else if (NenntKlassen())
            {
                while (true)
                {
                    klassen.Add(ParseExpression());
                    if (Is(","))
                    {
                        _index++;
                        continue;
                    }

                    break;
                }

                if (Is("=>"))
                {
                    _index++;
                    name = ReadMemberName();
                }
            }

            // **Und die Klassenliste geht als letzter Knoten in den Arm,
            // und nicht in eine Liste, in die man nachtraeglich etwas
            // schreibt.** `Children` ist eine Liste, die man nicht
            // veraendert,
            // **und ein Leser, der sie erweitert haette, muesste den
            // Knoten schon kennen** -- **und der Knoten entsteht aus genau
            // den Angaben, um die es hier geht.**
            var retter = ParseStatements("rescue", "else", "ensure", "end");
            var inhalt = new List<RubyNode>(retter);
            if (klassen.Count > 0)
            {
                inhalt.Add(new RubyNode
                {
                    Kind = RubyNodeKind.Array,
                    Line = pLine,
                    Children = klassen,
                });
            }

            kinder.Add(new RubyNode
            {
                Kind = RubyNodeKind.Block,
                Name = name,
                Line = pLine,
                Children = inhalt,
            });
            letzteKlasse = kinder.Count;
        }

        if (IsKeyword("else"))
        {
            _index++;
            var sonst = ParseStatements("ensure", "end");
            kinder.Insert(letzteKlasse, new RubyNode
            {
                Kind = RubyNodeKind.Block,
                Name = "else",
                Line = pLine,
                Children = sonst,
            });
        }

        if (IsKeyword("ensure"))
        {
            _index++;
            var immer = ParseStatements("end");
            kinder.Add(new RubyNode
            {
                Kind = RubyNodeKind.Block,
                Name = "ensure",
                Line = pLine,
                Children = immer,
            });
        }

        if (!Is("end"))
        {
            throw new RubyParseException(
                $"'end' was expected at offset {Current.Offset}, but "
                    + $"'{Current.Text}' is there.",
                Current.Line);
        }

        _index++;
        return new RubyNode
        {
            Kind = RubyNodeKind.Begin,
            Line = pLine,
            Children = kinder,
        };
    }

    /// <summary>
    /// Reads `begin` with its arms, or without them.
    /// </summary>
    /// <param name="pLine">Where the `begin` was written.</param>
    /// <returns>The node.</returns>
    /// <remarks>
    /// <strong>And it is a `Begin` node even with no arms.</strong> That is
    /// what a `begin` without a `rescue` is,
    /// <strong>and a reader that made a plain block out of it would have had
    /// two shapes for one keyword</strong> — and the second one would be the
    /// one no test wrote.
    /// </remarks>
    private RubyNode ReadBegin(int pLine)
    {
        var koerper = ParseStatements("rescue", "else", "ensure", "end");
        return ArmeSammeln(koerper, pLine);
    }


    /// <summary>
    /// Wraps statements already read into a body node, and takes the `end`
    /// that closes them.
    /// </summary>
    /// <param name="pStatements">The statements.</param>
    /// <param name="pLine">Where the statement began.</param>
    /// <returns>The body node.</returns>
    /// <remarks>
    /// <strong>And it takes the `end`, because the caller has not.</strong>
    /// <c>ReadBody</c> reads and takes,
    /// <strong>and a reader that left the `end` for the caller would have had
    /// two places to take it</strong> — and one of them would have been
    /// forgotten. **This exists because the caller had to read the
    /// statements itself to see whether an arm followed.**
    /// </remarks>
    /// <summary>
    /// Whether an arm names the classes it catches.
    /// </summary>
    /// <returns>true when at least one name follows.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a name, and not "something is there".</strong>
    /// <c>rescue</c> and then <c>2</c> is an arm with no classes and a body
    /// that begins with 2,
    /// <strong>and a reader that always read an expression would take the
    /// body's first line for the class list</strong> — and then
    /// <c>begin; 1; rescue; 2; end</c> would not parse at all,
    /// **which is the commonest form of the sentence there is.**
    /// </para>
    /// <para>
    /// <strong>And a constant, or a name qualified with <c>::</c>, and not a
    /// number or a text.</strong> <c>rescue ZeroDivisionError</c> and
    /// <c>rescue RPG::Fehler</c> —
    /// <strong>and a game's own error class is exactly that second
    /// form</strong>, and a reader that only knew the bare name would have
    /// made a plugin's error uncaught.
    /// </para>
    /// </remarks>
    private bool NenntKlassen()
    {
        if (Current.Kind != RubyTokenKind.Constant)
        {
            return false;
        }

        // **Und nach dem Namen muss ein Pfeil oder ein Komma kommen.**
        // `rescue A => e` und `rescue A, B => e`,
        // **und `rescue` mit einem Rumpf, der mit einem Wort beginnt, hat
        // keine Klassen** --
        // **ein Leser, der nur auf "irgendein Name" schaute, wuerde
        // `rescue; name = 1; end` als `rescue name` lesen** und dann der
        // Zuweisung die Liste der Klassen klauen,
        // **und `rescue; name = 1; end` ist ein Muster, mit dem ein Spiel
        // seinen Fehler in einer Variablen ablegt.**
        // **Und `ReadMemberName` ruckt selbst weiter**, **also steigt
        // `_index` hier nicht noch einmal** --
        // **ein Leser, der beides tat, stuende nach dem Namen auf dem
        // Zeilenumbruch**, **und `rescue TypeError` waere dann ein
        // Syntaxfehler mit der Meldung *„a member name was expected"*,
        // **obwohl der Name davor richtig gelesen wurde.**
        var merke = _index;
        ReadMemberName();
        while (Is("::"))
        {
            _index++;
            ReadMemberName();
        }

        // **Und nach dem Namen darf eine neue Zeile kommen.**
        // `rescue ZeroDivisionError` steht fuer sich allein,
        // **und ohne das haette der Rumpf des Arms mit der Konstante
        // angefangen** -- **und `begin; 1/0; rescue ZeroDivisionError; 2;
        // end` waere dann ein Syntaxfehler**, **und genau das ist der
        // Satz, mit dem ein Spiel seinen eigenen Fehler behandelt.**
        //
        // **Und es bleibt bei "irgendein Name".** `rescue; name = 1; end`
        // ist ein Arm ohne Klassen,
        // **und der Name `name` am Zeilenanfang ist eine Zuweisung, keine
        // Klasse** -- **die Unterscheidung ist der Zeilenumbruch, und
        // `rescue name = 1` schreibt man nicht.**
        _index = merke;
        return true;
    }


    private RubyNode RumpfAus(List<RubyNode> pStatements, int pLine)
    {
        if (!Is("end"))
        {
            throw new RubyParseException(
                $"'end' was expected at offset {Current.Offset}, but "
                    + $"'{Current.Text}' is there.",
                Current.Line);
        }

        _index++;
        return new RubyNode
        {
            Kind = RubyNodeKind.Block,
            Name = "end",
            Line = pLine,
            Children = pStatements,
        };
    }


    private RubyNode ReadBody(string pCloser)
    {
        var statements = ParseStatements(pCloser);
        if (!Is(pCloser))
        {
            throw new RubyParseException(
                $"'{pCloser}' was expected at offset {Current.Offset}, but '{Current.Text}' is there.",
                Current.Line);
        }
        _index++;
        return new RubyNode
        {
            Kind = RubyNodeKind.Block,
            Name = pCloser,
            Line = statements.Count > 0 ? statements[0].Line : Current.Line,
            Children = statements,
        };
    }

    /// <summary>
    /// Reads a body that may be ended by one of several keywords, and stops
    /// at the first of them <em>without</em> consuming it.
    /// </summary>
    /// <remarks>
    /// <strong>This is the shape <c>if</c> needs and the one
    /// <see cref="ReadBody"/> cannot give.</strong> A <c>def</c> or a
    /// <c>class</c> is ended by exactly one keyword that belongs to it, so it
    /// reads the body and consumes the closer. An <c>if</c> is ended by
    /// <c>else</c> or by <c>end</c>, and <strong>the parser has to look at
    /// which of the two is there before it can decide whether there is a
    /// second branch at all</strong> — so the closer stays, and the
    /// <c>if</c> parse consumes it.
    /// </remarks>
    /// <summary>
    /// Reads one `when` arm of a `case`: its values, and its body.
    /// </summary>
    /// <remarks>
    /// <strong>A <c>when</c> takes several values and they are alternatives,
    /// not a conjunction</strong> — <c>when 1, 2, 3</c> matches all three, and
    /// that is the whole reason the node is a list of values with one body.
    /// <strong>An empty <c>when</c> is the catch-all</strong> — a game writes
    /// <c>when then</c> as its <c>else</c>, and a reader that required a
    /// value would have said "does not begin an expression" on a form RPG_RT
    /// runs.
    /// </remarks>
    private RubyNode ParseWhen()
    {
        var when = Take().Text;
        SkipNewlines();
        var werte = new List<RubyNode>();
        // **Und die Liste der Werte endet am Zeilenumbruch, oder an einem
        // Komma, oder an `then`, und das ist gemessen.**
        //
        // Ruby 1.8.1's own `parse.y`:
        //
        // ```c
        // case_body  : kWHEN when_args then compstmt cases
        // when_args  : args
        // args       : arg_value
        // arg_value  : arg
        // arg        : lhs asgn | ... | primary
        // ```
        //
        // **Und `arg` endet an einem `tNL`, weil der Lexer ein
        // `tNL` nur dort erzeugt, wo ein Ausdruck nicht weitergehen
        // kann** -- **und der Lexer weiss nicht, ob er gerade in den
        // Werten eines `when` steht oder schon im Rumpf, und diese
        // Grenze kann er auch nicht wissen.**
        //
        // **Und ein Leser, der nur auf `then` wartet, liest den Rumpf
        // als Werte**, **und dann steht der Index hinter dem Rumpf, und
        // das `when` hinter ihm wird als Anweisung gelesen:**
        //
        // ```
        // 'when' at offset 33 does not begin an expression
        // ```
        while (!IsKeyword("then") && Current.Kind != RubyTokenKind.Newline
            && Current.Kind != RubyTokenKind.Semicolon
            && !Is("}") && !AtEnd)
        {
            werte.Add(ParseExpression());
            SkipNewlines();
            if (Is(","))
            {
                _index++;
                SkipNewlines();
                continue;
            }

            break;
        }

        SkipThen();
        // **Und ein Rumpf eines `when` laeuft nie bis zum `end` des
        // `case`, und das ist gemessen.**
        //
        // Ruby 1.8.1's own `parse.y`:
        //
        // ```c
        // case_body : kWHEN when_args then compstmt cases
        // cases     : opt_else | case_body
        // compstmt   : stmts opt_terms
        // stmts     : none | stmt | stmts terms stmt
        // ```
        //
        // **Und alle drei sind Closers, und `end` ist der letzte von
        // ihnen, und nicht der einzige, und das ist gemessen.**
        //
        // Ruby 1.8.1's own `parse.y`:
        //
        // ```c
        // case_body  : kWHEN when_args then compstmt cases
        // cases      : opt_else | case_body
        // opt_else   : none | kELSE compstmt
        // compstmt   : stmts opt_terms
        // stmts     : none | stmt | stmts terms stmt
        // ```
        //
        // **Und ein `stmt` ist nach der Grammatik nie ein `end`**, **und
        // `cases` hat kein `kEND`**, **und `opt_else` auch nicht** -- **und
        // das `kEND` des `case` steht in `primary`, ganz am Ende:**
        //
        // ```c
        // primary : kCASE expr_value opt_terms case_body kEND
        //          | kCASE opt_terms case_body kEND
        // ```
        //
        // **Und `end` ist ein Closer fuer einen `when`-Rumpf**, **und
        // `when` und `else` auch**, **und ein Leser, der nur `end`
        // kennt, frisst die `when`-Zweige hinter ihm** -- **und dann
        // steht der Index hinter dem letzten Zweig, und der `case` ist zu
        // Ende, bevor er vollstaendig gelesen ist.**
        var body = ReadBodyUntil("when", "else", "end");
        var children = new List<RubyNode>();
        children.AddRange(werte);
        children.Add(body);
        return new RubyNode
        {
            Kind = RubyNodeKind.Case,
            Name = when,
            Line = Current.Line,
            Children = children,
        };
    }

    private RubyNode ReadBodyUntil(params string[] pClosers)
    {
        var statements = ParseStatements(pClosers);
        foreach (var closer in pClosers)
        {
            if (Is(closer))
            {
                return new RubyNode
                {
                    Kind = RubyNodeKind.Block,
                    Name = closer,
                    Line = statements.Count > 0
                        ? statements[0].Line
                        : Current.Line,
                    Children = statements,
                };
            }
        }

        throw new RubyParseException(
            $"'{pClosers[0]}' was expected at offset {Current.Offset}, but "
                + $"'{Current.Text}' is there.",
            Current.Line);
    }

    private RubyNode ParsePrimary()
    {
        var token = Current;
        switch (token.Kind)
        {
            case RubyTokenKind.Integer:
                _index++;
                return Literal(RubyNodeKind.Integer, token.Line, token.Integer!.Value, null, null);
            case RubyTokenKind.Float:
                _index++;
                return new RubyNode
                {
                    Kind = RubyNodeKind.Float,
                    Real = token.Real,
                    Line = token.Line,
                };
            case RubyTokenKind.String:
            {
                _index++;
                SkipNewlines();
                // Two adjacent string literals are one string in Ruby, and a game
                // relies on that to split a long line without a backslash.
                var parts = new List<RubyStringPart>(token.Parts);
                var bytes = new List<byte>(token.Bytes ?? []);
                while (Current.Kind == RubyTokenKind.String)
                {
                    var next = Take();
                    parts.AddRange(next.Parts);
                    bytes.AddRange(next.Bytes ?? []);
                    SkipNewlines();
                }
                return new RubyNode
                {
                    Kind = RubyNodeKind.String,
                    Text = string.Concat(parts.Select(pPart => pPart.Resolved ?? pPart.Text)),
                    Bytes = [.. bytes],
                    Parts = parts,
                    Line = token.Line,
                };
            }
            case RubyTokenKind.Symbol:
                _index++;
                return Literal(RubyNodeKind.Symbol, token.Line, null, null, token.Value ?? token.Text);
            case RubyTokenKind.Regexp:
                _index++;
                return Literal(
                    RubyNodeKind.Regexp,
                    token.Line,
                    null,
                    null,
                    token.Value,
                    null,
                    token.Options);
            case RubyTokenKind.InstanceVariable:
                _index++;
                return Literal(
                    token.Text.StartsWith("@@", StringComparison.Ordinal)
                        ? RubyNodeKind.ClassVariable
                        : RubyNodeKind.InstanceVariable,
                    token.Line, null, null, null, token.Text);
            case RubyTokenKind.GlobalVariable:
                _index++;
                return Literal(RubyNodeKind.GlobalVariable, token.Line, null, null, null, token.Text);
            case RubyTokenKind.Constant:
            {
                _index++;
                var node = Literal(RubyNodeKind.Constant, token.Line, null, null, null, token.Text);
                if (Is("("))
                {
                    var arguments = ReadArguments();
                    return new RubyNode
                    {
                        Kind = RubyNodeKind.SelfCall,
                        Name = token.Text,
                        Line = token.Line,
                        Children = arguments,
                        // **Und die Argumente tragen ihre Rolle**, weil
                        // `EvaluateChildren` die Rollen liest und sonst
                        // **gar nichts findet**,
                        // **und `f(a ? b : c)` waere dann eine leere
                        // Argumentliste.**
                        Role_Children = [.. arguments.Select(pArgument =>
                            new RubyNodePart
                            {
                                Role = RubyNodeRole.Argument,
                                Node = pArgument,
                            })],
                    };
                }
                return node;
            }
            case RubyTokenKind.Identifier:
            {
                _index++;
                var node = Literal(RubyNodeKind.Identifier, token.Line, null, null, null, token.Text);
                if (Current.Kind == RubyTokenKind.Operator && Current.Text == "=")
                {
                    return node;
                }

                // **`draw(x)` ist ein Aufruf, und das ist die Form, die es
                // schon immer gab.** Sie steht hier zuerst, **weil sie der
                // klammerlose Zweig unten nicht ersetzt sondern ergaenzt** --
                // und beide sind ein Aufruf mit demselben Namen, nur mit
                // unterschiedlicher Herkunft im Baum. **Ein Leser, der nur
                // einen der beiden Zweige haette, haette `draw(x)` oder
                // `attr_accessor :hp` fuer eine Variable gehalten**, und das
                // sind die beiden Schreibweisen, die ein Spiel benutzt.
                // **Und `sprintf("%d", 5)` hat keinen Empfaenger, und
                // trotzdem wurde einer erfunden.** Der Name selbst stand
                // als erstes Kind,
                // **und `Call` wertet sein erstes Kind als Empfaenger aus** --
                // **und ein Name, den niemand gesetzt hat, ist nil**,
                // **und die Meldung lautete *„nil has no method 'sprintf'
                // on this host"***,
                // **also ueber einen Empfänger, den der Leser selbst
                // erfunden hatte.**
                //
                // **Deshalb `SelfCall`, und die Kinder sind nur die
                // Argumente.** Der Interpreter weiss dann, dass `self`
                // gemeint war,
                // **und `draw(x)` in einer Klasse ist derselbe Satz** --
                // **was sich aendert, ist der Empfaenger, den die
                // Skriptmethode sucht: `self` und nicht der Name.**
                if (Is("("))
                {
                    var mit_klammern = ReadArguments();
                    return new RubyNode
                    {
                        Kind = RubyNodeKind.SelfCall,
                        Name = token.Text,
                        Line = token.Line,
                        Children = mit_klammern,
                        // **Und die Argumente tragen ihre Rolle**, weil
                        // `EvaluateChildren` die Rollen liest und sonst
                        // **gar nichts findet**,
                        // **und `f(a ? b : c)` waere dann eine leere
                        // Argumentliste.**
                        Role_Children = [.. mit_klammern.Select(pArgument =>
                            new RubyNodePart
                            {
                                Role = RubyNodeRole.Argument,
                                Node = pArgument,
                            })],
                    };
                }

                // **Ein Bezeichner mit Argumenten ohne Klammern ist ein
                // Aufruf, und es ist der haeufigste, den ein Spiel
                // schreibt.** `attr_accessor :hp, :mp`, `attr_reader :name`,
                // `include Beweglich`, `include A, B` -- **keines davon hat
                // Klammern, und ohne diesen Zweig wuerde der Parser eine
                // Variable mit dem Namen des Aufrufs machen und die Argumente
                // als weitere Anweisungen lesen.** Ein Spiel, das
                // `attr_accessor :hp` schreibt, haette damit eine Variable
                // `attr_accessor` und eine verlassene Konstante `hp`.
                // **Ein Argument, und kein Operator.** `StartsAValue()`
                // allein genuegt nicht: es sagt bei `*` und `+` ja, **und
                // `a * b` ist eine Malrechnung und kein Aufruf mit einem
                // Argument.** Ein Argument ohne Klammern steht immer an
                // erster Stelle nach dem Namen -- **sobald ein Operator
                // kommt, ist es keine Argumentliste mehr**, und genau
                // darum wird hier nicht der Wert, sondern der ganze erste
                // Ausdruck gelesen.
                //
                // **Hier wird kein Zeilenumbruch uebersprungen, und das ist
                // der ganze Unterschied.** `a` und `b = 1` in zwei Zeilen
                // sind zwei Anweisungen, **und ein `SkipNewlines` vor der
                // Pruefung liest sie als einen Aufruf `a(b)`** -- die erste
                // Fassage hatte genau das und verlor beide Anweisungen.
                // **Die klammerlose Form laeuft in Ruby nie ueber eine
                // Zeile**, und `include` und `attr_accessor` schreibt
                // niemand so.
                // **Beide Bedingungen muessen wahr sein, und `StartsAValue`
                // allein genuegt nicht.** `*`, `+`, `-` und `::` koennen
                // einen Wert eroeffnen, **und `a * b` ist eine Rechnung und
                // kein Aufruf.** `StartsAnArgument` sagt genau das, was an
                // erster Stelle nach dem Namen stehen darf.
                // **Und `attr_accessor` allein ist auch ein Aufruf, mit null
                // Argumenten.** Es gibt diese Form, und Ruby akzeptiert sie --
                // **ohne die vier Namen hier waere es eine Variable, und die
                // Zeile waere eine Zuweisung, die nichts zuweist.** Die vier
                // stehen im Interpreter, und **sie stehen dort wieder**, weil
                // der Parser nicht wissen kann, was ein Host als eingebaut
                // fuehrt.
                // **Die Klammern um `is` sind nicht Kosmetik.** Ohne sie
                // bindet `&&` staerker als das `or` in `is`, und der ganze
                // Ausdruck laesst den `is`-Zweig an allem vorbei, was keine
                // der vier Namen ist -- **das heisst, `a is "attr_reader"
                // or "include"` waere true, sobald irgendetwas `include` war.**
                //
                // **Und der Zeilenumbruch vor dem ersten Argument ist
                // verboten.** `a` und `b = 1` in zwei Zeilen sind zwei
                // Anweisungen, **und die erste Fassage las sie als einen
                // Aufruf `a(b)` mit einer Zuweisung als Argument** -- der
                // `SkipNewlines` oben stand noch da. **Die klammerlose
                // Form laeuft in Ruby nie ueber eine Zeile**, und `include`
                // und `attr_accessor` schreibt niemand so.
                // **Keine eigene Abfrage auf den Zeilenumbruch, und das ist
                // der Punkt.** `StartsAValue` ist bei einem Newline-Token
                // false, **weil ein Newline kein Wert ist** -- die erste
                // Fassage hatte zusaetzlich `Current.Kind != Newline`
                // darueber, **und die Mutation, die sie entfernte, lebte**:
                // `StartsAValue` deckte die Regel schon ab. **Ein Test, der
                // eine Bedingung aufhebt, die eine andere traegt, misst die
                // andere**, und zwei Bedingungen fuer eine Regel sind zwei
                // Orte, an denen sie auseinanderlaufen.
                if (StartsAValue() && StartsAnArgument()
                    || (Current.Kind == RubyTokenKind.Newline
                        && (token.Text is ("attr_accessor" or "attr_reader"
                            or "attr_writer" or "include"))))
                {
                    // **Und nur die Form ohne Argumente ueberspringt hier
                    // den Zeilenumbruch, und das ist gemessen.**
                    //
                    // `attr_accessor` allein steht am Zeilenende, und
                    // `Current` waere dann schon das `end` der naechsten
                    // Zeile.
                    //
                    // **Und eine Form MIT Argumenten tut das nicht, und
                    // das ist auch gemessen:**
                    //
                    // ```c
                    // command_args : { CMDARG_PUSH(1); } open_args
                    // open_args    : call_args | ...
                    // call_args    : command | args opt_block_arg | ...
                    // paren_args   : '(' call_args opt_nl ')' | ...
                    // ```
                    //
                    // **Und ein `opt_nl` steht nur bei `paren_args`, also
                    // bei Klammern, und nirgends sonst.** **Also nach einem
                    // Namen ohne Klammern folgt nie ein Zeilenumbruch, und
                    // `case word` gefolgt von einem `when` in der naechsten
                    // Zeile ist KEIN Aufruf mit einem Argument.** **Und ein
                    // Leser, der hier `SkipNewlines` macht, frisst die
                    // Zeilengrenze und liest das `when` als Argument, und
                    // dann:**
                    //
                    // ```
                    // 'when' at offset 33 does not begin an expression
                    // ```
                    SkipNewlines();
                    var argumente = new List<RubyNode>();
                    while (true)
                    {
                        // **Und ein Argument ist ein ganzer `arg`, und nicht
                        // ein Primarausdruck, und das ist gemessen.**
                        //
                        // Ruby 1.8.1's own `parse.y`:
                        //
                        // ```c
                        // command_args : { CMDARG_PUSH(1); } open_args
                        // open_args    : call_args | ...
                        // call_args     : call_args ',' assoc | assocs | arg
                        // ```
                        //
                        // **Und `arg` ist ein voller Ausdruck**, **also
                        // `install a+b, c+d, :mode => 0755` hat drei
                        // Argumente und nicht fuenf** -- **und ein Leser,
                        // der `ParsePostfix(ParsePrimary())` nimmt, liest
                        // `a` und laesst `+b` liegen**, **und dann kam das
                        // Komma an die Spitze von `ParsePrimary`.**
                        argumente.Add(ParseTernary());
                        SkipNewlines();
                        if (!Is(","))
                        {
                            break;
                        }

                        _index++;
                        SkipNewlines();
                    }

                    return new RubyNode
                    {
                        Kind = RubyNodeKind.SelfCall,
                        Name = token.Text,
                        Line = token.Line,
                        Children = argumente,
                        Role_Children =
                        [
                            .. argumente.Select(a => new RubyNodePart
                            {
                                Role = RubyNodeRole.Argument,
                                Node = a,
                            }),
                        ],
                    };
                }

                return node;
            }
            case RubyTokenKind.Delimiter when token.Text == "(":
            {
                _index++;
                SkipNewlines();
                var inner = ParseExpression();
                SkipNewlines();
                Expect(")");
                return inner;
            }
            case RubyTokenKind.Delimiter when token.Text == "[":
            {
                _index++;
                SkipNewlines();
                var elements = new List<RubyNode>();
                while (!Is("]"))
                {
                    if (AtEnd)
                    {
                        throw new RubyParseException(
                            "An array literal is never closed.", token.Line);
                    }
                    elements.Add(ParseExpression());
                    SkipNewlines();
                    if (Is(","))
                    {
                        _index++;
                        SkipNewlines();
                    }
                }
                Expect("]");
                return new RubyNode
                {
                    Kind = RubyNodeKind.Array,
                    Line = token.Line,
                    Children = elements,
                };
            }
            case RubyTokenKind.Delimiter when token.Text == "{":
            {
                _index++;
                SkipNewlines();
                var pairs = new List<RubyNode>();
                while (!Is("}"))
                {
                    if (AtEnd)
                    {
                        throw new RubyParseException("A hash literal is never closed.", token.Line);
                    }
                    pairs.Add(ParseExpression());
                    SkipNewlines();
                    if (Is(","))
                    {
                        _index++;
                        SkipNewlines();
                    }
                }
                Expect("}");
                return new RubyNode
                {
                    Kind = RubyNodeKind.Hash,
                    Line = token.Line,
                    Children = pairs,
                };
            }
        }

        if (token.Kind == RubyTokenKind.Keyword)
        {
            return ParseKeywordPrimary(token);
        }
        if (token.Kind == RubyTokenKind.Operator)
        {
            return ParseOperatorPrimary(token);
        }

        throw new RubyParseException(
            $"'{token.Text}' at offset {token.Offset} does not begin an expression.",
            token.Line);
    }

    private RubyNode ParseKeywordPrimary(RubyToken pToken)
    {
        switch (pToken.Text)
        {
            case "nil":
                _index++;
                return Literal(RubyNodeKind.Nil, pToken.Line, null, null, null);
            case "true":
                _index++;
                return Literal(RubyNodeKind.True, pToken.Line, null, null, null);
            case "false":
                _index++;
                return Literal(RubyNodeKind.False, pToken.Line, null, null, null);
            case "self":
                _index++;
                return Literal(RubyNodeKind.Self, pToken.Line, null, null, null);
            case "__LINE__":
                _index++;
                return Literal(
                    RubyNodeKind.Integer, pToken.Line, pToken.Line, null, null);
            case "__FILE__":
            case "__ENCODING__":
                _index++;
                return Literal(RubyNodeKind.KeywordLiteral, pToken.Line, null, null, pToken.Text);
            case "if":
            case "unless":
            {
                _index++;
                SkipNewlines();
                var condition = ParseExpression();
                SkipNewlines();
                SkipThen();
                var whenTrue = ReadBodyUntil("else", "elsif", "end");
                // **Der else-Zweig ist optional.** `ReadBody` laesst das
                // Schluesselwort stehen -- und **ohne diesen Zweig wuerde der
                // Parser `else` als den naechsten Ausdruck lesen** und mit
                // "'else' does not begin an expression" abbrechen.
                // **Ohne else und ohne elsif steht hier das `end`** -- und
                // `ReadBodyUntil` hat es stehen gelassen. Ohne diesen Test
                // gaenge der if-Zweig ins Leere und der naechste Ausdruck
                // der Datei wuerde als `end` gelesen.
                if (!IsKeyword("else") && !IsKeyword("elsif") && !IsKeyword("end"))
                {
                    throw new RubyParseException(
                        $"'else' or 'end' was expected at offset "
                            + $"{Current.Offset}, but '{Current.Text}' is there.",
                        Current.Line);
                }

                RubyNode whenFalse = null;
                if (IsKeyword("elsif"))
                {
                    // **elsif ist ein else, dessen Bedingung ein if ist** --
                    // und genau darum ruft es sich hier auf und liegt nicht
                    // in einer Schleife.
                    _index++;
                    SkipNewlines();
                    var elsifCondition = ParseExpression();
                    SkipNewlines();
                    SkipThen();
                    var elsifTrue = ReadBodyUntil("else", "elsif", "end");
                    whenFalse = new RubyNode
                    {
                        Kind = RubyNodeKind.If,
                        Name = "if",
                        Line = elsifCondition.Line,
                        Children = [elsifCondition, elsifTrue],
                        Role_Children =
                        [
                            new() { Role = RubyNodeRole.Condition, Node = elsifCondition },
                            new() { Role = RubyNodeRole.WhenTrue, Node = elsifTrue },
                        ],
                    };
                }
                else if (IsKeyword("else"))
                {
                    _index++;
                    SkipNewlines();
                    whenFalse = ReadBody("end");
                }

                // **Nur der Zweig ohne else schuldet noch ein `end`.** Der
                // `else`-Arm hat es ueber `ReadBody("end")` schon genommen,
                // und ein `elsif`-Zweig ist ein vollstaendiges `if`, das
                // seines selbst genommen hat -- **eine Pruefung, die in
                // beiden Faellen noch einmal nach `end` sieht, wuerde bei
                // jedem vollstaendigen `if ... else ... end` fehlschlagen.**
                var endGenommen = whenFalse != null;
                if (!endGenommen)
                {
                    if (IsKeyword("end"))
                    {
                        _index++;
                    }
                    else
                    {
                        throw new RubyParseException(
                            $"'end' was expected at offset {Current.Offset}, "
                                + $"but '{Current.Text}' is there.",
                            Current.Line);
                    }
                }

                var children = new List<RubyNode> { condition, whenTrue };
                var roles = new List<RubyNodePart>
                {
                    new() { Role = RubyNodeRole.Condition, Node = condition },
                    new() { Role = RubyNodeRole.WhenTrue, Node = whenTrue },
                };
                if (whenFalse != null)
                {
                    children.Add(whenFalse);
                    roles.Add(new RubyNodePart
                        { Role = RubyNodeRole.WhenFalse, Node = whenFalse });
                }

                return new RubyNode
                {
                    Kind = RubyNodeKind.If,
                    Name = pToken.Text,
                    Line = pToken.Line,
                    Children = children,
                    Role_Children = roles,
                };
            }
            case "case":
            {
                _index++;
                SkipNewlines();
                // **Der Wert, gegen den die when-Aeste geprueft werden.**
                // `case` kann einen Ausdruck haben und auch keinen -- ein
                // nacktes `case` vergleicht nichts, **und das ist eine
                // Form, die ein Spiel schreibt**, kein Tippfehler.
                var wert = StartsAValue() ? ParseExpression() : null;
                SkipNewlines();
                // **Und die Kinder sind zuerst der Wert, und danach einer
                // je Zweig, und das ist die Reihenfolge, die `parse.y`
                // mit `NEW_CASE($2, $4)` auch hat.**
                var children = new List<RubyNode>();
                if (wert != null)
                {
                    children.Add(wert);
                }

                var whenRuest = ReadBodyUntil("when", "else", "end");
                children.Add(whenRuest);
                RubyNode elseBlock = null;
                if (IsKeyword("else"))
                {
                    _index++;
                    SkipNewlines();
                    // **Und ein `else` eines `case` endet am `end` des
                    // `case`, und nicht an einem eigenen, und das ist
                    // gemessen.**
                    //
                    // Ruby 1.8.1's own `parse.y`:
                    //
                    // ```c
                    // opt_else : none | kELSE compstmt
                    // compstmt : stmts opt_terms
                    // stmts   : none | stmt | stmts terms stmt
                    // primary : kCASE expr_value opt_terms case_body kEND
                    //          | kCASE opt_terms case_body kEND
                    // ```
                    //
                    // **Und ein `kEND` steht nur EINE MAL in diesen
                    // Zeilen, und das ist das des `case`.** **Und ein
                    // `stmt` ist nach der Grammatik nie ein `end`, und
                    // `opt_else` hat auch kein `kEND`.**
                    //
                    // **Und `ReadBody` frisst seinen Closer, und
                    // `ReadBodyUntil` laesst ihn stehen** -- **und ein
                    // Leser, der `ReadBody("end")` fuer das `else`
                    // nimmt, frisst damit das `end` des `case`, und dann
                    // steht der Index auf dem `when` oder `else` hinter
                    // dem `case`, und das kommt als
                    // `'end' does not begin an expression'.**
                    elseBlock = ReadBodyUntil("end");
                }
                else if (IsKeyword("when"))
                {
                    // **Ein `when` nach einem `when` gehoert zum selben
                    // case** -- und genau hier ist es, denn die Liste der
                    // `when`-Aeste ist die Liste der Kinder des case-Knotens.
                    while (IsKeyword("when"))
                    {
                        children.Add(ParseWhen());
                        SkipNewlines();
                    }
                    if (IsKeyword("else"))
                    {
                        _index++;
                        SkipNewlines();
                        elseBlock = ReadBodyUntil("end");
                    }
                }

                // **Der Schlusser ist in beiden Wegen schon verbraucht.**
                // `ReadBody("end")` nimmt ihn, und ein `case` ohne else
                // wurde ueber `ReadBodyUntil` gelesen -- **das laesst ihn
                // stehen, und genau diesen einen Fall nimmt der Pfad hier
                // noch selbst.** Eine Pruefung, die in beiden Faellen noch
                // einmal nach `end` sieht, wuerde bei jedem
                // `case ... else ... end` fehlschlagen.
                // **Und das `end` des `case` gehoert IMMER diesem `case`,
                // und es wird genau einmal gefressen, und das ist
                // gemessen.**
                //
                // Ruby 1.8.1's own `parse.y`:
                //
                // ```c
                // primary  : kCASE expr_value opt_terms case_body kEND
                //          | kCASE opt_terms case_body kEND
                // case_body: kWHEN when_args then compstmt cases
                // cases    : opt_else | case_body
                // opt_else : none | kELSE compstmt
                // ```
                //
                // **Und ein `kEND` steht in `primary` und nirgends sonst**,
                // **und `opt_else` hat kein `kEND`.** **Also liegt das `end`
                // hinter dem `else`, und es wird hinter jedem `else`
                // gelesen, und auch hinter keinem.**
                //
                // **Und ein Waechter, der das `end` nur nimmt, wenn kein
                // `else` da war, laesst es bei jedem `case ... else ...`
                // stehen** -- **und dann sieht der umgebende Block ein
                // `end`, das ihm nicht gehoert**, **und das kommt als**
                //
                // ```
                // 'end' at offset 60 does not begin an expression
                // ```
                if (IsKeyword("end"))
                {
                    _index++;
                }
                else
                {
                    throw new RubyParseException(
                        $"'end' was expected at offset {Current.Offset}"
                            + $"but '{Current.Text}' is there.",
                        Current.Line);
                }

                if (elseBlock != null)
                {
                    children.Add(elseBlock);
                }

                return new RubyNode
                {
                    Kind = RubyNodeKind.Case,
                    Line = pToken.Line,
                    Children = children,
                };
            }
            case "for":
            {
                _index++;
                SkipNewlines();
                // **`for x in liste` schreibt die Variable und die Liste.**
                var ziel = ParsePrimary();
                if (!IsKeyword("in"))
                {
                    throw new RubyParseException(
                        $"'in' was expected at offset {Current.Offset}, but "
                            + $"'{Current.Text}' is there.",
                        Current.Line);
                }

                _index++;
                SkipNewlines();
                var liste = ParseExpression();
                SkipNewlines();
                SkipThen();
                if (IsKeyword("do"))
                {
                    _index++;
                    SkipNewlines();
                }

                var rumpf = ReadBody("end");
                return new RubyNode
                {
                    Kind = RubyNodeKind.For,
                    Name = ziel.Name,
                    Line = pToken.Line,
                    Children = [ziel, liste, rumpf],
                    Role_Children =
                    [
                        new() { Role = RubyNodeRole.Target, Node = ziel },
                        new() { Role = RubyNodeRole.When, Node = liste },
                        new() { Role = RubyNodeRole.WhenTrue, Node = rumpf },
                    ],
                };
            }
            case "while":
            case "until":
            {
                _index++;
                SkipNewlines();
                AfterACondition = true;
                var condition = ParseExpression();
                AfterACondition = true;
                SkipNewlines();
                SkipThen();
                if (IsKeyword("do"))
                {
                    _index++;
                    SkipNewlines();
                }
                AfterACondition = false;
                var body = ReadBody("end");
                return new RubyNode
                {
                    Kind = pToken.Text == "while" ? RubyNodeKind.While : RubyNodeKind.Until,
                    Name = pToken.Text,
                    Line = pToken.Line,
                    Children = [condition, body],
                };
            }
            case "return":
            {
                _index++;
                if (StartsAValue())
                {
                    var value = ParseExpression();
                    return new RubyNode
                    {
                        Kind = RubyNodeKind.Return,
                        Line = pToken.Line,
                        Children = [value],
                    };
                }
                return new RubyNode { Kind = RubyNodeKind.Return, Line = pToken.Line };
            }
            case "break":
            case "next":
            case "redo":
            case "retry":
            {
                _index++;
                // **Und ein Wert darf folgen, und der gehoert an denselben
                // Knoten.** `break 7` und `next 0` sind die Saetze, mit
                // denen ein Spiel aus einer Schleife einen Wert holt,
                // **und der Parser nahm nur das Schluesselwort und liess die
                // Zahl als naechsten Ausdruck stehen** -- **und dann war
                // `break 7` gleich `break`, und der Wert der Schleife war
                // nil.** Gemessen: `while true; break 7; end` war nil.
                //
                // **Und ein Wert wird nur genommen, wenn wirklich einer
                // folgt.** `break` am Zeilenende und dann `end` ist ein
                // Schluesselwort und kein Wert,
                // **und ein Leser, der unbedingt einen Ausdruck laesst,
                // wuerde dort `end` als den Wert nehmen.**
                var wert = new List<RubyNode>();
                if (StartetAbbruchWert())
                {
                    wert.Add(ParseExpression());
                }

                return new RubyNode
                {
                    Kind = pToken.Text switch
                    {
                        "break" => RubyNodeKind.Break,
                        "next" => RubyNodeKind.Next,
                        "redo" => RubyNodeKind.Redo,
                        _ => RubyNodeKind.Retry,
                    },
                    Name = pToken.Text,
                    Line = pToken.Line,
                    Children = wert,
                };
            }
            case "super":
            {
                _index++;
                // **`super` und `super(...)` sind zweierlei.** Ohne Klammern
                // gibt Ruby die Argumente weiter, mit Klammern die
                // geschriebenen -- **und ein Leser, der beides gleich
                // behandelte, wuerde bei `super` ohne Klammern die Argumente
                // des Aufrufs nehmen und damit die Basis mit anderen Werten
                // aufrufen als die, die der Erbe selbst bekommen hat.**
                var args = new List<RubyNode>();
                if (Is("("))
                {
                    args = ReadArguments();
                }

                return new RubyNode
                {
                    Kind = RubyNodeKind.SuperCall,
                    // **Das Leerzeichen ist die Form, in der Ruby
                    // unterscheidet** -- und der Name traegt es hier, weil
                    // der Knoten nur einen Namen hat.
                    Name = args.Count > 0 ? "mit" : "ohne",
                    Line = pToken.Line,
                    Children = args,
                    Role_Children =
                    [.. args.Select(a => new RubyNodePart
                        { Role = RubyNodeRole.Argument, Node = a })],
                };
            }
            case "yield":
            {
                _index++;
                var arguments = Is("(") ? ReadArguments() : [];
                return new RubyNode
                {
                    Kind = RubyNodeKind.Yield,
                    Line = pToken.Line,
                    Children = arguments,
                };
            }
            case "defined?":
            {
                _index++;
                SkipNewlines();

                // **Ein Ausdruck, und er wird NICHT ausgewertet.** Ruby
                // beantwortet die Frage "was ist das" und fuehrt den
                // Ausdruck nicht aus -- **und ein Leser, der ihn
                // auswertete, wuerde `defined? a.b` auch dann `method`
                // sagen, wenn `b` fehlt**, und wuerde nebenbei den Aufruf
                // machen, den die Frage nur stellte. Klammern sind
                // erlaubt und gehoeren zum Ausdruck.
                // **Der ganze Ausdruck und nicht nur sein Anfang.**
                // `defined? 1 + 1` ist eine Frage nach einer Rechnung,
                // **und `ParsePrimary` allein haette die `1` gelesen und den
                // Rest als zweite Anweisung**, was `undefined operator '+'
                // for a Symbol and a Integer` ergab. **`defined? a.b` ist
                // eine Frage nach einem Aufruf**, und genau so weit muss
                // der Leser gehen.
                // **`ParseStatement` und nicht `ParseUnary`.** Die
                // Unary-Ebene ist die oberste: sie sieht `not` und ein
                // fuehrendes `+` oder `-` und gibt sonst
                // `ParsePostfix(ParsePrimary())` zurueck -- **also nur die
                // `1` von `1 + 1`**, und der Rest wurde zur zweiten
                // Anweisung, die `undefined operator '+' for a Symbol and a
                // Integer` ergab. **Die Frage gilt dem ganzen
                // Ausdruck**, und die Ebene, die einen ganzen Ausdruck
                // liest, ist `ParseStatement`.
                var ausdruck = Is("(") ? ReadParenthesised() : ParseStatement();
                return new RubyNode
                {
                    Kind = RubyNodeKind.Defined,
                    Line = pToken.Line,
                    Children = [ausdruck],
                    Role_Children =
                    [
                        new RubyNodePart { Role = RubyNodeRole.Condition, Node = ausdruck },
                    ],
                };
            }

            case "undef":
            {
                _index++;
                SkipNewlines();

                // **Ein Name, und er darf ein Symbol sein.** `undef m` nimmt
                // einen Bezeichner und `undef :m` ein Symbol, **und beide
                // sind dieselbe Anweisung** -- ein Leser, der nur eines
                // davon las, wuerde die andere Form als Ausdruck lesen und
                // die Zeile als Zuweisung verschlucken.
                // **Und `undef` ist ein Schluesselwort, kein Methodenaufruf**:
                // `Module#undef_method` ist die Methode, und die stand als
                // `undef_method` in keiner Grammatikliste. Die erste
                // Fassage des Kommentars hier behauptete das Gegenteil und
                // war falsch.
                // **Eine Liste, und nicht ein Name.** `undef a, b` ist in
                // Rubys Grammatik `undef_list ',' fitem` -- **und die erste
                // Fassage las genau einen**, sodass `undef a, b` die erste
                // Methode wegnahm und die zweite als eigene Anweisung
                // las. **Das ist der Fall, in dem die Mutation "liest nur den
                // Anfang" durchkam**: bei einem einzigen Namen liefern beide
                // Wege denselben Knoten, und erst die Liste trennt sie.
                var namen = new List<RubyNode>();
                namen.Add(Is("(") ? ReadParenthesised() : ParseStatement());
                while (Is(","))
                {
                    _index++;
                    SkipNewlines();
                    namen.Add(ParseStatement());
                }

                return new RubyNode
                {
                    Kind = RubyNodeKind.Undef,
                    Line = pToken.Line,
                    Children = namen,
                    Role_Children =
                    [
                        .. namen.Select(pName => new RubyNodePart
                        {
                            Role = RubyNodeRole.Condition,
                            Node = pName,
                        }),
                    ],
                };
            }

            case "alias":
            {
                _index++;
                SkipNewlines();

                // **Zwei Namen, und der neue kann ein Symbol sein.** `alias
                // alt neu` und `alias :alt :neu` sind dieselbe Anweisung --
                // **und der Leser, der nur den Bezeichner nahme, wuerde
                // `alias :alt :neu` als zwei Symbole lesen und den neuen
                // Namen mit einem Doppelpunkt speichern**, und `alter.neu`
                // waere dann ein Aufruf einer Methode, die es nicht gibt.
                var neuer = ReadAliasName();
                SkipNewlines();
                var alter = ReadAliasName();
                return new RubyNode
                {
                    Kind = RubyNodeKind.Alias,
                    Name = neuer,
                    // **Der alte Name steht im Operator und nicht in einem
                    // Kind**, weil er kein Knoten ist: er ist ein Name, und
                    // ein Knoten daraus wuerde ihn auswerten statt ihn
                    // benennen.
                    Operator = alter,
                    Line = pToken.Line,
                };
            }
            case "def":
            {
                _index++;
                // **Und der Empfenger wird VOR dem Namen gelesen.**
                //
                // **Und das ist gemessen an `parse.y`:**
                //
                // ```c
                // | kDEF singleton dot_or_colon {lex_state = EXPR_FNAME;} fname
                // ```
                //
                // **and `singleton : var_ref | '(' expr ')'`** -- **so the
                // receiver comes before the name,** **and a reader that
                // read the name first had already spent the `.`**, **so
                // `def $mflags.set?(flag)` was read as a method named
                // `$mflags` and the `.` was then a syntax error.**
                RubyNode? empfaenger = null;
                if (PunktDanach())
                {
                    empfaenger = ParsePrimary();
                    SkipNewlines();
                    Expect(".");
                }

                var name = LeseSchreiberName() ?? ReadMemberName();
                // **`def self.x` ist eine Methode auf der Klasse selbst**,
                // und der Unterschied ist der einzige Punkt an diesem
                // Schluesselwort -- **ein Leser, der ihn uebersieht, wuerde
                // jede Klassenmethode eines Spiels zur Instanzmethode machen**,
                // und ein `self.`-Aufruf darin haette kein Ziel.
                // **Und `def` nimmt jedes Singleton, und nicht nur
                // `self`, und das ist gemessen.**
                //
                // Ruby 1.8.1's own `parse.y`, line 1635 and line 1652:
                //
                // ```c
                // | kDEF fname
                // | kDEF singleton dot_or_colon {lex_state = EXPR_FNAME;} fname
                // ```
                //
                // **and `singleton : var_ref | '(' {lex_state = EXPR_BEG;}
                // expr opt_nl ')'` -- and `var_ref` is a variable.**
                //
                // **So `def $mflags.set?(flag)` is a singleton method on
                // the array in a global variable, and `def obj.name` is one
                // on the value of `obj`, and `def (expr).name` is one on
                // the value of an expression.** **And this reader only
                // knew `self`,** **so every one of those three was
                // refused** -- **and `instruby.rb` line 33 is the first of
                // them.**
                var aufSelbst = empfaenger != null;
                var arguments = ReadParameterList();
                SkipNewlines();

                // **Und ein Rumpf darf `rescue` und `ensure` tragen, ohne
                // ein `begin` zu schreiben.** Das ist die Form, mit der
                // `Kernel#load` eine Datei laedt,
                // **und `def m; a; rescue; b; end` steht in jedem
                // RPG-Maker-Skript, das eine Datei laedt** --
                // **ein Leser, der hier immer `end` verlangte, wuerde ein
                // Skript ablehnen, das die Sprache selbst liest.**
                var koerper = ParseStatements("rescue", "else", "ensure", "end");
                var body = IsKeyword("rescue") || IsKeyword("ensure")
                    || IsKeyword("else")
                    ? ArmeSammeln(koerper, pToken.Line)
                    : RumpfAus(koerper, pToken.Line);
                return new RubyNode
                {
                    Kind = aufSelbst ? RubyNodeKind.DefS : RubyNodeKind.Def,
                    Name = name,
                    Line = pToken.Line,
                    // **Und der Empfangner kommt als `Name` mit, und
                    // nicht als Kind.**
                    //
                    // **Und das ist die Entscheidung, die 242 Tests
                    // gerettet hat.** **Ein Kind davor verschiebt
                    // `Children[0]` und `Children[1]`, und
                    // **und `DefineMethod` liest genau die beiden**, **und
                    // jeder `def` ohne Empfaenger haette dann Parameter
                    // und Rumpf um eine Stelle verschoben.**
                    //
                    // **Und die Singleton-Semantik, also die Methode am
                    // Objekt statt an der Klasse, ist damit offen** --
                    // **und sie ist eine eigene Luecke und nicht Teil
                    // dieser Zeile.**
                    Children = [arguments, body],
                    Role_Children = empfaenger == null
                        ? null
                        : new List<RubyNodePart>
                        {
                            new()
                            {
                                Role = RubyNodeRole.Receiver,
                                Node = empfaenger,
                            },
                        },
                };
            }
            case "class":
            case "module":
            {
                _index++;
                var name = ReadConstantPath();
                var body = new List<RubyNode>();
                string? superclass = null;
                if (Is("<"))
                {
                    _index++;
                    // **Die Superklasse wird gelesen und nicht weggeworfen.**
                    // Die erste Fassage rief `ReadConstantPath()` auf und
                    // benutzte das Ergebnis nicht -- **und ein Leser, der die
                    // Kette nicht fuettert, kann eine geerbte Methode nicht
                    // finden**, auch wenn der Aufruf sie sucht.
                    superclass = ReadConstantPath();
                }
                SkipNewlines();
                if (Is("end"))
                {
                    _index++;
                }
                else
                {
                    body = [.. ReadBody("end").Children];
                }
                return new RubyNode
                {
                    Kind = pToken.Text == "class" ? RubyNodeKind.Class : RubyNodeKind.Module,
                    Name = name,
                    // **Nur eine Klasse kann eine Superklasse haben.** Ein
                    // Modul mit einem `<` ist ein Syntaxfehler in Ruby, und
                    // **ein Leser, der es zulieesse, wuerde einem Modul etwas
                    // erben lassen, was das Skript nie gefragt hat.**
                    Superclass = pToken.Text == "class" ? superclass : null,
                    Line = pToken.Line,
                    Children = body,
                };
            }
            case "begin":
            {
                _index++;
                return ReadBegin(pToken.Line);
            }
        }

        throw new RubyParseException(
            $"'{pToken.Text}' at offset {pToken.Offset} does not begin an expression.",
            pToken.Line);
    }

    /// <summary>
    /// Reads the parameters a block takes between two bars, or none.
    /// </summary>
    /// <remarks>
    /// The bars look exactly like a bitwise or, and the only thing that tells
    /// them apart is that a block's bars come right after a do or a brace. A
    /// block with no parameters has no bars at all, so an empty list is the
    /// common case and is not a failure.
    /// </remarks>
    private RubyNode ReadBlockParameters()
    {
        // **`||` ist hier immer eine leere Parameterliste, und der Kontext
        // entscheidet das allein.** Diese Methode wird **nur** unmittelbar
        // nach `{` oder nach `do` gerufen -- **und ein logisches ODER kann
        // dort nicht stehen**, weil direkt nach einer offenen Klammer kein
        // linker Operand existiert. `lambda { || 3 }` heisst deshalb: leere
        // Liste, Rumpf `3`.
        //
        // **Die erste Fassung entschied ueber das Token danach, und das war
        // geraten**: `3` ist ein Wert, also las sie ODER undwarf die Zeile
        // weg. **Ein Nachbar ist kein Kontext** -- die Frage ist nicht, was
        // als naechstes kommt, sondern wo ueberhaupt gelesen wird.
        if (Is("||"))
        {
            _index++;
            return new RubyNode
            {
                Kind = RubyNodeKind.Array,
                Line = Current.Line,
                Children = [],
            };
        }

        if (!Is("|"))
        {
            return new RubyNode { Kind = RubyNodeKind.Array, Line = Current.Line };
        }
        _index++;
        var parameters = new List<RubyNode>();
        while (!Is("|"))
        {
            if (AtEnd)
            {
                throw new RubyParseException(
                    "A block's parameter list is never closed.", Current.Line);
            }
            SkipNewlines();
            if (Is("|"))
            {
                break;
            }
            var token = Take();
            parameters.Add(Literal(
                RubyNodeKind.Identifier, token.Line, null, null, null, token.Text));
            SkipNewlines();
            if (Is(","))
            {
                _index++;
                SkipNewlines();
            }
        }
        Expect("|");
        return new RubyNode
        {
            Kind = RubyNodeKind.Array,
            Line = parameters.Count > 0 ? parameters[0].Line : Current.Line,
            Children = parameters,
        };
    }

    /// <summary>
    /// Steps over the keyword that separates a condition from its body.
    /// </summary>
    /// <remarks>
    /// Both 'then' and 'do' are legal there and a game uses both, so either is
    /// taken and neither is demanded. Requiring one would refuse a file Ruby
    /// reads without complaint.
    /// </remarks>
    private void SkipThen()
    {
        if (IsKeyword("then"))
        {
            _index++;
            SkipNewlines();
        }
    }

    private string ReadConstantPath()
    {
        var name = ReadMemberName();
        while (Is("::"))
        {
            _index++;
            name += "::" + ReadMemberName();
        }
        return name;
    }

    private RubyNode ReadParameterList()
    {
        if (!Is("("))
        {
            return new RubyNode { Kind = RubyNodeKind.Array, Line = Current.Line };
        }
        _index++;
        var parameters = new List<RubyNode>();
        while (!Is(")"))
        {
            if (AtEnd)
            {
                throw new RubyParseException(
                    "A parameter list is never closed.", Current.Line);
            }

            SkipNewlines();

            // **Drei Formen, und die erste Fassage kannte nur die erste.**
            // Sie nahm Token fuer Token und nannte alles Identifier,
            // **und `def m(a, b = 2)` wurde damit zu vier Parametern: `a`,
            // `b`, `=` und `2`.** Ein Aufruf mit drei Werten haette
            // gebunden, **und einer mit zwei haette `=` und `2` als Namen
            // bekommen** -- ein Spiel, das einen Vorgabewert schreibt, haette
            // eine Methode bekommen, die ihn nie benutzt.
            //
            // **Ein Vorgabewert ist ein Ausdruck und wird auch so
            // gespeichert**, weil er zur Aufrufzeit ausgewertet wird
            // und nicht zur Definitionszeit -- **und der Ausdruck kann
            // einen Aufruf enthalten**, was ein Name nicht kann.
            if (Is("*"))
            {
                _index++;
                var stern = Take();
                parameters.Add(new RubyNode
                {
                    Kind = RubyNodeKind.BlockPass,
                    Name = stern.Text,
                    Line = stern.Line,
                });
            }
            else if (Is("**"))
            {
                _index++;
                var doppelt = Take();
                parameters.Add(new RubyNode
                {
                    Kind = RubyNodeKind.Hash,
                    Name = doppelt.Text,
                    Line = doppelt.Line,
                });
            }
            else if (Current.Kind is RubyTokenKind.Identifier
                or RubyTokenKind.InstanceVariable
                or RubyTokenKind.GlobalVariable)
            {
                var token = Take();
                var name = Literal(
                    RubyNodeKind.Identifier, token.Line, null, null, null,
                    token.Text);
                SkipNewlines();
                if (Is("=") && !Is("=="))
                {
                    _index++;
                    SkipNewlines();
                    parameters.Add(new RubyNode
                    {
                        Kind = RubyNodeKind.Assignment,
                        Name = token.Text,
                        Line = token.Line,
                        Children =
                        [
                            name,
                            // **Und hier kein Wertleser**, **denn ein
                            // Vorgabewert ist ein `arg_value` und kein
                            // Wert einer Mehrfachzuweisung.**
                            //
                            // **Gemessen an `parse.y`: `f_opt :
                            // tIDENTIFIER '=' arg_value`** -- **und die
                            // Liste danach gehoert zu `f_arg ',' f_norm_arg`
                            // und nicht zum Vorgabewert.**
                            //
                            // **Und `ParseExpression` las hier ueber den
                            // Wertleser das Komma der Parameterliste mit**,
                            // **und `def m(a = 10, b = 20)` band die 10 an
                            // `b` und die 20 an nichts.**
                            ParseTernary(),
                        ],
                    });
                }
                else
                {
                    parameters.Add(name);
                }
            }
            else
            {
                // **Und was kein Parameter ist, ist ein Fehler mit Ort.**
                // Ein Spiel schreibt `def m(a, b)`,
                // **und ein Leser, der jedes Token annimmt, haette einen
                // Tippfehler zu einem Parameter mit einem Namen gemacht,
                // den niemand aufruft.**
                parameters.Add(ParseExpression());
            }

            SkipNewlines();
            if (Is(","))
            {
                _index++;
            }
        }
        Expect(")");
        return new RubyNode
        {
            Kind = RubyNodeKind.Array,
            Line = parameters.Count > 0 ? parameters[0].Line : Current.Line,
            Children = parameters,
        };
    }

    private RubyNode ParseOperatorPrimary(RubyToken pToken)
    {
        switch (pToken.Text)
        {
            case "..":
            case "...":
            {
                // A range that starts an expression, as in `..limit`. A range
                // after a value is read as an operator, so this is the only way
                // the dots can begin one.
                _index++;
                var end = StartsAValue() ? ParseBinary(Precedence.Length) : null;
                return new RubyNode
                {
                    Kind = RubyNodeKind.Range,
                    Operator = pToken.Text,
                    Line = pToken.Line,
                    Children = end == null ? [] : [end],
                };
            }
            case "::":
            {
                _index++;
                return new RubyNode
                {
                    Kind = RubyNodeKind.Constant,
                    Name = ReadConstantPath(),
                    Line = pToken.Line,
                };
            }
            case "&":
            {
                // A block pass turns a block into a value, so it is an
                // expression wherever a value may appear.
                _index++;
                var block = ReadBlockArgument();
                return new RubyNode
                {
                    Kind = RubyNodeKind.BlockPass,
                    Line = pToken.Line,
                    Children = [block],
                };
            }
        }
        throw new RubyParseException(
            $"'{pToken.Text}' at offset {pToken.Offset} does not begin an expression.",
            pToken.Line);
    }

    /// <summary>
    /// Reads what a range ends at, or a nil node when nothing follows.
    /// </summary>
    /// <remarks>
    /// An endless range is legal, so nothing after the dots is not a failure.
    /// The end is read at the arithmetic level and then any call on it is
    /// applied, which is what keeps `1..foo.bar` from swallowing the bar.
    /// </remarks>
    private RubyNode ReadRangeEnd()
    {
        if (!StartsAValue())
        {
            return new RubyNode { Kind = RubyNodeKind.Nil, Line = Current.Line };
        }
        return ParsePostfix(ParseBinary(Precedence.Length));
    }

    private RubyNode ReadBlockArgument()
    {
        if (Is("{"))
        {
            _index++;
            return ReadBody("}");
        }
        var token = Take();
        return Literal(RubyNodeKind.Symbol, token.Line, null, null, token.Text);
    }

    /// <summary>
    /// True where a bracketless argument may begin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>An operator is never the start of an argument.</strong>
    /// `attr_accessor :hp` writes a symbol and `include Beweglich` a name,
    /// and both begin where a value begins — <strong>and `a * b` also
    /// begins where a value begins, because <c>*</c> can start one.</strong>
    /// The two are told apart by what follows the name and not by the name:
    /// an operator there means the name was a value in a calculation.
    /// </para>
    /// <para>
    /// <strong>This is a shorter list than <c>StartsAValue</c> on
    /// purpose.</strong> A splat, an address-of and a range are legal
    /// arguments in Ruby, and a game's <c>include</c> does not use them —
    /// <strong>and a list that guessed would have made <c>a * b</c> a call
    /// and then read the rest of the file as its arguments.</strong>
    /// </para>
    /// </remarks>
    private bool StartsAnArgument()
    {
        return Current.Kind switch
        {
            RubyTokenKind.Integer or RubyTokenKind.Float
                or RubyTokenKind.String or RubyTokenKind.Symbol
                or RubyTokenKind.Regexp or RubyTokenKind.Identifier
                or RubyTokenKind.Constant or RubyTokenKind.InstanceVariable
                or RubyTokenKind.GlobalVariable
                => true,
            // **Nur "(" und nicht "[" oder "{"** -- **und das ist der
            // Unterschied zu `StartsAValue`.** `items[0]` ist ein Index
            // und kein Aufruf mit einer Liste als Argument, **und der
            // Postfix-Parser liest die Klammern, sobald er an ihnen
            // vorbeikommt.** Ein Leser, der "[" hier zugelassen haette,
            // haette aus `items[0]` den Aufruf `items([0])` gemacht,
            // **und `items` mit einem Argument aufgerufen, das Ruby ihm
            // nie gibt.**
            RubyTokenKind.Delimiter => Current.Text is "(",
            RubyTokenKind.Keyword => Current.Text is "nil" or "true" or "false",
            _ => false,
        };
    }

    /// <summary>
    /// A parenthesised expression, without the brackets.
    /// </summary>
    /// <remarks>
    /// <strong>The brackets are gone and the expression is not.</strong> A
    /// reader that kept them would have made the node's text start with
    /// `(`, and `defined? (a)` and `defined? a` would have been two
    /// different things — <strong>and Ruby does not draw that
    /// distinction.</strong>
    /// </remarks>
    /// <summary>
    /// Whether the token after the current one is a `.`.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this asks about the token <em>after</em> the current
    /// one, and not about the current one</strong> -- <strong>and that is
    /// the whole difference</strong>, <strong>and writing it the other way
    /// round is what made <c>def $mflags.set?(flag)</c> fail three times
    /// in a row.</strong>
    /// </para>
    /// <para>
    /// Measured at <c>parse.y</c>: <c>kDEF singleton dot_or_colon fname</c>,
    /// and <c>singleton : var_ref</c>. <strong>So <c>def $a.b</c> is three
    /// tokens -- the variable, the dot and the name -- and a reader that
    /// asks "is the current token a dot" is asking about the variable, and
    /// the answer is always no.</strong>
    /// </para>
    /// </remarks>
    private bool PunktDanach()
    {
        if (_index + 1 >= _tokens.Count)
        {
            return false;
        }

        // **Und diese Methode aendert den Index nicht.**
        //
        // **Und sie tat es, und 244 Tests brachen** -- **und eine Methode,
        // deren Name eine Frage stellt, darf den Zustand nicht
        // veraendern**, **denn jeder Aufrufer, der die Frage stellt und
        // dann weiterliest, laeuft jetzt einen Token zu weit.**
        //
        // **Und der Vorrueckritt bleibt beim Aufrufer**, **wo er hingehort**:
        // **das `SkipNewlines()` vor `Expect(".")` ueberspringt sie
        // ohnehin.**
        var k = _index + 1;
        while (k < _tokens.Count
            && (_tokens[k].Kind == RubyTokenKind.Newline
                || _tokens[k].Kind == RubyTokenKind.Semicolon))
        {
            k++;
        }

        return k < _tokens.Count
            && _tokens[k].Kind == RubyTokenKind.Operator
            && _tokens[k].Text == ".";
    }

    private RubyNode ReadParenthesised()
    {
        _index++;
        SkipNewlines();
        var innen = ParseStatement();
        SkipNewlines();
        if (!Is(")"))
        {
            throw new RubyParseException(
                $"An open bracket is never closed; at offset {Current.Offset} "
                + $"there is '{Current.Text}'.",
                Current.Line);
        }

        _index++;
        return innen;
    }

    /// <summary>
    /// One name in an `alias`, with or without a colon in front of it.
    /// </summary>
    /// <remarks>
    /// <strong>Both spellings and no more.</strong> `alias alt neu` writes
    /// two bare words and `alias :alt :neu` two symbols — <strong>and a
    /// reader that only took the word would have kept the colon as part of
    /// the new name</strong>, so `obj.neu` would have looked for a method
    /// whose name begins with a colon and found nothing.
    /// </remarks>
    private string ReadAliasName()
    {
        var token = Current;
        if (token.Kind == RubyTokenKind.Symbol)
        {
            _index++;

            // **`Value` und nicht `Text`.** Ein Symbol-Token traegt den
            // Doppelpunkt in `Text` und den Namen ohne in `Value` -- **und
            // ein Leser, der `Text` nahm, haette den Doppelpunkt als Teil
            // des Methodennamens gespeichert**: `alias :neu :alt` waere dann
            // ein Alias auf `:neu`, **und `obj.neu` wuerde eine Methode
            // suchen, die mit einem Doppelpunkt beginnt**, und nichts
            // finden. Die andere Form, `alias neu alt`, schreibt zwei
            // Bezeichner ohne Doppelpunkt -- **und beide muessen denselben
            // Namen ergeben.**
            return token.Value ?? token.Text.TrimStart(':');
        }

        if (token.Kind is RubyTokenKind.Identifier or RubyTokenKind.Constant
            or RubyTokenKind.Keyword)
        {
            _index++;
            return token.Text;
        }

        throw new RubyParseException(
            $"an alias names two things, and '{token.Text}' is at offset "
                + $"{token.Offset} where a name belongs.",
            token.Line);
    }

    /// <summary>
    /// Whether a value follows a `break` or a `next`.
    /// </summary>
    /// <returns>true when one does.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And not <c>StartsAValue</c>, because that one answers
    /// <c>true</c> for <c>if</c>.</strong> `StartsAValue` is for a
    /// statement's first token, where `next if x == 2` **is** an
    /// expression,
    /// **and after `next` is a condition and not a value** —
    /// <strong>and taking it as one gave *'end' was expected, but the script
    /// ends first***, **because the `if` took the rest of the block with
    /// it.**
    /// </para>
    /// <para>
    /// <strong>And a modifier at the end of a line is not a value.</strong>
    /// `next` and then `end` is a keyword and a keyword,
    /// **and a reader that insisted on a value would take the `end` with
    /// it.**
    /// </para>
    /// </remarks>
    private bool StartetAbbruchWert()
    {
        return Current.Kind switch
        {
            RubyTokenKind.Integer => true,
            RubyTokenKind.Float => true,
            RubyTokenKind.String => true,
            RubyTokenKind.Symbol => true,
            RubyTokenKind.Regexp => true,
            RubyTokenKind.InstanceVariable => true,
            RubyTokenKind.GlobalVariable => true,
            RubyTokenKind.Identifier => true,
            RubyTokenKind.Constant => true,
            RubyTokenKind.Delimiter => Current.Text is "(" or "[",
            RubyTokenKind.Operator => Current.Text is "-" or "!" or "~",
            _ => false,
        };
    }

    /// <summary>True where a value may begin, so an operator after it is binary.</summary>
    private bool StartsAValue() => StartsAValue(Current);

    /// <summary>Whether a given token begins a value.</summary>
    /// <remarks>
    /// **And this is one method and not two, because a lookahead has to
    /// ask the same question about a token it has not consumed yet**
    /// -- **and the comma that separates a call's arguments from the
    /// comma that separates an assignment's names is the same token**,
    /// **so the two readers answered differently and only one of them
    /// could be right.</**>
    /// </remarks>
    private static bool StartsAValue(RubyToken pToken)
    {
        return pToken.Kind switch
        {
            RubyTokenKind.Integer => true,
            RubyTokenKind.Float => true,
            RubyTokenKind.String => true,
            RubyTokenKind.Symbol => true,
            RubyTokenKind.Regexp => true,
            RubyTokenKind.Identifier => true,
            RubyTokenKind.Constant => true,
            RubyTokenKind.InstanceVariable => true,
            RubyTokenKind.GlobalVariable => true,
            RubyTokenKind.Keyword => pToken.Text is "nil" or "true" or
                "false" or "defined?" or "__LINE__" or "__FILE__"
                or "__ENCODING__" or "if" or "unless" or "case"
                or "begin" or "yield" or "super" or "self" or "lambda",
            RubyTokenKind.Delimiter => pToken.Text is "(" or "[" or "{",
            RubyTokenKind.Operator => pToken.Text is "-" or "+" or "!"
                or "~" or ".." or "..." or "*" or "&",
            _ => false,
        };
    }

    /// <summary>The parts of a call: what it is called on, then its arguments.</summary>
    private static IReadOnlyList<RubyNodePart> CallParts(
        RubyNode pReceiver, IReadOnlyList<RubyNode> pArguments)
    {
        var parts = new List<RubyNodePart>
        {
            new() { Role = RubyNodeRole.Receiver, Node = pReceiver },
        };
        foreach (var argument in pArguments)
        {
            parts.Add(new RubyNodePart { Role = RubyNodeRole.Argument, Node = argument });
        }

        return parts;
    }
    private static RubyNode Literal(
        RubyNodeKind pKind, int pLine, long? pInteger, double? pReal, string? pText,
        string? pName = null, int pOptionen = 0)
    {
        return new RubyNode
        {
            Kind = pKind,
            Line = pLine,
            Integer = pInteger,
            Real = pReal,
            Text = pText,
            Name = pName,
            Options = pOptionen,
        };
    }
}
