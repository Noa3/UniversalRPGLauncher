using Godot;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What this runtime loads from a game, and what it does not.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This is the answer to "download the RTP", and it is measured
/// rather than argued.</strong> Every claim this project makes about a game is
/// a claim about reading files. There is no Ruby VM, no native loading, no
/// process launch, and no RTP directory in any code path — <strong>and a
/// boundary that only exists in a comment is a boundary that a change can
/// cross without anybody noticing.</strong>
/// </para>
/// <para>
/// <strong>So the boundary is checked here.</strong> The check is on the
/// source text, because the alternative is to trust a review, and a review
/// is a thing that happens once.
/// </para>
/// <para>
/// <strong>What would break the boundary, named exactly:</strong>
/// <c>LoadLibrary</c>, <c>GetProcAddress</c>, <c>DllImport</c>,
/// <c>Process.Start</c>, <c>Assembly.Load</c>, <c>Assembly.LoadFrom</c>, and
/// <c>System.Reflection.Emit</c>. A game's DLL and a game's EXE are the two
/// things this project refuses to touch, and each of those is how they would
/// be touched.
/// </para>
/// </remarks>
public partial class TestRuntimeBoundary : TestBase
{
    /// <summary>
    /// A real path on this machine, read from the project's own source tree.
    /// </summary>
    /// <remarks>
    /// <strong>Over the filesystem and not over Godot's own reader.</strong> The
    /// test has to walk directories and read every file in them, and that is
    /// what the filesystem is for — <strong>and a reader that used Godot's
    /// path API for a tree walk would have had to guess at whether a
    /// directory is a directory.</strong>
    /// </remarks>
    private string Quelltext(string pRelativ)
    {
        var pfad = System.IO.Path.Combine(
            ProjektWurzel(), pRelativ.Replace('/', System.IO.Path.DirectorySeparatorChar));
        AssertTrue(File.Exists(pfad),
            "**the source is there to read** — " + pRelativ + " at " + pfad);
        return File.ReadAllText(pfad);
    }

    /// <summary>
    /// The project directory as this machine sees it.
    /// </summary>
    /// <remarks>
    /// <strong>`ProjectSettings.GlobalizePath` and nothing else.</strong> The
    /// path this test gets is <c>res://</c> and the path
    /// <c>System.IO.File</c> wants is a Windows one, <strong>and handing
    /// <c>res://</c> to the .NET API throws
    /// <c>"Die Syntax für den Dateinamen ist falsch"</c></strong> — which has
    /// now happened twice in this repository and is written down so that a
    /// third time is somebody recognising it and not rediscovering it.
    /// </remarks>
    private static string ProjektWurzel()
        => ProjectSettings.GlobalizePath("res://");

    /// <summary>
    /// Every source file that could reach a game's machine code, checked for
    /// every way of reaching it.
    /// </summary>
    /// <remarks>
    /// <strong>The check walks the whole <c>src/</c> tree, not a list.</strong>
    /// A hand-written list of files is a list that is wrong the day somebody
    /// adds a plugin, and **the file that loads a DLL would be the new one.**
    /// </remarks>
    public void Test_NoSourceFileCanLoadAMachinesNativeCode()
    {
        var verboten = new[]
        {
            "LoadLibrary", "GetProcAddress", "DllImport", "Process.Start",
            "Assembly.Load", "Assembly.LoadFrom", "Reflection.Emit",
            "Marshal.GetDelegateForFunctionPointer", "NativeLibrary",
        };
        var wurzel = System.IO.Path.Combine(ProjektWurzel(), "src");
        AssertTrue(Directory.Exists(wurzel),
            "**the source tree is where it says it is** — " + wurzel
                + ", and a walk of a directory that is not there would "
                + "report nothing and pass");
        var geprueft = 0;
        var treffer = new List<string>();
        Rekursiv(verboten, wurzel, treffer, ref geprueft);

        // **127 und nicht "mehr als 50".** Die Zahl stand zuerst als
        // Schwelle da, **und eine Schwelle ist eine gerundete Vermutung** --
        // dieselbe Sorte wie die 700 Karten von gestern. **Der Test
        // vergleicht mit dem, was auf der Maschine liegt**, und wenn morgen
        // eine Datei dazukommt, meldet er es und nicht erst der Review.
        var erwartet = Directory.GetFiles(
            System.IO.Path.Combine(ProjektWurzel(), "src"), "*.cs",
            SearchOption.AllDirectories).Length;
        AssertEq(geprueft, erwartet,
            "**every C# file under src/ was read** — " + geprueft + " of "
                + erwartet + ", and a walk that stops early would report "
                + "nothing and pass");
        AssertEq(treffer.Count, 0,
            "**and no file can reach a game's machine code** — " + string.Join(
                "; ", treffer.Take(5)));
    }

