using System;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that buying a good moves gold and changes the bag.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a shop that showed prices and could not buy was
/// half a command</strong>, -- <strong>and the reference's number
/// window caps at <c>GetGold() / price</c></strong>.
/// </para>
/// <para>
/// <strong>And the prices are the bank's own</strong>, -- <strong>and
/// the goods are the bank's own</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kLadenkaufZiehtGold : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the gold, the bag and the refusals all move.
    /// </summary>
    public void Test_DerKaufZiehtGoldUndFuelltDenBeutel()
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
        state.Gold = 100;

        var trank = bank.GetData()["items"].AsGodotArray()
            .Select(x => x.AsGodotDictionary())
            .First(x => x["name"].AsString().Contains("Potion")
                && x["price"].AsInt32() > 0);
        var itemId = trank["id"].AsInt32();
        var preis = trank["price"].AsInt32();
        Console.WriteLine("Ware " + trank["name"].AsString()
            + " id " + itemId + " Preis " + preis);

        var ok = Rm2kLadenKauf.Kaufe(state, bank.GetData(), itemId,
            3, out var grund);
        Console.WriteLine("3 Stueck: " + ok + " -> " + grund
            + "  Gold " + state.Gold + "  Beutel "
            + (state.ItemCounts.TryGetValue(itemId, out var n) ? n : 0));

        AssertTrue(ok,
            "**and buying three is accepted**");
        AssertEq(100 - preis * 3, state.Gold,
            "**and the gold is the bank's price times three**");
        AssertEq(3, state.ItemCounts[itemId],
            "**and the bag holds three**");

        // **Und  jetzt  das  Gold  reicht  nicht.**
        var zuTeuer = Rm2kLadenKauf.Kaufe(state, bank.GetData(),
            itemId, 100, out var fehler2);
        Console.WriteLine("100 Stueck: " + zuTeuer + " -> " + fehler2);
        AssertTrue(!zuTeuer,
            "**and a hundred is refused** -- and the reference"
                + " caps the number at gold / price");
        AssertTrue(fehler2.Contains("gold"),
            "**and the refusal names the gold**");

        // **Und  jetzt  ein  Shop,  der  nicht  kauft.**
        state.OpenShop(pType: 2, pCanBuy: false, pCanSell: true,
            pHasHandlers: false);
        var nichtKaufen = Rm2kLadenKauf.Kaufe(state, bank.GetData(),
            itemId, 1, out var fehler3);
        Console.WriteLine("Modus 2: " + nichtKaufen + " -> " + fehler3);
        AssertTrue(!nichtKaufen,
            "**and a shop that only sells does not buy**");

        // **Und  jetzt  eine  Ware  ohne  Preis** -- **und  die
        //  Referenz  teilt  dort  durch  null.**
        var ohnePreis = bank.GetData()["items"].AsGodotArray()
            .Select(x => x.AsGodotDictionary())
            .First(x => !x.ContainsKey("price")
                || x["price"].AsInt32() == 0);
        state.OpenShop(pType: 0, pCanBuy: true, pCanSell: true,
            pHasHandlers: false);
        var nullDivision = Rm2kLadenKauf.Kaufe(state, bank.GetData(),
            ohnePreis["id"].AsInt32(), 1, out var fehler4);
        Console.WriteLine("ohne Preis '" + ohnePreis["name"].AsString()
            + "': " + nullDivision + " -> " + fehler4);
        AssertTrue(!nullDivision,
            "**and a good the bank gives away is refused** -- and"
                + " the reference divides the gold by the price"
                + " and that is a division by zero");
        AssertTrue(fehler4.Contains("divide"),
            "**and the refusal says why**");
    }
}
