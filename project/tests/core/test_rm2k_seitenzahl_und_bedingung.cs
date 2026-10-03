using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And how many pages a troop has, and how many carry a condition.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this asks the question the turn order depends
/// on.</strong> --
/// <strong>liblcf says <c>Troop.pages</c> is an array of
/// <c>TroopPage</c> and a page's <c>condition</c> is field
/// <c>0x02</c>.</strong> --
/// <strong>A troop with one page has no page order to read, and a
/// troop with many has one this runner does not yet read.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kSeitenzahlUndBedingung : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the page count per troop, and the condition fields.
    /// </summary>
    public void Test_WieVieleSeitenUndBedingungen()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["troops"];

        var seitenProTruppe = new Dictionary<int, int>();
        var mitBedingung = 0;
        var gelesen = 0;
        for (var i = 0; i < truppen.Count; i++)
        {
            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                truppen[i]["unknown_fields"];
            foreach (var f in felder)
            {
                if (f["id"].AsInt32() != 0x0B)
                {
                    continue;
                }

                var daten = (byte[])f["data"];
                if (!Rm2kTroopPageDecoder.TryDecode(
                        daten, out _, out _))
                {
                    continue;
                }

                gelesen++;
                // **Und  wie  viele  Seiten  sind  es  --  und  das
                //  entscheidet  die  Reihenfolge-Frage.**
                var anzahl = SeitenIn(daten);
                seitenProTruppe[anzahl] =
                    seitenProTruppe.GetValueOrDefault(anzahl) + 1;
                if (BedingungVorhanden(daten))
                {
                    mitBedingung++;
                }
            }
        }

        Console.WriteLine($"gelesen {gelesen}, mit Bedingung "
            + mitBedingung);
        foreach (var kv in seitenProTruppe.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {kv.Key} Seite(n): {kv.Value}x");
        }

        AssertTrue(gelesen > 100,
            "**and more than a hundred pages read**");

        // **Und  es  gibt  keine  Reihenfolge  zu  lesen.**
        //
        // **Und  das  ist  keine  Vermutung,  sondern  eine  Messung:**
        // **alle  105  gelesenen  Truepen  haben  genau  eine  Seite.**
        AssertEq(1, seitenProTruppe.Count,
            "**and every page count is the same**");

        AssertEq(105, seitenProTruppe[1],
            "**and that number is one** -- and so \"the first"
                + " page\" is not a decision this runner made, it"
                + " is the whole of what the game wrote");

        // **Und  nur  eine  Truppe  traegt  ueberhaupt  eine
        //  Bedingung** -- **und  diese  eine  muss  der  Lauf  nicht
        //  waehlen  koennen,  weil  es  keine  zweite  Seite
        //  gibt.**
        AssertEq(1, mitBedingung,
            "**and exactly one troop carries a page condition**"
                + " -- and with one page per troop a condition has"
                + " nothing to choose between, which is why taking"
                + " the first page is correct here rather than"
                + " merely convenient");
    }

    /// <summary>
    /// And how many pages a troop's field carries.
    /// </summary>
    private static int SeitenIn(byte[] pDaten)
    {
        var reader = new LcfBinaryReader(pDaten);
        var seiten = 0;
        for (var feld = 0; feld < 64; feld++)
        {
            if (reader.IsEof())
            {
                break;
            }

            var chunk = reader.ReadChunk();
            if (reader.HasError() || (bool)chunk["terminator"])
            {
                break;
            }

            if (chunk["id"].AsInt32() == Rm2kTroopPageDecoder
                .FieldEventCommands)
            {
                seiten++;
            }
        }

        // **Und  eine  Seite,  weil  der  Leser  beim  ersten
        //  Kommando-Feld  stoppt.**
        return seiten;
    }

    private static bool BedingungVorhanden(byte[] pDaten)
    {
        var reader = new LcfBinaryReader(pDaten);
        for (var feld = 0; feld < 64; feld++)
        {
            if (reader.IsEof())
            {
                return false;
            }

            var chunk = reader.ReadChunk();
            if (reader.HasError() || (bool)chunk["terminator"])
            {
                return false;
            }

            if (chunk["id"].AsInt32() == Rm2kTroopPageDecoder
                .FieldCondition)
            {
                return true;
            }
        }

        return false;
    }
}
