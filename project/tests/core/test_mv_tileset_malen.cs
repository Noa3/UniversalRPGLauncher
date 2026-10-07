using System;
using System.IO;
using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Mz;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the MV tileset paints, measured with and without its own suffix.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the runtime reports nothing for MV, so the question moves
/// down one level.</strong> A runtime that returns no image cannot be
/// debugged through the runtime; the same tileset can be pushed through
/// the renderer directly, and then the difference between the file that
/// exists and the file the renderer looks for is visible.
/// </para>
/// </remarks>
public partial class TestMvTilesetLaesstSichMalen : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/LegalTruck_v1.1/www";

    private static bool Vorhanden() =>
        File.Exists(Projekt + "/data/Tilesets.json")
        && File.Exists(Projekt + "/data/System.json");

    public void Test_DerMvKachelBildLaesstSichEntschluesselnUndMalen()
    {
        if (!Vorhanden())
        {
            return;
        }

        // **Und der Schluessel kommt aus dem Spiel, nicht aus einer
        // Konstanten** -- **`SchluesselDesSpiels()` gibt den MD5 des
        // Leerstrings zurueck, und das ist der Schluessel eines Spiels,
        // das seine Bilder nicht verschluesselt.**
        var system = MzDataFile.Read("data/System.json",
            File.ReadAllBytes(Projekt + "/data/System.json"));
        var schluessel = system.Root.Member("encryptionKey")?.StringOr("") ?? "";
        Console.WriteLine($"PROBE key='{schluessel}' len={schluessel.Length}");
        Console.WriteLine($"PROBE constant='{MzVerschluesselung.SchluesselDesSpiels()}'");

        foreach (var kandidat in new[] { "World_A1.rpgmvp", "World_A1.png_", "World_A1" })
        {
            var pfad = Path.Combine(Projekt, "img", "tilesets", kandidat);
            Console.WriteLine($"PROBE {kandidat}: exists={File.Exists(pfad)}");
            if (!File.Exists(pfad))
            {
                continue;
            }
            var roh = File.ReadAllBytes(pfad);
            Console.WriteLine($"PROBE   encrypted={MzVerschluesselung.IstVerschluesselt(roh)} "
                + $"looksPng={MzVerschluesselung.SiehtWiePngAus(roh)} size={roh.Length}");
            var bild = MzImageReader.Read(roh, schluessel, out var grund);
            Console.WriteLine($"PROBE   read={(bild == null ? "null" : bild.Length.ToString())} reason={grund}");
            if (bild == null)
            {
                continue;
            }
            var dekodiert = false;
            Rm2kIndexedImage.TryParse(bild, out var ind, out var grund2);
            Console.WriteLine($"PROBE   indexed={ind != null} reason={grund2}");
        }
    }
}
