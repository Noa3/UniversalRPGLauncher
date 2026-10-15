using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Mz;
using UniversalRPG.Web;

namespace UniversalRPG.Mz;

/// <summary>
/// Decodes a troop page, which is the battle's own commands.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the field ids are from the MZ format:</strong>
/// </para>
/// <para>
/// <para>
/// <strong>And a page is a list of event commands</strong>,
/// -- <strong>and its <c>list</c> carries the commands themselves</strong>.
/// </para>
/// </remarks>
public static class MzTroopPageDecoder
{
    /// <summary>
    /// And it reads the page's commands or it says why not.
    /// </summary>
    /// <param name="pData">The bytes of the page (not used in MZ format).</param>
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
        out List<MzCommandEntry> pBefehle,
        out string pFehler)
    {
        pBefehle = new List<MzCommandEntry>();
        pFehler = "";

        if (pData == null || pData.Length == 0)
        {
            pFehler = "the troop page is empty";
            return false;
        }

        // MZ format doesn't use bytes for troop pages - they're in JSON
        // This method is kept for interface compatibility with RM2K
        // The actual decoding happens in the battle runner via TruppenBytes
        pFehler = "MZ troop pages are decoded from JSON, not bytes";
        return false;
    }

    /// <summary>
    /// Decodes a troop's page from JSON data.
    /// </summary>
    /// <param name="pBank">The parsed database.</param>
    /// <param name="pTroopId">The troop ID.</param>
    /// <param name="pBefehle">The decoded commands.</param>
    /// <param name="pFehler">Why it did not read, and empty on success.</param>
    /// <returns>Whether the page was read.</returns>
    public static bool TryDecode(
        MzDataFile pBank,
        int pTroopId,
        out List<MzCommandEntry> pBefehle,
        out string pFehler)
    {
        pBefehle = new List<MzCommandEntry>();
        pFehler = "";

        // **And a missing database is a refusal and not a crash**, because
        // the caller is another engine path and not a reason to end the
        // frame.
        if (pBank == null)
        {
            pFehler = "the troop database was not handed in";
            return false;
        }

        var troopsMember = pBank.Root.Member("troops");
        if (troopsMember?.Kind != MzKind.Array)
        {
            pFehler = "troops array not found";
            return false;
        }

        var truppen = troopsMember.Items;
        if (truppen == null)
        {
            pFehler = "troops array is null";
            return false;
        }

        foreach (var t in truppen)
        {
            var idMember = t.Member("id");
            if (idMember?.Kind != MzKind.Number || (int)Math.Round(idMember.Number) != pTroopId)
            {
                continue;
            }

            var pagesMember = t.Member("pages");
            if (pagesMember?.Kind != MzKind.Array)
            {
                pFehler = "troop " + pTroopId + " has no pages array";
                return false;
            }

            var pages = pagesMember.Items;
            if (pages == null || pages.Count == 0)
            {
                pFehler = "troop " + pTroopId + " has no pages";
                return false;
            }

            // Take the first page
            var page = pages[0];
            var listMember = page.Member("list");
            if (listMember?.Kind != MzKind.Array)
            {
                pFehler = "page has no list";
                return false;
            }

            var list = listMember.Items;
            if (list == null)
            {
                pFehler = "page list is null";
                return false;
            }

            foreach (var cmd in list)
            {
                var entry = MzCommandEntry.From(cmd);
                pBefehle.Add(entry);
            }

            if (pBefehle.Count == 0)
            {
                pFehler = "the page's commands decoded to nothing";
                return false;
            }

            return true;
        }

        pFehler = "troop " + pTroopId + " not found";
        return false;
    }
}