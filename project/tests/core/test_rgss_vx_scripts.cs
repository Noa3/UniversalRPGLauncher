using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Whether the VX game's own Ruby can be read at all.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the first measurement for criteria 5 and 6.</strong>
/// The map reader reads VX maps, -- <strong>and nothing so far has
/// read VX scripts</strong>, -- <strong>and a generation is not
/// supported until its own Ruby runs.</strong>
/// </para>
/// <para>
/// <strong>And the file name is <c>Scripts.rvdata</c> and not
/// <c>Scripts.rvdata2</c></strong>, -- <strong>and that is measured
/// from the directory, not assumed.</strong>
/// </para>
/// </remarks>
public partial class TestRgssVxScripts : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/Random Dungeon -English Version-";

    /// <summary>
    /// And the VX project's script file is where the measurement says.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>Scripts.rvdata2</c> does not exist</strong>, --
    /// <strong>and a reader that looked only for that name would
    /// report "no scripts" for a game that ships 338 KB of
    /// them.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieSkriptdateiDesVxSpielsExistiert()
    {
        var rvdata = Path.Combine(Wurzel, "Data", "Scripts.rvdata");
        var rvdata2 = Path.Combine(Wurzel, "Data", "Scripts.rvdata2");

        System.Console.WriteLine(
            "Scripts.rvdata: " + File.Exists(rvdata)
            + (File.Exists(rvdata)
                ? " (" + new FileInfo(rvdata).Length + " Bytes)"
                : "")
            + "; Scripts.rvdata2: " + File.Exists(rvdata2));

        AssertTrue(File.Exists(rvdata),
            "**and the VX game ships its scripts** -- and they are at "
                + rvdata);
        AssertFalse(File.Exists(rvdata2),
            "**and it does not ship a `Scripts.rvdata2`** -- and a"
                + " reader that looked only for that name would report"
                + " an empty project for a game with 338 KB of Ruby");
    }

    /// <summary>
    /// And the script list reads, and it is long.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this asks whether the existing
    /// <c>XpScriptBodies</c> reads a VX file</strong>, -- <strong>and
    /// it was written for XP</strong>, -- <strong>and whether the
    /// format is the same is a fact to measure and not a
    /// name to trust.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieSkriptlisteDesVxSpielsLiestSich()
    {
        var pfad = Path.Combine(Wurzel, "Data", "Scripts.rvdata");
        var leiber = XpScriptBodies.LeseAlle(pfad);

        System.Console.WriteLine(
            "Gelesen: " + leiber.Count + " Eintraege");
        var mitQuelle = 0;
        var namen = new List<string>();
        foreach (var leib in leiber)
        {
            if (leib.Text != null)
            {
                mitQuelle++;
            }

            if (namen.Count < 12)
            {
                namen.Add(leib.Name);
            }
        }

        System.Console.WriteLine(
            "  mit Quelle: " + mitQuelle + "; Namen: "
            + string.Join(" | ", namen.ToArray()));

        AssertTrue(leiber.Count > 100,
            "**and the list reads** -- and it holds " + leiber.Count
                + " entries, and the names are "
                + string.Join(" | ", namen.ToArray()));
        AssertTrue(mitQuelle > 50,
            "**and more than half of them carry Ruby source** -- and "
                + mitQuelle + " of " + leiber.Count
                + ", and that is the number this reader can run");
    }

    /// <summary>
    /// And the interpreter's name for it is `Game_Interpreter`.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the difference between XP and VX that was
    /// measured in step 9:</strong> XP writes <c>Interpreter 1</c> to
    /// <c>Interpreter 7</c>, -- <strong>and VX writes
    /// <c>Game_Interpreter</c></strong>, -- <strong>and a reader that
    /// only knew one of them finds nothing in half of all
    /// projects.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerInterpreterDesVxSpielsHeisstGameInterpreter()
    {
        var leiber = XpScriptBodies.LeseAlle(
            Path.Combine(Wurzel, "Data", "Scripts.rvdata"));
        var gefunden = 0;
        foreach (var leib in leiber)
        {
            if (leib.Name == "Game_Interpreter")
            {
                gefunden++;
            }
        }

        System.Console.WriteLine(
            "Game_Interpreter-Eintraege: " + gefunden);

        // **Und jetzt die Zahl, die Kriterium 5 ausmacht:**
        // **wie viele Befehle VX selbst definiert.**
        //
        // **Und der Befehlssatz steht in `Game_Interpreter`**, --
        // **und der wird gelesen wie bei XP**, --
        // **und diesmal ohne Unterstrich und ohne Einrueckung.**
        var befehle = RgssQuellBefehle.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rvdata"), out var f2);
        System.Console.WriteLine(
            "VX-Befehle gelesen: " + befehle.Count
            + (f2.Length == 0 ? "" : "; " + f2));
        var arten = RgssBefehlArten.Verteilung(befehle);
        var teile = new List<string>();
        foreach (var eintrag in arten)
        {
            teile.Add(eintrag.Key + "=" + eintrag.Value);
        }

        teile.Sort(StringComparer.Ordinal);
        System.Console.WriteLine("  Verteilung: "
            + string.Join(" ", teile.ToArray()));

        AssertTrue(gefunden == 1,
            "**and VX has exactly one `Game_Interpreter`** -- and this"
                + " game has " + gefunden + ", and XP has none at all"
                + " and writes `Interpreter 1` to `Interpreter 7`"
                + " instead");
    }
}
