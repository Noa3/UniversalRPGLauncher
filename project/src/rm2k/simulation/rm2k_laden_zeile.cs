using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And what a shop's shelves read.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>10720</c> fills <c>ShopItemIds</c> fifteen times in the
/// measured game</strong>, -- <strong>and no surface displayed it</strong>,
/// -- <strong>and a command the player cannot see is a dead
/// command</strong>.
/// </para>
/// <para>
/// <strong>And the formatting lives here and not in the window</strong>,
/// -- <strong>because a window cannot be asserted on in a headless run
/// and a price that only exists in a window cannot be checked at
/// all</strong>.
/// </para>
/// <para>
/// <strong>And every number is the game's</strong>:  -- <strong>the name
/// and the price from the item bank</strong>, -- <strong>and an item the
/// bank does not define is left out rather than shown without a
/// price</strong>.
/// </para>
/// </remarks>
public static class Rm2kLadenZeile
{
    /// <summary>
    /// And the shelves, one line per good.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pBank">The item bank.</param>
    /// <returns>
    /// One line per item the bank defines, and it may be empty when
    /// the bank does not define one of them.
    /// </returns>
    public static List<string> Regale(
        GameSimulationState pZustand,
        Godot.Collections.Dictionary? pBank)
    {
        var zeilen = new List<string>();
        if (pZustand == null)
        {
            return zeilen;
        }

        if (pBank == null
            || !pBank.TryGetValue("items", out var rohItems)
            || rohItems.VariantType != Godot.Variant.Type.Array)
        {
            return zeilen;
        }

        var items = rohItems.AsGodotArray();

        // **Und  der  Index  und  nicht  die  Aufzaehlung**, -- **und
        //  `Godot.Collections.Array`  traegt  keine  stabile
        //  Aufzaehlung  ueber  eine  Aenderung  hinweg.**
        for (var i = 0; i < pZustand.ShopItemIds.Count; i++)
        {
            var itemId = pZustand.ShopItemIds[i];
            foreach (var roh in items)
            {
                if (roh.VariantType
                    != Godot.Variant.Type.Dictionary)
                {
                    continue;
                }

                var item = roh.AsGodotDictionary();
                if (!item.TryGetValue("id", out var rohId)
                    || rohId.AsInt32() != itemId)
                {
                    continue;
                }

                var name = item.TryGetValue("name", out var rohName)
                    ? rohName.AsString() : ("Gegenstand " + itemId);
                var preis = item.TryGetValue("price",
                    out var rohPreis) ? rohPreis.AsInt32() : 0;
                zeilen.Add(string.Format("{0}  {1}", name, preis));
                break;
            }
        }

        return zeilen;
    }

    /// <summary>
    /// And the line that says what the shop will and will not do.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <returns>One line.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And buying and selling are two separate flags</strong>,
    /// -- <strong>because <c>10720</c>'s first parameter is a mode with
    /// three cases</strong>, -- <strong>and a shop that only buys must
    /// say so</strong>.
    /// </para>
    /// </remarks>
    public static string Kopf(GameSimulationState pZustand)
    {
        if (pZustand == null)
        {
            return "";
        }

        // **Und  die  Reihenfolge  der  Pruefung  ist  nicht
        //  beliebig.**
        //
        // **Und  `10720`  setzt  zwei  getrennte  Flags**, -- **und
        //  `ExecuteOpenShop`  macht  daraus  drei  Faelle**:
        //
        // <code>
        /// mode 0  buys and sells
        /// mode 1  buys only
        /// mode 2  sells only
        /// </code>
        //
        // **Und  ich  habe  zuerst  `CanBuy`  geprueft** -- **und  dann
        //  sagte  Modus  eins  "kauft und verkauft"**, -- **und  das  ist
        //  der  ganze  Unterschied  zwischen  einem  Shop,  der
        //  verkauft,  und  einem,  der  nur einkauft.**
        //
        // **Und  die  drei  Faelle  sind  drei  Zustaende  und  keine
        //  zwei  Flags  mit  einer  Rangfolge.**
        var text = pZustand.CanBuy && pZustand.CanSell
            ? "kauft und verkauft"
            : pZustand.CanBuy ? "kauft nur"
            : pZustand.CanSell ? "verkauft nur"
            : "kauft nicht";
        return string.Format("Laden  {0}  Gold {1}", text,
            pZustand.Gold);
    }
}
