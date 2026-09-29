using System;
using System.Collections.Generic;

namespace UniversalRPG.Rgss;

/// <summary>
/// What the interpreter may reach outside itself, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This is the whole safety boundary, and it is an interface rather
/// than a table inside the evaluator.</strong> The interpreter has no way to
/// name a function, read a file, reach the network or start a process: it can
/// only call what a host hands it, and only by name. <strong>A host that
/// answers <c>File.read</c> has made that decision itself</strong>, and the
/// interpreter's correctness is independent of it.
/// </para>
/// <para>
/// <strong>An unknown name is a refusal and not a guess.</strong> Ruby would
/// raise <c>NoMethodError</c>; this refuses with a diagnostic naming the
/// receiver, the method and what the host does know, because <strong>a game
/// that asks for a method this host has not got is a fact about the host and
/// a player needs to see it</strong> rather than a <c>nil</c> that spreads.
/// </para>
/// </remarks>
public interface IRubyHost
{
    /// <summary>
    /// The text of a script a game wants to load, and null when this host
    /// has no such script.
    /// </summary>
    /// <param name="pName">
    /// The name as written in <c>require</c>, without the <c>.rb</c>.
    /// </param>
    /// <param name="pEinmal">
    /// Whether the game said <c>require</c> and not <c>load</c>: a name
    /// that was required once is not required again.
    /// </param>
    /// <returns>
    /// The script's bytes, and null when this host has no such script —
    /// which is a refusal and not an empty script.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And the host, and not the interpreter.</strong> The host is
    /// the only one that has files,
    /// <strong>and an interpreter that read files would be a program that
    /// runs a game and can also go looking for things</strong> — which is
    /// the shape this project refuses everywhere else.
    /// </para>
    /// <para>
    /// <strong>And the name comes in as written, and not resolved.</strong>
    /// <c>require "Sprite_Picture"</c> asks for <c>Sprite_Picture</c>,
    /// **and the host decides what that means** — a folder, an extension, a
    /// search path. A reader that appended <c>.rb</c> here would have
    /// hard-coded one host's layout into a language runtime.
    /// </para>
    /// <para>
    /// <strong>And a missing script is null, and not empty text.</strong>
    /// An empty file runs and defines nothing,
    /// **and a game whose <c>require</c> silently did nothing would go on
    /// and fail somewhere else, far from the line that was actually
    /// missing.</strong>
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <strong>And a host that has no files does not have to say so.</strong>
    /// The default is null, and null is the refusal,
    /// <strong>and a host that does not load scripts should not have to write
    /// a method for it</strong> — **eine Schnittstelle, die jede neue
    /// Faehigkeit von jedem Host verlangt, waechst schneller als die Hosts,
    /// die sie brauchen**, **und dann schreibt jeder Host eine leere
    /// Methode, die niemand liest.**
    /// </remarks>
    byte[]? ReadScript(string pName, bool pEinmal) => null;

    /// <summary>
    /// Calls a method on a value.
    /// </summary>
    /// <param name="pReceiver">The receiver, which the host may be nil for.</param>
    /// <param name="pMethod">The method's name as written.</param>
    /// <param name="pArguments">The arguments, already evaluated.</param>
    /// <returns>
    /// The result, or null when this host has no such method — which is a
    /// refusal and not a value.
    /// </returns>
    RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments);

    /// <summary>
    /// Hands a block to this host, which may run it once per value.
    /// </summary>
    /// <param name="pReceiver">The receiver, which may be nil for.</param>
    /// <param name="pMethod">The method's name as written.</param>
    /// <param name="pArguments">The arguments, already evaluated.</param>
    /// <param name="pYield">
    /// Runs the block with one value, and returns what it returned.
    /// </param>
    /// <returns>
    /// The result, or null when this host has no such method — which is a
    /// refusal and not a value.
    /// </returns>
    /// <remarks>
    /// <strong>A block reaches a host as a thing to call and not as
    /// code.</strong> The host decides how often and with what, and this
    /// runtime has no objects and no closures, <strong>so the only shape a
    /// block can take on the way out is a callback</strong>. A host that
    /// wants a `Proc` has none here, **and a reader that invented one would
    /// have a closure model this interpreter does not have.**
    /// </remarks>
    RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield);

    /// <summary>
    /// Reads a constant by name.
    /// </summary>
    /// <param name="pName">The constant's name as written.</param>
    /// <returns>The value, or null when this host does not define it.</returns>
    RubyValue? LookupConstant(string pName);

    /// <summary>What this host does know, for a diagnostic.</summary>
    IReadOnlyList<string> KnownMethods { get; }
}

