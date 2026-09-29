using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The RM2K command coverage, measured rather than written down.
/// </summary>
/// <remarks>
/// <strong>The class is <c>partial</c> because <c>TestBase</c> derives from
/// <c>GodotObject</c></strong>, and Godot's source generator refuses a
/// non-partial one. That error is <c>GD0001</c> and <strong>not a
/// <c>CS</c> one</strong> — a build check that counts <c>error CS</c> alone
/// reports a clean build for a file that never compiled, and that is how a
/// new suite stays invisible while every other suite stays green.
/// </remarks>
public partial class TestRm2kCommandCoverage : TestBase
{
    /// <summary>
    /// The names liblcf gives to its base enumeration's command codes.
    /// </summary>
    /// <remarks>
    /// <strong>This is the reference list, and it is short on purpose.</strong>
    /// liblcf's base enumeration stops at the game's own commands — the
    /// Maniac and EasyRPG patch extensions are a different thing and are not
    /// in it. **A list that also held the patch codes would claim this
    /// runtime implements patches, and it does not.**
    /// </remarks>
    /// <summary>
    /// liblcf's own base enumeration, code by code, with its names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>These are liblcf's own values and not a pattern.</strong> The
    /// first version of this list was written out by hand as a run of
    /// five-digit numbers, **and the test caught it: it named four hundred
    /// codes liblcf does not have and missed the thirty it does.** A hand-made
    /// list of "the codes the game uses" is a guess, **and a guess that reads
    /// like a measurement is the one thing a coverage check must not be.**
    /// </para>
    /// <para>
    /// <strong>164 entries in the enumeration, 117 of them at or above
    /// 10000.</strong> The rest are the game's own short codes: `END = 10`,
    /// `CallCommonEvent = 1005`, `ForceFlee = 1006`, `EnableCombo = 1007`,
    /// `ChangeClass = 1008`, `ChangeBattleCommands = 1009`, and the `5001`
    /// to `5005` menues. They are not in this list because the interpreter
    /// files its own constants for them elsewhere, **and a list that mixed
    /// the two would have counted one command twice.**
    /// </para>
    /// </remarks>
    private static readonly int[] LiblcfCodes =
    [
        10110, // ShowMessage
        10120, // MessageOptions
        10130, // ChangeFaceGraphic
        10140, // ShowChoice
        10150, // InputNumber
        10210, // ControlSwitches
        10220, // ControlVars
        10230, // TimerOperation
        10310, // ChangeGold
        10320, // ChangeItems
        10330, // ChangePartyMembers
        10410, // ChangeExp
        10420, // ChangeLevel
        10430, // ChangeParameters
        10440, // ChangeSkills
        10450, // ChangeEquipment
        10460, // ChangeHP
        10470, // ChangeSP
        10480, // ChangeCondition
        10490, // FullHeal
        10500, // SimulatedAttack
        10610, // ChangeHeroName
        10620, // ChangeHeroTitle
        10630, // ChangeSpriteAssociation
        10640, // ChangeActorFace
        10650, // ChangeVehicleGraphic
        10660, // ChangeSystemBGM
        10670, // ChangeSystemSFX
        10680, // ChangeSystemGraphics
        10690, // ChangeScreenTransitions
        10710, // EnemyEncounter
        10720, // OpenShop
        10730, // ShowInn
        10740, // EnterHeroName
        10810, // Teleport
        10820, // MemorizeLocation
        10830, // RecallToLocation
        10840, // EnterExitVehicle
        10850, // SetVehicleLocation
        10860, // ChangeEventLocation
        10870, // TradeEventLocations
        10910, // StoreTerrainID
        10920, // StoreEventID
        11010, // EraseScreen
        11020, // ShowScreen
        11030, // TintScreen
        11040, // FlashScreen
        11050, // ShakeScreen
        11060, // PanScreen
        11070, // WeatherEffects
        11110, // ShowPicture
        11120, // MovePicture
        11130, // ErasePicture
        11210, // ShowBattleAnimation
        11310, // PlayerVisibility
        11320, // FlashSprite
        11330, // MoveEvent
        11340, // ProceedWithMovement
        11350, // HaltAllMovement
        11410, // Wait
        11510, // PlayBGM
        11520, // FadeOutBGM
        11530, // MemorizeBGM
        11540, // PlayMemorizedBGM
        11550, // PlaySound
        11560, // PlayMovie
        11610, // KeyInputProc
        11710, // ChangeMapTileset
        11720, // ChangePBG
        11740, // ChangeEncounterSteps
        11750, // TileSubstitution
        11810, // TeleportTargets
        11820, // ChangeTeleportAccess
        11830, // EscapeTarget
        11840, // ChangeEscapeAccess
        11910, // OpenSaveMenu
        11930, // ChangeSaveAccess
        11950, // OpenMainMenu
        11960, // ChangeMainMenuAccess
        12010, // ConditionalBranch
        12110, // Label
        12120, // JumpToLabel
        12210, // Loop
        12220, // BreakLoop
        12310, // EndEventProcessing
        12320, // EraseEvent
        12330, // CallEvent
        12410, // Comment
        12420, // GameOver
        12510, // ReturntoTitleScreen
        13110, // ChangeMonsterHP
        13120, // ChangeMonsterMP
        13130, // ChangeMonsterCondition
        13150, // ShowHiddenMonster
        13210, // ChangeBattleBG
        13260, // ShowBattleAnimation_B
        13310, // ConditionalBranch_B
        13410, // TerminateBattle
        20110, // ShowMessage_2
        20140, // ShowChoiceOption
        20141, // ShowChoiceEnd
        20710, // VictoryHandler
        20711, // EscapeHandler
        20712, // DefeatHandler
        20713, // EndBattle
        20720, // Transaction
        20721, // NoTransaction
        20722, // EndShop
        20730, // Stay
        20731, // NoStay
        20732, // EndInn
        22010, // ElseBranch
        22011, // EndBranch
        22210, // EndLoop
        22410, // Comment_2
        23310, // ElseBranch_B
        23311, // EndBranch_B
    ];

