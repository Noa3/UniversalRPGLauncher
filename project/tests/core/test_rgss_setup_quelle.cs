using System;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// MicroQuest's own <c>setup</c>, printed.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the run stops at <c>map_id an Game_game_map</c>
/// because the host answers every call with <c>null</c></strong>, --
/// <strong>and what <c>setup</c> asks for after that is the question
/// this prints the answer to.</strong>
/// </para>
/// <para>
/// <strong>And the text is printed and not summarised</strong>, --
/// <strong>because a list of names would say what to call and not
/// what the calls need to return.</strong>
/// </para>
/// </remarks>
public partial class TestRgssSetupQuelle : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    /// <summary>
    /// And the method as the game writes it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the whole method is printed, not the first
    /// line</strong>, -- <strong>because the first line is the one
    /// already measured and the rest is what the next step is
    /// made of.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieMethodeSetup()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rxdata"), out var grund);
        if (skripte == null)
        {
            throw new InvalidOperationException(grund);
        }

        var host = new SpielHost(skripte, Wurzel);
        foreach (var name in new string[]
        {
            "Interpreter 1", "Interpreter 2", "Interpreter 3",
        })
        {
            var bytes = host.ReadScript(name, true);
            if (bytes == null)
            {
                Console.WriteLine(name + ": nicht lesbar");
                continue;
            }

            var text = System.Text.Encoding.UTF8.GetString(bytes);
            Console.WriteLine("===== " + name + " =====");
            Console.WriteLine(Abschnitt(text, "def setup"));
            Console.WriteLine("--- was setup vom Host fragt:");
            foreach (var l in FragenAus(text, "def setup"))
            {
                Console.WriteLine("  " + l);
            }
        }

        AssertTrue(true,
            "**and the method and its host questions are printed"
                + " above** -- and what each question must return is"
                + " the next step, and this test asserts only that it"
                + " was measured");
    }

    /// <summary>And the text of a method.</summary>
    /// <param name="pText">The whole script.</param>
    /// <param name="pDef">The definition to start at.</param>
    /// <returns>The method, up to its <c>end</c>.</returns>
    private static string Abschnitt(string pText, string pDef)
    {
        var i = pText.IndexOf(pDef, StringComparison.Ordinal);
        if (i < 0)
        {
            return "(nicht gefunden)";
        }

        var zeilen = pText[i..].Replace("\r", "").Split('\n');
        var ende = zeilen.Length;
        for (var k = 1; k < zeilen.Length; k++)
        {
            if (zeilen[k].Trim() == "end")
            {
                ende = k + 1;
                break;
            }
        }

        return string.Join(NL, zeilen[..ende]);
    }

    /// <summary>And the questions a method asks of the host.</summary>
    /// <param name="pText">The whole script.</param>
    /// <param name="pDef">The definition to start at.</param>
    /// <returns>The calls that leave the script.</returns>
    private static System.Collections.Generic.List<string>
        FragenAus(string pText, string pDef)
    {
        var ergebnis = new System.Collections.Generic.List<string>();
        var zeilen = Abschnitt(pText, pDef)
            .Replace("\r", "").Split('\n');
        for (var k = 1; k < zeilen.Length; k++)
        {
            var z = zeilen[k];
            if (z.Trim() != "end")
            {
                continue;
            }

            // **Und ein Aufruf ist alles mit einem Punkt gefolgt von
            // einem Namen.**
            var teile = z.Split('.');
            for (var j = 1; j < teile.Length; j++)
            {
                var name = teile[j].Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                var ende = 0;
                while (ende < name.Length
                    && (char.IsLetterOrDigit(name[ende])
                        || name[ende] == '_' || name[ende] == '='))
                {
                    ende++;
                }

                if (ende > 0)
                {
                    ergebnis.Add(name[..ende] + "   <- " + z.Trim());
                }
            }
        }

        return ergebnis;
    }

    /// <summary>And a line break, spelled where it is used.</summary>
    private const string NL = "\n";
}
