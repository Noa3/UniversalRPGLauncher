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

    /// <summary>The superclass as written, or null for a module.</summary>
    public string? Superclass { get; init; }

    /// <summary>The methods defined in the body, by name.</summary>
    public Dictionary<string, RubyMethod> Methods { get; } = new(StringComparer.Ordinal);

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
