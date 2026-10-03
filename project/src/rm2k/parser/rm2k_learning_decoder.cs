using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Decodes a hero's learned skills, which are field <c>0x3F</c> of an
/// actor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the shape is liblcf's own, read from
/// <c>src/reader_struct_impl.h</c>:</strong>
///
/// <code>
/// template &lt;class S&gt;
/// void Struct&lt;S&gt;::ReadLcf(std::vector&lt;S&gt;&amp; vec, LcfReader&amp; stream) {
/// 	int count = stream.ReadInt();
/// 	vec.resize(count);
/// 	for (int i = 0; i &lt; count; i++) {
/// 		IDReader::ReadID(vec[i], stream);
/// 		TypeReader&lt;S&gt;::ReadLcf(vec[i], stream, 0);
/// 	}
/// }
/// </code>
///
/// <para>
/// <strong>And that is a count and then that many entries</strong>, --
/// <strong>and each entry is an id and then its own chunks</strong>, --
/// <strong>and the chunk list ends at <c>id == 0</c></strong>, -- <strong>
/// because <c>Struct&lt;S&gt;::ReadLcf(S&amp;, LcfReader&amp;)</c> breaks on
/// a zero id.</strong>
/// </para>
/// <para>
/// <strong>And <c>Learning</c> has an <c>int ID</c> of its own</strong>,
/// -- <strong>and <c>IDReaderT&lt;S,true&gt;::ReadID</c> reads it with
/// <c>stream.ReadInt()</c></strong>, -- <strong>so the leading number
/// belongs to the entry and not to a field.</strong>
/// </para>
/// <para>
/// <strong>And the measured bytes of Rast's 18 entries</strong>:
///
/// <code>
/// 18            count
/// 1 | 2,1,2 | 0     id 1, chunk 0x02 len 1 value 2
/// 2 | 2,1,3 | 0     id 2, chunk 0x02 len 1 value 3
/// 3 | 2,1,4 | 0     id 3, chunk 0x02 len 1 value 4
/// 4 | 2,1,5 | 0     id 4, chunk 0x02 len 1 value 5
/// 5 | 2,1,7 | 0     id 5, chunk 0x02 len 1 value 7
/// </code>
///
/// <para>
/// <strong>And this game writes no field <c>0x01</c> at all</strong>,
/// -- <strong>and the entry id runs <c>1, 2, 3, 4, 5</c> in learning
/// order</strong>, -- <strong>so the level lives in the id slot and
/// the reader must not insist on a field that the file does not
/// carry.</strong>
/// </para>
/// <para>
/// <strong>And that is why my first reader returned nothing</strong>: --
/// <strong>it looked for a count it had already consumed as a chunk
/// id.</strong>
/// </para>
/// <para>
/// <strong>And the measurement agrees.</strong> -- <strong>Rast's
/// 91 bytes read as <c>18</c> entries</strong>, -- <strong>and Spencer's
/// 6 bytes read as <c>1</c>.</strong> -- <strong>And the skill numbers
/// run <c>2, 3, 4, 5, 7, 8, 19, 20</c> and skip 6</strong>, -- <strong>
/// which is a learning list and not noise.</strong>
/// </para>
/// </remarks>
public static class Rm2kLearningDecoder
{
    /// <summary>And the level field.</summary>
    public const int FieldLevel = 0x01;

    /// <summary>And the skill field.</summary>
    public const int FieldSkillId = 0x02;

