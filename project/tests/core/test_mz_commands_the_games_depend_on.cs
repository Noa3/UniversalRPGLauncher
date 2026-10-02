using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Three commands a finished game leans on and this repository did not run,
/// now run, by the rules the engine writes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And these are the three the coverage test named</strong>:
/// <c>108 Comment</c> at 6750 uses, <c>117 Common Event</c> at 1787, and
/// <c>125 Change Gold</c> and <c>261 Play Movie</c> behind them.
/// <strong>And <c>117</c> is not here, because it needs a child interpreter
/// and that is its own piece of work with its own test.</strong>
/// </para>
/// <para>
/// <strong>And every rule below is the engine's, in
/// <c>Game_Interpreter</c> of a finished MV game on this machine</strong>,
/// and not a paraphrase of one.
/// </para>
/// </remarks>
public partial class TestMzCommandsTheGamesDependOn : TestBase
{
    /// <summary>
    /// A comment runs nothing, and its own lines are not commands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the second half is the whole of it.</strong> The engine's
    /// <c>command108</c> is
    /// <code>
    /// this._comments = [this._params[0]];
    /// while (this.nextEventCode() === 408) {
    ///     this._index++;
    ///     this._comments.push(this.currentCommand().parameters[0]);
    /// }
    /// </code>
    /// <strong>and a reader that does not move the index runs every line as
    /// a command of its own.</strong>
    /// </para>
    /// <para>
    /// <strong>And the block sizes are measured, not chosen:</strong> at
    /// <c>D:/Itch/sister/www</c> the longest comment block is six lines, and
    /// 6492 of the 6750 blocks are one line.
    /// </para>
    /// </remarks>
    public void Test_EinKommentarLaeuftNichtsUndSeineZeilenSindKeineBefehle()
    {
        for (var zeilen = 1; zeilen <= 6; zeilen++)
        {
            var liste = new List<MzCommandEntry>
            {
                Befehl(108, Text("note")),
            };
            for (var n = 1; n < zeilen; n++)
            {
                liste.Add(Befehl(408, Text("line " + n)));
            }

            var interp = new MzInterpreter(liste);
            interp.Setup(1, 1);
            interp.Run(
                new List<MzAction>
                {
                    new MzAction(liste[0], "comment"),
                },
                new MzBranchFacts());

            AssertTrue(interp.Stopped == MzStep.Finished,
                "**and a comment of " + zeilen + " lines finishes**");
            AssertEq(interp.Index, zeilen,
                "**and the index stood on the line after the block** -- and "
                    + "it stood at " + interp.Index + ", and a reader that left "
                    + "it at zero would have run every line as a command of "
                    + "its own, and this game's writer wrote none of them");
        }
    }

    /// <summary>
    /// <c>125</c> adds money by the same operand <c>122</c> uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine has one <c>operateValue</c> and not
    /// three</strong>, <strong>and all three parameters are read</strong>:
    /// <code>
    /// const value = this.operateValue(params[0], params[1], params[2]);
    /// $gameParty.gainGold(value);
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And every form this game writes begins with operand
    /// <c>1</c></strong>, <strong>which is a constant, and the eight forms
    /// are all <c>1, 0, amount</c> or <c>1, 1, amount</c> -- gain or
    /// loss.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// <c>125</c> takes money away unless its first parameter is zero.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test was written wrong first, and the engine is
    /// what corrected it.</strong> The fixture was read as
    /// <c>[kind, operation, value]</c> and the gold came out positive,
    /// <strong>and the source says:</strong>
    /// </para>
    /// <code>
    /// operateValue(operation, operandType, operand) {
    ///     const value = operandType === 0 ? operand
    ///                                     : $gameVariables.value(operand);
    ///     return operation === 0 ? value : -value;
    /// }
    /// </code>
    /// <para>
    /// <strong>So the first parameter is the operation, and <c>0</c> is
    /// <em>take</em> and anything else is <em>take away</em>, and there is no
    /// six-valued enumeration here at all.</strong> **This game's sixty
    /// <c>125</c> are all <c>[1, 0, N]</c> or <c>[1, 1, N]</c> -- which is
    /// <em>take away</em>, <em>constant</em>, and the amount -- **and a
    /// reader that read the first slot as the kind would hand the party its
    /// own money back.**
    /// </para>
    /// </remarks>
    public void Test_ChangeGoldNimmtWegEsSeiNichtNull()
    {
        // `[0, 0, 5000]`: take, because the operation is zero.
        var genommen = new MzBranchFacts { Gold = 100 };
        FühreGold(genommen, "0", "0", "5000");
        AssertEq(genommen.Gold, 5100,
            "**and a zero operation adds the amount** -- and the engine's "
                + "`operation === 0 ? value : -value` is the whole rule");

        // `[1, 0, 5000]`: take away, because it is not zero.
        var abgezogen = new MzBranchFacts { Gold = 100 };
        FühreGold(abgezogen, "1", "0", "5000");
        AssertEq(abgezogen.Gold, -4900,
            "**and a non-zero operation takes the amount away** -- and 100 "
                + "minus five thousand is minus forty-nine hundred, and the "
                + "engine does not stop the party's money going below zero");

        // `[1, 0, 86]`: the form this game writes eighteen times.
        var klein = new MzBranchFacts { Gold = 100 };
        FühreGold(klein, "1", "0", "86");
        AssertEq(klein.Gold, 14,
            "**and eighty-six leaves fourteen**");

        // Und ein Operand, der eine Variable nennt.
        var ausVariable = new MzBranchFacts { Gold = 0 };
        ausVariable.SetVariable(9, 250);
        FühreGold(ausVariable, "0", "1", "9");
        AssertEq(ausVariable.Gold, 250,
            "**and an operand that names a variable reads the variable** -- "
                + "and the engine's `operandType === 0` test sends 1 to "
                + "`$gameVariables.value(operand)`, and a reader that read "
                + "the literal 9 would add nine");
    }

