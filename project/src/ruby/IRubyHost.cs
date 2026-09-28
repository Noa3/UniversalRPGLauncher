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

    public RubyValue? LookupConstant(string pName)
    {
        _ = pName;
        return null;
    }

    public IReadOnlyList<string> KnownMethods => Array.Empty<string>();
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
