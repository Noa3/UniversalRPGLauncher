## class_name RM2KDatabase
## src/rm2k/database/rm2k_database.gd
##
## RPG Maker 2000/2003 database structures.
## Contains all RPG Maker game data: actors, items, skills, states,
## equipment, classes, enemy groups, battle animations, troopers.

extends RefCounted


## Actor data
class Actor:
	var id: int = 0
	var name: String = ""
	var class_id: int = 0
	var initial_level: int = 1
	var max_level: int = 99
	var growth: Array[int] = []  ## HP, MP, STR, DEX, INT, VIT per level
	var weapons: Array[int] = []
	var armors: Array[int] = []
	var nickname: String = ""
	
	func to_dict() -> Dictionary:
		return {
			id = id,
			name = name,
			class_id = class_id,
			initial_level = initial_level,
			max_level = max_level,
			growth = growth,
			weapons = weapons,
			armors = armors,
			nickname = nickname,
		}
	
	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")
		class_id = p_dict.get("class_id", 0)
		initial_level = p_dict.get("initial_level", 1)
		max_level = p_dict.get("max_level", 99)
		growth = p_dict.get("growth", [])
		weapons = p_dict.get("weapons", [])
		armors = p_dict.get("armors", [])
		nickname = p_dict.get("nickname", "")


## Item data
class Item:
	enum ItemType {
		WEAPON,
		ARMOR,
		CONSUMABLE,
	}
	
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
			id = id,
			name = name,
			description = description,
			icon_index = icon_index,
			item_type = item_type,
			price = price,
			animation_id = animation_id,
			database_id = database_id,
			key_item = key_item,
			unrestricted = unrestricted,
		}
	
	func from_dict(p_dict: Dictionary) -> void:
		id = p_dict.get("id", 0)
		name = p_dict.get("name", "")
		description = p_dict.get("description", "")
		icon_index = p_dict.get("icon_index", 0)
		item_type = p_dict.get("item_type", 0)
		price = p_dict.get("price", 0)
		animation_id = p_dict.get("animation_id", 0)
		database_id = p_dict.get("database_id", 0)
		key_item = p_dict.get("key_item", false)
		unrestricted = p_dict.get("unrestricted", false)


## Skill data
class Skill:
	var id: int = 0
	var name: String = ""
	var description: String = ""
	var icon_index: int = 0
	var cost_type: int = 0  ## 0=none, 1=HP, 2=MP
	var cost_value: int = 0
	var animation_id: int = 0
	var scope: int = 0  ## Battle target scope
	var message1: Array[String] = []
	var message2: Array[String] = []
	
	func to_dict() -> Dictionary:
		return {
			id = id,
			name = name,
			description = description,
			icon_index = icon_index,
			cost_type = cost_type,
			cost_value = cost_value,
			animation_id = animation_id,
			scope = scope,
		}


## State data
class State:
	var id: int = 0
	var name: String = ""
	var description: String = ""
	var icon_index: int = 0
	var removal_condition: int = 0
	var remove_by_restriction: bool = false
	var auto_removal_timing: int = 0
	var max_states: int = 1
	phases: Array[int] = []  ## Battle phases where state is active
	restriction: int = 0
	riding_horse: bool = false
	permanent: bool = false
	kill_when_reduced: bool = false
	overlay_bitmap: String = ""
	subwindow_bitmap: String = ""
	
	func to_dict() -> Dictionary:
		return {
			id = id,
			name = name,
			description = description,
			icon_index = icon_index,
			removal_condition = removal_condition,
		}


## Class data
class Class:
	var id: int = 0
	var name: String = ""
	
	func to_dict() -> Dictionary:
		return {id = id, name = name}


## Enemy data
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
			id = id,
			name = name,
			battler_name = battler_name,
			battler_hue = battler_hue,
		}


## Battle animation data
class BattleAnimation:
	var id: int = 0
	var name: String = ""
	var animation_data: Array[Dictionary] = []
	
	func to_dict() -> Dictionary:
		return {id = id, name = name}


## Trooper data (enemy battler graphics)
class Trooper:
	var id: int = 0
	var name: String = ""
	var battler_name: String = ""
	var battler_hue: int = 0
	
	func to_dict() -> Dictionary:
		return {id = id, name = name}