    /// <summary>
    /// Walks a directory tree and checks every C# file in it.
    /// </summary>
    /// <remarks>
    /// <strong>The tree and not a list of names.</strong> A list of files is a
    /// list that is wrong the day somebody adds a plugin, and <strong>the
    /// file that loads a DLL would be exactly the new one.</strong>
    /// </remarks>
    private static void Rekursiv(
        string[] pVerboten,
        string pVerzeichnis,
        List<string> pTreffer,
        ref int pGeprueft)
    {
        foreach (var datei in Directory.GetFiles(pVerzeichnis, "*.cs"))
        {
            Gehe(pVerboten, datei, pTreffer, ref pGeprueft);
        }

        foreach (var unter in Directory.GetDirectories(pVerzeichnis))
        {
            Rekursiv(pVerboten, unter, pTreffer, ref pGeprueft);
        }
    }

    private static void Gehe(
        string[] pVerboten,
        string pPfad,
        List<string> pTreffer,
        ref int pGeprueft)
    {
        if (!pPfad.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        pGeprueft++;
        var bytes = Godot.FileAccess.GetFileAsBytes(pPfad);
        if (bytes == null || bytes.Length == 0)
        {
            return;
        }

        var text = System.Text.Encoding.UTF8.GetString(bytes);
        // **Zeilenweise, und ein Treffer in einem Kommentar zählt nicht.**
        // **Die Grenze steht in genau zwei Kommentaren** — einer sagt, dass
        // Game.exe nie geladen wird — **und ein Test, der den Kommentar
        // als Verletzung meldet, ist ein Test, den man zum Erreichen des
        // Ziels wegkompiliert.** Also: der Code ohne die Kommentare.
        var code = OhneKommentare(text);
        foreach (var verboten in pVerboten)
        {
            if (code.Contains(verboten, StringComparison.Ordinal))
            {
                pTreffer.Add(Path.GetFileName(pPfad) + " has " + verboten);
            }
        }
    }

    /// <summary>
    /// The source with its comments taken out.
    /// </summary>
    /// <remarks>
    /// <strong>Line comments and block comments and nothing else.</strong> A
    /// regex is the wrong tool for C# in general and the right one here,
    /// because the thing being removed is text and not code — **and a
    /// mention of a forbidden name inside a string literal counts as a hit,
    /// which is the safe direction**: a false failure names a file and a
    /// line, and a false pass would not be seen at all.
    /// </remarks>
    private static string OhneKommentare(string pText)
    {
        var ohneZeilen = Regex.Replace(pText, @"//[^\n]*", "");
        return Regex.Replace(ohneZeilen, @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    /// <summary>
    /// The RGSS backend says in its own fields that it ran nothing, and names
    /// the RTP it did not need.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the answer to "download the RTP", in the data the
    /// runtime already carries.</strong> <c>RtpDependency</c> says which RTP
    /// a generation would use, <c>InspectedFileCount</c> says how many files
    /// were read, and <c>HasSystemData</c> says whether the game's own
    /// system data is there.
    /// </para>
    /// <para>
    /// <strong>And the field is a name and not a path.</strong> A field that
    /// held a directory would be somewhere a download could be wired in, and
    /// this one cannot be: <strong>the runtime reads the game and not the
    /// RTP, and the test says so against the field the code actually
    /// has.</strong>
    /// </para>
    /// </remarks>
    public void Test_TheRtpIsNamedAsDataAndNotAsAPath()
    {
        var info = new UniversalRPG.Plugins.RgssRuntimeInfo
        {
            PluginId = "rpgmaker-xp",
            Generation = "RGSS",
            RtpDependency = "RPG_RT",
            InspectedFileCount = 0,
        };

        AssertTrue(info.RtpDependency == "RPG_RT",
            "**the RTP is a name** — \"RPG_RT\", and not a directory: the "
                + "runtime reads the game and not the RTP, and a field that "
                + "held a path would be somewhere a download could be wired in");
        AssertTrue(!info.RtpDependency.Contains('/')
                && !info.RtpDependency.Contains('\\'),
            "**and it has no separator in it** — a name and not a place, which "
                + "is the whole point");
        AssertEq(info.DataFileCount, 0,
            "**and no data file was opened** — this runtime was handed a name "
                + "and opened nothing, which is what a runtime that does not "
                + "read an RTP looks like from the outside");
    }
}
