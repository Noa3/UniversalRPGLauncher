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

    public bool IsValid()
    {
        return MaxFileBytes > 0 && MaxFileBytes <= 64 * 1024 * 1024
            && MaxDatabaseRecords > 0 && MaxDatabaseFieldsPerRecord > 0
            && MaxMaps > 0 && MaxMapDimension > 0 && MaxMapDimension <= 4096
            && MaxMapTiles > 0 && MaxEventsPerMap > 0 && MaxCommandsPerEvent > 0
            && MaxCommonEvents > 0 && MaxStringBytes > 0;
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
    public int Frames { get; init; }
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
