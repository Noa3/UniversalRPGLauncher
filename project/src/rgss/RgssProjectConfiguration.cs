using System;
using UniversalRPG.Core;
using UniversalRPG.Sdk;

namespace UniversalRPG.Rgss;

public sealed class RgssProjectConfiguration
{
    public string ScriptArchivePath { get; init; } = "";
    public bool UsesConfiguredScriptPath { get; init; }
}

public static class RgssProjectConfigurationReader
{
    public const int MaxGameIniBytes = 1024 * 1024;

    public static SdkValueResult<RgssProjectConfiguration> Read(
        IGameContentSource pContent,
        RgssGeneration pGeneration)
    {
        if (pContent == null) throw new ArgumentNullException(nameof(pContent));
        var defaultPath = DefaultScriptPath(pGeneration);
        if (!pContent.Exists("Game.ini"))
        {
            return SdkValueResult<RgssProjectConfiguration>.Succeeded(new RgssProjectConfiguration
            {
                ScriptArchivePath = defaultPath,
                UsesConfiguredScriptPath = false,
            });
        }

        var read = pContent.Read("Game.ini");
        if (!read.Success)
        {
            return SdkValueResult<RgssProjectConfiguration>.Failed(
                "rgss.game-ini-read",
                $"Could not read Game.ini: [{read.ErrorCode}] {read.ErrorMessage}");
        }
        if (read.Data.Length > MaxGameIniBytes)
        {
            return SdkValueResult<RgssProjectConfiguration>.Failed(
                "rgss.game-ini-size",
                $"Game.ini exceeds the bounded {MaxGameIniBytes}-byte limit.");
        }

        var bytes = read.Data.ToArray();
        var text = new LegacyTextDecoder().Decode(bytes);
        if (string.IsNullOrEmpty(text) && bytes.Length > 0)
        {
            return SdkValueResult<RgssProjectConfiguration>.Failed(
                "rgss.game-ini-decode",
                "Game.ini could not be decoded using the supported legacy text boundary.");
        }

        var inGameSection = false;
        foreach (var rawLine in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                inGameSection = line[1..^1].Trim().Equals("Game", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inGameSection) continue;

            var equals = line.IndexOf('=');
            if (equals <= 0) continue;
            var key = line[..equals].Trim();
            if (!key.Equals("Scripts", StringComparison.OrdinalIgnoreCase)) continue;

            var value = Unquote(line[(equals + 1)..].Trim());
            if (string.IsNullOrWhiteSpace(value))
            {
                return SdkValueResult<RgssProjectConfiguration>.Failed(
                    "rgss.scripts-path-empty",
                    "Game.ini contains an empty Scripts path.");
            }
            if (!LogicalGamePath.TryNormalize(value, out var normalized, 2048))
            {
                return SdkValueResult<RgssProjectConfiguration>.Failed(
                    "rgss.scripts-path-unsafe",
                    $"Game.ini Scripts path '{value}' is absolute, traverses outside the game, or is otherwise unsafe.");
            }
            return SdkValueResult<RgssProjectConfiguration>.Succeeded(new RgssProjectConfiguration
            {
                ScriptArchivePath = normalized,
                UsesConfiguredScriptPath = true,
            });
        }

        return SdkValueResult<RgssProjectConfiguration>.Succeeded(new RgssProjectConfiguration
        {
            ScriptArchivePath = defaultPath,
            UsesConfiguredScriptPath = false,
        });
    }

    public static string DefaultScriptPath(RgssGeneration pGeneration) => pGeneration switch
    {
        RgssGeneration.Rgss1 => "Data/Scripts.rxdata",
        RgssGeneration.Rgss2 => "Data/Scripts.rvdata",
        _ => "Data/Scripts.rvdata2",
    };

    private static string Unquote(string pValue)
    {
        if (pValue.Length >= 2
            && ((pValue[0] == '"' && pValue[^1] == '"') || (pValue[0] == '\'' && pValue[^1] == '\'')))
        {
            return pValue[1..^1].Trim();
        }
        return pValue;
    }
}
