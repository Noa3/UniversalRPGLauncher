## tests/core/test_rm2k_database.gd
## Serialization regression tests for the RM2K/2003 database model.

extends Test


func test_database_round_trip_preserves_core_values() -> void:
	var database := RM2KDatabase.new()
	database.game_title = "Synthetic Test"
	database.party_members.assign([1, 2])
	database.encounter_step = 25
	database.battle_format = 1

	var actor := RM2KDatabase.Actor.new()
	actor.id = 1
	actor.name = "Hero"
	actor.growth.assign([100, 20, 10])
	database.actors.append(actor)

	var state := RM2KDatabase.State.new()
	state.id = 2
	state.name = "Poison"
	state.phases.assign([1, 2])
	state.permanent = true
	database.states.append(state)

	var enemy := RM2KDatabase.Enemy.new()
	enemy.id = 3
	enemy.name = "Slime"
	enemy.max_hp = 50
	enemy.actions.append({"kind": 1})
	database.enemies.append(enemy)

	var restored := RM2KDatabase.new()
	restored.from_dict(database.to_dict())

	assert_eq(restored.game_title, "Synthetic Test")
	assert_eq(restored.party_members, [1, 2])
	assert_eq(restored.encounter_step, 25)
	assert_eq(restored.battle_format, 1)
	assert_eq(restored.get_actor(1).name, "Hero")
	assert_eq(restored.get_actor(1).growth, [100, 20, 10])
	assert_eq(restored.get_state(2).name, "Poison")
	assert_eq(restored.get_state(2).phases, [1, 2])
	assert_true(restored.get_state(2).permanent)
	assert_eq(restored.get_enemy(3).name, "Slime")
	assert_eq(restored.get_enemy(3).max_hp, 50)
	assert_eq(restored.get_enemy(3).actions.size(), 1)


func test_invalid_database_ids_return_null() -> void:
	var database := RM2KDatabase.new()
	assert_eq(database.get_actor(0), null)
	assert_eq(database.get_actor(1), null)
	assert_eq(database.get_enemy(-1), null)


func test_sparse_database_ids_resolve_by_id() -> void:
	var database := RM2KDatabase.new()
	var actor := RM2KDatabase.Actor.new()
	actor.id = 42
	actor.name = "Sparse"
	database.actors.append(actor)
	assert_eq(database.get_actor(42).name, "Sparse")
	assert_eq(database.get_actor(1), null)


func test_to_dict_serializes_every_database_collection() -> void:
	var database := RM2KDatabase.new()
	database.items.append(RM2KDatabase.Item.new())
	database.skills.append(RM2KDatabase.Skill.new())
	database.states.append(RM2KDatabase.State.new())
	database.classes.append(RM2KDatabase.Class.new())
	database.weapons.append(RM2KDatabase.Item.new())
	database.armors.append(RM2KDatabase.Item.new())
	database.enemies.append(RM2KDatabase.Enemy.new())
	database.battle_animations.append(RM2KDatabase.BattleAnimation.new())
	database.troopers.append(RM2KDatabase.Trooper.new())

	var serialized := database.to_dict()
	assert_eq(serialized["items"].size(), 1)
	assert_eq(serialized["skills"].size(), 1)
	assert_eq(serialized["states"].size(), 1)
	assert_eq(serialized["classes"].size(), 1)
	assert_eq(serialized["weapons"].size(), 1)
	assert_eq(serialized["armors"].size(), 1)
	assert_eq(serialized["enemies"].size(), 1)
	assert_eq(serialized["battle_animations"].size(), 1)
	assert_eq(serialized["troopers"].size(), 1)
