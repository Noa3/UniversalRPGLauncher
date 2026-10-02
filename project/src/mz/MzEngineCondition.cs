using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UniversalRPG.Web;

/// <summary>
/// Two questions a game's own script asks, and the answers on a desktop.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not a JavaScript interpreter and it is not going to become
/// one.</b> It answers two named questions out of
/// <c>rpg_core.js</c>, and <b>answers nothing else at all</b>: a condition
/// this class does not know is a condition this class refuses, exactly as
/// it did before it existed.
/// </para>
/// <para>
/// <b>And why these two, measured at <c>D:/Itch/sister/www</c>:</b>
///
/// <code>
/// Utils.isOptionValid("test")        118x
/// Utils.isMobileDevice()              50x
/// -- und in den Bedingungen selbst:
/// Utils.isOptionValid("test")         95x
/// !Utils.isOptionValid("test")          6x
/// Utils.isMobileDevice()              32x
/// !Utils.isMobileDevice()              9x
/// -- von 4952 Bedingungen des Typs 12
/// </code>
///
/// <b>So 143 der 4952 Skript-Bedingungen eines fertigen Spiels sind genau
/// diese beiden Abfragen</b>, <b>und 143 sind keine Randzahl.</b>
/// </para>
/// <para>
/// <b>And both live in the engine's own file</b>, <b>not in a plugin and
/// not in the author's own code</b> -- <c>rpg_core.js</c>, and this
/// repository already reads that file for every other rule it follows.
/// <b>Evaluating them is not running the author's script; it is reading the
/// engine's.</b>
/// </para>
/// <para>
/// <b>And both have one answer on a desktop</b>, <b>and it is the same
/// answer every time</b>:
///
/// <code>
/// static isMobileDevice() {
///     const isDesktopApp = typeof require === 'function'
///         &amp;&amp; typeof process === 'object';
///     const isMobile = !isDesktopApp &amp;&amp; (...);
///     Utils.isMobileDevice = () =&gt; isMobile;
///     return isMobile;
/// }
///
/// static isOptionValid(name) {
///     const args = location.search.slice(1);
///     if (args.split("&").includes(name)) { return true; }
///     if (this.isNwjs() &amp;&amp; nw.App.argv.length &gt; 0) {
///         return nw.App.argv[0].split("&").includes(name);
///     }
///     return false;
/// }
/// </code>
///
/// <b>Neither reads a game file, a save, or a variable</b>, <b>and that is
/// what makes an answer here a fact and not a guess.</b>
/// </para>
/// <para>
/// <b>And the option is a launch flag</b> -- <b>the engine's own name for it
/// is the one RPG Maker puts in the URL and on the desktop's command
/// line.</b> <b>A launcher that has not been given one has not been given
/// one</b>, <b>and this repository is not given one.</b>
/// </para>
/// </remarks>
public static class MzEngineCondition
{
    /// <summary>
    /// What a desktop run has, and it is the same on every machine this
    /// repository runs on.
    /// </summary>
    /// <summary>
    /// The three flags this game's plugins read from
    /// <c>ConfigManager</c>, and every one of them is a question about the
    /// platform and not about the game.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And they are the ones this repository answers</strong>,
    /// <strong>because they are decided by whether a file exists</strong>:
    /// </para>
    /// <code>
    /// if (isDLsite || isCien || isFanza) {
    ///     ConfigManager.isJapanesePlatform = true;
    /// }
    /// </code>
    /// <para>
    /// <strong>And <c>isImouto</c> is deliberately not here</strong> --
    /// <strong>because it is also set from a file but is then written to
    /// from game state as well</strong>, <strong>and a flag that can be
    /// either is not a fact about the machine.</strong>
    /// </para>
    /// </remarks>
    public static string[] PlattformFelder { get; } =
    {
        "isJapanesePlatform", "isDLsite", "isCien", "isFanza",
    };

    /// <summary>
    /// Every <c>ConfigManager</c> field this game's plugins set, and a
    /// reader that meets another one says it does not know it.
    /// </summary>
    public static string[] PlatzhalterGesetzt { get; } =
    {
            "isJapanesePlatform", "isDLsite", "isCien", "isFanza",
            "isImouto", "harmonyMode",
        };

