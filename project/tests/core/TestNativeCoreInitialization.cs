using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestNativeCoreInitialization : TestBase
{
    private const string Language = ScriptLanguageIds.RpgMakerMvJavaScript;

    public void Test_OriginalMvManagerRunsBeforeCustomPluginAliases()
    {
        using var vm = new JintEmbeddedScriptVm(Language);
        var entry = Entry();
        using var runtime = new WebScriptRuntime(Language, vm, new Source("""
            if(PluginManager.parameters('Probe').Value!=='42') throw Error('original setup missing');
            const prior=Game_Example.prototype.value;
            Game_Example.prototype.value=function(){return prior.call(this)+2;};
            if(new Game_Example().value()!==42)throw Error('core not ready before plugin');
            if(PluginManager.parameters!==originalParameters)throw Error('parameter shim replaced original method');
            """), new[] { entry }, new[]
        {
            Module("core:utility", "js/core_util.js", "Array.prototype.contains=function(x){return this.indexOf(x)>=0;};"),
            Module("core:manager", "js/rpg_managers.js", OriginalManager()),
            Module("core:objects", "js/rpg_objects.js", "function Game_Example(){};Game_Example.prototype.value=function(){return 40;};var originalParameters=PluginManager.parameters;"),
            Module("core:config", "js/plugins.js", "var $plugins=[{name:'Probe',status:true,parameters:{Value:'42'}}];"),
            NativeCorePluginSetup.Build(Language, new[] { entry }),
        }, new WebBrowserHostOptions());
        Pass(runtime.LoadScripts(Policy()));
        Pass(runtime.ExecuteBootstrap());
    }

    public void Test_OriginalCoreFilesReceiveScopedCurrentScriptMetadata()
    {
        using var vm = new JintEmbeddedScriptVm(Language);
        using var runtime = new WebScriptRuntime(Language, vm, new Source(""), Array.Empty<WebScriptInventoryEntry>(), new[]
        {
            Module("core:source", "js/libs/My Library.js", "if(!document.currentScript.src.endsWith('/My%20Library.js'))throw Error('source metadata missing');"),
            Module("compat:check", "", "if(document.currentScript!==null)throw Error('scope not cleared');", ScriptOrigin.CompatibilityShim),
        }, new WebBrowserHostOptions());
        Pass(runtime.LoadScripts(Policy())); Pass(runtime.ExecuteBootstrap());
    }

    public void Test_CoreFailurePreventsPluginExecutionAndRetry()
    {
        using var vm = new JintEmbeddedScriptVm(Language);
        using var runtime = new WebScriptRuntime(Language, vm, new Source("throw Error('plugin must not run');"), new[] { Entry() }, new[]
        {
            Module("core:broken", "js/rpg_core.js", "throw Error('core failure');"),
        }, new WebBrowserHostOptions());
        Pass(runtime.LoadScripts(Policy()));
        var result = runtime.ExecuteBootstrap();
        AssertFalse(result.Success);
        AssertTrue(result.ErrorMessage.Contains("core failure", StringComparison.Ordinal));
        AssertTrue(result.ErrorMessage.Contains("js/rpg_core.js", StringComparison.Ordinal));
        AssertEq(runtime.ExecuteBootstrap().ErrorCode, "web.session-faulted");
    }

    public void Test_ChangedPluginConfigurationFailsBeforePluginEvaluation()
    {
        using var vm = new JintEmbeddedScriptVm(Language);
        using var runtime = new WebScriptRuntime(Language, vm, new Source("throw Error('must not run');"), new[] { Entry() }, new[]
        {
            Module("core:utility", "js/util.js", "Array.prototype.contains=function(x){return this.indexOf(x)>=0;};"),
            Module("core:manager", "js/rpg_managers.js", OriginalManager()),
            Module("core:config", "js/plugins.js", "var $plugins=[{name:'Different',status:true,parameters:{}}];"),
            NativeCorePluginSetup.Build(Language, new[] { Entry() }),
        }, new WebBrowserHostOptions());
        Pass(runtime.LoadScripts(Policy()));
        var result = runtime.ExecuteBootstrap();
        AssertFalse(result.Success);
        AssertTrue(result.ErrorMessage.Contains("schedule-mismatch", StringComparison.Ordinal));
        AssertEq(runtime.AdvanceFrame(0).ErrorCode, "web.session-faulted");
    }

    private static string OriginalManager() => Godot.FileAccess.GetFileAsString("res://tests/fixtures/core-startup/MVPluginManager.js");
    private void Pass(SdkOperationResult result)
    {
        AssertTrue(result.Success, result.ErrorMessage);
        if (!result.Success) throw new InvalidOperationException(result.ErrorMessage);
    }
    private static ScriptExecutionPolicy Policy() => new() { MaxMemoryMegabytes=128, MaxExecutionMillisecondsPerTick=500, MaxCallDepth=128 };
    private static ScriptModule Module(string id, string path, string source, ScriptOrigin origin = ScriptOrigin.Game) => new()
    {
        Descriptor=new EngineScriptDescriptor { Id=id, DisplayName=id, RelativePath=path, LanguageId=Language, Origin=origin },
        Source=Encoding.UTF8.GetBytes(source),
    };
    private static WebScriptInventoryEntry Entry() => new()
    {
        Enabled=true, Compatibility=WebScriptCompatibility.StandardBrowserApi,
        Script=new EngineScriptDescriptor { Id="plugin:probe", DisplayName="Probe", RelativePath="js/plugins/Probe.js", LanguageId=Language },
        Parameters=new Dictionary<string,string> { ["Value"]="42" },
    };
    private sealed class Source : IWebScriptSourceProvider
    {
        private readonly byte[] _bytes;
        public Source(string source) => _bytes=Encoding.UTF8.GetBytes(source);
        public ScriptSourceResult Read(EngineScriptDescriptor descriptor) => ScriptSourceResult.Succeeded(_bytes);
    }
}
