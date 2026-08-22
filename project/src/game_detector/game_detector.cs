using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;

namespace UniversalRPG.GameDetectorNs;

/// <summary>
/// Compatibility facade for the original application API. Engine-specific
/// inspection lives in registered detection plugins; this class only translates
/// the ranked plugin report into the legacy UI model.
/// </summary>
public partial class GameDetector : RefCounted
{
    public enum EngineType
    {
        Unknown,
        RpgMaker95,
        RpgMaker2000,
        RpgMaker2003,
        RpgMakerXp,
        RpgMakerVx,
        RpgMakerVxAce,
        RpgMakerMv,
        RpgMakerMz,
        RpgMaker2000_2003,
        WolfRpg,
        RpgMakerUnite,
    }

    public enum Confidence
    {
        Low,
        Medium,
        High,
    }

    public static readonly EngineType[] DetectableEngines =
    {
        EngineType.RpgMaker95,
        EngineType.RpgMaker2000,
        EngineType.RpgMaker2003,
        EngineType.RpgMakerXp,
        EngineType.RpgMakerVx,
        EngineType.RpgMakerVxAce,
        EngineType.RpgMakerMv,
        EngineType.RpgMakerMz,
        EngineType.WolfRpg,
        EngineType.RpgMakerUnite,
    };

    public const int MaxMetadataBytes = 1024 * 1024;

    private readonly PluginGameDetector _pluginDetector;

    public GameDetector()
        : this(BuiltInEnginePluginCatalog.CreateDetectionRegistry())
    {
    }

    public GameDetector(EngineDetectionRegistry pRegistry, GameInspectionLimits? pLimits = null)
    {
        _pluginDetector = new PluginGameDetector(pRegistry, pLimits);
    }

    public class DetectionResult
    {
        public EngineType Engine { get; set; } = EngineType.Unknown;
        public Confidence Confidence { get; set; } = Confidence.Low;
        public List<string> Evidence { get; } = new();
        public string Title { get; set; } = "";
        public string RtpDependency { get; set; } = "";
        public bool HasCustomScripts { get; set; }
        public bool HasNativeLibraries { get; set; }
        public bool HasEncryptedArchives { get; set; }
        public List<string> UnknownRuntimes { get; } = new();
        public string GameDirectory { get; set; } = "";
        public Version? EngineVersion { get; set; }
        public EngineDetectionReport Report { get; init; } = EngineDetectionReport.Unknown("", "No detection report was produced.");
        public IReadOnlyList<EngineDetectionCandidate> Candidates => Report.Candidates;
        public IReadOnlyList<PluginDiagnostic> Diagnostics => Report.Diagnostics
            .Concat(Report.InspectionDiagnostics.Select(pDiagnostic =>
                pDiagnostic.IsError
                    ? PluginDiagnostic.Warning(pDiagnostic.Code, pDiagnostic.Message)
                    : PluginDiagnostic.Info(pDiagnostic.Code, pDiagnostic.Message)))
            .ToArray();

        public string GetEngineName()
        {
            return Engine switch
            {
                EngineType.RpgMaker95 => "RPG Maker 95",
                EngineType.RpgMaker2000 => "RPG Maker 2000",
                EngineType.RpgMaker2003 => "RPG Maker 2003",
                EngineType.RpgMaker2000_2003 => "RPG Maker 2000/2003",
                EngineType.RpgMakerXp => "RPG Maker XP",
                EngineType.RpgMakerVx => "RPG Maker VX",
                EngineType.RpgMakerVxAce => "RPG Maker VX Ace",
                EngineType.RpgMakerMv => "RPG Maker MV",
                EngineType.RpgMakerMz => "RPG Maker MZ",
                EngineType.WolfRpg => "WOLF RPG Editor",
                EngineType.RpgMakerUnite => "RPG Maker Unite / Unity",
                _ => "Unknown",
            };
        }

        public string GetConfidenceString()
        {
            return Confidence switch
            {
                Confidence.High => TranslationServer.Translate("CONFIDENCE_HIGH"),
                Confidence.Medium => TranslationServer.Translate("CONFIDENCE_MEDIUM"),
                _ => TranslationServer.Translate("CONFIDENCE_LOW"),
            };
        }

        public string Describe()
        {
            var text = $"Detected engine: {GetEngineName()}\nConfidence: {GetConfidenceString()}\nEvidence:\n";
            foreach (var item in Evidence)
            {
                text += $"- {item}\n";
            }
            foreach (var diagnostic in Diagnostics)
            {
                text += $"[{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}\n";
            }
            return text;
        }
    }

