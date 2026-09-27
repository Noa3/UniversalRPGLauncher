using System.Collections.Generic;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves that <c>WolfMoveRouteReader</c> reads a WOLF move route as specified.
/// </summary>
/// <remarks>
/// <para>
/// The fixtures here are byte sequences built from the format specification.
/// They are not taken from a real WOLF game, because the repository has no WOLF
/// game to take them from. That bounds what these tests can claim: they prove
/// the reader agrees with the specification, not that the specification matches
/// a shipped game.
/// </para>
/// <para>
/// Two things in this format are worth a test of their own. The two option
/// blocks are bit fields, not bytes, so reading them as bytes shifts everything
/// after them by six. And a step describes its own argument lengths, so a reader
/// that takes the length from a per type table instead of from the file cannot
/// read a type it has never seen.
/// </para>
/// </remarks>
public partial class TestWolfMoveRoute : TestBase
{
    private static readonly WolfParseLimits Limits = new();

    private const string Source = "move route";

    public void Test_AnEmptyRouteIsReadWithItsHeaderAndNoSteps()
    {
        var route = Read(Route());

        AssertEq(route.Steps.Count, 0, "a route with no steps has none");
        AssertEq(route.MoveSpeed, 4, "the move speed is read");
        AssertEq(route.Mode, WolfMoveRouteMode.Custom, "the mode is read");
    }

    public void Test_TheHeaderFieldsAreReadInFileOrder()
    {
        // The order is animation frequency, move speed, move frequency, mode.
        // Reading them in a different order still produces four plausible bytes,
        // which is why each gets its own value here.
        var route = Read(Route(
            pAnimationFrequency: 7, pMoveSpeed: 3, pMoveFrequency: 9,
            pMode: WolfMoveRouteMode.TowardHero));

        AssertEq(route.AnimationFrequency, 7, "the animation frequency is first");
        AssertEq(route.MoveSpeed, 3, "the move speed is second");
        AssertEq(route.MoveFrequency, 9, "the move frequency is third");
        AssertEq(route.Mode, WolfMoveRouteMode.TowardHero, "and the mode is fourth");
    }

    public void Test_TheBehaviorBlockIsABitFieldNotAByte()
    {
        // Eight flags in one byte. Reading the block as a byte instead of as
        // bits would leave the cursor six bytes short and shift every field
        // after it, so the flags are checked by their exact bits.
        var route = Read(Route(
            pHalfStepLeft: true, pHalfStepUp: true, pSquareHitbox: true,
            pAboveHero: true, pSlipThrough: true, pFixedDirection: true,
            pMoveAnimation: true, pIdleAnimation: true));

        AssertTrue(route.HalfStepLeft, "the half step left bit");
        AssertTrue(route.HalfStepUp, "the half step up bit");
        AssertTrue(route.SquareHitbox, "the square hitbox bit");
        AssertTrue(route.AboveHero, "the above hero bit");
        AssertTrue(route.SlipThrough, "the slip through bit");
        AssertTrue(route.FixedDirection, "the fixed direction bit");
        AssertTrue(route.MoveAnimation, "the move animation bit");
        AssertTrue(route.IdleAnimation, "the idle animation bit");
    }

    public void Test_EachRouteOptionIsItsOwnBit()
    {
        // The three options sit in the high three bits and the low five are
        // reserved. Setting all three at once would not tell them apart, so
        // each gets a fixture of its own: a reader that read them in the wrong
        // order or off the wrong bits still passes a test that sets all three.
        AssertTrue(Read(Route(pWaitUntilDone: true)).WaitUntilDone,
            "0x80 alone is the wait until done option");
        AssertFalse(Read(Route(pWaitUntilDone: true)).SkipImpossibleMoves,
            "and it does not also set the skip impossible moves option");
        AssertFalse(Read(Route(pWaitUntilDone: true)).RepeatActions,
            "nor the repeat actions option");

        AssertTrue(Read(Route(pSkipImpossibleMoves: true)).SkipImpossibleMoves,
            "0x40 alone is the skip impossible moves option");
        AssertFalse(Read(Route(pSkipImpossibleMoves: true)).WaitUntilDone,
            "and it does not also set the wait until done option");

        AssertTrue(Read(Route(pRepeatActions: true)).RepeatActions,
            "0x20 alone is the repeat actions option");
        AssertFalse(Read(Route(pRepeatActions: true)).WaitUntilDone,
            "and it does not also set the wait until done option");
        AssertFalse(Read(Route(pRepeatActions: true)).SkipImpossibleMoves,
            "nor the skip impossible moves option");
    }

