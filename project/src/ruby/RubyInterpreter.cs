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

    /// <summary>
    /// The globals, by name without the dollar sign.
    /// </summary>
    /// <remarks>
    /// <strong>One flat table and not one per scope.</strong> That is Ruby's
    /// own rule and the reason a game's flag is visible from a method it did
    /// not pass through — <strong>and a reader that scoped them would have
    /// made a global that a method sets invisible to the code that reads
    /// it.</strong>
    /// </remarks>
    private readonly Dictionary<string, RubyValue> _globals = new(StringComparer.Ordinal);
    private readonly List<string> _diagnostics = new List<string>();

    /// <summary>
    /// The last constant the host did not know, for the caller that needs
    /// the name and not the value.
    /// </summary>
    /// <remarks>
    /// <strong>One value, and the newest wins.</strong> `include Fehlt` runs
    /// the constant first and reads the name straight after, and there is no
    /// other caller between the two — <strong>a reader that kept a list
    /// would have had to decide which entry belonged to which
    /// argument.</strong>
    /// </remarks>
    private string? _letzteUnbekannteKonstante;

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

    /// <summary>The class the script is inside, or null at the top.</summary>
    private RubyType? _aktuellerTyp;

    /// <summary>
    /// The classes a method call is walking, outermost first.
    /// </summary>
    /// <remarks>
    /// <strong>This is what <c>super</c> needs and it is a stack and not a
    /// name.</strong> A method that calls the base's method of the same name
    /// pushes the class it ran in, and a <c>super</c> inside that one pushes
    /// the next — <strong>and a reader that remembered only "the class I am
    /// in" would have called the subclass's own method again</strong>, which
    /// is a game going for ever on one line.
    /// </remarks>
    private readonly List<string> _aufrufKette = new();

    /// <summary>
    /// The blocks wrapping the calls now running, outermost first.
    /// </summary>
    /// <remarks>
    /// **A stack and not one value**, because a block can be inside a block:
    /// `a.each { b.each { ... } }` has two. **A reader that kept one would
    /// have let the inner block's parameters bind to the outer block's
    /// values**, and a game's nested loop would have used the wrong element.
    /// </remarks>
    private readonly List<RubyNode> _blockKette = new();

    /// <summary>
    /// How far down the stack a method call may see.
    /// </summary>
    /// <remarks>
    /// <strong>A stack and a scope chain are not the same thing.</strong> A
    /// block sees the locals around it; a method does not, because a method
    /// has its own frame from the moment it is called. <strong>Without this
    /// number, every local a caller had would be visible inside every method
    /// it calls</strong> — and a game would have a method whose value depends
    /// on who called it.
    /// </remarks>
    private int _methodenGrenze;

    /// <summary>
    /// The classes and modules the script defined, by name.
    /// </summary>
    /// <remarks>
    /// <strong>This is the interpreter's own and not the host's.</strong> A
    /// class a script writes is a class in that script, and <strong>a host
    /// that supplied it would have to be told about every class a game
    /// defines</strong>, which is the whole program and not a host contract.
    /// </remarks>
    private readonly Dictionary<string, RubyType> _types = new(StringComparer.Ordinal);

    /// <summary>The names of the types the script defined, in order.</summary>
    public IReadOnlyList<string> DefinedTypes => _types.Keys.ToList();

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
        _aktuellerTyp = null;
        _methodenGrenze = 0;
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
            // **Ein Name ist erst eine Variable und dann ein Aufruf.** Ruby
            // entscheidet das zur Laufzeit, **und der Parser kann es nicht**:
            // er sieht `hp` und weiss nicht, ob die Klasse eine Methode
            // `hp` hat. **Also entscheidet es der Interpreter, und die
            // Reihenfolge ist die von Ruby:** erst die lokale Variable,
            // und wenn es keine gibt, die Methode. **Ein Leser, der immer
            // die Variable zuerst nähme, hätte ein Spiel, das `@hp`
            // gleichzeitig als Feld und als Methode benutzt, mit dem
            // Feld gewinnen lassen** — und eins, das nur die Methode
            // meint, mit der Methode.
            RubyNodeKind.Identifier => Name(pNode),
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
            RubyNodeKind.Class => DefineType(pNode, true),
            RubyNodeKind.Module => DefineType(pNode, false),
            RubyNodeKind.Def => DefineMethod(pNode, false),
            RubyNodeKind.DefS => DefineMethod(pNode, true),
            RubyNodeKind.Alias => DefineAlias(pNode),
            RubyNodeKind.Defined => EvaluateDefined(pNode),
            RubyNodeKind.SuperCall => EvaluateSuper(pNode),
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

        // **Erst die Skript-Tabelle, dann der Host.** `A.rechnung` schreibt
        // `A` als Empfenger, und **wenn eine Konstante nur der Host kennt,
        // ist sie fuer den Interpreter nil** -- **jede Methode einer Klasse,
        // die das Skript selbst definiert, waere damit unerreichbar**, und
        // ein Spiel, das Klassen in eigenen Dateien schreibt, wuerde gar
        // nichts ausfuehren.
        if (_types.TryGetValue(name, out var typ))
        {
            return RubyValue.OfSymbol(typ.Name);
        }

        var wert = _host.LookupConstant(name);
        if (wert != null)
        {
            return wert;
        }

        // **Der Name bleibt fuer den Aufrufer sichtbar.** `include Fehlt`
        // braucht ihn, um sagen zu koennen, welches Modul fehlt -- **und
        // eine Diagnose, die den Namen nicht nennt, laesst den Leser
        // raten.**
        _letzteUnbekannteKonstante = name;
        _diagnostics.Add(
            $"the constant {name} is not defined by this host, and the "
            + "interpreter does not guess; a game's own constant needs a host "
            + "that provides it");
        return RubyValue.Nil;
    }

    private RubyValue Local(string pName)
    {
        // **Nur bis zur uebersten Methodengrenze.** Eine Methode sieht ihre
        // eigenen Variablen und die der Klammern darueber, **nicht die des
        // Aufrufers** -- und das ist der Unterschied zwischen einem Bereich
        // und einem Stapel. **Ein Leser, der alle Ebenen durchsuchte,
        // wuerde in einer Methode die Variable ihres Aufrufers sehen**, und
        // ein Spiel, das in einer Methode `x = 1` schreibt und es danach
        // liest, wuerde den Wert des Aufrufers bekommen.
        for (var i = _scopes.Count - 1; i >= _methodenGrenze; i--)
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

        // **Drei eingebaute Namen, und sie sind eingebaut, weil sie keine
        // Skriptmethode sind.** `attr_accessor`, `attr_reader` und
        // `attr_writer` erzeugen Methoden, **und diese Methoden gehoeren zu
        // der Klasse, in der sie geschrieben wurden** -- **ein eingebauter
        // Aufruf, der die Klasse nicht kennt, kann sie nicht erzeugen, und
        // ein Leser, der sie als "diese Methode kennt der Host nicht"
        // ablehnte, wuerde jedes RPG-Maker-Skript an seiner zweiten Zeile
        // anhalten.** `include` gehoert in dieselbe Reihe, und es ist
        // keine Methode, sondern ein Satz ueber die Klasse.
        // **Die vier Namen, ob mit oder ohne Klammern.** `attr_accessor`
        // ohne ein einziges Argument ist ein Aufruf mit null Argumenten und
        // **nicht das Fehlen eines Aufrufs** -- **und ein Leser, der nur
        // die Form mit Klammern erkannte, wuerde ein alleinstehendes
        // `attr_accessor` fuer eine Variable halten** und die Zeile als
        // Zuweisung lesen.
        if (_aktuellerTyp != null
            && (methode == "attr_accessor"
                || methode == "attr_reader"
                || methode == "attr_writer"
                || methode == "include"
                || methode == "extend"))
        {
            if (Eingebaut(_aktuellerTyp, methode, argumente))
            {
                return RubyValue.OfSymbol(methode);
            }
        }

        // **Erst die Skript-Methodentabelle, dann der Host.** Ein Aufruf,
        // den das Skript selbst definiert hat, gehoert dem Skript -- **und
        // ein Leser, der immer zum Host ginge, wuerde jedes `def` eines
        // Spiels als "diese Methode kennt der Host nicht" ablehnen**, also
        // waere kein Spiel lauffaehig.
        var eigene = EigeneMethode(empfaenger, methode);
        if (eigene != null)
        {
            // **Der Name des Empfaengers wandert mit** -- **und genau der
            // fehlt `super`**: ohne ihn wuesste der Aufruf nicht, aus
            // welcher Klasse er kam, und `super` haette nichts, wovon es
            // eine Ebene hoeher ginge.
            return Aufrufen(
                eigene,
                argumente,
                empfaenger.Kind == RubyValueKind.Symbol && empfaenger.Name != "self"
                    ? empfaenger.Name
                    : _aktuellerTyp?.Name);
        }

        // **Ein Block am Aufruf geht an den Host als Rueckruf.** Der Host
        // entscheidet, wie oft und womit, **und der Interpreter bindet die
        // Parameter des Blocks, wenn der Host zurueckruft** -- es gibt
        // keine Objekte und keine Closures, **und die kleinste ehrliche
        // Form eines Blocks auf diesem Weg ist ein Aufruf, den der Host
        // macht**. `Array#each`, `Integer#times` und `String#each_line`
        // sind die drei, die ein XP-Skript am haeufigsten schreibt, und
        // alle drei kommen ueber genau diesen Weg.
        var block = _blockKette.Count > 0 ? _blockKette[^1] : null;
        if (block != null)
        {
            var mitBlock = _host.CallMethodWithBlock(
                empfaenger, methode, argumente, werte => Yield(block, werte));
            if (mitBlock != null)
            {
                return mitBlock;
            }
        }

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

    /// <summary>
    /// The script's own method for a receiver, or null.
    /// </summary>
    /// <param name="pReceiver">The receiver, which may carry a class name.</param>
    /// <param name="pMethod">The method's name.</param>
    /// <returns>The method, or null when the script has none.</returns>
    /// <remarks>
    /// <strong>A class name as receiver means the class itself</strong>, and
    /// that is how `self.` and a bare class call both arrive here — <strong>a
    /// reader that only looked at instances would have made every class
    /// method unreachable.</strong>
    /// </remarks>
    private RubyMethod? EigeneMethode(RubyValue pReceiver, string pMethod)
    {
        // **`self` heisst die Klasse, in der gerade laeuft.** Ein
        // Aufruf ohne ausgeschriebenen Empfaenger hat keinen, **und
        // `EigeneMethode` bekam deshalb immer `self` und fand nie etwas.**
        // Das war keine Besonderheit von `attr_accessor`: **jeder
        // klammerlose Aufruf eines Spiels hat denselben Weg genommen und
        // dieselbe stille Antwort bekommen** -- und die Tabelle, in der
        // `hp` stand, wurde nie befragt.
        var name = pReceiver.Kind == RubyValueKind.Symbol
            && pReceiver.Name != "self"
            ? pReceiver.Name
            : _aktuellerTyp?.Name;

        if (name == null || name.Length == 0 || !_types.ContainsKey(name))
        {
            return null;
        }

        // **Die Klassenmethode zuerst.** `self.` ist der Schluessel, unter
        // dem sie abgelegt ist, **und ohne diesen Schritt waere
        // `Klasse.selbst_definiert` nicht erreichbar** -- waehrend
        // `Klasse.instanz_definiert` es waere.
        // **Zuerst die Klassenmethode, dann die Kette.** `self.` ist der
        // Schluessel, unter dem sie abgelegt ist, und **ohne diesen Schritt
        // waere `Klasse.selbst_definiert` nicht erreichbar** -- waehrend
        // `Klasse.instanz_definiert` es waere.
        if (_types[name].Methods.TryGetValue("self." + pMethod, out var aufSelbst))
        {
            return aufSelbst;
        }

        return FindMethod(name, pMethod);
    }

    /// <summary>
    /// Runs a method with the given arguments bound to its parameters.
    /// </summary>
    /// <param name="pMethode">The method to run.</param>
    /// <param name="pArgumente">The arguments, already evaluated.</param>
    /// <returns>The value of the last statement, or nil.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the method's own body runs in a fresh scope</strong>, so a
    /// local of the caller is not visible inside it — <strong>and a reader
    /// that shared the scope would let a method change its caller's
    /// variables</strong>, which is exactly the kind of bug a game does not
    /// report because it only shows up in one room.
    /// </para>
    /// <para>
    /// <strong>A parameter the call did not supply is nil</strong>, because
    /// that is Ruby's own rule and a game's optional parameter is written by
    /// leaving it out.
    /// </para>
    /// </remarks>
    private RubyValue Aufrufen(
        RubyMethod pMethode,
        IReadOnlyList<RubyValue> pArgumente,
        string? pKlasse)
    {
        var tiefe = _scopes.Count;
        var grenze = _methodenGrenze;
        _methodenGrenze = tiefe;
        _aufrufKette.Add(pKlasse ?? string.Empty);
        var alteArgumente = _aktuelleArgumente;
        var alteMethode = _aktuelleMethode;
        _aktuelleArgumente = pArgumente;
        _aktuelleMethode = pMethode.Name;

        // **Und die Klasse, in der die Methode geschrieben wurde, ist
        // "self" fuer ihren Rumpf.** `_aktuellerTyp` gilt nur fuer den
        // Klassenrumpf -- **und ohne diesen Satz war er beim Aufruf einer
        // Methode null**, also war `self.hp = 42` ein Aufruf an niemanden,
        // und `super` fand die Klasse nicht. **Das war derselbe Fehler
        // zweimal**: einmal fuer die Methodensuche und einmal fuer den
        // Rumpf.
        var laufenderTyp = _aktuellerTyp;
        var typWar = pKlasse == null
            ? null
            : _types.TryGetValue(pKlasse, out var gefunden) ? gefunden : null;
        _aktuellerTyp = typWar;

        _scopes.Add(new Dictionary<string, RubyValue>());
        for (var i = 0; i < pMethode.Parameters.Count; i++)
        {
            SetLocal(
                pMethode.Parameters[i],
                i < pArgumente.Count ? pArgumente[i] : RubyValue.Nil);
        }

        _returned = false;

        // **Ein Attribut hat keinen Rumpf, und es braucht auch keinen.**
        // `attr_accessor :hp` erzeugt einen Leser und einen Schreiber fuer
        // `@hp` -- **und der Leser liest das Feld und der Schreiber schreibt
        // es**, statt einen Knoten zu haben, den man laufen lassen koennte.
        var wert = pMethode.IsAttribute
            ? Attribut(pMethode, pArgumente)
            : Evaluate(pMethode.Body);
        _returned = false;
        _scopes.RemoveRange(tiefe, _scopes.Count - tiefe);
        _methodenGrenze = grenze;
        _aufrufKette.RemoveAt(_aufrufKette.Count - 1);
        _aktuelleArgumente = alteArgumente;
        _aktuelleMethode = alteMethode;
        _aktuellerTyp = laufenderTyp;
        return wert;
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
            case RubyNodeKind.GlobalVariable:
                // **Ein Global wird auch ausserhalb jeder Ebene
                // geschrieben.** Der `_scopes.Count > 0`-Test, den das
                // Instanzfeld braucht, waere hier falsch: **ein Spiel, das
                // beim Laden ein Globales setzt, hat keine offene Ebene**,
                // und die Zuweisung waere ins Leere gegangen.
                _globals[GlobalName(ziel)] = wert;
                return wert;
            case RubyNodeKind.Call:
            case RubyNodeKind.SelfCall:
            case RubyNodeKind.MethodCall:
                // **Ein Aufruf auf der linken Seite ist ein Schreibaufruf,
                // und sein Name ist der Name des Aufrufs plus ein
                // `=`.** Ruby wertet `self.hp = 42` als `hp=(42)` aus --
                // **und der Empfänger des Aufrufs ist nicht das Ziel, er ist
                // sein Empfaenger.** Der erste Versuch gab den Zielknoten
                // selbst als Empfaenger weiter, **und `self.hp` ist kein
                // Wert: es ist ein Knoten**, den `EigeneMethode` nicht
                // entpackt. Die Zuweisung lief ins Leere und der Leser
                // sah nil.
                return Call(new RubyNode
                {
                    Kind = RubyNodeKind.Call,
                    Name = ziel.Name + "=",
                    Line = pNode.Line,
                    Role_Children =
                    [
                        new RubyNodePart
                        {
                            Role = RubyNodeRole.Receiver,
                            Node = Child(ziel, RubyNodeRole.Receiver),
                        },
                        new RubyNodePart
                        {
                            Role = RubyNodeRole.Argument,
                            Node = Child(pNode, RubyNodeRole.Value),
                        },
                    ],
                });

            default:
                _diagnostics.Add(
                    $"{ziel.Kind} is on the left of an = and there is nowhere "
                    + "to put the value; the reference would raise, and this "
                    + "reader does not guess a place");
                return wert;
        }
    }

    private void SetLocal(string pName, RubyValue pValue)
    {
        if (_scopes.Count == 0)
        {
            _scopes.Add(new Dictionary<string, RubyValue>());
        }

        for (var i = _scopes.Count - 1; i >= _methodenGrenze; i--)
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
        // **Ein Block an einem Aufruf ist kein eigener Anweisungstyp, sondern
        // ein Mantel um den Aufruf.** `a.each do |x| ... end` ist ein Block,
        // dessen erstes Kind der Aufruf `a.each` ist -- **und ohne diesen
        // Schritt ginge der Aufruf nie zum Host**, es kaeme nie eine Liste
        // heraus, und der Rumpf wuerde nie laufen. **Das ist die Form, die
        // ein Spiel am haeufigsten schreibt**, und sie brauchte eine eigene
        // Abkuerzung, die es vorher nicht gab.
        if (pNode.Kind == RubyNodeKind.Block
            && pNode.Children.Count >= 3
            && pNode.Children[0].Kind is RubyNodeKind.Call
                or RubyNodeKind.MethodCall)
        {
            _blockKette.Add(pNode);
            try
            {
                return Evaluate(pNode.Children[0]);
            }
            finally
            {
                _blockKette.RemoveAt(_blockKette.Count - 1);
            }
        }

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

    /// <summary>
    /// Runs a `class` or `module` body and remembers the type.
    /// </summary>
    /// <param name="pNode">The definition node.</param>
    /// <param name="pIsClass">Whether this is a class and not a module.</param>
    /// <remarks>
    /// <para>
    /// <strong>The body runs once, when the definition is reached</strong> —
    /// and that is what puts the methods in the table. <strong>A second
    /// definition of the same name replaces the first's methods</strong>,
    /// because that is what a reopened class does, and a reader that merged
    /// them would have kept a method a game's later file meant to remove.
    /// </para>
    /// <para>
    /// <strong>And the outer type stands again afterwards</strong> — a class
    /// inside a class is a nested constant, and <strong>a reader that left
    /// the inner one standing would have filed the outer class's later
    /// methods under the inner one.</strong>
    /// </para>
    /// </remarks>
    private RubyValue DefineType(RubyNode pNode, bool pIsClass)
    {
        var name = pNode.Name ?? string.Empty;
        if (name.Length == 0)
        {
            _diagnostics.Add(
                "a class or module with no name was defined, and there is "
                    + "nothing to file it under");
            return RubyValue.Nil;
        }

        // **Eine zweite Definition ersetzt die Methoden** -- und die Typ-Art
        // bleibt, weil ein `class` nach einem `class` dieselbe Art ist.
        var typ = _types.TryGetValue(name, out var vorhanden)
            ? vorhanden
            : new RubyType
            {
                Name = name,
                IsClass = pIsClass,
                Superclass = pNode.Superclass,
            };
        typ.Methods.Clear();
        _types[name] = typ;

        var tiefe = _scopes.Count;
        var vorher = _aktuellerTyp;
        _aktuellerTyp = typ;
        _scopes.Add(new Dictionary<string, RubyValue>());
        foreach (var teil in Statements(pNode))
        {
            Evaluate(teil);
        }

        _aktuellerTyp = vorher;
        _scopes.RemoveRange(tiefe, _scopes.Count - tiefe);
        return RubyValue.OfSymbol(name);
    }

    /// <summary>
    /// Runs a `def` body once and remembers the method.
    /// </summary>
    /// <param name="pNode">The definition node.</param>
    /// <remarks>
    /// <para>
    /// <strong>The body is run once, with the parameters bound to
    /// nil.</strong> That is what Ruby does when a method is defined — the
    /// default arguments are expressions, evaluated in the definition's own
    /// scope — <strong>and it is why a method whose name is a variable's
    /// value is a thing this reader cannot know.</strong>
    /// </para>
    /// <para>
    /// <strong>And a `def` outside a class is a diagnostic, not a
    /// method.</strong> Ruby would define it on `Object`; <strong>this
    /// interpreter files methods under a class, and a host that wants
    /// `Object` says so</strong> rather than this reader inventing a root.
    /// </para>
    /// </remarks>
    private RubyValue DefineMethod(RubyNode pNode, bool pAufSelbst)
    {
        var name = pNode.Name ?? string.Empty;
        var typ = _aktuellerTyp;
        if (typ == null)
        {
            _diagnostics.Add(
                $"method {name} is defined outside a class, and this "
                + "interpreter files methods under a class; the reference "
                + "would define it on Object, and that is a host's job");
            return RubyValue.Nil;
        }

        var parameter = pNode.Children.Count > 0 ? pNode.Children[0] : null;
        var rumpf = pNode.Children.Count > 1 ? pNode.Children[1] : null;
        var namen = new List<string>();
        if (parameter != null)
        {
            // **Die Parameterliste ist ein Block, und ihre Kinder sind die
            // Namen.** `Statements` nimmt die Rolle `Body` und sonst die
            // Quellordnung -- **und fuer diesen Knoten ist die
            // Quellordnung richtig**, weil es keine Rollen gibt.
            foreach (var teil in Statements(parameter))
            {
                namen.Add(teil.Name ?? string.Empty);
            }
        }

        typ.Methods[(pAufSelbst ? "self." : string.Empty) + name] =
            new RubyMethod
            {
                Name = name,
                IsOnSelf = pAufSelbst,
                Parameters = namen,
                Body = rumpf!,
            };

        // **Der Rumpf laeuft nicht.** Ruby fuehrt ihn bei der Definition
        // aus, weil die Defaultargumente Ausdruecke sind -- **und dieser
        // Parser liefert fuer die Parameter eine Liste von Namen und keine
        // Ausdruecke**, es gibt also nichts zu berechnen. **Ein Leser, der
        // den Rumpf hier laufen liesse, wuerde jede Methode eines Spiels
        // ausfuehren, sobald ihre Klasse geoeffnet wird** -- und eine Methode,
        // die ein Feld anfasst, wuerde an einer Klasse scheitern, die noch
        // keine Instanz hat.
        return RubyValue.OfSymbol(name);
    }

    /// <summary>
    /// Finds a method, walking the superclass chain.
    /// </summary>
    /// <param name="pTypeName">The class to look in.</param>
    /// <param name="pMethod">The method's name.</param>
    /// <returns>The method, or null when no class in the chain has it.</returns>
    /// <remarks>
    /// <strong>The chain is walked, and not flattened at definition
    /// time.</strong> A subclass may be written before its superclass in a
    /// script that loads pieces in a different order, and <strong>a reader
    /// that copied the methods at definition would have an incomplete
    /// subclass</strong> and would never learn about the rest.
    /// </remarks>
    public RubyMethod? FindMethod(string pTypeName, string pMethod)
    {
        var gesehen = new HashSet<string>(StringComparer.Ordinal);
        var name = pTypeName;
        while (name != null && _types.TryGetValue(name, out var typ) && gesehen.Add(name))
        {
            // **Die Basismethode, und nicht die Klassenmethode der
            // Basis.** `super` aus einer Instanzmethode laeuft zur
            // Instanzmethode -- **und ein Leser, der auch hier die
            // Klassenmethode zuerst probierte, wuerde eine Instanzmethode
            // auf der Basis ausfuehren**, die es dort gar nicht gibt.
            if (typ.Methods.TryGetValue(pMethod, out var methode))
            {
                return methode;
            }

            name = typ.Superclass;
        }

        return null;
    }


    /// <summary>
    /// The arguments of the running method, for a bracketless `super`.
    /// </summary>
    private IReadOnlyList<RubyValue> _aktuelleArgumente = [];

    /// <summary>The name of the method that is running, for `super`.</summary>
    private string _aktuelleMethode = string.Empty;




    /// <summary>
    /// Runs the same method one level up the superclass chain.
    /// </summary>
    /// <param name="pNode">The `super` node.</param>
    /// <returns>What the base's method returned, or nil.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Two forms and they are not the same call.</strong> <c>super</c>
    /// without brackets hands the arguments on, <c>super(x)</c> hands the
    /// written ones — and <strong>a reader that treated both alike would have
    /// called the base with the subclass's arguments</strong>, so an
    /// override that changes what the base receives would not change anything.
    /// </para>
    /// <para>
    /// <strong>The chain is the call stack and not the class we are
    /// in.</strong> <c>super</c> inside a method that the base called finds
    /// the base's base — <strong>and a reader that remembered only the current
    /// class would have called the subclass's own method again</strong>,
    /// which is a game going for ever on one line.
    /// </para>
    /// <para>
    /// <strong>A `super` with no base is a diagnostic</strong>, because Ruby
    /// would raise, and a game whose script has a broken override is broken;
    /// the message says which thing is missing.
    /// </para>
    /// </remarks>
    private RubyValue EvaluateSuper(RubyNode pNode)
    {
        // **Die Argumente der aktuellen Methode**, wenn `super` ohne
        // Klammern dasteht. Sie stehen in diesem Rahmen, weil `Aufrufen`
        // sie gebunden hat.
        // **`super(x)` uebergibt die geschriebenen Argumente, und die
        // muessen ausgewertet werden, bevor sie weitergegeben werden.** Ein
        // Knoten, den niemand auswertet, ist eine Liste von Ausdruecken und
        // kein Wert -- **und `super(a * 2)` wuerde die Basis mit einem
        // Knoten statt mit zweiundvierzig aufrufen.**
        var argumente = pNode.Name == "mit"
            ? pNode.Children.Select(Evaluate).ToList()
            : CurrentArguments(false);

        // **Die eigene Klasse geht zuerst weg** -- `super` heisst "eine
        // Ebene hoeher", nicht "in meiner Klasse".
        if (_aufrufKette.Count == 0)
        {
            _diagnostics.Add(
                "super was called outside a method, and there is no class to "
                    + "go up to");
            return RubyValue.Nil;
        }

        var eigene = _aufrufKette[^1];
        if (!_types.TryGetValue(eigene, out var typ) || typ.Superclass == null)
        {
            _diagnostics.Add(
                $"{typ?.Name ?? eigene} has no superclass, so super has "
                    + "nowhere to go; the reference would raise here");
            return RubyValue.Nil;
        }

        // **`_aktuelleMethode` ist schon der nackte Name.** Er kommt aus
        // `RubyMethod.Name`, und dort steht der Name, den das Skript
        // geschrieben hat -- **der `self.`-Praefix ist ein Schluessel im
        // Speicher und kein Teil des Namens.** Die erste Fassage hatte
        // hier noch einen Abzweig, der ihn abschnitt; **die Mutation, die
        // ihn entfernt, lebt, weil es nichts abzuschneiden gab.**
        var methode = FindMethod(typ.Superclass, _aktuelleMethode);
        if (methode == null)
        {
            _diagnostics.Add(
                $"{typ.Superclass} does not have {_aktuelleMethode}, so "
                    + "super has no method to call");
            return RubyValue.Nil;
        }

        return Aufrufen(methode, argumente, typ.Superclass);
    }

    /// <summary>
    /// The arguments the running method was called with.
    /// </summary>
    /// <param name="pGeschrieben">Whether `super` was written with brackets.</param>
    /// <returns>The arguments, already evaluated.</returns>
    /// <remarks>
    /// <strong>`super` without brackets hands the caller's arguments
    /// on</strong>, and that is the whole difference between the two forms.
    /// The values are remembered per frame — <strong>and a reader that read
    /// them from the current scope would have found the locals of the method
    /// body</strong>, which is not what a caller passed.
    /// </remarks>
    private IReadOnlyList<RubyValue> CurrentArguments(bool pGeschrieben)
    {
        if (pGeschrieben)
        {
            return [];
        }

        return _aktuelleArgumente;
    }


    /// <summary>
    /// The four built-ins that act on the class they are written in.
    /// </summary>
    /// <param name="pTyp">The class the call stands in.</param>
    /// <param name="pMethode">The built-in's name.</param>
    /// <param name="pArgumente">The arguments, already evaluated.</param>
    /// <returns>true when it was one of the four.</returns>
    /// <remarks>
    /// <para>
    /// <strong>These four are not script methods and they are not host
    /// methods.</strong> They write into the class they stand in, and the
    /// caller is the only place that knows which class that is — which is
    /// why a host cannot implement them.
    /// </para>
    /// <para>
    /// <strong>`attr_accessor` writes a reader and a writer, `attr_reader`
    /// only the reader, `attr_writer` only the writer.</strong> A game writes
    /// `attr_accessor :hp` and then assigns and reads `@hp` in a dozen
    /// methods, **and a reader that made only a reader would have a game that
    /// raises on its first assignment.**
    /// </para>
    /// </para>
    /// <para>
    /// <strong>The values live in the class and not in an instance.</strong>
    /// This interpreter has no objects, so an instance variable has nowhere
    /// else to go — **and a reader that invented an object model here would
    /// have had a class whose attributes were shared between every thing
    /// made from it**, which is a different language. What this gives is the
    /// attribute's own storage, and a game that reads it back gets its own
    /// last value. That is stated here because it is a limit and not a
    /// detail.
    /// </para>
    /// </remarks>
    private bool Eingebaut(RubyType pTyp, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pMethode is "include" or "extend")
        {
            return Eingemischt(pTyp, pMethode, pArgumente);
        }

        return Attribute(pTyp, pMethode, pArgumente);
    }

    /// <summary>
    /// Writes the reader and the writer for a list of attribute names.
    /// </summary>
    /// <returns>true when the names were all symbols.</returns>
    /// <remarks>
    /// <strong>A name that is not a symbol is refused and said so.</strong>
    /// `attr_accessor "hp"` is Ruby and it uses the string as the name, and
    /// this parser gives a bare word no type — <strong>so a reader that
    /// accepted anything would have created a method named after whatever
    /// the game wrote</strong>, including a number.
    /// </remarks>
    private bool Attribute(
        RubyType pTyp,
        string pMethode,
        IReadOnlyList<RubyValue> pArgumente)
    {
        // **Null Namen ist kein Fehler.** Ruby macht dann keine Methode
        // und sagt nichts -- **es ist ein Aufruf mit leerer Argumentliste,
        // und das ist erlaubt.** Die erste Fassage meldete das, **und diese
        // Meldung waere eine Regel, die es in Ruby nicht gibt**; ein Spiel,
        // das `attr_accessor` bedinghaft leer aufruft, haette an einer Zeile
        // gestoppt, die der Referenz durchlaeuft.
        if (pArgumente.Count == 0)
        {
            return true;
        }

        var lesen = pMethode is not "attr_writer";
        var schreiben = pMethode is not "attr_reader";
        foreach (var arg in pArgumente)
        {
            if (arg.Kind != RubyValueKind.Symbol)
            {
                _diagnostics.Add(
                    $"attr_{pMethode} takes names as symbols, and the "
                    + "argument is not one; a reader that took anything would "
                    + "have made a method out of whatever the game wrote");
                return false;
            }

            var feld = "@" + (arg.Name ?? string.Empty);
            if (lesen)
            {
                pTyp.Methods[arg.Name ?? string.Empty] = new RubyMethod
                {
                    Name = arg.Name ?? string.Empty,
                    IsAttribute = true,
                    Field = feld,
                };
            }

            if (schreiben)
            {
                pTyp.Methods[arg.Name + "="] = new RubyMethod
                {
                    Name = arg.Name + "=",
                    IsAttribute = true,
                    IsWriter = true,
                    Field = feld,
                };
            }
        }

        return true;
    }

    /// <summary>
    /// Puts a module's methods into a class, or into the class itself.
    /// </summary>
    /// <param name="pTyp">The class the call stands in.</param>
    /// <param name="pMethode">`include` or `extend`.</param>
    /// <param name="pArgumente">The modules, already evaluated.</param>
    /// <returns>true when every module was found.</returns>
    /// <remarks>
    /// <para>
    /// <strong>`extend` is `include` written with `self` in front.</strong>
    /// Ruby defines the methods as singleton methods of the object, and in a
    /// class body that object is the class — <strong>so a class method made by
    /// `extend` belongs to the class and not to the things made from
    /// it.</strong> This runtime has no objects, <strong>and a class method
    /// is the only place a module's methods can go under `extend`</strong>,
    /// because the alternative would be to give every instance of the class
    /// its own copy and this runtime does not have instances.
    /// </para>
    /// <para>
    /// <strong>So `extend` writes under the `self.` prefix</strong>, and
    /// `include` writes plainly, and the two do not collide: a class with
    /// both gets both, <strong>which is what a game's mixin class
    /// wants.</strong>
    /// </para>
    /// <para>
    /// <strong>And a module that is not there is a diagnostic and names
    /// itself.</strong> "this host does not implement it" would be the wrong
    /// sentence: a module is a type and the game wrote its name.
    /// </para>
    /// </remarks>
    private bool Eingemischt(
        RubyType pTyp,
        string pMethode,
        IReadOnlyList<RubyValue> pArgumente)
    {
        if (pArgumente.Count == 0)
        {
            _diagnostics.Add(
                $"{pMethode} was called with no module, and there is nothing "
                    + "to take methods from");
            return false;
        }

        var praefix = pMethode == "extend" ? "self." : string.Empty;
        foreach (var arg in pArgumente)
        {
            // **Eine Konstante, die es nicht gibt, ist immer noch ein Name.**
            var name = arg.Kind switch
            {
                RubyValueKind.Symbol => arg.Name,
                RubyValueKind.Nil => _letzteUnbekannteKonstante,
                _ => null,
            };
            if (name == null || !_types.TryGetValue(name, out var modul))
            {
                _diagnostics.Add(
                    $"{pMethode} names {name ?? "(nothing)"}, and this "
                    + "interpreter has no module under that name; a module is a "
                    + "type and not a method, so a message about the host "
                    + "would be the wrong sentence");
                return false;
            }

            foreach (var methode in modul.Methods)
            {
                // **Ein eingebautes Attribut wird nicht kopiert**, und
                // **die Basisklasse einer Klasse folgt nicht** -- beides
                // waere eine Kopie von etwas, das die Quelle nicht hatte.
                if (methode.Value.IsAttribute)
                {
                    continue;
                }

                // **Der Schluessel, den eine Klassenmethode schon traegt,
                // wird nicht doppelt gesetzt.** `def self.x` im Modul gibt
                // `self.x`, und `extend` setzt daraus kein `self.self.x` --
                // **sonst waere die Methode unter einem Namen erreichbar,
                // den das Spiel nie geschrieben hat.**
                pTyp.Methods[methode.Key.StartsWith("self.", StringComparison.Ordinal)
                    ? methode.Key
                    : praefix + methode.Key] = methode.Value;
            }
        }

        return true;
    }

    /// <summary>
    /// Reads or writes an attribute's value.
    /// </summary>
    /// <param name="pMethode">The reader or the writer.</param>
    /// <param name="pArgumente">The call's arguments.</param>
    /// <returns>The value for a reader, and the written one for a
    /// writer.</returns>
    /// <remarks>
    /// <para>
    /// <strong>A reader with no value yet answers nil, and does not
    /// raise.</strong> Ruby would raise <c>NameError</c> for an unset
    /// instance variable — <strong>and a game's <c>attr_accessor</c> is
    /// read before it is written more often than anyone expects</strong>,
    /// because <c>initialize</c> runs after the object exists. Failing here
    /// would stop a game on a field it is about to set.
    /// </para>
    /// <para>
    /// <strong>A writer with no argument writes nil.</strong> That is
    /// Ruby's own answer for `send(:hp=)`, and a reader that refused the call
    /// would have rejected a program the reference runs.
    /// </para>
    /// </remarks>
    private RubyValue Attribut(RubyMethod pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        var feld = pMethode.Field ?? string.Empty;

        if (pMethode.IsWriter)
        {
            var geschrieben = pArgumente.Count > 0 ? pArgumente[0] : RubyValue.Nil;
            _instanceVariables[feld] = geschrieben;
            return geschrieben;
        }

        // **Ein noch nie gesetztes Feld ist nil, und das ist hier kein
        // Fehler.** Ruby wuerde `NameError` sagen -- **und ein Spiel liest
        // sein `attr_accessor` haeufig, bevor es schreibt**, weil
        // `initialize` nach dem Objekt laeuft. **Und `_instanceVariables` ist
        // derselbe Speicher, den `@hp` direkt liest** -- **ein zweiter
        // Speicher je Klasse wuerde `attr_accessor` und `@hp` entkoppelt
        // haben**, und das Skript wuerde in einem Fall einen Wert sehen und
        // im anderen einen anderen.
        return _instanceVariables.TryGetValue(feld, out var gelesen)
            ? gelesen
            : RubyValue.Nil;
    }



    /// <summary>
    /// A bare name: the local variable if there is one, and the method if
    /// not.
    /// </summary>
    /// <param name="pNode">The identifier node.</param>
    /// <returns>Whatever the name means here.</returns>
    /// <remarks>
    /// <para>
    /// <strong>The local first, and that is Ruby's own order.</strong> A
    /// method's own parameter is a local, and a method that has a parameter
    /// called <c>hp</c> and also has an <c>attr_accessor :hp</c> reads the
    /// parameter — **and a reader that asked the class first would have read
    /// the attribute and taken the argument away from the body.**
    /// </para>
    /// <para>
    /// <strong>And this is a runtime decision, not a parse.</strong> The
    /// parser cannot know whether a class has a method of that name, and a
    /// parser that guessed would have turned every local named like a method
    /// into a call — <strong>and a game with a local <c>name</c> and a
    /// method <c>name</c> would have lost the local.</strong>
    /// </para>
    /// </remarks>
    private RubyValue Name(RubyNode pNode)
    {
        var name = pNode.Name ?? string.Empty;
        if (_scopes.Count > 0 && _scopes[^1].ContainsKey(name))
        {
            return Local(name);
        }

        if (_aktuellerTyp != null && FindMethod(_aktuellerTyp.Name, name) != null)
        {
            return Call(new RubyNode
            {
                Kind = RubyNodeKind.SelfCall,
                Name = name,
                Line = pNode.Line,
                Role_Children = [new RubyNodePart { Role = RubyNodeRole.Receiver, Node = SelfNode(pNode.Line) }],
            });
        }

        return Local(name);
    }

    /// <summary>
    /// The `self` node a bare call hangs its receiver from.
    /// </summary>
    private static RubyNode SelfNode(int pLine) => new()
    {
        Kind = RubyNodeKind.Self,
        Line = pLine,
    };

    /// <summary>
    /// Files an existing method under a second name.
    /// </summary>
    /// <param name="pNode">The alias node.</param>
    /// <returns>The new name as a symbol.</returns>
    /// <remarks>
    /// <para>
    /// <strong>The method is copied and not the name.</strong> An alias is
    /// a second door into the same room: change the body and both doors
    /// open onto the change. <strong>A reader that stored a second name and
    /// looked the body up under it would have given the game's override
    /// nothing to override</strong> — `super` from the new name would have
    /// gone to the old one and the game's replacement would have been lost.
    /// </para>
    /// <para>
    /// <strong>And it resolves the chain.</strong> `alias b a` where `a` is
    /// itself an alias has to find the method behind both — <strong>and a
    /// reader that only looked in the class's own table would have failed on
    /// the second alias</strong>, which is a thing games write when they
    /// rename something twice.
    /// </para>
    /// <para>
    /// <strong>An alias outside a class is a diagnostic and names both
    /// names.</strong> Ruby would put it on `Object`; this interpreter files
    /// methods under a class, so it says which thing would have to provide
    /// that.
    /// </para>
    /// </remarks>
    private RubyValue DefineAlias(RubyNode pNode)
    {
        var neuer = pNode.Name ?? string.Empty;
        var alter = pNode.Operator ?? string.Empty;
        var typ = _aktuellerTyp;
        if (typ == null)
        {
            _diagnostics.Add(
                $"alias {neuer} {alter} stands outside a class, and this "
                + "interpreter files methods under a class; the reference "
                + "would put it on Object, and that is a host's job");
            return RubyValue.Nil;
        }

        var methode = FindMethod(typ.Name, alter);
        if (methode == null)
        {
            _diagnostics.Add(
                $"{typ.Name} does not have {alter}, so alias {neuer} has "
                    + "nothing to point at; an alias is a second name for an "
                    + "existing method and not a new one");
            return RubyValue.Nil;
        }

        typ.Methods[neuer] = methode;
        return RubyValue.OfSymbol(neuer);
    }



    /// <summary>
    /// Answers what an expression is, without running it.
    /// </summary>
    /// <param name="pNode">The `defined?` node.</param>
    /// <returns>The kind as a symbol, or nil for "not defined".</returns>
    /// <remarks>
    /// <para>
    /// <strong>It answers a question and it does not ask it by doing
    /// it.</strong> Ruby's <c>is_defined</c> in <c>eval.c</c> walks the node's
    /// kind and never evaluates the expression — <strong>and a reader that
    /// evaluated it would have said "method" for <c>defined? a.b</c> even when
    /// <c>b</c> is not there</strong>, and would have made the call the
    /// question only asked about.
    /// </para>
    /// <para>
    /// <strong>And it answers a string and not a boolean.</strong> That is
    /// Ruby 1.8.1, the version RPG Maker XP runs — <strong>and a reader that
    /// answered true or false would have broken every game that writes
    /// <c>defined?(@hp) ? "expression" : "nil"</c></strong>, which is how a
    /// game's own code asks. Current Ruby answers a boolean, and **this
    /// runtime is the 1.8 one because that is the one being
    /// emulated.**
    /// </para>
    /// <para>
    /// <strong>What is named here is what this runtime has.</strong> A local
    /// variable, an instance variable, a global, a class, a method of the
    /// running class, a constant the script or the host defines, and
    /// <c>yield</c> and <c>super</c> which this runtime has no frame
    /// bookkeeping for — <strong>and a reader that guessed at those two
    /// would have claimed a method exists for a block this runtime does
    /// not run.</strong>
    /// </para>
    /// </remarks>
    private RubyValue EvaluateDefined(RubyNode pNode)
    {
        var kind = Child(pNode, RubyNodeRole.Condition).Kind;
        switch (kind)
        {
            case RubyNodeKind.Identifier:
                return Defined(
                    Local(NameOf(pNode)).IsNil
                        && !HasLocal(NameOf(pNode)) ? null : "local-variable");
            case RubyNodeKind.InstanceVariable:
            case RubyNodeKind.ClassVariable:
                return Defined(
                    _instanceVariables.ContainsKey(NameOf(pNode))
                        ? "instance-variable" : null);
            case RubyNodeKind.GlobalVariable:
                return Defined(_globals.ContainsKey(NameOf(pNode)) ? "global-variable" : null);
            case RubyNodeKind.Constant:
                return Defined(
                    _types.ContainsKey(NameOf(pNode)) || _host.LookupConstant(NameOf(pNode)) != null
                        ? "constant" : null);
            case RubyNodeKind.Call:
            case RubyNodeKind.MethodCall:
            case RubyNodeKind.SelfCall:
                return Defined(
                    EigeneMethode(empfaengerOf(pNode), NameOf(pNode)) != null
                        ? "method" : null);
            case RubyNodeKind.SuperCall:
                return Defined(
                    _aufrufKette.Count > 0 && BasisVon(_aufrufKette[^1]) != null
                        ? "super" : null);
            case RubyNodeKind.Yield:
                return Defined(null);
            default:
                // **Jeder andere Ausdruck ist ein Ausdruck**, und das ist
                // Rubys eigener Default: `is_defined` gibt "expression"
                // fuer alles, was es nicht benennen kann.
                return Defined("expression");
        }
    }

    /// <summary>
    /// The name the `defined?` question is about, without a dollar sign.
    /// </summary>
    /// <remarks>
    /// <strong>One place strips the dollar and both sides call
    /// it.</strong> The lexer writes a global's name as written, with the
    /// `<c>$</c>` in it, <strong>and the table is keyed without it</strong>.
    /// The first version stripped it at the assignment and not at the
    /// question, <strong>and a global that was set answered nil to
    /// <c>defined?</c></strong> — which is the one use of the word a game
    /// relies on. **Two places for one rule is two places where the rule
    /// drifts.**
    /// </remarks>
    private static string NameOf(RubyNode pNode)
        => GlobalName(Child(pNode, RubyNodeRole.Condition));

    /// <summary>A global's name as the table keys it.</summary>
    private static string GlobalName(RubyNode pNode)
        => (pNode.Name ?? string.Empty).TrimStart('$');

    /// <summary>The receiver a call in a `defined?` question has.</summary>
    private RubyValue empfaengerOf(RubyNode pNode)
    {
        var ziel = Child(pNode, RubyNodeRole.Condition);
        if (ziel.Kind == RubyNodeKind.Call || ziel.Kind == RubyNodeKind.MethodCall)
        {
            return Evaluate(Child(ziel, RubyNodeRole.Receiver));
        }

        return RubyValue.OfSymbol("self");
    }

    /// <summary>The base of a class, or null when it has none.</summary>
    private string? BasisVon(string pTyp)
        => _types.TryGetValue(pTyp, out var typ) ? typ.Superclass : null;

    /// <summary>A local in any scope up to the method boundary.</summary>
    private bool HasLocal(string pName)
    {
        for (var i = _scopes.Count - 1; i >= _methodenGrenze; i--)
        {
            if (_scopes[i].ContainsKey(pName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A kind as a symbol, or nil.</summary>
    private static RubyValue Defined(string? pKind)
        => pKind == null ? RubyValue.Nil : RubyValue.OfSymbol(pKind);



    /// <summary>
    /// Runs a block once with the values the host handed it.
    /// </summary>
    /// <param name="pBlock">The block node.</param>
    /// <param name="pWerte">The values for this round.</param>
    /// <returns>What the block's last statement returned.</returns>
    /// <remarks>
    /// <para>
    /// <strong>The parameters are bound in a new level and not over the
    /// caller's.</strong> A block sees the locals around it, <strong>and
    /// this runtime gives it its own level on top of them** — so a block's
    /// parameter shadows an outer variable of the same name inside the
    /// block and not outside it, which is the difference between a block and
    /// a method.
    /// </para>
    /// <para>
    /// <strong>A value the block did not name is still passed on.</strong>
    /// `|a|` with two values gives `a` the first and throws the second
    /// away, and `each_with_index`'s two values are the ones a game asks
    /// for — <strong>so the binding is by position and stops at the
    /// parameter list</strong>, because a reader that handed the last value
    /// to a single parameter would have made `|a| a.each { |x, i| }` swap
    /// the two.
    /// </para>
    /// </remarks>
    private RubyValue Yield(RubyNode pBlock, IReadOnlyList<RubyValue> pWerte)
    {
        var rumpf = pBlock.Children.Count > 2 ? pBlock.Children[2] : null;
        var parameter = pBlock.Children.Count > 1 ? pBlock.Children[1] : null;
        if (rumpf == null)
        {
            return RubyValue.Nil;
        }

        var namen = parameter == null
            ? []
            : Statements(parameter).Select(t => t.Name ?? string.Empty).ToList();
        var tiefe = _scopes.Count;
        var ebene = new Dictionary<string, RubyValue>(StringComparer.Ordinal);
        _scopes.Add(ebene);

        // **Direkt in die neue Ebene und nicht mit `SetLocal`.** Der
        // Aufrufer kann eine Variable gleichen Namens haben, **und
        // `SetLocal` sucht von innen nach aussen und schreibt in die Ebene,
        // in der es den findet** -- **der Parameter waere also nie
        // angelegt worden**, und die erste Zuweisung im Rumpf haette nach
        // aussen geschrieben. Ein Spiel mit `x = 100` und `each do |x|`
        // haette am Ende drei statt hundert, **und das ist die haeufigste
        // Form von allem, was ein Skript schreibt**.
        for (var i = 0; i < namen.Count; i++)
        {
            if (namen[i].Length == 0)
            {
                continue;
            }

            ebene[namen[i]] = i < pWerte.Count ? pWerte[i] : RubyValue.Nil;
        }

        var wert = EvaluateBlock(rumpf);
        _scopes.RemoveRange(tiefe, _scopes.Count - tiefe);
        return wert;
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
