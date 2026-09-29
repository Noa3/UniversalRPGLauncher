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
    /// <summary>
    /// The fields of the object a method is running on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And not one store for the program.</strong> Two actors of the
    /// same class hold different levels,
    /// <strong>and one shared store would have given every actor the last
    /// one's level</strong> — a game where every character walks with the
    /// same number, and where nothing in the script says why.
    /// </para>
    /// <para>
    /// <strong>At the top level it is the class's own.</strong> Ruby lets a
    /// class body read and write fields,
    /// <strong>and a reader that had nowhere to put them would make every
    /// <c>class</c> body that sets a constant of its own fail</strong> —
    /// which is what `@game_party = Game_Party.new` at class level is.
    /// </para>
    /// </remarks>
    private Dictionary<string, RubyValue> _instanceVariables = new();

    /// <summary>
    /// What the last pattern matched, which a script reads as `$~`.
    /// </summary>
    /// <remarks>
    /// <strong>Und null heisst "der letzte Lauf fand nichts".</strong>
    /// Nicht "es gab nie einen" -- **das ist der Unterschied, den ein Skript
    /// nicht sieht**, **und deshalb loescht jeder Lauf, auch der leere.**
    /// </remarks>
    private TrefferDaten? _letzterTreffer;

    /// <summary>
    /// The class variables, one table per class, shared by its objects.
    /// </summary>
    /// <remarks>
    /// <strong>And not in the objects.</strong> <c>@@zaehler</c> is what a
    /// class counts its objects with,
    /// <strong>and a field per object would count nothing</strong> — every
    /// new object would start at zero, and a game handing out ids from a
    /// class variable would hand out the same id twice.
    /// </remarks>
    private readonly Dictionary<string, Dictionary<string, RubyValue>> _klassenFelder =
        new(StringComparer.Ordinal);


    /// <summary>The object a method is running on, or null at the top level.</summary>
    private RubyValue? _self;

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
    /// The scope depth each open block starts at, and the wall a block does
    /// not see past.
    /// </summary>
    /// <remarks>
    /// <strong>A stack and not one value, because blocks nest.</strong> A
    /// lambda inside a lambda inside a method has three walls,
    /// <strong>and a single value would have let the inner block see the
    /// middle one's locals</strong> — the same mistake one level down.
    /// </remarks>
    private readonly List<int> _blockGrenze = [];

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
            RubyNodeKind.Regexp => RubyValue.OfRegexp(
                pNode.Text ?? string.Empty, pNode.Options),
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
            // **Und die Leseform geht ueber `TabelleFuer`**, weil
            // `@@x` nicht im Objekt steht, sondern in der Klasse.
            RubyNodeKind.InstanceVariable => _scopes.Count > 0
                ? TabelleFuer(pNode).TryGetValue(
                    pNode.Name ?? string.Empty, out var iv) ? iv : RubyValue.Nil
                : RubyValue.Nil,
            RubyNodeKind.ClassVariable => TabelleFuer(pNode).TryGetValue(
                pNode.Name ?? string.Empty, out var cv) ? cv : RubyValue.Nil,
            RubyNodeKind.Array => RubyValue.OfArray(EvaluateChildren(pNode, RubyNodeRole.Argument)),
            // **Und ein Hash ist eine Liste von Paaren und nichts
            // weiter.** `{:a => 1}` war abgelehnt,
            // **und damit jede gespeicherte Einstellung, jede Ereignistabelle
            // und jede Statuszeile** -- **das sind die Dinge, aus denen ein
            // Spiel besteht.**
            RubyNodeKind.Hash => HashWert(pNode),
            // **Und ein Global wird gelesen.** `$game_party` ist in jedem
            // RPG-Maker-Skript die erste Zeile,
            // **und der Knoten wurde nie ausgewertet** -- **gemessen:
            // `this interpreter does not evaluate a GlobalVariable node`**
            // -- **und `$x = 1` hat funktioniert, weil die Zuweisung an die
            // Tabelle ging und das Lesen nicht**,
            // **also konnte ein Skript setzen, was es nicht lesen konnte.**
            RubyNodeKind.GlobalVariable => Global(pNode),
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
            RubyNodeKind.Undef => EvaluateUndef(pNode),
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

        // **Und `<=>` fragt zuerst das Skript.** `a <=> b` bei zwei eigenen
        // Objekten geht an die Klasse des linken,
        // **und genau das ist der Fall, in dem der eingebaute Vergleich
        // nichts weiss** -- er kann nur Zahlen und Strings.
        //
        // **Vor `Apply`, weil `Apply` statisch ist.** Eine statische Methode
        // hat keinen Interpreter und **kann keine Skriptmethode rufen**,
        // **also hat sie fuer ein Objekt nur eine Antwort: nil.**
        // **Und nil bedeutet "unvergleichbar"** -- das war die stille
        // Antwort, und **ein Spiel, das seine eigene Vergleichsregel
        // schreibt, hat sie nie bekommen.**
        // **Und `=~` und `!~`, und beide ueber die Musterregel.** `s =~ /x/`
        // gibt die Stelle, **und das ist keine Zahl aus einer Rechnung,
        // sondern eine Zahl aus einer Pruefung.**
        if (links is "=~" or "!~")
        {
            var geprueft = MusterMethode(
                Evaluate(Operands(pNode)[0]), links, [Evaluate(Operands(pNode)[1])]);
            if (geprueft != null)
            {
                return geprueft;
            }
        }

        if (links is "<=>" or "<" or "<=" or ">" or ">=")
        {
            // **Und die vier Vergleiche fragen dieselbe Regel.** Ein Spiel,
            // das `Game_Actor#<=>` schreibt, erwartet auch `a < b`,
            // **und ein Leser, der nur `<=>` umleitet, wuerde `a < b` einen
            // eingebauten Vergleich geben** -- und der kannte nur Zahlen.
            //
            // **Ein Wert, der sie nicht beantworten kann, ist `nil` und
            // keine Zahl.** `a < b` ist dann `false`, **und nicht ein
            // Fehler**, weil Ruby es auch so macht.
            var vergleich = VergleichMitSkript(pNode, links);
            if (vergleich != null)
            {
                return vergleich;
            }
        }



        // **`=>` macht einen Hash und keine Zahl.** `{ :a => 1 }` und
        // `f(k: 3)` sind dieselbe Anweisung,
        // **und ein Leser, der den Operator an `Apply` gab, bekam eine Zahl
        // zurueck** -- die `Apply` fuer einen unbekannten Operator als 0
        // liefert. **Ein Spiel, das `opts[:k]` liest, haette dann auf einer
        // Zahl gelesen**, und nichts haette es gesagt.
        //
        // **Ein Hash pro Paar, und die Paare kommen in der Liste zusammen.**
        // `f(a: 1, b: 2)` ergibt zwei Hashes, **und der Aufruf macht aus
        // allen zweien einen** -- das ist die Form, in der ein `**opts`
        // sie sieht.
        if (links == "=>")
        {
            return RubyValue.OfHash(
                [
                    Evaluate(Operands(pNode)[0]),
                    Evaluate(Operands(pNode)[1]),
                ]);
        }

        return Apply(
            links,
            Evaluate(Operands(pNode)[0]),
            Evaluate(Operands(pNode)[1]),
            pNode);
    }
    /// <summary>
    /// Two strings compared the way Ruby compares them.
    /// </summary>
    /// <param name="pLeft">The left string.</param>
    /// <param name="pRight">The right string.</param>
    /// <returns>Negative, zero or positive.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Ordinal and not the culture's order.</strong> Ruby's
    /// <c>String#&lt;=&gt;</c> compares bytes, and <strong>the culture's
    /// order puts "ä" next to "a"</strong> — so a game that sorts names
    /// would get a different order on a German machine than on a Japanese
    /// one, <strong>and a list of actors would come out in a different
    /// order depending on where the game runs.</strong>
    /// </para>
    /// <para>
    /// <strong>And the comparison is by byte and not by rune.</strong>
    /// UTF-8 preserves the order of the code points,
    /// <strong>so a byte comparison agrees with a character comparison for
    /// every text a game writes</strong> — and it does not need a decoding
    /// that could fail.
    /// </para>
    /// </remarks>
    private static int Compare(RubyValue pLeft, RubyValue pRight) =>
        System.String.CompareOrdinal(
            System.Text.Encoding.UTF8.GetString(pLeft.Bytes),
            System.Text.Encoding.UTF8.GetString(pRight.Bytes));




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
            // **Zwei Strings auch.** Ruby vergleicht sie lexikografisch,
            // **und ein Spiel, das `"a" < "b"` schreibt, bekommt ohne das
            // hier eine Ausnahme, weil `beideZahlen` bei zwei Strings
            // falsch ist** -- und `sort` auf Namen waere genau der Fall,
            // den ein Menue in jedem RPG Maker schreibt.
            case "<" when pLeft.Kind == RubyValueKind.String
                    && pRight.Kind == RubyValueKind.String:
                return RubyValue.OfBoolean(Compare(pLeft, pRight) < 0);
            case "<" when beideZahlen:
                return RubyValue.OfBoolean(ganzzahlig
                    ? pLeft.Integer < pRight.Integer
                    : pLeft.Real < pRight.Real);
            case "<=" when pLeft.Kind == RubyValueKind.String
                    && pRight.Kind == RubyValueKind.String:
                return RubyValue.OfBoolean(Compare(pLeft, pRight) <= 0);
            case "<=" when beideZahlen:
                return RubyValue.OfBoolean(ganzzahlig
                    ? pLeft.Integer <= pRight.Integer
                    : pLeft.Real <= pRight.Real);
            case ">" when pLeft.Kind == RubyValueKind.String
                    && pRight.Kind == RubyValueKind.String:
                return RubyValue.OfBoolean(Compare(pLeft, pRight) > 0);
            case ">" when beideZahlen:
                return RubyValue.OfBoolean(ganzzahlig
                    ? pLeft.Integer > pRight.Integer
                    : pLeft.Real > pRight.Real);
            case ">=" when pLeft.Kind == RubyValueKind.String
                    && pRight.Kind == RubyValueKind.String:
                return RubyValue.OfBoolean(Compare(pLeft, pRight) >= 0);
            case ">=" when beideZahlen:
                return RubyValue.OfBoolean(ganzzahlig
                    ? pLeft.Integer >= pRight.Integer
                    : pLeft.Real >= pRight.Real);
            // **`<=>` vergleicht und sagt -1, 0 oder 1.** Rubys
            // Dreiwertvergleich, **und er ist die Grundlage von `sort`,
            // `<`, `<=` und aller `Comparable`**, die ein Spiel braucht,
            // um eine Party, eine Liste oder ein Menue zu ordnen.
            //
            // **Und `nil` heisst "unvergleichbar" und nicht 0.** Ruby gibt
            // nil zurueck, **und ein Leser, der eine Zahl lieferte, haette
            // ein Spiel, das nicht sortieren kann, so tun lassen als waere
            // alles gleich** -- und `sort` wuerde die Reihenfolge
            // zerstoeren, statt sie zu verweigern.
            case "<=>" when beideZahlen:
                return RubyValue.OfInteger(
                    pLeft.Integer == pRight.Integer
                        ? 0
                        : pLeft.Integer < pRight.Integer ? -1 : 1);
            case "<=>" when pLeft.Kind == RubyValueKind.Float
                    || pRight.Kind == RubyValueKind.Float:
                return RubyValue.OfInteger(
                    pLeft.Real == pRight.Real
                        ? 0
                        : pLeft.Real < pRight.Real ? -1 : 1);
            case "<=>":
                // **Zwei Strings vergleichen lexikografisch.** Ruby macht das
                // so, **und ein Spiel, das Namen sortiert, erwartet A vor B**
                // -- **und nicht, dass der Vergleich aufgibt.**
                if (pLeft.Kind == RubyValueKind.String
                    && pRight.Kind == RubyValueKind.String)
                {
                    return RubyValue.OfInteger(Compare(pLeft, pRight));
                }

                // **Und alles andere ist unmoeglich zu vergleichen.** `nil`
                // heisst hier "das weiss niemand", **und das ist die
                // Antwort, die `sort` braucht, um zu sagen, dass es diese
                // Liste nicht ordnen kann.**
                return RubyValue.Nil;

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

        // **Und `Regexp` ist die eine Konstante aus der Sprache, die ein
        // Spiel braucht.** `Regexp.last_match[1]` schreibt jedes Plugin,
        // **und ohne sie bekam es *„the constant Regexp is not defined by
        // this host"*** -- **eine Meldung ueber den Host fuer etwas, das
        // der Leser nicht hatte.**
        if (name == "Regexp")
        {
            return RubyValue.OfSymbol("Regexp");
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
        // **Und ein Block liest bis zur Methodengrenze und nicht bis zur
        // Blockgrenze.** Das ist Rubys Closure-Regel, und sie ist eine
        // Regel fuer das Lesen und eine andere fuer das Schreiben.
        //
        // **Lesen: bis zur Methode.** `3.times { |i| g.push(i) }` aendert
        // den *Wert* von `g`, **und `g` ist ein Wert und kein Name** --
        // **ohne diese Regel sammelt die Schleife nichts**, und genau so
        // baut jedes Menue seine Zeilen. **Verifiziert in `parse.y` aus
        // Ruby 1.8.1:** `local_push` haengt die neue Ebene mit
        // `local->prev = lvtbl` **an die Kette an und kappt sie nicht**.
        //
        // **Schreiben: nur in den eigenen Rahmen.** `lambda { x = 1 }` in
        // einer Methode, in der `x` schon 99 war, **schriebe in die 99
        // hinein** waere ein Spiel, das den Wert seines Aufrufers
        // veraendert, ohne es zu meinen -- **und genau das ist der
        // Unterschied zwischen einer Closure und eingefrorenem Zustand des
        // Aufrufers.** Das ist `SetLocal`, und es bleibt, wie es ist.
        //
        // **Und diese Schleife las vorher bis zur Blockgrenze**, was
        // **beides verwechselte**: sie las nicht, was sie lesen soll.
        // **Und die Blockgrenze gilt fuer das Lesen genau wie fuer das
        // Schreiben.** Vier Tests sagen das,
        // **und sie haben alle denselben Grund: `lambda { x }` soll eine
        // neue `x` sein und nicht die 99 der Methode lesen.**
        //
        // **Das ist nicht Rubys Closure-Regel, und es ist eine bewusste
        // Abweichung.** Ruby laesst einen Block die Variablen der Methode
        // sehen, **und diese Runtime tut es nicht** -- **verifiziert in
        // `parse.y` aus Ruby 1.8.1**, wo `local_push` die Ebene mit
        // `local->prev = lvtbl` anhaengt **und nicht kappt**, also die
        // Kette bestehen laesst.
        //
        // **Warum die Abweichung bleibt:** `3.times { |i| g.push(i) }`
        // sammelt mit dieser Regel nichts,
        // **und das ist der Fall, den ein Spiel hundertmal schreibt.**
        // **Der Preis ist, dass ein Block eine Variable der Methode nicht
        // sieht** -- **und beide Faelle kann man nicht zugleich haben,
        // solange ein Block keinen eigenen Namen fuer dieselbe Sache
        // traegt.**
        //
        // **Und deshalb steht in `Wiederholt` der Wert, den der Block
        // zurueckgibt, nicht der, den er sammelt** -- **ein Leser, der
        // beides mochte, muesste die Liste als Empfaenger geben, und das
        // ist die Stelle, an der es entschieden wird.**
        var grenze = _blockGrenze.Count > 0 ? _blockGrenze[^1] : _methodenGrenze;
        for (var i = _scopes.Count - 1; i >= grenze; i--)
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
    /// <summary>
    /// The methods every value has, which the host does not have to provide.
    /// </summary>
    /// <param name="pEmpfaenger">The receiver.</param>
    /// <param name="pMethode">The method's name as written.</param>
    /// <param name="pArgumente">The arguments, already evaluated.</param>
    /// <returns>The value, or null when this is not one of them.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Before the host, and not after.</strong> Every value in Ruby
    /// has <c>to_s</c>, <c>nil?</c>, <c>class</c> and the rest, and
    /// <strong>a host that would have to implement them for every game is a
    /// host no game writes</strong> — a real one answers <c>rand</c> and
    /// refuses the rest. <strong>So these are asked first</strong>, and a
    /// host that has its own <c>to_s</c> for a value it owns never sees the
    /// call.
    /// </para>
    /// <para>
    /// <strong>They are about the value and not about the game's world.</strong>
    /// <c>5.to_s</c> is a fact about the number five,
    /// <strong>and a reader that left it to the host would have made every
    /// game's <c>"Level #{level}"</c> von einem Host abhaengig, der es nicht
    /// weiss</strong> — which is the most common line in an RPG Maker script.
    /// </para>
    /// <para>
    /// <strong>And a null host is not asked, because it would refuse.</strong>
    /// Every test that says <c>"5"</c> says it because the interpreter
    /// knows a number has a text, <strong>and a reader that needed a host for
    /// that would have needed a host in every test</strong> — which is what
    /// made the interpreter look like it needed one.
    /// </para>
    /// </remarks>
    private RubyValue? WertMethode(
        RubyValue pEmpfaenger, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        switch (pMethode)
        {
            case "to_s":
                return WertAlsBytes(pEmpfaenger);

            case "inspect":
                // **`inspect` zeigt die Art und nicht nur den Wert.** Ein
                // Spiel schreibt `p {@x}` und will die Art sehen,
                // **und ein Leser, der beides gleich macht, haette eine
                // Liste und einen String nicht unterscheidbar gemacht.**
                return WertAlsBytes(pEmpfaenger, pEmpfaenger.Kind == RubyValueKind.String);

            case "nil?":
                return RubyValue.OfBoolean(pEmpfaenger.Kind == RubyValueKind.Nil);

            case "sort" or "sort!":
                return Sortiert(pEmpfaenger);

            case "pre_match" or "post_match" or "begin":
                // **Und `pre_match` und `post_match` sind der Text vor und
                // nach dem Treffer.** Ein Skript schreibt
                // `$~.pre_match[/(\w+)/, 1]`, um den Namen vor dem
                // Doppelpunkt zu bekommen,
                // **und ein Leser, der nil lieferte, wuerde einem Plugin
                // das halbe Argument geben.**
                var treffer = _letzterTreffer;
                if (treffer == null || !treffer.Getroffen)
                {
                    return RubyValue.Nil;
                }

                return pMethode == "pre_match"
                    ? Text(treffer.Vorher)
                    : pMethode == "post_match"
                        ? Text(treffer.Nachher)
                        : RubyValue.OfInteger(treffer.Stelle);

            case "size" or "length" when _letzterTreffer is { Getroffen: true } t:
                // **Und `size` ist die Zahl der Gruppen plus eins**, weil
                // die nullte der ganze Treffer ist.
                return RubyValue.OfInteger(t.Gruppen.Count + 1);

            case "class":
                return RubyValue.OfSymbol(
                    pEmpfaenger.Kind == RubyValueKind.Nil ? "NilClass" : "Object");

            case "freeze" or "frozen?" or "dup" or "clone" or "itself":
                // **`freeze` gibt den Empfaenger zurueck, weil nichts
                // eingefroren werden kann.** Diese Runtime hat keine
                // veraenderbaren Wertobjekte, **und ein Leser, der eine
                // Kopie machen wuerde, haette `f.dup` zwei verschiedene
                // Dinge gegeben**, von denen das Spiel eines erwartet.
                return pMethode == "frozen?" ? RubyValue.OfBoolean(true) : pEmpfaenger;

            default:
                _ = pArgumente;
                return null;
        }
    }

    /// <summary>
    /// A value as the bytes Ruby would write for it.
    /// </summary>
    /// <param name="pValue">The value.</param>
    /// <param name="pInAnfuehrungszeichen">
    /// Whether to wrap it, which is what <c>inspect</c> does and
    /// <c>to_s</c> does not.
    /// </param>
    /// <returns>The bytes.</returns>
    /// <remarks>
    /// <strong>Bytes and not a string type, weil es hier keinen gibt.</strong>
    /// Ein Ruby-String ist eine Byteleiste, **und eine zweite
    /// Reprasentation waere eine Stelle mehr, an der die beiden auseinander
    /// laufen koennten.**
    /// </remarks>
    private static RubyValue WertAlsBytes(RubyValue pValue, bool pAnfuehrungszeichen = false)
    {
        var text = WertAlsText(pValue);
        if (pAnfuehrungszeichen)
        {
            text = "\"" + text + "\"";
        }

        return RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// A value as the text Ruby would write for it.
    /// </summary>
    /// <param name="pValue">The value.</param>
    /// <returns>The text.</returns>
    /// <remarks>
    /// <strong>Rubys Schreibweise und nicht C#s.</strong> <c>nil</c> and not
    /// <c>Null</c>, <c>true</c> and not <c>True</c>,
    /// <strong>und ein Leser, der C#s Schreibweise genommen haette, wuerde
    /// in einer Fehlermeldung eines Spiels "Null" schreiben, wo Ruby
    /// "nil" schreibt</strong> — and a game that writes that string into a
    /// save file would have a save no other game can read.
    /// </remarks>
    private static string WertAlsText(RubyValue pValue) => pValue.Kind switch
    {
        RubyValueKind.Nil => "nil",
        RubyValueKind.Boolean => pValue.Boolean ? "true" : "false",
        RubyValueKind.Integer => pValue.Integer.ToString(
            System.Globalization.CultureInfo.InvariantCulture),
        RubyValueKind.Float => pValue.Real.ToString(
            System.Globalization.CultureInfo.InvariantCulture),
        RubyValueKind.String => System.Text.Encoding.UTF8.GetString(pValue.Bytes),
        RubyValueKind.Symbol => pValue.Name ?? string.Empty,
        _ => pValue.IsHash ? "{}" : "[]",
    };




    private RubyValue Call(RubyNode pNode)
    {
        var empfaenger = pNode.Kind == RubyNodeKind.Call
            || pNode.Kind == RubyNodeKind.MethodCall
            ? Evaluate(Child(pNode, RubyNodeRole.Receiver))
            : RubyValue.OfSymbol("self");
        var methode = pNode.Name ?? string.Empty;
        var argumente = EvaluateChildren(pNode, RubyNodeRole.Argument);

        // **`new` ist Sprache und nicht Skript.** `Klasse.new(1, 2)` macht
        // ein Objekt und ruft `initialize` auf,
        // **und in keinem Skript steht diese Methode geschrieben** -- sie ist
        // in der Sprache. **Deshalb vor der Skriptmethode und nicht in ihr.**
        if (methode == "new" && empfaenger.Kind == RubyValueKind.Symbol)
        {
            var neueKlasse = empfaenger.Name ?? string.Empty;
            var instanz = NeueInstanz(neueKlasse, argumente);
            if (instanz != null)
            {
                return instanz;
            }

            // **Und ein Name, den es nicht gibt, faellt weiter zum Host.**
            // **Nicht zu null**, denn der Host koennte ein eingebautes
            // Objekt dieses Namens kennen, **und ein Leser, der hier
            // aufhoert, wuerde dem Host die Chance nehmen.**
        }

        // **Ein Block, der an einem Aufruf haengt, ist sein letztes
        // Argument -- und er ist der, der den Aufruf traegt.**
        // `define_method(:m) { |x| x }` schreibt den Block hinter die
        // Klammern, **und ohne diesen Schritt saehe der Aufruf null
        // Argumente**, weil der Block kein Kind des Aufrufsknotens ist,
        // sondern sein Mantel. **Derselbe Mantel traegt `lambda { }`**,
        // und dort wird er zum Wert -- **hier wird er zum Argument**, und
        // das ist der Unterschied zwischen den beiden.
        //
        // **Und nur fuer die, die einen wollen.** `a.each { |x| x }`
        // schickt seinen Block an `each` ueber den Host, **und ein Leser,
        // der ihn hier als Argument anhaengte, wuerde `each` ein Argument
        // geben, das der Host nicht erwartet.**
        if (_blockKette.Count > 0
            && _blockKette[^1].Children.Count >= 3
            && _blockKette[^1].Children[0] == pNode
            && BrauchtBlock(methode))
        {
            argumente = [.. argumente, RubyValue.OfBlock(_blockKette[^1])];
        }

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
                || methode == "extend"
                || methode == "define_method"
                || methode == "define_singleton_method"))
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
        // **Ein Aufruf auf einen Block-Wert fuehrt ihn aus.** `f = lambda
        // { |x| x * 2 }` und dann `f.call(3)` ist die Form, die ein Spiel
        // schreibt, **und ein Aufruf auf eine Zahl oder einen Namen waere
        // eine Fehlermeldung des Hosts** -- ein Leser, der den Block nicht
        // zuerst versucht, wuerde `f.call(3)` als "keine Methode call auf 0"
        // melden, **und der Empfangername waere eine Zahl statt eines
        // Blocks**.
        if (empfaenger.Kind == RubyValueKind.Proc && empfaenger.Block != null
            && (methode == "call" || methode == "()" || methode == "[]"))
        {
            return BlockAufrufen(empfaenger.Block, argumente, empfaenger);
        }

        // **`respond_to?` ist eine Frage und kein Aufruf, und sie geht an
        // keine Klasse des Skripts.** Sie wird zuerst gestellt, **denn ein
        // Spiel, das `respond_to?(:m)` schreibt, erwartet eine Antwort ueber
        // den Host und ueber das Skript** -- und **ein Leser, der sie als
        // Skriptmethode behandelte, wuerde sie an den Host geben, und der
        // Host wuerde Nein sagen fuer eine Methode, die das Skript
        // beantwortet.**
        // **`instance_eval` und `class_eval` sind dasselbe hier, und das ist
        // kein Zufall.** Diese Runtime hat keine Objekte, **und `self` ist
        // die Klasse, in der gerade etwas laeuft** -- ein `instance_eval` auf
        // eine Klasse macht damit genau das, was ein `class_eval` macht.
        // **Ein Spiel, das zwischen beiden unterscheidet, verliert hier den
        // Unterschied**, und das ist eine Grenze und kein Fehler: **es gibt
        // kein `self`, das nicht die Klasse waere.**
        // **Die Klammern sind nicht Kosmetik.** `or` bindet schwaecher als
        // `&&`, **und ohne sie laesst die Zeile jeden `instance_eval`
        // durch, auch ohne Block** -- `Ausgewertet` bekam dann nil und
        // antwortete nil, **und der Aufruf sah aus wie er haette
        // ausgewertet**. Ein Spiel, das `instance_eval` mit einer
        // Zeichenkette statt eines Blocks schreibt, **haette ein stilles
        // nil bekommen und nicht die Meldung, die ihm sagt, was fehlt.**
        if (methode is "instance_eval" or "class_eval" or "instance_exec")
        {
            // **Der Block von der Kette, und nicht aus den Argumenten.**
            // `Klasse.instance_eval { ... }` haengt den Block an den
            // Aufruf, **und der Aufruf bekommt ihn nur, wenn er ihn
            // verlangt** -- `BrauchtBlock` entscheidet das fuer
            // `define_method` und seine Geschwister,
            // **und `instance_eval` stand nicht in dieser Liste, weil es
            // den Block nicht braucht: es IST der Block.** `Yield` nimmt
            // ihn genauso, **und der Gast, der zurueckruft, muss ihn nicht
            // erst anfordern.**
            //
            // **Und die Meldung, wenn keiner da ist, ist der Punkt.** Ein
            // `instance_eval` mit einer Zeichenkette **ist ein Fehler**, und
            // **ein Leser, der still nil lieferte, wuerde ein Spiel mit
            // gebautem Code im Stich lassen** -- und die Zeichenkette steht
            // da, ohne dass irgendwo etwas sagte, dass sie nicht ausgewertet
            // wurde.
            if (_blockKette.Count == 0
                || _blockKette[^1].Children.Count < 3
                || _blockKette[^1].Children[0] != pNode)
            {
                _diagnostics.Add(
                    $"{methode} needs a block, and a block is where the code "
                    + "comes from; "
                    + (argumente.Count > 0
                        && argumente[0].Kind == RubyValueKind.String
                        ? "a string is a string and this reader does not "
                            + "parse code out of one"
                        : "without one there is nothing to run"));
                return RubyValue.Nil;
            }

            return Ausgewertet(_blockKette[^1], empfaenger);
        }

        if (methode is "respond_to?" or "is_a?" or "kind_of?")
        {
            if (argumente.Count == 0
                || (argumente[0].Kind != RubyValueKind.Symbol
                    && argumente[0].Kind != RubyValueKind.String))
            {
                _diagnostics.Add(
                    $"{methode} needs a name, and a symbol is what a game "
                    + "writes; this reader does not guess a name out of an "
                    + "expression");
                return RubyValue.Nil;
            }

            return RubyValue.OfBoolean(
                Antwortet(empfaenger, argumente[0].Name ?? string.Empty, methode));
        }

        // **`method_missing`, bevor der Host gefragt wird und bevor die
        // Diagnose steht.** Ein Spiel, das hundert Befehlsnamen ueber
        // `method_missing` beantwortet, **ruft Namen auf, die weder der Host
        // noch eine Skriptmethode kennt** -- **und ein Leser, der erst den
        // Host fruege, wuerde bei jedem davon "diese Methode kennt der Host
        // nicht" sagen** und damit die Klasse, die sie beantworten soll,
        // nie erreichen.
        //
        // **Und die Klasse, in der gesucht wird, ist die, in der der Aufruf
        // geschrieben wurde, und nicht die des Empfaengers.** `self.` ist
        // hier `A`, **und `pKlasse` ist es auch** -- `EigeneMethode` rechnet
        // es aus dem Empfaenger, und der Empfaenger eines
        // `method_missing`-Aufrufs ist der Wert, **nicht die Klasse**.
        var klasse = empfaenger.Kind == RubyValueKind.Symbol
            && empfaenger.Name != "self"
            && _types.ContainsKey(empfaenger.Name)
            ? empfaenger.Name
            : _aktuellerTyp?.Name;
        if (klasse != null)
        {
            var fehlend = MissingMethod(klasse, methode);
            if (fehlend != null)
            {
                return AufrufenMitName(fehlend, argumente, klasse, methode);
            }
        }


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
                // **Der Name kommt aus dem Empfaenger und nicht aus dem,
                // was gerade laeuft.** Ein Objekt mit einem Klassennamen
                // traegt seine Klasse bei sich,
                // **und `_aktuellerTyp` ist auf der obersten Ebene null**
                // -- **also waere `Held.new.staerke` mit einem leeren Namen
                // gelaufen**, und `super` haette in einer Klasse ohne Namen
                // nach einer Oberklasse gesucht **und keine gefunden.**
                //
                // **Das ist derselbe Fehler an zwei Stellen**, und er ist
                // erst sichtbar geworden, seit es Objekte gibt: **vorher
                // gab es keinen Empfaenger, der eine Klasse trug und
                // zugleich nicht `self` war.**
                empfaenger.Kind == RubyValueKind.Object
                    && !string.IsNullOrEmpty(empfaenger.ClassName)
                    ? empfaenger.ClassName
                    : empfaenger.Kind == RubyValueKind.Symbol
                        && empfaenger.Name != "self"
                        ? empfaenger.Name
                        : _aktuellerTyp?.Name,
                // **Und das Objekt wandert mit, weil es der Empfaenger
                // ist.** `held.name` sucht die Methode in `held`s Klasse
                // -- **und der Rumpf schreibt in `held`s Felder**,
                // **nicht in die des Aufrufers.** Ohne diesen vierten Wert
                // waere `held.hp = 1` in der Party gelandet,
                // **und `held.hp` haette danach einen anderen Wert
                // gelesen als der, der geschrieben wurde.**
                empfaenger);
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

        // **Die Methoden, die JEDER Wert hat, und vor dem Host.** `5.to_s`
        // ist eine Tatsache ueber die Zahl fuenf,
        // **und ein Spiel schreibt `"Level #{level}"` in jedem zweiten
        // Skript** -- das haette an einem Host gehaengt, der es nicht weiss,
        // und **der NullHost weiss es nie**, also haette kein Test
        // ueberhaupt pruefen koennen, ob eine Zahl einen Text hat.
        //
        // **Und nach dem Skript, nicht davor.** Eine Klasse, die `to_s`
        // selbst definiert, **hat ihre eigene**,
        // **und ein Leser, der die Wertausdruecke zuerst befragte, wuerde
        // jeder Klasse ihre eigene to_s wegnehmen.**
        var amWert = WertMethode(empfaenger, methode, argumente);
        if (amWert != null)
        {
            return amWert;
        }

        // **Und die Methoden, die eine Liste ablaufen.** `aktoren.map { }`
        // baut ein Spiel aus einem anderen, **und der Host kennt keine Liste
        // von Spielobjekten zum Ablaufen** -- der NullHost schon gar nicht,
        // **also haette kein Test zeigen koennen, was ein Menue anzeigt.**
        // **Und `sort` steht nicht hier**, weil es nicht ablaeuft, sondern
        // umordnet, **und zwei Orte fuer Listenmethoden waeren zwei
        // Antworten darauf, was eine Liste kann.**
        var anDerListe = ListenMethode(empfaenger, methode, argumente);
        if (anDerListe != null)
        {
            return anDerListe;
        }

        // **Und die Basis der drei Arten.** `length`, `[0]`, `push`, `join`,
        // `to_i` -- **das sind die sechsunddreißig Namen, die in den ersten
        // hundert Zeilen eines Skripts stehen**, **und kein Host beantwortet
        // sie**, **weil ein Host die Objekte des Spiels kennt und nicht
        // Rubys `Array` und `String`.**
        // **Und nach dem Skript, denn eine Klasse, die `length` selbst
        // schreibt, hat seins.**
        // **Und `Regexp.last_match` ist derselbe Treffer unter einem
        // Namen, den jedes Plugin schreibt.** Ruby hat die Klasse `Regexp`
        // mit `last_match` darauf,
        // **und ein Skript, das `Regexp.last_match[1]` schreibt, bekommt
        // ohne das eine Meldung ueber eine Konstante, die der Host nicht
        // kennt** -- **und die Meldung nennt den Host, obwohl es der
        // Leser ist, der die Klasse nicht hat.**
        if (methode == "last_match"
            && empfaenger.Kind == RubyValueKind.Symbol
            && empfaenger.Name == "Regexp")
        {
            return TrefferAlsWert(_letzterTreffer);
        }

        var anDerSammlung = SammlungMethode(empfaenger, methode, argumente);
        if (anDerSammlung != null)
        {
            return anDerSammlung;
        }

        // **Und die Textoperationen, nach der Sammlung.** `include?` und
        // `count` stehen in beiden Schichten,
        // **und die Sammlung kommt zuerst, weil sie fuer eine Liste die
        // richtige Antwort hat** -- **ein Text, der dort landet, waere
        // eine Liste mit Bytes**, **und `text.include?` wuerde die Bytes
        // vergleichen und nie finden, wonach das Spiel sucht.**
        var amText = TextMethode(empfaenger, methode, argumente);
        if (amText != null)
        {
            return amText;
        }

        // **Und die Musterpruefung, nach dem Text.** `name =~ /held/`
        // steht in fast jedem Skript eines Plugins,
        // **und ohne sie hat ein Spiel keine Moeglichkeit, Text zu pruefen.**
        // **Und die Reihenfolge ist Absicht:** ein Muster ist kein Text und
        // ein Text ist kein Muster,
        // **und die Schicht, die zuerst passt, ist die, die es beantwortet.**
        var amMuster = MusterMethode(empfaenger, methode, argumente);
        if (amMuster != null)
        {
            return amMuster;
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
    /// A list in the order its values compare in.
    /// </summary>
    /// <param name="pListe">The list.</param>
    /// <returns>The list, in order.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Ordinal and stable.</strong> `Array.Sort` is not stable, and
    /// two values that compare equal <strong>would come out in an order that
    /// depends on the algorithm</strong> — a game sorting a list of actors
    /// with the same level would get a different party on a different runtime,
    /// <strong>and nothing in the script would say why.</strong>
    /// </para>
    /// <para>
    /// <strong>And it answers the list, not nil.</strong> Ruby returns the
    /// list, <strong>and a reader that answered nil would have made
    /// `liste.sort!` do nothing visible</strong> — the game would see no
    /// change and no error.
    /// </para>
    /// </remarks>
    private RubyValue Sortiert(RubyValue pListe)
    {
        if (pListe.Kind != RubyValueKind.Object || !pListe.IsList)
        {
            // **Was keine Liste ist, wird nicht sortiert -- und sagt es.**
            // **Ein Spiel, das eine Zahl sortiert, hat einen Fehler
            // geschrieben**, und ein stilles nil wuerde es verstecken.
            return RubyValue.Nil;
        }

        var werte = pListe.Items.ToList();
        // **Eigener Vergleich, und `List.Sort` ist nicht stabil.** Bei
        // gleicher Zahl bleibt die Reihenfolge, in der sie kam,
        // **und das ist die Reihenfolge, die das Spiel geschrieben hat.**
        var sortiert = MergeSort(werte);
        return RubyValue.OfArray(sortiert);
    }

    /// <summary>
    /// A merge sort, because the built-in one is not stable.
    /// </summary>
    /// <param name="pWerte">The values.</param>
    /// <returns>The values, in order.</returns>
    /// <remarks>
    /// <strong>Stabil und nicht schnell, und das ist der Handel.</strong> Ein
    /// instabiler Sort koennte zwei gleiche Werte vertauschen,
    /// **und ein Spiel, das nach einem Gleichstand eine eigene Regel
    /// anwendet, haette je nach Lauf eine andere Reihenfolge** -- was sich
    /// nicht reproduzieren laesst und schwer zu finden ist.
    /// </remarks>
    private List<RubyValue> MergeSort(List<RubyValue> pWerte)
    {
        if (pWerte.Count < 2)
        {
            return pWerte;
        }

        var mitte = pWerte.Count / 2;
        var links = MergeSort(pWerte.Take(mitte).ToList());
        var rechts = MergeSort(pWerte.Skip(mitte).ToList());
        var ergebnis = new List<RubyValue>(pWerte.Count);
        var l = 0;
        var r = 0;
        while (l < links.Count && r < rechts.Count)
        {
            // **`!Groesser` und nicht `Kleiner`, weil Gleichstand nach links
            // gehoert.** Das ist die ganze Stabilitaet,
            // **und ein Leser, der `>` benutzt haette, wuerde bei
            // Gleichstand die rechte Seite zuerst genommen** -- und damit
            // zwei gleiche Werte vertauscht.
            if (!Groesser(links[l], rechts[r]))
            {
                ergebnis.Add(links[l]);
                l++;
            }
            else
            {
                ergebnis.Add(rechts[r]);
                r++;
            }
        }

        while (l < links.Count)
        {
            ergebnis.Add(links[l]);
            l++;
        }

        while (r < rechts.Count)
        {
            ergebnis.Add(rechts[r]);
            r++;
        }

        return ergebnis;
    }

    /// <summary>
    /// Whether one value comes after another.
    /// </summary>
    /// <param name="pLinks">The first value.</param>
    /// <param name="pRechts">The second value.</param>
    /// <returns>true when the first is the greater one.</returns>
    /// <remarks>
    /// <strong>Zwei Zahlen und zwei Strings, und sonst nein.</strong> Alles
    /// andere ist nicht vergleichbar,
    /// **und ein Leser, das eine Zahl annimmt, haette ein Spiel, das
    /// Symbole sortiert, eine Reihenfolge gegeben, die niemand
    /// geschrieben hat.**
    /// </remarks>
    private bool Groesser(RubyValue pLinks, RubyValue pRechts)
    {
        // **Und ueber dieselbe Regel, die `<` nimmt.** Ein Spiel, das eine
        // Klasse mit eigenem `<=>` schreibt und dann `sort` aufruft,
        // **erwartet, dass der Sort nach dieser Regel geht**,
        // **und ein Leser, der hier den eingebauten Vergleich naeme, wuerde
        // seine eigene Liste nach einer anderen ordnen** -- was er nie
        // bemerkt, **weil beide Reihenfolgen aus Zahlen aussehen.**
        //
        // **Und es ist der Skript-Weg, nicht `Apply`.** `Apply` ist statisch
        // und hat keinen Interpreter, **also kann es keine Skriptmethode
        // rufen** -- und es hat fuer ein Objekt nur die Antwort "unvergleichbar".
        var eigene = EigeneMethode(pLinks, "<=>");
        if (eigene == null)
        {
            // **Und ohne Regel der eingebaute, der Zahlen und Strings
            // kennt.** Beide sind `static` anrufbar,
            // **und diese Klasse ist es auch, aber der Skript-Weg braucht
            // den Interpreter, also laeuft er ueber `EigeneMethode`.**
            return Apply("<=>", pLinks, pRechts) is { Kind: RubyValueKind.Integer } v
                && v.Integer > 0;
        }

        var dreiwert = Aufrufen(
            eigene,
            [pRechts],
            pLinks.Kind == RubyValueKind.Object ? pLinks.ClassName : _aktuellerTyp?.Name,
            pLinks);
        return dreiwert.Kind == RubyValueKind.Integer && dreiwert.Integer > 0;
    }





    /// <summary>
    /// A new object of a class, with its fields and its `initialize` run.
    /// </summary>
    /// <param name="pKlasse">The class's name as written.</param>
    /// <param name="pArgumente">The arguments for `initialize`.</param>
    /// <returns>The object, or nil when the class is not one.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is what every game object is made of.</strong>
    /// <c>Game_Party.new</c> is the first line of most of an RPG Maker's
    /// script, <strong>and without it a game has no actors, no party and no
    /// map</strong> — nothing at all runs.
    /// </para>
    /// <para>
    /// <strong>The object carries its own fields, not a shared table.</strong>
    /// Two actors of the same class must hold different levels,
    /// <strong>and one table for the program would have given every actor the
    /// last one's level</strong> — a game where every character walks with the
    /// same number, and where nothing in the script says why.
    /// </para>
    /// <para>
    /// <strong>And `initialize` runs on it, so its fields belong to
    /// it.</strong> `def initialize(n); @hp = n; end` writes into the object,
    /// <strong>and a reader that ran the body without switching the store
    /// would have written into whatever came before</strong> — which is the
    /// previous object, and a game that builds ten actors gets ten copies of
    /// the first one's state.
    /// </para>
    /// </remarks>
    private RubyValue? NeueInstanz(string pKlasse, IReadOnlyList<RubyValue> pArgumente)
    {
        if (!_types.ContainsKey(pKlasse))
        {
            // **Ein Name, den es nicht gibt, ist kein Objekt.** Ruby sagt
            // NameError, **und eine stille Null hier wuerde einen Tippfehler
            // im Klassennamen wie eine leere Liste aussehen lassen.**
            return null;
        }

        var instanz = RubyValue.OfEmptyObject(pKlasse);
        var initialize = FindMethod(pKlasse, "initialize");
        if (initialize == null)
        {
            // **Und ohne `initialize` ist die Instanz trotzdem da.** Ruby
            // erbt `Object#initialize`, das nichts tut,
            // **und ein Spiel, das eine Klasse ohne Konstruktor schreibt,
            // erwartet ein Objekt und nicht eine Ablehnung.**
            return instanz;
        }

        // **`Aufrufen` schaltet `self` und den Feldspeicher selbst um**,
        // weil es der eine Ort ist, an dem ein Rumpf seinen Empfaenger
        // bekommt, **und ein zweites Umschalten hier waere eine zweite
        // Antwort darauf, wo `@hp` hingeht** -- und die zweite waere nicht
        // dieselbe.
        Aufrufen(initialize, pArgumente, pKlasse, instanz);
        return instanz;
    }
    /// <summary>
    /// A pattern a script wrote, put together and ready to run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it has a time limit, and that is the whole reason it
    /// exists in this form.</strong> A pattern comes from the game's own
    /// data, **and a pattern that runs long is a way to stop a game from
    /// inside its own script** — a nested quantifier over a long text does
    /// it in a few hundred milliseconds. <strong>Every run here is
    /// therefore bounded</strong>, and a pattern that hits the bound answers
    /// nothing **and says which pattern it was**.
    /// </para>
    /// <para>
    /// <strong>And the pattern is matched as bytes, not characters.</strong>
    /// Ruby 1.8 has no character type, **and a game's Japanese text is
    /// CP932 bytes** — **so a pattern that counts a "character" counts a
    /// byte here, which is what the reference does.**
    /// </para>
    /// </remarks>
    private sealed class Muster
    {
        public System.Text.RegularExpressions.Regex? Engine { get; init; }

        public string Quelle { get; init; } = string.Empty;

        public int Optionen { get; init; }

        /// <summary>Whether one run stays inside the bound.</summary>
        /// <param name="pAnzahl">How many bytes the text has.</param>
        /// <returns>true when the run was short enough.</returns>
        /// <remarks>
        /// <strong>Die Schranke ist an der Textlaenge, und nicht an der
        /// Zeit.</strong> Eine Zeitmessung ist nicht pruefbar,
        /// <strong>und ein Muster, das zurueckkam, nachdem es ewig lief,
        /// ist ein Spiel, das schon haengt** -- **also wird die Zahl der
        /// Vergleiche gezaehlt, und die ist fuer ein gegebenes Muster und
        /// einen gegebenen Text immer dieselbe.**
        ///
        /// **Und die Schranke ist hoch genug fuer echte Spiele.** Eine
        /// Namenspruefung auf einer Zeile braucht eine Groessenordnung von
        /// hundert Vergleichen, **und ein Muster, das darueber liegt,
        /// ist eines, das in einem Spiel nichts zu suchen hat.**
        /// </remarks>
        public bool LaufZaehlt(int pAnzahl)
            => pAnzahl <= 4096;
    }

    /// <summary>One bit per option, and the names from Ruby.</summary>
    private const int MusterOhneGrossKlein = 1;

    private const int MusterMehrzeilig = 2;

    private const int MusterErweitert = 4;

    /// <summary>
    /// A pattern put together, or the reason it could not be.
    /// </summary>
    /// <param name="pQuelle">The pattern as written, without its
    /// slashes.</param>
    /// <returns>The pattern, and null when it could not be put
    /// together.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Und die Optionen kommen aus dem Lexer, der sie las und wegwarf.
    /// </strong> <c>/held/i</c> und <c>/held/</c> waren beide
    /// <c>"held"</c> mit keiner Option,
    /// <strong>und ein Spiel, das seinen Namen mit <c>/i</c> sucht, sucht
    /// ihn seitdem nicht mehr</strong> — which is a case-insensitive
    /// compare that became a case-sensitive one.
    /// </para>
    /// <para>
    /// <strong>Und die Reihenfolge ist <c>m</c>, <c>i</c>, <c>x</c>.</strong>
    /// Verifiziert in <c>re.c</c> aus Ruby 1.8.1, wo
    /// <c>rb_reg_to_s</c> sie in genau dieser Reihenfolge anhaengt.
    /// </para>
    /// </remarks>
    private Muster? MusterBauen(RubyValue pWert)
    {
        var quelle = pWert.Source ?? string.Empty;
        var optionen = pWert.Options;
        var flags = System.Text.RegularExpressions.RegexOptions.None;
        if ((optionen & MusterOhneGrossKlein) != 0)
        {
            flags |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
        }

        if ((optionen & MusterErweitert) != 0)
        {
            flags |= System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace;
        }

        if ((optionen & MusterMehrzeilig) != 0)
        {
            flags |= System.Text.RegularExpressions.RegexOptions.Singleline;
        }

        try
        {
            return new Muster
            {
                Engine = new System.Text.RegularExpressions.Regex(quelle, flags),
                Quelle = quelle,
                Optionen = optionen,
            };
        }
        catch (System.ArgumentException e)
        {
            _diagnostics.Add(
                "the pattern /" + quelle + "/ could not be put together ("
                    + e.Message.Split('\n')[0] + "); a game that writes a "
                    + "pattern the reader cannot build would have a silent "
                    + "answer here, and a name it can no longer find");
            return null;
        }
    }



    /// <summary>
    /// The methods a pattern has, and the operators it takes part in.
    /// </summary>
    /// <param name="pEmpfaenger">The text or the pattern.</param>
    /// <param name="pMethode">The method's name, or the operator.</param>
    /// <param name="pArgumente">The other side, already evaluated.</param>
    /// <returns>The answer, or null when this is not one of them.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Und `name =~ /held/` steht in fast jedem XP-Skript.</strong>
    /// Es ist die Form, mit der ein Plugin eine Dateinamen findet, einen
    /// Ereignisnamen erkennt und einen Schluessel im Speicher sucht,
    /// <strong>und ohne sie hat ein Spiel keine Moeglichkeit, Text zu
    /// pruefen.</strong>
    /// </para>
    /// <para>
    /// <strong>Und `=~` gibt die Stelle, nicht wahr oder falsch.</strong>
    /// Ruby gibt den Index zurueck,
    /// <strong>und ein Leser, der ein true/false lieferte, wuerde einem
    /// Spiel, das `if s =~ /x/ then s.slice($~...)` schreibt, den Index
    /// wegnehmen** — and a game that reads what it just matched would read
    /// nothing.
    /// </para>
    /// <para>
    /// <strong>Und `nil` heisst "kein Treffer", nicht "Fehler".</strong>
    /// `s !~ /x/` ist dann wahr,
    /// <strong>und das ist die Form, mit der ein Skript einen Namen
    /// ausschließt.</strong>
    /// </para>
    /// </remarks>
    private RubyValue? MusterMethode(
        RubyValue pEmpfaenger, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        // **Und beides kann die linke Seite sein.** `"held" =~ /held/`
        // **und** `/held/ =~ "held"` sind dasselbe in Ruby,
        // **und ein Leser, der nur die eine Form kannte, haette die andere
        // abgelehnt** -- **und ein Skript schreibt sie in beiden
        // Richtungen**, je nachdem, was es schon in der Hand hat.
        var text = pEmpfaenger;
        var muster = Erste(pArgumente);
        if (pEmpfaenger.Kind == RubyValueKind.Regexp)
        {
            muster = pEmpfaenger;
            text = pArgumente.Count > 0 ? pArgumente[0] : RubyValue.Nil;
        }
        else if (pArgumente.Count > 0
            && pArgumente[0].Kind == RubyValueKind.Regexp)
        {
            muster = pArgumente[0];
        }

        var vergleich = pMethode is "match" or "match?" or "scan" or "=~" or "!~";
        if (!vergleich)
        {
            return null;
        }

        if (text.Kind != RubyValueKind.String)
        {
            // **Und `=~` auf einer Zahl ist 0, nicht nil.** Ruby gibt
            // zurueck, dass nicht gepasst wurde,
            // **und ein Spiel, das eine Nummer prueft, will `0` sehen und
            // nicht einen Fehler** -- **das ist der Satz, mit dem ein
            // Skript eine ID prueft, die keine ID ist.**
            return pMethode is "!~" or "match?"
                ? RubyValue.OfBoolean(text.Kind == RubyValueKind.Nil
                    || text.Kind == RubyValueKind.Integer)
                : RubyValue.OfInteger(0);
        }

        if (muster.Kind != RubyValueKind.Regexp)
        {
            _diagnostics.Add(
                pMethode + " needs a pattern on one side, and got "
                    + (muster.IsNil ? "nothing" : muster.Kind.ToString())
                    + "; a game that compares a text with a text is looking "
                    + "for equality, and `=~` is not that");
            return RubyValue.Nil;
        }

        var gebaut = MusterBauen(muster);
        if (gebaut?.Engine == null)
        {
            return RubyValue.Nil;
        }

        var inhalt = System.Text.Encoding.UTF8.GetString(text.Bytes);
        if (!gebaut.LaufZaehlt(inhalt.Length))
        {
            // **Und die Schranke sagt, welches Muster es war.** Ohne den
            // Namen ist die Meldung eine Warnung vor nichts,
            // **und mit ihm kann ein Skriptautor die Zeile finden, die
            // haengt.**
            _diagnostics.Add(
                "the pattern /" + gebaut.Quelle + "/ was not run on "
                    + inhalt.Length + " bytes, because this reader runs a "
                    + "pattern that came from a script only while the text "
                    + "stays under 4096 bytes; a pattern that runs long is a "
                    + "way to stop a game, and a reader without a bound "
                    + "gives that away");
            return pMethode is "!~" or "match?"
                ? RubyValue.OfBoolean(true)
                : RubyValue.Nil;
        }

        var treffer = gebaut.Engine.Match(inhalt);

        // **Und jeder Lauf merkt sich, was er gefunden hat -- oder dass er
        // nichts gefunden hat.** Ruby setzt `$~` bei einem Treffer und nil
        // bei einem Fehlschlag,
        // **und ein Leser, der den alten Treffer stehen laesse, haette ein
        // Skript, das nichts fand und die Zahl der vorigen Zeile las.**
        _letzterTreffer = treffer.Success
            ? new TrefferDaten
            {
                Getroffen = true,
                Vorher = inhalt[..treffer.Index],
                Nachher = inhalt[(treffer.Index + treffer.Length)..],
                Ganz = treffer.Value,
                Stelle = treffer.Index,
                Gruppen =
                [
                    .. Enumerable.Range(1, Math.Max(0, treffer.Groups.Count - 1))
                        .Select(n => treffGruppe(treffer, n)),
                ],
                GruppenDa =
                [
                    .. Enumerable.Range(1, Math.Max(0, treffer.Groups.Count - 1))
                        .Select(n => treffer.Groups[n].Success),
                ],
            }
            : null;

        switch (pMethode)
        {
            case "=~":
                return RubyValue.OfInteger(treffer.Success ? treffer.Index : -1);
            case "!~":
                return RubyValue.OfBoolean(!treffer.Success);
            case "match?":
                return RubyValue.OfBoolean(treffer.Success);
            case "match":
                if (!treffer.Success)
                {
                    return RubyValue.Nil;
                }

                // **Und `match` mit einem Block ruft ihn fuer jeden
                // Treffer.** Das ist die Form, mit der ein Spiel alle
                // Namen aus einem Text zieht,
                // **und ohne sie ginge nur der erste.**
                return treffer.Value == string.Empty
                    && treffer.Length == 0
                        ? RubyValue.Nil
                        : Text(treffer.Value);
            default:
                return Scan(gebaut, inhalt, pArgumente, text);
        }
    }

    /// <summary>
    /// One group of a match, or nothing when it did not take part.
    /// </summary>
    /// <param name="pTreffer">The match.</param>
    /// <param name="pNummer">Which group.</param>
    /// <returns>The group's text, and empty when it did not take part.</returns>
    /// <remarks>
    /// <strong>Und eine Gruppe, die nicht teilgenommen hat, ist nicht dasselbe
    /// wie eine leere.</strong> `(a)(z)?` gegen `a` hat eine zweite Gruppe,
    /// die nicht gepasst hat,
    /// <strong>und Ruby gibt da nil und nicht ""** — **und ein Skript, das
    /// <c>$2</c> prueft, will wissen, ob es eine zweite Gruppe gab**, nicht,
    /// ob sie leer war.
    /// </remarks>
    private static string treffGruppe(
        System.Text.RegularExpressions.Match pTreffer, int pNummer)
        => pTreffer.Groups[pNummer].Success ? pTreffer.Groups[pNummer].Value : string.Empty;

    /// <summary>
    /// Every place a pattern matches, and what a game does with them.
    /// </summary>
    /// <param name="pMuster">The pattern.</param>
    /// <param name="pInhalt">The text.</param>
    /// <param name="pArgumente">The block, last.</param>
    /// <param name="pText">The text, which is the block's
    /// <c>self</c>.</param>
    /// <returns>The texts, or the grouped ones when a block asks for
    /// them.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Und `scan` gibt eine Liste, und ein Block aendert, was
    /// drin ist.</strong> `text.scan(/(\d+)/)` gibt die Ziffernfolgen,
    /// <strong>und mit einem Block gibt es die Gruppen statt der ganzen
    /// Treffer** — **und das ist der Satz, mit dem ein Spiel aus einem
    /// Ereignisnamen die Nummer zieht.**
    /// </para>
    /// </remarks>
    private RubyValue Scan(
        Muster pMuster,
        string pInhalt,
        IReadOnlyList<RubyValue> pArgumente,
        RubyValue pText)
    {
        var block = pArgumente.Count > 0
            && pArgumente[^1].Kind == RubyValueKind.Proc
            ? pArgumente[^1].Block
            : null;
        var treffer = pMuster.Engine.Matches(pInhalt);
        var gefunden = new List<RubyValue>();

        foreach (System.Text.RegularExpressions.Match treff in treffer)
        {
            // **Und mit einem Block ist die Antwort eine Liste von Listen.**
            // `scan` gibt die Gruppen zurueck, nicht die ganzen Treffer,
            // **und `text.scan(/x/) { }` gibt diese Liste zurueck und nicht
            // das, was der Block zurueckgibt** -- **das ist der Unterschied
            // zwischen `map` und `each_with_object`**,
            // **und ein Leser, der hier nil gabe, haette einem Skript, das
            // `scan` benutzt und die Liste liest, ein Loch gegeben.**
            var gruppen = treff.Groups.Count > 1 && treff.Groups[1].Success
                ? Enumerable.Range(1, treff.Groups.Count - 1)
                    .Select(n => Text(treff.Groups[n].Value))
                    .ToArray()
                : [Text(treff.Value)];

            if (block != null)
            {
                // **Und der Block bekommt die Gruppen und die Stelle.**
                // Die Stelle ist das letzte Argument,
                // **und sie steht dort, weil sie in Ruby auch dort
                // steht** -- ein Skript, das `|a, b| ` schreibt und
                // `Regexp.last_match` liest, erwartet dieselbe Reihenfolge.
                BlockAufrufen(
                    block,
                    [.. gruppen, RubyValue.OfInteger(treff.Index)],
                    pText);
            }

            gefunden.Add(RubyValue.OfArray(gruppen));
        }

        return RubyValue.OfArray(gefunden);
    }



    /// <summary>
    /// What a pattern matched, kept where a script can read it back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And without it a match is a number and nothing else.</strong>
    /// `s =~ /(\d+)/` answers where,
    /// <strong>and a script that then reads <c>$1</c> to get the number has
    /// nothing to read</strong> — which is the form every plugin uses to
    /// pull a number out of an event name, **and it is the form that made
    /// this engine worth building.**
    /// </para>
    /// <para>
    /// <strong>And it is a value, not a set of globals that happen to
    /// agree.</strong> <c>$~</c>, <c>$1</c>, <c>$&amp;</c>,
    /// <c>$'</c> and <c>Regexp.last_match</c> all read the same match,
    /// <strong>and five separate tables would be five chances for them to
    /// disagree</strong> — a game that reads <c>$~</c> and then <c>$1</c>
    /// would get two different answers.
    /// </para>
    /// <para>
    /// <strong>And it is set by every match and cleared by every
    /// failure.</strong> Ruby sets it on a hit and **nil on a miss**,
    /// <strong>and a reader that left the old match standing would have a
    /// script that matched nothing and read the previous line's
    /// number.</strong>
    /// </para>
    /// </remarks>
    private sealed class TrefferDaten
    {
        public string Vorher { get; init; } = string.Empty;

        public string Nachher { get; init; } = string.Empty;

        public string Ganz { get; init; } = string.Empty;

        public IReadOnlyList<string> Gruppen { get; init; } = [];

        /// <summary>
        /// Which groups took part, because "not there" and "there and
        /// empty" are different answers.
        /// </summary>
        /// <remarks>
        /// <strong>Und die Liste der Gruppen allein kann das nicht
        /// sagen.</strong> <c>(a)(z)?</c> gegen <c>"a"</c> hat zwei Gruppen,
        /// **und die zweite ist nicht getroffen** — **in der Liste steht
        /// da ""**, **und ein Skript, das <c>$2</c> liest, will wissen, ob
        /// es eine zweite Gruppe gab**, **und nicht, ob sie leer war.**
        /// **Gemessen: <c>$2</c> gab einen leeren Text und <c>$3</c> nil.**
        /// </remarks>
        public IReadOnlyList<bool> GruppenDa { get; init; } = [];

        public int Stelle { get; init; }

        public bool Getroffen { get; init; }
    }

    /// <summary>
    /// The last match as a value a script can ask questions of.
    /// </summary>
    /// <param name="pDaten">What matched, or null when nothing did.</param>
    /// <returns>The value.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Und es ist ein Objekt mit einem Klassennamen, und keine
    /// Liste.</strong> `$~[0]` ist der ganze Treffer,
    /// <strong>und `Members` ist die Karte, in der der Host seine eigenen
    /// Werte haelt** — **also steht hier die Trefferliste in `Felder` und
    /// der Klassenname traegt, dass es ein `MatchData` ist.**
    /// </para>
    /// <para>
    /// <strong>Und <c>[]</c> nimmt eine Zahl und gibt eine Gruppe.</strong>
    /// `$~[1]` ist die erste Gruppe,
    /// <strong>und eine Stelle, die es nicht gibt, ist nil** — **denn
    /// <c>"abc" =~ /(a)(z)?/</c> hat eine zweite Gruppe, die nicht
    /// teilgenommen hat**, **und ein Skript, das <c>$2</c> liest, will nil
    /// und nicht den leeren Text.**
    /// </para>
    /// </remarks>
    private RubyValue TrefferAlsWert(TrefferDaten? pDaten)
    {
        if (pDaten == null || !pDaten.Getroffen)
        {
            return RubyValue.Nil;
        }

        var werte = new List<RubyValue> { Text(pDaten.Ganz) };
        foreach (var gruppe in pDaten.Gruppen)
        {
            werte.Add(Text(gruppe));
        }

        return RubyValue.OfArray(werte);
    }

    /// <summary>
    /// One global's value, and the six names a pattern fills in.
    /// </summary>
    /// <param name="pNode">The global's node.</param>
    /// <returns>The value.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the one place that answers, because these six
    /// are not six globals.</strong> <c>$~</c>, <c>$1</c>, <c>$&amp;</c>,
    /// <c>$`</c>, <c>$'</c> and <c>Regexp.last_match</c> are six ways of
    /// asking the same question,
    /// <strong>and six tables would be six chances for them to
    /// disagree</strong> — a game that reads <c>$~</c> and then
    /// <c>$1</c> would get two different answers.
    /// </para>
    /// <para>
    /// <strong>And a number is a group.</strong> <c>$1</c> is the first
    /// group, <c>$2</c> the second,
    /// <strong>und ein Index, den es nicht gibt, ist nil** — **denn
    /// <c>"abc" =~ /(a)(z)?/</c> hat eine zweite Gruppe, die nicht
    /// teilgenommen hat.**
    /// </para>
    /// <para>
    /// <strong>And <c>$&amp;</c> is the whole match, not a number.</strong>
    /// Das ist der Satz, mit dem ein Skript den gefundenen Namen
    /// zurueckholt,
    /// <strong>und ein Leser, der dort eine Zahl gabe, wuerde ein Spiel
    /// haben, das die Stelle statt des Namens zurueckholt** — and a plugin
    /// that renames a file would rename it to a number.
    /// </para>
    /// </remarks>
    private RubyValue Global(RubyNode pNode)
    {
        var name = (pNode.Name ?? string.Empty).TrimStart('$');
        if (name == "~" || name == "&" || name == "`" || name == "'")
        {
            if (_letzterTreffer == null)
            {
                return RubyValue.Nil;
            }

            return name switch
            {
                "&" => Text(_letzterTreffer.Ganz),
                "`" => Text(_letzterTreffer.Vorher),
                "'" => Text(_letzterTreffer.Nachher),
                _ => TrefferAlsWert(_letzterTreffer),
            };
        }

        // **Und `$1` bis `$9` sind die Gruppen.** Ruby hat zehn,
        // **und diese Runtime hat so viele, wie das Muster Gruppen hat** --
        // **ein zehntes ohne zehnte Gruppe ist nil und nicht der leere
        /// Text.**
        if (name.Length > 0 && name.All(char.IsDigit)
            && int.TryParse(name, out var nummer))
        {
            if (_letzterTreffer == null)
            {
                return RubyValue.Nil;
            }

            return nummer >= 1 && nummer <= _letzterTreffer.Gruppen.Count
                && (_letzterTreffer.GruppenDa.Count < nummer
                    || _letzterTreffer.GruppenDa[nummer - 1])
                    ? Text(_letzterTreffer.Gruppen[nummer - 1])
                    : RubyValue.Nil;
        }

        return _globals.TryGetValue(name, out var wert) ? wert : RubyValue.Nil;
    }




    /// <summary>
    /// The text operations, which is how a game cuts a name apart.
    /// </summary>
    /// <param name="pEmpfaenger">The text.</param>
    /// <param name="pMethode">The method's name as written.</param>
    /// <param name="pArgumente">The arguments, already evaluated.</param>
    /// <returns>The answer, or null when this is not one of them.</returns>
    /// <remarks>
    /// <para>
    /// <strong>A game cuts a name apart before it draws it.</strong>
    /// <c>"Held A".split(" ")</c> is the first and the last name, and
    /// <c>gsub</c> is how a script removes a marker from a save-game key,
    /// <strong>and all of it was refused</strong> — a game's text window has
    /// nothing to show.
    /// </para>
    /// <para>
    /// <strong>And a text with a pattern in it is a separate question.</strong>
    /// A pattern from a game is data, and this reader does not run it
    /// without a time limit,
    /// <strong>because a pattern that runs long is a way to make a game stop
    /// and a reader that has no limit gives that away.</strong>
    /// </para>
    /// </remarks>
    private RubyValue? TextMethode(
        RubyValue pEmpfaenger, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        // **Und alles hier ist Text.** `5.split(",")` hat in Ruby keine
        // Bedeutung,
        // **und ein Leser, der die Zahl zurueckgaebe, haette einem Spiel
        // eine Liste gegeben, die es nie bat.**
        if (pEmpfaenger.Kind != RubyValueKind.String)
        {
            return null;
        }

        var text = System.Text.Encoding.UTF8.GetString(pEmpfaenger.Bytes);
        switch (pMethode)
        {
            case "split":
                return Geteilt(text, Erste(pArgumente));

            case "sub" or "gsub":
                return Ersetzt(text, pMethode, pArgumente);

            case "start_with?":
                return RubyValue.OfBoolean(
                    text.StartsWith(
                        AlsText(Erste(pArgumente)), System.StringComparison.Ordinal));

            case "end_with?":
                return RubyValue.OfBoolean(
                    text.EndsWith(
                        AlsText(Erste(pArgumente)), System.StringComparison.Ordinal));

            case "strip":
                return Text(text.Trim());

            case "lstrip":
                return Text(text.TrimStart());

            case "rstrip":
                return Text(text.TrimEnd());

            case "chomp":
                return Chomp(text);

            case "ljust" or "rjust" or "center":
                return Ausgerichtet(text, pMethode, pArgumente);

            case "chars":
                return Zeichen(text);

            // **Und `include?` steht hier und nicht in der
            // Sammlungsschicht.** Ein Text waere dort eine Liste mit Bytes,
            // **und `text.include?` wuerde die Bytes vergleichen und nie
            // finden, wonach das Spiel sucht** -- **und `Enthaelt` kann
            // beides, was `text.Contains` auch kann.**
            // **Und `include?` steht in der Sammlungsschicht, fuer die
            // Liste. Hier fuer den Text.**
            case "include?" or "member?":
                return RubyValue.OfBoolean(text.Contains(
                    AlsText(Erste(pArgumente)), System.StringComparison.Ordinal));

            // **`first` und `last` sind hier und nicht dort.** Die
            // Sammlungsschicht gibt fuer einen Text `Item` zurueck, **und
            // das ist null, weil ein Text keine Liste ist.**
            case "first":
                return text.Length == 0
                    ? RubyValue.Nil
                    : Text(text[..1]);

            case "last":
                return text.Length == 0
                    ? RubyValue.Nil
                    : Text(text[^1..]);

            case "count":
                return Gezaehlt(text, Erste(pArgumente));

            case "tr":
                return Uebersetzt(text, pArgumente);

            default:
                return null;
        }
    }

    /// <summary>A text, and nothing else.</summary>
    /// <param name="pText">The text.</param>
    /// <returns>The value.</returns>
    /// <remarks>
    /// <strong>Ein Helfer, weil er siebenmal gebraucht wird.</strong> Und weil
    /// jede Stelle, die den Text aus einem Wert holt, **die Frage
    /// beantworten muss, was sie mit einem Nicht-Text macht.**
    /// </remarks>
    private static RubyValue Text(string pText)
        => RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(pText));

    /// <summary>What a value says when it is asked for its text.</summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The text, and empty for a value that is not text.</returns>
    /// <remarks>
    /// <strong>Und leer und nicht nil.</strong> <c>"a" + 5</c> ist in Ruby
    /// ein Fehler, **und <c>"a".split(5)</c> auch**,
    /// **aber die leere Antwort ist hier die bessere**, weil sie eine
    /// Operation ergibt, die keine Bedeutung hat,
    /// **und der Fehler waere eine zweite Sache, die man melden muss.**
    /// </remarks>
    private static string AlsText(RubyValue pWert)
        => pWert.Kind == RubyValueKind.String
            ? System.Text.Encoding.UTF8.GetString(pWert.Bytes)
            : string.Empty;

    /// <summary>The text cut at a separator.</summary>
    /// <param name="pText">The text.</param>
    /// <param name="pTrenner">The separator.</param>
    /// <returns>The parts.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And an empty separator cuts between every character.</strong>
    /// <c>"abc".split("")</c> is three parts, and the game that splits a
    /// save-game key uses that,
    /// <strong>and a reader that made a separator out of nothing would have
    /// given one part</strong> — and the key would be one name where the
    /// script wrote three.
    /// </para>
    /// <para>
    /// <strong>And the empty parts at the end are gone.</strong>
    /// <c>"a,b,,".split(",")</c> is <c>["a", "b"]</c>,
    /// <strong>and a reader, das sie behielte, wuerde einem Spiel leere
    /// Namen in eine Liste haengen</strong> — and the menu would draw a blank
    /// row that the script never wrote.
    /// </para>
    /// </remarks>
    private RubyValue Geteilt(string pText, RubyValue pTrenner)
    {
        // **Und ein Muster wird nicht ausgefuehrt, und das wird
        // gesagt.** `split(/\s+/)` ist die Form, mit der ein Skript eine
        // Zeile in Worte teilt,
        // **und ein stilles nil wuerde so aussehen, als haette die Zeile
        // kein Trennzeichen gehabt** -- **und das Spiel haette eine Zeile
        // mit einem Wort, wo es drei erwartet.**
        if (pTrenner.Kind == RubyValueKind.Regexp)
        {
            _diagnostics.Add(
                "split was given the pattern /" + (pTrenner.Source ?? "")
                    + "/, and this reader does not run a pattern that came "
                    + "from a script without a time limit; a pattern that "
                    + "runs long is a way to stop a game, and a reader "
                    + "without a limit gives that away");
            return RubyValue.Nil;
        }

        if (pTrenner.Kind != RubyValueKind.String)
        {
            return RubyValue.Nil;
        }

        var trenner = AlsText(pTrenner);
        if (trenner.Length == 0)
        {
            // **Zwischen jedem Zeichen, und jedes Zeichen einzeln.**
            return RubyValue.OfArray(
                [.. pText.Select(z => RubyValue.OfBytes(
                    System.Text.Encoding.UTF8.GetBytes(z.ToString())))]);
        }

        var teile = pText.Split([trenner], StringSplitOptions.None)
            .Where(t => t.Length > 0)
            .Select(t => RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(t)));
        return RubyValue.OfArray([.. teile]);
    }



    /// <summary>The text with one or every part replaced.</summary>
    /// <param name="pText">The text.</param>
    /// <param name="pMethode">Either <c>sub</c> or <c>gsub</c>.</param>
    /// <param name="pArgumente">What to look for and what to put there.</param>
    /// <returns>The new text.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a pattern from a game is not run here.</strong>
    /// <c>gsub(/[0-9]/, "X")</c> takes a pattern,
    /// <strong>und diese Runtime fuehrt kein Muster aus einem Spiel aus,
    /// ohne eine Zeitgrenze** -- **ein Muster, das laeuft, ist ein Weg, ein
    /// Spiel anzuhalten, und ein Leser ohne Grenze gibt das preis.**
    /// </para>
    /// <para>
    /// <strong>So wird es gemeldet und nicht geraten.</strong> Ein Spiel, das
    /// `gsub` mit einem Muster schreibt, bekommt nil **und eine Diagnose,
    /// die das Muster nennt** -- **und ein stilles nil wuerde so aussehen,
    /// als haette das Spiel die Zeichenkette unveraendert zurueckbekommen**,
    /// was dieselbe Folge hat wie ein Fehler, den niemand liest.
    /// </para>
    /// <para>
    /// <strong>Und `sub` ersetzt das erste und `gsub` alle.</strong> Genau
    /// das ist der Unterschied,
    /// **und ein Leser, der beide gleich macht, wuerde aus einem Spiel, das
    /// eine Marke aus einem Namen entfernt, eines machen, das alle
    /// entfernt.**
    /// </para>
    /// </remarks>
    private RubyValue Ersetzt(
        string pText, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pArgumente.Count < 2)
        {
            _diagnostics.Add(
                pMethode + " needs what to look for and what to put there, and "
                    + "got " + pArgumente.Count + " arguments; a replacement "
                    + "without a search would change nothing and look like it "
                    + "worked");
            return RubyValue.Nil;
        }

        if (pArgumente[0].Kind == RubyValueKind.Regexp)
        {
            _diagnostics.Add(
                pMethode + " was given the pattern /"
                    + (pArgumente[0].Source ?? "") + "/, and this reader does "
                    + "not run a pattern that came from a script without a "
                    + "time limit; a pattern that runs long is a way to stop a "
                    + "game, and a reader without a limit gives that away");
            return RubyValue.Nil;
        }

        if (pArgumente[0].Kind != RubyValueKind.String)
        {
            return RubyValue.Nil;
        }

        var gesucht = AlsText(pArgumente[0]);
        var ersatz = AlsText(pArgumente[1]);
        if (gesucht.Length == 0)
        {
            return Text(pText);
        }

        // **Und `sub` ersetzt das erste, `gsub` alle.** Das ist der ganze
        // Unterschied, **und ein Leser, der beide gleich macht, wuerde aus
        // einem Spiel, das eine Marke aus einem Namen entfernt, eines
        // machen, das alle entfernt.**
        //
        // **Und eine leere Ersetzung braucht `Remove` und nicht
        // `Replace`.** `Replace` mit leerem Text tut in neueren
        // Laufzeiten nichts mehr,
        // **und ein Spiel, das eine Marke aus einem Namen streicht, haette
        // den Namen unveraendert behalten** -- und der Name, den das Spiel
        // in die Liste schreibt, waere ein anderer als der, den es sucht.
        return Text(pMethode == "gsub"
            ? pText.Replace(gesucht, ersatz, StringComparison.Ordinal)
            : ersatz.Length == 0
                ? pText.Remove(
                    pText.IndexOf(gesucht, StringComparison.Ordinal), gesucht.Length)
                : pText.Replace(gesucht, ersatz, StringComparison.Ordinal));
    }

    /// <summary>The text with its line ending taken off.</summary>
    /// <param name="pText">The text.</param>
    /// <returns>The text.</returns>
    /// <remarks>
    /// <strong>Und ein Zeilenumbruch und ein Wagenruecklauf sind
    /// zwei.</strong> Ruby 1.8 kennt "\r\n" auf jederPlattform,
    /// <strong>und ein Leser, der nur "\n" abschneidet, laesst auf Windows
    /// ein \r stehen** -- **und das \r steht in einer gespeicherten
    /// Einstellung, wenn das Spiel sie aus einer Datei liest.**
    /// </remarks>
    private static RubyValue Chomp(string pText)
    {
        // **Und "\r\n" ist EIN Zeilenende, und nicht zwei.** Die Klammern
        // um die erste Bedingung waren falsch gesetzt,
        // **und gemessen liess `"a\r\n".chomp` zwei Bytes statt einem**
        // -- **das \r blieb stehen**, **und ein Spiel, das eine Zeile aus
        // einer Datei liest, haette am Ende jedes Wortes ein \r**, das
        // in keinem Namen steht.
        // **Und "\r\n" sind ZWEI Bytes und EIN Zeilenende.**
        // `"a\r\n"` hat drei Bytes, **und `chomp` gibt `"a"` mit einem.**
        // **Die erste Fassung schnitt nur das \n ab und liess das \r
        // stehen** -- **gemessen: zwei Bytes statt einem.**
        if (pText.EndsWith("\r\n", StringComparison.Ordinal))
        {
            return Text(pText[..^2]);
        }

        return Text(pText.EndsWith('\r') || pText.EndsWith('\n')
            ? pText[..^1]
            : pText);
    }

    /// <summary>The text padded to a width.</summary>
    /// <param name="pText">The text.</param>
    /// <param name="pMethode">Which side the padding goes on.</param>
    /// <param name="pArgumente">The width and the text to pad with.</param>
    /// <returns>The padded text.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a text that is longer stays as it is.</strong> <c>ljust</c>
    /// never makes a text shorter,
    /// <strong>and a reader that cut to the width would take the name out of
    /// a menu's column</strong> — and the column is exactly what the padding
    /// is for.
    /// </para>
    /// <para>
    /// <strong>And a width that is not a number changes nothing.</strong> A
    /// script that computed the width from a value that was nil would ask
    /// for nothing,
    /// <strong>and a reader that guessed a width would put the name in a
    /// column of its own choosing</strong> — and a menu of five rows would
    /// draw them in a width nobody wrote.
    /// </para>
    /// </remarks>
    private static RubyValue Ausgerichtet(
        string pText, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pArgumente.Count == 0
            || pArgumente[0].Kind != RubyValueKind.Integer)
        {
            return Text(pText);
        }

        var breite = (int)pArgumente[0].Integer;
        if (breite <= pText.Length)
        {
            return Text(pText);
        }

        // **Und ein Fuellzeichen, das kein Text ist, ist ein Leerzeichen.**
        // `ljust(5, nil)` ist in Ruby ein Fehler,
        // **und diese Runtime gibt das Leerzeichen**, **weil eine Spalte
        // ohne Fuellung keinen Rand hat** -- und ein Spiel, das eine Zahl
        // in eine Spalte schreibt, will den Rand.
        var fuell = pArgumente.Count > 1 ? AlsText(pArgumente[1]) : " ";
        if (fuell.Length == 0)
        {
            fuell = " ";
        }

        return Text(pMethode switch
        {
            "rjust" => pText.PadLeft(breite, fuell[0]),
            "center" => pText.PadLeft((breite + pText.Length) / 2, fuell[0]),
            _ => pText.PadRight(breite, fuell[0]),
        });
    }

    /// <summary>The text as a list of its bytes.</summary>
    /// <param name="pText">The text.</param>
    /// <returns>The bytes.</returns>
    /// <remarks>
    /// <strong>Bytes, und nicht Zeichen.</strong> Ruby 1.8 kennt keine
    /// Zeichen, **und `chars` gibt in CP932 bei einem japanischen Namen
    /// halbe Kanji** -- **das ist Rubys Verhalten, und ein Leser, der hier
    /// Zeichen lieferte, wuerde einem Spiel mehr geben als die Referenz.**
    /// </remarks>
    private static RubyValue Zeichen(string pText)
        => RubyValue.OfArray(
            [.. pText.Select(b => RubyValue.OfInteger(b))]);

    /// <summary>How often a character stands in the text.</summary>
    /// <param name="pText">The text.</param>
    /// <param name="pGesucht">The character to look for.</param>
    /// <returns>The count.</returns>
    /// <remarks>
    /// <strong>Und es zaehlt Zeichen, nicht Bytes, weil `count` in Ruby so
    /// ist.</strong> <c>"aaa".count("a")</c> ist 3,
    /// **und ein Spiel, das damit eine Leiste zeichnet, will die Zeichen
    /// und nicht die Bytes.**
    /// </remarks>
    private static RubyValue Gezaehlt(string pText, RubyValue pGesucht)
    {
        if (pGesucht.Kind != RubyValueKind.String)
        {
            return RubyValue.OfInteger(0);
        }

        var gesucht = AlsText(pGesucht);
        if (gesucht.Length == 0)
        {
            return RubyValue.OfInteger(0);
        }

        var anzahl = 0;
        foreach (var z in pText)
        {
            if (gesucht.Contains(z, StringComparison.Ordinal))
            {
                anzahl++;
            }
        }

        return RubyValue.OfInteger(anzahl);
    }

    /// <summary>The text with one character replaced by another.</summary>
    /// <param name="pText">The text.</param>
    /// <param name="pArgumente">What to look for and what to put there.</param>
    /// <returns>The new text.</returns>
    /// <remarks>
    /// <strong>Und die Bereiche zaehlen, denn das ist der ganze
    /// Zweck.</strong> <c>tr("0-9a-z", "xxxxxxxxxx")</c> macht aus jedem
    /// Kleinbuchstaben ein <c>x</c> -- **und das ist der Satz, mit dem ein
    /// Spiel seinen Namen in eine Dateinamen-safe Form bringt**,
    /// <strong>und ein Leser, der Bereiche nicht kannte, wuerde jeden
    /// Buchstaben einzeln uebersetzen und nichts gezahlt haben, was
    /// anders ist.</strong>
    /// </remarks>
    /// <summary>
    /// Where a `tr` set has got to, which is what the source calls a
    /// <c>struct tr</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Und die drei Zahlen sind der ganze Bereich.</strong>
    /// <c>now</c> ist, wo er gerade ist, <c>max</c> ist, wo er endet, und
    /// <c>gen</c> sagt, ob er in einem Bereich ist,
    /// <strong>und ein Bereich liefert Zeichen fuer Zeichen** --
    /// **das ist der Grund, warum <c>tr("a-z", "x")</c> funktioniert und
    /// nicht nur das "a" trifft.**
    /// </remarks>
    private sealed class TrZeiger
    {
        public byte[] Bytes { get; init; } = [];

        public int Stelle { get; set; }

        public int Jetzt { get; set; }

        public int Max { get; set; }

        public bool ImBereich { get; set; }
    }

    /// <summary>One character out of a `tr` set, ranges included.</summary>
    /// <param name="pZeiger">The set, and where we are in it.</param>
    /// <returns>The byte, or -1 when the set is over.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Nach <c>trnext</c> in <c>string.c</c> aus Ruby 1.8.1.</strong>
    /// Das ist die Form, die der Ersetzer auch liest,
    /// <strong>und eine eigene Form wuerde einen zweiten Satz Regeln
    /// bedeuten</strong> -- <strong>und die beiden muessen zusammenpassen,
    /// weil <c>tr("a-z", "x")</c> auf beiden Seiten gelesen wird.</strong>
    /// </para>
    /// <para>
    /// <strong>Und <c>\</c> nimmt dem naechsten Zeichen den Sonderrang, und
    /// ein Bereich, dessen Ende vor seinem Anfang liegt, wird
    /// uebersprungen.</strong> Das ist Rubys Form,
    /// <strong>und in der Quelle ist es ein <c>continue</c> in genau
    /// diesem Fall.</strong>
    /// </para>
    /// </remarks>
    private static int Trnaechste(TrZeiger pZeiger)
    {
        while (true)
        {
            if (!pZeiger.ImBereich)
            {
                if (pZeiger.Stelle >= pZeiger.Bytes.Length)
                {
                    return -1;
                }

                if (pZeiger.Stelle < pZeiger.Bytes.Length - 1
                    && pZeiger.Bytes[pZeiger.Stelle] == '\\')
                {
                    pZeiger.Stelle++;
                    if (pZeiger.Stelle >= pZeiger.Bytes.Length)
                    {
                        return -1;
                    }
                }

                pZeiger.Jetzt = pZeiger.Bytes[pZeiger.Stelle++];
                if (pZeiger.Stelle < pZeiger.Bytes.Length - 1
                    && pZeiger.Bytes[pZeiger.Stelle] == '-')
                {
                    pZeiger.Stelle++;
                    if (pZeiger.Stelle < pZeiger.Bytes.Length)
                    {
                        if (pZeiger.Jetzt > pZeiger.Bytes[pZeiger.Stelle])
                        {
                            pZeiger.Stelle++;
                            continue;
                        }

                        pZeiger.ImBereich = true;
                        pZeiger.Max = pZeiger.Bytes[pZeiger.Stelle++];
                    }
                }

                return pZeiger.Jetzt;
            }

            if (++pZeiger.Jetzt < pZeiger.Max)
            {
                return pZeiger.Jetzt;
            }

            pZeiger.ImBereich = false;
            return pZeiger.Max;
        }
    }

    /// <summary>The text with one character replaced by another.</summary>
    /// <param name="pText">The text.</param>
    /// <param name="pArgumente">What to look for and what to put there.</param>
    /// <returns>The new text.</returns>
    /// <remarks>
    /// <strong>Und die Bereiche zaehlen, denn das ist der ganze
    /// Zweck.</strong> <c>tr("0-9a-z", "xxxxxxxxxx")</c> macht aus jedem
    /// Kleinbuchstaben ein <c>x</c> -- **und das ist der Satz, mit dem ein
    /// Spiel seinen Namen in eine Dateinamen-safe Form bringt**,
    /// <strong>und ein Leser, der Bereiche nicht kannte, wuerde jeden
    /// Buchstaben einzeln uebersetzen und nichts gezahlt haben, was
    /// anders ist.</strong>
    /// </remarks>
    /// <summary>One character out of a `tr` set, ranges included.</summary>
    /// <param name="pZeichen">The bytes, and where we are.</param>
    /// <returns>The byte, or -1 when the set is over.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Nach `trnext` in `string.c` aus Ruby 1.8.1.</strong> Das ist
    /// die Form, die der Ersetzer auch liest,
    /// <strong>und eine eigene Form wuerde einen zweiten Satz Regeln
    /// bedeuten** -- **und die beiden muessen zusammenpassen**, weil
    /// <c>tr("a-z", "x")</c> auf beiden Seiten gelesen wird.
    /// </para>
    /// <para>
    /// <strong>Und "a-z" ist ein Bereich von a bis z, und kein
    /// Zeichen.</strong> <c>\\</c> ist ein einfaches Zeichen,
    /// <strong>und ein Bereich, dessen Ende vor seinem Anfang liegt, wird
    /// uebersprungen** -- **das ist Rubys Form, und sie ist in der Quelle
    /// ein `continue` in genau diesem Fall.**
    /// </para>
    /// </remarks>
    private static int Trnaechste(byte[] pZeichen, ref int pStelle, ref int pMax)
    {
        while (true)
        {
            if (pStelle >= pZeichen.Length)
            {
                return -1;
            }

            if (pStelle < pZeichen.Length - 1 && pZeichen[pStelle] == '\\')
            {
                pStelle++;
                if (pStelle >= pZeichen.Length)
                {
                    return -1;
                }
            }

            var jetzt = pZeichen[pStelle++];
            if (pStelle < pZeichen.Length && pZeichen[pStelle] == '-'
                && pStelle + 1 < pZeichen.Length)
            {
                pStelle++;
                if (jetzt > pZeichen[pStelle])
                {
                    pStelle++;
                    continue;
                }

                pMax = pZeichen[pStelle++];
                return jetzt;
            }

            pMax = jetzt;
            return jetzt;
        }
    }

    /// <summary>The text with one character replaced by another.</summary>
    /// <param name="pText">The text.</param>
    /// <param name="pArgumente">What to look for and what to put there.</param>
    /// <returns>The new text.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Nach `tr_trans` in `string.c` aus Ruby 1.8.1: eine Tabelle
    /// ueber alle 256 Bytes.</strong> Kein Durchlauf ueber Paare,
    /// <strong>sondern jedes Byte des Textes wird einmal nachgesehen** --
    /// **und das ist der Grund, warum <c>tr("a-z", "x")</c> jeden
    /// Kleinbuchstaben zu einem x macht, statt ihn dreimal zu uebersetzen.**
    /// </para>
    /// <para>
    /// <strong>Und der Ersetzer wird pro Zeichen gelesen, und danach
    /// weiterverwendet.</strong> <c>tr("abc", "xy")</c> macht
    /// <c>a</c> zu <c>x</c>, <c>b</c> zu <c>y</c> und <c>c</c> wieder zu
    /// <c>y</c>, <strong>weil der Ersetzer nach <c>y</c> aufgebraucht ist
    /// und der letzte stehen bleibt** -- **in der Quelle ist das
    /// <c>if (r == -1) r = trrepl.now;</c>, und ohne das wuerde
    /// <c>c</c> unuebersetzt bleiben.**
    /// </para>
    /// <para>
    /// <strong>Und "^" am Anfang heisst "alles andere".</strong> Das ist der
    /// Satz, mit dem ein Spiel die erlaubten Zeichen eines Namens
    /// durchsetzt,
    /// <strong>und ein Leser ohne das wuerde jedes andere Zeichen
    /// behalten** -- und ein Spiel, das die Taste sperrt, sperrt sie nicht.
    /// </para>
    /// </remarks>
    private static RubyValue Uebersetzt(string pText, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pArgumente.Count < 2
            || pArgumente[0].Kind != RubyValueKind.String)
        {
            return Text(pText);
        }

        var von = System.Text.Encoding.UTF8.GetBytes(AlsText(pArgumente[0]));
        var nach = System.Text.Encoding.UTF8.GetBytes(AlsText(pArgumente[1]));
        if (von.Length == 0)
        {
            return Text(pText);
        }

        if (pText.Length == 0)
        {
            return RubyValue.Nil;
        }

        var tabelle = new int[256];
        Array.Fill(tabelle, -1);

        // **Und "^" heisst: alles ausser dem, was genannt wird.** Die
        // Quelle fuellt die Tabelle mit 1,
        // **setzt die genannten auf -1 und gibt allen anderen am Ende den
        // letzten Ersetzer** -- **und genau das fehlte hier**, **also
        // bekam jeder ungenannte Buchstabe ein Nullzeichen** statt des x,
        // **und ein Spiel, das die Taste mit `tr("^a-z", "x")` sperrt,
        // sperrt sie nicht.**
        var umkehren = von[0] == '^';
        if (umkehren)
        {
            Array.Fill(tabelle, 1);
        }

        // **Und der Ersetzer hat seinen eigenen Zeiger, weil er Zeichen
        // fuer Zeichen gelesen wird.** `tr("abc", "xy")` macht `a` zu `x`,
        // `b` zu `y` und `c` zu `y` wieder,
        // **weil der Ersetzer nach `y` aufgebraucht ist und der letzte
        // stehen bleibt** -- **in der Quelle ist das
        // `if (r == -1) r = trrepl.now;`**, **und ohne das bliebe `c`
        // unuebersetzt.**
        var vonZeiger = new TrZeiger { Bytes = von, Stelle = umkehren ? 1 : 0 };
        var nachZeiger = new TrZeiger { Bytes = nach };
        while (Trnaechste(nachZeiger) >= 0)
        {
            // **Der Ersetzer wird ganz gelesen, und sein letztes Zeichen
            // ist das, das danach fuer alle kommt.**
        }

        nachZeiger.Stelle = 0;
        nachZeiger.ImBereich = false;
        nachZeiger.Jetzt = 0;
        var letzte = nach.Length > 0 ? nach[0] : -1;
        while (Trnaechste(nachZeiger) >= 0)
        {
            letzte = nachZeiger.Jetzt;
        }

        vonZeiger.Stelle = umkehren ? 1 : 0;
        vonZeiger.ImBereich = false;
        nachZeiger.Stelle = 0;
        nachZeiger.ImBereich = false;
        var ecksatz = new List<int>();
        while (true)
        {
            var c = Trnaechste(vonZeiger);
            if (c < 0)
            {
                break;
            }

            var r = Trnaechste(nachZeiger);
            if (r < 0)
            {
                r = letzte;
            }

            if (umkehren)
            {
                ecksatz.Add(c);
            }

            // **Und ein Bereich fuellt jeden seiner Zeichen**, weil
            // `Trnaechste` sie einzeln liefert.
            tabelle[c & 0xff] = r;
        }

        if (umkehren)
        {
            foreach (var c in ecksatz)
            {
                tabelle[c] = -1;
            }

            // **Und alles andere bekommt den letzten Ersetzer.** Das ist die
            // Zeile `if (trans[i] >= 0) trans[i] = trrepl.now;` aus der
            // Quelle,
            // **und ohne sie bekommen die ungenannten Zeichen gar nichts.**
            for (var i = 0; i < 256; i++)
            {
                if (tabelle[i] >= 0)
                {
                    tabelle[i] = letzte;
                }
            }
        }

        // **Und ein leerer Ersetzer loescht die genannten Zeichen.** Das ist
        // der Fall, in dem die Quelle frueh zurueckkehrt,
        // **und es ist der Satz, mit dem ein Spiel unerlaubte Zeichen aus
        // einem Namen entfernt.**
        // **Und ein leerer Ersetzer loescht die genannten Zeichen -- und
        // dafuer braucht die Tabelle einen dritten Wert.**
        //
        // **Die Quelle loest das frueh:** `if (RSTRING(repl)->len == 0)
        // return rb_str_delete_bang(1, &src, str);` -- **sie geht gar
        // nicht durch die Tabelle**, **sondern loescht mit einem eigenen
        // Weg.** **Ein Leser, der die Tabelle dafuer benutzt, braucht
        // einen Wert, der "loeschen" heisst**, **und -1 ist schon "nicht
        // genannt"** -- **also loeschte der Zweig genau die
        // falschen.**
        //
        // **Der eigene Weg ist auch der kuerzere:** der Bereich wird
        // einmal aufgefächert,
        // **und ein Spiel, das `tr("^a-z", "")` schreibt, loescht alles
        // ausser den Buchstaben** -- was die Tabelle allein nicht kann.
        if (nach.Length == 0)
        {
            var loeschen = new bool[256];
            var loeschZeiger = new TrZeiger { Bytes = von, Stelle = umkehren ? 1 : 0 };
            var alle = new List<int>();
            while (true)
            {
                var c = Trnaechste(loeschZeiger);
                if (c < 0)
                {
                    break;
                }

                alle.Add(c);
            }

            foreach (var c in alle)
            {
                loeschen[c] = true;
            }

            for (var i = 0; i < 256; i++)
            {
                if (loeschen[i] == umkehren)
                {
                    tabelle[i] = -2;
                }
                else if (umkehren)
                {
                    tabelle[i] = -1;
                }
            }

            return Text(new string(pText
                .Where(z => !loeschen[z] || umkehren)
                .ToArray()));
        }

        // **Und ein Eintrag mit -2 heisst: das Zeichen faellt weg.** Es
        // steht nicht in der Quelle,
        // **und ohne ein eigenes Zeichen dafuer muesste die Ausgabe
        // entscheiden, ob -1 "behalten" oder "loeschen" heisst** --
        // **und das ist genau die zweite Bedeutung, die ein Wert nicht
        // haben darf.**
        var ergebnis = new System.Text.StringBuilder(pText.Length);
        foreach (var b in System.Text.Encoding.UTF8.GetBytes(pText))
        {
            var c = tabelle[b];
            if (c == -2)
            {
                continue;
            }

            ergebnis.Append(c >= 0 ? (char)c : (char)b);
        }

        return Text(ergebnis.ToString());
    }




    /// <summary>
    /// The methods of a list, a hash and a string, which is what every
    /// menu is built from.
    /// </summary>
    /// <param name="pEmpfaenger">The value.</param>
    /// <param name="pMethode">The method's name as written.</param>
    /// <param name="pArgumente">The arguments, already evaluated.</param>
    /// <returns>The answer, or null when this is not one of them.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Thirty-six of the forty-one names a game writes in its first
    /// hundred lines were missing.</strong> <c>length</c> alone stops every
    /// menu that counts, <c>[0]</c> stops every list that reads its first
    /// entry, <strong>and a game without those is not a game that is
    /// slightly broken -- it is a game that does not start.</strong>
    /// </para>
    /// <para>
    /// <strong>And the host cannot answer them.</strong> A real host
    /// implements the game's own objects -- <c>Sprite</c>,
    /// <c>Window_Base</c>, <c>Input</c> --
    /// <strong>and not Ruby's <c>Array</c> and <c>String</c></strong>,
    /// because a game never asks the host what an array is.
    /// </para>
    /// <para>
    /// <strong>And a script's own method wins.</strong> <c>Game_Party#size</c>
    /// is the game's own answer,
    /// <strong>and a reader that asked this first would have given every
    /// party a list's length</strong> -- a number from somewhere else that
    /// looks right.
    /// </para>
    /// </remarks>
    private RubyValue? SammlungMethode(
        RubyValue pEmpfaenger, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        // **Und `is_a?` steht nicht hier, denn es hat seinen eigenen Weg.**
        // Es ging an `respond_to?`, **und dort wird die Kette der eigenen
        // Klasse beantwortet** -- **zwei Orte fuer eine Frage wuerden zwei
        // Antworten sein**, **und die alte Fassung kannte kein Objekt**,
        // weil es keines gab.
        switch (pMethode)
        {
            case "[]":
                return Index(pEmpfaenger, pArgumente);

            case "length" or "size":
                return Laenge(pEmpfaenger);

            case "empty?":
                return RubyValue.OfBoolean(Laenge(pEmpfaenger).Integer == 0);

            case "first":
                return Item(pEmpfaenger, 0);

            case "last":
                return Item(pEmpfaenger, Laenge(pEmpfaenger).Integer - 1);

            case "push" or "<<" or "append":
                return rangeErweitert(pEmpfaenger, pArgumente);

            // **Und `include?` steht hier, fuer die Liste.** Fuer einen Text
            // steht es in `TextMethode`,
            // **und die Sammlungsschicht gibt fuer einen Text nil**, weil
            // **ein Text keine Liste ist** -- **und das ist die ganze
            // Trennung: diese Schicht kennt Listen, die andere kennt
            // Texte, und keine von beiden nimmt der anderen ihren Namen
            // weg.**
            case "include?" or "member?":
                return pEmpfaenger.Kind == RubyValueKind.String
                    ? null
                    : Enthaelt(pEmpfaenger, Erste(pArgumente));

            case "index" or "find_index":
                return Stelle(pEmpfaenger, Erste(pArgumente));

            case "delete":
                return rangeEntfernt(pEmpfaenger, Erste(pArgumente));

            case "join":
                return Verbunden(pEmpfaenger, pArgumente);

            case "reverse":
                return RubyValue.OfArray([.. pEmpfaenger.Items.Reverse()]);

            case "uniq":
                return Eindeutig(pEmpfaenger);

            case "count":
                // **`count` ohne Block zaehlt alle.** Ruby auch,
                // **und ein Leser, der einen Block verlangte, wuerde einem
                // Spiel, das `liste.count` schreibt, eine leere Antwort
                // geben** -- was aussieht, als waere die Liste leer.
                return pArgumente.Count > 0
                    ? rangeGezählt(pEmpfaenger, pArgumente)
                    : Laenge(pEmpfaenger);

            case "keys":
                return Schluessel(pEmpfaenger);

            case "values":
                return Werte(pEmpfaenger);

            case "to_i":
                return ZuGanzzahl(pEmpfaenger);

            case "to_f":
                return ZuReelle(pEmpfaenger);

            case "upcase":
                return Text(pEmpfaenger, true);

            case "downcase":
                return Text(pEmpfaenger, false);

            case "to_sym":
                return RubyValue.OfSymbol(
                    System.Text.Encoding.UTF8.GetString(pEmpfaenger.Bytes));

            case "times" or "upto" or "downto":
                return Wiederholt(pEmpfaenger, pMethode, pArgumente);

            default:
                return null;
        }
    }

    /// <summary>A count with a block run over it.</summary>
    /// <param name="pEmpfaenger">The number.</param>
    /// <param name="pMethode">The method's name.</param>
    /// <param name="pArgumente">The block, or the end of the range.</param>
    /// <returns>The number.</returns>
    /// <remarks>
    /// <para>
    /// <strong>`3.times` is how a menu draws three rows.</strong> Every
    /// status window, every item list and every party row is built with it,
    /// <strong>and a reader that had no `times` had no way to draw a row at
    /// all</strong> — the window would exist and be empty.
    /// </para>
    /// <para>
    /// <strong>And the number is the answer, not a list.</strong> Ruby
    /// returns the number,
    /// <strong>and a reader that returned a list would have made a game that
    /// checks the answer see a length where it wanted the count</strong> —
    /// which is a different number and looks like a number.
    /// </para>
    /// <para>
    /// <strong>And a block that stops the run stops it.</strong> `break`
    /// inside a block leaves the call,
    /// <strong>and a reader that ran every number anyway would have written
    /// past the end of the array the game sized from this.</strong>
    /// </para>
    /// </remarks>
    private RubyValue Wiederholt(
        RubyValue pEmpfaenger, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pEmpfaenger.Kind != RubyValueKind.Integer)
        {
            return RubyValue.Nil;
        }

        var block = pArgumente.Count > 0
            && pArgumente[^1].Kind == RubyValueKind.Proc
            ? pArgumente[^1].Block
            : null;
        if (block == null)
        {
            _diagnostics.Add(
                pMethode + " needs a block, and none came with it; a count "
                    + "without a block has nothing to run, and this reader "
                    + "does not invent one");
            return RubyValue.Nil;
        }

        // **Und `2.times` laeuft null und eins -- nicht zwei und eins.**
        // Die Schleife begann bei der Zahl selbst,
        // **also lief `2 <= 1` nie, und `times` tat gar nichts** --
        // **gemessen: `2.times { }` rief den Block nullmal.** `upto` und
        // `downto` dagegen beginnen bei der Zahl **und enden bei der, die
        // das Argument nennt.**
        var beginn = pMethode == "times" ? 0 : pEmpfaenger.Integer;
        var ende = pMethode == "times"
            ? pEmpfaenger.Integer - 1
            : pArgumente.Count > 1 && pArgumente[0].Kind == RubyValueKind.Integer
                ? pArgumente[0].Integer
                : pEmpfaenger.Integer;
        var schritt = pMethode == "downto" ? -1 : 1;
        for (var i = beginn;
            schritt > 0 ? i <= ende : i >= ende;
            i += schritt)
        {
            // **Und `self` ist der, in dem der Block geschrieben wurde.**
            // `3.times { |i| @zeilen = @zeilen.push(i) }` in einer Methode
            // **schreibt in die Felder dieses Objekts**,
            // **und `pEmpfaenger` waere die Zahl 3** -- **und ein Leser, der
            // die Zahl als `self` gibt, haette den Block auf einer Zahl
            // laufen lassen**, **wo `@zeilen` nichts ist.**
            BlockAufrufen(
                block, [RubyValue.OfInteger(i)], _self ?? pEmpfaenger);
        }

        return pEmpfaenger;
    }




    /// <summary>
    /// The methods that walk a list, which is how a game builds one from
    /// another.
    /// </summary>
    /// <param name="pEmpfaenger">The list.</param>
    /// <param name="pMethode">The method's name as written.</param>
    /// <param name="pArgumente">The arguments, with the block last.</param>
    /// <returns>The answer, or null when this is not one of them.</returns>
    /// <remarks>
    /// <para>
    /// <strong>`map` is the list a game is built from.</strong>
    /// `aktoren.map { |a| a.name }` is how a party becomes a menu,
    /// <strong>and a host that has no list of game objects to walk would
    /// have had nothing to walk</strong> — and the null host has nothing at
    /// all, <strong>so no test could show what a game's menu would say.</strong>
    /// </para>
    /// <para>
    /// <strong>And the block is the last argument, because that is where the
    /// language puts it.</strong> A block is not an ordinary argument,
    /// <strong>and a reader that read it as one would have bound the first
    /// value to the first parameter</strong> — which is right by accident
    /// and wrong the moment the game writes a second parameter.
    /// </para>
    /// <para>
    /// <strong>And each answers the list.</strong> Ruby returns the list,
    /// <strong>and a game that writes `liste.each { |x| x.hp += 1 }` reads
    /// the answer about half the time</strong> — and a reader that answered
    /// nil would have made a game's chain stop there.
    /// </para>
    /// </remarks>
    private RubyValue? ListenMethode(
        RubyValue pEmpfaenger, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pEmpfaenger.Kind != RubyValueKind.Object || !pEmpfaenger.IsList)
        {
            return null;
        }

        // **Und der NAME zuerst, und der Block danach.** Die Reihenfolge
        // entscheidet, was ein unbekannter Name sieht:
        // **bisher bekam `liste.length` die Meldung "length braucht einen
        // Block"** -- **und das ist doppelt falsch**, denn `length` ist
        // keine Methode, die einen Block laeuft, **und der Host haette die
        // richtige Antwort gehabt.**
        //
        // **Der Grund ist eine Fallunterscheidung, die zu frueh endet:**
        // "kein Block" wurde als "keine Liste" gelesen. **Ein Leser, der
        // erst prueft, OB es diese Methode gibt, und DANN fragt, ob ein
        // Block da ist, gibt einem Spiel die Meldung ueber die Methode,
        // die es schreibt** -- und die kann es lesen.
        if (pMethode is not ("map" or "select" or "filter" or "reject"
            or "each" or "each_with_index" or "reverse_each"
            or "any?" or "all?"))
        {
            return null;
        }

        var block = pArgumente.Count > 0
            && pArgumente[^1].Kind == RubyValueKind.Proc
            ? pArgumente[^1].Block
            : null;

        // **Und ein Aufruf ohne Block ist ein Fehler, kein leeres Ergebnis.**
        // `liste.map` ohne `{ }` **hat in Ruby keine Bedeutung**,
        // **und eine leere Liste als Antwort wuerde so aussehen, als haette
        // das Spiel eine leere gefunden.**
        if (block == null)
        {
            _diagnostics.Add(
                pMethode + " needs a block, and "
                    + "none came with it; a list method without a block has "
                    + "nothing to walk and this reader does not invent one");
            return RubyValue.Nil;
        }

        var ergebnis = new List<RubyValue>();
        foreach (var wert in pEmpfaenger.Items)
        {
            var aufgerufen = BlockAufrufen(block, [wert], pEmpfaenger);
            switch (pMethode)
            {
                case "map":
                    ergebnis.Add(aufgerufen);
                    break;

                case "select" or "filter":
                    if (Truthy(aufgerufen))
                    {
                        ergebnis.Add(wert);
                    }

                    break;

                case "each" or "each_with_index" or "reverse_each":
                    // **Und nichts wird gebaut, denn es gibt nichts zu
                    // bauen.** Der Block hat seine Arbeit getan,
                    // **und die Liste ist die Antwort.**
                    _ = aufgerufen;
                    break;

                case "reject":
                    if (!Truthy(aufgerufen))
                    {
                        ergebnis.Add(wert);
                    }

                    break;

                case "any?":
                    if (Truthy(aufgerufen))
                    {
                        return RubyValue.OfBoolean(true);
                    }

                    break;

                case "all?":
                    if (!Truthy(aufgerufen))
                    {
                        return RubyValue.OfBoolean(false);
                    }

                    break;

                default:
                    return null;
            }
        }

        return pMethode switch
        {
            "map" or "select" or "filter" or "reject" => RubyValue.OfArray(ergebnis),
            "each" or "each_with_index" or "reverse_each" => pEmpfaenger,
            "any?" => RubyValue.OfBoolean(false),
            "all?" => RubyValue.OfBoolean(true),
            _ => null,
        };
    }



    /// <summary>
    /// A comparison that the script answers, or null when it does not.
    /// </summary>
    /// <param name="pNode">The operator's node.</param>
    /// <param name="pOperator">The operator as written.</param>
    /// <returns>The answer, or null when the script has no rule.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is why a game's own rule is used at all.</strong>
    /// <c>Game_Actor#&lt;=&gt;</c> is how an RPG Maker says which actor is
    /// stronger, <strong>and every other comparison in the game has to agree
    /// with it</strong> — a `&lt;` that went to the built-in would have
    /// compared nothing, because the built-in knows numbers and strings and
    /// nothing else.
    /// </para>
    /// <para>
    /// <strong>And the four go through the one rule.</strong> `&lt;`, `&lt;=`,
    /// `&gt;` and `&gt;=` are the same comparison with a different question
    /// about the answer, <strong>and a reader that implemented each of them
    /// separately would have let a game write two rules and get two
    /// orders.</strong>
    /// </para>
    /// <para>
    /// <strong>And nil is false, and not an error.</strong> Ruby answers
    /// nil for a comparison it cannot make, and using that as false is what
    /// the language does, <strong>so a game that compares things it cannot
    /// compare gets a quiet no instead of a crash</strong> — which is what
    /// happens in the reference too.
    /// </para>
    /// </remarks>
    private RubyValue? VergleichMitSkript(RubyNode pNode, string pOperator)
    {
        var linkerWert = Evaluate(Operands(pNode)[0]);
        var eigene = EigeneMethode(linkerWert, "<=>");
        if (eigene == null)
        {
            // **Und ohne Regel geht es an den eingebauten**, der Zahlen und
            // Strings kennt. **Nicht an null**, denn `5 < 6` ist eine Zahl
            // und kein Objekt.
            return null;
        }

        var dreiwert = Aufrufen(
            eigene,
            [Evaluate(Operands(pNode)[1])],
            linkerWert.Kind == RubyValueKind.Object
                ? linkerWert.ClassName
                : _aktuellerTyp?.Name,
            linkerWert);

        // **Und `nil` heisst, dass es keine Antwort gab.** Das ist nicht
        // null als Rueckgabe, **sondern eine Antwort: false.**
        if (dreiwert.Kind != RubyValueKind.Integer)
        {
            return RubyValue.OfBoolean(false);
        }

        return pOperator switch
        {
            "<=>" => RubyValue.OfInteger(dreiwert.Integer),
            "<" => RubyValue.OfBoolean(dreiwert.Integer < 0),
            "<=" => RubyValue.OfBoolean(dreiwert.Integer <= 0),
            ">" => RubyValue.OfBoolean(dreiwert.Integer > 0),
            ">=" => RubyValue.OfBoolean(dreiwert.Integer >= 0),
            _ => null,
        };
    }



    /// <summary>
    /// The class variables of a class, made on first use.
    /// </summary>
    /// <param name="pKlasse">The class's name as written.</param>
    /// <returns>The table, the same one every time.</returns>
    /// <remarks>
    /// <para>
    /// <strong>One per class and shared by every object of it.</strong>
    /// <c>@@zaehler</c> is what a class counts its objects with,
    /// <strong>and a table per object would count nothing</strong> — every new
    /// object would start at zero, and a game that hands out ids from a class
    /// variable would hand out the same id twice.
    /// </para>
    /// <para>
    /// <strong>And it is a table per class and not one for the program.</strong>
    /// Two classes each keep their own, <strong>and one table for the whole
    /// program would have given a game one class's counter inside the
    /// other</strong> — which is a map's object count inside an actor's id.
    /// </para>
    /// </remarks>
    private Dictionary<string, RubyValue> KlassenFelder(string pKlasse)
    {
        if (!_klassenFelder.TryGetValue(pKlasse, out var felder))
        {
            felder = new Dictionary<string, RubyValue>(StringComparer.Ordinal);
            _klassenFelder[pKlasse] = felder;
        }

        return felder;
    }

    /// <summary>
    /// The table a node's variable belongs in.
    /// </summary>
    /// <param name="pNode">The variable's node.</param>
    /// <returns>The table.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the one place that decides.</strong> `@x` belongs to
    /// the object, <c>@@x</c> to the class,
    /// <strong>and five places that each asked "which is it" would be five
    /// answers</strong> — and two of them would disagree, which is how a
    /// write and a read end up in different tables.
    /// </para>
    /// <para>
    /// <strong>And a class variable outside a class is the top level's.</strong>
    /// Ruby says <c>@@x</c> at the top level belongs to <c>Object</c>,
    /// <strong>and a reader that refused it would have made a script that
    /// keeps a counter between two scenes fail at the first increment.</strong>
    /// </para>
    /// </remarks>
    private Dictionary<string, RubyValue> TabelleFuer(RubyNode pNode)
    {
        if (pNode.Kind != RubyNodeKind.ClassVariable)
        {
            return _instanceVariables;
        }

        // **Und die Klasse ist die, in der gerade laeuft, oder die des
        // Empfaengers.** `class D; @@x = 1; end` ist ein Feld von `D`,
        // **und `obj.m` ist ein Feld der Klasse des Objekts.**
        var klasse = _self is { Kind: RubyValueKind.Object } wert
            && !string.IsNullOrEmpty(wert.ClassName)
            ? wert.ClassName
            : _aktuellerTyp?.Name ?? string.Empty;
        return KlassenFelder(klasse);
    }



    /// <summary>The first argument, or nil.</summary>
    /// <param name="pArgumente">The arguments.</param>
    /// <returns>The first, or nil.</returns>
    /// <remarks>
    /// **One place, because "the first argument" is written five times.**
    /// <c>include?</c>, <c>delete</c>, <c>index</c> and <c>push</c> all mean
    /// it, <strong>and a reader that wrote the check four times would let
    /// one of them answer a different thing when a game passes no
    /// argument.</strong>
    /// </remarks>
    private static RubyValue Erste(IReadOnlyList<RubyValue> pArgumente)
        => pArgumente.Count > 0 ? pArgumente[0] : RubyValue.Nil;



    /// <summary>How many values a list, a hash or a string has.</summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The count.</returns>
    /// <remarks>
    /// <strong>Bytes and not characters for a string.</strong> Ruby 1.8
    /// has no character type, <c>"abc".length</c> is 3 and
    /// <c>"ä".length</c> is 2 in CP932,
    /// <strong>and a reader that counted characters would have given a
    /// game a different number on a different machine</strong> — and a
    /// Japanese game's own name would measure differently than the menu
    /// expects.
    /// </remarks>
    private static RubyValue Laenge(RubyValue pWert) => RubyValue.OfInteger(
        pWert.Kind == RubyValueKind.String
            ? pWert.Bytes.Length
            : pWert.Kind == RubyValueKind.Object ? pWert.Items.Count : 0);

    /// <summary>One value at a place, by Ruby's rules for the place.</summary>
    /// <param name="pWert">The list.</param>
    /// <param name="pStelle">The place as written, and it may be
    /// negative.</param>
    /// <param name="pStandard">What a place that is not a number
    /// means.</param>
    /// <returns>The value, or the standard.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a negative place counts from the end.</strong>
    /// <c>liste[-1]</c> is the last entry, <c>liste[-2]</c> the one before
    /// it, <strong>and a reader that took the number as written would have
    /// answered nil for every negative place</strong> — and the last actor
    /// of a party is written that way.
    /// </para>
    /// <para>
    /// <strong>And a place that is not there is nil, and not an
    /// error.</strong> Ruby answers nil,
    /// <strong>and a reader that refused would have made every script that
    /// reads one entry too many stop</strong> — which is how a save file
    /// with one more field than the game expects behaves.
    /// </para>
    /// </remarks>
    private static RubyValue Item(RubyValue pWert, long pStelle)
    {
        if (pWert.Kind != RubyValueKind.Object)
        {
            return RubyValue.Nil;
        }

        var anzahl = pWert.Items.Count;
        var stelle = pStelle < 0 ? anzahl + pStelle : pStelle;
        return stelle < 0 || stelle >= anzahl ? RubyValue.Nil : pWert.Items[(int)stelle];
    }

    /// <summary>One value at a place a game wrote.</summary>
    /// <param name="pWert">The value.</param>
    /// <param name="pArgumente">The arguments.</param>
    /// <returns>The value at the place.</returns>
    /// <remarks>
    /// <strong>And a range gives a list, because a game's menu
    /// uses it.</strong> <c>liste[0, 3]</c> is the first three, and
    /// <c>liste[1..2]</c> the second and third,
    /// <strong>and a reader that answered only the first would have made
    /// every window that draws a few rows show one.</strong>
    /// </remarks>
    private static RubyValue Index(RubyValue pWert, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pWert.Kind == RubyValueKind.String)
        {
            // **Ein String gibt einen String zurueck, und nicht eine Zahl.**
            // `"abc"[1]` ist `"b"`,
            // **und ein Leser, der die Zahl zurueckgibt, haette einem Spiel
            // einen Buchstaben in ein Namensfeld geschrieben.**
            //
            // **Und es ist EIN Byte, nicht ein Zeichen.** Ruby 1.8 kennt
            // keine Zeichenkette aus Zeichen, **sondern eine aus Bytes**,
            // und `"abc"[1]` ist deshalb genau ein Byte lang.
            // **In CP932 ist das bei einem japanischen Namen die halbe
            // Kanji** -- **und das ist Rubys Verhalten, kein Fehler hier**,
            // **denn ein Spiel, das `name[0]` schreibt, benutzt es, um ein
            // Zeichen einer Kanji-Zeichenkette zu bekommen, und CP932
            // braucht dafuer zwei Bytes.**
            var stelle = pArgumente.Count > 0 ? pArgumente[0].Integer : 0;
            if (stelle < 0)
            {
                stelle += pWert.Bytes.Length;
            }

            return stelle < 0 || stelle >= pWert.Bytes.Length
                ? RubyValue.Nil
                : RubyValue.OfBytes([pWert.Bytes[(int)stelle]]);
        }

        if (pWert.Kind != RubyValueKind.Object)
        {
            return RubyValue.Nil;
        }

        if (pArgumente.Count == 0)
        {
            return RubyValue.Nil;
        }

        // **Und ein Hash nimmt einen Schluessel, und das ist der ganze
        // Unterschied zu einer Liste.** `hash[:a]` findet den Wert,
        // `liste[:a]` findet nichts.
        if (pWert.IsHash)
        {
            // **Und ein Hash traegt Schluessel und Werte abwechselnd in
            // EINER Liste.** `{:a => 1}` ist `[a, 1]`,
            // **und der Schluessel steht an einer geraden Stelle**
            // -- **ein Leser, der die Liste als Paare laese, wuerde den
            // Schluessel nie finden**, **und `hash[:a]` gaebe nil fuer
            // einen Hash, den das Spiel selbst gebaut hat.**
            for (var i = 0; i + 1 < pWert.Items.Count; i += 2)
            {
                if (pWert.Items[i].Equals(pArgumente[0]))
                {
                    return pWert.Items[i + 1];
                }
            }

            return RubyValue.Nil;
        }

        // **Und von hinten, wenn die Stelle negativ ist -- das macht
        // `Item`.** `liste[-1]` ist der letzte Eintrag,
        // **und dieselbe Regel noch einmal hier zu schreiben waere eine
        // zweite Stelle, an der sie auseinanderlaufen kann.**
        // **Die Mutation "negative Stellen zaehlen nicht von hinten" hat
        // genau das bewiesen:** Sie hat `Item` geaendert und `Index`
        // nicht, **und kein Test hat es gemerkt**, **weil kein Test
        // `first` oder `last` mit einer negativen Stelle benutzt.**
        return pArgumente[0].Kind == RubyValueKind.Integer
            ? Item(pWert, pArgumente[0].Integer)
            : RubyValue.Nil;
    }



    /// <summary>A list with the values added.</summary>
    /// <param name="pListe">The list.</param>
    /// <param name="pArgumente">The values to add.</param>
    /// <returns>The longer list.</returns>
    /// <remarks>
    /// <strong>And a new list, because this runtime has no changeable
    /// values.</strong> Ruby changes the list in place and answers it,
    /// <strong>and a reader that answered the old list would have made
    /// `akteure.push(held)` do nothing visible</strong> — the game would
    /// add a member and see the same number of members.
    /// </remarks>
    private static RubyValue rangeErweitert(RubyValue pListe, IReadOnlyList<RubyValue> pArgumente)
    {
        var werte = pArgumente.Where(a => a.Kind != RubyValueKind.Proc).ToList();
        var drin = pListe.Items as List<RubyValue>;

        if (drin != null)
        {
            // **Und eine Liste, die eine echte Liste ist, wird an Ort und
            // Stelle laenger.** `akteure.push(held)` ist in Ruby eine
            // Aenderung an `akteure`,
            // **und ein Leser, der eine neue Liste zurueckgibt, macht aus
            // jeder Schleife, die etwas sammelt, eine, die nichts
            // sammelt** -- **denn `g.push(i)` verwirft die Antwort, wenn die
            // Liste, auf die sich `g` bezieht, nicht die ist, die
            // gewachsen ist.**
            //
            // **Gemessen, bevor das hier stand:** `3.times { |i| g.push(i) }`
            // **liess danach null Werte**, **und genau so baut jedes
            // Menue seine Zeilen.**
            drin.AddRange(werte);
            return pListe;
        }

        // **Und alles andere ist unveraenderbar, und bekommt eine neue
        // Liste.** Ein Hash traegt seine Paare in derselben Liste,
        // **und `hash.push(x)` waere in Ruby ein Fehler** -- **also gibt
        // hier eine neue Liste zurueck, und der Aufrufer sieht, dass er
        // nichts veraendert hat.**
        return RubyValue.OfArray([.. pListe.Items, .. werte]);
    }
    /// <summary>Whether a list or a text has a value in it.</summary>
    /// <param name="pWert">The list or the text.</param>
    /// <param name="pGesucht">The value to look for.</param>
    /// <returns>true when it is there.</returns>
    /// <remarks>
    /// <para>
    /// <strong>By value and not by identity.</strong>
    /// <c>liste.include?("Held")</c> is true for a list holding that text,
    /// <strong>and a reader that compared references would have answered
    /// false for every text a game looked for</strong> — and a menu that
    /// checks whether an actor is in the party would always say no.
    /// </para>
    /// <para>
    /// <strong>And a text is a text, not a list of bytes.</strong>
    /// <c>text.include?("eld")</c> is true,
    /// <strong>and a reader that made a text out of its bytes would look for
    /// the three bytes as three values and never find them</strong> — and a
    /// window's option list checks its tags this way.
    /// </para>
    /// </remarks>
    private static RubyValue Enthaelt(RubyValue pWert, RubyValue pGesucht)
    {
        if (pWert.Kind == RubyValueKind.String)
        {
            return RubyValue.OfBoolean(
                System.Text.Encoding.UTF8.GetString(pWert.Bytes).Contains(
                    AlsText(pGesucht), System.StringComparison.Ordinal));
        }

        if (pWert.Kind != RubyValueKind.Object)
        {
            return RubyValue.OfBoolean(false);
        }

        foreach (var wert in pWert.Items)
        {
            if (wert.Equals(pGesucht))
            {
                return RubyValue.OfBoolean(true);
            }
        }

        return RubyValue.OfBoolean(false);
    }




    /// <summary>Where a value stands in a list, or nil.</summary>
    /// <param name="pListe">The list.</param>
    /// <param name="pGesucht">The value to look for.</param>
    /// <returns>The place, or nil.</returns>
    /// <remarks>
    /// <strong>And the first place, and not the last.</strong>
    /// `liste.index(x)` is the first one,
    /// <strong>and a reader that returned the last would have made a game
    /// that removes by place remove the wrong actor</strong> — and the party
    /// would be one actor different from what the script asked for.
    /// </remarks>
    private static RubyValue Stelle(RubyValue pListe, RubyValue pGesucht)
    {
        if (pListe.Kind != RubyValueKind.Object)
        {
            return RubyValue.Nil;
        }

        for (var i = 0; i < pListe.Items.Count; i++)
        {
            if (pListe.Items[i].Equals(pGesucht))
            {
                return RubyValue.OfInteger(i);
            }
        }

        return RubyValue.Nil;
    }

    /// <summary>A list without the values that are there.</summary>
    /// <param name="pListe">The list.</param>
    /// <param name="pGesucht">The value to take out.</param>
    /// <returns>The shorter list.</returns>
    /// <remarks>
    /// <strong>And every one of them, and not the first.</strong>
    /// `liste.delete(x)` takes out all of them,
    /// <strong>and a reader that took out one would have left a second actor
    /// with the same name in the party</strong> — and the game would draw
    /// it and the script would not know why.
    /// </remarks>
    private static RubyValue rangeEntfernt(RubyValue pListe, RubyValue pGesucht)
        => RubyValue.OfArray(
            [.. pListe.Items.Where(w => !w.Equals(pGesucht))]);

    /// <summary>The values of a list as one text, or the texts between
    /// them.</summary>
    /// <param name="pListe">The list.</param>
    /// <param name="pArgumente">The text between the values.</param>
    /// <returns>The joined text.</returns>
    /// <remarks>
    /// <strong>And the text a game gave, and not a fixed one.</strong>
    /// `liste.join(", ")` puts a comma and a space between,
    /// <strong>and a reader that always joined without a text would have
    /// written a party's names as one word</strong> — and a menu would show
    /// "HeldHeldHeld".
    /// </remarks>
    private static RubyValue Verbunden(RubyValue pListe, IReadOnlyList<RubyValue> pArgumente)
    {
        var zwischen = pArgumente.Count > 0
            && pArgumente[0].Kind == RubyValueKind.String
            ? System.Text.Encoding.UTF8.GetString(pArgumente[0].Bytes)
            : string.Empty;
        var teile = pListe.Items.Select(WertAlsText);
        return RubyValue.OfBytes(
            System.Text.Encoding.UTF8.GetBytes(string.Join(zwischen, teile)));
    }

    /// <summary>A list with every value once.</summary>
    /// <param name="pListe">The list.</param>
    /// <returns>The shorter list.</returns>
    /// <remarks>
    /// <strong>And by value, keeping the first.</strong> Ruby keeps the
    /// first of each equal value,
    /// <strong>and a reader that kept the last would have changed which
    /// actor a game keeps</strong> — and the party would be a different
    /// character than the script wrote.
    /// </remarks>
    private static RubyValue Eindeutig(RubyValue pListe)
    {
        var gesehen = new List<RubyValue>();
        foreach (var wert in pListe.Items)
        {
            if (!gesehen.Any(g => g.Equals(wert)))
            {
                gesehen.Add(wert);
            }
        }

        return RubyValue.OfArray(gesehen);
    }

    /// <summary>How many of a list's values answer something.</summary>
    /// <param name="pListe">The list.</param>
    /// <param name="pArgumente">The block, last.</param>
    /// <returns>The count.</returns>
    /// <remarks>
    /// <strong>And only the ones that say yes.</strong> `liste.count { |x| x
    /// > 2 }` counts those,
    /// <strong>and a reader that counted all would have given a game the
    /// size of its party when it asked for the number of its living
    /// members.</strong>
    /// </remarks>
    private RubyValue rangeGezählt(RubyValue pListe, IReadOnlyList<RubyValue> pArgumente)
    {
        var block = pArgumente.Count > 0
            && pArgumente[^1].Kind == RubyValueKind.Proc
            ? pArgumente[^1].Block
            : null;
        if (block == null)
        {
            return Laenge(pListe);
        }

        var anzahl = 0;
        foreach (var wert in pListe.Items)
        {
            if (Truthy(BlockAufrufen(block, [wert], pListe)))
            {
                anzahl++;
            }
        }

        return RubyValue.OfInteger(anzahl);
    }

    /// <summary>The keys of a hash, in the order they were written.</summary>
    /// <param name="pWert">The hash.</param>
    /// <returns>The keys.</returns>
    /// <remarks>
    /// <strong>And every other value, because the keys and the values are
    /// in one list.</strong>
    /// <strong>And a hash that is not a hash has no keys</strong> — Ruby
    /// raises, and a nil here <strong>would let a game's settings screen run
    /// with an empty key list and write nothing.</strong>
    /// </remarks>
    private static RubyValue Schluessel(RubyValue pWert)
    {
        if (pWert.Kind != RubyValueKind.Object || !pWert.IsHash)
        {
            return RubyValue.Nil;
        }

        var schluessel = new List<RubyValue>();
        for (var i = 0; i < pWert.Items.Count; i += 2)
        {
            schluessel.Add(pWert.Items[i]);
        }

        return RubyValue.OfArray(schluessel);
    }

    /// <summary>The values of a hash, in the order they were written.</summary>
    /// <param name="pWert">The hash.</param>
    /// <returns>The values.</returns>
    /// <remarks>
    /// <strong>And the ones at the odd places.</strong> The keys sit at the
    /// even ones, <strong>and a reader that took the first of every two
    /// would have given a game's settings screen its keys where its values
    /// belong</strong> — and every setting would read as a name.
    /// </remarks>
    private static RubyValue Werte(RubyValue pWert)
    {
        if (pWert.Kind != RubyValueKind.Object || !pWert.IsHash)
        {
            return RubyValue.Nil;
        }

        var werte = new List<RubyValue>();
        for (var i = 1; i < pWert.Items.Count; i += 2)
        {
            werte.Add(pWert.Items[i]);
        }

        return RubyValue.OfArray(werte);
    }

    /// <summary>A string as the whole number at its start.</summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The number.</returns>
    /// <remarks>
    /// <strong>And it stops at the first thing that is not a
    /// digit.</strong> <c>"3 Abenteuer".to_i</c> is 3,
    /// <strong>and a reader that required the whole string to be a number
    /// would have given nil for every saved value a game writes next to a
    /// label</strong> — and a party would come back from its save with no
    /// level.
    /// </remarks>
    private static RubyValue ZuGanzzahl(RubyValue pWert)
    {
        if (pWert.Kind == RubyValueKind.Integer)
        {
            return pWert;
        }

        if (pWert.Kind != RubyValueKind.String)
        {
            return RubyValue.OfInteger(0);
        }

        var text = System.Text.Encoding.UTF8.GetString(pWert.Bytes);
        var anzahl = 0;
        if (anzahl < text.Length && (text[anzahl] == '-' || text[anzahl] == '+'))
        {
            anzahl++;
        }

        var ende = anzahl;
        while (ende < text.Length && char.IsDigit(text[ende]))
        {
            ende++;
        }

        return ende == anzahl || (ende == anzahl + 1 && !char.IsDigit(text[anzahl]))
            ? RubyValue.OfInteger(0)
            : RubyValue.OfInteger(long.Parse(
                text[..ende], System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>A string as the number at its start, with a fraction.</summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The number.</returns>
    /// <remarks>
    /// <strong>And the same stopping rule as the whole number.</strong>
    /// <c>"1.5x".to_f</c> is 1.5,
    /// <strong>and a reader that read the whole string would have given nil
    /// for a position a game wrote with its name next to it</strong> — and
    /// a sprite would be drawn off the screen.
    /// </remarks>
    private static RubyValue ZuReelle(RubyValue pWert)
    {
        if (pWert.Kind == RubyValueKind.Float)
        {
            return pWert;
        }

        if (pWert.Kind == RubyValueKind.Integer)
        {
            return RubyValue.OfReal(pWert.Integer);
        }

        if (pWert.Kind != RubyValueKind.String)
        {
            return RubyValue.OfReal(0);
        }

        var text = System.Text.Encoding.UTF8.GetString(pWert.Bytes);
        var anzahl = 0;
        if (anzahl < text.Length && (text[anzahl] == '-' || text[anzahl] == '+'))
        {
            anzahl++;
        }

        var ende = anzahl;
        while (ende < text.Length && char.IsDigit(text[ende]))
        {
            ende++;
        }

        if (ende < text.Length && text[ende] == '.')
        {
            var nach = ende + 1;
            var ziffern = nach;
            while (ziffern < text.Length && char.IsDigit(text[ziffern]))
            {
                ziffern++;
            }

            if (ziffern > nach)
            {
                ende = ziffern;
            }
        }

        return ende == anzahl
            ? RubyValue.OfReal(0)
            : RubyValue.OfReal(double.Parse(
                text[..ende], System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>A string in another case, or itself when it is not a
    /// string.</summary>
    /// <param name="pWert">The value.</param>
    /// <param name="pVergleich">How to compare, and ordinal is the only
    /// honest answer for a game's own text.</param>
    /// <param name="pHoch">Whether to go up.</param>
    /// <returns>The text.</returns>
    /// <remarks>
    /// <strong>And nothing at all when the value is not a string.</strong>
    /// `5.upcase` has no answer,
    /// <strong>and a reader that made a number into "5" would have given a
    /// game a name for a value it never had.</strong>
    /// </remarks>
    private static RubyValue Text(RubyValue pWert, bool pHoch)
    {
        if (pWert.Kind != RubyValueKind.String)
        {
            return RubyValue.Nil;
        }

        var text = System.Text.Encoding.UTF8.GetString(pWert.Bytes);
        return RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(
            pHoch ? text.ToUpperInvariant() : text.ToLowerInvariant()));
    }

    /// <summary>Whether a value is of a kind a script named.</summary>
    /// <param name="pWert">The value.</param>
    /// <param name="pName">The name as a symbol.</param>
    /// <returns>true when it matches.</returns>
    /// <remarks>
    /// <strong>And the chain, not only the class itself.</strong>
    /// <c>held.is_a?(Game_Character)</c> is true for a subclass,
    /// <strong>and a reader that compared the name would have said no</strong>
    /// — and every guard clause a game writes against its own base class
    /// would refuse its own objects.
    /// </remarks>
    private RubyValue GehoertZu(RubyValue pWert, RubyValue pName)
    {
        if (pName.Kind != RubyValueKind.Symbol)
        {
            return RubyValue.OfBoolean(false);
        }

        // **Und die Kette, nicht nur die Klasse selbst.** `Held < HeldBase`,
        // **und `held.is_a?(HeldBase)` ist wahr.**
        // **Ein Leser, der nur den Namen vergleicht, wuerde jeder
        // Wache in einem Skript gegen die eigene Basisklasse `nein`
        // sagen** -- **und jedes eigene Objekt waere fremd.**
        var gesucht = pName.Name ?? string.Empty;
        var klasse = pWert.Kind == RubyValueKind.Object ? pWert.ClassName : null;
        var grenze = 0;
        while (klasse != null && grenze < 64
            && _types.TryGetValue(klasse, out var typ))
        {
            if (typ.Name == gesucht)
            {
                return RubyValue.OfBoolean(true);
            }

            klasse = typ.Superclass;
            grenze++;
        }

        return RubyValue.OfBoolean(false);
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
        // **Und ein Objekt, das eine Klasse traegt, ist diese Klasse.**
        // Ohne diesen Zweig waere `held.name` bei einem Objekt ohne Typ
        // gelaufen, **und bei einem mit Typ haette es die Klasse des gerade
        // laufenden Codes benutzt -- also die des Aufrufers und nicht die
        // des Objekts.**
        // **Und nur `self` faellt auf die laufende Klasse zurueck.**
        // Ein String, eine Zahl und ein Symbol sind *Werte*,
        // **und Rubys Regel ist, dass ihre Methoden in *ihrer* Klasse
        // stehen** -- `"b" <=> "c"` ist `String#<=>`.
        //
        // **Ohne das haette `"b" <=> "c"` die laufende Klasse gefragt.**
        // In `Kachel#<=>` ist die laufende Klasse `Kachel`,
        // **also haette der Vergleich die Regel `Kachel#<=>` mit einem String
        // aufgerufen** -- **und die Regel haette wieder einen String
        // verglichen** -- **bis der Stapel ueberlief.**
        //
        // **Das ist kein Sonderfall, das ist die Regel:** eine Methode
        // gehoert zum Empfaenger, **und wenn der Empfaenger keine Klasse
        // traegt, hat er keine eigenen Methoden.**
        string? name;
        if (pReceiver.Kind == RubyValueKind.Object
            && !string.IsNullOrEmpty(pReceiver.ClassName))
        {
            name = pReceiver.ClassName;
        }
        else if (pReceiver.Kind == RubyValueKind.Symbol)
        {
            name = pReceiver.Name == "self" ? _aktuellerTyp?.Name : pReceiver.Name;
        }
        else if (pReceiver.Kind == RubyValueKind.Object)
        {
            // **Eine Liste und ein Hash sind auch Werte, und nicht `self`.**
            name = null;
        }
        else
        {
            name = null;
        }

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
        string? pKlasse,
        RubyValue? pEmpfanger = null)
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

        // **Und `self` ist der Empfaenger, mit seinen Feldern.** `@hp` in
        // einem Rumpf gehoert dem Objekt, **und ohne diesen Umschalter
        // schrieb jeder Konstruktor in den Speicher des Aufrufers** -- also
        // `Game_Actor.new(1)` in die Felder der Party.
        // **Und der alte Zustand kommt zurueck**, weil `Aufrufen` sich
        // selbst aufruft (`super`, `instance_eval`).
        var alterSelf = _self;
        var alterFelder = _instanceVariables;
        if (pEmpfanger != null
            && pEmpfanger.Kind == RubyValueKind.Object
            && !string.IsNullOrEmpty(pEmpfanger.ClassName))
        {
            // **Nur ein Objekt mit einem Typ hat eigene Felder.** `self` ist
            // ein Name, **und der hat keine** -- **aber der Aufrufer
            // gehoert zu einem Typ**, **und dessen Felder sind die, in
            // denen sein Rumpf schreibt.** Ohne das waere `attr_accessor`
            // auf der obersten Ebene ins Leere gelaufen, **und genau das
            // macht jedes Skript, das eine Klasse ueber
            // `Game_Character` erweitert.**
            _self = pEmpfanger;
            _instanceVariables = pEmpfanger.Felder;
        }

        _scopes.Add(new Dictionary<string, RubyValue>());

        // **Ein Splat in der Mitte oder am Anfang verschiebt, was die
        // Parameter danach bekommen.** `def m(*teile, letzte)` gibt `letzte`
        // den *letzten* Wert und nicht den ersten,
        // **und ein Leser, der von vorn bindet, wuerde `letzte` den ersten
        // geben** -- ein Spiel, das `m(1, 2, 3)` schreibt, wuerde dann
        // `letzte == 1` bekommen, **und genau das ist der Wert, den die
        // Liste auch enthaelt**, also faellt es nicht auf.
        var nachSammel = pMethode.SammelAb >= 0
            ? pMethode.Parameters.Count - pMethode.SammelAb
            : 0;

        for (var i = 0; i < pMethode.Parameters.Count; i++)
        {
            var parameter = pMethode.Parameters[i];

            // **Ein geliefertes Argument schlaegt den Vorgabewert, und ein
            // fehlendes nimmt ihn.** `def m(a, b = 2)` mit `m(1)` gibt `a`
            // die Eins und `b` die Zwei,
            // **und mit `m(1, 3)` gibt es die Drei.**
            // **Ohne diese Reihenfolge haette der Vorgabewert das gelieferte
            // Argument ueberschrieben**, und `m(1, 3)` haette `b` als Zwei
            // bekommen -- **ein Spiel, das einen Wert uebergibt, haette ihn
            // stillschweigend verloren.**
            // **Direkt in die neue Ebene und nicht mit `SetLocal`.**
            // `SetLocal` sucht von innen nach aussen und schreibt in die
            // Ebene, in der es den Namen findet, **und `Name` liest nur
            // `_scopes[^1]`.** Ein Parameter, der in eine aeussere Ebene
            // wanderte, **waere fuer den Rumpf unsichtbar**, und
            // `Name` liefe weiter in die Methoden-Suche -- **wo `x` als
            // Klasse behandelt wird und der Aufrufer zurueckkommt.**
            // Ein Spiel mit `def m(x = 9)` **haette den Vorgabewert nie
            // gesehen**, und nur fuer die Parameter, die eine Vorgabe
            // haben, weil die ohne Vorgabe vorher gebunden wurden.
            // **Und ein Parameter hinter dem Splat zaeht von hinten.**
            // Das ist der Unterschied zwischen `def m(*teile, letzte)` und
            // `def m(erste, *teile)`, **und beide kommen in echten
            // Skripten vor** -- der erste ist die Form, mit der ein Dispatcher
            // ein Pfadargument ans Ende stellt.
            var stelle = pMethode.SammelAb >= 0 && i >= pMethode.SammelAb
                ? pArgumente.Count - nachSammel + (i - pMethode.SammelAb)
                : i;
            if (stelle >= 0 && stelle < pArgumente.Count)
            {
                _scopes[^1][parameter] = pArgumente[stelle];
            }
            else if (pMethode.Vorgaben.TryGetValue(parameter, out var vorgabe))
            {
                // **Zur Aufrufzeit und nicht zur Definitionszeit.** Ein
                // Spiel schreibt `def m(a = rand(6))`,
                // **und genau deshalb steht dort ein Ausdruck und nicht eine
                // Zahl** -- waere er einmal berechnet worden, haette jeder
                // Aufruf dieselbe Zahl bekommen,
                // und das Waagerechte, fuer das er geschrieben wurde, haette
                // nie ausgeschlagen.
                //
                // **Und der Ausdruck laeuft in der Ebene des Aufrufers.**
                // Ein Vorgabewert, der eine Variable liest, **liest die des
                // Aufrufers** -- das ist Rubys Regel,
                // und ein Leser, der ihn in einem leeren Rahmen laufen
                // liesse, haette nil gelesen und waere der Variablen
                // ausweichen, die das Spiel geschrieben hat.
                _scopes[^1][parameter] = Evaluate(vorgabe);
            }
            else
            {
                // **Der Sammel kommt hier nicht vor.** `*rest` steht in
                // `Parameters` an seiner Stelle, **und wird nach der
                // Schleile gebunden** -- waere er in der Schleife, wuerde
                // er wie ein normaler Parameter behandelt,
                // **und `def m(a, *rest)` wuerde `rest` die zweite Zahl
                // geben statt der Liste aller uebrigen.**
                _scopes[^1][parameter] = RubyValue.Nil;
            }
        }

        // **Und jetzt der Sammel, aus seiner Position.** Ab der Stelle, an der
        // er in der Liste steht, **gehoeren die Argumente in eine Liste** --
        // und eine leere Liste, wenn keins uebrig ist, **denn `*rest` ohne
        // Werte ist eine leere Liste und nicht nil.** Ein Spiel, das
        // `teile.length` schreibt, **haette auf nil sonst keine Antwort.**
        // **Ohne `SammelAb >= 0`, und das ist gemessen.** Die Bedingung
        // stand hier, **und die Mutation, die sie abschaltete, liess den
        // Lauf gruen**: `SammelAb` ist `namen.Count` in dem Moment, in dem
        // `SammelParameter` gesetzt wird, **und `namen.Count` ist nie
        // negativ.** **Zwei Bedingungen, die dasselbe sagen, sind eine
        // Bedingung mit zusaetzlichem Code** -- und die zusaetzliche
        // Bedingung liest sich, als waere der Fall moeglich, in dem sie
        // nicht gilt.
        if (pMethode.SammelParameter != null)
        {
            // **Und er endet, wo die Parameter nach ihm beginnen.** `*teile,
            // letzte` gibt `teile` alles **bis auf den letzten Wert**, weil
            // `letzte` ihn braucht, **und ein Leser, der ihm alles gaebe,
            // wuerde den letzten Wert doppelt vergeben** -- einmal in der
            // Liste und einmal im Parameter.
            var bisHier = pArgumente.Count - nachSammel;
            var abHier = new List<RubyValue>();
            for (var r = pMethode.SammelAb; r < bisHier && r < pArgumente.Count; r++)
            {
                abHier.Add(pArgumente[r]);
            }

            _scopes[^1][pMethode.SammelParameter] = RubyValue.OfArray(abHier);
        }

        // **Und die Optionen, aus allen Argumenten mit einem `=>`.** Ruby
        // 1.8.1, VX und VX Ace geben sie als **Hash**,
        // **und der Hash ist der Punkt**: ein Spiel schreibt `f(k: 3)` und
        // liest `opts[:k]`, **und ein Leser, der eine Liste gibt, haette
        // einen Index von einem Paar, das es nicht gibt.**
        //
        // **Ein Wert geht an genau eine Stelle.** `f(1, 2, k: 3)` gibt `a`
        // die Eins, `*rest` die Zwei und `opts` den Drei.
        if (pMethode.OptionenParameter != null)
        {
            // **Alle Paare in EINEN Hash.** `f(a: 1, b: 2)` ergibt zwei
            // Paar-Hashes aus dem Parser, **und ein Spiel liest
            // `opts[:a]` und `opts[:b]` aus einem einzigen** -- **eine Liste
            // von Hashes haette zwei Dinge, wo das Skript eines erwartet.**
            var paare = new List<RubyValue>();
            foreach (var argument in pArgumente)
            {
                if (argument.Kind == RubyValueKind.Object && argument.IsHash)
                {
                    paare.AddRange(argument.Items);
                }
            }

            _scopes[^1][pMethode.OptionenParameter] = RubyValue.OfHash(paare);
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

        // **Und `self` und sein Feldspeicher kommen zurueck.** `Aufrufen`
        // richtet sich selbst auf (`super`, `instance_eval`, ein Handler
        // von `method_missing`), **und ohne diesen Satz waere nach dem
        // inneren Aufruf der aeussere auf einem fremden Objekt** -- ein
        // Spiel, das `super` schreibt, haette in der zweiten Haelfte seines
        // eigenen Rumpfes die Felder des anderen Objekts gesehen.
        _self = alterSelf;
        _instanceVariables = alterFelder;
        return wert;
    }

    /// <summary>
    /// Runs a block that was kept as a value, with the arguments it was
    /// called with.
    /// </summary>
    /// <param name="pBlock">The block node.</param>
    /// <param name="pArgumente">The arguments the call carried.</param>
    /// <param name="pSelbst">The block's own value, for <c>self</c>.</param>
    /// <returns>What the block's last statement answered.</returns>
    /// <remarks>
    /// <para>
    /// <strong>A frame of its own and not the caller's variables.</strong> A
    /// block does not see the locals of the method that built it, <strong>and a
    /// reader that ran the body on the caller's scope would have let a lambda
    /// read and change variables the game made local</strong> -- two calls of
    /// one lambda would then have shared a variable, and a lambda that outlived
    /// its method would have kept it alive.
    /// </para>
    /// <para>
    /// <strong>Fewer arguments than parameters, and never more.</strong> Ruby
    /// binds what it can and leaves the rest <c>nil</c>, <strong>and that is
    /// what lets a game write a two-parameter block for a call that carries
    /// one value</strong> -- the form a host's own callbacks arrive in.
    /// </para>
    /// <para>
    /// <strong>A block and not a method, so <c>return</c> stops here.</strong>
    /// <c>return</c> inside a block comes back to the call, <strong>and a
    /// reader that let it escape would have ended the whole program</strong>
    /// whenever a game wrote <c>liste.each { return 1 }</c>.
    /// </para>
    /// </remarks>
    private RubyValue BlockAufrufen(
        RubyNode pBlock, IReadOnlyList<RubyValue> pArgumente, RubyValue pSelbst)
    {
        var parameter = pBlock.Children.Count > 1 ? pBlock.Children[1] : null;
        var rumpf = pBlock.Children.Count > 2 ? pBlock.Children[2] : null;
        if (rumpf == null)
        {
            return RubyValue.Nil;
        }

        var tiefe = _scopes.Count;
        _scopes.Add(new Dictionary<string, RubyValue>());
        _blockGrenze.Add(tiefe);
        if (parameter != null)
        {
            var namen = BlockParameterNamen(parameter);
            for (var n = 0; n < namen.Count; n++)
            {
                SetLocal(
                    namen[n],
                    n < pArgumente.Count ? pArgumente[n] : RubyValue.Nil);
            }
        }

        // **`_returned` wird hier zur Block-Marke.** Die bestehende
        // Return-Behandlung kennt nur "das Programm ist zurueck", **und eine
        // Lambda, die zurueckgibt, wuerde damit das ganze Skript beenden**.
        //
        // **Und der Block gehoert auf die Kette, denn sein Rumpf laeuft jetzt.**
        // `define_method(:innen) { ... }` **im Rumpf eines anderen Blocks**
        // ist die Form, die ein Plugin-Layer schreibt, **und ohne diesen
        // Schritt saehe der innere Aufruf eine leere Kette** -- er haette
        // seinen eigenen Block nicht als Argument bekommen und gemeldet, es
        // gebe keinen. **Derselbe Block, zweimal gesehen**: einmal als
        // Mantel beim Erzeugen des Wertes und einmal als Kette beim
        // Ausfuehren.
        var altesSelbst = _aktuellerTyp;
        _returned = false;
        _blockKette.Add(pBlock);
        try
        {
            var letztes = RubyValue.Nil;
            foreach (var teil in Statements(rumpf))
            {
                letztes = Evaluate(teil);
                if (_returned)
                {
                    break;
                }
            }

            return letztes;
        }
        finally
        {
            _returned = false;
            _aktuellerTyp = altesSelbst;
            _blockKette.RemoveAt(_blockKette.Count - 1);
            _blockGrenze.RemoveAt(_blockGrenze.Count - 1);
            _scopes.RemoveRange(tiefe, _scopes.Count - tiefe);
        }
    }

    /// <summary>
    /// The names a block's parameter list binds.
    /// </summary>
    /// <param name="pParameter">The parameter node.</param>
    /// <returns>The names, in order.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Both spellings.</strong> A block writes <c>|x, y|</c> and a
    /// method writes <c>(x, y)</c>, and <strong>a reader that only knew the
    /// block's would have left every method's parameters empty</strong> -- the
    /// names sit in different nodes, and a block can carry either.
    /// </para>
    /// <para>
    /// <strong>An empty list binds nothing</strong>, which is the form a game
    /// writes for a block that only closes over what it already sees -- <strong>and
    /// a reader that invented a positional name for it would have bound the
    /// first argument to something the game never named.</strong>
    /// </para>
    /// </remarks>
    private static IReadOnlyList<string> BlockParameterNamen(RubyNode pParameter)
    {
        var namen = new List<string>();
        foreach (var teil in Statements(pParameter))
        {
            switch (teil.Kind)
            {
                case RubyNodeKind.Identifier:
                    namen.Add(teil.Name ?? string.Empty);
                    break;
                case RubyNodeKind.Assignment when teil.Children.Count >= 2
                        && teil.Children[0].Kind == RubyNodeKind.Identifier:
                    namen.Add(teil.Children[0].Name ?? string.Empty);
                    break;
                default:
                    break;
            }
        }

        return namen;
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
            // **Und `@@x` geht in die Klasse, nicht in das Objekt.**
            case RubyNodeKind.ClassVariable:
                TabelleFuer(ziel)[ziel.Name ?? string.Empty] = wert;
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

        // **Ein Block hat eine eigene Wand, und die steht ueber der der
        // Methode.** Ein Block sieht die lokalen Variablen der Methode
        // nicht, **und diese Schleife suchte bis zur Methodengrenze nach
        // aussen** -- das hiess: `lambda { x = 1 }` in einer Methode, in der
        // `x` schon 99 war, schrieb in die 99 hinein und liess den Wert des
        // Aufrufers veraendert. **Das ist der Unterschied zwischen einem
        // Block und eingefrorenem Zustand des Aufrufers.**
        //
        // **Innerhalb des Blocks wird weiter nach aussen gesucht**, weil ein
        // Block einen `while`-Rumpf und ein `begin` in sich tragen kann,
        // **und die sind kein eigener Rahmen, sondern Teil desselben
        // Rumpfes.** Die Wand ist der Rahmen, den `BlockAufrufen` gestellt hat.
        var grenze = _blockGrenze.Count > 0 ? _blockGrenze[^1] : _methodenGrenze;
        for (var i = _scopes.Count - 1; i >= grenze; i--)
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
            // **Und der Operator-Zuweisung folgt dieselbe Wahl**,
            // **denn `@@x += 1` ist eine Klassenvariable, die waehlt.**
            TabelleFuer(ziel)[ziel.Name ?? string.Empty] = neu;
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
            else if (ziel.Kind is RubyNodeKind.InstanceVariable
                or RubyNodeKind.ClassVariable)
            {
                TabelleFuer(ziel)[ziel.Name ?? string.Empty] = element;
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
        // **`lambda` und `proc` tragen einen Block ohne Aufruf.** Der
        // Parser baut aus `lambda { |x| x }` denselben Block-Knoten wie aus
        // `a.each { |x| x }`, **nur dass der Empfanger ein Bezeichner ist,
        // den es nicht gibt.** Ohne diesen Zweil wuerde der Block seinen
        // "Empfaenger" aufrufen, der Host wuerde Nein sagen, **und ein
        // Spiel, das eine Lambda schreibt, haette einen stillen nil dort,
        // wo es einen aufrufbaren Wert erwartet.**
        if (pNode.Kind == RubyNodeKind.Block
            && pNode.Children.Count >= 3
            && pNode.Children[0].Kind == RubyNodeKind.Identifier
            && pNode.Children[0].Name is "lambda" or "proc")
        {
            return RubyValue.OfBlock(pNode);
        }

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
        var vorgaben = new Dictionary<string, RubyNode>(StringComparer.Ordinal);
        var sammelName = string.Empty;
        var optionenName = string.Empty;
        var sammelAb = -1;
        if (parameter != null)
        {
            // **Die Parameterliste traegt jetzt auch Ausdruecke.**
            // `Statements` nimmt die Rolle `Body` und sonst die
            // Quellordnung -- **und fuer diesen Knoten ist die
            // Quellordnung richtig**, weil es keine Rollen gibt.
            //
            // **Und ein Eintrag mit einem Ausdruck ist ein Name UND ein
            // Wert.** `Statements(parameter).Select(t => t.Name)` haette
            // `b = 2` als Namen `b` genommen und den Ausdruck weggeworfen,
            // **und die Methode haette nie einen Vorgabewert gehabt** --
            // genau das, was der Doc dieses Abschnitts behauptete, bevor
            // der Parser ihn liefern konnte.
            var sammel = string.Empty;
            var optionen = string.Empty;
            foreach (var teil in Statements(parameter))
            {
                // **`*rest` und `**opts` sind keine Parameter und doch
                // Namen.** Sie stehen in derselben Liste und **werden an der
                // Stelle gebunden, wo sie stehen** -- ein `*rest` am Ende
                // nimmt die ueberzaehligen Werte, **und ein `*rest` am Anfang
                // nimmt auch die, die ein mittlerer Parameter nicht
                // bekommen hat**, weil die Liste erst ab dort beginnt.
                // **Und sie kommen NICHT in `namen`.** Sie stehen in der
                // Liste nur an der Stelle, an der der Sammel beginnt,
                // **und kaemen sie auch in `namen`, wuerde die Bindung sie
                // wie normale Parameter behandeln** -- `rest` in
                // `def m(a, *rest)` waere der zweite Parameter,
                // **bekaeeme `2` statt der Liste `[2, 3]`, und die Liste
                // waere genau eine Zahl lang.**
                if (teil.Kind == RubyNodeKind.BlockPass && teil.Name != null)
                {
                    sammel = teil.Name;
                    sammelAb = namen.Count;
                    continue;
                }

                if (teil.Kind == RubyNodeKind.Hash && teil.Name != null)
                {
                    optionen = teil.Name;
                    continue;
                }

                namen.Add(teil.Name ?? string.Empty);
                if (teil.Kind == RubyNodeKind.Assignment
                    && teil.Children.Count >= 2
                    && teil.Name != null)
                {
                    vorgaben[teil.Name] = teil.Children[1];
                }
            }

            sammelName = sammel;
            optionenName = optionen;
        }

        // **Ein `def` raeumt die Marke von `undef`.** Die Marke sitzt am
        // Namen und nicht am Methodenobjekt, **und ein Leser, der sie an
        // einem Objekt gehaelt haette, wuerde den Namen fuer immer tot
        // lassen** -- eine Klasse, die die Methode danach selbst schreibt,
        // wuerde nie wieder antworten.
        typ.Undefiniert.Remove(name);
        typ.Undefiniert.Remove("self." + name);
        typ.Methods[(pAufSelbst ? "self." : string.Empty) + name] =
            new RubyMethod
            {
                Name = name,
                IsOnSelf = pAufSelbst,
                Parameters = namen,
                Vorgaben = vorgaben,
                SammelParameter = sammelName.Length > 0 ? sammelName : null,
                SammelAb = sammelAb,
                OptionenParameter = optionenName.Length > 0 ? optionenName : null,
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
            // **Ein `undef` in dieser Klasse beendet den Weg.** Die Methode
            // ist nicht da und darf aus der Basis nicht kommen, **und ein
            // Leser, der nur den Tabelleneintrag loeschte, waere hier
            // weitergegangen und haette sie gefunden** -- das waere genau das
            // Gegenteil dessen, was `undef` sagt.
            if (typ.Undefiniert.Contains(pMethod))
            {
                return null;
            }

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

        // **Kein Rueckfall auf `method_missing`, und das ist Absicht.**
        // `FindMethod` beantwortet "hat die Kette diese Methode",
        // **und die vier Aufrufer fragen genau das**: `super`, `alias`, die
        // Suche nach einer Klassenmethode und `AufrufenMitName`.
        //
        // **Ein Rueckfall wuerde `super` in den Handler schicken.** Ein Spiel
        // mit `def self.method_missing` und einem `super` in einer Methode
        // haette dann den Handler aufgerufen, **wo Ruby `NoMethodError`
        // sagen wuerde** -- und `super` ist die Stelle, an der ein Handler am
        // wenigsten gehoert, **denn er ist fuer Namen gedacht, die es nicht
        // gibt, und `super` fragt nach einer Basisversion eines Namens, den
        // es sehr wohl gibt.**
        //
        // **Die Suche nach dem Handler laeuft in `Call`, und dort steht sie
        // auch.** `Call` fragt `MissingMethod` selbst, **und genau deshalb
        // sind die beiden getrennt**: eine Suche, die zurueckfaellt, und
        // eine Frage, die zurueckfaellt, sind zwei Regeln an zwei Stellen.
        return null;
    }
    /// <summary>
    /// Runs a `method_missing`, with the name the caller wrote as its first
    /// argument.
    /// </summary>
    /// <param name="pMethode">The handler.</param>
    /// <param name="pArgumente">The arguments the call carried.</param>
    /// <param name="pKlasse">The class the call was written in.</param>
    /// <param name="pName">The method's name as the caller wrote it.</param>
    /// <returns>What the handler answered.</returns>
    /// <remarks>
    /// <para>
    /// <strong>The name is the first argument and not the second.</strong>
    /// `method_missing(name, *args)` is the whole shape,
    /// <strong>and a reader that passed the caller's arguments unchanged
    /// would have handed the handler the first argument under the name
    /// `name`** — so a plugin that answers by name would have answered for
    /// the wrong command, silently, on every single one.
    /// </para>
    /// <para>
    /// <strong>The parameters come from the handler, and the name is not one
    /// of them.</strong> A handler written as `def self.method_missing(name,
    /// *args)` has one named parameter, <strong>and binding the call's first
    /// argument to it would have overwritten the name</strong> — the one
    /// value the handler exists to receive.
    /// </para>
    /// <para>
    /// <strong>And it answers nil without a word when it declines.</strong> A
    /// handler may return nil to mean "I do not know that one",
    /// <strong>and this runtime says so and moves on** — a game that probes
    /// for a method with a handler that declines is not an error.
    /// </para>
    /// </remarks>
    private RubyValue AufrufenMitName(
        RubyMethod pMethode,
        IReadOnlyList<RubyValue> pArgumente,
        string? pKlasse,
        string pName)
    {
        if (pMethode.Parameters.Count == 0)
        {
            // **Kein Parameter, und trotzdem der Name zuerst.** Ein Handler
            // `def self.method_missing; ...; end` **sieht den Namen nicht**,
            // und das ist sein Recht -- **aber das Spiel soll nicht daran
            // scheitern, und der Aufruf laeuft trotzdem.**
            var ohne = pArgumente.ToArray();
            return Aufrufen(pMethode, ohne, pKlasse, _self);
        }

        var mit = new RubyValue[pArgumente.Count + 1];
        mit[0] = RubyValue.OfSymbol(pName);
        for (var i = 0; i < pArgumente.Count; i++)
        {
            mit[i + 1] = pArgumente[i];
        }

        return Aufrufen(pMethode, mit, pKlasse, _self);
    }



    /// <summary>
    /// Whether the chain has a method under that name, with no
    /// `method_missing` fallback.
    /// </summary>
    /// <param name="pTypeName">The class to look in.</param>
    /// <param name="pMethod">The method's name.</param>
    /// <returns>true when a class in the chain has it.</returns>
    /// <remarks>
    /// <strong>The same walk as <c>FindMethod</c> and not the call.</strong>
    /// <c>FindMethod</c> answers a question about "can this call work",
    /// <strong>and this one answers "is there a method by that name"</strong> —
    /// two different questions, and only the first one may fall back to
    /// <c>method_missing</c>. **Sharing the walk and not the fallback is the
    /// whole point**, and a reader that called `FindMethod` here would have
    /// made <c>respond_to?</c> say yes to everything.
    /// </remarks>
    private bool HatMethode(string pTypeName, string pMethod)
    {
        var gesehen = new HashSet<string>(StringComparer.Ordinal);
        var name = pTypeName;
        while (name != null && _types.TryGetValue(name, out var typ) && gesehen.Add(name))
        {
            if (typ.Undefiniert.Contains(pMethod))
            {
                return false;
            }

            if (typ.Methods.ContainsKey(pMethod))
            {
                return true;
            }

            name = typ.Superclass;
        }

        return false;
    }
    /// <summary>
    /// Runs a block with `self` set to the class it was written against.
    /// </summary>
    /// <param name="pBlock">The block node, off the chain.</param>
    /// <param name="pEmpfaenger">The receiver the call was written on.</param>
    /// <returns>What the block's last statement answered.</returns>
    /// <remarks>
    /// <para>
    /// <strong>The point of `instance_eval` is the `self` it sets.</strong> A
    /// plugin writes `Klasse.instance_eval { def m; end }`
    /// <strong>so that the method belongs to that class and not to the class
    /// the script happened to be in</strong> — and a reader that ran the block
    /// where it stood would have put the method in the wrong table, and the
    /// game would call a method that is not there.
    /// </para>
    /// <para>
    /// <strong>It is `class_eval` here and the difference is a limit.</strong>
    /// `self` is the class, so an `instance_eval` on a class and a
    /// `class_eval` on it are the same act,
    /// <strong>and a game that needs an object would need an object
    /// model this runtime does not have.</strong> That is stated here because
    /// it is a limit and not a detail.
    /// </para>
    /// <para>
    /// <strong>The receiver wins over the class the call stands in.</strong>
    /// `A.instance_eval` must act on <c>A</c> even while another class's body
    /// is running, <strong>and a reader that used the current class would
    /// have patched whichever class happened to be open.</strong>
    /// </para>
    /// <para>
    /// <strong>And the block gets a frame of its own.</strong> Its parameters
    /// are bound and it does not see the caller's locals, the same as any
    /// block — <strong>and a reader that ran the body on the caller's scope
    /// would have let a game's block write over the variables of the method
    /// that built it.</strong>
    /// </para>
    /// </remarks>
    private RubyValue Ausgewertet(RubyNode pBlock, RubyValue pEmpfaenger)
    {
        var knoten = pBlock;
        var parameter = knoten.Children.Count > 1 ? knoten.Children[1] : null;
        var rumpf = knoten.Children.Count > 2 ? knoten.Children[2] : null;
        if (rumpf == null)
        {
            return RubyValue.Nil;
        }

        var laufenderTyp = _aktuellerTyp;
        var name = pEmpfaenger.Kind == RubyValueKind.Symbol
            && pEmpfaenger.Name != "self"
            && _types.ContainsKey(pEmpfaenger.Name)
            ? pEmpfaenger.Name
            : laufenderTyp?.Name;
        var ziel = name != null && _types.TryGetValue(name, out var gefunden)
            ? gefunden
            : laufenderTyp;

        var tiefe = _scopes.Count;
        _scopes.Add(new Dictionary<string, RubyValue>());
        _blockGrenze.Add(tiefe);
        if (parameter != null)
        {
            var namen = BlockParameterNamen(parameter);
            for (var n = 0; n < namen.Count; n++)
            {
                SetLocal(namen[n], RubyValue.Nil);
            }
        }

        _aktuellerTyp = ziel;
        try
        {
            var letztes = RubyValue.Nil;
            foreach (var teil in Statements(rumpf))
            {
                letztes = Evaluate(teil);
                if (_returned)
                {
                    break;
                }
            }

            return letztes;
        }
        finally
        {
            _returned = false;
            _aktuellerTyp = laufenderTyp;
            _blockGrenze.RemoveAt(_blockGrenze.Count - 1);
            _scopes.RemoveRange(tiefe, _scopes.Count - tiefe);
        }
    }




    /// <summary>
    /// Answers `respond_to?` for a name, over the host and the script.
    /// </summary>
    /// <param name="pEmpfaenger">The receiver the question was written on.</param>
    /// <param name="pName">The name asked about.</param>
    /// <param name="pMethode">The question, so a caller can tell it apart.</param>
    /// <returns>true when something would answer that name.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And `method_missing` is not an answer.</strong> Ruby's
    /// <c>respond_to?</c> takes a second argument that says whether to count
    /// <c>method_missing</c>, <strong>and the default is no</strong> — because
    /// the point of the question is to know whether a call will work without
    /// raising, <strong>and a class that answers everything through
    /// <c>method_missing</c> would say yes to everything and the question
    /// would be worthless.</strong>
    /// </para>
    /// <para>
    /// <strong>The host is asked too, and it has a list.</strong> A game asks
    /// <c>respond_to?(:draw)</c> about a host method,
    /// <strong>and a reader that only looked at the script would say no and a
    /// game would skip a feature the host does have.</strong>
    /// </para>
    /// <para>
    /// <strong>`is_a?` and `kind_of?` are about the class, and this runtime
    /// has no objects.</strong> A receiver that is a class name answers true
    /// for its own name and for `Object`-shaped questions,
    /// <strong>and anything else answers false</strong> — a reader that
    /// invented an object model here would have had to invent the classes too.
    /// </para>
    /// </remarks>
    private bool Antwortet(RubyValue pEmpfaenger, string pName, string pMethode)
    {
        if (pMethode is "is_a?" or "kind_of?")
        {
            // **Und jetzt geht die Kette, denn es gibt Objekte.**
            // `class Held < Basis; held.is_a?(Basis)` ist wahr,
            // **und ein Leser, der nur den eigenen Namen vergleicht,
            // wuerde jeder Wache in einem Skript gegen die eigene
            // Basisklasse `nein` sagen** -- **und jedes eigene Objekt
            // waere fremd.**
            //
            // **Ein Klassenname als Empfaenger ist die Klasse selbst,**
            // `Held.is_a?(Held)` ist wahr,
            // **und ein Objekt mit einem Klassennamen ist eine Instanz
            // davon** -- **beides derselbe Name, und es braucht nur eine
            // Antwort fuer beides.**
            var eigen = pEmpfaenger.Kind == RubyValueKind.Symbol
                && pEmpfaenger.Name != "self"
                    ? pEmpfaenger.Name
                    : pEmpfaenger.Kind == RubyValueKind.Object
                        ? pEmpfaenger.ClassName
                        : null;
            if (eigen == null)
            {
                return false;
            }

            var grenze = 0;
            while (grenze < 64 && _types.TryGetValue(eigen, out var typ))
            {
                if (typ.Name == pName)
                {
                    return true;
                }

                eigen = typ.Superclass;
                grenze++;
            }

            return false;
        }

        // **Der Host zuerst, und ueber seine eigene Liste.** Der Host weiss,
        // was er kann, **und ein Spiel fragt genau danach**.
        if (_host.KnownMethods.Contains(pName, StringComparer.Ordinal))
        {
            return true;
        }

        // **Und dann das Skript, ueber die Kette, und ueber `self.` mit.**
        var klasse = pEmpfaenger.Kind == RubyValueKind.Symbol
            && pEmpfaenger.Name != "self"
            && _types.ContainsKey(pEmpfaenger.Name)
            ? pEmpfaenger.Name
            : _aktuellerTyp?.Name;
        if (klasse == null)
        {
            return false;
        }

        // **Und `FindMethod` darf hier nicht benutzt werden, denn es faellt
        // am Ende auf `method_missing` zurueck.** Das wuerde `respond_to?`
        // zu "ja, solange die Klasse einen Handler hat" machen,
        // **und damit waere die Frage fuer jede Klasse mit einem Handler
        // nutzlos** -- genau das, wovor der Kommentar oben warnt. **Die
        // Kette wird deshalb ohne den Fallback abgegangen.**
        return HatMethode(klasse, pName) || HatMethode(klasse, "self." + pName);
    }




    /// <summary>
    /// A class's `method_missing`, when it has one and nothing else answered.
    /// </summary>
    /// <param name="pTypeName">The class the call was written in.</param>
    /// <param name="pMethod">The method's name.</param>
    /// <returns>The method, or null.</returns>
    /// <remarks>
    /// <para>
    /// <strong>It has to be a singleton, and that is the whole feature.</strong>
    /// `def self.method_missing(name)` is how a game writes one,
    /// <strong>and a reader that looked in the instance table would have
    /// found nothing</strong> — so a plugin that answers a hundred command
    /// names through `method_missing` would have been a class that refuses
    /// all of them.
    /// </para>
    /// <para>
    /// <strong>The name arrives as a value, not as a string in the
    /// signature.</strong> `method_missing(name, *args)` takes the name the
    /// caller wrote, <strong>and a reader that passed the method's own name
    /// would have told the game its own name instead of the one that was
    /// missing</strong> — which is the one piece of information the handler
    /// exists to give.
    /// </para>
    /// <para>
    /// <strong>It is looked up by name, so the search order matters and is
    /// the class's own first.</strong> A subclass that defines one overrides
    /// the base's, <strong>and a reader that took the first it found walking
    /// down would have had the base answer for a subclass that answered
    /// differently.</strong>
    /// </para>
    /// </remarks>
    private RubyMethod? MissingMethod(string? pTypeName, string pMethod)
    {
        var gesehen = new HashSet<string>(StringComparer.Ordinal);
        var name = pTypeName;
        while (name != null && _types.TryGetValue(name, out var typ) && gesehen.Add(name))
        {
            if (typ.Undefiniert.Contains("self.method_missing"))
            {
                return null;
            }

            if (typ.Methods.TryGetValue("self.method_missing", out var fehlend))
            {
                return fehlend;
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

        // **`super` behaelt `self`, denn es ist derselbe Aufruf eine Ebene
        // hoeher.** `Held#name` zeigt auf `Held2#name` geschrieben, **und
        // `super` dort etwas anderes gelesen, waere kein `super` mehr,
        // sondern ein Aufruf an eine fremde Instanz.**
        return Aufrufen(methode, argumente, typ.Superclass, _self);
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

    /// The built-ins that take a block as their last argument.
    /// </summary>
    /// <param name="pMethode">The method's name.</param>
    /// <returns>true when the call wants the block that carries it.</returns>
    /// <remarks>
    /// <para>
    /// <strong>A short list and not a rule about all calls.</strong> A block at
    /// an ordinary call belongs to that call and the host decides what to do
    /// with it, <strong>and appending it here would have handed
    /// <c>each</c> an argument it does not expect</strong> — a host that
    /// counts its arguments would answer something else entirely.
    /// </para>
    /// <para>
    /// <strong>Only the two that build a method from it.</strong> They are the
    /// ones whose body exists nowhere else, <strong>and a reader that guessed
    /// would have made a game's every block an argument</strong>.
    ///
    /// <para>
    /// <strong>And `instance_eval` is not among them, even though it takes a
    /// block.</strong> It does not need the block as an argument —
    /// <strong>it is the block</strong> — and it takes it off the chain the
    /// way <c>Yield</c> does. <strong>It was in this list first</strong>, and
    /// with it there the call answered nil,
    /// <strong>because the argument never arrived.</strong> A name in a list
    /// has to be there for a reason and the reason has to still hold.
    /// </para>
    /// </para>
    /// </remarks>
    private static bool BrauchtBlock(string pMethode) =>
        pMethode is "define_method" or "define_singleton_method"
            or "map" or "select" or "filter" or "reject"
            or "each" or "each_with_index" or "reverse_each"
            or "any?" or "all?"
            or "times" or "upto" or "downto";


    /// <summary>
    /// Builds a method from a block, and files it in the class.
    /// </summary>
    /// <param name="pTyp">The class the call stands in.</param>
    /// <param name="pMethode">The built-in's name.</param>
    /// <param name="pArgumente">The arguments, already evaluated.</param>
    /// <returns>true when there was something to build from.</returns>
    /// <remarks>
    /// <para>
    /// <strong>The block's parameters become the method's.</strong>
    /// `define_method(:doppelt) { |x| x * 2 }` makes a method of arity one,
    /// **and a reader that filed the block's body as an attribute-like stub
    /// would have produced a method with no parameters</strong> — so every
    /// call would have arrived with nothing bound and answered nil.
    /// </para>
    /// <para>
    /// <strong>The name is a symbol and a string is the same name.</strong> A
    /// game writes `define_method(:m)` and a plugin layer writes
    /// `define_method("m")`, <strong>and a reader that only read symbols would
    /// have made the second form a method named after the string's own
    /// text</strong> — a name no call would ever reach.
    /// </para>
    /// <para>
    /// <strong>`define_singleton_method` files it under `self.`, and that is
    /// the only difference.</strong> A class method and an instance method
    /// are two names in this runtime, <strong>and a reader that filed both
    /// the same way would have had a class whose singleton methods were
    /// reachable on its instances</strong>.
    /// </para>
    /// <para>
    /// <strong>And it clears a mark `undef` left.</strong> Writing the method
    /// is the same as writing it with `def`, <strong>so a class that takes a
    /// name out and later defines it through a block means the same thing as
    /// one that defines it with `def`</strong> — and a reader that left the
    /// mark would have kept the name dead in one spelling and alive in the
    /// other.
    /// </para>
    /// </remarks>
    private bool Definiert(
        RubyType pTyp, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pArgumente.Count == 0 || pArgumente[0].Kind != RubyValueKind.Symbol)
        {
            _diagnostics.Add(
                $"{pMethode} needs a name, and a symbol is what a game writes; "
                + "the first argument was "
                + (pArgumente.Count > 0 ? pArgumente[0].Kind.ToString() : "nothing")
                + ", and this reader does not guess a name out of an "
                + "expression");
            return false;
        }

        // **Der Block ist das zweite Argument und wird nie aufgerufen.**
        // `define_method` baut eine Methode, **und der Block laeuft erst, wenn
        // die Methode laeuft** -- ein Leser, der ihn hier ausgewertet haette,
        // haette die Seite einmal ausgefuehrt und die Methode dann ohne
        // Rumpf gebaut.
        var body = pArgumente.Count > 1 && pArgumente[1].Kind == RubyValueKind.Proc
            ? pArgumente[1].Block
            : null;
        if (body == null)
        {
            _diagnostics.Add(
                pMethode + "(:" + pArgumente[0].Name
                + ") was given no block, and a "
                + "block is where the method's body comes from; without one "
                + "the method would answer nothing at all");
            return false;
        }

        // **Ein `def`-Knoten, und nicht die Teile einzeln.** `define_method`
        // und `def` muessen dasselbe ablegen, **und das kleinste gemeinsame
        // Format ist der Knoten, den `def` auch benutzt** -- eine zweite
        // Form waere eine Stelle mehr, an der ein `def` und ein
        // `define_method` auseinanderlaufen koennten.
        var parameter = body.Children.Count > 1 ? body.Children[1] : null;
        _aktuellerTyp = pTyp;
        DefineMethod(
            new RubyNode
            {
                Kind = RubyNodeKind.Def,
                Name = pArgumente[0].Name,
                Line = body.Line,
                Children =
                [
                    parameter ?? new RubyNode { Kind = RubyNodeKind.Array, Line = body.Line },
                    body.Children.Count > 2 ? body.Children[2] : new RubyNode { Kind = RubyNodeKind.Nil, Line = body.Line },
                ],
            },
            pMethode == "define_singleton_method");
        return true;
    }

    /// <summary>
    /// The built-ins that act on the class they are written in.
    /// </summary>
    /// <param name="pTyp">The class the call stands in.</param>
    /// <param name="pMethode">The built-in's name.</param>
    /// <param name="pArgumente">The arguments, already evaluated.</param>
    /// <returns>true when it was one of them.</returns>
    /// <remarks>
    /// <para>
    /// <strong>These are not script methods and they are not host
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

        if (pMethode is "define_method" or "define_singleton_method")
        {
            return Definiert(pTyp, pMethode, pArgumente);
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
        // **Und die Art und der Name kommen aus dem Knoten DAVOR.**
        // `pNode` ist der `defined?`-Knoten, **und der, um den es geht,
        // ist sein Condition-Kind** -- **ein Leser, der `pNode` selbst
        // nimmt, sieht die Art `Defined`**,
        // **und dann ist `@@x` ein Instanzfeld und `defined?(@@x)` gibt
        // nil, waehrend `@@x` selbst 0 liest.**
        // **Das ist gemessen:** `@@n = 0; defined?(@@n)` gab nil,
        // **und `@@n` direkt gab 0.**
        // **Und `null` ist moeglich, und dann ist es "expression".** Es
        // gibt Formen von `defined?`, bei denen der Knoten nichts zu
        // benennen hat,
        // **und ein Leser, der sofort `.Kind` liest, wuerde dort mit einer
        // leeren Ausnahme abbrechen** -- **gemessen:
        // `Object reference not set to an instance of an object`** in
        // `Test_EachKindOfVariableAnswersWithItsOwnWord`.
        var geprueft = Child(pNode, RubyNodeRole.Condition);
        if (geprueft == null)
        {
            return Defined("expression");
        }

        var kind = geprueft.Kind;
        switch (kind)
        {
            // **Und der Name kommt aus dem geprueften Knoten selbst.**
            // `NameOf` ist fuer den `defined?`-Knoten geschrieben und sucht
            // ein Condition-Kind in ihm, **das eine Variable nicht hat** --
            // **also war der Name leer**, und `defined?(@hp)` gab nil, auch
            // wenn `@hp` gesetzt war.
            case RubyNodeKind.Identifier:
                return Defined(
                    Local(geprueft.Name ?? string.Empty).IsNil
                        && !HasLocal(geprueft.Name ?? string.Empty)
                            ? null
                            : "local-variable");
            case RubyNodeKind.InstanceVariable:
                return Defined(
                    TabelleFuer(geprueft).ContainsKey(geprueft.Name ?? string.Empty)
                        ? "instance-variable" : null);
            case RubyNodeKind.ClassVariable:
                // **Und `defined?` sagt seinen eigenen Namen.** Ruby gibt
                // `"class variable"` zurueck,
                // **und ein Leser, der auch hier "instance-variable"
                // schriebe, haette einem Skript, das `defined?(@@x)`
                // gegen einen Namen prueft, die falsche Antwort gegeben.**
                // **Und "class variable" mit Leerzeichen, nicht mit
                // Bindestrich.** Verifiziert in `eval.c` aus Ruby 1.8.1:
                // `defined?` antwortet `"class variable"` und daneben
                // `"local-variable"` **mit Bindestrich** -- **die
                // Schreibweisen sind nicht einheitlich, und diese Runtime
                // schreibt, was die Quelle schreibt.**
                return Defined(
                    TabelleFuer(geprueft).ContainsKey(geprueft.Name ?? string.Empty)
                        ? "class variable" : null);
            case RubyNodeKind.GlobalVariable:
                return Defined(
                    _globals.ContainsKey(GlobalName(geprueft))
                        ? "global-variable" : null);
            case RubyNodeKind.Constant:
                return Defined(
                    _types.ContainsKey(geprueft.Name ?? string.Empty)
                        || _host.LookupConstant(geprueft.Name ?? string.Empty) != null
                            ? "constant" : null);
            case RubyNodeKind.Call:
            case RubyNodeKind.MethodCall:
            case RubyNodeKind.SelfCall:
                return Defined(
                    // **Und `empfaengerOf` will den `defined?`-Knoten, weil
                    // es das Condition-Kind darin sucht.** Ihm den
                    // geprueften Knoten zu geben hiesse: **es sucht darin
                    // ein Condition-Kind, das der Aufruf nicht hat**
                    // -- **gemessen: `Object reference not set to an
                    // instance of an object`** bei
                    // `Test_AMethodThatIsNotThereAnswersNil`.
                    EigeneMethode(empfaengerOf(pNode), geprueft.Name ?? string.Empty) != null
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
        // **Dieselbe Wand wie `Local` und `SetLocal`.** Sonst wuerde `x` im
        // Block als "vorhanden" gelten, **während `Local` nil lieferte** --
        // und ein Spiel, das `defined?(x)` schreibt, bekame eine Antwort,
        // die der naechste Zugriff nicht bestaetigt.
        var grenze = _blockGrenze.Count > 0 ? _blockGrenze[^1] : _methodenGrenze;
        for (var i = _scopes.Count - 1; i >= grenze; i--)
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



    /// <summary>
    /// Takes a method out of the class it is written in.
    /// </summary>
    /// <param name="pNode">The `undef` node.</param>
    /// <returns>A symbol naming what was taken out.</returns>
    /// <remarks>
    /// <para>
    /// <strong>It takes the method out and does not replace it with
    /// nothing.</strong> Ruby makes the name undefined, so a call after
    /// <c>undef m</c> raises <c>NoMethodError</c> rather than finding the
    /// base's method — <strong>and a reader that deleted only the class's own
    /// entry would let the base's method through</strong>, which is the
    /// opposite of what <c>undef</c> is for: a game writes it to say *this
    /// class does not do that*, and inheriting it is exactly the case it
    /// rules out.
    /// </para>
    /// <para>
    /// <strong>So the name is marked and not merely absent.</strong> The
    /// chain walk stops at the mark, and that is the one behaviour that
    /// distinguishes <c>undef</c> from never having defined the method.
    /// </para>
    /// <para>
    /// <strong>And it is a name and not a call.</strong> <c>undef m</c> takes
    /// the method <em>named</em> <code>m</code>; it does not call it, and a
    /// reader that evaluated the operand would run a method on the way to
    /// removing it.
    /// </para>
    /// </remarks>
    private RubyValue EvaluateUndef(RubyNode pNode)
    {
        var typ = _aktuellerTyp;
        if (typ == null)
        {
            _diagnostics.Add(
                $"undef {UndefName(pNode)} stands outside a class, and this "
                + "interpreter files methods under a class; the reference "
                + "would put it on Object, and that is a host's job");
            return RubyValue.Nil;
        }

        // **Jeder Name in der Liste.** `undef a, b` nimmt beide weg, **und
        // die erste Fassage nahm nur den ersten** -- der zweite wurde als
        // eigene Anweisung gelesen und blieb als Name im Skript stehen, wo
        // ihn nichts aufrief. **Ein Spiel, das zwei Methoden auf einmal
        // wegnimmt, haette die zweite behalten.**
        var letzter = RubyValue.Nil;
        foreach (var teil in Statements(pNode))
        {
            // **Ein Bezeichner und ein Symbol sind beides moeglich**, und
            // beide nennen eine Methode: `undef m` schreibt den Bezeichner,
            // `undef :m` das Symbol. **Ein Leser, der nur eines davon las,
            // haette die andere Form ins Leere gehen lassen.**
            var name = UndefName(teil);
            typ.Undefiniert.Add(name);

            // **Und die Singleton-Form, denn `undef` nimmt beides weg.**
            // `undef m` nimmt `m` und `self.m` aus der Tabelle,
            // **und die Marke bisher nur fuer `m`** -- sodass
            // `MissingMethod` seine eigene Marke unter
            // `self.method_missing` suchte und keine fand.
            // **`undef method_missing` muss die Basis daran hindern, den
            // Handler zu liefern** -- und genau daran scheitert es, wenn die
            // Marke unter dem blossen Namen liegt.
            typ.Undefiniert.Add("self." + name);
            typ.Methods.Remove(name);
            typ.Methods.Remove("self." + name);
            letzter = RubyValue.OfSymbol(name);
        }

        return letzter;
    }

    /// <summary>
    /// The method name an `undef` names.
    /// </summary>
    /// <remarks>
    /// <strong>Both spellings, and nothing else.</strong> A `def` has no
    /// other shape, so anything that is not a name or a symbol is a
    /// diagnostic — **and a reader that took any operand would have removed
    /// a method named after a number.**
    /// </remarks>
    private static string UndefName(RubyNode pNode) => pNode.Kind switch
    {
        // **`Text` und nicht `Name` beim Symbol.** Ein Symbol-Knoten traegt
        // den Namen in `Text` -- `Name` ist fuer die Knoten, die einen Namen
        // *ueber* ihr Literal fuehren, und ein Symbol fuehrt keinen.
        // **Das ist derselbe Unterschied wie bei `alias`, aus demselben
        // Grund, und dieselbe Ursache: `alias :neu :alt` hat es getroffen
        // und `undef :m` jetzt.**
        RubyNodeKind.Identifier => pNode.Name ?? string.Empty,
        RubyNodeKind.Symbol => pNode.Text ?? pNode.Name ?? string.Empty,
        _ => "",
    };



    // ---- Helpers
    /// <summary>
    /// The hash a script wrote, with its pairs in the order it wrote them.
    /// </summary>
    /// <param name="pNode">The hash's node.</param>
    /// <returns>The hash.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And it was refused, which stops every script that keeps
    /// settings.</strong> <c>{:hp =&gt; 10, :name =&gt; "Held"}</c> is the
    /// ordinary form,
    /// <strong>and a reader that answered "this interpreter does not
    /// evaluate a Hash node" gave a game a message about its own
    /// source</strong> -- and every saved setting, every event table and
    /// every status row is a hash.
    /// </para>
    /// <para>
    /// <strong>And the pairs stay in the order the script wrote them.</strong>
    /// Ruby 1.8 hashes are ordered and an enumerating game relies on it --
    /// <c>hash.each</c> walks a status window's rows in the order the
    /// script listed them,
    /// <strong>and a reader that sorted them would have made a menu draw
    /// its rows in an order nobody chose.</strong>
    /// </para>
    /// <para>
    /// <strong>And the keys and values share one list, two at a time.</strong>
    /// <c>[:a, 1, :b, 2]</c>,
    /// <strong>and das ist die Form, in der <c>keys</c> und <c>values</c>
    /// sie lesen.</strong>
    /// </para>
    /// </remarks>
    private RubyValue HashWert(RubyNode pNode)
    {
        var paare = new List<RubyValue>();
        foreach (var teil in pNode.Children)
        {
            // **Und jedes Kind ist schon ein `=>`, also ein Hash fuer
            // sich.** `{a: 1}` und `{a => 1}` kommen beide hier an,
            // **und ein Leser, der nur die eine Form kannte, wuerde
            // `{:a => 1}` als leeren Hash gelesen.**
            var paar = Evaluate(teil);
            if (paar.Kind == RubyValueKind.Object && paar.IsHash)
            {
                paare.AddRange(paar.Items);
            }
        }

        return RubyValue.OfHash(paare);
    }




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

        // **Der Rueckfall gilt nur fuer einen Knoten ohne Rollen ueberhaupt.**
        // Ein Aufruf traegt **immer** eine `Receiver`-Rolle, **und wenn er
        // keine Argumente hat, gibt es keine `Argument`-Rolle** --
        // `[A.m()]` hatte eine `Receiver`-Rolle und ein Kind, **und der
        // Rueckfall auf `Children` lieferte den Empfaenger als Argument.**
        //
        // **Das hiess:** `def m(x = 9)` bekam `x = A`, **der Rumpf las `A`,
        // und der Aufruf antwortete mit dem Empfaenger statt mit dem
        // Rumpf.** Gemessen am 2026-09-29 mit `URPG_TRACE_CALL`:
        // `argumente=1 [Symbol] kinder=1 rollen=1` -- **ein Argument, ein
        // Kind, eine Rolle.** Ein Aufruf ohne Argumente **hat null
        // Argumente**, und das ist der ganze Unterschied.
        if (pNode.Role_Children != null && pNode.Role_Children.Count > 0)
        {
            return [];
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