using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And a bank a fixture can fight with.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because seven tests stopped working when
/// the encounter learned to build its troop.</strong> --
/// <strong>They built a state with no database, so
/// <c>10710</c> had no monsters and refused to start</strong> --
/// <strong>which was right, and the tests were the thing that had to
/// change.</strong>
/// </para>
/// <para>
/// <strong>And the bank is built from liblcf's field ids</strong>, --
/// <strong>because a fixture that invented a different shape than
/// the parser produces would pass here and fail on a real
/// game.</strong>
/// </para>
/// </remarks>
internal static class Rm2kBegegnungsFixture
{
    /// <summary>
    /// And one troop, one enemy, and the enemy's own numbers.
    /// </summary>
    /// <param name="pMaxHp">The enemy's hit points.</param>
    /// <param name="pAngriff">The enemy's attack.</param>
    /// <param name="pVerteidigung">The enemy's defence.</param>
    /// <returns>A bank with one troop and one enemy.</returns>
    public static Godot.Collections.Dictionary Bank(
        int pMaxHp = 10, int pAngriff = 5, int pVerteidigung = 1)
    {
        // **Und  diese  Bytes  sind  aus  dem  Spiel  kopiert  und
        //  nicht  zusammengesetzt.**
        //
        // **Und  Truppe  2  von  Dragon  Destiny  ist:**
        //
        //     1,1,1,1,2,2,2,129,32,3,1,116,0
        //
        // **Und  gelesen  wird  das  so:**
        //
        //     1            ein Mitglied
        //     1            Objekt-Id des Mitglieds
        //     1,1,2        Feld 0x01, Laenge 1, enemy_id = 2
        //     2,2,129,32   Feld 0x02, Laenge 2, x = 0x8120 -> 32
        //     3,1,116      Feld 0x03, Laenge 1, y = 116
        //     0            Nullchunk: Ende des Mitglieds
        //
        // **Und  mein  erster  Fixture  hatte  zwei  Nullchunk  und
        //  deshalb  kein  Ende** -- **und  das  hat  der  Decoder
        //  korrekt  gemeldet  statt  es  zu  uebersehen.**
        var mitglieder = new byte[]
        {
            1,          // ein Mitglied
            1,          // Objekt-Id des Mitglieds
            1, 1, 2,    // enemy_id = 2
            2, 2, 129, 32, // x = 32
            3, 1, 8,    // y = 8
            0,          // Ende des Mitglieds
        };

        var truppe = new Godot.Collections.Dictionary
        {
            { "id", 3 },
            { "name", "Testgegner" },
            {
                "unknown_fields",
                new Godot.Collections
                    .Array<Godot.Collections.Dictionary>
                    {
                        new Godot.Collections.Dictionary
                        {
                            { "id", 0x02 },
                            { "length", mitglieder.Length },
                            { "offset", 0 },
                            { "payload_offset", 0 },
                            { "data", mitglieder },
                            { "terminator", false },
                        },
                    }
            },
        };

        var monster = new Godot.Collections.Dictionary
        {
            { "id", 2 },
            { "name", "Testgegner" },
            { "battler_name", "" },
            { "battler_hue", 0 },
            { "max_hp", pMaxHp },
            { "max_sp", 0 },
            { "attack", pAngriff },
            { "defense", pVerteidigung },
            { "spirit", 0 },
            { "agility", 0 },
            { "unknown_fields", new Godot.Collections
                .Array<Godot.Collections.Dictionary>() },
        };

        // **Und  eine  zweite  Trupe  fuer  die  ID  1**, -- **denn
        //  die  Begegnungstests  benutzen  die  1  als  Vorgabe  und
        //  eine  weitere  die  3**, --
        // **und  eine  Fixture,  die  nur  eine  der  beiden  kennt,
        //  laeuft  an  der  anderen  vorbei  und  sieht  dann  aus
        //  wie  ein  Fehler  im  Kampf.**
        var truppeEins = new Godot.Collections.Dictionary
        {
            { "id", 1 },
            { "name", "Testgegner" },
            {
                "unknown_fields",
                new Godot.Collections
                    .Array<Godot.Collections.Dictionary>
                    {
                        new Godot.Collections.Dictionary
                        {
                            { "id", 0x02 },
                            { "length", mitglieder.Length },
                            { "offset", 0 },
                            { "payload_offset", 0 },
                            { "data", mitglieder },
                            { "terminator", false },
                        },
                    }
            },
        };

        return new Godot.Collections.Dictionary
        {
            { "header", "LcfDataBase" },
            { "troops", new Godot.Collections
                .Array<Godot.Collections.Dictionary>
                {
                    truppeEins, truppe,
                } },
            { "enemies", new Godot.Collections
                .Array<Godot.Collections.Dictionary> { monster } },
        };
    }

    /// <summary>
    /// And it says whether the fixture is one a parser could produce.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion that keeps the fixture
    /// honest</strong>, -- <strong>because a fixture that drifted from
    /// the real shape would make every battle test a
    /// fiction.</strong>
    /// </para>
    /// </remarks>
    public static void PruefeForm()
    {
        var bank = Bank();
        if (!Rm2kBegegnungAufbauen.Versuche(
                bank, 3, out var m, out var warum))
        {
            throw new InvalidOperationException(
                "die Fixture-Bank baut nicht: " + warum);
        }

        if (m.Count != 1 || m[0]["hp"].AsInt32() != 10)
        {
            throw new InvalidOperationException(
                "die Fixture traegt nicht die Zahlen, die sie"
                + " behauptet");
        }
    }
}
