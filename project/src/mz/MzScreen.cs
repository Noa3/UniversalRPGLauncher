using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// The pictures a game puts on its screen, and what 231, 232 and 235 do to
/// them.
/// </summary>
/// <remarks>
/// <para>
/// K-126 changed what the party carries. <b>A picture is the next thing this
/// game's map actually asks for</b>: nine of them across the one map in the
/// fixture, on images 1, 86 and 87, and two of them are moved rather than
/// shown. They are the first commands in this game that need something other
/// than numbers to have an effect, and a reader that has no place to put a
/// picture has nothing to say about them.
/// </para>
/// <para>
/// The rules, each read out of the engine:
///
/// <list type="number">
/// <item>
/// <b>A shown picture is a new object, not a change to the old one.</b>
/// <c>showPicture</c> makes <c>new Game_Picture()</c> and puts it in the slot,
/// so a show on an id that already has a picture <b>throws the old one away</b>,
/// tone, rotation and any movement with it. A reader that changed the existing
/// picture in place would keep a tint the engine has just discarded.
/// </item>
/// <item>
/// <b>A picture id is routed through <c>realPictureId</c>, which is not the
/// identity.</b> In a battle a map picture and a battle picture share the same
/// editor number, and <c>realPictureId</c> adds <c>maxPictures()</c> to tell
/// them apart. <b>This game's <c>System.json</c> sets
/// <c>picturesUpperLimit</c> to 110</b> and not to the hundred the engine falls
/// back on, and a reader that used the hundred would put a battle picture on
/// top of a map one that the game keeps at 110.
/// </item>
/// <item>
/// <b>A move sets a target, not a value.</b> <c>move</c> writes
/// <c>_targetX</c> and friends and <c>_duration</c>, and <c>updateMove</c> only
/// moves while <c>_duration &gt; 0</c>. **A move of zero frames therefore
/// changes nothing at all**, which is not what a reader that copied the numbers
/// straight over would do — it would snap the picture to the target.
/// </item>
/// <item>
/// <b>Erasing sets the slot to null</b>, and moving a picture that is not there
/// does nothing, because <c>picture(pictureId)</c> answers nothing and
/// <c>movePicture</c> checks. A move on an id that was never shown is not an
/// error and not a change, and it is recorded as a notice rather than
/// silently ignored.
/// </item>
/// </list>
/// </para>
/// <para>
/// <b>What is deliberately not here.</b> A picture is a name, a place on the
/// screen and some numbers; it is not a texture, and nothing here loads one.
/// The blend mode and the scale are kept as the numbers the game wrote, not
/// resolved to a rendering, because a reader that has no renderer must not
/// pretend to have one. The tone and the rotation of a 233 or a 234 are the
/// commands after these and are not modelled here.
/// </para>
/// </remarks>
public sealed class MzScreen
{
    /// <summary>
    /// One picture as the engine holds it: what it is called, where it is, how
    /// big, how see-through, and where it is on its way to.
    /// </summary>
    /// <remarks>
    /// <b>Every field is a pair</b> where the engine has one — a value and a
    /// target — because a picture is nearly always somewhere between two
    /// places. A reader with a single number for the position would have to
    /// choose which of the two it means, and a game that fades a picture out
    /// over twenty frames would snap it instead.
    /// </remarks>
    public sealed class Picture
    {
        /// <summary>The image the game named, or empty for a slot it cleared.</summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// Where the picture is anchored: 0 the upper left, 1 the centre, 2
        /// the lower right. The engine keeps it as a number and a renderer
        /// works out what to do with it; so does this.
        /// </summary>
        public int Origin { get; internal set; }

        /// <summary>Where it is now.</summary>
        public int X { get; internal set; }

        public int Y { get; internal set; }

        public int ScaleX { get; internal set; }

        public int ScaleY { get; internal set; }

        public int Opacity { get; internal set; }

        /// <summary>Where it is on its way to, and the same value when it is
        /// not moving.</summary>
        public int TargetX { get; internal set; }

        public int TargetY { get; internal set; }

        public int TargetScaleX { get; internal set; }

        public int TargetScaleY { get; internal set; }

        public int TargetOpacity { get; internal set; }

        /// <summary>The blend mode the game wrote, kept as a number.</summary>
        public int BlendMode { get; internal set; }

        /// <summary>Frames of movement left, as the engine's <c>_duration</c>.</summary>
        public int Duration { get; internal set; }

