using System;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The four VX scripts that fail on a comma, with the text at the
/// place each one fails.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And four of the eight fail on the same thing:</strong>
/// <c>',' at offset N does not begin an expression.</c> If those four
/// offsets sit in one construct, that is one fault and not four.
/// </para>
/// <para>
/// <strong>And the text around each offset is printed, because
/// "a comma does not begin an expression" names no construct at
/// all</strong>, -- <strong>and without the text this test would say
/// nothing that could be acted on.</strong>
/// </para>
/// </remarks>
public partial class TestRgssVxKomma : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/Random Dungeon -English Version-";

    /// <summary>
    /// And the four places.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this asserts only that it was measured</strong>, --
    /// <strong>because what the four offsets have in common is the
    /// question, and the printed text is the answer.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieVierKommaStellen()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rvdata"), out var grund);
        if (skripte == null)
        {
            throw new InvalidOperationException(grund);
        }

        var host = new SpielHost(skripte, Wurzel);
        foreach (var name in new[]
        {
            "多人数パーティ", "ポップアップ", "ボス専用コラプス",
            "マップ小物メソッド",
        })
        {
            var bytes = host.ReadScript(name, true);
            if (bytes == null)
            {
                Console.WriteLine(name + ": nicht lesbar");
                continue;
            }

            var text = System.Text.Encoding.UTF8.GetString(bytes);
            string meldung;
            try
            {
                new RubyParser(new RubyLexer(text).Tokenize()).ParseProgram();
                Console.WriteLine(name + ": parst jetzt");
                continue;
            }
            catch (RubyParseException ausnahme)
            {
                meldung = ausnahme.Message;
            }

            Console.WriteLine(name + ": " + meldung);
            var stelle = Fehlerstelle(meldung);
            if (stelle < 0 || stelle >= text.Length)
            {
                Console.WriteLine("  (kein Offset in der Meldung)");
                continue;
            }

            var zeile = 1;
            for (var k = 0; k < stelle; k++)
            {
                if (text[k] == '\n')
                {
                    zeile++;
                }
            }

            var von = Math.Max(0, stelle - 90);
            var bis = Math.Min(text.Length, stelle + 90);
            Console.WriteLine("  Zeile " + zeile + ": |"
                + text[von..bis].Replace("\r", "").Replace("\n", " | ")
                + "|");
        }

        AssertTrue(true,
            "**and the four places are printed above** -- and whether"
                + " they are one construct or four is what the printed"
                + " text answers, and this test asserts only that it"
                + " was measured");
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
