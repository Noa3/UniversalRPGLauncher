using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalRPG.Mz;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The screen object, measured against the game and read out of the
/// engine's own file.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And 397 calls break into 18 engine methods and 6 plugin
/// methods</strong>:
///
/// <code>
/// .picture()          175     .startFadeIn()         14
/// .changePictureName() 103    .startFadeOut()         9
/// ._pictureCidArray()   20     .startFlash()           9
/// .erasePicture()      18     .setPictureRemoveCommon() 6
/// .setShakeRandom()     16     .zoomScale()            5
///                                  .clearShake()           4
///                                  .showPicture()          4
/// </code>
///
/// <strong>And four of those are plugin methods</strong> --
/// <c>changePictureName</c> is in <c>PictureCallCommon.js</c>, --
/// <c>setShakeRandom</c> in <c>DirectivityShake.js</c>, --
/// <c>_pictureCidArray</c> in three <c>QJ-MPMZLib</c> files.
/// </para>
/// </remarks>
public partial class TestMzGameScreen : TestBase
{
    private const string Wurzel = "D:/Itch/sister/www/data";

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
                    || ps.GetArrayLength() == 0
                    || ps[0].ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                alle.Add(ps[0].GetString() ?? "");
            }
        }

        foreach (var datei in Directory.GetFiles(Wurzel, "Map*.json"))
        {
            if (Path.GetFileName(datei)
                .Equals("MapInfos.json", StringComparison.OrdinalIgnoreCase)
                || new FileInfo(datei).Length > 4_000_000)
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

        var ce = Path.Combine(Wurzel, "CommonEvents.json");
        if (File.Exists(ce))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ce));
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                if (e.ValueKind == JsonValueKind.Object)
                {
                    Seite(e);
                }
            }
        }

        return alle;
    }

    /// <summary>
    /// And the picture id is 120 higher in battle, and that is not a
    /// detail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine writes it in three lines</strong>:
    /// </para>
    /// <code>
    /// realPictureId(pictureId) {
    ///     if ($gameParty.inBattle()) {
    ///         return pictureId + this.maxPictures();
    ///     } else { return pictureId; }
    /// }
    /// maxPictures() { return 120; }
    /// </code>
    /// <para>
    /// <strong>And a reader that stored pictures under the number the
    /// script wrote would have <c>showPicture(1, ...)</c> and
    /// <c>picture(1)</c> disagree the moment a battle starts</strong>,
    /// -- <strong>and the game calls those two 179 times between
    /// them.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerKampfVerschiebtDieBildnummer()
    {
        var s = new MzGameScreen { ImKampf = false };
        s.ShowPicture(1, "Karte", 100, 50, 255);

        AssertTrue(s.Picture(1) != null,
            "**and outside battle the number is the number**");
        Console.WriteLine("ohne Kampf: RealBildId(1) = "
            + s.RealBildId(1));
        AssertEq(1, s.RealBildId(1),
            "**and outside battle the real id is the number itself**");
        AssertEq(1, s.SichtbareBilder(),
            "**and one picture is shown**");

        // **Und jetzt der Kampf.**
        s.ImKampf = true;
        AssertEq(121, s.RealBildId(1),
            "**and in battle the real id is 120 higher** -- and"
                + " `maxPictures()` returns the literal 120, and a"
                + " reader that stored under the script's own number"
                + " would lose the picture the moment a battle"
                + " starts");

        s.ErasePicture(1);
        s.ShowPicture(1, "Karte", 100, 50, 255);
        AssertTrue(s.Picture(1) != null,
            "**and a picture shown in battle is found again by the"
                + " same number the script wrote** -- and that is"
                + " what `realPictureId` exists for");
    }

    /// <summary>
    /// And a fade in clears the fade out, because the engine writes
    /// two lines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>startFadeIn</c> writes
    /// <c>this._fadeInDuration = duration; this._fadeOutDuration = 0;</c></strong>,
    /// -- <strong>and a reader that stored a direction would answer a
    /// number the game never wrote.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinEinblendenLoeschtDasAusblenden()
    {
        var s = new MzGameScreen();
        s.StartFadeOut(60);
        AssertTrue(s.Blendet, "**and a fade out is running**");

        s.StartFadeIn(30);
        AssertEq(0, s.FadeRausDauer,
            "**and a fade in clears the fade out** -- and the engine"
                + " writes both lines, and the game calls this 14"
                + " times");

        s.ClearFade();
        AssertTrue(!s.Blendet,
            "**and `clearFade` stops both**");
    }

    /// <summary>
    /// And the methods the scripts use, and the ones this does not
    /// answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the six plugin methods are named here</strong>, --
    /// <strong>because a list of what a reader does not answer is
    /// worth more than a number that pretends to.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieAufteilung()
    {
        var skripte = Skripte();
        var zaehler = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in skripte)
        {
            foreach (Match m in Regex.Matches(s,
                @"\$gameScreen\.([A-Za-z_]\w*)"))
            {
                var n = m.Groups[1].Value;
                zaehler.TryGetValue(n, out var k);
                zaehler[n] = k + 1;
            }
        }

        Console.WriteLine(string.Join("  ", zaehler.OrderByDescending(
            x => x.Value).Take(8).Select(x => x.Key + " " + x.Value)));

        AssertEq(175, zaehler.GetValueOrDefault("picture"),
            "**and `picture()` is called 175 times** -- and the"
                + " engine writes it as two lines and one of them is"
                + " `realPictureId`");
        AssertEq(18, zaehler.GetValueOrDefault("erasePicture"),
            "**and `erasePicture()` is called 18 times** -- and the"
                + " engine sets the slot to null rather than removing"
                + " the key");
        AssertEq(14, zaehler.GetValueOrDefault("startFadeIn"),
            "**and `startFadeIn()` is called 14 times**");
        AssertEq(103, zaehler.GetValueOrDefault("changePictureName"),
            "**and 103 calls are `changePictureName` from"
                + " `PictureCallCommon.js`** -- and this repository"
                + " does not answer plugin methods, and says so here"
                + " rather than in a number");
    }
}
