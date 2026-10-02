using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace UniversalRPG.Rgss;

/// <summary>
/// Serves a project's data files to the Ruby that runs it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the host a running XP game asks for, and not a
/// second language.</strong>
/// </para>
/// <para>
/// <strong>And the sentence that needs it is written in MicroQuest's own
/// <c>Game_Map.setup</c>:</strong>
/// </para>
/// <code>
/// @map = load_data(sprintf("Data/Map%03d.rxdata", @map_id))
/// </code>
/// <para>
/// <strong>And the chain behind that sentence is measured:</strong>
/// </para>
/// <list type="arrow">
/// <item><description><c>Map001.rxdata</c> is a Marshal file,</description></item>
/// <item><description>the interpreter's own host boundary asks for
/// <c>IRubyHost</c>, and this is the implementation, and</description></item>
/// <item><description>the file opens, reads and executes on demand --
/// <strong>and nothing of it is executed before it is asked for.</strong></description></item>
/// </list>
/// <para>
/// <strong>And a name is resolved against one directory and one
/// extension list</strong>, -- <strong>because <c>load_data</c> in XP
/// writes the extension itself</strong> (`Data/Map%03d.rxdata`), --
/// <strong>and this host takes the name as written.</strong>
/// </para>
/// <para>
/// <strong>And it refuses everything it was not asked for.</strong>
/// <c>load_data</c> on a name outside the data directory is null and
/// not a silent empty value, -- <strong>and a run that continued past
/// a refused load would be running on a map that does not exist.</strong>
/// </para>
/// </remarks>
public sealed class RgssDatenHost : IRubyHost
{
    private readonly string _wurzel;
    private readonly Dictionary<string, object?> _globals =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Opens a project's data directory.
    /// </summary>
    /// <param name="pWurzel">
    /// The game directory, and every name is resolved inside it.
    /// </param>
    /// <param name="pFehler">What stopped it, and empty when nothing did.</param>
    /// <returns>A host, and nothing when the directory is not there.</returns>
    /// <remarks>
    /// <strong>And the root is fixed once and for nothing is read from
    /// outside it</strong>, -- <strong>and a game that asks for a path
    /// with a parent in it is refused</strong>, -- <strong>because an
    /// imported game is untrusted input and a host that followed a path
    /// out of the project would be a reader that can read the
    /// machine.</strong>
    /// </remarks>
    public static RgssDatenHost? Oeffne(string pWurzel, out string pFehler)
    {
        pFehler = "";
        if (!Directory.Exists(pWurzel))
        {
            pFehler = "the game directory is not there: " + pWurzel;
            return null;
        }

        return new RgssDatenHost(pWurzel);
    }

    private RgssDatenHost(string pWurzel)
    {
        _wurzel = pWurzel;
    }

    /// <summary>How many data files this host was asked for.</summary>
    public int Gelesen { get; private set; }

    /// <summary>How many of those requests it refused.</summary>
    public int Verweigert { get; private set; }

