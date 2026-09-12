using System;
using System.Collections.Generic;
using UniversalRPG.Core;

namespace UniversalRPG.Rm2k.Parser;

public enum Rm2kMoveCommandCode
{
    MoveUp = 0,
    MoveRight = 1,
    MoveDown = 2,
    MoveLeft = 3,
    MoveUpRight = 4,
    MoveDownRight = 5,
    MoveDownLeft = 6,
    MoveUpLeft = 7,
    MoveRandom = 8,
    MoveTowardsHero = 9,
    MoveAwayFromHero = 10,
    MoveForward = 11,
    FaceUp = 12,
    FaceRight = 13,
    FaceDown = 14,
    FaceLeft = 15,
    TurnRight90 = 16,
    TurnLeft90 = 17,
    Turn180 = 18,
    Turn90Random = 19,
    FaceRandom = 20,
    FaceHero = 21,
    FaceAwayFromHero = 22,
    Wait = 23,
    BeginJump = 24,
    EndJump = 25,
    LockFacing = 26,
    UnlockFacing = 27,
    IncreaseMoveSpeed = 28,
    DecreaseMoveSpeed = 29,
    IncreaseMoveFrequency = 30,
    DecreaseMoveFrequency = 31,
    SwitchOn = 32,
    SwitchOff = 33,
    ChangeGraphic = 34,
    PlaySoundEffect = 35,
    WalkEverywhereOn = 36,
    WalkEverywhereOff = 37,
    StopAnimation = 38,
    StartAnimation = 39,
    IncreaseTransparency = 40,
    DecreaseTransparency = 41,
}

public sealed class Rm2kMoveCommand
{
    public Rm2kMoveCommandCode Code { get; init; }
    public string ParameterString { get; init; } = "";
    public int ParameterA { get; init; }
    public int ParameterB { get; init; }
    public int ParameterC { get; init; }
}

public sealed class Rm2kMoveRoute
{
    public IReadOnlyList<Rm2kMoveCommand> Commands { get; init; } = Array.Empty<Rm2kMoveCommand>();
    public bool Repeat { get; init; } = true;
    public bool Skippable { get; init; }
}

public sealed class Rm2kMoveRouteDecodeResult
{
    private Rm2kMoveRouteDecodeResult(bool pSuccess, Rm2kMoveRoute? pRoute, string pError)
    {
        Success = pSuccess;
        Route = pRoute;
        Error = pError;
    }

    public bool Success { get; }
    public Rm2kMoveRoute? Route { get; }
    public string Error { get; }

    public static Rm2kMoveRouteDecodeResult Succeeded(Rm2kMoveRoute pRoute)
        => new(true, pRoute, "");

    public static Rm2kMoveRouteDecodeResult Failed(string pError)
        => new(false, null, pError ?? "Invalid RM2K move route.");
}

/// <summary>
/// Bounded decoder for the LMU MoveRoute structure. Command IDs/parameter
/// layouts follow liblcf's rpg::MoveCommand contract. Unknown command IDs fail
/// closed instead of guessing their byte length and desynchronizing the route.
/// </summary>
public static class Rm2kMoveRouteDecoder
{
    public const int MaxCommands = 10000;
    public const int MaxRouteBytes = 1024 * 1024;
    public const int MaxParameterStringBytes = 64 * 1024;

    private const int FieldCommandBytes = 0x0B;
    private const int FieldCommands = 0x0C;
    private const int FieldRepeat = 0x15;
    private const int FieldSkippable = 0x16;

