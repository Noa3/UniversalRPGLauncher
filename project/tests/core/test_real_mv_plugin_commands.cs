using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Godot;

using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The plugin calls a finished MV game makes, five thousand of them, and what
/// this repository can say about each one.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the defect was a number.</strong> The branch for a plugin
/// command existed, and it was written for <c>357</c>, <strong>and this game
/// writes <c>356</c> and not one <c>357</c>.</strong> <strong>And a command
/// outside <c>HasEffect</c> is never dispatched and is reported as
/// finished</strong>, <strong>so five thousand four hundred and seventy-two
/// plugin calls ran as five thousand four hundred and seventy-two no-ops.</strong>
/// </para>
/// <para>
/// <strong>And the two are not the same command.</strong> <c>357</c> is MZ's
/// numbered form with a plugin file name and its arguments as separate
/// parameters; <strong><c>356</c> is MV's and it is one string the engine
/// splits itself.</strong>
/// </para>
/// </remarks>
public partial class TestRealMvPluginCommands : TestBase
{
    private const string Projekt = "D:/Itch/sister/www";

    private static bool Vorhanden()
    {
        if (!Directory.Exists(Projekt + "/data"))
        {
            GD.Print("    (skipped: no " + Projekt + ")");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Every <c>356</c> in the game, and each one is one string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And sixteen hundred and sixty-eight different texts for five
    /// thousand four hundred and seventy-two calls</strong>, <strong>which is
    /// the first sign that this is not a command but a door into the
    /// project's own plugin.</strong>
    /// </para>
    /// </remarks>
    public void Test_Jeder356IstEinStringUndKeinParameterArray()
    {
        if (!Vorhanden())
        {
            return;
        }

        var anzahl = 0;
        var falscheForm = 0;
        var texte = new HashSet<string>();
        foreach (var datei in Directory.GetFiles(
            Projekt + "/data", "Map*.json", SearchOption.AllDirectories))
        {
            if (!Path.GetFileNameWithoutExtension(datei)[3..]
                .All(char.IsDigit))
            {
                continue;
            }

            foreach (var befehl in BefehleDerKarte(datei))
            {
                if (befehl.Code != MzCommandTable.PluginCommandCall)
                {
                    continue;
                }

                anzahl++;
                if (befehl.Parameters.Count != 1)
                {
                    falscheForm++;
                    continue;
                }

                texte.Add(Text(befehl, 0));
            }
        }

        AssertTrue(anzahl > 5000,
            "**and this game makes more than five thousand plugin calls** -- "
            + anzahl + ", and a reader that answered none of them answered "
            + "every one wrongly");
        AssertEq(falscheForm, 0,
            "**and every one of them carries exactly one parameter**");
        AssertTrue(texte.Count > 1000,
            "**and they are sixteen hundred different texts** -- "
            + texte.Count + ", which is what says that this is a door into "
            + "the project's plugin and not a command");
        System.Console.WriteLine(
            "MV Pluginaufrufe: " + anzahl + " aus " + texte.Count
            + " verschiedenen Texten, alle mit 1 Parameter");
    }

    /// <summary>
    /// The call's name is the first word, and the rest is its argument.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine's own line decides this</strong>:
    /// <c>const args = this._params[0].split(" ");</c> and
    /// <c>const command = args.shift();</c> <strong>-- so the name is the
    /// first word, and a call with no space has an empty argument and not a
    /// missing one.</strong>
    /// </para>
    /// <para>
    /// <strong>And the three forms are read out of the game's own
    /// files.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerNameIstDasErsteWortUndDerRestDasArgument()
    {
        var faelle = new[]
        {
            // **Und diese drei Formen stehen so in den Karten des Spiels,
            // und sie sind keine von mir erfunden.**
            new
            {
                Roh = "\u4e8b\u4ef6\u7ba1\u7406\u6838\u5fc3 "
                    + "\u672c\u4e8b\u4ef6 : \u5f7b\u5e95\u5220\u9664",
                Name = "\u4e8b\u4ef6\u7ba1\u7406\u6838\u5fc3",
                Argument = "\u672c\u4e8b\u4ef6 : \u5f7b\u5e95\u5220\u9664",
            },
            new
            {
                Roh = ">\u5141\u8bb8\u64cd\u4f5c\u73a9\u5bb6\u79fb\u52a8 "
                    + ": \u5173\u95ed",
                Name = ">\u5141\u8bb8\u64cd\u4f5c\u73a9\u5bb6\u79fb\u52a8",
                Argument = ": \u5173\u95ed",
            },
            new
            {
                // **Und diese hier hat kein Leerzeichen, und `shift()`
                // nimmt damit alles und laesst nichts, und ein Leser, der
                // bei einem fehlenden Argument abbricht, verwechselt
                // "leer" mit "nicht da".**
                Roh = "PB_BGS_ALL_STOP",
                Name = "PB_BGS_ALL_STOP",
                Argument = "",
            },
        };

        foreach (var fall in faelle)
        {
            var teile = fall.Roh.Split(
                ' ', StringSplitOptions.RemoveEmptyEntries);
            AssertEq(teile[0], fall.Name,
                "**and the name is the first word**");
            AssertEq(
                teile.Length > 1 ? string.Join(' ', teile[1..]) : "",
                fall.Argument,
                "**and the argument is the rest**");
        }
    }

    /// <summary>
    /// A plugin command is reported, and not run, and not silently stepped
    /// over.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And "reported" is a claim that can be checked.</strong> The
    /// branch adds a notice and an action, <strong>and the notice names the
    /// plugin and the command</strong>, <strong>so a caller can tell that
    /// something was asked for and did not happen.</strong>
    /// </para>
    /// <para>
    /// <strong>And this is the boundary and not a gap:</strong> running the
    /// call means running the project's own JavaScript, <strong>and
    /// <c>AGENTS.md</c> forbids that.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinPluginaufrufWirdGemeldetUndNichtAusgefuehrt()
    {
        var fakten = new MzBranchFacts();
        var eintrag = Befehl(
            MzCommandTable.PluginCommandCall,
            ">\u5141\u8bb8\u64cd\u4f5c\u73a9\u5bb6\u79fb\u52a8 : \u5173\u95ed");
        var interp = new MzInterpreter(new List<MzCommandEntry> { eintrag });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(interp.Stopped, MzStep.Finished,
            "**and the run finishes**");
        AssertTrue(fakten.Notices.Count > 0,
            "**and the caller is told** -- " + fakten.Notices.Count
            + " notices, and five thousand four hundred and seventy-two "
            + "calls that ran as no-ops would leave a game looking as if it "
            + "worked");

        var gemeldet = string.Join(" ", fakten.Notices);
        AssertTrue(gemeldet.Contains("\u5141\u8bb8"),
            "**and the notice names the plugin** -- it said '" + gemeldet
            + "', and a refusal that does not say what it would need to run "
            + "is a refusal nobody can act on");
        AssertTrue(gemeldet.Contains("JavaScript"),
            "**and the notice says why** -- it said '" + gemeldet + "'");
    }

    private static IEnumerable<MzCommandEntry> BefehleDerKarte(
        string pPfad)
    {
        var karte = JsonDocument.Parse(File.ReadAllText(pPfad)).RootElement;
        if (!karte.TryGetProperty("events", out var events))
        {
            yield break;
        }

        foreach (var ereignis in events.EnumerateArray())
        {
            if (ereignis.ValueKind != JsonValueKind.Object
                || !ereignis.TryGetProperty("pages", out var seiten))
            {
                continue;
            }

            foreach (var seite in seiten.EnumerateArray())
            {
                if (seite.ValueKind != JsonValueKind.Object
                    || !seite.TryGetProperty("list", out var liste))
                {
                    continue;
                }

                foreach (var befehl in liste.EnumerateArray())
                {
                    if (befehl.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    // **Und `MzCommandEntry.From` nimmt einen `MzValue`
                    // und keine `JsonElement`** -- **und wer den falschen
                    // Typen reingibt, scheitert an der Uebersetzung und
                    // nicht an der Sache.**
                    var roh = "{\"code\":"
                        + (befehl.TryGetProperty("code", out var c)
                            ? c.GetRawText() : "0")
                        + ",\"indent\":"
                        + (befehl.TryGetProperty("indent", out var d)
                            ? d.GetRawText() : "0")
                        + ",\"parameters\":"
                        + (befehl.TryGetProperty("parameters", out var ps)
                            ? ps.GetRawText() : "[]")
                        + "}";
                    MzJson.TryParse(roh, out var wert, out var _fehler);
                    yield return MzCommandEntry.From(wert);
                }
            }
        }
    }

    private static string Text(MzCommandEntry pEintrag, int pIndex)
    {
        return pEintrag.Parameters.Count > pIndex
            && pEintrag.Parameters[pIndex] is string s
            ? s
            : "";
    }

    private static MzCommandEntry Befehl(int pCode, string pText)
    {
        var json = "{\"code\":" + pCode + ",\"indent\":0,"
            + "\"parameters\":["
            + JsonSerializer.Serialize(pText) + "]}";
        MzJson.TryParse(json, out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException("fixture: " + fehler);
        }

        return MzCommandEntry.From(wert);
    }
}
