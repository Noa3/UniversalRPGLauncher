using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The one-line <c>for</c> that VX writes and this reader did not
/// parse.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the source is quoted out of Random Dungeon's own
/// <c>Game_Interpreter</c>, line 145</strong>, --
/// <strong>measured by <c>TestRgssVxElseProbe</c>:</strong>
/// </para>
/// <code>
/// if param == 0       # 全体
///   for actor in $game_party.members do yield actor end
/// else                # 単体
/// </code>
/// <para>
/// <strong>And the failure is measured too:</strong>
/// <c>'else' at offset 5721 does not begin an expression</c>.
/// </para>
/// </remarks>
public partial class TestRubyForDo : TestBase
{
    private static string? Parst(string pQuelle)
    {
        try
        {
            var knoten = new RubyParser(
                new RubyLexer(pQuelle).Tokenize()).ParseProgram();
            return knoten.Count + " Anweisungen";
        }
        catch (RubyParseException ausnahme)
        {
            return ausnahme.Message;
        }
    }

    /// <summary>
    /// And the one-line form from VX's own script.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a `for` with <c>do ... end</c> on one line is
    /// ordinary Ruby</strong>, -- <strong>and RPG Maker VX's standard
    /// library uses it</strong>, -- <strong>and a reader that cannot
    /// parse it cannot load the interpreter of any VX
    /// game.</strong>
    /// </para>
    /// </remarks>
    public void Test_ForMitDoUndEndInEinerZeile()
    {
        const string Einzeiler =
            "for actor in $game_party.members do yield actor end\n";
        const string Mehrzeilig =
            "for actor in $game_party.members\n  yield actor\nend\n";

        var a = Parst(Einzeiler);
        var b = Parst(Mehrzeilig);
        System.Console.WriteLine(
            "einzeilig: " + a + " | mehrzeilig: " + b);

        AssertTrue(b != null && !b.Contains("was expected"),
            "**and the multi-line form parses** -- and it gives: " + b);
        AssertTrue(a != null && !a.Contains("was expected"),
            "**and the one-line form parses too** -- and it gives: "
                + a + ", and that line is `for actor in"
                + " $game_party.members do yield actor end` out of"
                + " Random Dungeon's own Game_Interpreter");
    }

    /// <summary>
    /// And the whole method VX writes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the four lines around it, verbatim</strong>,
    /// -- <strong>and if this parses then the failure further down is
    /// a different one</strong>, -- <strong>and if it does not, then
    /// this method is where the fix goes.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieGanzeMethodeAusVx()
    {
        // **Und Zeilen 143 bis 151 aus Random Dungeons
        // `Game_Interpreter`:**
        //
        // ```ruby
        // def iterate_actor_id(param)
        //   if param == 0       # 全体
        //     for actor in $game_party.members do yield actor end
        //   else                # 単体
        //     actor = $game_actors[param]
        //     yield actor unless actor == nil
        //   end
        // end
        // ```
        const string Ganze = @"
def iterate_actor_id(param)
  if param == 0       # alle
    for actor in $game_party.members do yield actor end
  else                # einzeln
    actor = $game_actors[param]
    yield actor unless actor == nil
  end
end
";
        var wert = Parst(Ganze);
        System.Console.WriteLine("Ganze Methode: " + wert);

        AssertTrue(wert != null && !wert.Contains("was expected"),
            "**and the whole method VX writes parses** -- and it"
                + " gives: " + wert);
    }
}