        /// <summary>The length the movement was asked for, which the easing
        /// needs and is not the same as what is left of it.</summary>
        public int WholeDuration { get; internal set; }

        /// <summary>The easing the game chose, as a number.</summary>
        public int EasingType { get; internal set; }

        /// <summary>Whether this picture is on its way somewhere.</summary>
        public bool IsMoving => Duration > 0;


        /// <summary>
        /// The tint the picture is moving toward, and the engine's own
        /// four numbers.
        /// </summary>
        public int[] TargetPictureTone { get; set; } = new int[] { 0, 0, 0, 0 };

        /// <summary>How long that move takes, in frames.</summary>
        public int PictureToneDuration { get; set; }

        /// <summary>
        /// <c>Game_Picture.prototype.tint(tone, duration)</c>, and the
        /// three lines are all of it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Measured at <c>rpg_objects.js</c>:</strong>
        /// </para>
        /// <code>
        /// tint(tone, duration) {
        ///     if (!this._tone) {
        ///         this._tone = [0, 0, 0, 0];
        ///     }
        ///     this._toneTarget = tone.clone();
        ///     this._toneDuration = duration;
        ///     if (this._toneDuration === 0) {
        ///         this._tone = this._toneTarget.clone();
        ///     }
        /// }
        /// </code>
        /// <para>
        /// <strong>And a duration of zero sets the tone at once</strong> --
        /// <strong>and a duration above zero only records the target</strong>
        /// -- <strong>and a picture that was never tinted has
        /// <c>_tone</c> undefined, and <c>if (!this._tone)</c> gives it
        /// four zeros.</strong>
        /// </para>
        /// </remarks>
        /// <param name="pTon">The four numbers, and they are kept as they
        /// are.</param>
        /// <param name="pDauer">The frames, and zero means at once.</param>
        /// <returns>One line, for an action and for a log.</returns>
        public string Ton(int[] pTon, int pDauer)
        {
            TargetPictureTone = Vier(pTon);
            PictureToneDuration = pDauer > 0 ? pDauer : 0;
            if (PictureToneDuration == 0)
            {
                TargetPictureTone = new int[] { pTon[0], pTon[1], pTon[2],
                    pTon[3] };
            }

            return "tint " + TargetPictureTone[0] + ","
                + TargetPictureTone[1] + "," + TargetPictureTone[2]
                + "," + TargetPictureTone[3]
                + (PictureToneDuration > 0
                    ? " over " + PictureToneDuration + " frames"
                    : " at once");
        }
        /// <summary>One line, for an action and for a log.</summary>
        public override string ToString() =>
            $"{Name} at {X},{Y} scale {ScaleX},{ScaleY} opacity {Opacity}"
            + (Duration > 0
                ? $", moving to {this.TargetX},{this.TargetY} in {this.Duration}"
                : "");
    }

    /// <summary>
    /// How many picture ids there are. <c>maxPictures</c> reads
    /// <c>$dataSystem.advanced.picturesUpperLimit</c> and falls back to a
    /// hundred only when the key is missing.
    /// </summary>
    /// <remarks>
    /// <b>This game sets it to 110.</b> A reader that used the hundred would
    /// tell a battle picture apart from a map one by a hundred instead of a
    /// hundred and ten, and would put the second on top of the first where the
    /// game keeps them apart.
    /// </remarks>
    public int MaxPictures { get; init; } = 100;

    /// <summary>Whether the game is in a battle, which is what
    /// <c>realPictureId</c> asks before it decides.</summary>
    public bool InBattle { get; init; }

    /// <summary>
    /// The colour the screen is washed in, and how long it takes to get
    /// there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is <c>223 Screen Tint</c>, and the engine keeps four
    /// numbers and not one</strong>: <c>$gameScreen.startTint(params[0],
    /// params[1])</c> <strong>copies the whole list and gives it a
    /// duration</strong>, <strong>and a reader that kept only the three
    /// channels it expected would drop the fourth the editor wrote
    /// alongside them.</strong>
    /// </para>
    /// <para>
    /// <code>
    /// startTint(tone, duration) {
    ///     this._toneTarget = tone.clone();
    ///     this._toneDuration = duration;
    ///     if (this._toneDuration === 0) {
    ///         this._tone = this._toneTarget.clone();
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And a duration of zero copies the target straight
    /// over</strong>, <strong>which is the one case where the tint is there
    /// at once.</strong>
    /// </para>
    /// </remarks>
    public int[] Tone { get; private set; } = new int[] { 0, 0, 0, 0 };