## Main database container
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
var music: Dictionary = {}  ## BGM settings
var system_se: Dictionary = {}  ## System SE settings

var game_title: String = ""
var party_members: Array[int] = []
var starting_member_index: int = 0
var test_battle: bool = false
var encounter_step: int = 0
var encounter_enabled: bool = false
var encounter_half_step: bool = false
var encounter_double_step: bool = false
var battle_format: int = 0  ## 0=traditional, 1=side-view

func get_actor(p_id: int) -> Actor:
	if p_id > 0 and p_id <= actors.size():
		return actors[p_id - 1]
	return null


func get_item(p_id: int) -> Item:
	if p_id > 0 and p_id <= items.size():
		return items[p_id - 1]
	return null


func get_skill(p_id: int) -> Skill:
	if p_id > 0 and p_id <= skills.size():
		return skills[p_id - 1]
	return null


func get_state(p_id: int) -> State:
	if p_id > 0 and p_id <= states.size():
		return states[p_id - 1]
	return null


func get_enemy(p_id: int) -> Enemy:
	if p_id > 0 and p_id <= enemies.size():
		return enemies[p_id - 1]
	return null


func get_trooper(p_id: int) -> Trooper:
	if p_id > 0 and p_id <= troopers.size():
		return troopers[p_id - 1]
	return null


func to_dict() -> Dictionary:
	return {
		game_title = game_title,
		party_members = party_members,
		actors = [actor.to_dict() for actor in actors],
		items = [item.to_dict() for item in items],
		skills = [skill.to_dict() for skill in skills],
		states = [state.to_dict() for state in states],
		classes = [cls.to_dict() for cls in classes],
		weapons = [w.to_dict() for w in weapons],
		armors = [a.to_dict() for a in armors],
		enemies = [e.to_dict() for e in enemies],
		battle_animations = [ba.to_dict() for ba in battle_animations],
		troopers = [t.to_dict() for t in troopers],
		animation_count = animation_count,
		tileset_data = tileset_data,
		battleback_name = battleback_name,
		battleback_back_name = battleback_back_name,
		music = music,
		system_se = system_se,
	}


func from_dict(p_dict: Dictionary) -> void:
	game_title = p_dict.get("game_title", "")
	party_members = p_dict.get("party_members", [])
	
	actors = []
	for actor_data in p_dict.get("actors", []):
		var actor := Actor.new()
		actor.from_dict(actor_data)
		actors.append(actor)
	
	items = []
	for item_data in p_dict.get("items", []):
		var item := Item.new()
		item.from_dict(item_data)
		items.append(item)
	
	skills = []
	for skill_data in p_dict.get("skills", []):
		var skill := Skill.new()
		skill.from_dict(skill_data)
		skills.append(skill)
	
	states = []
	for state_data in p_dict.get("states", []):
		var state := State.new()
		state.from_dict(state_data)
		states.append(state)
	
	classes = []
	for class_data in p_dict.get("classes", []):
		var cls := Class.new()
		cls.from_dict(class_data)
		classes.append(cls)
	
	weapons = []
	for weapon_data in p_dict.get("weapons", []):
		var weapon := Item.new()
		weapon.from_dict(weapon_data)
		weapons.append(weapon)
	
	armors = []
	for armor_data in p_dict.get("armors", []):
		var armor := Item.new()
		armor.from_dict(armor_data)
		armors.append(armor)
	
	enemies = []
	for enemy_data in p_dict.get("enemies", []):
		var enemy := Enemy.new()
		enemy.from_dict(enemy_data)
		enemies.append(enemy)
	
	battle_animations = []
	for ba_data in p_dict.get("battle_animations", []):
		var ba := BattleAnimation.new()
		ba.from_dict(ba_data)
		battle_animations.append(ba)
	
	troopers = []
	for trooper_data in p_dict.get("troopers", []):
		var trooper := Trooper.new()
		trooper.from_dict(trooper_data)
		troopers.append(trooper)
	
	animation_count = p_dict.get("animation_count", 0)
	tileset_data = p_dict.get("tileset_data", [])
	battleback_name = p_dict.get("battleback_name", "")
	battleback_back_name = p_dict.get("battleback_back_name", "")
	music = p_dict.get("music", {})
	system_se = p_dict.get("system_se", {})
