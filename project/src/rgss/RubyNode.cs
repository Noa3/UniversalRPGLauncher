using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UniversalRPG.Rgss;

/// <summary>Which kind of name a script wrote.</summary>
/// <remarks>
/// This is a lexical fact about the text, not a statement about a binding. A
/// script may write `@x` on an object that does not exist yet, and the name is
/// still an instance variable; nothing here knows or guesses which object.
/// </remarks>
public enum RubyScope
{
    /// <summary>A name with no sigil, resolved when it runs.</summary>
    Plain,
    /// <summary>A local, written as a bare name inside a method or block.</summary>
    Local,
    /// <summary>An instance variable, written with one sigil.</summary>
    Instance,
    /// <summary>A class variable, written with two sigils.</summary>
    Class,
    /// <summary>A global, written with a sigil and a sigil name.</summary>
    Global,
    /// <summary>A constant, written with a leading capital.</summary>
    Constant,
}

/// <summary>A node in a parsed Ruby script.</summary>
/// <remarks>
/// A node is a shape, not a value. Nothing here has been evaluated: an
/// <see cref="RubyNodeKind.Identifier"/> node knows the name that was written
/// and not whether that name is a method, a local or something undefined, and a
/// <see cref="RubyNodeKind.Call"/> node knows that a call was written and not
/// what it does. Deciding those is a separate step, and it is the step that
/// would run the game's code, so it is not part of reading a script.
/// </remarks>
public sealed class RubyNode
{
    /// <summary>What kind of node this is.</summary>
    public required RubyNodeKind Kind { get; init; }

    /// <summary>The name, for the nodes that carry one.</summary>
    public string? Name { get; init; }

    /// <summary>The operator, for the operator nodes.</summary>
    public string? Operator { get; init; }

    /// <summary>The value, for a literal.</summary>
    public long? Integer { get; init; }

    /// <summary>The value, for a float literal.</summary>
    public double? Real { get; init; }

    /// <summary>The text, for a string, a symbol or a regular expression.</summary>
    public string? Text { get; init; }

    /// <summary>The bytes, for a string, because its encoding is not this reader's.</summary>
    public byte[]? Bytes { get; init; }

    /// <summary>
    /// The module or class a name was written under, or null.
    /// </summary>
    /// <remarks>
    /// This records the text as written and nothing more. A node that says
    /// `Actor` has not looked up whether `Actor` is a constant, exists, or is
    /// the thing a game means by it. Filling this in from a lookup would mix two
    /// phases into one, and the first has no business knowing the answer.
    /// </remarks>
    public string? Realm { get; init; }

    /// <summary>
    /// The receiver a call was written on, as a node, or null.
    /// </summary>
    /// <remarks>
    /// The receiver stays a node rather than becoming a value, so the tree can be
    /// walked without asking this parser what anything meant.
    /// </remarks>
    public RubyNode? Receiver { get; init; }

    /// <summary>
    /// Which kind of name this node is, or null when the kind says nothing.
    /// </summary>
    /// <remarks>
    /// A scope is a lexical fact about the text, not a statement about a binding.
    /// A script may write `@x` on an object that does not exist yet and the name
    /// is still an instance variable; nothing here knows which object.
    /// </remarks>
    public RubyScope? Scope { get; init; }

    /// <summary>The line in the source this node started on.</summary>
    public required int Line { get; init; }

    /// <summary>The children, in source order.</summary>
    public IReadOnlyList<RubyNode> Children { get; init; } = Array.Empty<RubyNode>();

    /// <summary>The first child, or null.</summary>
    public RubyNode? First => Children.Count > 0 ? Children[0] : null;

    /// <summary>The second child, or null.</summary>
    public RubyNode? Second => Children.Count > 1 ? Children[1] : null;

    /// <summary>The third child, or null.</summary>
    public RubyNode? Third => Children.Count > 2 ? Children[2] : null;

    /// <summary>The parts of a string literal, escapes included.</summary>
    public IReadOnlyList<RubyStringPart> Parts { get; init; } = Array.Empty<RubyStringPart>();

    public override string ToString()
    {
        if (Name != null)
        {
            return $"{Kind} {Name}";
        }
        if (Operator != null)
        {
            return $"{Kind} {Operator}";
        }
        if (Integer.HasValue)
        {
            return $"{Kind} {Integer.Value.ToString(CultureInfo.InvariantCulture)}";
        }
        if (Real.HasValue)
        {
            return $"{Kind} {Real.Value.ToString(CultureInfo.InvariantCulture)}";
        }
        if (Text != null)
        {
            return $"{Kind} \"{Text}\"";
        }
        return Children.Count == 0
            ? Kind.ToString()
            : $"{Kind}({Children.Count})";
    }

    /// <summary>
    /// Renders the node back to source-like text.
    /// </summary>
    /// <remarks>
    /// This is for diagnostics and for tests, not for running anything. It is
    /// not guaranteed to be a source a Ruby parser would accept, and it does not
    /// try to be: the point is to show the shape that was read.
    /// </remarks>
    public string Describe()
    {
        if (Children.Count == 0)
        {
            return ToString();
        }
        var parts = new List<string>();
        foreach (var child in Children)
        {
            parts.Add(child.Describe());
        }
        var head = Name != null
            ? Name
            : Operator != null
                ? Operator
                : Kind.ToString();
        return $"{head}({string.Join(", ", parts)})";
    }
}
