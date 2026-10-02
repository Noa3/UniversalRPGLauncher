using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Godot;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The three commands that leave the map for another screen, and the two
/// that give an actor a different shape.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>push</c> and <c>goto</c> are not the same
/// operation</strong>, <strong>and an event list writes both</strong>.
/// Measured at <c>rpg_managers.js</c>:
/// </para>
/// <code>
/// static goto(sceneClass) {
///     if (sceneClass) { this._nextScene = new sceneClass(); }
///     if (this._scene) { this._scene.stop(); }
/// };
/// static push(sceneClass) {
///     this._stack.push(this._scene.constructor);
///     this.goto(sceneClass);
/// };
/// </code>
/// <para>
/// <strong>And the difference is one line</strong> -- <strong><c>push</c>
/// remembers the scene it came from and <c>goto</c> does not.</strong>
/// <strong>So <c>352 Save</c> can be left and <c>354 Return to Title</c>
/// cannot.</strong>
/// </para>
/// <para>
/// <strong>And measured at <c>D:/Itch/sister/www</c>: <c>352</c> nine
/// times, <c>354</c> seven, <c>353</c> zero, <c>321</c> eight, <c>319</c>
/// eleven</strong> -- <strong>and all of <c>352</c> and <c>354</c> carry
/// no parameters at all.</strong>
/// </para>
/// </remarks>
public partial class TestMvSceneChange : TestBase
{
    private const string Projekt = "D:/Itch/sister/www";

    private static bool Vorhanden()
    {
        if (Directory.Exists(Projekt + "/data"))
        {
            return true;
        }

        GD.Print("    (skipped: no MV project at " + Projekt + ")");
        return false;
    }

