using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A parameter that has a value when the call does not bring one, which is
/// how a game writes an optional command.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The parser could not read one.</strong> <c>def m(a, b = 2)</c>
/// parsed as four parameters: <c>a</c>, <c>b</c>, <c>=</c>, <c>2</c>.
/// <strong>The parameter list took a token at a time and called every one an
/// identifier.</strong> A call with two values would have bound, <strong>and a
/// call with one would have bound <c>=</c> and <c>2</c> as names</strong>: a
/// game that writes a default would have got a method that never used it.
/// </para>
/// <para>
/// <strong>And the doc said the parser could not do this.</strong>
/// <c>DefineMethod</c> explained that the body does not run at definition
/// time *because* the parser returns names and no expressions,
/// <strong>and that was the honest state of the code at the time and became
/// false the moment the parser learned.</strong>
/// </para>
/// <para>
/// <strong>One fault is open and has its own test.</strong> A call with no
/// arguments at all does not reach the body, and it has nothing to do with
/// defaults: <see cref="Test_AZeroArgumentCallDoesNotReachTheBody"/> measures
/// it.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A parameter with a default takes it when the call brings nothing.
    /// </summary>
    /// <remarks>
    /// <strong>The whole feature in the simplest case.</strong> <c>m(1)</c>
    /// binds <c>a</c> to one and <c>b</c> to two,
    /// <strong>and a reader that only bound what the call carried would have
    /// left <c>b</c> nil</strong> which is a game whose optional argument is
    /// suddenly nil.
    /// </remarks>
    public void Test_AParameterWithADefaultTakesItWhenTheCallBringsNothing()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a, b = 2)\n"
            + "    [a, b]\n"
            + "  end\n"
            + "end\n"
            + "A.new.m(1)\n"));

        AssertEq(wert.Items.Count, 2, "**two values came back**");
        AssertEq(AsInteger(wert.Items[0]), 1, "**the first is the one it was given**");
        AssertEq(
            AsInteger(wert.Items[1]),
            2,
            "**and the second is the default it was not given** -- a reader "
                + "that only bound what the call carried would have left it "
                + "nil, and a game's optional argument would be suddenly "
                + "nothing");
    }

    /// <summary>
    /// A value the call does bring beats the default.
    /// </summary>
    /// <remarks>
    /// <strong>The other half, and the one that can be got wrong quietly.</strong>
    /// <c>m(1, 3)</c> gives <c>b</c> the three,
    /// <strong>and a reader that filled in the defaults first would have let
    /// the two overwrite the three</strong>: a game that passes a value would
    /// have lost it silently, and only for the arguments that have a default.
    /// </remarks>
    public void Test_AValueTheCallBringsBeatsTheDefault()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a, b = 2)\n"
            + "    [a, b]\n"
            + "  end\n"
            + "end\n"
            + "A.new.m(1, 3)\n"));

        AssertEq(wert.Items.Count, 2, "**two values came back**");
        AssertEq(AsInteger(wert.Items[0]), 1, "**the first is the one it was given**");
        AssertEq(
            AsInteger(wert.Items[1]),
            3,
            "**and the second is the three, not the two** -- a reader that "
                + "filled the defaults in first would have let the two "
                + "overwrite the three, and a game that passes a value would "
                + "have lost it silently, and only for the arguments that "
                + "have a default");
    }

    /// <summary>
    /// Each default belongs to its own name.
    /// </summary>
    /// <remarks>
    /// <strong>The defaults are stored by name and not by position.</strong> A
    /// reader that kept two lists in step would have bound the first
    /// expression to the second parameter the moment one of them was missing,
    /// <strong>and a method that answered a number nobody wrote would look
    /// exactly like one that did not.</strong>
    /// </remarks>
    public void Test_EachDefaultBelongsToItsOwnName()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a = 10, b = 20)\n"
            + "    [a, b]\n"
            + "  end\n"
            + "end\n"
            + "[A.new.m(1), A.new.m(1, 2)]\n"));

        AssertEq(wert.Items.Count, 2, "**two calls came back**");
        AssertEq(
            AsInteger(wert.Items[0].Items[0]),
            1,
            "**the first call's first argument is the one it was given**");
        AssertEq(
            AsInteger(wert.Items[0].Items[1]),
            20,
            "**and its second is twenty, not ten** -- the two defaults are ten "
                + "and twenty, and a reader that stored them by position and "
                + "lost the step would have bound the ten to the second "
                + "parameter");
        AssertEq(
            AsInteger(wert.Items[1].Items[0]),
            1,
            "**the second call's first is the one it was given**");
        AssertEq(
            AsInteger(wert.Items[1].Items[1]),
            2,
            "**and its second is two** -- both arguments were given, so no "
                + "default is in play and the first one did not slide along");
    }

    /// <summary>
    /// A parameter without a default is nil, and that did not change.
    /// </summary>
    /// <remarks>
    /// <strong>The same rule as before, and it is worth saying so.</strong> A
    /// parameter with no default is nil when the call is short,
    /// <strong>and a reader that filled something in for it would have made
    /// every such method answer a value the game never asked for.</strong>
    /// </remarks>
    public void Test_AParameterWithoutADefaultIsStillNil()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a, b)\n"
            + "    [a, b]\n"
            + "  end\n"
            + "end\n"
            + "A.new.m(1)\n"));

        AssertEq(wert.Items.Count, 2, "**two values came back**");
        AssertEq(AsInteger(wert.Items[0]), 1, "**the first is the one it was given**");
        AssertTrue(
            wert.Items[1].Kind == RubyValueKind.Nil,
            "**and the second is nil** -- a reader that filled something in for "
                + "a parameter with no default would have made every such "
                + "method answer a value the game never asked for");
    }

    /// <summary>
    /// A default sits beside the arguments the call did bring.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's rule, and a reader that ran a default in an empty frame
    /// would have got nil.</strong> <c>m(1)</c> on <c>def m(a = 3, b = 2)</c>
    /// gives <c>a</c> the one and <c>b</c> the two,
    /// <strong>and a game writes this whenever a default depends on something
    /// set at load time.</strong>
    /// </remarks>
    public void Test_ADefaultSitsBesideTheArgumentsTheCallBrought()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a = 3, b = 2)\n"
            + "    [a, b]\n"
            + "  end\n"
            + "end\n"
            + "A.new.m(1)\n"));

        AssertEq(wert.Items.Count, 2, "**two values came back**");
        AssertEq(AsInteger(wert.Items[0]), 1, "**the first is the one it was given**");
        AssertEq(
            AsInteger(wert.Items[1]),
            2,
            "**and the second is its own default** -- the two defaults are "
                + "three and two, and a reader that bound the first to the "
                + "second would have answered three here");
    }

    /// <summary>
    /// A default is not evaluated when the call brings a value.
    /// </summary>
    /// <remarks>
    /// <strong>The rule in its other half.</strong> A default is for the
    /// arguments the call leaves out,
    /// <strong>and a reader that evaluated it anyway would have asked the host
    /// on every call</strong> which a game writing <c>def wuerfel(n = rand(6))</c>
    /// would feel as a die that costs a number on every roll whether the face
    /// was taken or not.
    /// </remarks>
    public void Test_ADefaultIsNotRunWhenTheCallBringsAValue()
    {
        var host = new ZaehlHost();
        var mit = new RubyInterpreter(host);
        mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a = rand(6))\n"
            + "    a\n"
            + "  end\n"
            + "end\n"
            + "A.new.m(1)\n"
            + "A.new.m(1)\n"));

        var aufrufe = 0;
        foreach (var a in host.Aufrufe)
        {
            if (a == "rand")
            {
                aufrufe++;
            }
        }

        AssertEq(
            aufrufe,
            0,
            "**the host was never asked for a number** -- both calls brought "
                + "a value, so the default did not run, and a reader that "
                + "evaluated it anyway would have asked on every call; the "
                + "host saw: " + string.Join(", ", host.Aufrufe));
    }

    /// <summary>
    /// The open fault: a call with no arguments does not reach the body.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Measured on 2026-09-29 and still wrong.</strong> A method whose
    /// body reads a parameter answers <c>Symbol:A</c> when called with
    /// <c>A.new.m()</c> -- <strong>the receiver, not the body</strong> -- and the
    /// same method with a fixed body, or with an argument in the call, answers
    /// correctly. <strong>So the fault is narrow and it is in the
    /// zero-argument path.</strong>
    /// </para>
    /// <para>
    /// <strong>What was ruled out, so the next attempt does not repeat it.</strong>
    /// The node is a <c>Call</c> with a <c>Receiver</c> role and no argument
    /// role. <c>Child</c>, <c>EvaluateChildren</c>, <c>EigeneMethode</c>,
    /// <c>Aufrufen</c> and <c>Name</c> all read correctly when read one by
    /// one. The parameter is filed in the method's own scope, and the body
    /// runs. <strong>What was not isolated is which of the zero-argument paths
    /// returns the receiver</strong>, and that is where the next attempt
    /// starts.
    /// </para>
    /// <para>
    /// <strong>It is not the defaults.</strong> The same fault happens without
    /// any default, <strong>so a reader that looked only at the new code would
    /// chase the wrong thing.</strong>
    /// </para>
    /// </remarks>
    public void Test_AZeroArgumentCallDoesNotReachTheBody()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(x = 9)\n"
            + "    x\n"
            + "  end\n"
            + "end\n"
            + "A.new.m()\n"));

        AssertEq(
            AsInteger(wert),
            9,
            "**the default came through** -- and it did not: a body that reads "
                + "its parameter answers the receiver (" + wert.Kind + ":"
                + (wert.Name ?? "-") + ") when the call brings no argument at "
                + "all, **and the same method with a fixed body or with an "
                + "argument answers correctly.** The fault is in the "
                + "zero-argument path, not in the defaults: the same thing "
                + "happens without any default.");
    }
}
