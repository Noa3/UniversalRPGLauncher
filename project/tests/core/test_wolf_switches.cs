using System;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF's map and common event switches.
/// </summary>
/// <remarks>
/// <para>
/// The help's page-call note says that 0 and above address a map event and
/// 500,000 and above address a common event. The VM had one
/// <c>Dictionary&lt;int, bool&gt;</c>, so <strong>a map switch and a common
/// switch with the same index collided</strong> — and the collision is silent,
/// because both reads answer with a boolean and only the wrong one.
/// </para>
/// <para>
/// The split is 500,000 and not a million. The variable bands start at
/// 1,100,000; the switches start much lower, and a reader that reused the
/// variable scheme for switches would put every common switch into the map
/// range and lose it.
/// </para>
/// </remarks>
public partial class TestWolfSwitches : TestBase
{
	/// <summary>Normal variable 0, as a reference.</summary>
	private static int Normal0 => WolfVariable.BaseNormal;

	/// <summary>A common event switch, on the help's own base.</summary>
	private static int Common7 => WolfEventVm.CommonSwitchBase + 7;

	// ---- Die Trennung

	/// <summary>
	/// A map switch and a common switch with the same index are two switches.
	/// </summary>
	/// <remarks>
	/// <strong>This is the whole reason the split exists.</strong> A reader with
	/// one dictionary would have the second write overwrite the first, and a
	/// test that only read one of them would still see a boolean — so it would
	/// pass against the broken reader.
	/// </remarks>
	public void Test_AMapSwitchAndACommonSwitchAreTwoSwitches()
	{
		var vm = new WolfEventVm();
		vm.SetSwitch(7, true);
		vm.SetSwitch(Common7, false);

		AssertEq(
			vm.GetSwitch(7), true,
			"**and map switch 7 is on**, because it has its own storage;"
			+ $" it is {vm.GetSwitch(7)}");
		AssertEq(
			vm.GetSwitch(Common7), false,
			"**and common switch 7 is off**, which one dictionary would have"
			+ $" overwritten with the map value; it is {vm.GetSwitch(Common7)}");
	}

	/// <summary>
	/// The base is 500,000 and not a million.
	/// </summary>
	/// <remarks>
	/// <strong>The help names 500,000 in the page-call note.</strong> A reader
	/// that reused the variable bands' 1,100,000 for switches would leave
	/// 100,001 to 500,000 unreachable, and a game with a switch in that range
	/// would find it permanently off.
	/// </remarks>
	public void Test_TheBaseIsFiveHundredThousand()
	{
		AssertEq(
			WolfEventVm.CommonSwitchBase, 500_000,
			"**and the common base is 500,000**, the number the help's page-call"
			+ " note gives for common events — not a million, and not the"
			+ " variable bands' 1,100,000;"
			+ $" it is {WolfEventVm.CommonSwitchBase}");
		AssertEq(
			WolfEventVm.SwitchMapOf(Common7), 1,
			"**and 500,007 is a common switch**;"
			+ $" it is {WolfEventVm.SwitchMapOf(Common7)}");
		AssertEq(
			WolfEventVm.SwitchIndexOf(Common7), 7,
			"**and its index is 7**, the same index a map switch 7 has, which"
			+ $" is the collision; it is {WolfEventVm.SwitchIndexOf(Common7)}");
		AssertEq(
			WolfEventVm.SwitchMapOf(499_999), 0,
			"**and 499,999 is still a map switch**, because the map range runs to"
			+ " the base and not one short of it;"
			+ $" it is {WolfEventVm.SwitchMapOf(499_999)}");
	}

	/// <summary>
	/// A switch number outside both ranges changes nothing.
	/// </summary>
	/// <remarks>
	/// <strong>A reader that grew a dictionary would store a switch the editor
	/// cannot hold,</strong> and the next load would not carry it — so the
	/// switch would appear to work during the session and vanish after it. The
	/// refusal is silent here because the editor offers no such number, and a
	/// diagnostic would fire on every frame of a game that never asked for one.
	/// </remarks>
	public void Test_ASwitchOutsideBothRangesChangesNothing()
	{
		var vm = new WolfEventVm();
		vm.SetSwitch(5, true);

		AssertEq(
			WolfEventVm.SwitchMapOf(1_000_000), -1,
			"**and 1,000,000 is in neither range**, which is -1;"
			+ $" it is {WolfEventVm.SwitchMapOf(1_000_000)}");
		AssertEq(
			WolfEventVm.SwitchMapOf(-1), -1,
			"**and so is -1**;"
			+ $" it is {WolfEventVm.SwitchMapOf(-1)}");
		vm.SetSwitch(1_000_000, true);
		AssertEq(
			vm.GetSwitch(5), true,
			"**and map switch 5 is untouched**, because a number in neither"
			+ " range is not a place to write;"
			+ $" it is {vm.GetSwitch(5)}");
	}

