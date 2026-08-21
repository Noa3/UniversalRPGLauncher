using System;
using System.Collections.Generic;
using Godot;

namespace UniversalRPG.Compatibility;

/// <summary>
/// Extensible compatibility profile system for game-specific quirks and fixes.
/// Profiles are loaded from JSON and applied dynamically.
/// </summary>
public partial class CompatibilityProfile : RefCounted
{
	public enum FlagType
	{
		String,
		Boolean,
		Integer,
		Float,
		Array,
		Dictionary,
	}

	/// <summary>Individual compatibility flag.</summary>
	public class CompatFlag
	{
		public string Name;
		public FlagType Type;
		public Variant Value;

		public CompatFlag(string pName, FlagType pType, Variant pValue)
		{
			Name = pName;
			Type = pType;
			Value = pValue;
		}
	}

	/// <summary>Compatibility profile entry.</summary>
	public class ProfileEntry
	{
		public string Id = "";
		public string Sha256 = "";
		public string Engine = "";
		public string GameTitle = "";
		public string Type = "";  // "game_profile", "plugin_profile", "dll_profile", etc.
		public string Compatibility = "";  // "full", "partial", "experimental", "unknown"
		public List<CompatFlag> Flags { get; } = new();
		public string Notes = "";
		public string Replacement = "";

		public bool HasFlag(string pName)
		{
			foreach (var flag in Flags)
			{
				if (flag.Name == pName)
				{
					return true;
				}
			}
			return false;
		}

		public Variant GetFlagValue(string pName)
		{
			foreach (var flag in Flags)
			{
				if (flag.Name == pName)
				{
					return flag.Value;
				}
			}
			return default;
		}
	}

	/// <summary>Compatibility database.</summary>
	public class CompatibilityDatabase
	{
		public List<ProfileEntry> Entries { get; } = new();
		public Dictionary<string, CompatFlag> Flags { get; } = new();  // Global flags

		public void AddEntry(ProfileEntry pEntry)
		{
			Entries.Add(pEntry);
		}

		public ProfileEntry FindBySha256(string pSha256)
		{
			foreach (var entry in Entries)
			{
				if (entry.Sha256 == pSha256)
				{
					return entry;
				}
			}
			return null;
		}

		public List<ProfileEntry> FindByEngine(string pEngine)
		{
			var result = new List<ProfileEntry>();
			foreach (var entry in Entries)
			{
				if (entry.Engine == pEngine)
				{
					result.Add(entry);
				}
			}
			return result;
		}

		public List<ProfileEntry> FindAllMatching(string pSha256, string pEngine)
		{
			var result = new List<ProfileEntry>();
			foreach (var entry in Entries)
			{
				// Empty selectors must never turn a specific profile into a wildcard.
				// Empty fields on the profile itself are intentional wildcards (for
				// example, an engine-wide compatibility profile).
				var shaMatches = string.IsNullOrEmpty(entry.Sha256)
					|| (!string.IsNullOrEmpty(pSha256) && entry.Sha256 == pSha256);
				var engineMatches = string.IsNullOrEmpty(entry.Engine)
					|| (!string.IsNullOrEmpty(pEngine) && entry.Engine == pEngine);
				if (shaMatches && engineMatches)
				{
					result.Add(entry);
				}
			}
			return result;
		}
	}

	private readonly CompatibilityDatabase _database = new();
	private readonly List<string> _loadedFiles = new();

	/// <summary>Load a compatibility profile from JSON file.</summary>
	public bool LoadProfile(string pPath)
	{
		if (!FileAccess.FileExists(pPath))
		{
			GD.PrintErr("[CompatibilityProfile] File not found: ", pPath);
			return false;
		}

		string jsonString;
		using (var file = FileAccess.Open(pPath, FileAccess.ModeFlags.Read))
		{
			if (file == null)
			{
				GD.PrintErr("[CompatibilityProfile] Cannot open file: ", pPath);
				return false;
			}
			jsonString = file.GetAsText();
		}

		var json = new Json();
		var parseError = json.Parse(jsonString);
		if (parseError != Error.Ok)
		{
			GD.PrintErr("[CompatibilityProfile] JSON parse error: ", json.GetErrorMessage(), " in ", pPath);
			return false;
		}

		var data = json.Data;
		if (data.VariantType != Variant.Type.Dictionary)
		{
			GD.PrintErr("[CompatibilityProfile] Invalid profile format in ", pPath);
			return false;
		}

		ParseProfileData(data.AsGodotDictionary());
		_loadedFiles.Add(pPath);
		return true;
	}

	/// <summary>Load profiles from a directory.</summary>
	public int LoadProfilesFromDirectory(string pDirectory)
	{
		var count = 0;
		using var dir = DirAccess.Open(pDirectory);
		if (dir == null)
		{
			return 0;
		}

		dir.ListDirBegin();
		var fileName = dir.GetNext();
		while (!string.IsNullOrEmpty(fileName))
		{
			if (fileName.EndsWith(".json"))
			{
				if (LoadProfile(pDirectory + "/" + fileName))
				{
					count += 1;
				}
			}
			fileName = dir.GetNext();
		}
		dir.ListDirEnd();

		return count;
	}

	/// <summary>Add a compatibility flag.</summary>
	public void AddFlag(string pName, FlagType pType, Variant pValue)
	{
		_database.Flags[pName] = new CompatFlag(pName, pType, pValue);
	}

