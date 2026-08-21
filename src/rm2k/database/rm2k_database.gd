## src/rm2k/database/rm2k_database.gd
##
## Serializable data model for RPG Maker 2000/2003 database content.
## This is deliberately separate from the LCF parser and gameplay runtime.

class_name RM2KDatabase
extends RefCounted


class Actor:
	var id: int = 0
	var name: String = ""
	var class_id: int = 0
	var initial_level: int = 1
	var max_level: int = 99
	var growth: Array[int] = []
	var weapons: Array[int] = []
	var armors: Array[int] = []
	var nickname: String = ""

	func to_dict() -> Dictionary:
		return {
			id = id, name = name, class_id = class_id,
			initial_level = initial_level, max_level = max_level,
			growth = growth, weapons = weapons, armors = armors,
			nickname = nickname,
		}

	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")
		class_id = p_dict.get("class_id", 0)
		initial_level = p_dict.get("initial_level", 1)
		max_level = p_dict.get("max_level", 99)
		growth.assign(p_dict.get("growth", []))
		weapons.assign(p_dict.get("weapons", []))
		armors.assign(p_dict.get("armors", []))
		nickname = p_dict.get("nickname", "")


class Item:
	enum ItemType { WEAPON, ARMOR, CONSUMABLE }

	var id: int = 0
	var name: String = ""
	var description: String = ""
	var icon_index: int = 0
	var item_type: ItemType = ItemType.CONSUMABLE
	var price: int = 0
	var animation_id: int = 0
	var database_id: int = 0
	var key_item: bool = false
	var unrestricted: bool = false
	var remove_type: int = 0
	var weapon_type_id: int = 0
	var armor_type_id: int = 0
	var weapon_hit_type: int = 0
	var armor_move_type: int = 0

	func to_dict() -> Dictionary:
		return {
			id = id, name = name, description = description,
			icon_index = icon_index, item_type = item_type, price = price,
			animation_id = animation_id, database_id = database_id,
			key_item = key_item, unrestricted = unrestricted,
			remove_type = remove_type, weapon_type_id = weapon_type_id,
			armor_type_id = armor_type_id, weapon_hit_type = weapon_hit_type,
			armor_move_type = armor_move_type,
		}

	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")
		description = p_dict.get("description", "")
		icon_index = p_dict.get("icon_index", 0)
		item_type = p_dict.get("item_type", ItemType.CONSUMABLE)
		price = p_dict.get("price", 0)
		animation_id = p_dict.get("animation_id", 0)
		database_id = p_dict.get("database_id", 0)
		key_item = p_dict.get("key_item", false)
		unrestricted = p_dict.get("unrestricted", false)
		remove_type = p_dict.get("remove_type", 0)
		weapon_type_id = p_dict.get("weapon_type_id", 0)
		armor_type_id = p_dict.get("armor_type_id", 0)
		weapon_hit_type = p_dict.get("weapon_hit_type", 0)
		armor_move_type = p_dict.get("armor_move_type", 0)


class Skill:
	var id: int = 0
	var name: String = ""
	var description: String = ""
	var icon_index: int = 0
	var cost_type: int = 0
	var cost_value: int = 0
	var animation_id: int = 0
	var scope: int = 0
	var message1: Array[String] = []
	var message2: Array[String] = []

	func to_dict() -> Dictionary:
		return {
			id = id, name = name, description = description,
			icon_index = icon_index, cost_type = cost_type,
			cost_value = cost_value, animation_id = animation_id,
			scope = scope, message1 = message1, message2 = message2,
		}

	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")
		description = p_dict.get("description", "")
		icon_index = p_dict.get("icon_index", 0)
		cost_type = p_dict.get("cost_type", 0)
		cost_value = p_dict.get("cost_value", 0)
		animation_id = p_dict.get("animation_id", 0)
		scope = p_dict.get("scope", 0)
		message1.assign(p_dict.get("message1", []))
		message2.assign(p_dict.get("message2", []))