    /// <summary>
    /// The folders searched for the placeholder files, and the game does
    /// the same two-step in its own words.
    /// </summary>
    /// <remarks>
    /// <strong>And this reader takes the folders a caller gives it</strong>
    /// <strong>and no others</strong> -- <strong>because a path that
    /// exists is a fact about the disk and a path that is invented is
    /// not.</strong>
    /// </remarks>
    public static string[] PlatzhalterDateien
    {
        get => _platzhalterOrdner;
        set => _platzhalterOrdner = value ?? Array.Empty<string>();
    }

    private static string[] _platzhalterOrdner = Array.Empty<string>();

    /// <summary>
    /// The placeholder file behind one flag, and an empty answer means the
    /// flag is not one this repository can decide.
    /// </summary>
    /// <param name="pFeld">The field's name, without the class.</param>
    /// <returns>
    /// The file name, or an empty string when the flag is written from
    /// game state as well.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>isJapanesePlatform</c> has no file of its
    /// own</strong> -- <strong>it is the or of three:</strong>
    /// </para>
    /// <code>
    /// if (isDLsite || isCien || isFanza) {
    ///     ConfigManager.isJapanesePlatform = true;
    /// }
    /// </code>
    /// <para>
    /// <strong>And a reader that looked for <c>isJapanesePlatform.json</c>
    /// would find nothing</strong>, <strong>and would then answer false
    /// even when the game was running on DLsite.</strong> <strong>And
    /// that is a wrong answer and not a missing one.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>isImouto</c> and <c>harmonyMode</c> are
    /// refused on purpose:</strong>
    /// </para>
    /// <code>
    /// if (isImouto || DrillUp.g_COSt_list[119]["context"] !== "Mio") {
    ///     isImouto = true;
    /// }
    /// </code>
    /// <para>
    /// <strong>So those two read a file and then get written from the
    /// game</strong>, <strong>and a flag that can be either is not a fact
    /// about the machine.</strong>
    /// </para>
    /// </remarks>
    private static string PlatzhalterDateiFuer(string pFeld) => pFeld switch
    {
        "isDLsite" => "DLsite.json",
        "isCien" => "Cien.json",
        "isFanza" => "Fanza.json",
        "isJapanesePlatform" => "DLsite.json",
        _ => "",
    };


    public const bool IsMobileDevice = false;

    /// <summary>
    /// Whether a launch option is set, and this repository is given none.
    /// </summary>
    /// <remarks>
    /// <strong>And a caller's own options can be given in.</strong> <b>A
    /// launch flag is a fact about how the game was started</b>, <b>and a
    /// launcher that knows one is entitled to say so.</b> <b>And the
    /// default is the empty set and not "everything"</b>, <b>because a
    /// reader that answered true to an unknown option would run every test
    /// branch of every game.</b>
    /// </remarks>
    public static IReadOnlyCollection<string> LaunchOptions { get; set; } =
        Array.Empty<string>();

