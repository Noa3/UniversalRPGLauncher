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
            PreservesMethodReceiverAndPrimitiveArguments();
            RejectsHostObjectsAndOversizedArguments();
            BoundsGetterExecutionDuringInvocation();
            BoundsStoredModuleTextAndResetsBudget();
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
        var result = new JintScriptVmFactory().Create(new ScriptVmRequest
        {
            LanguageId = ScriptLanguageIds.RpgMakerMzJavaScript,
            CompatibilityProfile = "rmmz-web-runtime",
            Policy = Policy(250),
            RequiredFeatures = new[] { "javascript", "window", "rmmz-api" },
        });
        Check(!result.Success, "raw Jint factory rejects unimplemented host features");
        Check(result.Result.ErrorCode == "jint.feature-unsupported", "feature refusal has stable code");
    }

    private static void ExecutesOrderedJavaScriptAndPreservesRealmState()
    {
        using var vm = Create();
        Check(vm.State == ScriptVmState.Configured, "configured state recorded");
        Success(vm.LoadModule(Module("plugin:first", 0,
            "if (typeof System !== 'undefined') throw Error('CLR leaked');" +
            "globalThis.counter = 41; function add(a,b) { if(a+b!==5) throw Error('arguments'); }")), "load first module");
        Success(vm.LoadModule(Module("plugin:second", 1,
            "if(counter !== 41) throw Error('realm state lost'); counter++;")), "load second module");
        Check(vm.State == ScriptVmState.Ready, "loading does not execute scripts");
        Success(vm.ExecuteModule("plugin:first"), "execute first module");
        Success(vm.ExecuteModule("plugin:second"), "second module sees first state");
        Check(vm.State == ScriptVmState.Running, "running state recorded");
        Success(vm.Invoke(Call("globalThis", "add", 2, 3)), "invoke global function");
        Success(vm.Reset(), "reset running VM");
        Check(vm.State == ScriptVmState.Configured, "reset creates clean configured realm");
        Check(!vm.ExecuteModule("plugin:first").Success, "reset clears modules");
    }

    private static void RejectsInvalidUtf8BeforeExecution()
    {
        using var vm = Create();
        var result = vm.LoadModule(new ScriptModule
        {
            Descriptor = Descriptor("plugin:invalid-utf8", 0),
            Source = new byte[] { 0xC3, 0x28 },
        });
        Check(!result.Success && result.ErrorCode == "jint.source-encoding", "invalid UTF-8 is refused");
    }

    private static void BoundsInfiniteExecution()
    {
        using var vm = Create(25);
        Success(vm.LoadModule(Module("plugin:loop", 0, "while(true){}")), "load loop as data");
        var execution = vm.ExecuteModule("plugin:loop");
        Check(!execution.Success, "infinite JS stops");
        Check(execution.ErrorCode is "jint.timeout" or "jint.statement-limit", "loop reaches a real execution constraint");
        Check(vm.State == ScriptVmState.Faulted, "constraint faults session");
        Success(vm.Reset(), "reset faulted VM");
        Check(vm.State == ScriptVmState.Configured, "reset after fault restores lifecycle");
    }

    private static void PreservesMethodReceiverAndPrimitiveArguments()
    {
        using var vm = Create();
        Success(vm.LoadModule(Module("plugin:receiver", 0, """
            globalThis.api = {
                count: 0,
                change: function(n, flag, text, empty) {
                    'use strict';
                    if (this !== api || n !== 42 || flag !== true || text !== '日本語' || empty !== null)
                        throw Error('receiver or primitive argument mismatch');
                    this.count++;
                },
                verify: function() { if(this !== api || this.count !== 1) throw Error('method receiver lost'); },
                get probe() { this.reads = (this.reads || 0) + 1; return this.verify; }
            };
            globalThis.globalProbe = function() { 'use strict'; if(this !== globalThis) throw Error('global receiver lost'); };
            """)), "load receiver fixture");
        Success(vm.ExecuteModule("plugin:receiver"), "execute receiver fixture");
        Success(vm.Invoke(Call("api", "change", 42, true, "日本語", null)), "retain receiver and primitive values");
        Success(vm.Invoke(Call("api", "probe")), "resolve getter within invocation");
        Success(vm.Invoke(Call("", "globalProbe")), "empty target uses global receiver");
        Success(vm.Invoke(Call("globalThis", "globalProbe")), "globalThis target uses global receiver");
    }

    private static void RejectsHostObjectsAndOversizedArguments()
    {
        using var vm = Create();
        Success(vm.LoadModule(Module("plugin:guard", 0,
            "globalThis.calls = 0; function accept(){calls++;} function check(){if(calls !== 0) throw Error('invalid arguments executed');}")), "load argument fixture");
        Success(vm.ExecuteModule("plugin:guard"), "execute argument fixture");
        foreach (var value in new object[] { new HostObject(), typeof(string), (Action)(() => { }), double.NaN, double.PositiveInfinity, long.MaxValue, ulong.MaxValue })
        {
            var rejected = vm.Invoke(Call("", "accept", value));
            Check(!rejected.Success && rejected.ErrorCode == "jint.argument-type", "reject unsafe/non-representable argument");
        }
        var oversized = vm.Invoke(Call("", "accept", new string('x', 8193)));
        Check(!oversized.Success && oversized.ErrorCode == "jint.argument-limit", "reject oversized argument string");
        var many = vm.Invoke(new ScriptInvocation
        {
            Member = "accept",
            Arguments = Enumerable.Range(0, 257).Select(n => new ScriptValue(n)).ToArray(),
        });
        Check(!many.Success && many.ErrorCode == "jint.argument-limit", "reject excessive argument count");
        Check(vm.State == ScriptVmState.Running, "bad host request does not damage VM state");
        Success(vm.Invoke(Call("", "check")), "invalid arguments never reach game code");
    }

    private static void BoundsGetterExecutionDuringInvocation()
    {
        using var vm = Create(50);
        Success(vm.LoadModule(Module("plugin:getter-loop", 0,
            "globalThis.api = { get forever() { while(true){} } };")), "load getter fixture");
        Success(vm.ExecuteModule("plugin:getter-loop"), "execute getter fixture");
        var result = vm.Invoke(Call("api", "forever"));
        Check(!result.Success && (result.ErrorCode is "jint.timeout" or "jint.statement-limit"), "property lookup is also execution-bounded");
        Check(vm.State == ScriptVmState.Faulted, "getter constraint faults VM");
        Success(vm.Reset(), "recover from getter timeout");
    }

    private static void BoundsStoredModuleTextAndResetsBudget()
    {
        using var vm = new JintEmbeddedScriptVm(ScriptLanguageIds.RpgMakerMvJavaScript);
        var policy = new ScriptExecutionPolicy
        {
            MaxMemoryMegabytes = 16,
            MaxExecutionMillisecondsPerTick = 250,
            MaxCallDepth = 128,
        };
        Success(vm.Configure(policy), "configure source budget fixture");
        // Each ASCII source becomes 10 MiB of UTF-16 text, outside Jint's own
        // per-entry allocation counter. Two must not fit the 16 MiB source cap.
        var source = new string(' ', 5 * 1024 * 1024);
        Success(vm.LoadModule(Module("plugin:large-a", 0, source)), "first bounded source fits");
        var second = vm.LoadModule(Module("plugin:large-b", 1, source));
        Check(!second.Success && second.ErrorCode == "jint.source-memory-limit", "aggregate source memory is bounded before decode");
        Success(vm.Reset(), "reset source registry");
        Success(vm.LoadModule(Module("plugin:large-a", 0, source)), "reset releases source budget");
    }

    private static IEmbeddedScriptVm Create(int milliseconds = 250)
    {
        var created = new JintScriptVmFactory().Create(new ScriptVmRequest
        {
            LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript,
            CompatibilityProfile = "javascript-core",
            Policy = Policy(milliseconds),
            RequiredFeatures = new[] { "javascript" },
        });
        Check(created.Success && created.Vm != null, "create JavaScript-only VM");
        var vm = created.Vm!;
        var configured = vm.Configure(Policy(milliseconds));
        if (!configured.Success) { vm.Dispose(); Success(configured, "configure VM"); }
        else Success(configured, "configure VM");
        return vm;
    }

    private static ScriptExecutionPolicy Policy(int milliseconds) => new()
    {
        MaxMemoryMegabytes = 64,
        MaxExecutionMillisecondsPerTick = milliseconds,
        MaxCallDepth = 128,
    };

    private static ScriptInvocation Call(string target, string member, params object?[] values) => new()
    {
        Target = target,
        Member = member,
        Arguments = values.Select(value => new ScriptValue(value)).ToArray(),
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

    private static void Success(SdkOperationResult result, string label)
        => Check(result.Success, $"{label}: [{result.ErrorCode}] {result.ErrorMessage}");

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class HostObject
    {
        public string Secret => throw new InvalidOperationException("Host getter must never be read.");
        public override string ToString() => throw new InvalidOperationException("Host coercion must never run.");
    }
}
