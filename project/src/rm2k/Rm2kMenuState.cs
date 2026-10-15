using System;

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
/// <para>
/// <strong>And the menu has submenus, each a scene of its own.</strong>
/// Measured at <c>Scene_Menu.prototype.createCommandWindow</c>:
/// <c>item</c> pushes <c>Scene_Item</c>, <c>skill</c> pushes
/// <c>Scene_Skill</c>, <c>equip</c> pushes <c>Scene_Equip</c>,
/// <c>status</c> pushes <c>Scene_Status</c>, <c>formation</c> pushes
/// <c>Scene_Formation</c>, <c>save</c> pushes <c>Scene_Save</c>,
/// <c>gameEnd</c> pushes <c>Scene_GameEnd</c>. <strong>And cancel
/// pops the current scene</strong>, returning to the menu or the map.
/// </para>
/// </remarks>
public enum Rm2kMenuState
{
    /// <summary>No menu is open.</summary>
    Closed = 0,

    /// <summary>The main menu (Scene_Menu) is open.</summary>
    Main = 1,

    /// <summary>Item scene (Scene_Item) is open.</summary>
    Item = 2,

    /// <summary>Skill scene (Scene_Skill) is open.</summary>
    Skill = 3,

    /// <summary>Equip scene (Scene_Equip) is open.</summary>
    Equip = 4,

    /// <summary>Status scene (Scene_Status) is open.</summary>
    Status = 5,

    /// <summary>Formation scene (Scene_Formation) is open.</summary>
    Formation = 6,

    /// <summary>Options scene (Scene_Options) is open.</summary>
    Options = 7,

    /// <summary>Save scene (Scene_Save) is open.</summary>
    Save = 8,

    /// <summary>Game End scene (Scene_GameEnd) is open.</summary>
    GameEnd = 9,
}