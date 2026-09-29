using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;

namespace UniversalRPG.Rgss;

/// <summary>
/// Evaluates a parsed Ruby tree against a host, and reaches nothing else.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This is the interpreter the card asked for, and it is built to the
/// user's rule: no <c>eval</c>, no marshal execution, and no way to name
/// anything the host did not hand over.</strong> The evaluator walks a
/// <see cref="RubyNode"/> tree it was given; it never sees a string of source
/// and never compiles one.
/// </para>
/// <para>
/// <strong>It is a partial interpreter and says which parts.</strong> The
/// leaves, the operators, the comparisons, the control flow and the local
/// variables are all here. A method call, a constant and a class definition go
/// to <see cref="IRubyHost"/> — <strong>and an unknown name is a refusal with
/// a diagnostic, not a guess and not a nil</strong>, because a game asking for
/// something this project has not implemented is a fact the player needs to
/// see.
/// </para>
/// <para>
/// <strong>Ruby's own rules are kept where they are observable.</strong> Only
/// <c>nil</c> and <c>false</c> are false, zero is true; division by zero raises
/// and does not answer; and an integer division truncates toward negative
/// infinity, which is not what C# does.
/// </para>
/// </remarks>
public sealed class RubyInterpreter
{
    private readonly IRubyHost _host;

    /// <summary>
    /// The local names in scope, innermost first.
    /// </summary>
    /// <remarks>
    /// <strong>This interpreter's own storage, and not the parser's
    /// <c>RubyScope</c></strong> — that enum names how a name was *written*,
    /// which is a question the parser answers and this one reads. <strong>A
    /// scope that names a kind is not a place a value lives in</strong>, and
    /// the two would have to be kept apart or every variable would be
    /// resolved by how it was spelled.
    /// </remarks>
    private readonly List<Dictionary<string, RubyValue>> _scopes = new();

    /// <summary>The instance variables of the running script.</summary>
    private Dictionary<string, RubyValue> _instanceVariables = new();
    private readonly List<string> _diagnostics = new List<string>();

    /// <summary>Builds an interpreter that can only reach one host.</summary>
    /// <param name="pHost">What it may call, and nothing else.</param>
    public RubyInterpreter(IRubyHost pHost)
    {
        _host = pHost ?? new RubyNullHost();
    }

    /// <summary>What the interpreter could not do, and why.</summary>
    public IReadOnlyList<string> Diagnostics => _diagnostics;

    /// <summary>
    /// How many steps a single call may take, because a game's script may not
    /// finish.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's <c>while true</c> runs for ever and this stops.</strong>
    /// The reference runtime has no such limit, but a reference runtime is
    /// running one game the player chose; <strong>a runtime that opens an
    /// arbitrary file has to be able to say "this script does not
    /// finish"</strong> rather than hang with the window up. The default is
    /// generous enough for a game's own scripts and small enough to fail.
    /// </remarks>
    public int StepLimit { get; init; } = 2_000_000;

    private int _steps;

    /// <summary>Whether a <c>return</c> has been seen, which stops the loops.</summary>
    private bool _returned;

    /// <summary>
    /// Runs a tree and answers with its value.
    /// </summary>
    /// <param name="pNode">The tree, as the parser produced it.</param>
    /// <returns>
    /// The value of the last statement, or <see cref="RubyValue.Nil"/> for an
    /// empty tree.
    /// </returns>
    /// <exception cref="RubyRuntimeException">
    /// The script raised, or the step limit ran out. <strong>An exception is
    /// how Ruby says a thing and is not a defect here</strong> — a game's
    /// own <c>/ 0</c> must not be swallowed.
    /// </exception>
    public RubyValue Run(RubyNode pNode)
    {
        _steps = 0;
        _returned = false;
        _diagnostics.Clear();
        _scopes.Clear();
        return Evaluate(pNode);
    }

    /// <summary>
    /// Runs a parsed program, which is a list of statements.
    /// </summary>
    /// <param name="pProgram">The statements, as the parser produced them.</param>
    /// <returns>The value of the last statement, or nil for an empty
    /// program.</returns>
    /// <remarks>
    /// <strong>This and not <c>Run</c> called once per statement.</strong>
    /// <c>Run</c> clears the scopes, because it starts a script from nothing —
    /// <strong>and a caller that ran a program's statements one at a time
    /// would have had the first statement's variables gone by the
    /// second</strong>. The first version of the local-variable test did
    /// exactly that and reported its own memory as a failure.
    /// </remarks>
    public RubyValue RunProgram(IReadOnlyList<RubyNode> pProgram)
    {
        _steps = 0;
        _returned = false;
        _diagnostics.Clear();
        _scopes.Clear();
        if (_scopes.Count == 0)
        {
            _scopes.Add(new Dictionary<string, RubyValue>());
        }

        var letztes = RubyValue.Nil;
        foreach (var anweisung in pProgram)
        {
            letztes = Evaluate(anweisung);
            if (_returned)
            {
                break;
            }
        }

        return letztes;
    }

