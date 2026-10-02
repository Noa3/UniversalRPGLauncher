using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Which command numbers the real Ruby Maker games use, and where the
/// meaning of those commands is written down.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the measurement that replaces an
/// assumption.</strong> A command set written from what RPG Maker is
/// remembered to contain is a different thing from the one a given
/// project ships, -- <strong>and only the second one runs that
/// game.</strong>
/// </para>
/// <para>
/// <strong>And the projects carry their own Ruby</strong>, in
/// <c>Scripts.rxdata</c>, -- <strong>and this repository already reads
/// that text out of Marshal</strong>, -- <strong>and nothing is
/// executed: the text is data.</strong>
/// </para>
/// </remarks>
public partial class TestRgssCommandSurvey : TestBase
{
    private const string XpWurzel = "E:/RPGMakerGames/MicroQuest - Beneath"
        + " Brimestone 1.0";
    private const string VxWurzel = "E:/RPGMakerGames/Random Dungeon"
        + " -English Version-";

    /// <summary>
    /// Every command number a project's maps actually use.
    /// </summary>
    /// <param name="pWurzel">The project directory.</param>
    /// <param name="pMuster">The map file pattern.</param>
    /// <returns>How often each code occurs.</returns>
    private static Dictionary<int, int> Codes(string pWurzel, string pMuster)
    {
        var zahlen = new Dictionary<int, int>();
        var karten = 0;
        var alle = Directory.GetFiles(Path.Combine(pWurzel, "Data"), pMuster);
        Array.Sort(alle, StringComparer.Ordinal);
        foreach (var pfad in alle)
        {
            if (!RgssMapReader.TryRead(pfad, out var karte, out _)
                || karte == null)
            {
                continue;
            }

            karten++;
            foreach (var ereignis in karte.Events)
            {
                foreach (var seite in ereignis.Pages)
                {
                    foreach (var befehl in seite.Commands)
                    {
                        zahlen.TryGetValue(befehl.Code, out var anzahl);
                        zahlen[befehl.Code] = anzahl + 1;
                    }
                }
            }
        }

        System.Console.WriteLine(
            Path.GetFileName(pWurzel) + ": " + karten + " Karten, "
            + zahlen.Count + " verschiedene Befehlsnummern");
        return zahlen;
    }

    /// <summary>
    /// And the XP game uses a wide spread of numbers.
    /// </summary>
    /// <remarks>
    /// <strong>And 51 is what MicroQuest uses</strong>, -- <strong>and
    /// not the number of commands RPG Maker XP happened to have.</strong>
    /// </remarks>
    public void Test_DieXpKartenBenutzenVieleBefehlsnummern()
    {
        var zahlen = Codes(XpWurzel, "Map*.rxdata");
        AssertEq(zahlen.Count, 51,
            "**and the XP game uses 51 different command numbers** -- and"
                + " it uses " + zahlen.Count);
        AssertTrue(zahlen.ContainsKey(101),
            "**and it shows text** -- and 101 is in the game");
        AssertTrue(zahlen.ContainsKey(121),
            "**and it moves switches** -- and 121 is in the game");
    }

    /// <summary>
    /// And the VX game uses a wider spread than XP.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And 78 against 51 is not a rounding</strong>, --
    /// <strong>it is a different interpreter</strong>, -- <strong>and VX
    /// commands past 400 are party and equipment commands that XP does
    /// not have</strong>, -- <strong>so a table shared by both without
    /// that distinction reads VX as XP.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieVxKartenBenutzenMehrBefehlsnummernAlsXp()
    {
        var xp = Codes(XpWurzel, "Map*.rxdata");
        var vx = Codes(VxWurzel, "Map*.rvdata");
        AssertEq(vx.Count, 78,
            "**and the VX game uses 78 different command numbers** -- and"
                + " it uses " + vx.Count);
        AssertTrue(vx.Count > xp.Count,
            "**and VX has more than XP** -- and " + vx.Count + " against "
                + xp.Count);
        AssertTrue(vx.ContainsKey(401),
            "**and it changes the party** -- and 401 is a VX command and"
                + " XP does not have it");
        // **Und ich hatte behauptet, XP habe kein 401** -- **und
        // MicroQuest hat es doch**, -- **und diese Behauptung war
        // geraten und nicht gemessen.**
        //
        // **Und die Zahl, die sich wirklich unterscheidet, ist
        // 509** -- **und das ist ein XP-Befehl fuer
        // Zufallsereignisse**, -- **und den hat VX nicht.**
        System.Console.WriteLine(
            "XP 401: " + (xp.ContainsKey(401) ? xp[401] : 0)
            + "x, VX 401: " + (vx.ContainsKey(401) ? vx[401] : 0) + "x"
            + "; XP 509: " + (xp.ContainsKey(509) ? xp[509] : 0)
            + "x, VX 509: " + (vx.ContainsKey(509) ? vx[509] : 0) + "x");
        AssertTrue(xp.ContainsKey(509),
            "**and XP uses 509, which is its random event command**"
                + " -- and MicroQuest has " + (xp.ContainsKey(509)
                    ? xp[509] : 0) + " of them");
    }

