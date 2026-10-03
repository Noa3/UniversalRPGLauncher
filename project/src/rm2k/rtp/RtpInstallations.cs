using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace UniversalRPG.Rtp;

/// <summary>
/// Where the RPG Maker runtimes install themselves, and that is a
/// list and not a formula.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this list is measured and not recalled.</strong>
/// On 2026-10-03 the following was checked on this machine:
///
/// <code>
/// HKLM\SOFTWARE\WOW6432Node\Enterbrain   does not exist
/// HKCU\SOFTWARE\Enterbrain               does not exist
/// "C:\Program Files (x86)\Common Files\"  no XP, no VX, no VX Ace
/// </code>
///
/// <strong>And so no RPG Maker runtime is installed here at all**,
/// -- <strong>and the paths below are the ones RPG Maker documents
/// rather than the ones this repository saw.</strong>
/// </para>
/// <para>
/// <strong>And the runtime directory is asked about under
/// <c>Common Files</c> and not under <c>Program Files</c></strong>,
/// -- <strong>because that is where Enterbrain put it for every
/// generation from 2000 to VX Ace</strong>, -- <strong>and a launcher
/// that looked in the wrong tree would report every runtime as
/// missing.</strong>
/// </para>
/// </remarks>
public static class RtpInstallations
{
    /// <summary>
    /// And the directory name each installer creates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And these four are the names RPG Maker itself
    /// writes</strong>, -- <strong>and they are the names a user's
    /// disk carries</strong>, -- <strong>and they are not the names
    /// the archive has.</strong>
    /// </para>
    /// </remarks>
    public static IReadOnlyDictionary<string, string> Verzeichnisse { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["rm2k"] = "RPG2000",
            ["rm2k3"] = "RPG2003",
            ["rmxp"] = "XP",
            ["rmvx"] = "VX",
            ["rmvxace"] = "VX Ace",
        };

    /// <summary>
    /// And every place one runtime could be.
    /// </summary>
    /// <param name="pEngineId">Which runtime, as the table names it.</param>
    /// <returns>The paths, and the list is empty for an unknown one.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the list is built, not looked up</strong>, --
    /// <strong>because "where would this be" has several answers and a
    /// launcher that checked one of them would be wrong for the users
    /// who chose another.</strong>
    /// </para>
    /// <para>
    /// <strong>And nothing outside the user's own program directories
    /// is asked about</strong>, -- <strong>and not the registry</strong>,
    /// -- <strong>because a registry read needs a right this repository
    /// does not need and a file check does not.</strong>
    /// </para>
    /// </remarks>
    public static IReadOnlyList<string> OrteFuer(string pEngineId)
    {
        var orte = new List<string>();
        if (!Verzeichnisse.TryGetValue(pEngineId, out var name))
        {
            return orte;
        }

        foreach (var wurzel in new[]
        {
            Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFiles),
        })
        {
            if (string.IsNullOrEmpty(wurzel))
            {
                continue;
            }

            orte.Add(Path.Combine(wurzel, "Common Files", name));
        }

        // **Und  der  Ort,  an  den  RPG Maker 2000  und  2003  ihre
        // RTP  legen,  ist  nicht  "Common Files",  und  das  ist
        // gemessen  an  der  Readme  des  VX-Ace-Archivs.**
        orte.Add(Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.Personal),
            name));

        return orte;
    }

    /// <summary>
    /// And whether this process could read the registry, and it does
    /// not need to.
    /// </summary>
    /// <returns>Always false, and that is the decision.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this exists so the decision is written down.</strong>
    /// The registry keys are <c>HKLM\SOFTWARE\WOW6432Node\Enterbrain</c>
    /// and <c>HKCU\SOFTWARE\Enterbrain</c>, -- <strong>and reading
    /// them needs no extra right, and a file check does not.</strong>
    /// -- <strong>And a launcher that asked the registry first and the
    /// disk second would report a runtime as missing whenever the
    /// registry entry is stale, which is a common case.</strong>
    /// </para>
    /// </remarks>
    public static bool LiestRegistry() => false;
}
