using System;
using System.Collections.Generic;
using System.Reflection;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Verify that production Graphics methods cross the trusted host boundary.</summary>
public partial class TestRgssFrameBridge : TestBase
{
    public void Test_WaitPublishesEveryFrameAndFrameResetDoesNotResetTheCounter()
    {
        var graphics = new RgssGrafikHost();
        var interpreter = new RubyInterpreter(graphics);
        var runtimeType = typeof(XpEngineRuntime);
        var runtime = new XpEngineRuntime("test", new PluginGameInfo());
        runtimeType.GetField("_interpreter", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(runtime, interpreter);
        runtimeType.GetMethod("DefineBuiltinClasses", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(runtime, null);
        interpreter.RunProgram(new RubyParser(new RubyLexer("""
            Graphics.update
            Graphics.wait(2)
            Graphics.frame_reset
            $count = Graphics.frame_count
            """).Tokenize()).ParseProgram());
        AssertEq(graphics.Frames, 3);
        AssertEq(interpreter.Global("$count").Integer, 3,
            "frame_reset resets timing, not the count; wait advances both host and Ruby frames.");
        runtime.Dispose();
    }

    public void Test_MeasuredSceneCompletionDoesNotRunTheLegacyUpdateLoopAgain()
    {
        foreach (var runtimeType in new[]
            { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost { Budget = 4 };
            var interpreter = new RubyInterpreter(graphics);
            var runtime = (IEngineRuntime)Activator.CreateInstance(runtimeType, "test", new PluginGameInfo())!;
            runtimeType.GetField("_interpreter", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(runtime, interpreter);
            runtimeType.GetField("_grafik", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(runtime, graphics);
            runtimeType.GetMethod("DefineBuiltinClasses", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(runtime, null);
            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                class BudgetScene
                  def main
                    loop do
                      Graphics.update
                    end
                  end
                end
                $scene = BudgetScene.new
                """).Tokenize()).ParseProgram());
            runtimeType.GetProperty("State")!.SetValue(runtime, PluginRuntimeState.Initialized);
            AssertTrue(runtime.Start().Success);
            runtimeType.GetMethod("SpielSchleifeStarten")!.Invoke(runtime, null);
            AssertEq(graphics.Frames, 4, runtimeType.Name);
            AssertTrue(graphics.BudgetErreicht, runtimeType.Name);
            AssertEq(runtime.State, PluginRuntimeState.Stopped, runtimeType.Name);
            AssertFalse(runtime.Update(1.0 / 60.0).Success,
                "An unwound measured scene is terminal, not a resumable continuation.");
            AssertEq(graphics.Frames, 4, runtimeType.Name);
            runtime.Dispose();
        }
    }

    public void Test_EachGenerationPublishesHostFramesFromItsOwnSceneLoop()
    {
        foreach (var runtimeType in new[]
            { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var observed = new List<int>();
            var graphics = new RgssGrafikHost { Rahmen = observed.Add };
            var interpreter = new RubyInterpreter(graphics);
            var runtime = Activator.CreateInstance(runtimeType, "test", new PluginGameInfo());
            runtimeType.GetField("_interpreter", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(runtime, interpreter);
            runtimeType.GetMethod("DefineBuiltinClasses", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(runtime, null);

            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                $setup_count = 0
                $updates = 0
                class FrameScene
                  def main
                    $setup_count = $setup_count + 1
                    loop do
                      Graphics.update
                      $updates = $updates + 1
                      break if $updates == 3
                    end
                  end
                end
                $scene = FrameScene.new
                $scene.main
                $frame_count = Graphics.frame_count
                """).Tokenize()).ParseProgram());

            AssertEq(graphics.Frames, 3, runtimeType.Name + " must invoke the host, not only count in Ruby.");
            AssertEq(observed.Count, 3, runtimeType.Name + " must publish each frame.");
            AssertEq(interpreter.Global("$frame_count").Integer, 3, runtimeType.Name);
            AssertEq(interpreter.Global("$setup_count").Integer, 1, runtimeType.Name);
            AssertEq(interpreter.Global("$updates").Integer, 3, runtimeType.Name);
            AssertEq(interpreter.Diagnostics.Count, 0, runtimeType.Name);
        }
    }
}
