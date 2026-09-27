using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A real RPG Maker 2000 game's own database, read as bytes.
/// </summary>
/// <remarks>
/// <para>
/// The fixtures below are the database, the map tree and two maps of a real
/// RM2K game, taken from a game the repository was given and frozen here. They
/// are far larger than the test game this parser was written against: a database
/// of four hundred and sixteen thousand bytes and a map tree of fifty seven,
/// against two hundred and ten and six, from a game with seven hundred and forty
/// three maps.
/// </para>
/// <para>
/// A small test game is small on purpose, and it is untested on the things a
/// game with hundreds of actors, a hundred maps and a long party list actually
/// holds. This is the first real RM2K data in the repository that is not the
/// test game.
/// </para>
/// </remarks>
partial class TestRealRm2kData : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/rm2k-dragon-destiny";

    private Rm2kParser _parser = null!;

    public override void Setup()
    {
        _parser = new Rm2kParser();
    }

    public void Test_ThisGamesDatabaseIsReadWholeAndIsTwiceTheTestGamesSize()
    {
        var result = _parser.ParseDatabase(FixtureRoot.PathJoin("RPG_RT.ldb"));
        AssertTrue(result.IsSuccess(), $"the database is read: {Describe(result)}");
        if (!result.IsSuccess())
        {
            return;
        }

        var data = result.GetData();
        AssertEq(data["header"].AsString(), "LcfDataBase", "and it is a database");
        AssertTrue(
            (long)data["file_size"] > 400000,
            $"holding four hundred thousand bytes, {(long)data["file_size"]}");
        AssertTrue(
            data["chunk_count"].AsInt32() > 0,
            $"with {data["chunk_count"].AsInt32()} chunks in it");
    }

    public void Test_TheMapTreeOfThisGameHoldsFiftyThousandBytesOfMaps()
    {
        var tree = _parser.ParseMapTree(FixtureRoot.PathJoin("RPG_RT.lmt"));
        AssertTrue(tree.IsSuccess(), $"the map tree is read: {Describe(tree)}");
        if (!tree.IsSuccess())
        {
            return;
        }

        var data = tree.GetData();
        AssertEq(data["header"].AsString(), "LcfMapTree", "and it is a map tree");
        // The test game has a handful of maps and this game has hundreds, so a
        // tree whose length is held in one byte somewhere would pass on the first
        // and fail on this one.
        AssertTrue(
            (long)data["file_size"] > 50000,
            $"holding fifty thousand bytes of maps, {(long)data["file_size"]}");
    }

    public void Test_TwoMapsOfThisGameAreRead()
    {
        foreach (var name in new[] { "Map0001.lmu", "Map0100.lmu" })
        {
            var map = _parser.ParseMap(FixtureRoot.PathJoin(name));
            AssertTrue(map.IsSuccess(), $"{name} is read: {Describe(map)}");
            if (!map.IsSuccess())
            {
                continue;
            }

            var data = map.GetData();
            // Measured from this game, not from the test game: a map unit is
            // what the engine writes into a .lmu, and the test game's own maps
            // agree, but the name is not one to be remembered.
            AssertEq(
                data["header"].AsString(), "LcfMapUnit",
                $"{name} is a map unit");
            AssertTrue(
                (long)data["width"] > 0 && (long)data["height"] > 0,
                $"{name} has the size the game gave it,"
                + $" {data["width"]} by {data["height"]}");
        }
    }

    private static string Describe(Rm2kParser.ParseResult pResult) =>
        pResult.Error?.Message ?? "no error, and no data";
}