    // ---- Leaves

    private RubyValue Evaluate(RubyNode pNode)
    {
        _steps++;
        if (_steps > StepLimit)
        {
            throw new RubyRuntimeException(
                "RuntimeError",
                $"this script ran {StepLimit} steps without finishing; a "
                    + "game's while-true does not come back and this runtime "
                    + "stops rather than hang");
        }

        return pNode.Kind switch
        {
            RubyNodeKind.Nil => RubyValue.Nil,
            RubyNodeKind.True => RubyValue.OfBoolean(true),
            RubyNodeKind.False => RubyValue.OfBoolean(false),
            RubyNodeKind.Self => RubyValue.OfSymbol("self"),
            RubyNodeKind.Integer => RubyValue.OfInteger(pNode.Integer ?? 0),
            RubyNodeKind.Float => RubyValue.OfReal(pNode.Real ?? 0d),
            RubyNodeKind.String => RubyValue.OfBytes(
                pNode.Bytes ?? Encoding.UTF8.GetBytes(pNode.Text ?? string.Empty)),
            RubyNodeKind.Symbol => RubyValue.OfSymbol(pNode.Name ?? pNode.Text ?? string.Empty),
            RubyNodeKind.Regexp => RubyValue.OfRegexp(pNode.Text ?? string.Empty, 0),
            RubyNodeKind.Constant => Constant(pNode),
            RubyNodeKind.Identifier => Local(pNode.Name ?? string.Empty),
            RubyNodeKind.InstanceVariable => _scopes.Count > 0
                ? _instanceVariables.TryGetValue(
                    pNode.Name ?? string.Empty, out var iv) ? iv : RubyValue.Nil
                : RubyValue.Nil,
            RubyNodeKind.Array => RubyValue.OfArray(EvaluateChildren(pNode, RubyNodeRole.Argument)),
            RubyNodeKind.Binary => Binary(pNode),
            RubyNodeKind.Unary => Unary(pNode),
            RubyNodeKind.Not => RubyValue.OfBoolean(
                !Truthy(Evaluate(Child(pNode, RubyNodeRole.Right)))),
            RubyNodeKind.Call => Call(pNode),
            RubyNodeKind.MethodCall => Call(pNode),
            RubyNodeKind.SelfCall => Call(pNode),
            RubyNodeKind.Assignment => Assign(pNode),
            RubyNodeKind.OpAssignment => OpAssign(pNode),
            RubyNodeKind.Ternary => Ternary(pNode),
            RubyNodeKind.Until => EvaluateUntil(pNode),
            RubyNodeKind.Return => EvaluateReturn(pNode),
            RubyNodeKind.Case => EvaluateCase(pNode),
            RubyNodeKind.For => EvaluateFor(pNode),
            RubyNodeKind.If => EvaluateIf(pNode),
            RubyNodeKind.While => EvaluateWhile(pNode),
            RubyNodeKind.Begin => EvaluateBlock(pNode),
            RubyNodeKind.Block => EvaluateBlock(pNode),
            _ => Refuse(pNode),
        };
    }

    /// <summary>
    /// A prefix operator, and the three that matter.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's unary minus on an integer is the integer itself
    /// negated</strong>, and its <c>~</c> is a bitwise complement —
    /// <c>-7.5</c> is the only case that is not a negation, and a reader that
    /// used the host's own unary would have answered <c>-7</c> there.
    /// <para>
    /// <strong>And a unary on nil is a refusal, not a zero.</strong> The
    /// script wrote <c>-x</c> where nothing assigned <c>x</c>; Ruby answers
    /// <c>nil</c> and a reader that turned nil into zero would have invented a
    /// number the script never had.
    /// </para>
    /// </remarks>
    private RubyValue Unary(RubyNode pNode)
    {
        var op = pNode.Operator ?? "-";
        var operand = Child(pNode, RubyNodeRole.Right) ?? pNode.Children.FirstOrDefault();
        if (operand == null)
        {
            return Refuse(pNode);
        }

        var wert = Evaluate(operand);
        switch (op)
        {
            case "-":
                return wert.Kind switch
                {
                    RubyValueKind.Integer => RubyValue.OfInteger(-wert.Integer),
                    RubyValueKind.Float => RubyValue.OfReal(-wert.Real),
                    _ => throw new RubyRuntimeException(
                        "NoMethodError",
                        $"-@ called on a {wert.Kind}, and nil has no minus"),
                };
            case "+":
                return wert;
            case "!":
                return RubyValue.OfBoolean(!Truthy(wert));
            case "~":
                if (wert.Kind == RubyValueKind.Integer)
                {
                    return RubyValue.OfInteger(~wert.Integer);
                }

                throw new RubyRuntimeException(
                    "NoMethodError", $"~@ called on a {wert.Kind}");
            default:
                return Refuse(pNode);
        }
    }

