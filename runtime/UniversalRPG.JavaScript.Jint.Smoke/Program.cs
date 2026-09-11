using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Sdk;

static class Smoke
{
    private static int _checks;

    public static int Main()
    {
        try
        {
            FactoryRejectsBrowserFeaturesUntilHostApisExist();
            ExecutesOrderedJavaScriptAndPreservesRealmState();
            RejectsInvalidUtf8BeforeExecution();
            BoundsInfiniteExecution();
            Console.WriteLine($"UniversalRPG Jint smoke passed ({_checks} checks).");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("UniversalRPG Jint smoke FAILED: " + exception);
            return 1;
        }
    }

    private static void FactoryRejectsBrowserFeaturesUntilHostApisExist()
    {
        var factory = new JintScriptVmFactory();
        var result = factory.Create(new ScriptVmRequest
        {
            LanguageId = ScriptLanguageIds.RpgMakerMzJavaScript,
            CompatibilityProfile = "rmmz-web-runtime",
            Policy = Policy(100),
            RequiredFeatures = new[] { "javascript", "window", "rmmz-api" },
        });
        Check(!result.Success, "raw Jint factory rejects unimplemented browser/RPG Maker host features");
        Check(result.Result.ErrorCode == "jint.feature-unsupported", "feature rejection uses stable error code");
    }

    private static void ExecutesOrderedJavaScriptAndPreservesRealmState()
    {
        var factory = new JintScriptVmFactory();
        var created = factory.Create(new ScriptVmRequest
        {
            LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript,
            CompatibilityProfile = "javascript-core",
            Policy = Policy(250),
            RequiredFeatures = new[] { "javascript" },
        });
        Check(created.Success && created.Vm != null, "factory creates JavaScript-only VM");
        using var vm = created.Vm!;

        Check(vm.Configure(Policy(250)).Success, "VM configures constrained policy");
        Check(vm.State == ScriptVmState.Configured, "configured state recorded");

        var first = Module(
            "plugin:first",
            0,
            "if (typeof System !== 'undefined') throw new Error('CLR leaked');\n" +
            "globalThis.counter = 41;\n" +
            "function add(a, b) { return a + b; }\n");
        var second = Module(
            "plugin:second",
            1,
            "if (globalThis.counter !== 41) throw new Error('realm state lost');\n" +
            "globalThis.counter += 1;\n");

        Check(vm.LoadModule(first).Success, "first module loads without execution");
        Check(vm.LoadModule(second).Success, "second module loads before execution");
        Check(vm.State == ScriptVmState.Ready, "loaded modules move VM to ready state");
        Check(vm.ExecuteModule("plugin:first").Success, "first module executes");
        Check(vm.ExecuteModule("plugin:second").Success, "second module sees state from first module");
        Check(vm.State == ScriptVmState.Running, "executed VM is running");
        Check(vm.Invoke(new ScriptInvocation
        {
            Target = "globalThis",
            Member = "add",
            Arguments = new[] { new ScriptValue(2), new ScriptValue(3) },
        }).Success, "global JavaScript function can be invoked through VM contract");

        Check(vm.Reset().Success, "VM reset succeeds");
        Check(vm.State == ScriptVmState.Configured, "reset recreates clean configured realm");
        Check(!vm.ExecuteModule("plugin:first").Success, "reset clears loaded module registry");
    }

    private static void RejectsInvalidUtf8BeforeExecution()
    {
        var factory = new JintScriptVmFactory();
        var created = factory.Create(new ScriptVmRequest
        {
            LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript,
            CompatibilityProfile = "javascript-core",
            Policy = Policy(100),
            RequiredFeatures = new[] { "javascript" },
        });
        Check(created.Success && created.Vm != null, "UTF-8 smoke VM created");
        using var vm = created.Vm!;
        Check(vm.Configure(Policy(100)).Success, "UTF-8 smoke VM configured");

        var result = vm.LoadModule(new ScriptModule
        {
            Descriptor = Descriptor("plugin:invalid-utf8", 0),
            Source = new byte[] { 0xC3, 0x28 },
        });
        Check(!result.Success && result.ErrorCode == "jint.source-encoding", "invalid UTF-8 rejected before execution");
    }

    private static void BoundsInfiniteExecution()
    {
        var factory = new JintScriptVmFactory();
        var policy = Policy(25);
        var created = factory.Create(new ScriptVmRequest
        {
            LanguageId = ScriptLanguageIds.RpgMakerMzJavaScript,
            CompatibilityProfile = "javascript-core",
            Policy = policy,
            RequiredFeatures = new[] { "javascript" },
        });
        Check(created.Success && created.Vm != null, "bounded execution VM created");
        using var vm = created.Vm!;
        Check(vm.Configure(policy).Success, "bounded execution VM configured");
        Check(vm.LoadModule(Module("plugin:loop", 0, "while (true) { }\n")).Success, "loop module loads as data");

        var execution = vm.ExecuteModule("plugin:loop");
        Check(!execution.Success, "infinite JavaScript is stopped by VM constraints");
        Check(execution.ErrorCode is "jint.timeout" or "jint.statement-limit" or "jint.execution-failed",
            "constraint failure returns bounded Jint error");
        Check(vm.State == ScriptVmState.Faulted, "constraint violation faults the VM session");
    }

    private static ScriptExecutionPolicy Policy(int milliseconds) => new()
    {
        AllowReadGameFiles = true,
        AllowWriteSaveFiles = true,
        AllowWriteCacheFiles = true,
        AllowArbitraryHostFileSystem = false,
        AllowNetwork = false,
        AllowClipboard = false,
        AllowProcessExecution = false,
        AllowNativeInterop = false,
        MaxMemoryMegabytes = 64,
        MaxExecutionMillisecondsPerTick = milliseconds,
        MaxCallDepth = 128,
    };

    private static ScriptModule Module(string id, int order, string source) => new()
    {
        Descriptor = Descriptor(id, order),
        Source = Encoding.UTF8.GetBytes(source),
    };

    private static EngineScriptDescriptor Descriptor(string id, int order) => new()
    {
        Id = id,
        DisplayName = id,
        LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript,
        RelativePath = "js/plugins/" + id.Replace(':', '_') + ".js",
        Origin = ScriptOrigin.Plugin,
        Required = true,
        LoadOrder = order,
    };

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
