using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And what this game's shops actually sell.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>10720</c>, Open Shop, stands fifteen times in this
/// game</strong>, -- <strong>and the interpreter fills
/// <c>ShopItemIds</c> and the launcher shows
/// nothing</strong>.
/// </para>
/// <para>
/// <strong>And a shop that no surface displays is a dead
/// command</strong>, -- <strong>and that is a launcher gap and not an
/// engine one</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kLadenGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the goods' prices come from the item bank.
    /// </summary>
    public void Test_DieLadenpreiseAusDerItembank()
    {
        var bank = new UniversalRPG.Rm2k.Parser.Rm2kParser()
            .ParseDatabase(
                "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb");
        if (!bank.IsSuccess() || !bank.GetData().ContainsKey("items"))
        {
            AssertTrue(false, "**and the item bank is read**");
            return;
        }

        var items = bank.GetData()["items"].AsGodotArray();
        Console.WriteLine("Gegenstaende: " + items.Count);

        var mitPreis = 0;
        var teuerste = 0;
        var teuerstesName = "";
        foreach (var roh in items)
        {
            var item = roh.AsGodotDictionary();
            if (!item.TryGetValue("price", out var rohPreis))
            {
                continue;
            }

            mitPreis++;
            var preis = rohPreis.AsInt32();
            if (preis > teuerste)
            {
                teuerste = preis;
                teuerstesName = item.TryGetValue("name",
                    out var n) ? n.AsString() : "?";
            }
        }

        Console.WriteLine("mit Preis: " + mitPreis
            + "  teuerstes: " + teuerstesName + " " + teuerste);

        AssertTrue(mitPreis > 10,
            "**and the bank carries prices for many items**");

        AssertTrue(teuerste > 0,
            "**and the dearest of them costs something** -- and '"
                + teuerstesName + "' costs " + teuerste
                + " -- and a shop that shows no price teaches the"
                + " player nothing");
    }
}
