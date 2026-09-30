namespace UniversalRPG.Web;

/// <summary>
/// A move route's own codes, as the finished game's engine numbers them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And these are read out of the engine, and not guessed, and
/// the guess would have been wrong in the most expensive way.</strong>
/// Measured in <c>CamelliaCoronation-Win/js/rmmz_objects.js</c>:
/// <c>ROUTE_MOVE_DOWN</c> is <strong>1</strong>, and
/// <c>ROUTE_TURN_DOWN</c> is <strong>16</strong>. <strong>Sixteen is not
/// a direction and it is not a step</strong> — <strong>it turns a
/// figure.</strong>
/// </para>
/// <para>
/// <strong>And the project's own routes use only four of them</strong>:
/// seven pages carry <c>16, 18, 17, 19</c> and nothing else — <strong>so
/// a reader that mistook 16 for "move down" walked a room of people who
/// were only ever supposed to turn.</strong>
/// </para>
/// </remarks>
public static class MzRouteCode
{
    /// <summary>The route is finished.</summary>
    public const int End = 0;

    /// <summary>Facing down, which is the engine's own number.</summary>
    public const int DirectionDown = 2;

    /// <summary>Facing left.</summary>
    public const int DirectionLeft = 4;

    /// <summary>Facing right.</summary>
    public const int DirectionRight = 6;

    /// <summary>Facing up.</summary>
    public const int DirectionUp = 8;

    /// <summary>Move one tile down.</summary>
    public const int MoveDown = 1;

    /// <summary>Move one tile left.</summary>
    public const int MoveLeft = 2;

    /// <summary>Move one tile right.</summary>
    public const int MoveRight = 3;

    /// <summary>Move one tile up.</summary>
    public const int MoveUp = 4;

    /// <summary>Wait for a number of frames.</summary>
    public const int Wait = 15;

    /// <summary>Turn to face down, and stay put.</summary>
    public const int TurnDown = 16;

    /// <summary>Turn to face left, and stay put.</summary>
    public const int TurnLeft = 17;

    /// <summary>Turn to face right, and stay put.</summary>
    public const int TurnRight = 18;

    /// <summary>Turn to face up, and stay put.</summary>
    public const int TurnUp = 19;

    /// <summary>Turn a quarter to the right.</summary>
    /// <remarks>
    /// <strong>And this is measured too</strong>, and it was the first
    /// thing a reader would miss after 16 to 19: <strong>the engine has
    /// four more turns between 19 and 24</strong>, <strong>and a reader
    /// that treated 20 and 21 as something else turned a figure the
    /// wrong quarter.</strong>
    /// </remarks>
    public const int TurnQuarterRight = 20;

    /// <summary>Turn a quarter to the left.</summary>
    public const int TurnQuarterLeft = 21;

    /// <summary>Turn about.</summary>
    public const int TurnAbout = 22;

    /// <summary>Turn a quarter to the right or to the left.</summary>
    public const int TurnQuarterRightOrLeft = 23;

    /// <summary>Turn to face a random way.</summary>
    public const int TurnRandom = 24;

    /// <summary>Turn to face the player.</summary>
    public const int TurnToward = 25;

    /// <summary>Turn to face away from the player.</summary>
    public const int TurnAway = 26;

    /// <summary>Turn on a switch.</summary>
    public const int SwitchOn = 27;

    /// <summary>Turn off a switch.</summary>
    public const int SwitchOff = 28;

    /// <summary>Change how fast the figure moves.</summary>
    public const int ChangeSpeed = 29;

    /// <summary>Change how fast the pattern advances.</summary>
    public const int ChangeFrequency = 30;

    /// <summary>Show the walk animation.</summary>
    public const int WalkAnimeOn = 31;

    /// <summary>Do not show the walk animation.</summary>
    public const int WalkAnimeOff = 32;

    /// <summary>Show a step animation while standing.</summary>
    public const int StepAnimeOn = 33;

    /// <summary>Do not show a step animation while standing.</summary>
    public const int StepAnimeOff = 34;

    /// <summary>Keep the direction even when moving.</summary>
    public const int DirFixOn = 35;

    /// <summary>Let the direction follow the walk.</summary>
    public const int DirFixOff = 36;

    /// <summary>Walk through everything.</summary>
    public const int ThroughOn = 37;

    /// <summary>Stop walking through everything.</summary>
    public const int ThroughOff = 38;

    /// <summary>Become see-through.</summary>
    public const int TransparentOn = 39;

    /// <summary>Stop being see-through.</summary>
    public const int TransparentOff = 40;

    /// <summary>Show a different picture.</summary>
    public const int ChangeImage = 41;

    /// <summary>Change how solid the figure looks.</summary>
    public const int ChangeOpacity = 42;

    /// <summary>Change how the figure blends.</summary>
    public const int ChangeBlendMode = 43;

    /// <summary>Play a sound.</summary>
    public const int PlaySe = 44;

    /// <summary>
    /// Turns a figure a quarter to the right, as the engine does.
    /// </summary>
    /// <param name="pDirection">The direction it faces now.</param>
    /// <returns>The direction it faces afterwards.</returns>
    /// <remarks>
    /// <strong>And the engine's own table, verbatim:</strong> down
    /// becomes left, left becomes up, up becomes right, right becomes
    /// down. <strong>And that is not what "right" means on a
    /// compass</strong> — <strong>it is what it means looking at a
    /// figure from above in a top-down world</strong> — <strong>and a
    /// reader that rotated the numbers instead of following this table
    /// turned every figure the wrong way while looking exactly
    /// right.</strong>
    /// </remarks>
    public static int QuarterRight(int pDirection)
    {
        return pDirection switch
        {
            2 => 4,
            4 => 8,
            8 => 6,
            6 => 2,
            _ => pDirection,
        };
    }

    /// <summary>
    /// Turns a figure a quarter to the left, as the engine does.
    /// </summary>
    /// <param name="pDirection">The direction it faces now.</param>
    /// <returns>The direction it faces afterwards.</returns>
    public static int QuarterLeft(int pDirection)
    {
        return pDirection switch
        {
            2 => 6,
            6 => 8,
            8 => 4,
            4 => 2,
            _ => pDirection,
        };
    }

    /// <summary>
    /// Whether a code is one that changes a direction without moving.
    /// </summary>
    /// <param name="pCode">The route code.</param>
    /// <returns>Whether it is a turn.</returns>
    /// <remarks>
    /// <strong>And this distinction is the whole point.</strong> The four
    /// turns are 16 to 19 and the four moves are 1 to 4, and
    /// <strong>a figure in this project only ever turns</strong> —
    /// <strong>so a runtime that treated 16 as a move walked seven
    /// figures off their tiles.</strong>
    /// </remarks>
    public static bool IsTurn(int pCode)
    {
        return pCode >= TurnDown && pCode <= TurnUp;
    }

    /// <summary>
    /// Whether a code is one that moves the figure a tile.
    /// </summary>
    /// <param name="pCode">The route code.</param>
    /// <returns>Whether it is a move.</returns>
    public static bool IsMove(int pCode)
    {
        return pCode >= MoveDown && pCode <= MoveUp;
    }
}