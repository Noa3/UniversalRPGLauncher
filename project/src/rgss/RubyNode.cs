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
/// <summary>What a child of a node is for.</summary>
/// <remarks>
/// The parser used to say only that a child was there, which left every
/// consumer to know the layout of every kind: a keyword that opens a test
/// held the keyword first and the test second, a ternary held the test
/// first, a block on a call held the call, the parameters and the body, and
/// a block that was a body held only statements. One kind could therefore
/// mean two things, and a second meaning was invisible. Naming the role
/// means a consumer can ask for what it wants.
/// </remarks>
public enum RubyNodeRole
{
    /// <summary>The part of the tree that holds the other parts.</summary>
    Body,

    /// <summary>The value of a test.</summary>
    Condition,

    /// <summary>What runs when a test holds.</summary>
    WhenTrue,

    /// <summary>What runs when a test does not hold.</summary>
    WhenFalse,

    /// <summary>The left of an operation.</summary>
    Left,

    /// <summary>The right of an operation.</summary>
    Right,

    /// <summary>The name a call is made on.</summary>
    Receiver,

    /// <summary>The value assigned to.</summary>
    Target,

    /// <summary>The value assigned.</summary>
    Value,

    /// <summary>One argument of a call, in the order written.</summary>
    Argument,

    /// <summary>One parameter of a block, in the order written.</summary>
    Parameter,

    /// <summary>What a block runs.</summary>
    BlockBody,

    /// <summary>The part of a class or a method that gives it a name.</summary>
    Definition,

    /// <summary>One alternative of a case, in the order written.</summary>
    When,

    /// <summary>A statement, in the order written.</summary>
    Statement,
}

/// <summary>A child of a node together with what it is for.</summary>
public sealed class RubyNodePart
{
    /// <summary>What this child is for.</summary>
    public required RubyNodeRole Role { get; init; }

    /// <summary>The child itself.</summary>
    public required RubyNode Node { get; init; }
}
public sealed class RubyNode
{
    /// <summary>What kind of node this is.</summary>
    public required RubyNodeKind Kind { get; init; }

    /// <summary>The name, for the nodes that carry one.</summary>
    public string? Name { get; init; }

    /// <summary>
    /// The superclass as written, on a class node and nowhere else.
    /// </summary>
    /// <remarks>
    /// <strong>The parser records the text and does not look anything
    /// up</strong>, and that is the same rule the rest of the tree follows:
    /// a node says what was written, and what it means is a later phase's
    /// question. <strong>Only a class has one</strong>, because a module with
    /// a <c>&lt;</c> is not Ruby, and a reader that let one through would let
    /// a game's module inherit something it never asked for.
    /// </remarks>
    public string? Superclass { get; init; }

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
    /// <summary>
    /// The children with the role each one plays, empty when the parser
    /// recorded only a position.
    /// </summary>
    /// <remarks>
    /// This is the list to read. Children stays for the reader that wants the
    /// order and does not care what the order means, and a node whose parts
    /// are empty has not been given roles yet rather than having none.
    /// </remarks>
    public IReadOnlyList<RubyNodePart> Role_Children { get; init; } = Array.Empty<RubyNodePart>();

    /// <summary>The first child playing the given role, or null.</summary>
    public RubyNode? Part(RubyNodeRole pRole)
    {
        foreach (var part in Role_Children)
        {
            if (part.Role == pRole)
            {
                return part.Node;
            }
        }

        return null;
    }

    /// <summary>Every child playing the given role, in the order written.</summary>
    public IReadOnlyList<RubyNode> PartsOf(RubyNodeRole pRole)
    {
        var found = new List<RubyNode>();
        foreach (var part in Role_Children)
        {
            if (part.Role == pRole)
            {
                found.Add(part.Node);
            }
        }

        return found;
    }

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
