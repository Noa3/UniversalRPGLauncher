namespace UniversalRPG.Web;

/// <summary>
/// Whether a menu is open, as the last command that opened or closed one left
/// it.
/// </summary>
/// <remarks>
/// <para>
/// A menu is a thing a player sees rather than a number the game computes, so
/// this is a state and not a value. <b>It exists so that a 351 is not
/// indistinguishable from a command that did nothing</b>: a reader with no
/// screen still has to be able to answer "did the game open a menu here", and
/// without somewhere to write the answer down it could only be silent.
/// </para>
/// <para>
/// The engine's <c>command351</c> is
/// <c>if (!$gameParty.inBattle()) { SceneManager.push(Scene_Menu); }</c>, and
/// <c>SceneManager</c> has no matching pop for a menu an event pushed — the
/// player closes it. So <see cref="Closed"/> here means <b>closed by the
/// player</b>, and nothing in an event list sets it. A 351 in a battle leaves
/// it exactly where it was, which is the engine's answer and not a failure.
/// </para>
/// </remarks>
public enum MzMenuState
{
    /// <summary>No menu is open.</summary>
    Closed = 0,

    /// <summary>A menu is open and a player is looking at it.</summary>
    Open = 1,
}