    public static Rm2kMoveRouteDecodeResult Decode(byte[] pData)
    {
        if (pData == null || pData.Length > MaxRouteBytes)
        {
            return Rm2kMoveRouteDecodeResult.Failed("RM2K move route is null or exceeds the bounded route size.");
        }

        var reader = new LcfBinaryReader(pData);
        byte[]? commandBytes = null;
        int? declaredBytes = null;
        var repeat = true;
        var skippable = false;
        var fieldCount = 0;
        var terminated = false;

        while (!reader.IsEof())
        {
            if (++fieldCount > LcfBinaryReader.MaxStructFields)
            {
                return Rm2kMoveRouteDecodeResult.Failed("RM2K move route contains too many structure fields.");
            }
            var field = reader.ReadChunk();
            if (reader.HasError())
            {
                return Rm2kMoveRouteDecodeResult.Failed($"RM2K move route field error at 0x{reader.ErrorOffset:X}: {reader.ErrorMessage}");
            }
            if ((bool)field["terminator"])
            {
                terminated = true;
                break;
            }

            var id = (int)field["id"];
            var data = (byte[])field["data"];
            switch (id)
            {
                case FieldCommandBytes:
                    if (declaredBytes.HasValue)
                        return Rm2kMoveRouteDecodeResult.Failed("RM2K move route contains duplicate command-size fields.");
                    if (data.Length == 0) declaredBytes = 0;
                    else if (TryReadSingleBer(data, out var size)) declaredBytes = size;
                    else return Rm2kMoveRouteDecodeResult.Failed("RM2K move route byte-size hint is malformed.");
                    break;
                case FieldCommands:
                    if (commandBytes != null)
                        return Rm2kMoveRouteDecodeResult.Failed("RM2K move route contains duplicate command-stream fields.");
                    if (data.Length > MaxRouteBytes)
                        return Rm2kMoveRouteDecodeResult.Failed("RM2K move route command stream exceeds the bounded size.");
                    commandBytes = data;
                    break;
                case FieldRepeat:
                    if (!TryReadBoolean(data, out repeat))
                        return Rm2kMoveRouteDecodeResult.Failed("RM2K move route repeat flag is malformed.");
                    break;
                case FieldSkippable:
                    if (!TryReadBoolean(data, out skippable))
                        return Rm2kMoveRouteDecodeResult.Failed("RM2K move route skippable flag is malformed.");
                    break;
                default:
                    // Preserve forward compatibility by ignoring unknown route
                    // structure fields whose payload is already length-bounded.
                    break;
            }
        }

        if (!terminated && pData.Length > 0)
        {
            return Rm2kMoveRouteDecodeResult.Failed("RM2K move route structure is missing its terminator.");
        }
        if (!reader.IsEof())
        {
            return Rm2kMoveRouteDecodeResult.Failed("RM2K move route contains trailing bytes after its terminator.");
        }

        commandBytes ??= Array.Empty<byte>();
        // Field 0x0B is a SizeField (serialized bytes), not a CountField.
        // Like liblcf, tolerate stale size hints: the actual bounded payload
        // determines decoding, allocation and the independent MaxCommands cap.
        var commandsResult = DecodeCommands(commandBytes);
        if (!commandsResult.Success)
        {
            return commandsResult;
        }
        return Rm2kMoveRouteDecodeResult.Succeeded(new Rm2kMoveRoute
        {
            Commands = commandsResult.Route!.Commands,
            Repeat = repeat,
            Skippable = skippable,
        });
    }

