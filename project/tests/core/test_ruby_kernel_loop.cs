using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Regression coverage for Kernel#loop and its attached block.</summary>
public partial class TestRubyKernelLoop : TestBase
{
    public void Test_OrdinaryMethodBodyIsNotAnAttachedBlock()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            class BodyScene
              def clear
                @value = 0
              end
              def setup
                clear
                @value = 41
                @value + 1
              end
            end
            $result = BodyScene.new.setup
            """).Tokenize()).ParseProgram());
        AssertEq(interpreter.Global("$result").Integer, 42,
            "An ordinary statement list must not be mistaken for a block wrapper.");
    }

    public void Test_ScriptDefinedLoopOverridesTheKernelFallback()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            class CustomLoop
              def loop
                29
              end
              def value
                loop
              end
            end
            $value = CustomLoop.new.value
            """).Tokenize()).ParseProgram());
        AssertEq(interpreter.Global("$value").Integer, 29,
            "Kernel fallback must not intercept the game's own method.");
        AssertEq(interpreter.Diagnostics.Count, 0);
    }

    public void Test_NestedLoopsConsumeOnlyTheirOwnBreakAndNext()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost()) { StepLimit = 2000 };
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            $outer = 0
            $inner = 0
            $tail = 0
            loop do
              $outer = $outer + 1
              next if $outer == 1
              loop { $inner = $inner + 1; break }
              break if $outer == 3
              $tail = $tail + 1
            end
            $after = 1
            """).Tokenize()).ParseProgram());

        AssertEq(interpreter.Global("$outer").Integer, 3);
        AssertEq(interpreter.Global("$inner").Integer, 2);
        AssertEq(interpreter.Global("$tail").Integer, 1);
        AssertEq(interpreter.Global("$after").Integer, 1);
    }

    public void Test_LoopCannotBypassTheStepLimit()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost()) { StepLimit = 100 };
        var exceeded = false;
        try
        {
            interpreter.RunProgram(new RubyParser(new RubyLexer("loop do; end")
                .Tokenize()).ParseProgram());
        }
        catch (RubyRuntimeException error)
        {
            exceeded = error.Message.Contains("100 steps");
        }
        AssertTrue(exceeded, "An endless empty block must exhaust the step budget.");
    }

    public void Test_LoopReceivesItsBlockAndReturnsTheBreakValue()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost()) { StepLimit = 1000 };
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            $iterations = 0
            $result = loop do
              $iterations = $iterations + 1
              break 37 if $iterations == 3
            end
            $after = 91
            """).Tokenize()).ParseProgram());

        AssertEq(interpreter.Global("$iterations").Integer, 3,
            "The attached block must run until its break condition.");
        AssertEq(interpreter.Global("$result").Integer, 37,
            "Kernel#loop must return the value carried by break.");
        AssertEq(interpreter.Global("$after").Integer, 91,
            "Break must not escape into the caller.");
        AssertEq(interpreter.Diagnostics.Count, 0,
            "A valid loop must not report a missing block or method.");
    }
}
