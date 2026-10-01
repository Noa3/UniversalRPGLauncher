using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Turns a token stream into a tree of shapes.
/// </summary>
/// <remarks>
/// Every expectation is a tree written out by hand from the grammar's rules. A
/// tree produced by the parser and compared against itself would prove nothing,
/// so the expected shapes here are written as the structure a reader of the
/// grammar would draw, and the parser has to agree with them.
/// </remarks>
public partial class TestRubyParser : TestBase
{
    private static List<RubyNode> Parse(string pSource)
    {
        return new RubyParser(new RubyLexer(pSource).Tokenize()).ParseProgram();
    }

    private static RubyNode One(string pSource)
    {
        var statements = Parse(pSource);
        if (statements.Count != 1)
        {
            throw new InvalidOperationException(
                $"'{pSource}' parsed as {statements.Count} statements, not one.");
        }
        return statements[0];
    }

    /// <summary>
    /// Ruby 1.8.1's own four scripts, and the shapes they stop on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And these are the engine's own files, byte for byte.</strong>
    /// The four <c>.rb</c> files of the v1_8_1 tag, 20426 bytes together.
    /// <strong>And a test that wrote its own Ruby would have proved that
    /// the reader agrees with its author's idea of Ruby, and nothing
    /// else.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieVierSkripteVonRuby181SelbstWerdenGelesen()
    {
        var wurzel = "res://tests/fixtures/ruby181";
        var namen = new[]
        {
            "instruby.rb", "mdoc2man.rb", "mkconfig.rb", "rubytest.rb",
        };
        var fehler = new List<string>();
        var knoten = 0;

        foreach (var name in namen)
        {
            var pfad = $"{wurzel}/{name}";
            if (!Godot.FileAccess.FileExists(pfad))
            {
                fehler.Add($"{name} is not at {pfad}");
                continue;
            }

            var bytes = Godot.FileAccess.GetFileAsBytes(pfad);
            var quelle = System.Text.Encoding.UTF8.GetString(bytes);

            try
            {
                var program = new RubyParser(new RubyLexer(quelle).Tokenize())
                    .ParseProgram();
                knoten += program.Count;
                if (program.Count == 0)
                {
                    fehler.Add($"{name} parsed as no statements, and it"
                        + $" holds {quelle.Length} characters");
                }
            }
            catch (Exception pProblem)
            {
                // **And the line is in the exception, measured at
                // `RubyParseException.Line` and `RubySyntaxException.Line`,
                // and the text around it comes out of the file itself**,
                // **so the message shows what stands there.**
                var zeile = pProblem is RubyParseException ppe
                    ? ppe.Line
                    : (pProblem as RubySyntaxException)?.Line ?? -1;
                var anfang = 0;
                for (var k = 0; k < zeile - 1 && anfang < quelle.Length; k++)
                {
                    anfang = quelle.IndexOf('\n', anfang) + 1;
                }

                fehler.Add($"{name} at line {zeile}: ["
                    + quelle.Substring(
                        anfang,
                        Math.Min(110, quelle.Length - anfang))
                        .Replace("\n", "\\n")
                    + "]");
            }
        }

        AssertTrue(
            fehler.Count == 0,
            "**and every one of Ruby 1.8.1's own four scripts parses** --"
            + $" and {fehler.Count} did not: "
            + string.Join(" | ", fehler));
        AssertTrue(
            knoten > 400,
            "**and they come out as a program, and not as a shrug** -- and"
            + $" the four files hold {knoten} top-level statements");
    }

    /// <summary>
    /// The shapes the four real scripts stop on, one at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And every shape here is copied out of one of the four
    /// files, and not invented</strong> -- <strong>and a reader that
    /// handles a whole file stops on the first shape it never met, so the
    /// shape is the unit of failure.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieFormenDieDieEchtenSkripteNochStoppen()
    {
        var faelle = new (string Quelle, string Woher)[]
        {
            ("mflags = ($OPT['a'] || '').strip if mflags.empty?",
             "instruby.rb 23 -- a modifier after a call on a bracket"),
            ("$make, *rest = Shellwords.shellwords($make)",
             "instruby.rb 31 -- a star that takes the rest of the values"),
            ("retval << \".nf\n\" << '\\&  '",
             "mdoc2man.rb 232 -- two appends, which is one expression"),
            ("dest = drive ? /= \"x\"(?![a])/i : /= \"y\"/",
             "mkconfig.rb 90 -- a ternary whose arms are delimited regexps"),
            ("error << line if line =~ %r:^(a|not):",
             "rubytest.rb 42 -- a modifier after an append, and %r"),
        };
        var fehler = new List<string>();

        foreach (var (quelle, woher) in faelle)
        {
            try
            {
                var program = Parse(quelle);
                if (program.Count != 1)
                {
                    fehler.Add($"{woher}: parsed as {program.Count}"
                        + " statements, not 1");
                }
            }
            catch (Exception pProblem)
            {
                var alle = new RubyLexer(quelle).Tokenize();
                var liste = string.Join(" ",
                    alle.Select(x => x.Kind + ":" + x.Text));
                fehler.Add($"{woher}: {pProblem.GetType().Name}"
                    + $" {pProblem.Message} | tokens: {liste}");
            }
        }

        AssertTrue(
            fehler.Count == 0,
            "**and each of the shapes the real scripts stop at parses on"
            + $" its own** -- and {fehler.Count} did not: "
            + string.Join(" | ", fehler));
    }

    private string Refusal(Action pAction)
    {
        try
        {
            pAction();
            return "";
        }
        catch (RubyParseException exception)
        {
            return exception.Message;
        }
    }

