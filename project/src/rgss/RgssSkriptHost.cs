using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Rgss;

/// <summary>
/// Serves a Ruby Maker project's own scripts to the Ruby interpreter,
/// out of the Marshal file they arrive in.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this host answers exactly one question: what does
/// <c>require "Interpreter 1"</c> mean.</strong>
/// </para>
/// <para>
/// <strong>And a Ruby Maker project has no script folder.</strong>
/// XP, VX and VX Ace keep their Ruby inside <c>Data/Scripts.rxdata</c> and
/// <c>Data/Scripts.rvdata2</c>, one zlib-compressed body per entry, --
/// <strong>and the names are the editor's, not file names.</strong> --
/// <strong>So a host that looked for a folder would find
/// nothing</strong>, -- <strong>and a host that invented
/// <c>.rb</c> would look for a layout no game of this generation
/// has.</strong>
/// </para>
/// <para>
/// <strong>And the interpreter's safety does not depend on this host.</strong>
/// <c>IRubyHost</c> is the whole boundary, and this one answers
/// <c>ReadScript</c> from a fixed list it read once, and answers
/// <c>CallMethod</c> and <c>LookupConstant</c> with a refusal.
/// </para>
/// </remarks>
public sealed class RgssSkriptHost : IRubyHost
{
    private readonly Dictionary<string, byte[]> _skripte;

    /// <summary>
    /// Reads a project's scripts and keeps them by name.
    /// </summary>
    /// <param name="pSkriptePfad">A <c>Scripts.rxdata</c> or <c>Scripts.rvdata2</c>.</param>
    /// <param name="pFehler">What stopped it, and empty when nothing did.</param>
    /// <returns>A host, and nothing when the file could not be read.</returns>
    /// <remarks>
    /// <strong>And a script that could not be inflated is not put in the
    /// dictionary</strong>, -- <strong>because a host that returned
    /// those bytes would hand the parser something that is not
    /// Ruby</strong>, -- <strong>and the parse error would name a
    /// script that is actually fine.</strong>
    /// </remarks>
    public static RgssSkriptHost? Lese(string pSkriptePfad, out string pFehler)
    {
        pFehler = "";
        List<XpScript> skripte;
        try
        {
            skripte = XpScriptBodies.LeseAlle(pSkriptePfad);
        }
        catch (IOException ausnahme)
        {
            pFehler = ausnahme.Message;
            return null;
        }

        var dict = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var ohneText = 0;
        foreach (var skript in skripte)
        {
            if (!skript.Entpackt || skript.Text == null)
            {
                ohneText++;
                continue;
            }

            dict[skript.Name] = new System.Text.UTF8Encoding(false)
                .GetBytes(skript.Text);
        }

        if (dict.Count == 0)
        {
            pFehler = "none of " + skripte.Count + " scripts could be"
                + " read, and a host with no scripts answers no name";
            return null;
        }

        if (ohneText > 0)
        {
            pFehler = $"{ohneText} of {skripte.Count} scripts could not be"
                + " inflated, and they are not in this host";
        }

        return new RgssSkriptHost(dict);
    }

    private RgssSkriptHost(Dictionary<string, byte[]> pSkripte)
    {
        _skripte = pSkripte;
    }

    /// <summary>
    /// How many scripts this host can hand out.
    /// </summary>
    public int Anzahl => _skripte.Count;

    /// <summary>
    /// The names it knows, in the order the file listed them.
    /// </summary>
    public IReadOnlyCollection<string> Namen => _skripte.Keys;

    /// <inheritdoc/>
    public byte[]? ReadScript(string pName, bool pEinmal) =>
        _skripte.TryGetValue(pName, out var bytes) ? bytes : null;

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments)
    {
        ArgumentNullException.ThrowIfNull(pReceiver);
        ArgumentNullException.ThrowIfNull(pMethod);
        return null;
    }

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
    {
        ArgumentNullException.ThrowIfNull(pReceiver);
        ArgumentNullException.ThrowIfNull(pMethod);
        return null;
    }

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName) => null;

    /// <summary>
    /// What this host does know, for a diagnostic.
    /// </summary>
    /// <remarks>
    /// <strong>And the answer is the script names and not the method
    /// names</strong>, -- <strong>because this host answers
    /// <c>ReadScript</c> and nothing else</strong>, -- <strong>and a
    /// diagnostic that said "no methods" would hide the fact that the
    /// scripts are all there.</strong>
    /// </remarks>
    public IReadOnlyList<string> KnownMethods =>
        new List<string>(_skripte.Keys);
}
