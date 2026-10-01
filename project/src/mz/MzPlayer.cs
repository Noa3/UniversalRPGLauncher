using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Web;

/// <summary>
/// Where the player is, and whether a transfer is still on its way.
/// </summary>
/// <remarks>
/// <para>
/// K-125 made a run wait for a number of frames, and K-127 made it wait for a
/// picture's movement. <b>A transfer is neither</b>: the engine's
/// <c>command201</c> ends in <c>this.setWaitMode("transfer")</c> and
/// <c>updateWaitMode</c> answers <c>waiting = $gamePlayer.isTransferring()
/// </c>. So the event waits for a <b>condition</b> — until the map has actually
/// been changed — and not for a count a caller could pass in.
/// </para>
/// <para>
/// **And that is why a transfer cannot be applied the moment it is read.**
/// <c>reserveTransfer</c> only records where the player is going:
/// <c>this._transferring = true; this._newMapId = mapId; …</c> — nothing has
/// moved yet. The map changes in <c>performTransfer</c>, which the scene calls
/// when it is ready, and only then does <c>isTransferring</c> answer false. A
/// reader that applied a transfer while reading the command would have moved
/// the player before the event after it ran, which is the opposite of what the
/// engine does and is the difference between a game that leads the player and
/// one that does not.
/// </para>
/// <para>
/// <b>Two ways a transfer is refused</b>, and both are the engine's own:
///
/// <list type="bullet">
/// <item><c>if ($gameParty.inBattle() || $gameMessage.isBusy()) return false;
/// </c> — **in a battle or with a message on screen nothing is transferred at
/// all**, and the command returns false so the interpreter stops where it is.
/// A reader that transferred anyway would move a player out of a fight.</item>
/// <item>A map the game does not have is not a place to go. <c>command201</c>
/// does not check; <c>$gameMap.setup</c> fails further on. This reader names
/// the map instead, and a transfer to a map this repository has no file for is
/// reported rather than half-applied.</item>
/// </list>
///
/// <para>
/// <b>What this is not.</b> A position, not a picture of a position. Nothing
/// here loads a map, and the direction and the fade type are kept as the
/// numbers the game wrote — a reader with no renderer must not pretend to have
/// one.
/// </para>
/// </remarks>
public sealed class MzPlayer
{
    /// <summary>
    /// A transfer that has been asked for and has not happened yet, as
    /// <c>reserveTransfer</c> leaves it: where to, which way to face, and how
    /// the screen changes on the way.
    /// </summary>
    public readonly record struct Reservation(
        int MapId, int X, int Y, int Direction, int FadeType);

    /// <summary>Where the player is, and has been since the last transfer.</summary>
    public int MapId { get; private set; } = 1;

    public int X { get; private set; }

    public int Y { get; private set; }

    /// <summary>
    /// Which way the player faces, as <c>_newDirection</c> holds it.
    /// <b>Set only when the transfer happens</b>, because
    /// <c>performTransfer</c> is what calls <c>setDirection</c> — a reader that
    /// turned the player on the reservation would turn them a frame early.
    /// </summary>
    public int Direction { get; private set; }

    /// <summary>
    /// The balloon icon over the player, and how long it has left.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the player carries one, and not the map.</strong>
    /// Measured on a finished project: <c>213 [-1, 2, false]</c> fifteen
    /// times, **and minus one is the player** — the official help says
    /// *The display location will be based on the position of the
    /// player or event*, **and the first parameter chooses which of the
    /// two.**
    /// </para>
    /// <para>
    /// <strong>And this is the same state an event carries, and not a
    /// copy of it.</strong> A player and a figure are different types
    /// here for their own reasons, **and a balloon is the same three
    /// numbers on both**, **so the numbers are written out twice and the
    /// behaviour is not** — a reader that gave the player no balloon at
    /// all lost fifteen of thirty-six commands in the game in front of
    /// us.
    /// </para>
    /// </remarks>
    public int BalloonIcon { get; private set; } = MzCharacter.NoBalloon;

