using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Decodes a troop's member list, which is field <c>0x02</c> of a
/// troop and is an array of <c>TroopMember</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the field ids are liblcf's, read out of
/// <c>generator/csv/fields.csv</c> and not out of a guess.</strong>
/// </para>
/// <para>
/// <code>
/// Troop          members        0x02  Array&lt;TroopMember&gt;
/// TroopMember    enemy_id       0x01  Ref&lt;Enemy&gt;
/// TroopMember    x              0x02  Int32
/// TroopMember    y              0x03  Int32
/// TroopMember    invisible      0x04  Boolean
/// </code>
/// </para>
/// <para>
/// <strong>And every member begins with <c>enemy_id</c> and not with
/// a count.</strong> -- <strong>I believed for one commit that the
/// first byte of this field was the monster count</strong>, --
/// <strong>and the measurement that disproved it is in
/// <c>test_rm2k_truppenfelder_empirisch.cs</c>.</strong>
/// </para>
/// <para>
/// <strong>And a troop without members is refused</strong>, --
/// <strong>because a fight started against nobody is not a fight
/// and is not something a reader should invent around.</strong>
/// </para>
/// </remarks>
public static class Rm2kTroopMemberDecoder
{
    /// <summary>
    /// And the limit, and it is the one
    /// <c>rm2k_event_command_decoder.cs</c> already uses.
    /// </summary>
    public const int MaxMembers = 64;

    /// <summary>
    /// And the limit on fields per member, and it is five because
    /// liblcf names exactly five.
    /// </summary>
    public const int MaxFieldsPerMember = 16;

    private const int FieldEnemyId = 0x01;
    private const int FieldX = 0x02;
    private const int FieldY = 0x03;
    private const int FieldInvisible = 0x04;

    /// <summary>
    /// And it reads the list or it says why not.
    /// </summary>
    /// <param name="pData">The bytes of field <c>0x02</c>.</param>
    /// <param name="pMembers">
    /// The members, and each one carries <c>enemy_id</c>, <c>x</c>,
    /// <c>y</c> and <c>invisible</c>.
    /// </param>
    /// <param name="pFehler">Why it did not read, and empty on success.</param>
    /// <returns>Whether the list was read.</returns>
        public static bool TryDecode(
        byte[] pData,
        out Godot.Collections.Array<Godot.Collections.Dictionary> pMembers,
        out string pFehler)
    {
        pMembers = new Godot.Collections
            .Array<Godot.Collections.Dictionary>();
        pFehler = "";

        if (pData == null)
        {
            pFehler = "no data";
            return false;
        }

        if (pData.Length == 0)
        {
            pFehler = "the troop has no members, and a fight must not"
                + " start against nobody";
            return false;
        }

        var reader = new LcfBinaryReader(pData);

        // **Und  das  ist  der  Punkt,  an  dem  mein  erster
        //  Decoder  falsch  war.**
        //
        // **Und  ein  `Array<TroopMember>`  ist  KEIN  flacher
        //  Chunk-Array.**  --
        // **Und  es  beginnt  mit  einer  BER-Anzahl  und  dann
        //  kommt  je  Objekt  eine  ID  und  eine  Feldliste  mit
        //  einem  Nullchunk  am  Ende** --
        // **genauso  wie  `ParseStructArray`  es  fuer  die
        //  Kapsel  eines  Spieles  tut.**
        var anzahl = reader.ReadBer();
        if (reader.HasError())
        {
            pFehler = "the member list does not begin with a count";
            return false;
        }

        if (anzahl <= 0)
        {
            pFehler = "the troop has " + anzahl + " members, and a"
                + " fight must not start against nobody";
            return false;
        }

        if (anzahl > MaxMembers)
        {
            pFehler = anzahl + " members exceeds the limit of "
                + MaxMembers;
            return false;
        }

        for (var index = 0; index < anzahl; index++)
        {
            var objectId = reader.ReadBer();
            if (reader.HasError())
            {
                pFehler = "member " + index + " has no id";
                return false;
            }

            var felder = new Godot.Collections
                .Array<Godot.Collections.Dictionary>();
            var feldCount = 0;
            var beendet = false;
            while (!reader.IsEof())
            {
                if (feldCount >= MaxFieldsPerMember)
                {
                    pFehler = "member " + index + " has more than "
                        + MaxFieldsPerMember + " fields";
                    return false;
                }

                var feld = reader.ReadChunk();
                if (reader.HasError())
                {
                    pFehler = "member " + index + " did not read at"
                        + " field " + feldCount;
                    return false;
                }

                if ((bool)feld["terminator"])
                {
                    beendet = true;
                    break;
                }

                felder.Add(feld);
                feldCount++;
            }

            if (!beendet)
            {
                pFehler = "member " + index + " is missing its"
                    + " terminator";
                return false;
            }

            var mitglied = MemberOf(index, objectId, felder, ref pFehler);
            if (mitglied == null)
            {
                return false;
            }

            pMembers.Add(mitglied);
        }

        if (!reader.IsEof())
        {
            pFehler = "trailing bytes after " + pMembers.Count
                + " members";
            return false;
        }

        return true;
    }

    /// <summary>
    /// And one member out of its field list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And liblcf's <c>TroopMember</c> begins with
    /// <c>enemy_id</c> at <c>0x01</c></strong>, -- <strong>and the
    /// field list of a member begins with that chunk or the bytes are
    /// not a member.</strong> -- <strong>And refusing rather than
    /// taking field two as the id is the whole reason this refuses
    /// at all.</strong>
    /// </para>
    /// </remarks>
    private static Godot.Collections.Dictionary? MemberOf(
        int pIndex,
        int pObjectId,
        Godot.Collections.Array<Godot.Collections.Dictionary> pFelder,
        ref string pFehler)
    {
        var mitglied = new Godot.Collections.Dictionary
        {
            { "id", pObjectId },
            { "enemy_id", 0 },
            { "x", 0 },
            { "y", 0 },
            { "invisible", false },
        };

        foreach (var feld in pFelder)
        {
            var id = feld["id"].AsInt32();
            switch (id)
            {
                case FieldEnemyId:
                case FieldX:
                case FieldY:
                case FieldInvisible:
                    mitglied[id == FieldEnemyId ? "enemy_id"
                        : id == FieldX ? "x" : id == FieldY ? "y"
                            : "invisible"] = DecodeBer(
                        (byte[])feld["data"], ref pFehler);
                    break;

                default:
                    // **Und  ein  unbekanntes  Feld  bleibt
                    //  erhalten  und  wird  nicht  verworfen.**
                    mitglied["unknown_fields"] =
                        (mitglied.ContainsKey("unknown_fields")
                            ? (Godot.Collections.Array<Godot.Collections
                                .Dictionary>)mitglied["unknown_fields"]
                            : new Godot.Collections
                                .Array<Godot.Collections.Dictionary>());
                    ((Godot.Collections.Array<Godot.Collections
                        .Dictionary>)mitglied["unknown_fields"]).Add(feld);
                    break;
            }
        }

        if (mitglied["enemy_id"].AsInt32() <= 0)
        {
            pFehler = "member " + pIndex + " names no enemy, and a"
                + " troop must not carry an empty member";
            return null;
        }

        return mitglied;
    }

    private static int DecodeBer(byte[] pData, ref string pFehler)
    {
        if (pData == null || pData.Length == 0)
        {
            pFehler = "empty payload";
            return 0;
        }

        var sub = new LcfBinaryReader(pData);
        var wert = sub.ReadBer();
        if (sub.HasError())
        {
            pFehler = "payload is not a BER value";
            return 0;
        }

        return wert;
    }
}