    /// <summary>Where the tint is going, and the same tone when it is not
    /// moving.</summary>
    public int[] TargetTone { get; private set; } = new int[] { 0, 0, 0, 0 };

    /// <summary>Frames of tint left, as the engine's
    /// <c>_toneDuration</c>.</summary>
    public int ToneDuration { get; private set; }

    /// <summary>Whether a tint is on its way, which is what a wait asks
    /// about.</summary>
    public bool ToneIsMoving => ToneDuration > 0;

    /// <summary>
    /// Begin a tint, the way the engine's <c>startTint</c> does.
    /// </summary>
    /// <remarks>
    /// <strong>And the tone starts where it is and not at
    /// nothing.</strong> <strong>The engine moves the existing colour toward
    /// the new one and keeps the old one until the first frame runs</strong>
    /// -- <strong>and a reader that set the tone to the target here would
    /// skip every frame of the change.</strong>
    /// </remarks>
    public string StarteTon(int[] pTon, int pDauer)
    {
        TargetTone = Vier(pTon);
        ToneDuration = pDauer > 0 ? pDauer : 0;
        if (ToneDuration == 0)
        {
            Tone = new int[] { TargetTone[0], TargetTone[1],
                               TargetTone[2], TargetTone[3] };
        }

        return "tint " + Tone[0] + "," + Tone[1] + "," + Tone[2]
            + "," + Tone[3]
            + (ToneDuration > 0
                ? " toward " + TargetTone[0] + "," + TargetTone[1] + ","
                    + TargetTone[2] + "," + TargetTone[3]
                    + " over " + ToneDuration + " frames"
                : " at once");
    }

    /// <summary>
    /// One frame of the tint, and the engine's <c>updateTone</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is not a fixed step toward the target.</strong> The
    /// engine moves a <b>fraction</b> of the way on every frame, and the
    /// fraction is the rest of the duration, so a tint covers most of its
    /// distance in its first frames and eases into the last:
    /// </para>
    /// <code>
    /// updateTone() {
    ///     if (this._toneDuration &gt; 0) {
    ///         const d = this._toneDuration;
    ///         for (let i = 0; i &lt; 4; i++) {
    ///             this._tone[i] =
    ///                 (this._tone[i] * (d - 1) + this._toneTarget[i]) / d;
    ///         }
    ///         this._toneDuration--;
    ///     }
    /// }
    /// </code>
    /// <para>
    /// <strong>And a reader that stepped by <c>255 / duration</c> had the
    /// wrong shape in two ways at once</strong> -- <strong>a fixed step is
    /// not an easing one</strong>, <strong>and the engine's numbers are the
    /// colour itself and not an opacity over it.</strong>
    /// </para>
    /// </remarks>
    public void TickTon()
    {
        if (ToneDuration <= 0)
        {
            return;
        }

        var d = ToneDuration;
        for (var i = 0; i < 4; i++)
        {
            Tone[i] = (Tone[i] * (d - 1) + TargetTone[i]) / d;
        }

        ToneDuration--;
    }

    /// <summary>
    /// Four numbers out of a parameter that is a list, in the order the
    /// engine reads them.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>updateTone</c> walks <c>i &lt; 4</c> and not
    /// three</strong>, <strong>so the fourth number is read by the engine
    /// and a reader that kept three would be one short.</strong>
    /// </remarks>
    private static int[] Vier(int[] pTon)
    {
        return new int[]
        {
            pTon.Length > 0 ? pTon[0] : 0,
            pTon.Length > 1 ? pTon[1] : 0,
            pTon.Length > 2 ? pTon[2] : 0,
            pTon.Length > 3 ? pTon[3] : 0,
        };
    }


    private readonly Dictionary<int, Picture> _pictures = new();

