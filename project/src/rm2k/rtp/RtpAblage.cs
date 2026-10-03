using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Rtp;

/// <summary>
/// Where a fetched runtime is put, and that is the caller's choice.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a type and not a decision</strong>, because the
/// two places are not equivalent and only the user knows which one
/// they want.
/// </para>
/// <para>
/// <strong>And they are not equivalent, and this is why:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><c>user://</c> needs no right, and
/// <strong>a real RPG Maker game does not look there</strong> --
/// it looks in <c>Common Files</c> -- <strong>so a runtime put there
/// serves this launcher and nothing else.</strong></description></item>
/// <item><description><c>Common Files</c> is where RPG Maker writes,
/// so the games find it -- <strong>and writing there needs an
/// elevation this launcher does not take on its own.</strong></description></item>
/// </list>
/// <para>
/// <strong>And that is the whole reason this type exists</strong>,
/// -- <strong>and a class that picked one would be making a product
/// decision this repository cannot make.</strong>
/// </para>
/// </remarks>
public sealed class RtpAblage
{
    /// <summary>
    /// And the place the launcher may always write to.
    /// </summary>
    /// <param name="pEngineId">Which runtime.</param>
    /// <returns>A directory under <c>user://</c>, and it may not exist.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the path is
    /// <c>user://rtp/&lt;engine&gt;</c></strong>, --
    /// <strong>and it is beside <c>user://library.cfg</c></strong>,
    /// -- <strong>which is where this application already keeps what it
    /// knows.</strong>
    /// </para>
    /// </remarks>
    public static string Benutzer(string pEngineId) => Path.Combine(
        "user://rtp", pEngineId);

    /// <summary>
    /// And the place RPG Maker itself would write to.
    /// </summary>
    /// <param name="pEngineId">Which runtime.</param>
    /// <returns>The first existing path, or the first candidate.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the first existing one wins</strong>, --
    /// <strong>because that is where an installed runtime already
    /// lives and a second copy next to it would be a second runtime.</strong>
    /// </para>
    /// </remarks>
    public static string System(string pEngineId)
    {
        var orte = RtpInstallations.OrteFuer(pEngineId);
        foreach (var ort in orte)
        {
            if (Directory.Exists(ort))
            {
                return ort;
            }
        }

        return orte.Count > 0 ? orte[0] : string.Empty;
    }

    /// <summary>
    /// And whether this process may write there without elevating.
    /// </summary>
    /// <param name="pPfad">The directory.</param>
    /// <returns>Yes or no, and it is measured, not assumed.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured and not assumed</strong>, --
    /// <strong>because a launcher that promises a write it cannot do
    /// fails after the download instead of before it.</strong>
    /// </para>
    /// </remarks>
    public static bool Schreibbar(string pPfad)
    {
        if (string.IsNullOrWhiteSpace(pPfad))
        {
            return false;
        }

        try
        {
            var probe = Path.Combine(pPfad, ".urpg_schreibtest");
            var ordner = Path.GetDirectoryName(probe);
            if (!string.IsNullOrEmpty(ordner))
            {
                Directory.CreateDirectory(ordner);
            }

            File.WriteAllText(probe, "x");
            File.Delete(probe);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// And every place a runtime could be looked for, in the order a
    /// launcher should try them.
    /// </summary>
    /// <param name="pEngineId">Which runtime.</param>
    /// <returns>The paths, and never empty for a known engine.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the user's own directory comes first</strong>, --
    /// <strong>because that is the one this launcher can always
    /// write</strong>, -- <strong>and the system paths follow</strong>,
    /// -- <strong>because a runtime that is already installed must be
    /// found before one is fetched.</strong>
    /// </para>
    /// </remarks>
    public static IReadOnlyList<string> Orte(string pEngineId)
    {
        var orte = new List<string> { Benutzer(pEngineId) };
        orte.AddRange(RtpInstallations.OrteFuer(pEngineId));
        return orte;
    }
}
