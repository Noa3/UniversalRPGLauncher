using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRubyCopyInitialization : TestBase
{
    public void Test_RescuedCopyFailureRestoresCallerReceiverAndMethod()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        // RunProgram creates the persistent top-level scope on first entry.
        interpreter.RunProgram(System.Array.Empty<RubyNode>());
        int Depth(string field) => ((System.Collections.ICollection)typeof(RubyInterpreter)
            .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(interpreter)!).Count;
        var scopeDepth = Depth("_scopes");
        var callDepth = Depth("_aufrufKette");
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            class CopyFailure
              def initialize_copy(original)
                raise "copy refused"
              end
            end
            class CopyCaller
              attr_reader :marker
              def run(source)
                @marker = 7
                begin
                  source.dup
                rescue RuntimeError
                  @marker = 11
                end
                $receiver_after = self
                $method_after = __method__()
                @marker
              end
            end
            $caller = CopyCaller.new
            $result = $caller.run(CopyFailure.new)
            $marker = $caller.marker
            """).Tokenize()).ParseProgram());
        AssertTrue(ReferenceEquals(interpreter.Global("$caller"), interpreter.Global("$receiver_after")),
            "A rescued copy failure must restore the caller's self.");
        AssertEq(interpreter.Global("$method_after").Name, "run");
        AssertEq(interpreter.Global("$result").Integer, 11);
        AssertEq(interpreter.Global("$marker").Integer, 11);
        AssertEq(Depth("_scopes"), scopeDepth, "A rescued failure must not retain method scopes.");
        AssertEq(Depth("_aufrufKette"), callDepth, "A rescued failure must not retain call frames.");
        AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
    }

    public void Test_CopyHookRunsOnTheNewReceiverAndItsReturnValueIsIgnored()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            $copy_hooks = 0
            class CopyProbe
              attr_accessor :value
              def initialize; @value = 5; end
              def initialize_copy(original)
                $copy_hooks = $copy_hooks + 1
                @value = original.value + 1
                99
              end
            end
            class ChildProbe < CopyProbe
            end
            $original = ChildProbe.new
            $duplicate = $original.dup
            $clone = $original.clone
            $sent = $original.send(:dup)
            $original_value = $original.value
            $duplicate_value = $duplicate.value
            $clone_value = $clone.value
            $sent_value = $sent.value
            """).Tokenize()).ParseProgram());
        AssertEq(interpreter.Global("$copy_hooks").Integer, 3);
        AssertEq(interpreter.Global("$original_value").Integer, 5);
        foreach (var name in new[] { "$duplicate", "$clone", "$sent" })
        {
            AssertEq(interpreter.Global(name).ClassName, "ChildProbe");
            AssertTrue(!ReferenceEquals(interpreter.Global(name), interpreter.Global("$original")));
        }
        AssertEq(interpreter.Global("$duplicate_value").Integer, 6);
        AssertEq(interpreter.Global("$clone_value").Integer, 6);
        AssertEq(interpreter.Global("$sent_value").Integer, 6);
        AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
    }
}
