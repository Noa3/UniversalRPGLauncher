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
}
