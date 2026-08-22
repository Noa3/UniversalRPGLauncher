using System;
using System.IO;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

public partial class TestWolfRuntime : TestBase
{
    private const string TempBase = "user://wolf_runtime_test";

    public override void Setup()
    {
        Cleanup();
        WriteJson("Data/Game.dat", "{\"format\":\"urpg-wolf-plain-json\",\"version\":1,\"kind\":\"game\",\"title\":\"Synthetic Wolf\",\"protected\":false}");
        WriteJson("Data/BasicData/System.db", Database("system", "System", "WorldName", "Synthetic"));
        WriteJson("Data/BasicData/User.db", Database("user", "User", "Name", "Hero"));
        WriteJson("Data/BasicData/Variable.db", Database("variable", "Variables", "1", "5"));
        WriteJson("Data/MapData/Map001.mps", Map());
    }

    public override void Teardown() => Cleanup();

    public void Test_DetectsAndLoadsPlainWolfProject()
    {
        var root = Global(TempBase);
        var report = new PluginGameDetector(BuiltInEnginePluginCatalog.CreateDetectionRegistry()).Analyze(root);
        AssertEq(report.SelectedCandidate?.PluginId, EnginePluginIds.WolfRpg);
        AssertEq(report.SelectedCandidate?.Status, EngineDetectionStatus.Supported);

        var project = new WolfDataReader().Load(root);
        AssertTrue(project.Success, project.Error?.Message ?? "WOLF project load failed");
        AssertEq(project.Value?.Title, "Synthetic Wolf");
        AssertEq(project.Value?.SystemDatabase?.Records.Count, 1);
        AssertEq(project.Value?.UserDatabases.Count, 1);
        AssertEq(project.Value?.Maps.Count, 1);
        AssertEq(project.Value?.Maps[0].Tiles.Count, 4);
        AssertEq(project.Value?.Maps[0].Events.Count, 1);
    }

    public void Test_WolfPluginRuntimeLoadsDataAndAdvancesDeterministicEventVm()
    {
        var game = new PluginGameInfo
        {
            GameDirectory = Global(TempBase),
            EngineId = EnginePluginIds.WolfRpg,
            Generation = "wolf",
            DetectorScore = 3,
        };
        using var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(game);
        AssertTrue(started.Success, started.Error?.Message ?? "WOLF runtime start failed");
        AssertEq(host.State, PluginRuntimeState.Running);
        AssertTrue(host.Runtime is WolfEngineRuntime);
        if (host.Runtime is not WolfEngineRuntime runtime)
        {
            return;
        }

        AssertTrue(runtime.StartEvent(1).Success);
        AssertTrue(host.Update(1.0 / 60.0).Success);
        AssertEq(runtime.EventVm.GetVariable(1), 5);
        AssertTrue(runtime.EventVm.GetSwitch(7));
        AssertEq(runtime.EventVm.Messages.Count, 1);
        AssertEq(runtime.EventVm.State, WolfVmState.Waiting);

        AssertTrue(host.Update(1.0 / 30.0).Success);
        AssertEq(runtime.EventVm.Messages.Count, 1);
        AssertTrue(host.Update(1.0 / 60.0).Success);
        AssertEq(runtime.EventVm.Messages.Count, 2);
        AssertEq(runtime.EventVm.PendingTransfer?.MapId, 2);
        AssertEq(runtime.EventVm.PendingTransfer?.X, 1);
        AssertEq(runtime.EventVm.PendingTransfer?.Y, 1);
        AssertTrue(host.Stop().Success);
    }

