using System;
using System.Security.Cryptography;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// Bridges engine-independent logical content into the MV/MZ plugin loader.
/// When inventory recorded a SHA-256, changed bytes require reinspection. This
/// is content identity checking, not code-signing or a trust/safety decision.
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
        if (pScript == null) return ScriptSourceResult.Failed("A script descriptor is required.");
        var validation = pScript.Validate();
        if (!validation.Success) return ScriptSourceResult.Failed(validation.ErrorMessage);
        if (string.IsNullOrWhiteSpace(pScript.RelativePath))
            return ScriptSourceResult.Failed($"Script '{pScript.Id}' does not declare a logical source path.");

        var expected = pScript.Sha256;
        if (expected.Length != 0)
        {
            if (expected.Length != 64)
                return ScriptSourceResult.Failed("[scripts.invalid-hash] Expected a 64-character SHA-256.");
            foreach (var character in expected)
                if (!Uri.IsHexDigit(character))
                    return ScriptSourceResult.Failed("[scripts.invalid-hash] Expected hexadecimal SHA-256 metadata.");
        }
        var read = _content.Read(pScript.RelativePath);
        if (!read.Success) return ScriptSourceResult.Failed($"[{read.ErrorCode}] {read.ErrorMessage}");
        if (expected.Length != 0
            && !Convert.ToHexString(SHA256.HashData(read.Data.Span)).Equals(expected, StringComparison.OrdinalIgnoreCase))
            return ScriptSourceResult.Failed($"[scripts.source-changed] '{pScript.RelativePath}' changed after inspection; inspect the project again.");
        return ScriptSourceResult.Succeeded(read.Data);
    }
}
