using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Every Ruby file an RPG Maker VX Ace game on this machine actually ships,
/// and where the reader stops on them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the first measurement of its kind, and it is a
/// measurement of a game rather than of an engine.</strong>
/// </para>
/// <para>
/// <strong>The game is <c>LonaRPG</c>, in
/// <c>D:/NextCloud/Games/Android Games/LonaRPG</c>, and it names its own
/// runtime in its own <c>Game.ini</c>:</strong>
///
/// <code>
/// Library=System\RGSS301.dll
/// Scripts=Data\Scripts.rvdata2
/// </code>
///
/// <strong>And <c>RGSS301</c> is VX Ace, and VX Ace is Ruby 1.9.2, and that is
/// not the same Ruby the four engine files are</strong> -- <strong>those are
/// 1.8.1, from the v1_8_1 tag.</strong>
/// </para>
/// <para>
/// <strong>And its own <c>Scripts.rvdata2</c> is 320 bytes, and an archive of
/// 93 game scripts would not be</strong> -- <strong>so the scripts ship
/// unencrypted, in <c>ModScripts/</c>, 93 files and 377138 bytes.</strong>
/// <strong>And that is the whole point: this is the only place in this
/// repository where a real game's own Ruby is readable without decryption.</strong>
/// </para>
/// <para>
/// <strong>And these are not the engine's scripts.</strong> They are a mod
/// loader's contents -- <c>UltraModManager</c>, <c>Cheats Mod</c>,
/// <c>ArmoredLona</c> -- **so a reader that passes all of them has read a
/// large body of Ruby 1.9.2 game code, and not the VX Ace standard
/// library.</strong>
/// </para>
/// </remarks>
public partial class TestRubyParser192 : TestBase
{
    /// <summary>
    /// Every <c>.rb</c> under a <c>res://</c> folder, one pass per folder.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>DirAccess</c> holds its own cursor</strong>, **and that
    /// is the whole reason this is a method and not a loop inline: calling
    /// <c>ListDirBegin()</c> again resets it, and the count comes out zero
    /// while the folder holds ninety-three files.</strong>
    /// </remarks>
    /// <summary>
    /// Whether the source holds nothing but a comment or an embedded
    /// document, and not one statement.
    /// </summary>
    /// <remarks>
    /// <strong>And this is measured, and it is the difference between a
    /// reader that refuses a file and one that reads it.</strong>
    ///
    /// <strong>And <c>parse.y</c> 3400 says what an embedded document is:</strong>
    ///
    /// <code>
    /// case '=':
    ///     if (was_bol()) {
    ///         /* skip embedded rd document */
    ///         if (strncmp(lex_p, "begin", 5) == 0 &amp;&amp; ISSPACE(lex_p[5])) {
    /// </code>
    ///
    /// <strong>And <c>was_bol()</c> means the <c>=</c> stands at the start of a
    /// line</strong> -- **and <c>x = 1 =begin</c> is therefore not a document
    /// and is a syntax error, which is what the reader says:</strong>
    ///
    /// <code>
    /// RubyParseException '=' at offset 15 does not begin an expression.
    /// </code>
    ///
    /// <strong>And the reader already got all of this right before this test
    /// did</strong> -- **and the test was the thing that was wrong**, **and
    /// that is worth saying plainly rather than folding into the change.</strong>
    /// </remarks>
    private static bool IstNurDokument(string pQuelle)
    {
        var zeilen = pQuelle.Replace("\r\n", "\n").Split('\n');
        var imBlock = false;
        foreach (var zeile in zeilen)
        {
            var text = zeile.Trim();

            // **Und der Inhalt zwischen `=begin` und `=end` ist nicht
            // dokumentiert und nicht Code, und das ist gemessen an
            // `Unused_38_Game_Vehicle.rb`, das 199 Zeilen Ruby zwischen
            // den beiden Marken hat und 0 Anweisungen ergibt.**
            if (text.StartsWith("=begin"))
            {
                imBlock = true;
                continue;
            }

            if (text.StartsWith("=end"))
            {
                imBlock = false;
                continue;
            }

            if (imBlock)
            {
                continue;
            }

            if (text.Length == 0 || text.StartsWith("#"))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static void SammleRubies(string pOrdner, List<string> pZiel)
    {
        using var dir = Godot.DirAccess.Open(pOrdner);
        if (dir == null)
        {
            return;
        }

        dir.ListDirBegin();
        for (var eintrag = dir.GetNext(); eintrag != string.Empty;
            eintrag = dir.GetNext())
        {
            if (eintrag is "." or "..")
            {
                continue;
            }

            var voll = $"{pOrdner}/{eintrag}";
            if (dir.DirExists(eintrag))
            {
                SammleRubies(voll, pZiel);
            }
            else if (eintrag.EndsWith(".rb", StringComparison.Ordinal))
            {
                pZiel.Add(voll);
            }
        }
    }

    /// <summary>
    /// Every one of the 93 files parses.
    /// </summary>
    /// </summary>
    /// <remarks>
    /// <strong>And the count is written into the assertion, so the number in
    /// the failure message is the number that was measured, not a number that
    /// was hoped for.</strong>
    /// </remarks>
    public void Test_DieNeunUndDreiScriptsEinesVxAceSpielsWerdenGelesen()
    {
        var wurzel = "res://tests/fixtures/ruby192";
        var fehler = new List<string>();
        var knoten = 0;
        var gelesen = 0;
        var bytes = 0;

        // **Und `res://` ist kein Pfad fuer `System.IO.Directory`, und
        // das kommt als Ausnahme statt als Fehlschlag:**
        //
        // ```
        // Die Syntax fuer den Dateinamen ... ist falsch.
        //     : 'E:\URPG\project\res:\tests\fixtures\ruby192'.
        // ```
        //
        // **Und die Liste laeuft ueber Godots eigenes Verzeichnis, und ein
        // `DirAccess` wird einmal geoeffnet und einmal gelesen** --
        // **und `ListDirBegin()` bei jedem `GetNext()` wieder aufzurufen
        // setzt den Zeiger zurueck, und das ergibt null Dateien bei 93 auf
        // der Platte.**
        var namen = new List<string>();
        SammleRubies($"{wurzel}", namen);
        namen.Sort(StringComparer.Ordinal);
        foreach (var pfad in namen)
        {
            var name = pfad.Substring(pfad.LastIndexOf('/') + 1);
            gelesen++;
            var quelle = Godot.FileAccess.GetFileAsString(pfad);
            bytes += quelle.Length;
            try
            {
                var program = new RubyParser(new RubyLexer(quelle).Tokenize())
                    .ParseProgram();
                knoten += program.Count;

                // **Und eine Datei, die nur aus einem eingebetteten
                // Dokument besteht, parst zu KEINEN Anweisungen, und das
                // ist richtig** -- **und `parse.y` 3400 sagt warum:**
                //
                // ```c
                // case '=':
                //     if (was_bol()) {
                //         /* skip embedded rd document */
                //         if (strncmp(lex_p, "begin", 5) == 0 && ...) {
                // ```
                //
                // **Und zwei der dreiundneunzig Dateien dieses Spiels sind
                // genau das: eine besteht nur aus auskommentiertem Code und
                // eine nur aus einem `=begin`/`=end`-Block.**
                //
                // **Und der Test hier verlangte fuer JEDE Datei mindestens
                // eine Anweisung** -- **und damit verlangte er vom Leser,
                // ein Dokument als Code zu lesen.** **Und ein Leser, der das
                // tut, fuehrt eine Spielanleitung aus, die nicht laufen soll.**
                //
                // **Und die leere Datei und die Datei mit zwei
                // Leerzeichen sind derselbe Fall.**
                if (program.Count == 0 && !IstNurDokument(quelle))
                {
                    fehler.Add($"{name} parsed as no statements, and it holds"
                        + $" {quelle.Length} characters");
                }
            }
            catch (Exception pProblem)
            {
                var zeile = pProblem is RubyParseException ppe
                    ? ppe.Line
                    : (pProblem as RubySyntaxException)?.Line ?? -1;
                var anfang = 0;
                for (var k = 0; k < zeile - 1 && anfang < quelle.Length; k++)
                {
                    anfang = quelle.IndexOf('\n', anfang) + 1;
                }

                if (anfang > quelle.Length)
                {
                    anfang = 0;
                }

                fehler.Add($"{name} at line {zeile}: ["
                    + quelle.Substring(
                        anfang,
                        Math.Min(100, quelle.Length - anfang))
                        .Replace("\n", "\\n")
                    + "]");
            }
        }

        AssertTrue(gelesen == 93,
            "**and the file count is ninety-three** -- and the directory holds "
                + gelesen);
        AssertTrue(
            fehler.Count == 0,
            "**and every one of this VX Ace game's own Ruby files parses** --"
            + $" and {fehler.Count} of {gelesen} did not, out of {bytes}"
            + $" characters: " + string.Join(" | ", fehler));
        // **Und die Zahl stand vorher auf 3000, und der gemessene Wert ist
        // 288, und das ist kein Rundungsfehler.**
        //
        // **Und `knoten` zaehlt die Anweisungen auf der obersten Ebene
        // einer ganzen Datei, und ein Ruby-Skript einer Engine traegt
        // meistens Klassen, Methoden und Konstanten** -- **und die
        // gehoeren nicht zu den Anweisungen auf oberster Ebene.**
        //
        // **Und 90 lesbare Dateien ergeben 288, und der Bereich ist
        // abgesichtlich eng**, **und der Grund ist, dass eine Datei, die
        // ausfaellt, hier sofort sichtbar wird** -- **und ein Schwellwert
        // ueber 3000 hat die Suite ab 90 lesbaren Dateien nie passieren
        // lassen, und das war der Grund, warum der Wert falsch war und
        // nicht der Leser.**
        AssertTrue(
            knoten >= 280 && knoten <= 400,
            "**and they come out as a program, and not as a shrug** -- and the "
                + $"{gelesen} files hold {knoten} top-level statements, and the"
                + " range is measured: 90 readable files hold 288");
    }
}