    public void Test_AllRouteOptionsAreClearWhenNoBitIsSet()
    {
        var without = Read(Route());
        AssertFalse(without.WaitUntilDone, "the wait until done option is clear");
        AssertFalse(without.SkipImpossibleMoves, "and the skip impossible moves option");
        AssertFalse(without.RepeatActions, "and the repeat actions option");
    }

    public void Test_AStepWithNoArgumentsStillWritesBothLengthBytes()
    {
        // A step that takes no arguments writes a zero word count and a zero
        // byte count. Reading only the first would take the second from the
        // next step's type, which is the drift a self describing format is
        // meant to prevent.
        var route = Read(Route(pSteps: [Step(WolfMoveRouteType.MoveDown)]));

        AssertEq(route.Steps.Count, 1, "the step is read");
        AssertEq(route.Steps[0].Type, WolfMoveRouteType.MoveDown, "with its own type");
        AssertEq(route.Steps[0].Arguments.Count, 0, "and no four byte arguments");
        AssertEq(route.Steps[0].ByteArguments.Count, 0, "and no single byte arguments");
    }

    public void Test_StepsAreReadInFileOrderWithoutDrift()
    {
        // Consecutive steps with different shapes: no arguments, two four byte
        // arguments, one four byte argument and one single byte argument. A
        // reader that is off by a byte anywhere cannot read all four.
        var route = Read(Route(pSteps:
        [
            Step(WolfMoveRouteType.MoveDown),
            Step(WolfMoveRouteType.Jump, [3, -4], [7]),
            Step(WolfMoveRouteType.SetMoveSpeed, [5], [0]),
            Step(WolfMoveRouteType.MoveUp),
        ]));

        AssertEq(route.Steps.Count, 4, "all four steps are read");
        AssertEq(route.Steps[0].Type, WolfMoveRouteType.MoveDown, "the first is a plain move");
        AssertEq(route.Steps[1].Type, WolfMoveRouteType.Jump, "the second is a jump");
        AssertEq(route.Steps[1].Arguments.Count, 2, "with two four byte arguments");
        AssertEq(route.Steps[1].Arguments[0], 3, "the first of them");
        AssertEq(route.Steps[1].Arguments[1], -4, "and a negative second one");
        AssertEq(route.Steps[1].ByteArguments[0], 7, "and its single byte argument");
        AssertEq(route.Steps[2].Type, WolfMoveRouteType.SetMoveSpeed, "the third sets a speed");
        AssertEq(route.Steps[2].Arguments[0], 5, "with the speed as its argument");
        AssertEq(route.Steps[3].Type, WolfMoveRouteType.MoveUp, "and the fourth is a plain move");
    }

    public void Test_TheArgumentLengthsComeFromTheFileNotFromATable()
    {
        // A type this reader has no name for still reads, because the lengths
        // are in the file. That is the property a per type table cannot have.
        var unknown = Read(Route(pSteps: [Step(0x24, [11, 22, 33], [1, 2])]));

        AssertEq(unknown.Steps.Count, 1, "a step of an unnamed type is read");
        AssertEq(unknown.Steps[0].Type, 0x24, "with its raw type value");
        AssertEq(unknown.Steps[0].Arguments.Count, 3, "and all three arguments");
        AssertEq(unknown.Steps[0].Arguments[2], 33, "including the last one");
        AssertEq(unknown.Steps[0].ByteArguments.Count, 2, "and both byte arguments");
        AssertTrue(unknown.Steps[0].IsKnownType, "and the table does define this type");
    }

    public void Test_AValueTheTableLeavesUnusedIsReportedRatherThanSkipped()
    {
        // 0x2A and 0x2B are the gap in the table. A reader that stepped over
        // them as if they were ordinary would not be able to say so.
        var gap = Read(Route(pSteps: [Step(0x2A)]));

        AssertFalse(gap.Steps[0].IsKnownType, "the gap is not a known type");
        AssertTrue(gap.Steps[0].Name.Contains("unused", System.StringComparison.Ordinal),
            $"and the name says so, but it said: {gap.Steps[0].Name}");
    }

    public void Test_AStepCountOverTheLimitIsRefused()
    {
        var bytes = new List<byte>();
        AddHeader(bytes, 0, 0, 0, 0);
        AddUInt32(bytes, 0xFFFFFFF);

        var error = "";
        try
        {
            Read(bytes.ToArray());
        }
        catch (WolfFormatException exception)
        {
            error = exception.Message;
        }
        AssertTrue(error.Contains("steps", System.StringComparison.OrdinalIgnoreCase),
            $"the diagnostic names the step count, but it said: {error}");
    }

