using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Web;

/// <summary>
/// One command of this generation, and the name the engine gives it.
/// </summary>
/// <param name="Code">The number a game stores in a command list.</param>
/// <param name="Name">
/// The name the engine carries above the method it dispatches that number
/// to, read out of the engine source of a real game.
/// </param>
public readonly record struct MzCommand(int Code, string Name);

/// <summary>
/// Every command this generation stores in an event.
/// </summary>
/// <remarks>
/// <para>
/// The editor names a command after the method the engine dispatches it to,
/// and the name is the one carried above that method:
/// </para>
/// <code>
/// // Show Text
/// Game_Interpreter.prototype.command101 = function(params) { ... };
/// </code>
/// <para>
/// Every number and every name here was read out of that source rather than
/// from a page someone wrote about the engine, because a page is a
/// description and the engine is what a game was written against. A number
/// guessed here would be a command that does nothing.
/// </para>
/// <para>
/// <b>The numbering is this generation's own and not the one before.</b>
/// There a command's number is its own value times a thousand and a reader
/// divides by a thousand to learn what a command is. Here the numbers run
/// from 101 to 603 with nothing multiplied by anything, so a reader written
/// for the generation before divides every command of a real game to zero.
/// </para>
/// <para>
/// <b>A name is written once.</b> An earlier shape of this was an enum with a
/// name in each member and a second table beside it carrying the same names
/// as strings, because a C# identifier cannot be <c>Show Text</c>. The two
/// copies drifted: changing one left the other, and the reader handed out
/// the string while the enum carried the prose. This is a record instead, so
/// the number and the name are one value and there is nowhere for a second
/// copy to live.
/// </para>
/// </remarks>
public static class MzCommandSet
{
    private static readonly MzCommand[] All =
    [
        new(101, "Show Text"),
        new(102, "Show Choices"),
        new(103, "Input Number"),
        new(104, "Select Item"),
        new(105, "Show Scrolling Text"),
        new(108, "Comment"),
        new(109, "Skip"),
        new(111, "Conditional Branch"),
        new(112, "Loop"),
        new(113, "Break Loop"),
        new(115, "Exit Event Processing"),
        new(117, "Common Event"),
        new(118, "Label"),
        new(119, "Jump to Label"),
        new(121, "Control Switches"),
        new(122, "Control Variables"),
        new(123, "Control Self Switch"),
        new(124, "Control Timer"),
        new(125, "Change Gold"),
        new(126, "Change Items"),
        new(127, "Change Weapons"),
        new(128, "Change Armors"),
        new(129, "Change Party Member"),
        new(132, "Change Battle BGM"),
        new(133, "Change Victory ME"),
        new(134, "Change Save Access"),
        new(135, "Change Menu Access"),
        new(136, "Change Encounter"),
        new(137, "Change Formation Access"),
        new(138, "Change Window Color"),
        new(139, "Change Defeat ME"),
        new(140, "Change Vehicle BGM"),
        new(201, "Transfer Player"),
        new(202, "Set Vehicle Location"),
        new(203, "Set Event Location"),
        new(204, "Scroll Map"),
        new(205, "Set Movement Route"),
        new(206, "Get on/off Vehicle"),
        new(211, "Change Transparency"),
        new(212, "Show Animation"),
        new(213, "Show Balloon Icon"),
        new(214, "Erase Event"),
        new(216, "Change Player Followers"),
        new(217, "Gather Followers"),
        new(221, "Fadeout Screen"),
        new(222, "Fadein Screen"),
        new(223, "Tint Screen"),
        new(224, "Flash Screen"),
        new(225, "Shake Screen"),
        new(230, "Wait"),
        new(231, "Show Picture"),
        new(232, "Move Picture"),
        new(233, "Rotate Picture"),
        new(234, "Tint Picture"),
        new(235, "Erase Picture"),
        new(236, "Set Weather Effect"),
        new(241, "Play BGM"),
        new(242, "Fadeout BGM"),
        new(243, "Save BGM"),
        new(244, "Resume BGM"),
        new(245, "Play BGS"),
        new(246, "Fadeout BGS"),
        new(249, "Play ME"),
        new(250, "Play SE"),
        new(251, "Stop SE"),
        new(261, "Play Movie"),
        new(281, "Change Map Name Display"),
        new(282, "Change Tileset"),
        new(283, "Change Battle Background"),
        new(284, "Change Parallax"),
        new(285, "Get Location Info"),
        new(301, "Battle Processing"),
        new(302, "Shop Processing"),
        new(303, "Name Input Processing"),
        new(311, "Change HP"),
        new(312, "Change MP"),
        new(313, "Change State"),
        new(314, "Recover All"),
        new(315, "Change EXP"),
        new(316, "Change Level"),
        new(317, "Change Parameter"),
        new(318, "Change Skill"),
        new(319, "Change Equipment"),
        new(320, "Change Name"),
        new(321, "Change Class"),
        new(322, "Change Actor Images"),
        new(323, "Change Vehicle Image"),
        new(324, "Change Nickname"),
        new(325, "Change Profile"),
        new(326, "Change TP"),
        new(331, "Change Enemy HP"),
        new(332, "Change Enemy MP"),
        new(333, "Change Enemy State"),
        new(334, "Enemy Recover All"),
        new(335, "Enemy Appear"),
        new(336, "Enemy Transform"),
        new(337, "Show Battle Animation"),
        new(339, "Force Action"),
        new(340, "Abort Battle"),
        new(342, "Change Enemy TP"),
        new(351, "Open Menu Screen"),
        new(352, "Open Save Screen"),
        new(353, "Game Over"),
        new(354, "Return to Title Screen"),
        new(355, "Script"),
        new(356, "Plugin Command MV (deprecated)"),
        new(357, "Plugin Command"),
        new(402, "When [**]"),
        new(403, "When Cancel"),
        new(411, "Else"),
        new(413, "Repeat Above"),
        new(601, "If Win"),
        new(602, "If Escape"),
        new(603, "If Lose"),
    ];

