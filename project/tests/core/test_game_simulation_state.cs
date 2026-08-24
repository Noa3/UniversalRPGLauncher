using System;
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
		AssertEq(_state.SceneStack.Count, 0);
		AssertEq(_state.CurrentScene, "Menu");
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
		AssertEq(_state.ActiveTroopId, -1);
		AssertFalse(_state.IsBattleActive);
		AssertEq(_state.BattlePhase, -1);
		AssertEq(_state.SceneStack.Count, 1);
		AssertEq(_state.CurrentScene, "Menu");
		AssertEq(_state.Diagnostics.Count, 0);
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
}
