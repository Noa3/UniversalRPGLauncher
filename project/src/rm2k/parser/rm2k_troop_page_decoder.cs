using System;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Decodes a troop page, which is field <c>0x0B</c> of a troop and
/// holds the battle's own commands.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the field ids are liblcf's, from
/// <c>generator/csv/fields.csv</c>:</strong>
///
/// <code>
/// Troop       pages             0x0B  Array&lt;TroopPage&gt;
/// TroopPage   condition         0x02  TroopPageCondition
/// TroopPage   event_commands    0x0B  Vector&lt;EventCommand&gt;
/// TroopPage   event_commands    0x0C  Vector&lt;EventCommand&gt;
/// </code>
///
/// <para>
/// <strong>And a page is a field list like any other LCF struct</strong>,
/// -- <strong>and its <c>0x0C</c> carries the commands themselves</strong>,
/// -- <strong>which is what this decoder hands to the page's own event
/// command reader.</strong>
/// </para>
/// <para>
/// <strong>And the page's condition at <c>0x02</c> stays raw</strong>,
/// -- <strong>because a condition this repository does not read must
/// not be turned into "always true"</strong>, -- <strong>and a battle
/// that ran a page the game had switched off is a battle the game
/// never wrote.</strong>
/// </para>
/// </remarks>
public static class Rm2kTroopPageDecoder
{
    /// <summary>And the field carrying the commands.</summary>
    public const int FieldEventCommands = 0x0C;

    /// <summary>And the field carrying the page condition.</summary>
    public const int FieldCondition = 0x02;

    /// <summary>
    /// And it reads the page's commands or it says why not.
    /// </summary>
    /// <param name="pData">The bytes of field <c>0x0B</c>.</param>
    /// <param name="pBefehle">The commands.</param>
    /// <param name="pFehler">Why it did not read, and empty on success.</param>
    /// <returns>Whether the page was read.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a page without commands is refused</strong>, --
    /// <strong>because a battle turn with nothing in it is a turn the
    /// game never wrote</strong>, -- <strong>and a reader that ran an
    /// empty page would look like a turn that did nothing.</strong>
    /// </para>
    /// </remarks>
    public static bool TryDecode(
        byte[] pData,
        out Godot.Collections.Array<Godot.Collections.Dictionary> pBefehle,
        out string pFehler)
    {
        pBefehle = new Godot.Collections
            .Array<Godot.Collections.Dictionary>();
        pFehler = "";

        if (pData == null || pData.Length == 0)
        {
            pFehler = "the troop page is empty";
            return false;
        }

        var reader = new LcfBinaryReader(pData);
        byte[]? befehleBytes = null;

        for (var feld = 0; feld < 64; feld++)
        {
            if (reader.IsEof())
            {
                break;
            }

            var chunk = reader.ReadChunk();
            if (reader.HasError())
            {
                pFehler = "the page did not read at field " + feld;
                return false;
            }

            if ((bool)chunk["terminator"])
            {
                break;
            }

            if (chunk["id"].AsInt32() == FieldEventCommands)
            {
                befehleBytes = (byte[])chunk["data"];
            }
        }

        if (befehleBytes == null)
        {
            pFehler = "the page carries no event commands, and a turn"
                + " with nothing in it is a turn the game never"
                + " wrote";
            return false;
        }

        var dekodiert = Rm2kEventCommandDecoder.Decode(befehleBytes);
        if (!dekodiert.Success)
        {
            pFehler = dekodiert.Error?.Describe()
                ?? "the page's commands did not read";
            return false;
        }

        pBefehle = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            dekodiert.Data["commands"];
        if (pBefehle.Count == 0)
        {
            pFehler = "the page's commands decoded to nothing";
            return false;
        }

        return true;
    }
}
