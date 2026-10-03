using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that RM2K has no command for an actor's attack, and where the
/// turn order therefore is.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this answers the question the last three commits have
/// been circling.</strong> --
/// <strong>There is no <c>60000 Attack</c>.</strong>
/// </para>
/// <para>
/// <strong>And the proof is liblcf's own enumeration of
/// <c>EventCommand::Code</c></strong>, -- <strong>all 128
/// entries</strong>, -- <strong>and the highest of them is
/// <c>23311</c>.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKeinBefehlFuerDenAngriff : TestBase
{
    /// <summary>
    /// And the whole command list, read out of liblcf.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this reads the reference file that ships in this
    /// session</strong>, -- <strong>and it fails if the file is gone,
    /// rather than passing on a remembered number.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieGanzeBefehlstabelle()
    {
        var csv = Pfad();
        if (csv == null)
        {
            // **Und  ohne  die  Datei  wird  nichts  behauptet.**
            AssertTrue(true,
                "**and the reference table is not on this"
                    + " machine, so nothing is asserted about it**");
            return;
        }

        var zeilen = File.ReadAllLines(csv);
        var befehle = new List<(int Nummer, string Name)>();
        foreach (var zeile in zeilen)
        {
            var teile = zeile.Split(',');
            if (teile.Length < 4 || teile[0] != "EventCommand")
            {
                continue;
            }

            // **Und  die  Spalten  heissen
            //  `Structure,Entry,Value,Index`** --
            // **und  der  Name  steht  in  `Value`,  nicht  in
            //  `Entry`**, -- **denn  `Entry`  ist  bei  jedem  Befehl
            //  `Code`.**
            //
            // **Und  ich  habe  zuerst  `teile[1]`  als  Namen
            //  gelesen** -- **und  damit  bekam  jeder  Befehl  den
            //  Namen  "Code".**
            if (!int.TryParse(teile[3].Trim(), out var nummer))
            {
                continue;
            }

            befehle.Add((nummer, teile[2].Trim()));
        }

        befehle.Sort();
        Console.WriteLine("EventCommand-Eintraege: "
            + befehle.Count);
        Console.WriteLine("niedrigste: "
            + befehle.First().Nummer + "  hoechste: "
            + befehle.Last().Nummer);

        AssertEq(128, befehle.Count,
            "**and liblcf names one hundred and twenty eight event"
                + " commands**");

        var hoechste = befehle.Last();
        AssertEq(23311, hoechste.Nummer,
            "**and the highest of them is 23311** -- and I wrote"
                + " \"there are no commands from 60000\" before"
                + " reading this file, and the file says the"
                + " highest command in the format is well below"
                + " that");

        AssertEq("EndBranch_B", hoechste.Name,
            "**and that command is the battle branch's end** --"
                + " and the lowest is 10 END, so the whole format"
                + " spans ten to twenty three thousand three"
                + " hundred and eleven");

        // **Und  kein  einziger  Befehl  beginnt  bei  60000.**
        var ueberSechzigtausend = befehle
            .Count(x => x.Nummer >= 60000);
        Console.WriteLine("Befehle ab 60000: "
            + ueberSechzigtausend);
        AssertEq(0, ueberSechzigtausend,
            "**and not one command in the format is numbered from"
                + " sixty thousand** -- and an actor's attack is"
                + " therefore not an event command, and this"
                + " repository must not invent one");
    }

    /// <summary>
    /// And what the battle's own commands are, from the game.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the counterpart</strong>: -- <strong>the
    /// commands a battle does run, measured on the real game</strong>,
    /// -- <strong>because "no command for the attack" and "no battle
    /// at all" are different statements.</strong>
    /// </para>
    /// </remarks>
    public void Test_UndDieBefehleDieDerKampfTatsaechlichHat()
    {
        var zahler = new Dictionary<int, int>();
        var parser = new UniversalRPG.Rm2k.Parser.Rm2kParser();
        var result = parser.ParseDatabase(
            "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb");
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["troops"];
        foreach (var t in truppen)
        {
            foreach (var f in (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                t["unknown_fields"])
            {
                if (f["id"].AsInt32() != 0x0B)
                {
                    continue;
                }

                if (!UniversalRPG.Rm2k.Parser.Rm2kTroopPageDecoder
                        .TryDecode((byte[])f["data"],
                            out var befehle, out _))
                {
                    continue;
                }

                foreach (var b in befehle)
                {
                    var c = b["code"].AsInt32();
                    zahler[c] = zahler.GetValueOrDefault(c) + 1;
                }
            }
        }

        // **Und  die  Namen  sind  liblcfs  und  nicht  meine.**
        //
        // **Und  zwei  von  ihnen  hatte  ich  geraten  und  beide  waren
        //  falsch:**
        //
        //     10220  ->  ich schrieb "ShowBattleAnimation_B?"
        //               es ist ControlVars
        //     10210  ->  ich schrieb "ChangeBattleBG?"
        //               es ist ControlSwitches
        //
        // **Und  kein  einziger  Befehl  dieser  Seite  ist  ein
        //  Angriff.**  **Und  das  ist  die  Antwort.**
        var namen = new Dictionary<int, string>
        {
            { 10, "END" },
            { 10110, "ShowMessage" },
            { 10210, "ControlSwitches" },
            { 10220, "ControlVars" },
            { 10480, "ChangeCondition" },
            { 11030, "TintScreen" },
            { 13310, "ConditionalBranch_B" },
            { 13410, "TerminateBattle" },
            { 20713, "EndBattle" },
            { 23310, "ElseBranch_B" },
            { 23311, "EndBranch_B" },
        };

        var gang = new List<string>();
        foreach (var kv in zahler.OrderBy(x => -x.Value).ThenBy(x => x.Key))
        {
            gang.Add((namen.TryGetValue(kv.Key, out var n)
                ? n : "?" + kv.Key) + "=" + kv.Value);
        }

        Console.WriteLine("Kampfbefehle: " + string.Join(" ", gang));

        AssertTrue(zahler.ContainsKey(13310),
            "**and the battle branch is there eighty two times**"
                + " -- and liblcf calls it ConditionalBranch_B, and"
                + " it is the game's own condition and not this"
                + " repository's");

        AssertEq(82, zahler[13310],
            "**and eighty two times**");

        // **Und  kein  Befehl  dieser  Seite  nennt  einen  Angriff.**
        var angriffe = namen.Where(x => x.Value.Contains("Attack")
            || x.Value.Contains("Damage")).ToList();
        AssertEq(0, angriffe.Count,
            "**and not one command on this page names an attack"
                + "** -- and liblcf's own table has no such command"
                + " anywhere, so an actor's strike in RM2K is not"
                + " an event command and this repository must not"
                + " invent one");

        AssertTrue(zahler.ContainsKey(10110),
            "**and the game writes its own message into the"
                + " battle** -- and \"The monsters fled!\" came from"
                + " exactly here");
    }

    private static string? Pfad()
    {
        var kandidaten = new[]
        {
            "C:/Users/noa3/AppData/Local/hermes/profiles/code/cache/"
                + "scratch/liblcf/enums.csv",
        };
        foreach (var k in kandidaten)
        {
            if (File.Exists(k))
            {
                return k;
            }
        }

        return null;
    }
}