    /// <summary>Something a command asked for and could not do, in order.</summary>
    /// <summary>
    /// One audio channel, and the state a command left it in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a channel is a state and not a sound.</strong> Nothing
    /// here plays anything: a game of this generation ships its music as
    /// <c>.ogg</c> files and this reader does not open one. What a
    /// command does is name a file and three numbers,
    /// <strong>and a channel that knows what it was told and never
    /// carried a player is a fact about the command and not a claim.</strong>
    /// </para>
    /// <para>
    /// <strong>And the four numbers are the ones MZ writes.</strong>
    /// Measured on a finished project:
    /// <c>241 [{"name":"Scene8","volume":40,"pitch":80,"pan":0}]</c> and
    /// <c>250 [{"name":"Thunder4","volume":60,"pitch":120,"pan":0}]</c>
    /// — **an object in the parameter list, and not the four integers
    /// XP writes.**
    /// </para>
    /// </remarks>
    public sealed class Audio
    {
        /// <summary>The file name, or empty when the channel is
        /// silent.</summary>
        public string Name { get; internal set; } = "";

        /// <summary>Volume in percent, from <c>parameters[0].volume</c>.
        /// </summary>
        public int Volume { get; internal set; }

        /// <summary>Pitch in percent, from <c>parameters[0].pitch</c>.</summary>
        public int Pitch { get; internal set; }

        /// <summary>Pan from -100 to 100, from
        /// <c>parameters[0].pan</c>.</summary>
        public int Pan { get; internal set; }

        /// <summary>Frames the channel has left to fade out over.</summary>
        public int FadeFramesLeft { get; internal set; }

        /// <summary>Whether the channel is silent.</summary>
        public bool IsSilent => Name.Length == 0;

        public override string ToString()
        {
            if (IsSilent)
            {
                return "silence";
            }

            return FadeFramesLeft > 0
                ? $"{Name} volume {Volume} pitch {Pitch} pan {Pan}, "
                    + $"fading out over {FadeFramesLeft} frames"
                : $"{Name} volume {Volume} pitch {Pitch} pan {Pan}";
        }
    }

    /// <summary>The background music, from <c>241</c> and <c>242</c>.
    /// </summary>
    public Audio Bgm { get; } = new();

    /// <summary>The background sound, from <c>245</c> and <c>246</c>.
    /// </summary>
    public Audio Bgs { get; } = new();

    /// <summary>The music that plays alone, from <c>249</c>.
    /// </summary>
    public Audio Me { get; } = new();

    /// <summary>The last sound effect, from <c>250</c> and <c>251</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And one, and not a list.</strong> <c>250</c> plays a
    /// sound and <c>251</c> stops it, and both name no slot —
    /// **so a game that plays two sounds and then stops one has stopped
    /// the one that is here, and that is what the reference does.**
    /// </remarks>
    public Audio Se { get; } = new();

    /// <summary>
    /// Names a file on a channel, from <c>241</c>, <c>245</c> and
    /// <c>249</c>.
    /// </summary>
    /// <param name="pChannel">The channel.</param>
    /// <param name="pName">The file name, without its extension.</param>
    /// <param name="pVolume">Volume in percent.</param>
    /// <param name="pPitch">Pitch in percent.</param>
    /// <param name="pPan">Pan from -100 to 100.</param>
    /// <returns>What the channel now is.</returns>
    /// <remarks>
    /// <strong>And a play cancels a fade, and not the other way round.</strong>
    /// The reference's <c>playBgm</c> stops whatever was playing and
    /// starts the new file at the fade-in the command carries,
    /// **and a command that arrives mid-fade replaces it rather than
    /// queueing behind it** — a second <c>241</c> in a row is the second
    /// track, not the first one twice.
    /// </remarks>
    public string Play(Audio pChannel, string pName, int pVolume,
        int pPitch, int pPan)
    {
        ArgumentNullException.ThrowIfNull(pChannel);
        pChannel.Name = pName;
        pChannel.Volume = Math.Clamp(pVolume, 0, MaxAudioVolumePercent);
        pChannel.Pitch = Math.Clamp(pPitch, 0, MaxAudioPitchPercent);
        pChannel.Pan = Math.Clamp(pPan, -MaxAudioPan, MaxAudioPan);
        pChannel.FadeFramesLeft = 0;
        return pChannel.ToString();
    }

