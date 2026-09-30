using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The folder tree every host in this file answers from, so
/// <c>require_relative</c> has a "next to me" to work out.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the folder tree is written out, and not built at run
/// time.</strong> The names in <see cref="Suche"/> are the whole point of
/// the host, **and a test that generated its own tree would have proved
/// that the generator works and nothing else.**
/// </para>
/// <para>
/// <strong>And <c>util</c> from the top is a different file from
/// <c>lib/util</c>, and it says so in its value.</strong> `99` and `2`,
/// **and that is what lets a test say <em>which</em> of the two a reader
/// loaded** — **and <c>require</c>'s own answer, <c>true</c> or
/// <c>false</c>, is the same for both.**
/// </para>
/// <para>
/// <strong>And <c>..</c> is resolved here and not in the reader.</strong>
/// A path that climbs a level is a file system question,
/// **and a reader that worked it out would have hard-coded one host's
/// separator into the language runtime.**
/// </para>
/// </remarks>
internal static class Skriptdateien
{
    /// <summary>The bytes a name has, or null when there is no such file.</summary>
    /// <param name="pName">The name, as this host knows it.</param>
    /// <returns>The bytes, or null.</returns>
    public static byte[]? Suche(string pName) => pName switch
    {
        "lib/util" => System.Text.Encoding.UTF8.GetBytes("2\n"),
        "lib/a" => System.Text.Encoding.UTF8.GetBytes("1\n"),
        "lib/tief/util" => System.Text.Encoding.UTF8.GetBytes("3\n"),
        // **Und zwei Geschwister in `lib/tief/`, und eines davon kommt
        // nur ueber `..` zu `lib/tief/util`.** `anders` schreibt
        // `require_relative "../tief/util"`,
        // **und ein Leser, der nur den Ordner davorgeklebt haette,
        // `lib/tief/../tief/util` geschrieben** — **und das sind zwei
        // verschiedene Zeichenketten fuer eine Datei**, und ein Leser,
        // der die geschriebene in sein Buch schreibt, laedt sie zweimal.
        "lib/tief/anders" => System.Text.Encoding.UTF8.GetBytes(
            "require_relative \"../tief/util\"\n"),
        "lib/tief/tief/util"
            => System.Text.Encoding.UTF8.GetBytes("7\n"),
        // **Und dieselbe Regel eine Ebene tiefer.**
        "lib/tief/noch/tiefer" => System.Text.Encoding.UTF8.GetBytes(
            "require_relative \"tief/util\"\n"),
        "lib/tief/noch/tief/util"
            => System.Text.Encoding.UTF8.GetBytes("8\n"),
        "util" => System.Text.Encoding.UTF8.GetBytes("99\n"),
        _ => null,
    };

    /// <summary>
    /// Puts a name that was written next to a script's name onto the same
    /// footing.
    /// </summary>
    /// <param name="pAufrufend">The name of the script doing the reading.</param>
    /// <param name="pName">The name as written.</param>
    /// <returns>The one name both writers would agree on.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>..</c> climbs and <c>.</c> does not.</strong>
    /// <c>lib/tief/anders</c> writes <c>require_relative "../tief/util"</c>
    /// and means <c>lib/tief/util</c>.
    /// </para>
    /// <para>
    /// <strong>And a name with its own folder is taken as it stands.</strong>
    /// <c>require_relative "lib/util"</c> from anywhere is
    /// <c>lib/util</c> and not the caller's folder plus that,
    /// **because that is what a file system does with a path that starts
    /// at a folder and not at <c>..</c>.**
    /// </para>
    /// </remarks>
    public static string Aufloesen(string pAufrufend, string pName)
    {
        if (!pName.Contains('/'))
        {
            var schluessel = pAufrufend.LastIndexOf('/');
            return schluessel < 0
                ? pName
                : pAufrufend[..(schluessel + 1)] + pName;
        }

        var teile = new List<string>();
        var grund = pAufrufend.LastIndexOf('/');
        var vorlauf = grund < 0 ? string.Empty : pAufrufend[..grund];
        foreach (var teil in (vorlauf + "/" + pName).Split('/'))
        {
            if (teil is "" or ".")
            {
                continue;
            }

            if (teil == "..")
            {
                if (teile.Count > 0)
                {
                    teile.RemoveAt(teile.Count - 1);
                }

                continue;
            }

            teile.Add(teil);
        }

        return string.Join("/", teile);
    }
}