class State:
	var id: int = 0
	var name: String = ""
	var description: String = ""
	var icon_index: int = 0
	var removal_condition: int = 0
	var remove_by_restriction: bool = false
	var auto_removal_timing: int = 0
	var max_states: int = 1
	var phases: Array[int] = []
	var restriction: int = 0
	var riding_horse: bool = false
	var permanent: bool = false
	var kill_when_reduced: bool = false
	var overlay_bitmap: String = ""
	var subwindow_bitmap: String = ""

	func to_dict() -> Dictionary:
		return {
			id = id, name = name, description = description,
			icon_index = icon_index, removal_condition = removal_condition,
			remove_by_restriction = remove_by_restriction,
			auto_removal_timing = auto_removal_timing,
			max_states = max_states, phases = phases, restriction = restriction,
			riding_horse = riding_horse, permanent = permanent,
			kill_when_reduced = kill_when_reduced,
			overlay_bitmap = overlay_bitmap, subwindow_bitmap = subwindow_bitmap,
		}

	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")
		description = p_dict.get("description", "")
		icon_index = p_dict.get("icon_index", 0)
		removal_condition = p_dict.get("removal_condition", 0)
		remove_by_restriction = p_dict.get("remove_by_restriction", false)
		auto_removal_timing = p_dict.get("auto_removal_timing", 0)
		max_states = p_dict.get("max_states", 1)
		phases.assign(p_dict.get("phases", []))
		restriction = p_dict.get("restriction", 0)
		riding_horse = p_dict.get("riding_horse", false)
		permanent = p_dict.get("permanent", false)
		kill_when_reduced = p_dict.get("kill_when_reduced", false)
		overlay_bitmap = p_dict.get("overlay_bitmap", "")
		subwindow_bitmap = p_dict.get("subwindow_bitmap", "")


class Class:
	var id: int = 0
	var name: String = ""

	func to_dict() -> Dictionary:
		return {id = id, name = name}

	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")


class Enemy:
	var id: int = 0
	var name: String = ""
	var battler_name: String = ""
	var battler_hue: int = 0
	var max_hp: int = 0
	var max_mp: int = 0
	var attack: int = 0
	var defense: int = 0
	var magic_defense: int = 0
	var agility: int = 0
	var luck: int = 0
	var exp: int = 0
	var gold: int = 0
	var mwp: int = 0
	var drop_item_id: int = 0
	var drop_item_weight: int = 0
	var actions: Array[Dictionary] = []

	func to_dict() -> Dictionary:
		return {
			id = id, name = name, battler_name = battler_name,
			battler_hue = battler_hue, max_hp = max_hp, max_mp = max_mp,
			attack = attack, defense = defense, magic_defense = magic_defense,
			agility = agility, luck = luck, exp = exp, gold = gold, mwp = mwp,
			drop_item_id = drop_item_id, drop_item_weight = drop_item_weight,
			actions = actions,
		}

	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")
		battler_name = p_dict.get("battler_name", "")
		battler_hue = p_dict.get("battler_hue", 0)
		max_hp = p_dict.get("max_hp", 0)
		max_mp = p_dict.get("max_mp", 0)
		attack = p_dict.get("attack", 0)
		defense = p_dict.get("defense", 0)
		magic_defense = p_dict.get("magic_defense", 0)
		agility = p_dict.get("agility", 0)
		luck = p_dict.get("luck", 0)
		exp = p_dict.get("exp", 0)
		gold = p_dict.get("gold", 0)
		mwp = p_dict.get("mwp", 0)
		drop_item_id = p_dict.get("drop_item_id", 0)
		drop_item_weight = p_dict.get("drop_item_weight", 0)
		actions.assign(p_dict.get("actions", []))


class BattleAnimation:
	var id: int = 0
	var name: String = ""
	var animation_data: Array[Dictionary] = []

	func to_dict() -> Dictionary:
		return {id = id, name = name, animation_data = animation_data}

	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")
		animation_data.assign(p_dict.get("animation_data", []))


class Trooper:
	var id: int = 0
	var name: String = ""
	var battler_name: String = ""
	var battler_hue: int = 0

	func to_dict() -> Dictionary:
		return {id = id, name = name, battler_name = battler_name, battler_hue = battler_hue}

	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")
		battler_name = p_dict.get("battler_name", "")
		battler_hue = p_dict.get("battler_hue", 0)


var actors: Array[Actor] = []
var items: Array[Item] = []
var skills: Array[Skill] = []
var states: Array[State] = []
var classes: Array[Class] = []
var weapons: Array[Item] = []
var armors: Array[Item] = []
var enemies: Array[Enemy] = []
var battle_animations: Array[BattleAnimation] = []
var troopers: Array[Trooper] = []

var animation_count: int = 0
var tileset_data: Array[String] = []
var battleback_name: String = ""
var battleback_back_name: String = ""
var music: Dictionary = {}
var system_se: Dictionary = {}
var game_title: String = ""
var party_members: Array[int] = []
var starting_member_index: int = 0
var test_battle: bool = false
var encounter_step: int = 0
var encounter_enabled: bool = false
var encounter_half_step: bool = false
var encounter_double_step: bool = false
var battle_format: int = 0


func get_actor(p_id: int) -> Actor:
	for value in actors:
		if value.id == p_id:
			return value
	return null


func get_item(p_id: int) -> Item:
	for value in items:
		if value.id == p_id:
			return value
	return null


func get_skill(p_id: int) -> Skill:
	for value in skills:
		if value.id == p_id:
			return value
	return null


func get_state(p_id: int) -> State:
	for value in states:
		if value.id == p_id:
			return value
	return null


