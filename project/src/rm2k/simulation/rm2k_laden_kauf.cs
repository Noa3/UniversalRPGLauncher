using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And buying one of a shop's goods.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the reference's number window caps at
/// <c>GetGold() / price</c></strong>:
///
/// <code>
/// max = std::min&lt;int&gt;(max, Main_Data::game_party-&gt;GetGold() / item-&gt;price);
/// </code>
///
/// <strong>And a price of zero divides by zero here</strong>, -- <strong>and
/// the bank has one hundred and eleven goods without a
/// price</strong>, -- <strong>so this refuses rather than
/// dividing</strong>.
/// </para>
/// <para>
/// <strong>And the shop must have asked to sell</strong>, -- <strong>because
/// <c>10720</c>'s first parameter is a mode and a mode of two does not
/// buy</strong>.
/// </para>
/// <para>
/// <strong>And a price the gold does not cover is refused</strong>, --
/// <strong>because a shop that gives an item for nothing is a rule this
/// repository would have made up</strong>.
/// </para>
/// </remarks>
public static class Rm2kLadenKauf
{
    /// <summary>
    /// And how many of a good the gold could buy.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pBank">The item bank.</param>
    /// <param name="pItemId">The good.</param>
    /// <param name="pAnzahl">How many the caller wants.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the goods were handed over.</returns>
    public static bool Kaufe(
        GameSimulationState pZustand,
        Godot.Collections.Dictionary? pBank,
        int pItemId,
        int pAnzahl,
        out string pFehler)
    {
        pFehler = "";
        if (pZustand == null || !pZustand.IsShopOpen)
        {
            pFehler = "no shop is open, and buying from a closed"
                + " shop is a command the game never issued";
            return false;
        }

        if (!pZustand.CanBuy)
        {
            pFehler = "this shop does not buy, and 10720's first"
                + " parameter is the mode that says so";
            return false;
        }

        if (pAnzahl <= 0)
        {
            pFehler = "the number of goods is " + pAnzahl
                + ", and that is not a purchase";
            return false;
        }

        var preis = Preis(pBank, pItemId);
        if (preis < 0)
        {
            pFehler = "item " + pItemId + " is not on the shelf, and"
                + " a price for it would be one this runtime"
                + " invented";
            return false;
        }

        if (preis == 0)
        {
            pFehler = "item " + pItemId + " costs nothing in the bank,"
                + " and the reference divides the gold by the"
                + " price, so this runtime refuses rather than"
                + " divide by zero";
            return false;
        }

        var gesamt = preis * pAnzahl;
        if (gesamt > pZustand.Gold)
        {
            pFehler = "the goods cost " + gesamt + " and the party"
                + " has " + pZustand.Gold + ", and the reference"
                + " caps the number at gold / price";
            return false;
        }

        if (pZustand.ItemCounts.TryGetValue(pItemId, out var alt))
        {
            pZustand.ItemCounts[pItemId] = alt + pAnzahl;
        }
        else
        {
            pZustand.ItemCounts[pItemId] = pAnzahl;
        }

        pZustand.Gold -= gesamt;
        pZustand.AddDiagnostic(
            "RM2K the party buys " + pAnzahl + " of item " + pItemId
            + " for " + gesamt + " and has " + pZustand.Gold + " left");
        return true;
    }

    /// <summary>
    /// And the bank's own price for a good.
    /// </summary>
    /// <param name="pBank">The item bank.</param>
    /// <param name="pItemId">The good.</param>
    /// <returns>
    /// The price, zero for a good the bank gives away, and minus one
    /// when the bank does not define the item at all.
    /// </returns>
    public static int Preis(
        Godot.Collections.Dictionary? pBank, int pItemId)
    {
        if (pBank == null || !pBank.TryGetValue("items", out var roh)
            || roh.VariantType != Godot.Variant.Type.Array)
        {
            return -1;
        }

        foreach (var eintrag in roh.AsGodotArray())
        {
            if (eintrag.VariantType
                != Godot.Variant.Type.Dictionary)
            {
                continue;
            }

            var item = eintrag.AsGodotDictionary();
            if (item.TryGetValue("id", out var rohId)
                && rohId.AsInt32() == pItemId)
            {
                return item.TryGetValue("price", out var rohPreis)
                    ? rohPreis.AsInt32() : 0;
            }
        }

        return -1;
    }
}