    /// <summary>
    /// The two questions, and what each one comes to here.
    /// </summary>
    /// <param name="pText">The author's expression, as the game wrote it.</param>
    /// <param name="pMissing">
    /// Why it was refused, and empty when it was answered.
    /// </param>
    /// <returns>
    /// Whether the expression is answered, and <c>null</c> when it is not.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>And this answers a question and not an expression.</b> <b>A
    /// condition is answered only when the whole of it is one of these two
    /// questions</b>, <b>with or without a <c>!</c> in front</b>, <b>and
    /// with nothing else in it.</b>
    /// </para>
    /// <para>
    /// <b>And <c>Utils.isOptionValid("test") || $gameVariables.value(25) &gt;
    /// 50</c> is not answered</b>, <b>because half of it is the author's own
    /// script and a reader that answered the first half would be claiming a
    /// result it did not compute.</b> <b>And this game writes exactly that,
    /// once.</b>
    /// </para>
    /// </remarks>
    public static bool? Answer(
        string pText, MzBranchFacts pFacts, out string pMissing)
    {
        pMissing = "";
        var text = (pText ?? "").Trim();
        if (text.Length == 0)
        {
            pMissing = "an empty script condition";
            return null;
        }

        // **Und ein `!` davor ist die einzige Sache, die dieser Leser
        // versteht** -- **und kein `&&`, kein `||` und keine Klammer.**
        var verneint = false;
        while (text.StartsWith("!", StringComparison.Ordinal))
        {
            verneint = !verneint;
            text = text.Substring(1).Trim();
        }

        bool wert;
        if (text == "Utils.isMobileDevice()")
        {
            wert = IsMobileDevice;
        }
        else if (text.StartsWith("ConfigManager.",
            StringComparison.Ordinal))
        {
            // **Und `ConfigManager` ist die Engine-Klasse und nicht der
            // Spielzustand** -- **und dieses Spiel haengt drei eigene
            // Felder an sie an.** **Und alle drei sind keine Frage ueber
            // den Spielstand, sondern ueber die Plattform, auf der das
            // Spiel laeuft.**
            //
            // ```js
            // if (isDLsite)  ConfigManager.isDLsite = true;
            // if (isCien)    ConfigManager.isCien = true;
            // if (isFanza)   ConfigManager.isFanza = true;
            // if (isDLsite || isCien || isFanza) {
            //     ConfigManager.isJapanesePlatform = true;
            // }
            // ```
            //
            // **Und `isDLsite`, `isCien` und `isFanza` kommen aus
            // `DataManager.checkPlaceholderExists("DLsite.json")`** --
            // **das ist eine Dateipruefung und keine Skriptausfuehrung.**
            //
            // ```js
            // DataManager.checkPlaceholderExists = function (fileName) {
            //     if (Utils.isNwjs && Utils.isNwjs()) {
            //         const fs = require('fs');
            //         ...
            //         return Promise.resolve(fs.existsSync(p));
            //     }
            //     ...
            // };
            // ```
            //
            // **Und diese Dateien liegen in `data/`, und eine
            // Desktop-Portierung dieses Spiels hat keine davon** -- **und
            // damit ist `isJapanesePlatform` falsch, und nicht
            // unbekannt.** **Und das ist der Unterschied zwischen
            // verweigert und beantwortet: eine verweigerte Bedingung
            // haelt das Spiel an, und eine beantwortete laesst es
            // weiterlaufen.**
            //
            // **Und gemessen an diesem Spiel: die Dateien `SFW.json`,
            // `DLsite.json`, `Cien.json`, `Fanza.json` und `Imouto.json`
            // existieren keine davon, und `ConfigManager.isJapanesePlatform`
            // wird in vierzig Bedingungen gelesen.**
            var feld = text.Substring("ConfigManager.".Length).Trim();
            if (feld.EndsWith("()", StringComparison.Ordinal))
            {
                pMissing = $"ConfigManager.{feld}, which is a call and not "
                    + "a flag, and this repository does not run the "
                    + "author's JavaScript";
                return null;
            }

            // **Und jedes Feld hat seinen eigenen Dateinamen**, **und
            // `isJapanesePlatform` ist die ODERUNG aus dreien.** **Und
            // meine erste Fassung hat `Any(...)` ueber die Ordner
            // gelaufen**, **und dabei den Ordnernamen als Dateinamen
            // benutzt** -- **und das ist `data/sister`, **und das gibt
            // es nicht**, **und die Antwort war deshalb immer falsch.**
            var datei = PlatzhalterDateiFuer(feld);
            if (datei == "")
            {
                pMissing = $"ConfigManager.{feld}, which this game's "
                    + "plugins also write from game state, so it is not "
                    + "a fact about the machine";
                return null;
            }

            wert = PlatzhalterDateien.Any(ordner =>
                !string.IsNullOrEmpty(ordner)
                && File.Exists(Path.Combine(ordner, "data", datei)));
        }
        else if (text.StartsWith("$gameSelfSwitches.value(",
            StringComparison.Ordinal))
        {
            // **Und `$gameSelfSwitches` ist Spielzustand und keine
            // Maschine** -- **und es ist der haeufigste Ausdruck in den
            // Bedingungen eines fertigen Spiels, und dieser Leser hat
            // ihn.**
            //
            // ```js
            // $gameSelfSwitches.value([$gameMap.mapId(), 22, 'B'])
            // ```
            //
            // **Und gemessen an `D:/Itch/sister/www`: 208mal, und alle
            // mit derselben Form.** **Und der Schluessel besteht aus der
            // Karte, dem Ereignis und einem Buchstaben** -- **denn
            // `command123` schreibt `[this._mapId, this._eventId,
            // params[0]]`, und dieser Leser legt sie unter demselben
            // Namen ab: `karte_ereignis_Buchstabe`.**
            //
            // **Und `$gameMap.mapId()` ist die Karte, auf der der Lauf
            // steht, und `this._eventId` das Ereignis, auf dem er
            // steht** -- **und beide sind Tatsachen ueber den Lauf und
            // keine Rechnung ueber das Spiel.** **Und `this._eventId` ist
            // null in einem gemeinsamen Ereignis**, **und dann fragt der
            // Ausdruck nach einem Schalter, den es nicht gibt.**
            var schluessel = SelfSwitchKey(text, pFacts);
            if (schluessel == null)
            {
                pMissing = "the self switch in '" + text + "', because "
                    + "the run stands on no event and a self switch "
                    + "belongs to an event on a map";
                return null;
            }

            wert = pFacts.SelfSwitches.TryGetValue(schluessel, out var an)
                && an;
        }
        else if (text.StartsWith("localStorage.", StringComparison.Ordinal))
        {
            // **Und `localStorage` ist ein Speicher des Browsers und nicht
            // ein Spielstand** -- **und das Spiel benutzt ihn fuer eine
            // einzige Sache: um sich zu merken, dass es den Steam-Link
            // schon gezeigt hat.**
            //
            // ```js
            // if (!localStorage.getItem("hasShownSteamLink")) {
            //     QJ.MPMZ.tl.steamStorePageAdvertisement?.();
            //     localStorage.setItem("hasShownSteamLink", "true");
            // }
            // ```
            //
            // **Und das ist Werbung und kein Spiel** -- **und sie laeuft
            // auf einer Maschine ohne Steam ueberhaupt nicht**, **weil
            // `?.()` auf `undefined` nichts tut.**
            //
            // **Und die Antwort ist deshalb "der Speicher ist leer",
            // und der Block laeuft, und er tut nichts.** **Und das ist
            // die Antwort der Engine auf einer Desktopmaschine ohne
            // Werbespeicher, und nicht eine, die dieses Repository
            // erfunden hat.**
            wert = false;
        }
        else if (text.StartsWith("Utils.isOptionValid(", StringComparison.Ordinal)
            && text.EndsWith(")", StringComparison.Ordinal))
        {
            var arg = text.Substring(
                "Utils.isOptionValid(".Length,
                text.Length - "Utils.isOptionValid(".Length - 1).Trim();
            if (arg.Length < 2
                || !arg.StartsWith("\"", StringComparison.Ordinal)
                || !arg.EndsWith("\"", StringComparison.Ordinal))
            {
                pMissing = $"Utils.isOptionValid({arg}), whose argument is "
                    + "not one quoted word and this reader will not read "
                    + "anything else out of a launch flag";
                return null;
            }

            // **Und der Name muss ein einziges Wort sein** -- **denn
            // `includes(name)` vergleicht ganze Abschnitte der
            // Argumentliste, und ein Argument mit einem `&&` darin ist
            // kein Abschnitt.** **Und `"a" && "b"` ist in JavaScript
            // `"b"`, und `0 || 2` ist `2`** -- **und dieser Leser rechnet
            // nichts, er liest.**
            //
            // **Und die erste Fassung dieser Pruefung hat nur auf die
            // Anfuehrungszeichen gesehen und `Utils.isOptionValid("a" &&
            // "b")` als die Option `b` beantwortet.**
            var name = arg.Substring(1, arg.Length - 2);
            if (name.Length == 0
                || name.IndexOf('"') >= 0
                || name.IndexOf('&') >= 0
                || name.IndexOf('|') >= 0
                || name.IndexOf(' ') >= 0
                || name.IndexOf('=') >= 0
                || name.IndexOf('\'') >= 0)
            {
                pMissing = $"the option name {name}, which is not a single "
                    + "word and so is not one entry of the launch options "
                    + "list";
                return null;
            }

            wert = IsOptionValid(name);
        }
        else
        {
            pMissing = "the author's own script, and this repository runs no "
                + "JavaScript";
            return null;
        }

        return verneint ? !wert : wert;
    }

