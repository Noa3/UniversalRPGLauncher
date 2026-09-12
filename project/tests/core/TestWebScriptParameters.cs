using System.Collections.Generic;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestWebScriptParameters : TestBase
{
    public void Test_LastConfiguredDuplicatePluginWinsParameterLookup()
    {
        var first = Entry("Duplicate", 0, "Value", "first");
        var second = Entry("duplicate", 1, "Value", "second");
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMzJavaScript,
            new EmptyVm(),
            new EmptySource(),
            new[] { first, second });

        var parameters = runtime.GetPluginParameters("DUPLICATE");

        AssertEq(parameters["Value"], "second");
    }

    public void Test_UnknownPluginReturnsEmptyParameterSet()
    {
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMzJavaScript,
            new EmptyVm(),
            new EmptySource(),
            new[] { Entry("Known", 0, "Value", "x") });

        AssertEq(runtime.GetPluginParameters("Missing").Count, 0);
    }

    private static WebScriptInventoryEntry Entry(
        string pName,
        int pOrder,
        string pParameter,
        string pValue)
    {
        return new WebScriptInventoryEntry
        {
            Enabled = true,
            Compatibility = WebScriptCompatibility.StandardBrowserApi,
            Parameters = new Dictionary<string, string> { [pParameter] = pValue },
            Script = new EngineScriptDescriptor
            {
                Id = "plugin:" + pName.ToLowerInvariant(),
                DisplayName = pName,
                LanguageId = ScriptLanguageIds.RpgMakerMzJavaScript,
                RelativePath = "js/plugins/" + pName + ".js",
                Sha256 = new string('a', 64),
                Origin = ScriptOrigin.Plugin,
                Required = true,
                LoadOrder = pOrder,
            },
        };
    }

    private sealed class EmptySource : IWebScriptSourceProvider
    {
        public ScriptSourceResult Read(EngineScriptDescriptor pScript)
            => ScriptSourceResult.Succeeded(System.ReadOnlyMemory<byte>.Empty);
    }

    private sealed class EmptyVm : IEmbeddedScriptVm
    {
        public string LanguageId => ScriptLanguageIds.RpgMakerMzJavaScript;
        public ScriptVmState State => ScriptVmState.Created;
        public ScriptExecutionPolicy Policy => ScriptExecutionPolicy.SafeDefault;
        public SdkOperationResult Configure(ScriptExecutionPolicy pPolicy) => SdkOperationResult.Succeeded();
        public SdkOperationResult LoadModule(ScriptModule pModule) => SdkOperationResult.Succeeded();
        public SdkOperationResult ExecuteModule(string pScriptId) => SdkOperationResult.Succeeded();
        public SdkOperationResult Invoke(ScriptInvocation pInvocation) => SdkOperationResult.Succeeded();
        public SdkOperationResult Reset() => SdkOperationResult.Succeeded();
        public void Dispose() { }
    }
}
