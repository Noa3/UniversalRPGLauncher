using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Database;

/// <summary>
/// Serializable data model for RPG Maker 2000/2003 database content.
/// This is deliberately separate from the LCF parser and gameplay runtime.
/// </summary>
public class Rm2kDatabaseModel
{
	public class Actor
	{
		public int Id;
		public string Name = "";
		public int ClassId;
		public int InitialLevel = 1;
		public int MaxLevel = 99;
		public List<int> Growth = new();
		public List<int> Weapons = new();
		public List<int> Armors = new();
		public string Nickname = "";

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "id", Id }, { "name", Name }, { "class_id", ClassId },
				{ "initial_level", InitialLevel }, { "max_level", MaxLevel },
				{ "growth", Growth }, { "weapons", Weapons }, { "armors", Armors },
				{ "nickname", Nickname },
			};
		}

		public void FromDict(Dictionary<string, object> pDict)
		{
			Id = Rm2kMap.GetInt(pDict, "id");
			Name = Rm2kMap.GetString(pDict, "name");
			ClassId = Rm2kMap.GetInt(pDict, "class_id");
			InitialLevel = Rm2kMap.GetInt(pDict, "initial_level", 1);
			MaxLevel = Rm2kMap.GetInt(pDict, "max_level", 99);
			CopyInts(Growth, pDict, "growth");
			CopyInts(Weapons, pDict, "weapons");
			CopyInts(Armors, pDict, "armors");
			Nickname = Rm2kMap.GetString(pDict, "nickname");
		}
	}

	public class Item
	{
		public enum ItemType
		{
			Weapon,
			Armor,
			Consumable,
		}

		public int Id;
		public string Name = "";
		public string Description = "";
		public int IconIndex;
		public ItemType ItemKind = ItemType.Consumable;
		public int Price;
		public int AnimationId;
		public int DatabaseId;
		public bool KeyItem;
		public bool Unrestricted;
		public int RemoveType;
		public int WeaponTypeId;
		public int ArmorTypeId;
		public int WeaponHitType;
		public int ArmorMoveType;

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "id", Id }, { "name", Name }, { "description", Description },
				{ "icon_index", IconIndex }, { "item_type", (int)ItemKind }, { "price", Price },
				{ "animation_id", AnimationId }, { "database_id", DatabaseId },
				{ "key_item", KeyItem }, { "unrestricted", Unrestricted },
				{ "remove_type", RemoveType }, { "weapon_type_id", WeaponTypeId },
				{ "armor_type_id", ArmorTypeId }, { "weapon_hit_type", WeaponHitType },
				{ "armor_move_type", ArmorMoveType },
			};
		}

		public void FromDict(Dictionary<string, object> pDict)
		{
			Id = Rm2kMap.GetInt(pDict, "id");
			Name = Rm2kMap.GetString(pDict, "name");
			Description = Rm2kMap.GetString(pDict, "description");
			IconIndex = Rm2kMap.GetInt(pDict, "icon_index");
			ItemKind = (ItemType)Rm2kMap.GetInt(pDict, "item_type", (int)ItemType.Consumable);
			Price = Rm2kMap.GetInt(pDict, "price");
			AnimationId = Rm2kMap.GetInt(pDict, "animation_id");
			DatabaseId = Rm2kMap.GetInt(pDict, "database_id");
			KeyItem = Rm2kMap.GetBool(pDict, "key_item");
			Unrestricted = Rm2kMap.GetBool(pDict, "unrestricted");
			RemoveType = Rm2kMap.GetInt(pDict, "remove_type");
			WeaponTypeId = Rm2kMap.GetInt(pDict, "weapon_type_id");
			ArmorTypeId = Rm2kMap.GetInt(pDict, "armor_type_id");
			WeaponHitType = Rm2kMap.GetInt(pDict, "weapon_hit_type");
			ArmorMoveType = Rm2kMap.GetInt(pDict, "armor_move_type");
		}
	}

	public class Skill
	{
		public int Id;
		public string Name = "";
		public string Description = "";
		public int IconIndex;
		public int CostType;
		public int CostValue;
		public int AnimationId;
		public int Scope;
		public List<string> Message1 = new();
		public List<string> Message2 = new();

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "id", Id }, { "name", Name }, { "description", Description },
				{ "icon_index", IconIndex }, { "cost_type", CostType },
				{ "cost_value", CostValue }, { "animation_id", AnimationId },
				{ "scope", Scope }, { "message1", Message1 }, { "message2", Message2 },
			};
		}

		public void FromDict(Dictionary<string, object> pDict)
		{
			Id = Rm2kMap.GetInt(pDict, "id");
			Name = Rm2kMap.GetString(pDict, "name");
			Description = Rm2kMap.GetString(pDict, "description");
			IconIndex = Rm2kMap.GetInt(pDict, "icon_index");
			CostType = Rm2kMap.GetInt(pDict, "cost_type");
			CostValue = Rm2kMap.GetInt(pDict, "cost_value");
			AnimationId = Rm2kMap.GetInt(pDict, "animation_id");
			Scope = Rm2kMap.GetInt(pDict, "scope");
			CopyStrings(Message1, pDict, "message1");
			CopyStrings(Message2, pDict, "message2");
		}
	}

	public class State
	{
		public int Id;
		public string Name = "";
		public string Description = "";
		public int IconIndex;
		public int RemovalCondition;
		public bool RemoveByRestriction;
		public int AutoRemovalTiming;
		public int MaxStates = 1;
		public List<int> Phases = new();
		public int Restriction;
		public bool RidingHorse;
		public bool Permanent;
		public bool KillWhenReduced;
		public string OverlayBitmap = "";
		public string SubwindowBitmap = "";

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "id", Id }, { "name", Name }, { "description", Description },
				{ "icon_index", IconIndex }, { "removal_condition", RemovalCondition },
				{ "remove_by_restriction", RemoveByRestriction },
				{ "auto_removal_timing", AutoRemovalTiming },
				{ "max_states", MaxStates }, { "phases", Phases }, { "restriction", Restriction },
				{ "riding_horse", RidingHorse }, { "permanent", Permanent },
				{ "kill_when_reduced", KillWhenReduced },
				{ "overlay_bitmap", OverlayBitmap }, { "subwindow_bitmap", SubwindowBitmap },
			};
		}

		public void FromDict(Dictionary<string, object> pDict)
		{
			Id = Rm2kMap.GetInt(pDict, "id");
			Name = Rm2kMap.GetString(pDict, "name");
			Description = Rm2kMap.GetString(pDict, "description");
			IconIndex = Rm2kMap.GetInt(pDict, "icon_index");
			RemovalCondition = Rm2kMap.GetInt(pDict, "removal_condition");
			RemoveByRestriction = Rm2kMap.GetBool(pDict, "remove_by_restriction");
			AutoRemovalTiming = Rm2kMap.GetInt(pDict, "auto_removal_timing");
			MaxStates = Rm2kMap.GetInt(pDict, "max_states", 1);
			CopyInts(Phases, pDict, "phases");
			Restriction = Rm2kMap.GetInt(pDict, "restriction");
			RidingHorse = Rm2kMap.GetBool(pDict, "riding_horse");
			Permanent = Rm2kMap.GetBool(pDict, "permanent");
			KillWhenReduced = Rm2kMap.GetBool(pDict, "kill_when_reduced");
			OverlayBitmap = Rm2kMap.GetString(pDict, "overlay_bitmap");
			SubwindowBitmap = Rm2kMap.GetString(pDict, "subwindow_bitmap");
		}
	}

	public class Class
	{
		public int Id;
		public string Name = "";

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object> { { "id", Id }, { "name", Name } };
		}

		public void FromDict(Dictionary<string, object> pDict)
		{
			Id = Rm2kMap.GetInt(pDict, "id");
			Name = Rm2kMap.GetString(pDict, "name");
		}
	}

	public class Enemy
	{
		public int Id;
		public string Name = "";
		public string BattlerName = "";
		public int BattlerHue;
		public int MaxHp;
		public int MaxMp;
		public int Attack;
		public int Defense;
		public int MagicDefense;
		public int Agility;
		public int Luck;
		public int Exp;
		public int Gold;
		public int Mwp;
		public int DropItemId;
		public int DropItemWeight;
		public List<Dictionary<string, object>> Actions = new();

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "id", Id }, { "name", Name }, { "battler_name", BattlerName },
				{ "battler_hue", BattlerHue }, { "max_hp", MaxHp }, { "max_mp", MaxMp },
				{ "attack", Attack }, { "defense", Defense }, { "magic_defense", MagicDefense },
				{ "agility", Agility }, { "luck", Luck }, { "exp", Exp }, { "gold", Gold }, { "mwp", Mwp },
				{ "drop_item_id", DropItemId }, { "drop_item_weight", DropItemWeight },
				{ "actions", Actions },
			};
		}

		public void FromDict(Dictionary<string, object> pDict)
		{
			Id = Rm2kMap.GetInt(pDict, "id");
			Name = Rm2kMap.GetString(pDict, "name");
			BattlerName = Rm2kMap.GetString(pDict, "battler_name");
			BattlerHue = Rm2kMap.GetInt(pDict, "battler_hue");
			MaxHp = Rm2kMap.GetInt(pDict, "max_hp");
			MaxMp = Rm2kMap.GetInt(pDict, "max_mp");
			Attack = Rm2kMap.GetInt(pDict, "attack");
			Defense = Rm2kMap.GetInt(pDict, "defense");
			MagicDefense = Rm2kMap.GetInt(pDict, "magic_defense");
			Agility = Rm2kMap.GetInt(pDict, "agility");
			Luck = Rm2kMap.GetInt(pDict, "luck");
			Exp = Rm2kMap.GetInt(pDict, "exp");
			Gold = Rm2kMap.GetInt(pDict, "gold");
			Mwp = Rm2kMap.GetInt(pDict, "mwp");
			DropItemId = Rm2kMap.GetInt(pDict, "drop_item_id");
			DropItemWeight = Rm2kMap.GetInt(pDict, "drop_item_weight");
			Actions = new List<Dictionary<string, object>>();
			if (pDict.TryGetValue("actions", out var actionsValue) && actionsValue is IEnumerable<object> actionItems)
			{
				foreach (var item in actionItems)
				{
					if (item is Dictionary<string, object> action)
					{
						Actions.Add(action);
					}
				}
			}
		}
	}

	public class BattleAnimation
	{
		public int Id;
		public string Name = "";
		public List<Dictionary<string, object>> AnimationData = new();

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "id", Id }, { "name", Name }, { "animation_data", AnimationData },
			};
		}

		public void FromDict(Dictionary<string, object> pDict)
		{
			Id = Rm2kMap.GetInt(pDict, "id");
			Name = Rm2kMap.GetString(pDict, "name");
			AnimationData = new List<Dictionary<string, object>>();
			if (pDict.TryGetValue("animation_data", out var dataValue) && dataValue is IEnumerable<object> dataItems)
			{
				foreach (var item in dataItems)
				{
					if (item is Dictionary<string, object> entry)
					{
						AnimationData.Add(entry);
					}
				}
			}
		}
	}

	public class Trooper
	{
		public int Id;
		public string Name = "";
		public string BattlerName = "";
		public int BattlerHue;

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "id", Id }, { "name", Name }, { "battler_name", BattlerName }, { "battler_hue", BattlerHue },
			};
		}

		public void FromDict(Dictionary<string, object> pDict)
		{
			Id = Rm2kMap.GetInt(pDict, "id");
			Name = Rm2kMap.GetString(pDict, "name");
			BattlerName = Rm2kMap.GetString(pDict, "battler_name");
			BattlerHue = Rm2kMap.GetInt(pDict, "battler_hue");
		}
	}

	public List<Actor> Actors { get; } = new();
	public List<Item> Items { get; } = new();
	public List<Skill> Skills { get; } = new();
	public List<State> States { get; } = new();
	public List<Class> Classes { get; } = new();
	public List<Item> Weapons { get; } = new();
	public List<Item> Armors { get; } = new();
	public List<Enemy> Enemies { get; } = new();
	public List<BattleAnimation> BattleAnimations { get; } = new();
	public List<Trooper> Troopers { get; } = new();

	public int AnimationCount;
	public List<string> TilesetData = new();
	public string BattlebackName = "";
	public string BattlebackBackName = "";
	public Dictionary<string, object> Music = new();
	public Dictionary<string, object> SystemSe = new();
	public string GameTitle = "";
	public List<int> PartyMembers = new();
	public int StartingMemberIndex;
	public bool TestBattle;
	public int EncounterStep;
	public bool EncounterEnabled;
	public bool EncounterHalfStep;
	public bool EncounterDoubleStep;
	public int BattleFormat;

	public Actor GetActor(int pId)
	{
		foreach (var value in Actors)
		{
			if (value.Id == pId)
			{
				return value;
			}
		}
		return null;
	}

	public Item GetItem(int pId)
	{
		foreach (var value in Items)
		{
			if (value.Id == pId)
			{
				return value;
			}
		}
		return null;
	}

	public Skill GetSkill(int pId)
	{
		foreach (var value in Skills)
		{
			if (value.Id == pId)
			{
				return value;
			}
		}
		return null;
	}

	public State GetState(int pId)
	{
		foreach (var value in States)
		{
			if (value.Id == pId)
			{
				return value;
			}
		}
		return null;
	}

	public Enemy GetEnemy(int pId)
	{
		foreach (var value in Enemies)
		{
			if (value.Id == pId)
			{
				return value;
			}
		}
		return null;
	}

	public Trooper GetTrooper(int pId)
	{
		foreach (var value in Troopers)
		{
			if (value.Id == pId)
			{
				return value;
			}
		}
		return null;
	}

	public Dictionary<string, object> ToDict()
	{
		var serializedActors = new List<Dictionary<string, object>>();
		foreach (var actor in Actors)
		{
			serializedActors.Add(actor.ToDict());
		}
		var serializedItems = new List<Dictionary<string, object>>();
		foreach (var item in Items)
		{
			serializedItems.Add(item.ToDict());
		}
		var serializedSkills = new List<Dictionary<string, object>>();
		foreach (var skill in Skills)
		{
			serializedSkills.Add(skill.ToDict());
		}
		var serializedStates = new List<Dictionary<string, object>>();
		foreach (var state in States)
		{
			serializedStates.Add(state.ToDict());
		}
		var serializedClasses = new List<Dictionary<string, object>>();
		foreach (var cls in Classes)
		{
			serializedClasses.Add(cls.ToDict());
		}
		var serializedWeapons = new List<Dictionary<string, object>>();
		foreach (var weapon in Weapons)
		{
			serializedWeapons.Add(weapon.ToDict());
		}
		var serializedArmors = new List<Dictionary<string, object>>();
		foreach (var armor in Armors)
		{
			serializedArmors.Add(armor.ToDict());
		}
		var serializedEnemies = new List<Dictionary<string, object>>();
		foreach (var enemy in Enemies)
		{
			serializedEnemies.Add(enemy.ToDict());
		}
		var serializedBattleAnimations = new List<Dictionary<string, object>>();
		foreach (var animation in BattleAnimations)
		{
			serializedBattleAnimations.Add(animation.ToDict());
		}
		var serializedTroopers = new List<Dictionary<string, object>>();
		foreach (var trooper in Troopers)
		{
			serializedTroopers.Add(trooper.ToDict());
		}

		return new Dictionary<string, object>
		{
			{ "game_title", GameTitle },
			{ "party_members", PartyMembers },
			{ "starting_member_index", StartingMemberIndex },
			{ "test_battle", TestBattle },
			{ "encounter_step", EncounterStep },
			{ "encounter_enabled", EncounterEnabled },
			{ "encounter_half_step", EncounterHalfStep },
			{ "encounter_double_step", EncounterDoubleStep },
			{ "battle_format", BattleFormat },
			{ "actors", serializedActors },
			{ "items", serializedItems },
			{ "skills", serializedSkills },
			{ "states", serializedStates },
			{ "classes", serializedClasses },
			{ "weapons", serializedWeapons },
			{ "armors", serializedArmors },
			{ "enemies", serializedEnemies },
			{ "battle_animations", serializedBattleAnimations },
			{ "troopers", serializedTroopers },
			{ "animation_count", AnimationCount },
			{ "tileset_data", TilesetData },
			{ "battleback_name", BattlebackName },
			{ "battleback_back_name", BattlebackBackName },
			{ "music", Music },
			{ "system_se", SystemSe },
		};
	}

	public void FromDict(Dictionary<string, object> pDict)
	{
		GameTitle = Rm2kMap.GetString(pDict, "game_title");
		PartyMembers = new List<int>();
		if (pDict.TryGetValue("party_members", out var partyValue) && partyValue is IEnumerable<object> partyItems)
		{
			foreach (var item in partyItems)
			{
				PartyMembers.Add(System.Convert.ToInt32(item));
			}
		}
		StartingMemberIndex = Rm2kMap.GetInt(pDict, "starting_member_index");
		TestBattle = Rm2kMap.GetBool(pDict, "test_battle");
		EncounterStep = Rm2kMap.GetInt(pDict, "encounter_step");
		EncounterEnabled = Rm2kMap.GetBool(pDict, "encounter_enabled");
		EncounterHalfStep = Rm2kMap.GetBool(pDict, "encounter_half_step");
		EncounterDoubleStep = Rm2kMap.GetBool(pDict, "encounter_double_step");
		BattleFormat = Rm2kMap.GetInt(pDict, "battle_format");

		Actors.Clear();
		FillList(Actors, pDict, "actors", () => new Actor());
		Items.Clear();
		FillList(Items, pDict, "items", () => new Item());
		Skills.Clear();
		FillList(Skills, pDict, "skills", () => new Skill());
		States.Clear();
		FillList(States, pDict, "states", () => new State());
		Classes.Clear();
		FillList(Classes, pDict, "classes", () => new Class());
		Weapons.Clear();
		FillList(Weapons, pDict, "weapons", () => new Item());
		Armors.Clear();
		FillList(Armors, pDict, "armors", () => new Item());
		Enemies.Clear();
		FillList(Enemies, pDict, "enemies", () => new Enemy());
		BattleAnimations.Clear();
		FillList(BattleAnimations, pDict, "battle_animations", () => new BattleAnimation());
		Troopers.Clear();
		FillList(Troopers, pDict, "troopers", () => new Trooper());

		AnimationCount = Rm2kMap.GetInt(pDict, "animation_count");
		TilesetData = new List<string>();
		if (pDict.TryGetValue("tileset_data", out var tilesetValue) && tilesetValue is IEnumerable<object> tilesetItems)
		{
			foreach (var item in tilesetItems)
			{
				TilesetData.Add(item?.ToString() ?? "");
			}
		}
		BattlebackName = Rm2kMap.GetString(pDict, "battleback_name");
		BattlebackBackName = Rm2kMap.GetString(pDict, "battleback_back_name");
		Music = CopyDict(pDict, "music");
		SystemSe = CopyDict(pDict, "system_se");
	}

	private static void FillList<T>(List<T> pTarget, Dictionary<string, object> pDict, string pKey,
		System.Func<T> pFactory) where T : class
	{
		if (!pDict.TryGetValue(pKey, out var listValue) || listValue is not IEnumerable<object> items)
		{
			return;
		}
		foreach (var item in items)
		{
			if (item is not Dictionary<string, object> data)
			{
				continue;
			}
			var value = pFactory();
			switch (value)
			{
				case Actor actor: actor.FromDict(data); break;
				case Item itemEntry: itemEntry.FromDict(data); break;
				case Skill skill: skill.FromDict(data); break;
				case State state: state.FromDict(data); break;
				case Class cls: cls.FromDict(data); break;
				case Enemy enemy: enemy.FromDict(data); break;
				case BattleAnimation animation: animation.FromDict(data); break;
				case Trooper trooper: trooper.FromDict(data); break;
			}
			pTarget.Add(value);
		}
	}

	private static void CopyInts(List<int> pTarget, Dictionary<string, object> pDict, string pKey)
	{
		pTarget.Clear();
		if (!pDict.TryGetValue(pKey, out var value) || value is not IEnumerable<object> items)
		{
			return;
		}
		foreach (var item in items)
		{
			pTarget.Add(System.Convert.ToInt32(item));
		}
	}

	private static void CopyStrings(List<string> pTarget, Dictionary<string, object> pDict, string pKey)
	{
		pTarget.Clear();
		if (!pDict.TryGetValue(pKey, out var value) || value is not IEnumerable<object> items)
		{
			return;
		}
		foreach (var item in items)
		{
			pTarget.Add(item?.ToString() ?? "");
		}
	}

	private static Dictionary<string, object> CopyDict(Dictionary<string, object> pDict, string pKey)
	{
		if (pDict.TryGetValue(pKey, out var value) && value is Dictionary<string, object> dict)
		{
			return new Dictionary<string, object>(dict);
		}
		return new Dictionary<string, object>();
	}
}
