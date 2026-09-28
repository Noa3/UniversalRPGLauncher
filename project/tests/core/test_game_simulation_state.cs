using System;
using System.IO;
using Godot;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestGameSimulationState : TestBase
{
	private GameSimulationState _state = null!;

	public override void Setup()
	{
		_state = new GameSimulationState();
	}

	public void Test_NewStateHasValidDefaults()
	{
		AssertEq(_state.MapId, 0);
		AssertEq(_state.MapX, 0);
		AssertEq(_state.MapY, 0);
		AssertEq(_state.FacingDirection, 2);
		AssertEq(_state.Gold, 0);
		AssertEq(_state.FrameCount, 0);
		AssertEq(_state.Steps, 0);
		AssertEq(_state.FrameRate, 60);
		AssertFalse(_state.IsPaused);
		AssertFalse(_state.IsMenuOpen);
		AssertTrue(_state.IsSaveEnabled);
		AssertEq(_state.ActiveTroopId, -1);
		AssertFalse(_state.IsBattleActive);
		AssertEq(_state.BattlePhase, 0);
		// **No scene is current, and the stack is empty.** A previous version
		// asserted "Menu" here and pushed it onto the stack, but "Menu" is not
		// an RPG_RT scene name — it was an invention that made this test pass
		// against a fiction, and it contradicted the line above it.
		AssertEq(_state.SceneStack.Count, 0);
		AssertEq(_state.CurrentScene, "");
		AssertEq(_state.Diagnostics.Count, 0);
	}

	public void Test_ResetRestoresDefaults()
	{
		_state.MapId = 42;
		_state.MapX = 100;
		_state.MapY = 200;
		_state.Gold = 999;
		_state.FrameCount = 12345;
		_state.IsPaused = true;
		_state.IsMenuOpen = true;
		_state.IsSaveEnabled = false;
		_state.IsTransferPending = true;
		_state.PendingMapId = 42;
		_state.PendingX = 12;
		_state.PendingY = 34;
		_state.ActiveTroopId = 5;
		_state.IsBattleActive = true;
		_state.BattlePhase = 3;
		_state.SceneStack.Add("Battle");
		_state.CurrentScene = "Battle";
		_state.AddDiagnostic("test diag");

		_state.Reset();

		AssertEq(_state.MapId, 0);
		AssertEq(_state.MapX, 0);
		AssertEq(_state.MapY, 0);
		AssertEq(_state.Gold, 0);
		AssertEq(_state.FrameCount, 0);
		AssertFalse(_state.IsPaused);
		AssertFalse(_state.IsMenuOpen);
		AssertTrue(_state.IsSaveEnabled);
		AssertFalse(_state.IsTransferPending);
		AssertEq(_state.PendingMapId, 0);
		AssertEq(_state.PendingX, 0);
		AssertEq(_state.PendingY, 0);
		AssertEq(_state.ActiveTroopId, -1);
		AssertFalse(_state.IsBattleActive);
		AssertEq(_state.BattlePhase, -1);
		// **A new game opens nothing.** The reset used to push "Menu" onto the
		// stack and make it current, which meant a new game began with a scene
		// open and a stack that had to be popped before anything else could
		// happen.
		AssertEq(_state.SceneStack.Count, 0);
		AssertEq(_state.CurrentScene, "");
		AssertFalse(_state.ExitRequested);
		AssertFalse(_state.SupportsRpg2k3ECommands);
		AssertEq(_state.AtbWaitMode, true);
		AssertFalse(_state.FullscreenRequested);
		AssertEq(_state.Diagnostics.Count, 0);
	}

	public void Test_ResetClearsMutableRuntimeCollections()
	{
		_state.Switches.Add(true);
		_state.Variables.Add(42);
		_state.ItemCounts[7] = 3;
		_state.PartyMemberIds.Add(5);
		_state.ActorState[5] = new Godot.Collections.Dictionary { ["hp"] = 10 };
		_state.TroopMembers.Add(new Godot.Collections.Dictionary { ["id"] = 1 });
		_state.CommonEventIds.Add(9);
		_state.ConfigureMap(1, 1, 1, new[] { true });

		_state.Reset();

		AssertEq(_state.Switches.Count, 0, "Reset clears switches");
		AssertEq(_state.Variables.Count, 0, "Reset clears variables");
		AssertEq(_state.ItemCounts.Count, 0, "Reset clears inventory");
		AssertEq(_state.PartyMemberIds.Count, 0, "Reset clears party");
		AssertEq(_state.ActorState.Count, 0, "Reset clears actor state");
		AssertEq(_state.TroopMembers.Count, 0, "Reset clears troop members");
		AssertEq(_state.CommonEventIds.Count, 0, "Reset clears common event ids");
		AssertEq(_state.PassableTiles.Count, 0, "Reset clears map passability");
	}

	public void Test_BattleStateTransitions()
	{
		AssertFalse(_state.IsBattleActive);
		AssertEq(_state.ActiveTroopId, -1);

		_state.ActiveTroopId = 10;
		_state.IsBattleActive = true;
		_state.BattlePhase = 1;
		_state.BattleTurn = 1;

		AssertTrue(_state.IsBattleActive);
		AssertEq(_state.ActiveTroopId, 10);
		AssertEq(_state.BattlePhase, 1);
		AssertEq(_state.BattleTurn, 1);
	}

	public void Test_MaxTroopMembersLimit()
	{
		AssertEq(GameSimulationState.MaxTroopMembers, 8);
	}

	public void Test_MaxPartyMembersLimit()
	{
		AssertEq(GameSimulationState.MaxPartyMembers, 4);
	}

	public void Test_MaxMapIdLimit()
	{
		AssertEq(GameSimulationState.MaxMapId, 1000);
	}

	public void Test_MaxSwitchesLimit()
	{
		AssertEq(GameSimulationState.MaxSwitches, 50000);
	}

	public void Test_MaxVariablesLimit()
	{
		AssertEq(GameSimulationState.MaxVariables, 50000);
	}

	public void Test_DiagnosticsAreBounded()
	{
		AssertEq(_state.Diagnostics.Count, 0);
		for (var i = 0; i < 105; i++)
		{
			_state.AddDiagnostic($"diag {i}");
		}
		AssertTrue(_state.Diagnostics.Count <= 100);
	}

	public void Test_SwitchesAndVariablesInitialized()
	{
		AssertEq(_state.Switches.Count, 0);
		AssertEq(_state.Variables.Count, 0);
	}

	public void Test_ActorStateInitialized()
	{
		AssertEq(_state.ActorState.Count, 0);
	}

	public void Test_TroopMembersInitialized()
	{
		AssertEq(_state.TroopMembers.Count, 0);
	}

	public void Test_CommonEventIdsInitialized()
	{
		AssertEq(_state.CommonEventIds.Count, 0);
	}

	public void Test_MapMovementUpdatesPositionFacingAndSteps()
	{
		_state.ConfigureMap(3, 3, 2, new[] { true, true, true, true, true, true });
		AssertTrue(_state.TryMove(1, 0));
		AssertEq(_state.MapX, 1);
		AssertEq(_state.MapY, 0);
		AssertEq(_state.FacingDirection, 6);
		AssertEq(_state.Steps, 1);
	}

	public void Test_MapMovementBlocksImpassableAndBounds()
	{
		_state.ConfigureMap(3, 2, 2, new[] { true, false, true, true });
		AssertFalse(_state.TryMove(1, 0));
		AssertEq(_state.MapX, 0);
		AssertEq(_state.Steps, 0);
		AssertFalse(_state.TryMove(0, -1));
		AssertEq(_state.FacingDirection, 8);
	}

	public void Test_MapMovementRejectsDiagonalAndInvalidPassabilityShape()
	{
		_state.ConfigureMap(3, 2, 2, new[] { true, true, true, true });
		AssertFalse(_state.TryMove(1, 1));
		AssertEq(_state.Diagnostics.Count, 1);
		var threw = false;
		try
		{
			_state.ConfigureMap(3, 2, 2, new[] { true });
		}
		catch (ArgumentException)
		{
			threw = true;
		}
		AssertTrue(threw);
	}

	public void Test_SaveCodecRoundTripsBoundedSimulationState()
	{
		_state.ConfigureMap(7, 2, 2, new[] { true, false, true, true });
		_state.MapX = 1; _state.MapY = 1; _state.Gold = 1234; _state.FrameCount = 77;
		_state.Switches.Add(true); _state.Variables.Add(42); _state.ItemCounts[3] = 2;
		_state.PartyMemberIds.Add(5); _state.SceneStack.Add("Map"); _state.CurrentScene = "Map";
		_state.SetTimer(1, 9);

		var json = Rm2kSimulationSaveCodec.Serialize(_state);
		var restored = new GameSimulationState();

		AssertTrue(Rm2kSimulationSaveCodec.TryRestore(json, restored, out var error));
		AssertEq(error, ""); AssertEq(restored.MapId, 7); AssertEq(restored.MapX, 1);
		AssertEq(restored.Gold, 1234); AssertEq(restored.Variables[0], 42);
		AssertEq(restored.ItemCounts[3], 2); AssertTrue(restored.Timer1Active); AssertEq(restored.Timer1Seconds, 9);
	}

	public void Test_SaveCodecRejectsMalformedAndOversizedPayloads()
	{
		var restored = new GameSimulationState();
		AssertFalse(Rm2kSimulationSaveCodec.TryRestore("{not-json", restored, out _));
		AssertFalse(Rm2kSimulationSaveCodec.TryRestore(new string('x', Rm2kSimulationSaveCodec.MaxPayloadBytes + 1), restored, out _));
	}

	public void Test_SaveCodecWritesAndReadsBoundedSaveDirectorySlot()
	{
		var directory = ProjectSettings.GlobalizePath("user://rm2k-save-slots");
		try
		{
			if (Directory.Exists(directory)) Directory.Delete(directory, true);
			_state.ConfigureMap(2, 1, 1, new[] { true });
			_state.Gold = 99;
			AssertTrue(Rm2kSimulationSaveCodec.TryWriteFile(directory, "slot1", _state, out var writeError));
			AssertEq(writeError, "");
			var restored = new GameSimulationState();
			AssertTrue(Rm2kSimulationSaveCodec.TryReadFile(directory, "slot1", restored, out var readError));
			AssertEq(readError, ""); AssertEq(restored.Gold, 99);
			AssertFalse(Rm2kSimulationSaveCodec.TryWriteFile(directory, "../escape", _state, out _));
		}
		finally
		{
			if (Directory.Exists(directory)) Directory.Delete(directory, true);
		}
	}
}