/// <summary>A host with a few scripts, and no method of its own.</summary>
/// <remarks>
/// <strong>And a script with a kanji in its class name, in CP932</strong>,
/// because every script of that time is Shift_JIS,
/// **and a host that refused it would have proved nothing about the
/// reader's decoder.**
/// </remarks>
internal sealed class SkriptHost : IRubyHost
{
    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments)
    {
        _ = pReceiver;
        _ = pMethod;
        _ = pArguments;
        return null;
    }

    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
    {
        _ = pReceiver;
        _ = pMethod;
        _ = pArguments;
        _ = pYield;
        return null;
    }

    public RubyValue? LookupConstant(string pName)
    {
        _ = pName;
        return null;
    }

    public IReadOnlyList<string> KnownMethods => Array.Empty<string>();

    public string? ResolveScriptName(
        string pAufrufend, string pName)
        => Skriptdateien.Aufloesen(pAufrufend, pName);

    public byte[]? ReadScriptRelative(
        string pAufrufend, string pName, bool pEinmal)
    {
        _ = pEinmal;
        var vollstaendig = Skriptdateien.Aufloesen(pAufrufend, pName);
        if (Skriptdateien.Suche(vollstaendig) != null)
        {
            return Skriptdateien.Suche(vollstaendig);
        }

        return Skriptdateien.Suche(pName);
    }

    public byte[]? ReadScript(string pName, bool pEinmal)
    {
        _ = pEinmal;
        return pName switch
        {
            "a" => System.Text.Encoding.UTF8.GetBytes("1\n"),
            "erbt" => System.Text.Encoding.UTF8.GetBytes(
                "class Neu < Aussen\n"
                + "  def gruessen\n"
                + "    \"geerbt\"\n"
                + "  end\n"
                + "end\n"),
            "kaputt" => System.Text.Encoding.UTF8.GetBytes("class \n"),
            // **Und diese Datei ist CP932 und nicht UTF-8.** `93 fa`
            // ist das Kanji für "Tag" in Shift_JIS,
            // **und als UTF-8 ist das keine gueltige Bytefolge** --
            // **ein Leser, der UTF-8 annimmt, wuerde hier entweder einen
            // Ersatzzeichen-Text oder einen Absturz bekommen**, und
            // **jedes echte Skript eines Spiels aus dieser Zeit ist so
            // kodiert.**
            "kanji" => KanjiSkript(),
            _ => Skriptdateien.Suche(pName),
        };
    }

    /// <summary>
    /// A script whose class name is a kanji, in CP932.
    /// </summary>
    /// <returns>The bytes, which are not valid UTF-8.</returns>
    /// <remarks>
    /// <strong>And the bytes are written out, and not encoded at run
    /// time.</strong> Encoding a string inside the test would prove that
    /// the test's own encoder works,
    /// **and would prove nothing about the reader's decoder** --
    /// **the point is that these bytes arrive, and the reader has to
    /// make something of them.**
    /// </remarks>
    private static byte[] KanjiSkript()
    {
        // "class Kanji<93 fa> LF  def object_id LF    1 LF  end LFend LF",
        // **und `93 fa` ist ein Kanji in Shift_JIS und keine gueltige
        // UTF-8-Bytefolge**,
        // **und es haengt direkt am Namen, weil es sonst ein zweites
        // Wort waere** -- **und der Lexer haelt es dann fuer einen eigenen
        // Namen**, und die Klasse hiesse `Kanji`.
        return new byte[]
        {
            0x63, 0x6c, 0x61, 0x73, 0x73, 0x20, 0x4b, 0x61, 0x6e,
            0x6a, 0x69, 0x93, 0xfa, 0x0a, 0x20, 0x20, 0x64,
            0x65, 0x66, 0x20, 0x6f, 0x62, 0x6a, 0x65, 0x63, 0x74,
            0x5f, 0x69, 0x64, 0x0a, 0x20, 0x20, 0x20, 0x20, 0x31,
            0x0a, 0x20, 0x20, 0x65, 0x6e, 0x64, 0x0a, 0x65, 0x6e,
            0x64, 0x0a,
        };
    }
}

/// <summary>
/// A host that writes down what it was asked for, so a test can say
/// which file was read and not only that something was.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the name goes in the order it was asked.</strong>
/// <c>require</c> answers <c>true</c> or <c>false</c> and nothing else,
/// **and a reader that wrote two different files and got <c>true</c>
/// twice has learned nothing** — **and the one thing a plugin's folder
/// depends on is <em>which</em> file, and not whether one was read.**
/// </para>
/// <para>
/// <strong>And the same tree as every other host here.</strong> One
/// listing and not three,
/// **because a host with a folder of its own would be a second answer to
/// a question this file answers once.**
/// </para>
/// </remarks>
internal sealed class ZaehlerHost : IRubyHost
{
    /// <summary>The names that were read, in the order they came.</summary>
    public List<string> Geladen { get; } = [];

    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments)
    {
        _ = pReceiver;
        _ = pMethod;
        _ = pArguments;
        return null;
    }

    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
    {
        _ = pReceiver;
        _ = pMethod;
        _ = pArguments;
        _ = pYield;
        return null;
    }

    public RubyValue? LookupConstant(string pName)
    {
        _ = pName;
        return null;
    }

    public IReadOnlyList<string> KnownMethods => Array.Empty<string>();

    public string? ResolveScriptName(
        string pAufrufend, string pName)
        => Skriptdateien.Aufloesen(pAufrufend, pName);

    public byte[]? ReadScriptRelative(
        string pAufrufend, string pName, bool pEinmal)
    {
        _ = pEinmal;
        var vollstaendig = Skriptdateien.Aufloesen(pAufrufend, pName);
        if (Skriptdateien.Suche(vollstaendig) != null)
        {
            Geladen.Add(vollstaendig);
            return Skriptdateien.Suche(vollstaendig);
        }

        Geladen.Add(pName);
        return Skriptdateien.Suche(pName);
    }

    public byte[]? ReadScript(string pName, bool pEinmal)
    {
        _ = pEinmal;
        Geladen.Add(pName);
        return Skriptdateien.Suche(pName);
    }
}
