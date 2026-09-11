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
        var route = Route(commands, 4, repeat: false, skippable: true);

        var result = Rm2kMoveRouteDecoder.Decode(route);

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

        var result = Rm2kMoveRouteDecoder.DecodeCommands(commands.ToArray(), 4);

        AssertTrue(result.Success, result.Error);
        AssertEq(result.Route!.Commands[0].ParameterA, 12);
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
        var commands = Ber((int)Rm2kMoveCommandCode.MoveDown);
        var route = Bytes(
            Chunk(0x0B, Ber(1)),
            Chunk(0x0C, commands),
            new byte[] { 0x00 });

        var result = Rm2kMoveRouteDecoder.Decode(route);

        AssertTrue(result.Success, result.Error);
        AssertTrue(result.Route!.Repeat);
        AssertFalse(result.Route.Skippable);
    }

    public void Test_RejectsDeclaredCountMismatch()
    {
        var route = Route(Ber((int)Rm2kMoveCommandCode.MoveLeft), 2, repeat: true, skippable: false);
        var result = Rm2kMoveRouteDecoder.Decode(route);

        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("declared 2", StringComparison.Ordinal));
    }

    public void Test_RejectsUnknownCommandBecauseLengthCannotBeInferred()
    {
        var result = Rm2kMoveRouteDecoder.DecodeCommands(Ber(99));

        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("Unsupported", StringComparison.Ordinal));
    }

    public void Test_RejectsMalformedBooleanField()
    {
        var route = Bytes(
            Chunk(0x0B, Ber(0)),
            Chunk(0x0C, Array.Empty<byte>()),
            Chunk(0x15, new byte[] { 0x01, 0x00 }),
            new byte[] { 0x00 });

        var result = Rm2kMoveRouteDecoder.Decode(route);

        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("repeat", StringComparison.OrdinalIgnoreCase));
    }

    public void Test_RejectsTrailingBytesAfterRouteTerminator()
    {
        var route = Bytes(
            Chunk(0x0B, Ber(0)),
            Chunk(0x0C, Array.Empty<byte>()),
            new byte[] { 0x00, 0x55 });

        var result = Rm2kMoveRouteDecoder.Decode(route);

        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("trailing", StringComparison.OrdinalIgnoreCase));
    }

    private static byte[] Route(byte[] pCommands, int pCount, bool repeat, bool skippable)
        => Bytes(
            Chunk(0x0B, Ber(pCount)),
            Chunk(0x0C, pCommands),
            Chunk(0x15, new byte[] { repeat ? (byte)1 : (byte)0 }),
            Chunk(0x16, new byte[] { skippable ? (byte)1 : (byte)0 }),
            new byte[] { 0x00 });

    private static byte[] Chunk(int pId, byte[] pPayload)
        => Bytes(Ber(pId), Ber(pPayload.Length), pPayload);

    private static byte[] Bytes(params byte[][] pParts)
    {
        var result = new List<byte>();
        foreach (var part in pParts) result.AddRange(part);
        return result.ToArray();
    }

    private static byte[] Ber(int pValue)
    {
        if (pValue < 0) throw new ArgumentOutOfRangeException(nameof(pValue));
        var groups = new List<byte> { (byte)(pValue & 0x7F) };
        var value = pValue >> 7;
        while (value > 0)
        {
            groups.Add((byte)(value & 0x7F));
            value >>= 7;
        }
        groups.Reverse();
        for (var index = 0; index < groups.Count - 1; index++) groups[index] |= 0x80;
        return groups.ToArray();
    }
}
