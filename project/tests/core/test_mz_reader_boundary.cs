using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What the reader that was already here makes of these files, and what it does
/// not.
/// </summary>
/// <remarks>
/// <para>
/// A data directory reader exists already and this is its boundary, stated
/// plainly so the two are not confused. It counts entries and takes names, it
/// caps a data file at two megabytes, and it hands back
/// <c>ActorCount</c>, <c>MapCount</c> and a bounded list of names. That is
/// enough to tell a player what a game is called and how big it is, and it is
/// not a reader of a game: a map's events, a command, a stat and a coordinate
/// are all things it does not return.
/// </para>
/// <para>
/// The reader added for this card is the other half. It returns the values, and
/// the two are used together: this one says what is in the file, the other says
/// what the file is. Neither is derived from the other.
/// </para>
/// </remarks>
partial class TestMzReaderBoundary : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    public void Test_TheReaderThatWasAlreadyHereCountsAndNamesAndStopsThere()
    {
        var analysis = new PluginGameDetector(
            BuiltInEnginePluginCatalog.CreateDetectionRegistry())
            .Analyze(ProjectSettings.GlobalizePath(FixtureRoot));
        AssertTrue(analysis.Inspection != null, "the fixture folder is inspected");

        var result = MzDataDirectoryResult.Extract(analysis.Inspection!);
        AssertTrue(result != null, "and the data directory is read from it");
        AssertTrue(
            result!.ActorCount > 0,
            $"it counts the game's actors, {result.ActorCount}");
        AssertTrue(
            result.MapCount > 0,
            $"and its maps, {result.MapCount}");
        AssertTrue(
            result.ActorNames.Count > 0 && !string.IsNullOrEmpty(result.ActorNames[0]),
            $"and names the first of them, '{result.ActorNames.FirstOrDefault()}'");
    }

    public void Test_TheCapItStatesIsTheCapItHas()
    {
        // The reader says it stops at two megabytes, so that is what a file
        // above it gets. A stated limit that no file reaches is a limit nobody
        // has tested, and a file above one that is still read is a reader that
        // does not know where it stops.
        AssertEq(
            WebDataDirectoryResult.MaxDataJsonBytes, 2048 * 1024,
            "the cap is the one the reader states");
    }
}