    public void Test_AnArgumentCountOverTheLimitIsRefused()
    {
        var bytes = new List<byte>();
        AddHeader(bytes, 0, 0, 0, 0);
        AddUInt32(bytes, 1);
        bytes.Add(WolfMoveRouteType.MoveDown);
        bytes.Add(0xFF); // a four byte argument count of 255

        var error = "";
        try
        {
            Read(bytes.ToArray());
        }
        catch (WolfFormatException exception)
        {
            error = exception.Message;
        }
        AssertTrue(error.Contains("over the", System.StringComparison.OrdinalIgnoreCase),
            $"the diagnostic names the limit, but it said: {error}");
    }

    // ---- helpers ----

    private static WolfMoveRoute Read(byte[] pBytes)
    {
        var cursor = new WolfByteCursor(pBytes, Source);
        return WolfMoveRouteReader.Read(cursor, Limits);
    }

    private static void AddUInt32(List<byte> pBytes, int pValue)
    {
        pBytes.Add((byte)(pValue & 0xFF));
        pBytes.Add((byte)((pValue >> 8) & 0xFF));
        pBytes.Add((byte)((pValue >> 16) & 0xFF));
        pBytes.Add((byte)((pValue >> 24) & 0xFF));
    }

    private static void AddHeader(
        List<byte> pBytes, int pAnimationFrequency, int pMoveSpeed,
        int pMoveFrequency, int pMode, bool pHalfStepLeft = false,
        bool pHalfStepUp = false, bool pSquareHitbox = false,
        bool pAboveHero = false, bool pSlipThrough = false,
        bool pFixedDirection = false, bool pMoveAnimation = false,
        bool pIdleAnimation = false, bool pWaitUntilDone = false,
        bool pSkipImpossibleMoves = false, bool pRepeatActions = false)
    {
        pBytes.Add((byte)pAnimationFrequency);
        pBytes.Add((byte)pMoveSpeed);
        pBytes.Add((byte)pMoveFrequency);
        pBytes.Add((byte)pMode);

        var behavior = 0;
        if (pHalfStepLeft) behavior |= 0x01;
        if (pHalfStepUp) behavior |= 0x02;
        if (pSquareHitbox) behavior |= 0x04;
        if (pAboveHero) behavior |= 0x08;
        if (pSlipThrough) behavior |= 0x10;
        if (pFixedDirection) behavior |= 0x20;
        if (pMoveAnimation) behavior |= 0x40;
        if (pIdleAnimation) behavior |= 0x80;
        pBytes.Add((byte)behavior);

        // The low five bits of this block are reserved and must be zero.
        var options = 0;
        if (pWaitUntilDone) options |= 0x80;
        if (pSkipImpossibleMoves) options |= 0x40;
        if (pRepeatActions) options |= 0x20;
        pBytes.Add((byte)options);
    }

    private static byte[] Step(
        byte pType, int[] pWords = null, byte[] pBytes = null)
    {
        pWords ??= [];
        pBytes ??= [];
        var body = new List<byte> { pType, (byte)pWords.Length };
        foreach (var word in pWords)
        {
            body.Add((byte)(word & 0xFF));
            body.Add((byte)((word >> 8) & 0xFF));
            body.Add((byte)((word >> 16) & 0xFF));
            body.Add((byte)((word >> 24) & 0xFF));
        }
        body.Add((byte)pBytes.Length);
        body.AddRange(pBytes);
        return body.ToArray();
    }

    private static byte[] Route(
        int pAnimationFrequency = 4, int pMoveSpeed = 4,
        int pMoveFrequency = 4, int pMode = WolfMoveRouteMode.Custom,
        bool pHalfStepLeft = false, bool pHalfStepUp = false,
        bool pSquareHitbox = false, bool pAboveHero = false,
        bool pSlipThrough = false, bool pFixedDirection = false,
        bool pMoveAnimation = false, bool pIdleAnimation = false,
        bool pWaitUntilDone = false, bool pSkipImpossibleMoves = false,
        bool pRepeatActions = false, byte[][] pSteps = null)
    {
        pSteps ??= [];
        var bytes = new List<byte>();
        AddHeader(bytes, pAnimationFrequency, pMoveSpeed, pMoveFrequency, pMode,
            pHalfStepLeft, pHalfStepUp, pSquareHitbox, pAboveHero, pSlipThrough,
            pFixedDirection, pMoveAnimation, pIdleAnimation, pWaitUntilDone,
            pSkipImpossibleMoves, pRepeatActions);
        AddUInt32(bytes, pSteps.Length);
        foreach (var step in pSteps)
        {
            bytes.AddRange(step);
        }
        return bytes.ToArray();
    }
}