    /// <summary>
    /// The animation over the player, and how long it has left.
    /// </summary>
    /// <remarks>
    /// <strong>And the player's animation is its own field, and the same
    /// numbers as the figure's.</strong> The help says the display
    /// location is *based on the position of the player or event*,
    /// **and that is the choice the first parameter makes** — **and
    /// measured on a finished project, <c>221</c> comes with the same
    /// minus one that <c>213</c> does.**
    /// </remarks>
    /// <summary>
    /// Whether the running event is the player, and has erased it.
    /// </summary>
    /// <remarks>
    /// <strong>And the player is a character for this too.</strong>
    /// *Temporarily removes the event currently being run* — **and a
    /// map event whose page runs under the player is the player's own
    /// case**, **and a reader that only had a flag on map events had
    /// nowhere to put it for a command running as the player.**
    /// </remarks>
    public bool Erased { get; private set; }

    /// <summary>
    /// The image each vehicle shows, and an empty one where the game
    /// asked for <c>[(None)]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And there are three vehicles, and the engine numbers them
    /// 0, 1 and 2.</strong> The help for <c>322 Change Vehicle Image</c>
    /// says only *Specify the target vehicle*, <strong>and the editor's
    /// list is Boat, Ship, Airship in that order</strong>, <strong>and
    /// measured on a finished project the first value is 1 six times over,
    /// which is the ship.</strong>
    /// </para>
    /// <para>
    /// <strong>And this file has no field for "which vehicle the player
    /// rides", because no command in this reader changes that.</strong>
    /// The images are here without it, <strong>and a reader that added a
    /// riding flag to hold an image would have had a second answer to a
    /// question nothing asks.</strong>
    /// </para>
    /// </remarks>
    public Dictionary<int, string> VehicleImages { get; } = new();

    /// <summary>
    /// The player as a figure a route acts on, which is what it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the player is not a thing that has a figure.</strong>
    /// Measured: <c>Game_Player.prototype.initialize</c> is
    /// <c>Game_Character.prototype.initialize.call(this)</c>, and
    /// <c>initMembers</c> is <c>Game_Character</c>’s with a few own
    /// members added — <strong>so the player inherits
    /// <c>_x</c>, <c>_y</c>, <c>_direction</c> and <c>_pattern</c> and
    /// keeps them itself.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that gave the player a second figure, and
    /// then copied between the two, wrote a copy loop the engine does
    /// not have.</strong> <strong>The engine has one object with one
    /// position, and this has the same shape</strong> — <strong>which
    /// is why the player and an event can share one type and why a
    /// <c>205</c> that names minus one does not need to know whether
    /// it moved the player or an event.</strong>
    /// </para>
    /// </remarks>
    public MzCharacter? Figur { get; private set; }

    /// <summary>
    /// Builds the player’s figure from where the player stands.
    /// </summary>
    /// <returns>The figure, or nothing when there is no player.</returns>
    public MzCharacter? BuildFigure()
    {
        if (MapId <= 0)
        {
            return null;
        }

        Figur = new MzCharacter(X, Y);
        Figur.TurnTo(Direction);
        return Figur;
    }

    /// <summary>
    /// Takes the figure’s place and facing, so the player is where its
    /// figure walked to.
    /// </summary>
    /// <remarks>
    /// <strong>And this is called after every route step.</strong>
    /// <strong>A route acts on the figure</strong>, <strong>and the
    /// player is what the rest of the game asks about</strong>,
    /// <strong>and without this the two would drift apart</strong>
    /// <strong>and a map would show a figure on a tile where the player
    /// is not.</strong>
    /// </remarks>
    public void SyncFigure()
    {
        if (Figur == null)
        {
            return;
        }

        if (Figur.X != X || Figur.Y != Y)
        {
            StandAt(MapId, Figur.X, Figur.Y);
        }

        if (Figur.Direction != Direction)
        {
            StandAt(MapId, X, Y, Figur.Direction);
        }
    }


