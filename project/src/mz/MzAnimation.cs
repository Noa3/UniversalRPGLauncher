using System;
using System.Collections.Generic;
using UniversalRPG.Web;

namespace UniversalRPG.Mz;

/// <summary>
/// And one animation, as <c>Animations.json</c> describes it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And an MZ animation is not a sheet of pictures.</strong> Measured
/// on the real project: of 160 animations, <strong>158 carry an
/// <c>effectName</c> and none carries an <c>animation1Name</c></strong> --
/// so they are Effekseer effects (<c>effects/*.efkefc</c>) and not the
/// frame sheets of MV.
/// </para>
/// <para>
/// <strong>And what is left when the particles are taken away is real and
/// measurable</strong>, measured in the project's own <c>rmmz_sprites.js</c>
/// at <c>Sprite_Animation</c>:
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
/// Sprite_Animation.prototype.processFlashTimings = function() {
///     for (const timing of this._animation.flashTimings) {
///         if (timing.frame === this._frameIndex) {
///             this._flashColor = timing.color.clone();
///             this._flashDuration = timing.duration;
///         }
///     }
/// };
/// Sprite_Animation.prototype.updateFlash = function() {
///     if (this._flashDuration &gt; 0) {
///         const d = this._flashDuration--;
///         this._flashColor[3] *= (d - 1) / d;
///         for (const target of this._targets) {
///             target.setBlendColor(this._flashColor);
///         }
///     }
/// };
/// Sprite_Animation.prototype.checkEnd = function() {
///     if (this._frameIndex &gt; this._maxTimingFrames &amp;&amp;
///         this._flashDuration === 0 &amp;&amp;
///         !(this._handle &amp;&amp; this._handle.exists)) {
///         this._playing = false;
///     }
/// };
/// </code>
/// <para>
/// <strong>so an animation is a count of frames, a list of frames that play
/// a sound, and a list of frames that tint its target.</strong> -- <strong>and
/// the tint decays by the same <c>(d - 1) / d</c> the screen's flash uses,
/// and lands through the same <c>ColorFilter</c> the screen's tone
/// uses.</strong>
/// </para>
/// <para>
/// <strong>And the third term of <c>checkEnd</c> is the one this reader
/// cannot honour</strong>: <c>this._handle.exists</c> asks an Effekseer
/// particle system whether it is still alive. Without it the bound is
/// <c>_maxTimingFrames</c> and the last flash, <strong>and an effect whose
/// particles outlive its timings would be cut short here</strong> -- which is
/// written down rather than hidden.
/// </para>
/// </remarks>
public sealed class MzAnimation
{
    /// <summary>Which animation, from the game's own list.</summary>
    public int Id { get; init; }

    /// <summary>Its name, for a report.</summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// And the Effekseer effect it plays, or an empty string.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the part this runtime does not run.</strong> An
    /// Effekseer effect is a compiled particle system
    /// (<c>effects/*.efkefc</c>) with its own binary format, and running one
    /// is a renderer of its own. <strong>Measured: 158 of this project's 160
    /// animations name one.</strong>
    /// </remarks>
    public string EffectName { get; init; } = "";

    /// <summary>And whether this animation is an Effekseer effect.</summary>
    public bool IsEffekseer => EffectName.Length > 0;

    /// <summary>
    /// And where it is shown: on the target, on the screen, or both.
    /// </summary>
    public int DisplayType { get; init; }

    /// <summary>And the highest frame any of its timings names.</summary>
    /// <remarks>
    /// <strong>And this is the engine's own <c>_maxTimingFrames</c></strong>,
    /// taken over <c>soundTimings</c> and <c>flashTimings</c> together.
    /// </remarks>
    public int MaxTimingFrames { get; init; }

    /// <summary>And one frame that tints the animation's target.</summary>
    /// <param name="Frame">When, counted from the animation's first frame.</param>
    /// <param name="Color">Red, green, blue and alpha.</param>
    /// <param name="Duration">How long the tint decays.</param>
    public readonly record struct FlashTiming(int Frame, int[] Color, int Duration);

    /// <summary>And one frame that plays a sound.</summary>
    /// <param name="Frame">When, counted from the animation's first frame.</param>
    /// <param name="Name">The file's name in <c>audio/se</c>.</param>
    /// <param name="Volume">How loud.</param>
    /// <param name="Pitch">How fast.</param>
    /// <param name="Pan">Which side.</param>
    public readonly record struct SoundTiming(
        int Frame, string Name, int Volume, int Pitch, int Pan);

    /// <summary>And the frames that tint the target.</summary>
    public IReadOnlyList<FlashTiming> FlashTimings { get; init; } =
        Array.Empty<FlashTiming>();

