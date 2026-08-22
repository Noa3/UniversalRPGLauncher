using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using UniversalRPG.Plugins;

namespace UniversalRPG.Wolf;

/// <summary>
/// Bounded reader for the initial WOLF plain-data conformance envelope. The
/// reader accepts only UTF-8 JSON with the explicit URPG format marker. This is
/// deliberately not an archive decryptor and it never loads executable code.
/// </summary>
public sealed class WolfDataReader
{
    private readonly WolfParseLimits _limits;
    private readonly WolfDatabaseReader _databaseReader;
    private readonly WolfMapReader _mapReader;

    public WolfDataReader(WolfParseLimits? pLimits = null)
    {
        _limits = pLimits ?? new WolfParseLimits();
        _databaseReader = new WolfDatabaseReader(_limits);
        _mapReader = new WolfMapReader(_limits);
    }

    public PluginResult<WolfProjectData> Load(string pGameDirectory)
    {
        if (!_limits.IsValid())
        {
            return Failed<WolfProjectData>("WOLF parsing limits are outside the safe supported range.", "limits");
        }

        var root = ResolveDirectory(pGameDirectory);
        if (root == null)
        {
            return Failed<WolfProjectData>("WOLF runtime requires an existing game directory.", "load");
        }

        var dataDirectory = FindDirectory(root, "Data") ?? root;
        var gamePath = FindFile(dataDirectory, "Game.dat") ?? FindFile(root, "Game.dat");
        if (gamePath == null)
        {
            return Failed<WolfProjectData>("WOLF plain data requires Data/Game.dat or Game.dat.", "load");
        }

        var diagnostics = new List<PluginDiagnostic>();
        var game = ReadGame(gamePath);
        diagnostics.AddRange(game.Diagnostics);
        if (!game.Success || game.Value == null)
        {
            return PluginResult<WolfProjectData>.Failed(game.Error!, diagnostics);
        }

        var basicDirectory = FindDirectory(dataDirectory, "BasicData");
        WolfDatabaseData? systemDatabase = null;
        WolfDatabaseData? variableDatabase = null;
        var userDatabases = new List<WolfDatabaseData>();
        if (basicDirectory != null)
        {
            var systemPath = FindAnyFile(basicDirectory, "System.db", "System.json", "SystemData.db");
            if (systemPath != null)
            {
                var system = _databaseReader.Read(systemPath, "system");
                diagnostics.AddRange(system.Diagnostics);
                if (!system.Success || system.Value == null)
                {
                    return PluginResult<WolfProjectData>.Failed(system.Error!, diagnostics);
                }
                systemDatabase = system.Value;
            }

            var variablePath = FindAnyFile(basicDirectory, "Variable.db", "Variables.db", "VariableData.db");
            if (variablePath != null)
            {
                var variable = _databaseReader.Read(variablePath, "variable");
                diagnostics.AddRange(variable.Diagnostics);
                if (!variable.Success || variable.Value == null)
                {
                    return PluginResult<WolfProjectData>.Failed(variable.Error!, diagnostics);
                }
                variableDatabase = variable.Value;
            }

            var databaseFiles = EnumerateDataFiles(basicDirectory)
                .Where(pPath => IsDatabaseFile(pPath)
                    && (systemPath == null || !pPath.Equals(systemPath, StringComparison.OrdinalIgnoreCase))
                    && (variablePath == null || !pPath.Equals(variablePath, StringComparison.OrdinalIgnoreCase)))
                .Take(_limits.MaxMaps)
                .ToArray();
            foreach (var databasePath in databaseFiles)
            {
                var database = _databaseReader.Read(databasePath, Path.GetFileNameWithoutExtension(databasePath));
                diagnostics.AddRange(database.Diagnostics);
                if (!database.Success || database.Value == null)
                {
                    return PluginResult<WolfProjectData>.Failed(database.Error!, diagnostics);
                }
                userDatabases.Add(database.Value);
            }
        }

        var maps = new List<WolfMapData>();
        var mapDirectory = FindDirectory(dataDirectory, "MapData");
        if (mapDirectory != null)
        {
            var mapPaths = EnumerateDataFiles(mapDirectory)
                .Where(pPath => Path.GetExtension(pPath).Equals(".mps", StringComparison.OrdinalIgnoreCase)
                    || Path.GetExtension(pPath).Equals(".json", StringComparison.OrdinalIgnoreCase))
                .Take(_limits.MaxMaps)
                .ToArray();
            foreach (var mapPath in mapPaths)
            {
                var map = _mapReader.Read(mapPath);
                diagnostics.AddRange(map.Diagnostics);
                if (!map.Success || map.Value == null)
                {
                    return PluginResult<WolfProjectData>.Failed(map.Error!, diagnostics);
                }
                maps.Add(map.Value);
            }
        }

        var commonEvents = Array.Empty<WolfEventProgram>();
        var commonPath = FindAnyFile(dataDirectory, "CommonEvents.json", "CommonEvent.db", "CommonEvents.db");
        if (commonPath != null)
        {
            var common = WolfMapReader.ReadCommonEvents(commonPath, _limits);
            diagnostics.AddRange(common.Diagnostics);
            if (!common.Success || common.Value == null)
            {
                return PluginResult<WolfProjectData>.Failed(common.Error!, diagnostics);
            }
            commonEvents = common.Value.ToArray();
        }

        return PluginResult<WolfProjectData>.Succeeded(new WolfProjectData
        {
            Title = game.Value.Title,
            FormatVersion = game.Value.FormatVersion,
            IsProtected = false,
            SourceDirectory = root,
            SystemDatabase = systemDatabase,
            UserDatabases = userDatabases,
            VariableDatabase = variableDatabase,
            Maps = maps.OrderBy(pMap => pMap.Id).ToArray(),
            CommonEvents = commonEvents.OrderBy(pEvent => pEvent.Id).ToArray(),
        }, diagnostics);
    }

