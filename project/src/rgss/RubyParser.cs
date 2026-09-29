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
        ["||"],
        ["&&"],
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

    private RubyNode ParseAssignment()
    {
        var left = ParseTernary();
        if (Is("=") || Is("=>"))
        {
            var op = Take().Text;
            var right = ParseAssignment();
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
        foreach (var op in new[] { "+=", "-=", "*=", "/=", "%=", "**=", "<<=", ">>=", "|=", "&=", "^=" })
        {
            if (Is(op))
            {
                _index++;
                var right = ParseAssignment();
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
            if (Current.Kind != RubyTokenKind.Operator)
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
            if (Is("["))
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
            if (Is("(") && CanStartACall(node))
            {
                var arguments = ReadArguments();
                node = new RubyNode
                {
                    Kind = RubyNodeKind.Call,
                    Name = node.Name ?? string.Empty,
                    Line = node.Line,
                    Children = [node, .. arguments],
                    Role_Children = CallParts(node, arguments),
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
        throw new RubyParseException(
            $"A member name was expected at offset {token.Offset}, but '{token.Text}' is there.",
            token.Line);
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
            arguments.Add(ParseExpression());
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
        while (!IsKeyword("then"))
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
                return Literal(RubyNodeKind.Regexp, token.Line, null, null, token.Value);
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
                if (Is("("))
                {
                    var mit_klammern = ReadArguments();
                    return new RubyNode
                    {
                        Kind = RubyNodeKind.Call,
                        Name = token.Text,
                        Line = token.Line,
                        Children = [node, .. mit_klammern],
                        Role_Children = CallParts(node, mit_klammern),
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
                    // **Erst hier wird der Zeilenumbruch uebersprungen, und
                    // nur fuer die Form ohne Argumente.** `attr_accessor`
                    // allein steht am Zeilenende, **und ohne dieses
                    // `SkipNewlines` waere der Zweig nie erreicht**, weil
                    // `Current` dann schon das `end` der naechsten Zeile
                    // waere.
                    SkipNewlines();
                    var argumente = new List<RubyNode>();
                    while (true)
                    {
                        argumente.Add(ParsePostfix(ParsePrimary()));
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
                var whenRuest = ReadBodyUntil("when", "else", "end");
                var children = new List<RubyNode>();
                if (wert != null)
                {
                    children.Add(wert);
                }

                children.Add(whenRuest);
                RubyNode elseBlock = null;
                if (IsKeyword("else"))
                {
                    _index++;
                    SkipNewlines();
                    elseBlock = ReadBody("end");
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
                        elseBlock = ReadBody("end");
                    }
                }

                // **Der Schlusser ist in beiden Wegen schon verbraucht.**
                // `ReadBody("end")` nimmt ihn, und ein `case` ohne else
                // wurde ueber `ReadBodyUntil` gelesen -- **das laesst ihn
                // stehen, und genau diesen einen Fall nimmt der Pfad hier
                // noch selbst.** Eine Pruefung, die in beiden Faellen noch
                // einmal nach `end` sieht, wuerde bei jedem
                // `case ... else ... end` fehlschlagen.
                if (elseBlock == null && whenRuest.Name != "end")
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
            case "def":
            {
                _index++;
                var name = ReadMemberName();
                // **`def self.x` ist eine Methode auf der Klasse selbst**,
                // und der Unterschied ist der einzige Punkt an diesem
                // Schluesselwort -- **ein Leser, der ihn uebersieht, wuerde
                // jede Klassenmethode eines Spiels zur Instanzmethode machen**,
                // und ein `self.`-Aufruf darin haette kein Ziel.
                var aufSelbst = false;
                if (name == "self" && Is("."))
                {
                    // **Der Punkt ist noch da** -- `ReadMemberName` hat nur
                    // "self" genommen, **und ohne ihn waere der zweite
                    // Lesevorgang auf einem Punkt gelandet**, was als
                    // Syntaxfehler endet und nicht als "eine Klassenmethode".
                    _index++;
                    name = ReadMemberName();
                    aufSelbst = true;
                }

                var arguments = ReadParameterList();
                SkipNewlines();
                var body = ReadBody("end");
                return new RubyNode
                {
                    Kind = aufSelbst ? RubyNodeKind.DefS : RubyNodeKind.Def,
                    Name = name,
                    Line = pToken.Line,
                    Children = [arguments, body],
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
                var body = ReadBody("end");
                return new RubyNode
                {
                    Kind = RubyNodeKind.Begin,
                    Line = pToken.Line,
                    Children = [body],
                };
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
            var token = Take();
            parameters.Add(Literal(
                RubyNodeKind.Identifier, token.Line, null, null, null, token.Text));
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

    /// <summary>True where a value may begin, so an operator after it is binary.</summary>
    private bool StartsAValue()
    {
        return Current.Kind switch
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
            RubyTokenKind.Keyword => Current.Text is "nil" or "true" or "false" or "self"
                or "defined?" or "__LINE__" or "__FILE__" or "__ENCODING__" or "not"
                or "if" or "unless" or "case" or "begin" or "yield" or "super" or "return"
                or "lambda",
            RubyTokenKind.Delimiter => Current.Text is "(" or "[" or "{",
            RubyTokenKind.Operator => Current.Text is "-" or "+" or "!" or "~" or "::"
                or ".." or "..." or "*" or "&",
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
        string? pName = null)
    {
        return new RubyNode
        {
            Kind = pKind,
            Line = pLine,
            Integer = pInteger,
            Real = pReal,
            Text = pText,
            Name = pName,
        };
    }
}
