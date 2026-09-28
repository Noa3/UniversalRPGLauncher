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
