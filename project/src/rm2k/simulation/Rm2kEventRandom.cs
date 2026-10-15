namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// The stream the autonomous movement types draw from.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because the movement types need numbers and the
/// runtime had none for events.</strong> Random walking, the stop delay a
/// random page draws, and the toward/away choices all come from the engine's
/// own generator, and a reader that invented its numbers from a clock could
/// not be told apart from one that made them up.
/// </para>
/// <para>
/// <strong>A stream of its own, and not the battle stream.</strong> A game
/// that fights while an event walks should not have one draw move the other's
/// numbers, and a test that seeds this one has to be able to predict the
/// walk without predicting the fight.
/// </para>
/// <para>
/// <strong>And the draws are inclusive on both ends</strong>, because the
/// reference's <c>Rand::GetRandomNumber(from, to)</c> is: the random movement
/// table reads <c>GetRandomNumber(0, 9)</c> and has ten buckets, not nine.
/// </para>
/// </remarks>
public sealed class Rm2kEventRandom
{
    private int _state = 1;

    /// <summary>Draws a number in an inclusive interval, as the engine does.</summary>
    /// <param name="pFrom">The lowest value that can come back.</param>
    /// <param name="pTo">The highest value that can come back.</param>
    /// <returns>A value in <c>[pFrom, pTo]</c>.</returns>
    public int NextInclusive(int pFrom, int pTo)
    {
        if (pTo <= pFrom)
        {
            return pFrom;
        }
        return pFrom + Next(pTo - pFrom + 1);
    }

    /// <summary>Draws the next number below a bound.</summary>
    /// <param name="pMax">The exclusive upper bound.</param>
    /// <returns>A value in <c>[0, pMax)</c>, and zero for a bound below two.</returns>
    public int Next(int pMax)
    {
        if (pMax <= 1)
        {
            return 0;
        }
        // A small generator of the caller's own, so a run is repeatable. This
        // is not a claim of the engine's numbers; the engine's are not
        // repeatable, and a save file needs a run it can replay.
        _state = (int)((uint)(_state * 1103515245 + 12345) & 0x7fffffff);
        return _state % pMax;
    }

    /// <summary>Sets the starting point, so a caller can replay a known run.</summary>
    /// <param name="pState">The state to continue from; zero becomes one.</param>
    public void Seed(int pState) => _state = pState == 0 ? 1 : pState;

    /// <summary>The current state, so a save file can carry it.</summary>
    public int State => _state;
}