    /// <summary>
    /// The interpreter's own source, read as text.
    /// </summary>
    /// <remarks>
    /// <strong>This reads the file and does not run it.</strong> The coverage
    /// claim is about which names the dispatch compares, and that is a fact
    /// about the text — **and a reader that asked the running interpreter
    /// would have had no way to ask "which codes do you have a case
    /// for".**
    /// </remarks>
    private string InterpreterSource()
    {
        // **Eine `res://`-URL und kein Windows-Pfad.** `File.ReadAllText`
        // wuerde "Die Syntax fuer den Dateinamen ist falsch" werfen, **und
        // das ist der zweite Leser, der an dieser Stelle schon gescheitert
        // ist** -- der erste war der .NET-Pfad statt der Godot-URL.
        var pfad = "res://src/rm2k/interpreter/EventInterpreter.cs";
        var bytes = Godot.FileAccess.GetFileAsBytes(pfad);
        AssertTrue(bytes != null && bytes.Length > 0,
            $"**the interpreter source is there to read**; it is at {pfad}");
        return System.Text.Encoding.UTF8.GetString(bytes!);
    }

    /// <summary>
    /// The codes the interpreter's own dispatch reaches.
    /// </summary>
    private HashSet<int> Dispatched()
    {
        var quelle = InterpreterSource();
        var konstanten = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(quelle, @"public const int (\w+) = (\d+);"))
        {
            konstanten[m.Groups[1].Value] = int.Parse(m.Groups[2].Value,
                System.Globalization.CultureInfo.InvariantCulture);
        }

