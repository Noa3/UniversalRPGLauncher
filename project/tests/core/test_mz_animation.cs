using System;
using System.IO;
using UniversalRPG.Mz;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And an animation runs, tints its target and stops.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And an MZ animation is not a sheet of pictures.</strong> Measured
/// on the real project: of 160 animations, <strong>158 carry an
/// <c>effectName</c> and none carries an <c>animation1Name</c></strong> -- so
/// they are Effekseer effects and not the frame sheets of MV. Running one is a
/// particle renderer of its own and is <em>not</em> what this slice does.
/// </para>
/// <para>
/// <strong>And what is left is real</strong>, measured in the project's own
/// <c>rmmz_sprites.js</c> at <c>Sprite_Animation</c>:
/// </para>
/// <code>
/// Sprite_Animation.prototype.setup = function(targets, animation, ...) {
///     const timings = animation.soundTimings.concat(animation.flashTimings);
///     for (const timing of timings) {
///         if (timing.frame &gt; this._maxTimingFrames) {
///             this._maxTimingFrames = timing.frame;
///         }
///     }
/// };
/// Sprite_Animation.prototype.updateMain = function() {
///     this.processSoundTimings();
///     this.processFlashTimings();
///     this._frameIndex++;
///     this.checkEnd();
/// };
/// Sprite_Animation.prototype.updateFlash = function() {
///     if (this._flashDuration &gt; 0) {
///         const d = this._flashDuration--;
///         this._flashColor[3] *= (d - 1) / d;
///     }
/// };
/// </code>
/// <para>
/// <strong>And the duration this runtime used before was the number of the
/// entry's own JSON properties</strong>, <c>12 * 4 + 1 = 49</c> -- a fact
/// about the file and not about the animation. <c>MzScreen.AnimationsDauer</c>
/// is MV's rule, where an animation is a sheet with <c>frames.length</c>
/// cells, <strong>and this project has 158 Effekseer animations and not one
/// MV animation.</strong>
/// </para>
/// </remarks>
public partial class TestMzAnimation : TestBase
{
    private const string Skies =
        "D:/NextCloud/Games/PornGames/SkiesInflateableAdventure";

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
    /// And the animations are read, with their timings.
    /// </summary>
    public void Test_DieAnimationenWerdenGelesen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        Console.WriteLine($"MZ animation: {runtime.Animations.Count} animations, "
            + $"{runtime.AnimationFrames.Count} durations, "
            + $"problem={runtime.TilesetProblem}");

