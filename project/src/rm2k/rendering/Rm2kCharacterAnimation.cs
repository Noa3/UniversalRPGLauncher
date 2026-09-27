namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// The RPG Maker 2000 character walk animation, reproduced from
/// <c>Game_Character::UpdateAnim</c> and <c>Game_Character::IncAnimFrame</c>.
/// </summary>
/// <remarks>
/// <para>
/// The character animation is not a clock. It counts <c>anim_count</c> up once
/// per update and only advances the visible frame when a per-speed threshold is
/// reached, and the frame cycles over four values, not over the three columns a
/// character cell has. The fourth value is <c>Frame_middle2</c>, which
/// <c>Sprite_Character::Draw</c> clamps back to <c>Frame_middle</c>, so the
/// hero visibly stands still on every fourth step while the state keeps
/// rotating. Any implementation that cycles over three frames therefore
/// animates at a visibly different rate from the original.
/// </para>
/// <para>
/// The thresholds come from <c>GetStationaryAnimFrames</c> and
/// <c>GetContinuousAnimFrames</c>, both indexed by a one based speed, so a
/// speed of 1 uses the first entry and a speed outside 1..6 has no defined
/// limit and is rejected rather than indexed out of bounds.
/// </para>
/// </remarks>
public static class Rm2kCharacterAnimation
{
    /// <summary>
    /// liblcf <c>rpg::EventPage::Frame_left</c>.
    /// </summary>
    public const int FrameLeft = 0;

    /// <summary>
    /// liblcf <c>rpg::EventPage::Frame_middle</c>.
    /// </summary>
    public const int FrameMiddle = 1;

    /// <summary>
    /// liblcf <c>rpg::EventPage::Frame_right</c>.
    /// </summary>
    public const int FrameRight = 2;

    /// <summary>
    /// liblcf <c>rpg::EventPage::Frame_middle2</c>. Reached by the modulo
    /// rotation, then drawn as <see cref="FrameMiddle"/>.
    /// </summary>
    public const int FrameMiddle2 = 3;

    /// <summary>
    /// The number of frames the visible frame rotates over.
    /// </summary>
    public const int FrameCount = 4;

    /// <summary>Highest defined move speed, since the tables are one based.</summary>
    public const int MaxMoveSpeed = 6;

    private static readonly int[] StationaryLimits = [12, 10, 8, 6, 5, 4];

    private static readonly int[] ContinuousLimits = [16, 12, 10, 8, 7, 6];

    /// <summary>
    /// <c>Sprite_Character::Draw</c> clamps anything from
    /// <see cref="FrameMiddle2"/> back to <see cref="FrameMiddle"/>, because a
    /// character cell only has three columns. Applied here as well so a stored
    /// pattern outside the visible range is normalised once, where it is read.
    /// </summary>
    public static int ClampFrame(int pFrame)
    {
        if (pFrame < FrameLeft)
        {
            return FrameLeft;
        }
        return pFrame >= FrameMiddle2 ? FrameMiddle : pFrame;
    }

    /// <summary>
    /// <c>Game_Character::GetStationaryAnimFrames</c>.
    /// </summary>
    public static int StationaryAnimFrames(int pSpeed)
    {
        return StationaryLimits[IndexFor(pSpeed)];
    }

    /// <summary>
    /// <c>Game_Character::GetContinuousAnimFrames</c>.
    /// </summary>
    public static int ContinuousAnimFrames(int pSpeed)
    {
        return ContinuousLimits[IndexFor(pSpeed)];
    }

    /// <summary>
    /// <c>Game_Character::GetSpinAnimFrames</c>.
    /// </summary>
    public static int SpinAnimFrames(int pSpeed)
    {
        return SpinningLimits[IndexFor(pSpeed)];
    }

    private static readonly int[] SpinningLimits = [24, 16, 12, 8, 6, 4];

    private static int IndexFor(int pSpeed)
    {
        if (pSpeed < 1 || pSpeed > MaxMoveSpeed)
        {
            // The upstream tables are indexed unchecked, so an out of range
            // speed is a corrupt data problem, not a value to clamp.
            throw new Rm2kAnimationDataException(
                $"A character animation speed of {pSpeed} is outside the defined range 1..{MaxMoveSpeed}.");
        }
        return pSpeed - 1;
    }

    /// <summary>
    /// One tick of <c>Game_Character::UpdateAnim</c>, returning the new frame
    /// and count. A paused or jumping character resets to the middle frame, and
    /// a character that is not animated returns unchanged, so a non animated
    /// event never walks.
    /// </summary>
    /// <param name="pFrame">The current <c>anim_frame</c>.</param>
    /// <param name="pCount">The current <c>anim_count</c>.</param>
    /// <param name="pStopCount">The current <c>stop_count</c>.</param>
    /// <param name="pSpeed">The move speed, 1..6.</param>
    /// <param name="pAnimated">
    /// Whether the character animates at all, the inverse of
    /// <c>Game_Character::IsAnimated</c>.
    /// </param>
    /// <param name="pContinuous">
    /// <c>Game_Character::IsContinuous</c>, true while a move route drives
    /// the character without stopping.
    /// </param>
    /// <param name="pPaused">
    /// <c>Game_Character::IsAnimPaused</c>, or whether the character is
    /// jumping. Both reset the animation to the middle frame.
    /// </param>
    public static (int Frame, int Count) Update(
        int pFrame,
        int pCount,
        int pStopCount,
        int pSpeed,
        bool pAnimated = true,
        bool pContinuous = false,
        bool pPaused = false)
    {
        if (pPaused)
        {
            // ResetAnimation sets anim_count to 0, and keeps anim_frame when the
            // animation type is a fixed graphic.
            return (pFrame, 0);
        }
        if (!pAnimated)
        {
            return (pFrame, pCount);
        }

        var stationaryLimit = StationaryAnimFrames(pSpeed);
        var continuousLimit = ContinuousAnimFrames(pSpeed);

        if (pContinuous
            || pStopCount == 0
            || pFrame == FrameLeft
            || pFrame == FrameRight
            || pCount < stationaryLimit - 1)
        {
            pCount++;
        }

        if (pCount >= continuousLimit
            || (pStopCount == 0 && pCount >= stationaryLimit))
        {
            return ((pFrame + 1) % FrameCount, 0);
        }

        return (pFrame, pCount);
    }
}
