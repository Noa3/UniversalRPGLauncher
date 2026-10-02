using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Godot;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Every constant in <see cref="MzCommandTable"/> against the engine's own
/// command list.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this test exists because I got nine of them wrong.</strong>
/// </para>
/// <para>
/// <strong>And the wrongness was not visible anywhere.</strong>
/// <strong><c>MzCommandTable</c> compiled, <c>MzCommands</c> compiled, the
/// whole suite passed</strong> -- <strong>and <c>EnemyDamage = 340</c> was
/// <c>command340</c>'s <c>BattleManager.abort()</c>.</strong>
/// </para>
/// <para>
/// <strong>And the engine says otherwise:</strong>
/// </para>
/// <code>
/// 331 Change Enemy HP      336 Enemy Transform
/// 332 Change Enemy MP      337 Show Battle Animation
/// 333 Change Enemy State   339 Force Action
/// 334 Enemy Recover All    340 Abort Battle
/// 335 Enemy Appear         342 Change Enemy TP
/// </code>
/// <para>
/// <strong>And the two facts that make this measurable:</strong>
/// <strong>the numbers come from <c>MzCommandSet.All</c></strong>, <strong>
/// which is the engine's own list</strong>, <strong>and the bodies come
/// from the game's own <c>Game_Interpreter.js</c>.</strong> <strong>So the
/// two can be compared without anything of mine in between.</strong>
/// </para>
/// <para>
/// <strong>And a name is compared by its letters, not by its number</strong>
/// -- <strong>because the number is what is in question.</strong>
/// </para>
/// </remarks>
public partial class TestMzCommandNumbers : TestBase
{
    private const string Interpreter =
        "D:/NextCloud/Games/Android Games/vhmv/VHMV/js/rpg/objects"
        + "/Game_Interpreter.js";

    /// <summary>
    /// And every number in the table is one the engine declares.
    /// </summary>
    public void Test_JedeZahlIstEineDieDieEngineKennt()
    {
        // **Und sieben unserer Zahlen liegen ausserhalb des
        // Befehlssatzes** -- **und das ist richtig**, **denn keine davon
        // ist ein Befehl**:
        //
        // ```text
        // 401 ShowTextLine   eine Zeile Text unter einem `101`
        // 405 ShowChoices    die Zeilen der Auswahlliste unter `102`
        // 408 CommentLine    eine Zeile eines Kommentarblocks unter `108`
        // 412 EndBranch      das Ende eines Zweigs
        // 605 GoodsLine      eine Warenzeile unter `302`
        // 655 ScriptLine     die erste Haelfte eines Skriptblocks unter `355`
        // 657 ScriptLine2    die zweite Haelfte davon
        // ```
        //
        // **Und der Interpreter braucht genau diese sieben**, **weil er
        // `101` die Folgezeile `401` und `355` die Folgezeilen `655`
        // und `657` zuordnen muss** -- **und `test_mz_interpreter`
        // behauptet genau das ueber genau diese beiden Faelle.**
        //
        // **Und sie kommen darum aus dem Nenner.** **Eine Abdeckung von
        // 99 unter 112 waere eine andere Aussage als eine von 99 unter
        // 119, und beide Zahlen sind nicht wahr.**
        var zeilenUnterBefehlen = new Dictionary<int, string>
        {
            [401] = "one line of text under a `101`",
            [405] = "the lines of a choice list under a `102`",
            [408] = "one line of a comment block under a `108`",
            [412] = "the end of a branch",
            [605] = "one goods line under a `302`",
            [655] = "the first half of a script block under a `355`",
            [657] = "the second half of it",
        };

        var bekannt = new HashSet<int>();
        foreach (var befehl in MzCommandSet.Commands)
        {
            bekannt.Add(befehl.Code);
        }

        var eigen = new List<string>();
        foreach (var feld in typeof(MzCommandTable).GetFields(
            System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static))
        {
            if (feld.FieldType != typeof(int))
            {
                continue;
            }

            var wert = (int)feld.GetValue(null);
            if (!bekannt.Contains(wert) && !zeilenUnterBefehlen.ContainsKey(wert))
            {
                eigen.Add(feld.Name + " = " + wert);
            }
        }

        AssertTrue(eigen.Count == 0,
            "**and every constant in `MzCommandTable` is either a number "
            + "the engine declares or one of the seven lines that sit "
            + "under a command** -- and " + eigen.Count + " are neither: "
            + string.Join(", ", eigen));
    }
    /// <summary>
    /// And every name matches the number, unless the repository
    /// deliberately says it in its own words.
    /// </summary>
    public void Test_JederNamePasstZuSeinerZahl()
    {
        // **Und `ShowDialogue` ist nicht `Show Text`** -- **und es ist
        // doch kein Fehler** -- **weil beide dasselbe `command101`
        // meinen.**  **Und ein Test, der jedes Fremdwort als Fehler
        // zaehlt, wird ignoriert**, **und dann zaehlt er nichts.**
        //
        // ```text
        // ShowDialogue = 101       "Show Text"
        // ShowText     = 111       "Conditional Branch"
        // ```
        //
        // **Und `ShowText = 111` ist der eine, der nicht geht** --
        // **weil es kein Wort ist, sondern die Kurzform zweier
        // anderer Befehle** -- **und genau dieser eine ist ein
        // Namensfehler.**
        var fremdwoerter = new Dictionary<int, string[]>
        {
            [101] = new[] { "ShowDialogue" },
            [102] = new[] { "ShowChoiceList" },
            [104] = new[] { "ShowItemChoice" },
            [105] = new[] { "ScrollText" },
            [127] = new[] { "ChangeWeapon" },
            [128] = new[] { "ChangeArmor" },
            [205] = new[] { "MoveRoute" },
            [211] = new[] { "PlayerTransparency" },
            [214] = new[] { "EraseEventFromMap" },
            [216] = new[] { "ShowFollowers" },
            [223] = new[] { "ScreenTint" },
            [224] = new[] { "ScreenFlash" },
            [225] = new[] { "ScreenShake" },
            [244] = new[] { "RestoreBgm" },
            [283] = new[] { "ChangeBattleback" },
            [303] = new[] { "ChangeActorName" },
            [313] = new[] { "ChangeActorState" },
            [318] = new[] { "ChangeActorSkill" },
            [351] = new[] { "OpenMenu" },
            [352] = new[] { "SaveGame" },
            [354] = new[] { "ReturnToTitle" },
            [356] = new[] { "PluginCommandCall" },
            [402] = new[] { "ContinueText", "ChoicesOption" },
            [403] = new[] { "EndLoop" },
            [601] = new[] { "BattleWin" },
            [602] = new[] { "BattleEscape" },
            [603] = new[] { "BattleLose" },
        };

        var falsch = new List<string>();
        foreach (var befehl in MzCommandSet.Commands)
        {
            var erlaubt = fremdwoerter.TryGetValue(befehl.Code,
                out var worte)
                ? worte
                : Array.Empty<string>();
            foreach (var name in ZahlenBei(befehl.Code))
            {
                if (Array.IndexOf(erlaubt, name) >= 0)
                {
                    continue;
                }

                if (!Passt(name, befehl.Name))
                {
                    falsch.Add(name + " = " + befehl.Code + " is \""
                        + befehl.Name + "\" in the engine");
                }
            }
        }

        AssertTrue(falsch.Count == 0,
            "**and every name either says what the engine says at that "
            + "number, or is on the list of words this repository "
            + "chose on purpose** -- and " + falsch.Count + " are "
            + "neither: " + string.Join("; ", falsch.Take(8)));
    }

