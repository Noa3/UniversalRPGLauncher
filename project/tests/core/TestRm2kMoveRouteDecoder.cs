using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestRm2kMoveRouteDecoder : TestBase
{
    public void Test_DecodesSimpleMovementAndFacingCommands()
    {
        var commands = Bytes(
            Ber((int)Rm2kMoveCommandCode.MoveUp),
            Ber((int)Rm2kMoveCommandCode.MoveRight),
            Ber((int)Rm2kMoveCommandCode.FaceLeft),
            Ber((int)Rm2kMoveCommandCode.Wait));
        var result = Rm2kMoveRouteDecoder.Decode(Route(commands, repeat: false, skippable: true));

        AssertTrue(result.Success, result.Error);
        AssertEq(result.Route!.Commands.Count, 4);
        AssertEq(result.Route.Commands[0].Code, Rm2kMoveCommandCode.MoveUp);
        AssertEq(result.Route.Commands[1].Code, Rm2kMoveCommandCode.MoveRight);
        AssertEq(result.Route.Commands[2].Code, Rm2kMoveCommandCode.FaceLeft);
        AssertEq(result.Route.Commands[3].Code, Rm2kMoveCommandCode.Wait);
        AssertFalse(result.Route.Repeat);
        AssertTrue(result.Route.Skippable);
    }

    public void Test_DecodesParameterizedCommands()
    {
        var graphic = Encoding.ASCII.GetBytes("Hero");
        var sound = Encoding.ASCII.GetBytes("Step");
        var commands = new List<byte>();
        commands.AddRange(Ber((int)Rm2kMoveCommandCode.SwitchOn));
        commands.AddRange(Ber(12));
        commands.AddRange(Ber((int)Rm2kMoveCommandCode.SwitchOff));
        commands.AddRange(Ber(13));
        commands.AddRange(Ber((int)Rm2kMoveCommandCode.ChangeGraphic));
        commands.AddRange(Ber(graphic.Length));
        commands.AddRange(graphic);
        commands.AddRange(Ber(5));
        commands.AddRange(Ber((int)Rm2kMoveCommandCode.PlaySoundEffect));
        commands.AddRange(Ber(sound.Length));
        commands.AddRange(sound);
        commands.AddRange(Ber(80));
        commands.AddRange(Ber(100));
        commands.AddRange(Ber(50));

        // Exercise the containing on-disk structure too: raw-stream-only tests
        // previously missed the incorrect byte-size-as-command-count check.
        var result = Rm2kMoveRouteDecoder.Decode(Route(commands.ToArray(), repeat: true, skippable: false));
        AssertTrue(result.Success, result.Error);
        AssertEq(result.Route!.Commands.Count, 4);
        AssertEq(result.Route.Commands[0].ParameterA, 12);
        AssertEq(result.Route.Commands[1].ParameterA, 13);
        AssertEq(result.Route.Commands[2].ParameterString, "Hero");
        AssertEq(result.Route.Commands[2].ParameterA, 5);
        AssertEq(result.Route.Commands[3].ParameterString, "Step");
        AssertEq(result.Route.Commands[3].ParameterA, 80);
        AssertEq(result.Route.Commands[3].ParameterB, 100);
        AssertEq(result.Route.Commands[3].ParameterC, 50);
    }

    public void Test_DefaultsRepeatTrueAndSkippableFalseWhenFieldsAbsent()
    {
        var result = Rm2kMoveRouteDecoder.Decode(Bytes(
            Chunk(0x0B, Ber(1)), Chunk(0x0C, Ber((int)Rm2kMoveCommandCode.MoveDown)), new byte[] { 0 }));
        AssertTrue(result.Success, result.Error);
        AssertTrue(result.Route!.Repeat);
        AssertFalse(result.Route.Skippable);
    }

    public void Test_ExplicitRawCommandCountValidationRemainsAvailable()
    {
        // DecodeCommands' optional count is an explicit caller assertion. It is
        // not the meaning of the serialized MoveRoute field 0x0B.
        var result = Rm2kMoveRouteDecoder.DecodeCommands(Ber((int)Rm2kMoveCommandCode.MoveLeft), 2);
        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("declared 2", StringComparison.Ordinal));
    }

    public void Test_OneInstructionCanOccupySeveralSerializedBytes()
    {
        var stream = Bytes(Ber((int)Rm2kMoveCommandCode.SwitchOn), Ber(128));
        AssertEq(stream.Length, 3);
        var result = Rm2kMoveRouteDecoder.Decode(Route(stream, repeat: false, skippable: false));
        AssertTrue(result.Success, result.Error);
        AssertEq(result.Route!.Commands.Count, 1);
        AssertEq(result.Route.Commands[0].ParameterA, 128);
    }

    public void Test_AdvisorySizeHintsDoNotAllocateOrCountInstructions()
    {
        var stream = Bytes(Ber((int)Rm2kMoveCommandCode.SwitchOff), Ber(123));
        foreach (var hint in new[] { 0, 1, int.MaxValue })
        {
            var result = Rm2kMoveRouteDecoder.Decode(Bytes(Chunk(0x0B, Ber(hint)), Chunk(0x0C, stream), new byte[] { 0 }));
            AssertTrue(result.Success, result.Error);
            AssertEq(result.Route!.Commands.Count, 1);
        }
        AssertTrue(Rm2kMoveRouteDecoder.Decode(Bytes(Chunk(0x0C, stream), new byte[] { 0 })).Success);
    }

    public void Test_MalformedAndDuplicateSizeFieldsAreRejected()
    {
        var malformed = Bytes(Chunk(0x0B, new byte[] { 0x80 }), new byte[] { 0 });
        AssertFalse(Rm2kMoveRouteDecoder.Decode(malformed).Success);
        var duplicate = Bytes(Chunk(0x0B, Ber(0)), Chunk(0x0B, Ber(0)), new byte[] { 0 });
        AssertFalse(Rm2kMoveRouteDecoder.Decode(duplicate).Success);
    }

    public void Test_ActualCommandCountLimitCannotBeBypassedBySmallHint()
    {
        // Opcode zero is MoveUp in movement streams, not a route terminator.
        var tooMany = new byte[Rm2kMoveRouteDecoder.MaxCommands + 1];
        var result = Rm2kMoveRouteDecoder.Decode(Bytes(Chunk(0x0B, Ber(0)), Chunk(0x0C, tooMany), new byte[] { 0 }));
        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("exceeds", StringComparison.Ordinal));
    }

    public void Test_RejectsUnknownCommandBecauseLengthCannotBeInferred()
    {
        var result = Rm2kMoveRouteDecoder.DecodeCommands(Ber(99));
        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("Unsupported", StringComparison.Ordinal));
    }

    public void Test_RejectsMalformedBooleanField()
    {
        var result = Rm2kMoveRouteDecoder.Decode(Bytes(
            Chunk(0x0B, Ber(0)), Chunk(0x0C, Array.Empty<byte>()),
            Chunk(0x15, new byte[] { 1, 0 }), new byte[] { 0 }));
        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("repeat", StringComparison.OrdinalIgnoreCase));
    }

    public void Test_RejectsTrailingBytesAfterRouteTerminator()
    {
        var result = Rm2kMoveRouteDecoder.Decode(Bytes(
            Chunk(0x0B, Ber(0)), Chunk(0x0C, Array.Empty<byte>()), new byte[] { 0, 0x55 }));
        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("trailing", StringComparison.OrdinalIgnoreCase));
    }

    private static byte[] Route(byte[] commands, bool repeat, bool skippable)
        => Bytes(
            Chunk(0x0B, Ber(commands.Length)), Chunk(0x0C, commands),
            Chunk(0x15, new byte[] { repeat ? (byte)1 : (byte)0 }),
            Chunk(0x16, new byte[] { skippable ? (byte)1 : (byte)0 }), new byte[] { 0 });

    private static byte[] Chunk(int id, byte[] payload) => Bytes(Ber(id), Ber(payload.Length), payload);
    private static byte[] Bytes(params byte[][] parts)
    {
        var result = new List<byte>();
        foreach (var part in parts) result.AddRange(part);
        return result.ToArray();
    }

    private static byte[] Ber(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        var groups = new List<byte> { (byte)(value & 0x7F) };
        while ((value >>= 7) > 0) groups.Add((byte)(value & 0x7F));
        groups.Reverse();
        for (var i = 0; i < groups.Count - 1; i++) groups[i] |= 0x80;
        return groups.ToArray();
    }
}