    /// <summary>
    /// Fades a channel out over a number of frames, from <c>242</c> and
    /// <c>246</c>.
    /// </summary>
    /// <param name="pChannel">The channel.</param>
    /// <param name="pFrames">How long the fade takes.</param>
    /// <returns>What the channel now is.</returns>
    /// <remarks>
    /// <strong>And a fade of zero is a stop, and not a fade.</strong> The
    /// reference's <c>fadeOutBgm</c> takes the frames and asks the
    /// player for them; **a game that wrote zero because the field was
    /// empty means "now"**, **and a reader that refused it would leave
    /// the music playing for ever.**
    /// </remarks>
    public string FadeOut(Audio pChannel, int pFrames)
    {
        ArgumentNullException.ThrowIfNull(pChannel);
        var frames = Math.Clamp(pFrames, 0, MaxAudioFadeFrames);
        if (pChannel.IsSilent)
        {
            return "nothing is playing, so there is nothing to fade out";
        }

        if (frames == 0)
        {
            var name = pChannel.Name;
            pChannel.Name = "";
            pChannel.FadeFramesLeft = 0;
            return $"{name} stopped";
        }

        pChannel.FadeFramesLeft = frames;
        return pChannel.ToString();
    }

    /// <summary>The highest volume and pitch in percent, from the editor's
    /// own range.</summary>
    public const int MaxAudioVolumePercent = 100;

    /// <summary>The highest pitch in percent, from the editor's own
    /// range.</summary>
    public const int MaxAudioPitchPercent = 200;

    /// <summary>The pan's reach on each side, from the editor's own
    /// range.</summary>
    public const int MaxAudioPan = 100;

    /// <summary>
    /// The longest fade in frames, ten seconds at sixty frames a second.
    /// </summary>
    /// <remarks>
    /// <strong>A bound and not a rule.</strong> A game that wrote a fade of
    /// a minute would get a minute here, **and the number this reader can
    /// hold is the number it says it can hold** — a fade of four million
    /// frames is a file that says something else.
    /// </remarks>
    public const int MaxAudioFadeFrames = 3600;

    /// <summary>
    /// How long a balloon icon stays when the command did not say.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a number this repository chose, and the code
    /// says so twice.</strong> The official help for
    /// <c>213 Show Balloon Icon</c> names three settings — the
    /// character, the icon, and whether to wait — <strong>and no fourth
    /// one, and no duration anywhere in the command.</strong>
    /// </para>
    /// <para>
    /// <strong>And without a duration, "wait for the icon to disappear"
    /// is a wait that never ends</strong> — the event would sit on its
    /// branch for ever, and the player would see a game that stopped.
    /// <strong>A second is the shortest span a player perceives as
    /// "it was there", and a minute is longer than any balloon in any
    /// game of that time.</strong>
    /// </para>
    /// <para>
    /// <strong>And a game whose balloon is meant to stay until something
    /// else erases it will lose that here</strong>, **and the honest
    /// thing is to say the number is a choice rather than to hide it in
    /// a constant whose name does not admit it.**
    /// </para>
    /// </remarks>
    public const int MaxBalloonFrames = 60;

    /// <summary>
    /// How long an animation stays when the command did not say.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the same choice as the balloon's, and the
    /// code admits it the same way.</strong> The official help for
    /// <c>221 Show Animation</c> names the character, the animation and
    /// whether to wait, <strong>and no duration, and no duration
    /// anywhere in the command.</strong>
    /// </para>
    /// <para>
    /// <strong>And an animation is not a balloon.</strong> A balloon is
    /// an icon over a head; <strong>an animation is a short picture that
    /// plays once</strong>, **and one second is long enough to read one
    /// and short enough that a game's map does not sit still for it.**
    /// </para>
    /// </para>
    /// </remarks>
    public const int MaxAnimationFrames = 60;


    /// <summary>
    /// Advances a fade by a frame, and silences the channel when it ends.
    /// </summary>
    /// <remarks>
    /// <strong>On the same tick as the picture moves, and for the same
    /// reason.</strong> A fade that ran on a different clock would end at
    /// a different moment than a picture that was told to finish with it.
    /// </remarks>
    public void TickAudio()
    {
        foreach (var channel in new[] { Bgm, Bgs, Me, Se })
        {
            if (channel.FadeFramesLeft <= 0)
            {
                continue;
            }

            channel.FadeFramesLeft--;
            if (channel.FadeFramesLeft <= 0)
            {
                channel.FadeFramesLeft = 0;
                channel.Name = "";
            }
        }
    }


    private readonly List<string> _notices = new();

    public MzScreen(int pMaxPictures = 100, bool pInBattle = false)
    {
        MaxPictures = pMaxPictures <= 0 ? 100 : pMaxPictures;
        InBattle = pInBattle;
    }

    public IReadOnlyList<string> Notices => _notices;