/// <summary>
/// A host that answers nothing, and says so once.
/// </summary>
/// <remarks>
/// <strong>The default is a host with no capabilities, not a permissive
/// one.</strong> A game script that reaches for something this project has not
/// implemented gets a named refusal, and the reason it is refused is that
/// nothing was configured — <strong>not that the interpreter failed.</strong>
/// </remarks>
public sealed class RubyNullHost : IRubyHost
{
    /// <summary>The name, so a diagnostic can say which host refused.</summary>
    public string Name { get; init; } = "the null host";

    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments)
    {
        _ = pReceiver;
        _ = pMethod;
        _ = pArguments;
        return null;
    }

    /// <summary>
    /// This host has no method that takes a block either.
    /// </summary>
    /// <remarks>
    /// **The null host is the one that says "no"**, and it says it the same
    /// way for a block as for everything else — **so a call with a block on a
    /// host that cannot do it gets the same diagnostic as a call without
    /// one**, and a game's `each` fails in the place a reader can see.
    /// </remarks>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
    {
        _ = pReceiver;
        _ = pMethod;
        _ = pArguments;
        _ = pYield;
        return null;
    }

    public RubyValue? LookupConstant(string pName)
    {
        _ = pName;
        return null;
    }

    public IReadOnlyList<string> KnownMethods => Array.Empty<string>();
}

/// <summary>
/// One method a script defined, with the parameters it takes.
/// </summary>
/// <param name="pName">The method's name as written.</param>
/// <param name="pParameters">The parameter names, in the order written.</param>
/// <param name="pBody">What it runs, as a block node.</param>
/// <remarks>
/// <strong>A method and a class body are the same thing here</strong>, because
/// Ruby's <c>def</c> inside a class body defines a method on that class and
/// <c>def self.x</c> defines one on the class itself — <strong>and the only
/// difference a reader has to see is which table the name lands in.</strong>
/// </remarks>
public sealed class RubyMethod
{
    /// <summary>The method's name as written.</summary>
    public string Name { get; init; } = "";

