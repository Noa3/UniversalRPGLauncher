using System;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// Bridges the engine-independent logical content layer into the MV/MZ plugin
/// loader. Script descriptors keep logical relative paths; the content source
/// decides whether those bytes come from a directory, override, archive, or
/// transparently decrypted engine-managed source.
/// </summary>
public sealed class GameContentWebScriptSourceProvider : IWebScriptSourceProvider
{
    private readonly IGameContentSource _content;

    public GameContentWebScriptSourceProvider(IGameContentSource pContent)
    {
        _content = pContent ?? throw new ArgumentNullException(nameof(pContent));
    }

    public ScriptSourceResult Read(EngineScriptDescriptor pScript)
    {
        if (pScript == null)
        {
            return ScriptSourceResult.Failed("A script descriptor is required.");
        }
        var validation = pScript.Validate();
        if (!validation.Success)
        {
            return ScriptSourceResult.Failed(validation.ErrorMessage);
        }
        if (string.IsNullOrWhiteSpace(pScript.RelativePath))
        {
            return ScriptSourceResult.Failed($"Script '{pScript.Id}' does not declare a logical source path.");
        }

        var read = _content.Read(pScript.RelativePath);
        return read.Success
            ? ScriptSourceResult.Succeeded(read.Data)
            : ScriptSourceResult.Failed($"[{read.ErrorCode}] {read.ErrorMessage}");
    }
}
