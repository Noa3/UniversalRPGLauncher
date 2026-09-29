using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What an object is asked about itself: its fields, its identity, and the
/// method it is running.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the second line of most VX plugins.</strong>
/// <c>obj.instance_variable_get(:@hp)</c> and <c>obj.send(:refresh)</c>,
/// <strong>and a reader that had no answer for either would stop at the
/// plugin's own first line</strong> — and a plugin that reads a state it
/// set itself would see nil.
/// </para>
/// <para>
/// <strong>Measured before: nine of ten questions were refused.</strong>
/// <c>A.new.send(:gruessen)</c> said <em>A has no method 'send' on this
/// host</em>, **and <c>__method__</c> answered nil without a word** — which
/// is the worst shape a refusal can take, because the caller cannot tell it
/// from a method that returned nothing.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A field that is set is read back, and the name carries its `@`.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the store is the object's own and not a copy.</strong>
    /// <c>set</c> then <c>get</c> is the same number,
    /// **and a reader that answered from a table of its own would give a
    /// number the object does not have</strong> — and a plugin that writes a
    /// field would then read nil.
    /// </para>
    /// <para>
    /// <strong>And <c>:hp</c> and <c>:@hp</c> are the same name.</strong>
    /// **And a reader that demanded the <c>@</c> would say *no such
    /// variable* for the spelling a game writes most**, **and every
    /// <c>get</c>/<c>set</c> pair would fail for half its callers.**
    /// </para>
    /// <para>
    /// <strong>And a field that is not there is nil and not an
    /// error.</strong> That is how a plugin reads a state it sets later.
    /// </para>
    /// </remarks>
    public void Test_AFieldIsSetAndReadBackOnTheSameObject()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def initialize\n"
            + "    @hp = 10\n"
            + "  end\n"
            + "end\n"
            + "h = Held.new\n"
            + "h.instance_variable_set(:@mp, 20)\n"
            + "[h.instance_variable_get(:@hp), h.instance_variable_get(:mp),"
            + " h.instance_variable_get(:@mp),"
            + " h.instance_variable_get(:@gibtsnicht),"
            + " h.instance_variable_defined?(:@hp),"
            + " h.instance_variable_defined?(:@gibtsnicht)]\n"));

        AssertEq(wert.Items[0].Integer, 10,
            "**the field the constructor set is there**");
        // **Und gemessen: `get(:mp)` gibt `10`, und `get(:@mp)` gibt `20`,
        // weil `:mp` und `:@hp` verschiedene Felder sind.** Der Test
        // behauptete, `get(:mp)` sei `10`,
        // **und `:mp` ohne `@` bedeutet das Feld `@mp`, und nicht
        // `@hp`** -- **ein Test, der eine Erweiterung behauptet, muss
        // auch die richtige Zahl nennen.**
        AssertEq(wert.Items[1].Integer, 20,
            "**and `:mp` is the field `@mp`** — the reader adds the `@`, and "
                + "a reader that demanded the `@` would say *no such "
                + "variable* for the spelling a game writes most");
        AssertEq(wert.Items[2].Integer, 20,
            "**and what `set` wrote is what `get` reads** — a reader that "
                + "answered from a table of its own would give a number the "
                + "object does not have, and a plugin that writes a field "
                + "would then read nil");
        AssertTrue(wert.Items[3].Kind == RubyValueKind.Nil,
            "**and a field that is not there is nil and not an error** — "
                + "that is how a plugin reads a state it sets later");
        AssertTrue(wert.Items[4].Kind == RubyValueKind.Boolean
            && wert.Items[4].Boolean,
            "**and `instance_variable_defined?` says yes**");
        AssertTrue(wert.Items[5].Kind == RubyValueKind.Boolean
            && !wert.Items[5].Boolean,
            "**and no for one that is not**");
    }

    /// <summary>
    /// The names come back with their `@`, so writing them back does not
    /// make a second field.
    /// </summary>
    /// <remarks>
    /// <strong>And the names carry the point.</strong> A reader that answered
    /// `:hp</c> would make a game that writes the names back — <c>obj
    /// .instance_variable_set(*name, 1)</c> — create a second field under
    /// another name, **and the field it meant to change would stay as it
    /// was.</strong>
    /// </remarks>
    public void Test_TheFieldNamesComeBackWithTheirPoint()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def initialize\n"
            + "    @hp = 10\n"
            + "    @mp = 20\n"
            + "  end\n"
            + "end\n"
            + "h = Held.new\n"
            + "[h.instance_variables, h.instance_variables[0].to_s,"
            + " h.instance_variables[1].to_s]\n"));

        AssertEq(wert.Items[0].Items.Count, 2,
            "**both fields came back** — measured before: *instance_variables "
                + "takes the name of a field as a symbol*, because the "
                + "reader checked the same argument for all five questions, "
                + "and this is the one that takes none");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "@hp",
            "**and the first name carries its `@`** — a reader that answered "
                + "`:hp` would make `instance_variable_set(*name, 1)` create "
                + "a second field under another name, and the field it meant "
                + "to change would stay as it was");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "@mp",
            "**and so does the second**");
    }

    /// <summary>
    /// `send` calls the method the name stands for, and an operator by its
    /// name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And `send` goes through the same search as a written
    /// name.</strong> That is why it lives in the reader and not at the
    /// host, **and a reader that resolved the name itself would go around
    /// `super` and the receiver** — and a plugin that calls a subclass
    /// method through `send` would run the base class's.
    /// </para>
    /// <para>
    /// <strong>And <c>1.send(:+, 2)</c> is <c>1 + 2</c>.</strong> An
    /// operator is not a method under a name here,
    /// **and a reader that only asked the value's methods would answer
    /// nil** — **and `send` is exactly the sentence a plugin uses when it
    /// applies an operator by its name.**
    /// </para>
    /// </remarks>
    public void Test_SendCallsTheMethodTheNameStandsFor()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def gruessen\n"
            + "    'hi'\n"
            + "  end\n"
            + "end\n"
            + "[Held.new.send(:gruessen), 1.send(:+, 2)]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "hi",
            "**`send` called the method** — measured before: *A has no method "
                + "'send' on this host*, and a plugin that calls a method it "
                + "knows only by name would do nothing");
        AssertEq(wert.Items[1].Integer, 3,
            "**and `1.send(:+, 2)` is `1 + 2`** — an operator is not a method "
                + "under a name here, and a reader that only asked the "
                + "value's methods would answer nil, and a plugin that "
                + "applies an operator by its name would quietly do nothing");
    }

    /// <summary>
    /// `__method__` says which method is running.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And written without brackets it is an identifier.</strong>
    /// **A reader that only looked in the script's own table would answer a
    /// local variable of that name** — which is nil —
    /// <strong>and `__method__` would be silent where it is exactly the
    /// thing a method needs to say which one it is.</strong>
    /// </para>
    /// <para>
    /// <strong>And the answer is a symbol.</strong> <c>__method__ == :x</c>
    /// is the sentence a game writes to check which override ran.
    /// </para>
    /// </remarks>
    public void Test_TheMethodSaysWhichOneItIs()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        // **Und beide Wege, denn `__method__` ohne Klammern ist ein
        // Bezeichner und mit Klammern ein Aufruf.** Der Bezeichner ist der
        // Weg, den ein Skript schreibt,
        // **und die ueberlebende Mutation hat nur den Bezeichner
        // abgeschaltet** -- **und mein Test hat bisher nur den Aufruf
        // geprueft**, also genau den nicht.
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def x\n"
            + "    [__method__, __method__()]\n"
            + "  end\n"
            + "end\n"
            + "r = Held.new.x\n"
            + "[r[0], r[0] == :x, r[1]]\n"));

        AssertTrue(wert.Items[0].Kind == RubyValueKind.Symbol,
            "**`__method__` as a name is a symbol** — measured before: nil "
                + "with no diagnostics, which is the worst shape a refusal "
                + "can take, because the caller cannot tell it from a method "
                + "that returned nothing");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Boolean
            && wert.Items[1].Boolean,
            "**and it is the name of the method that is running** — and a "
                + "reader that answered a local variable of that name would "
                + "be nil, and a reader that only had the call would leave "
                + "the name silent, which is the shape a game writes");
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Symbol,
            "**and `__method__()` as a call is the same name** — the two "
                + "spellings are one question, and a reader that had only "
                + "one of them would answer nil for the other without "
                + "saying why");
    }

    /// <summary>
    /// Two objects have two numbers, and the same object has one.
    /// </summary>
    /// <remarks>
    /// <strong>And it is counted, and not made from the address.</strong> A
    /// reader that made the number from the place in memory would give the
    /// same number twice — a collection between two calls would hand it out
    /// again, **and <c>list.uniq</c> would have two different heroes in one
    /// entry.**
    /// </remarks>
    public void Test_TwoObjectsHaveTwoNumbers()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "end\n"
            + "a = Held.new\n"
            + "b = Held.new\n"
            + "[a.object_id == a.object_id, a.object_id == b.object_id]\n"));

        AssertTrue(wert.Items[0].Kind == RubyValueKind.Boolean
            && wert.Items[0].Boolean,
            "**one object keeps its number**");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Boolean
            && !wert.Items[1].Boolean,
            "**and two objects do not share one** — a reader that made the "
                + "number from the place in memory would hand the same one "
                + "out again after a collection, and `list.uniq` would have "
                + "two different heroes in one entry");
    }

    /// <summary>
    /// `instance_of?` is the class itself and not a superclass.
    /// </summary>
    /// <remarks>
    /// <strong>And that is the difference from <c>is_a?</c>.</strong>
    /// A base class instance is not an instance of the subclass,
    /// **and a reader that made the two the same would let a game add a
    /// subclass instance to a list of base instances** — and the list would
    /// answer a method the item does not have.
    /// </remarks>
    public void Test_InstanceOfIsTheClassAndNotTheSuperclass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "end\n"
            + "class Sub < Basis\n"
            + "end\n"
            + "b = Basis.new\n"
            + "s = Sub.new\n"
            + "[s.instance_of?(Sub), s.instance_of?(Basis),"
            + " b.instance_of?(Sub), s.is_a?(Basis)]\n"));

        AssertTrue(wert.Items[0].Kind == RubyValueKind.Boolean
            && wert.Items[0].Boolean,
            "**an object is an instance of its own class**");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Boolean
            && !wert.Items[1].Boolean,
            "**and not of a superclass** — a reader that made the two the "
                + "same would let a game add a subclass instance to a list "
                + "of base instances, and the list would answer a method "
                + "the item does not have");
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Boolean
            && !wert.Items[2].Boolean,
            "**and a base instance is not an instance of the subclass**");
        AssertTrue(wert.Items[3].Kind == RubyValueKind.Boolean
            && wert.Items[3].Boolean,
            "**while `is_a?` walks the chain** — that is the difference "
                + "between the two words");
    }
}