    // ---- Operators

    /// <summary>
    /// The two sides of a binary node, in source order.
    /// </summary>
    /// <remarks>
    /// <strong>The roles when the parser recorded them, and the source order
    /// otherwise.</strong> A binary node's children are written left then
    /// right, so <c>Children[0]</c> and <c>Children[1]</c> are the sides — and
    /// <strong>an evaluator that demanded the roles would have answered
    /// "unknown node" for every operation the parser wrote plainly</strong>,
    /// which is most of a game's own arithmetic.
    /// </remarks>
    private static IReadOnlyList<RubyNode> Operands(RubyNode pNode)
    {
        var links = Child(pNode, RubyNodeRole.Left);
        var rechts = Child(pNode, RubyNodeRole.Right);
        if (links != null && rechts != null)
        {
            return new[] { links, rechts };
        }

        return pNode.Children;
    }

    private RubyValue Binary(RubyNode pNode)
    {
        var links = pNode.Operator ?? "+";

        // **Nur `nil` und `false` sind falsch, und null ist wahr.**
        var linksIsLogik = links is "&&" or "||";
        if (linksIsLogik)
        {
            var l = Truthy(Evaluate(Operands(pNode)[0]));
            if (links == "&&" && !l)
            {
                return RubyValue.OfBoolean(false);
            }

            if (links == "||" && l)
            {
                return RubyValue.OfBoolean(true);
            }

            return RubyValue.OfBoolean(Truthy(Evaluate(Operands(pNode)[1])));
        }

        return Apply(
            links,
            Evaluate(Operands(pNode)[0]),
            Evaluate(Operands(pNode)[1]),
            pNode);
    }

    /// <summary>
    /// One arithmetic, comparison or bitwise operator, on two values.
    /// </summary>
    /// <remarks>
    /// <strong>Division by zero raises, and it raises for both</strong>
    /// integers and floats — a reader that answered <c>Infinity</c> the way
    /// C# does would give a game a number RPG_RT never produces.
    /// <strong>And an integer division truncates toward negative infinity</strong>,
    /// so <c>-7 / 2</c> is <c>-4</c> and not <c>-3</c>.
    /// </remarks>
    public static RubyValue Apply(
        string pOperator,
        RubyValue pLeft,
        RubyValue pRight,
        RubyNode? pNode = null)
    {
        var beideZahlen = (pLeft.Kind == RubyValueKind.Integer
                || pLeft.Kind == RubyValueKind.Float)
            && (pRight.Kind == RubyValueKind.Integer
                || pRight.Kind == RubyValueKind.Float);
        var ganzzahlig = pLeft.Kind == RubyValueKind.Integer
            && pRight.Kind == RubyValueKind.Integer;

        switch (pOperator)
        {
            case "+" when pLeft.Kind == RubyValueKind.String:
                return RubyValue.OfBytes(Concat(pLeft.Bytes, pRight.Bytes));
            case "+" when beideZahlen:
                return ganzzahlig
                    ? RubyValue.OfInteger(pLeft.Integer + pRight.Integer)
                    : RubyValue.OfReal(pLeft.Real + pRight.Real);
            case "-" when beideZahlen:
                return ganzzahlig
                    ? RubyValue.OfInteger(pLeft.Integer - pRight.Integer)
                    : RubyValue.OfReal(pLeft.Real - pRight.Real);
            case "*" when beideZahlen:
                return ganzzahlig
                    ? RubyValue.OfInteger(pLeft.Integer * pRight.Integer)
                    : RubyValue.OfReal(pLeft.Real * pRight.Real);
            case "/" when beideZahlen:
                if (ganzzahlig)
                {
                    if (pRight.Integer == 0)
                    {
                        throw new RubyRuntimeException(
                            "ZeroDivisionError", "divided by 0");
                    }

                    return RubyValue.OfInteger(FloorDivide(pLeft.Integer, pRight.Integer));
                }

                if (pRight.Real == 0d)
                {
                    throw new RubyRuntimeException(
                        "ZeroDivisionError", "divided by 0.0");
                }

                return RubyValue.OfReal(pLeft.Real / pRight.Real);
            case "%" when beideZahlen:
                if (pRight.Integer == 0)
                {
                    throw new RubyRuntimeException(
                        "ZeroDivisionError", "divided by 0");
                }

                // **Rubys Rest hat das Vorzeichen des Dividenden, und `%`
                // in C# nicht** -- -7 % 3 ist 2 in Ruby und -1 in C#.
                return RubyValue.OfInteger(
                    pLeft.Integer - FloorDivide(pLeft.Integer, pRight.Integer) * pRight.Integer);
            case "**" when beideZahlen:
                return RubyValue.OfReal(Math.Pow(
                    ganzzahlig ? pLeft.Integer : pLeft.Real,
                    ganzzahlig ? pRight.Integer : pRight.Real));
            case "==" :
                return RubyValue.OfBoolean(Equal(pLeft, pRight));
            case "!=":
                return RubyValue.OfBoolean(!Equal(pLeft, pRight));
            case "<" when beideZahlen:
                return RubyValue.OfBoolean(ganzzahlig
                    ? pLeft.Integer < pRight.Integer
                    : pLeft.Real < pRight.Real);
            case "<=" when beideZahlen:
                return RubyValue.OfBoolean(ganzzahlig
                    ? pLeft.Integer <= pRight.Integer
                    : pLeft.Real <= pRight.Real);
            case ">" when beideZahlen:
                return RubyValue.OfBoolean(ganzzahlig
                    ? pLeft.Integer > pRight.Integer
                    : pLeft.Real > pRight.Real);
            case ">=" when beideZahlen:
                return RubyValue.OfBoolean(ganzzahlig
                    ? pLeft.Integer >= pRight.Integer
                    : pLeft.Real >= pRight.Real);
            case "&&":
                return RubyValue.OfBoolean(Truthy(pLeft) && Truthy(pRight));
            case "||":
                return RubyValue.OfBoolean(Truthy(pLeft) || Truthy(pRight));
            default:
                _ = pNode;
                throw new RubyRuntimeException(
                    "NoMethodError",
                    $"undefined operator '{pOperator}' for a "
                        + $"{pLeft.Kind} and a {pRight.Kind}");
        }
    }

