using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestScriptVmProfiles : TestBase
{
    public void Test_RgssProfilesKeepHistoricalCompatibilityLinesSeparate()
    {
        var rgss1 = RgssVmProfiles.For(RgssGeneration.Rgss1);
        var rgss2 = RgssVmProfiles.For(RgssGeneration.Rgss2);
        var rgss3 = RgssVmProfiles.For(RgssGeneration.Rgss3);

        AssertEq(rgss1.LanguageId, ScriptLanguageIds.Rgss1Ruby);
        AssertEq(rgss2.LanguageId, ScriptLanguageIds.Rgss2Ruby);
        AssertEq(rgss3.LanguageId, ScriptLanguageIds.Rgss3Ruby);
        AssertEq(rgss1.HistoricalRubyLine, "ruby-1.8-compatible");
        AssertEq(rgss2.HistoricalRubyLine, "ruby-1.8-compatible");
        AssertEq(rgss3.HistoricalRubyLine, "ruby-1.9.2-compatible");
        AssertTrue(rgss3.RequiredFeatures.Contains("ruby-encoding-metadata"));
    }

    public void Test_RgssFactoryReceivesGenerationSpecificRequest()
    {
        var factory = new RecordingFactory(
            ScriptLanguageIds.Rgss1Ruby,
            ScriptLanguageIds.Rgss2Ruby,
            ScriptLanguageIds.Rgss3Ruby);

        var result = RgssVmProfiles.CreateVm(RgssGeneration.Rgss3, factory);

        AssertTrue(result.Success, result.Result.ErrorMessage);
        AssertTrue(result.Vm != null);
        AssertEq(factory.LastRequest!.LanguageId, ScriptLanguageIds.Rgss3Ruby);
        AssertEq(factory.LastRequest.CompatibilityProfile, "rgss3-ruby192");
        AssertTrue(factory.LastRequest.RequiredFeatures.Contains("rgss3-host-api"));
        result.Vm?.Dispose();
    }

    public void Test_RgssFactoryCannotSilentlySubstituteWrongVmLanguage()
    {
        var factory = new RecordingFactory(ScriptLanguageIds.Rgss3Ruby)
        {
            ReturnedLanguageId = ScriptLanguageIds.Rgss1Ruby,
        };

        var result = RgssVmProfiles.CreateVm(RgssGeneration.Rgss3, factory);

        AssertFalse(result.Success);
        AssertEq(result.Result.ErrorCode, "rgss.vm-factory-mismatch");
    }

    public void Test_WebProfilesSeparateMvAndMzCompatibilityApis()
    {
        var mv = WebVmProfiles.For(pMZ: false);
        var mz = WebVmProfiles.For(pMZ: true);

        AssertEq(mv.LanguageId, ScriptLanguageIds.RpgMakerMvJavaScript);
        AssertEq(mz.LanguageId, ScriptLanguageIds.RpgMakerMzJavaScript);
        AssertTrue(mv.RequiredFeatures.Contains("rmmv-api"));
        AssertTrue(mz.RequiredFeatures.Contains("rmmz-api"));
    }

    public void Test_WebFactoryReceivesAdditionalCompatibilityFeatures()
    {
        var factory = new RecordingFactory(ScriptLanguageIds.RpgMakerMzJavaScript);

        var result = WebVmProfiles.CreateVm(
            pMZ: true,
            factory,
            ScriptExecutionPolicy.SafeDefault,
            new[] { "node-fs-shim", "node-path-shim" });

        AssertTrue(result.Success, result.Result.ErrorMessage);
        AssertEq(factory.LastRequest!.CompatibilityProfile, "rmmz-web-runtime");
        AssertTrue(factory.LastRequest.RequiredFeatures.Contains("node-fs-shim"));
        AssertTrue(factory.LastRequest.RequiredFeatures.Contains("node-path-shim"));
        result.Vm?.Dispose();
    }

    private sealed class RecordingFactory : IEmbeddedScriptVmFactory
    {
        public RecordingFactory(params string[] pLanguages)
        {
            LanguageIds = pLanguages;
        }

        public string Id => "test-vm-factory";
        public IReadOnlyList<string> LanguageIds { get; }
        public ScriptVmRequest? LastRequest { get; private set; }
        public string ReturnedLanguageId { get; init; } = "";

        public SdkVmResult Create(ScriptVmRequest pRequest)
        {
            LastRequest = pRequest;
            var language = string.IsNullOrEmpty(ReturnedLanguageId) ? pRequest.LanguageId : ReturnedLanguageId;
            return SdkVmResult.Succeeded(new EmptyVm(language));
        }
    }

    private sealed class EmptyVm : IEmbeddedScriptVm
    {
        public EmptyVm(string pLanguageId) => LanguageId = pLanguageId;
        public string LanguageId { get; }
        public ScriptVmState State { get; private set; } = ScriptVmState.Created;
        public ScriptExecutionPolicy Policy { get; private set; } = ScriptExecutionPolicy.SafeDefault;

        public SdkOperationResult Configure(ScriptExecutionPolicy pPolicy)
        {
            Policy = pPolicy;
            State = ScriptVmState.Configured;
            return SdkOperationResult.Succeeded();
        }

        public SdkOperationResult LoadModule(ScriptModule pModule) => SdkOperationResult.Succeeded();
        public SdkOperationResult ExecuteModule(string pScriptId) => SdkOperationResult.Succeeded();
        public SdkOperationResult Invoke(ScriptInvocation pInvocation) => SdkOperationResult.Succeeded();
        public SdkOperationResult Reset() => SdkOperationResult.Succeeded();
        public void Dispose() => State = ScriptVmState.Disposed;
    }
}