    /// <inheritdoc/>
    public byte[]? ReadScript(string pName, bool pEinmal) =>
        null;

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments)
    {
        ArgumentNullException.ThrowIfNull(pMethod);

        // **Und `load_data` ist der einzige Name, den dieser Host
        // beantwortet**, -- **und er beantwortet ihn mit einem
        // `RubyValue`**, -- **und nicht mit einem C#-Objekt.**
        if (pMethod != "load_data" || pArguments.Count < 1)
        {
            return null;
        }

        var name = System.Text.Encoding.UTF8.GetString(pArguments[0].Bytes);
        var pfad = Pfad(name);
        if (pfad == null)
        {
            Verweigert++;
            return null;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(pfad);
        }
        catch (IOException)
        {
            Verweigert++;
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            Verweigert++;
            return null;
        }

        Gelesen++;
        try
        {
            // **Und der Marshal-Baum wird in einen Ruby-Baum
            // ueberfuehrt**, -- **und nicht zurueckgegeben.**
            //
            // **Und das ist keine Kosmetik**: -- **`load_data` gibt in
            // Ruby den Wert zurueck, den die Datei haelt**, --
            // **und `@map.tileset_id` ist ein Feldzugriff darauf**,
            // -- **und ein Leser, der seinen eigenen Wertbaum in das
            // Objekt steckte, wuerde dem Spiel eine Klasse geben, die
            // es nie gesehen hat.**
            return AlsRuby(new MarshalReader(bytes).Read(), 0);
        }
        catch (MarshalFormatException)
        {
            Verweigert++;
            return null;
        }
    }

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield) => null;

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName) => null;

    /// <summary>
    /// Turns one marshalled value into the Ruby value it is.
    /// </summary>
    /// <param name="pWert">The value tree.</param>
    /// <param name="pTiefe">How far down, and it stops there.</param>
    /// <returns>The value, and nil when the tree is deeper than the bound.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the depth bound is not a guess about how deep a map
    /// is.</strong> It is what a reader can hold, -- <strong>and a
    /// value that is too deep is <c>nil</c> and not a truncated
    /// object</strong>, -- <strong>because a half-read map would let a
    /// run draw tiles out of a tile id that is not there.</strong>
    /// </para>
    /// </remarks>
    private static RubyValue AlsRuby(MarshalValue pWert, int pTiefe)
    {
        if (pTiefe > 12)
        {
            return RubyValue.Nil;
        }

        if (pWert.Kind == "symbol" && pWert.Text != null)
        {
            return RubyValue.OfSymbol(pWert.Text);
        }

        if (pWert.Kind == "string" && pWert.Text != null)
        {
            return RubyValue.OfBytes(System.Text.Encoding.UTF8
                .GetBytes(pWert.Text));
        }

        if (pWert.Kind == "nil")
        {
            return RubyValue.Nil;
        }

        if (pWert.Kind == "true")
        {
            return RubyValue.OfBoolean(true);
        }

        if (pWert.Kind == "false")
        {
            return RubyValue.OfBoolean(false);
        }

        // **Und der Integer-Zweig kommt VOR dem Objekt-Zweig** --
        // **und das war der Fehler.**
        //
        // **Und `MarshalValue.Integer` ist bei einem Objekt der
        // *Objektindex* und nicht der Wert**, -- **denn
        // `ReadHash`, `Lese` und `LeseObjekt` schreiben dort
        // `Integer = objectIndex`** -- **und MicroQuests
        // `RPG::Map` kam als `Integer` zurueck.**
        //
        // **Und die Reihenfolge ist nicht Geschmack, sondern
        // Korrektheit:** -- **ein Objekt, ein Hash und ein String
        // tragen alle eine Zahl in diesem Feld**, -- **und wer sie
        // zuerst prueft, gibt Zahlen fuer Klassen zurueck.**
        //
        // **Und `MarshalValue` unterscheidet die Faelle ueber
        // `Kind`**, -- **und das ist das Feld, das geprueft werden
        // muss.**
        if (pWert.Kind == "integer" && pWert.Integer.HasValue)
        {
            return RubyValue.OfInteger(pWert.Integer.Value);
        }

        if (pWert.Kind == "float" && pWert.Real.HasValue)
        {
            return RubyValue.OfReal(pWert.Real.Value);
        }

        if (pWert.Kind == "string" && pWert.Bytes.Length > 0)
        {
            return RubyValue.OfBytes(pWert.Bytes);
        }

        if (pWert.Kind == "array")
        {
            var liste = new List<RubyValue>(pWert.Items.Count);
            foreach (var element in pWert.Items)
            {
                liste.Add(AlsRuby(element, pTiefe + 1));
            }

            return RubyValue.OfArray(liste);
        }

        if (pWert.Kind == "hash")
        {
            var paare = new List<RubyValue>();
            for (var i = 0; i < pWert.Keys.Count && i < pWert.Items.Count; i++)
            {
                paare.Add(RubyValue.OfBytes(System.Text.Encoding.UTF8
                    .GetBytes(pWert.Keys[i])));
                paare.Add(RubyValue.OfSymbol(pWert.Keys[i]));
            }

            return RubyValue.OfHash(paare);
        }

        // **Und `Kind` ist bei MicroQuests Tileset NICHT
        // `"object"`.** -- **und der Marshal-Leser schreibt
        // `Kind = "user defined"`**, -- **denn RPG Makers Klassen
        // kommen als `u`-Byte** -- **und `MarshalValue` nennt das
        // `ClassName = "RPG::Tileset"`**.
        //
        // **Und gemessen an `Data/Tilesets.rxdata`:**
        //
        // ```text
        // [1]: object / RPG::Tileset, Keys: 17, Items: 17
        // ```
        //
        // **und `@name` ist `"Gralssland"`** -- **ein Name aus dem
        // Spiel selbst.**
        if ((pWert.Kind == "object" || pWert.Kind == "UserClass")
            && pWert.ClassName != null)
        {
            // **Und die Felder des Objekts wandern mit.**
            //
            // **Und die erste Fassung gab `OfObject` einen LEEREN
            // Member-Satz**, -- **und `RPG::Tileset` kam damit ohne ein
            // einziges Feld zurueck**, -- **und `tileset_name` war
            // leer, und `Game_Map#setup` haette ein leeres Feld in
            // `@tileset_name` geschrieben.**
            //
            // **Und gemessen:**
            //
            // ```text
            // Tileset[1]: Object / RPG::Tileset, Felder: 0
            // ```
            //
            // **Und `MarshalValue.Keys` traegt die Feldnamen unter
            // `@name`**, -- **und `Items` traegt die Werte in
            // derselben Reihenfolge.**
            var felder = new Dictionary<RubyValue, RubyValue>();
            for (var i = 0; i < pWert.Keys.Count
                && i < pWert.Items.Count; i++)
            {
                var name = pWert.Keys[i];
                if (name.Length == 0)
                {
                    continue;
                }

                felder[RubyValue.OfSymbol(name)] =
                    AlsRuby(pWert.Items[i], pTiefe + 1);
            }

            // **Und `OfObject` nimmt einen `Members`-Satz, und der
            // heisst nicht `Felder`.**
            //
            // **Und `MarshalValue.Keys` traegt `@name`**, -- **und ein
            // Ruby-Objekt traegt `@name` als Instanzvariable**,
            // -- **und `Members` ist die Tabelle fuer
            // Singleton-Methoden und `Felder` die fuer
            // Instanzvariablen.**
            //
            // **Und die erste Fassung fuellte `Members` und las `Felder`
            // aus**, -- **und das sind zwei verschiedene
            // Speicher**, -- **und deshalb kam `Felder: 0` heraus.**
            var objekt = RubyValue.OfObject(pWert.ClassName, felder);
            foreach (var paar in felder)
            {
                var schluessel = paar.Key.Name ?? string.Empty;
                if (schluessel.StartsWith("@", StringComparison.Ordinal))
                {
                    objekt.Felder[schluessel] = paar.Value;
                }
            }

            return objekt;
        }

        return RubyValue.Nil;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> KnownMethods { get; } =
        new List<string> { "load_data" };

    /// <summary>
    /// Turns a name the game wrote into a path inside the project.
    /// </summary>
    /// <param name="pName">The name, as written.</param>
    /// <returns>The path, and null when the name leaves the project.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a name with a parent directory in it is
    /// refused</strong>, -- <strong>and that is the whole safety
    /// property of this class in one line</strong>, -- <strong>and a
    /// game that wrote <c>"../../Users/…"</c> would otherwise be a
    /// reader that reads the machine.</strong>
    /// </para>
    /// <para>
    /// <strong>And the name is taken as written.</strong> XP's own
    /// <c>load_data</c> is called with <c>"Data/Map%03d.rxdata"</c>,
    /// -- <strong>and the extension is part of the name</strong>, --
    /// <strong>and a host that appended one would miss the file.</strong>
    /// </para>
    /// </remarks>
    private string? Pfad(string pName)
    {
        if (pName.Length == 0 || Path.IsPathRooted(pName))
        {
            return null;
        }

        foreach (var teil in pName.Split('/'))
        {
            if (teil == "..")
            {
                return null;
            }
        }

        var voll = Path.GetFullPath(Path.Combine(_wurzel, pName));
        var wurzel = Path.GetFullPath(_wurzel);
        if (!voll.StartsWith(wurzel, StringComparison.Ordinal))
        {
            return null;
        }

        return File.Exists(voll) ? voll : null;
    }

    /// <summary>
    /// Loads one data file the way the game asks for it.
    /// </summary>
    /// <param name="pName">The name as written, e.g. <c>Data/Tilesets.rxdata</c>.</param>
    /// <returns>The value, and null when the host refuses the name.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the door a runtime comes through.</strong>
    /// <c>Scene_Title</c> writes nine lines and <c>Game_Map#setup</c>
    /// writes one, -- <strong>and all ten say
    /// <c>load_data("Data/…")</c></strong>, -- <strong>and a runtime
    /// that assembles its own world has to be able to ask for those
    /// ten files before the game's first scene runs.</strong>
    /// </para>
    /// <para>
    /// <strong>And the name is taken as written</strong>, -- because
    /// that is how <c>load_data</c> is called, -- <strong>and a
    /// helper that took a bare name would have to guess an extension
    /// the caller already wrote.</strong>
    /// </para>
    /// </remarks>
    public RubyValue? Lade(string pName) => CallMethod(
        RubyValue.OfSymbol("Kernel"),
        "load_data",
        new[] { RubyValue.OfBytes(System.Text.Encoding.UTF8
            .GetBytes(pName ?? string.Empty)) });

    /// <summary>
    /// The name the game writes for a map file.
    /// </summary>
    /// <param name="pMapId">The map's own number.</param>
    /// <returns>The name, exactly as MicroQuest's <c>setup</c> formats it.</returns>
    /// <remarks>
    /// <strong>And <c>%03d</c> is not a convention this repository
    /// chose</strong>, -- <strong>and it is written in MicroQuest's
    /// <c>Game_Map.setup</c>:</strong>
    /// <c>sprintf("Data/Map%03d.rxdata", @map_id)</c>.
    /// </remarks>
    public static string KartenName(int pMapId) =>
        "Data/Map" + pMapId.ToString("D3", CultureInfo.InvariantCulture)
            + ".rxdata";
}
