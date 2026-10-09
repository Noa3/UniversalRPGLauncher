using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Input;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestMzTitleCommandIdentity : TestBase
{
    private const string Project = "E:/RPGMakerGames/CamelliaCoronation-Win";

    public void Test_LocalizedNewGameUsesItsCommandIdentity()
    {
        WithTitle(runtime =>
        {
            SetTerm(runtime, 18, "Neues Spiel");
            RefreshCommands(runtime);
            AssertEq(runtime.TitleSelection, "Neues Spiel", "Displayed text uses the localized game term");
            AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm),
                $"Localized new-game command starts: {runtime.TitleProblem}");
            AssertFalse(runtime.TitleVisible, "Title closes after selecting newGame");
            AssertEq(runtime.CurrentMapId, 2, "The real fixture's start map opens");
        });
    }

    public void Test_AnOptionsCaptionCannotMasqueradeAsNewGame()
    {
        WithTitle(runtime =>
        {
            SetTerm(runtime, 11, "New Game");
            RefreshCommands(runtime);
            runtime.MoveTitleCursor(1);
            AssertEq(runtime.TitleSelection, "New Game", "Caption collision is present in the fixture");
            AssertFalse(runtime.SubmitInput(Rm2kInputAction.Confirm),
                "An unsupported options scene is refused even if its caption says New Game");
            AssertTrue(runtime.TitleVisible, "Refusal does not unexpectedly start a game");
        });
    }

    // Modify only the already-loaded display vocabulary. The real command
    // construction and input path remain under test; no imported code runs.
    private static void SetTerm(MzEngineRuntime runtime, int index, string term)
    {
        var field = typeof(MzEngineRuntime).GetField("_termsCommands", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var terms = (List<string>)field.GetValue(runtime)!;
        while (terms.Count <= index) { terms.Add(""); }
        terms[index] = term;
    }

    private static void RefreshCommands(MzEngineRuntime runtime) =>
        typeof(MzEngineRuntime).GetMethod("ReadTitleCommands", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(runtime, null);

    private void WithTitle(Action<MzEngineRuntime> test)
    {
        if (!Directory.Exists(Project + "/data"))
        {
            Console.WriteLine("Skipped: Camellia real-game fixture is unavailable.");
            return;
        }
        using var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var result = host.Start(new PluginGameInfo
        {
            GameDirectory = Project,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
            PresentTitleScreen = true
        });
        AssertTrue(result.Success, $"Real fixture starts: {result.Error?.Message}");
        if (result.Success && host.Runtime is MzEngineRuntime runtime) { test(runtime); }
    }
}
