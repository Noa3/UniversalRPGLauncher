using System.Text.Json;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kPictureSave : TestBase
{
    private static GameSimulationState CreateState()
    {
        var state = new GameSimulationState { Presentation = new PresentationState() };
        state.ConfigureMap(1, 1, 1, new[] { true });
        return state;
    }

    private static void AddPicture(GameSimulationState pState, int pId, int pX, int pY)
    {
        pState.Presentation!.Pictures[pId] = new PictureState
        {
            Id = pId, Name = "Checkpoint", X = pX, Y = pY,
            Width = 16, Height = 16, Magnify = 100, Saturation = 100,
        };
    }

    public void Test_ResumedPictureMatchesUninterruptedMovementOnEveryRemainingFrame()
    {
        var original = CreateState();
        AddPicture(original, 1, -40, 80);
        AssertTrue(original.Presentation!.MovePicture(1, 120, -80, 10));
        original.Presentation.Tick(4);
        AssertEq((original.Presentation.Pictures[1].X, original.Presentation.Pictures[1].Y), (24, 16));

        var restored = CreateState();
        AssertTrue(Rm2kSimulationSaveCodec.TryRestore(
            Rm2kSimulationSaveCodec.Serialize(original), restored, out var error), error);
        for (var elapsed = 0; elapsed <= 7; elapsed++)
        {
            AssertEq((restored.Presentation!.Pictures[1].X, restored.Presentation.Pictures[1].Y),
                (original.Presentation.Pictures[1].X, original.Presentation.Pictures[1].Y),
                $"Save/resume must preserve the trajectory at resumed frame {elapsed}");
            AssertEq(restored.Presentation.PictureMoveFramesLeft(1),
                original.Presentation.PictureMoveFramesLeft(1));
            if (elapsed == 7) break;
            original.Presentation.Tick(1);
            restored.Presentation.Tick(1);
        }
        AssertEq((restored.Presentation!.Pictures[1].X, restored.Presentation.Pictures[1].Y), (120, -80));
        AssertEq(restored.Presentation.PictureMoveFramesLeft(1), 0);
    }

    public void Test_LoadReplacesOldMovementsForStationaryAndAbsentPictures()
    {
        var saved = CreateState();
        AddPicture(saved, 1, 30, 40);
        var target = CreateState();
        AddPicture(target, 1, 0, 0);
        AddPicture(target, 2, 50, 60);
        AssertTrue(target.Presentation!.MovePicture(1, 100, 200, 10));
        AssertTrue(target.Presentation.MovePicture(2, 200, 300, 20));

        AssertTrue(Rm2kSimulationSaveCodec.TryRestore(
            Rm2kSimulationSaveCodec.Serialize(saved), target, out var error), error);
        AssertEq(target.Presentation.PictureMoveFramesLeft(1), 0,
            "The restored stationary picture must not inherit a live movement");
        AssertEq(target.Presentation.PictureMoveFramesLeft(2), 0,
            "A picture absent from the save must have no residual movement");
        target.Presentation.Tick(3);
        AssertEq((target.Presentation.Pictures[1].X, target.Presentation.Pictures[1].Y), (30, 40));
        AssertFalse(target.Presentation.Pictures.ContainsKey(2));
    }

    public void Test_InvalidMovementTimingIsRejectedBeforeChangingLiveState()
    {
        var original = CreateState();
        AddPicture(original, 1, -40, 80);
        original.Presentation!.MovePicture(1, 120, -80, 10);
        original.Presentation.Tick(4);
        var valid = Rm2kSimulationSaveCodec.Serialize(original);
        foreach (var (total, left, startX, startY) in new (int, int, int?, int?)[]
        {
            (0, 6, -40, 80), (-1, 6, -40, 80), (10, -1, -40, 80),
            (10, 0, -40, 80), (10, 11, -40, 80), (10, int.MaxValue, -40, 80),
            (10, 6, null, 80), (10, 6, -40, null),
        })
        {
            var data = JsonSerializer.Deserialize<Rm2kSimulationSaveCodec.SaveData>(valid)!;
            var picture = data.Presentation.Pictures[0];
            picture.MoveTotalFrames = total;
            picture.MoveFramesLeft = left;
            picture.MoveStartX = startX;
            picture.MoveStartY = startY;
            var target = CreateState();
            target.MapId = 7;
            target.Gold = 912;
            AddPicture(target, 2, 30, 40);
            target.Presentation!.MovePicture(2, 100, 200, 20);
            var before = Rm2kSimulationSaveCodec.Serialize(target);

            AssertFalse(Rm2kSimulationSaveCodec.TryRestore(JsonSerializer.Serialize(data), target, out var error),
                $"Invalid movement total={total}, remaining={left}, origin=({startX},{startY}) must be refused");
            AssertTrue(!string.IsNullOrEmpty(error), "A rejected movement must explain its failure");
            AssertEq(Rm2kSimulationSaveCodec.Serialize(target), before,
                "A rejected save must preserve the complete live snapshot");
        }
    }

    /// <summary>
    /// A tint saved in the middle of its fade is resumed, not restarted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the target is the part that would have been lost.</strong> A
    /// save that carried only the colour the screen is showing would resume the
    /// fade towards whatever the next tick interpolated against -- which, with
    /// nothing to aim at, is the colour it already has: the screen would stop
    /// fading and stay grey for the rest of the game.
    /// </para>
    /// <para>
    /// <strong>And the assertion is frame by frame against the uninterrupted
    /// run</strong>, because "it looks about right after loading" is exactly the
    /// kind of claim that hides a one-frame offset.
    /// </para>
    /// </remarks>
    public void Test_EinTintWirdMittenImFadeGespeichertUndWeitergefuehrt()
    {
        var original = CreateState();
        AssertTrue(original.Presentation!.TintScreen(0, 0, 0, 100, 4),
            "the tint of four frames is accepted");
        original.Presentation.Tick(2);
        AssertEq(original.Presentation.TintRed, 50,
            "**and the screen is half way to black**");

        var restored = CreateState();
        AssertTrue(Rm2kSimulationSaveCodec.TryRestore(
            Rm2kSimulationSaveCodec.Serialize(original), restored, out var error), error);
        AssertEq(restored.Presentation!.TintRed, 50,
            "**and the load keeps the colour the screen is showing**");
        AssertEq(restored.Presentation.TintFinishRed, 0,
            "**and the colour it was fading towards**");
        AssertEq(restored.Presentation.TintFramesRemaining, 2,
            "and the frames it had left");

        for (var frame = 0; frame < 2; frame++)
        {
            original.Presentation.Tick(1);
            restored.Presentation.Tick(1);
            AssertEq(restored.Presentation.TintRed, original.Presentation.TintRed,
                "**and every remaining frame of the fade is the frame it would"
                    + " have been without the save**");
        }
        AssertEq(restored.Presentation.TintRed, 0,
            "**and both land exactly on the target**");
    }
}