    /// <summary>And the frames that play a sound.</summary>
    public IReadOnlyList<SoundTiming> SoundTimings { get; init; } =
        Array.Empty<SoundTiming>();

    /// <summary>
    /// And it builds one out of the game's own entry.
    /// </summary>
    /// <param name="pEintrag">The entry, from <c>Animations.json</c>.</param>
    /// <returns>The animation, or null when the entry is not one.</returns>
    public static MzAnimation? Read(MzValue? pEintrag)
    {
        if (pEintrag == null || pEintrag.Kind != MzKind.Object)
        {
            return null;
        }

        var blitze = new List<FlashTiming>();
        if (pEintrag.Member("flashTimings") is { Kind: MzKind.Array } blitzListe)
        {
            foreach (var eintrag in blitzListe.Items)
            {
                if (eintrag.Kind != MzKind.Object)
                {
                    continue;
                }
                var farbe = new[] { 0, 0, 0, 0 };
                if (eintrag.Member("color") is { Kind: MzKind.Array } farbListe)
                {
                    for (var i = 0; i < 4 && i < farbListe.Items.Count; i++)
                    {
                        farbe[i] = farbListe.Items[i].IntOr(0);
                    }
                }
                blitze.Add(new FlashTiming(
                    eintrag.Member("frame")?.IntOr(0) ?? 0,
                    farbe,
                    eintrag.Member("duration")?.IntOr(0) ?? 0));
            }
        }

        var klaenge = new List<SoundTiming>();
        if (pEintrag.Member("soundTimings") is { Kind: MzKind.Array } klangListe)
        {
            foreach (var eintrag in klangListe.Items)
            {
                if (eintrag.Kind != MzKind.Object)
                {
                    continue;
                }
                var se = eintrag.Member("se");
                klaenge.Add(new SoundTiming(
                    eintrag.Member("frame")?.IntOr(0) ?? 0,
                    se?.Member("name")?.StringOr("") ?? "",
                    se?.Member("volume")?.IntOr(90) ?? 90,
                    se?.Member("pitch")?.IntOr(100) ?? 100,
                    se?.Member("pan")?.IntOr(0) ?? 0));
            }
        }

        // **And `_maxTimingFrames` is the highest frame over both lists**,
        // which is what the engine's own `setup` computes.
        var hoechstes = 0;
        foreach (var blitz in blitze)
        {
            hoechstes = Math.Max(hoechstes, blitz.Frame);
        }
        foreach (var klang in klaenge)
        {
            hoechstes = Math.Max(hoechstes, klang.Frame);
        }

        return new MzAnimation
        {
            Id = pEintrag.Member("id")?.IntOr(0) ?? 0,
            Name = pEintrag.Member("name")?.StringOr("") ?? "",
            EffectName = pEintrag.Member("effectName")?.StringOr("") ?? "",
            DisplayType = pEintrag.Member("displayType")?.IntOr(0) ?? 0,
            MaxTimingFrames = hoechstes,
            FlashTimings = blitze,
            SoundTimings = klaenge,
        };
    }

    /// <summary>
    /// And how long it runs when the particles are not there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is not the engine's <c>checkEnd</c>, and the
    /// difference is written down.</strong> The engine asks three things:
    /// the frame count has passed <c>_maxTimingFrames</c>, the last tint has
    /// decayed, <strong>and an Effekseer handle no longer exists.</strong>
    /// </para>
    /// <para>
    /// <strong>And this reader has no handle</strong>, so the bound is the
    /// first two -- <c>_maxTimingFrames + 1</c>, and then however long the
    /// longest tint takes to decay. <strong>Measured at this project's
    /// <c>Hit Fire</c>:</strong> its timings reach frame 2, its first tint
    /// lasts 30 frames, <strong>so the engine would keep it alive for at
    /// least 32 frames even with no particles at all</strong> -- and the
    /// number this reader used before was the count of the entry's own JSON
    /// properties, <c>12 * 4 + 1 = 49</c>, which is a fact about the file and
    /// not about the animation.
    /// </para>
    /// </remarks>
    public int DurationFrames()
    {
        var letzterBlitz = 0;
        foreach (var blitz in FlashTimings)
        {
            letzterBlitz = Math.Max(letzterBlitz, blitz.Frame + blitz.Duration);
        }
        return Math.Max(MaxTimingFrames + 1, letzterBlitz + 1);
    }

    /// <summary>And a line for a report.</summary>
    public override string ToString() =>
        $"animation {Id} {Name} (effect={EffectName}, "
        + $"{FlashTimings.Count} flashes, {SoundTimings.Count} sounds, "
        + $"{DurationFrames()} frames)";
}
