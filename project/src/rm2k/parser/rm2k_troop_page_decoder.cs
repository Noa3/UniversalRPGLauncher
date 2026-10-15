using System;
using System.Collections.Generic;
using Godot;

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
/// Troop       pages             0x0B  Array<TroopPage>
/// TroopPage   condition         0x02  TroopPageCondition
/// TroopPage   event_commands    0x0B  Vector<EventCommand>
/// TroopPage   event_commands    0x0C  Vector<EventCommand>
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
    /// A decoded troop page with commands and condition.
    /// </summary>
    public sealed class DecodedPage
    {
        public Godot.Collections.Array<Godot.Collections.Dictionary> Commands { get; set; } = new();
        public byte[]? ConditionData { get; set; }
    }

    /// <summary>
    /// And it reads all pages from a troop, with their commands and conditions.
    /// </summary>
    /// <param name="pData">The bytes of field <c>0x0B</c> (troop pages array).</param>
    /// <param name="pPages">The decoded pages.</param>
    /// <param name="pFehler">Why it did not read, and empty on success.</param>
    /// <returns>Whether at least one page was read.</returns>
    public static bool TryDecodeAll(
        byte[] pData,
        out List<DecodedPage> pPages,
        out string pFehler)
    {
        pPages = new List<DecodedPage>();
        pFehler = "";

        if (pData == null || pData.Length == 0)
        {
            pFehler = "the troop pages array is empty";
            return false;
        }

        var reader = new LcfBinaryReader(pData);

        // The pages array is a sequence of page structs.
        // Each page struct has fields: 0x02 (condition), 0x0B/0x0C (commands).
        // We read until EOF or terminator.
        for (var pageIndex = 0; pageIndex < 64; pageIndex++)
        {
            if (reader.IsEof())
            {
                break;
            }

            // Peek at the next field ID to see if it's a page field (0x02 or 0x0B/0x0C)
            // or a terminator (0x00)
            var currentPos = reader.GetPosition();
            if (currentPos >= pData.Length)
            {
                break;
            }

            // Try to read a chunk - if it fails or is terminator, we're done
            var chunk = reader.ReadChunk();
            if (reader.HasError())
            {
                pFehler = "the troop pages array did not read at page " + pageIndex;
                return false;
            }

            if ((bool)chunk["terminator"])
            {
                break;
            }

            var fieldId = chunk["id"].AsInt32();
            
            // If this is not a page field, we might be at the end or in an unexpected position
            if (fieldId != FieldCondition && fieldId != FieldEventCommands)
            {
                // Not a page field - skip and continue
                continue;
            }

            // This is the start of a page - read all its fields
            byte[]? befehleBytes = null;
            byte[]? conditionData = null;

            // If we started with FieldEventCommands, we already have it
            if (fieldId == FieldEventCommands)
            {
                befehleBytes = (byte[])chunk["data"];
            }
            else if (fieldId == FieldCondition)
            {
                conditionData = (byte[])chunk["data"];
            }

            // Read remaining fields of this page
            for (var feld = 0; feld < 64; feld++)
            {
                if (reader.IsEof())
                {
                    break;
                }

                var pageChunk = reader.ReadChunk();
                if (reader.HasError())
                {
                    pFehler = "page " + pageIndex + " did not read at field " + feld;
                    return false;
                }

                if ((bool)pageChunk["terminator"])
                {
                    break;
                }

                var pageFieldId = pageChunk["id"].AsInt32();
                if (pageFieldId == FieldEventCommands)
                {
                    befehleBytes = (byte[])pageChunk["data"];
                }
                else if (pageFieldId == FieldCondition)
                {
                    conditionData = (byte[])pageChunk["data"];
                }
                // Ignore other fields
            }

            if (befehleBytes == null)
            {
                // Page without commands - skip
                continue;
            }

            var dekodiert = Rm2kEventCommandDecoder.Decode(befehleBytes);
            if (!dekodiert.Success)
            {
                pFehler = "page " + pageIndex + " commands did not read: " + (dekodiert.Error?.Describe() ?? "unknown");
                return false;
            }

            var commands = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                dekodiert.Data["commands"];
            if (commands.Count == 0)
            {
                // Page with no commands - skip
                continue;
            }

            var page = new DecodedPage
            {
                Commands = commands,
                ConditionData = conditionData
            };
            pPages.Add(page);
        }

        if (pPages.Count == 0)
        {
            pFehler = "no valid pages found in troop";
            return false;
        }

        return true;
    }

    /// <summary>
    /// And it reads the first page's commands or it says why not.
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