    public void Test_LiteralsBecomeLeavesWithTheirValues()
    {
        AssertEq(One("42").Kind, RubyNodeKind.Integer, "a number is an integer node");
        AssertEq(One("42").Integer!.Value, 42L, "holding forty two");
        AssertEq(One("1.5").Kind, RubyNodeKind.Float, "a fraction is a float node");
        AssertEq(One("1.5").Real!.Value, 1.5, "holding one and a half");
        AssertEq(One("\"hi\"").Kind, RubyNodeKind.String, "a string is a string node");
        AssertEq(One("\"hi\"").Text, "hi", "holding its text");
        AssertEq(One(":sym").Kind, RubyNodeKind.Symbol, "a symbol is a symbol node");
        AssertEq(One(":sym").Text, "sym", "naming sym");
        AssertEq(One("nil").Kind, RubyNodeKind.Nil, "nil is nil");
        AssertEq(One("true").Kind, RubyNodeKind.True, "true is true");
        AssertEq(One("false").Kind, RubyNodeKind.False, "false is false");
        AssertEq(One("self").Kind, RubyNodeKind.Self, "self is self");
    }

    public void Test_ANameAndAConstantAreDifferentNodes()
    {
        AssertEq(One("sprite").Kind, RubyNodeKind.Identifier, "a lower case name");
        AssertEq(One("Sprite").Kind, RubyNodeKind.Constant, "an upper case name");
        AssertEq(One("sprite").Name, "sprite", "naming sprite");
    }

    public void Test_MultiplicationBindsTighterThanAddition()
    {
        // The first thing the precedence table decides. If this were wrong,
        // every arithmetic expression in a game would parse into the other tree
        // and nothing about the result would look wrong.
        var node = One("a + b * c");
        AssertEq(node.Kind, RubyNodeKind.Binary, "the whole is a binary node");
        AssertEq(node.Operator, "+", "at the top of the tree is the addition");
        AssertEq(node.First!.Kind, RubyNodeKind.Identifier, "with a name on its left");
        AssertEq(node.Second!.Kind, RubyNodeKind.Binary, "and a binary node on its right");
        AssertEq(node.Second!.Operator, "*", "which is the multiplication");
        AssertEq(node.Second!.First!.Name, "b", "of b");
        AssertEq(node.Second!.Second!.Name, "c", "and c");
    }

    public void Test_AdditionBindsLooserThanMultiplicationOnTheLeftToo()
    {
        // The same rule seen from the other side: the tree is not simply
        // right leaning, and a reader that got this wrong would produce a chain
        // instead of a split.
        var node = One("a * b + c");
        AssertEq(node.Kind, RubyNodeKind.Binary, "the whole is a binary node");
        AssertEq(node.Operator, "+", "with the addition on top");
        AssertEq(node.Second!.Name, "c", "and c alone on the right");
        AssertEq(node.First!.Operator, "*", "the multiplication is entirely on the left");
    }

    public void Test_ComparisonBindsLooserThanArithmetic()
    {
        // The grammar puts the comparison level below addition, so the whole
        // arithmetic is on the left of the comparison.
        var node = One("a + b < c * d");
        AssertEq(node.Operator, "<", "the comparison is at the top");
        AssertEq(node.First!.Operator, "+", "the addition is on the left");
        AssertEq(node.Second!.Operator, "*", "and the multiplication on the right");
    }

    public void Test_BitwiseLevelsFollowTheGrammarOrder()
    {
        // The grammar declares '|' and '^' at one level, '&' below them, and the
        // shifts below that, so each binds tighter than the one above.
        var shifts = One("a | b & c << d");
        AssertEq(shifts.Operator, "|", "the loosest of the three is at the top");
        AssertEq(shifts.Second!.Operator, "&", "'&' binds tighter than '|'");
        AssertEq(shifts.Second!.Second!.Operator, "<<", "and '<<' tighter still");

        // Within one level the operators are left associative, so a chain
        // leans left.
        var chain = One("a - b - c");
        AssertEq(chain.Operator, "-", "the top is a subtraction");
        AssertEq(chain.First!.Kind, RubyNodeKind.Binary, "whose left is another one");
        AssertEq(chain.First!.First!.Name, "a", "starting at a");
        AssertEq(chain.First!.Second!.Name, "b", "then b");
        AssertEq(chain.Second!.Name, "c", "and c is on the right of the top");
    }

    public void Test_PowerIsRightAssociative()
    {
        // The grammar declares '**' with %right, so a chain leans right. A
        // reader that treated it as left associative would compute a different
        // number for the same source.
        var node = One("a ** b ** c");
        AssertEq(node.Operator, "**", "the top is the power");
        AssertEq(node.First!.Name, "a", "with a alone on the left");
        AssertEq(node.Second!.Kind, RubyNodeKind.Binary, "and another power on the right");
        AssertEq(node.Second!.First!.Name, "b", "of b");
        AssertEq(node.Second!.Second!.Name, "c", "and c");
    }

    public void Test_UnaryOperatorsBindTighterThanBinaryOnes()
    {
        // The grammar puts '!' above every binary operator and the unary minus
        // just below the arithmetic, so -a * b is not (a * b) negated but the
        // product of a negated a and b.
        var product = One("-a * b");
        AssertEq(product.Operator, "*", "the multiplication is at the top");
        AssertEq(product.First!.Kind, RubyNodeKind.Unary, "with a unary on the left");
        AssertEq(product.First!.Operator, "-", "which is the minus");
        AssertEq(product.First!.First!.Name, "a", "of a");

        // '!' is an operator node and 'not' is a keyword node, so the two are
        // told apart even though both negate.
        var negated = One("!a && b");
        AssertEq(negated.Operator, "&&", "the logical and is the looser of the two");
        AssertEq(negated.First!.Kind, RubyNodeKind.Unary, "so '!' binds tighter than it");
    }