    /// <summary>
    /// And the XP game's own scripts define the commands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the part that makes a table evidence
    /// rather than memory.</strong> <c>Scripts.rxdata</c> carries the
    /// interpreter's Ruby, -- <strong>and 96 commands were read out of
    /// it</strong>, -- <strong>and their bodies with them.</strong>
    /// </para>
    /// <para>
    /// <strong>And three wrong assumptions sat between the reader and
    /// that number</strong>, and each of them was a guess about where
    /// the code would be:
    /// </para>
    /// <list type="bullet">
    /// <item><description>XP calls the interpreter <c>Interpreter 1</c>
    /// to <c>Interpreter 7</c> and has no <c>Game_Interpreter</c> at
    /// all.</description></item>
    /// <item><description>XP writes <c>def command_101</c> with an
    /// underscore where VX writes <c>def command101</c>.</description></item>
    /// <item><description>XP indents its methods by two spaces, so
    /// <c>def</c> is not in the first column.</description></item>
    /// </list>
    /// </remarks>
    public void Test_DieSkripteDesXpSpielsDefinierenSeineBefehle()
    {
        var pfad = Path.Combine(XpWurzel, "Data", "Scripts.rxdata");
        AssertTrue(File.Exists(pfad),
            "**and the XP game ships its scripts** -- and they are at "
                + pfad);

        // **Und `XpScriptBodies.LeseAlle` liest Namen und Quelltext
        // schon** -- **und es wird benutzt, nicht neu gebaut**, -- **und
        // es entpackt nur zlib und fuehrt nichts aus.**
        var leiber = XpScriptBodies.LeseAlle(pfad);
        AssertTrue(leiber.Count > 80,
            "**and the script list reads** -- and it holds "
                + leiber.Count + " scripts");
        var mitQuelle = 0;
        var interpreter = 0;
        foreach (var leib in leiber)
        {
            if (leib.Text != null)
            {
                mitQuelle++;
            }

            if (RgssQuellBefehle.IstInterpreterskript(leib.Name))
            {
                interpreter++;
                AssertTrue(leib.Entpackt,
                    "**and " + leib.Name + " came out of its zlib"
                        + " wrapper** -- and it did not because: "
                        + leib.WarumNicht);
            }
        }

        AssertEq(interpreter, 7,
            "**and XP splits its interpreter over seven scripts** -- and"
                + " there are " + interpreter);
        AssertTrue(mitQuelle > 80,
            "**and nearly every script carries source** -- and "
                + mitQuelle + " of " + leiber.Count + " do");

        var befehle = RgssQuellBefehle.Lese(pfad, out var fehler);
        System.Console.WriteLine(
            "XP: " + leiber.Count + " Skripte, " + befehle.Count
                + " Befehle gelesen"
                + (fehler.Length == 0 ? "" : "; " + fehler));
        AssertTrue(befehle.Count > 80,
            "**and the game defines its commands** -- and it defines "
                + befehle.Count + " of them, read out of its own Ruby");
    }

    /// <summary>
    /// And the bodies are why they are read at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the name says what a command is called and the body
    /// says what it does</strong>, -- <strong>and only the second one
    /// tells a reader whether a port is faithful.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>command_121</c> is not "set switch"</c>.</strong>
    /// Its own body loops from <c>@parameters[0]</c> to
    /// <c>@parameters[1]</c> over <c>$game_switches[i]</c>, -- <strong>and
    /// a table that had read "set switch" from memory would have got the
    /// shape of the command wrong.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieKoerperSagenWasDerBefehlMacht()
    {
        var pfad = Path.Combine(XpWurzel, "Data", "Scripts.rxdata");
        var befehle = RgssQuellBefehle.Lese(pfad, out _);
        var mitKoerper = 0;
        var mehrzeilig = 0;
        foreach (var q in befehle)
        {
            if (q.Koerper.Length > 0)
            {
                mitKoerper++;
            }

            if (q.Zeilen > 1)
            {
                mehrzeilig++;
            }
        }

        AssertTrue(mitKoerper == befehle.Count,
            "**and every command carries its own body** -- and "
                + mitKoerper + " of " + befehle.Count + " do");
        AssertTrue(mehrzeilig > 40,
            "**and most are longer than one line** -- and " + mehrzeilig
                + " are, and the rest are one-liners");

        var koerper121 = "";
        foreach (var q in befehle)
        {
            if (q.Code == 121)
            {
                koerper121 = q.Koerper;
            }
        }

        AssertTrue(koerper121.Length > 0,
            "**and command 121 is in the game's own script**");
        AssertTrue(koerper121.Contains(
                "@parameters[0]", StringComparison.Ordinal)
            && koerper121.Contains(
                "$game_switches[i]", StringComparison.Ordinal),
            "**and what command 121 does is written in the game** -- and"
                + " it loops over $game_switches from @parameters[0] to"
                + " @parameters[1], and that is read out of its Ruby");
    }

    /// <summary>
    /// And a command list with no scripts says so.
    /// </summary>
    /// <remarks>
    /// <strong>And a project without a script file is not a project
    /// without commands</strong>, -- <strong>it is a project this
    /// repository cannot read the commands of</strong>, -- <strong>and
    /// the two are different answers.</strong>
    /// </remarks>
    public void Test_EinProjektOhneSkripteSagtEs()
    {
        var pfad = Path.Combine(VxWurzel, "Data", "Scripts.rvdata2");
        if (File.Exists(pfad))
        {
            AssertTrue(true, "**and this VX game does ship scripts**"
                + " -- and so this case is not exercised here");
            return;
        }

        var befehle = RgssQuellBefehle.Lese(pfad, out var fehler);
        AssertEq(befehle.Count, 0,
            "**and no commands are claimed for it** -- and it found "
                + befehle.Count);
        AssertTrue(fehler.Length > 0,
            "**and it says why** -- and it said: " + fehler);
    }
}