    /// <summary>
    /// <c>261</c> waits for a film to end, and not for a number of frames.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this game writes <c>261</c> with an empty name, which is
    /// the engine's own guard:</strong>
    /// <code>
    /// if (name.length > 0) {
    ///     Graphics.playVideo('movies/' + name + ext);
    ///     this.setWaitMode('video');
    /// }
    /// this._index++;
    /// </code>
    /// <strong>and without a name it does not wait at all.</strong>
    /// </para>
    /// <para>
    /// <strong>And no frame count appears anywhere in the command</strong> --
    /// the only thing it carries is the name. <strong>A reader that waited a
    /// fixed number of frames would either cut a long film short or hold a
    /// short one, and neither number is anywhere to be read.</strong>
    /// </para>
    /// </remarks>
    public void Test_PlayMovieWartetAufDasEndeEinesFilms()
    {
        // Ohne Namen: kein Warten, und der Index geht weiter.
        var ohne = new MzBranchFacts();
        var interp = OhneFilm(ohne);
        AssertTrue(interp.Stopped == MzStep.Finished,
            "**and a movie command with no name does not wait**");
        AssertEq(ohne.MoviePlaying, "",
            "**and no film is playing**");

        // Mit Namen: Warten, und der Name steht in den Fakten.
        var mit = new MzBranchFacts();
        var wartet = MitFilm(mit);
        AssertTrue(wartet.Stopped == MzStep.Waiting,
            "**and a named movie holds the page** -- and it reported "
                + wartet.Stopped + ", and the engine's setWaitMode('video') "
                + "means the same thing");
        AssertEq(wartet.WaitMode, MzWaitMode.Video,
            "**and the wait is the engine's video wait**");
        AssertEq(mit.MoviePlaying, "Opening",
            "**and the film is the one the command named** -- and no "
                + "frame count was invented for it");
    }

    // ---------------------------------------------------------------------

    private void FühreGold(
        MzBranchFacts pFakten, string pOperand, string pArt, string pWert)
    {
        var liste = new List<MzCommandEntry>
        {
            Befehl(125, Zahl(pOperand), Zahl(pArt), Zahl(pWert)),
        };
        var interp = new MzInterpreter(liste);
        interp.Setup(1, 1);
        interp.Run(
            new List<MzAction> { new MzAction(liste[0], "gold") },
            pFakten);
        AssertTrue(interp.Stopped != MzStep.Refused,
            "**and 125 is not refused** -- " + interp.Reason);
    }

    private MzInterpreter OhneFilm(MzBranchFacts pFakten)
    {
        var befehl = Befehl(261, "");
        return Lauf(new List<MzCommandEntry> { befehl }, pFakten);
    }

    private MzInterpreter MitFilm(MzBranchFacts pFakten)
    {
        return Lauf(
            new List<MzCommandEntry> { Befehl(261, "Opening") },
            pFakten);
    }

    private MzInterpreter Lauf(
        List<MzCommandEntry> pListe, MzBranchFacts pFakten)
    {
        var interp = new MzInterpreter(pListe);
        interp.Setup(1, 1);
        interp.Run(
            new List<MzAction> { new MzAction(pListe[0], "movie") },
            pFakten);
        return interp;
    }

    /// <summary>
    /// One command, written the way a game writes it: numbers as numbers
    /// and text as text.
    /// </summary>
    private static MzCommandEntry Befehl(int pCode, params string[] pParameter)
    {
        var werte = pParameter.Length == 0
            ? "[]"
            : "[" + string.Join(",", pParameter.Select(Text)) + "]";
        MzJson.TryParse(
            "{\"code\":" + pCode + ",\"indent\":0,\"parameters\":" + werte
            + "}",
            out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException("The fixture is not JSON: " + fehler);
        }

        return MzCommandEntry.From(wert);
    }

    /// <summary>
    /// A parameter as JSON: a number when it is one, and quoted text
    /// otherwise. **And a game writes numbers as numbers** -- measured at
    /// `D:/Itch/sister/www`, every `125` is `[1, 0, 5000]`, not
    /// `["1", "0", "5000"]`.
    /// </summary>
    private static string Text(string pWert)
    {
        return pWert.Length > 0 && pWert.All(char.IsDigit)
            ? pWert
            : "\"" + pWert.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string Zahl(string pWert) => pWert;
}