    public void Test_AssignmentIsItsOwnLevelAndBindsLoosest()
    {
        // The grammar makes '=' right associative and looser than any comparison,
        // so `a = b == c` assigns the comparison and not a bare name.
        var node = One("a = b == c");
        AssertEq(node.Kind, RubyNodeKind.Assignment, "the whole is an assignment");
        AssertEq(node.First!.Name, "a", "of a");
        AssertEq(node.Second!.Kind, RubyNodeKind.Binary, "and the comparison on the right");
        AssertEq(node.Second!.Operator, "==", "which is the comparison");

        var right = One("a = b = c");
        AssertEq(right.Second!.Kind, RubyNodeKind.Assignment, "a chain of assignments");
        AssertEq(right.Second!.First!.Name, "b", "where the inner one assigns b");
        AssertEq(right.Second!.Second!.Name, "c", "and the inner one's value is c");
    }

    public void Test_ATernaryBindsLooserThanEveryOperator()
    {
        // The grammar puts '?' at the top of the precedence table, so the whole
        // comparison becomes its condition.
        var node = One("a < b ? c : d");
        AssertEq(node.Kind, RubyNodeKind.Ternary, "the whole is a ternary");
        AssertEq(node.First!.Kind, RubyNodeKind.Binary, "whose condition is a comparison");
        AssertEq(node.Second!.Name, "c", "then branch c");
        AssertEq(node.Third!.Name, "d", "and else branch d");
    }

    public void Test_ArraysAndHashesHoldTheirElementsInOrder()
    {
        var array = One("[1, 2, 3]");
        AssertEq(array.Kind, RubyNodeKind.Array, "a bracketed list is an array");
        AssertEq(array.Children.Count, 3, "with three elements");
        AssertEq(array.Children[0].Integer!.Value, 1L, "the first is one");
        AssertEq(array.Children[2].Integer!.Value, 3L, "the third is three");

        var hash = One("{ :a => 1, :b => 2 }");
        AssertEq(hash.Kind, RubyNodeKind.Hash, "a braced list is a hash");
        AssertEq(hash.Children.Count, 2, "with two entries");
        AssertEq(hash.Children[0].Kind, RubyNodeKind.Binary, "the first is a pair");
        AssertEq(hash.Children[0].Operator, "=>", "built from a fat arrow");
    }

    public void Test_ACallRecordsThatItWasWrittenAndNothingMore()
    {
        // The whole point of this parser: a call node names what was written and
        // knows nothing about whether it resolves or what it does.
        var node = One("sprite.draw(x, y)");
        AssertEq(node.Kind, RubyNodeKind.Call, "a call is a call node");
        AssertEq(node.Name, "draw", "naming the method as written");
        AssertEq(node.First!.Name, "sprite", "with the receiver as its first child");
        AssertEq(node.Children.Count, 3, "and the receiver plus two arguments");
        AssertEq(node.Children[1].Name, "x", "the first argument");
        AssertEq(node.Children[2].Name, "y", "the second");
    }

    /// <summary>
    /// A call without a written receiver is a call on `self`, and says so.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the earlier form put the name where the receiver goes.</strong>
    /// <c>draw(x)</c> hat keinen geschriebenen Empfaenger,
    /// **und der Knoten legte sich selbst als erstes Kind** —
    /// **und `Call` wertet sein erstes Kind als Empfaenger aus**,
    /// **und ein Name, den niemand gesetzt hat, ist nil**,
    /// **und die Meldung lautete *„nil has no method 'sprintf' on this
    /// host"*** — **also ueber einen Empfaenger, den der Leser selbst
    /// erfunden hatte.**
    /// </para>
    /// <para>
    /// <strong>And `SelfCall` is a different kind, and not a different
    /// name.</strong> The interpreter knows that <c>self</c> was meant,
    /// **and a call that really has a receiver stays a `Call`** —
    /// which is the whole difference between <c>draw(x)</c> in a class and
    /// <c>sprite.draw(x)</c> on an object.
    /// </para>
    /// </remarks>
    public void Test_ACallWithoutAReceiverIsACallOnSelf()
    {
        var node = One("draw(x)");
        AssertEq(node.Kind, RubyNodeKind.SelfCall,
            "**a call with no written receiver is a call on self** — and a "
                + "reader that made the name the receiver had a receiver "
                + "nobody wrote, and the message named a host that had done "
                + "nothing");
        AssertEq(node.Name, "draw", "naming the method as written");
        AssertEq(node.Children.Count, 1, "**and the argument is the only child**");
        AssertEq(node.Children[0].Name, "x", "which is the argument that was written");
    }

    public void Test_IndexingIsACallNamedForTheIndexOperator()
    {
        var node = One("items[0]");
        AssertEq(node.Kind, RubyNodeKind.Call, "an index is a call");
        AssertEq(node.Name, "[]", "named for the operator that was written");
        AssertEq(node.First!.Name, "items", "with the receiver first");
        AssertEq(node.Second!.Integer!.Value, 0L, "and the index as an argument");
    }

    public void Test_TwoAdjacentStringLiteralsAreOneString()
    {
        // Ruby joins them, and a game relies on that to split a long line
        // without a backslash. A parser that kept them apart would report a
        // syntax error on a file Ruby accepts.
        var node = One("\"a\" \"b\"");
        AssertEq(node.Kind, RubyNodeKind.String, "the pair is one string");
        AssertEq(node.Text, "ab", "with both halves in it");
    }

