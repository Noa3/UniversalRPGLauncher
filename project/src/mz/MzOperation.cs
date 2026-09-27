namespace UniversalRPG.Web;

/// <summary>Where a variable command gets the number it works with.</summary>
public enum MzOperand
{
    Constant = 0,
    Variable = 1,
    Random = 2,
    GameData = 3,
    Script = 4,
}

/// <summary>What a variable command does with that number.</summary>
public enum MzOperation
{
    Set = 0,
    Add = 1,
    Subtract = 2,
    Multiply = 3,
    Divide = 4,
    Modulo = 5,
}

/// <summary>
/// A source of random numbers the caller supplies, so that reading a command
/// list twice gives the same numbers twice.
/// </summary>
/// <remarks>
/// The engine draws from <c>Math.random</c>, so its own replay is not
/// reproducible either. This reader does not roll its own, because a reader
/// whose random numbers come from a clock cannot be told apart from one that
/// made them up, and a command that gave different answers on two runs of the
/// same file would be impossible to test. The engine's rule is kept: the same
/// drawn number is added to every variable in a range rather than a number drawn
/// for each.
/// </remarks>
public sealed class MzRandom
{
    private int _state = 1;

    /// <summary>Draws the next number below a bound, as the engine's own
    /// <c>Math.randomInt</c> does.</summary>
    public int Next(int pMax)
    {
        if (pMax <= 1)
        {
            return 0;
        }
        // A small generator of the caller's own, so a run is repeatable. This is
        // not a claim of the engine's numbers; the engine's are not repeatable.
        _state = (int)((uint)(_state * 1103515245 + 12345) & 0x7fffffff);
        return _state % pMax;
    }

    /// <summary>Sets the starting point, so a caller can replay a known run.</summary>
    public void Seed(int pState) => _state = pState == 0 ? 1 : pState;
}