    /// <summary>
    /// This game's own 352, 354, 321 and 319, and what they carry.
    /// </summary>
    public void Test_DieVierBefehleAusDiesemSpielUndIhreFormen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var zahlen = new Dictionary<int, int>();
        var ohneParameter = new HashSet<int>();
        foreach (var datei in Karten())
        {
            foreach (var c in BefehleDerKarte(datei))
            {
                if (c.Code != MzCommandTable.SaveGame
                    && c.Code != MzCommandTable.ReturnToTitle
                    && c.Code != MzCommandTable.ChangeClass
                    && c.Code != MzCommandTable.ChangeEquipment)
                {
                    continue;
                }

                zahlen[c.Code] = zahlen.GetValueOrDefault(c.Code) + 1;
                if (c.Parameters.Count == 0)
                {
                    ohneParameter.Add(c.Code);
                }
            }
        }

        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.SaveGame) >= 9,
            "**and this game asks for the save screen nine times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.SaveGame));
        AssertTrue(
            zahlen.GetValueOrDefault(MzCommandTable.ReturnToTitle) >= 7,
            "**and the title screen seven times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ReturnToTitle));
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeClass) >= 8,
            "**and a class change eight times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeClass));
        AssertTrue(
            zahlen.GetValueOrDefault(MzCommandTable.ChangeEquipment) >= 11,
            "**and an equipment change eleven times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeEquipment));

        // **Und beide Szenenbefehle tragen keine Parameter** -- **und ein
        // Leser, der bei beiden einen Parameter erwartet, liest einen,
        // den es nicht gibt.**
        AssertTrue(
            ohneParameter.Contains(MzCommandTable.SaveGame)
            && ohneParameter.Contains(MzCommandTable.ReturnToTitle),
            "**and every 352 and every 354 carries no parameters** -- "
            + "and that is `[]` in all sixteen, and neither "
            + "command reads a parameter at all");
    }

    /// <summary>
    /// And push remembers where it came from and goto does not.
    /// </summary>
    public void Test_PushMerktSichDieVorherigeSzeneUndGotoNicht()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.SaveGame),
            Befehl(MzCommandTable.ReturnToTitle),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        // **Und die Karte ist die Szene, von der aus `push` geht.**
        fakten.Szene.GeheZu("Scene_Map");
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.Szene.Stack.Count, 1,
            "**and one push leaves one thing on the stack** -- "
            + fakten.Szene.Stack.Count + ", and `push` is `this._stack."
            + "push(this._scene.constructor); this.goto(sceneClass);`");
        AssertEq(fakten.Szene.Stack[0], "Scene_Map",
            "**and it is the scene the game was on** -- it is '"
            + fakten.Szene.Stack[0] + "', and a save screen the player "
            + "can leave and a title screen they cannot are one line of "
            + "difference in the engine");
        AssertEq(fakten.Szene.Current, "Scene_Title",
            "**and the title screen is what stands there now** -- it is '"
            + fakten.Szene.Current + "', and `goto` did not push, so "
            + "coming back out of it means coming back out of the map "
            + "and not out of the save screen");
    }

    /// <summary>
    /// And a 352 in a battle does nothing, as the engine's own `if` says.
    /// </summary>
    public void Test_EinSpeichernImKampfTutNichtsUndIstKeinFehler()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.SaveGame),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        // **Und `InBattle` ist `private set`** -- **und
        // `EnterBattle()` ist der einzige Weg hinein**, **und das ist
        // `301 Battle Processing`'s Weg.**
        fakten.EnterBattle();
        var aktionen = new List<MzAction>();
        interp.Run(aktionen, fakten);

        AssertEq(fakten.Szene.Current, "",
            "**and nothing was shown** -- the scene is '"
            + fakten.Szene + "', and `command352` is `if ("
            + "!$gameParty.inBattle()) { SceneManager.push(Scene_Save); }"
            + " return true;`");
        AssertEq(fakten.Szene.Stack.Count, 0,
            "**and nothing was pushed** -- " + fakten.Szene.Stack.Count
            + ", and the engine's own condition is not a refusal");
        AssertTrue(fakten.Notices.Count > 0,
            "**and it says why it did nothing** -- and it said '"
            + string.Join(" / ", fakten.Notices) + "', and a command "
            + "that did nothing and said nothing is what a reader that "
            + "only counts steps looks like");
    }

    /// <summary>
    /// And 319's third parameter is not a direction.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>changeEquipById</c> is <c>const slotId = etypeId -
    /// 1; if (this.equipSlots()[slotId] === 1) { this.changeEquip(slotId,
    /// $dataWeapons[itemId]); } else { this.changeEquip(slotId,
    /// $dataArmors[itemId]); }</c></strong> -- <strong>a slot type and an
    /// item id, and no "equip or unequip" anywhere.</strong>
    /// </remarks>
    public void Test_DerDritteParameterIstDieNummerUndKeineRichtung()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeEquipment, "2", "2", "156"),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts
        {
            PartyMembers = new HashSet<int> { 2 },
        };
        interp.Run(new List<MzAction>(), fakten);

        AssertTrue(fakten.Equipment.Contains((2, 156)),
            "**and it wears item 156** -- and it holds "
            + fakten.Equipment.Count + " things, and this game's own "
            + "form is `[2, 2, 156]`, and a reader that took the third "
            + "parameter as a direction would have stored a direction "
            + "and no item at all");
        AssertTrue(!fakten.Equipment.Contains((2, 0)),
            "**and it is not item zero** -- and a reader that read the "
            + "third parameter as a flag would store (actor, 1) or "
            + "(actor, 0), and neither is an item");
    }
    /// <summary>
    /// This project's maps, and the common events with them.
    /// </summary>
    /// <remarks>
    /// <strong>And both, because `CommonEvents.json` holds eight of this
    /// game's 321</strong> -- <strong>and a count over the maps alone is a
    /// smaller number and not the whole one.</strong>
    /// </remarks>
    private static IEnumerable<string> Karten()
    {
        // **Und `SearchOption.AllDirectories`, und nicht ohne** --
        // **denn dieses Spiel legt vier `GameLanguage`-Pakete und
        // achtzehn Sprachordner unter `data/` ab**, **und zusammen sind
        // das 62 Karten plus 243 weitere.**
        //
        // **Und das ist keine Foehlsuche, sondern die Sache selbst:**
        // **`352` steht viermal in `data/` und viermal in den
        // Sprachpaketen, `321` steht keine einzige in `data/` und
        // achtmal in `CommonEvents.json`, und `311` siebenmal in den
        // Karten und siebenmal in den gemeinsamen Ereignissen.**
        //
        // **Und meine erste Fassung dieses Tests zaehlte nur die erste
        // Ebene** -- **und fand 5 statt 9 `352`, 3 statt 7 `354` und 3
        // statt 11 `319`** -- **und das waren nicht zu wenige, sondern
        // eine andere Frage: nur die Karten der Hauptsprache.**
        foreach (var pfad in Directory.GetFiles(
            Projekt + "/data", "Map*.json", SearchOption.AllDirectories))
        {
            yield return pfad;
        }

        yield return Projekt + "/data/CommonEvents.json";
    }

    private static IEnumerable<MzCommandEntry> BefehleDerKarte(string pPfad)
    {
        var karte = JsonDocument.Parse(File.ReadAllText(pPfad)).RootElement;
        // **Und `CommonEvents.json` ist ein Array aus Objekten mit einem
        // `list`, und eine Karte ist ein Objekt mit einem `events`** --
        // **und beide kommen in denselben Spielen vor.** **Und die erste
        // Fassung dieses Lesers rief `GetProperty("events")` und warf auf
        // dem Array, und die ganze Zaehlung war weg.**
        if (karte.ValueKind == JsonValueKind.Array)
        {
            foreach (var eintrag in karte.EnumerateArray())
            {
                if (eintrag.ValueKind != JsonValueKind.Object
                    || !eintrag.TryGetProperty("list", out var liste))
                {
                    continue;
                }

                foreach (var befehl in liste.EnumerateArray())
                {
                    if (befehl.ValueKind == JsonValueKind.Object)
                    {
                        yield return MzCommandEntry.From(AlsWert(befehl));
                    }
                }
            }

            yield break;
        }

        if (!karte.TryGetProperty("events", out var events))
        {
            yield break;
        }

        foreach (var ereignis in events.EnumerateArray())
        {
            if (ereignis.ValueKind != JsonValueKind.Object
                || !ereignis.TryGetProperty("pages", out var seiten))
            {
                continue;
            }

            foreach (var seite in seiten.EnumerateArray())
            {
                if (seite.ValueKind != JsonValueKind.Object
                    || !seite.TryGetProperty("list", out var liste))
                {
                    continue;
                }

                foreach (var befehl in liste.EnumerateArray())
                {
                    if (befehl.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var roh = "{\"code\":"
                        + (befehl.TryGetProperty("code", out var c)
                            ? c.GetRawText() : "0")
                        + ",\"indent\":"
                        + (befehl.TryGetProperty("indent", out var d)
                            ? d.GetRawText() : "0")
                        + ",\"parameters\":"
                        + (befehl.TryGetProperty("parameters", out var ps)
                            ? ps.GetRawText() : "[]")
                        + "}";
                    MzJson.TryParse(roh, out var wert, out var _fehler);
                    yield return MzCommandEntry.From(wert);
                }
            }
        }
    }

    /// <summary>
    /// One command out of a <c>JsonElement</c>, and the reader takes an
    /// <c>MzValue</c>.
    /// </summary>
    private static MzValue AlsWert(JsonElement pBefehl)
    {
        var roh = "{\"code\":"
            + (pBefehl.TryGetProperty("code", out var c)
                ? c.GetRawText() : "0")
            + ",\"indent\":"
            + (pBefehl.TryGetProperty("indent", out var d)
                ? d.GetRawText() : "0")
            + ",\"parameters\":"
            + (pBefehl.TryGetProperty("parameters", out var ps)
                ? ps.GetRawText() : "[]")
            + "}";
        MzJson.TryParse(roh, out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException("fixture: " + fehler);
        }

        return wert;
    }

    /// <summary>
    /// One command out of a <c>JsonElement</c>, and the reader takes an
    /// <c>MzValue</c> and not a <c>JsonElement</c>.

    private static MzCommandEntry Befehl(int pCode, params string[] pParameter)
    {
        var json = "{\"code\":" + pCode + ",\"indent\":0,"
            + "\"parameters\":["
            + string.Join(",", pParameter) + "]}";
        MzJson.TryParse(json, out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException("fixture: " + fehler);
        }

        return MzCommandEntry.From(wert);
    }
}
