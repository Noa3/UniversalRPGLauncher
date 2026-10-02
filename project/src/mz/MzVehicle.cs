using System;

namespace UniversalRPG.Web;

/// <summary>
/// One vehicle, and it is a character that moves on its own.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And three of the last commands need it</strong> --
/// <strong><c>140 Change Vehicle BGM</c>, <c>202 Set Vehicle
/// Location</c> und <c>323 Change Vehicle Image</c></strong> --
/// <strong>and all three begin with the same three lines:</strong>
/// </para>
/// <code>
/// command140 = function() {
///     var vehicle = $gameMap.vehicle(this._params[0]);
///     if (vehicle) {
///         vehicle.setBgm(this._params[1]);
///     }
///     return true;
/// };
/// </code>
/// <para>
/// <strong>And that <c>if (vehicle)</c> is the whole difference between
/// a command that works and one that does nothing.</strong>
/// </para>
/// <para>
/// <strong>And a vehicle that is not on the map is not an error</strong>
/// -- <strong>it is a ship that nobody has come near yet</strong> --
/// <strong>and <c>$gameMap.vehicle(i)</c> walks the event list for one
/// whose <c>vehicleIndex</c> matches.</strong> <strong>And a game that
/// asks for vehicle two when it only has vehicle one does not get an
/// error and does not get a vehicle either.</strong>
/// </para>
/// <para>
/// <strong>And <c>202</c> reads its three numbers through the same
/// <c>if (this._params[1] === 0)</c> gate as every other
/// <c>setLocation</c>-Befehl</strong> -- <strong>a zero takes them
/// plain and anything else takes them out of
/// <c>$gameVariables.value(...)</c>.</strong>
/// </para>
/// </remarks>
public sealed class MzVehicle
{
    public MzVehicle(int pIndex, string pName)
    {
        Index = pIndex;
        Name = pName;
    }

    /// <summary>
    /// Which one it is, and <c>$gameMap.vehicle(i)</c> matches this
    /// against every event's <c>vehicleIndex</c>.
    /// </summary>
    public int Index { get; }

    /// <summary>Its name, as the event list spells it.</summary>
    public string Name { get; }

    /// <summary>
    /// Where it stands, and <c>command202</c> puts it there with one
    /// <c>setLocation(mapId, x, y)</c>.
    /// </summary>
    public int MapId { get; private set; }

    /// <summary>And its x on that map.</summary>
    public int X { get; private set; }

    /// <summary>And its y, which counts downwards.</summary>
    public int Y { get; private set; }

    /// <summary>
    /// The picture it shows, and <c>setImage(name, index)</c> is the two.
    /// </summary>
    /// <remarks>
    /// <strong>And an empty picture name is what takes a vehicle off the
    /// map</strong>, <strong>not a picture called "none".</strong>
    /// </remarks>
    public string BildName { get; private set; } = "";

    /// <summary>And which layer of the tileset it stands on.</summary>
    public int Ebene { get; private set; }

    /// <summary>
    /// Whether the player is inside it, and <c>206 Get on/off Vehicle</c>
    /// is one call with no parameter at all.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>command206</c> is
    /// <c>$gamePlayer.getOnOffVehicle(); return true;</c></strong> --
    /// <strong>it takes no parameter</strong>, <strong>because the
    /// engine finds the vehicle under the player itself</strong>,
    /// <strong>and not one the command names.</strong>
    /// </remarks>
    public bool Befahren { get; private set; }

    /// <summary>
    /// <c>vehicle.setLocation(mapId, x, y)</c>, and it is three
    /// assignments.
    /// </summary>
    /// <param name="pMap">Which map.</param>
    /// <param name="pX">Where on it.</param>
    /// <param name="pY">And where on it, downwards.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeOrt(int pMap, int pX, int pY)
    {
        MapId = pMap;
        X = pX;
        Y = pY;
        return Name + " is put on map " + pMap + " at " + pX + ", " + pY
            + ", and `command202` is one `vehicle.setLocation(mapId, x, "
            + "y)` and nothing else";
    }

    /// <summary>
    /// <c>vehicle.setImage(imageName, layerIndex)</c>, and it is two
    /// assignments.
    /// </summary>
    /// <param name="pBild">The picture's name, and an empty one takes it
    /// off the map.</param>
    /// <param name="pEbene">Which layer, and two to four.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeBild(string pBild, int pEbene)
    {
        BildName = pBild;
        Ebene = pEbene;
        return Name + " now shows " + pBild + " on layer " + pEbene;
    }

    /// <summary>
    /// <c>$gamePlayer.getOnOffVehicle()</c>, and it flips the flag.
    /// </summary>
    /// <returns>One line, for an action and for a log.</returns>
    public string Wechsel()
    {
        Befahren = !Befahren;
        return "the player " + (Befahren ? "gets in" : "gets out of") + " "
            + Name + ", and `command206` takes no parameter, because the "
            + "engine looks for the vehicle under the player itself";
    }

    /// <summary>One line, for a log.</summary>
    public override string ToString() =>
        Name + " on map " + MapId + " at " + X + ", " + Y;
}
