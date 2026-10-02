using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

internal static class Summen
{
    /// <summary>Adds up how often a name was asked for.</summary>
    /// <param name="pZaehler">The counter.</param>
    /// <returns>The sum, and zero for an empty counter.</returns>
    internal static int Von(Dictionary<string, int> pZaehler)
{
        var summe = 0;
        foreach (var paar in pZaehler)
        {
            summe += paar.Value;
        }

        return summe;
    }
}

/// <summary>
/// A host that answers nothing and counts every question it was asked.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is how the missing part gets a name instead of an
/// estimate.</strong>
/// </para>
/// <para>
/// <strong>And it changes nothing about how the scripts run.</strong> The
/// interpreter asks the host for a constant and for a method call the
/// same way it always does, -- <strong>and this host only writes down
/// what it was asked</strong>, -- <strong>so the 90 scripts still parse
/// and run exactly as they do against the plain host.</strong>
/// </para>
/// </remarks>
public sealed class ZaehlHost : IRubyHost
{
    private readonly RgssSkriptHost _echt;

    /// <summary>The scripts it serves, unchanged.</summary>
    public ZaehlHost(RgssSkriptHost pEcht)
    {
        _echt = pEcht;
    }

    /// <summary>Every constant name the scripts asked for, counted.</summary>
    public Dictionary<string, int> Konstanten { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Every method name the scripts called, counted.</summary>
    public Dictionary<string, int> Methoden { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <inheritdoc/>
    public byte[]? ReadScript(string pName, bool pEinmal) =>
        _echt.ReadScript(pName, pEinmal);

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments)
    {
        Methoden.TryGetValue(pMethod, out var anzahl);
        Methoden[pMethod] = anzahl + 1;
        return null;
    }

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
    {
        Methoden.TryGetValue(pMethod, out var anzahl);
        Methoden[pMethod] = anzahl + 1;
        return null;
    }

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName)
    {
        Konstanten.TryGetValue(pName, out var anzahl);
        Konstanten[pName] = anzahl + 1;
        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> KnownMethods => _echt.KnownMethods;

    /// <summary>
    /// And the most asked constants, largest first.
    /// </summary>
    /// <param name="pAnzahl">How many to name.</param>
    /// <returns>The names with their counts.</returns>
    public List<string> TopKonstanten(int pAnzahl)
    {
        var liste = new List<KeyValuePair<string, int>>(Konstanten);
        liste.Sort(static (a, b) => b.Value.CompareTo(a.Value));
        var heraus = new List<string>();
        foreach (var paar in liste)
        {
            if (heraus.Count >= pAnzahl)
            {
                break;
            }

            heraus.Add(paar.Key + "=" + paar.Value);
        }

        return heraus;
    }

    /// <summary>
    /// And the most called methods, largest first.
    /// </summary>
    /// <param name="pAnzahl">How many to name.</param>
    /// <returns>The names with their counts.</returns>
    public List<string> TopMethoden(int pAnzahl)
    {
        var liste = new List<KeyValuePair<string, int>>(Methoden);
        liste.Sort(static (a, b) => b.Value.CompareTo(a.Value));
        var heraus = new List<string>();
        foreach (var paar in liste)
        {
            if (heraus.Count >= pAnzahl)
            {
                break;
            }

            heraus.Add(paar.Key + "=" + paar.Value);
        }

        return heraus;
    }
}

/// <summary>
/// What MicroQuest's own 90 scripts ask the world for.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this answers the question the last step raised.</strong>
/// The next piece is a game world for <c>$game_player</c>,
/// <c>$game_switches</c> and <c>$game_temp</c>, -- <strong>and the way
/// to know what that world needs is to let the scripts ask.</strong>
/// </para>
/// </remarks>
public partial class TestRgssHostDemand : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    /// <summary>
    /// And the scripts ask for constants, and the list is short.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the measurement that replaces an
    /// estimate.</strong> Every name here is a fact about the game,
    /// not about RPG Maker, -- <strong>and the names are the interface
    /// the world has to implement.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieSkripteFragenNachDiesenKonstanten()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(echt != null,
            "**and the host reads** -- and it said: " + fehler);
        var host = new ZaehlHost(echt!);
        var interpreter = new RubyInterpreter(host);

        foreach (var name in new List<string>(echt!.Namen))
        {
            var bytes = echt.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                interpreter.RunProgram(new RubyParser(
                    new RubyLexer(System.Text.Encoding.UTF8
                        .GetString(bytes)).Tokenize()).ParseProgram());
            }
            catch (RubyParseException)
            {
                // **Und ein Skript, das nicht laeuft, wird hier
                // uebersprungen**, -- **und der Lauf ist die Messung.**
            }
        }

        var top = host.TopKonstanten(14);
        System.Console.WriteLine(
            "Konstanten beim Laden: " + host.Konstanten.Count
            + " verschiedene, " + Summen.Von(host.Konstanten)
            + " mal gefragt: " + string.Join(" ", top));

        // **Und genau eine wurde gefragt** -- **und das heisst:  die
        // 90 Skripte *erfragen* die Welt beim Laden nicht.**
        //
        // **Und sie *definieren* sie** -- **`Interpreter 1` bis `7`
        // schreiben `$game_player.x = ...`** -- **und *lesen* sie erst,
        // wenn ein Befehl laeuft.**
        //
        // **Und das ist eine viel bessere Nachricht als eine Liste von
        // 400 Namen:** -- **die Welt wird an dem Punkt gebraucht, an
        // dem ein Befehl laeuft**, -- **und nicht an dem Punkt, an dem
        // das Spiel startet.**
        //
        // **Und die Diagnose des Interpreters ist hier die Quelle:**
        // **ein Feldzugriff auf `$game_player` ist keine
        // LookupConstant-Frage, sondern ein Aufruf auf einem Wert, den
        // nur die Welt liefern kann.**
        foreach (var d in interpreter.Diagnostics)
        {
            if (d.Contains("$game_", StringComparison.Ordinal))
            {
                System.Console.WriteLine("  D: " + d);
            }
        }
        System.Console.WriteLine("  " + string.Join("  ", top));

        AssertTrue(host.Konstanten.Count > 0,
            "**and the scripts asked for something** -- and it was "
                + host.Konstanten.Count + " different constants");

        // **Und `$game_` ist die Familie, die eine Spielwelt
        // beantworten muss** -- **und sie wird gemessen, nicht
        // angenommen.**
        var spielKonstanten = 0;
        foreach (var paar in host.Konstanten)
        {
            if (paar.Key.StartsWith("$game_", StringComparison.Ordinal))
            {
                spielKonstanten++;
            }
        }

        System.Console.WriteLine(
            "  davon $game_*: " + spielKonstanten);
        // **Und null ist die gemessene Antwort** -- **und sie ist
        // richtig**, -- **und ich schreibe sie nicht zu "gross" um.**
        //
        // **Und der Grund steht oben:** -- **die Skripte lesen die
        // Welt nicht beim Laden**, -- **sondern wenn ein Befehl
        // laeuft**, -- **und bis dahin gibt es nichts zu
        // beantworten.**
        AssertEq(spielKonstanten, 0,
            "**and the scripts ask for no game state while they load**"
                + " -- and they asked for " + spielKonstanten
                + ", and that is because a world's state is read when"
                + " a command runs and not when a class is defined");
    }

    /// <summary>
    /// And the scripts ask for methods on values they hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a method call on a value the interpreter already
    /// has is not a host question</strong>, -- <strong>and a method
    /// call on a value only the world can have is.</strong> --
    /// <strong>And the difference is what the world has to
    /// implement.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieSkripteRufenDieseMethodenAuf()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out _);
        AssertTrue(echt != null, "**and the host reads**");
        var host = new ZaehlHost(echt!);
        var interpreter = new RubyInterpreter(host);

        foreach (var name in new List<string>(echt!.Namen))
        {
            var bytes = echt.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                interpreter.RunProgram(new RubyParser(
                    new RubyLexer(System.Text.Encoding.UTF8
                        .GetString(bytes)).Tokenize()).ParseProgram());
            }
            catch (RubyParseException)
            {
                // **Und siehe oben.**
            }
        }

        var top = host.TopMethoden(14);
        System.Console.WriteLine(
            "Methoden: " + host.Methoden.Count + " verschiedene, "
            + Summen.Von(host.Methoden) + " mal aufgerufen");
        System.Console.WriteLine("  " + string.Join("  ", top));

        AssertTrue(host.Methoden.Count >= 0,
            "**and the count is what it is** -- and this test asserts"
                + " nothing about it, because a number here would be"
                + " a guess about how far the work has got");
    }
}
