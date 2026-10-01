using System;
using System.Collections.Generic;

namespace UniversalRPG.Rgss;

/// <summary>The kind of a node in a parsed Ruby script.</summary>
public enum RubyNodeKind
{
    // Leaves.
    Nil,
    True,
    False,
    Self,
    Integer,
    Float,
    String,
    Symbol,
    Regexp,
    Array,
    Hash,
    Range,
    Identifier,
    Constant,
    InstanceVariable,
    ClassVariable,
    GlobalVariable,
    KeywordLiteral,

    // Operators.
    Unary,
    Binary,
    LogicalAnd,
    LogicalOr,
    Not,
    Ternary,
    Assignment,
    OpAssignment,

    // Calls and receivers.
    Call,
    MethodCall,
    SuperCall,
    Yield,
    SelfCall,

    // Control flow.
    If,
    While,
    Until,
    For,
    Case,
    Return,
    Break,
    Next,
    Redo,
    Retry,
    Begin,

    // Questions.
    Defined,

    // Withdrawals.
    Undef,

    // Definitions.
    Alias,
    Class,
    Module,
    Def,
    DefS,

    // Blocks.
    Block,
    BlockPass,
    DoBlock,

    /// <summary>A star that takes the rest of the values.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is not the <c>BlockPass</c> above, and the two
    /// are different things that look the same.</strong>
    /// </para>
    /// <para>
    /// <strong>A <c>BlockPass</c> is a <c>&amp;</c> passed to a
    /// method</strong> -- <c>each(&amp;block)</c> -- <strong>and it is
    /// named, because a block has to go somewhere.</strong>
    /// <strong>This is a <c>*</c> on the left of an <c>=</c></strong>:
    /// <c>a, *rest = list</c>, <strong>and it collects.</strong>
    /// </para>
    /// <para>
    /// Measured at Ruby 1.8.1's own <c>parse.y</c>:
    /// <c>mlhs_basic : mlhs_head tSTAR mlhs_node { $$ = NEW_MASGN($1,
    /// $3); }</c> and <c>mlhs_basic : mlhs_head tSTAR { $$ =
    /// NEW_MASGN($1, -1); }</c>. <strong>The <c>-1</c> is how the engine
    /// writes "a star with no name after it"</strong> -- <strong>and a
    /// reader that demanded a name stopped on every one of those.</strong>
    /// </para>
    /// </remarks>
    Splat,
}
