using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `*rest` and `**opts`, which are how a game writes a method that takes any
/// number of values.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The parser could read them and the interpreter had never seen
/// one.</strong> <c>def m(a, *rest)</c> parses to a
/// <c>BlockPass</c> node named <c>rest</c> in the parameter list, and
/// <c>def m(**opts)</c> to a <c>Hash</c> node named <c>opts</c> —
/// <strong>and the interpreter treated both as an ordinary name</strong>, so
/// <c>rest</c> was a parameter that nothing could ever fill and a call with
/// extra values dropped them.
/// </para>
/// <para>
/// <strong>They are the shape every plugin dispatcher is written in.</strong>
/// A command that takes a variable number of values writes
/// <c>def befehl(*werte)</c>,
/// <strong>and without it a game plugin that forwards arguments has no
/// syntax.</strong>
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `*rest` collects the values the named parameters did not take.
    /// </summary>
    /// <remarks>
    /// <strong>The whole feature in the simplest case.</strong>
    /// <c>m(1, 2, 3)</c> on <c>def m(a, *rest)</c> gives <c>a</c> the one and
    /// <c>rest</c> a list of the other two,
    /// <strong>and a reader that treated <c>rest</c> as an ordinary name would
    /// have given it nil and dropped the two values</strong> — which is a game
    /// plugin whose command silently loses its arguments.
    /// </remarks>
    public void Test_TheSplatCollectsWhatTheNamedParametersDidNotTake()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a, *rest)\n"
            + "    [a, rest]\n"
            + "  end\n"
            + "end\n"
            + "A.m(1, 2, 3)\n"));

        AssertEq(wert.Items.Count, 2,
            "**two values came back**");
        AssertEq(AsInteger(wert.Items[0]), 1,
            "**the first is the one the named parameter took**");
        AssertEq(wert.Items[1].Items.Count, 2,
            "**and the second is a list of two** — the values the named "
                + "parameter did not take, and a reader that treated `rest` "
                + "as an ordinary name would have given it nil and dropped "
                + "them both");
    }

    /// <summary>
    /// `*rest` with no values left is an empty list, and not nil.
    /// </summary>
    /// <remarks>
    /// <strong>A game writes <c>teile.length</c>.</strong> A splat that
    /// collected nothing is a list that is empty,
    /// <strong>and a reader that answered nil would have made a game ask
    /// <c>length</c> of nil</strong> — the failure a plugin shows as an
    /// exception on the first call that happens to pass nothing.
    /// </remarks>
    public void Test_TheSplatWithNothingLeftIsAnEmptyList()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(*rest)\n"
            + "    rest\n"
            + "  end\n"
            + "end\n"
            + "A.m()\n"));

        AssertEq(wert.Kind, RubyValueKind.Object,
            "**the splat is a list and not nil** — a game writes "
                + "`teile.length`, and a reader that answered nil would have "
                + "made it ask `length` of nothing");
        AssertEq(wert.Items.Count, 0,
            "**and it is empty** — no values, and the list says so");
    }

    /// <summary>
    /// The named parameter before the splat still wins.
    /// </summary>
    /// <remarks>
    /// <strong>The list has a place where the rest starts.</strong> A reader
    /// that gave the splat everything would have taken the one as well,
    /// <strong>and a game's first argument would have been in the list
    /// instead of in the name the script reads it from.</strong>
    /// </remarks>
    public void Test_TheParameterBeforeTheSplatStillTakesItsValue()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a, *rest)\n"
            + "    [a, rest]\n"
            + "  end\n"
            + "end\n"
            + "A.m(7)\n"));

        AssertEq(AsInteger(wert.Items[0]), 7,
            "**the named parameter has the seven**");
        AssertEq(wert.Items[1].Items.Count, 0,
            "**and the splat is empty** — the rest starts after the name, "
                + "and a reader that gave the splat everything would have "
                + "taken the seven as well");
    }

    /// <summary>
    /// Two named parameters, then the splat.
    /// </summary>
    /// <remarks>
    /// <strong>Position for position, and the splat gets what is left.</strong>
    /// A reader that bound by name would have swapped them, **and a game with
    /// two arguments of different meanings would have swapped them too.</strong>
    /// </remarks>
    public void Test_TwoParametersThenTheSplatGetTheRestInOrder()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a, b, *rest)\n"
            + "    [a, b, rest]\n"
            + "  end\n"
            + "end\n"
            + "A.m(1, 2, 3, 4)\n"));

        AssertEq(AsInteger(wert.Items[0]), 1,
            "**the first is one**");
        AssertEq(AsInteger(wert.Items[1]), 2,
            "**the second is two and not three** — the binding is by "
                + "position, and a reader that bound by name would have "
                + "swapped them");
        AssertEq(wert.Items[2].Items.Count, 2,
            "**and the splat has the last two** — three and four, in that "
                + "order");
    }

    /// <summary>
    /// `**opts` is a parameter of its own, and not a list.
    /// </summary>
    /// <remarks>
    /// <strong>Two different things and two different names.</strong>
    /// <c>*rest</c> takes the surplus <em>values</em> and <c>**opts</c> the
    /// surplus <em>name/value pairs</em>,
    /// <strong>and a reader that filed both as one would have a game that
    /// passes a hash receive a list.</strong>
    /// </remarks>
    public void Test_TheDoubleSplatIsItsOwnParameterAndNotAList()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a, **opts)\n"
            + "    [a, opts]\n"
            + "  end\n"
            + "end\n"
            + "A.m(1, 2)\n"));

        var methode = mit.FindMethod("A", "m");
        AssertEq(methode?.OptionenParameter, "opts",
            "**the method carries the name `opts`** — and a reader that filed "
                + "it as a plain parameter would have filled it with a value "
                + "instead of a hash");
        AssertTrue(methode?.SammelParameter == null,
            "**and it is not a splat** — `*rest` and `**opts` are two "
                + "different things, and a reader that kept one name for both "
                + "would have given a game a list where it expects a hash");
    }

    /// <summary>
    /// A method without a splat drops the surplus, and that did not change.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's rule and this runtime's.</strong> <c>m(1, 2, 3)</c> on
    /// <c>def m(a)</c> binds the one and drops the rest,
    /// <strong>and a reader that invented a list for them would have made
    /// every short method collect something the game never named.</strong>
    /// </remarks>
    public void Test_AMethodWithoutASplatDropsTheSurplus()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a)\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "A.m(1, 2, 3)\n"));

        var methode = mit.FindMethod("A", "m");
        AssertTrue(methode?.SammelParameter == null,
            "**the method carries no splat name** — the surplus is dropped, "
                + "and a reader that invented a list for it would have made "
                + "every short method collect something the game never named");
    }

    /// <summary>
    /// A splat at the front takes everything, and the parameters after it get
    /// what is left.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the position is not optional.</strong> <c>def m(*teile,
    /// letzte)</c> gives <c>teile</c> every value but the last one and
    /// <c>letzte</c> the last, <strong>and a reader that only knew that a
    /// splat exists would have bound it after every parameter</strong> — which
    /// is right for the common case and wrong here.
    /// </para>
    /// <para>
    /// <strong>This is the case that kills the "the position is never -1"
    /// mutation.</strong> With the splat first, <c>SammelAb</c> is zero, and
    /// the last parameter binds from the end of the list
    /// <strong>backwards</strong>, which is the only way the two can share
    /// one argument list.
    /// </para>
    /// </remarks>
    public void Test_ASplatAtTheFrontLeavesTheLastValueToTheLastParameter()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(*teile, letzte)\n"
            + "    [teile, letzte]\n"
            + "  end\n"
            + "end\n"
            + "A.m(1, 2, 3)\n"));

        AssertEq(wert.Items.Count, 2,
            "**two values came back**");
        AssertEq(wert.Items[0].Items.Count, 2,
            "**the splat has the first two** — it starts at zero, and a "
                + "reader that bound it after every parameter would have put "
                + "all three in the list and left the last parameter nothing");
        AssertEq(AsInteger(wert.Items[1]), 3,
            "**and the last parameter has the three** — the one value the "
                + "splat did not take, and a reader that gave the splat "
                + "everything would have answered nil here");
    }

    /// <summary>
    /// `k: 3` is a pair, and it reaches `**opts` as one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>It did not parse at all, and that was the whole gap.</strong>
    /// <c>ParseExpression</c> read <c>k</c> as a name and then stood on a
    /// <c>:</c>, <strong>and the call was reported as a syntax error</strong> —
    /// the spelling every Ruby writer uses, in every call a game makes.
    /// </para>
    /// <para>
    /// <strong>And the key becomes a symbol.</strong> <c>k: 3</c> means
    /// <c>:k =&gt; 3</c> in a hash, <strong>and a reader that kept the bare
    /// name would have given a game a hash with a key it never writes</strong>
    /// — it would be looking up <c>opts[:k]</c> and finding nothing.
    /// </para>
    /// <para>
    /// <strong>And the pairs go into one hash, not a list of them.</strong>
    /// <c>f(a: 1, b: 2)</c> is two pair-hashes from the parser, and the call
    /// joins them, <strong>because a game reads <c>opts[:a]</c> and
    /// <c>opts[:b]</c> out of one thing</strong>.
    /// </para>
    /// </remarks>
    public void Test_ANamedArgumentReachesTheOptionsAsAPair()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(**opts)\n"
            + "    opts\n"
            + "  end\n"
            + "end\n"
            + "A.m(k: 3)\n"));

        AssertTrue(wert.Kind == RubyValueKind.Object,
            "**the options came back and not nil** — a reader that answered "
                + "nil would have made a game's named argument disappear with "
                + "nothing said, and the call would have looked like it "
                + "worked");
        // **Und es ist ein Hash und keine Liste.** Beide sind `Object`,
        // **und ein Leser, der eine Liste gibt, haette ein Spiel, das
        // `opts[:k]` liest, auf einer Liste ohne Index** -- und genau hier
        // faellt es nicht auf, **weil die Items dieselben sind.**
        AssertTrue(wert.IsHash,
            "**and it is a hash, not a list** — both are `Object` and both "
                + "hold the same two values, so nothing else in this test "
                + "can tell them apart; a game reads `opts[:k]` out of a "
                + "hash and would have found nothing in a list");
        AssertEq(wert.Items.Count, 2,
            "**and they are one pair** — a key and a value, and a reader that "
                + "put two pairs in would have doubled every named argument "
                + "a game writes");
        AssertEq(wert.Items[0].Kind, RubyValueKind.Symbol,
            "**the key is a symbol** — `k: 3` means `:k => 3`, and a reader "
                + "that kept the bare name would have given a game a hash with "
                + "a key it never writes");
        AssertEq(wert.Items[0].Name, "k",
            "**and it is named k**");
        AssertEq(AsInteger(wert.Items[1]), 3,
            "**and the value is the three**");
    }

    /// <summary>
    /// Two named arguments go into one hash, and a value still goes to the
    /// parameter.
    /// </summary>
    /// <remarks>
    /// <strong>A value goes to exactly one place.</strong> <c>m(1, k: 3)</c>
    /// gives the parameter the one and the options the three,
    /// <strong>and a reader that put everything into the hash would have given
    /// the parameter nothing</strong> — which is a game whose first argument
    /// is a number it then reads as nil.
    /// </remarks>
    public void Test_AValueAndAPairGoToTwoDifferentPlaces()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(a, **opts)\n"
            + "    [a, opts]\n"
            + "  end\n"
            + "end\n"
            + "A.m(1, k: 3)\n"));

        AssertEq(AsInteger(wert.Items[0]), 1,
            "**the parameter has the one** — a value goes to a named "
                + "parameter and a reader that put everything into the hash "
                + "would have given it nothing");
        AssertEq(wert.Items[1].Items.Count, 2,
            "**and the options have the pair** — one key and one value, not "
                + "three items and not nil");
    }

    /// <summary>
    /// A call with no named arguments gets an empty hash.
    /// </summary>
    /// <remarks>
    /// <strong>An empty hash and not nil.</strong> A game writes
    /// <c>opts.empty?</c> and a nil would have no answer,
    /// <strong>and a reader that made a list out of nothing would have given
    /// a game a thing that answers <c>empty?</c> differently from a hash.</strong>
    /// </remarks>
    public void Test_NoNamedArgumentsIsAnEmptyHash()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m(**opts)\n"
            + "    opts\n"
            + "  end\n"
            + "end\n"
            + "A.m()\n"));

        AssertTrue(wert.Kind == RubyValueKind.Object,
            "**the options are a value and not nil** — a game writes "
                + "`opts.empty?` and nil has no answer");
        AssertTrue(wert.IsHash,
            "**and it is a hash** — an empty hash answers `empty?` and an "
                + "empty list does not, so a reader that made a list out of "
                + "nothing would have changed the answer a game gets");
        AssertEq(wert.Items.Count, 0,
            "**and it is empty** — no pairs, and the value says so");
    }
}
