using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;

namespace UniversalRPG.Tests.Core;

public partial class TestRgssSpriteFrames
{
    public void Test_FirstFrameHasASeparateStartupDeadlineButLaterFramesRemainBounded()
    {
        var graphics = new RgssGrafikHost();
        var interpreter = new RubyInterpreter(graphics);
        graphics.Rahmen = frame => { if (frame == 2) Thread.Sleep(150); };
        var program = new RubyParser(new RubyLexer("rgss_present_frame(); rgss_present_frame()").Tokenize()).ParseProgram();
        using var session = new RgssFrameSession(interpreter, graphics, program, () => Thread.Sleep(150));
        AssertTrue(session.Advance(TimeSpan.FromMilliseconds(40)), "Startup must not use the normal frame deadline.");
        AssertEq(session.Frames, 1);
        AssertFalse(session.Advance(TimeSpan.FromMilliseconds(40)));
        AssertEq(session.State, RgssExecutionState.Faulted);
        AssertTrue(session.Error?.Contains("scene") == true);
    }

    public void Test_CompletionOfProjectScriptsDoesNotInventAnotherEntryPoint()
    {
        foreach (var presentsFrame in new[] { false, true })
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            var setup = new RgssLaufzeit(interpreter, null);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_laufzeit", flags)!.SetValue(runtime, setup);
            InstallStartup(type, runtime, new Dictionary<string, byte[]>
            {
                ["entry"] = Encoding.UTF8.GetBytes(presentsFrame
                    ? "Graphics.update; $entry_returned = true" : "$entry_returned = true"),
            });
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            if (presentsFrame) AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertEq(runtime.State, PluginRuntimeState.Stopped);
            AssertTrue(interpreter.Global("$entry_returned").Boolean);
            AssertEq(setup.Verweigert, 0, "A returning Ruby program must not trigger synthetic game setup.");
            AssertEq(setup.Gesetzt.Count, 0);
        });
    }

    public void Test_StartupProgressDistinguishesPendingRunningAndBudgetTermination()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            InstallStartup(type, runtime, new Dictionary<string, byte[]>
            {
                ["entry"] = Encoding.UTF8.GetBytes("loop do; Graphics.update; end"),
                ["unreached"] = Encoding.UTF8.GetBytes("$unreached = true"),
            });
            var property = type.GetProperty("ScriptProgress");
            AssertTrue(property != null, "Every RGSS generation must expose an immutable startup receipt.");
            if (property == null) return;
            var pending = property.GetValue(runtime)!;
            AssertEq(ProgressState(pending), "Pending");
            graphics.Budget = 1;
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            var running = property.GetValue(runtime)!;
            AssertEq(ProgressState(running), "Running");
            AssertEq((int)running.GetType().GetProperty("Completed")!.GetValue(running)!, 0);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            var stopped = property.GetValue(runtime)!;
            AssertEq(ProgressState(stopped), "FrameBudget");
            AssertEq(ProgressState(running), "Running", "Previously published receipts must stay unchanged.");
            AssertEq((int)stopped.GetType().GetProperty("Total")!.GetValue(stopped)!, 2);
            AssertEq((int)stopped.GetType().GetProperty("Attempted")!.GetValue(stopped)!, 1);
            AssertEq((int)stopped.GetType().GetProperty("Completed")!.GetValue(stopped)!, 0);
            AssertEq(interpreter.Global("$unreached").Kind, RubyValueKind.Nil);
        });
    }

    public void Test_ReturningProjectDoesNotReplayASceneLeftInAGlobal()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            InstallStartup(type, runtime, new Dictionary<string, byte[]>
            {
                ["entry"] = Encoding.UTF8.GetBytes("""
                    class LeftoverScene
                      def main; raise "the host replayed a finished project"; end
                    end
                    Graphics.update
                    $scene = LeftoverScene.new
                    """),
            });
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertEq(runtime.State, PluginRuntimeState.Stopped);
        });
    }

    public void Test_StartupTimeoutNamesThePhaseAndFailureReceiptsAreExact()
    {
        var graphics = new RgssGrafikHost();
        var interpreter = new RubyInterpreter(graphics);
        using (var session = new RgssFrameSession(interpreter, graphics, Array.Empty<RubyNode>(),
            () => Thread.Sleep(150), TimeSpan.FromMilliseconds(40)))
        {
            AssertFalse(session.Advance(TimeSpan.FromSeconds(1)));
            AssertEq(session.State, RgssExecutionState.Faulted);
            AssertTrue(session.Error?.Contains("startup") == true);
        }
        ForEachGeneration((type, host, ruby, runtime) =>
        {
            InstallStartup(type, runtime, new Dictionary<string, byte[]>
            {
                ["broken script"] = Encoding.UTF8.GetBytes("raise 'named startup failure'"),
                ["following script"] = Encoding.UTF8.GetBytes("$following_ran = true"),
            });
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            var progress = (RgssScriptProgress)type.GetProperty("ScriptProgress")!.GetValue(runtime)!;
            AssertEq(progress.State, RgssScriptRunState.Completed);
            AssertEq(progress.Total, 2);
            AssertEq(progress.Attempted, 2);
            AssertEq(progress.Completed, 1);
            AssertEq(progress.Failed, 1);
            AssertTrue(ruby.Global("$following_ran").Boolean);
            var problems = (IReadOnlyList<string>)type.GetProperty("ScriptProblems")!.GetValue(runtime)!;
            AssertEq(problems.Count, 1);
            AssertTrue(problems[0].Contains("broken script"));
            AssertFalse(runtime.Update(1.0 / 60).Success);
            AssertTrue(ReferenceEquals(progress, type.GetProperty("ScriptProgress")!.GetValue(runtime)));
        });
    }

    private static string ProgressState(object progress) => progress.GetType().GetProperty("State")!.GetValue(progress)!.ToString()!;

    private static void InstallStartup(Type type, IEngineRuntime runtime, Dictionary<string, byte[]> scripts)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var host = (RgssSkriptHost)Activator.CreateInstance(typeof(RgssSkriptHost), flags,
            null, new object[] { scripts }, null)!;
        type.GetField("_scriptHost", flags)!.SetValue(runtime, host);
        type.GetField("_bootstrapPending", flags)!.SetValue(runtime, true);
    }
}