func get_enemy(p_id: int) -> Enemy:
	for value in enemies:
		if value.id == p_id:
			return value
	return null


func get_trooper(p_id: int) -> Trooper:
	for value in troopers:
		if value.id == p_id:
			return value
	return null


func to_dict() -> Dictionary:
	var serialized_actors: Array[Dictionary] = []
	for actor in actors:
		serialized_actors.append(actor.to_dict())
	var serialized_items: Array[Dictionary] = []
	for item in items:
		serialized_items.append(item.to_dict())
	var serialized_skills: Array[Dictionary] = []
	for skill in skills:
		serialized_skills.append(skill.to_dict())
	var serialized_states: Array[Dictionary] = []
	for state in states:
		serialized_states.append(state.to_dict())
	var serialized_classes: Array[Dictionary] = []
	for cls in classes:
		serialized_classes.append(cls.to_dict())
	var serialized_weapons: Array[Dictionary] = []
	for weapon in weapons:
		serialized_weapons.append(weapon.to_dict())
	var serialized_armors: Array[Dictionary] = []
	for armor in armors:
		serialized_armors.append(armor.to_dict())
	var serialized_enemies: Array[Dictionary] = []
	for enemy in enemies:
		serialized_enemies.append(enemy.to_dict())
	var serialized_battle_animations: Array[Dictionary] = []
	for animation in battle_animations:
		serialized_battle_animations.append(animation.to_dict())
	var serialized_troopers: Array[Dictionary] = []
	for trooper in troopers:
		serialized_troopers.append(trooper.to_dict())

	return {
		game_title = game_title,
		party_members = party_members,
		starting_member_index = starting_member_index,
		test_battle = test_battle,
		encounter_step = encounter_step,
		encounter_enabled = encounter_enabled,
		encounter_half_step = encounter_half_step,
		encounter_double_step = encounter_double_step,
		battle_format = battle_format,
		actors = serialized_actors,
		items = serialized_items,
		skills = serialized_skills,
		states = serialized_states,
		classes = serialized_classes,
		weapons = serialized_weapons,
		armors = serialized_armors,
		enemies = serialized_enemies,
		battle_animations = serialized_battle_animations,
		troopers = serialized_troopers,
		animation_count = animation_count,
		tileset_data = tileset_data,
		battleback_name = battleback_name,
		battleback_back_name = battleback_back_name,
		music = music,
		system_se = system_se,
	}


func from_dict(p_dict: Dictionary) -> void:
	game_title = p_dict.get("game_title", "")
	party_members.assign(p_dict.get("party_members", []))
	starting_member_index = p_dict.get("starting_member_index", 0)
	test_battle = p_dict.get("test_battle", false)
	encounter_step = p_dict.get("encounter_step", 0)
	encounter_enabled = p_dict.get("encounter_enabled", false)
	encounter_half_step = p_dict.get("encounter_half_step", false)
	encounter_double_step = p_dict.get("encounter_double_step", false)
	battle_format = p_dict.get("battle_format", 0)

	actors.clear()
	for data in p_dict.get("actors", []):
		var value := Actor.new()
		value.from_dict(data)
		actors.append(value)
	items.clear()
	for data in p_dict.get("items", []):
		var value := Item.new()
		value.from_dict(data)
		items.append(value)
	skills.clear()
	for data in p_dict.get("skills", []):
		var value := Skill.new()
		value.from_dict(data)
		skills.append(value)
	states.clear()
	for data in p_dict.get("states", []):
		var value := State.new()
		value.from_dict(data)
		states.append(value)
	classes.clear()
	for data in p_dict.get("classes", []):
		var value := Class.new()
		value.from_dict(data)
		classes.append(value)
	weapons.clear()
	for data in p_dict.get("weapons", []):
		var value := Item.new()
		value.from_dict(data)
		weapons.append(value)
	armors.clear()
	for data in p_dict.get("armors", []):
		var value := Item.new()
		value.from_dict(data)
		armors.append(value)
	enemies.clear()
	for data in p_dict.get("enemies", []):
		var value := Enemy.new()
		value.from_dict(data)
		enemies.append(value)
	battle_animations.clear()
	for data in p_dict.get("battle_animations", []):
		var value := BattleAnimation.new()
		value.from_dict(data)
		battle_animations.append(value)
	troopers.clear()
	for data in p_dict.get("troopers", []):
		var value := Trooper.new()
		value.from_dict(data)
		troopers.append(value)

	animation_count = p_dict.get("animation_count", 0)
	tileset_data.assign(p_dict.get("tileset_data", []))
	battleback_name = p_dict.get("battleback_name", "")
	battleback_back_name = p_dict.get("battleback_back_name", "")
	music = p_dict.get("music", {}).duplicate(true)
	system_se = p_dict.get("system_se", {}).duplicate(true)