    /// <summary>The boat, the engine's own number.</summary>
    public const int Boat = 0;

    /// <summary>The ship, the engine's own number.</summary>
    public const int Ship = 1;

    /// <summary>The airship, the engine's own number.</summary>
    public const int Airship = 2;

    /// <summary>
    /// Sets a vehicle's image, from <c>322 Change Vehicle Image</c>.
    /// </summary>
    /// <param name="pVehicle">Which vehicle.</param>
    /// <param name="pFile">The image file, or an empty string.</param>
    /// <param name="pName">The image name inside it.</param>
    /// <param name="pIndex">Which frame of it.</param>
    /// <returns>What happened, in a sentence.</returns>
    public string SetVehicleImage(
        int pVehicle, string pFile, string pName, int pIndex)
    {
        // **Und `[(None)]` heisst "kein Bild", und nicht ein Dateiname.**
        var datei = pFile == NoImage ? "" : pFile;
        VehicleImages[pVehicle] = datei;
        return datei.Length == 0
            ? $"vehicle {pVehicle} has no image"
            : $"vehicle {pVehicle} shows {datei} / {pName} at {pIndex}";
    }

    /// <summary>What the editor writes when a vehicle shows nothing.</summary>
    /// <remarks>
    /// <strong>And this string is the editor's, and not this
    /// repository's.</strong> It appears in a project's <c>data</c> files
    /// **as that word, inside brackets**, **and a reader that guessed
    /// <c>""</c> instead would have kept looking for a file the game
    /// never named.**
    /// </remarks>
    public const string NoImage = "[(None)]";

    /// <summary>Hides the player, from <c>222 Erase Event</c>.</summary>
    public void Erase() => Erased = true;


    public int Animation { get; private set; } = MzCharacter.NoAnimation;

    /// <summary>Whether an animation is playing over the player.</summary>
    public bool HasAnimation => Animation != MzCharacter.NoAnimation;

    /// <summary>Frames the animation has left.</summary>
    public int AnimationFramesLeft { get; private set; }

    /// <summary>Plays an animation over the player, from <c>221</c>.</summary>
    /// <param name="pAnimation">Which one.</param>
    /// <param name="pFrames">How long it plays.</param>
    public void ShowAnimation(int pAnimation, int pFrames)
    {
        Animation = pAnimation;
        AnimationFramesLeft = pFrames;
    }

    /// <summary>Stops the animation at once.</summary>
    public void ClearAnimation()
    {
        Animation = MzCharacter.NoAnimation;
        AnimationFramesLeft = 0;
    }

    /// <summary>Counts the animation's clock down.</summary>
    /// <param name="pFrames">How many frames passed.</param>
    public void TickAnimation(int pFrames)
    {
        if (AnimationFramesLeft <= 0)
        {
            return;
        }

        AnimationFramesLeft -= pFrames;
        if (AnimationFramesLeft <= 0)
        {
            AnimationFramesLeft = 0;
            Animation = MzCharacter.NoAnimation;
        }
    }

    /// <summary>Whether an icon is showing over the player.</summary>
    public bool HasBalloon => BalloonIcon != MzCharacter.NoBalloon;

    /// <summary>Frames the icon has left, and zero when none shows.</summary>
    public int BalloonFramesLeft { get; private set; }

    /// <summary>Shows an icon over the player, from <c>213</c>.</summary>
    /// <param name="pIcon">Which icon.</param>
    /// <param name="pFrames">How long it stays.</param>
    public void ShowBalloon(int pIcon, int pFrames)
    {
        BalloonIcon = pIcon;
        BalloonFramesLeft = pFrames;
    }

    /// <summary>Takes the icon away at once.</summary>
    public void ClearBalloon()
    {
        BalloonIcon = MzCharacter.NoBalloon;
        BalloonFramesLeft = 0;
    }

