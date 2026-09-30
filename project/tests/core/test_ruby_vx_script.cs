using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The first script of an RPG Maker VX project runs here, and not only in
/// pieces.
/// </summary>
public partial class TestRubyInterpreter
{
    private const string VxRoot = "res://tests/fixtures/ruby";

    /// <summary>
    /// A whole VX script parses and runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And every other test in this suite runs a sentence.</strong>
    /// Measured before this: 340 <c>new RubyInterpreter(...)</c> across 36
    /// files, **and not one of them reads a script file**, **and there was
    /// no fixture of a real game script anywhere in the tree.**
    /// </para>
    /// <para>
    /// <strong>And a sentence can hide what a file does not.</strong> I
    /// measured a VX script line by line first, **and every line threw** —
    /// <c>'end' was expected, but the script ends first</c> —
    /// <strong>because a line is not a file</strong>, <strong>and a line
    /// has no <c>end</c> to close the <c>def</c> it starts.</strong>
    /// <strong>The same text as a file parses.</strong>
    /// </para>
    /// <para>
    /// <strong>And what is in the file is what a game writes.</strong> A
    /// class that inherits <c>Window</c>, which the reader has never heard
    /// of; a constant; two conditionals, one of them with an <c>else</c>;
    /// a global; a method that assigns to the reader; **and the
    /// inheritance from a name that does not exist must not stop it**,
    /// because <c>Window</c> is the RTP's and **the RTP is a file this
    /// reader does not have and must not pretend to have.**
    /// </para>
    /// </remarks>
    public void Test_AWholeVxScriptParsesAndRuns()
    {
        var pfad = System.IO.Path.Combine(
            System.IO.Directory.GetCurrentDirectory(),
            "tests/fixtures/ruby/vx_window_base.rb");
        var m = new RubyInterpreter(new RubyNullHost());
        List<RubyNode> knoten;
        RubyValue wert;
        try
        {
            knoten = Statements(
                System.Text.Encoding.GetEncoding(932)
                    .GetString(System.IO.File.ReadAllBytes(pfad)));
            wert = m.RunProgram(knoten);
        }
        catch (System.Exception e)
        {
            AssertTrue(false, "the first script of a VX project reads: "
                + e.Message);
            return;
        }

        AssertEq(knoten.Count, 1,
            "**one class, and one node** -- and a reader that made a node "
                + "per line would not be able to close the `def` inside "
                + "the `class`");

        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));

        AssertTrue(m.FindMethod("Window_Base", "initialize") != null,
            "**and `initialize` is a method of the class**");
        AssertTrue(m.FindMethod("Window_Base", "refresh") != null,
            "**and `refresh` is a method of the class** -- and it is the "
                + "second method in the file, after a `def` that was closed "
                + "by its own `end` inside a `class` that was closed by "
                + "another");
    }
}
