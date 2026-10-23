using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRubyBlockBodyDispatch : TestBase
{
    public void Test_CallFirstBranchRescueAndEnsureBodiesKeepTheirFollowingStatements()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            module BodyProbe
              def self.touch; true; end
            end
            if true
              BodyProbe.touch()
              $branch = true
              $branch_tail = true
            end
            begin
              BodyProbe.touch()
              $normal = true
              raise "expected failure"
            rescue
              BodyProbe.touch()
              $rescued = true
              $rescue_tail = true
            ensure
              BodyProbe.touch()
              $ensured = true
              $ensure_tail = true
            end
            case 1
            when 1
              BodyProbe.touch()
              $case_body = true
              $case_tail = true
            end
            """).Tokenize()).ParseProgram());
        foreach (var name in new[] { "$branch", "$branch_tail", "$normal", "$rescued", "$rescue_tail", "$ensured", "$ensure_tail", "$case_body", "$case_tail" })
            AssertTrue(interpreter.Global(name).Boolean, name);
        AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
    }

    public void Test_MethodStartingWithACallExecutesAllFollowingStatements()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            module Starter
              def self.begin_work; $started = true; end
            end
            class CallFirstScene
              def main
                Starter.begin_work()
                $after_call = 41
                $finished = true
              end
            end
            CallFirstScene.new.main
            """).Tokenize()).ParseProgram());
        AssertTrue(interpreter.Global("$started").Boolean);
        AssertEq(interpreter.Global("$after_call").Integer, 41);
        AssertTrue(interpreter.Global("$finished").Boolean);
        AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
    }
}
