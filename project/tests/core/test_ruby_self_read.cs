using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A reader called as <c>self.x</c> from inside a method.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the frame's only failure, cut down to its smallest
/// form.</strong> On a real XP game the engine's own loop reported
/// <c>NoMethodError: undefined operator '&lt;' for a Integer and a Nil</c>,
/// and the source is two lines of the engine's own
/// <c>Window_Selectable</c>: <c>update_cursor_rect</c> compares
/// <c>row &lt; self.top_row</c>, and <c>top_row</c> is
/// <c>self.oy / 32</c>. Measured on that game,
/// <c>Window_Base.new(0, 0, 100, 100).oy</c> answers <b>0</b> and the same
/// window's <c>top_row</c> answers <b>nil</b>.
/// </para>
/// <para>
/// <strong>And the difference is where the call comes from.</strong> Called
/// from outside the class the receiver is the object, and the reader
/// answers its field. Called as <c>self.oy</c> from inside a method the
/// answer is nil -- <strong>and a game that draws its own windows reads
/// those fields on every frame.</strong>
/// </para>
/// <para>
/// <strong>And this test is deliberately not about the tree.</strong> A
/// tree is how the reader works, and a test of the tree would pass while
/// the game still crashed; the assertions below are the answers a game asks
/// for.
/// </para>
/// </remarks>
public partial class TestRubySelfRead : TestBase
{
    /// <summary>
    /// A reader answers the same value from outside the class and from
    /// inside a method.
    /// </summary>
    public void Test_SelfReaderVonInnenUndAussen()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());

        // **And the class is the shape the engine's own windows have**: a
        // field, an `attr_accessor` over it, and a method that reads it
        // through `self.`
        interpreter.RunProgram(new RubyParser(new RubyLexer(
            "class Pruef\n"
            + "  attr_accessor :oy, :breite\n"
            + "  def initialize\n"
            + "    @oy = 64\n"
            + "    @breite = 64\n"
            + "  end\n"
            + "  def top_row\n"
            + "    return self.oy / 32\n"
            + "  end\n"
            + "  def breite_innen\n"
            + "    return self.breite / 32\n"
            + "  end\n"
            + "end\n"
            + "$pruef = Pruef.new\n"
            + "$aussen = $pruef.oy\n"
            + "$breite_aussen = $pruef.breite\n"
            + "$breite_innen = $pruef.breite_innen\n"
            + "$innen = $pruef.top_row\n")
            .Tokenize()).ParseProgram());

        AssertEq(interpreter.Global("$aussen").Integer, 64,
            "**and a reader called on the object answers its field** -- and it"
                + " answered " + interpreter.Global("$aussen").Integer
                + ", and a reader that cannot do this much is not a reader");

        AssertEq(interpreter.Global("$breite_aussen").Integer, 64,
            "**and a second reader answers from outside as well** -- and it"
                + " answered " + interpreter.Global("$breite_aussen").Integer);

        AssertEq(interpreter.Global("$breite_innen").Integer, 2,
            "**and the same second reader called as `self.breite` from inside a"
                + " method answers it too** -- and it answered "
                + (interpreter.Global("$breite_innen").IsNil
                    ? "nil"
                    : interpreter.Global("$breite_innen").Integer.ToString())
                + ", and a reader whose name is not the one the game uses"
                + " answers the same way");

        // **And this is the assertion the game fails.** Measured on a real XP
        // game: `self.oy / 32` inside the method answered nil, the comparison
        // after it raised, and the engine's own loop stopped.
        AssertEq(interpreter.Global("$innen").Integer, 2,
            "**and the reader the engine's own windows call answers the same"
                + " field from inside a method** -- and it answered "
                + (interpreter.Global("$innen").IsNil
                    ? "nil"
                    : interpreter.Global("$innen").Integer.ToString())
                + ", and nil / 32 is a silent nil, so the comparison that"
                + " follows it is the frame's only failure");
    }
}