    /// <summary>
    /// Decodes only the raw MoveCommand vector contained in MoveRoute field
    /// 0x0C. This is public for synthetic fixtures and future event-command
    /// Set Move Route support.
    /// </summary>
    public static Rm2kMoveRouteDecodeResult DecodeCommands(byte[] pData, int? pExpectedCount = null)
    {
        if (pData == null || pData.Length > MaxRouteBytes)
        {
            return Rm2kMoveRouteDecodeResult.Failed("RM2K move command stream is null or too large.");
        }
        if (pExpectedCount is < 0 or > MaxCommands)
        {
            return Rm2kMoveRouteDecodeResult.Failed("RM2K move command count is outside the bounded limit.");
        }

        var reader = new LcfBinaryReader(pData);
        var commands = new List<Rm2kMoveCommand>();
        var decoder = new LegacyTextDecoder();
        while (!reader.IsEof())
        {
            if (commands.Count >= MaxCommands)
            {
                return Rm2kMoveRouteDecodeResult.Failed($"RM2K move route exceeds {MaxCommands} commands.");
            }

            var rawCode = reader.ReadBer();
            if (reader.HasError())
                return ReadFailure(reader);
            if (rawCode < (int)Rm2kMoveCommandCode.MoveUp || rawCode > (int)Rm2kMoveCommandCode.DecreaseTransparency)
            {
                return Rm2kMoveRouteDecodeResult.Failed($"Unsupported RM2K move command ID {rawCode}; byte length cannot be inferred safely.");
            }

            var command = new Rm2kMoveCommand { Code = (Rm2kMoveCommandCode)rawCode };
            switch (command.Code)
            {
                case Rm2kMoveCommandCode.SwitchOn:
                case Rm2kMoveCommandCode.SwitchOff:
                    if (!TryReadBer(reader, out var switchId)) return ReadFailure(reader);
                    command = new Rm2kMoveCommand { Code = command.Code, ParameterA = switchId };
                    break;
                case Rm2kMoveCommandCode.ChangeGraphic:
                    if (!TryReadString(reader, decoder, out var graphicName, out var stringError))
                        return Rm2kMoveRouteDecodeResult.Failed(stringError);
                    if (!TryReadBer(reader, out var graphicIndex)) return ReadFailure(reader);
                    command = new Rm2kMoveCommand
                    {
                        Code = command.Code,
                        ParameterString = graphicName,
                        ParameterA = graphicIndex,
                    };
                    break;
                case Rm2kMoveCommandCode.PlaySoundEffect:
                    if (!TryReadString(reader, decoder, out var soundName, out stringError))
                        return Rm2kMoveRouteDecodeResult.Failed(stringError);
                    if (!TryReadBer(reader, out var volume)
                        || !TryReadBer(reader, out var tempo)
                        || !TryReadBer(reader, out var balance))
                        return ReadFailure(reader);
                    command = new Rm2kMoveCommand
                    {
                        Code = command.Code,
                        ParameterString = soundName,
                        ParameterA = volume,
                        ParameterB = tempo,
                        ParameterC = balance,
                    };
                    break;
            }
            commands.Add(command);
        }

        if (pExpectedCount.HasValue && commands.Count != pExpectedCount.Value)
        {
            return Rm2kMoveRouteDecodeResult.Failed(
                $"RM2K move route declared {pExpectedCount.Value} commands but decoded {commands.Count}.");
        }
        return Rm2kMoveRouteDecodeResult.Succeeded(new Rm2kMoveRoute { Commands = commands });
    }

    private static bool TryReadString(
        LcfBinaryReader pReader,
        LegacyTextDecoder pDecoder,
        out string pValue,
        out string pError)
    {
        pValue = "";
        pError = "";
        var length = pReader.ReadBer();
        if (pReader.HasError())
        {
            pError = $"RM2K move command string length error at 0x{pReader.ErrorOffset:X}: {pReader.ErrorMessage}";
            return false;
        }
        if (length < 0 || length > MaxParameterStringBytes || length > pReader.GetRemaining())
        {
            pError = "RM2K move command string exceeds the bounded/remaining payload.";
            return false;
        }
        var bytes = pReader.ReadBytes(length);
        if (pReader.HasError())
        {
            pError = $"RM2K move command string read error at 0x{pReader.ErrorOffset:X}: {pReader.ErrorMessage}";
            return false;
        }
        pValue = pDecoder.Decode(bytes);
        if (string.IsNullOrEmpty(pValue) && bytes.Length > 0)
        {
            pError = "RM2K move command string could not be decoded using the legacy text boundary.";
            return false;
        }
        return true;
    }

    private static bool TryReadSingleBer(byte[] pData, out int pValue)
    {
        pValue = 0;
        if (pData.Length == 0) return false;
        var reader = new LcfBinaryReader(pData);
        pValue = reader.ReadBer();
        return !reader.HasError() && reader.IsEof();
    }

    private static bool TryReadBoolean(byte[] pData, out bool pValue)
    {
        // liblcf's Primitive<bool> reads an LCF compressed integer and expects
        // a one-byte field for normal true/false values.
        pValue = false;
        if (pData.Length != 1 || !TryReadSingleBer(pData, out var raw))
        {
            return false;
        }
        pValue = raw > 0;
        return true;
    }

    private static bool TryReadBer(LcfBinaryReader pReader, out int pValue)
    {
        pValue = pReader.ReadBer();
        return !pReader.HasError();
    }

    private static Rm2kMoveRouteDecodeResult ReadFailure(LcfBinaryReader pReader)
        => Rm2kMoveRouteDecodeResult.Failed(
            $"RM2K move command read error at 0x{pReader.ErrorOffset:X}: {pReader.ErrorMessage}");
}
