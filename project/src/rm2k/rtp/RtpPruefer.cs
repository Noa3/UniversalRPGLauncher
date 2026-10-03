using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Rtp;

/// <summary>
/// Whether a game needs an RTP that is not there yet.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because the detection carries only a name
/// and no state</strong>, -- <strong>which is measured</strong>:
///
/// <code>
/// RtpDependency   "which runtime the game's own file names"
/// MissingRtp      (no such word anywhere in src/)
/// </code>
///
/// <strong>And a name cannot answer the question the user asked</strong>,
/// -- <strong>which is "do I have to download something"</strong>, --
/// <strong>and a launcher that always asked would be asking about a
/// runtime it already has.</strong>
/// </para>
/// <para>
/// <strong>And the check is filesystem work and not a guess</strong>,
/// -- <strong>because the real answer depends on where the RTP was
/// installed</strong>, -- <strong>and those places are written in
/// <see cref="RtpInstallations"/> instead of being assembled here.</strong>
/// </para>
/// </remarks>
public static class RtpPruefer
{
    /// <summary>
    /// And what a check found.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a game that needs nothing gets
    /// <see cref="Fehlt"/> false and no message</strong>, --
    /// <strong>because "you need nothing" is not something a launcher
    /// should interrupt a user for.</strong>
    /// </para>
    /// </remarks>
    public sealed class Ergebnis
    {
        /// <summary>And whether the game names a runtime at all.</summary>
        public bool NenntRtp { get; init; }

        /// <summary>And whether it is missing.</summary>
        public bool Fehlt { get; init; }

        /// <summary>And which runtime, as the game's own file names it.</summary>
        public string Nennung { get; init; } = "";

        /// <summary>And which measured archive would answer it.</summary>
        public string EngineId { get; init; } = "";

        /// <summary>And where it would be looked for, if it were there.</summary>
        public IReadOnlyList<string> GesuchteOrte { get; init; } =
            Array.Empty<string>();

        /// <summary>
        /// And a line a launcher can show, and never a guess.
        /// </summary>
        public string Meldung { get; init; } = "";
    }

    /// <summary>
    /// And asks whether one game needs something it does not have.
    /// </summary>
    /// <param name="pNennung">The runtime the game's own file names.</param>
    /// <returns>The answer, and never an exception.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And an empty name means the game is
    /// <c>FullPackageFlag</c> and needs no RTP</strong>, --
    /// <strong>and that is measured on real games:</strong>
    /// <c>RTP=</c> appears in VX and XP <c>Game.ini</c> files next to
    /// <c>RTP=RPGVX</c> ones, -- <strong>and an empty value is the
    /// form that says "do not look for anything".</strong>
    /// </para>
    /// <para>
    /// <strong>And a name that has no measured archive is reported
    /// rather than resolved</strong>, -- <strong>because a mapping a
    /// reader invents is how the wrong runtime gets
    /// installed.</strong>
    /// </para>
    /// </remarks>
    public static Ergebnis Pruefe(string? pNennung)
    {
        if (string.IsNullOrWhiteSpace(pNennung))
        {
            return new Ergebnis
            {
                NenntRtp = false,
                Fehlt = false,
            };
        }

        var nennung = pNennung.Trim();

        if (!RtpArchivFakten.Alle().TryGetValue(nennung, out var fakt))
        {
            return new Ergebnis
            {
                NenntRtp = true,
                Fehlt = false,
                Nennung = nennung,
                Meldung = "Die Dateien des Spiels nennen \"" + nennung
                    + "\", -- und dafuer steht in der gemessenen"
                    + " Tabelle kein Archiv, -- und eine Zuordnung zu"
                    + " erfinden wuerde die falsche Laufzeit"
                    + " installieren.",
            };
        }

        var orte = RtpInstallations.OrteFuer(nennung);
        foreach (var ort in orte)
        {
            if (Directory.Exists(ort))
            {
                return new Ergebnis
                {
                    NenntRtp = true,
                    Fehlt = false,
                    Nennung = nennung,
                    EngineId = nennung,
                    GesuchteOrte = orte,
                };
            }
        }

        return new Ergebnis
        {
            NenntRtp = true,
            Fehlt = true,
            Nennung = nennung,
            EngineId = nennung,
            GesuchteOrte = orte,
            Meldung = "Dieses Spiel braucht die Laufzeit \"" + nennung
                + "\", -- und sie ist an keiner der " + orte.Count
                + " Stellen, -- an denen RPG Maker sie sonst"
                + " ablegt.",
        };
    }
}