    /// <summary>How far the screen is currently shaken, in pixels.</summary>
    /// <remarks>
    /// <strong>And the engine's own arithmetic, measured at
    /// <c>Game_Screen.prototype.updateShake</c>:</strong>
    /// <c>const delta = (this._shakePower * this._shakeSpeed *
    /// this._shakeDirection) / 10;</c> — <strong>and it reverses at
    /// <c>this._shake &gt; this._shakePower * 2</c> and at the negative
    /// of it</strong>, <strong>and it stops to zero when
    /// <c>this._shake * (this._shake + delta) &lt; 0</c> with a duration
    /// of one or less.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>startShake(power, speed, duration)</c> stores the
    /// three numbers and nothing else</strong>, <strong>which is why a
    /// screen that was shaken keeps moving for its duration and not
    /// longer.</strong>
    /// </para>
    /// </remarks>
    public int ShakeOffset { get; private set; }

    /// <summary>Frames the shake still has to run.</summary>
    /// <remarks>
    /// <strong>And it counts down every frame, measured at
    /// <c>updateShake</c>:</strong> <c>if (this._shakeDuration &gt; 0)
    /// this._shakeDuration--;</c>
    /// </remarks>
    public int ShakeFramesLeft { get; private set; }

    private int _shakePower;

    private int _shakeSpeed = 1;

    private int _shakeDirection = 1;

    /// <summary>Starts the screen shaking.</summary>
    /// <param name="pPower">How far it may go either way.</param>
    /// <param name="pSpeed">How quickly it moves.</param>
    /// <param name="pDuration">How many frames it lasts.</param>
    /// <remarks>
    /// <strong>And this is the engine's own signature, measured at
    /// <c>startShake</c>:</strong> <c>this._shakePower = power; this
    /// ._shakeSpeed = speed; this._shakeDuration = duration;</c> — <strong>and
    /// it does not touch the offset, so a shake added while one is
    /// running continues from where it was.</strong>
    /// </remarks>
    public void StarteWackeln(int pPower, int pSpeed, int pDuration)
    {
        _shakePower = Math.Max(0, pPower);
        _shakeSpeed = Math.Max(1, pSpeed);
        ShakeFramesLeft = Math.Max(0, pDuration);
    }

    /// <summary>Moves the shake one frame and counts it down.</summary>
    /// <remarks>
    /// <strong>And this is <c>updateShake</c> in full, measured:</strong>
    /// the delta is <c>(power * speed * direction) / 10</c>, the offset
    /// stops at zero when <c>offset * (offset + delta) &lt; 0</c> and the
    /// duration is one or less, and the direction flips at
    /// <c>±power * 2</c>.
    /// </remarks>
    /// <summary>
    /// The colour the screen is flashed in, and how long that takes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>224 Screen Flash</c> is <c>223</c> with a colour and
    /// not a tint</strong>, <strong>and the engine keeps it in
    /// <c>_flashTarget</c> and moves <c>_flash</c> toward it exactly the
    /// way it moves a tone</strong> -- <strong>same four channels, same
    /// fraction of the way, same count of frames.</strong>
    /// </para>
    /// <para>
    /// <strong>And measured on this game: <c>[[255, 255, 255, 119], 60,
    /// false]</c></strong> -- <strong>a white flash of a hundred and
    /// nineteen over sixty frames</strong>, <strong>and the fourth number is
    /// the strength and not an alpha.</strong>
    /// </para>
    /// </remarks>
    public int[] Flash { get; private set; } = new int[] { 0, 0, 0, 0 };

    /// <summary>Where the flash is going, and the same colour when it is
    /// not moving.</summary>
    public int[] TargetFlash { get; private set; } = new int[] { 0, 0, 0, 0 };

    /// <summary>Frames of flash left, as the engine's
    /// <c>_flashDuration</c>.</summary>
    public int FlashDuration { get; private set; }

    /// <summary>Whether a flash is on its way, which is what a wait asks
    /// about.</summary>
    public bool FlashIsMoving => FlashDuration > 0;

    /// <summary>
    /// Begin a flash, the way the engine's <c>startFlash</c> does.
    /// </summary>
    public string StarteBlitz(int[] pFarbe, int pDauer)
    {
        TargetFlash = Vier(pFarbe);
        FlashDuration = pDauer > 0 ? pDauer : 0;
        if (FlashDuration == 0)
        {
            Flash = new int[] { TargetFlash[0], TargetFlash[1],
                                TargetFlash[2], TargetFlash[3] };
        }

        return "flash " + Flash[0] + "," + Flash[1] + "," + Flash[2]
            + "," + Flash[3]
            + (FlashDuration > 0
                ? " toward " + TargetFlash[0] + "," + TargetFlash[1] + ","
                    + TargetFlash[2] + "," + TargetFlash[3]
                    + " over " + FlashDuration + " frames"
                : " at once");
    }

