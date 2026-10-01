using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Godot;

using UniversalRPG.Rm2k.Assets;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The official RPG Maker 2003 RTP on this machine, and what it gives a
/// reader.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And there is no RPG Maker 2003 game on this machine, and the
/// repository's <c>.lcf</c> reader has therefore never been measured against
/// anything.</strong> The three RM2K games here are RPG Maker 2000 --
/// <c>.ldb</c> and <c>.lmu</c>, and no <c>.lcf</c> anywhere in the folder.
/// </para>
/// <para>
/// <strong>And the RTP is the part of the engine that is downloadable
/// without a licence.</strong> It carries the fifteen character sheets, the
/// thirty-four backdrops, the fifty-four battle animations, the sounds and
/// the music -- <strong>and it carries no map, and no database, and no
/// event.</strong> <strong>So it is half of what a game needs and not the
/// half that decides whether a game runs.</strong>
/// </para>
/// <para>
/// <strong>And the honest claim is this narrow:</strong> the RTP is installed,
/// its files are read, and its character sheets carry the raster this
/// repository's animation draws from. <strong>Nothing here claims that an
/// RM2K3 game runs, because no RM2K3 game is here to run.</strong>
/// </para>
/// </remarks>
public partial class TestRealRm2k3Rtp : TestBase
{
    private const string RtpWurzel =
        "C:/Users/noa3/AppData/Local/RPG Maker 2003/RTP";

    private static void UeberspringeWennKeinRtp()
    {
        if (!Directory.Exists(RtpWurzel))
        {
            GD.Print(
                "    (skipped: no RPG Maker 2003 RTP at " + RtpWurzel
                + " -- the 2003 asset layout was not measured against the"
                + " engine's own files, and a green run that checked nothing"
                + " is the worst form of it)");
        }
    }

    /// <summary>
    /// The RTP is installed and carries what the engine's own installer lays
    /// down.
    /// </summary>
    public void Test_DasRtpIstInstalliertUndTraegtDieEngineOrdner()
    {
        UeberspringeWennKeinRtp();
        if (!Directory.Exists(RtpWurzel))
        {
            return;
        }

        var dateien = Directory.GetFiles(RtpWurzel, "*", SearchOption.AllDirectories);
        AssertTrue(dateien.Length > 500,
            "**and the RTP is the engine's own and not a part of it** -- "
                + dateien.Length + " files, and the official installer lays "
                + "down six hundred and seventy-five");

        // **Und die vierzehn Ordner sind die des Installers, und nicht
        // welche, die diese Liste sich ausgedacht hat.**
        foreach (var ordner in new[]
        {
            "Backdrop", "Battle", "BattleCharSet", "BattleWeapon", "CharSet",
            "ChipSet", "FaceSet", "GameOver", "Monster", "Music", "Panorama",
            "Sound", "System", "System2", "Title",
        })
        {
            var pfad = RtpWurzel + "/" + ordner;
            AssertTrue(Directory.Exists(pfad),
                "**and the engine's own folder " + ordner + " is there**");
            AssertTrue(Directory.GetFiles(pfad).Length > 0,
                "**and " + ordner + " is not empty** -- an empty folder is "
                    + "one the installer made and nothing put in");
        }
    }