    public void Test_IfBecomesANodeWithItsConditionAndBody()
    {
        var node = One("if a then b end");
        AssertEq(node.Kind, RubyNodeKind.If, "an if is an if node");
        AssertEq(node.Children.Count, 2, "with a condition and a body");
        AssertEq(node.First!.Name, "a", "the condition is a");
        AssertEq(node.Second!.Children[0].Name, "b", "and the body holds b");
    }

    public void Test_ATrailingIfIsAModifierAndHoldsTheConditionSecond()
    {
        // The two shapes are the same words in a different order, and which is
        // which decides the order of the children.
        var modifier = One("b if a");
        AssertEq(modifier.Kind, RubyNodeKind.If, "a trailing if is an if node");
        AssertEq(modifier.First!.Name, "b", "whose body comes first");
        AssertEq(modifier.Second!.Name, "a", "and whose condition comes second");
    }

    public void Test_WhileAndUntilBecomeTheirOwnNodes()
    {
        AssertEq(One("while a do b end").Kind, RubyNodeKind.While, "while is a while");
        AssertEq(One("until a do b end").Kind, RubyNodeKind.Until, "until is an until");
        AssertEq(One("a while b").Kind, RubyNodeKind.While, "a trailing while too");
    }

    public void Test_DefRecordsTheNameAndItsParameters()
    {
        var node = One("def draw(x, y)\n  x\nend");
        AssertEq(node.Kind, RubyNodeKind.Def, "a def is a def node");
        AssertEq(node.Name, "draw", "naming the method");
        AssertEq(node.First!.Children.Count, 2, "with two parameters");
        AssertEq(node.First!.Children[0].Name, "x", "the first is x");
        AssertEq(node.First!.Children[1].Name, "y", "the second is y");
    }

    public void Test_ClassAndModuleRecordTheirName()
    {
        var classNode = One("class Foo\n  def bar\n  end\nend");
        AssertEq(classNode.Kind, RubyNodeKind.Class, "a class is a class node");
        AssertEq(classNode.Name, "Foo", "named Foo");
        AssertEq(classNode.Children.Count, 1, "with one thing inside");

        var moduleNode = One("module Bar\nend");
        AssertEq(moduleNode.Kind, RubyNodeKind.Module, "a module is a module node");
        AssertEq(moduleNode.Name, "Bar", "named Bar");
    }

    public void Test_ARangeIsItsOwnNodeAndMayBeOpenOnEitherSide()
    {
        var closed = One("1..2");
        AssertEq(closed.Kind, RubyNodeKind.Range, "a range is a range node");
        AssertEq(closed.Operator, "..", "with two dots");
        AssertEq(closed.Children.Count, 2, "holding both ends");
        AssertEq(closed.First!.Integer!.Value, 1L, "the start is one");
        AssertEq(closed.Second!.Integer!.Value, 2L, "and the end is two");

        // An endless range is legal, so the missing end is a nil node rather
        // than a failure and rather than a guess.
        var endless = One("1..");
        AssertEq(endless.Kind, RubyNodeKind.Range, "an open ended range is still a range");
        AssertEq(endless.Second!.Kind, RubyNodeKind.Nil, "with a nil end");

        var exclusive = One("1...2");
        AssertEq(exclusive.Operator, "...", "three dots is the exclusive form");
    }

    public void Test_ReturnCarriesAValueOnlyWhenOneFollowsIt()
    {
        var withValue = One("return 1");
        AssertEq(withValue.Kind, RubyNodeKind.Return, "a return is a return");
        AssertEq(withValue.Children.Count, 1, "with a value");

        var bare = One("return");
        AssertEq(bare.Kind, RubyNodeKind.Return, "a bare return is still a return");
        AssertEq(bare.Children.Count, 0, "with no value");
    }

    public void Test_NotIsItsOwnNodeRatherThanAnOperator()
    {
        // 'not' is a keyword with its own level in the grammar, and folding it
        // into a unary minus would lose which one the source wrote.
        var node = One("not a");
        AssertEq(node.Kind, RubyNodeKind.Not, "'not' is a not node");
        AssertEq(node.First!.Name, "a", "of a");
        AssertEq(One("!a").Kind, RubyNodeKind.Unary, "while '!' is a unary operator");
    }

    public void Test_SeveralStatementsBecomeSeveralNodes()
    {
        var statements = Parse("a\nb\nc");
        AssertEq(statements.Count, 3, "three lines are three statements");
        AssertEq(statements[0].Name, "a", "the first is a");
        AssertEq(statements[2].Name, "c", "the third is c");
    }

    public void Test_ASemicolonAlsoSeparatesStatements()
    {
        var statements = Parse("a; b");
        AssertEq(statements.Count, 2, "a semicolon separates like a newline");
    }

    public void Test_AnEmptyScriptHasNoStatements()
    {
        AssertEq(Parse("").Count, 0, "an empty script has none");
        AssertEq(Parse("\n\n").Count, 0, "and neither does one that is only blank lines");
    }

    public void Test_ABlockBecomesANodeHoldingItsBody()
    {
        var node = One("items.each do |item|\n  item\nend");
        AssertEq(node.Kind, RubyNodeKind.Block, "a do block is a block node");
        AssertEq(node.First!.Kind, RubyNodeKind.Call, "whose receiver call comes first");
        AssertEq(node.Second!.Children.Count, 1, "and the body holds one statement");
    }

