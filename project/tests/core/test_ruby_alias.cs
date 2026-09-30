using System.Collections.Generic;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `alias`, which is a second name for a method and not a second method.
/// </summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// An alias is callable, and both names run the same body.
    /// </summary>
    /// <remarks>
    /// <strong>An alias is a second door into the same room.</strong> A
    /// reader that stored a second name and looked the body up under it would
    /// have given the game's override nothing to override — <strong>and
    /// `super` from the new name would have gone to the old one</strong>,
    /// which is the bug a rename causes and nobody reports because it only
    /// shows up in a game that patches itself.
    /// </remarks>
    public void Test_AnAliasIsCallableAndBothNamesRunTheSameBody()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def alt\n"
            + "    7\n"
            + "  end\n"
            + "  alias neu alt\n"
            + "end\n"));

        var alt = mit.RunProgram(Statements("A.new.alt\n"));
        var neu = new RubyInterpreter(new RubyNullHost());
        var neuwert = neu.RunProgram(Statements(
            "class A\n"
            + "  def alt\n"
            + "    7\n"
            + "  end\n"
            + "  alias neu alt\n"
            + "end\n"
            + "A.new.neu\n"));

        AssertEq(AsInteger(alt), 7, "**the old name answers seven**");
        AssertEq(AsInteger(neuwert), 7,
            "**and the new one answers the same** — one body under two names, "
                + "and a reader that copied the name instead of the method "
                + "would have given the new name nothing to run");
        AssertEq(AsInteger(mit.RunProgram(Statements("1\n"))), 1,
            "**and the first program ended cleanly** — the alias did not run "
                + "the method's body at the moment it was written");
    }

    /// <summary>
    /// A later `def` under the old name does not change the alias, and that
    /// is Ruby's own rule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Verified against <c>ruby/ruby</c>'s <c>vm_method.c</c> and not
    /// against my first guess.</strong> <c>rb_alias</c> stores a
    /// <c>VM_METHOD_TYPE_ALIAS</c> entry holding
    /// <c>body.alias.original_me</c> — the method entry as it was when the
    /// alias ran. A later <c>def alt</c> writes <c>Methods["alt"]</c> and
    /// <strong>leaves the alias's entry alone.</strong>
    /// </para>
    /// <para>
    /// <strong>The first version of this test asserted the opposite</strong>
    /// and called a copy a share. The implementation was right and the test
    /// was wrong, **and the difference is exactly the one a game that
    /// renames and then overrides depends on**: a subclass's
    /// <c>alias</c> plus its own <c>def</c> under the old name is a
    /// deliberate way to keep the old behaviour reachable.
    /// </para>
    /// </remarks>
    public void Test_ALaterDefDoesNotChangeTheAlias()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def alt\n"
            + "    1\n"
            + "  end\n"
            + "  alias neu alt\n"
            + "  def alt\n"
            + "    2\n"
            + "  end\n"
            + "end\n"
            + "A.new.neu\n"));

        AssertEq(AsInteger(wert), 1,
            "**the new name still answers one** — the alias holds the method "
                + "entry it was given, and a later `def alt` writes a new entry "
                + "under the old name only. This is `vm_method.c`'s "
                + "`VM_METHOD_TYPE_ALIAS` holding `body.alias.original_me`, and "
                + "it is what a game that renames and then overrides relies on");
    }
    /// <summary>
    /// An alias resolves the chain, so renaming twice works.
    /// </summary>
    /// <remarks>
    /// <strong>`alias b a` where `a` is itself an alias has to find the
    /// method behind both.</strong> A reader that only looked in the class's
    /// own table would have found nothing for the second alias — <strong>and
    /// a game that renames something twice is not a rare thing to write.</strong>
    /// </remarks>
    public void Test_AnAliasOfAnAliasResolvesTheChain()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def eins\n"
            + "    5\n"
            + "  end\n"
            + "  alias zwei eins\n"
            + "  alias drei zwei\n"
            + "end\n"
            + "A.new.drei\n"));

        AssertEq(AsInteger(wert), 5,
            "**the third name answers five** — each alias resolved the one "
                + "before it, and a reader that looked only in the class's own "
                + "table would have found nothing for `drei`");
    }

    /// <summary>
    /// The colon spelling of `alias` names the same two things.
    /// </summary>
    /// <remarks>
    /// <strong>`alias :neu :alt` and `alias neu alt` are one
    /// instruction.</strong> A reader that only took the bare word would have
    /// kept the colon in the new name — <strong>and then `obj.neu` would have
    /// looked for a method whose name begins with a colon</strong>, and found
    /// nothing, in a game that writes this form on purpose.
    /// </remarks>
    public void Test_TheColonSpellingNamesTheSameTwoThings()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def alt\n"
            + "    9\n"
            + "  end\n"
            + "  alias :neu :alt\n"
            + "end\n"
            + "A.new.neu\n"));

        AssertEq(AsInteger(wert), 9,
            "**the new name answers nine** — the colons are not part of the "
                + "names, and a reader that kept them would have looked for a "
                + "method called `:neu`");
    }

    /// <summary>
    /// An alias of a method that is not there names both names.
    /// </summary>
    /// <remarks>
    /// <strong>An alias points at an existing method and does not make
    /// one.</strong> Ruby would raise `NameError` — <strong>and a reader that
    /// made a stub would have given the game a method that always answers
    /// nil</strong>, which is a bug that only shows up as a character who
    /// cannot do the one thing they were renamed for.
    /// </remarks>
    public void Test_AnAliasOfAMethodThatIsNotThereNamesBothNames()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  alias neu fehlt\n"
            + "end\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("does not have fehlt")
                && d.Contains("alias neu has nothing to point at"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "**and it names both names and says what is "
                + "wrong** — the diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
        AssertTrue(mit.FindMethod("A", "neu") == null,
            "**and no stub was made** — a reader that made one would have "
                + "given the game a method that always answers nil");
    }

    /// <summary>
    /// An alias outside a class is a diagnostic.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby would put it on `Object`</strong> — and this interpreter
    /// files methods under a class, so the message says which thing would
    /// have to provide that. **A reader that invented a root would have put a
    /// game's top-level alias in a place no game asks for.</strong>
    /// </remarks>
    public void Test_AnAliasOutsideAClassIsADiagnostic()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        RubyValue wert;
        try
        {
            wert = mit.RunProgram(Statements("def a\n 1\nend\nalias b a\n"));
        }
        catch (System.Exception e)
        {
            AssertTrue(false, "the run threw: " + e + "; diagnostics: "
                + string.Join(" | ", mit.Diagnostics));
            return;
        }

        AssertTrue(wert.IsNil, "**and the alias answered nil**");
        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("outside a class"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "**and it says so** — the diagnostics were: "
            + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// An alias in a subclass holds the base's method, and that is where a
    /// name pointer and a method pointer come apart.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the case that kills the mutation the other tests let
    /// through.</strong> Two mutations of the same line — store the method
    /// under the new name, or store what the class's own table holds under
    /// the old name — are indistinguishable while both names are in the same
    /// class, because there the table's entry and the method are the same
    /// object.
    /// </para>
    /// <para>
    /// <strong>In a subclass the method comes from the base and the table
    /// has no entry for the old name at all.</strong> A reader that stored
    /// the name pointer would have found nothing and made no alias —
    /// <strong>and a game that aliases a method it inherited and then
    /// overrides it would have had no alias to override</strong>, which is
    /// the one thing a game's plugin layer does.
    /// </para>
    /// </remarks>
    public void Test_AnAliasInASubclassHoldsTheBasesMethod()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def rechnen(a)\n"
            + "    a * 2\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "  alias rechnen_alt rechnen\n"
            + "end\n"
            + "Erbe.new.rechnen_alt(21)\n"));

        AssertEq(AsInteger(wert), 42,
            "**the alias reaches the base's method** — the subclass's own table "
                + "has no entry for the old name, so a reader that stored a "
                + "pointer to the name would have made no alias at all, and a "
                + "game that aliases an inherited method and then overrides it "
                + "would have had nothing to override");
    }
}