    public PluginResult<WolfProjectData> Analyze(string pGameDirectory) => Load(pGameDirectory);

    private PluginResult<WolfGameHeader> ReadGame(string pPath)
    {
        var root = ReadDocument(pPath, "game");
        if (!root.Success)
        {
            return PluginResult<WolfGameHeader>.Failed(root.Error!, root.Diagnostics);
        }
        if (IsProtected(root.Value))
        {
            return PluginResult<WolfGameHeader>.Failed(PluginError.Create(
                PluginErrorCode.UnsupportedEngine,
                "Protected or encrypted WOLF data is intentionally unsupported; provide authorized plain data.",
                EnginePluginIds.WolfRpg,
                "wolf-load"), new[]
            {
                PluginDiagnostic.Warning("wolf.protected-data", "Protected WOLF data was detected and was not decrypted.", EnginePluginIds.WolfRpg),
            });
        }

        var version = ReadRequiredInt(root.Value, "version", pPath);
        if (!version.Success)
        {
            return PluginResult<WolfGameHeader>.Failed(version.Error!, root.Diagnostics);
        }
        var title = ReadOptionalString(root.Value, "title", _limits.MaxStringBytes);
        return PluginResult<WolfGameHeader>.Succeeded(new WolfGameHeader
        {
            Title = title,
            FormatVersion = version.Value,
        }, root.Diagnostics);
    }

