using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

public sealed partial class WebScriptRuntime
{
    private SdkOperationResult ExecutePreludeModule(ScriptModule module)
    {
        // Original project libraries/core files need currentScript just like
        // plugins. Compatibility shims (including the frame host itself) do not.
        var scoped = _browserHostOptions != null
            && module.Descriptor.Origin != ScriptOrigin.CompatibilityShim
            && !string.IsNullOrEmpty(module.Descriptor.RelativePath);
        if (scoped)
        {
            var entered = EnterPluginScript(module.Descriptor.RelativePath);
            if (!entered.Success) return entered;
        }
        var executed = _vm.ExecuteModule(module.Descriptor.Id);
        if (!executed.Success) return executed; // outer lifecycle is fail-stop
        return scoped ? LeavePluginScript() : executed;
    }
}