	/// <summary>Add a profile entry.</summary>
	public void AddEntry(ProfileEntry pEntry)
	{
		_database.AddEntry(pEntry);
	}

	/// <summary>Get all flags for a game.</summary>
	public List<CompatFlag> GetGameFlags(string pSha256, string pEngine)
	{
		var matchingEntries = _database.FindAllMatching(pSha256, pEngine);
		var flagsByName = new Dictionary<string, CompatFlag>();

		// Global defaults are applied first.
		foreach (var pair in _database.Flags)
		{
			flagsByName[pair.Key] = pair.Value;
		}

		// Matching entries override global defaults and earlier matching entries.
		// This is intentional: per-game compatibility rules must be able to
		// disable or change a global compatibility default.
		foreach (var entry in matchingEntries)
		{
			foreach (var flag in entry.Flags)
			{
				flagsByName[flag.Name] = flag;
			}
		}

		return new List<CompatFlag>(flagsByName.Values);
	}

	/// <summary>Check if a specific flag is set for a game.</summary>
	public bool HasFlag(string pSha256, string pEngine, string pFlagName)
	{
		var flags = GetGameFlags(pSha256, pEngine);
		foreach (var flag in flags)
		{
			if (flag.Name == pFlagName)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Get a flag value for a game.</summary>
	public Variant GetFlagValue(string pSha256, string pEngine, string pFlagName)
	{
		var flags = GetGameFlags(pSha256, pEngine);
		foreach (var flag in flags)
		{
			if (flag.Name == pFlagName)
			{
				return flag.Value;
			}
		}
		return default;
	}

	/// <summary>Parse a profile JSON structure.</summary>
	private void ParseProfileData(Godot.Collections.Dictionary pData)
	{
		// Parse global flags
		if (pData.TryGetValue("flags", out var flagsVariant) && flagsVariant.VariantType == Variant.Type.Array)
		{
			foreach (var flagDataVariant in flagsVariant.AsGodotArray())
			{
				if (flagDataVariant.VariantType != Variant.Type.Dictionary)
				{
					continue;
				}
				var flagData = flagDataVariant.AsGodotDictionary();
				if (!flagData.ContainsKey("name"))
				{
					continue;
				}
				var flagType = StringToFlagType(
					flagData.TryGetValue("type", out var typeVariant) ? typeVariant.AsString() : "STRING");
				var value = flagData.TryGetValue("value", out var valueVariant) ? valueVariant : default;
				AddFlag(flagData["name"].AsString(), flagType, value);
			}
		}

		// Parse entries
		if (!pData.TryGetValue("entries", out var entriesVariant) || entriesVariant.VariantType != Variant.Type.Array)
		{
			return;
		}
		foreach (var entryDataVariant in entriesVariant.AsGodotArray())
		{
			if (entryDataVariant.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			var entryData = entryDataVariant.AsGodotDictionary();
			var entry = new ProfileEntry
			{
				Id = entryData.TryGetValue("id", out var idVariant) ? idVariant.AsString() : "",
				Sha256 = entryData.TryGetValue("sha256", out var shaVariant) ? shaVariant.AsString() : "",
				Engine = entryData.TryGetValue("engine", out var engineVariant) ? engineVariant.AsString() : "",
				GameTitle = entryData.TryGetValue("game_title", out var titleVariant) ? titleVariant.AsString() : "",
				Type = entryData.TryGetValue("type", out var typeVariant2) ? typeVariant2.AsString() : "game_profile",
				Compatibility = entryData.TryGetValue("compatibility", out var compatVariant) ? compatVariant.AsString() : "unknown",
				Notes = entryData.TryGetValue("notes", out var notesVariant) ? notesVariant.AsString() : "",
				Replacement = entryData.TryGetValue("replacement", out var replVariant) ? replVariant.AsString() : "",
			};

			// Parse flags
			if (entryData.TryGetValue("flags", out var entryFlagsVariant) && entryFlagsVariant.VariantType == Variant.Type.Array)
			{
				foreach (var flagDataVariant in entryFlagsVariant.AsGodotArray())
				{
					if (flagDataVariant.VariantType != Variant.Type.Dictionary)
					{
						continue;
					}
					var flagData = flagDataVariant.AsGodotDictionary();
					if (!flagData.ContainsKey("name"))
					{
						continue;
					}
					var flagType = StringToFlagType(
						flagData.TryGetValue("type", out var typeVariant3) ? typeVariant3.AsString() : "BOOLEAN");
					var value = flagData.TryGetValue("value", out var valueVariant2) ? valueVariant2 : false;
					entry.Flags.Add(new CompatFlag(flagData["name"].AsString(), flagType, value));
				}
			}

			_database.AddEntry(entry);
		}
	}

	/// <summary>Convert string to FlagType enum.</summary>
	private static FlagType StringToFlagType(string pString)
	{
		return pString.ToUpperInvariant() switch
		{
			"BOOLEAN" => FlagType.Boolean,
			"INTEGER" => FlagType.Integer,
			"FLOAT" => FlagType.Float,
			"ARRAY" => FlagType.Array,
			"DICTIONARY" => FlagType.Dictionary,
			_ => FlagType.String,
		};
	}

	/// <summary>Get the compatibility database.</summary>
	public CompatibilityDatabase GetDatabase() => _database;

	/// <summary>Get loaded profile files.</summary>
	public List<string> GetLoadedFiles() => new(_loadedFiles);
}