    internal PluginResult<JsonElement> ReadDocument(string pPath, string pExpectedKind)
    {
        try
        {
            var fileInfo = new FileInfo(pPath);
            if (!fileInfo.Exists)
            {
                return Failed<JsonElement>($"WOLF data file '{pPath}' does not exist.", "wolf-load");
            }
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return Failed<JsonElement>($"WOLF data file '{pPath}' is a reparse point and was not followed.", "wolf-load");
            }
            if (fileInfo.Length <= 0 || fileInfo.Length > _limits.MaxFileBytes)
            {
                return Failed<JsonElement>($"WOLF data file '{Path.GetFileName(pPath)}' exceeds the bounded file-size limit.", "wolf-load");
            }

            var bytes = File.ReadAllBytes(pPath);
            var text = DecodeUtf8(bytes);
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                MaxDepth = 64,
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });
            var root = document.RootElement.Clone();
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Failed<JsonElement>($"WOLF data file '{Path.GetFileName(pPath)}' must contain a JSON object.", "wolf-load");
            }
            var format = ReadOptionalString(root, "format", 128);
            if (!format.Equals(WolfPlainFormat.Format, StringComparison.Ordinal))
            {
                return Failed<JsonElement>($"WOLF data file '{Path.GetFileName(pPath)}' is not the supported unencrypted URPG fixture format.", "wolf-load");
            }
            var version = ReadOptionalInt(root, "version", -1);
            if (version != WolfPlainFormat.Version)
            {
                return Failed<JsonElement>($"WOLF data file '{Path.GetFileName(pPath)}' uses unsupported plain-data version {version}.", "wolf-load");
            }
            var kind = ReadOptionalString(root, "kind", 128);
            if (!kind.Equals(pExpectedKind, StringComparison.Ordinal))
            {
                return Failed<JsonElement>($"WOLF data file '{Path.GetFileName(pPath)}' has kind '{kind}', expected '{pExpectedKind}'.", "wolf-load");
            }
            return PluginResult<JsonElement>.Succeeded(root);
        }
        catch (JsonException exception)
        {
            return Failed<JsonElement>($"WOLF data file '{Path.GetFileName(pPath)}' contains invalid JSON: {exception.Message}", "wolf-load");
        }
        catch (IOException exception)
        {
            return Failed<JsonElement>($"WOLF data file '{Path.GetFileName(pPath)}' could not be read: {exception.Message}", "wolf-load");
        }
        catch (UnauthorizedAccessException)
        {
            return Failed<JsonElement>($"WOLF data file '{Path.GetFileName(pPath)}' could not be accessed.", "wolf-load");
        }
    }

    internal static PluginResult<T> Failed<T>(string pMessage, string pPhase)
    {
        return PluginResult<T>.Failed(PluginError.Create(PluginErrorCode.InvalidGame, pMessage, EnginePluginIds.WolfRpg, pPhase));
    }

    internal static string DecodeUtf8(byte[] pBytes)
    {
        var offset = pBytes.Length >= 3 && pBytes[0] == 0xef && pBytes[1] == 0xbb && pBytes[2] == 0xbf ? 3 : 0;
        return new UTF8Encoding(false, true).GetString(pBytes, offset, pBytes.Length - offset);
    }

    internal static bool IsProtected(JsonElement pRoot)
    {
        if (pRoot.TryGetProperty("protected", out var protectedValue)
            && protectedValue.ValueKind == JsonValueKind.True)
        {
            return true;
        }
        if (pRoot.TryGetProperty("encryption", out var encryption)
            && encryption.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(encryption.GetString())
            && !encryption.GetString()!.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }

    internal static string ReadOptionalString(JsonElement pRoot, string pName, int pMaxBytes)
    {
        if (!pRoot.TryGetProperty(pName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return "";
        }
        var result = value.GetString() ?? "";
        return Encoding.UTF8.GetByteCount(result) <= pMaxBytes ? result : "";
    }

    internal static int ReadOptionalInt(JsonElement pRoot, string pName, int pDefault)
    {
        return pRoot.TryGetProperty(pName, out var value) && value.TryGetInt32(out var result) ? result : pDefault;
    }

    internal static PluginResult<int> ReadRequiredInt(JsonElement pRoot, string pName, string pPath)
    {
        if (!pRoot.TryGetProperty(pName, out var value) || !value.TryGetInt32(out var result))
        {
            return PluginResult<int>.Failed(PluginError.Create(
                PluginErrorCode.InvalidGame,
                $"WOLF data file '{Path.GetFileName(pPath)}' is missing integer field '{pName}'.",
                EnginePluginIds.WolfRpg,
                "wolf-load"));
        }
        return PluginResult<int>.Succeeded(result);
    }

    internal static IEnumerable<JsonElement> ReadArray(JsonElement pRoot, string pName)
    {
        if (!pRoot.TryGetProperty(pName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<JsonElement>();
        }
        return value.EnumerateArray().Select(pItem => pItem.Clone()).ToArray();
    }

    private static string? ResolveDirectory(string pPath)
    {
        if (string.IsNullOrWhiteSpace(pPath))
        {
            return null;
        }
        try
        {
            var root = Path.GetFullPath(pPath);
            var info = new DirectoryInfo(root);
            return info.Exists && !info.Attributes.HasFlag(FileAttributes.ReparsePoint) ? root : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindDirectory(string pRoot, string pName)
    {
        try
        {
            return new DirectoryInfo(pRoot).EnumerateDirectories()
                .Where(pDirectory => !pDirectory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                .OrderBy(pDirectory => pDirectory.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(pDirectory => pDirectory.Name.Equals(pName, StringComparison.OrdinalIgnoreCase))?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindFile(string pRoot, string pName)
    {
        try
        {
            return new DirectoryInfo(pRoot).EnumerateFiles()
                .Where(pFile => !pFile.Attributes.HasFlag(FileAttributes.ReparsePoint))
                .OrderBy(pFile => pFile.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(pFile => pFile.Name.Equals(pName, StringComparison.OrdinalIgnoreCase))?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindAnyFile(string pRoot, params string[] pNames)
    {
        foreach (var name in pNames)
        {
            var path = FindFile(pRoot, name);
            if (path != null)
            {
                return path;
            }
        }
        return null;
    }

    private static IEnumerable<string> EnumerateDataFiles(string pRoot)
    {
        try
        {
            return new DirectoryInfo(pRoot).EnumerateFiles()
                .Where(pFile => !pFile.Attributes.HasFlag(FileAttributes.ReparsePoint))
                .OrderBy(pFile => pFile.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(pFile => pFile.Name, StringComparer.Ordinal)
                .Select(pFile => pFile.FullName)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static bool IsDatabaseFile(string pPath)
    {
        var extension = Path.GetExtension(pPath);
        return extension.Equals(".db", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class WolfGameHeader
    {
        public string Title { get; init; } = "";
        public int FormatVersion { get; init; }
    }
}
