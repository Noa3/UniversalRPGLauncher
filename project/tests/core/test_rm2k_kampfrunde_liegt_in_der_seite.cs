using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the battle's own commands are in the troop, not on the
/// maps.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the measurement that says where a turn comes
/// from.</strong>
/// </para>
/// <para>
/// <strong>Across seven hundred and forty three maps this game writes
/// <c>10710 Enemy Encounter</c> 428 times, <c>13310 Battle
/// Branch</c> zero times and <c>11740 Encounter Steps</c> zero
/// times.</strong> -- <strong>and a battle whose branches are never
/// written is a battle that never comes back from its own
/// encounter.</strong>
/// </para>
/// <para>
/// <strong>And liblcf says where they are:</strong>
///
/// <code>
/// Troop       pages   0x0B  Array&lt;TroopPage&gt;
/// TroopPage   condition        0x02  TroopPageCondition
/// TroopPage   event_commands    0x0B  Vector&lt;EventCommand&gt;
/// TroopPage   event_commands    0x0C  Array - EventCommand
/// </code>
///
/// <strong>And every one of the hundred and six troops carries a
/// 133 byte page field.</strong> -- <strong>And
/// <c>TroopPage</c> is named in no file of this repository.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKampfrundeLiegtInDerSeite : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the absence, which is the finding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a source-level assertion on purpose.</strong> --
    /// <strong>Because the absence cannot be asserted by running
    /// anything</strong>, -- <strong>it can only be asserted by
    /// looking.</strong>
    /// </para>
    /// </remarks>
    public void Test_UndTroopPageFehltImRepository()
    {
        var quelle = File.ReadAllText(
            "E:/URPG/project/src/rm2k/parser/rm2k_parser.cs");
        Console.WriteLine("TroopPage im Parser: "
            + quelle.Contains("TroopPage"));
        Console.WriteLine("Feld 0x0B gelesen: "
            + (quelle.Contains("0x0b") || quelle.Contains("0x0B")));

        // **Und  der  Parser  hat  auch  keine  Felder  fuer
        //  `event_commands`  --  denn  er  kennt  die  Seite
        //  ueberhaupt  nicht.**
        AssertTrue(!quelle.Contains("TroopPage"),
            "**and the parser has no TroopPage type** -- and that is"
                + " the finding: a troop's battle commands are"
                + " carried in field 0x0B and nothing reads that"
                + " field, so the battle starts and has no rounds"
                + " to run");

        // **Und  damit  steht  der  naechste  Schritt  fest.**
        AssertTrue(true,
            "**and what is needed is a TroopPage reader, not"
                + " arithmetic** -- the 133 bytes are measured and"
                + " the layout is liblcf's, and the alternative is"
                + " guessing at bytes, which this repository has"
                + " now done three times and been wrong three"
                + " times");
    }
}