    public DetectionResult Analyze(string pGameDirectory)
    {
        var report = _pluginDetector.Analyze(pGameDirectory);
        var snapshot = report.Inspection;
        var top = report.Candidates.FirstOrDefault();
        var selected = report.SelectedCandidate;
        var engine = ResolveEngine(report);
        var score = selected?.Score ?? top?.Score ?? 0;
        var result = new DetectionResult
        {
            Engine = engine,
            Confidence = ToConfidence(score),
            Title = selected?.Title ?? top?.Title ?? "",
            RtpDependency = selected?.RtpDependency ?? top?.RtpDependency ?? "",
            GameDirectory = pGameDirectory,
            EngineVersion = selected?.EngineVersion ?? top?.EngineVersion,
            Report = report,
        };
        if (report.IsAmbiguous)
        {
            foreach (var candidate in report.Candidates.Where(pCandidate => pCandidate.Score == score))
            {
                result.Evidence.AddRange(candidate.Evidence);
            }
        }
        else if (selected != null)
        {
            result.Evidence.AddRange(selected.Evidence);
        }
        result.Evidence.AddRange(report.InspectionDiagnostics
            .Where(pDiagnostic => pDiagnostic.IsError)
            .Select(pDiagnostic => pDiagnostic.Message));
        var uniqueEvidence = result.Evidence.Distinct(StringComparer.Ordinal).ToArray();
        result.Evidence.Clear();
        result.Evidence.AddRange(uniqueEvidence);

        if (snapshot != null)
        {
            result.HasCustomScripts = snapshot.Files.Any(pFile =>
                pFile.RelativePath.EndsWith(".rb", StringComparison.OrdinalIgnoreCase)
                || pFile.RelativePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase));
            result.HasNativeLibraries = snapshot.Files.Any(pFile => IsNative(pFile.RelativePath));
            result.HasEncryptedArchives = snapshot.Files.Any(pFile =>
                pFile.RelativePath.EndsWith(".rgssad", StringComparison.OrdinalIgnoreCase)
                || pFile.RelativePath.EndsWith(".rgss2a", StringComparison.OrdinalIgnoreCase)
                || pFile.RelativePath.EndsWith(".rgss3a", StringComparison.OrdinalIgnoreCase));
            foreach (var file in snapshot.Files)
            {
                var name = System.IO.Path.GetFileName(file.RelativePath);
                if (!IsNative(file.RelativePath) || name.StartsWith("rgss", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("rpg_rt", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                result.UnknownRuntimes.Add(name);
            }
            result.UnknownRuntimes.Sort(StringComparer.OrdinalIgnoreCase);
        }
        return result;
    }

    private static EngineType ResolveEngine(EngineDetectionReport pReport)
    {
        if (pReport.IsAmbiguous)
        {
            var ids = pReport.Candidates.Where(pCandidate => pCandidate.Score == pReport.Candidates[0].Score)
                .Select(pCandidate => pCandidate.EngineId).ToHashSet(StringComparer.Ordinal);
            if (ids.Contains(EnginePluginIds.RpgMaker2000) && ids.Contains(EnginePluginIds.RpgMaker2003))
            {
                return EngineType.RpgMaker2000_2003;
            }
            return EngineType.Unknown;
        }
        return FromPluginId(pReport.SelectedCandidate?.EngineId ?? "");
    }

    private static EngineType FromPluginId(string pEngineId)
    {
        return pEngineId switch
        {
            EnginePluginIds.RpgMaker95 => EngineType.RpgMaker95,
            EnginePluginIds.RpgMaker2000 => EngineType.RpgMaker2000,
            EnginePluginIds.RpgMaker2003 => EngineType.RpgMaker2003,
            EnginePluginIds.RpgMakerXp => EngineType.RpgMakerXp,
            EnginePluginIds.RpgMakerVx => EngineType.RpgMakerVx,
            EnginePluginIds.RpgMakerVxAce => EngineType.RpgMakerVxAce,
            EnginePluginIds.RpgMakerMv => EngineType.RpgMakerMv,
            EnginePluginIds.RpgMakerMz => EngineType.RpgMakerMz,
            EnginePluginIds.WolfRpg => EngineType.WolfRpg,
            EnginePluginIds.RpgMakerUnite => EngineType.RpgMakerUnite,
            _ => EngineType.Unknown,
        };
    }

    private static Confidence ToConfidence(int pScore)
    {
        return pScore >= 700 ? Confidence.High : pScore >= 400 ? Confidence.Medium : Confidence.Low;
    }

    private static bool IsNative(string pPath)
    {
        return pPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            || pPath.EndsWith(".so", StringComparison.OrdinalIgnoreCase)
            || pPath.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase)
            || pPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
    }
}