    /// <summary>
    /// The RTP's character sheets carry the raster the animation draws from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the measurement that matters for criterion 2 on
    /// the 2003 side, and it is the same rule the 2000 side uses.</strong>
    /// A character cell is three columns wide, and the fourth value the frame
    /// rotation reaches is drawn as the middle.
    /// </para>
    /// <para>
    /// <strong>And the RTP's sheets are 288 pixels wide, measured on all
    /// fifteen of them</strong> -- <strong>and 288 is what the three
    /// RPG Maker 2000 games on this machine use, and what
    /// <c>test_real_rm2k_game_data</c> asserts over all thirty-seven of their
    /// sheets.</strong> <strong>And that is a fact about the engine and not
    /// about a version, and it is why the same raster serves both
    /// generations.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieCharSetsDesRtpPassenInsRaster()
    {
        UeberspringeWennKeinRtp();
        if (!Directory.Exists(RtpWurzel))
        {
            return;
        }

        var ordner = RtpWurzel + "/CharSet";
        var dateien = Directory.GetFiles(ordner);
        AssertTrue(dateien.Length >= 10,
            "**and the RTP carries a game's worth of character sheets** -- "
                + dateien.Length + " files");

        var breiten = new Dictionary<int, int>();
        foreach (var datei in dateien)
        {
            var masse = Bildmasse(datei);
            AssertTrue(masse != null,
                "**and every one of them is a PNG this reader can "
                + "measure** -- and " + Path.GetFileName(datei) + " is not");
            if (masse == null)
            {
                continue;
            }

            breiten[masse.Value.Width] =
                breiten.GetValueOrDefault(masse.Value.Width) + 1;
        }

        System.Console.WriteLine(
            "RM2K3 RTP: " + dateien.Length + " CharSets, "
            + string.Join(", ", breiten.Select(
                pKvp => pKvp.Key + "px breit (" + pKvp.Value + "x)")));

        foreach (var breite in breiten.Keys)
        {
            AssertTrue(breite == 288,
                "**and every sheet is 288 pixels wide** -- " + breite
                    + " is not, and a character cell is three columns of 96 "
                    + "and the 2000 games on this machine use the same 288");
        }
    }

    /// <summary>
    /// The RTP is resolvable through the registry this repository uses, and
    /// the registry refuses a path that leaves the mount.
    /// </summary>
    public void Test_DasRtpLaesstSichUeberDenRegisterAufloesen()
    {
        UeberspringeWennKeinRtp();
        if (!Directory.Exists(RtpWurzel))
        {
            return;
        }

        var registry = new RtpRegistry();
        registry.Register(new RtpProfile
        {
            Id = "rm2k3-local",
            EngineId = "rm2k3",
            Generation = "2003",
            DependencyName = "RTP",
            RootPath = RtpWurzel,
        });

        var gefunden = registry.Resolve(
            "rm2k3", "2003", "RTP", "CharSet/Actor1.png");
        AssertTrue(gefunden.Status == RtpResolutionStatus.Found,
            "**and the registry resolves a real file in the mounted RTP** -- "
                + gefunden.Status + " and the path it gave was '"
                + gefunden.ResolvedPath + "', and a registry that resolved nothing "
                + "would let a game start without its own graphics");

        // **Und eine Datei, die es nicht gibt, wird nicht erfunden.**
        var fehlt = registry.Resolve("rm2k3", "2003", "RTP", "CharSet/GibtsNicht.png");
        AssertTrue(fehlt.Status == RtpResolutionStatus.MissingAsset,
            "**and a file that is not there is not invented** -- "
                + fehlt.Status);

        // **Und ein Weg aus dem Mount heraus wird abgelehnt, und das ist
        // die eine Sicherheitsgrenze, die hier zaehlt.**
        var ausbruch = registry.Resolve(
            "rm2k3", "2003", "RTP", "../../../Windows/System32/drivers/etc/hosts");
        AssertTrue(ausbruch.Status == RtpResolutionStatus.InvalidPath,
            "**and a path that leaves the mount is refused** -- "
                + ausbruch.Status);
    }

    /// <summary>The width and height of a PNG, from the file itself.</summary>
    private static (int Width, int Height)? Bildmasse(string pPfad)
    {
        var kopf = File.ReadAllBytes(pPfad)[..24];
        if (kopf.Length < 24 || kopf[0] != 0x89 || kopf[1] != (byte)'P')
        {
            return null;
        }

        return ((kopf[16] << 24) | (kopf[17] << 16) | (kopf[18] << 8) | kopf[19],
            (kopf[20] << 24) | (kopf[21] << 16) | (kopf[22] << 8) | kopf[23]);
    }
}