    public void Test_AnUnexpectedTokenIsRefusedWithItsLine()
    {
        var error = Refusal(() => Parse("a = ]"));
        AssertTrue(error.Length > 0, "a bracket where a value belongs is refused");
    }

    public void Test_AnUnclosedBlockIsRefused()
    {
        var error = Refusal(() => Parse("def a\n  1\n"));
        AssertTrue(error.Contains("end"), $"a def without its end is refused: {error}");
    }

    public void Test_AnUnclosedArrayIsRefused()
    {
        var error = Refusal(() => Parse("[1, 2"));
        AssertTrue(error.Contains("never closed"), $"an array without its bracket: {error}");
    }

    public void Test_ANodeCarriesTheLineItStartedOn()
    {
        var statements = Parse("a\nb = 1");
        AssertEq(statements.Count, 2, "a newline separates two statements");
        AssertEq(statements[0].Line, 1, "the first is on line one");
        AssertEq(statements[1].Line, 2, "and the second on line two");
    }

    /// <summary>
    /// A name on one line and a value on the next are two statements, and not
    /// a call with the second as its argument.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the rule the bracketless branch nearly broke.</strong>
    /// The parser skips newlines before it decides whether a name has
    /// arguments, and with that skip in place `a` and `b = 1` became one
    /// statement: the call `a` with an assignment as its argument.
    /// </para>
    /// <para>
    /// <strong>The line count alone did not catch it.</strong>
    /// <c>Test_ANodeCarriesTheLineItStartedOn</c> read two statements and two
    /// lines, and it stayed green through that — <strong>because the
    /// statement was the wrong one and the count was still two.</strong>
    /// The mutation that removed the guard survived until this test, and it
    /// is here because **a test that checks how many statements there are
    /// does not check which statements they are.**
    /// </para>
    /// </remarks>
    public void Test_ABareNameDoesNotTakeTheNextLineAsItsArgument()
    {
        var statements = Parse("a\nb = 1");
        AssertEq(statements[0].Kind, RubyNodeKind.Identifier,
            "**the first statement is a bare name** — and a reader that made "
                + "it a call would have taken the second line as its "
                + "argument, and a game that writes a name and then a value "
                + "would have lost both");
        AssertEq(statements[0].Name, "a", "and it is the name that was written");
        AssertEq(statements[1].Kind, RubyNodeKind.Assignment,
            "**and the second is still an assignment** — the newline ended the "
                + "first statement, which is what a newline is for");
    }

    public void Test_PowerBindsTighterThanEveryArithmeticOperator()
    {
        // The grammar gives '**' a level of its own, above multiplication. Folded
        // in with the rest of the arithmetic, `a * b ** c` would become
        // `(a * b) ** c` and a game would compute a different number from the
        // same line.
        var node = One("a * b ** c");
        AssertEq(node.Operator, "*", "the multiplication is the looser of the two");
        AssertEq(node.First!.Name, "a", "with a alone on the left");
        AssertEq(node.Second!.Kind, RubyNodeKind.Binary, "and the power on the right");
        AssertEq(node.Second!.Operator, "**", "which is the power");
        AssertEq(node.Second!.First!.Name, "b", "of b");
        AssertEq(node.Second!.Second!.Name, "c", "and c");
    }

    public void Test_ARelationChainAcrossTheTwoIsLeftAssociative()
    {
        // The same shared level seen from the other side: `a < b == c` leans
        // left, and a reader that read the two as different levels would put the
        // equality on top instead.
        var node = One("a < b == c");
        AssertEq(node.Operator, "==", "the chain leans left, so the equality is on top");
        AssertEq(node.First!.Kind, RubyNodeKind.Binary, "with the relation on its left");
        AssertEq(node.First!.Operator, "<", "which is the less than");
        AssertEq(node.First!.First!.Name, "a", "starting at a");
        AssertEq(node.First!.Second!.Name, "b", "and b");
        AssertEq(node.Second!.Name, "c", "and c on the top's right");
    }

    public void Test_LogicalOperatorsBindLooserThanEveryComparison()
    {
        // The grammar puts the logical pair at the two loosest binary levels, so
        // a comparison is entirely inside the condition and a logical and is
        // never part of a comparison.
        var and = One("a < b && c > d");
        AssertEq(and.Operator, "&&", "the logical and is the loosest of the three");
        AssertEq(and.First!.Operator, "<", "with the first comparison on its left");
        AssertEq(and.Second!.Operator, ">", "and the second on its right");

        var or = One("a == b || c == d");
        AssertEq(or.Operator, "||", "the logical or is looser still than the and");
        AssertEq(or.First!.Operator, "==", "with a comparison on its left");
        AssertEq(or.Second!.Operator, "==", "and one on its right");
    }

    public void Test_AndBindsTighterThanOr()
    {
        // Both are on their own level, with the and the tighter of the two, so a
        // mix of them groups the and first.
        var node = One("a || b && c");
        AssertEq(node.Operator, "||", "the or is the looser of the two");
        AssertEq(node.First!.Name, "a", "with a on its left");
        AssertEq(node.Second!.Operator, "&&", "and the and on its right");
    }

