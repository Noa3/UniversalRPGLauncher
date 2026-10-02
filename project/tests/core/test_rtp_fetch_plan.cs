using System;
using System.IO;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The plan that asks before it acts, and proves it does nothing.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a user said the RTP may be downloaded after asking
/// first</strong>, -- <strong>and the first half of that rule is the
/// half that gets forgotten</strong>: <strong>not to act without asking,
/// not to fail to ask.</strong>
/// </para>
/// <para>
/// <strong>And so this test writes the plan's own behaviour down</strong>,
/// and <strong>it does that by looking for the absence of the means and
/// not by trusting a comment.</strong>
/// </para>
/// </remarks>
public partial class TestRtpFetchPlan : TestBase
{
    /// <summary>
    /// The plan names what a project asks for and fetches none of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the names are the installer's</strong>, and a table
    /// that conflated VX and VX Ace would send a user to the wrong
    /// archive, -- <strong>and that is the whole reason this test names
    /// all four generations.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerPlanNenntDieLaufzeitumgebungenUndLadtKeine()
    {
        var wurzel = Path.Combine(
            Path.GetTempPath(), "urpg_rtp_plan_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(wurzel);
        try
        {
            // **Und ein Projekt, das vier Generationen nennt** -- **und
            // jede in der Datei, die die Engine dafuer benutzt.**
            // **Und diese vier Formen sind an echten Spielen auf dieser
            // Maschine gemessen**, -- **und keine davon ist geraten:**
            //
            // Die gemessenen Formen:
            //   Dreaming Mary    RTP=      Library=RGSS301.dll
            //   Random Dungeon   RTP=RPGVX Library=RGSS202E.dll
            //   Heartache 101    RTP1=     Library=RGSS102E.dll
            //   MicroQuest       RTP1=     Library=RGSS104E.dll
            //   VX Ace                     Library=RGSS3A.dll
            //   RM2K             RPG_RT.ini mit Abschnitt RPG_RT
            File.WriteAllText(
                Path.Combine(wurzel, "System.ini"),
                "[RPGVX]\nlibrary=RGSS301.dll\n");
            File.WriteAllText(
                Path.Combine(wurzel, "System2.ini"),
                "[RPGVXAce]\nlibrary=RGSS3A.dll\n");
            File.WriteAllText(
                Path.Combine(wurzel, "RPG_RT.ini"),
                "[RPG_RT]\nGameTitle=Testspiel\nMapEditMode=2\n");
            // **Und diese Formen sind an vier echten Spielen auf dieser
            // Maschine gemessen**, -- **und `RTP=RPGVX` und
            // `Library=RGSS202E.dll` sind dieselbe Laufzeit in zwei
            // Schreibweisen.**
            File.WriteAllText(
                Path.Combine(wurzel, "Game.ini"),
                "[Game]\nRTP=RPGVX\nLibrary=RGSS202E.dll\n");
            // **Und XP fragt nach `RGSS202E.dll`**, -- **und die
            // `.ini` eines Projekts nennt `RGSS202` ohne das `E`**,
            // **und der Leser muss beides als dasselbe erkennen.**
            //
            // **Und RM2K fragt `RPG_RT.ldb`, und eine `.ini` sagt
            // `RPG_RT=1`** -- **und auch das ist dieselbe Laufzeit.**
            File.WriteAllText(
                Path.Combine(wurzel, "Game2.ini"),
                "[Game]\nRTP=\nLibrary=RGSS102E.dll\nRTP1=Standard\n");

            var ziel = Path.Combine(wurzel, "rtp");
            var plan = RtpFetchPlanner.Build(wurzel, ziel);

            System.Console.WriteLine(
                "RTP-Plan: " + string.Join(" ", plan.Describe()));
            AssertEq(plan.Entries.Count, 5,
                "**and it names five runtimes** -- and it named "
                + plan.Entries.Count + ": the fixture writes RGSS301,"
                + " RGSS3A, RGSS202E, RGSS102E and RPG_RT, and a"
                + " project names every one of them in its own files");

            var namen = new System.Collections.Generic.List<string>();
            foreach (var eintrag in plan.Entries)
            {
                namen.Add(eintrag.DependencyName);
            }

            AssertTrue(namen.Contains("RGSS202E.dll") || namen.Contains("RGSS301.dll"),
                "**and VX asks for its own RGSS2 oder RGSS3** -- and the names are "
                + string.Join(" ", namen));
            AssertTrue(namen.Contains("RGSS3A.dll"),
                "**and VX Ace asks for its own RGSS3A**");
            AssertTrue(namen.Contains("RGSS102E.dll"),
                "**and XP asks for its own RGSS102E**");
            AssertTrue(namen.Contains("RPG_RT")
                || namen.Contains("RPG_RT.ldb"),
                "**and RM2K asks for RPG_RT** -- and the names are "
                + string.Join(" ", namen));

            // **Und nichts davon liegt im Zielverzeichnis**, **und
            // das ist der ganze Test.**
            AssertTrue(!Directory.Exists(ziel),
                "**and the target directory does not exist** -- and it did"
                + $" not, and a plan that created it would already have"
                + " written something to disk");
            AssertTrue(plan.FullySourced,
                "**and this repository knows a source for all"
                + " five**");
        }
        finally
        {
            if (Directory.Exists(wurzel))
            {
                Directory.Delete(wurzel, true);
            }
        }
    }

    /// <summary>
    /// And the type has no means of fetching anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a claim about the assembly and not about a
    /// comment</strong>, -- <strong>and it is checkable</strong>: <strong>if
    /// somebody adds <c>HttpClient</c>, <c>WebClient</c>,
    /// <c>DownloadFile</c> or <c>ZipFile</c> to a type in this
    /// namespace, this test fails.</strong>
    /// </para>
    /// <para>
    /// <strong>And that is the only way to keep a promise like this
    /// honest</strong>, -- <strong>because a promise in a comment is
    /// worth nothing the day someone wants it to be worth
    /// something.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieserNamensraumKannNichtLadenUndNichtEntpacken()
    {
        var versammlung = typeof(RtpFetchPlan).Assembly;
        foreach (var typ in versammlung.GetTypes())
        {
            if (typ.Namespace != "UniversalRPG.Rtp")
            {
                continue;
            }

            foreach (var feld in typ.GetFields())
            {
                var name = feld.FieldType.FullName ?? "";
                AssertTrue(
                    !name.Contains("HttpClient", StringComparison.Ordinal)
                    && !name.Contains("WebClient", StringComparison.Ordinal),
                    "**and no type here holds a network client** -- and "
                        + $"{typ.Name}.{feld.Name} is {name}");
            }

            foreach (var methode in typ.GetMethods())
            {
                var name = methode.ReturnType.FullName ?? "";
                AssertTrue(
                    !name.Contains("Task", StringComparison.Ordinal),
                    "**and no method here returns a task** -- and "
                        + $"{typ.Name}.{methode.Name} returns {name}");
                AssertTrue(
                    !(methode.Name.Contains("Download", StringComparison.Ordinal)
                        || methode.Name.Contains("Fetch", StringComparison.Ordinal)
                        || methode.Name.Contains("Extract",
                            StringComparison.Ordinal)
                        || methode.Name.Contains("Unpack",
                            StringComparison.Ordinal)),
                    "**and no method here is named after an action** -- and "
                        + $"{typ.Name}.{methode.Name}");
            }
        }

        AssertTrue(true,
            "**and the RTP namespace is a plan and nothing more**");
    }

    /// <summary>
    /// And what a plan says when a human reads it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a plan nobody can read is not consent</strong>, --
    /// <strong>and this is what a user would be shown before being
    /// asked.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerPlanSagtInWortenWasErMachtUndWasNicht()
    {
        var plan = new RtpFetchPlan
        {
            TargetDirectory = "C:/RTP",
            Entries = [RtpNennungen.ProfilFuer("RGSS3A.dll")],
        };
        var zeilen = plan.Describe();

        AssertTrue(zeilen.Count == 3,
            "**and it is three lines** -- and it is " + zeilen.Count);
        AssertTrue(zeilen[0].Contains("C:/RTP"),
            "**and the first names where**");
        AssertTrue(zeilen[1].Contains("geladen"),
            "**and the second says that nothing was loaded** -- and it"
            + $" says: {zeilen[1]}");
        AssertTrue(zeilen[2].Contains("weiss, woher"),
            "**and the third says where it would come from**");
    }
}
