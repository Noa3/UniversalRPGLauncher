using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A game can invent a constant at the moment it is first named.
/// </summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `const_missing` is a method the game wrote, and not a hook.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is a method on the module, and not on this
    /// reader.</strong> Ruby 1.8.1 does
    /// <c>rb_funcall(klass, "const_missing", 1, ID2SYM(id))</c> —
    /// <c>variable.c</c> line 1120 — **and `klass` is the module the
    /// name was written on.**
    /// </para>
    /// <para>
    /// <strong>And this is how a game invents a class.</strong>
    /// <c>module RPG; module Actors; end; end</c> runs through this in
    /// every VX project, **and measured before this: nil, and the
    /// diagnostic said *the constant GIBT_ES_NICHT is not defined by
    /// this host, and the interpreter does not guess; a game's own
    /// constant needs a host that provides it*** — **and that message
    /// asks the host for something the game itself has to write.**
    /// </para>
    /// <para>
    /// <strong>And the name arrives as a symbol</strong> —
    /// <c>ID2SYM(id)</c> — **and a game that writes
    /// <c>def self.const_missing(n)</c> and then <c>n.to_s</c> gets the
    /// name it asked for.**
    /// </para>
    /// </remarks>
    public void Test_AConstantIsInventedAtTheMomentItIsNamed()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements(
            "class Object\n"
            + "  def self.const_missing(n)\n"
            + "    7\n"
            + "  end\n"
            + "end\n"
            + "GIBT_ES_NICHT\n"));

        AssertEq(wert.Integer, 7,
            "**`GIBT_ES_NICHT` is seven** -- measured before this: nil, "
                + "and the diagnostic said the constant is not defined by "
                + "this host and that a game's own constant needs a host "
                + "that provides it, and that asks the host for something "
                + "the game itself has to write");
        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));

        // **Und der Name kommt als Symbol an.**
        var name = new RubyInterpreter(new RubyNullHost());
        var geantwortet = name.RunProgram(Statements(
            "class Object\n"
            + "  def self.const_missing(n)\n"
            + "    n.to_s\n"
            + "  end\n"
            + "end\n"
            + "IRGEND_WAS\n"));
        AssertTrue(geantwortet.Kind == RubyValueKind.String,
            "**and the answer is a text** -- because the game turned the "
                + "name into one, and `ID2SYM(id)` is what Ruby hands it");
        AssertTrue(geantwortet.Kind == RubyValueKind.String,
            "**and the answer is a text** -- because the game turned the "
                + "name into one, and `ID2SYM(id)` is what Ruby hands it");
        AssertEq(
            System.Text.Encoding.GetEncoding(932).GetString(geantwortet.Bytes),
            "IRGEND_WAS",
            "**and the name it got is the one it asked for** -- "
                + "measured: a reader that hands `const_missing` some "
                + "builds a class per name build the wrong class, and "
                + "nothing says so**");

        // **Und `const_missing` laeuft die Kette nach oben.** Ruby sucht
        // es an dem Modul, auf das geschrieben wurde, **und ein Spiel
        // kann es in ein Modul schreiben, das es einbindet** -- **und
        // `class Object; include Macher; end` ist der Weg, mit dem ein
        // Plugin seine Klassen fuer alle bereitstellt.**
        var eingebunden = new RubyInterpreter(new RubyNullHost());
        var erfunden = eingebunden.RunProgram(Statements(
            "module Macher\n"
            + "  def self.const_missing(n)\n"
            + "    5\n"
            + "  end\n"
            + "end\n"
            + "class Object\n"
            + "  include Macher\n"
            + "end\n"
            + "UEBER_DEN_WEG\n"));
        AssertEq(erfunden.Integer, 5,
            "**and a `const_missing` in an included module answers** "
                + "-- 5, and a reader that looked only at the class "
                + "name the host for it");
    }

    /// <summary>
    /// A name the reader and the game both lack is still named in the
    /// message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a reader that guessed would break the rest.</strong>
    /// Every constant of every RPG Maker script is a name, **and a
    /// reader that made one up would produce a plugin that loads and then
    /// fails somewhere else.**
    /// </para>
    /// <para>
    /// <strong>And the message names the thing.</strong> `include Fehlt`
    /// needs the name, **and a diagnostic that does not name it leaves
    /// the reader guessing.**
    /// </para>
    /// </remarks>
    public void Test_AConstantNobodyHasIsStillNamed()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements("GIBT_ES_NICHT\n"));

        AssertEq(wert.Kind, RubyValueKind.Nil,
            "**and it is nil** -- the interpreter does not guess, and a "
                + "name it made up would be a plugin that loads and then "
                + "fails somewhere else");
        AssertTrue(
            string.Join(" | ", m.Diagnostics).Contains("GIBT_ES_NICHT"),
            "**and the message names it** -- `include Fehlt` needs the "
                + "name, and a diagnostic without it leaves the reader "
                + "guessing. The diagnostics were: "
                + string.Join(" | ", m.Diagnostics));
    }
}