    /// <summary>
    /// Whether the engine has a method for a number, which is what
    /// <c>executeCommand</c> asks: <c>methodName =
    /// "command" + params.code; if (typeof this[methodName] === "function")
    /// { … }</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Being in this list and having a method are two different things</b>,
    /// and the difference is the whole of how MZ dispatches a command.
    /// </para>
    ///
    /// <para>
    /// The list above is the 178 numbers MZ reserves. <b>Fourteen of them have
    /// no <c>commandNNN</c> method</b>, and a game writes them all the time:
    /// <c>0</c> the end of a block, <c>401</c> a line of text, <c>404</c> the
    /// end of the choices, <c>405</c> the choices, <c>412</c> the end of a
    /// branch, <c>505</c> a move route, <c>601</c> and <c>602</c> a battle's
    /// beginning and victory, <c>603</c> its end, <c>604</c> the escape from
    /// it, <c>605</c> its end, <c>657</c> a plugin's next step.
    /// </para>
    ///
    /// <para>
    /// <b>The engine steps over every one of them</b>, and that is not a
    /// failure: a 401 is read by position inside a 101's block, and a 505 is
    /// an entry inside a 205's list. A reader that reported them as unknown
    /// would stop a game over a command the game itself runs past a thousand
    /// times.
    /// </para>
    ///
    /// <para>
    /// <b>And a reader may still read them</b>, because the reader is asked a
    /// different question: not "does the engine dispatch this" but "what did
    /// the game write". Those are two questions with two answers, and
    /// collapsing them is how a reader either stops a game that is running
    /// fine or claims a game has no text in it.
    /// </para>
    /// </remarks>
    public static bool HasMethod(int pCode) => All.Any(c => c.Code == pCode)
        && NoMethodCodes.Contains(pCode) == false;

    /// <summary>
    /// The nine numbers this reader has measured as having no
    /// <c>commandNNN</c> method in MZ 1.9.1. <b>All nine lie outside the
    /// hundred and fourteen above</b>, which is why the check has two terms.
    /// </summary>
    public static readonly IReadOnlyCollection<int> NoMethodCodes = new[]
    {
        0, 401, 404, 405, 412, 505, 604, 605, 657,
    };

    /// <summary>Every command, in the order the engine declares them.</summary>
    public static IReadOnlyList<MzCommand> Commands => All;
}
