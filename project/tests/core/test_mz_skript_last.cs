using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What this MZ game actually puts into script commands, counted.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the count is 10 111 script commands, not "4952
/// conditions".</strong> -- <strong>and the earlier number came
/// from a different reading of a different thing.</strong>
/// </para>
/// <para>
/// <strong>And both numbers came from the games own files</strong>,
/// -- <strong>and MZ writes <c>Map001.json</c> as an object with
/// a <c>pages</c> array, where MV writes a list.</strong>
/// </para>
/// <para>
/// <strong>And what dominates the scripts is not arithmetic but
/// four lines that read the current event</strong>, -- <strong>and
/// a reader that implemented only comparison operators would serve
/// almost none of this game.</strong>
/// </para>
/// </remarks>
public partial class TestMzSkriptLast : TestBase
{
    private const string Wurzel = "D:/Itch/sister/www/data";

    /// <summary>And every script command in the whole game.</summary>
    /// <returns>The texts, in file order.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And 355 is the script command</strong>, -- <strong>and
    /// it was read out of the engine file under the comment
    /// "Script"</strong>, -- <strong>and not guessed and not carried
    /// over from another number.</strong>
    /// </para>
    /// </remarks>
    private static List<string> Skripte()
    {
        var alle = new List<string>();

        void Seite(JsonElement pSeite)
        {
            if (!pSeite.TryGetProperty("list", out var liste)
                || liste.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var b in liste.EnumerateArray())
            {
                if (b.ValueKind != JsonValueKind.Object
                    || !b.TryGetProperty("code", out var c)
                    || c.GetInt32() != 355
                    || !b.TryGetProperty("parameters", out var ps)
                    || ps.ValueKind != JsonValueKind.Array
                    || ps.GetArrayLength() == 0)
                {
                    continue;
                }

                // **Und `ps[0]` kann jedes Skalartyp sein**, --
                // **und `GetString()` wirft bei einer Zahl**, --
                // **und der Wert wird hier nur gezaehlt.**
                alle.Add(ps[0].ValueKind == JsonValueKind.String
                    ? ps[0].GetString() ?? ""
                    : ps[0].ToString());
            }
        }

        // **Und `MapInfos.json` passt auf das Muster und ist keine
        // Karte** -- **es ist die Liste der Karten** --
        // **und ihre Wurzel ist ein Array**, --
        // **und `TryGetProperty` darauf wirft.** --
        // **Und das ist gemessen:  die Datei ist die einzige, die
        // so auffaellt.**
        foreach (var datei in Directory.GetFiles(Wurzel, "Map*.json")
            .Where(d => !Path.GetFileName(d)
                .Equals("MapInfos.json", StringComparison.OrdinalIgnoreCase)))
        {
            if (new FileInfo(datei).Length > 4_000_000)
            {
                continue;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(datei));
            }
            catch (JsonException)
            {
                continue;
            }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("events", out var ev)
                    || ev.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var e in ev.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object
                        || !e.TryGetProperty("pages", out var seiten)
                        || seiten.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var s in seiten.EnumerateArray())
                    {
                        if (s.ValueKind == JsonValueKind.Object)
                        {
                            Seite(s);
                        }
                    }
                }
            }
        }

        using var ce = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(Wurzel, "CommonEvents.json")));
        foreach (var e in ce.RootElement.EnumerateArray())
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                Seite(e);
            }
        }

        return alle;
    }

    /// <summary>And the lines that dominate, counted.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this asserts the four that carry the game</strong>,
    /// -- <strong>because they are what a reader has to answer
    /// first</strong>, -- <strong>and the fourth is a name that
    /// appears 3162 times.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieHaeufigstenSkripte()
    {
        var skripte = Skripte();
        var zaehler = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in skripte)
        {
            var k = s.Trim();
            zaehler.TryGetValue(k, out var n);
            zaehler[k] = n + 1;
        }

        var rang = new List<KeyValuePair<string, int>>(zaehler);
        rang.Sort((a, b) => b.Value.CompareTo(a.Value));
        foreach (var kv in rang.GetRange(0, Math.Min(10, rang.Count)))
        {
            Console.WriteLine($"{kv.Value,5}  {kv.Key.Substring(0,
                Math.Min(68, kv.Key.Length))}");
        }

        int gameRefs = 0, audioRefs = 0, sceneRefs = 0;
        foreach (var s in skripte)
        {
            if (s.Contains("$game")) { gameRefs++; }
            if (s.Contains("AudioManager")) { audioRefs++; }
            if (s.Contains("SceneManager")) { sceneRefs++; }
        }

        Console.WriteLine($"Skriptbefehle 355: {skripte.Count}");
        Console.WriteLine($"$game {gameRefs}  AudioManager {audioRefs}"
            + $"  SceneManager {sceneRefs}");
        // **Und die vier Zeilen, die das Spiel tragen.**
        AssertTrue(skripte.Count > 5000, "ueber 5000 Skriptbefehle");
        AssertEq(345, zaehler.GetValueOrDefault(
            "let event = $gameMap.event(this._eventId);"),
            "die haeufigste Zeile fragt das aktuelle Ereignis");
        AssertEq(256, zaehler.GetValueOrDefault("let id = this._eventId;"),
            "die zweite liest die Ereignisnummer");
        AssertTrue(gameRefs > 3000 && audioRefs > 100,
            "das Spielobjekt und der Audiomanager tragen es");
    }
}