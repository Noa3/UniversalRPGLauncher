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

    /// <summary>The pairs `sort_by` gathered, and their measured sizes.</summary>
    /// <remarks>
    /// **Zwei Listen, weil der Wert und sein Mass getrennt bleiben
    /// muessen.** `sort_by` gibt den Wert zurueck und ordnet nach dem, was
    /// der Block gemessen hat,
    /// **und eine Liste von Werten allein wuerde nach dem Falschen
    /// ordnen** — and a game that sorts its actors by level would sort them
    /// by name.
    /// </remarks>
    private readonly List<(RubyValue Item1, RubyValue Item2)> Paare = [];

    /// <summary>The groups `group_by` gathered, in the order they appeared.</summary>
    private readonly Dictionary<RubyValue, List<RubyValue>> Gruppen = [];

    /// <summary>
    /// The constants that hold a value, and not only a type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a constant may hold a value, and not only a type.</strong>
    /// <c>Punkt = Struct.new(:x, :y)</c> is exactly that,
    /// <strong>and <c>RPG::Actor = Struct.new(:id, :name)</c> is the first
    /// line of the standard library of RPG Maker XP, VX and VX Ace</strong> —
    /// <strong>without this table not one of those games would be
    /// readable</strong>, because the value of a constant had nowhere to go.
    /// </para>
    /// <para>
    /// <strong>And it stands next to the types and not inside them.</strong> A
    /// constant that holds a type is in <c>_types</c>,
    /// <strong>and one that holds a number cannot be in the same table</strong>
    /// — <strong>the difference between "this is a class" and "this is a
    /// value" is the difference between a class and a number</strong>, and a
    /// game's <c>LIMIT = 100</c> would be a class.
    /// </para>
    /// </remarks>
    private readonly Dictionary<string, RubyValue> _konstanten = new(StringComparer.Ordinal);

    /// <summary>How many structs have been built, and it is only a
    /// counter.</summary>
    /// <remarks>
    /// <strong>And it counts, and it knows nothing.</strong> The name of a
    /// struct class has to differ from the last one,
    /// <strong>and that is all it needs</strong> — <strong>a reader that read
    /// the number as "so many fields" would have called a three-field struct
    /// the third struct in the world</strong>, and two games with the same
    /// number would share their fields.
    /// </remarks>
    private int _strukturZaehler;

    /// <summary>The scripts that were required once, and their names.</summary>
    /// <remarks>
    /// <strong>And it belongs to the interpreter, and not to the host.</strong>
    /// The host has the files, **and "already loaded" is a fact about the
    /// run and not about the disk** —
    /// <strong>a host that remembered it would load a game twice as soon as
    /// it were started again</strong>, **and a game that was started again is
    /// a new game.** <strong>So `load` skips nothing</strong> —
    /// **and that is the whole difference between the two words.**
    /// </remarks>
    private readonly HashSet<string> _geladeneSkripte = new(StringComparer.Ordinal);

    /// <summary>
    /// The number each object was given, so `object_id` is the same for one
    /// object and not for another.
    /// </summary>
    /// <remarks>
    /// <strong>And a table, and not the address.</strong> Ruby counts
    /// objects,
    /// **and a reader that made the number from the place in memory would
    /// give the same number twice** -- a collection between two calls would
    /// hand it out again, **and <c>list.uniq</c> would have two different
    /// heroes in one entry.**
    /// </remarks>
    private readonly Dictionary<RubyValue, int> _instanzNummern = [];

    /// <summary>The number the next object gets.</summary>
    private int _naechsteInstanzNummer;

    /// <summary>
    /// The error classes of the language, by name.
    /// </summary>
    /// <remarks>
    /// <strong>And a fixed list, and not a question to the host.</strong>
    /// The classes are part of the language,
    /// <strong>and a host that sie nicht kennt, hat sie trotzdem</strong> —
    /// **and `rescue` compares names and not types, so the list is
    /// enough**, and a game that writes its <em>own</em> error class
    /// (<c>class MeinFehler &lt; StandardError</c>) is matched against the
    /// same names.
    /// </remarks>
    private static readonly HashSet<string> FehlerKlassen = new(StringComparer.Ordinal)
    {
        "Exception", "StandardError", "RuntimeError", "ArgumentError",
        "TypeError", "RangeError", "ZeroDivisionError", "FloatDomainError",
        "IndexError", "KeyError", "IOError", "EOFError", "ScriptError",
        "NotImplementedError", "LoadError", "SyntaxError", "LocalJumpError",
        "NameError", "NoMethodError", "SystemExit", "SystemStackError",
        "StopIteration", "FrozenError", "RegexpError", "EncodingError",
    };

    /// <summary>
    /// The types of the language, by name, with the one that is the base of
    /// the other.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a fixed list, and not a question to the host.</strong>
    /// These names are part of the language,
    /// <strong>and a game's own <c>class Held</c> sits under
    /// <c>Object</c> whether a host says so or not</strong> — **and a
    /// reader that asked the host would refuse <c>Object.ancestors</c>
    /// in every test**, **and a script that walks the chain would stop
    /// there.**
    /// </para>
    /// <para>
    /// <strong>And <c>BasicObject</c> is the end of every chain.</strong>
    /// <c>BasicObject.superclass</c> is nil and not <c>BasicObject</c>,
    /// **and a reader that made a class its own base would walk
    /// forever.**
    /// </para>
    /// <para>
    /// <strong>And they are real types, and not names.</strong>
    /// <c>Object.instance_methods</c> has to answer with the methods the
    /// host gave it, <strong>and a reader that made them names would answer
    /// an empty list</strong> — **and a plugin that asks
    /// <c>Object.instance_methods.include?(:draw)</c> before it overrides
    /// anything would say <c>no</c> and override a method twice.
    /// </para>
    /// </remarks>
    private static readonly (string Name, bool Klasse, string? Basis)[]
        SprachTypen =
        [
            ("BasicObject", true, null),
            ("Object", true, "BasicObject"),
            ("Module", true, "Object"),
            ("Class", true, "Module"),
            ("Kernel", false, null),
            ("Comparable", false, null),
            ("Enumerable", false, null),
            ("String", true, "Object"),
            ("Integer", true, "Object"),
            ("Float", true, "Object"),
            ("Numeric", true, "Object"),
            ("Symbol", true, "Object"),
            ("Array", true, "Object"),
            ("Hash", true, "Object"),
            ("Range", true, "Object"),
            ("Proc", true, "Object"),
            ("Regexp", true, "Object"),
            ("NilClass", true, "Object"),
            ("TrueClass", true, "Object"),
            ("FalseClass", true, "Object"),
            ("Struct", true, "Object"),
            ("Exception", true, "Object"),
            ("StandardError", true, "Exception"),
            ("RuntimeError", true, "StandardError"),
            ("ArgumentError", true, "StandardError"),
            ("TypeError", true, "StandardError"),
            ("RangeError", true, "StandardError"),
            ("ZeroDivisionError", true, "StandardError"),
            ("FloatDomainError", true, "RangeError"),
            ("IndexError", true, "StandardError"),
            ("KeyError", true, "IndexError"),
            ("IOError", true, "StandardError"),
            ("EOFError", true, "IOError"),
            ("ScriptError", true, "Exception"),
            ("NotImplementedError", true, "ScriptError"),
            ("LoadError", true, "ScriptError"),
            ("SyntaxError", true, "ScriptError"),
            ("LocalJumpError", true, "StandardError"),
            ("NameError", true, "StandardError"),
            ("NoMethodError", true, "NameError"),
            ("SystemExit", true, "Exception"),
            ("SystemStackError", true, "Exception"),
            ("StopIteration", true, "IndexError"),
            ("FrozenError", true, "RuntimeError"),
            ("RegexpError", true, "StandardError"),
            ("EncodingError", true, "StandardError"),
            ("Method", true, "Object"),
            ("UnboundMethod", true, "Object"),
            ("Binding", true, "Object"),
        ];

    /// <summary>
    /// The modules Ruby puts into the built-in classes, as type and module.
    /// </summary>
    /// <remarks>
    /// <strong>And this is measured from the language and not guessed.</strong>
    /// <c>String.include?(Comparable)</c> is true and a class sorted by
    /// name needs it,
    /// **and a reader that answered <c>false</c> here would make every sort
    /// guard in a VX script say "not included"** -- and a plugin that adds
    /// <c>&lt;=&gt;</c> would be told the class is not comparable.
    /// </remarks>
    private static readonly (string Typ, string Modul)[] SprachEingebunden =
    [
        ("String", "Comparable"),
        ("String", "Kernel"),
        ("Integer", "Comparable"),
        ("Integer", "Numeric"),
        ("Integer", "Kernel"),
        ("Float", "Comparable"),
        ("Float", "Numeric"),
        ("Float", "Kernel"),
        ("Numeric", "Comparable"),
        ("Numeric", "Kernel"),
        ("Symbol", "Comparable"),
        ("Symbol", "Kernel"),
        ("Array", "Enumerable"),
        ("Array", "Kernel"),
        ("Hash", "Enumerable"),
        ("Hash", "Kernel"),
        ("Range", "Enumerable"),
        ("Range", "Kernel"),
        ("Regexp", "Kernel"),
        ("Struct", "Enumerable"),
        ("Struct", "Kernel"),
        ("Proc", "Kernel"),
        ("NilClass", "Kernel"),
        ("TrueClass", "Kernel"),
        ("FalseClass", "Kernel"),
        ("Object", "Kernel"),
    ];

    /// <summary>The names that are the language's and not a script's.</summary>
    private static readonly HashSet<string> SprachTypennamen =
        new(SprachTypen.Select(t => t.Name), StringComparer.Ordinal);

    /// <summary>The default each struct field was given, by class name.</summary>
    /// <remarks>
    /// <strong>And by the class name, and not in the class.</strong> The
    /// fields are in <c>RubyType.Struct</c>,
    /// <strong>and their defaults are values and not nodes</strong> —
    /// <strong>a default is a value because it was already evaluated when the
    /// struct was built</strong>, and a game that wrote
    /// <c>Struct.new(:id, :name, "")</c> has a default that is a value and
    /// not an expression to run again.
    /// </remarks>
    private readonly Dictionary<string, Dictionary<string, RubyValue>> _strukturVorgaben
        = new(StringComparer.Ordinal);

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

        // **Und die Typen der Sprache stehen da, bevor irgendein Skript
        // laeuft.** `Object`, `String`, `Module` und die Fehlerklassen
        // **sind Teile der Sprache und nicht etwas, das ein Host anbietet**,
        // **und ein Leser, der sie erst beim ersten `include` anlegt, haette
        // `Object.ancestors` in einer Kette, die vorher nicht existierte.**
        foreach (var (name, klasse, basis) in SprachTypen)
        {
            _types[name] = new RubyType
            {
                Name = name,
                IsClass = klasse,
                Superclass = basis,
            };
        }
        // **Und die eingebauten Module stehen in den Klassen, in denen
        // Ruby sie einbindet.** `String.include?(Comparable)` ist wahr und
        // nicht eine Behauptung dieses Lesers,
        // **und `Integer` nimmt `Comparable`, `Numeric` und `Kernel`**, **und
        // `Array`, `Hash` und `Range` nehmen `Enumerable` und `Kernel`**.
        // **Und jedes Objekt nimmt `Kernel`** -- **denn `puts` und `raise`
        // sind Methoden von `Kernel` und stehen in jedem Skript ohne
        // Receiver.**
        foreach (var (typ, modulName) in SprachEingebunden)
        {
            // **`modul` ist ein Schluesselwort in C# und der Name einer
            // Variablen ist hier keine Ausnahme.** Der erste Versuch hiess
            // `modul` und der Compiler sagte *the name 'module' does not
            // exist in the current context* -- **und das sieht nach einem
            // Tippfehler aus und ist ein Schluesselwort.**
            if (_types.TryGetValue(typ, out var haelter)
                && _types.ContainsKey(modulName))
            {
                haelter.Eingebunden.Add((modulName, false));
            }
        }

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
    /// The name of the script that is running, so `require_relative` knows
    /// where "next to me" is.
    /// </summary>
    /// <remarks>
    /// <strong>And a stack and not a single name.</strong> A file requires
    /// another, and that one requires a third,
    /// **and a reader that kept one name would have asked the host for a
    /// path relative to the outermost file for all three** --
    /// **and a game whose helpers live in a folder next to their users
    /// would look one folder too high.** Empty at the top, **because the
    /// script the host started is the host's business and not ours.**
    /// </remarks>
    private readonly List<string> _skriptKette = [];

    /// <summary>
    /// Whether a <c>break</c> has been seen, which stops the loop around it.
    /// </summary>
    /// <remarks>
    /// <strong>And a flag and not an exception.</strong> Ruby gives
    /// <c>break</c> a value, and a reader that threw for it would have to
    /// catch it at every loop, <strong>and a <c>break</c> in a block inside
    /// a loop would have ended the loop and not the block</strong> — which
    /// is the sentence <c>[1,2,3].each { |x| break if x == 2 }</c> is.
    /// </remarks>
    private bool _gebrochen;

    /// <summary>
    /// Whether a <c>next</c> has been seen, which skips the rest of one turn.
    /// </summary>
    private bool _weiter;

    /// <summary>What <c>break</c> or <c>next</c> carried out of the loop.</summary>
    private RubyValue _abbruchWert = RubyValue.Nil;

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

    /// <summary>
    /// The names of the types the script defined, in order.
    /// </summary>
    /// <remarks>
    /// <strong>And only those, and not the types of the language.</strong>
    /// <c>Object</c>, <c>String</c> and the error classes are there from the
    /// first statement,
    /// **and a host asking what a script defined does not want a list of
    /// forty names it did not get from the script** --
    /// it wants to know what the script added,
    /// **and that is what a test about a script is about.**
    /// </remarks>
    public IReadOnlyList<string> DefinedTypes => _types.Keys
        .Where(name => !SprachTypennamen.Contains(name))
        .ToList();

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
            RubyNodeKind.Break => EvaluateAbbruch(pNode, true),
            RubyNodeKind.Next => EvaluateAbbruch(pNode, false),
            RubyNodeKind.While => EvaluateWhile(pNode),
            RubyNodeKind.Begin => EvaluateBegin(pNode),
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

        // **And `==` and `!=` ask the script first.** A game that writes
        // its own equality writes `def ==(other)`,
        // **and `a == b` goes to `Apply`, which is static and cannot call a
        // script method** --
        // **and without this line a `def ==` in a class had no effect**, and
        // `list.include?(held)` was always false. Measured: `a == b` with a
        // `def ==` answered false and said *a value has no method 'n=' on
        // this host* -- **and the message spoke of a host that was never
        // asked.**
        //
        // **And the two sides may answer it.** `held == held` is
        // symmetric,
        // **and a reader that asked only the left one would have answered
        // `a == b` wrong and `b == a` right**, depending on which value
        // stood on the left.
        if (links is "==" or "!=")
        {
            var ausDemSkript = VergleichMitSkript(pNode, "==");
            if (ausDemSkript != null)
            {
                return links == "==" ? ausDemSkript
                    : RubyValue.OfBoolean(!Truthy(ausDemSkript));
            }

            var erst = Evaluate(Operands(pNode)[0]);
            var zweit = Evaluate(Operands(pNode)[1]);
            var verglichen = StructMethode(erst, "==", [zweit]);
            if (verglichen == null)
            {
                verglichen = StructMethode(zweit, "==", [erst]);
            }

            if (verglichen != null)
            {
                return links == "==" ? verglichen
                    : RubyValue.OfBoolean(!Truthy(verglichen));
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

        // **Und `%` auf einem Text ist das Format, und nicht der Rest.**
        // `"%05.2f" % wert` steht in jeder Statuszeile von VX,
        // **und ein Leser, der hier nur Zahlen nahm, wuerde genau diese
        // Zeile mit *„undefined operator '%' for a String and a
        // Float"* ablehnen** --
        // **und die Fehlermeldung waere ueber einen Operator, den es
        // gibt.**
        //
        // **Vor dem `switch`, weil ein `case` mit Bedingung keinen
        // `goto case` traegt** -- **und ein Leser, der ihn brauchte,
        // haette ihn nicht geschrieben und waere an der zweiten Stelle
        // gescheitert.**
        if (pOperator == "%" && pLeft.Kind == RubyValueKind.String)
        {
            var alsText = ProzentFormatieren(pLeft, pRight);
            if (alsText != null)
            {
                return alsText;
            }
        }

        switch (pOperator)
        {
            // **Und `*` mit einer Zahl wiederholt den Text.** `"-" * 30`
            // ist die Trennlinie zwischen zwei Fenstern,
            // **und `str * n` steht in jedem Bildschirm, der eine Leiste
            // zeichnet** -- **und ein Leser, der nur Zahlen nahm, wuerde
            // *„undefined operator '*' for a String and a Integer"*
            // sagen**, **und die Meldung waere ueber einen Operator, den es
            // gibt.**
            case "*" when pLeft.Kind == RubyValueKind.String
                && (pRight.Kind == RubyValueKind.Integer
                    || pRight.Kind == RubyValueKind.Float):
            {
                var anzahl = (int)Math.Max(
                    0d,
                    pRight.Kind == RubyValueKind.Integer
                        ? pRight.Integer
                        : pRight.Real);
                var wiederholt = new List<byte>(pLeft.Bytes.Length * anzahl);
                for(var mal = 0; mal < anzahl; mal++)
                {
                    wiederholt.AddRange(pLeft.Bytes);
                }

                return RubyValue.OfBytes([.. wiederholt]);
            }

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
            // **Und `%` auf einem Text ist das Format, und nicht der Rest.**
            // `"%05.2f" % wert` steht in jeder Statuszeile von VX,
            // **und ein Leser, der hier nur Zahlen nahm, wuerde genau diese
            // Zeile mit *„undefined operator '%' for a String and a
            // Float"* ablehnen** --
            // **und die Fehlermeldung waere ueber einen Operator, den es
            // gibt.**

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

        // **And a constant that holds a value gives that value.**
        // `LIMIT = 100` and `Punkt = Struct.new(:x, :y)`,
        // **and without this step `LIMIT` was nil and every game that read
        // it did its arithmetic on nothing.**
        if (_konstanten.TryGetValue(name, out var konstant))
        {
            return konstant;
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

        // **And `Struct` is the second one.** `RPG::Actor = Struct.new(:id,
        // :name)` is the **first line** of the standard library of XP, VX and
        // VX Ace, **and without it not one of those games is
        // readable** -- **this is not a convenience, this is the data
        // **Und die Fehlerklassen der Sprache, und weil `raise` sie
        // braucht.** `raise ArgumentError, "x"` und
        // `rescue ZeroDivisionError` schreiben sie,
        // **und ohne sie gaebe `raise ArgumentError` *„the constant
        // ArgumentError is not defined by this host"*** -- **eine Meldung
        // ueber den Host fuer eine Klasse, die der Leser haette** --
        // **und `rescue ZeroDivisionError` wuerde nie greifen**, weil der
        // Arm einen Namen vergleicht, den niemand geschrieben hat.
        //
        // **Und sie stehen als Symbole und nicht als echte Typen.** Es sind
        // Namen, und `rescue` vergleicht Namen,
        // **ein Leser, der daraus Klassen mit leeren Methoden baute,
        // haette `ArgumentError.new` zu einer Klasse ohne Konstruktor
        // gemacht** -- **und `raise ArgumentError.new("x")`, die
        // haeufigste der drei Formen, waere dann `nil`.**
        if (FehlerKlassen.Contains(name))
        {
            return RubyValue.OfSymbol(name);
        }

        // catalogue of every RPG Maker script.**
        if (name == "Struct")
        {
            return RubyValue.OfSymbol("Struct");
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
        // **Und ein Block sieht die Variablen der Methode, und das ist
        // Rubys Regel.** `local_push` haengt die neue Ebene mit
        // `local->prev = lvtbl` **an die Kette an und kappt sie nicht** --
        // verifiziert in `parse.y` aus Ruby 1.8.1.
        //
        // **Die alte Fassung fing bei der Blockebene an, und damit las und
        // schrieb ein Block nur seine eigenen Namen.** Gemessen:
        // `g = []; 3.times { |i| g.push(i) }; g.length` war **0** --
        // **und genau dieser Satz baut jedes Menue und jedes Fenster eines
        // Spiels**, **und `g` ist ausserhalb gesetzt, also ist es keine
        // neue Variable des Blocks.**
        // *Der Kommentar, der diese Regel begruendete, nannte `3.times
        // { |i| g.push(i) }` als den Fall, fuer den sie noetig sei --
        // und der Fall funktionierte nicht.*
        // **Und die Kette geht bis zur Methode, und nicht bis zur
        // Blockebene.** `local_push` haengt die Ebene an,
        // **und nur `ruby_dyna_vars` wird in `opt_block_var` gerettet.**
        var grenze = _methodenGrenze;
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
    /// <summary>
    /// The questions an object is asked about its own fields.
    /// </summary>
    /// <param name="pObjekt">The object.</param>
    /// <param name="pMethode">The question.</param>
    /// <param name="pArgumente">The name of the field, and its new
    /// value.</param>
    /// <returns>The answer, or nil.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the fields are the object's own store, and not a copy.</strong>
    /// <c>instance_variable_set(:@hp, 20)</c> then
    /// <c>instance_variable_get(:@hp)</c> is <c>20</c>,
    /// **and a reader that answered from a table of its own would give a
    /// number the object does not have** — and a plugin that writes a
    /// field would then read nil.
    /// </para>
    /// <para>
    /// <strong>And a name without the <c>@</c> is the same name.</strong>
    /// <c>instance_variable_get(:hp)</c> is <c>:@hp</c>,
    /// **and a reader that demanded the <c>@</c> would say *no such
    /// variable* for the spelling a game writes most** —
    /// <strong>and every <c>get</c>/<c>set</c> pair would fail for half its
    /// callers.**
    /// </para>
    /// <para>
    /// <strong>And a name that is not a name is a diagnostic.</strong>
    /// <c>instance_variable_get("@hp")</c> is a text,
    /// **and Ruby refuses it** — <strong>and a reader that accepted a text
    /// would make a game that writes the wrong thing look like it
    /// worked.</strong>
    /// </para>
    /// </remarks>
    private RubyValue InstanzVariable(
        RubyValue pObjekt,
        string pMethode,
        IReadOnlyList<RubyValue> pArgumente)
    {
        // **And `instance_variables` takes no argument.** It is the only
        // one of these five questions that takes no names,
        // **and a reader that checked the same argument for all five would
        /// answer `nil` here** -- **and this is exactly the question every
        // plugin that walks `@ivars` asks.**
        if (pMethode == "instance_variables")
        {
            var alleNamen = new List<RubyValue>();
            foreach (var feld in pObjekt.Felder.Keys
                .OrderBy(k => k, StringComparer.Ordinal))
            {
                if (feld.StartsWith("@", StringComparison.Ordinal))
                {
                    alleNamen.Add(RubyValue.OfSymbol(feld));
                }
            }

            return RubyValue.OfArray(alleNamen);
        }

        if (pArgumente.Count == 0
            || pArgumente[0].Kind != RubyValueKind.Symbol)
        {
            _diagnostics.Add(
                pMethode + " takes the name of a field as a symbol, and the "
                    + "first argument is not one; a reader that took any "
                    + "value would have answered about whatever the game "
                    + "wrote");
            return RubyValue.Nil;
        }

        // **Und der Punkt gehoert zum Namen.** `instance_variable_get(:hp)`
        // und `:@hp` sind derselbe Satz,
        // **und der Leser haette sonst fuer die haeufigere Schreibweise
        // *no such variable* gesagt.**
        var name = pArgumente[0].Name ?? string.Empty;
        if (!name.StartsWith("@", StringComparison.Ordinal))
        {
            name = "@" + name;
        }

        switch (pMethode)
        {
            case "instance_variable_get":
                // **Und ein Feld, das es nicht gibt, ist nil und kein
                // Fehler.** `p.instance_variable_get(:@optional)` ist der
                // Satz, mit dem ein Plugin einen Zustand liest, den es
                // selbst gesetzt hat,
                // **und ein Leser, der das ablehnt, wuerde jedes Plugin
                // stoppen, das einen Zustand erst spaeter setzt.**
                return pObjekt.Felder.TryGetValue(name, out var wert)
                    ? wert
                    : RubyValue.Nil;

            case "instance_variable_defined?":
                return RubyValue.OfBoolean(pObjekt.Felder.ContainsKey(name));

            case "instance_variable_set":
                if (pArgumente.Count < 2)
                {
                    _diagnostics.Add(
                        "instance_variable_set takes a name and a value, and "
                            + "only the name came; a reader that set nil "
                            + "would have written a field the game never "
                            + "gave");
                    return RubyValue.Nil;
                }

                pObjekt.Felder[name] = pArgumente[1];
                return pArgumente[1];

            case "remove_instance_variable":
                if (!pObjekt.Felder.TryGetValue(name, out var alt))
                {
                    return RubyValue.Nil;
                }

                pObjekt.Felder.Remove(name);
                return alt;

        }

        return RubyValue.Nil;
    }

    private int InstanzNummer(RubyValue pWert)
    {
        if (pWert.Kind != RubyValueKind.Object)
        {
            // **Und fuer jeden Wert gilt derselbe Satz.** `1.object_id` ist
            // eine Zahl und `1.object_id == 1.object_id`,
            // **denn kleine Zahlen sind in Ruby dieselben Objekte.**
            return 8 + (int)pWert.Kind;
        }

        if (!_instanzNummern.TryGetValue(pWert, out var nummer))
        {
            _naechsteInstanzNummer += 2;
            nummer = _naechsteInstanzNummer;
            _instanzNummern[pWert] = nummer;
        }

        return nummer;
    }


    /// <summary>
    /// A copy that does not share what can be written.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The copy.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the fields are copied one by one.</strong> An object has
    /// its own store, **and a copy that pointed at the same store would be
    /// the same object under a second name** — and `a.dup.n = 2` would
    /// change `a.n`.
    /// </para>
    /// <para>
    /// <strong>And a list is a new list, and a hash a new hash.</strong>
    /// `l = [1,2]; d = l.dup; d << 3` leaves `l` with two entries,
    /// **and a reader that handed back the same list would have three in
    /// both** — and a game's party list would grow when a plugin took a
    /// copy of it.
    /// </para>
    /// <para>
    /// <strong>And a number and a symbol are themselves.</strong> Ruby hands
    /// back the same object for those, **and a copy of `1` is `1`** — and a
    /// reader that made a fresh one would answer an object where a number
    /// is expected, and every arithmetic on it would go through the host.
    /// </para>
    /// </remarks>
    private static RubyValue Kopie(RubyValue pWert)
    {
        switch (pWert.Kind)
        {
            case RubyValueKind.Object:
                {
                    // **Und ein Objekt mit einer Klasse wird kein
                    // Feldhaufen.** `Kopie` baut eine Liste, und
                    // `OfArray` weiss nichts von der Klasse,
                    // **und `a.dup` waere dann ein Objekt ohne ClassName** --
                    // **und `EigeneMethode` ginge auf `null` und `b.n = 2`
                    // wuerde sagen *a value has no method 'n=' on this
                    // host*** -- **und genau das ist gemessen worden.**
                    // **Die Kopie traegt also den Klassennamen bei sich,
                    // und die Felder kommen einzeln hinein.**
                    if (!string.IsNullOrEmpty(pWert.ClassName)
                        && pWert.Items.Count == 0)
                    {
                        var feldKopie = RubyValue.OfObject(pWert.ClassName, new Dictionary<RubyValue, RubyValue>());
                        foreach (var (name, wert) in pWert.Felder)
                        {
                            feldKopie.Felder[name] = Kopie(wert);
                        }

                        return feldKopie;
                    }

                    var neueListe = new List<RubyValue>(pWert.Items.Count);
                    foreach (var eintrag in pWert.Items)
                    {
                        neueListe.Add(Kopie(eintrag));
                    }

                    var kopie = pWert.IsHash
                        ? RubyValue.OfHash(neueListe)
                        : RubyValue.OfArray(neueListe);
                    foreach (var (name, wert) in pWert.Felder)
                    {
                        kopie.Felder[name] = Kopie(wert);
                    }

                    return kopie;
                }

            case RubyValueKind.String:
                return RubyValue.OfBytes([.. pWert.Bytes]);

            default:
                return pWert;
        }
    }


    /// <summary>
    /// A number that is the same for one object and not for another.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <returns>Its number.</returns>
    /// <remarks>
    /// <strong>And it is counted, and not made from the address.</strong>
    /// Ruby counts objects, **and a reader that made the number from the
    /// place in memory would give the same number twice** — a garbage
    /// collection between two calls would hand it out again,
    /// **and `list.uniq` would have two different heroes in one entry.**
    /// </remarks>
    /// <summary>
    /// Whether a name is one this reader answers as an operator.
    /// </summary>
    /// <param name="pName">The name, as `send` received it.</param>
    /// <returns>true when it is an operator.</returns>
    /// <remarks>
    /// <strong>And a list, and not a guess.</strong>
    /// <c>send(:+)</c>, <c>send(:[])</c> and <c>send(:&lt;=&gt;)</c> are the
    /// names a plugin writes,
    /// **and a reader that tried every name as an operator would answer a
    /// value for a method that does not exist** — and the game would carry
    /// a number it never computed.
    /// </remarks>
    private static bool OperatorName(string pName) => pName is
        "+" or "-" or "*" or "/" or "%" or "**" or "==" or "!=" or "<"
        or ">" or "<=" or ">=" or "<=>" or "<<" or ">>" or "&" or "|"
        or "^" or "[]" or "[]=" or "==" or "===";



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
                // **Und ein Fehler traegt seinen Klassennamen bei sich, und
                // nicht den Namen seiner Hülle.** `e.class` waere sonst
                // `Object`,
                // **und der Satz, mit dem ein Spiel seinen eigenen Fehler
                // unterscheidet -- `if e.class == ZeroDivisionError` -- waere
                // immer falsch.**
                if (pEmpfaenger.ClassName == "Fehler"
                    && pEmpfaenger.Felder.TryGetValue("@klasse", out var fehlerKlasse))
                {
                    return fehlerKlasse;
                }

                return RubyValue.OfSymbol(
                    pEmpfaenger.Kind == RubyValueKind.Nil ? "NilClass" : "Object");

            // **Und die Fragen, die ein Spiel an sein eigenes Objekt
            // stellt.** `instance_variable_get(:@hp)` steht in jedem
            // Plugin, das `@ivars` durchsucht,
            // **und `send` ist der Satz, mit dem ein Plugin eine Methode
            // aufruft, deren Namen es erst zur Laufzeit kennt.**
            case "instance_variable_get" or "instance_variable_set"
                or "instance_variables" or "instance_variable_defined?"
                or "remove_instance_variable":
                return InstanzVariable(
                    pEmpfaenger, pMethode, pArgumente);

            case "send" or "public_send" or "__send__":
                // **Und `send` nimmt einen Namen und ruft die Methode.**
                // `1.send(:+, 2)` ist `1 + 2`,
                // **und das ist der Satz, mit dem ein Plugin einen
                // Operator ueber seinen Namen anwendet** -- **und ein
                // Leser, der `send` ablehnt, wuerde einem Plugin, das
                // `list.send(:sort!)` schreibt, jede Aenderung verweigern.**
                if (pArgumente.Count == 0
                    || pArgumente[0].Kind != RubyValueKind.Symbol)
                {
                    _diagnostics.Add(
                        pMethode + " takes the name of a method as a symbol, "
                            + "and the first argument is not one; a reader "
                            + "that took any value would have called a "
                            + "method named after whatever the game wrote");
                    return RubyValue.Nil;
                }

                // **And the call goes through the same search as a call
                // with a written name.** That is why `send` lives here and
                // not at the host,
                // **and a reader that resolved the name itself would go
                // around `super` and the receiver.**
                var gesuchterName = pArgumente[0].Name ?? string.Empty;
                var weitere = System.Linq.Enumerable
                    .Skip(pArgumente, 1).ToList();
                var eigene = EigeneMethode(pEmpfaenger, gesuchterName);
                if (eigene != null)
                {
                    return Aufrufen(
                        eigene, weitere,
                        pEmpfaenger.Kind == RubyValueKind.Object
                            ? pEmpfaenger.ClassName
                            : gesuchterName,
                        pEmpfaenger);
                }

                // **And the operators, because `1.send(:+, 2)` is `1 + 2`.**
                // An operator is not a method under a name in this reader,
                // **and a reader that only asked `WertMethode` would answer
                /// nil** -- **and `send` is exactly the sentence a plugin
                // uses when it applies an operator by its name.**
                if (OperatorName(gesuchterName) && weitere.Count == 1)
                {
                    return Apply(
                        gesuchterName, pEmpfaenger, weitere[0]);
                }

                // **And the value's other methods.**
                var wertAntwort = WertMethode(
                    pEmpfaenger, gesuchterName, weitere);
                if (wertAntwort != null)
                {
                    return wertAntwort;
                }

                _diagnostics.Add(
                    gesuchterName + " is a name this value has no method "
                        + "under, and send does not guess; a reader that took "
                        + "any name would have called a method nobody wrote");
                return RubyValue.Nil;

            case "instance_of?":
                return RubyValue.OfBoolean(
                    pArgumente.Count == 1
                    && pArgumente[0].Kind == RubyValueKind.Symbol
                    && pArgumente[0].Name == pEmpfaenger.ClassName);

            case "object_id":
                // **Und eine Zahl, die pro Objekt verschieden ist.**
                // `equal?` und `object_id` sind derselbe Satz,
                // **und eine feste Zahl wuerde jedes Objekt gleich
                // machen** -- **und `list.uniq` haette dann zwei verschiedene
                // Helden zu einer gemacht.**
                return RubyValue.OfInteger(InstanzNummer(pEmpfaenger));

            case "__method__":
                // **Und der Name der Methode, die gerade laeuft.** Ruby gibt
                // ein Symbol zurueck,
                // **und ein Leser, das nil gibt, schweigt** -- **und
                // `__method__` wird genau dann gebraucht, wenn man nicht
                // weiss, in welcher Methode man ist.**
                return RubyValue.OfSymbol(
                    string.IsNullOrEmpty(_aktuelleMethode)
                        ? "Object"
                        : _aktuelleMethode);

            case "dup" or "clone":
                // **Und `dup` macht eine Kopie, und nicht denselben Wert
                // noch einmal.** `a = Held.new; b = a.dup; b.n = 2`
                // **laesst `a.n` bei 1**,
                // **und ein Leser, der den Empfaenger zurueckgab, wuerde
                // `a.n` auch auf 2 setzen** -- **und ein Spiel, das zwei
                // Figuren aus einem Helden macht, haette dieselbe Figur
                // zweimal, und jede Aenderung an der einen waere an der
                // anderen sichtbar.**
                return Kopie(pEmpfaenger);

            case "itself":
                return pEmpfaenger;

            case "freeze" or "frozen?":
                // **Und `freeze` gibt den Empfaenger zurueck, weil es hier
                // nichts einzufrieren gibt.** `frozen?` ist immer wahr,
                // **und ein Leser, der `false` sagte, wuerde jedes Skript
                // stoppen, das seinen Zustand einfriert**, **obwohl der
                // Zustand sich trotzdem aendern wuerde** -- **das ist die
                // schlimmere Haelfte des Vertrags: der Code glaubt, er
                // sei sicher, und ist es nicht.**
                return pMethode == "frozen?"
                    ? RubyValue.OfBoolean(true)
                    : pEmpfaenger;

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
        // **Und `raise` ist Sprache, und kein Aufruf an den Host.** Ohne
        // sie laeuft ein Skript nach seinem eigenen Fehlerpfad weiter,
        // **und genau das ist der Unterschied zwischen einem Spiel, das
        // sich weigert, und einem Spiel, das abstuerzt.**
        // **Und `sprintf` und `printf` sind `Kernel`-Methoden, und
        // `sprintf` ist genau das, was `String#%` tut.** Das ist der
        // Satz, den jedes Skript schreibt, das eine Zahl in eine Meldung
        // setzt,
        // **und ein Leser, der nur den Operator kannte, wuerde beiden
        // Namen *„has no method 'sprintf' on this host"* sagen** --
        // **und `printf` schreibt, waehrend `sprintf` zurueckgibt.**
        if ((methode == "sprintf" || methode == "printf")
            && empfaenger.Kind == RubyValueKind.Symbol
            && argumente.Count > 0
            && argumente[0].Kind == RubyValueKind.String)
        {
            var formatiert = ProzentFormatieren(
                argumente[0],
                argumente.Count > 1
                    ? RubyValue.OfArray([.. argumente.Skip(1)])
                    : RubyValue.OfArray([]));
            if (formatiert != null)
            {
                // **Und `printf` gibt nil zurueck, weil es schreibt.**
                // Ruby gibt nil zurueck,
                // **und ein Leser, der den Text zurueckgaebe, haette
                // `printf(...)` in einer Kette**, **und `puts` und `printf`
                // hintereinander waere dann eine Ausgabe in einer
                // Variablen.**
                return methode == "printf" ? RubyValue.Nil : formatiert;
            }
        }

        if (methode == "raise" && empfaenger.Name == "self")
        {
            throw RubyFehlerAus(argumente);
        }

        // **Und `require` und `load` sind auch Sprache, und nicht nur ein
        // Aufruf an den Host.** Sie brauchen drei Dinge, die nur der
        // Interpreter hat: **den Lexer, den Parser und sich selbst**,
        // **und ein Host, der die Datei gibt.**
        // **Ohne diese drei war `require` eine Ablehnung ueber den Host**
        // -- **und damit waere jedes VX- und VX-Ace-Skript unlesbar,
        // denn die laden ihr halbes System nach.**

        // **Und `require_relative` fragt nach einem Namen, der neben dem
        // laufenden Skript liegt.** `require` fragt nach dem Namen, wie er
        // geschrieben ist,
        // **und `require_relative "util"` fragt nach `util` neben der Datei,
        // die es schreibt** -- **und die Datei, die es schreibt, weiss nur
        // der Aufrufer.**
        if ((methode == "require" || methode == "load"
            || methode == "require_relative")
            && empfaenger.Name == "self"
            && argumente.Count == 1
            && argumente[0].Kind == RubyValueKind.String)
        {
            // **Und `require_relative` ist ein `require`.** Beide merken
            // sich, was sie geladen haben, **und nur `load` vergisst** --
            // **und ein Leser, der `methode == "require"` schrieb, gab
            // `require_relative` den Namen von `load`**, **und dann
            // laedt jede Zeile dieselbe Datei noch einmal**, **und jedes
            // Mal laeuft der Klassenrumpf noch einmal** -- **und so
            // bekommt ein Spiel zwei `Window_Base`-Definitionen, und die,
            // die sein eigenes `super` findet, ist nicht die, die der
            // Spieler sieht.**
            return SkriptLaden(
                methode != "load",
                System.Text.Encoding.UTF8.GetString(argumente[0].Bytes),
                methode == "require_relative"
                    ? _skriptKette.Count > 0
                        ? _skriptKette[^1]
                        : null
                    : null);
        }

        // **Und ein Typ ist ein Name und wird befragt.** `M.include?(N)`,
        // `A.ancestors`, `K.instance_methods` -- **das ist die erste Zeile
        // von fast jedem VX-Plugin**, **und der Leser hat diese Fragen
        // bisher nur an Objekte gestellt.**
        // **Und die Antwort kommt aus zwei Quellen, die getrennt
        // entstehen:** die eingebundenen Module stehen in `Eingebunden`,
        // **und die Methoden stehen in `Methods`**, **und ein Leser, der
        // nur die eine liest, antwortet bei der anderen falsch.**
        if ((methode == "instance_methods" || methode == "instance_method"
            || methode == "include?" || methode == "included_modules"
            || methode == "ancestors" || methode == "name"
            || methode == "superclass" || methode == "to_s"
            || methode == "is_a?" || methode == "kind_of?"
            || methode == "method_defined?"
            || methode == "private_method_defined?"
            || methode == "public_method_defined?"
            || methode == "protected_method_defined?"
            || methode == "module_function")
            && empfaenger.Kind == RubyValueKind.Symbol
            && _types.ContainsKey(empfaenger.Name ?? string.Empty))
        {
            var typAntwort = TypBefragt(
                empfaenger.Name!, methode, argumente);
            if (typAntwort != null)
            {
                return typAntwort;
            }
        }


        if (methode == "new" && empfaenger.Kind == RubyValueKind.Symbol)
        {
            // **And `Struct.new(:a, :b)` builds a class and gives it
            // back.** That is neither an object nor a number,
            // **it is the beginning of a class, and it gets its fields as
            // attributes** -- **exactly the ones `attr_accessor` makes**,
            // because Ruby builds nothing of its own here either.
            if (empfaenger.Name == "Struct")
            {
                return StructNeue(argumente);
            }

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
                || methode == "define_singleton_method"
                || methode == "undef_method"
                || methode == "module_function"))
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


        // **Und ein Aufruf auf einen Namen geht die Kette nach oben.**
        // `A.x` where `x` is `def self.x` in `A`'s base class is a call
        // that Ruby finds by walking `RCLASS_SUPER`,
        // **and a reader that only looked at the class itself answered
        // nil** — **and `A.respond_to?(:x)` said `true` at the same
        // time**, so a plugin's `unless A.respond_to?(:x)` guard would
        // skip the write and the call would then fail.
        //
        // **Und `def self.x` im Typ selbst geht vor, und nicht der
        // `TypBefragt`-Umweg.** `M.x` where M wrote `def self.x` is a
        // method the type has for itself,
        // **und ein Leser, der hier immer `TypBefragt` befragte, wuerde
        // die Kopie finden, die `module_function` macht, und nicht die,
        // die das Skript geschrieben hat** — **und bei einem Modul, das
        // beides hat, gewinnt die falsche**.
        if (empfaenger.Kind == RubyValueKind.Symbol
            && empfaenger.Name != null
            && empfaenger.Name != "self"
            && _types.ContainsKey(empfaenger.Name)
            && (_types[empfaenger.Name].Methods
                    .ContainsKey("self." + methode)
                || !_types[empfaenger.Name].Methods.ContainsKey(methode)))
        {
            var typAntwort = TypBefragt(
                empfaenger.Name, methode, argumente);
            if (typAntwort != null)
            {
                return typAntwort;
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

        // **Und ein Struct wird wie eine Liste behandelt, und nicht als
        // eine Klasse mit Feldern.** `p[0]`, `p.to_a` und `p.each` sind
        // Feldzugriffe, nur in der Reihenfolge, in der die Felder
        // geschrieben wurden,
        // **und ein Leser, der das als "die Methode kennt der Host nicht"
        // ablehnte, wuerde `RPG::Actor.new(1, "Held")[1]` zu einer
        // Fehlermeldung machen** -- **und die Fehlermeldung waere ueber
        // den Host, obwohl der Struct es sehr wohl kann.**
        //
        // **Vor der Sammlung, weil ein Struct eine Liste *ist*.**
        // `p.size` ist die Zahl der Felder,
        // **und ohne das haette ein Leser die Felderzaehlung der
        // Listenlaenge gleichgesetzt** -- und ein Struct mit nil in einem
        // Feld haette eine andere Laenge als einer mit einer Zahl darin.
        var anStruct = StructMethode(empfaenger, methode, argumente);
        if (anStruct != null)
        {
            return anStruct;
        }

        // **Und ein Fehler ist ein Objekt mit zwei Feldern, und keine
        // Klasse mit zwei Methoden.** `e.message` ist der Satz, mit dem ein
        // Spiel seinen Fehler anzeigt,
        // **und ein Leser, der ihn ablehnte, wuerde daraus „the host does
        // not know this method" machen** -- **und der Satz, mit dem ein
        // Spiel seinen Fehler anzeigt, waere genau der, der nicht geht.**
        //
        // **Und `e.class` steht in der Sammlung und nicht hier**, denn die
        // Sammlung beantwortet `class` fuer jeden Empfaenger
        // -- **und dieser Zweig wuerde nie erreicht**, weil sie vorher
        // antwortet.
        // **Und ein Fehler ist ein Objekt mit zwei Feldern, und keine
        // Klasse mit zwei Methoden.** `e.message` und `e.class` sind die
        // zwei Saetze, die ein Spiel schreibt, wenn es einen Fehler
        // anzeigt,
        // **und ein Leser, der sie ablehnte, wuerde `e.message` zu „the
        // host does not know this method" machen** -- **und der Satz, mit
        // dem ein Spiel seinen Fehler anzeigt, waere genau der, der
        // nicht geht.**
        if (empfaenger.Kind == RubyValueKind.Object
            && empfaenger.ClassName == "Fehler"
            && argumente.Count == 0)
        {
            var feld = methode switch
            {
                "message" or "to_s" or "full_message" =>
                    RubyValue.OfBytes(
                        System.Text.Encoding.UTF8.GetBytes(
                            empfaenger.Felder.TryGetValue("@message", out var m)
                                && m.Kind == RubyValueKind.String
                                    ? System.Text.Encoding.UTF8.GetString(m.Bytes)
                                    : string.Empty)),
                "backtrace" => RubyValue.OfArray([]),
                _ => null,
            };
            if (feld != null)
            {
                return feld;
            }
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

        // **And a struct gets its fields from the arguments, and that is
        // the whole difference from a class with an `initialize`.**
        // `Punkt.new(3, 4)` writes `@x = 3` and `@y = 4`,
        // **and the order is the one the fields were written in** --
        // **a reader that looked the fields up by name would have needed
        // `Punkt.new(:y => 4)`, and `RPG::Actor.new(1, "Held")` does not
        // write that.**
        if (_types.TryGetValue(pKlasse, out var typ)
            && typ.Struct is not null)
        {
            StructFuellen(pKlasse, instanz, pArgumente);
        }

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
    /// <summary>
    /// The class `Struct.new` builds, and its fields become attributes.
    /// </summary>
    /// <param name="pArgumente">The field names, as written.</param>
    /// <returns>The class, as its name.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the name is a number that counts up.</strong> Ruby names
    /// them <c>Struct</c>, then <c>Struct::1</c> — <strong>and the number is
    /// not a count of anything, it just has to differ</strong> from the last
    /// one. A reader that named them by their field names would give two
    /// different structs the same type,
    /// <strong>and <c>is_a?</c> could not tell a game's weapon from its
    /// armour.</strong>
    /// </para>
    /// <para>
    /// <strong>And a field with a default is stored, and not ignored.</strong>
    /// <c>Struct.new(:a, :b, 0)</c> gives <c>b</c> the default 0,
    /// <strong>and a game that writes
    /// <c>Struct.new(:id, :name, "")</c> and then reads <c>b</c> expects the
    /// empty text</strong> — a reader that dropped it would hand nil to a
    /// field the game declared.
    /// </para>
    /// </remarks>
    private RubyValue StructNeue(IReadOnlyList<RubyValue> pArgumente)
    {
        var felder = new List<string>();
        var vorgaben = new Dictionary<string, RubyValue>(StringComparer.Ordinal);
        for(var stelle = 0; stelle < pArgumente.Count; stelle++)
        {
            var argument = pArgumente[stelle];
            if (argument.Kind != RubyValueKind.Symbol)
            {
                _diagnostics.Add(
                    "Struct.new was given something that is not a name, and a "
                        + "field without a name has nowhere to be read from");
                return RubyValue.Nil;
            }

            var feld = argument.Name ?? string.Empty;
            felder.Add(feld);

            // **And the value behind a field is its default.**
            if (stelle + 1 < pArgumente.Count
                && pArgumente[stelle + 1].Kind != RubyValueKind.Symbol)
            {
                vorgaben[feld] = pArgumente[stelle + 1];
                stelle++;
            }
        }

        _strukturZaehler++;
        var name = "Struct::"
            + _strukturZaehler.ToString(CultureInfo.InvariantCulture);
        var typ = new RubyType
        {
            Name = name,
            IsClass = true,
            Struct = new RubyStruct { Fields = felder },
        };
        _types[name] = typ;
        _strukturVorgaben[name] = vorgaben;

        // **And the fields get their accessors as attributes, because Ruby
        // builds nothing of its own here either.** `a.x` reads `@x` and
        // `a.x = 1` writes `@x`,
        // **and exactly those two pairs are what a struct needs** —
        // **the rest (`to_a`, `==`, `each`) is added further down.**
        foreach(var feld in felder)
        {
            typ.Methods[feld] = new RubyMethod
            {
                Name = feld,
                IsAttribute = true,
                Field = "@" + feld,
                IsWriter = false,
            };
            typ.Methods[feld + "="] = new RubyMethod
            {
                Name = feld + "=",
                IsAttribute = true,
                Field = "@" + feld,
                IsWriter = true,
            };
        }

        return RubyValue.OfSymbol(name);
    }

    /// <summary>
    /// Writes a struct's fields, from the arguments and then the defaults.
    /// </summary>
    /// <param name="pKlasse">The struct class's name.</param>
    /// <param name="pInstanz">The object to write into.</param>
    /// <param name="pArgumente">The values, in the order written.</param>
    /// <remarks>
    /// <para>
    /// <strong>And a field that got no value gets its default, and a field
    /// with no default gets nil.</strong> <c>Struct.new(:a, :b, 0)</c> and
    /// <c>Punkt.new(3)</c> — <strong>and that is the whole answer to "what is
    /// in this struct"</strong>, because a game's database is full of structs
    /// built with fewer values than they have fields.
    /// </para>
    /// <para>
    /// <strong>And the values go to the fields in order, and not by
    /// name.</strong> A struct's constructor takes them positionally, and
    /// <strong>the reader cannot know which value belongs to which field if it
    /// guessed</strong> — and a guess would put an actor's name where its id
    /// belongs, and the game's save would be a list of values in the wrong
    /// order.
    /// </para>
    /// </remarks>
    private void StructFuellen(
        string pKlasse,
        RubyValue pInstanz,
        IReadOnlyList<RubyValue> pArgumente)
    {
        var felder = _types[pKlasse].Struct!.Fields;
        _strukturVorgaben.TryGetValue(pKlasse, out var vorgaben);
        for(var stelle = 0; stelle < felder.Count; stelle++)
        {
            var feld = felder[stelle];
            if (stelle < pArgumente.Count)
            {
                pInstanz.Felder["@" + feld] = pArgumente[stelle];
            }
            else if (vorgaben != null && vorgaben.TryGetValue(feld, out var vorgabe))
            {
                pInstanz.Felder["@" + feld] = vorgabe;
            }
        }
    }

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



    /// <summary>
    /// `sub` and `gsub`, with a text, a pattern, or a block.
    /// </summary>
    /// <param name="pText">The text.</param>
    /// <param name="pMethode">`sub` or `gsub`.</param>
    /// <param name="pArgumente">What to look for and what to put there.</param>
    /// <returns>The new text, or nil when there is nothing to look for.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And `sub` replaces the first one and `gsub` all of
    /// them.</strong> That is the whole difference,
    /// <strong>and a reader that used <c>Replace</c> for both would turn a
    /// game that removes one mark from a name into one that removes
    /// all</strong> — and the name the game writes into its list would not
    /// be the one it looked for.
    /// </para>
    /// <para>
    /// <strong>And a block is the form every text rewriter uses.</strong>
    /// <c>gsub(/(\w+)/) { |w| w.upcase }</c>,
    /// <strong>and without it a game's item renamer, its tag filter and
    /// its dialogue formatter all fail at the same line.</strong>
    /// </para>
    /// <para>
    /// <strong>And the block gets the match, and the whole match.</strong>
    /// It is not given the name of a pattern group,
    /// <strong>because <c>$1</c> is how a game reads one</strong> — and
    /// that is the sentence the block and the global both serve.
    /// </para>
    /// </remarks>
    private RubyValue Ersetzt(
        string pText, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pArgumente.Count == 0)
        {
            _diagnostics.Add(
                pMethode + " needs what to look for, and got no arguments; a "
                    + "replacement without a search would change nothing and "
                    + "look like it worked");
            return RubyValue.Nil;
        }

        // **Und ein Muster darf laufen, weil es hier eine Schranke
        // gibt.** Die gemessene Grenze liegt beim Muster selbst,
        // **und ein Muster ohne Schranke ist ein Weg, ein Spiel von innen
        // anzuhalten** -- ein verschachteltes `*` ueber einen langen Text
        // tut es in ein paar hundert Millisekunden.
        // **Die erste Fassung hat jedes Muster abgelehnt, und damit auch
        // jedes Skript, das eines schreibt** --
        // **und die Absicht war Sicherheit, und die Wirkung war eine
        // Ablehnung ohne Grenze.** Die Grenze ist hier, und die Messung
        // sagt, welche.
        if (pArgumente[0].Kind == RubyValueKind.Regexp)
        {
            return ErsetztNachMuster(
                pText, pMethode, pArgumente[0], pArgumente);
        }

        if (pArgumente[0].Kind != RubyValueKind.String)
        {
            _diagnostics.Add(
                pMethode + " was given something that is neither a text nor a "
                    + "pattern to look for");
            return RubyValue.Nil;
        }

        var gesucht = AlsText(pArgumente[0]);
        if (gesucht.Length == 0)
        {
            return Text(pText);
        }

        var erstes = pText.IndexOf(gesucht, StringComparison.Ordinal);
        if (erstes < 0)
        {
            return Text(pText);
        }

        // **Und ein Block laeuft auch bei einem Text, und nicht nur bei
        // einem Muster.** `gsub("X") { |x| x * 2 }` ist derselbe Satz,
        // **und er ist der, den ein Spiel schreibt, wenn es ein Zeichen
        // umschreibt, ohne ein Muster zu bauen** --
        // **und ein Leser, der den Block nur im Musterpfad annahm, wuerde
        // hier den leeren Ersatz nehmen**, **und jedes X streichen, und der
        // Name waere weg.**
        if (pArgumente.Count > 1 && IstBlock(pArgumente[1]))
        {
            return ErsetztMitBlock(pText, pMethode, gesucht, pArgumente[1]);
        }

        if (pMethode == "sub")
        {
            var ersatz = pArgumente.Count > 1
                ? AlsText(pArgumente[1])
                : gesucht;
            return Text(pText[..erstes] + ersatz + pText[(erstes + gesucht.Length)..]);
        }

        // **Und `gsub` mit einer leeren Ersetzung braucht `Remove` und
        // nicht `Replace`.** `Replace` mit leerem Text tut in neueren
        // Laufzeiten nichts mehr,
        // **und ein Spiel, das eine Marke aus einem Namen streicht, haette
        // den Namen unveraendert behalten.**
        var ersatzGsub = pArgumente.Count > 1 && !IstBlock(pArgumente[1])
            ? AlsText(pArgumente[1])
            : string.Empty;
        if (ersatzGsub.Length == 0)
        {
            var neu = new System.Text.StringBuilder();
            var stelle = 0;
            while(erstes >= 0)
            {
                neu.Append(pText, stelle, erstes - stelle);
                stelle = erstes + gesucht.Length;
                erstes = pText.IndexOf(gesucht, stelle, StringComparison.Ordinal);
            }

            neu.Append(pText, stelle, pText.Length - stelle);
            return Text(neu.ToString());
        }

        return Text(pText.Replace(gesucht, ersatzGsub, StringComparison.Ordinal));
    }

    /// <summary>
    /// Whether a value is a block and not an answer.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <returns>true when it is a block.</returns>
    /// <remarks>
    /// <strong>And a block is a value and not a list of words.</strong>
    /// <c>gsub("X", "Y")</c> and <c>gsub("X") { "Y" }</c> sind derselbe
    /// Satz,
    /// **und der Leser muss sie unterscheiden koennen, ohne die Form zu
    /// raten** -- **und raten heisst hier: der Block als Text, und der
    /// Spieler sieht `X` und `Y` und eine leere Zeile dazwischen.**
    /// </remarks>
    /// <summary>
    /// `sub` and `gsub` with a pattern, and the block that answers each
    /// match.
    /// </summary>
    /// <param name="pText">The text.</param>
    /// <param name="pMethode">`sub` or `gsub`.</param>
    /// <param name="pMuster">The pattern, as written.</param>
    /// <param name="pArgumente">What to put there, and the block.</param>
    /// <returns>The new text, or nil when the pattern did not run.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the pattern runs inside the bound, and the bound is
    /// already written.</strong> <c>MusterBauen</c> and <c>LaufZaehlt</c>
    /// came with the first pattern this reader ever ran,
    /// **and the first version of this method refused every pattern
    /// instead** -- **a refusal with no bound, which is a refusal for
    /// safety's sake and not for any bound at all.** The bound is here,
    /// the same one `=~` uses,
    /// **and a game that rewrites its item names is no longer refused.**
    /// </para>
    /// <para>
    /// <strong>And the block gets the match, and the whole match.</strong>
    /// `gsub(/(\w+)/) { |w| w.upcase }`,
    /// **and a reader that gave the block only a group would make the
    /// commonest form of the sentence fail** -- and the game would
    /// uppercase nothing and draw the text unchanged.
    /// </para>
    /// <para>
    /// <strong>And `$1` is the group of the match being replaced, and not
    /// of the last one.</strong> `gsub` is a loop over the matches,
    /// **and a reader that ran the block after collecting them all would
    /// have every replacement see the last match's groups** -- and
    /// <c>gsub(/(\w+)/) { $1.capitalize }</c> is a game's name tidy.
    /// </para>
    /// </remarks>
    private RubyValue ErsetztNachMuster(
        string pText,
        string pMethode,
        RubyValue pMuster,
        IReadOnlyList<RubyValue> pArgumente)
    {
        var gebaut = MusterBauen(pMuster);
        if (gebaut?.Engine == null)
        {
            return RubyValue.Nil;
        }

        if (!gebaut.LaufZaehlt(pText.Length))
        {
            // **Und die Meldung nennt das Musster.** Die alte Fassung
            // sagte nur die Laenge,
            // **und eine Meldung ohne das Muster schickt den Leser
            // suchen** -- **und bei hundert Mustern in einem Spiel weiss
            // er dann nicht, welches von ihnen zu gross war.**
            _diagnostics.Add(
                pMethode + " was not run on " + pText.Length + " bytes, "
                    + "because the pattern /" + gebaut.Quelle + "/ is only "
                    + "run while the text stays under 4096 bytes; a pattern "
                    + "that runs long is a way to stop a game, and a reader "
                    + "without a bound gives that away");
            return RubyValue.Nil;
        }

        var block = pArgumente.Count > 1 && pArgumente[1].Kind == RubyValueKind.Proc
            ? pArgumente[1].Block
            : null;
        var ersatz = block == null && pArgumente.Count > 1
            ? AlsText(pArgumente[1])
            : null;
        var text = RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(pText));

        var Ergebnis = new System.Text.StringBuilder();
        var stelle = 0;
        foreach (System.Text.RegularExpressions.Match treffer
            in gebaut.Engine.Matches(pText))
        {

            Ergebnis.Append(pText, stelle, treffer.Index - stelle);

            // **Und `$1` gehoert zu diesem Treffer**, **und nicht zum
            // letzten** -- **und das ist der ganze Grund, warum es den
            // Block gibt.**
            var alterTreffer = _letzterTreffer;
            _letzterTreffer = new TrefferDaten
            {
                Getroffen = true,
                Vorher = pText[..treffer.Index],
                Nachher = pText[(treffer.Index + treffer.Length)..],
                Ganz = treffer.Value,
                Stelle = treffer.Index,
                Gruppen =
                [
                    .. Enumerable.Range(1, Math.Max(0, treffer.Groups.Count - 1))
                        .Select(gruppe => treffGruppe(treffer, gruppe)),
                ],
                GruppenDa =
                [
                    .. Enumerable.Range(1, Math.Max(0, treffer.Groups.Count - 1))
                        .Select(gruppe => treffer.Groups[gruppe].Success),
                ],
            };
            try
            {
                if (block != null)
                {
                    var antwort = BlockAufrufen(
                        block,
                        [text, RubyValue.OfInteger(treffer.Index)],
                        text);
                    Ergebnis.Append(
                        antwort.Kind == RubyValueKind.String
                            ? System.Text.Encoding.UTF8.GetString(antwort.Bytes)
                            : WertAlsText(antwort));
                }
                else
                {
                    Ergebnis.Append(ErsatzzEingesetzt(
                        ersatz ?? string.Empty, treffer.Value, _letzterTreffer.Gruppen));
                }
            }
            finally
            {
                _letzterTreffer = alterTreffer;
            }

            stelle = treffer.Index + treffer.Length;
            if (pMethode == "sub")
            {
                break;
            }
        }

        Ergebnis.Append(pText, stelle, pText.Length - stelle);
        return Text(Ergebnis.ToString());
    }





    /// <summary>
    /// The replacement text, with `\1` and `\0` standing for the groups.
    /// </summary>
    /// <param name="pErsatz">The replacement, as written.</param>
    /// <param name="pGanze">The whole match.</param>
    /// <param name="pGruppen">The pattern's groups, as values.</param>
    /// <returns>The text that replaces the match.</returns>
    /// <remarks>
    /// <strong>And `\0` is the whole match and `\1` the first group.</strong>
    /// That is Ruby 1.8,
    /// **and a reader that only knew `\1` would leave `\0` standing in
    /// every replacement** -- and a game that swaps the halves of a name
    /// with `\0` would write the two characters `\` and `0` into it.
    /// </remarks>
    private static string ErsatzzEingesetzt(
        string pErsatz,
        string pGanze,
        IReadOnlyList<string> pGruppen)
    {
        if (!pErsatz.Contains('\\'))
        {
            return pErsatz;
        }

        var Ergebnis = new System.Text.StringBuilder();
        for(var stelle = 0; stelle < pErsatz.Length; stelle++)
        {
            if (pErsatz[stelle] != '\\' || stelle + 1 >= pErsatz.Length)
            {
                Ergebnis.Append(pErsatz[stelle]);
                continue;
            }

            stelle++;
            var ziffer = pErsatz[stelle];
            if (ziffer is >= '0' and <= '9')
            {
                var nummer = ziffer - '0';
                if (nummer == 0)
                {
                    Ergebnis.Append(pGanze);
                }
                else if (nummer > 0 && nummer <= pGruppen.Count)
                {
                    // **Und die Liste der Gruppen faengt bei 0 an, und die
                    // Ziffer im Ersatzerzeugnis bei 1.** `\1` ist die erste
                    // Gruppe,
                    // **und ein Leser, der die Ziffer direkt als Index nahm,
                    // gab `\1` die zweite Gruppe zurueck** --
                    // **und `"anna bob".gsub(/(\w+) (\w+)/, "\\2 \\1")`
                    // antwortete dann `' bob'`, weil beide Gruppen
                    // vertauscht waren** -- **und das sieht wie ein
                    // Zeichenfehler aus und ist einer.**
                    Ergebnis.Append(pGruppen[nummer - 1] ?? string.Empty);
                }

                continue;
            }

            Ergebnis.Append('\\');
            Ergebnis.Append(ziffer);
        }

        return Ergebnis.ToString();
    }




    /// <summary>
    /// `sub` and `gsub` with a text to look for, and a block that answers.
    /// </summary>
    /// <param name="pText">The text.</param>
    /// <param name="pMethode">`sub` or `gsub`.</param>
    /// <param name="pGesucht">The text to look for.</param>
    /// <param name="pBlock">The block, as a value.</param>
    /// <returns>The new text.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the block gets the whole match and the place.</strong>
    /// `gsub("X") { |x| x * 2 }`,
    /// **and a reader that gave the block only the text it found would make
    /// the block unable to ask where it was</strong> — and a game that
    /// doubles a letter only every second time needs the place.
    /// </para>
    /// <para>
    /// <strong>And the block's answer is the text that goes there, and not
    /// a conversion of it.</strong> A block that answers a number and
    /// `<c>to_s`es it is how a game turns a mark into its own name.
    /// </para>
    /// </remarks>
    private RubyValue ErsetztMitBlock(
        string pText,
        string pMethode,
        string pGesucht,
        RubyValue pBlock)
    {
        var text = RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(pText));
        var Ergebnis = new System.Text.StringBuilder();
        var stelle = 0;
        var treffer = pText.IndexOf(pGesucht, StringComparison.Ordinal);
        while(treffer >= 0)
        {
            Ergebnis.Append(pText, stelle, treffer - stelle);
            var antwort = BlockAufrufen(
                pBlock.Block,
                [RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(pGesucht)),
                 RubyValue.OfInteger(treffer)],
                text);
            Ergebnis.Append(
                antwort.Kind == RubyValueKind.String
                    ? System.Text.Encoding.UTF8.GetString(antwort.Bytes)
                    : WertAlsText(antwort));
            stelle = treffer + pGesucht.Length;
            if (pMethode == "sub")
            {
                break;
            }

            treffer = pText.IndexOf(pGesucht, stelle, StringComparison.Ordinal);
        }

        if (stelle <= pText.Length)
        {
            Ergebnis.Append(pText, stelle, pText.Length - stelle);
        }

        return Text(Ergebnis.ToString());
    }


    private static bool IstBlock(RubyValue pWert)
        => pWert.Kind == RubyValueKind.Proc;



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
    /// <summary>
    /// The methods a struct answers by its fields, in the order they were
    /// written.
    /// </summary>
    /// <param name="pEmpfaenger">The struct.</param>
    /// <param name="pMethode">The method's name.</param>
    /// <param name="pArgumente">What the call carried.</param>
    /// <returns>The answer, or null when this is not a struct method.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And `to_a` is the fields in the order they were written.</strong>
    /// **That order is the whole reason a struct exists** — it is what a
    /// save file is,
    /// <strong>and a reader that sorted them or used a dictionary would write
    /// a save no other game can read.</strong>
    /// </para>
    /// <para>
    /// <strong>And `==` is the fields, one by one, and not the
    /// identity.</strong> A game that looks for an item in a list writes
    /// <c>liste.include?(item)</c>,
    /// <strong>and a reader that compared the objects themselves would never
    /// find it</strong> — and every inventory screen would be empty.
    /// </para>
    /// <para>
    /// <strong>And a field that is nil counts as a value.</strong>
    /// <c>Struct.new(:a).new == Struct.new(:a).new</c> is true,
    /// <strong>because both have nothing in them</strong> — and a reader
    /// that treated nil as "no answer" would compare two empty structs as
    /// different, and a game's "do I already have this" check would always
    /// say no.
    /// </para>
    /// </remarks>
    private RubyValue? StructMethode(
        RubyValue pEmpfaenger,
        string pMethode,
        IReadOnlyList<RubyValue> pArgumente)
    {
        if (pEmpfaenger.Kind != RubyValueKind.Object
            || pEmpfaenger.ClassName is not { Length: > 0 } name
            || !_types.TryGetValue(name, out var typ)
            || typ.Struct is null)
        {
            return null;
        }

        var felder = typ.Struct.Fields;
        switch (pMethode)
        {
            case "to_a" or "to_ary" or "values":
                return RubyValue.OfArray(
                    [.. felder.Select(feld => FeldOderNil(pEmpfaenger, feld))]);

            case "[]":
                if (pArgumente.Count == 0)
                {
                    return RubyValue.OfArray(
                        [.. felder.Select(feld => FeldOderNil(pEmpfaenger, feld))]);
                }

                if (pArgumente[0].Kind == RubyValueKind.Integer)
                {
                    var stelle = pArgumente[0].Integer;
                    if (stelle < 0)
                    {
                        stelle += felder.Count;
                    }

                    return stelle >= 0 && stelle < felder.Count
                        ? FeldOderNil(pEmpfaenger, felder[(int)stelle])
                        : RubyValue.Nil;
                }

                // **Und ein Name ist auch ein Feld.** `p[:x]` ist `p.x`,
                // **und das ist der Fall, den ein Spiel schreibt, wenn es
                // Felder in einer Schleife liest** -- **ein Leser, der nur
                // Zahlen nahm, wuerde `held[:hp]` nil geben**, und eine
                // Statusleiste, die so etwas liest, zeichnete nichts.
                if (pArgumente[0].Kind == RubyValueKind.Symbol)
                {
                    return FeldOderNil(pEmpfaenger, pArgumente[0].Name ?? string.Empty);
                }

                return RubyValue.Nil;

            case "[]=":
                if (pArgumente.Count < 2)
                {
                    return RubyValue.Nil;
                }

                var ziel = pArgumente[0].Kind == RubyValueKind.Symbol
                    ? pArgumente[0].Name ?? string.Empty
                    : pArgumente[0].Integer >= 0 && pArgumente[0].Integer < felder.Count
                        ? felder[(int)pArgumente[0].Integer]
                        : string.Empty;
                if (ziel.Length == 0)
                {
                    return RubyValue.Nil;
                }

                pEmpfaenger.Felder["@" + ziel] = pArgumente[1];
                return pArgumente[1];

            case "size" or "length" or "count_fields":
                return RubyValue.OfInteger(felder.Count);

            case "members" or "keys":
                return RubyValue.OfArray(
                    [.. felder.Select(feld => RubyValue.OfSymbol(feld))]);

            // **Und `each` geht ueber die Felder, in ihrer Reihenfolge.**
            // **Und nicht ueber die Felder mit ihrem Namen**,
            // **weil Ruby 1.8.1 `struct.each` ohne Block eine Liste gibt**
            // **und ein Spiel, das `held.each { |v| }` schreibt, will die
            // Werte in der Reihenfolge, in der sie gespeichert sind.**
            case "each" or "each_pair":
                return RubyValue.OfArray(
                    [.. felder.Select(feld => FeldOderNil(pEmpfaenger, feld))]);

            case "==" or "eql?":
                if (pArgumente.Count == 0
                    || pArgumente[0].Kind != RubyValueKind.Object
                    || pArgumente[0].ClassName != name)
                {
                    return RubyValue.OfBoolean(false);
                }

                // **Und zwei verschiedene Struct-Arten sind nie gleich.**
                // Eine Waffe und eine Ruestung koennen beide aus drei
                // Zahlen bestehen,
                // **und ein Leser, der nur die Felder verglich, wuerde ein
                // Schwert fuer eine Ruestung halten** -- **und
                // `liste.include?` wuerde das finden.
                for(var stelle = 0; stelle < felder.Count; stelle++)
                {
                    if (!Truthy(Apply(
                        "==",
                        FeldOderNil(pEmpfaenger, felder[stelle]),
                        FeldOderNil(pArgumente[0], felder[stelle]))))
                    {
                        return RubyValue.OfBoolean(false);
                    }
                }

                return RubyValue.OfBoolean(true);

            case "to_s" or "inspect":
                return RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(
                    "#<" + name + " "
                    + ">"));

            default:
                return null;
        }
    }

    /// <summary>
    /// A struct's field, or nil when nothing was put into it.
    /// </summary>
    /// <param name="pInstanz">The struct.</param>
    /// <param name="pFeld">The field's name, without the <c>@</c>.</param>
    /// <returns>The value.</returns>
    /// <remarks>
    /// <strong>Und nil, und nicht eine Fehlermeldung.</strong> Ruby gibt nil
    /// fuer ein Feld, in dem nichts steht,
    /// **und ein Leser, der einen Fehler daraus gemacht haette, wuerde ein
    /// Feld, das die Reihenfolge nicht erreicht, zum Abbruch fuehren** --
    /// **und genau das passiert in jedem Spiel, das einen Struct mit weniger
    /// Werten baut, als er Felder hat.**
    /// </remarks>
    private static RubyValue FeldOderNil(RubyValue pInstanz, string pFeld)
        => pInstanz.Felder.TryGetValue("@" + pFeld, out var wert)
            ? wert
            : RubyValue.Nil;


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

            case "min" or "max":
                return Kleinste(pEmpfaenger, pMethode == "max");

            case "flatten":
                // **Und `flatten` nimmt die Listen in der Liste weg, so
                // tief es geht.** `[[1, [2]], 3]` ist `1, 2, 3`,
                // **und ein Leser, der nur eine Ebene flach machte, wuerde
                // eine Liste in der Liste lassen** — and a game that
                // flattens its event rows would draw a row that is a list.
                return RubyValue.OfArray(
                    [.. Abgeflacht(pEmpfaenger.Items)]);

            case "take" or "drop":
                // **Und eine Zahl, die nicht da ist, heisst "alles" oder
                // "nichts".** `liste.take(nil)` ist die ganze Liste,
                // **und `liste.take(0)` ist keine** — **das sind zwei
                // verschiedene Anfragen, und `0` ist eine davon.**
                if (pArgumente.Count == 0
                    || pArgumente[0].Kind != RubyValueKind.Integer)
                {
                    return pMethode == "take" ? pEmpfaenger : RubyValue.OfArray([]);
                }

                var anzahl = (int)Math.Max(0, pArgumente[0].Integer);
                return RubyValue.OfArray(pMethode == "take"
                    ? [.. pEmpfaenger.Items.Take(anzahl)]
                    : [.. pEmpfaenger.Items.Skip(anzahl)]);

            case "compact":
                // **Und `compact` nimmt die nil heraus, und nicht die
                // falschen.** `[1, nil, 2].compact` ist `[1, 2]`,
                // **und `reject { |x| !x }` waere dasselbe, aber es laeuft
                // den Block und gibt false heraus** — and a game that
                // compacts a list of lookups would get `false` in it.
                return RubyValue.OfArray(
                    [.. pEmpfaenger.Items.Where(w => w.Kind != RubyValueKind.Nil)]);

            case "sum":
                // **Und `sum` ist die Summe und nicht die Liste.**
                // `liste.sum` ist eine Zahl,
                // **und ein Leser, der die Liste gabe, wuerde einem
                // Schadensfenster, das die Summe anzeigt, eine Liste
                // anzeigen** — and the number in it would be a number of
                // values rather than a total.
                if (pEmpfaenger.Items.Count == 0)
                {
                    return RubyValue.OfInteger(0);
                }

                var summe = 0d;
                foreach (var wert in pEmpfaenger.Items)
                {
                    summe += Groesse(wert);
                }

                return RubyValue.OfReal(summe);

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
            // **Und `break` endet die Zaehlung.** `3.times { |i| break
            // if i == 2 }` soll zweimal laufen,
            // **und ohne diese Zeile lief es dreimal und der dritte Durchgang
            // war der, den das Spiel abbrechen wollte.**
            if (_gebrochen)
            {
                _gebrochen = false;
                _abbruchWert = RubyValue.Nil;
                break;
            }

            BlockAufrufen(
                block, [RubyValue.OfInteger(i)], _self ?? pEmpfaenger);
        }

        return pEmpfaenger;
    }



    /// <summary>
    /// How large a value is, for the comparisons a list makes.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The size.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Und es ist eine Zahl, und nicht die Laenge der
    /// Antwort.</strong> `sort_by { |a| a.level }` misst das Level,
    /// **und ein Leser, der die Liste, die der Block gibt, vergleichen
    /// wuerde nach der Laenge ordnen** — and a game that sorts its actors
    /// by level would sort them by how long their name is.
    /// </para>
    /// <para>
    /// <strong>Und zwei Werte, die sich nicht vergleichen lassen, sind
    /// gleich gross.</strong> Das ist Rubys Weg, es zu umgehen,
    /// <strong>und ein Leser, der nil zu null machte, wuerde alle
    /// unvergleichbaren Werte an den Anfang sortieren**
    /// — and a game whose list mixes a number and a name would draw its
    /// rows in an order nobody wrote.
    /// </para>
    /// </remarks>
    /// <summary>How large a value is, for the comparisons a list makes.</summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The size.</returns>
    private static double Groesse(RubyValue pWert) => pWert.Kind switch
    {
        RubyValueKind.Integer => pWert.Integer,
        RubyValueKind.Float => pWert.Real,
        RubyValueKind.String => pWert.Bytes.Length,
        _ => 0,
    };

    /// <summary>
    /// The smallest or largest value of a list, and it is the value and not
    /// a measurement.
    /// </summary>
    /// <param name="pListe">The list.</param>
    /// <param name="pGroesste">Whether to look for the largest.</param>
    /// <returns>The value, or nil when the list is empty.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Und es ist nil fuer eine leere Liste, und nicht 0.</strong>
    /// `liste.min` auf nichts hat keine Antwort,
    /// <strong>und ein Leser, der 0 gaebe, wuerde eine leere Party mit
    /// einem Level von null zeigen** — and a game would draw a bar for a
    /// level nobody has.
    /// </para>
    /// <para>
    /// <strong>Und der erste bei Gleichstand.</strong> Zwei Schauspieler
    /// mit demselben Level,
    /// <strong>und ein Leser, der den letzten nähme, wuerde die Party
    /// anders stellen, je nachdem, wie die Liste entstanden ist** — and a
    /// game that sorts its party would show a different lead depending on
    /// the order a filter happened to leave.
    /// </para>
    /// </remarks>
    private static RubyValue Kleinste(RubyValue pListe, bool pGroesste)
    {
        if (pListe.Kind != RubyValueKind.Object)
        {
            return RubyValue.Nil;
        }

        var bestes = RubyValue.Nil;
        foreach (var wert in pListe.Items)
        {
            if (bestes.Kind == RubyValueKind.Nil)
            {
                bestes = wert;
                continue;
            }

            var vergleich = Apply("<=>", wert, bestes);
            if (vergleich.Kind != RubyValueKind.Integer)
            {
                continue;
            }

            if (pGroesste ? vergleich.Integer > 0 : vergleich.Integer < 0)
            {
                bestes = wert;
            }
        }

        return bestes;
    }

    /// <summary>
    /// The values of a list with the lists inside it opened up, all the
    /// way down.
    /// </summary>
    /// <param name="pWerte">The values.</param>
    /// <returns>The opened-up values.</returns>
    /// <remarks>
    /// <strong>Und ohne Grenze, und das ist der Unterschied zu
    /// `flat_map`.</strong> `[[1, [2]], 3]` gibt `1, 2, 3`,
    /// <strong>und ein Leser, der nur eine Ebene oeffnete, wuerde eine
    /// Liste in der Liste lassen** — and a game that flattens its event
    /// rows would draw a row that is a list.
    /// </remarks>
    private static IEnumerable<RubyValue> Abgeflacht(IReadOnlyList<RubyValue> pWerte)
    {
        foreach (var wert in pWerte)
        {
            if (wert.Kind == RubyValueKind.Object && wert.IsList)
            {
                foreach (var inne in Abgeflacht(wert.Items))
                {
                    yield return inne;
                }
            }
            else
            {
                yield return wert;
            }
        }
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
        // **Und die Liste der Namen, die hier beantwortet werden.** Sie steht
        // hier und nicht in der Fallunterscheidung weiter unten,
        // **weil der Block-Check vor der Fallunterscheidung laeuft** --
        // **und ein Name, der nicht in dieser Liste steht, bekommt die
        // Antwort des Hosts und nicht diese Blockmeldung.**
        //
        // **Und `min` und `max` stehen nicht hier, weil sie keinen Block
        // brauchen.** Sie sind ein Teil von `SammlungMethode`,
        // **denn ein Block, den sie nicht brauchen, waere eine erfundene
        // Pflicht** -- **und ein Leser, der `liste.min` ablehnte, weil
        // kein Block da war, wuerde jede Statuszeile ablehnen, die den
        // kleinsten Wert zeigt.**
        if (pMethode is not ("map" or "select" or "filter" or "reject"
            or "each" or "each_with_index" or "reverse_each"
            or "any?" or "all?" or "none?" or "one?"
            or "find" or "detect" or "find_all"
            or "inject" or "reduce" or "each_with_object"
            or "group_by" or "partition" or "sort_by" or "min_by" or "max_by"
            or "flat_map"))
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

        // **Und die Schleife nimmt einen oder zwei Werte.** `map` gibt dem
        // Block einen, `inject` und `each_with_object` geben ihm zwei
        // (das, was gesammelt wird, und den Wert),
        // **und ein Leser, der immer nur einen gab, wuerde
        // `each_with_object { |x, l| l.push(x) }` die Liste in `x`
        // schreiben lassen** — **und das ist der Satz, mit dem ein
        // Statusfenster seine Zeilen baut.**
        var ergebnis = new List<RubyValue>();
        var gesammelt = pArgumente.Count > 0 && pArgumente[0].Kind != RubyValueKind.Proc
            ? pArgumente[0]
            : null;
        var gefunden = RubyValue.Nil;
        List<RubyValue>? wahr = null;
        List<RubyValue>? falsch = null;

        for (var stelle = 0; stelle < pEmpfaenger.Items.Count; stelle++)
        {
            var wert = pEmpfaenger.Items[stelle];

            // **Und `inject` ohne Anfang nimmt das erste Element als
            // Anfang.** Ruby macht das,
            // **und ein Leser, der bei null anfing, wuerde die Summe einer
            // Liste mit einem Text als Anfang nicht berechnen koennen** —
            // and that is a list of numbers written by a game.
            if (pMethode is "inject" or "reduce")
            {
                if (gesammelt == null)
                {
                    gesammelt = wert;
                    continue;
                }

                gesammelt = BlockAufrufen(
                    block, [gesammelt, wert], pEmpfaenger);
                continue;
            }

            if (pMethode == "each_with_object")
            {
                // **Und der zweite Wert ist das, was der Block
                // zurueckgibt.** `each_with_object` baut ein Ding und gibt es
                // zurueck,
                // **und das Ding waechst in diesem Lauf** — **ein Leser, der
                // nur den Wert gabe, wuerde `l.push(x)` auf einem nil
                // laufen lassen** und jedes Fenster waere leer.
                gesammelt = BlockAufrufen(
                    block, [wert, gesammelt ?? RubyValue.Nil], pEmpfaenger);
                continue;
            }

            var aufgerufen = BlockAufrufen(
                block, [wert, RubyValue.OfInteger(stelle)], pEmpfaenger);
            switch (pMethode)
            {
                case "map":
                    ergebnis.Add(aufgerufen);
                    break;

                case "flat_map":
                    // **Und `flat_map` nimmt die Liste, die der Block
                    // gibt, und legt sie in die Antwort.** `[1, 2]
                    // .flat_map { |x| [x, x] }` ist vier Werte lang,
                    // **und ein Leser, der die Listen in die Antwort legte,
                    // wuerde zwei Listen geben, wo das Spiel vier Zeilen
                    // zeichnet.**
                    if (aufgerufen.Kind == RubyValueKind.Object && aufgerufen.IsList)
                    {
                        ergebnis.AddRange(aufgerufen.Items);
                    }
                    else
                    {
                        ergebnis.Add(aufgerufen);
                    }

                    break;

                case "select" or "filter" or "find_all":
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

                case "none?":
                    if (Truthy(aufgerufen))
                    {
                        return RubyValue.OfBoolean(false);
                    }

                    break;

                case "one?":
                    if (Truthy(aufgerufen))
                    {
                        gefunden = RubyValue.OfBoolean(true);
                    }

                    break;

                // **`find` gibt den WERT und nicht den Ort.** Das ist der
                // ganze Unterschied zwischen `find` und `index`,
                // **und ein Leser, der den Ort gabe, wuerde einem Skript,
                // das `find { |x| x.name == "Held" }` schreibt, eine Zahl
                // geben, wo es einen Schauspieler erwartet** — and a game's
                // party would hold a number where it holds a name.
                case "find" or "detect":
                    if (Truthy(aufgerufen))
                    {
                        return wert;
                    }

                    break;

                // **`min_by` und `max_by` vergleichen, was der Block
                // gibt, und geben den WERT der Liste zurueck.** Nicht den
                // Vergleichswert,
                // **und das ist der Unterschied, den ein Skript bemerkt,
                // das `akteure.min_by { |a| a.level }` schreibt und dann
                // `a.name` liest.**
                case "min_by" or "max_by" or "sort_by":
                {
                            var mass = aufgerufen;
                    if (pMethode == "sort_by")
                    {
                        Paare.Add((wert, mass));
                        break;
                    }

                    if (gefunden.Kind == RubyValueKind.Nil)
                    {
                        gefunden = wert;
                        continue;
                    }

                    var alt = BlockAufrufen(
                        block, [gefunden, wert], pEmpfaenger);
                    var istKleiner = Groesse(mass) < Groesse(alt);
                    if (pMethode == "max_by" ? !istKleiner : istKleiner)
                    {
                        gefunden = wert;
                    }

                    break;
                }

                case "group_by":
                {
                    // **Und `group_by` baut einen Hash, und keinen Index
                    // darueber.** `liste.group_by { |x| x.art }`,
                    // **und die Gruppen stehen in der Reihenfolge, in der
                    // die Arten zum ersten Mal kamen** — **ein Leser, der sie
                    // sortierte, wuerde ein Menue in einer Reihenfolge
                    // zeichnen, die niemand geschrieben hat.**
                    var schluessel = aufgerufen;
                    // **Und die Liste wird hier angelegt, wenn es sie
                    // noch nicht gibt.** Die erste Fassing holte eine leere
                    // Liste aus einem Helfer und legte sie nie ab,
                    // **also war jede Gruppe leer** -- **gemessen:
                    // `group_by` gab einen Hash ohne Paare.**
                    if (!Gruppen.TryGetValue(schluessel, out var sammlung))
                    {
                        sammlung = [];
                        Gruppen[schluessel] = sammlung;
                    }

                    sammlung.Add(wert);
                    break;
                }

                case "partition":
                {
                    if (wahr == null)
                    {
                        wahr = [];
                        falsch = [];
                    }

                    if (Truthy(aufgerufen))
                    {
                        wahr.Add(wert);
                    }
                    else
                    {
                        falsch.Add(wert);
                    }

                    break;
                }

                default:
                    return null;
            }

            // **Und `next` und `break` kommen hier an, und nicht im
            // Schleifenkopf.** Das Flag entsteht **während** des Durchgangs,
            // **und ein Leser, der am Kopf nachsah, sah es einen Durchgang
            // zu spaet** -- **und `next` waere dann ein `break` und `break`
            // waere gar nichts.** Gemessen: `[1,2,3].each { |x| next if
            // x == 2 }` addierte alle drei.
            // *Ein Steuerwort, das am Kopf geprueft wird, beendet den
            // Lauf, in dem es geschrieben wurde, und nicht den, der folgt.*
            if (_gebrochen)
            {
                _gebrochen = false;
                var gebrochenWert = _abbruchWert;
                _abbruchWert = RubyValue.Nil;
                return pMethode is "find" or "detect" && gefunden.Kind
                    != RubyValueKind.Nil
                        ? gefunden
                        : gebrochenWert;
            }

            if (_weiter)
            {
                _weiter = false;
                continue;
            }
        }

        // **Und was aus der Schleife herauskam, wird nach der Sache
        // beantwortet, um die es ging.** Das steht am Ende und nicht
        // darueber, **weil `find` und `min_by` frueher zurueckgeben und
        // `map` am Ende** —
        // **und das ist der Unterschied zwischen einer Schleife, die etwas
        // findet, und einer, die etwas baut.**
        if (pMethode is "inject" or "reduce" or "each_with_object")
        {
            return gesammelt ?? RubyValue.Nil;
        }

        if (pMethode == "partition")
        {
            return RubyValue.OfArray(
                [RubyValue.OfArray(wahr ?? []), RubyValue.OfArray(falsch ?? [])]);
        }

        if (pMethode == "group_by")
        {
            var paare = new List<RubyValue>();
            foreach (var schluessel in Gruppen.Keys)
            {
                paare.Add(schluessel);
                paare.Add(RubyValue.OfArray(Gruppen[schluessel]));
            }

            Gruppen.Clear();
            return RubyValue.OfHash(paare);
        }

        if (pMethode == "sort_by")
        {
            var sortiert = Paare
                .OrderBy(p => p.Item2, Comparer<RubyValue>.Create(
                    (pLinks, pRechts) => Groesse(pLinks).CompareTo(Groesse(pRechts))))
                .Select(p => p.Item1)
                .ToList();
            Paare.Clear();
            return RubyValue.OfArray(sortiert);
        }

        if (pMethode == "one?")
        {
            return gefunden.Kind == RubyValueKind.Nil
                ? RubyValue.OfBoolean(false)
                : gefunden;
        }

        return pMethode switch
        {
            "map" or "select" or "filter" or "reject" or "find_all"
                or "flat_map" => RubyValue.OfArray(ergebnis),
            "each" or "each_with_index" or "reverse_each" => pEmpfaenger,
            "any?" => RubyValue.OfBoolean(false),
            "all?" => RubyValue.OfBoolean(true),
            "none?" => RubyValue.OfBoolean(true),
            "find" or "detect" or "min_by" or "max_by" => gefunden,
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
    /// <summary>
    /// A comparison a script wrote, and not the built-in one.
    /// </summary>
    /// <param name="pNode">The node.</param>
    /// <param name="pOperator">The operator as written.</param>
    /// <returns>The answer, or null when the script has no rule.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And `==` is not built out of `&lt;=&gt;`.</strong>
    /// <c>def ==(other)</c> is the sentence with which a game writes its own
    /// equality,
    /// **and <c>a == b</c> is <c>a.==(b)</c> in Ruby, and not
    /// <c>a.&lt;=&gt;(b) == 0</c>** -- **and a reader that confused the two
    /// would hold a class that writes only <c>==</c> to be unequal**,
    /// **and <c>list.include?(held)</c> would always be false.**
    /// </para>
    /// <para>
    /// <strong>And the other side may answer it.</strong> <c>held == held</c>
    /// is symmetric,
    /// **and a reader that asked only the left one would answer <c>a == b</c>
    /// wrong and <c>b == a</c> right** — and the answer would change when
    /// the game swapped two operands.
    /// </para>
    /// <para>
    /// <strong>And a value with no rule goes to the built-in, which knows
    /// numbers and strings</strong> — **and not to null**, because
    /// <c>5 &lt; 6</c> is a number and not an object.
    /// </para>
    /// </remarks>
    private RubyValue? VergleichMitSkript(RubyNode pNode, string pOperator)
    {
        var linkerWert = Evaluate(Operands(pNode)[0]);

        if (pOperator == "==")
        {
            var erste = EigeneMethode(linkerWert, "==");
            if (erste != null)
            {
                return Aufrufen(
                    erste,
                    [Evaluate(Operands(pNode)[1])],
                    linkerWert.Kind == RubyValueKind.Object
                        ? linkerWert.ClassName
                        : _aktuellerTyp?.Name,
                    linkerWert);
            }

            var rechte = Evaluate(Operands(pNode)[1]);
            var vonRechts = EigeneMethode(rechte, "==");
            if (vonRechts != null)
            {
                return Aufrufen(
                    vonRechts,
                    [linkerWert],
                    rechte.Kind == RubyValueKind.Object
                        ? rechte.ClassName
                        : _aktuellerTyp?.Name,
                    rechte);
            }

            return null;
        }

        var eigene = EigeneMethode(linkerWert, "<=>");
        if (eigene == null)
        {
            return null;
        }

        var dreiwert = Aufrufen(
            eigene,
            [Evaluate(Operands(pNode)[1])],
            linkerWert.Kind == RubyValueKind.Object
                ? linkerWert.ClassName
                : _aktuellerTyp?.Name,
            linkerWert);

        // **And `nil` means that there was no answer.** That is not null as
        // a return, **but an answer: false.**
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

                // **Und `next` und `break` beenden den Rumpf hier.** `next
                // if x == 2` ist ein `if`-Knoten im Rumpf,
                // **und ohne diese Zeile lief der Rest des Rumpfs weiter**
                // -- **und `each { |x| next if x == 2; r = r + x }` haette
                // dann bei jedem Element `r` erhoeht.**
                // *Ein Steuerwort, das im Rumpf steht, beendet den Rumpf und
                // nicht erst den Lauf um ihn herum.*
                if (_returned || _gebrochen || _weiter)
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
            // **And a constant may be given a value.** Ruby allows it,
            // **and `Klasse = Struct.new(:a, :b)` is the line every XP data
            // class is born on** --
            // `RPG::Actor = Struct.new(:id, :name)` is the first line of the
            // standard library of XP, VX and VX Ace.
            // Without this branch it said *„is on the left of an = and there
            // is nowhere to put the value"*,
            // **and a message about the reader for something the reader very
            // well can do** -- **and the script that needs it is every game
            // from that time.**
            case RubyNodeKind.Constant:
                // **Und der Name traegt den aeusseren Typ mit.** `module
                // RPG; KLASSE = 1; end` legt `RPG::KLASSE` ab,
                // **weil `RPG::KLASSE` danach gelesen wird**
                // **und `KLASSE` allein waere ein Name, den nichts findet**
                // -- **und `RPG::Actor = Struct.new(:id, :name)` ist genau
                // diese Form, und sie ist die erste Zeile jedes
                // RPG-Maker-Skripts.**
                var name = ziel.Name ?? string.Empty;
                _konstanten[name] = wert;
                if (_aktuellerTyp?.Name is { Length: > 0} aussen
                    && name.IndexOf("::", StringComparison.Ordinal) < 0)
                {
                    _konstanten[aussen + "::" + name] = wert;
                    if (wert.Kind == RubyValueKind.Symbol
                        && wert.Name is { Length: > 0 } symbolisch
                        && _types.TryGetValue(symbolisch, out var getypt))
                    {
                        // **Und die Klasse steht auch unter dem vollen
                        // Namen.** `RPG::Actor` ist ein Typ, und nicht nur
                        // ein Wert,
                        // **und `RPG::Actor.new(1)` sucht den Typen** --
                        // **ein Leser, der nur den Wert ablegte, wuerde bei
                        // `.new` den Typen nicht finden**, und `nil.new`
                        // waere die Antwort,
                        // **und die Meldung ginge ueber den Host, obwohl die
                        // Klasse genau da ist.**
                        _types[aussen + "::" + name] = getypt;
                    }
                }

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
        // **Und ein Block sieht die Variablen der Methode, und das ist
        // Rubys Regel.** `local_push` haengt die neue Ebene mit
        // `local->prev = lvtbl` **an die Kette an und kappt sie nicht** --
        // verifiziert in `parse.y` aus Ruby 1.8.1.
        //
        // **Die alte Fassung fing bei der Blockebene an, und damit las und
        // schrieb ein Block nur seine eigenen Namen.** Gemessen:
        // `g = []; 3.times { |i| g.push(i) }; g.length` war **0** --
        // **und genau dieser Satz baut jedes Menue und jedes Fenster eines
        // Spiels**, **und `g` ist ausserhalb gesetzt, also ist es keine
        // neue Variable des Blocks.**
        // *Der Kommentar, der diese Regel begruendete, nannte `3.times
        // { |i| g.push(i) }` als den Fall, fuer den sie noetig sei --
        // und der Fall funktionierte nicht.*
        var grenze = _methodenGrenze;
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
        while (!Truthy(Evaluate(bedingung)) && !_returned && !_gebrochen)
        {
            _weiter = false;
            letztes = Evaluate(rumpf);
            _weiter = false;
            if (_gebrochen)
            {
                break;
            }
        }

        if (_gebrochen)
        {
            _gebrochen = false;
            return _abbruchWert;
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

            _weiter = false;
            letztes = Evaluate(rumpf);

            // **Und `break` beendet die Schleife und nimmt seinen Wert
            // mit.** `for x in xs; break x; end` gibt das erste Element,
            // **und ein Leser, der den Wert fallen liess, wuerde nil
            // geben** -- **und `xs.first` waere dann der ganze Satz eines
            // Spiels.**
            if (_gebrochen)
            {
                _gebrochen = false;
                return _abbruchWert;
            }

            _weiter = false;
            if (_returned)
            {
                break;
            }
        }

        return letztes;
    }

    // ---- Control flow

    /// <summary>
    /// An `if`, whether it stands on its own or hangs on the statement
    /// before it.
    /// </summary>
    /// <param name="pNode">The node.</param>
    /// <returns>The branch that ran.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the body is asked before the written form, because a
    /// modifier writes it under the name <c>Body</c> and the written
    /// <c>if</c> under <c>WhenTrue</c>.</strong> `next if x == 2` is an
    /// <c>if</c> whose body is the <c>next</c> and whose condition is
    /// <c>x == 2</c>,
    /// **and <c>EvaluateBranch(WhenTrue)</c> delivered an empty list**,
    /// **because <c>PartsOf</c> gives nothing when the roles are there and
    /// the name is not among them** — **and the whole body did not run.**
    /// </para>
    /// <para>
    /// <strong>And that is the commoner form of the two.</strong> Measured:
    /// <c>each { |x| next if x == 2; r = r + x }</c> added all three,
    /// **and <c>each { |x| if x == 2; next; end; r = r + x }</c> added
    /// four** — *two spellings of one sentence, and the one with a word
    /// in the middle did nothing.*
    /// </para>
    /// </remarks>
    private RubyValue EvaluateIf(RubyNode pNode)
    {
        var bedingung = Child(pNode, RubyNodeRole.Condition);
        if (bedingung == null)
        {
            return Refuse(pNode);
        }

        if (Truthy(Evaluate(bedingung)))
        {
            return PartsOf(pNode, RubyNodeRole.Body).Count > 0
                ? EvaluateBranch(pNode, RubyNodeRole.Body)
                : EvaluateBranch(pNode, RubyNodeRole.WhenTrue);
        }

        return EvaluateBranch(pNode, RubyNodeRole.WhenFalse);
    }

    /// <summary>
    /// `break` and `next`, and what they carry out of the loop.
    /// </summary>
    /// <param name="pNode">The node.</param>
    /// <param name="pIstBreak">true for `break`, false for `next`.</param>
    /// <returns>What was carried, so a chain sees it.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the value is kept, and not thrown.</strong>
    /// <c>break 9</c> answers nine,
    /// **and a reader that answered nil would make a game's search return
    /// nothing at all** — and a `find`-like sentence that returns the value
    /// it found would return nil.
    /// </para>
    /// <para>
    /// <strong>And `break` and `next` are two flags and not one.</strong>
    /// `next` skips the rest of one turn and goes on,
    /// **and a reader with one flag would have ended the loop on `next`** —
    /// **and every `next` in every enumerator would have been a `break`.**
    /// </para>
    /// </remarks>
    private RubyValue EvaluateAbbruch(RubyNode pNode, bool pIstBreak)
    {
        _abbruchWert = pNode.Children.Count > 0
            ? Evaluate(pNode.Children[0])
            : RubyValue.Nil;
        if (pIstBreak)
        {
            _gebrochen = true;
        }
        else
        {
            _weiter = true;
        }

        return _abbruchWert;
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
        while (Truthy(Evaluate(bedingung)) && !_returned && !_gebrochen)
        {
            if (rumpf == null)
            {
                return Refuse(pNode);
            }

            _weiter = false;
            letztes = Evaluate(rumpf);

            // **Und `next` beendet den Rumpf dieser Runde und nicht die
            // Schleife.** `while x; next; y; end` macht weiter,
            // **und ein Leser, der das Flag nicht zuruecksetzte, wuerde die
            // Runde nach der zweiten beenden** -- **und `y` wuerde nur im
            // ersten Durchlauf laufen.**
            if (_weiter)
            {
                _weiter = false;
            }

            if (_gebrochen)
            {
                break;
            }
        }

        if (_gebrochen)
        {
            _gebrochen = false;
            return _abbruchWert;
        }

        return letztes;
    }

    /// <summary>
    /// Runs a body with its `rescue`, `else` and `ensure` arms.
    /// </summary>
    /// <param name="pNode">The node, with the body first.</param>
    /// <returns>What the body or the arm that ran answered.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And `ensure` runs on every way out.</strong> Normally, on a
    /// raised error, and when an arm ran,
    /// <strong>and a reader that ran it only on the way without an error
    /// would leave a file open when the game failed</strong> — and the next
    /// save would go into a file that is already open, and the save before
    /// it would be gone.
    /// </para>
    /// <para>
    /// <strong>And `ensure` may raise, and then the error stands.</strong>
    /// Ruby's rule,
    /// <strong>and a reader that swallowed the second error to keep the
    /// first would have hidden the failure that actually broke the
    /// game</strong> — and a save file that is half written looks exactly
    /// like a save file that was written.
    /// </para>
    /// <para>
    /// <strong>And an arm with no class list catches everything.</strong>
    /// `rescue => e` faengt jeden Fehler,
    /// <strong>and a reader that required a class would have made the
    /// commonest form of the sentence a syntax error</strong> — and that
    /// form is the one a game writes when it wraps a call it does not trust.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The error a `raise` describes, from the three ways a game writes it.
    /// </summary>
    /// <param name="pArgumente">What the call carried.</param>
    /// <returns>The error, ready to be thrown.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And there are three ways, and they are not
    /// interchangeable.</strong> <c>raise "text"</c> raises a
    /// <c>RuntimeError</c>,
    /// <c>raise ArgumentError, "text"</c> raises that class,
    /// <strong>and <c>raise ArgumentError.new("text")</c> raises it with
    /// the text inside</strong> — and a reader that treated the second and
    /// the third alike would have named every error by its class and lost
    /// every message, <strong>and a game's error dialog would be a class name
    /// with no text in it.</strong>
    /// </para>
    /// <para>
    /// <strong>And `raise` with no argument re-raises.</strong> That is what
    /// a handler does to pass an error further up,
    /// <strong>and a reader that raised a blank error instead would have
    /// replaced a game's error with a message that says nothing</strong> —
    /// and the handler that caught it would lose the class it matches on.
    /// </para>
    /// </remarks>
    private static RubyRuntimeException RubyFehlerAus(
        IReadOnlyList<RubyValue> pArgumente)
    {
        if (pArgumente.Count == 0)
        {
            return new RubyRuntimeException(
                "RuntimeError", "raise without an argument re-raises, and "
                    + "this reader is not inside a handler; a game that "
                    + "writes it outside one is a game that asks the "
                    + "reference for the last error, and there is none");
        }

        var erstes = pArgumente[0];

        // **Und `Klasse, "text"` ist die zweite Form.** Der Name steht an
        // erster Stelle und der Text an zweiter,
        // **und ein Leser, der den ersten Wert als Text genommen haette,
        // wuerde eine Fehlermeldung ueber die Klasse schreiben.**
        if (erstes.Kind == RubyValueKind.Symbol)
        {
            var klasse = erstes.Name ?? "RuntimeError";
            var text = pArgumente.Count > 1 && pArgumente[1].Kind == RubyValueKind.String
                ? System.Text.Encoding.UTF8.GetString(pArgumente[1].Bytes)
                : klasse;
            return new RubyRuntimeException(klasse, text);
        }

        // **Und `Klasse.new("text")` ist die dritte.** Der Text steckt im
        // Objekt,
        // **und das Objekt ist eine game class, und nicht `Fehler`**, weil
        // ein Spiel sie selbst gebaut haben kann.
        if (erstes.Kind == RubyValueKind.Object)
        {
            var klasse = erstes.ClassName is { Length: > 0 } name
                ? name
                : "RuntimeError";
            var text = erstes.Felder.TryGetValue("@message", out var nachricht)
                    && nachricht.Kind == RubyValueKind.String
                ? System.Text.Encoding.UTF8.GetString(nachricht.Bytes)
                : klasse;
            return new RubyRuntimeException(klasse, text);
        }

        return new RubyRuntimeException(
            "RuntimeError",
            erstes.Kind == RubyValueKind.String
                ? System.Text.Encoding.UTF8.GetString(erstes.Bytes)
                : WertAlsText(erstes));
    }


    /// <summary>
    /// Loads a script the game named, and runs it in this interpreter.
    /// </summary>
    /// <param name="pEinmal">
    /// Whether the game said <c>require</c> and not <c>load</c>.
    /// </param>
    /// <param name="pName">The name as written.</param>
    /// <returns>
    /// True, and false when the name is already loaded.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And a name that was required once is not required again.</strong>
    /// That is the whole difference between `require` and `load`, and it is
    /// the reason a game's <c>Scene_Base</c> is not defined twice,
    /// <strong>and a reader that always ran the file would have every class
    /// in a game defined twice</strong> — and the second definition would
    /// take the methods with it, and a subclass written after it would
    /// inherit from a class that is a different one.
    /// </para>
    /// <para>
    /// <strong>And a name that is not there is a refusal, and not an
    /// empty script.</strong> An empty file runs and defines nothing,
    /// <strong>and a game whose <c>require</c> silently did nothing would go
    /// on and fail somewhere else, far from the line that was
    /// missing</strong> — so this says which name it could not find.
    /// </para>
    /// <para>
    /// <strong>And the file runs in this interpreter, and not in a new
    /// one.</strong> A class the file defines has to be visible to the file
    /// that required it,
    /// <strong>and a reader that made a new interpreter per file would have
    /// every game's class in a world of its own</strong> — and
    /// <c>Sprite_Picture &lt; Sprite</c> would have no `Sprite`.
    /// </para>
    /// </remarks>
    private RubyValue SkriptLaden(
        bool pEinmal, string pName, string? pAufrufend)
    {
        if (pName.Length == 0)
        {
            throw new RubyRuntimeException(
                "LoadError", "require and load need a name, and this one is "
                    + "empty; the reference says the same");
        }

        // **Und ein Name, der schon da ist, wird nicht wieder gelesen.**
        // **Und bei `require_relative` zaehlt der aufgeloeste Name, und
        // nicht der geschriebene** -- `require_relative "util"` aus
        // `lib/a.rb` und `require_relative "util"` aus `lib/b.rb` sind
        // dieselbe Datei, **und ein Leser, der den geschriebenen Namen
        // in seine Liste schreibt, laedt sie zweimal**,
        // **weil er `a_util` und `b_util` fuer zwei Dateien haelt.**
        var vollstaendig = pAufrufend == null
            ? pName
            : _host.ResolveScriptName(pAufrufend, pName) ?? pName;

        if (pEinmal && _geladeneSkripte.Contains(vollstaendig))
        {
            return RubyValue.OfBoolean(false);
        }

        var quelle = pAufrufend == null
            ? _host.ReadScript(pName, pEinmal)
            : _host.ReadScriptRelative(pAufrufend, pName, pEinmal);
        if (quelle == null)
        {
            // **Und der Name steht in der Meldung, weil eine Diagnose ohne
            // Namen den Leser raten laesst.**
            _diagnostics.Add(
                "require " + (pEinmal ? "" : "or load ") + "'" + pName
                    + "' asked for a script this host does not have; the host "
                    + "decides what a name means, because a folder, an "
                    + "extension and a search path are its business and not "
                    + "the language's");
            return RubyValue.OfBoolean(false);
        }

        if (pEinmal)
        {
            _geladeneSkripte.Add(vollstaendig);
        }

        // **Und CP932, wenn die Datei so kodiert ist.** Rubys `require`
        // liest eine `.rb` in der Kodierung des Skripts,
        // **und ein Spiel aus dieser Zeit hatCP932-Bytes in seinem Quelltext**
        // -- **ein Leser, der UTF-8 annimmt, wuerde jedes zweite kanji
        // zweimal lesen und jede Meldung unlesbar machen.**
        List<RubyNode> anweisungen;
        try
        {
            anweisungen = new RubyParser(
                new RubyLexer(QuelleAlsText(quelle)).Tokenize()).ParseProgram();
        }
        catch (RubyParseException ausnahme)
        {
            // **Und ein Syntaxfehler in der geladenen Datei nennt die
            // Datei.** Sonst stuende eine Zeilennummer ohne Ort da,
            // **und ein Spiel mit dreihundert Skripten laesst sich so nicht
            // finden.**
            throw new RubyRuntimeException(
                "SyntaxError",
                "in '" + pName + "': " + ausnahme.Message);
        }

        // **Und waehrend die Datei laeuft, ist sie die, um die es geht.**
        // Ein `require_relative` in ihr muss sie selbst als Nachbarin
        // nennen, **und nicht die Datei, die sie geladen hat** --
        // **sonst sucht jede Datei in der Ordnerstruktur der ersten.**
        _skriptKette.Add(vollstaendig);
        try
        {
            foreach (var anweisung in anweisungen)
            {
                Evaluate(anweisung);
                if (_returned)
                {
                    _returned = false;
                    break;
                }
            }
        }
        finally
        {
            _skriptKette.RemoveAt(_skriptKette.Count - 1);
        }

        return RubyValue.OfBoolean(true);
    }

    /// <summary>
    /// A script's bytes as text, in the encoding the script says.
    /// </summary>
    /// <param name="pQuelle">The bytes.</param>
    /// <returns>The text.</returns>
    /// <remarks>
    /// <strong>And CP932, and not UTF-8.</strong> Ruby 1.8 has no
    /// encoding magic in a source file, and every game of that time is
    /// Shift_JIS,
    /// <strong>and a reader that assumed UTF-8 would turn every kanji in a
    /// game's text into two characters</strong> — and a name on a menu
    /// would be a name with holes in it.
    /// </remarks>
    /// <summary>
    /// Ruby's `String#%`, which is `rb_str_sprintf` and nothing else.
    /// </summary>
    /// <param name="pMuster">The format, as written.</param>
    /// <param name="pWerte">The values, as evaluated.</param>
    /// <returns>The text, and nil when the pattern asked for a value that
    /// is not there.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the grammar of `sprintf.c` from Ruby 1.8.1 and
    /// not a table I wrote.</strong> Flags, then width, then precision, then
    /// the conversion,
    /// <strong>and the one-argument form takes the rest of an array as
    /// values</strong> — `&quot;%s und %s&quot; % [&quot;a&quot;, &quot;b&quot;]`
    /// is the sentence a game's message box writes.
    /// </para>
    /// <para>
    /// <strong>And a value that is not there is an error, and not an empty
    /// string.</strong> `&quot;%d %d&quot; % [1]` raises <c>too few
    /// argument</c>,
    /// <strong>because a status line that shows a hole where a number belongs
    /// is worse than one that stops</strong> — and the reference stops.
    /// </para>
    /// <para>
    /// <strong>And `%%` is a percent sign, and not a conversion.</strong>
    /// It is in every caption that writes &quot;100%%&quot;.
    /// </para>
    /// </remarks>
    private static RubyValue? ProzentFormatieren(
        RubyValue pMuster,
        RubyValue pRechts)
    {
        var text = pMuster.Kind == RubyValueKind.String
            ? System.Text.Encoding.UTF8.GetString(pMuster.Bytes)
            : string.Empty;
        var werte = new List<RubyValue>();

        // **Und ein Array rechts ist die Liste der Werte.** `&quot;%s und %s&quot;
        // % [&quot;a&quot;, &quot;b&quot;]` ist eine Liste und kein Wert,
        // **und ein Leser, der das Array als Wert formatierte, wuerde
        // `[&quot;a&quot;, &quot;b&quot;]` in die erste Luecke schreiben** --
        // **und das ist der Satz, mit dem jedes Spiel eine Meldung
        // zusammenbaut.**
        if (pRechts.Kind == RubyValueKind.Object && pRechts.IsList)
        {
            werte.AddRange(pRechts.Items);
        }
        else
        {
            werte.Add(pRechts);
        }

        var Ergebnis = new System.Text.StringBuilder();
        var stelle = 0;
        var naechste = 0;
        for(var zeichen = 0; zeichen < text.Length; zeichen++)
        {
            var z = text[zeichen];
            if (z != '%')
            {
                Ergebnis.Append(z);
                continue;
            }

            zeichen++;
            if (zeichen >= text.Length)
            {
                throw new RubyRuntimeException(
                    "ArgumentError", "malformed format string");
            }

            if (text[zeichen] == '%')
            {
                Ergebnis.Append('%');
                continue;
            }

            // **Und die Flags, in der Reihenfolge, in der Ruby sie liest.**
            var minus = false;
            var nullen = false;
            var plus = false;
            var leerzeichen = false;
            var raute = false;
            for(; zeichen < text.Length; zeichen++)
            {
                if (text[zeichen] == '-')
                {
                    minus = true;
                }
                else if (text[zeichen] == '0')
                {
                    nullen = true;
                }
                else if (text[zeichen] == '+')
                {
                    plus = true;
                }
                else if (text[zeichen] == ' ')
                {
                    leerzeichen = true;
                }
                else if (text[zeichen] == '#')
                {
                    raute = true;
                }
                else
                {
                    break;
                }
            }

            if (zeichen >= text.Length)
            {
                throw new RubyRuntimeException(
                    "ArgumentError", "malformed format string");
            }

            // **Und die Breite, und sie kann aus einem Wert kommen.**
            var breite = -1;
            if (text[zeichen] == '*')
            {
                breite = NaechsterZahl(werte, ref naechste, "width given twice");
                if (breite < 0)
                {
                    minus = true;
                    breite = -breite;
                }

                zeichen++;
            }
            else
            {
                var ziffern = 0;
                while (zeichen < text.Length && char.IsDigit(text[zeichen]))
                {
                    ziffern = (ziffern * 10) + (text[zeichen] - '0');
                    zeichen++;
                    if (ziffern > 1_000_000)
                    {
                        throw new RubyRuntimeException(
                            "ArgumentError", "malformed format string");
                    }
                }

                if (ziffern > 0)
                {
                    breite = ziffern;
                }
            }

            if (breite < 0 && nullen)
            {
                // **Und `0` ohne Breite ist eine Breite von null, und nicht
                // das Flag fuer sich.** Ruby setzt FWIDTH,
                // **und ein Leser, der `0` nur als Flag las, wuerde aus
                // `%010d` eine Zahl ohne Fuehrungsnull machen** --
                // **und das ist die Form, mit der eine Uhr ihre Stunden
                // schreibt.**
                breite = 0;
            }

            // **Und die Praezision, und sie kann auch aus einem Wert
            // kommen.**
            var praezision = -1;
            if (zeichen < text.Length && text[zeichen] == '.')
            {
                zeichen++;
                if (zeichen < text.Length && text[zeichen] == '*')
                {
                    praezision = NaechsterZahl(werte, ref naechste, "precision given twice");
                    zeichen++;
                }
                else
                {
                    praezision = 0;
                    while (zeichen < text.Length && char.IsDigit(text[zeichen]))
                    {
                        praezision = (praezision * 10) + (text[zeichen] - '0');
                        zeichen++;
                    }
                }
            }

            if (zeichen >= text.Length)
            {
                throw new RubyRuntimeException(
                    "ArgumentError", "malformed format string");
            }

            var wand = text[zeichen];
            if (wand == 'l' || wand == 'h')
            {
                // **Und `l` und `h` sind Längen, und die Formatierung
                // ändert sich nicht.** Sie stehen in jedem alten printf,
                // **und ein Leser, der sie ablehnte, würde jedes Skript
                // aus der Zeit vor 1999 ablehnen.**
                zeichen++;
                if (zeichen >= text.Length)
                {
                    throw new RubyRuntimeException(
                        "ArgumentError", "malformed format string");
                }

                wand = text[zeichen];
            }

            var wert = NaechsterWert(werte, ref naechste);
            Ergebnis.Append(
                WandAn(wand, wert, breite, praezision, minus, nullen, plus,
                    leerzeichen, raute));
            stelle++;
            _ = stelle;
        }

        return RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(
            Ergebnis.ToString()));
    }


    /// <summary>
    /// The next value a format string asked for, and the value itself.
    /// </summary>
    /// <param name="pWerte">The values.</param>
    /// <param name="pNaechste">How many were taken.</param>
    /// <returns>The value, as written.</returns>
    /// <remarks>
    /// <strong>And a value that is not there is an error.</strong> Ruby's own
    /// message is <c>too few argument</c> and that is the one here,
    /// <strong>because a status line with a hole in it is worse than one
    /// that stops</strong> — and the reference stops.
    /// </remarks>
    private static RubyValue NaechsterWert(
        List<RubyValue> pWerte,
        ref int pNaechste)
    {
        if (pNaechste >= pWerte.Count)
        {
            throw new RubyRuntimeException("ArgumentError", "too few argument");
        }

        return pWerte[pNaechste++];
    }

    /// <summary>
    /// The next value a format string used as a number.
    /// </summary>
    /// <param name="pWerte">The values.</param>
    /// <param name="pNaechste">How many were taken.</param>
    /// <param name="pWarum">The message when there is none.</param>
    /// <returns>The value as a whole number.</returns>
    /// <remarks>
    /// <strong>And `*` takes the width from a value, and the value stays a
    /// value.</strong> <c>"%*d" % [5, 42]</c> is <c>   42</c>,
    /// <strong>and the 5 is taken for the width and not for the
    /// number</strong> — **a reader that took it for both would have made
    /// one value out of two.**
    /// </remarks>
    private static int NaechsterZahl(
        List<RubyValue> pWerte,
        ref int pNaechste,
        string pWarum)
    {
        var wert = NaechsterWert(pWerte, ref pNaechste);
        return wert.Kind switch
        {
            RubyValueKind.Integer => (int)wert.Integer,
            RubyValueKind.Float => (int)wert.Real,
            RubyValueKind.String => int.TryParse(
                System.Text.Encoding.UTF8.GetString(wert.Bytes),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var gezahlt)
                ? gezahlt
                : 0,
            _ => 0,
        };
    }

    /// <summary>
    /// One conversion, and the whole of what it writes.
    /// </summary>
    /// <param name="pWand">The conversion character.</param>
    /// <param name="pWert">The value.</param>
    /// <param name="pBreite">The width, or -1.</param>
    /// <param name="pPraezision">The precision, or -1.</param>
    /// <param name="pMinus">Whether `-` was written.</param>
    /// <param name="pNullen">Whether `0` was written.</param>
    /// <param name="pPlus">Whether `+` was written.</param>
    /// <param name="pLeerzeichen">Whether a space was written.</param>
    /// <param name="pRaute">Whether `#` was written.</param>
    /// <returns>The text this conversion writes.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a value of the wrong kind is converted, and not
    /// refused.</strong> <c>&quot;%d&quot; % &quot;3&quot;</c> is <c>3</c> in
    /// Ruby,
    /// <strong>because a game's save file has numbers as text and the
    /// status line reads them back</strong> — and a reader that refused
    /// would break every old save.
    /// </para>
    /// <para>
    /// <strong>And `%s` uses `to_s`, and not `inspect`.</strong> A name on a
    /// menu is the name,
    /// <strong>und ein Leser, der `inspect` nähme, würde Anführungszeichen
    /// um jeden Namen schreiben** — and a menu item would be
    /// <c>"Held"</c> with the quotes drawn.
    /// </para>
    /// </remarks>
    private static string WandAn(
        char pWand,
        RubyValue pWert,
        int pBreite,
        int pPraezision,
        bool pMinus,
        bool pNullen,
        bool pPlus,
        bool pLeerzeichen,
        bool pRaute)
    {
        switch (pWand)
        {
            case 'd' or 'i' or 'u':
                return MitBreite(
                    Ganzzahl(pWert, pPlus, pLeerzeichen),
                    pBreite,
                    pMinus,
                    pNullen);

            case 'b':
                return MitBreite(
                    Vorzeichen(pWert, pPlus, pLeerzeichen)
                        + System.Convert.ToString(
                            GroßeVon(pWert), 2),
                    pBreite,
                    pMinus,
                    pNullen);

            case 'o':
                return MitBreite(
                    Vorzeichen(pWert, pPlus, pLeerzeichen)
                        + System.Convert.ToString(
                            GroßeVon(pWert), 8),
                    pBreite,
                    pMinus,
                    pNullen);

            case 'x':
                return MitBreite(
                    Vorzeichen(pWert, pPlus, pLeerzeichen)
                        + (pRaute ? "0x" : string.Empty)
                        + System.Convert.ToString(
                            GroßeVon(pWert), 16),
                    pBreite,
                    pMinus,
                    pNullen);

            case 'X':
                return MitBreite(
                    Vorzeichen(pWert, pPlus, pLeerzeichen)
                        + (pRaute ? "0X" : string.Empty)
                        + System.Convert.ToString(
                            GroßeVon(pWert), 16).ToUpperInvariant(),
                    pBreite,
                    pMinus,
                    pNullen);

            case 'f' or 'e' or 'E' or 'g' or 'G':
            {
                var zahl = ReelleVon(pWert);
                var praezision = pPraezision < 0 ? 6 : pPraezision;
                var kopf = zahl < 0 || (zahl == 0d && 1d / zahl < 0) ? "-" : string.Empty;
                var mantisse = Math.Abs(zahl);
                string text;
                if (pWand is 'e' or 'E')
                {
                    text = mantisse.ToString(
                        (pWand == 'E' ? "E" : "e") + praezision.ToString(
                            CultureInfo.InvariantCulture),
                        CultureInfo.InvariantCulture);
                }
                else if (pWand is 'g' or 'G')
                {
                    // **Und `g` nimmt die kuerzeste Form und faellt auf `e`
                    // zurueck, wenn die Zahl zu klein oder zu gross ist.**
                    text = mantisse.ToString(
                        (pWand == 'G' ? "G" : "G") + Math.Max(
                            1, pPraezision < 0 ? 6 : pPraezision),
                        CultureInfo.InvariantCulture);
                }
                else
                {
                    text = mantisse.ToString(
                        "F" + praezision.ToString(CultureInfo.InvariantCulture),
                        CultureInfo.InvariantCulture);
                }

                var mitVorzeichen = kopf;
                if (zahl >= 0)
                {
                    mitVorzeichen = pPlus ? "+" : pLeerzeichen ? " " : string.Empty;
                }

                return MitBreite(mitVorzeichen + text, pBreite, pMinus, false);
            }

            case 'c':
            {
                // **Und `%c` nimmt die Zahl als Zeichen, und `to_s` von einem
                // Zeichen als Zeichen.** `"%c" % 65` ist `A`,
                // **und das ist der Satz, mit dem ein Spiel aus einer
                // Tastennummer einen Buchstaben macht.**
                var zeichen = pWert.Kind == RubyValueKind.String
                    ? System.Text.Encoding.UTF8.GetString(pWert.Bytes)
                    : ((char)(GroßeVon(pWert) & 0xFFFF)).ToString();
                return MitBreite(zeichen, pBreite, pMinus, false);
            }

            case 's':
            {
                var inhalt = pWert.Kind == RubyValueKind.String
                    ? System.Text.Encoding.UTF8.GetString(pWert.Bytes)
                    : WertAlsText(pWert);
                return MitBreite(
                    inhalt,
                    pBreite,
                    pMinus,
                    false,
                    pPraezision < 0 ? -1 : pPraezision);
            }

            case 'p':
                return MitBreite("0x" + System.Convert.ToString(GroßeVon(pWert), 16),
                    pBreite, pMinus, pNullen);

            default:
                throw new RubyRuntimeException(
                    "ArgumentError", $"malformed format string - %{pWand}");
        }
    }

    /// <summary>
    /// A value's size, and nil is zero.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The number.</returns>
    private static long GroßeVon(RubyValue pWert) => pWert.Kind switch
    {
        RubyValueKind.Integer => pWert.Integer,
        RubyValueKind.Float => (long)pWert.Real,
        RubyValueKind.String => long.TryParse(
            System.Text.Encoding.UTF8.GetString(pWert.Bytes),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var gezahlt)
            ? gezahlt
            : 0,
        _ => 0,
    };

    /// <summary>
    /// A value as a whole number, with the sign a format asked for.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <param name="pPlus">Whether `+` was written.</param>
    /// <param name="pLeerzeichen">Whether a space was written.</param>
    /// <returns>The number as text.</returns>
    private static string Ganzzahl(RubyValue pWert, bool pPlus, bool pLeerzeichen)
    {
        var zahl = GroßeVon(pWert);
        if (zahl < 0)
        {
            return zahl.ToString(CultureInfo.InvariantCulture);
        }

        if (pPlus)
        {
            return "+" + zahl.ToString(CultureInfo.InvariantCulture);
        }

        return pLeerzeichen
            ? " " + zahl.ToString(CultureInfo.InvariantCulture)
            : zahl.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The sign a format asked for, on its own.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <param name="pPlus">Whether `+` was written.</param>
    /// <param name="pLeerzeichen">Whether a space was written.</param>
    /// <returns>The sign, or an empty string.</returns>
    private static string Vorzeichen(RubyValue pWert, bool pPlus, bool pLeerzeichen)
    {
        var zahl = GroßeVon(pWert);
        if (zahl < 0)
        {
            return "-";
        }

        return pPlus ? "+" : pLeerzeichen ? " " : string.Empty;
    }

    /// <summary>
    /// A value as a real number.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The number.</returns>
    private static double ReelleVon(RubyValue pWert) => pWert.Kind switch
    {
        RubyValueKind.Integer => pWert.Integer,
        RubyValueKind.Float => pWert.Real,
        RubyValueKind.String => double.TryParse(
            System.Text.Encoding.UTF8.GetString(pWert.Bytes),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var gezahlt)
            ? gezahlt
            : 0d,
        _ => 0d,
    };

    /// <summary>
    /// Text at the width a format asked for.
    /// </summary>
    /// <param name="pText">The text.</param>
    /// <param name="pBreite">The width, or -1.</param>
    /// <param name="pLinks">Whether it stands left.</param>
    /// <param name="pNullen">Whether the gap is filled with zeros.</param>
    /// <param name="pKuerzen">The precision, or -1.</param>
    /// <returns>The text, at the width.</returns>
    /// <remarks>
    /// <strong>Und die Luecke wird bei einer Zahl mit Nullen gefuellt und
    /// bei einem Text mit Leerzeichen.</strong> `%05d` und `%5s` benutzen
    /// dieselbe Breite und anderes Fuellmaterial,
    /// <strong>und ein Leser, der beides gleich macht, wuerde aus
    /// `%5s` eine Zahl mit Nullen machen</strong> — **und das ist die Form,
    /// mit der ein Spiel eine Namensspalte ausrichtet: `%5s`, nicht
    /// `%05d`.**
    /// </remarks>
    private static string MitBreite(
        string pText,
        int pBreite,
        bool pLinks,
        bool pNullen,
        int pKuerzen = -1)
    {
        var text = pKuerzen >= 0 && pText.Length > pKuerzen
            ? pText[..pKuerzen]
            : pText;
        if (pBreite <= 0 || text.Length >= pBreite)
        {
            return text;
        }

        var luecke = pBreite - text.Length;
        if (pLinks)
        {
            return text + new string(' ', luecke);
        }

        if (pNullen)
        {
            // **Und die Nullen gehen hinter das Vorzeichen, und nicht davor.**
            // `-42` mit `%06d` ist `-00042` in Ruby,
            // **und ein Leser, der die Nullen davor schriebe, gaebe
            // `000-42`** — **und das ist eine Zahl, die kein Mensch lesen
            // kann und kein Spiel anzeigen sollte.**
            if (text.Length > 0 && (text[0] == '-' || text[0] == '+'))
            {
                return text[..1] + new string('0', luecke) + text[1..];
            }

            return new string('0', luecke) + text;
        }

        return new string(' ', luecke) + text;
    }


    private static string QuelleAlsText(byte[] pQuelle)
    {
        try
        {
            return Encoding.GetEncoding(932).GetString(pQuelle);
        }
        catch (DecoderFallbackException)
        {
            // **Und was CP932 nicht lesen kann, wird UTF-8 gelesen.**
            // `�` ist kein moeglicher Kanji,
            // **und die Datei ist dann keine aus dieser Zeit.**
            return Encoding.UTF8.GetString(pQuelle);
        }
    }


    private RubyValue EvaluateBegin(RubyNode pNode)
    {
        var kinder = pNode.Children;
        if (kinder.Count == 0)
        {
            return RubyValue.Nil;
        }

        // **Und der Rumpf laeuft zuerst, und ein Fehler daraus geht in
        // die Arme.** Das steht in einem `try`,
        // **weil `ensure` bei jedem Ausgang laufen muss, auch wenn kein
        // Fehler da war** --
        // **und ein Leser, der `ensure` nur im Fehlerfall aufriefe,
        // haette eine Datei offen gelassen, wenn das Spiel scheitert**, und
        // die naechste Speicherung ginge in eine schon offene Datei.
        try
        {
            var koerper = EvaluateBlock(kinder[0]);
            foreach (var kind in kinder)
            {
                if (kind.Name == "else")
                {
                    EvaluateBlock(kind);
                    break;
                }
            }

            return koerper;
        }
        catch (RubyRuntimeException ausnahme)
        {
            return ArmAntwort(kinder, ausnahme);
        }
        finally
        {
            EnsureLaeuft(kinder);
        }
    }

    /// <summary>
    /// Runs the first arm that catches an error.
    /// </summary>
    /// <param name="pKinder">The nodes, with the body first.</param>
    /// <param name="pAusnahme">What was raised.</param>
    /// <returns>What the arm answered.</returns>
    /// <remarks>
    /// <strong>And the first arm that catches it wins.</strong> Ruby takes
    /// the arms in the order they were written,
    /// <strong>and a reader that took the last would have run a game's
    /// general handler before its specific one</strong> — and the specific
    /// one is the one that knows what to do.
    /// </remarks>
    private RubyValue ArmAntwort(
        IReadOnlyList<RubyNode> pKinder,
        RubyRuntimeException pAusnahme)
    {
        for(var stelle = 1; stelle < pKinder.Count; stelle++)
        {
            var arm = pKinder[stelle];

            // **Und nur `else` und `ensure` sind keine Arme.** Der Name
            // eines Arms ist der Name, den das `=&gt;` fuer den Fehler
            // gewaehlt hat -- **`rescue =&gt; e` haelt also `e`**,
            // **und ein Leser, der einen Arm nur an einem leeren Namen
            // erkannte, wuerde jeden Arm mit einem gebundenen Fehler
            // ueberspringen** --
            // **und `rescue =&gt; e` ist die haeufigste Form von allen.**
            if (arm.Kind != RubyNodeKind.Block
                || arm.Name is "else" or "ensure")
            {
                continue;
            }

            // **Und das ist der Grund fuer den Sprung, und er ist nicht
            // sichtbar, wenn man nur auf die Antwort schaut.** Ohne ihn
            // liefe `else` als Arm durch und gaebe seine 99 zurueck,
            // **und `begin; raise "x"; rescue; 5; else; 99; end` gaebe 99
            // statt 5** --
            // **und `else` ist nicht die Antwort, sondern der Weg, den man
            // geht, wenn nichts schiefging**, **und das ist der ganze
            // Unterschied zwischen `else` und einem zweiten `rescue`.**

            var klassen = FehlerklassenVon(arm);
            if (klassen.Count > 0 && !FaengtDieKlasse(klassen, pAusnahme.Class))
            {
                continue;
            }

            // **Und der Fehler kommt unter dem Namen an, den das
            // `=>` genannt hat.** `rescue => e` und dann `e.message` --
            // **und ein Leser, der den Namen nicht gebunden haette, gaebe
            // nil**, **und `nil.message` waere ein zweiter Fehler in der
            // Fehlerbehandlung**, and the game would crash while handling a
            // crash.
            if (arm.Name.Length > 0)
            {
                _scopes[^1][arm.Name] = FehlerAlsWert(pAusnahme);
            }

            // **Und die Klassenliste ist kein Satz, und sie steht am
            // Ende des Arms.** Sie ist der letzte Knoten,
            // **und ein Leser, der den Arm wie einen Rumpf auswertete,
            // gaebe sie zurueck** -- **und `begin; 1/0; rescue
            // ZeroDivisionError; 2; end` gaebe eine leere Liste statt der
            // 2**,
            // **und das ist der Satz, mit dem ein Spiel einen eigenen
            // Fehler behandelt.**
            // **Und ohne Klassenliste wird gar nichts abgeschnitten.**
            // `rescue => e` und `rescue` haben keine Liste,
            // **und ein Leser, der immer den letzten Knoten strich, wuerde
            // aus `rescue => e; e.message` ein `e` machen** -- **und
            // `nil.message` waere ein zweiter Fehler in der
            // Fehlerbehandlung.**
            return EvaluateBlock(klassen.Count > 0
                ? new RubyNode
                {
                    Kind = RubyNodeKind.Block,
                    Name = arm.Name,
                    Line = arm.Line,
                    Children = [.. arm.Children.Take(arm.Children.Count - 1)],
                }
                : arm);
        }

        // **Und kein Arm passt, dann steht der Fehler wieder da.**
        // **Weil das der Unterschied zwischen "behandelt" und
        // "verschluckt" ist.**
        throw pAusnahme;
    }

    /// <summary>
    /// The error classes an arm names, and empty when it names none.
    /// </summary>
    /// <param name="pArm">The arm.</param>
    /// <returns>The names.</returns>
    /// <remarks>
    /// <strong>And the list is the last node, and the statements before
    /// it.</strong> `rescue A, B => e` stores its two names at the end of
    /// the arm, <strong>and a reader that ran the array as a statement would
    /// have a game's error handler end in a value it never uses</strong> --
    /// and `A, B` is not a statement Ruby runs.
    /// </remarks>
    private static List<string> FehlerklassenVon(RubyNode pArm)
    {
        var namen = new List<string>();
        if (pArm.Children.Count == 0)
        {
            return namen;
        }

        var letztes = pArm.Children[^1];
        if (letztes.Kind != RubyNodeKind.Array)
        {
            return namen;
        }

        foreach (var klasse in letztes.Children)
        {
            if (klasse.Kind == RubyNodeKind.Constant)
            {
                namen.Add(klasse.Name ?? string.Empty);
            }
        }

        return namen;
    }

    /// <summary>
    /// Whether an arm's class list catches an error of that class.
    /// </summary>
    /// <param name="pKlassen">The names the arm gives.</param>
    /// <param name="pKlasse">The class the error is of.</param>
    /// <returns>true when it is caught.</returns>
    /// <remarks>
    /// <strong>And the name of the class itself counts.</strong>
    /// `rescue StandardError` faengt einen `ArgumentError`,
    /// <strong>because `ArgumentError` is a `StandardError`** -- and a
    /// reader that compared the two names for equality would catch nothing
    /// that a game's own hierarchy says it should catch.
    /// </remarks>
    private static bool FaengtDieKlasse(
        IReadOnlyList<string> pKlassen,
        string pKlasse)
    {
        foreach (var genannt in pKlassen)
        {
            if (genannt == pKlasse)
            {
                return true;
            }

            // **Und die Elternkette, und nicht "genauso tief".**
            // `ZeroDivisionError` und `TypeError` sind Geschwister unter
            // `StandardError`,
            // **und ein Leser, der nur die Tiefe verglich, wuerde sagen
            // "`TypeError` faengt `ZeroDivisionError`"** --
            // **und dann wuerde `rescue TypeError` einen Rechenfehler
            // fangen**,
            // **und ein Spiel, das einen Rechenfehler von einem Typfehler
            // unterscheidet, haette keinen Unterschied mehr.**
            for(var eltern = ElternVon(pKlasse); eltern.Count > 0;
                eltern = ElternVon(eltern[0]))
            {
                if (genannt == eltern[0])
                {
                    return true;
                }
            }

        }

        return false;
    }

    /// <summary>
    /// The class a class is directly under, and nothing when it is a root.
    /// </summary>
    /// <param name="pKlasse">The class name.</param>
    /// <returns>Its direct parent, or an empty list.</returns>
    /// <remarks>
    /// <strong>And a fixed list, because the reader cannot ask the
    /// host.</strong> The classes a game rescues are the ones the language
    /// has,
    /// <strong>and a reader that asked the host would be asking about a
    /// hierarchy it is not the owner of</strong> — and a host that answered
    /// with nil would make every `rescue` catch nothing.
    /// </remarks>
    private static IReadOnlyList<string> ElternVon(string pKlasse) => pKlasse switch
    {
        "ArgumentError" or "TypeError" or "RangeError" or "ZeroDivisionError"
            or "IndexError" or "KeyError" or "FloatDomainError" or "IOError"
            or "EOFError" or "NotImplementedError" or "LocalJumpError"
            or "RegexpError" or "RuntimeError" => ["StandardError"],
        "NoMethodError" => ["NameError"],
        "NameError" => ["StandardError"],
        "LoadError" or "SyntaxError" => ["ScriptError"],
        "ScriptError" => ["Exception"],
        "StandardError" => ["Exception"],
        "SystemExit" or "SystemStackError" or "EncodingError" or "FrozenError"
            or "StopIteration" => ["Exception"],
        _ => [],
    };


    /// <summary>

    /// <summary>
    /// The error as a value, for `rescue =&gt; e`.
    /// </summary>
    /// <param name="pAusnahme">The error.</param>
    /// <returns>The value.</returns>
    /// <remarks>
    /// <strong>And it answers `message` and `class`.</strong> Those are the
    /// two things a game reads,
    /// <strong>and a reader that gave nil for both would have an error
    /// dialog with no text in it</strong> — and a player staring at an
    /// empty box with an OK button.
    /// </remarks>
    private static RubyValue FehlerAlsWert(RubyRuntimeException pAusnahme)
    {
        var felder = new Dictionary<string, RubyValue>(StringComparer.Ordinal)
        {
            ["@message"] = RubyValue.OfBytes(
                System.Text.Encoding.UTF8.GetBytes(pAusnahme.Detail)),
            ["@klasse"] = RubyValue.OfSymbol(pAusnahme.Class),
        };
        var wert = RubyValue.OfEmptyObject("Fehler");
        foreach (var feld in felder)
        {
            wert.Felder[feld.Key] = feld.Value;
        }

        return wert;
    }

    /// <summary>
    /// Runs the `ensure` arm, if there is one.
    /// </summary>
    /// <param name="pKinder">The nodes.</param>
    /// <remarks>
    /// <strong>And nothing is done with what it answers.</strong> Ruby's
    /// rule,
    /// <strong>and a reader that made `ensure` its answer would have a
    /// game's `begin; a; ensure; b; end` answer `b` instead of `a`</strong>
    /// -- and the value a method returns would be the value of the line that
    /// closed a file.
    /// </remarks>
    private void EnsureLaeuft(IReadOnlyList<RubyNode> pKinder)
    {
        foreach (var kind in pKinder)
        {
            if (kind.Name == "ensure")
            {
                EvaluateBlock(kind);
            }
        }
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
                or RubyNodeKind.MethodCall
                // **Und `SelfCall`, denn ein Name mit Klammern und ohne
                // Empfaenger ist jetzt `SelfCall`.** `sprintf("%d", 5) { }`
                // ist nicht ueblich,
                // **aber `draw_text(x, y, "a") do ... end` steht in jedem
                // Bildschirm, der eine Liste zeichnet**,
                // **und ohne dieses `or` wuerde der Blockzweig den Aufruf
                // nicht als Aufruf erkennen**, und die Liste waere leer.
                or RubyNodeKind.SelfCall)
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
        // **And `class A` without `<` sits under `Object`.** Ruby gives a
        // class with no written base the base `Object`,
        // **and a reader that took `Superclass = null` would have a class
        // with no parent** -- **and `A.instance_methods` would then be A
        // alone, `A.is_a?(Object)` would be false, and `A.ancestors` would
        // be `[A]`** -- **and every guard in a script would say `no` to the
        // class's own base.**
        var geschrieben = pNode.Superclass
            ?? (pIsClass && name != "Object" && name != "BasicObject"
                ? "Object"
                : null);
        var typ = _types.TryGetValue(name, out var vorhanden)
            ? vorhanden
            : new RubyType
            {
                Name = name,
                IsClass = pIsClass,
                Superclass = geschrieben,
            };
        if (geschrieben != null)
        {
            typ.Superclass = geschrieben;
        }
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

        // **Und der aeussere Typ steht wieder, wenn der innere fertig
        // ist.** `class Aussen; class Innen; def a; end; def b; end; end`
        // -- **ohne diesen Satz waere `b` unter `Innen` gelandet**, und
        // **ein Leser, der den inneren Typen stehen liess, haette die
        // aeusseren Methoden des Spiels in einer Hilfsklasse
        // eingesammelt.**
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

        // **Und `def` unter einem wartenden `module_function` schreibt
        // zweimal.** Ruby 1.8's `rb_mod_modfunc` defines the method a
        // second time on the singleton,
        // **und ein Leser, der nur die eine Liste fuellt, laesst `M.x`
        // ungerufen** — **und `M.x` ist der Satz, mit dem ein VX-Plugin
        // seine Hilfsmethoden aufruft.**
        if (typ.AlsModulFunktion && !pAufSelbst)
        {
            typ.Methods["self." + name] = new RubyMethod
            {
                Name = name,
                IsOnSelf = true,
                Parameters = namen,
                Vorgaben = vorgaben,
                SammelParameter = sammelName.Length > 0 ? sammelName : null,
                SammelAb = sammelAb,
                OptionenParameter = optionenName.Length > 0 ? optionenName : null,
                Body = rumpf!,
            };
        }

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

            // **Und hier steht nichts ueber Module, und das ist
            // richtig.** `include` kopiert die Modulmethoden nach
            // `Methods`,
            // **und dieser Leser laeuft die Basisklassen hoch und sieht
            // sie dort** -- **ein Aufruf geht genau so, und `include` ist
            // genau das, was `Eingemischt` tut.**
            // **Ein zweiter Weg ueber `Eingebunden` waere eine zweite
            // Antwort auf eine Frage, die schon beantwortet ist** --
            // **und sie kann auseinanderlaufen**, weil ein Modul, das
            // nach dem `include` noch eine Methode bekommt, in der einen
            // Liste steht und in der anderen nicht.

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
        // **Und ein Objekt traegt seinen Klassennamen bei sich.** `A.new`
        // ist ein Objekt der Klasse `A`,
        // **und ein Leser, der den Empfangernamen fuer den Klassennamen
        // hielt, fragte die Klasse, in der die Frage geschrieben
        // wurde** -- **und `A.new.respond_to?(:zeichne)` war dann `false`
        // und `true` zur selben Zeit**, je nachdem, wo das stand:
        // `module_defined?` sagte ja und `respond_to?` nein, und beide
        // Zeilen stehen zwei Zeilen auseinander in einem Plugin.
        var klasse = pEmpfaenger.Kind == RubyValueKind.Symbol
            && pEmpfaenger.Name != "self"
            && _types.ContainsKey(pEmpfaenger.Name)
            ? pEmpfaenger.Name
            : pEmpfaenger.Kind == RubyValueKind.Object
                && pEmpfaenger.ClassName != null
                && _types.ContainsKey(pEmpfaenger.ClassName)
                ? pEmpfaenger.ClassName
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
            or "any?" or "all?" or "none?" or "one?"
            or "find" or "detect" or "find_all"
            or "inject" or "reduce" or "each_with_object"
            or "group_by" or "partition" or "sort_by" or "min_by" or "max_by"
            or "flat_map"
            // **Und `sub` und `gsub`, denn mit einem Block sind sie der
            // Satz, mit dem ein Spiel seinen Text umschreibt.**
            // `name.gsub(/(\w+)/) { |w| w.upcase }`,
            // **und ein Leser, der sie nicht hier nennt, haette den Block
            // nie an den Aufruf gehaengt** -- **und `gsub` wuerde mit einem
            // leeren Ersatz laufen und jedes X streichen**, und **ein Spiel,
            // das seinen Gegaennamen in Grossbuchstaben schreibt, wuerde
            // leere Zeichen daraus machen und der Name waere weg.**
            or "sub" or "gsub" or "delete" or "squeeze"
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
    /// <summary>
    /// What a class or a module answers when it is asked about itself.
    /// </summary>
    /// <param name="pName">The type's name.</param>
    /// <param name="pMethode">The question.</param>
    /// <param name="pArgumente">What was asked with.</param>
    /// <returns>The answer, or null when the question was not one of
    /// these.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And `instance_methods` walks the whole chain, unless the
    /// caller says not to.</strong> <c>A.instance_methods(false)</c> is
    /// only A's own, <strong>and that is the argument a plugin uses to ask
    /// "what did I not already do"</strong> — so a reader that ignored the
    /// argument would say *yes* to a method its base class has,
    /// **and it would define the method a second time.**
    /// </para>
    /// <para>
    /// <strong>And the answer is symbols, and a game checks them with
    /// <c>include?</c>.</strong> <c>M.instance_methods.include?(:x)</c>,
    /// **and a reader that gave the names as texts would answer
    /// <c>false</c> for every name the game wrote**, because
    /// <c>include?(:x)</c> looks for a symbol.
    /// </para>
    /// </remarks>
    private RubyValue? TypBefragt(
        string pName, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        if (!_types.TryGetValue(pName, out var typ))
        {
            return null;
        }

        // **Und eine Frage ist nicht die einzige Sache, die ein Name
        // beantwortet.** `A.x` is a call, and the call has to run,
        // **and a reader that only answered questions would answer
        // `A.method_defined?(:x)` correctly and `A.x` with nil** --
        // **and the two sit two lines apart in a plugin, so the guard
        // passes and the call fails.**
        //
        // **And the method comes from the whole chain, upwards.** `def
        // self.x` in a base class is a class method of the subclass,
        // **and the object this runs on is a value of the type, and not
        // the type** — `A.x` must not be `A.new.x`.
        if (!IstFrage(pMethode) && !typ.Methods.ContainsKey(pMethode))
        {
            var klassenMethode = TypHatSingleton(typ, pMethode)
                ? SucheSingleton(typ, pMethode)
                : null;
            if (klassenMethode != null)
            {
                return Aufrufen(
                    klassenMethode,
                    pArgumente,
                    typ.Name,
                    RubyValue.OfSymbol(typ.Name));
            }
        }


        switch (pMethode)
        {
            case "name":
                return RubyValue.OfSymbol(typ.Name);

            case "to_s":
                // **Und `to_s` sagt `M`, und nicht `#<Module:M>`.** Das ist
                // die kurze Form,
                // **und `A.to_s` in einer Meldung ist der Name, den der
                // Spieler liest** -- **ein Leser, der die Ruby-Darstellung
                // baute, wuerde in jeder Diagnose eine Klassen-ID zeigen,
                // die der Spieler nicht kennt.**
                return RubyValue.OfBytes(
                    System.Text.Encoding.UTF8.GetBytes(typ.Name));

            case "superclass":
                // **Und nil, wenn es keine gibt.** `BasicObject.superclass`
                // ist nil und nicht `BasicObject`,
                // **und eine Schleife ueber der Elternkette, die sich selbst
                // zur Basis macht, endet nie.**
                return typ.Superclass == null
                    ? RubyValue.Nil
                    : RubyValue.OfSymbol(typ.Superclass);

            case "include?":
            case "<":
                if (pArgumente.Count != 1)
                {
                    return RubyValue.OfBoolean(false);
                }

                // **Und der Name wird aufgeloest, und nicht verglichen.**
                // `A.include?(String)` nennt die Klasse, nicht einen Text,
                // **und `Comparable` und `Kernel` sind Namen, die dieser
                // Leser kennt, ohne dass ein Host sie anbietet.**
                return RubyValue.OfBoolean(
                    GehoertTypAn(typ, TypNameAus(pArgumente[0])));

            case "included_modules":
                {
                    // **Und was `include?` beantwortet, steht auch hier.**
                    // **`Kernel` steckt in jeder Klasse, und
                    // `A.included_modules.include?(Kernel)` muss wahr sein**,
                    // **denn die beiden Fragen ueber denselben Typ muessen
                    // sich nicht widersprechen** -- **und ein Leser, der hier
                    // nur die Skript-Includes nennt, wuerde `false` sagen,
                    // waehrend `include?` `true` sagt.**
                    var liste = new List<RubyValue>();
                    var gesehen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var eing in typ.Eingebunden)
                    {
                        if (gesehen.Add(eing.Name))
                        {
                            liste.Add(RubyValue.OfSymbol(eing.Name));
                        }
                    }

                    // **Und die Module der Klassen, die in der Kette
                    // liegen.** `String` nimmt `Comparable` und `Kernel`,
                    // **und `A < String` erbt beide durch die Basis.**
                    foreach (var (behaelter, modul) in SprachEingebunden)
                    {
                        if (GehoertBasis(typ, behaelter) && gesehen.Add(modul))
                        {
                            liste.Add(RubyValue.OfSymbol(modul));
                        }
                    }

                    return RubyValue.OfArray(liste);
                }

            case "ancestors":
                {
                    // **Und die Elternkette steht vor den Modulen, und
                    // das eingebundene Modul vor dem eigenen Typ.**
                    // Das ist die Reihenfolge, in der Ruby nach einer
                    // Methode sucht,
                    // **und ein Leser, der die Module hinten anstellt,
                    // wuerde einer Basisklasse den Vorrang geben** --
                    // **und `include M` waere dann ein stiller
                    // Fehlschlag.**
                    var liste = new List<RubyValue>();
                    var klasse = typ.Superclass;
                    var grenze = 0;
                    while (klasse != null && grenze < 64)
                    {
                        liste.Add(RubyValue.OfSymbol(klasse));
                        klasse = _types.TryGetValue(klasse, out var oben)
                            ? oben.Superclass
                            : null;
                        grenze++;
                    }

                    // **Und die eingebundenen Module, und die eingebauten
                    // mit.** Gemessen: `Object.ancestors` war
                    // `[BasicObject, Kernel, Object]`,
                    // **und `Kernel` stand dort nur, weil es in
                    // `SprachEingebunden` steht, nicht weil es eingebunden
                    // ist** -- **und ein Typ, dessen Skript kein `include`
                    // schreibt, hat trotzdem Module, und die gehoeren in
                    // die Liste.**
                    foreach (var eing in typ.Eingebunden)
                    {
                        if (eing.Vorn)
                        {
                            liste.Insert(0, RubyValue.OfSymbol(eing.Name));
                        }
                        else
                        {
                            liste.Add(RubyValue.OfSymbol(eing.Name));
                        }
                    }

                    foreach (var (behaelter, modul) in SprachEingebunden)
                    {
                        if (behaelter == typ.Name
                            && !liste.Any(x => x.Name == modul))
                        {
                            liste.Add(RubyValue.OfSymbol(modul));
                        }
                    }

                    // **Und der Typ selbst steht am Ende, und nicht in der
                    // Kette der Basisklassen.** `class A` hat die Basis
                    // `Object`, **und `Object` steht darum in der Liste,
                    // weil die Kette dort endet** -- **und gemessen war
                    // `Object.ancestors` gleich `[BasicObject, Kernel,
                    // Object]`, was richtig ist, und `A.ancestors` gleich
                    // `[Object, A]`, was auch richtig ist.**
                    liste.Add(RubyValue.OfSymbol(typ.Name));
                    return RubyValue.OfArray(liste);
                }

            case "is_a?":
            case "kind_of?":
                if (pArgumente.Count != 1)
                {
                    return RubyValue.Nil;
                }

                // **Und `M.is_a?(Module)` ist wahr, und `M.is_a?(Class)`
                // nur fuer eine Klasse.** Das unterscheidet `module` von
                // `class`,
                // **und ein Spiel, das eine Basisklasse von einem Modul
                // unterscheiden muss, kann das nicht ohne diese
                // Antwort.**
                var gesucht = TypNameAus(pArgumente[0]);
                if (gesucht == "Module")
                {
                    return RubyValue.OfBoolean(true);
                }

                if (gesucht == "Class")
                {
                    return RubyValue.OfBoolean(typ.IsClass);
                }

                return RubyValue.OfBoolean(typ.Name == gesucht);

            case "method_defined?":
            case "private_method_defined?":
            case "public_method_defined?":
            case "protected_method_defined?":
                {
                    // **Und die Frage geht an den ganzen Weg nach oben,
                    // und nicht nur an den Typ selbst.** Ruby 1.8's
                    // `rb_mod_method_defined` walks `RCLASS_SUPER`,
                    // **und ein Leser, der nur `typ.Methods` laesst, sagt
                    // `false` fuer jede geerbte Methode** --
                    // **und `if !mod.method_defined?(:initialize)` ist der
                    // erste Satz eines Ruby-Plugins**, und er wuerde bei
                    // jedem Typ `initialize` erneut schreiben.
                    //
                    // **Und `module_function` schreibt eine `self.`-Kopie,
                    // und die zaehlt fuer die Frage nicht.** Ruby
                    // `rb_mod_method_defined` walks the *instance* chain,
                    // **und ein Leser, der beide Listen mischt, wuerde
                    // sagen, ein Modul habe `x` als eigene Methode,
                    // obwohl `M.x` es nur ueber `module_function`
                    // bekommen hat** --
                    // **und `if !M.method_defined?(:x)` wuerde dann das
                    // Schreiben ueberspringen, das der Spieler sieht.**
                    if (pArgumente.Count == 0)
                    {
                        return null;
                    }

                    var gefragt = TypNameAus(pArgumente[0]);
                    if (gefragt == null)
                    {
                        return null;
                    }

                    return RubyValue.OfBoolean(
                        gefragt.StartsWith("self.", StringComparison.Ordinal)
                            ? TypHatSingleton(typ, gefragt[5..])
                            : TypHatMethode(typ, gefragt));

                }

            case "module_function":
                {
                    // **Und `module_function` ohne Namen nimmt die
                    // folgenden Definitionen und macht sie zugleich
                    // statisch.** Ruby 1.8's `rb_mod_modfunc` sets
                    // `MFLAG_MODFUNC` and defines a singleton copy,
                    // **und ein Leser, der es als Abfrage las, wuerde
                    // `true` sagen und das Wort fuer eine Tatsache
                    // halten.**
                    //
                    // **Und mit Namen ist es eine Anweisung an die
                    // Methodenliste, und keine Anfrage.**
                    if (pArgumente.Count == 0)
                    {
                        typ.AlsModulFunktion = true;
                        return RubyValue.OfBoolean(true);
                    }

                    foreach (var argument in pArgumente)
                    {
                        var gesuchte = TypNameAus(argument);
                        if (gesuchte != null
                            && gesuchte.Length > 0
                            && typ.Methods.ContainsKey(gesuchte))
                        {
                            typ.ModulFunktionen.Add(gesuchte);
                        }
                    }

                    return RubyValue.OfBoolean(true);

                }

            case "instance_methods":
            case "instance_method":
                {
                    // **And `true` means "the whole chain", and `false`
                    // means "this type alone".** Ruby takes `true` as the
                    // default,
                    // **and the default is not the same as "always
                    // everything": `A.instance_methods` is the chain, and
                    // `A.instance_methods(false)` is A alone** --
                    // **and a reader that had the two the other way round
                    // would answer `false` for a class with nothing of its
                    // own** and put every base class's method in the list a
                    // plugin asks for before it overrides anything.
                    // **And `nil` stands for `true`, because
                    // `A.instance_methods nil` is that too.**
                    var dieGanzeKette = pArgumente.Count == 0
                        || pArgumente[0].Kind == RubyValueKind.Nil
                        || (pArgumente[0].Kind == RubyValueKind.Boolean
                            && pArgumente[0].Boolean);
                    var namen = new List<RubyValue>();
                    if (!dieGanzeKette)
                    {
                        foreach (var methode in typ.Methods.Keys)
                        {
                            if (MethodeGehoertDemTyp(methode, true))
                            {
                                namen.Add(RubyValue.OfSymbol(methode));
                            }
                        }

                        return RubyValue.OfArray(namen);
                    }

                    // **Und die eigenen Namen kommen zuerst in die
                    // Antwort und in die Liste der gesehenen.** Sie stehen
                    // nicht nur in `gesehen`,
                    // **sonst waere `A.instance_methods` bei einer Klasse mit
                    // eigener Methode die leere Liste von der Basis** --
                    // **und gemessen: `all=[geerbt]` statt `[eigenes,
                    // geerbt]`, also genau die Methoden, die A nicht
                    // selbst geschrieben hat.**
                    var gesehen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var methode in typ.Methods.Keys)
                    {
                        if (MethodeGehoertDemTyp(methode, true))
                        {
                            gesehen.Add(methode);
                            namen.Add(RubyValue.OfSymbol(methode));
                        }
                    }

                    // **Und die Kette laeuft von hier nach oben, und jede
                    // Klasse ueberschreibt die untere.**
                    var klasse = typ.Superclass;
                    var grenze = 0;
                    while (klasse != null && grenze < 64
                        && _types.TryGetValue(klasse, out var oben))
                    {
                        foreach (var methode in oben.Methods.Keys)
                        {
                            if (MethodeGehoertDemTyp(methode, true)
                                && gesehen.Add(methode))
                            {
                                namen.Add(RubyValue.OfSymbol(methode));
                            }
                        }

                        klasse = oben.Superclass;
                        grenze++;
                    }

                    foreach (var eing in typ.Eingebunden)
                    {
                        if (!_types.TryGetValue(eing.Name, out var modul))
                        {
                            continue;
                        }

                        foreach (var methode in modul.Methods.Keys)
                        {
                            if (MethodeGehoertDemTyp(methode, true)
                                && gesehen.Add(methode))
                            {
                                namen.Add(RubyValue.OfSymbol(methode));
                            }
                        }
                    }

                    return RubyValue.OfArray(namen);
                }
        }

        return null;
    }

    /// <summary>
    /// Whether a name is a method a caller can send, and not a class
    /// method or a name `undef` took out.
    /// </summary>
    /// <param name="pMethode">The name in the table.</param>
    /// <param name="pMitSelf">Whether a class method counts.</param>
    /// <returns>true when a caller could send it.</returns>
    /// <remarks>
    /// <strong>And <c>self.</c> is not one of them.</strong>
    /// <c>A.instance_methods</c> does not list <c>A.selbst</c>,
    /// **and a reader that listed it would make a plugin
    /// <c>unless A.instance_methods.include?(method)</c> true for every
    /// class method it ever wrote** — and it would then skip defining a
    /// class method it had meant to define.
    /// </remarks>
    private static bool MethodeGehoertDemTyp(string pMethode, bool pMitSelf)
    {
        if (pMethode.StartsWith("self.", StringComparison.Ordinal))
        {
            return pMitSelf;
        }

        return true;
    }

    /// <summary>
    /// Whether the type itself has the method, as opposed to the objects
    /// that include it.
    /// </summary>
    /// <param name="pTyp">The type asked about.</param>
    /// <param name="pName">The method's name as written.</param>
    /// <returns>true when the type can be asked for it.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And two ways to be one.</strong> Ruby 1.8 has a real
    /// singleton class per class, and `def self.x` writes into it, while
    /// `module_function` copies the instance method across and records the
    /// name in <c>RBASIC_SET_CLASS_IV_TBL</c>,
    /// **so this interpreter keeps the copies in <c>Methods</c> under a
    /// <c>self.</c> prefix and the names <c>module_function</c> was given
    /// in a second list** — and one list alone would answer half the
    /// cases.
    /// </para>
    /// <para>
    /// <strong>And it walks upward.</strong> A class method written in a
    /// base class is a class method of the subclass,
    /// **and a reader that stopped at the type itself would say
    /// <c>false</c> to <c>B.selbst_aus_der_basis</c>** —
    /// and a plugin's `unless A.respond_to?(:x)` guard would then write a
    /// method the base already provided, and the base's version would be
    /// gone.
    /// </para>
    /// </remarks>
    /// <summary>
    /// Whether the word is a question a type is asked, and not a method a
    /// type is called with.
    /// </summary>
    /// <param name="pMethode">The method's name as written.</param>
    /// <returns>true when the name is one of the questions.</returns>
    /// <remarks>
    /// <strong>And the list and not "anything with a question mark".</strong>
    /// <c>respond_to?</c> and <c>is_a?</c> ask,
    /// **and <c>instance_methods</c> asks without a mark**,
    /// **and a reader that looked for <c>?</c> would have treated
    /// <c>instance_methods</c> as a call** — and then tried to run a
    /// method the type does not have, and said *A has no method
    /// 'instance_methods'*, **which is the sentence a game sees when
    /// its plugin asks the one question it exists to ask.**
    /// </remarks>
    private static bool IstFrage(string pMethode) => pMethode
        is "name"
        or "superclass"
        or "ancestors"
        or "include?"
        or "included_modules"
        or "instance_methods"
        or "instance_method"
        or "is_a?"
        or "kind_of?"
        or "method_defined?"
        or "private_method_defined?"
        or "public_method_defined?"
        or "protected_method_defined?"
        or "module_function"
        or "to_s";

    /// <summary>
    /// The type's own method, found the way a call finds it.
    /// </summary>
    /// <param name="pTyp">The type asked of.</param>
    /// <param name="pName">The method's name as written.</param>
    /// <returns>The method, or null when there is none.</returns>
    /// <remarks>
    /// <strong>And it goes up, and it stops at the first one.</strong>
    /// **And <c>module_function</c>'s copy counts, because that is the
    /// copy the module was told to make.**
    /// <strong>And a tombstone stops the walk, not skips one name.**
    /// </remarks>
    private RubyMethod? SucheSingleton(RubyType pTyp, string pName)
    {
        var lauf = pTyp;
        while (lauf != null)
        {
            if (lauf.Undefiniert.Contains("self." + pName))
            {
                return null;
            }

            if (lauf.Methods.TryGetValue("self." + pName, out var gefunden))
            {
                return gefunden;
            }

            if (lauf.ModulFunktionen.Contains(pName)
                && lauf.Methods.TryGetValue(pName, out var alsGanzes))
            {
                // **Und die Kopie laeuft mit dem Empfang des Moduls,
                // und nicht mit dem der Klasse, die sie einbindet.**
                var kopie = new RubyMethod
                {
                    Name = alsGanzes.Name,
                    IsOnSelf = true,
                    Parameters = alsGanzes.Parameters,
                    Vorgaben = alsGanzes.Vorgaben,
                    SammelParameter = alsGanzes.SammelParameter,
                    SammelAb = alsGanzes.SammelAb,
                    OptionenParameter = alsGanzes.OptionenParameter,
                    Body = alsGanzes.Body,
                };
                return kopie;
            }

            lauf = lauf.Superclass == null
                || !_types.TryGetValue(lauf.Superclass, out var oben)
                ? null
                : oben;
        }

        return null;
    }

    private bool TypHatSingleton(RubyType pTyp, string pName)
    {
        var lauf = pTyp;
        while (lauf != null)
        {
            if (lauf.Methods.ContainsKey("self." + pName)
                || lauf.ModulFunktionen.Contains(pName))
            {
                return true;
            }

            lauf = lauf.Superclass == null
                || !_types.TryGetValue(lauf.Superclass, out var oben)
                ? null
                : oben;
        }

        return false;
    }

    /// <summary>
    /// Whether objects of the type can be sent the method, following the
    /// base classes and the modules it took in.
    /// </summary>
    /// <param name="pTyp">The type asked about.</param>
    /// <param name="pName">The method's name as written.</param>
    /// <returns>true when a call would find it.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is a search and not a name test.</strong> Ruby 1.8's
    /// <c>rb_mod_method_defined</c> walks <c>RCLASS_SUPER</c> and then
    /// <c>RMODULE_INCLUDED</c> and returns the answer to "could a call
    /// reach it",
    /// **and a reader that only asked "is this name free of a
    /// <c>self.</c> prefix" would say <c>true</c> for every name in the
    /// world** — and then
    /// <c>if !A.method_defined?(:update)</c> would never fire, and a
    /// plugin's whole purpose is that it fires.
    /// </para>
    /// <para>
    /// <strong>And <c>undef_method</c> is a tombstone that answers
    /// <c>false</c>.</strong> The name is in <c>Undefiniert</c> and not in
    /// <c>Methods</c>,
    /// **and a search that stopped at the first base class with the name
    /// would report a method a class explicitly said it does not have** —
    /// and then <c>super</c> would find it anyway, and the <c>undef</c>
    /// would be the only thing in the game that did nothing.
    /// </para>
    /// </remarks>
    private bool TypHatMethode(RubyType pTyp, string pName)
    {
        var lauf = pTyp;
        while (lauf != null)
        {
            if (lauf.Undefiniert.Contains(pName))
            {
                return false;
            }

            if (lauf.Methods.ContainsKey(pName)
                || lauf.Methods.ContainsKey("self." + pName))
            {
                return true;
            }

            // **Und auch hier nichts ueber Module.** `include` hat die
            // Methoden längst nach `Methods` geschrieben,
            // **und `A.method_defined?(:draw)` sieht sie dort** --
            // **und genau darum hat `Eingemischt` sie dorthin
            // geschrieben und nicht bloss vermerkt.**

            lauf = lauf.Superclass == null
                || !_types.TryGetValue(lauf.Superclass, out var oben)
                ? null
                : oben;
        }

        return false;
    }

    /// <summary>
    /// The type name a value stands for, whether it is a constant or a
    /// text.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <returns>The name, or null when the value names no type.</returns>
    /// <remarks>
    /// <strong>And a text counts, because a game writes
    /// <c>include?("Comparable")</c> too.</strong>
    /// **A reader that took only symbols would say <c>false</c> for a
    /// name the game wrote in the other spelling**, **and the plugin would
    /// include itself a second time.**
    /// </remarks>
    private string? TypNameAus(RubyValue pWert) => pWert.Kind switch
    {
        RubyValueKind.Symbol => pWert.Name,
        RubyValueKind.String => System.Text.Encoding.UTF8.GetString(
            pWert.Bytes),
        _ => null,
    };

    /// <summary>
    /// Whether a type is a module of this one, by name.
    /// </summary>
    /// <param name="pTyp">The type asked about.</param>
    /// <param name="pName">The module's name, or null.</param>
    /// <returns>true when the module is in the chain.</returns>
    /// <remarks>
    /// <strong>And the built-in names count.</strong> A class includes
    /// <c>Object</c> whether a host says so or not, <strong>and a reader
    /// that answered from <c>Eingebunden</c> alone would say
    /// <c>A.include?(Object)</c> is <c>false</c></strong> — and that is the
    /// question a plugin asks before it redefines the basics.
    /// </remarks>
    private bool GehoertTypAn(RubyType pTyp, string? pName)
    {
        if (pName == null)
        {
            return false;
        }

        foreach (var eing in pTyp.Eingebunden)
        {
            if (eing.Name == pName)
            {
                return true;
            }
        }

        // **Und die eingebauten Module zaehlen mit.** `A.include?(Kernel)`
        // ist wahr, weil `Kernel` in jeder Klasse steckt,
        // **und ein Leser, der nur die Skript-Includes liest, wuerde hier
        // `false` sagen** -- **und `A.included_modules` wuerde `Kernel` nicht
        // nennen, obwohl `include?` es behauptet** -- **und die beiden
        // Fragen widersprachen sich dann.**
        foreach (var (typ, modulName) in SprachEingebunden)
        {
            if (modulName == pName && typ == pTyp.Name)
            {
                return true;
            }

            // **Und die Basisklasse bringt ihre Module mit.** `class A <
            // String` hat `Comparable` durch die Basis,
            // **und das ist der Weg, den Ruby geht.**
            if (modulName == pName && GehoertBasis(pTyp, typ))
            {
                return true;
            }
        }

        // **Und `Object` steckt in jeder Klasse, die nicht ausdruecklich
        // `BasicObject` ist.** Ruby nimmt es an,
        // **und ein Spiel, das `include?(Object)` fragt, erwartet
        // `true`.**
        //
        // **Und die Bedingung fragt die Basiskette und nicht nur das
        // Feld `Superclass`.** Die erste Fassung schloss `A` aus, wenn
        // `A.superclass == "Object"` war,
        // **und `class A` hat seit heute genau das** -- **und damit war
        // `A.include?(Object)` `false` fuer jede Klasse ohne geschriebene
        // Basis**, **und `Object.include?(Object)` ebenfalls, weil dort
        // dasselbe Feld `Object` traegt.** Gemessen: alle drei `false`.
        // *Ein `include?`, das die Basis ausschliesst statt sie zu suchen,
        // ist eine membership, die nach einem Feld fragt und nicht nach der
        // Kette.*
        return pName == "Object" && GehoertBasis(pTyp, "Object")
            && pTyp.Name != "Object";
    }
    /// <summary>
    /// Whether a type is a class or has one in its chain.
    /// </summary>
    /// <param name="pTyp">The type asked about.</param>
    /// <param name="pName">The class to look for.</param>
    /// <returns>true when the class is in the chain.</returns>
    /// <remarks>
    /// <strong>And the whole chain, and not only the type itself.</strong>
    /// `class A &lt; String` has `Comparable` through its base,
    /// **and a reader that asked only <c>A.Name == "String"</c> would say
    /// no** -- **and `A.include?(Comparable)` would be false for a class
    /// that Ruby sorts.**
    /// </remarks>
    private bool GehoertBasis(RubyType pTyp, string pName)
    {
        var klasse = pTyp.Name;
        var grenze = 0;
        while (klasse != null && grenze < 64
            && _types.TryGetValue(klasse, out var typ))
        {
            if (typ.Name == pName)
            {
                return true;
            }

            klasse = typ.Superclass;
            grenze++;
        }

        return false;
    }



    private bool Eingebaut(RubyType pTyp, string pMethode, IReadOnlyList<RubyValue> pArgumente)
    {
        if (pMethode is "include" or "extend" or "prepend")
        {
            return Eingemischt(pTyp, pMethode, pArgumente);
        }

        if (pMethode == "undef_method")
        {
            // **Und `undef_method` ist ein Aufruf auf dem Modul, und kein
            // Schluesselwort.** `undef` ist das Schluesselwort,
            // **und `Module#undef_method` nimmt Symbole** --
            // **und ein Leser, der es ablehnte, liesse jedes Skript
            // scheitern, das es schreibt** -- **und die Meldung sprach
            // von einem Host, der nie gefragt wurde.**
            //
            // **Und die Marke sitzt am Namen, damit eine geerbte Methode
            // nicht wieder durchkommt.** `undef` entfernt nur aus der
            // eigenen Tabelle, **und `undef_method` nimmt auch das
            // Erbe weg** -- das ist der Unterschied, und ein Leser, der
            // beides gleich machte, haette `undef` in einer Unterklasse
            // wirkungslos.
            foreach (var argument in pArgumente)
            {
                var name = argument.Kind == RubyValueKind.Symbol
                    ? argument.Name
                    : argument.Kind == RubyValueKind.String
                        ? System.Text.Encoding.UTF8.GetString(argument.Bytes)
                        : null;
                if (name == null)
                {
                    continue;
                }

                pTyp.Methods.Remove(name);
                pTyp.Methods.Remove("self." + name);
                pTyp.Undefiniert.Add(name);
                pTyp.Undefiniert.Add("self." + name);
            }

            return true;
        }

        if (pMethode == "module_function")
        {
            // **Und im Rumpf ist das Wort eine Anweisung an das, was
            // danach kommt.** `module M; module_function; def x; end; end`
            // **und ein Leser, der es als Abfrage las, wuerde `true`
            // zurueckgeben und das Wort fuer eine Tatsache halten** --
            // **und `M.x` waere nicht definiert, obwohl der Spieler es
            // aufruft.**
            if (pArgumente.Count == 0)
            {
                pTyp.AlsModulFunktion = true;
                return true;
            }

            foreach (var argument in pArgumente)
            {
                if (argument.Kind == RubyValueKind.Symbol
                    && argument.Name != null)
                {
                    pTyp.ModulFunktionen.Add(argument.Name);
                }
            }

            return true;
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

            // **Und das Modul wird gemerkt, und nicht nur seine Methoden.**
            // `include?` braucht genau das,
            // **und ein Leser, der nur die Kopie haelt, koennte nie sagen,
            // ob ein Plugin schon drin ist** -- **und das ist die erste
            // Zeile von fast jedem VX-Plugin.**
            pTyp.Eingebunden.Add((name, pMethode == "prepend"));

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

        // **And `__method__` is a built-in, and not a script method.**
        // Written without brackets it is an identifier,
        // **and a reader that only looked in the script's own table would
        // answer a local variable of that name** -- **which is nil** --
        // **and `__method__` would be silent where it is exactly the thing
        /// a method needs to say which one it is.**
        if (name == "__method__" || name == "__dir__")
        {
            return RubyValue.OfSymbol(
                string.IsNullOrEmpty(_aktuelleMethode)
                    ? "Object"
                    : _aktuelleMethode);
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
        // **Und ein Block sieht die Variablen der Methode, und das ist
        // Rubys Regel.** `local_push` haengt die neue Ebene mit
        // `local->prev = lvtbl` **an die Kette an und kappt sie nicht** --
        // verifiziert in `parse.y` aus Ruby 1.8.1.
        //
        // **Die alte Fassung fing bei der Blockebene an, und damit las und
        // schrieb ein Block nur seine eigenen Namen.** Gemessen:
        // `g = []; 3.times { |i| g.push(i) }; g.length` war **0** --
        // **und genau dieser Satz baut jedes Menue und jedes Fenster eines
        // Spiels**, **und `g` ist ausserhalb gesetzt, also ist es keine
        // neue Variable des Blocks.**
        // *Der Kommentar, der diese Regel begruendete, nannte `3.times
        // { |i| g.push(i) }` als den Fall, fuer den sie noetig sei --
        // und der Fall funktionierte nicht.*
        var grenze = _methodenGrenze;
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