    public void Test_ABlockRecordsTheParametersItWasGiven()
    {
        // A block that names its parameters is a different shape from one that
        // names none, and a parser that dropped the names would read a file that
        // Ruby reads as a block over one value rather than over several.
        var node = One("items.each do |item, index|\n  item\nend");
        AssertEq(node.Kind, RubyNodeKind.Block, "the whole is a block");
        AssertEq(node.Children.Count, 3, "with a receiver, a parameter list and a body");
        var parameters = node.Children[1];
        AssertEq(parameters.Kind, RubyNodeKind.Array, "the parameters are a list");
        AssertEq(parameters.Children.Count, 2, "with two of them");
        AssertEq(parameters.Children[0].Name, "item", "the first is item");
        AssertEq(parameters.Children[1].Name, "index", "the second is index");

        var bare = One("items.each do\n  1\nend");
        AssertEq(bare.Kind, RubyNodeKind.Block, "a block with no parameters is still a block");
        AssertEq(bare.Children[1].Children.Count, 0, "with an empty parameter list");
    }

    public void Test_ARelationshipLevelChainIsLeftAssociative()
    {
        // A chain at one level leans left, so `a - b - c` is `(a - b) - c`. The
        // shape is what says so, since the result of a different grouping looks
        // like a plausible number either way.
        var node = One("a < b < c");
        AssertEq(node.Operator, "<", "the top is a relation");
        AssertEq(node.First!.Kind, RubyNodeKind.Binary, "whose left is another relation");
        AssertEq(node.First!.First!.Name, "a", "starting at a");
        AssertEq(node.Second!.Name, "c", "and c on the right of the top");
    }

    public void Test_ARelationAndAnEqualityShareOneLevel()
    {
        // The grammar declares the two on separate lines but resolves them with
        // `rel_expr %prec tCMP`, which says a relation sits at the same level as
        // a comparison. A chain across the two is therefore left associative,
        // which is what this checks.
        var node = One("a == b < c");
        AssertEq(node.Operator, "<", "the chain leans left, so the top is the relation");
        AssertEq(node.First!.Kind, RubyNodeKind.Binary, "with the equality on its left");
        AssertEq(node.First!.Operator, "==", "which is the equality");
        AssertEq(node.First!.First!.Name, "a", "of a");
        AssertEq(node.First!.Second!.Name, "b", "and b");
        AssertEq(node.Second!.Name, "c", "and c on the top's right");
    }

    public void Test_TheLogicalPairSitsAboveEveryComparison()
    {
        // The grammar puts '||' and '&&' at the two loosest binary levels, below
        // the assignment and below every comparison. A parser that put them
    // anywhere else would read a game's guard clause as something else.
        var node = One("a < b || c > d && e == f");
        AssertEq(node.Operator, "||", "the loosest logical level is at the top");
        AssertEq(node.First!.Operator, "<", "with a relation on its left");
        var and = node.Second!;
        AssertEq(and.Operator, "&&", "and the tighter logical level on its right");
        AssertEq(and.First!.Operator, ">", "holding a relation");
        AssertEq(and.Second!.Operator, "==", "and an equality");
    }

    public void Test_NotNegatesTheWholeAssignmentLevelNotJustAValue()
    {
        // The grammar gives 'not' a level of its own, between the logical pair
        // and the assignment. Read as a unary operator it would bind tighter than
        // every operator in the table, and `not a == b` would become
        // `(not a) == b` instead of `not (a == b)`.
        var node = One("not a == b");
        AssertEq(node.Kind, RubyNodeKind.Not, "the whole is a not node");
        AssertEq(node.First!.Kind, RubyNodeKind.Binary, "over the comparison");
        AssertEq(node.First!.Operator, "==", "which is the comparison");
        AssertEq(node.First!.First!.Name, "a", "of a");
        AssertEq(node.First!.Second!.Name, "b", "and b");
    }

    public void Test_NotStillBindsLooserThanTheLogicalPair()
    {
        // The one level above 'not' is the logical pair, so `a || not b` is the
        // or with a negation on its right rather than a negation over the or.
        var node = One("a || not b");
        AssertEq(node.Operator, "||", "the or is at the top");
        AssertEq(node.First!.Name, "a", "with a on its left");
        AssertEq(node.Second!.Kind, RubyNodeKind.Not, "and the not on its right");
    }

    public void Test_ANegationReachesTheAssignmentItGuards()
    {
        // 'not' sits above the assignment, so a negated assignment negates the
        // whole assignment. This is the shape the grammar describes and it is
        // the one a reader who treated 'not' as a unary would get wrong.
        var node = One("not a = b");
        AssertEq(node.Kind, RubyNodeKind.Not, "the whole is a not node");
        AssertEq(node.First!.Kind, RubyNodeKind.Assignment, "over the assignment");
        AssertEq(node.First!.First!.Name, "a", "of a");
        AssertEq(node.First!.Second!.Name, "b", "to b");
    }

    public void Test_TheLogicalPairSitsAboveEveryBitwiseOperator()
    {
        // The grammar puts the logical pair at the two loosest binary levels,
        // above the bitwise ones. Nothing else in the suite crosses that
        // boundary, so a reader with the pair buried among the bitwise levels
        // would pass every other test here.
        var node = One("a | b && c");
        AssertEq(node.Operator, "&&", "the logical and is the looser of the two");
        AssertEq(node.First!.Operator, "|", "with the bitwise or on its left");
        AssertEq(node.First!.First!.Name, "a", "of a");
        AssertEq(node.First!.Second!.Name, "b", "and b");
        AssertEq(node.Second!.Name, "c", "and c on the right");

        var or = One("a & b || c");
        AssertEq(or.Operator, "||", "the logical or is looser than the and");
        AssertEq(or.First!.Operator, "&", "with the bitwise and on its left");
    }

