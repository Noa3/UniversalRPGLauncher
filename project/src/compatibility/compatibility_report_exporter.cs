using System;
using System.Text;
using UniversalRPG.App.Library;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;

namespace UniversalRPG.Compatibility;

/// <summary>
/// Builds a redacted, deterministic Markdown report suitable for a GitHub issue.
/// It contains detection metadata only and never uploads or executes anything.
/// </summary>
public static class CompatibilityReportExporter
{
    public static string ToMarkdown(GameLibrary.GameEntry pEntry)
    {
        ArgumentNullException.ThrowIfNull(pEntry);
        var detection = pEntry.Detection;
        var builder = new StringBuilder();
        builder.AppendLine("# UniversalRPG Compatibility Report");
        builder.AppendLine();
        builder.AppendLine("> Generated locally from bounded metadata inspection. No game code was executed.");
        builder.AppendLine();
        builder.AppendLine("## Game");
        builder.AppendLine();
        AppendField(builder, "Title", pEntry.Title);
        AppendField(builder, "Path", pEntry.Path);
        AppendField(builder, "Library status", pEntry.CompatibilityStatus.ToString());
        AppendField(builder, "Selected plugin", string.IsNullOrEmpty(pEntry.SelectedPluginId) ? "none" : pEntry.SelectedPluginId);
        builder.AppendLine();
        builder.AppendLine("## Detection");
        builder.AppendLine();
        var primary = detection.Candidates.Count > 0 ? detection.Candidates[0] : null;
        AppendField(builder, "Engine", detection.GetEngineName());
        AppendField(builder, "Engine ID", primary?.EngineId ?? "unknown");
        AppendField(builder, "Generation", primary?.Generation ?? "unknown");
        AppendField(builder, "Runtime version", detection.EngineVersion?.ToString() ?? "unknown");
        AppendField(builder, "Confidence", detection.GetConfidenceString());
        AppendField(builder, "Inspection", detection.Report.Inspection?.IsPartial == true ? "partial (bounded limit reached)" : "complete");
        builder.AppendLine();
        AppendCandidates(builder, detection);
        AppendDiagnostics(builder, detection);
        builder.AppendLine("## Security boundary");
        builder.AppendLine();
        builder.AppendLine("- Imported content was inspected as untrusted data only.");
        builder.AppendLine("- EXE, DLL, Ruby, JavaScript, native plugins, and external processes were not executed.");
        builder.AppendLine("- MV/MZ and RGSS runtime support remains detection-only unless a bounded native backend is explicitly available.");
        return builder.ToString();
    }

    private static void AppendCandidates(StringBuilder pBuilder, GameDetector.DetectionResult pDetection)
    {
        pBuilder.AppendLine("## Candidates");
        pBuilder.AppendLine();
        pBuilder.AppendLine("| Plugin | Generation | Version | Score | Status |");
        pBuilder.AppendLine("|---|---|---|---:|---|");
        foreach (var candidate in pDetection.Candidates)
        {
            pBuilder.Append('|').Append(Cell(candidate.DisplayName))
                .Append('|').Append(Cell(candidate.Generation))
                .Append('|').Append(Cell(candidate.EngineVersion?.ToString() ?? "unknown"))
                .Append('|').Append(candidate.Score)
                .Append('|').Append(Cell(candidate.Status.ToString())).AppendLine("|");
        }
        if (pDetection.Candidates.Count == 0)
        {
            pBuilder.AppendLine("| none | — | unknown | 0 | Unknown |");
        }
        pBuilder.AppendLine();
    }

    private static void AppendDiagnostics(StringBuilder pBuilder, GameDetector.DetectionResult pDetection)
    {
        pBuilder.AppendLine("## Diagnostics");
        pBuilder.AppendLine();
        if (pDetection.Diagnostics.Count == 0)
        {
            pBuilder.AppendLine("No diagnostics reported.");
            pBuilder.AppendLine();
            return;
        }
        foreach (var diagnostic in pDetection.Diagnostics)
        {
            pBuilder.Append("- **").Append(Cell(diagnostic.Severity.ToString())).Append("** ")
                .Append('`').Append(Cell(diagnostic.Code)).Append("` ")
                .AppendLine(diagnostic.Message);
        }
        pBuilder.AppendLine();
    }

    private static void AppendField(StringBuilder pBuilder, string pName, string pValue)
    {
        pBuilder.Append("- **").Append(pName).Append(":** ").AppendLine(Cell(pValue));
    }

    private static string Cell(string? pValue)
    {
        return (pValue ?? "").Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ").Replace("\n", " ");
    }
}