    /// <summary>
    /// And it reads what a hero learned, or it says why not.
    /// </summary>
    /// <param name="pData">The bytes of field <c>0x3F</c>.</param>
    /// <param name="pGelernt">
    /// The entries, each carrying <c>level</c> and <c>skill_id</c>.
    /// </param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the field was read.</returns>
    public static bool TryDecode(
        byte[] pData,
        out Godot.Collections.Array<Godot.Collections.Dictionary> pGelernt,
        out string pFehler)
    {
        pGelernt = new Godot.Collections
            .Array<Godot.Collections.Dictionary>();
        pFehler = "";

        if (pData == null)
        {
            pFehler = "the learning field is absent";
            return false;
        }

        if (pData.Length == 0)
        {
            // **Und  null  gelernt  ist  eine  Antwort  und  kein
            //  Fehler** -- **denn  Salazar  und  Moser  haben
            //  tatsaechlich  genau  ein  Byte  und  sonst  nichts.**
            return true;
        }

        var reader = new LcfBinaryReader(pData);
        var anzahl = reader.ReadBer();
        if (reader.HasError())
        {
            pFehler = "the learning field names no count";
            return false;
        }

        if (anzahl < 0 || anzahl > 4096)
        {
            pFehler = "the learning field claims " + anzahl
                + " entries, and that is outside what a hero can"
                + " learn, so the field is not a learning list";
            return false;
        }

        for (var eintrag = 0; eintrag < anzahl; eintrag++)
        {
            if (reader.IsEof())
            {
                pFehler = "the learning list ended after " + eintrag
                    + " of " + anzahl + " entries";
                return false;
            }

            // **Und  das  ist  die  ID  des  Eintrags** -- **und  sie
            //  gehoert  zum  Eintrag  und  nicht  zu  den  Chunks.**
            var eintragId = reader.ReadBer();

            var neu = new Godot.Collections.Dictionary
            {
                { "entry_id", eintragId },
                // **Und  liblcfs  Vorgabe  ist  `int32_t level = 1`.**
                { "level", 1 },
                { "skill_id", 0 },
            };

            // **Und  nur  die  Faehigkeit  ist  Pflicht.**
            //
            // **Und  mein  erster  Leser  verlangte  beide  Felder**,
            // -- **und  dieses  Spiel  schreibt  kein  Feld  0x01**,
            // -- **und  damit  hat  er  jede  einzige  Lernliste  des
            //  Spiels  verworfen.**
            var pflicht = 0;
            while (!reader.IsEof())
            {
                // **Und  `ReadChunk`  liest  genau  `ReadBer`  fuer  die
                //  ID  und  dann  `ReadBer`  fuer  die  Laenge** --
                // **das  ist  die  Form,  die  liblcf  benutzt.**
                var feld = reader.ReadChunk();
                if (reader.HasError())
                {
                    pFehler = "learning entry " + eintrag
                        + " has a field the file does not finish";
                    return false;
                }

                if ((bool)feld["terminator"])
                {
                    break;
                }

                var feldId = feld["id"].AsInt32();
                var inhalt = (byte[])feld["data"];

                switch (feldId)
                {
                    case FieldLevel:
                        // **Und  wenn  das  Feld  da  ist,  dann  ist
                        //  es  die  Stufe** -- **und  wenn  es  fehlt,
                        //  dann  steht  die  Stufe  in  der  ID.**
                        neu["level"] = Zahl(inhalt);
                        break;

                    case FieldSkillId:
                        neu["skill_id"] = Zahl(inhalt);
                        pflicht++;
                        break;

                    default:
                        // **Und  ein  unbekanntes  Feld  bleibt  da.**
                        if (!neu.ContainsKey("unknown_fields"))
                        {
                            neu["unknown_fields"] =
                                new Godot.Collections
                                    .Array<Godot.Collections
                                        .Dictionary>();
                        }

                        ((Godot.Collections.Array<Godot.Collections
                            .Dictionary>)neu["unknown_fields"])
                            .Add(new Godot.Collections.Dictionary
                            {
                                { "id", feldId },
                                { "data", inhalt },
                            });
                        break;
                }
            }

            // **Und  der  Eintrag  traegt  nur  eine  Faehigkeit.**
            //
            // **Und  ich  hatte  geraten,  die  ID  sei  die
            //  Lernstufe**, -- **und  die  Messung  sagt:  die  IDs
            //  laufen  1, 2, 3, 4, 5  und  sind  ein  laufender
            //  Zaehler**, -- **und  liblcf  laesst  `level` auf  seinem
            //  Standard  1,  wenn  das  Feld  fehlt.**
            //
            // **Und  eine  erfundene  Lernstufe  waere  eine  erfundene
            //  Regel**, -- **und  sie  wuerde  jedem  Helden  sagen,
            //  dass  er  alles  auf  Stufe  eins  gelernt  hat.**
            //
            // **Und  `entry_id`  bleibt  sichtbar**, -- **denn  es  ist
            //  die  eigene  Identitaet  des  Eintrags  und  nicht  ein
            //  Spielwert.**

            if (neu["skill_id"].AsInt32() <= 0)
            {
                pFehler = "learning entry " + eintrag
                    + " has skill id "
                    + neu["skill_id"].AsInt32() + ", and a hero cannot"
                    + " learn skill zero";
                return false;
            }

            pGelernt.Add(neu);
        }

        return true;
    }

    private static int Zahl(byte[] pDaten)
    {
        if (pDaten == null || pDaten.Length == 0)
        {
            return 0;
        }

        var reader = new LcfBinaryReader(pDaten);
        var wert = reader.ReadBer();
        return reader.HasError() ? 0 : wert;
    }
}