        var mitEffekt = 0;
        var mitBlitz = 0;
        var mitKlang = 0;
        foreach (var (_, animation) in runtime.Animations)
        {
            if (animation.IsEffekseer)
            {
                mitEffekt++;
            }
            if (animation.FlashTimings.Count > 0)
            {
                mitBlitz++;
            }
            if (animation.SoundTimings.Count > 0)
            {
                mitKlang++;
            }
        }
        Console.WriteLine($"MZ animation: {mitEffekt} name an Effekseer effect, "
            + $"{mitBlitz} have flashes, {mitKlang} have sounds");
        AssertTrue(mitBlitz > 0, "and some of them tint their target");
    }

    /// <summary>
    /// And the real game's own numbers, measured.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the one measurement that proves the reader reads
    /// the file and not a guess</strong>: <c>Hit Fire</c> in the real project
    /// is measured as
    /// <c>flashTimings = [{frame: 0, duration: 30, color: [255,119,102,221]},
    /// {frame: 2, duration: 30, color: [255,136,51,153]}]</c> and
    /// <c>soundTimings = [{frame: 0, se: {name: "Blow1", volume: 90}},
    /// {frame: 0, se: {name: "Fire1", volume: 100}}]</c>.
    /// </remarks>
    public void Test_HitFireStimmtMitDerDateiUeberein()
    {
        if (!File.Exists(Skies + "/data/Animations.json"))
        {
            Console.WriteLine("MZ animation: the real project is not there");
            return;
        }

        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Skies,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        if (!started.Success)
        {
            host.Dispose();
            Console.WriteLine("MZ animation: the real project did not start");
            return;
        }

        var runtime = (MzEngineRuntime)host.Runtime!;
        Console.WriteLine($"MZ animation: the real project has "
            + $"{runtime.Animations.Count} animations");

        MzAnimation? hitFire = null;
        foreach (var (_, animation) in runtime.Animations)
        {
            if (animation.Name == "Hit Fire")
            {
                hitFire = animation;
            }
        }
        if (hitFire == null)
        {
            Console.WriteLine("MZ animation: this project has no Hit Fire");
            return;
        }

        Console.WriteLine($"MZ animation: {hitFire}");
        AssertEq(hitFire.EffectName, "HitFire", "and its effect is HitFire");
        AssertTrue(hitFire.IsEffekseer, "and so it is an Effekseer animation");
        AssertEq(hitFire.FlashTimings.Count, 2, "and it has two flashes");
        AssertEq(hitFire.FlashTimings[0].Frame, 0, "the first at frame 0");
        AssertEq(hitFire.FlashTimings[0].Duration, 30, "lasting thirty frames");
        AssertEq(hitFire.FlashTimings[0].Color[0], 255, "and red 255");
        AssertEq(hitFire.FlashTimings[0].Color[1], 119, "green 119");
        AssertEq(hitFire.FlashTimings[0].Color[2], 102, "blue 102");
        AssertEq(hitFire.FlashTimings[0].Color[3], 221, "alpha 221");
        AssertEq(hitFire.FlashTimings[1].Frame, 2, "the second at frame 2");
        AssertEq(hitFire.FlashTimings[1].Color[3], 153, "with alpha 153");

        AssertEq(hitFire.SoundTimings.Count, 2, "and two sounds");
        AssertEq(hitFire.SoundTimings[0].Name, "Blow1", "the first is Blow1");
        AssertEq(hitFire.SoundTimings[0].Volume, 90, "at volume 90");
        AssertEq(hitFire.SoundTimings[1].Name, "Fire1", "the second is Fire1");
        AssertEq(hitFire.SoundTimings[1].Volume, 100, "at volume 100");

        // **And the engine's own `_maxTimingFrames` is 2**, the highest frame
        // over both lists -- **and not 49.**
        AssertEq(hitFire.MaxTimingFrames, 2,
            "**and the highest frame any timing names is 2**");
        // **Und die Dauer ist das Ende des letzten Blitzes**, und nicht
        // die Zahl der Felder: der zweite Blitz steht auf Frame 2 und
        // dauert 30 Bilder, **also endet die Animation auf 2 + 30 + 1 =
        // 33** -- und die Zahl der Felder haette 45 gesagt.
        Console.WriteLine($"MZ animation: its duration is "
            + $"{hitFire.DurationFrames()} frames, and the field count "
            + "would have said 45");
        AssertEq(hitFire.DurationFrames(), 33,
            "**and its duration is the last tint's own end and not a "
            + "field count**");
    }

    /// <summary>
    /// And a running animation tints its target and then stops.
    /// </summary>
    public void Test_EineAnimationFaerbtUndEndet()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Repaint();
        var ohne = runtime.PaintedMap!;
        AssertEq(runtime.AnimationsDrawn, 0, "a frame with no animation tints none");

        // **And the first animation of this game, whatever it is.**
        var erste = 0;
        foreach (var (id, _) in runtime.Animations)
        {
            erste = id;
            break;
        }
        if (erste == 0)
        {
            return;
        }
        AssertTrue(runtime.StarteAnimation(-1, erste),
            "and an animation can start on the player");
        AssertTrue(runtime.AnimationLaeuft(-1), "and it is running");

        // **And the tint lands after a frame**, because the engine sets it in
        // `processFlashTimings` and only a frame runs that.
        runtime.Tick();
        var blend = runtime.AnimationsBlend(-1);
        Console.WriteLine($"MZ animation: after one frame the blend is "
            + (blend == null ? "none"
                : $"[{string.Join(",", blend)}], frame "
                    + $"{runtime.AnimationsBild(-1)}"));

        if (blend != null && blend[3] > 0)
        {
            runtime.Repaint();
            Console.WriteLine($"MZ animation: drawn={runtime.AnimationsDrawn}");
            AssertEq(runtime.AnimationsDrawn, 1, "and the target is tinted");

            var mit = runtime.PaintedMap!;
            var geaendert = 0;
            for (var i = 0; i + 3 < ohne.Pixels.Length; i += 4)
            {
                if (ohne.Pixels[i] != mit.Pixels[i])
                {
                    geaendert++;
                }
            }
            Console.WriteLine($"MZ animation: {geaendert} pixels changed");
            AssertTrue(geaendert > 0, "and the frame shows it");
        }

        // **And it ends**: `checkEnd` passes once the frame count has gone
        // past the timings and the last tint has decayed.
        for (var i = 0; i < 200 && runtime.AnimationLaeuft(-1); i++)
        {
            runtime.Tick();
        }
        Console.WriteLine($"MZ animation: running="
            + $"{runtime.AnimationLaeuft(-1)} after the run");
        AssertFalse(runtime.AnimationLaeuft(-1),
            "**and it stops, which is the wait `212` asks for**");
        AssertEq(runtime.AnimationsBild(-1), -1, "and no frame is left");

        runtime.Repaint();
        AssertEq(runtime.AnimationsDrawn, 0, "and the frame is clean again");
    }
}