	// ---- Der Nullzustand

	/// <summary>
	/// A switch nobody set is off, and a switch that cannot be read is off.
	/// </summary>
	/// <remarks>
	/// <strong>The condition list is "on" and "off" and nothing else</strong>,
	/// so an unreadable switch is off — which is also what a game expects before
	/// it has set the switch. Returning an error instead would stop the event.
	/// </remarks>
	public void Test_AnUnsetOrUnreadableSwitchIsOff()
	{
		var vm = new WolfEventVm();

		AssertEq(
			vm.GetSwitch(3), false,
			"**and a switch nobody set is off**, which is the zero state and not"
			+ $" an error; it is {vm.GetSwitch(3)}");
		AssertEq(
			vm.GetSwitch(2_000_000), false,
			"**and a switch in neither range is off too**, so a condition on it"
			+ $" takes the off arm and the game runs; it is {vm.GetSwitch(2_000_000)}");
	}

	/// <summary>
	/// A new game empties both switch maps.
	/// </summary>
	/// <remarks>
	/// <strong>A reset that cleared one would leave the other</strong>, and a
	/// game that started with the last game's common switches would start with
	/// its progression flags already set.
	/// </remarks>
	public void Test_ANewGameEmptiesBothSwitchMaps()
	{
		var vm = new WolfEventVm();
		vm.SetSwitch(1, true);
		vm.SetSwitch(Common7, true);

		vm.ResetState();

		AssertEq(
			vm.GetSwitch(1), false,
			"**and map switch 1 is off**, because the reset cleared the map"
			+ $" switches; it is {vm.GetSwitch(1)}");
		AssertEq(
			vm.GetSwitch(Common7), false,
			"**and common switch 7 is off as well**, because a reset that"
			+ " cleared only the map switches would leave the game with its"
			+ $" progression flags set; it is {vm.GetSwitch(Common7)}");
	}

	// ---- Die VM

	/// <summary>
	/// The program writes the switch map, not the VM's default.
	/// </summary>
	/// <remarks>
	/// <strong>Common switch 7 in a program</strong> has to land in the common
	/// map, and a reader that indexed one dictionary would put it where a map
	/// switch 7 lives.
	/// </remarks>
	public void Test_TheProgramWritesTheRightSwitchMap()
	{
		var vm = new WolfEventVm();
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.SetSwitch,
					Operand = Common7,
					Value = 1,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();

		AssertEq(
			vm.GetSwitch(Common7), true,
			"**and common switch 7 is on**, because the number's own range"
			+ $" decides; it is {vm.GetSwitch(Common7)}");
		AssertEq(
			vm.GetSwitch(7), false,
			"**and map switch 7 is still off**, which is the collision a single"
			+ $" dictionary would have made; it is {vm.GetSwitch(7)}");
	}

	/// <summary>
	/// A condition on a common switch branches on the common switch.
	/// </summary>
	/// <remarks>
	/// <strong>The branch and the write have to agree</strong> on which map a
	/// number belongs to, or a game that sets a common switch and tests it
	/// would take the other arm.
	/// </remarks>
	public void Test_AConditionOnACommonSwitchBranchesOnIt()
	{
		var vm = new WolfEventVm();
		vm.SetSwitch(Common7, true);
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.IfSwitch,
					Operand = Common7,
					Value = 1,
					TrueJumpIndex = 2,
					JumpIndex = 3,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
				new WolfEventCommand { Opcode = WolfEventOpcode.SetVariable, Operand = Normal0, Value = 111 },
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();
		vm.StepTick();

		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandNormal, 0), 111,
			"**and the true arm ran**, because common switch 7 is on and the"
			+ " branch reads the same map the write used;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandNormal, 0)}");
	}
}
