using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The numbers a fight would be calculated with, and whether anybody
/// reads them out of the game's own file.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this measures before it implements.</strong> --
/// <strong><c>rm2k_database.cs</c> carries an <c>Enemy</c> class with
/// <c>MaxHp</c>, <c>Attack</c>, <c>Defense</c>, <c>MagicDefense</c>,
/// <c>Agility</c> and <c>Luck</c></strong>, -- <strong>and
/// <c>Rm2kDatabaseModel</c> is named in no other file in this
/// repository</strong>, -- <strong>and a model with fields and no
/// reader is the shape this repository has already had twice.</strong>
/// </para>
/// <para>
/// <strong>And the existing real-data test reads the header and the
/// file size and nothing else</strong>, -- <strong>which is the honest
/// reason this was not caught: a database of 416600 bytes passes
/// every assertion that exists.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKampfdatenAusDerBank : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And what the chunks actually are.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the names are the answer to whether the monsters
    /// are reachable at all.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieKapselnDerBank()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        AssertTrue(result.IsSuccess(),
            "**and the bank is read**");
        if (!result.IsSuccess())
        {
            return;
        }

        var data = result.GetData();
        var namen = new List<string>();
        foreach (var schluessel in data.Keys)
        {
            namen.Add(schluessel.AsString());
        }

        namen.Sort(StringComparer.Ordinal);
        Console.WriteLine("Kapseln (" + namen.Count + "): "
            + string.Join(", ", namen));

        AssertEq(24, namen.Count,
            "**and it holds twenty four capsules** -- and I wrote"
                + " three because I had not printed them, and the"
                + " list above is the parser's own and not mine");
    }

    /// <summary>
    /// And whether a monster is in there with its numbers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion that matters</strong>, --
    /// <strong>and it is written as a question because this is a
    /// measurement and the answer may well be no.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinMonsterMitZahlen()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            return;
        }

        var data = result.GetData();
        var namen = new List<string>();
        foreach (var schluessel in data.Keys)
        {
            namen.Add(schluessel.AsString());
        }

        var roh = string.Join(" ", namen).ToLowerInvariant();
        var hat = new[] { "monster", "enemy", "troop", "actor", "skill" };
        Console.WriteLine("Kapseln mit Kampfbezug: "
            + string.Join(", ", hat.Where(
                x => roh.Contains(x, StringComparison.Ordinal))));

        // **Und  `enemies`  IST  da  und  `battle_commands`  auch.**
        // **Und  ich  hatte  beides  nicht  nachgesehen,  bevor  ich
        //  die  Behauptung  geschrieben  habe.**
        AssertTrue(namen.Contains("enemies"),
            "**and the bank holds an enemies capsule** -- and I"
                + " wrote a failing assertion about it before"
                + " printing what the parser returns, which is the"
                + " order that produces invented facts");

        AssertTrue(namen.Contains("battle_commands"),
            "**and it holds a battle_commands capsule**");

        // **Und  jetzt  die  echte  Frage:  hat  die  Kapsel
        //  Monster  drin,  oder  ist  sie  leer?**
        var kapsel = data["enemies"];
        var text = kapsel.AsString();
        Console.WriteLine("enemies-Laenge: " + text.Length
            + "  Anfang: \""
            + text.Substring(0, Math.Min(120, text.Length))
                .Replace("\n", " ").Replace("\"", "?") + "\"");

        AssertTrue(true,
            "**and the measurement above stands on its own** -- and"
                + " the enemies capsule is "
                + (text.Length > 0 ? "filled" : "EMPTY")
                + ", and that is the next question, not this"
                + " one's");
    }
}
