using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `lambda` and `proc` as values, which is what a game stores in a variable
/// and calls later.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Neither word is a keyword.</strong> The lexer's grammar list has
/// forty-one names and neither is among them, <strong>and a reader that
/// guessed "keyword" would have had to add two more and break the count
/// test.</strong> They are ordinary identifiers that a block hangs off, and
/// the parser builds the same block node for <c>lambda { |x| x }</c> as for
/// <c>a.each { |x| x }</c> — <strong>with the one difference that the
/// receiver is a name that does not exist.</strong>
/// </para>
/// <para>
/// <strong>A kind of its own.</strong> A lambda answers an arity, takes
/// arguments and answers a value, so it cannot be a number or a name:
/// <strong>a reader that filed it under one could not tell it from
/// <c>0</c></strong>, and storing a lambda in a variable is the most
/// ordinary thing a script does.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A lambda is a value, and calling it runs the block.
    /// </summary>
    /// <remarks>
    /// <strong>The two statements are separate.</strong> <c>f = lambda { |x|
    /// x * 2 }</c> must build something, and only a later <c>f.call(3)</c>
    /// runs it — <strong>a reader that ran the block where it stood would
    /// have answered a number there and had nothing in <c>f</c>.</strong>
    /// </remarks>
    public void Test_ALambdaIsAValueAndCallingItRunsTheBlock()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "f = lambda { |x| x * 2 }\n"
            + "f.call(3)\n"));

        AssertEq(AsInteger(wert), 6,
            "**the call answers six** — the block was kept as a value and the "
                + "call ran it, and a reader that ran the block where it stood "
                + "would have answered six in the first statement and had "
                + "nothing in f to call");
    }

    /// <summary>
    /// `lambda` and `proc` are one thing under two names.
    /// </summary>
    /// <remarks>
    /// <strong>Both spellings, and both have to work.</strong> A game writes
    /// <c>proc</c> in a host callback and <c>lambda</c> in a local helper,
    /// <strong>and a reader that only knew one of them would have left the
    /// other as a call to a name that does not exist</strong> — a silent nil
    /// where the game expects something callable.
    /// </remarks>
    public void Test_LambdaAndProcAreTheSameThing()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "a = lambda { |x| x + 1 }\n"
            + "b = proc { |x| x + 1 }\n"
            + "a.call(1) + b.call(1)\n"));

        AssertEq(AsInteger(wert), 4,
            "**both answer two and add up to four** — the two names are one "
                + "instruction, and a reader that knew only `lambda` would "
                + "have left `proc` as a call to a name that does not exist");
    }

    /// <summary>
    /// A parameter the call did not supply is nil.
    /// </summary>
    /// <remarks>
    /// <strong>Fewer arguments than parameters is normal and not an
    /// error.</strong> Ruby binds what it can and leaves the rest nil,
    /// <strong>and that is what lets a game write a two-parameter block for
    /// a call that carries one value</strong> — the form a host's own
    /// callbacks arrive in, and the reason this rule has to be here.
    /// </remarks>
    public void Test_AMissingArgumentIsNilAndNotAnError()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "f = lambda { |x, y| [x, y] }\n"
            + "f.call(7)\n"));

        AssertTrue(mit.Diagnostics.Count == 0,
            "**and nothing was said** — a block that asks for two and is "
                + "called with one is ordinary Ruby, and a reader that "
                + "complained would have stopped every host callback that "
                + "carries fewer values than the block names; the "
                + "diagnostics were: " + string.Join(" | ", mit.Diagnostics));
        AssertEq(wert.Items.Count, 2,
            "**and the block got two values**");
        AssertEq(AsInteger(wert.Items[0]), 7,
            "**the first is the seven it was called with**");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Nil,
            "**and the second is nil** — the block named it, the call did "
                + "not supply it, and a reader that bound the first "
                + "argument twice, or that refused the call, would have "
                + "answered something else here");
    }


    /// <summary>
    /// `return` inside a block comes back to the call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>It does not end the program.</strong> That is what makes a
    /// block different from a method, <strong>and a reader that let it escape
    /// would have ended the whole program</strong> whenever a game wrote
    /// <c>liste.each { return 1 }</c> — which is a normal early exit inside a
    /// loop, and the most common shape there is.
    /// </para>
    /// <para>
    /// <strong>The value that comes back is the return's value</strong> and
    /// not the last statement's — here the block continues after the
    /// <c>return</c>, <strong>and a reader that only stopped at the end would
    /// have answered the wrong number.</strong>
    /// </para>
    /// </remarks>
    public void Test_ReturnInsideABlockComesBackToTheCall()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "f = lambda { return 7; 99 }\n"
            + "g = f.call\n"
            + "g\n"));

        AssertEq(AsInteger(wert), 7,
            "**the return's value and not the last statement's** — a block's "
                + "return comes back to the call, and a reader that let it "
                + "escape would have ended the program here and answered "
                + "nothing at all, and one that only stopped at the end would "
                + "have answered 99");
    }

    /// <summary>
    /// A block with no parameters binds nothing.
    /// </summary>
    /// <remarks>
    /// <strong>An empty <c>||</c> is a real form</strong> — a game writes it
    /// for a block that only closes over what it already sees,
    /// <strong>and a reader that invented a positional name for it would have
    /// bound the first argument to something the game never named</strong>,
    /// so a host callback that carries an event would have leaked it into the
    /// block as a variable the game did not ask for.
    /// </remarks>
    public void Test_ABlockWithNoParametersBindsNothing()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "x = 5\n"
            + "f = lambda { || 3 }\n"
            + "f.call(1, 2, 3)\n"));

        AssertEq(AsInteger(wert), 3,
            "**the block ran and the arguments were not bound** — an empty "
                + "parameter list binds nothing, and a reader that invented a "
                + "positional name for it would have bound the first "
                + "argument to something the game never named");
    }

    /// <summary>
    /// A block without braces is a block too.
    /// </summary>
    /// <remarks>
    /// <strong>`lambda do |x| ... end` is the same instruction</strong> — and
    /// <strong>the long form is the one a game's own style tends to
    /// use</strong>, because a multi-line block reads better that way. A
    /// reader that only knew the braces would have read the word as a call
    /// that happens to be followed by something.
    /// </remarks>
    public void Test_TheDoEndSpellingIsABlockToo()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "f = lambda do |x| x + 10 end\n"
            + "f.call(5)\n"));

        AssertEq(AsInteger(wert), 15,
            "**the do/end form answers fifteen** — it is the same "
                + "instruction as the braces, and the long form is the one a "
                + "game's own style tends to write");
    }

    /// <summary>
    /// A lambda is not a number and not a name.
    /// </summary>
    /// <remarks>
    /// <strong>Its own kind, and this test is why.</strong> A reader that
    /// filed a lambda under a number could not tell it from <c>0</c>,
    /// <strong>and a game that stores a lambda in a variable and calls it
    /// later is the most ordinary thing a script does</strong> — so the
    /// difference is not a detail.
    /// </remarks>
    public void Test_ALambdaHasItsOwnKind()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("lambda { |x| x }\n"));

        AssertTrue(wert.Kind == RubyValueKind.Proc,
            "**it is a Proc and not a number, a name or nil** — a reader that "
                + "filed it under one of those could not tell it from the "
                + "value that says the same thing, and the game calls it "
                + "later");
    }

    /// <summary>
    /// A block an an ordinary call belongs to the call, and not to lambda.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The two must not be confused, and the proof is what the host
    /// is asked about.</strong> <c>a.each { |x| x }</c> hands its block to
    /// <c>each</c> and the host runs it, and <c>lambda { |x| x }</c> keeps it
    /// as a value the game calls itself — <strong>and a reader that had made
    /// every block a Proc would have answered the block itself where the
    /// game's <c>each</c> belongs.</strong>
    /// </para>
    /// <para>
    /// <strong>The null host has no <c>each</c></strong>, and that is the
    /// point: its complaint names <c>each</c>, which means the block went to
    /// the call. <strong>A block that had been swallowed would have produced
    /// a Proc and no complaint at all</strong>, so the message itself is the
    /// evidence.
    /// </para>
    /// </remarks>
    public void Test_AnOrdinaryCallStillTakesItsBlock()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements("a = [1, 2, 3]\na.each { |x| x }\n"));

        var ueberDenAufruf = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("'each'"))
            {
                ueberDenAufruf = true;
            }
        }

        AssertTrue(ueberDenAufruf,
            "**the host was asked about each, so the block went to the call** "
                + "— a reader that had made every block a Proc would have "
                + "answered the block itself and never asked about each; the "
                + "diagnostics were: " + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A block gets its own variables, and not the caller's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the difference between a block and frozen caller
    /// state.</strong> Two calls of one lambda must not share a variable,
    /// <strong>and a reader that ran the body on the caller's scope would
    /// have let the second call see what the first left behind.</strong>
    /// </para>
    /// <para>
    /// <strong>The block cannot even see the name the caller used.</strong> A
    /// method's locals are not visible inside a lambda, so a block that
    /// writes <c>x</c> without a parameter is a different <c>x</c> —
    /// <strong>and a reader that looked outward would have bound the
    /// caller's <c>x</c> instead</strong>, which is the bug this test exists
    /// for.
    /// </para>
    /// <para>
    /// <strong>The block's own answer goes into an array, and not into the
    /// method's return.</strong> A block is not a method, so its last
    /// statement is not the method's value — <strong>and a reader that
    /// returned the block's answer would have answered one here</strong>,
    /// having had the block write over the method's variable.
    /// </para>
    /// </remarks>
    public void Test_ABlockHasItsOwnVariables()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def aussen\n"
            + "    x = 99\n"
            + "    f = lambda { x = 1; x }\n"
            + "    g = f.call\n"
            + "    [x, g]\n"
            + "  end\n"
            + "end\n"
            + "A.aussen\n"));

        AssertEq(wert.Items.Count, 2,
            "**the method answered two values**");
        AssertEq(AsInteger(wert.Items[0]), 99,
            "**the caller's x is still 99 after the block** — the block's x "
                + "is its own, and a reader that shared the scope would have "
                + "answered one here, having had the block write over the "
                + "method's variable");
        AssertEq(AsInteger(wert.Items[1]), 1,
            "**and the block answered one** — its own x, which is a different "
                + "x from the caller's, and a reader that looked outward would "
                + "have written over the method's 99 and answered 99 here");
    }

    /// <summary>
    /// Two blocks deep, and the inner one does not see the outer one's
    /// locals.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the test the missing cleanup needed.</strong> A block
    /// wall that was pushed and never popped makes every later block stop at
    /// an <em>older</em> wall, <strong>and then an inner block sees the outer
    /// one's variables</strong> — the same mistake one level down, and one
    /// that only shows up after a lambda has been called at least once.
    /// </para>
    /// <para>
    /// <strong>Two calls, and the second one is where it shows.</strong> The
    /// first call leaves its wall behind if the cleanup is missing;
    /// <strong>the second call then stands inside the first call's wall</strong>
    /// and the inner block can see a name the game made local to the outer
    /// one. A reader that never popped would have answered the outer value
    /// here.
    /// </para>
    /// </remarks>
    public void Test_TwoBlocksDeepAndTheInnerOneSeesNothing()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def aussen\n"
            + "    x = 99\n"
            + "    innen = lambda { x = 1; lambda { x = 2; x }.call; x }\n"
            + "    [x, innen.call, innen.call]\n"
            + "  end\n"
            + "end\n"
            + "A.aussen\n"));

        AssertEq(wert.Items.Count, 3,
            "**the method answered three values**");
        AssertEq(AsInteger(wert.Items[0]), 99,
            "**the caller's x is untouched**");
        AssertEq(AsInteger(wert.Items[1]), 1,
            "**the outer block's answer is its own one** — a reader whose "
                + "wall was never popped would have let the inner block see "
                + "the outer's x and written over it, so the outer would "
                + "have answered two here");
        AssertEq(AsInteger(wert.Items[2]), 1,
            "**and the second call answers the same one** — the wall left "
                + "behind by the first call is what would have made this "
                + "answer something else, and two calls are what it takes to "
                + "see that");
    }

    /// <summary>
    /// A block that reads a name the method has sees nothing, and not its
    /// value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Reading and writing are both walls, and the first version
    /// only walled the write.</strong> `SetLocal` stopped at the block wall,
    /// <c>Local</c> and <c>HasLocal</c> did not — <strong>so a block that
    /// wrote <c>x = 1</c> kept the method's 99 safe, and a block that only
    /// <em>read</em> <c>x</c> got the 99 as if it were its own.</strong> A
    /// game would have read a value from a name it never wrote, and nothing
    /// anywhere said so.
    /// </para>
    /// <para>
    /// <strong>This is also the test that kills the missing cleanup.</strong>
    /// A block wall that was pushed and never popped makes every later
    /// <c>Local</c> walk stop at an older frame, <strong>so the second call
    /// of one lambda stands inside the first call's wall</strong> and sees a
    /// name from a frame that is long gone. Two calls and a read are what it
    /// takes to see that.
    /// </para>
    /// </remarks>
    public void Test_ABlockThatOnlyReadsSeesNothing()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def aussen\n"
            + "    x = 99\n"
            + "    f = lambda { x }\n"
            + "    [f.call, f.call]\n"
            + "  end\n"
            + "end\n"
            + "A.aussen\n"));

        AssertEq(wert.Items.Count, 2,
            "**the method answered two values**");
        AssertTrue(wert.Items[0].Kind == RubyValueKind.Nil,
            "**and the first read is nil** — the block named `x` and did not "
                + "write it, and a reader whose `Local` walked past the block "
                + "wall would have handed it the method's 99, which is a "
                + "value out of a name the block never wrote");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Nil,
            "**and so is the second** — a wall that was pushed and never "
                + "popped would have left the second call inside the first "
                + "one's frame, and one read is enough to see it");
    }

    /// <summary>
    /// `defined?` in a block sees the block's own names, and not the
    /// method's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>HasLocal is the third wall, and it is the one that answers
    /// questions.</strong> `Local` reads a value and `HasLocal` answers
    /// whether there is one, <strong>and they have to agree</strong>: a block
    /// whose <c>defined?(x)</c> said <c>"local-variable"</c> while the very
    /// next <c>x</c> answered <c>nil</c> would be a script that lies to
    /// itself. <strong>Fixing <c>Local</c> alone left that split open</strong>,
    /// and the mutation that removed the wall from <c>HasLocal</c> survived
    /// every other test in this file because none of them asks a question
    /// inside a block.
    /// </para>
    /// <para>
    /// <strong>Both halves are in one test on purpose.</strong> The
    /// <c>defined?</c> answer and the value answer are the same fact seen
    /// twice, <strong>and a test that only asked one of them would have
    /// passed with the walls in different places.</strong>
    /// </para>
    /// </remarks>
    public void Test_DefinedInABlockSeesTheBlockAndNotTheMethod()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def aussen\n"
            + "    x = 99\n"
            + "    f = lambda { [defined?(x), x] }\n"
            + "    f.call\n"
            + "  end\n"
            + "end\n"
            + "A.aussen\n"));

        AssertEq(wert.Items.Count, 2,
            "**the block answered two things** — the question and the value, "
                + "which are the same fact seen twice");
        AssertTrue(wert.Items[0].Kind == RubyValueKind.Nil,
            "**`defined?(x)` in the block is nil** — the block named `x` and "
                + "wrote nothing, and a reader whose `HasLocal` walked past "
                + "the block wall would have answered the local-variable word here "
                + "and then answered nil for the very same name, which is a "
                + "script lying to itself");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Nil,
            "**and `x` is nil** — the two answers agree, and a reader that "
                + "walled one and not the other would have them disagree");
    }
}
