using System.Linq;

using Godot;

using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What the RGSS runtime says about the system data a generation needs.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A patch in <c>qa_patches/</c> from 2026-08-24 asked for this and
/// it was never written.</strong> <c>ExpectedSystemDataPath</c> and
/// <c>HasSystemData</c> are written by <c>Initialize</c> and read by nobody,
/// and the diagnostic <c>rgss.system-data-missing</c> is emitted in the code
/// and asserted nowhere.
/// </para>
/// <para>
/// <strong>It is a separate file because appending to the other one duplicated
/// the whole class twice in a row</strong> — which is the third time in this
/// repository that editing a test file by string surgery has cost more than it
/// saved.
/// </para>
/// </remarks>
public partial class TestRgssRuntime
{
    /// <summary>
    /// Each generation names its own system data, and it names it itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>XP reads <c>System.rxdata</c>, VX <c>System.rvdata</c>, VX Ace
    /// <c>System.rvdata2</c></strong> — and a runtime that answered
    /// <c>rxdata</c> for all three would point a VX Ace game at a file that
    /// does not exist there.
    /// </para>
    /// <para>
    /// <strong>The runtime is built and the path read from it.</strong> The
    /// first version of this test built a <c>RgssRuntimeInfo</c> and asserted
    /// on it, <strong>which proves nothing</strong> — it asserts that a value
    /// the test itself wrote is the value the test read back. The rule under
    /// test is the constructor's <c>switch</c>, and
    /// <c>ExpectedSystemDataPath</c> is public for exactly that reason:
    /// <strong>a field that only <c>Initialize</c> writes and nothing reads
    /// becomes wrong without a run noticing.</strong>
    /// </para>
    /// </remarks>
    public void Test_EachGenerationNamesItsOwnSystemDataPath()
    {
        var faelle = new[]
        {
            (Name: "XP", Generation: "xp", Daten: ".rxdata", Erwartet: "Data/System.rxdata"),
            (Name: "VX", Generation: "vx", Daten: ".rvdata", Erwartet: "Data/System.rvdata"),
            (Name: "VXAce", Generation: "vx-ace", Daten: ".rvdata2", Erwartet: "Data/System.rvdata2"),
        };

        foreach (var fall in faelle)
        {
            var laufzeit = Baue(fall.Generation, fall.Daten);
            AssertEq(laufzeit.ExpectedSystemDataPath, fall.Erwartet,
                $"**{fall.Name} names its own system data** — a runtime that "
                    + "answered rxdata for all three would point a VX Ace game "
                    + "at a file that does not exist there");
            AssertTrue(laufzeit.State == PluginRuntimeState.Created,
                $"**and the {fall.Name} runtime is built and untouched** — it "
                    + "was constructed and not started, and a runtime that ran "
                    + "here would have run something this project does not run");
        }
    }

    /// <summary>
    /// An unknown generation names no system data at all.
    /// </summary>
    /// <remarks>
    /// <strong>The default is the empty string and not rxdata.</strong> A
    /// reader that fell back to the XP path for a generation it does not know
    /// would name a file the game does not have — <strong>and that name is
    /// the first thing a person reads when a game does not start.</strong>
    /// </remarks>
    public void Test_AnUnknownGenerationNamesNoSystemData()
    {
        AssertEq(Baue("rgss9", ".rxdata").ExpectedSystemDataPath, "",
            "**an unknown generation names no path** — and not rxdata, because "
                + "a guess here would name a file the game does not have and "
                + "the name is what a person reads first when nothing starts");
        AssertEq(Baue("", ".rxdata").ExpectedSystemDataPath, "",
            "**and an empty generation names none either** — a caller that "
                + "leaves the generation out is answered with nothing rather "
                + "than with a guess");
    }

    /// <summary>
    /// The three generations name three different files, and none of them is
    /// the default.
    /// </summary>
    /// <remarks>
    /// <strong>Two cases that could agree would prove nothing.</strong> If XP
    /// and VX Ace named the same file, the first test would still pass while
    /// the runtime pointed half the engines at a file that is not there —
    /// <strong>so the test asks for three distinct answers and says
    /// why.</strong>
    /// </remarks>
    public void Test_TheThreeGenerationsNameThreeDifferentFiles()
    {
        var pfade = new[]
        {
            Baue("xp", ".rxdata").ExpectedSystemDataPath,
            Baue("vx", ".rvdata").ExpectedSystemDataPath,
            Baue("vx-ace", ".rvdata2").ExpectedSystemDataPath,
        };
        AssertEq(pfade.Distinct().Count(), 3,
            "**the three names are three and not one** — they are "
                + string.Join(", ", pfade) + ", and a runtime that answered "
                + "rxdata for all three would pass the first test and be wrong "
                + "about two engines");
        AssertTrue(!pfade.Any(pPfad => pPfad.Length == 0),
            "**and none of them is empty** — a generation this runtime claims "
                + "to support and cannot name a file for is a generation it "
                + "cannot read, and that should be visible here");
    }

    /// <summary>
    /// The runtime, built and not started.
    /// </summary>
    /// <remarks>
    /// <strong>No plugin, no probe, no host.</strong> The path is decided in
    /// the constructor, <strong>and the parts that need a plugin selection are
    /// parts this test is not about</strong> — the selector refuses RGSS with
    /// `UnsupportedEngine`, and that is a separate and separately tested
    /// decision.
    /// </remarks>
    private static RgssEngineRuntime Baue(string pGeneration, string pDatenEndung)
        => new(
            "plugin." + pGeneration,
            pGeneration,
            "RGSS",
            pDatenEndung,
            ".rgssad",
            new PluginGameInfo
            {
                GameDirectory = ProjectSettings.GlobalizePath(TempBase),
                EngineId = "plugin." + pGeneration,
            });
}
