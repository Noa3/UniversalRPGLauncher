using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRubySelfIdentity : TestBase
{
    public void Test_SceneSelfKeepsTheSceneLoopAliveUntilARealTransition()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost()) { StepLimit = 2000 };
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            $frames = 0
            class SceneIdentity
              def main
                loop do
                  $frames = $frames + 1
                  $scene = nil if $frames == 3
                  break if $scene != self
                end
              end
            end
            $scene = SceneIdentity.new
            $scene.main
            """).Tokenize()).ParseProgram());
        AssertEq(interpreter.Global("$frames").Integer, 3,
            "The current scene must equal self until the scene actually changes.");
    }

    public void Test_ModuleSelfDoesNotInheritTheCallingInstance()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            module IdentityModule
              def self.identity
                self
              end
            end
            class IdentityCaller
              def identity
                IdentityModule.identity
              end
            end
            $module = IdentityCaller.new.identity
            """).Tokenize()).ParseProgram());
        AssertEq(interpreter.Global("$module").Name, "IdentityModule",
            "A module method's self is its receiver, not the calling object.");
    }
}