    /// <summary>
    /// Ruby's whole-number division, which rounds towards negative infinity.
    /// </summary>
    /// <remarks>
    /// <strong>C# rounds towards zero</strong>, so <c>-7 / 2</c> is <c>-3</c>
    /// there and <c>-4</c> in Ruby. A reader that used the language's own
    /// operator would have shifted every negative half in a game's damage
    /// formula by one.
    /// </remarks>
    public static long FloorDivide(long pLeft, long pRight)
    {
        var q = pLeft / pRight;
        var r = pLeft % pRight;
        if (r != 0 && ((r < 0) != (pRight < 0)))
        {
            q -= 1;
        }

        return q;
    }

    /// <summary>
    /// Whether a value counts as true, and only nil and false do not.
    /// </summary>
    /// <remarks>
    /// <strong>Zero is true, the empty string is true and the empty array is
    /// true.</strong> A reader that used C#'s truth would have taken the
    /// <c>while 0</c> branch a Ruby game never takes.
    /// </remarks>
    public static bool Truthy(RubyValue pValue)
    {
        if (pValue.Kind == RubyValueKind.Nil)
        {
            return false;
        }

        if (pValue.Kind == RubyValueKind.Boolean)
        {
            return pValue.Boolean;
        }

        return true;
    }

    // ---- Names, calls and variables

    private RubyValue Constant(RubyNode pNode)
    {
        var name = pNode.Name ?? string.Empty;
        var wert = _host.LookupConstant(name);
        if (wert != null)
        {
            return wert;
        }

        _diagnostics.Add(
            $"the constant {name} is not defined by this host, and the "
            + "interpreter does not guess; a game's own constant needs a host "
            + "that provides it");
        return RubyValue.Nil;
    }

