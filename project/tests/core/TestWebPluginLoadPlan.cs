using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestWebPluginLoadPlan : TestBase
{
    public void Test_FirstEnabledExactNameWinsInConfiguredOrder()
    {
        var entries = new[]
        {
            Entry("A", 3, true, "late"), Entry("A", 0, false, "disabled"),
            Entry("B", 2, true, "other"), Entry("A", 1, true, "first"),
        };
        var plan = WebPluginLoadPlan.SelectEnabled(entries);
        AssertEq(string.Join(",", plan.Select(e => e.Script.DisplayName)), "A,B");
        AssertEq(plan[0].Parameters["Value"], "first");
    }

    public void Test_EqualLoadOrderPreservesCallerSequence()
    {
        var plan = WebPluginLoadPlan.SelectEnabled(new[] { Entry("B", 0), Entry("A", 0), Entry("B", 0, true, "ignored") });
        AssertEq(string.Join(",", plan.Select(e => e.Script.DisplayName)), "B,A");
        AssertEq(plan[0].Parameters["Value"], "original");
    }

    public void Test_NameDeduplicationIsNotParameterKeyNormalization()
    {
        var plan = WebPluginLoadPlan.SelectEnabled(new[] { Entry("Name", 0), Entry("name", 1), Entry(" Name ", 2) });
        AssertEq(plan.Count, 3, "the original loader de-duplicates exact names, not lowercased/trimmed aliases");
    }

    public void Test_ParameterSnapshotDoesNotFollowCallerMutation()
    {
        var values = new Dictionary<string, string> { ["Value"] = "original" };
        var plan = WebPluginLoadPlan.SelectEnabled(new[]
        {
            new WebScriptInventoryEntry { Enabled = true, Script = Entry("A", 0).Script, Parameters = values },
        });
        values["Value"] = "changed";
        values["Extra"] = "added";
        AssertEq(plan[0].Parameters["Value"], "original");
        AssertEq(plan[0].Parameters.Count, 1);
        var readOnly = false;
        try { ((IDictionary<string, string>)plan[0].Parameters)["Value"] = "mutated"; }
        catch (NotSupportedException) { readOnly = true; }
        AssertTrue(readOnly);
    }

    public void Test_DisabledEntriesDoNotReachTheExecutablePlan()
    {
        var plan = WebPluginLoadPlan.SelectEnabled(new[] { Entry("A", 0, false), Entry("B", 1, false) });
        AssertEq(plan.Count, 0);
    }

    public void Test_EnumeratorStopsAtTheBoundedInputLimit()
    {
        var visited = 0;
        IEnumerable<WebScriptInventoryEntry> Entries()
        {
            while (true) { visited++; yield return Entry("A", visited); }
        }
        Reject(() => WebPluginLoadPlan.SelectEnabled(Entries()));
        AssertEq(visited, WebScriptInventory.MaxPlugins + 1);
    }

    public void Test_NullEntryAndDescriptorFailExplicitly()
    {
        Reject(() => WebPluginLoadPlan.SelectEnabled(new WebScriptInventoryEntry[] { null! }));
        Reject(() => WebPluginLoadPlan.SelectEnabled(new[] { new WebScriptInventoryEntry { Enabled = true, Script = null! } }));
    }

    public void Test_InvalidParameterMetadataIsRejected()
    {
        Reject(() => WebPluginLoadPlan.SelectEnabled(new[]
        {
            new WebScriptInventoryEntry { Enabled = true, Script = Entry("A", 0).Script, Parameters = null! },
        }));
        Reject(() => WebPluginLoadPlan.SelectEnabled(new[]
        {
            new WebScriptInventoryEntry
            {
                Enabled = true, Script = Entry("A", 0).Script,
                Parameters = new Dictionary<string, string> { ["Value"] = null! },
            },
        }));
    }

    public void Test_ParameterValuesAndAggregateInputAreBounded()
    {
        Reject(() => WebPluginLoadPlan.SelectEnabled(new[] { Entry("A", 0, true, new string('x', WebScriptInventory.MaxParameterValueLength + 1)) }));
        var repeated = new string('x', WebScriptInventory.MaxParameterValueLength);
        Reject(() => WebPluginLoadPlan.SelectEnabled(Enumerable.Range(0, 100).Select(i => Entry("P" + i, i, true, repeated))));
    }

    public void Test_PrototypeParameterKeysRemainData()
    {
        var plan = WebPluginLoadPlan.SelectEnabled(new[]
        {
            new WebScriptInventoryEntry
            {
                Enabled = true, Script = Entry("__proto__", 0).Script,
                Parameters = new Dictionary<string, string> { ["__proto__"] = "value", ["constructor"] = "text" },
            },
        });
        AssertEq(plan[0].Parameters["__proto__"], "value");
        AssertEq(plan[0].Parameters["constructor"], "text");
    }

    private void Reject(Action action)
    {
        var rejected = false;
        try { action(); } catch (ArgumentException) { rejected = true; }
        AssertTrue(rejected, "invalid host input must fail before VM work");
    }

    private static WebScriptInventoryEntry Entry(string name, int order, bool enabled = true, string value = "original") => new()
    {
        Enabled = enabled,
        Script = new EngineScriptDescriptor
        {
            Id = "plugin:" + order, DisplayName = name, LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript, LoadOrder = order,
        },
        Parameters = new Dictionary<string, string> { ["Value"] = value },
    };
}
