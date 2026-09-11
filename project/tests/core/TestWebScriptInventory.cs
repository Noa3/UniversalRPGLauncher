using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestWebScriptInventory : TestBase
{
    private const string Root = "user://web_script_inventory";

    public override void Setup()
    {
        Cleanup();
        var root = ProjectSettings.GlobalizePath(Root);
        Directory.CreateDirectory(Path.Combine(root, "js", "plugins"));
        File.WriteAllText(Path.Combine(root, "js", "plugins.js"),
            "$plugins = [\n" +
            "  {\"name\":\"StandardPlugin\",\"status\":true,\"description\":\"\",\"parameters\":{\"Text\":\"hello\",\"Count\":3,\"Flag\":true,\"Struct\":\"{\\\"x\\\":1}\"}},\n" +
            "  {\"name\":\"NodePlugin\",\"status\":true,\"description\":\"\",\"parameters\":{}},\n" +
            "  {\"name\":\"ProcessPlugin\",\"status\":false,\"description\":\"\",\"parameters\":{}},\n" +
            "  {\"name\":\"NativePlugin\",\"status\":true,\"description\":\"\",\"parameters\":{}}\n" +
            "];\n");
        File.WriteAllText(Path.Combine(root, "js", "plugins", "StandardPlugin.js"),
            "window.StandardPlugin = true;\n");
        File.WriteAllText(Path.Combine(root, "js", "plugins", "NodePlugin.js"),
            "const fs = require('fs');\n");
        File.WriteAllText(Path.Combine(root, "js", "plugins", "ProcessPlugin.js"),
            "require('child_process').spawn('tool');\n");
        File.WriteAllText(Path.Combine(root, "js", "plugins", "NativePlugin.js"),
            "const addon = require('./addon.node');\n");
        File.WriteAllText(Path.Combine(root, "js", "plugins", "UnlistedPlugin.js"),
            "document.title = 'unlisted';\n");
    }

    public override void Teardown() => Cleanup();

    public void Test_InventoryUsesPluginsJsOrderAndEnabledState()
    {
        var result = WebScriptInventory.Inspect(ProjectSettings.GlobalizePath(Root), pMZ: true);

        AssertEq(result.Entries.Count, 5);
        AssertEq(result.Entries[0].Script.DisplayName, "StandardPlugin");
        AssertEq(result.Entries[1].Script.DisplayName, "NodePlugin");
        AssertEq(result.Entries[2].Script.DisplayName, "ProcessPlugin");
        AssertEq(result.Entries[3].Script.DisplayName, "NativePlugin");
        AssertEq(result.Entries[4].Script.DisplayName, "UnlistedPlugin");
        AssertTrue(result.Entries[0].Enabled);
        AssertFalse(result.Entries[2].Enabled);
        AssertFalse(result.Entries[4].Enabled, "unlisted plugin is inventoried but not silently enabled");
        AssertEq(result.Entries[0].Script.LanguageId, ScriptLanguageIds.RpgMakerMzJavaScript);
        AssertTrue(result.Entries.All(pEntry => pEntry.Script.Validate().Success));
    }

    public void Test_InventoryPreservesConfiguredPluginParameters()
    {
        var result = WebScriptInventory.Inspect(ProjectSettings.GlobalizePath(Root), pMZ: true);
        var standard = Find(result, "StandardPlugin");

        AssertEq(standard.Parameters.Count, 4);
        AssertEq(standard.Parameters["Text"], "hello");
        AssertEq(standard.Parameters["Count"], "3");
        AssertEq(standard.Parameters["Flag"], "true");
        AssertEq(standard.Parameters["Struct"], "{\"x\":1}");
    }

    public void Test_InventoryClassifiesHostApiRequirementsWithoutExecutingScripts()
    {
        var result = WebScriptInventory.Inspect(ProjectSettings.GlobalizePath(Root), pMZ: false);

        AssertEq(Find(result, "StandardPlugin").Compatibility, WebScriptCompatibility.StandardBrowserApi);
        AssertEq(Find(result, "NodePlugin").Compatibility, WebScriptCompatibility.RequiresNodeShim);
        AssertEq(Find(result, "ProcessPlugin").Compatibility, WebScriptCompatibility.RequiresProcessExecution);
        AssertEq(Find(result, "NativePlugin").Compatibility, WebScriptCompatibility.RequiresNativeAddon);
        AssertEq(Find(result, "StandardPlugin").Script.LanguageId, ScriptLanguageIds.RpgMakerMvJavaScript);
    }

    public void Test_InventoryHashesCompletePluginSources()
    {
        var result = WebScriptInventory.Inspect(ProjectSettings.GlobalizePath(Root), pMZ: true);
        var standard = Find(result, "StandardPlugin");

        AssertEq(standard.Script.Sha256.Length, 64);
        AssertTrue(standard.Script.Sha256.All(pCharacter =>
            (pCharacter >= '0' && pCharacter <= '9') || (pCharacter >= 'a' && pCharacter <= 'f')));
    }

    public void Test_MissingConfiguredPluginKeepsItsParameters()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        File.Delete(Path.Combine(root, "js", "plugins", "StandardPlugin.js"));

        var result = WebScriptInventory.Inspect(root, pMZ: true);
        var missing = Find(result, "StandardPlugin");

        AssertTrue(missing.Enabled);
        AssertEq(missing.Compatibility, WebScriptCompatibility.MissingFile);
        AssertEq(missing.Script.Sha256, "");
        AssertEq(missing.Parameters["Text"], "hello");
        AssertTrue(missing.Reasons.Count > 0);
    }

    private static WebScriptInventoryEntry Find(WebScriptInventoryResult pResult, string pName)
        => pResult.Entries.First(pEntry => pEntry.Script.DisplayName == pName);

    private static void Cleanup()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
