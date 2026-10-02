using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Godot;

using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The three page conditions this repository used to refuse, and the pages
/// of a finished game that ask for them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the refusal was not a limit of the format.</strong> It was one
/// line:
///
/// <code>
/// // And the fields this project does not set.
/// foreach (var feld in new[] { "actorValid", "itemValid", "variableValid" })
/// {
///     if (Gilt(pConditions, feld)) { return false; }
/// }
/// </code>
///
/// <strong>And "this project does not set" was a fact about one finished
/// MZ game and not about the format.</strong> <strong>Three finished games
/// were measured before the line changed, and not one of them sets
/// <c>actorValid</c> or <c>itemValid</c> at all.</strong>
/// </para>
/// </remarks>
public partial class TestPageConditionsThatWereRefused : TestBase
{
    /// <summary>
    /// A page that asks for a variable this repository has, and that variable
    /// reaches the value, shows itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the comparison is <c>&gt;=</c>, not <c>==</c>.</strong>
    /// That is not a guess: it is the line in
    /// <c>Game_Event.meetsConditions</c> of a finished MV game on this
    /// machine.
    /// </para>
    /// </remarks>
    public void Test_EineVariablenbedingungZeigtDieSeiteAbDemWert()
    {
        var fakten = new MzBranchFacts();

        // **Und beide Richtungen, denn ein `==` waere an beiden Stellen
        // falsch und faelle bei keinem Test auf.**
        var zu = Bedingungen(
            ("variableId", 7), ("variableValue", 100),
            ("variableValid", true));
        AssertTrue(!Meets(zu, fakten),
            "**and a variable that was never set does not satisfy a "
            + "condition that asks for a hundred**");

        fakten.SetVariable(7, 99);
        AssertTrue(!Meets(zu, fakten),
            "**and ninety-nine does not either**");

        fakten.SetVariable(7, 100);
        AssertTrue(Meets(zu, fakten),
            "**and exactly a hundred does**");

        fakten.SetVariable(7, 101);
        AssertTrue(Meets(zu, fakten),
            "**and a hundred and one does as well** -- and the engine says "
            + "`value(id) < condition` returns false, and a reader that "
            + "asked for equality would hide the page at a hundred and one");
    }

    /// <summary>
    /// A page that asks for an item, and the item is in the inventory.
    /// </summary>
    public void Test_EineGegenstandsbedingungFolgtDemInventar()
    {
        var fakten = new MzBranchFacts();
        var ohne = Bedingungen(("itemId", 3), ("itemValid", true));

        AssertTrue(!Meets(ohne, fakten),
            "**and a page asking for an item does not show while the party "
            + "has none**");
        fakten.Items[3] = 1;
        AssertTrue(Meets(ohne, fakten),
            "**and it shows as soon as `126` gives the party one**");

        // **Und eine zweite Seite, die einen anderen Gegenstand will, und
        // die faellt weiterhin durch** -- **und der Fall ist der, der die
        // Regel beweist: eine Seite ist nicht "irgendein Gegenstand".**
        var anders = Bedingungen(("itemId", 4), ("itemValid", true));
        fakten.Items[4] = 1;
        AssertTrue(Meets(anders, fakten),
            "**and a page asking for the fourth item shows once it is given**");
        AssertTrue(!Meets(
                Bedingungen(("itemId", 5), ("itemValid", true)), fakten),
            "**and a page asking for a fifth one does not**");
    }

    /// <summary>
    /// A page that asks for a party member, and `129` adds one.
    /// </summary>
    public void Test_EineAkteurbedingungFolgtDerPartei()
    {
        var fakten = new MzBranchFacts();
        var mit = Bedingungen(("actorId", 2), ("actorValid", true));

        AssertTrue(!Meets(mit, fakten),
            "**and a page asking for an actor does not show while the party "
            + "lacks them**");
        fakten.PartyMembers.Add(2);
        AssertTrue(Meets(mit, fakten),
            "**and it shows when `129` adds them**");
        var mit3 = With(fakten, 3);
        AssertTrue(Meets(
                Bedingungen(("actorId", 3), ("actorValid", true)),
                mit3),
            "**and so does a page asking for the third**");
        AssertTrue(!Meets(
                Bedingungen(("actorId", 4), ("actorValid", true)), fakten),
            "**and a page asking for a fourth does not**");
    }

    /// <summary>
    /// A page with no condition at all is not "no answer".
    /// </summary>
    public void Test_EineSeiteOhneBedingungIstNichtEineOhneAntwort()
    {
        AssertTrue(Meets(null, new MzBranchFacts()),
            "**and a page with no conditions object shows**");
        AssertTrue(Meets(Bedingungen(),
                new MzBranchFacts()),
            "**and so does a page whose conditions are all false**");
    }

