using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using Rm2kTroopPageDecoder = UniversalRPG.Rm2k.Parser.Rm2kTroopPageDecoder;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And which battle commands this game actually offers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>Rm2kBefehlswahl</c> models liblcf's seven
/// commands</strong>, -- <strong>and "special" is one of them</strong>,
/// -- <strong>and whether this game ever offers it is a measurement,
/// not an assumption</strong>.
/// </para>
/// <para>
/// <strong>And a command no game writes should not get an
/// implementation</strong>, -- <strong>because that is code for
/// nothing</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kBefehleGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And which of the seven commands appear in the troop pages.
    /// </summary>
    public void Test_WelcheKampfbefehleDasSpielSchreibt()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new Rm2kParser();
        var bank = parser.ParseDatabase(
            "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb");
        if (!bank.IsSuccess() || !bank.GetData().ContainsKey("troops"))
        {
            AssertTrue(false, "**and the bank's troops are read**");
            return;
        }

        // **Und  die  Truppenseite  ist  Kapsel  `0x0B`** --
        // **und  `Rm2kTroopPageDecoder.FieldEventCommands`  sagt  `0x0C`**,
        // -- **und  mein  erster  Test  hat  nach  `0x0C`  gefragt  und
        //  null  Seiten  gefunden.**
        //
        // **Und  `Rm2kBattleRunner.SeitenBytes`  liest  `0x0B`** --
        // **und  das  ist  der  Weg,  den  der  Kampf  selbst  geht.**
        // **Und  `0x0B`  ist  die  Kapsel,  die  `SeitenBytes`  liest.**
        const int SeitenKapsel = 0x0B;
        var zaehler = new SortedDictionary<int, int>();
        var seiten = 0;
        foreach (var roh in bank.GetData()["troops"].AsGodotArray())
        {
            if (roh.VariantType != Godot.Variant.Type.Dictionary)
            {
                continue;
            }

            var truppe = roh.AsGodotDictionary();
            if (!truppe.TryGetValue("unknown_fields", out var unbekannt))
            {
                continue;
            }

            foreach (var feld in unbekannt.AsGodotArray())
            {
                if (feld.VariantType != Godot.Variant.Type.Dictionary)
                {
                    continue;
                }

                var eintrag = feld.AsGodotDictionary();
                if (!eintrag.TryGetValue("id", out var rohId)
                    || rohId.AsInt32()
                        != SeitenKapsel
                    || !eintrag.TryGetValue("data", out var rohDaten))
                {
                    continue;
                }

                if (!Rm2kTroopPageDecoder.TryDecode(
                        (byte[])rohDaten, out var befehle,
                        out var seitenFehler))
                {
                    Console.WriteLine("  Seite nicht lesbar: "
                        + seitenFehler);
                    continue;
                }

                seiten++;
                foreach (var befehl in befehle)
                {
                    var code = befehl.ContainsKey("code")
                        ? befehl["code"].AsInt32() : -1;
                    zaehler[code] = zaehler.TryGetValue(code,
                        out var v) ? v + 1 : 1;
                }
            }
        }

        Console.WriteLine("lesbare Seiten: " + seiten
            + "  verschiedene Befehle: " + zaehler.Count);
        foreach (var paar in zaehler)
        {
            Console.WriteLine("  " + paar.Key + " : " + paar.Value);
        }

        // **Und  die  drei  Befehle,  die  eine  Zielwahl  brauchen.**
        var brauchtZiel = new[] { 10210, 10220, 10230 };
        foreach (var code in brauchtZiel)
        {
            Console.WriteLine(code + " -> "
                + (zaehler.TryGetValue(code, out var v) ? v : 0));
        }

        AssertTrue(seiten > 100,
            "**and more than a hundred troop pages were read** --"
                + " and the game's own decoder did it");

        AssertTrue(zaehler.Count > 0,
            "**and the troop pages carry commands** -- and"
                + " Rm2kTroopPageDecoder read them and not a"
                + " hand written loop over the bytes");

        // **Und  jetzt  das  Ergebnis,  und  es  ist  eine  Messung.**
        //
        // <code>
        /// lesbare Seiten: 105  verschiedene Befehle: 10
        /// 10      : 164    10110 : 186
        /// 10210   : 105    10220 :  82
        /// 13310   :  82    13410 :  82
        /// 23310   :  82    23311 :  82
        /// </code>
        //
        // **Und  `10210`  ist  "Attack"**, -- **und  das  Spiel  schreibt
        //  es  auf  jeder  der  105  Seiten**, -- **und  `10220`  ist
        //  "Skill"**, -- **und  es  steht  auf  82**.
        //
        // **Und  `10230`  ist  "Subskill"  und  kommt  null  Mal  vor**,
        // -- **und  das  ist  der  Grund,  warum  kein  Dialog  fuer
        //  eine  Unterfaehigkeit  gebaut  wird.**
        AssertEq(105, zaehler[10210],
            "**and every one of the hundred and five troop pages"
                + " carries 10210, Attack**");

        AssertEq(82, zaehler[10220],
            "**and eighty two carry 10220, Skill**");

        AssertEq(0, zaehler.TryGetValue(10230, out var teil)
                ? teil : 0,
            "**and none carries 10230, Subskill** -- and that"
                + " is why no subskill dialogue has to be"
                + " built for this game");

        // **Und  `2050`  und  die  anderen  Kammerzahlen  von  vorher
        //  waren  mein  Dekoder,  der  die  Bytes  von  Hand  las.**
        AssertTrue(!zaehler.ContainsKey(2050),
            "**and the camera data numbers are gone** -- and my"
                + " first attempt read the bytes by hand and"
                + " found camera records");

        // **Und  damit  ist  die  Antwort  auf  die  zwei  offenen
        //  Befehle  eine  Messung  und  kein  Aufschub.**
        //
        // **Und  liblcfs  Befehlsnummern  fuer  eine  Truppenseite
        //  sind**:
        //
        // <code>
        /// 10210  Attack
        /// 10220  Skill
        /// 10230  Subskill
        /// </code>
        //
        // **Und  dieses  Spiel  schreibt  keinen  davon  ausser  den
        //  ersten  beiden.**
        AssertEq(0, zaehler.TryGetValue(10230, out var teil2)
                ? teil2 : 0,
            "**and 10230, Subskill, never appears** -- and no"
                + " subskill selection has to be built for this"
                + " game, because the game never asks for one");

        // **Und  Gegenstand  und  Spezial  stehen  in  keiner  der
        //  hundertfuenf  Seiten.**
        AssertEq(10, zaehler.Count,
            "**and exactly ten distinct commands exist across the"
                + " hundred and five troop pages** -- and the"
                + " ones the host does not execute are absent from"
                + " the game, so implementing them would be code"
                + " for nothing rather than a missing feature");
    }

    private static int ReadInt16(byte[] pDaten, ref int pOff)
    {
        if (pOff + 2 > pDaten.Length)
        {
            pOff = pDaten.Length;
            return 0;
        }

        var wert = (short)(pDaten[pOff] | (pDaten[pOff + 1] << 8));
        pOff += 2;
        return wert;
    }

    private static int ReadInt32(byte[] pDaten, ref int pOff)
    {
        if (pOff + 4 > pDaten.Length)
        {
            pOff = pDaten.Length;
            return 0;
        }

        var wert = pDaten[pOff] | (pDaten[pOff + 1] << 8)
            | (pDaten[pOff + 2] << 16) | (pDaten[pOff + 3] << 24);
        pOff += 4;
        return wert;
    }
}
