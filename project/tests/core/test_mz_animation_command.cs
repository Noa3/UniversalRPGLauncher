using System;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And <c>212 Show Animation</c> reaches the runtime, and survives a repaint.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the one thing a list could not do.</strong>
/// <c>MzBranchFacts.AnimationAsked</c> was written by the command and read by
/// nobody -- <strong>and it could not have been read</strong>: the command
/// runs with the child facts <c>WithCharacters</c> builds, so a list that
/// grows there is thrown away when the page ends. Measured:
/// <c>grep -rn "AnimationAsked"</c> finds the declaration, the copy, and one
/// write, and no reader at all.
/// </para>
/// <para>
/// <strong>And a delegate is carried across the copy and keeps pointing at
/// the runtime.</strong> That is what makes it work no matter which facts
/// object the command holds -- and it is the property this test measures,
/// because <c>Repaint()</c> replaces <c>Facts</c> with a fresh copy on every
/// map load and on every repaint that rebuilds the figures.
/// </para>
/// <para>
/// <strong>And the figure alone cannot carry the animation.</strong> Measured
/// on the balloon: <c>Repaint()</c> does <c>new MzCharacter(...)</c> for every
/// event and <c>BuildFigure()</c> for the player, so anything that lives on
/// the figure goes with it. The run lives in the runtime instead.
/// </para>
/// </remarks>
public partial class TestMzAnimationCommand : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden() =>
        File.Exists(Projekt + "/data/System.json")
        && File.Exists(Projekt + "/data/Map002.json");

    private static MzEngineRuntime Start()
    {
        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        if (!started.Success)
        {
            host.Dispose();
            throw new InvalidOperationException(
                $"the MZ game did not start: {started.Error?.Message}");
        }
        return (MzEngineRuntime)host.Runtime!;
    }

    /// <summary>
    /// And the runtime is reachable through the facts it was handed.
    /// </summary>
    public void Test_DieLaufzeitIstDurchDieTatsachenErreichbar()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        Console.WriteLine($"MZ animation 212: the callback is "
            + $"{(runtime.Facts.AnimationGestartet == null ? "missing" : "set")}");
        AssertTrue(runtime.Facts.AnimationGestartet != null,
            "**and `212` can reach the runtime at all**");

        // **Und es ueberlebt den Neuaufbau**, und genau daran scheiterte die
        // Liste: `Repaint` ersetzt `Facts` durch `WithCharacters(...)`.
        runtime.Repaint();
        Console.WriteLine($"MZ animation 212: after a repaint the callback is "
            + $"{(runtime.Facts.AnimationGestartet == null ? "missing" : "set")}");
        AssertTrue(runtime.Facts.AnimationGestartet != null,
            "**and the copy carries it, which a list added to could not do**");
    }

    /// <summary>
    /// And a page that asks for one gets one, and it ends.
    /// </summary>
    public void Test_EinBildLaeuftUndEndet()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Repaint();

        var erste = 0;
        foreach (var (id, _) in runtime.Animations)
        {
            erste = id;
            break;
        }
        AssertTrue(erste > 0, "and this project has animations");

        // **Und so ruft der Befehl es auf**, mit derselben Zahl fuer den
        // Spieler, die `205` benutzt.
        runtime.Facts.AnimationGestartet!(-1, erste);
        Console.WriteLine($"MZ animation 212: animation {erste} runs="
            + $"{runtime.AnimationLaeuft(-1)}");
        AssertTrue(runtime.AnimationLaeuft(-1),
            "**and asking for it starts it, which is what 212 does**");

        var animation = runtime.Animations[erste];
        var gesehen = 0;
        for (var i = 0; i < animation.DurationFrames() + 10
            && runtime.AnimationLaeuft(-1); i++)
        {
            runtime.Tick();
            gesehen++;
        }
        Console.WriteLine($"MZ animation 212: it ended after {gesehen} frames, "
            + $"and its own duration is {animation.DurationFrames()}");
        AssertFalse(runtime.AnimationLaeuft(-1),
            "**and the wait a page asks for does end**");
        AssertTrue(gesehen >= animation.MaxTimingFrames,
            "and it lasted at least past its own timings");
    }

    /// <summary>
    /// And a number the project does not have is reported, not ignored.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the engine's own gap.</strong> Measured at
    /// <c>command212</c>: <c>this._character.requestAnimation(animationId)</c>
    /// with an id the project does not have puts <c>undefined</c> into
    /// <c>_animationId</c>, and the sprite then asks
    /// <c>$dataAnimations[undefined]</c> -- <strong>so the engine does not
    /// cover it either, and a reader that swallowed the number would hide a
    /// page that names an animation its own project lacks.</strong>
    /// </remarks>
    public void Test_EineUnbekannteNummerWirdGemeldet()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        var vorher = runtime.Facts.Notices.Count;
        runtime.Facts.AnimationGestartet!(-1, 99999);

        Console.WriteLine($"MZ animation 212: asking for 99999 added "
            + $"{runtime.Facts.Notices.Count - vorher} notice(s)");
        AssertTrue(runtime.Facts.Notices.Count > vorher,
            "**and the page is told, because the project has no such animation**");
        AssertFalse(runtime.AnimationLaeuft(-1), "and nothing runs");
    }
}
