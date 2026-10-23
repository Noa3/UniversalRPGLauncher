using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Library setup for the known MicroQuest fixture, not a production script loader.</summary>
internal static class MicroQuestScriptFixture
{
    public static bool RunLibraryScript(RubyInterpreter interpreter, string name, IReadOnlyList<RubyNode> program)
    {
        // These partial-host tests inspect library classes, not the title screen.
        // Callers still parse the known fixture's Main; the real runtime tests
        // execute it on the owned worker with native built-ins.
        if (string.Equals(name, "Main", StringComparison.Ordinal)) return false;
        interpreter.RunProgram(program);
        return true;
    }
}

public partial class TestMicroQuestScriptFixture : TestBase
{
    private const string Scripts = "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data/Scripts.rxdata";

    public void Test_LibrarySetupDefersTheKnownEntryWithoutCountingItAsExecuted()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        AssertTrue(MicroQuestScriptFixture.RunLibraryScript(interpreter, "Game_Probe",
            Parse("$library_loaded = true")));
        AssertFalse(MicroQuestScriptFixture.RunLibraryScript(interpreter, "Main",
            Parse("$entry_started = true")));
        AssertTrue(interpreter.Global("$library_loaded").Boolean);
        AssertEq(interpreter.Global("$entry_started").Kind, RubyValueKind.Nil);
    }

    public void Test_LibraryRuntimeFailuresAreNotAcceptedOrSuppressed()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        RubyRuntimeException? observed = null;
        try
        {
            MicroQuestScriptFixture.RunLibraryScript(interpreter, "Game_Broken",
                Parse("raise 'library failure sentinel'"));
        }
        catch (RubyRuntimeException error)
        {
            observed = error;
        }
        AssertTrue(observed != null && observed.Message.Contains("library failure sentinel"));
    }

    public void Test_RealFixtureParsesEverySourceAndExecutesEveryLibrary()
    {
        var host = RgssSkriptHost.Lese(Scripts, out var error);
        AssertTrue(host != null, error);
        if (host == null) return;
        var names = host.Namen.ToArray();
        AssertEq(names.Count(name => name == "Main"), 1);
        AssertEq(names.Last(), "Main", "This library-only fixture requires its known final entry.");
        var interpreter = new RubyInterpreter(host);
        var executed = 0;
        var deferred = 0;
        foreach (var name in names)
        {
            var source = host.ReadScript(name, true)
                ?? throw new InvalidOperationException("Missing fixture source: " + name);
            var program = Parse(Encoding.UTF8.GetString(source));
            try
            {
                if (MicroQuestScriptFixture.RunLibraryScript(interpreter, name, program)) executed++;
                else deferred++;
            }
            catch (RubyRuntimeException failure)
            {
                throw new InvalidOperationException("Fixture script " + name + ": " + failure.Message, failure);
            }
        }
        AssertEq(executed, names.Length - 1);
        AssertEq(deferred, 1);
        AssertEq(executed + deferred, names.Length);
        Console.WriteLine($"MicroQuest: {names.Length} parsed, {executed} library scripts executed, {deferred} entry deferred.");
    }

    private static IReadOnlyList<RubyNode> Parse(string source) =>
        new RubyParser(new RubyLexer(source).Tokenize()).ParseProgram();
}