    public void Test_ParsingRunsNothingAndResolvesNothing()
    {
        // The promise this parser makes, checked rather than asserted in a
        // comment. A game calls a method and the system method it names does not
        // exist, and the source also names something that would read or write a
        // file. Parsing both has to succeed, produce the same shapes, and touch
        // nothing. If this parser ever resolved or ran anything, the shapes
        // would differ or the call would have happened.
        const string Source = "Runtime.exec(\"rm -rf /\", File.read(\"secrets\"))";

        RubyNode first;
        RubyNode second;
        try
        {
            first = One(Source);
            // Parsed a second time, in the same process, after the first parse
            // has already walked the same tokens.
            second = One(Source);
        }
        catch (Exception exception)
        {
            AssertTrue(false, $"the parser must accept a call it cannot resolve: {exception.Message}");
            return;
        }

        AssertEq(first.Kind, RubyNodeKind.Call, "the outer call is recorded as written");
        AssertEq(first.Name, "exec", "naming the method, not checking it");
        AssertEq(first.First!.Name, "Runtime", "with the receiver as written");
        AssertEq(first.Children.Count, 3, "and the two arguments");
        AssertEq(first.Children[2].Kind, RubyNodeKind.Call, "the nested call is a node");
        AssertEq(first.Children[2].Name, "read", "naming its method too");
        AssertEq(first.Children[2].First!.Name, "File", "with its own receiver as written");

        // Two parses of the same source agree, which is what data rather than
        // execution looks like: a call that ran would have produced a second
        // result that depended on the first.
        AssertEq(first.Describe(), second.Describe(), "two parses of one source agree");
    }

    public void Test_ANodeCarriesOnlyWhatWasWrittenAndNothingAboutIt()
    {
        // A call node records a name and its receiver as text. It does not carry
        // a resolved target, a class, an arity or a source location inside the
        // game, because none of those are known from the text alone and a
        // reader that invented one would be guessing.
        var node = One("Actor.new(1)");
        AssertEq(node.Kind, RubyNodeKind.Call, "the call is a call");
        AssertEq(node.Name, "new", "naming the method as written");
        AssertEq(node.First!.Name, "Actor", "and the receiver as written");
        AssertEq(node.Realm, null, "with no realm or module attached to it");
        AssertEq(node.Scope, null, "and no scope either");
        AssertEq(node.Receiver, null, "and no resolved receiver");
    }

    public void Test_EachChildSaysWhatItIsFor()
    {
        // The reason the roles exist. A consumer of the tree has to ask for the
        // test rather than know that an `if` holds the keyword first and the
        // test second, and that a ternary holds the test first, and that a block
        // on a call holds the call, the parameters and the body. Those layouts
        // disagree with each other, and a reader that learned the wrong one would
        // run the test as the true branch and pass a test written the same way.
        //
        // A name is held in Name and not in Text. That is worth stating here
        // because a test that reads Text would find null and could be fixed
        // either by filling Text or by reading Name, and only one of those is
        // right.
        var conditional = One("a if b");
        var keyword = conditional;
        AssertEq(keyword.Kind, RubyNodeKind.If, "the keyword opens the test, but was " + keyword.Kind + " name " + (keyword.Name ?? "null") + " children " + keyword.Children.Count);
        var test = keyword.Part(RubyNodeRole.Condition)!;
        AssertEq(test.Kind, RubyNodeKind.Identifier, "the test is a name");
        AssertEq(test.Name, "b", "which is the name written after the keyword");

        var ternary = One("a ? b : c");
        AssertEq(ternary.Kind, RubyNodeKind.Ternary, "a ternary is a ternary");
        AssertEq(ternary.Part(RubyNodeRole.Condition)!.Name, "a", "the test is named as the test");
        AssertEq(ternary.Part(RubyNodeRole.WhenTrue)!.Name, "b", "the true branch as the true branch");
        AssertEq(ternary.Part(RubyNodeRole.WhenFalse)!.Name, "c", "the false branch as the false branch");
    }

    public void Test_AnOperationAndAnAssignmentNameTheirTwoSides()
    {
        var sum = One("a + b");
        AssertEq(sum.Kind, RubyNodeKind.Binary, "an operation is an operation");
        AssertEq(sum.Operator, "+", "holding the operator written");
        AssertEq(sum.Part(RubyNodeRole.Left)!.Name, "a", "the left named as the left");
        AssertEq(sum.Part(RubyNodeRole.Right)!.Name, "b", "and the right as the right");

        var assignment = One("a = b");
        AssertEq(assignment.Kind, RubyNodeKind.Assignment, "an assignment is an assignment");
        AssertEq(assignment.Part(RubyNodeRole.Target)!.Name, "a", "the target is what is written to");
        AssertEq(assignment.Part(RubyNodeRole.Value)!.Name, "b", "and the value is what is written");
    }

    public void Test_TheOrderIsKeptAsWellAsTheRoles()
    {
        // Roles are an addition, not a replacement. A reader that wants the order
        // and does not care what the order means still has it, and the two lists
        // describe the same children.
        var assignment = One("a = b");
        AssertEq(assignment.Children.Count, 2, "two children as before");
        AssertEq(assignment.Children[0].Name, "a", "the target comes first");
        AssertEq(assignment.Children[1].Name, "b", "and the value second");
        AssertEq(assignment.Role_Children.Count, 2, "both children carry a role");
    }