    /// <summary>
    /// No finished game on this machine asks for the two conditions that
    /// would have needed a party and a database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the measurement that decided whether the refusal
    /// was worth keeping.</strong> <strong>If some game needed
    /// <c>actorValid</c>, the refusal had a reason.</strong> It does not, in
    /// any of the three.
    /// </para>
    /// </remarks>
    public void Test_KeinFertigesSpielHierBrauchtDieBeidenBedingungen()
    {
        var spiele = new[]
        {
            "D:/Itch/sister/www/data",
            "E:/RPGMakerGames/LegalTruck_v1.1/www/data",
            "E:/RPGMakerGames/CamelliaCoronation-Win/data",
        };

        var geprueft = 0;
        var mitAkteur = 0;
        var mitGegenstand = 0;
        var seiten = 0;
        foreach (var ordner in spiele)
        {
            if (!Directory.Exists(ordner))
            {
                continue;
            }

            geprueft++;
            foreach (var datei in Directory.GetFiles(ordner, "Map*.json",
                SearchOption.AllDirectories))
            {
                if (!Path.GetFileNameWithoutExtension(datei)[3..]
                    .All(char.IsDigit))
                {
                    continue;
                }

                JsonElement karte;
                try
                {
                    karte = JsonDocument.Parse(File.ReadAllText(datei))
                        .RootElement;
                }
                catch (JsonException)
                {
                    continue;
                }

                if (!karte.TryGetProperty("events", out var events))
                {
                    continue;
                }

                foreach (var ereignis in events.EnumerateArray())
                {
                    if (ereignis.ValueKind != JsonValueKind.Object
                        || !ereignis.TryGetProperty("pages", out var seitenListe))
                    {
                        continue;
                    }

                    foreach (var seite in seitenListe.EnumerateArray())
                    {
                        if (seite.ValueKind != JsonValueKind.Object
                            || !seite.TryGetProperty("conditions", out var bed))
                        {
                            continue;
                        }

                        seiten++;
                        if (Gilt(bed, "actorValid"))
                        {
                            mitAkteur++;
                        }

                        if (Gilt(bed, "itemValid"))
                        {
                            mitGegenstand++;
                        }
                    }
                }
            }
        }

        System.Console.WriteLine(
            "Seitenbedingungen: " + seiten + " Seiten in " + geprueft
            + " Spielen, actorValid=" + mitAkteur + ", itemValid="
            + mitGegenstand);
        AssertTrue(geprueft >= 3,
            "**and all three finished games were read** -- " + geprueft);
        AssertTrue(mitAkteur == 0 && mitGegenstand == 0,
            "**and not one page in any of them asks for a party member or an "
                + "item** -- actorValid " + mitAkteur + "x, itemValid "
                + mitGegenstand + "x, and the refusal named them as fields "
                + "no project sets, and no project here does");
    }

    // ---------------------------------------------------------------------

    private static bool Gilt(JsonElement pBed, string pFeld)
    {
        return pBed.TryGetProperty(pFeld, out var wert)
            && wert.ValueKind == JsonValueKind.True;
    }

    private static bool Gilt(MzValue pBed, string pFeld)
    {
        var m = pBed.Member(pFeld);
        return m != null && m.Kind == MzKind.Bool && m.Boolean;
    }

    private static bool Meets(MzValue? pBed, MzBranchFacts pFacts)
    {
        return MzMapFigureReader.Meets(pBed, pFacts, 1, 1);
    }

    /// <summary>
    /// The same facts with one more party member, so a page asking for
    /// somebody else can be asked about without disturbing the first.
    /// </summary>
    private static MzBranchFacts With(MzBranchFacts pFacts, int pActor)
    {
        pFacts.PartyMembers.Add(pActor);
        return pFacts;
    }

    /// <summary>
    /// A conditions object, written the way a game writes it: numbers as
    /// numbers and flags as booleans.
    /// </summary>
    private static MzValue Bedingungen(
        params (string Feld, object Wert)[] pFelder)
    {
        var text = "{" + string.Join(",", pFelder.Select(p =>
            "\"" + p.Feld + "\":" + (p.Wert is bool || p.Wert is int
                ? p.Wert.ToString()!.ToLowerInvariant()
                : "\"" + p.Wert + "\""))) + "}";
        MzJson.TryParse(text, out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException(
                "The fixture is not JSON: " + fehler + " in " + text);
        }

        return wert;
    }
}