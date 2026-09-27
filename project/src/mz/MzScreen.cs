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

    private readonly Dictionary<int, Picture> _pictures = new();

    /// <summary>Something a command asked for and could not do, in order.</summary>
    private readonly List<string> _notices = new();

    public MzScreen(int pMaxPictures = 100, bool pInBattle = false)
    {
        MaxPictures = pMaxPictures <= 0 ? 100 : pMaxPictures;
        InBattle = pInBattle;
    }

    public IReadOnlyList<string> Notices => _notices;

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
