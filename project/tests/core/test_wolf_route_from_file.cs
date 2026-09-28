using System;
using System.IO;
using System.Linq;

using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What a move route in a map file has to survive on its way to the VM.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The VM ran two opcodes the map reader never produced.</strong>
/// <c>MoveRoute</c> and <c>WaitUntilRouteDone</c> were in the VM's dispatch and
/// in the opcode enum, and <c>ParseOpcode</c> had no name for either — so a map
/// file that said "move_route" arrived as <c>Unknown</c>, which the VM refuses.
/// A patrol written in the editor stood still, and nothing said why.
/// </para>
/// <para>
/// <strong>Every other test built its command by hand</strong>, which is why
/// this went unnoticed: a hand-built command has the character id and the route
/// already filled in, and the file path is the one that has to fill them.
/// </para>
/// </remarks>
public partial class TestWolfRouteFromFile : TestBase
{
    private const string TempBase = "user://wolf_route_file_test";

    /// <summary>A map with one event whose move route has to arrive intact.</summary>
    private static string MapWithRoute()
    {
        return "{\"format\":\"urpg-wolf-plain-json\",\"version\":1,\"kind\":\"map\","
            + "\"id\":1,\"name\":\"Start\",\"width\":8,\"height\":8,"
            // **The tile count is the map area and not a fixed four.** The
            // reader checks it against width times height — which is the check
            // that would catch a file whose header and body disagree — so a
            // fixture with four tiles on an eight by eight map is a file the
            // reader is right to refuse, and the test would be measuring the
            // refusal rather than the route.
            + "\"tiles\":["
            + string.Join(",", Enumerable.Range(0, 64).Select(i => (i % 4) + 1))
            + "],"
            + "\"events\":[{\"id\":1,\"x\":2,\"y\":2,\"commands\":["
            + "{\"op\":\"move_route\",\"character\":3,\"route\":{"
            + "\"mode\":\"custom\",\"wait\":1,\"steps\":["
            + "{\"type\":\"move_right\",\"args\":[]},"
            + "{\"type\":\"facing_up\",\"args\":[]},"
            + "{\"type\":\"wait_x_frames\",\"args\":[30]}"
            + "]}},"
            + "{\"op\":\"move_route_wait\"},"
            + "{\"op\":\"end\"}"
            + "]}]}";
    }

