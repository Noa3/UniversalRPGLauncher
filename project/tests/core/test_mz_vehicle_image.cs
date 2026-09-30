using System;
using System.Collections.Generic;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// <c>322 Change Vehicle Image</c>, measured against a finished MZ
/// project and read from the official manual.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the help names two settings, and the second has a value
/// that is not a file.</strong> *Change the image used for vehicles.
/// These settings will remain in effect until updated again by using this
/// event command. Vehicle — Specify the target vehicle. Images —
/// Double-click the box to specify the image to be displayed. Setting
/// this to <c>[(None)]</c> will result in no image being displayed.*
/// </para>
/// <para>
/// <strong>And the measured form is
/// <c>[1, "MC_Sprite_sheet", 1, "SlimeActors", 5, "Actor1_1"]</c></strong>
/// — six values, <strong>and the first is the vehicle, which is 1 in all
/// six of them.</strong>
/// </para>
/// </remarks>
public partial class TestMzVehicleImage : TestBase
{
    private static (MzBranchFacts Facts, MzInterpreter Lauf) Start()
    {
        return (new MzBranchFacts(), new MzInterpreter(new List<MzCommandEntry> { }));
    }

    /// <summary>
    /// An image goes to the vehicle the file names, and nowhere else.
    /// </summary>
    /// <remarks>
    /// <strong>And the first value is the vehicle, and not the index of
    /// the image.</strong> The help says *Specify the target vehicle* and
    /// names nothing else; <strong>and the editor's list is Boat, Ship,
    /// Airship, so 1 is the ship</strong> — <strong>and a reader that read
    /// the first value as a frame index put a ship's picture on the
    /// boat.</strong>
    /// </remarks>
    public void Test_EinBildGehtAnDasFahrzeugUndAnKeinAnderes()
    {
        var (fakten, lauf) = Start();
        var aktionen = new List<MzAction>();

        AssertTrue(MzCommands.TryExecute(lauf,
                new MzCommandEntry(322,
                    ["1", "MC_Sprite_sheet", "1", "SlimeActors", "5",
                        "Actor1_1"], 0),
                aktionen, fakten, new MzRandom()),
            "**and the command runs**");

        AssertEq(fakten.Player.VehicleImages[MzPlayer.Ship], "MC_Sprite_sheet",
            "**and the ship shows the file the game named** -- and the first "
                + "value is the vehicle, which is 1 here, and that is the "
                + "ship, and a reader that read it as a frame index put a "
                + "ship's picture on the boat");
        AssertTrue(fakten.Player.VehicleImages.Count == 1,
            "**and no other vehicle has an image**");
        AssertEq(fakten.Notices.Count, 0,
            "**and nothing is complained about**");
    }

    /// <summary>
    /// The three vehicles are the engine's own numbers.
    /// </summary>
    /// <remarks>
    /// <strong>And the boat is zero.</strong> The engine's
    /// <c>$gameVehicle</c> indexes from zero,
    /// <strong>and a reader that started at one had no key for the boat
    /// at all</strong> — <strong>and a game that changes the boat's
    /// image changed nobody's.</strong>
    /// </remarks>
    public void Test_DieDreiFahrzeugeSindDieNummernDesMotors()
    {
        var (fakten, lauf) = Start();
        foreach (var fahrzeug in new[]
        {
            MzPlayer.Boat, MzPlayer.Ship, MzPlayer.Airship,
        })
        {
            MzCommands.TryExecute(lauf,
                new MzCommandEntry(322,
                    [fahrzeug.ToString(), $"datei{fahrzeug}", "0", "name", "0",
                        "bild"], 0),
                new List<MzAction>(), fakten, new MzRandom());
        }

        AssertEq(fakten.Player.VehicleImages.Count, 3,
            "**and all three have an image** -- and a reader that started at "
                + "one had no key for the boat at all, and a game that "
                + "changes the boat's image changed nobody's");
        AssertEq(fakten.Player.VehicleImages[MzPlayer.Boat], "datei0",
            "**and the boat has its own**");
        AssertEq(fakten.Player.VehicleImages[MzPlayer.Airship], "datei2",
            "**and the airship has its own**");
    }