    /// <summary>
    /// One frame of the flash, and the engine's <c>updateFlash</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And it is the same arithmetic as <see cref="TickTon"/>,
    /// written twice in the engine</strong> -- <strong>once for the tone and
    /// once for the flash</strong> -- <strong>and a reader that shared one
    /// would have been right about both.</strong>
    /// </remarks>
    public void TickBlitz()
    {
        if (FlashDuration <= 0)
        {
            return;
        }

        var d = FlashDuration;
        for (var i = 0; i < 4; i++)
        {
            Flash[i] = (Flash[i] * (d - 1) + TargetFlash[i]) / d;
        }

        FlashDuration--;
    }


    public void TickWackeln()
    {
        if (ShakeFramesLeft <= 0 && ShakeOffset == 0)
        {
            return;
        }

        var delta = _shakePower * _shakeSpeed * _shakeDirection / 10;
        if (ShakeFramesLeft <= 1 && ShakeOffset * (ShakeOffset + delta) < 0)
        {
            ShakeOffset = 0;
        }
        else
        {
            ShakeOffset += delta;
        }

        if (ShakeOffset > _shakePower * 2)
        {
            _shakeDirection = -1;
        }

        if (ShakeOffset < -_shakePower * 2)
        {
            _shakeDirection = 1;
        }

        if (ShakeFramesLeft > 0)
        {
            ShakeFramesLeft--;
        }
    }

    /// <summary>
    /// The slot a picture id really lives in, as <c>realPictureId</c> answers
    /// it. **A battle picture sits a hundred and ten above the map one**, and
    /// a map picture is where the number says.
    /// </summary>
    public int RealPictureId(int pPictureId) =>
        InBattle ? pPictureId + MaxPictures : pPictureId;

    /// <summary>The pictures that are on the screen, by their real id.</summary>
    public IEnumerable<KeyValuePair<int, Picture>> Pictures =>
        new List<KeyValuePair<int, Picture>>(_pictures);

    /// <summary>Whether a picture is on the screen, as <c>picture(id)</c>
    /// answers it.</summary>
    public bool Has(int pPictureId) => _pictures.ContainsKey(RealPictureId(pPictureId));

    /// <summary>The picture at a real slot, or null.</summary>
    public Picture At(int pRealId) =>
        _pictures.TryGetValue(pRealId, out var picture) ? picture : null;

    /// <summary>What 231 does.</summary>
    public string Show(
        int pPictureId, string pName, int pOrigin, int pX, int pY,
        int pScaleX, int pScaleY, int pOpacity, int pBlendMode)
    {
        // `showPicture` makes a new Game_Picture and puts it in the slot, so
        // **anything the old one had is gone with it** — its tone, its
        // rotation and any movement. `initTarget` then copies the values it
        // was given into the targets, which is why a shown picture is not
        // moving: `_duration` is 0 and `updateMove` only moves above 0.
        var real = RealPictureId(pPictureId);
        var replaced = _pictures.ContainsKey(real);
        var picture = new Picture
        {
            Name = pName,
            Origin = pOrigin,
            X = pX,
            Y = pY,
            ScaleX = pScaleX,
            ScaleY = pScaleY,
            Opacity = pOpacity,
            TargetX = pX,
            TargetY = pY,
            TargetScaleX = pScaleX,
            TargetScaleY = pScaleY,
            TargetOpacity = pOpacity,
            BlendMode = pBlendMode,
        };
        _pictures[real] = picture;

        return replaced
            ? $"picture {pPictureId} shown as {pName}, replacing the one that"
                + $" was there at {pX},{pY}"
            : $"picture {pPictureId} shown as {pName} at {pX},{pY}";
    }