    public void Test_WolfVmChoiceAndUnknownOperationAreDeterministic()
    {
        var vm = new WolfEventVm();
        var choice = new WolfEventProgram
        {
            Id = 42,
            Commands = new[]
            {
                new WolfEventCommand { Opcode = WolfEventOpcode.Choice, RawOperation = "choice", Choices = new[] { "Yes", "No" } },
                new WolfEventCommand { Opcode = WolfEventOpcode.Message, RawOperation = "message", Text = "Selected" },
                new WolfEventCommand { Opcode = WolfEventOpcode.End, RawOperation = "end" },
            },
        };
        AssertTrue(vm.Start(choice).Success);
        AssertTrue(vm.StepTick().Success);
        AssertEq(vm.State, WolfVmState.Waiting);
        AssertEq(vm.PendingChoices.Count, 2);
        AssertTrue(vm.SelectChoice(1).Success);
        AssertTrue(vm.StepTick().Success);
        AssertEq(vm.Messages[0].Text, "Selected");
        AssertEq(vm.State, WolfVmState.Completed);

        var unknown = new WolfEventVm();
        AssertTrue(unknown.Start(new WolfEventProgram
        {
            Id = 7,
            Commands = new[] { new WolfEventCommand { Opcode = WolfEventOpcode.Unknown, RawOperation = "native_call" } },
        }).Success);
        var failed = unknown.StepTick();
        AssertFalse(failed.Success);
        AssertEq(unknown.State, WolfVmState.Faulted);
        AssertEq(failed.Error?.Phase, "command");
    }

    public void Test_ProtectedAndOversizedPlainInputsAreRejectedWithoutBypass()
    {
        WriteJson("Data/Game.dat", "{\"format\":\"urpg-wolf-plain-json\",\"version\":1,\"kind\":\"game\",\"title\":\"Protected\",\"protected\":true}");
        var protectedResult = new WolfDataReader().Load(Global(TempBase));
        AssertFalse(protectedResult.Success);
        AssertEq(protectedResult.Error?.Code, PluginErrorCode.UnsupportedEngine);

        WriteJson("Data/Game.dat", "{\"format\":\"urpg-wolf-plain-json\",\"version\":1,\"kind\":\"game\",\"title\":\"Plain\",\"protected\":false}");
        var limited = new WolfDataReader(new WolfParseLimits { MaxFileBytes = 64 }).Load(Global(TempBase));
        AssertFalse(limited.Success);
        AssertEq(limited.Error?.Code, PluginErrorCode.InvalidGame);
    }

    public void Test_MissingOptionalDatabasesRemainSafe()
    {
        var systemPath = Global(TempBase.PathJoin("Data/BasicData/System.db"));
        var variablePath = Global(TempBase.PathJoin("Data/BasicData/Variable.db"));
        File.Delete(systemPath);
        File.Delete(variablePath);

        var result = new WolfDataReader().Load(Global(TempBase));
        AssertTrue(result.Success, result.Error?.Message ?? "WOLF load failed without optional databases");
        AssertEq(result.Value?.SystemDatabase, null);
        AssertEq(result.Value?.VariableDatabase, null);
        AssertEq(result.Value?.UserDatabases.Count, 1);
    }

    private static string Database(string pId, string pName, string pField, string pValue)
    {
        return $"{{\"format\":\"urpg-wolf-plain-json\",\"version\":1,\"kind\":\"database\",\"databaseType\":\"{pId}\",\"name\":\"{pName}\",\"records\":[{{\"id\":1,\"fields\":{{\"{pField}\":\"{pValue}\"}}}}]}}";
    }

    private static string Map()
    {
        return "{\"format\":\"urpg-wolf-plain-json\",\"version\":1,\"kind\":\"map\",\"id\":1,\"name\":\"Start\",\"width\":2,\"height\":2,\"tiles\":[1,2,3,4],\"events\":[{\"id\":1,\"x\":0,\"y\":0,\"commands\":[{\"op\":\"set_variable\",\"operand\":1,\"value\":2},{\"op\":\"add_variable\",\"operand\":1,\"value\":3},{\"op\":\"set_switch\",\"operand\":7,\"value\":1},{\"op\":\"message\",\"text\":\"Hello\"},{\"op\":\"wait\",\"frames\":2},{\"op\":\"message\",\"text\":\"After\"},{\"op\":\"transfer\",\"map_id\":2,\"x\":1,\"y\":1},{\"op\":\"end\"}]}]}";
    }

    private static void WriteJson(string pRelativePath, string pText)
    {
        var path = Global(TempBase.PathJoin(pRelativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, pText);
    }

    private static string Global(string pPath) => Godot.ProjectSettings.GlobalizePath(pPath);

    private static void Cleanup()
    {
        var path = Global(TempBase);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }
}
