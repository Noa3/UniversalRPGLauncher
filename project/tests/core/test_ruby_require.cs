using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `require` and `load`, which is how VX and VX Ace read a third of
/// themselves.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not a convenience.</strong> Every VX and VX Ace game
/// splits its scripts across a hundred files and loads them in order,
/// <strong>and a reader that only refused the unknown method would have said
/// *has no method 'require' on this host* on the first line of every
/// plugin.</strong>
/// </para>
/// <para>
/// <strong>And the host is the one that reads files, and not the
/// interpreter.</strong> The host is the only one that has them,
/// <strong>and an interpreter that went looking for files would be a program
/// that runs a game and can also go and find things</strong> — the shape
/// this project refuses everywhere else.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A name that was required once is not required again, and `load` skips
    /// nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is the whole difference between the two words.</strong>
    /// <c>require "Sprite_Picture"</c> twice loads it once,
    /// <strong>and a reader that always ran the file would have every class in
    /// a game defined twice</strong> — and the second definition would take
    /// the methods with it, and a subclass written after it would inherit
    /// from a class that is a different one.
    /// </para>
    /// <para>
    /// <strong>And the answer says which happened.</strong> True and false,
    /// <strong>because a plugin asks</strong> — <code>return false unless
    /// require "my_lib"</code> is the sentence that decides whether a
    /// plugin installs itself.
    /// </para>
    /// </remarks>
    public void Test_RequireRunsOnceAndLoadRunsEveryTime()
    {
        var nurEinmal = new RubyInterpreter(new SkriptHost());
        var erste = nurEinmal.RunProgram(Statements("require \"a\"\n"));
        AssertTrue(erste.Kind == RubyValueKind.Boolean && erste.Boolean,
            "**the first require ran the script**");
        var zweite = nurEinmal.RunProgram(Statements("require \"a\"\n"));
        AssertTrue(zweite.Kind == RubyValueKind.Boolean && !zweite.Boolean,
            "**and the second did not** — a reader that always ran the file "
                + "would have every class in a game defined twice, and the "
                + "second definition would take the methods with it");

        var jedesMal = new RubyInterpreter(new SkriptHost());
        for(var mal = 0; mal < 3; mal++)
        {
            var wert = jedesMal.RunProgram(Statements("load \"a\"\n"));
            AssertTrue(wert.Kind == RubyValueKind.Boolean && wert.Boolean,
                "**and `load` skips nothing** — it is the word for a file "
                    + "that runs again, and the difference from `require` is "
                    + "the whole content of the two");
        }
    }

    /// <summary>
    /// The loaded file runs in this interpreter, and not in one of its own.
    /// </summary>
    /// <remarks>
    /// <strong>And that is why a subclass across two files works.</strong>
    /// <c>class Neu &lt; Aussen</c> in a required file needs the class the
    /// requiring file defined,
    /// <strong>and a reader that made a new interpreter per file would have
    /// every game's class in a world of its own</strong> — and `Neu` would
    /// have no `Aussen`.
    /// </remarks>
    public void Test_TheLoadedFileRunsInThisInterpreterAndSeesItsClasses()
    {
        var mit = new RubyInterpreter(new SkriptHost());
        var wert = mit.RunProgram(Statements(
            "class Aussen\n"
            + "  def gruessen\n"
            + "    \"aussen\"\n"
            + "  end\n"
            + "end\n"
            + "require \"erbt\"\n"
            + "Neu.new.gruessen\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Bytes), "geerbt",
            "**the loaded file saw the class the requiring file defined** — "
                + "and a reader that made a new interpreter per file would "
                + "have every game's class in a world of its own, and `Neu` "
                + "would have no `Aussen`");
    }

    /// <summary>
    /// A name the host does not have is a refusal, and a broken file names
    /// itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is false, and not an empty script.</strong> An empty
    /// file runs and defines nothing,
    /// <strong>and a game whose <c>require</c> silently did nothing would go
    /// on and fail somewhere else, far from the line that was
    /// missing.</strong>
    /// </para>
    /// <para>
    /// <strong>And a syntax error in the loaded file says which file.</strong>
    /// A line number without a file is a line number in three hundred
    /// scripts,
    /// <strong>and a game with that many files cannot be searched.</strong>
    /// </para>
    /// </remarks>
    public void Test_AMissingNameIsARefusalAndABrokenFileNamesItself()
    {
        var ohne = new RubyInterpreter(new SkriptHost());
        var wert = ohne.RunProgram(Statements("require \"fehlt\"\n"));
        AssertTrue(wert.Kind == RubyValueKind.Boolean && !wert.Boolean,
            "**a name the host does not have is false** — and not an empty "
                + "script, because a game whose `require` silently did "
                + "nothing would go on and fail somewhere else, far from the "
                + "line that was missing");
        AssertTrue(ohne.Diagnostics.Count > 0
            && ohne.Diagnostics[0].Contains("fehlt"),
            "**and the diagnostic names it** — a diagnostic without a name "
                + "leaves the reader guessing");

        var kaputt = new RubyInterpreter(new SkriptHost());
        var ausnahme = FehlerAus<RubyRuntimeException>(() =>
            kaputt.RunProgram(Statements("require \"kaputt\"\n")));
        AssertEq(ausnahme.Class, "SyntaxError",
            "**a file that does not parse is a syntax error** — the "
                + "reference raises, and a reader that ran it and continued "
                + "would leave a game running on half a script");
        AssertTrue(ausnahme.Detail.Contains("kaputt"),
            "**and the message says which file** — a line number without a "
                + "file is a line number in three hundred scripts, and a game "
                + "with that many files cannot be searched");
    }

    /// <summary>
    /// A host that has no files does not have to say so.
    /// </summary>
    /// <remarks>
    /// <strong>And the default is a refusal.</strong> Every host that existed
    /// before this worked without being changed,
    /// <strong>because the method has a body and that body says "no".</strong>
    /// A reader that made it required would have broken five hosts for a
    /// feature four of them do not have,
    /// <strong>und dann schreibt jeder Host eine leere Methode, die niemand
    /// liest.**
    /// </remarks>
    public void Test_AHostWithNoFilesRefusesWithoutSayingItHasNone()
    {
        var nullHost = new RubyInterpreter(new RubyNullHost());
        var wert = nullHost.RunProgram(Statements("require \"irgendwas\"\n"));
        AssertTrue(wert.Kind == RubyValueKind.Boolean && !wert.Boolean,
            "**the null host says no, like it says no to everything** — and a "
                + "diagnostic that named which host refused would be the one "
                + "diagnostic a game never sees, because the game failed "
                + "somewhere else");
    }

    /// <summary>
    /// A script in CP932, which is what every script of that time is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And Ruby 1.8 has no encoding note in a source file.</strong>
    /// Every game of that time is Shift_JIS,
    /// <strong>and a reader that assumed UTF-8 would turn every kanji in a
    /// game's text into two characters</strong> — and a name on a menu would
    /// be a name with holes in it.
    /// </para>
    /// <para>
    /// <strong>And the bytes here are not valid UTF-8.</strong>
    /// <c>93 fa</c> is one kanji in Shift_JIS and an illegal byte sequence
    /// in UTF-8,
    /// <strong>so a reader that tried UTF-8 first would have a class called
    /// <c>Kanji \uFFFD\uFFFD</c></strong> — and the test below looks it up
    /// by its CP932 name, **which is the only way it can be written.**
    /// </para>
    /// </remarks>
    public void Test_AScriptInCp932IsReadAsItWasWritten()
    {
        var mit = new RubyInterpreter(new SkriptHost());
        mit.RunProgram(Statements("require \"kanji\"\n"));

        AssertTrue(mit.FindMethod("Kanji\u65e5", "object_id") != null,
            "**the class whose name is a kanji exists under that name** — and "
                + "a reader that read the file as UTF-8 would have had a "
                + "class named with two replacement characters, and a game "
                + "that names a class after a day of the week would not find "
                + "it");
    }

    /// <summary>
    /// A script whose class name is a kanji, in CP932.
    /// </summary>
    /// <returns>The bytes, which are not valid UTF-8.</returns>
    /// <remarks>
    /// <strong>And the bytes are written out, and not encoded at run
    /// time.</strong> Encoding a string inside the test would prove that the
    /// test's own encoder works,
    /// <strong>and would prove nothing about the reader's decoder</strong> —
    /// <strong>the point is that these bytes arrive, and the reader has to
    /// make something of them.</strong>
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

    /// <summary>A host with two scripts, and no method at all.</summary>
    private sealed class SkriptHost : IRubyHost
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
                _ => null,
            };
        }
    }}