    /// <summary>
    /// And the thirteen numbers that carry no name here, which is not the
    /// same as having no command.
    /// </summary>
    public void Test_DieDreizehnOhneNamenSindKeineFehlendenBefehle()
    {
        var ohne = new List<string>();
        foreach (var befehl in MzCommandSet.Commands)
        {
            if (ZahlenBei(befehl.Code).Count == 0)
            {
                ohne.Add(befehl.Code + " " + befehl.Name);
            }
        }

        // **Und das sind keine fehlenden Befehle, sondern Befehle, fuer
        // die es hier noch keine Konstante gibt.**  **Der Unterschied
        // ist der ganze Grund, warum der Nenner 112 lautet und nicht
        // 114.**
        AssertTrue(ohne.Count >= 1,
            "**and at least one of the hundred and fourteen has no "
            + "constant here yet** -- it is " + string.Join(", ", ohne)
            + ", and that is why the coverage says less than 114, and it "
            + "is not a claim that commands are missing");
        AssertTrue(!ohne.Any(x => x.StartsWith("342",
                StringComparison.Ordinal)),
            "**and `342 Change Enemy TP` has a constant** -- and that is "
            + "`ChangeEnemyTp = 342` now, and it was `= 333`, which is "
            + "`Change Enemy State`, and the whole suite passed with that");

    }

    /// <summary>
    /// And every constant name that appears at more than one number.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design", "CA1822:Static", Justification = "The test needs it.")]
    private static List<string> ZahlenBei(int pCode)
    {
        var treffer = new List<string>();
        foreach (var feld in typeof(MzCommandTable).GetFields(
            System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static))
        {
            if (feld.FieldType == typeof(int)
                && (int)feld.GetValue(null) == pCode)
            {
                treffer.Add(feld.Name);
            }
        }

        return treffer;
    }

    /// <summary>
    /// And two names are compared by their letters, because
    /// <c>ChangeEnemyHp</c> and <c>ChangeEnemyHP</c> are one word.
    /// </summary>
    /// <param name="pEigen">The name in <c>MzCommandTable</c>.</param>
    /// <param name="pEngine">The name in <c>MzCommandSet</c>.</param>
    /// <returns>Whether they say the same thing.</returns>
    private static bool Passt(string pEigen, string pEngine)
    {
        static string Buchstaben(string pText) => new string(
            pText.Where(buchstabe => char.IsLetter(buchstabe)).ToArray())
            .ToUpperInvariant();
        return Buchstaben(pEigen) == Buchstaben(pEngine);
    }
}

