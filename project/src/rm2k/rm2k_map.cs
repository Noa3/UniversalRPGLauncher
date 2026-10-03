using System.Collections.Generic;

namespace UniversalRPG.Rm2k;

/// <summary>
/// Represents a single RM2K map with tile layers, events, and metadata.
/// Separate from gameplay logic — purely data representation.
/// </summary>
public class Rm2kMap
{
	/// <summary>
	/// One entry of a move route, matching liblcf's
	/// <c>rpg::MoveCommand</c>: <c>command_id</c>, <c>parameter_string</c> and
	/// the three integers <c>parameter_a</c>, <c>parameter_b</c>,
	/// <c>parameter_c</c>.
	/// </summary>
	public class MoveCommand
	{
		/// <summary>liblcf <c>MoveCommand::command_id</c>, an
		/// <c>Enum&lt;MoveCommand_Code&gt;</c>.</summary>
		public int CommandId;

		/// <summary>liblcf <c>MoveCommand::parameter_string</c>, a DBString.</summary>
		public string ParameterString = "";

		public int ParameterA;
		public int ParameterB;
		public int ParameterC;

		public MoveCommand(int pCommandId = 0, string pParameterString = "", int pA = 0, int pB = 0, int pC = 0)
		{
			CommandId = pCommandId;
			ParameterString = pParameterString;
			ParameterA = pA;
			ParameterB = pB;
			ParameterC = pC;
		}

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "command_id", CommandId },
				{ "parameter_string", ParameterString },
				{ "parameter_a", ParameterA },
				{ "parameter_b", ParameterB },
				{ "parameter_c", ParameterC },
			};
		}
	}

	/// <summary>Event command for RM2K.</summary>
	public class EventCommand
	{
		public int Code;
		public List<int> Parameters = new();
		public string Text = "";

		/// <summary>
		/// The block this command sits in, from the LCF <c>0x0D</c> chunk.
		/// </summary>
		/// <remarks>
		/// <para>
		/// <strong>This is the field that makes 20140 and 20141 possible, and
		/// losing it is not a detail.</strong> The two commands are a
		/// sub-command pair: a game writes one "show choice option" per branch,
		/// and the engine uses this number to tell which branches belong
		/// together and which to skip.
		/// </para>
		/// <para>
		/// A decoder that reads the chunk and then drops the number produces
		/// events that parse completely and behave wrongly — and every test of
		/// the codes, the parameters and the strings stays green.
		/// </para>
		/// </remarks>
		public int Indent;

	/// <summary>
	/// And it builds a command out of the parser's own dictionary.
	/// </summary>
	/// <param name="pFeld">
	/// One entry of <c>Rm2kEventCommandDecoder</c>'s output.
	/// </param>
	/// <remarks>
	/// <para>
	/// <strong>And this bridge is needed and not optional</strong>, --
	/// <strong>because the decoder returns Godot dictionaries and the
	/// interpreter takes <c>EventCommand</c></strong>, -- <strong>and
	/// the two shapes have drifted apart.</strong>
	/// </para>
	/// <para>
	/// <strong>And a troop page needs it exactly as a map page
	/// does</strong>, -- <strong>which is the point: a battle is not a
	/// second command dialect.</strong>
	/// </para>
	/// <para>
	/// <strong>And the block number is carried</strong>, -- <strong>and
	/// a converter that dropped it would break <c>20140</c> and
	/// <c>20141</c> the same way it breaks them on a map.</strong>
	/// </para>
	/// </remarks>
	public static EventCommand AusGelesenem(
		Godot.Collections.Dictionary pFeld)
	{
		var befehl = new EventCommand();
		if (pFeld == null)
		{
			return befehl;
		}

		befehl.Code = pFeld.ContainsKey("code")
			? pFeld["code"].AsInt32() : 0;
		befehl.Text = pFeld.ContainsKey("text")
			? pFeld["text"].AsString() : "";

		if (pFeld.ContainsKey("indent"))
		{
			var einrueckung = pFeld["indent"].AsInt32();
			if (einrueckung > 0)
			{
				befehl.Indent = einrueckung;
			}
		}

		if (pFeld.ContainsKey("parameters"))
		{
			foreach (var wert in (Godot.Collections
				.Array<long>)pFeld["parameters"])
			{
				befehl.Parameters.Add((int)wert);
			}
		}

		return befehl;
	}


		public EventCommand(
			int pCode = 0, List<int>? pParams = null, string pText = "", int pIndent = 0)
		{
			Code = pCode;
			Parameters = pParams ?? new List<int>();
			Text = pText;
			Indent = pIndent;
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

		// liblcf EventPage::Trigger: 0=action, 1=touched, 2=collision, 3=auto_start, 4=parallel.
		public int Trigger;

		// liblcf EventPage::Layers (LMU chunk 0x22): 0=below, 1=same, 2=above.
		public int Layer;

		// liblcf EventPage::move_route, LMU chunk 0x29. The route is a
		// rpg::MoveRoute struct: move_commands 0x0B size and 0x0C array,
		// repeat 0x15, skippable 0x16. Defaults are false for skippable and
		// true for repeat.
		public List<MoveCommand> MoveRouteCommands { get; } = new();

		/// <summary>liblcf MoveRoute::repeat, default true.</summary>
		public bool MoveRouteRepeat = true;

		/// <summary>liblcf MoveRoute::skippable, default false.</summary>
		public bool MoveRouteSkippable;

		// liblcf EventPage::move_frequency, LMU chunk 0x20. The value is the
		// move frequency, which is the divisor in GetMaxStopCountForStep and
		// not the per update step amount.
		public int MoveFrequency;

		public Dictionary<string, object> ToDict()
		{
			return new Dictionary<string, object>
			{
				{ "conditions", Conditions },
				{ "commands_count", Commands.Count },
				{ "graphic", Graphic },
				{ "trigger", Trigger },
				{ "move_route_count", MoveRouteCommands.Count },
				{ "move_route_repeat", MoveRouteRepeat },
				{ "move_frequency", MoveFrequency },
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

		// LMT chunk 0x15, liblcf Event::character_name.
		public string CharacterName = string.Empty;

		// LMT chunk 0x16, liblcf Event::character_index.
		public int CharacterIndex;

		// LMT chunk 0x17, liblcf Event::character_direction, stored as this
		// project stores directions: 2 down, 4 left, 6 right, 8 up.
		public byte Direction = 2;

		// LMT chunk 0x19, liblcf Event::character_pattern. This is the
		// initial anim_frame, so a page can start on a given walk pose.
		public int AnimationFrame;

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