    /// <summary>
    /// The same questions without any game state, for a caller that has none.
    /// </summary>
    /// <remarks>
    /// <strong>And this answers only the two machine questions.</strong>
    /// <strong>A condition about a self switch needs to know where the run
    /// stands</strong>, <strong>and a caller that does not know that is
    /// told so rather than answered.</strong>
    /// </remarks>
    public static bool? Answer(string pText, out string pMissing) =>
        Answer(pText, null, out pMissing);

    /// <summary>
    /// The self switch a <c>$gameSelfSwitches.value([...])</c> asks about,
    /// as the key this repository stores it under.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine's key is a list of three and this
    /// repository's is a text</strong>, <strong>and the text is built from
    /// all three</strong> -- <c>karte_ereignis_Buchstabe</c> -- <strong>so
    /// two switches of two events cannot be the same.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>$gameMap.mapId()</c> is the map the run is on and
    /// <c>this._eventId</c> is the event the run is in</strong>, <strong>and
    /// both are facts about where the run stands and not a computation over
    /// the game.</strong> <strong>And <c>this._eventId</c> is
    /// <c>undefined</c> in a common event</strong>, <strong>and a self
    /// switch belongs to an event on a map</strong>, <strong>so there the
    /// answer is that there is none.</strong>
    /// </para>
    /// </remarks>
    private static string? SelfSwitchKey(
        string pText, MzBranchFacts pFacts)
    {
        // `$gameSelfSwitches.value([` ... `])`
        const string praefix = "$gameSelfSwitches.value([";
        if (!pText.StartsWith(praefix, StringComparison.Ordinal)
            || !pText.EndsWith("])", StringComparison.Ordinal))
        {
            return null;
        }

        var inhalt = pText.Substring(
            praefix.Length, pText.Length - praefix.Length - 2);
        var teile = inhalt.Split(',');
        if (teile.Length != 3)
        {
            return null;
        }

        string kartenTeil = teile[0].Trim();
        string ereignisTeil = teile[1].Trim();
        string buchstabeTeil = teile[2].Trim();

        // **Und `$gameMap.mapId()` ist die Karte des Laufs.**
        var kartenId = 0;
        if (kartenTeil == "$gameMap.mapId()")
        {
            kartenId = pFacts?.MapId ?? 0;
        }
        else if (!int.TryParse(kartenTeil, out kartenId))
        {
            return null;
        }

        // **Und `this._eventId` ist null in einem gemeinsamen Ereignis,
        // und das heisst: es gibt keinen Schalter.**
        var ereignisId = 0;
        if (ereignisTeil == "this._eventId")
        {
            ereignisId = pFacts?.EventId ?? 0;
        }
        else if (!int.TryParse(ereignisTeil, out ereignisId))
        {
            return null;
        }

        if (kartenId <= 0 || ereignisId <= 0)
        {
            return null;
        }

        var gross = buchstabeTeil.Trim('\'', '"');
        if (gross.Length != 1 || !char.IsLetter(gross[0]))
        {
            return null;
        }

        return kartenId + "_" + ereignisId + "_"
            + char.ToUpperInvariant(gross[0]);
    }

    /// <summary>
    /// Whether one launch option is set, the engine's own way.
    /// </summary>
    private static bool IsOptionValid(string pName)
    {
        foreach (var gesetzt in LaunchOptions)
        {
            if (string.Equals(gesetzt, pName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// How much of a game's script conditions this class can answer, which is
    /// the number a caller compares against next time.
    /// </summary>
    public const string WasThis = "Utils.isMobileDevice() and "
        + "Utils.isOptionValid(\"name\"), with or without a leading !, and "
        + "nothing else";
}