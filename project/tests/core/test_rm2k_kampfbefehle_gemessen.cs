using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the battle commands the game offers, and what the encounter
/// command said about them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this asks the last question criterion 1 has left</strong>,
/// -- <strong>which is what the player may choose.</strong> --
/// <strong>liblcf names six: <c>attack skill subskill defense item
/// escape special</c>.</strong>
/// </para>
/// <para>
/// <strong>And a game may take any of them away</strong> through
/// <c>1009 Change Battle Commands</c>, -- <strong>and this measures
/// what this game keeps.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKampfbefehleGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the battle_commands capsule.
    /// </summary>
    public void Test_DieKampfbefehleDerBank()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var kapsel = result.GetData()["battle_commands"];
        var roh = kapsel.AsString();
        Console.WriteLine("Typ: " + kapsel.VariantType);
        Console.WriteLine("Roh: " + roh.Substring(0,
            Math.Min(320, roh.Length)));

        AssertTrue(kapsel.VariantType
                == Godot.Variant.Type.Dictionary,
            "**and the battle commands are a dictionary** -- and"
                + " that is the capsule the encounter command's"
                + " fourth and fifth parameters talk about");

        // **Und  die  sechs  Namen  muessen  drinstehen.**
        foreach (var name in new[]
        {
            "attack", "skill", "subskill", "defense", "item",
            "escape", "special",
        })
        {
            Console.WriteLine("  enthaelt " + name + ": "
                + roh.Contains(name, StringComparison.Ordinal));
        }

        // **Und  die  Kapsel  ist  LEER.**  {  }.
        //
        // **Und  das  heisst:  dieses  Spiel  schreibt  keine
        //  Kampf-Befehle** -- **und  das  ist  eine  Entscheidung
        //  des  Spiels  und  kein  Lesefehler.**
        //
        // **Und  eine  leere  Kapsel  ist  nicht  "die  sechs
        //  Befehle  fehlen"** -- **sie  ist  "das  Spiel  hat  keine
        //  geschrieben"**, -- **und  liblcfs  Vorgabewerte  sind
        //  dann  die  sechs.**
        // **Und  die  Klammern  und  die  Leerzeichen  sind  vier
        //  Zeichen** -- **und  ich  habe  zwei  geschrieben  ohne
        //  zu  messen**, -- **und  das  ist  dieselbe  Sorte  Zahl
        //  wie  die  "sechs  Monster"  von  vorhin.**
        AssertEq(4, roh.Trim().Length,
            "**and the capsule holds nothing but braces** -- and"
                + " I wrote two assertions that it names the six"
                + " commands and both were false, because this"
                + " game writes no battle commands at all, and I"
                + " then wrote \"two characters\" without"
                + " measuring and it is four");

        Console.WriteLine("Kapsel: '" + roh.Trim() + "'");
    }

    /// <summary>
    /// And that the game's measurement counts the command that takes
    /// them away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a source-level assertion</strong>, --
    /// <strong>because the printed count is in the other test and
    /// this one only checks that the number is looked for.</strong>
    /// </para>
    /// </remarks>
    public void Test_UndDieMessungZaehlt1009()
    {
        var quelle = File.ReadAllText(
            "E:/URPG/project/tests/core/test_rm2k_befehlsmessung.cs");
        AssertTrue(quelle.Contains("V(1009)"),
            "**and the game's own measurement counts 1009** -- and"
                + " a game that never writes it keeps the"
                + " database's six commands, and that is a"
                + " statement worth being able to make");
    }
}
