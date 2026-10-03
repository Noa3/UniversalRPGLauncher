using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Every VX script of Random Dungeon, and the text where one fails.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the number fell from thirteen to eight to four.</strong>
/// The one-line <c>for</c> form took it to eight, and the assignment
/// lookahead took it to four, -- <strong>and a number says nothing
/// about which script to read next.</strong>
/// </para>
/// <para>
/// <strong>And the text around each failure is printed, because
/// "'+=' at offset 8193 does not begin an expression" names no
/// construct at all</strong>, -- <strong>and without the text this
/// test could not be acted on.</strong>
/// </para>
/// </remarks>
public partial class TestRgssVxRestTexte : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/Random Dungeon -English Version-";

    /// <summary>
    /// And every script, and for a failure its text and its line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this asserts the bound and not the list</strong>,
    /// -- <strong>because the list is what changes and the bound is
    /// what must not grow unnoticed.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieVierRestlichen()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rvdata"), out var grund);
        if (skripte == null)
        {
            throw new InvalidOperationException(grund);
        }

        var host = new SpielHost(skripte, Wurzel);
        var fehler = new List<string>();

        foreach (var name in new List<string>(host.KnownMethods))
        {
            var bytes = host.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            var text = System.Text.Encoding.UTF8.GetString(bytes);
            string meldung;
            bool laufzeit;
            try
            {
                new RubyParser(new RubyLexer(text).Tokenize()).ParseProgram();
                continue;
            }
            catch (RubyParseException ausnahme)
            {
                meldung = ausnahme.Message;
                laufzeit = false;
            }
            catch (RubyRuntimeException ausnahme)
            {
                meldung = ausnahme.Message;
                laufzeit = true;
            }

            Console.WriteLine("VX-FEHLER " + name + (laufzeit ? " [Laufzeit] "
                : " [Parse] ") + meldung);
            var stelle = Fehlerstelle(meldung);
            if (stelle >= 0 && stelle < text.Length)
            {
                var zeile = 1;
                for (var k = 0; k < stelle; k++)
                {
                    if (text[k] == '\n')
                    {
                        zeile++;
                    }
                }

                var von = Math.Max(0, stelle - 100);
                var bis = Math.Min(text.Length, stelle + 100);
                Console.WriteLine("  Zeile " + zeile + ": |"
                    + text[von..bis].Replace("\r", "")
                        .Replace("\n", " | ") + "|");
            }

            fehler.Add(name);
        }

        Console.WriteLine("VX noch fehlgeschlagen: " + fehler.Count
            + " von " + host.KnownMethods.Count);
        foreach (var f in fehler)
        {
            Console.WriteLine("  - " + f);
        }

        // **Und die Schranke ist null,  und nicht "hoechstens vier".**
        //
        // **Und der Weg dorthin ist gemessen und nicht geschaetzt:**
        //
        // ```text
        // 13  alle,  bis auf  Scene_Battle und die drei Komma-Skripte
        //  8  die einzeilige `for`-Form  --  InForCollection
        //  4  die Ziellisten-Vorausschau  --  ZielAn
        //  3  der Ternaer-Doppelpunkt  --  ReadWord
        //  2  die op-Asgn-Kette  --  IsOpAssign
        //  0  der Alias-Name  --  ReadAliasName
        // ```
        AssertTrue(fehler.Count == 0,
            "**and no VX script fails any more** -- and it were"
                + " thirteen, and the one-line `for` form took it to"
                + " eight, the assignment lookahead to four, the ternary"
                + " colon to three, the op-assignment chain to two, and"
                + " the alias name to none, -- and every one of those"
                + " steps is a separate parser or lexer place with a"
                + " test of its own");
    }

    /// <summary>And the offset out of the message, if it carries one.</summary>
    /// <param name="pMeldung">The message to read.</param>
    /// <returns>The offset, or -1.</returns>
    private static int Fehlerstelle(string pMeldung)
    {
        const string kWort = "offset ";
        var i = pMeldung.IndexOf(kWort, StringComparison.Ordinal);
        if (i < 0)
        {
            return -1;
        }

        i += kWort.Length;
        var anfang = i;
        while (i < pMeldung.Length && char.IsDigit(pMeldung[i]))
        {
            i++;
        }

        return i > anfang && int.TryParse(pMeldung[anfang..i], out var wert)
            ? wert
            : -1;
    }
}
