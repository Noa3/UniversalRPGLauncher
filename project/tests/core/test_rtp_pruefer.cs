using System;
using System.IO;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Whether a game needs a runtime it does not have.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the two forms that decide it were read out of real
/// <c>Game.ini</c> files</strong>:
///
/// <code>
/// Random Dungeon (VX)   RTP=RPGVX
/// MicroQuest (XP)       RTP1=Standard
///                       RTP2=
///                       RTP3=
/// </code>
///
/// <strong>And an empty value is the form that says "look for
/// nothing"</strong>, -- <strong>and a launcher that read
/// <c>RTP2=</c> as a runtime named <c>""</c> would ask about
/// nothing.</strong>
/// </para>
/// <para>
/// <strong>And this machine has no RPG Maker runtime at all</strong>,
/// -- <strong>which was checked</strong>, -- <strong>and so every
/// named runtime is measured as missing here.</strong>
/// </para>
/// </remarks>
public partial class TestRtpPruefer : TestBase
{
    /// <summary>
    /// And an empty name is a game that needs nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>RTP2=</c> out of MicroQuest is this
    /// case</strong>, -- <strong>and a launcher that interrupted here
    /// would ask about a runtime that no one named.</strong>
    /// </para>
    /// </remarks>
    public void Test_LeererNameBrauchtNichts()
    {
        foreach (var form in new[] { "", "   ", null })
        {
            var e = RtpPruefer.Pruefe(form);
            Console.WriteLine($"\"{form ?? "null"}\" -> "
                + $"nennt={e.NenntRtp} fehlt={e.Fehlt}");
            AssertTrue(!e.NenntRtp && !e.Fehlt,
                "**and an empty name means the game needs no"
                + " runtime** -- and that is the form `RTP2=` has in"
                + " MicroQuest's own `Game.ini`, and a launcher that"
                + " asked there would be asking about nothing");
        }
    }

    /// <summary>
    /// And a named runtime that has no measured archive is reported,
    /// not resolved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>Standard</c> is exactly that name</strong>, --
    /// <strong>and MicroQuest writes it</strong>, -- <strong>and it is
    /// not an engine id in the table</strong>.
    /// </para>
    /// </remarks>
    public void Test_EinNameOhneArchivWirdGemeldet()
    {
        // **Und `Standard` kommt aus MicroQuest's `Game.ini`.**
        var e = RtpPruefer.Pruefe("Standard");
        Console.WriteLine("Standard -> nennt=" + e.NenntRtp
            + " fehlt=" + e.Fehlt);
        Console.WriteLine("Meldung: " + e.Meldung);

        AssertTrue(e.NenntRtp,
            "**and the name is reported as named**");
        AssertTrue(e.Meldung.Contains("kein Archiv"),
            "**and it says no measured archive exists for it** -- and"
                + " it does not guess one, because a mapping this"
                + " repository invents is how the wrong runtime gets"
                + " installed");
        AssertTrue(!e.Fehlt,
            "**and it does not claim the runtime is missing** -- and"
                + " I do not know what this is and it is"
                + " missing are different answers and the first"
                + " one is the honest one");
    }

    /// <summary>
    /// And the five engine ids are all mapped to places to look.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this machine has no RPG Maker runtime</strong>, --
    /// <strong>which was checked in the registry and under
    /// <c>Common Files</c></strong>, -- <strong>and so every named
    /// runtime comes back missing here and that is the honest
    /// answer.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieFuenfLaufzeiten()
    {
        foreach (var kv in RtpArchivFakten.Alle())
        {
            var e = RtpPruefer.Pruefe(kv.Key);
            Console.WriteLine($"{kv.Key,-8} fehlt={e.Fehlt}  "
                + $"Orte={e.GesuchteOrte.Count}  Name={kv.Value.ArchivName}");
            AssertTrue(e.NenntRtp && e.GesuchteOrte.Count >= 2,
                "**and every runtime the table knows has places to"
                    + " look for it** -- and there are at least two,"
                    + " because RPG Maker writes into"
                    + " `Program Files (x86)/Common Files` and a"
                    + " launcher that checked one tree would report"
                    + " every runtime as missing");
        }
    }

    /// <summary>
    /// And the registry is not consulted, and that is written down.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a decision and not an omission</strong>, --
    /// <strong>and a launcher that asked the registry first would
    /// report a runtime as missing whenever the entry is stale.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieRegistryWirdNichtGelesen()
    {
        Console.WriteLine("LiestRegistry: " + RtpInstallations.LiestRegistry());
        AssertTrue(!RtpInstallations.LiestRegistry(),
            "**and no registry key is read** -- and the keys are"
                + " `HKLM/SOFTWARE/WOW6432Node/Enterbrain` and"
                + " `HKCU/SOFTWARE/Enterbrain`, -- and neither"
                + " exists on this machine, -- and a stale entry"
                + " would report a runtime as missing that is"
                + " installed");
    }
}
