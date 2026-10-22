using System;
using System.Reflection;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Exercise each production bootstrap without loading game scripts.</summary>
public partial class TestRgssBitmapFont : TestBase
{
    public void Test_BitmapsOwnIndependentWritableFontsInEveryRgssGeneration()
    {
        foreach (var runtimeType in new[]
            { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var interpreter = new RubyInterpreter(new RubyNullHost());
            var runtime = Activator.CreateInstance(runtimeType, "test", new PluginGameInfo());
            // Invoke only the actual built-in bootstrap, never Start or a game's scripts.
            runtimeType.GetField("_interpreter", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(runtime, interpreter);
            runtimeType.GetMethod("DefineBuiltinClasses", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(runtime, null);

            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                $first = Bitmap.new(16, 16)
                $second = Bitmap.new(16, 16)
                $font_missing = $first.font.nil?
                $first.font.size = 28
                $first.font.color = Color.new(12, 34, 56, 78)
                $size = $first.font.size
                $red = $first.font.color.red
                $alpha = $first.font.color.alpha
                $other_size = $second.font.size
                $other_red = $second.font.color.red
                """).Tokenize()).ParseProgram());

            AssertFalse(interpreter.Global("$font_missing").Boolean, runtimeType.Name);
            AssertEq(interpreter.Global("$size").Integer, 28, runtimeType.Name);
            AssertEq(interpreter.Global("$red").Integer, 12, runtimeType.Name);
            AssertEq(interpreter.Global("$alpha").Integer, 78, runtimeType.Name);
            AssertEq(interpreter.Global("$other_size").Integer, 22, runtimeType.Name);
            AssertEq(interpreter.Global("$other_red").Integer, 255, runtimeType.Name);
            AssertEq(interpreter.Diagnostics.Count, 0, runtimeType.Name);
        }
    }
}