    /// <summary>The parameter names, in the order written.</summary>
    public IReadOnlyList<string> Parameters { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this was written `def self.x` and belongs to the class.
    /// </summary>
    /// <remarks>
    /// <strong>A class method and an instance method of the same name are two
    /// different things</strong>, and a game's script has both under the same
    /// name more than once. <strong>The flag is what keeps them apart</strong>,
    /// because a reader that filed them together would have answered a
    /// <c>self.</c> call with the instance body.
    /// </remarks>
    public bool IsOnSelf { get; init; }

    /// <summary>
    /// Whether this was made by `attr_reader`, `attr_writer` or
    /// `attr_accessor` and is not a body the script wrote.
    /// </summary>
    /// <remarks>
    /// <strong>The flag is what keeps an attribute out of an
    /// `include`.</strong> Including a module copies its methods, and an
    /// attribute is a pair of methods standing for a field — <strong>a reader
    /// that copied it would have copied the class's storage into the
    /// including class</strong>, and the two classes would share a value.
    /// </remarks>
    public bool IsAttribute { get; init; }

    /// <summary>
    /// The instance variable an attribute stands for, and null for a
    /// method with a body.
    /// </summary>
    /// <remarks>
    /// <strong>The field name is the method's own name with an
    /// <c>@</c> in front.</strong> That is Ruby's rule, and it is why the
    /// reader and the writer can be told apart by the one character: a
    /// writer is stored under `hp=` and stands for `@hp`, **and a reader that
    /// gave the writer its own field would have written to a place nothing
    /// reads.**
    /// </remarks>
    public string? Field { get; init; }

    /// <summary>
    /// Whether this is the writer half of an attribute.
    /// </summary>
    /// <remarks>
    /// <strong>Only the writer assigns.</strong> A reader that made the
    /// reader assign would have a game where reading a value changes it, and
    /// <strong>the reference would have raised</strong> for a constant.
    /// </remarks>
    public bool IsWriter { get; init; }

    /// <summary>
    /// The expression behind each parameter that has one, by parameter name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>By name and not by position.</strong> A reader that stored them
    /// by position would have to keep the two lists in step, <strong>and
    /// losing the step would bind the wrong default to the wrong
    /// parameter</strong> — which is a method that answers a number nobody
    /// wrote.
    /// </para>
    /// <para>
    /// <strong>An expression and not a value.</strong> `def m(a = rand(6))`
    /// evaluates its default <strong>at every call, not once at
    /// definition</strong> — and a reader that evaluated it at definition
    /// would have given every call the same number, which is the whole
    /// reason a game writes that instead of a constant.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, RubyNode> Vorgaben { get; init; }
        = new Dictionary<string, RubyNode>(StringComparer.Ordinal);

    /// <summary>
    /// The name of the `*rest` parameter, and null when the method has none.
    /// </summary>
    /// <remarks>
    /// <strong>A name and not a flag.</strong> A game writes
    /// <c>def sammle(*teile)</c> and then <c>teile.length</c>,
    /// <strong>und ein Leser, der nur ein <c>bool</c> gespeichert haette,
    /// haette den Namen nicht und haette ihn aus dem Aufruf heraus raten
    /// muessen** -- wo er nicht steht.
    /// </remarks>
    public string? SammelParameter { get; init; }

    /// <summary>
    /// Where the splat begins in the parameter list, and -1 when there is
    /// none.
    /// </summary>
    /// <remarks>
    /// <strong>The place and not just the fact.</strong> A splat at the end
    /// takes everything left, and a splat in the middle takes everything left
    /// <em>from there</em> — and
    /// <strong>a reader that only knew that one exists would bind it after
    /// every parameter</strong>, which is right for the common case and wrong
    /// for `def m(*teile, letzte)`, where the last value is not in the list.
    /// </remarks>
    public int SammelAb { get; init; } = -1;

    /// <summary>
    /// The name of the `**opts` parameter, and null when the method has none.
    /// </summary>
    /// <remarks>
    /// <strong>Separate and not the same field.</strong> <c>*rest</c> sammelt
    /// die ueberzaehligen **Werte** und <c>**opts</c> die ueberzaehligen
    /// **Namen/Wert-Paare**,
    /// <strong>und ein Leser, der beides in eines legte, haette ein Spiel,
    /// das <c>f(1, 2, k: 3)</c> schreibt, mit einem Argument gespiesen statt
    /// mit zweien.</strong>
    /// </remarks>
    public string? OptionenParameter { get; init; }

    /// <summary>What it runs.</summary>
    public RubyNode Body { get; init; } = null!;
}

/// <summary>
/// A class that `Struct` built, and the fields it was given.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And it is a class and not a special case.</strong>
/// <c>Struct.new(:a, :b)</c> gives a class, and that class goes into the
/// same table as one the script wrote,
/// <strong>so every method that finds a class finds this one too</strong> —
/// and a reader that gave Struct its own storage would have made a game's
/// <c>RPG::Actor</c> invisible to the code that reads it.
/// </para>
/// <para>
/// <strong>And the fields are attributes, and not a second kind of
/// member.</strong> <c>a.x</c> and <c>a.x = 1</c> are the reader and the
/// writer of <c>@x</c>,
/// <strong>and Ruby builds a struct's accessors exactly that way</strong> —
/// so a game's struct takes a class, an instance variable and no new code.
/// </para>
/// </remarks>
public sealed class RubyStruct
{
    /// <summary>The field names, in the order they were written.</summary>
    public IReadOnlyList<string> Fields { get; init; } = Array.Empty<string>();
}

/// <summary>
/// One class or module a script defined, with the methods in it.
/// </summary>
/// <param name="pName">The class's name as written.</param>
/// <param name="pSuperclass">The superclass, or null.</param>
/// <remarks>
/// <strong>A module and a class are kept apart</strong>, because a module has
/// no superclass and <c>include</c> means something different from
/// <c>&lt;</c> — <strong>and a reader that treated them alike would let a
/// game's module inherit something it never asked for.</strong>
/// </remarks>
public sealed class RubyType
{
    /// <summary>The class's or module's name as written.</summary>
    public string Name { get; init; } = "";