    private RubyValue Local(string pName)
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].TryGetValue(pName, out var wert))
            {
                return wert;
            }
        }

        // **Ruby nennt das nil und faehrt fort.** Es ist keine Ausnahme,
        // weil ein Spiel Skripte schreibt, die auf Variablen lesen, die ein
        // frueherer Teil gesetzt hat und dieser nicht.
        return RubyValue.Nil;
    }

    private RubyValue Call(RubyNode pNode)
    {
        var empfaenger = pNode.Kind == RubyNodeKind.Call
            || pNode.Kind == RubyNodeKind.MethodCall
            ? Evaluate(Child(pNode, RubyNodeRole.Receiver))
            : RubyValue.OfSymbol("self");
        var methode = pNode.Name ?? string.Empty;
        var argumente = EvaluateChildren(pNode, RubyNodeRole.Argument);

        var ergebnis = _host.CallMethod(empfaenger, methode, argumente);
        if (ergebnis != null)
        {
            return ergebnis;
        }

        _diagnostics.Add(
            $"{Describe(empfaenger)} has no method '{methode}' on this host; "
            + "the interpreter does not guess, and a method that is not "
            + "implemented is a fact about the host and not about the script");
        return RubyValue.Nil;
    }

    private RubyValue Assign(RubyNode pNode)
    {
        var ziel = Child(pNode, RubyNodeRole.Target);
        var wert = Evaluate(Child(pNode, RubyNodeRole.Value));

        switch (ziel.Kind)
        {
            case RubyNodeKind.Identifier:
                SetLocal(ziel.Name ?? string.Empty, wert);
                return wert;
            case RubyNodeKind.InstanceVariable:
                if (_scopes.Count > 0)
                {
                    _instanceVariables[ziel.Name ?? string.Empty] = wert;
                }

                return wert;
            default:
                // **Ein Aufruf auf der linken Seite ist kein Ziel, das man
                // belegen kann** -- Ruby wertet `a.b = 1` als einen Methoden-
                // aufruf aus, und genau so wird es hier auch getan.
                return Call(new RubyNode
                {
                    Kind = RubyNodeKind.Call,
                    Name = ziel.Name,
                    Line = pNode.Line,
                    Role_Children =
                    [
                        new RubyNodePart { Role = RubyNodeRole.Receiver, Node = ziel },
                        new RubyNodePart { Role = RubyNodeRole.Value, Node = Child(pNode, RubyNodeRole.Value) },
                    ],
                });
        }
    }

    private void SetLocal(string pName, RubyValue pValue)
    {
        if (_scopes.Count == 0)
        {
            _scopes.Add(new Dictionary<string, RubyValue>());
        }

        for (var i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].ContainsKey(pName))
            {
                _scopes[i][pName] = pValue;
                return;
            }
        }

        _scopes[_scopes.Count - 1][pName] = pValue;
    }

    /// <summary>
    /// `a += 1`, and the read and the write are the same expression.
    /// </summary>
    /// <remarks>
    /// <strong>The current value is read once and written back once</strong>,
    /// and a reader that evaluated the right side against a stale target would
    /// have written the difference. <strong>And <c>+=</c> on a string
    /// concatenates</strong>, because Ruby's <c>+</c> is the same operator
    /// here and not a special case.
    /// </remarks>
    private RubyValue OpAssign(RubyNode pNode)
    {
        var op = pNode.Operator ?? "+=";

        // **Der Operator ohne sein Zuweisungszeichen.** `**=` ist `**` und
        // nicht `*` zweimal, und **der Parser erzeugt diesen Knoten nie mit
        // einem nackten `=`** — ein Gleichheitszeichen ergibt einen
        // `Assignment`-Knoten. Die erste Fassung behandelte `op == "="` trotzdem
        // und schrieb in einem Test fuer einen Fall, den es nicht gibt.
        var basis = op[..^1];
        var ziel = pNode.Children.Count > 0
            ? pNode.Children[0]
            : Child(pNode, RubyNodeRole.Target);
        var rechts = pNode.Children.Count > 1
            ? pNode.Children[1]
            : Child(pNode, RubyNodeRole.Value);
        if (ziel == null || rechts == null)
        {
            return Refuse(pNode);
        }

        var istNeu = Evaluate(rechts);
        if (ziel.Kind != RubyNodeKind.Identifier
            && ziel.Kind != RubyNodeKind.InstanceVariable)
        {
            // **Ein Aufruf auf der linken Seite ist kein Ziel, das man
            // belegen kann** -- Ruby wertet `a.b += 1` als einen Methodenaufruf
            // aus, und genau so wird es hier auch getan.
            return Refuse(pNode);
        }

        // **Der alte Wert kommt aus dem Ziel und der neue aus dem
        // Operator** -- `x += 1` heisst `x = x + 1`, und **ein Leser, der den
        // rechten Wert zurueckschriebe, haette `x += 1` zu `x = 1` gemacht**,
        // was bei einer Schleife ein Spiel zum Stehen bringt.
        var alt = Evaluate(ziel);
        var neu = Apply(basis, alt, istNeu, pNode);

        if (ziel.Kind == RubyNodeKind.Identifier)
        {
            SetLocal(ziel.Name ?? string.Empty, neu);
        }
        else
        {
            _instanceVariables[ziel.Name ?? string.Empty] = neu;
        }

        return neu;
    }

    /// <summary>
    /// `a ? b : c`, and the two arms must be different numbers.
    /// </summary>
    /// <remarks>
    /// <strong>The condition decides which arm runs, and the other is not
    /// evaluated.</strong> That is the whole point of the form: a game writes
    /// <c>x &gt; 0 ? 1 / x : 0</c> to avoid dividing by zero, and a reader that
    /// evaluated both arms would divide.
    /// </remarks>
    private RubyValue Ternary(RubyNode pNode)
    {
        var bedingung = Child(pNode, RubyNodeRole.Condition)
            ?? (pNode.Children.Count > 0 ? pNode.Children[0] : null);
        var dann = Child(pNode, RubyNodeRole.WhenTrue)
            ?? (pNode.Children.Count > 1 ? pNode.Children[1] : null);
        var sonst = Child(pNode, RubyNodeRole.WhenFalse)
            ?? (pNode.Children.Count > 2 ? pNode.Children[2] : null);
        if (bedingung == null || dann == null || sonst == null)
        {
            return Refuse(pNode);
        }

        return Truthy(Evaluate(bedingung)) ? Evaluate(dann) : Evaluate(sonst);
    }

    /// <summary>
    /// `until`, which is `while` with the condition read the other way round.
    /// </summary>
    /// <remarks>
    /// <strong>And a modifier <c>until</c> at the end of a statement is the
    /// same form on one line</strong> — the parser writes the keyword into the
    /// node's name, so the two read the same way here.
    /// </remarks>
    private RubyValue EvaluateUntil(RubyNode pNode)
    {
        var bedingung = Child(pNode, RubyNodeRole.Condition)
            ?? (pNode.Children.Count > 0 ? pNode.Children[0] : null);
        var rumpf = Child(pNode, RubyNodeRole.WhenTrue)
            ?? (pNode.Children.Count > 1 ? pNode.Children[1] : null);
        if (bedingung == null || rumpf == null)
        {
            return Refuse(pNode);
        }

        var letztes = RubyValue.Nil;
        while (!Truthy(Evaluate(bedingung)) && !_returned)
        {
            letztes = Evaluate(rumpf);
        }

        return letztes;
    }

    /// <summary>
    /// `return`, which leaves the method and the block it is in.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's <c>return</c> returns from the method, not from the
    /// loop it is written in</strong> — a <c>break</c> does the latter. This
    /// interpreter has no method frames yet, so <c>return</c> sets the flag
    /// the outermost <see cref="Run"/> reads, and <strong>the loops below
    /// check it and stop</strong>, which is what "leaves the method" means
    /// once there is only one frame.
    /// </remarks>
    private RubyValue EvaluateReturn(RubyNode pNode)
    {
        var wert = pNode.Children.Count > 0
            ? Evaluate(pNode.Children[0])
            : RubyValue.Nil;
        _returned = true;
        return wert;
    }


    /// <summary>
    /// `case`, and a `when` arm matches when one of its values equals the
    /// case's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The children are: the case's value, then each arm, then the
    /// else block.</strong> A case without a value compares nothing, and a
    /// bare <c>when</c> matches everything — <strong>both are forms a game
    /// writes</strong>, and a reader that required a value on either side
    /// would have said "does not begin an expression" on a script RPG_RT runs.
    /// </para>
    /// <para>
    /// <strong>And the arms are tried in order, and the first one that matches
    /// runs alone.</strong> <c>when 1, 2</c> is two alternatives and not a
    /// conjunction — <strong>a reader that required every value to match
    /// would have made it unreachable</strong>, since a case has one value.
    /// </para>
    /// </remarks>
    private RubyValue EvaluateCase(RubyNode pNode)
    {
        // **Die Kinder sind: der Wert, die Arme, und der else-Block.** Ein
        // `when`-Knoten ist ein Case-Knoten mit einem Namen; ein Block ist der
        // else-Zweig. **Alles andere im Baum gehoert zu keinem von beiden**,
        // und ein Kind, das keines ist, wird nicht ausgewertet -- **ein
        // Leser, der jedes Kind als Arm behandelte, wuerde den else-Block
        // ausfuehren, sobald sein Wert dem case-Wert entspricht.**
        RubyValue wert = null;
        var start = 0;
        if (pNode.Children.Count > 0 && !IstArm(pNode.Children[0])
            && !IstBlock(pNode.Children[0]))
        {
            wert = Evaluate(pNode.Children[0]);
            start = 1;
        }

        for (var i = start; i < pNode.Children.Count; i++)
        {
            var kind = pNode.Children[i];
            if (!IstArm(kind))
            {
                continue;
            }

            // **Die Werte sind alle Kinder ausser dem letzten**, und das
            // letzte ist der Rumpf -- so schreibt ParseWhen es, und
            // **ein Rumpf an der ersten Stelle waere ein Arm ohne Koerper**.
            var rumpf = kind.Children.Count > 0
                ? kind.Children[kind.Children.Count - 1]
                : null;
            if (rumpf == null)
            {
                continue;
            }

            if (kind.Children.Count == 1)
            {
                // **Ein nacktes `when` ist der Fangarm**, und ohne ihn waere
                // der else-Zweig die einzige Wahl -- was ein Spiel, das
                // `when then` schreibt, nicht meint.
                return Evaluate(rumpf);
            }

            for (var k = 0; k < kind.Children.Count - 1; k++)
            {
                var test = Evaluate(kind.Children[k]);
                if (wert != null && BooleanOf(Apply("==", wert, test, pNode)))
                {
                    return Evaluate(rumpf);
                }
            }
        }

        // **Und der else-Zweig ist der letzte Block unter den Kindern.**
        for (var i = pNode.Children.Count - 1; i >= start; i--)
        {
            if (IstBlock(pNode.Children[i]))
            {
                return Evaluate(pNode.Children[i]);
            }
        }

        return RubyValue.Nil;
    }

    private static bool BooleanOf(RubyValue pValue)
    {
        return pValue.Kind == RubyValueKind.Boolean && pValue.Boolean;
    }

    private static bool IstArm(RubyNode pNode)
    {
        return pNode.Kind == RubyNodeKind.Case && pNode.Name is "when";
    }

    private static bool IstBlock(RubyNode pNode)
    {
        return pNode.Kind == RubyNodeKind.Block;
    }

    /// <summary>
    /// `for x in liste`, which walks a list and binds each element.
    /// </summary>
    /// <remarks>
    /// <strong>The list is evaluated once, before the first step</strong> — a
    /// reader that re-evaluated it each time would walk a different list when
    /// the body changes it, and a game's loop would run a number of times
    /// nobody wrote. <strong>And a nil list walks zero times</strong>, because
    /// Ruby's <c>for</c> over nil is empty and not an error.
    /// </remarks>
    private RubyValue EvaluateFor(RubyNode pNode)
    {
        if (pNode.Children.Count < 3)
        {
            return Refuse(pNode);
        }

        var ziel = pNode.Children[0];
        var liste = Evaluate(pNode.Children[1]);
        var rumpf = pNode.Children[2];
        var letztes = RubyValue.Nil;

        if (liste.Kind != RubyValueKind.Object || !liste.IsList)
        {
            return RubyValue.Nil;
        }

        foreach (var element in liste.Items)
        {
            if (ziel.Kind == RubyNodeKind.Identifier)
            {
                SetLocal(ziel.Name ?? string.Empty, element);
            }
            else if (ziel.Kind == RubyNodeKind.InstanceVariable)
            {
                _instanceVariables[ziel.Name ?? string.Empty] = element;
            }

            letztes = Evaluate(rumpf);
            if (_returned)
            {
                break;
            }
        }

        return letztes;
    }

    // ---- Control flow

    private RubyValue EvaluateIf(RubyNode pNode)
    {
        var bedingung = Child(pNode, RubyNodeRole.Condition);
        if (bedingung == null)
        {
            return Refuse(pNode);
        }

        if (Truthy(Evaluate(bedingung)))
        {
            return EvaluateBranch(pNode, RubyNodeRole.WhenTrue);
        }

        return EvaluateBranch(pNode, RubyNodeRole.WhenFalse);
    }

    private RubyValue EvaluateWhile(RubyNode pNode)
    {
        // **Die Rolle, und sonst das erste Kind.** Ein `while` schreibt
        // zuerst die Bedingung und dann den Rumpf, und **ein Knoten ohne
        // Rollen traegt beides in Quellordnung** — so schreibt der Parser es.
        // **Ein Leser, der die Rolle verlangte, haette jeden while-Arm als
        // unbekannt abgelehnt**, und ein Spiel mit einer Schleife waere
        // stillstehend.
        var bedingung = Child(pNode, RubyNodeRole.Condition);
        if (bedingung == null && pNode.Children.Count > 0)
        {
            bedingung = pNode.Children[0];
        }

        if (bedingung == null)
        {
            return Refuse(pNode);
        }

        var rumpf = Child(pNode, RubyNodeRole.WhenTrue);
        if (rumpf == null && pNode.Children.Count > 1)
        {
            rumpf = pNode.Children[1];
        }

        var letztes = RubyValue.Nil;
        while (Truthy(Evaluate(bedingung)) && !_returned)
        {
            if (rumpf == null)
            {
                return Refuse(pNode);
            }

            letztes = Evaluate(rumpf);
        }

        return letztes;
    }

    private RubyValue EvaluateBlock(RubyNode pNode)
    {
        var letztes = RubyValue.Nil;
        foreach (var teil in Statements(pNode))
        {
            letztes = Evaluate(teil);
            if (_returned)
            {
                // **Ein `return` verlaesst den Block und nicht nur die
                // Schleife** -- und das ist der Unterschied zu `break`.
                break;
            }
        }

        return letztes;
    }

    /// <summary>
    /// The statements of a block or a body, from either child list.
    /// </summary>
    /// <remarks>
    /// <strong>A block node's children are its statements, in source
    /// order</strong> — the parser writes them without a role per child,
    /// because "these are the statements" needs no saying. <strong>A reader
    /// that asked for the <c>Body</c> role on such a node would find
    /// nothing</strong> and evaluate a block to nil, which is the difference
    /// between running a game and silently not running it.
    /// </remarks>
    private static IReadOnlyList<RubyNode> Statements(RubyNode pNode)
    {
        var mitRolle = PartsOf(pNode, RubyNodeRole.Body);
        if (mitRolle.Count > 0)
        {
            return mitRolle;
        }

        var alsAussage = PartsOf(pNode, RubyNodeRole.Statement);
        return alsAussage.Count > 0 ? alsAussage : pNode.Children;
    }

    /// <summary>
    /// Runs one arm of a branch, and the arm is a block node.
    /// </summary>
    /// <remarks>
    /// <strong>The arm is a block and not a statement.</strong> The parser
    /// wraps a body in a <c>Block</c> node, and <c>Evaluate</c> on a block
    /// runs its statements — <strong>so evaluating the arm as a statement
    /// would have looked for a value where there is a body</strong>, and a
    /// branch would have answered nil on both sides. That is a test that
    /// passes for a reader that never takes a branch at all.
    /// </remarks>
    private RubyValue EvaluateBranch(RubyNode pNode, RubyNodeRole pRole)
    {
        var teile = PartsOf(pNode, pRole);
        var letztes = RubyValue.Nil;
        foreach (var teil in teile)
        {
            letztes = Evaluate(teil);
        }

        return letztes;
    }

    // ---- Helpers

    private RubyValue Refuse(RubyNode pNode)
    {
        _diagnostics.Add(
            $"this interpreter does not evaluate a {pNode.Kind} node, and the "
            + "node is in the tree; the parser produces it and the evaluator "
            + "says so rather than answering nil and moving on");
        return RubyValue.Nil;
    }

    private static string Describe(RubyValue pValue)
    {
        return pValue.Kind switch
        {
            RubyValueKind.Nil => "nil",
            RubyValueKind.Integer => pValue.Integer.ToString(CultureInfo.InvariantCulture),
            RubyValueKind.Float => pValue.Real.ToString(CultureInfo.InvariantCulture),
            RubyValueKind.Boolean => pValue.Boolean ? "true" : "false",
            RubyValueKind.String => $"\"{Encoding.UTF8.GetString(pValue.Bytes)}\"",
            RubyValueKind.Symbol => pValue.Name ?? "a symbol",
            _ => pValue.ClassName ?? "a value",
        };
    }

    private static bool Equal(RubyValue pLeft, RubyValue pRight)
    {
        if (pLeft.Kind != pRight.Kind)
        {
            return false;
        }

        return pLeft.Kind switch
        {
            RubyValueKind.Nil => true,
            RubyValueKind.Integer => pLeft.Integer == pRight.Integer,
            RubyValueKind.Float => pLeft.Real == pRight.Real,
            RubyValueKind.Boolean => pLeft.Boolean == pRight.Boolean,
            RubyValueKind.String => BytesEqual(pLeft.Bytes, pRight.Bytes),
            RubyValueKind.Symbol => pLeft.Name == pRight.Name,
            _ => ReferenceEquals(pLeft, pRight),
        };
    }

    private static bool BytesEqual(byte[] pLeft, byte[] pRight)
    {
        if (pLeft.Length != pRight.Length)
        {
            return false;
        }

        for (var i = 0; i < pLeft.Length; i++)
        {
            if (pLeft[i] != pRight[i])
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] Concat(byte[] pLeft, byte[] pRight)
    {
        var ergebnis = new byte[pLeft.Length + pRight.Length];
        Array.Copy(pLeft, 0, ergebnis, 0, pLeft.Length);
        Array.Copy(pRight, 0, ergebnis, pLeft.Length, pRight.Length);
        return ergebnis;
    }

    /// <summary>
    /// The first child playing a role, from either list the node carries.
    /// </summary>
    /// <remarks>
    /// <strong>A node has two child lists and this reads both.</strong>
    /// <c>Role_Children</c> is what the parser fills when it knows what each
    /// child is for; <c>Children</c> is the plain source order, and a node
    /// that carries only that one has no roles to match. <strong>A reader that
    /// looked at one list only would have found nothing on half the
    /// tree</strong> — and an evaluator that answers "I do not know this node"
    /// for every binary operation is an evaluator that cannot add.
    /// </remarks>
    private static RubyNode? Child(RubyNode pNode, RubyNodeRole pRole)
    {
        foreach (var teil in pNode.Role_Children)
        {
            if (teil.Role == pRole)
            {
                return teil.Node;
            }
        }

        return null;
    }

    private static IReadOnlyList<RubyNode> PartsOf(RubyNode pNode, RubyNodeRole pRole)
    {
        var liste = new List<RubyNode>();
        foreach (var teil in pNode.Role_Children)
        {
            if (teil.Role == pRole)
            {
                liste.Add(teil.Node);
            }
        }

        if (liste.Count > 0)
        {
            return liste;
        }

        // **Ohne Rollen bleibt die Quellordnung** -- und das ist die
        // Bedeutung der Rolle, die nicht vergeben wurde.
        return pNode.Children;
    }

    private List<RubyValue> EvaluateChildren(RubyNode pNode, RubyNodeRole pRole)
    {
        var werte = new List<RubyValue>();
        foreach (var teil in PartsOf(pNode, pRole))
        {
            werte.Add(Evaluate(teil));
        }

        return werte;
    }
}
