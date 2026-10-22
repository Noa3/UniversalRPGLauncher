using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Production Update must resume one owned scene stack, never replay setup.</summary>
public partial class TestRgssResumableScene : TestBase
{
    public void Test_SceneTransitionsContinueWithoutReplayingTheOldSetup()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = CreateRuntime(type, graphics, interpreter);
            Run(interpreter, """
                $first_setup = 0
                $second_setup = 0
                $cleanup = 0
                class SecondScene
                  def main
                    $second_setup = $second_setup + 1
                    Graphics.update
                    $scene = nil
                  end
                end
                class FirstScene
                  def main
                    $first_setup = $first_setup + 1
                    Graphics.update
                    $scene = SecondScene.new
                    $cleanup = $cleanup + 1
                  end
                end
                $scene = FirstScene.new
                """);
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60.0).Success);
            AssertEq(interpreter.Global("$first_setup").Integer, 1);
            AssertEq(interpreter.Global("$second_setup").Integer, 0);
            AssertTrue(runtime.Update(1.0 / 60.0).Success);
            AssertEq(interpreter.Global("$cleanup").Integer, 1);
            AssertEq(interpreter.Global("$second_setup").Integer, 1);
            AssertEq(graphics.Frames, 2);
            AssertTrue(runtime.Update(1.0 / 60.0).Success);
            AssertEq(runtime.State, PluginRuntimeState.Stopped);
            AssertEq(graphics.Frames, 2);
        }
    }

    public void Test_StepBudgetIsRenewedOnlyAtHostFrameBoundaries()
    {
        var graphics = new RgssGrafikHost();
        var interpreter = new RubyInterpreter(graphics) { StepLimit = 100 };
        using var runtime = CreateRuntime(typeof(XpEngineRuntime), graphics, interpreter);
        Run(interpreter, """
            class BudgetedScene
              def main
                loop do
                  Graphics.update
                end
              end
            end
            $scene = BudgetedScene.new
            """);
        AssertTrue(runtime.Start().Success);
        for (var frame = 1; frame <= 40; frame++)
        {
            AssertTrue(runtime.Update(1.0 / 60.0).Success, "A yielding game must not hit a lifetime step limit.");
            AssertEq(graphics.Frames, frame);
        }
        AssertTrue(runtime.Stop().Success);
    }

    public void Test_NonYieldingScriptFaultsAndItsWorkerCanBeJoined()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost()) { StepLimit = 100 };
        var graphics = new RgssGrafikHost();
        using var session = new RgssFrameSession(interpreter, graphics,
            new RubyParser(new RubyLexer("loop do; end").Tokenize()).ParseProgram());
        AssertFalse(session.Advance(TimeSpan.FromSeconds(2)));
        AssertEq(session.State, RgssExecutionState.Faulted);
        AssertTrue(session.Error?.Contains("100 steps") == true, session.Error ?? "No failure reported");
        AssertTrue(session.Stop(TimeSpan.FromSeconds(2)));
        AssertFalse(session.IsWorkerAlive);
    }

    public void Test_DisposeWakesAndJoinsAPausedWorker()
    {
        var graphics = new RgssGrafikHost();
        var interpreter = new RubyInterpreter(graphics);
        Run(interpreter, "module Graphics; end");
        var session = new RgssFrameSession(interpreter, graphics,
            new RubyParser(new RubyLexer("loop do; rgss_present_frame(); end").Tokenize()).ParseProgram());
        try
        {
            AssertTrue(session.Advance(TimeSpan.FromSeconds(2)));
            AssertEq(session.State, RgssExecutionState.AtFrame);
            AssertTrue(session.IsWorkerAlive);
        }
        finally
        {
            session.Dispose();
        }
        AssertFalse(session.IsWorkerAlive);
        AssertEq(session.State, RgssExecutionState.Cancelled);
        AssertEq(graphics.Frames, 1);
        session.Dispose();
    }

    public void Test_TimeoutRetainsLiveResourcesUntilTheHostReturns()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var graphics = new RgssGrafikHost
        {
            Rahmen = _ =>
            {
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Test host was not released.");
            }
        };
        var interpreter = new RubyInterpreter(graphics);
        using var session = new RgssFrameSession(interpreter, graphics,
            new RubyParser(new RubyLexer("rgss_present_frame()").Tokenize()).ParseProgram());
        try
        {
            AssertFalse(session.Advance(TimeSpan.FromMilliseconds(200)));
            AssertTrue(entered.IsSet, "The timed-out worker reached the blocking host.");
            AssertEq(session.State, RgssExecutionState.Faulted);
            AssertTrue(session.Error?.Contains("allowed time") == true);
            AssertFalse(session.Stop(TimeSpan.FromMilliseconds(20)), "A blocked host must not be reported as stopped.");
            AssertTrue(session.IsWorkerAlive);
        }
        finally
        {
            release.Set();
        }
        AssertTrue(session.Stop(TimeSpan.FromSeconds(2)));
        AssertFalse(session.IsWorkerAlive);
        AssertEq(session.State, RgssExecutionState.Faulted, "Cancellation must not erase the timeout reason.");
        AssertFalse(session.Advance(TimeSpan.FromSeconds(1)));
    }

    public void Test_ConcurrentAdvanceIsRejectedWithoutLosingAFrame()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var graphics = new RgssGrafikHost
        {
            Rahmen = _ =>
            {
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Test host was not released.");
            }
        };
        var interpreter = new RubyInterpreter(graphics);
        using var session = new RgssFrameSession(interpreter, graphics,
            new RubyParser(new RubyLexer("rgss_present_frame()").Tokenize()).ParseProgram());
        var first = Task.Run(() => session.Advance(TimeSpan.FromSeconds(3)));
        try
        {
            AssertTrue(entered.Wait(TimeSpan.FromSeconds(2)));
            var refused = false;
            try { session.Advance(TimeSpan.FromSeconds(1)); }
            catch (InvalidOperationException) { refused = true; }
            AssertTrue(refused);
        }
        finally
        {
            release.Set();
        }
        AssertTrue(first.Wait(TimeSpan.FromSeconds(3)));
        AssertTrue(first.Result);
        AssertEq(session.Frames, 1);
        AssertTrue(session.Stop(TimeSpan.FromSeconds(2)));
        AssertFalse(session.IsWorkerAlive);
    }

    public void Test_ActiveSessionRejectsDiagnosticExecutionAndBudgetMutation()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = CreateRuntime(type, graphics, interpreter);
            Run(interpreter, """
                class OwnedScene
                  def main
                    loop do
                      Graphics.update
                    end
                  end
                end
                $scene = OwnedScene.new
                """);
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60.0).Success);
            var refusedMeasurement = false;
            try { type.GetMethod("SpielSchleifeStarten")!.Invoke(runtime, null); }
            catch (TargetInvocationException error) when (error.InnerException is InvalidOperationException)
            { refusedMeasurement = true; }
            AssertTrue(refusedMeasurement, type.Name + " has only one interpreter owner");
            var refusedMutation = false;
            try { type.GetProperty("FrameBudget")!.SetValue(runtime, 1); }
            catch (TargetInvocationException error) when (error.InnerException is InvalidOperationException)
            { refusedMutation = true; }
            AssertTrue(refusedMutation, type.Name + " cannot mutate a live worker's budget");
        }
    }

    public void Test_ContinuousDiagnosticsAreBoundedAndPublishedSnapshotsStayStable()
    {
        var graphics = new RgssGrafikHost();
        var interpreter = new RubyInterpreter(graphics);
        using var session = new RgssFrameSession(interpreter, graphics,
            new RubyParser(new RubyLexer("loop do; missing_feature(); rgss_present_frame(); end").Tokenize()).ParseProgram());
        AssertTrue(session.Advance(TimeSpan.FromSeconds(2)));
        var firstSnapshot = session.Diagnostics;
        AssertEq(firstSnapshot.Count, 1);
        for (var frame = 1; frame < 80; frame++)
            AssertTrue(session.Advance(TimeSpan.FromSeconds(2)));
        AssertEq(interpreter.Diagnostics.Count, 64, "The underlying list, not only its published copy, must be bounded.");
        AssertEq(session.Diagnostics.Count, 64);
        AssertEq(firstSnapshot.Count, 1, "An earlier frame snapshot must never change.");
        AssertTrue(session.Diagnostics[0].Contains("missing_feature"));
        AssertTrue(session.DroppedDiagnosticCount > 0, "Truncated diagnostic history must be observable.");
    }

    public void Test_ConfiguredFrameBudgetCompletesInsteadOfFaulting()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost { Budget = 2 };
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = CreateRuntime(type, graphics, interpreter);
            Run(interpreter, """
                class MeasuredScene
                  def main
                    loop do
                      Graphics.update
                    end
                  end
                end
                $scene = MeasuredScene.new
                """);
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60.0).Success);
            AssertTrue(runtime.Update(1.0 / 60.0).Success);
            AssertEq(graphics.Frames, 2);
            // The last callback stays paused until the caller acknowledges it.
            AssertTrue(runtime.Update(1.0 / 60.0).Success, type.Name + " reports a normal measured completion");
            AssertEq(runtime.State, PluginRuntimeState.Stopped);
            AssertEq(graphics.Frames, 2);
        }
    }

    public void Test_FaultedRuntimeCanStillStopItsOwnedWorker()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics) { StepLimit = 100 };
            using var runtime = CreateRuntime(type, graphics, interpreter);
            Run(interpreter, """
                class FaultingScene
                  def main
                    loop do
                    end
                  end
                end
                $scene = FaultingScene.new
                """);
            AssertTrue(runtime.Start().Success);
            AssertFalse(runtime.Update(1.0 / 60.0).Success);
            AssertEq(runtime.State, PluginRuntimeState.Faulted);
            AssertTrue(runtime.Stop().Success, type.Name + " must allow cleanup after a frame fault");
            AssertEq(runtime.State, PluginRuntimeState.Stopped);
        }
    }

    public void Test_ConcurrentStopDoesNotBecomeAScriptFailure()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var graphics = new RgssGrafikHost
            {
                Rahmen = _ =>
                {
                    entered.Set();
                    if (!release.Wait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException("Test host was not released.");
                }
            };
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = CreateRuntime(type, graphics, interpreter);
            Run(interpreter, """
                class StoppableScene
                  def main
                    loop do
                      Graphics.update
                    end
                  end
                end
                $scene = StoppableScene.new
                """);
            AssertTrue(runtime.Start().Success);
            var update = Task.Run(() => runtime.Update(1.0 / 60.0));
            Task<PluginOperationResult>? stop = null;
            try
            {
                AssertTrue(entered.Wait(TimeSpan.FromSeconds(2)));
                stop = Task.Run(() => runtime.Stop());
                AssertTrue(SpinWait.SpinUntil(() => interpreter.ExecutionCancellation.IsCancellationRequested,
                    TimeSpan.FromSeconds(1)), "Stop must be able to cancel an active Update.");
            }
            finally
            {
                release.Set();
            }
            if (!update.Wait(TimeSpan.FromSeconds(3)) || stop == null || !stop.Wait(TimeSpan.FromSeconds(3)))
                throw new TimeoutException("Concurrent lifecycle operations failed to return.");
            AssertTrue(stop.Result.Success);
            AssertFalse(update.Result.Success);
            AssertEq(runtime.State, PluginRuntimeState.Stopped);
            var problems = (System.Collections.Generic.IReadOnlyList<string>)type.GetProperty("ScriptProblems")!.GetValue(runtime)!;
            AssertEq(problems.Count, 0, type.Name + " must not record an intentional stop as a game failure");
        }
    }

    public void Test_MissingImageDiagnosticsStayBoundedAndSnapshotsStayStable()
    {
        var graphics = new RgssGrafikHost();
        graphics.BildGroesse("Bitmap", "first");
        var firstSnapshot = graphics.FehlendeBilder;
        for (var image = 0; image < 100; image++)
            graphics.BildGroesse("Bitmap", "missing_" + image);
        AssertEq(graphics.FehlendeBilder.Count, 64);
        AssertEq(firstSnapshot.Count, 1, "Published image diagnostics cannot expose the mutable list.");
        AssertTrue(graphics.FehlendeBilder[63].Contains("missing_99"));
        graphics.BildGroesse("Bitmap", "missing_99");
        AssertEq(graphics.FehlendeBilder.Count, 64);
        AssertTrue(graphics.FehlendeBilder[62].Contains("missing_98"), "Repeated missing files are deduplicated.");
    }

    public void Test_UpdatePreservesSceneLocalsAcrossHostFrameBoundaries()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = CreateRuntime(type, graphics, interpreter);
            Run(interpreter, """
                $setups = 0
                $observed = 0
                class PersistentScene
                  def main
                    $setups = $setups + 1
                    local_frame = 0
                    loop do
                      local_frame = local_frame + 1
                      $observed = local_frame
                      Graphics.update
                    end
                  end
                end
                $scene = PersistentScene.new
                """);
            AssertTrue(runtime.Start().Success, type.Name);
            for (var frame = 1; frame <= 3; frame++)
            {
                AssertTrue(runtime.Update(1.0 / 60.0).Success, type.Name);
                // Update's contract leaves the worker paused: these reads cannot race it.
                AssertEq(interpreter.Global("$setups").Integer, 1, type.Name + " setup runs once");
                AssertEq(interpreter.Global("$observed").Integer, frame, type.Name + " retains local state");
                AssertEq(graphics.Frames, frame, type.Name + " advances exactly one boundary");
            }
            AssertTrue(runtime.Stop().Success, type.Name);
            AssertFalse(runtime.Update(1.0 / 60.0).Success, type.Name);
        }
    }

    private static IEngineRuntime CreateRuntime(Type type, RgssGrafikHost graphics, RubyInterpreter interpreter)
    {
        var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        type.GetField("_interpreter", flags)!.SetValue(runtime, interpreter);
        type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
        type.GetField("_scriptsRun", flags)!.SetValue(runtime, true);
        type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
        type.GetProperty("State")!.SetValue(runtime, PluginRuntimeState.Initialized);
        return runtime;
    }

    private static void Run(RubyInterpreter interpreter, string source) =>
        interpreter.RunProgram(new RubyParser(new RubyLexer(source).Tokenize()).ParseProgram());
}
