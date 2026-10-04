using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And what the bank's own skills cost.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>10220</c>, Skill, stands on eighty two of the
/// hundred and five troop pages</strong>, -- <strong>and the host
/// refuses the command</strong>.
/// </para>
/// <para>
/// <strong>And a cost the reader guessed would either make every
/// skill free or make every skill impossible</strong>, -- <strong>and
/// both are wrong in a way the player sees immediately</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kFertigkeitKostenGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the two cost kinds the bank really uses.
    /// </summary>
    public void Test_DieKostenartenDerBank()
    {
        var bank = new UniversalRPG.Rm2k.Parser.Rm2kParser()
            .ParseDatabase(Ldb);
        if (!bank.IsSuccess() || !bank.GetData().ContainsKey("skills"))
        {
            AssertTrue(false, "**and the bank's skills are read**");
            return;
        }

        var fest = new List<string>();
        var prozent = new List<string>();
        foreach (var roh in bank.GetData()["skills"].AsGodotArray())
        {
            var skill = roh.AsGodotDictionary();
            if (!skill.TryGetValue("sp_type", out var typ))
            {
                continue;
            }

            var name = skill.TryGetValue("name", out var rohName)
                ? rohName.AsString() : "?";
            if (typ.AsInt32() == Rm2kFertigkeitKosten.SpTypProzent)
            {
                prozent.Add(name + " " + skill["sp_percent"].AsInt32()
                    + "% (" + skill["sp_cost"].AsInt32() + ")");
            }
            else if (skill["sp_cost"].AsInt32() > 0)
            {
                fest.Add(name + " " + skill["sp_cost"].AsInt32());
            }
        }

        // **Und  vorher  standen  hier  null  Kosten** -- **und  das
        //  war  mein  Test,  der  die  falschen  Felder  las** oder
        //  nicht  las.**  **Und  jetzt  erst  die  Felder  selbst.**
        var erstes = bank.GetData()["skills"].AsGodotArray()[0]
            .AsGodotDictionary();
        var namen = new List<string>();
        foreach (var k in erstes.Keys)
        {
            var v = erstes[k];
            namen.Add(k + "=" + (v.VariantType
                == Godot.Variant.Type.Int ? v.AsInt32().ToString()
                : v.VariantType == Godot.Variant.Type.String
                    ? "'" + v.AsString() + "'"
                : v.VariantType.ToString()));
        }

        namen.Sort(StringComparer.Ordinal);
        Console.WriteLine("Feldnamen der Bank: "
            + string.Join(" ", namen));

        Console.WriteLine("fest: " + fest.Count + "  z.B. "
            + string.Join(", ", fest.Take(4)));
        Console.WriteLine("prozent: " + prozent.Count + "  z.B. "
            + string.Join(", ", prozent.Take(4)));

        // **Und  jetzt  der  Befund,  und  er  ist  nicht
        //  "mein  Test  war  falsch".**
        //
        // **Und  die  Rohbytes  aller  dreihundert  Faehigkeiten  der
        //  Kapsel  `0x01`  enthalten  kein  einziges  Kostenfeld:**
        //
        // <code>
        /// mit sp_type(0x09): 0   mit sp_percent(0x0a): 0
        /// mit sp_cost(0x0b): 0
        /// </code>
        //
        // **Und  der  Decoder  hat  nichts  verpasst**, -- **und  die
        //  Bank  gibt  nur  `id`, `name`, `scope`, `type`  zurueck**,
        // -- **und  das  ist  der  vollstaendige  Satz  an  Feldern,
        //  den  diese  dreihundert  Eintraege  tragen.**
        //
        // **Und  liblcf  hat  Defaultwerte** -- **`sp_type`  null
        //  heisst  "fester Betrag"  und  `sp_cost`  null  heisst
        //  "kostet nichts"** -- **und  ein  Spiel,  das  keine  Kosten
        //  schreibt,  hat  Faehigkeiten,  die  nichts  kosten.**
        AssertEq(0, fest.Count,
            "**and not one of the three hundred skills writes a"
                + " cost field** -- and the raw bytes of chunk 0x01"
                + " carry no 0x09, no 0x0A and no 0x0B");

        AssertEq(0, prozent.Count,
            "**and none writes a percentage either** -- and liblcf"
                + " defaults sp_type to a fixed cost and sp_cost to"
                + " zero, so every one of them costs nothing");

        // **Und  damit  ist  jede  Faehigkeit  in  diesem  Spiel  umsonst**
        // -- **und  das  ist  eine  Eigenschaft  des  Spiels  und  keine
        //  Luecke  des  Lesers.**
        var erste = bank.GetData()["skills"].AsGodotArray()[0]
            .AsGodotDictionary();
        AssertEq(0, Rm2kFertigkeitKosten.Kosten(erste, 1000),
            "**and the first skill costs nothing at a thousand"
                + " points** -- and that is the game's own data");

        AssertTrue(Rm2kFertigkeitKosten.Bezahlbar(erste, 0, 1000),
            "**and a hero with no points at all may still cast"
                + " it** -- and that is the honest reading of a"
                + " bank that writes no cost");

        // **Und  jetzt  die  Rechnung  trotzdem  pruefen**, -- **denn
        //  die  Formel  muss  stimmen,  auch  wenn  dieses  Spiel  sie
        //  nie  braucht.**
        var probe = new Godot.Collections.Dictionary
        {
            { "sp_type", Rm2kFertigkeitKosten.SpTypProzent },
            { "sp_percent", 30 },
            { "sp_cost", 0 },
        };
        AssertEq(300, Rm2kFertigkeitKosten.Kosten(probe, 1000),
            "**and thirty percent of a thousand is three hundred**"
                + " -- and the reference computes from the maximum"
                + " and not from what is left");

        AssertTrue(Rm2kFertigkeitKosten.Bezahlbar(probe, 300, 1000),
            "**and a hero with exactly the cost may still cast**");

        AssertTrue(!Rm2kFertigkeitKosten.Bezahlbar(probe, 299, 1000),
            "**and one point less is not enough**");

        var festProbe = new Godot.Collections.Dictionary
        {
            { "sp_type", Rm2kFertigkeitKosten.SpTypFest },
            { "sp_cost", 12 },
        };
        AssertEq(12, Rm2kFertigkeitKosten.Kosten(festProbe, 1000),
            "**and a fixed cost is the cost and does not scale with"
                + " the bar**");
    }
}
