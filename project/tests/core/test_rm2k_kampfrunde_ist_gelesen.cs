using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a troop's battle commands are read, and which ones.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this closes the gap the last commit named.</strong> --
/// <strong>All one hundred and six troops carry a 133 byte page
/// field</strong>, -- <strong>and <c>TroopPage</c> was named in no
/// file of this repository.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKampfrundeIstGelesen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    private static List<byte[]> Seiten()
    {
        var liste = new List<byte[]>();
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            return liste;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["troops"];
        foreach (var t in truppen)
        {
            foreach (var f in (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                t["unknown_fields"])
            {
                if (f["id"].AsInt32() == 0x0B)
                {
                    liste.Add((byte[])f["data"]);
                }
            }
        }

        return liste;
    }

    /// <summary>
    /// And how many pages read, and which commands they hold.
    /// </summary>
    public void Test_DieKampfrundenSindLesbar()
    {
        var seiten = Seiten();
        var gelesen = 0;
        var verweigert = new List<string>();
        var haeufig = new Dictionary<int, int>();
        var beispiele = new List<string>();

        for (var i = 0; i < seiten.Count; i++)
        {
            if (!Rm2kTroopPageDecoder.TryDecode(
                    seiten[i], out var befehle, out var warum))
            {
                if (verweigert.Count < 4)
                {
                    verweigert.Add("#" + (i + 1) + ": " + warum);
                }

                continue;
            }

            gelesen++;
            foreach (var b in befehle)
            {
                var code = b["code"].AsInt32();
                haeufig[code] = haeufig.GetValueOrDefault(code) + 1;
            }

            if (beispiele.Count < 4)
            {
                var teile = new List<string>();
                foreach (var b in befehle)
                {
                    var code = b["code"].AsInt32();
                    var text = b.ContainsKey("text")
                        ? b["text"].AsString() : "";
                    teile.Add(code
                        + (text.Length > 0 ? " \"" + text + "\"" : ""));
                }

                beispiele.Add("#" + (i + 1) + ": "
                    + string.Join(" | ", teile));
            }
        }

        foreach (var b in beispiele)
        {
            Console.WriteLine(b);
        }

        Console.WriteLine($"gelesen {gelesen} von {seiten.Count}, "
            + $"verweigert {verweigert.Count}");
        foreach (var v in verweigert)
        {
            Console.WriteLine("  " + v);
        }

        var rang = haeufig.OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key).ToList();
        Console.WriteLine("Befehle: "
            + string.Join(", ",
                rang.Take(14).Select(x => x.Key + ":" + x.Value)));

        AssertTrue(gelesen > 100,
            "**and more than a hundred pages were read** -- and"
                + " that is what no file of this repository could"
                + " do a commit ago");
    }

    /// <summary>
    /// And that the game's own words come out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion that proves the bytes are
    /// commands and not a number list.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieEigenenWoerterDesSpiels()
    {
        var gelesen = 0;
        var woerter = new List<string>();
        foreach (var seite in Seiten())
        {
            if (!Rm2kTroopPageDecoder.TryDecode(
                    seite, out var befehle, out _))
            {
                continue;
            }

            gelesen++;
            foreach (var b in befehle)
            {
                if (b.ContainsKey("text"))
                {
                    var t = b["text"].AsString();
                    if (t.Length > 3 && woerter.Count < 8)
                    {
                        woerter.Add(t);
                    }
                }
            }

            if (gelesen >= 20)
            {
                break;
            }
        }

        foreach (var w in woerter)
        {
            Console.WriteLine("  \"" + w + "\"");
        }

        AssertTrue(woerter.Any(x => x.Contains("monster")
                || x.Contains("Nothing")
                || x.Contains("But")),
            "**and the game's own sentences come out** -- and"
                + " \"The monsters fled!\" was in the bytes and no"
                + " reader in this repository could reach it");

        AssertTrue(woerter.Count > 0,
            "**and there is text at all**");
    }
}