    public void Test_ARoleThatIsNotThereIsAbsentRatherThanTheFirstChild()
    {
        // The fault the roles exist to prevent, stated as a test. A lookup that
        // answered with the first child when the role was not there would let a
        // consumer run a name as if it were the body, and the mistake would look
        // like a value rather than like a mistake.
        var assignment = One("a = b");
        AssertTrue(assignment.Role_Children.Count > 0, "the node does carry roles");
        AssertTrue(
            assignment.Part(RubyNodeRole.Condition) is null,
            "an assignment has no test, and says so");
        AssertTrue(
            assignment.PartsOf(RubyNodeRole.WhenTrue).Count == 0,
            "and no true branch either");

        // A node that was never given roles at all must not answer for any of
        // them, or a consumer cannot tell an absent role from a missing one.
        var bare = new RubyNode { Kind = RubyNodeKind.Integer, Line = 1, Integer = 1 };
        AssertTrue(
            bare.Role_Children.Count == 0,
            "a node with no roles has none listed");
        AssertTrue(bare.Part(RubyNodeRole.Body) is null, "and answers nothing");
    }

    public void Test_TheRoleLookupKeepsTheOrderItWasGiven()
    {
        // A node may hold several children under one role, as a call holds
        // several arguments, and a lookup that took the last would hand back a
        // different one than a lookup that takes the first whenever the caller
        // asked for the head of the list.
        var call = One("a.b(c, d)");
        AssertEq(call.Kind, RubyNodeKind.Call, "a call is a call");
        AssertEq(call.Name, "b", "holding the name it is called by");
        AssertEq(
            call.Part(RubyNodeRole.Receiver)!.Name, "a",
            "and the name it is called on");
        var arguments = call.PartsOf(RubyNodeRole.Argument);
        AssertEq(arguments.Count, 2, "with two arguments");
        AssertEq(arguments[0].Name, "c", "the first written first");
        AssertEq(arguments[1].Name, "d", "and the second after it");
    }

    public void Test_TheHeadOfARoleIsTheFirstOfItAndNotTheLast()
    {
        // A call holds several children under one role, and a caller that asks
        // for the head of a role wants the first, because that is the first
        // argument as written. A lookup that took the last would pass every test
        // above it, since nothing there had two of the same role, and would hand
        // a game the wrong argument.
        var call = One("a.b(c, d)");
        AssertEq(
            call.Part(RubyNodeRole.Argument)!.Name, "c",
            "the head of the arguments is the first one written");
        AssertEq(
            call.PartsOf(RubyNodeRole.Argument).Last().Name, "d",
            "while the list still ends with the last");
    }

    public void Test_APrecedenceChainMixesLevelsWithoutLosingAny()
    {
        // One expression that touches most of the table at once. A reader with
        // any level in the wrong place gets a different tree and nothing about
        // the result looks wrong, so the shape is spelled out level by level.
        //
        //   (a + b * c - d / e << f) == ((g & h) | i)
        //
        // The comparison is the loosest operator in it, the shift is on its left
        // and the bit or is on its right, because the grammar puts '==' above the
        // ranges and above the bitwise levels and puts '<<' below '|'.
        var node = One("a + b * c - d / e << f == g & h | i");
        AssertEq(node.Operator, "==", "the comparison is the loosest thing in it");
        AssertEq(node.Second!.Operator, "|", "the bitwise or is on its right");
        AssertEq(node.Second!.First!.Operator, "&", "with the and under the or");
        AssertEq(node.Second!.Second!.Name, "i", "and i on the or's right");

        var shift = node.First!;
        AssertEq(shift.Operator, "<<", "the shift is on the left of the comparison");
        AssertEq(shift.Second!.Name, "f", "with f on its right");
        var difference = shift.First!;
        AssertEq(difference.Operator, "-", "the subtraction is under the shift");
        AssertEq(difference.Second!.Operator, "/", "with the division on its right");
        AssertEq(difference.Second!.First!.Name, "d", "the division divides d");
        AssertEq(difference.Second!.Second!.Name, "e", "by e");
        var sum = difference.First!;
        AssertEq(sum.Operator, "+", "and the addition is on the subtraction's left");
        AssertEq(sum.First!.Name, "a", "starting at a");
        AssertEq(sum.Second!.Operator, "*", "with the product under it");
        AssertEq(sum.Second!.First!.Name, "b", "of b");
        AssertEq(sum.Second!.Second!.Name, "c", "and c");
    }

    /// <summary>
    /// A name in an argument list is a key only when a colon says so.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A ternary has a colon too.</strong> <c>f(a ? b : c)</c> has a
    /// name and a colon in it, and <strong>the colon is not next to the
    /// name</strong> — a reader that only looked for a colon anywhere in the
    /// argument would have read the <c>b</c> of a conditional as a key,
    /// <strong>and a game that passes a conditional as an argument would have
    /// had its second half read as a named value.</strong>
    /// </para>
    /// <para>
    /// <strong>And a constant is a key.</strong> <c>Sprite: 1</c> is ordinary
    /// Ruby, and <strong>a reader that only knew lower-case names would have
    /// made every named argument with a class name a syntax error.</strong>
    /// </para>
    /// </remarks>
    public void Test_ANameIsAKeyOnlyWhenAColonSaysSo()
    {
        var miternaer = One("f(a ? b : c)");
        var dasArgument = miternaer.Role_Children!
            .First(p => p.Role == RubyNodeRole.Argument);
        AssertEq(dasArgument.Node.Kind, RubyNodeKind.Ternary,
            "**a conditional is a ternary and not a pair** — the count was "
                + "one either way, and it is the kind that says whether the "
                + "colon was read as a key; a reader that looked for a colon "
                + "anywhere would have made this a pair with the value `b : c`");

        var klasse = One("f(Sprite: 1)");
        AssertEq(klasse.Role_Children!.Count(p => p.Role == RubyNodeRole.Argument), 1,
            "**and a constant is a key like any other name** — a reader that "
                + "only knew lower-case names would have made every named "
                + "argument with a class name a syntax error");
    }
}
