using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// RPG Maker MV's command numbers and names, as the engine itself writes
/// them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the source is a file, and not this repository.</strong> Every
/// name here is the comment RPG Maker MV puts above its own
/// <c>commandNNN</c> function, read out of <c>js/rpg_objects.js</c> of a
/// finished game on this machine.
/// </para>
/// <para>
/// <strong>And the finding is the opposite of what was expected.</strong>
/// <c>rpg_objects.js</c> defines one hundred and twelve <c>commandNNN</c>
/// functions, <strong>and every one of them is a number this repository's
/// MZ table already carries</strong> -- <c>MzCommandSet.Commands</c> has
/// one hundred and fourteen. <strong>MV is a branch of the same command
/// set and not a second one,</strong> <strong>and MV therefore needs no
/// command table of its own.</strong>
/// </para>
/// <para>
/// <strong>And that was not the first count.</strong> A first reading took
/// the MZ table's forty-eight public <c>const</c> fields for its size, found
/// seventy of MV's numbers missing, and wrote it down.
/// <strong>Forty-eight is the number of named constants; one hundred and
/// fourteen is the number the interpreter runs.</strong> <strong>The
/// difference between those two numbers is a lesson, and it is recorded
/// here because this table would otherwise have carried a wrong claim in
/// its own documentation.</strong>
/// </para>
/// <para>
/// <strong>And three numbers say where the two engines differ.</strong>
/// <c>109 Skip</c> is MZ's and MV's engine has no method for it;
/// <c>111</c> is MZ's Conditional Branch and MV's continuation line of a
/// 101; <strong>and <c>356</c> is MZ's <c>Plugin Command MV
/// (deprecated)</c> and MV's <c>Plugin Command</c>.</strong> <strong>And
/// MZ's text lines, choice lines, branch end and script lines -- <c>401</c>,
/// <c>405</c>, <c>412</c>, <c>655</c>, <c>657</c> -- are not in this table
/// because MV's engine gives them no method either, and MV carries them in
/// a finished game's files anyway.</strong>
/// </para>
/// </remarks>
public static class MvCommandTable
{
    /// <summary>
    /// MV's own numbers, with the names its own source gives them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a transcription, not a table written from
    /// memory.</strong> Every row was read out of <c>rpg_objects.js</c>,
    /// <strong>and a number in a finished game that is missing here is a gap
    /// in this list and not in the engine.</strong>
    /// </para>
    /// </remarks>
    public static readonly IReadOnlyDictionary<int, string> Names =
        new Dictionary<int, string>
        {
            [101] = "Show Text",
            [102] = "Show Choices",
            [103] = "Input Number",
            [104] = "Select Item",
            [105] = "Show Scrolling Text",
            [108] = "Comment",
            [111] = "Continuation",
            [112] = "Loop",
            [113] = "Break Loop",
            [115] = "Exit Event Processing",
            [117] = "Common Event",
            [118] = "Label",
            [119] = "Jump to Label",
            [121] = "Control Switches",
            [122] = "Control Variables",
            [123] = "Control Self Switch",
            [124] = "Control Timer",
            [125] = "Change Gold",
            [126] = "Change Items",
            [127] = "Change Weapons",
            [128] = "Change Armors",
            [129] = "Change Party Member",
            [132] = "Change Battle BGM",
            [133] = "Change Victory ME",
            [134] = "Change Save Access",
            [135] = "Change Menu Access",
            [136] = "Change Encounter Disable",
            [137] = "Change Formation Access",
            [138] = "Change Window Color",
            [139] = "Change Defeat ME",
            [140] = "Change Vehicle BGM",
            [201] = "Transfer Player",
            [202] = "Set Vehicle Location",
            [203] = "Set Event Location",
            [204] = "Scroll Map",
            [205] = "Set Movement Route",
            [206] = "Getting On and Off Vehicles",
            [211] = "Change Transparency",
            [212] = "Show Animation",
            [213] = "Show Balloon Icon",
            [214] = "Erase Event",
            [216] = "Change Player Followers",
            [217] = "Gather Followers",
            [221] = "Fadeout Screen",
            [222] = "Fadein Screen",
            [223] = "Tint Screen",
            [224] = "Flash Screen",
            [225] = "Shake Screen",
            [230] = "Wait",
            [231] = "Show Picture",
            [232] = "Move Picture",
            [233] = "Rotate Picture",
            [234] = "Tint Picture",
            [235] = "Erase Picture",
            [236] = "Set Weather Effect",
            [241] = "Play BGM",
            [242] = "Fadeout BGM",
            [243] = "Save BGM",
            [244] = "Resume BGM",
            [245] = "Play BGS",
            [246] = "Fadeout BGS",
            [249] = "Play ME",
            [250] = "Play SE",
            [251] = "Stop SE",
            [261] = "Play Movie",
            [281] = "Change Map Name Display",
            [282] = "Change Tileset",
            [283] = "Change Battle Back",
            [284] = "Change Parallax",
            [285] = "Get Location Info",
            [301] = "Battle Processing",
            [302] = "Shop Processing",
            [303] = "Name Input Processing",
            [311] = "Change HP",
            [312] = "Change MP",
            [313] = "Change State",
            [314] = "Recover All",
            [315] = "Change EXP",
            [316] = "Change Level",
            [317] = "Change Parameter",
            [318] = "Change Skill",
            [319] = "Change Equipment",
            [320] = "Change Name",
            [321] = "Change Class",
            [322] = "Change Actor Images",
            [323] = "Change Vehicle Image",
            [324] = "Change Nickname",
            [325] = "Change Profile",
            [326] = "Change TP",
            [331] = "Change Enemy HP",
            [332] = "Change Enemy MP",
            [333] = "Change Enemy State",
            [334] = "Enemy Recover All",
            [335] = "Enemy Appear",
            [336] = "Enemy Transform",
            [337] = "Show Battle Animation",
            [339] = "Force Action",
            [340] = "Abort Battle",
            [342] = "Change Enemy TP",
            [351] = "Open Menu Screen",
            [352] = "Open Save Screen",
            [353] = "Game Over",
            [354] = "Return to Title Screen",
            [355] = "Script",
            [356] = "Plugin Command",
            [402] = "When [**]",
            [403] = "When Cancel",
            [411] = "Else",
            [413] = "Repeat Above",
            [601] = "If Win",
            [602] = "If Escape",
            [603] = "If Lose",

        };

    /// <summary>The name MV's own source gives this number, or empty.</summary>
    public static string NameOf(int pCode) =>
        Names.TryGetValue(pCode, out var name) ? name : "";

    /// <summary>Whether MV's engine has a method for this number.</summary>
    public static bool IsCommand(int pCode) => Names.ContainsKey(pCode);
}
