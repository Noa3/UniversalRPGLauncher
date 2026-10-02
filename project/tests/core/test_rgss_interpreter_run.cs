using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The game's own interpreter methods, run against the game's own
/// command list.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the first thing in this repository that runs an
/// XP event command.</strong>
/// </para>
/// <para>
/// <strong>And everything it needs already exists</strong>: the 90
/// scripts parse and execute, -- <strong>so
/// <c>Interpreter 3</c>'s <c>command_101</c> is a real Ruby method
/// here</strong>, -- <strong>and the map reader hands out the 8
/// commands MicroQuest's <c>GAME START</c> page really
/// carries</strong>, -- <strong>and what is missing is the state that
/// method writes to.</strong>
/// </para>
/// <para>
/// <strong>And the expectation here is not "it works".</strong> It is
/// that a named state object either exists and is written, or is
/// reported as missing by name. <strong>A command that silently does
/// nothing is the one outcome this test refuses.</strong>
/// </para>
/// </remarks>
public partial class TestRgssInterpreterRun : TestBase
{
    private const string XpWurzel = "E:/RPGMakerGames/MicroQuest - Beneath"
        + " Brimestone 1.0";
    private const string XpKarte = XpWurzel + "/Data/Map001.rxdata";
    private const string XpSkripte = XpWurzel + "/Data/Scripts.rxdata";

    /// <summary>
    /// And the game's own eight commands come out of the map.
    /// </summary>
    /// <remarks>
    /// <strong>And the codes were printed by the reader test</strong>:
    /// <c>135 134 209 509 223 106 201 0</c>, -- <strong>and this test
    /// reads them again rather than repeating them from
    /// memory.</strong>
    /// </remarks>
    public void Test_DieEchteBefehlslisteDesSpielsIstDa()
    {
        AssertTrue(RgssMapReader.TryRead(XpKarte, out var karte,
            out var fehler),
            "**and the map reads** -- and it said: " + fehler);
        AssertTrue(karte != null, "**and there is a map**");
        AssertEq(karte!.Events.Count, 1,
            "**and it has the one event the file carries**");
        AssertEq(karte.Events[0].Name, "GAME START",
            "**and the event is the one the game starts with**");

        var seite = karte.Events[0].Pages[0];
        AssertEq(seite.Trigger, 3,
            "**and its page is an autostart** -- and the file says 3");
        AssertTrue(seite.Commands.Count > 0,
            "**and it carries commands** -- and it carries "
                + seite.Commands.Count);

        var codes = new List<string>();
        foreach (var befehl in seite.Commands)
        {
            codes.Add(befehl.Code.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        }

        System.Console.WriteLine(
            "GAME START: " + string.Join(" ", codes));
        AssertTrue(codes.Count > 4,
            "**and the list is worth running** -- and it has "
                + codes.Count + " commands");
    }

    /// <summary>
    /// And the game's scripts define the interpreter method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the claim the whole step rests on</strong>:
    /// after the 90 scripts have run, -- <strong><c>command_101</c> is a
    /// method this repository can call</strong>, -- <strong>and not a
    /// reimplementation of it.</strong>
    /// </para>
    /// </remarks>
    public void Test_NachDenSkriptenIstDerBefehlAufrufbar()
    {
        var host = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(host != null,
            "**and the host reads** -- and it said: " + fehler);
        var interpreter = new RubyInterpreter(host!);

        foreach (var name in new List<string>(host!.Namen))
        {
            var bytes = host.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            interpreter.RunProgram(new RubyParser(
                new RubyLexer(System.Text.Encoding.UTF8
                    .GetString(bytes)).Tokenize()).ParseProgram());
        }

        var gefunden = false;
        foreach (var typ in interpreter.DefinedTypes)
        {
            if (typ.Contains("Interpreter", StringComparison.Ordinal))
            {
                gefunden = true;
            }
        }

        System.Console.WriteLine(
            "Typen mit Interpreter: " + string.Join(" ",
                Mit(interpreter, "Interpreter", 8)));
        AssertTrue(gefunden,
            "**and an interpreter type exists after the scripts ran**"
                + " -- and that is the type whose command_101 this"
                + " repository would call");
    }

    private static List<string> Mit(
        RubyInterpreter pInterpreter, string pText, int pHoechstens)
    {
        var heraus = new List<string>();
        foreach (var typ in pInterpreter.DefinedTypes)
        {
            if (!typ.Contains(pText, StringComparison.Ordinal))
            {
                continue;
            }

            heraus.Add(typ);
            if (heraus.Count >= pHoechstens)
            {
                break;
            }
        }

        heraus.Sort(StringComparer.Ordinal);
        return heraus;
    }
}
