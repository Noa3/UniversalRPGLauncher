using System.Collections.Generic;

namespace UniversalRPG.Rm2k;

/// <summary>
/// Canonical in-memory event models shared by the RM2000/2003 parser,
/// scheduler and interpreter.
///
/// Map geometry is intentionally not stored here. Parsed LMU geometry remains
/// in the bounded parser result and is projected into dedicated simulation and
/// rendering models (for example VirtualFramebuffer and Rm2kPassabilityMap).
/// Keeping the event model separate avoids the older byte-based TileLayer
/// representation, which could not represent real RM2K tile IDs above 255.
/// </summary>
public static class Rm2kMap
{
    public sealed class EventCommand
    {
        public int Code { get; set; }
        public List<int> Parameters { get; } = new();
        public string Text { get; set; } = "";

        public EventCommand(int pCode = 0, IEnumerable<int>? pParams = null, string pText = "")
        {
            Code = pCode;
            if (pParams != null)
            {
                Parameters.AddRange(pParams);
            }
            Text = pText ?? "";
        }

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                { "code", Code },
                { "parameters", new List<int>(Parameters) },
                { "text", Text },
            };
        }
    }

    public sealed class EventPage
    {
        public Dictionary<string, object> Conditions { get; set; } = new();
        public List<EventCommand> Commands { get; } = new();
        public Dictionary<string, object> Graphic { get; set; } = new();

        /// <summary>
        /// Internal semantic trigger value. Raw LMU trigger codes are converted
        /// through Rm2kEventTriggerCodec before pages reach the scheduler.
        /// </summary>
        public int Trigger { get; set; }

        /// <summary>
        /// Verified LMU event layer: 0=below, 1=same level, 2=above.
        /// </summary>
        public int Layer { get; set; } = 1;

        /// <summary>Raw RM2K movement frequency metadata (0..8 in normal projects).</summary>
        public int MoveFrequency { get; set; }

        public Dictionary<string, object> ToDict()
        {
            var serializedCommands = new List<Dictionary<string, object>>(Commands.Count);
            foreach (var command in Commands)
            {
                serializedCommands.Add(command.ToDict());
            }
            return new Dictionary<string, object>
            {
                { "conditions", new Dictionary<string, object>(Conditions) },
                { "commands_count", Commands.Count },
                { "commands", serializedCommands },
                { "graphic", new Dictionary<string, object>(Graphic) },
                { "trigger", Trigger },
                { "layer", Layer },
                { "move_frequency", MoveFrequency },
            };
        }
    }

    public sealed class Event
    {
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public List<EventPage> Pages { get; } = new();

        public Event(int pId = 0, int pX = 0, int pY = 0)
        {
            Id = pId;
            X = pX;
            Y = pY;
        }

        public Dictionary<string, object> ToDict()
        {
            var serializedPages = new List<Dictionary<string, object>>(Pages.Count);
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
}
