using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Database;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

partial class TestRm2kDatabase : TestBase
{
	public void Test_DatabaseRoundTripPreservesCoreValues()
	{
		var database = new Rm2kDatabaseModel
		{
			GameTitle = "Synthetic Test",
			EncounterStep = 25,
			BattleFormat = 1,
		};
		database.PartyMembers.Add(1);
		database.PartyMembers.Add(2);

		var actor = new Rm2kDatabaseModel.Actor { Id = 1, Name = "Hero" };
		actor.Growth.AddRange(new[] { 100, 20, 10 });
		database.Actors.Add(actor);

		var state = new Rm2kDatabaseModel.State { Id = 2, Name = "Poison", Permanent = true };
		state.Phases.AddRange(new[] { 1, 2 });
		database.States.Add(state);

		var enemy = new Rm2kDatabaseModel.Enemy { Id = 3, Name = "Slime", MaxHp = 50 };
		enemy.Actions.Add(new Dictionary<string, object> { { "kind", 1 } });
		database.Enemies.Add(enemy);

		var restored = new Rm2kDatabaseModel();
		restored.FromDict(database.ToDict());

		AssertEq(restored.GameTitle, "Synthetic Test");
		AssertIntSeq(restored.PartyMembers, new[] { 1, 2 });
		AssertEq(restored.EncounterStep, 25);
		AssertEq(restored.BattleFormat, 1);
		AssertEq(restored.GetActor(1)!.Name, "Hero");
		AssertIntSeq(restored.GetActor(1)!.Growth, new[] { 100, 20, 10 });
		AssertEq(restored.GetState(2)!.Name, "Poison");
		AssertIntSeq(restored.GetState(2)!.Phases, new[] { 1, 2 });
		AssertTrue(restored.GetState(2)!.Permanent);
		AssertEq(restored.GetEnemy(3)!.Name, "Slime");
		AssertEq(restored.GetEnemy(3)!.MaxHp, 50);
		AssertEq(restored.GetEnemy(3)!.Actions.Count, 1);
	}

	public void Test_InvalidDatabaseIdsReturnNull()
	{
		var database = new Rm2kDatabaseModel();
		AssertEq(database.GetActor(0), null);
		AssertEq(database.GetActor(1), null);
		AssertEq(database.GetEnemy(-1), null);
	}

	public void Test_SparseDatabaseIdsResolveById()
	{
		var database = new Rm2kDatabaseModel();
		var actor = new Rm2kDatabaseModel.Actor { Id = 42, Name = "Sparse" };
		database.Actors.Add(actor);
		AssertEq(database.GetActor(42)!.Name, "Sparse");
		AssertEq(database.GetActor(1), null);
	}

	public void Test_ToDictSerializesEveryDatabaseCollection()
	{
		var database = new Rm2kDatabaseModel();
		database.Items.Add(new Rm2kDatabaseModel.Item());
		database.Skills.Add(new Rm2kDatabaseModel.Skill());
		database.States.Add(new Rm2kDatabaseModel.State());
		database.Classes.Add(new Rm2kDatabaseModel.Class());
		database.Weapons.Add(new Rm2kDatabaseModel.Item());
		database.Armors.Add(new Rm2kDatabaseModel.Item());
		database.Enemies.Add(new Rm2kDatabaseModel.Enemy());
		database.BattleAnimations.Add(new Rm2kDatabaseModel.BattleAnimation());
		database.Troopers.Add(new Rm2kDatabaseModel.Trooper());

		var serialized = database.ToDict();
		AssertEq(((List<object>)serialized["items"]).Count, 1);
		AssertEq(((List<object>)serialized["skills"]).Count, 1);
		AssertEq(((List<object>)serialized["states"]).Count, 1);
		AssertEq(((List<object>)serialized["classes"]).Count, 1);
		AssertEq(((List<object>)serialized["weapons"]).Count, 1);
		AssertEq(((List<object>)serialized["armors"]).Count, 1);
		AssertEq(((List<object>)serialized["enemies"]).Count, 1);
		AssertEq(((List<object>)serialized["battle_animations"]).Count, 1);
		AssertEq(((List<object>)serialized["troopers"]).Count, 1);
	}

	private void AssertIntSeq(List<int> pActual, int[] pExpected)
	{
		AssertTrue(pActual.SequenceEqual(pExpected),
			$"Expected [{string.Join(",", pExpected)}], got [{string.Join(",", pActual)}]");
	}
}
