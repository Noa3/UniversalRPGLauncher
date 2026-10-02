using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Which VX scripts still fail, named one by one.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the fault above moved the number from 13 to 8.</strong>
/// <c>for a in b do c end</c> used to read its <c>do</c> as a block
/// opener, and the rest of the <c>for</c> as that block's body. --
/// <strong>And what is left is a list and not a number</strong>, --
/// <strong>because a number says nothing about which script to read
/// next.</strong>
/// </para>
/// <para>
/// <strong>And this reuses <c>TestRgssVxRun</c>'s own loading path
/// rather than building a second one</strong>, -- <strong>because two
/// loaders would measure two things.</strong>
/// </para>
/// </remarks>
public partial class TestRgssVxRest : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/Random Dungeon -English Version-";

    /// <summary>
    /// And the eight, with what each one says.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the names are printed, because the point of this
    /// test is to say which script to read next</strong>, --
    /// <strong>and the bound is there so that a later fix cannot make
    /// the list grow without saying so.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieRestlichenVxSkripte()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rvdata"), out var grund);
        if (skripte == null)
        {
            throw new InvalidOperationException(grund);
        }

        var host = new SpielHost(skripte, Wurzel);
        var interpreter = new RubyInterpreter(host);
        var fehler = new List<string>();

        foreach (var name in new List<string>(host.KnownMethods))
        {
            var bytes = host.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                interpreter.RunProgram(new RubyParser(
                    new RubyLexer(System.Text.Encoding.UTF8
                        .GetString(bytes)).Tokenize()).ParseProgram());
            }
            catch (RubyParseException ausnahme)
            {
                fehler.Add(name + " [Parse] " + ausnahme.Message);
            }
            catch (RubyRuntimeException ausnahme)
            {
                fehler.Add(name + " [Laufzeit] " + ausnahme.Message);
            }
        }

        foreach (var f in fehler)
        {
            Console.WriteLine("VX-FEHLER " + f);
        }

        Console.WriteLine("VX noch fehlgeschlagen: " + fehler.Count
            + " von " + host.KnownMethods.Count);

        AssertTrue(fehler.Count <= 8,
            "**and at most eight VX scripts still fail** -- and it"
                + " were thirteen before the one-line `for` form was"
                + " read correctly, -- and that is the measured"
                + " consequence of that fix, -- and the names are"
                + " printed above");
    }
}
