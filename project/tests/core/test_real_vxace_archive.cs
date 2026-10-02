using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Godot;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The one finished RPG Maker VX Ace game on this machine, and the archive
/// that carries all of it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And criterion 6 was measured against ninety-three loose
/// <c>.rb</c> files taken from a game's mod script directories.</strong> Those
/// were real game scripts, and they were read as Ruby 1.9.2. <strong>But they
/// were not what the engine loads</strong> -- <strong>and the engine loads
/// <c>Data/Scripts.rvdata2</c>, and this game has no <c>Data</c>
/// directory at all.</strong>
/// </para>
/// <para>
/// <strong>And that is the whole of this game on disk:</strong>
/// <c>Game.rgss3a</c>, twenty-eight megabytes, one header and the data
/// directory inside it. <strong>So the archive reader is the only way to
/// the game's own <c>Scripts.rvdata2</c>, and until this test it had never
/// been pointed at one.</strong>
/// </para>
/// </remarks>
public partial class TestRealVxAceArchive : TestBase
{
    private const string Wurzel = "E:/RPGMakerGames/Dreaming Mary";

    /// <summary>
    /// The reader this repository uses, created here so that no test reaches
    /// for a static that does not exist.
    /// </summary>
    private readonly RgssArchiveReader _archiv = new();

    private static bool Vorhanden()
    {
        if (!File.Exists(Wurzel + "/Game.rgss3a"))
        {
            GD.Print(
                "    (skipped: no Game.rgss3a at " + Wurzel + " -- the VX Ace"
                + " archive reader was not measured against a game's own"
                + " archive, and the ninety-three loose scripts are not the"
                + " same thing)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The archive header names the format, and the version byte says which
    /// of the three generations wrote it.
    /// </summary>
    public void Test_DerKopfNenntFormatUndGeneration()
    {
        if (!Vorhanden())
        {
            return;
        }

        var bytes = File.ReadAllBytes(Wurzel + "/Game.rgss3a");
        AssertTrue(RgssArchiveReader.HasArchiveHeader(bytes),
            "**and the file starts with the archive header** -- and RGSSAD is "
                + "six bytes, and RPG Maker writes it, and no other engine does");
        AssertEq((int)RgssArchiveReader.ReadVersion(bytes)!,
            (int)RgssArchiveReader.VersionVxAce,
            "**and it is version three, which is VX Ace** -- and version one "
                + "is XP and VX, and a reader that accepted version one here "
                + "would read every entry offset wrong");
    }

    /// <summary>
    /// An entry list that cannot be followed is refused with a reason, and
    /// not with a crash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test exists because the first version of it took the
    /// archive reader down.</strong> On this archive <c>ListEntries</c> threw
    /// <c>OverflowException</c> out of <c>new char[nameLength]</c>,
    /// <strong>because a decoded length above <c>int.MaxValue</c> becomes a
    /// negative <c>int</c> and the guard that followed only tested the upper
    /// bound.</strong>
    /// </para>
    /// <para>
    /// <strong>And the crash is gone, and the failure is a sentence.</strong>
    /// </para>
    /// <para>
    /// <strong>What is not in, and is not claimed:</strong> this archive's
    /// entries do not list. The key derivation is not understood, and four
    /// materially different hypotheses were tried and all four were wrong
    /// -- <strong>the generator does not advance as this reader advances it at
    /// this point in the stream.</strong> So <strong>the RGSS archive reader
    /// remains unmeasured against a real finished VX Ace game</strong>, and
    /// the ninety-three loose scripts stay the only VX Ace evidence this
    /// repository has. <strong>See the BLOCKED entry in
    /// SESSION_STATE.md for the exact unblock condition.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineNichtFolgbareEintragslisteWirdAbgelehntUndNichtAbgestuerzt()
    {
        if (!Vorhanden())
        {
            return;
        }

        var bytes = File.ReadAllBytes(Wurzel + "/Game.rgss3a");
        var gelistet = new RgssArchiveReader().ListEntries(bytes, "Game.rgss3a");

        // **Und beides ist zulaessig: eine Liste oder eine Ablehnung mit
        // Grund. Ein Absturz ist es nicht mehr, und das ist der Punkt.**
        if (!gelistet.Success)
        {
            AssertTrue(gelistet.Error != null,
                "**and a refusal names its reason**");
            System.Console.WriteLine(
                "VXAce Archiv: nicht lesbar -- "
                + gelistet.Error!.Message);
            return;
        }

        var eintraege = gelistet.Value!;
        AssertTrue(eintraege.Count > 500,
            "**and a listing that succeeds lists the whole game** -- "
                + eintraege.Count);
    }
}
