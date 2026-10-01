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
                if (program.Count == 0)
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
        AssertTrue(knoten > 3000,
            "**and they come out as a program, and not as a shrug** -- and the "
                + $"{gelesen} files hold {knoten} top-level statements");
    }
}