        // **Nur was verglichen wird, ist verdrahtet.** Eine Konstante, die
        // nirgends vorkommt, ist eine Zahl und kein Befehl -- und genau
        // darum hat die fruehere Zaehlung vierundzwanzig Luecken gemeldet,
        // die keine waren.
        var benutzt = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(quelle, @"(?:==|case)\s+(\w+)\b"))
        {
            benutzt.Add(m.Groups[1].Value);
        }

        var raus = new HashSet<int>();
        foreach (var paar in konstanten)
        {
            if (paar.Value >= 10000 && benutzt.Contains(paar.Key))
            {
                raus.Add(paar.Value);
            }
        }

        return raus;
    }

    /// <summary>
    /// Every code liblcf names reaches a dispatch, and nothing else does.
    /// </summary>
    /// <remarks>
    /// <strong>This is the check that closes the card, and it is in code.</strong>
    /// Three numbers were written into the card's evidence over time and none
    /// of them agreed with the next — **and a coverage claim that is only a
    /// sentence goes stale the moment a command is added.** This fails on the
    /// first command the interpreter gains without the reference list, and on
    /// the first reference code the interpreter never gains.
    /// </remarks>
    public void Test_EveryLiblcfCodeReachesTheDispatchAndNothingElseDoes()
    {
        var verdrahtet = Dispatched();
        var soll = new HashSet<int>(LiblcfCodes);
        var fehlt = soll.Except(verdrahtet).OrderBy(x => x).ToList();
        var zu_viel = verdrahtet.Except(soll).OrderBy(x => x).ToList();

        AssertTrue(fehlt.Count == 0,
            "**every code liblcf names reaches the dispatch**; missing: "
                + string.Join(", ", fehlt));
        AssertTrue(zu_viel.Count == 0,
            "**and the dispatch holds nothing liblcf does not name**; extra: "
                + string.Join(", ", zu_viel));
        AssertEq(verdrahtet.Count, soll.Count,
            "**so the two sets are the same size** — a coverage number that "
                + "counts the same commands twice is not a coverage number");
    }

    /// <summary>
    /// The operation codes are values and not commands, and they are not in
    /// the count.
    /// </summary>
    /// <remarks>
    /// <strong>Fourteen operation codes and two sentinels sit beside the
    /// commands in the same file.</strong> A reader that counted every constant
    /// as a command would have reported gaps that are not gaps, **and a reader
    /// that left them out by name would have had to be edited every time a new
    /// one was added** — which is how the three disagreeing numbers got
    /// there.
    /// </remarks>
    public void Test_TheOperationCodesAreNotCommands()
    {
        var quelle = InterpreterSource();
        var operationen = new[]
        {
            "VarOpAdd", "VarOpSet", "VarOpMul", "GoldOpAdd", "GoldOpSubtract",
            "ItemOpAdd", "PartyOpRemove", "FlashSubOnce", "FlashSubBegin",
            "ShakeSubOnce", "ShakeSubBegin", "SwitchModeFlip",
            "MaxWaitFrames", "MaxGold", "MaxItemCount", "MaxItemId",
            "MaxScriptRecursion",
        };
        foreach (var name in operationen)
        {
            AssertTrue(
                Regex.IsMatch(quelle, @"public const int " + name + @" = \d+;"),
                $"**the operation code {name} is declared** — it is a value and "
                    + "not a command, and the coverage count has to leave it "
                    + "out on purpose and not by accident");
        }

        // **Und keine von ihnen zaehlt als Befehl.** Sie sind nicht im
        // Dispatch, denn `Dispatched` nimmt nur Namen, die verglichen werden
        // -- **und eine Operation wird nie gegen `case` verglichen.**
        var verdrahtet = Dispatched();
        foreach (var name in operationen)
        {
            var wert = int.Parse(Regex.Match(quelle,
                @"public const int " + name + @" = (\d+);").Groups[1].Value,
                System.Globalization.CultureInfo.InvariantCulture);
            AssertTrue(!verdrahtet.Contains(wert),
                $"**and {name} is not counted as a command** — a value in the "
                    + "same file is still a value");
        }
    }
}