    /// <summary>Whether this is a class and not a module.</summary>
    public bool IsClass { get; init; }

    /// <summary>
    /// The superclass, as written or as Ruby gives it.
    /// </summary>
    /// <remarks>
    /// <strong>And not init, because a class is opened more than
    /// once.</strong> `class A` twice is one class,
    /// **and a reader that could only set the base in the object
    /// initialiser kept the first one** -- **and a game that reopens a class
    /// under a base it wrote the first time would have the old base, and
    /// every method the first class had would still be found first.**
    /// </remarks>
    public string? Superclass { get; set; }

    /// <summary>The methods defined in the body, by name.</summary>
    public Dictionary<string, RubyMethod> Methods { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// The modules this type took in, in the order they were written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a list and not a copy.</strong> <c>include M</c> copies
    /// M's methods into this type, <strong>and a reader that kept only the
    /// copy could not answer <c>include?</c></strong> — and
    /// <c>include?</c> is how a plugin asks whether it is already in a
    /// class, **which is the first line of most VX plugins**.
    /// </para>
    /// <para>
    /// <strong>And <c>prepend</c> is the same list with the other
    /// meaning.</strong> A prepended module's methods come before this
    /// type's own, <strong>and a reader that stored it here without saying
    /// which, would have it win exactly like an include</strong> — and a
    /// plugin that meant to wrap a method would have replaced it instead.
    /// </para>
    /// </remarks>
    public List<(string Name, bool Vorn)> Eingebunden { get; } = [];

    /// <summary>
    /// The names `undef` took out of this class, whether they were here or
    /// inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A set and not the absence of an entry.</strong> `undef m` in a
    /// subclass that never had its own `m` has to stop the base's `m` from
    /// coming through, **and a table without an entry says nothing about
    /// that** — the walk would simply carry on upward and find it.
    /// </para>
    /// <para>
    /// <strong>This is the whole difference between `undef` and never having
    /// defined the method</strong>, and it is the reason a game's subclass
    /// can say <em>this class does not do that</em> in a way that survives the
    /// base class gaining the method later.
    /// </para>
    /// </remarks>
    public HashSet<string> Undefiniert { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// The fields, when `Struct` built this class, and null when a script
    /// wrote it.
    /// </summary>
    /// <remarks>
    /// <strong>Null and not an empty list.</strong> A class a script wrote has
    /// no fields, and a class `Struct` built has them,
    /// <strong>and a reader that gave both an empty list could not tell
    /// <c>to_a</c> from a mistake</strong> — a hand-written class has no
    /// members because nothing said it should, and that is different from
    /// having none.
    /// </remarks>
    public RubyStruct? Struct { get; set; }
}

/// <summary>
/// One thing that went wrong, with where.
/// </summary>
/// <remarks>
/// <strong>A raised Ruby exception and a refused call are different
/// answers</strong> and are kept apart: a <c>/ 0</c> in a game's script is
/// the script's own behaviour, and a host that has no such method is this
/// project's boundary. A reader that folded them into one "error" would make
/// a game's own bug look like a missing feature.
/// </remarks>
public sealed class RubyRuntimeException : Exception
{
    /// <summary>Builds one with the message Ruby would use.</summary>
    /// <param name="pClass">The class, <c>ZeroDivisionError</c> or
    /// <c>NoMethodError</c>.</param>
    /// <param name="pMessage">The message.</param>
    public RubyRuntimeException(string pClass, string pMessage)
        : base($"{pClass}: {pMessage}")
    {
        Class = pClass;
        Detail = pMessage;
    }

    /// <summary>The class Ruby would name, for a caller that matches on it.</summary>
    public string Class { get; }

    /// <summary>The message without the class.</summary>
    public string Detail { get; }
}
