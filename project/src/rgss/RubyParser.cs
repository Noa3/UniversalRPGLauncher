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
    private List<RubyNode> ParseStatements(string pCloser)
    {
        var statements = new List<RubyNode>();
        while (true)
        {
            SkipNewlines();
            if (AtEnd)
            {
                if (pCloser == "end of input")
                {
                    return statements;
                }
                throw new RubyParseException(
                    $"'{pCloser}' was expected, but the script ends first.",
                    Current.Line);
            }
            if (Is(pCloser))
            {
                return statements;
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
                    };
                    continue;
                }
                node = new RubyNode
                {
                    Kind = RubyNodeKind.Call,
                    Name = name,
                    Line = node.Line,
                    Children = [node],
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
                var whenTrue = ReadBody("end");
                return new RubyNode
                {
                    Kind = RubyNodeKind.If,
                    Name = pToken.Text,
                    Line = pToken.Line,
                    Children = [condition, whenTrue],
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
                var arguments = ReadParameterList();
                SkipNewlines();
                var body = ReadBody("end");
                return new RubyNode
                {
                    Kind = RubyNodeKind.Def,
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
                if (Is("<"))
                {
                    _index++;
                    ReadConstantPath();
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
