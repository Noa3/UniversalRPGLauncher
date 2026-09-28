using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// The only data representation accepted by the initial WOLF runtime slice.
/// It is a small, documented, unencrypted JSON fixture envelope used for
/// deterministic conformance tests; proprietary/protected WOLF archives are
/// never decrypted or otherwise bypassed.
/// </summary>
public static class WolfPlainFormat
{
    public const int Version = 1;
    public const string Format = "urpg-wolf-plain-json";
}

public sealed class WolfParseLimits
{
    public long MaxFileBytes { get; init; } = 8 * 1024 * 1024;
    public int MaxDatabaseRecords { get; init; } = 100_000;
    public int MaxDatabaseFieldsPerRecord { get; init; } = 128;
    public int MaxMaps { get; init; } = 100_000;
    public int MaxMapDimension { get; init; } = 500;
    public int MaxMapTiles { get; init; } = 250_000;
    public int MaxEventsPerMap { get; init; } = 10_000;
    public int MaxCommandsPerEvent { get; init; } = 100_000;
    public int MaxCommonEvents { get; init; } = 100_000;
    public int MaxStringBytes { get; init; } = 64 * 1024;

    /// <summary>
    /// The most strings one command may declare.
    /// </summary>
    /// <remarks>
    /// The field is a single byte, so 255 is the largest value a file can
    /// express. The limit is set below that on purpose: a count near the byte's
    /// maximum is far more likely to be a misread than a real command with two
    /// hundred strings, and refusing it is better than reading two hundred
    /// strings out of unrelated bytes.
    /// </remarks>
    public int MaxStringsPerCommand { get; init; } = 32;

    public bool IsValid()
    {
        return MaxFileBytes > 0 && MaxFileBytes <= 64 * 1024 * 1024
            && MaxDatabaseRecords > 0 && MaxDatabaseFieldsPerRecord > 0
            && MaxMaps > 0 && MaxMapDimension > 0 && MaxMapDimension <= 4096
            && MaxMapTiles > 0 && MaxEventsPerMap > 0 && MaxCommandsPerEvent > 0
            && MaxCommonEvents > 0 && MaxStringBytes > 0
            && MaxStringsPerCommand > 0 && MaxStringsPerCommand <= 255;
    }
}

public sealed class WolfProjectData
{
    public string Title { get; init; } = "";
    public int FormatVersion { get; init; } = WolfPlainFormat.Version;
    public bool IsProtected { get; init; }
    public string SourceDirectory { get; init; } = "";
    public WolfDatabaseData? SystemDatabase { get; init; }
    public IReadOnlyList<WolfDatabaseData> UserDatabases { get; init; } = Array.Empty<WolfDatabaseData>();
    public WolfDatabaseData? VariableDatabase { get; init; }
    public IReadOnlyList<WolfMapData> Maps { get; init; } = Array.Empty<WolfMapData>();
    public IReadOnlyList<WolfEventProgram> CommonEvents { get; init; } = Array.Empty<WolfEventProgram>();
}

/// <summary>
/// WOLF database records are intentionally schema-free. Games define their own
/// database types and fields, so the loader preserves scalar JSON values as
/// canonical JSON text instead of pretending they are RPG Maker actors/items.
/// </summary>
public sealed class WolfDatabaseData
{
    public string DatabaseId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public IReadOnlyList<WolfDatabaseRecord> Records { get; init; } = Array.Empty<WolfDatabaseRecord>();
}

public sealed class WolfDatabaseRecord
{
    public int Id { get; init; }
    public IReadOnlyDictionary<string, string> Fields { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed class WolfMapData
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int Width { get; init; }
    public int Height { get; init; }
    public IReadOnlyList<int> Tiles { get; init; } = Array.Empty<int>();
    public IReadOnlyList<WolfEventProgram> Events { get; init; } = Array.Empty<WolfEventProgram>();
}

public sealed class WolfEventProgram
{
    public int Id { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public IReadOnlyList<WolfEventCommand> Commands { get; init; } = Array.Empty<WolfEventCommand>();
}

public enum WolfEventOpcode
{
    Unknown,
    Message,
    SetVariable,
    AddVariable,
    SetSwitch,
    IfSwitch,
    IfVariable,
    Wait,
    MoveRoute,
    WaitUntilRouteDone,
    Choice,
    Transfer,
    End,
}

public sealed class WolfEventCommand
{
    public WolfEventOpcode Opcode { get; init; }
    public string RawOperation { get; init; } = "";
    public string Text { get; init; } = "";
    public int Operand { get; init; }
    public int Value { get; init; }

