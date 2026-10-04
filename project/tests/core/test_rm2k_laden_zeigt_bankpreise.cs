using System;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a shelf shows the bank's own price.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>10720</c> fills the shop and no surface showed
/// it</strong>, -- <strong>and <c>AddShopGood</c> stores a price of
/// zero for every good</strong>, -- <strong>because the price is not a
/// parameter of <c>10720</c> and lives in the item bank</strong>.
/// </para>
/// <para>
/// <strong>And a shelf that shows a zero price would be a
/// lie</strong>, -- <strong>and a player would buy everything</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kLadenZeigtBankpreise : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the shelves carry the bank's prices, not zeros.
    /// </summary>
    public void Test_DieRegaleTragenDieBankpreise()
    {
        var bank = new Rm2kParser().ParseDatabase(Ldb);
        if (!bank.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var state = new GameSimulationState();
        state.OpenShop(pType: 0, pCanBuy: true, pCanSell: true,
            pHasHandlers: false);

        // **Und  zwei  echte  Waren** -- **und  nicht  zwei  Zahlen,
        //  die  der  Test  erfunden  hat.**
        var items = bank.GetData()["items"].AsGodotArray()
            .Select(x => x.AsGodotDictionary())
            .Where(x => x.ContainsKey("price")
                && x["price"].AsInt32() > 0)
            .ToList();
        var erstes = items[0];
        var zweites = items[1];
        var preis1 = erstes["price"].AsInt32();
        var name1 = erstes["name"].AsString();
        state.AddShopGood(erstes["id"].AsInt32());
        state.AddShopGood(zweites["id"].AsInt32());
        Console.WriteLine("Waren: '" + name1 + "' " + preis1
            + "  und '" + zweites["name"].AsString() + "' "
            + zweites["price"].AsInt32());
        Console.WriteLine("Kopf: " + Rm2kLadenZeile.Kopf(state));

        var zeilen = Rm2kLadenZeile.Regale(state, bank.GetData());
        foreach (var z in zeilen)
        {
            Console.WriteLine("  " + z);
        }

        AssertEq(2, zeilen.Count,
            "**and two shelves for two goods**");

        AssertTrue(zeilen[0].Contains(name1) && zeilen[0].Contains(
                preis1.ToString()),
            "**and the first shelf carries the bank's name and the"
                + " bank's price** -- " + name1 + " costs "
                + preis1);

        AssertTrue(!zeilen[0].EndsWith(" 0"),
            "**and no shelf ends in a zero** -- the reader's own"
                + " price is zero because 10720 carries no price,"
                + " and showing that would make everything free");

        // **Und  die  Bank  hat  auch  Waren  ohne  Preis.**
        var ohnePreis = bank.GetData()["items"].AsGodotArray()
            .Select(x => x.AsGodotDictionary())
            .Count(x => !x.ContainsKey("price")
                || x["price"].AsInt32() == 0);
        Console.WriteLine("Waren ohne Preis: " + ohnePreis);
        AssertTrue(ohnePreis > 0,
            "**and the bank has goods without a price too** -- and"
                + " those would show a zero and that is the game's"
                + " own data, not the reader's invention");

        // **Und  der  Kopf  nennt  die  Kaufregel.**
        AssertTrue(Rm2kLadenZeile.Kopf(state).Contains(
                "kauft und verkauft"),
            "**and the header says the shop buys and sells** -- and"
                + " 10720's first parameter is a mode with three"
                + " cases and this state opened with 0, which"
                + " buys and sells");

        // **Und  jetzt  der  zweite  Modus.**
        state.OpenShop(pType: 1, pCanBuy: true, pCanSell: false,
            pHasHandlers: false);
        Console.WriteLine("Modus 1: CanBuy " + state.CanBuy
            + "  CanSell " + state.CanSell + "  -> "
            + Rm2kLadenZeile.Kopf(state));
        AssertTrue(Rm2kLadenZeile.Kopf(state).Contains("kauft nur"),
            "**and mode 1 buys only** -- and a shop that only"
                + " buys must say so rather than look like one"
                + " that does both");

        state.OpenShop(pType: 2, pCanBuy: false, pCanSell: true,
            pHasHandlers: false);
        Console.WriteLine("Modus 2: CanBuy " + state.CanBuy
            + "  CanSell " + state.CanSell + "  -> "
            + Rm2kLadenZeile.Kopf(state));
        AssertTrue(Rm2kLadenZeile.Kopf(state).Contains("verkauft nur"),
            "**and mode 2 sells only**");
    }
}