    public override void Setup()
    {
        Cleanup();
        var path = Global(TempBase.PathJoin("Data/MapData/Map001.mps"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, MapWithRoute());
    }

    public override void Teardown() => Cleanup();

    private static string Global(string pPath) =>
        Godot.ProjectSettings.GlobalizePath(pPath);

    private void Cleanup()
    {
        var path = Global(TempBase);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    private WolfMapData Read()
    {
        var reader = new WolfMapReader(new WolfParseLimits());
        var result = reader.Read(
            Global(TempBase.PathJoin("Data/MapData/Map001.mps")));
        AssertTrue(
            result.Success,
            $"the map file reads, but the reader said: {result.Error?.Message}");
        AssertTrue(result.Value != null, "and the result carries a map");
        return result.Value!;
    }

    // ---- Der Befehl kommt an

    /// <summary>
    /// A move route in the file is a move route in the program, and not Unknown.
    /// </summary>
    /// <remarks>
    /// <strong>This is the bug this file exists for.</strong> The VM's dispatch had
    /// the case and the enum had the value, and the map reader had no name for
    /// it — so the instruction arrived as something the VM refuses, and a
    /// patrol in a real game stood still with no diagnostic anywhere.
    /// </remarks>
    public void Test_AMoveRouteInTheFileIsAMoveRoute()
    {
        var map = Read();
        var command = map.Events[0].Commands[0];

        AssertEq(
            command.Opcode, WolfEventOpcode.MoveRoute,
            "**and the first command is the route the file asked for**, and not"
            + $" the unknown opcode the reader used to produce; it is {command.Opcode}");
    }

    /// <summary>
    /// The command names the figure it drives.
    /// </summary>
    /// <remarks>
    /// <strong>Without it the route drives nobody.</strong> A reader that filled
    /// no character id would reach the VM with an instruction to start a route
    /// with no figure, and the route would finish at once and report itself
    /// done — so the event would run, the route would be "finished", and the
    /// guard would not move.
    /// </remarks>
    public void Test_TheRouteNamesItsCharacter()
    {
        var map = Read();

        AssertEq(
            map.Events[0].Commands[0].CharacterId, 3,
            "**and the figure is the one the file named**;"
            + $" it is {map.Events[0].Commands[0].CharacterId}");
    }

    /// <summary>
    /// The steps arrive, in order, with their numbers.
    /// </summary>
    /// <remarks>
    /// <strong>Three steps and their arguments.</strong> The wait step carries
    /// thirty frames, and a reader that dropped the argument list would have the
    /// runner refuse the step as impossible — so a route with a pause in it
    /// would stop at the pause.
    /// </remarks>
    public void Test_TheStepsArriveInOrder()
    {
        var map = Read();
        var route = map.Events[0].Commands[0].Route;

        AssertTrue(route != null, "**and the route is there**; it is null");
        AssertEq(
            route!.Steps.Count, 3,
            "**and it has all three steps**;"
            + $" it has {route.Steps.Count}");
        AssertEq(
            route.Steps[0].Type, WolfMoveRouteType.MoveRight,
            "**and the first is a step to the right**;"
            + $" it is {route.Steps[0].Type}");
        AssertEq(
            route.Steps[1].Type, WolfMoveRouteType.FacingUp,
            "**and the second turns the figure up**, which is a facing step and"
            + $" not a move; it is {route.Steps[1].Type}");
        AssertEq(
            route.Steps[2].Type, WolfMoveRouteType.WaitXFrames,
            "**and the third waits**; it is {route.Steps[2].Type}");
        AssertEq(
            route.Steps[2].Arguments.Count, 1,
            "**and the wait carries its frame count**;"
            + $" it carries {route.Steps[2].Arguments.Count}");
        AssertEq(
            route.Steps[2].Arguments[0], 30,
            "**which is thirty**; it is {route.Steps[2].Arguments[0]}");
    }

    /// <summary>
    /// The wait flag and the mode arrive.
    /// </summary>
    /// <remarks>
    /// <strong>A route that waits and a route that does not are different
    /// games.</strong> A reader that dropped the flag would have every patrol
    /// block its event until it stopped, and a reader that defaulted the mode to
    /// zero would have stood every figure still — because zero is "do not move".
    /// </remarks>
    public void Test_TheWaitFlagAndTheModeArrive()
    {
        var map = Read();
        var route = map.Events[0].Commands[0].Route!;

        AssertEq(
            route.WaitUntilDone, true,
            "**and the route waits**, because the file asked it to;"
            + $" it is {route.WaitUntilDone}");
        AssertEq(
            route.Mode, WolfMoveRouteMode.Custom,
            "**and it is driven by its own steps**, and not by the default that"
            + " is the same number; it is {route.Mode}");
    }

    /// <summary>
    /// The waiting opcode is in the file's vocabulary too.
    /// </summary>
    /// <remarks>
    /// <strong>The pair, and not the one.</strong> A route that starts without
    /// waiting and an event that waits for it are two commands, and a reader
    /// that mapped only the first would leave every event carrying on while its
    /// figure was still walking.
    /// </remarks>
    public void Test_TheWaitOpcodeIsInTheVocabulary()
    {
        var map = Read();

        AssertEq(
            map.Events[0].Commands[1].Opcode, WolfEventOpcode.WaitUntilRouteDone,
            "**and the second command waits for the route**, which is the"
            + $" separate opcode the file named; it is {map.Events[0].Commands[1].Opcode}");
    }

    // ---- Was der Leser nicht erfindet

    /// <summary>
    /// A name the reader does not know is not the first step type.
    /// </summary>
    /// <remarks>
    /// <strong>0xFF and not 0.</strong> The verified step types run from 0x00 to
    /// 0x3A, and 0x00 is a step down — so a reader that fell back to zero would
    /// turn a typo in a step name into a figure walking one tile south, and the
    /// game would look right until the day it did not.
    /// </remarks>
    public void Test_AnUnknownStepNameIsNotAStepDown()
    {
        var path = Global(TempBase.PathJoin("Data/MapData/Map002.mps"));
        File.WriteAllText(path,
            "{\"format\":\"urpg-wolf-plain-json\",\"version\":1,\"kind\":\"map\","
            + "\"id\":2,\"name\":\"Typo\",\"width\":4,\"height\":4,\"tiles\":[1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4],"
            + "\"events\":[{\"id\":1,\"x\":0,\"y\":0,\"commands\":["
            + "{\"op\":\"move_route\",\"character\":1,\"route\":{"
            + "\"steps\":[{\"type\":\"move_rite\",\"args\":[]}]}}]}]}");

        var reader = new WolfMapReader(new WolfParseLimits());
        var result = reader.Read(path);
        var step = result.Value!.Events[0].Commands[0].Route!.Steps[0];

        AssertEq(
            step.Type, (byte)0xFF,
            "**and the unknown name is the unmapped byte**, which the runner"
            + $" refuses; it is {step.Type}");
    }

    /// <summary>
    /// An unknown mode is a custom route and not a standing one.
    /// </summary>
    /// <remarks>
    /// <strong>Mode 0 means "do not move".</strong> A reader that defaulted an
    /// unmapped name to zero would take a patrol whose mode it did not
    /// recognise and stand it still, with no error and no movement.
    /// </remarks>
    public void Test_AnUnknownModeIsACustomRoute()
    {
        var path = Global(TempBase.PathJoin("Data/MapData/Map003.mps"));
        File.WriteAllText(path,
            "{\"format\":\"urpg-wolf-plain-json\",\"version\":1,\"kind\":\"map\","
            + "\"id\":3,\"name\":\"Mode\",\"width\":4,\"height\":4,\"tiles\":[1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4],"
            + "\"events\":[{\"id\":1,\"x\":0,\"y\":0,\"commands\":["
            + "{\"op\":\"move_route\",\"character\":1,\"route\":{"
            + "\"mode\":\"sideways\",\"steps\":[]}}]}]}");

        var reader = new WolfMapReader(new WolfParseLimits());
        var result = reader.Read(path);

        AssertEq(
            result.Value!.Events[0].Commands[0].Route!.Mode,
            WolfMoveRouteMode.Custom,
            "**and an unmapped mode runs the route's own steps**, and not the"
            + " zero that means the figure stands still; it is "
            + result.Value!.Events[0].Commands[0].Route!.Mode);
    }

    /// <summary>
    /// A command with no route has none, and not an empty route.
    /// </summary>
    /// <remarks>
    /// <strong>Null and not an empty object.</strong> A reader that built an
    /// empty route for every command would make a message command carry a route
    /// with no steps, and anything that asked "does this command have a route"
    /// would get the wrong answer from every command in the game.
    /// </remarks>
    public void Test_ACommandWithNoRouteHasNone()
    {
        var map = Read();

        AssertEq(
            map.Events[0].Commands[2].Route, null,
            "**and the end command carries no route**, which is what a file that"
            + " wrote none asked for;"
            + $" it is {(map.Events[0].Commands[2].Route == null ? "null" : "a route")}");
    }

    /// <summary>
    /// A flag is one and not any number.
    /// </summary>
    /// <remarks>
    /// <strong>Only 1 is true.</strong> A file that wrote 2 into a flag is a file
    /// that is wrong, and a reader that treated it as true would make a route
    /// that does not wait wait — a guard that stops on the staircase, every
    /// time.
    /// </remarks>
    public void Test_AFlagIsOneAndNotAnyNumber()
    {
        var path = Global(TempBase.PathJoin("Data/MapData/Map004.mps"));
        File.WriteAllText(path,
            "{\"format\":\"urpg-wolf-plain-json\",\"version\":1,\"kind\":\"map\","
            + "\"id\":4,\"name\":\"Flag\",\"width\":4,\"height\":4,\"tiles\":[1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4],"
            + "\"events\":[{\"id\":1,\"x\":0,\"y\":0,\"commands\":["
            + "{\"op\":\"move_route\",\"character\":1,\"route\":{"
            + "\"wait\":2,\"skip\":1,\"repeat\":0,\"steps\":[]}}]}]}");

        var reader = new WolfMapReader(new WolfParseLimits());
        var result = reader.Read(path);
        var route = result.Value!.Events[0].Commands[0].Route!;

        AssertEq(
            route.WaitUntilDone, false,
            "**and a wait of 2 is not a wait**, because only 1 is true;"
            + $" it is {route.WaitUntilDone}");
        AssertEq(
            route.SkipImpossibleMoves, true,
            "**and a skip of 1 is a skip**; it is {route.SkipImpossibleMoves}");
    }
}
