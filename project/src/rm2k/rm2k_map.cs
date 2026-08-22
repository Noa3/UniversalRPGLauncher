using System.Collections.Generic;

namespace UniversalRPG.Rm2k;

/// <summary>
/// Represents a single RM2K map with tile layers, events, and metadata.
/// Separate from gameplay logic — purely data representation.
/// </summary>
public class Rm2kMap
{
	/// <summary>Event command for RM2K.</summary>
	public class EventCommand
	{
		public int Code;
		public List<int> Parameters = new();
		public string Text = "";

		public EventCommand(int pCode = 0, List<int>? pParams = null, string pText = "")
		{
			Code = pCode;
			Parameters = pParams ?? new List<int>();
			Text = pText;
		}

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "code", Code },
				{ "parameters", Parameters },
				{ "text", Text },
			};
		}
	}

	/// <summary>Event page.</summary>
	public class EventPage
	{
		public Dictionary<string, object> Conditions { get; set; } = new();
		public List<EventCommand> Commands { get; } = new();
		public Dictionary<string, object> Graphic { get; set; } = new();

		// 0=autorun, 1=parallel, 2=action, 3=touch
		public int Trigger;

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "conditions", Conditions },
				{ "commands_count", Commands.Count },
				{ "graphic", Graphic },
				{ "trigger", Trigger },
			};
		}
	}

	/// <summary>Event.</summary>
	public class Event
	{
		public int Id;
		public int X;
		public int Y;
		public List<EventPage> Pages { get; } = new();

		public Event(int pId = 0, int pX = 0, int pY = 0)
		{
			Id = pId;
			X = pX;
			Y = pY;
		}

		public Dictionary<string, object> ToDict()
		{
			var serializedPages = new List<Dictionary<string, object>>();
			foreach (var page in Pages)
			{
				serializedPages.Add(page.ToDict());
			}
			return new Dictionary<string, object>
			{
				{ "id", Id },
				{ "x", X },
				{ "y", Y },
				{ "pages", serializedPages },
			};
		}
	}

	/// <summary>Tile data for a layer.</summary>
	public class TileLayer
	{
		public byte[] Data = System.Array.Empty<byte>();
		public int Width;
		public int Height;

		public int GetTile(int pX, int pY)
		{
			if (pX >= 0 && pX < Width && pY >= 0 && pY < Height)
			{
				return Data[pY * Width + pX];
			}
			return -1;
		}

		public void SetTile(int pX, int pY, int pValue)
		{
			if (pX >= 0 && pX < Width && pY >= 0 && pY < Height)
			{
				Data[pY * Width + pX] = (byte)pValue;
			}
		}
	}

	public int MapId;
	public int Width;
	public int Height;
	public string Name = "";
	public int TilesetId;
	public string Battleback = "";
	public string Parallax = "";
	public bool ParallaxLoop;
	public bool ParallaxLoopX;
	public bool ParallaxLoopY;
	public int ParallaxS;
	public int ParallaxX;
	public int ParallaxY;

	// Tile layers (lower, middle, upper)
	public TileLayer LowerLayer = new();
	public TileLayer MiddleLayer = new();
	public TileLayer UpperLayer = new();

	// Passability layer (0=passable, 1=impassable)
	public TileLayer PassabilityLayer = new();

	// Events
	public List<Event> Events { get; } = new();

	// Map metadata
	public string DisplayName = "";
	public List<int> EncounterList = new();
	public int EncounterStep;

	public int GetTile(string pLayer, int pX, int pY)
	{
		return pLayer switch
		{
			"lower" => LowerLayer.GetTile(pX, pY),
			"middle" => MiddleLayer.GetTile(pX, pY),
			"upper" => UpperLayer.GetTile(pX, pY),
			_ => -1,
		};
	}

	public Event? GetEvent(int pId)
	{
		foreach (var mapEvent in Events)
		{
			if (mapEvent.Id == pId)
			{
				return mapEvent;
			}
		}
		return null;
	}

	public List<Event> GetEventsAt(int pX, int pY)
	{
		var result = new List<Event>();
		foreach (var mapEvent in Events)
		{
			if (mapEvent.X == pX && mapEvent.Y == pY)
			{
				result.Add(mapEvent);
			}
		}
		return result;
	}

	public Dictionary<string, object> ToDict()
	{
		var serializedEvents = new List<Dictionary<string, object>>();
		foreach (var mapEvent in Events)
		{
			serializedEvents.Add(mapEvent.ToDict());
		}
		return new Dictionary<string, object>
		{
			{ "map_id", MapId },
			{ "width", Width },
			{ "height", Height },
			{ "name", Name },
			{ "tileset_id", TilesetId },
			{ "battleback", Battleback },
			{ "parallax", Parallax },
			{ "parallax_loop", ParallaxLoop },
			{ "events", serializedEvents },
			{ "display_name", DisplayName },
			{ "encounter_list", EncounterList },
			{ "encounter_step", EncounterStep },
		};
	}

	public void FromDict(Dictionary<string, object> pDict)
	{
		MapId = GetInt(pDict, "map_id");
		Width = GetInt(pDict, "width");
		Height = GetInt(pDict, "height");
		Name = GetString(pDict, "name");
		TilesetId = GetInt(pDict, "tileset_id");
		Battleback = GetString(pDict, "battleback");
		Parallax = GetString(pDict, "parallax");
		ParallaxLoop = GetBool(pDict, "parallax_loop");

		Events.Clear();
		if (pDict.TryGetValue("events", out var eventsValue) && eventsValue is IEnumerable<object> eventList)
		{
			foreach (var eventDataObj in eventList)
			{
				if (eventDataObj is not Dictionary<string, object> eventData)
				{
					continue;
				}
				var mapEvent = new Event
				{
					Id = GetInt(eventData, "id"),
					X = GetInt(eventData, "x"),
					Y = GetInt(eventData, "y"),
				};
				if (eventData.TryGetValue("pages", out var pagesValue) && pagesValue is IEnumerable<object> pageList)
				{
					foreach (var pageDataObj in pageList)
					{
						if (pageDataObj is not Dictionary<string, object> pageData)
						{
							continue;
						}
						var page = new EventPage
						{
							Trigger = GetInt(pageData, "trigger"),
						};
						if (pageData.TryGetValue("conditions", out var conditionsValue) && conditionsValue is Dictionary<string, object> conditions)
						{
							page.Conditions = conditions;
						}
						mapEvent.Pages.Add(page);
					}
				}
				Events.Add(mapEvent);
			}
		}

		DisplayName = GetString(pDict, "display_name");
		EncounterList = new List<int>();
		if (pDict.TryGetValue("encounter_list", out var encounterValue) && encounterValue is IEnumerable<object> encounterItems)
		{
			foreach (var item in encounterItems)
			{
				EncounterList.Add(System.Convert.ToInt32(item));
			}
		}
		EncounterStep = GetInt(pDict, "encounter_step");
	}

	internal static int GetInt(Dictionary<string, object> pDict, string pKey, int pDefault = 0)
	{
		return pDict.TryGetValue(pKey, out var value) ? System.Convert.ToInt32(value) : pDefault;
	}

	internal static string GetString(Dictionary<string, object> pDict, string pKey, string pDefault = "")
	{
		return pDict.TryGetValue(pKey, out var value) ? value?.ToString() ?? pDefault : pDefault;
	}

	internal static bool GetBool(Dictionary<string, object> pDict, string pKey, bool pDefault = false)
	{
		return pDict.TryGetValue(pKey, out var value) ? System.Convert.ToBoolean(value) : pDefault;
	}
}