    /// <summary>
    /// <c>[(None)]</c> is no image, and not a file name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the value the help calls out by name.</strong>
    /// *Setting this to <c>[(None)]</c> will result in no image being
    /// displayed* — <strong>and a reader that stored the string showed a
    /// vehicle with a picture whose file does not exist</strong>, which
    /// on a map with a ship in it is a hole where a ship should be.
    /// </para>
    /// <para>
    /// <strong>And an image that comes after <c>[(None)]</c> is a real
    /// image again.</strong> The help says the settings remain in effect
    /// *until updated again by using this event command*,
    /// <strong>and a game that hides a picture and shows it again two
    /// commands later is normal</strong>.
    /// </para>
    /// </remarks>
    public void Test_DerWertFuerKeinBildIstKeinDateiname()
    {
        var (fakten, lauf) = Start();

        MzCommands.TryExecute(lauf,
            new MzCommandEntry(322,
                ["1", MzPlayer.NoImage, "0", "", "0", ""], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.Player.VehicleImages[MzPlayer.Ship], "",
            "**and the ship has no image** -- and a reader that stored the "
                + "string itself showed a vehicle with a picture whose file "
                + "does not exist, which on a map with a ship in it is a "
                + "hole where a ship should be");

        // **Und danach wieder ein echtes Bild.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(322,
                ["1", "MC_Sprite_sheet", "0", "Boat", "0", "Ship"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.Player.VehicleImages[MzPlayer.Ship], "MC_Sprite_sheet",
            "**and a later command brings it back** -- and the help says the "
                + "settings remain in effect until updated again by using "
                + "this event command");
    }

    /// <summary>
    /// A vehicle this reader does not know is named.
    /// </summary>
    /// <remarks>
    /// <strong>And there are three, and the engine names them itself.</strong>
    /// Measured: the first value is 1 six times over.
    /// <strong>A reader that accepted any number put a fourth vehicle into
    /// a game that has three</strong>, <strong>and a map that draws its
    /// three would have nothing to draw the fourth with.</strong>
    /// </remarks>
    public void Test_EinFahrzeugDasDieserLeserNichtKenntWirdGenannt()
    {
        var (fakten, lauf) = Start();

        MzCommands.TryExecute(lauf,
            new MzCommandEntry(322,
                ["7", "MC_Sprite_sheet", "0", "x", "0", "y"], 0),
            new List<MzAction>(), fakten, new MzRandom());

        AssertEq(fakten.Player.VehicleImages.Count, 0,
            "**and no fourth vehicle appears** -- and a reader that accepted "
                + "any number put a vehicle into a game that has three");
        AssertEq(fakten.Notices.Count, 1,
            "**and the number is named**");
    }

    /// <summary>
    /// A negative vehicle number is refused, and not only a big one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what the living mutation rule found.</strong>
    /// The test above asks for vehicle 7, and a reader whose lower bound
    /// was missing accepts 7 out of habit and refuses it only by
    /// accident of the comparison's other half.
    /// </para>
    /// <para>
    /// <strong>And minus one is the number every other MZ character
    /// parameter uses for "the player".</strong> Measured across this
    /// dispatch: <c>213</c> and <c>221</c> take <c>-1</c> for the player,
    /// <strong>and a reader that carried that habit into this command put
    /// a ship's picture on the player</strong> — <strong>and a game that
    /// asks for vehicle minus one asks for the player, not for
    /// anything with wheels.</strong>
    /// </para>
    /// <para>
    /// <strong>And zero is the boat, and is accepted</strong>,
    /// <strong>because the engine's own <c>$gameVehicle</c> indexes from
    /// zero.</strong> The bound has to be below it, not above.
    /// </para>
    /// </remarks>
    public void Test_EineNegativeFahrzeugzahlWirdAbgewiesen()
    {
        var (fakten, lauf) = Start();

        MzCommands.TryExecute(lauf,
            new MzCommandEntry(322,
                ["-1", "MC_Sprite_sheet", "0", "x", "0", "y"], 0),
            new List<MzAction>(), fakten, new MzRandom());

        AssertEq(fakten.Player.VehicleImages.Count, 0,
            "**and no image lands anywhere** -- and minus one is the "
                + "number every other character parameter in this dispatch "
                + "uses for the player, so a reader that carried the habit "
                + "here put a ship's picture on the player");
        AssertEq(fakten.Notices.Count, 1,
            "**and the number is named**");

        // **Und null ist das Boot, und das wird angenommen.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(322,
                ["0", "MC_Sprite_sheet", "0", "x", "0", "y"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.Player.VehicleImages.Count, 1,
            "**and zero is the boat, and it is accepted** -- and the "
                + "engine's own $gameVehicle indexes from zero, so a bound "
                + "that started at one had no key for the boat at all");
    }
}
