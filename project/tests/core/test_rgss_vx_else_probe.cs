using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The parser failure in VX's own <c>Game_Interpreter</c>, shown as the
/// offending source lines.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is measured: <c>'else' at offset 5721 does not begin
/// an expression.</strong> -- <strong>and the interpreter of a game with
/// 657 maps cannot run without this one script.</strong>
/// </para>
/// </remarks>
public partial class TestRgssVxElseProbe : TestBase
{
    private const string Skripte =
        "E:/RPGMakerGames/Random Dungeon -English Version-/Data"
        + "/Scripts.rvdata";

    /// <summary>
    /// And the source around the offset, printed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this prints and asserts nothing about the cause</strong>,
    /// -- <strong>because a cause is a conclusion and a conclusion needs
    /// the lines first.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieZeilenUmDenFehlerAusgeben()
    {
        foreach (var leib in XpScriptBodies.LeseAlle(Skripte))
        {
            if (leib.Name != "Game_Interpreter" || leib.Text == null)
            {
                continue;
            }

            const int Fehler = 5721;
            var zeilen = leib.Text.Split('\n');
            var zeichen = 0;
            var fehlerZeile = 0;
            for (var i = 0; i < zeilen.Length; i++)
            {
                if (zeichen + zeilen[i].Length >= Fehler)
                {
                    fehlerZeile = i;
                    break;
                }

                zeichen += zeilen[i].Length + 1;
            }

            System.Console.WriteLine(
                "Game_Interpreter hat " + zeilen.Length + " Zeilen,"
                + " der Fehler ist in Zeile " + (fehlerZeile + 1));
            var von = Math.Max(0, fehlerZeile - 4);
            var bis = Math.Min(zeilen.Length, fehlerZeile + 3);
            for (var i = von; i < bis; i++)
            {
                System.Console.WriteLine(
                    "  " + (i + 1) + ": " + zeilen[i].Trim());
            }

            AssertTrue(fehlerZeile > 0,
                "**and the offending offset lies inside the script**"
                    + " -- and it is at line " + (fehlerZeile + 1)
                    + " of " + zeilen.Length + ", and the lines above"
                    + " are printed, and this test does not say what"
                    + " is wrong with them because that is the next"
                    + " measurement");
        }
    }
}