    /// <summary>What 232 does, and the frames it asks a caller to wait for.</summary>
    /// <param name="pPictureId">The editor's number, not the real slot.</param>
    /// <param name="pWait">Whether the game asked to wait, which is
    /// <c>params[11]</c>. The frames are the movement's own duration, so a
    /// move that is set to wait is waiting for exactly as long as it takes.</param>
    public string Move(
        int pPictureId, int pOrigin, int pX, int pY, int pScaleX,
        int pScaleY, int pOpacity, int pBlendMode, int pDuration,
        int pEasingType, bool pWait, out int pWaitFrames)
    {
        pWaitFrames = 0;
        var picture = At(RealPictureId(pPictureId));

        if (picture == null)
        {
            // `movePicture` does `const picture = this.picture(pictureId);
            // if (picture) { ... }`, so a move on a slot with nothing in it
            // does nothing at all. **This reader says so**, because a game that
            // moved a picture it had not shown has a reason a log should hold.
            var note =
                $"picture {pPictureId} is moved but there is nothing at that"
                + " slot, so nothing changed";
            _notices.Add(note);
            return note;
        }

        // `move` writes the targets and the duration, and **it does not touch
        // the current values**. `updateMove` only moves while the duration is
        // above zero, so a move of zero frames leaves the picture where it is
        // and asks for nothing to be waited for — even when the game asked to
        // wait, because the engine's `this.wait(params[10])` is then a wait of
        // no frames, which is over at once.
        picture.Origin = pOrigin;
        picture.TargetX = pX;
        picture.TargetY = pY;
        picture.TargetScaleX = pScaleX;
        picture.TargetScaleY = pScaleY;
        picture.TargetOpacity = pOpacity;
        picture.BlendMode = pBlendMode;
        picture.Duration = pDuration;
        picture.WholeDuration = pDuration;
        picture.EasingType = pEasingType;

        if (pWait)
        {
            pWaitFrames = pDuration;
        }

        if (pDuration <= 0)
        {
            return $"picture {pPictureId} moved to {pX},{pY} in no frames, so"
                + $" it is still at {picture.X},{picture.Y}";
        }
        return $"picture {pPictureId} moving to {pX},{pY} over {pDuration}"
            + $" frames, opacity to {pOpacity}"
            + (pWait ? ", and waiting for them" : "");
    }

    /// <summary>What 235 does.</summary>
    public string Erase(int pPictureId)
    {
        // `erasePicture` sets the slot to null, which is not the same as
        // removing it: the slot is still there and still answers null. The
        // difference only shows in a count, and this reader keeps the slots
        // rather than the entries so a later show lands in the same place.
        var real = RealPictureId(pPictureId);
        var was = _pictures.Remove(real);
        return was
            ? $"picture {pPictureId} erased"
            : $"picture {pPictureId} erased, and there was nothing at that slot";
    }

    /// <summary>
    /// One frame of every picture that is moving, as the engine's
    /// <c>update</c> does it. **Without the easing**, which is a shape the
    /// engine applies and not a straight line — so a picture arrives at the
    /// right place on the right frame, and a reader that walked the straight
    /// line would be visibly wrong in between.
    /// </summary>
    public void PassFrame()
    {
        // **Und der Ton laeuft auf demselben Bild wie die Bilder**, **denn
        // `Game_Screen.update` ruft `updateTone` zwischen `updatePicture`
        // und `updateWeather`, und ein Lauf, der den Ton nicht mitnimmt,
        // laesst eine Einfaerbung fuer unendlich stehen.**
        TickTon();

        // **Und der Blitz laeuft auf demselben Bild wie der Ton**,
        // **denn `Game_Screen.update` ruft `updateFlash` zwischen
        // `updateTone` und `updateWeather`.**
        TickBlitz();

        // **Und die Audio-Fades laufen auf demselben Bild wie die
        // Bilder** -- **denn ein Befehl, der ein Bild dreht und
        // gleichzeitig die Musik ausblaendet, hat beide mit derselben
        // Zahl gestartet, und ein Leser, der nur eines davon
        // fortschreibt, laesst das andere laenger laufen.**
        TickAudio();
        foreach (var picture in _pictures.Values)
        {
            if (picture.Duration <= 0)
            {
                continue;
            }
            // `updateMove` moves every value by easing and then takes one off
            // the duration. **The last frame lands exactly on the target**,
            // because the engine's easing is built to do that, and this reader
            // puts it there rather than easing towards it.
            if (picture.Duration == 1)
            {
                picture.X = picture.TargetX;
                picture.Y = picture.TargetY;
                picture.ScaleX = picture.TargetScaleX;
                picture.ScaleY = picture.TargetScaleY;
                picture.Opacity = picture.TargetOpacity;
            }
            picture.Duration--;
        }
    }
}