    /// <summary>
    /// Which of the seven comparisons a branch command uses.
    /// </summary>
    /// <remarks>
    /// **Separate from <see cref="Value"/> and not folded into it.** The
    /// editor writes the comparison as its own number, and a reader that
    /// assumed every branch was an equality test would take one branch in seven
    /// — with the other six falling through to the else path, so a chest
    /// guarded by "V0 is at least 1" would never open.
    /// </remarks>
    public int Comparison { get; init; }

    /// <summary>
    /// Which of the fourteen assignment operators a variable command uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Its own field and not folded into the opcode or the value.</strong>
    /// The editor picks the operator in its own dropdown next to the
    /// destination, so a program that only knows <c>=</c> and <c>+=</c> cannot
    /// express a hit rate, a damage formula, or an angle.
    /// </para>
    /// <para>
    /// <strong>Defaults to assignment</strong>, because a file that predates the
    /// operator, or a program that never set one, assigns.
    /// </para>
    /// </remarks>
    public int Operator { get; init; }

    /// <summary>
    /// The second number, for the operators that take two.
    /// </summary>
    /// <remarks>
    /// **Separate from <see cref="Value"/>, and empty is not zero.** The
    /// arc tangent reads two vectors and a file that gives one of them is not
    /// the same file as a file that gives a zero, so a reader that treated a
    /// missing second number as zero would compute an angle of zero for every
    /// slope whose Y vector was left out.
    /// </remarks>
    public int Right2 { get; init; }

    /// <summary>Whether a second number was given at all.</summary>
    public bool HasRight2 { get; init; }

    /// <summary>
    /// Where the true arm of a branch starts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Two targets and not one, and this is the whole point of a
    /// branch.</strong> <see cref="WolfEventCommand.JumpIndex"/> is where the
    /// <em>false</em> arm goes; this is where the <em>true</em> arm goes. A
    /// reader with one target runs both arms when the condition holds — the
    /// fall-through is the true arm and the jump is the false one, so without a
    /// second target the false arm is always the only one that runs.
    /// </para>
    /// <para>
    /// <strong>-1 means "the true arm is the fall-through"</strong>, which is
    /// the one shape a single-target reader can express. Every test in this
    /// file sets it, because a branch with two arms is what a game writes.
    /// </para>
    /// </remarks>
    public int TrueJumpIndex { get; init; } = -1;

	/// <summary>
	/// Where the command after both arms of a branch starts.
	/// </summary>
	/// <remarks>
	/// <strong>Arms have to end somewhere, and a fall-through does not do it.</strong>
	/// Without this a branch whose true arm runs would go on into the false
	/// arm, and the two would both execute. A game writing a chest that opens
	/// or a guard that attacks writes exactly one of them, and this is the
	/// field that keeps it to one.
	/// </remarks>
	public int EndJumpIndex { get; init; } = -1;

	/// <summary>
	/// Where this command jumps after it runs, or -1 for the next one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>On the command and not on the branch.</strong> A branch names
	/// where its two arms start, but each arm is a block of ordinary commands
	/// and <em>the last command of an arm has to step over the other arm</em>:
	/// a command with no jump runs on into whatever follows it.
	/// </para>
	/// <para>
	/// The alternative — an arm end remembered by the VM — was tried in this
	/// slice and needs hidden state that a program with two branches in a row
	/// leaks from one into the other. A jump on the command has no such state,
	/// and it is also the shape a WOLF event list has: the editor writes the
	/// jump after the last command of an arm.
	/// </para>
	/// </remarks>
	public int NextIndex { get; init; } = -1;

    public int Frames { get; init; }

    /// <summary>
    /// Which character a move command acts on.
    /// </summary>
    /// <remarks>
    /// <strong>An event id, and 0 is the hero.</strong> The format's character
    /// commands pick a figure from a list that starts with the hero, so a
    /// command that acted on "the current event" would move the wrong figure
    /// whenever a program ran for an event instead of the hero.
    /// </remarks>
    public int CharacterId { get; init; }

    /// <summary>
    /// The route a move command starts, or null when it carries none.
    /// </summary>
    /// <remarks>
    /// <strong>The steps on the command and not a name.</strong> The route
    /// reader produced the steps, and a reader that looked a route up by name
    /// at execution time would have to load it again for every step, and a
    /// program that ran a route the reader could not find would have no way to
    /// say so.
    /// </remarks>
    public WolfMoveRoute? Route { get; init; }
    public int MapId { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int JumpIndex { get; init; } = -1;
    public IReadOnlyList<string> Choices { get; init; } = Array.Empty<string>();
}

public sealed class WolfEventMessage
{
    public long Sequence { get; init; }
    public int EventId { get; init; }
    public string Text { get; init; } = "";
}

public sealed class WolfTransferRequest
{
    public int MapId { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
}
