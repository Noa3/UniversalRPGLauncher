using System;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 10620 Change Hero Title, 10630 Change Sprite Association, 10640
/// Change Actor Face, 10650 Change Vehicle Graphic and 10850 Set Vehicle
/// Location.
/// </summary>
/// <remarks>
/// <para>
/// The interesting one is <c>10850</c>: <strong>vehicle id -1 moves the party
/// and not a vehicle.</strong> The reference has a comment on it saying that
/// RPG_RT stores -1 for a party in no vehicle, and a reader that refused it as
/// an invalid id would make every "teleport the hero" command in a game do
/// nothing.
/// </para>
/// <para>
/// The other three are about fields a hero has and the reference does not
/// share: a title beside the name, a sprite index beside the character, and a
/// face index with no transparency flag.
/// </para>
/// </remarks>
public partial class TestRm2kActorGraphics : TestBase
{
	private static Rm2kMap.EventCommand Cmd(
		int pCode, string pText, params int[] pParameters)
	{
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = pText,
			Parameters = [.. pParameters],
		};
	}

	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		state.ConfigureMap(1, 20, 20, new bool[400]);
		var interpreter = new EventInterpreter(state, 1, pCommands);
		return (interpreter, state);
	}

	private static Rm2kVehicleState AddBoat(GameSimulationState pState)
	{
		var boat = new Rm2kVehicleState(
			Rm2kVehicle.Boat, pState.MapId, 5, 5);
		pState.Vehicles.Add(boat);
		return boat;
	}

	// ---- 10620

	/// <summary>
	/// A title is a field of its own and not the name.
	/// </summary>
	/// <remarks>
	/// The reference calls <c>SetTitle</c>, which is its own field. <strong>A
	/// reader that wrote the name would replace the database name</strong>, and a
	/// game that gives a hero a title keeps the name for the party window.
	/// </remarks>
	public void Test_ATitleIsItsOwnField()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeHeroTitle, "the Silent", 1, 1, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.ActorValues[1].Title, "the Silent",
			"**and hero 1 is now called that**, from the string field;"
			+ $" it is \"{state.ActorValues[1].Title}\"");
		AssertEq(
			state.GetOrCreateActorState(1).ContainsKey("title"), false,
			"**and no title was written into the database name field**, because"
			+ " the title is its own field on the hero and a reader that"
			+ " reused the name key would have shown a title in the party window;"
			+ $" \"{state.GetOrCreateActorState(1)["name"]}\"");
	}

	/// <summary>
	/// A missing hero is a warning and not a refusal.
	/// </summary>
	/// <remarks>
	/// The reference calls <c>GetActor</c>, checks the result, writes a warning
	/// and <c>return true</c>. <strong>A reader that held the page would leave a
	/// cutscene waiting for a hero the database never had</strong>, and a game
	/// that addresses an actor slot it chose not to fill would hang there
	/// forever.
	/// </remarks>
	public void Test_AMissingHeroIsWarnedAboutAndThePageMovesOn()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeHeroTitle, "Ghost",
			GameSimulationState.MaxActorId + 1, 1, 0, 0),
			Cmd(EventInterpreter.ChangeScreenTransitions, "", 0, 3));

		interpreter.ExecuteFrame();
		var weiter = interpreter.ExecuteFrame();

		AssertEq(
			weiter, true,
			"**and the page moved on**, because the reference warns and returns"
			+ $" true; the frame reported {weiter}");
		AssertTrue(
			ContainsDiagnostic(state, "is outside 1 to"),
			"and the diagnostic names the bound; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	// ---- 10630

	/// <summary>
	/// The sprite index is a pose and not a character number.
	/// </summary>
	/// <remarks>
	/// <strong>A costume is the same file with a different index.</strong> A
	/// reader that read the index as a character number would put a hero in
	/// somebody else's costume, and the file name would be right — which is why
	/// no visual check catches it.
	/// </remarks>
	public void Test_TheSpriteIndexIsAPose()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeSpriteAssociation, "Hero2", 1, 3, 0, 4, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.ActorValues[1].SpriteName, "Hero2",
			"**and hero 1 wears Hero2**, from the string field; it is"
			+ $" \"{state.ActorValues[1].SpriteName}\"");
		AssertEq(
			state.ActorValues[1].SpriteIndex, 3,
			"**and at pose 3**, from parameters[1], which is a pose in that"
			+ $" file and not a character; it is {state.ActorValues[1].SpriteIndex}");
		AssertEq(
			state.ActorValues[1].SpriteTransparent, false,
			"**and not transparent**, because parameters[2] was zero;"
			+ $" it is {state.ActorValues[1].SpriteTransparent}");
	}

	/// <summary>
	/// The transparency is parameters[2] and not part of the bitfield.
	/// </summary>
	/// <remarks>
	/// The reference reads the mode index for the file and the pose but takes
	/// the transparency <strong>directly from <c>parameters[2]</c></strong>.
	/// A reader that took all three from the bitfield would make a costume
	/// transparent whenever a Maniac game packed a different value there.
	/// </remarks>
	public void Test_TheTransparencyComesStraightFromTheThirdParameter()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeSpriteAssociation, "Hero2", 1, 3, 1, 4, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.ActorValues[1].SpriteTransparent, true,
			"**and hero 1 is transparent**, from a non-zero third parameter"
			+ $" even though the mode is one; it is {state.ActorValues[1].SpriteTransparent}");
		AssertEq(
			state.ActorValues[1].SpriteIndex, 3,
			"**and the pose is still 3**, because a reader that read the index"
			+ " out of the mode index would have taken the mode instead;"
			+ $" it is {state.ActorValues[1].SpriteIndex}");
	}

	// ---- 10640

	/// <summary>
	/// A face is two values and not three.
	/// </summary>
	/// <remarks>
	/// The name and the index, and nothing else. <strong>A reader that read a
	/// transparency flag here would shift the index by one</strong> and put a
	/// hero in the wrong face.
	/// </remarks>
	public void Test_AFaceIsTwoValues()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeActorFace, "Face2", 1, 2, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.ActorValues[1].FaceName, "Face2",
			"**and hero 1 shows Face2**, from the string field; it is"
			+ $" \"{state.ActorValues[1].FaceName}\"");
		AssertEq(
			state.ActorValues[1].FaceIndex, 2,
			"**and slot 2**, from the second parameter, which is the index and"
			+ " not a transparency flag; it is"
			+ $" {state.ActorValues[1].FaceIndex}");
	}

	/// <summary>
	/// A face outside the file's four slots is refused.
	/// </summary>
	public void Test_AFaceOutsideTheFourSlotsIsRefused()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeActorFace, "Face2", 1, 9, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.ActorValues[1].FaceName, "",
			"**and nothing was set**, because the file has four slots and a"
			+ $" reader that stored a ninth would name a face that is not there;"
			+ $" the name is \"{state.ActorValues[1].FaceName}\"");
		AssertTrue(
			ContainsDiagnostic(state, "four slots"),
			"and the diagnostic says why; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	// ---- 10650

	/// <summary>
	/// Parameter 0 plus one, and the sprite is set twice.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The liblcf enum is <c>None = 0, Boat = 1, Ship = 2, Airship =
	/// 3</c></strong> and the reference writes
	/// <c>(Game_Vehicle::Type)(com.parameters[0] + 1)</c>. A reader that used the
	/// parameter directly would address vehicle 0 — and vehicle 0 is the party,
	/// not a boat.
	/// </para>
	/// <para>
	/// <strong>It sets two fields</strong>, the current sprite and the original
	/// one. <strong>A reader that set only the current one would leave a vehicle
	/// in its costume after the party got out</strong>, and the tests check both.
	/// </para>
	/// </remarks>
	public void Test_AVehicleSpriteIsSetTwiceAndTheIdIsShifted()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeVehicleGraphic, "Ship2", 0, 2));
		var boot = AddBoat(state);

		interpreter.ExecuteFrame();

		AssertEq(
			boot.CharacterName, "Ship2",
			"**and parameter 0 changed the boat**, because the reference adds one"
			+ " and 0 is the first vehicle; a reader that used it directly would"
			+ $" have addressed vehicle 0; the name is \"{boot.CharacterName}\"");
		AssertEq(
			boot.SpriteIndex, 2,
			"**and at pose 2**, from parameters[1]; it is"
			+ $" {boot.SpriteIndex}");
		AssertEq(
			boot.OriginalCharacterName, "Ship2",
			"**and the original is the same name**, because that is what the"
			+ " vehicle goes back to when a board ends; it is"
			+ $" \"{boot.OriginalCharacterName}\"");
		AssertEq(
			boot.OriginalSpriteIndex, 2,
			"and the original pose too; it is"
			+ $" {boot.OriginalSpriteIndex}");
	}

	/// <summary>
	/// A vehicle that is not on the map is warned about.
	/// </summary>
	/// <remarks>
	/// The reference calls <c>GetVehicle</c>, checks the result and warns. <strong>
	/// A reader that threw would take a game down</strong> over a vehicle the map
	/// happens not to carry.
	/// </remarks>
	public void Test_AMissingVehicleIsWarnedAbout()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeVehicleGraphic, "Ship2", 3, 2));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Vehicles.Count, 0,
			"**and no vehicle was created**, because the command changes a"
			+ $" vehicle that exists; there are {state.Vehicles.Count}");
		AssertTrue(
			ContainsDiagnostic(state, "is not on this map"),
			"and the diagnostic says so; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	// ---- 10850

	/// <summary>
	/// Vehicle id -1 moves the party and is not an invalid id.
	/// </summary>
	/// <remarks>
	/// <strong>This is the whole point of the command's fourth parameter.</strong>
	/// The reference has a comment on it: in RPG_RT a party in no vehicle has
	/// the id -1, and passing -1 moves the party on its own. A reader that
	/// refused it would make every "teleport the hero" command in a game do
	/// nothing — and that is a very common command.
	/// </remarks>
	public void Test_VehicleIdMinusOneMovesTheParty()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.SetVehicleLocation, "", -1, 0, 4, 7, 9));

		interpreter.ExecuteFrame();

		AssertEq(
			state.MapX, 7,
			"**and the party is at 7**, because id -1 means the party and not a"
			+ $" vehicle; it is {state.MapX}");
		AssertEq(
			state.MapY, 9,
			$"and at 9 on the row; it is {state.MapY}");
		AssertEq(
			state.MapId, 4,
			"**and on map 4**, from the map parameter, which is a different"
			+ $" value and not the map the party started on; it is {state.MapId}");
	}

	/// <summary>
	/// A real vehicle moves on its own when nobody is in it.
	/// </summary>
	public void Test_AVehicleMovesOnItsOwn()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.SetVehicleLocation, "", 0, 0, 2, 3, 4));
		var boot = AddBoat(state);

		interpreter.ExecuteFrame();

		AssertEq(
			boot.X, 3,
			"**and the boat is at 3**, because parameter 0 is the boat after the"
			+ $" shift; it is {boot.X}");
		AssertEq(
			boot.MapId, 2,
			"**and on map 2**, which is a different map and not a tile; it is"
			+ $" {boot.MapId}");
		AssertEq(
			state.MapX, 0,
			"**and the party stayed where it was**, because nobody is aboard;"
			+ $" the party is at {state.MapX}");
	}

	/// <summary>
	/// A vehicle with the party inside moves the party too.
	/// </summary>
	/// <remarks>
	/// <strong>Both move as one and the reference returns right after.</strong>
	/// Moving only the vehicle would leave the hero standing in the map they
	/// left, which in a game with a boat is a party in open water.
	/// </remarks>
	public void Test_APartyInsideAVehicleMovesWithIt()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.SetVehicleLocation, "", 0, 0, 2, 6, 6));
		var boot = AddBoat(state);
		// **BeginEmbark and not a field assignment.** The boarding state has
		// private setters for a reason — the same reason the timer has
		// SetTimer and StartTimer — and a test that reached past them would
		// prove a state the runtime cannot actually reach.
		state.Boarding = new Rm2kVehicleBoarding();
		state.Boarding.BeginEmbark(Rm2kVehicle.Boat, 4, boot.X, boot.Y);

		interpreter.ExecuteFrame();

		AssertEq(
			boot.X, 6,
			$"**and the boat is at 6**, from the command; it is {boot.X}");
		AssertEq(
			state.MapId, 2,
			"**and the party is on map 2 as well**, which is the half that"
			+ $" matters; it is {state.MapId}");
		AssertEq(
			state.MapX, 6,
			"and at the same column, because they move as one; it is"
			+ $" {state.MapX}");
	}

	/// <summary>
	/// The three coordinates can come from a variable.
	/// </summary>
	/// <remarks>
	/// <strong>All three go through <c>ValueOrVariable</c></strong> with the mode
	/// in <c>parameters[1]</c>, so a game can follow a variable. A reader that
	/// read them as constants could only ever move a vehicle to one tile.
	/// </remarks>
	public void Test_TheCoordinatesCanComeFromAVariable()
	{
		var state = new GameSimulationState { MapId = 1 };
		state.ConfigureMap(1, 20, 20, new bool[400]);
		for (var i = 0; i < 10; i++)
		{
			state.Variables.Add(0);
		}
		// **Indices 0, 1 and 2, because the array is zero based and a variable
		// id is one based.** Writing to 1, 2 and 3 would have put the map in
		// variable 2 and read variable 1 for the column, which is empty.
		state.Variables[0] = 2;
		state.Variables[1] = 11;
		state.Variables[2] = 12;
		var boot = new Rm2kVehicleState(Rm2kVehicle.Boat, 1, 5, 5);
		state.Vehicles.Add(boot);
		var interpreter = new EventInterpreter(state, 1, [Cmd(
			EventInterpreter.SetVehicleLocation, "", 0, 1, 1, 2, 3)]);

		interpreter.ExecuteFrame();

		AssertEq(
			boot.MapId, 2,
			"**and the boat is on the map variable 1 held**, because all three"
			+ $" coordinates go through ValueOrVariable; it is {boot.MapId}");
		AssertEq(
			boot.X, 11,
			"**and at column 11**, from variable 2, which is a different"
			+ $" variable from the map; it is {boot.X}");
		AssertEq(
			boot.Y, 12,
			$"and at row 12, from variable 3; it is {boot.Y}");
	}

	/// <summary>
	/// A vehicle that is not there is refused, and -1 is still the party.
	/// </summary>
	/// <remarks>
	/// <strong>Two different meanings for one field.</strong> -1 is the party and
	/// works; 0 is the boat and needs a boat. A reader that refused everything
	/// outside the vehicle list would have refused the party case too, and that
	/// one is the common command.
	/// </remarks>
	public void Test_AMissingVehicleIsRefusedAndMinusOneIsNot()
	{
		var (fehlt, state1) = Run(
			Cmd(EventInterpreter.SetVehicleLocation, "", 2, 0, 1, 2, 2));
		fehlt.ExecuteFrame();

		AssertEq(
			state1.MapX, 0,
			"**and the party did not move**, because parameter 2 names a"
			+ $" vehicle this map has not; the party is at {state1.MapX}");
		AssertTrue(
			ContainsDiagnostic(state1, "is not on this map"),
			"and the diagnostic says so; the diagnostics are"
			+ $" {state1.Diagnostics.Count}");

		var (partei, state2) = Run(
			Cmd(EventInterpreter.SetVehicleLocation, "", -1, 0, 1, 2, 2));
		partei.ExecuteFrame();
		AssertEq(
			state2.MapX, 2,
			"**and -1 still moved the party**, because it is the party and not a"
			+ $" vehicle id; the party is at {state2.MapX}");
	}

	/// <summary>
	/// None of it survives a new game.
	/// </summary>
	/// <remarks>
	/// <strong>A new game that inherited the last game's costume would show a
	/// boat in a sailor's suit</strong>, and a hero in somebody else's sprite.
	/// </remarks>
	public void Test_NoneOfItSurvivesANewGame()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeHeroTitle, "the Silent", 1, 1, 0, 0),
			Cmd(EventInterpreter.ChangeSpriteAssociation, "Hero2", 1, 3, 0, 4, 0),
			Cmd(EventInterpreter.ChangeActorFace, "Face2", 1, 2, 0, 0),
			Cmd(EventInterpreter.ChangeVehicleGraphic, "Ship2", 0, 2));
		AddBoat(state);
		for (var i = 0; i < 4; i++)
		{
			interpreter.ExecuteFrame();
		}

		state.Reset();

		AssertEq(
			state.ActorValues.Count, 0,
			"**and the heroes are gone**, because their titles, sprites and"
			+ $" faces belong to the last game; there are {state.ActorValues.Count}");
		AssertEq(
			state.Vehicles.Count, 0,
			"**and the vehicles are gone**, for the same reason; a new game that"
			+ $" inherited one would start with a boat already at sea; there are {state.Vehicles.Count}");
	}

	private static bool ContainsDiagnostic(
		GameSimulationState pState, string pNeedle)
	{
		foreach (var line in pState.Diagnostics)
		{
			if (line.Contains(pNeedle, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}
}