    /// <summary>Counts the icon's clock down.</summary>
    /// <param name="pFrames">How many frames passed.</param>
    public void TickBalloon(int pFrames)
    {
        if (BalloonFramesLeft <= 0)
        {
            return;
        }

        BalloonFramesLeft -= pFrames;
        if (BalloonFramesLeft <= 0)
        {
            BalloonFramesLeft = 0;
            BalloonIcon = MzCharacter.NoBalloon;
        }
    }


    private Reservation? _reserved;

    /// <summary>
    /// Whether a transfer is on its way, as <c>isTransferring</c> answers it.
    /// This is the condition a 201's page waits on.
    /// </summary>
    public bool IsTransferring => _reserved.HasValue;

    /// <summary>The transfer that is on its way, or null.</summary>
    public Reservation? Reserved => _reserved;

    /// <summary>Maps this reader has been given, so it can tell one that is
    /// not there from one that is.</summary>
    public HashSet<int> KnownMaps { get; init; } = new();

    /// <summary>Something a transfer asked for and could not do, in order.</summary>
    public List<string> Notices { get; } = new();

    /// <summary>
    /// Puts the player somewhere without a transfer, as the engine does when
    /// a game starts and when a map is set up.
    /// </summary>
    /// <remarks>
    /// <c>setupForNewGame</c> is
    /// <c>this.reserveTransfer($dataSystem.startMapId, …)</c> — so the start
    /// goes through the reservation as well — and <c>Game_Map.setup</c> places
    /// what a transfer brought. A reader needs somewhere to put a player that
    /// is not being transferred, or every game would start on map one at the
    /// origin whatever its <c>System.json</c> says.
    /// </remarks>
    public void StandAt(int pMapId, int pX, int pY, int pDirection = 0)
    {
        MapId = pMapId;
        X = pX;
        Y = pY;
        Direction = pDirection;
    }

    /// <summary>What <c>reserveTransfer</c> does: records where to go.</summary>
    public string Reserve(
        int pMapId, int pX, int pY, int pDirection, int pFadeType)
    {
        _reserved = new Reservation(pMapId, pX, pY, pDirection, pFadeType);
        return $"a transfer to map {pMapId} at {pX},{pY} is reserved, facing"
            + $" {pDirection}, fade {pFadeType}; the player is still at"
            + $" map {MapId} {X},{Y} until it happens";
    }

    /// <summary>
    /// What <c>performTransfer</c> does, and it is the only thing that does it.
    /// </summary>
    /// <param name="pLoadedMaps">
    /// The maps the caller has actually read. A transfer to a map that is not
    /// among them is refused and named — <c>command201</c> does not check, and
    /// <c>$gameMap.setup</c> fails further on where nobody is looking.
    /// </param>
    public string PerformTransfer(IReadOnlyCollection<int>? pLoadedMaps = null)
    {
        if (_reserved == null)
        {
            // `performTransfer` is `if (this.isTransferring()) { … }`, so a
            // call with nothing reserved does nothing at all. It is not an
            // error and not a change.
            return "there is no transfer on its way, so nothing happened";
        }

        var was = _reserved.Value;

        if (pLoadedMaps != null && !pLoadedMaps.Contains(was.MapId))
        {
            // **The map is not one this reader has.** Half-applying it would
            // put the player on a map nothing can be read from, which is worse
            // than not moving at all: a caller would see a position and no
            // file behind it.
            var note =
                $"the transfer to map {was.MapId} was not carried out, because"
                + " this reader has not read that map";
            Notices.Add(note);
            return note;
        }

        MapId = was.MapId;
        X = was.X;
        Y = was.Y;
        // **The direction is set here and not on the reservation**, because
        // `performTransfer` is what calls `setDirection` and a player that
        // turned one frame early would be facing a map they are not on yet.
        Direction = was.Direction;
        _reserved = null;

        return $"the player is now on map {MapId} at {X},{Y}, facing"
            + $" {Direction}";
    }

    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        $"player on map {MapId} at {X},{Y} facing {Direction}"
        + (IsTransferring ? $", with a transfer to {_reserved!.Value.MapId} waiting"
            : "");
}
