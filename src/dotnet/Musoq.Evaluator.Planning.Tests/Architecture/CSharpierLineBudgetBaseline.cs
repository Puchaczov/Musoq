using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Musoq.Evaluator.Tests.Architecture;

/// <summary>
/// Fixed, measured formatting allowances preserve the pre-CSharpier growth headroom.
/// New files have no allowance. Never refresh this baseline to accommodate code growth.
/// </summary>
internal static class CSharpierLineBudgetBaseline
{
    private static readonly IReadOnlyDictionary<string, (int Lines, int NonBlankLines)> Adjustments = Load();

    public static int Lines(string filePath) => Adjustment(filePath).Lines;

    public static int NonBlankLines(string filePath) => Adjustment(filePath).NonBlankLines;

    internal static int Apply(int physicalLines, int formattingDelta) => physicalLines - formattingDelta;

    private static (int Lines, int NonBlankLines) Adjustment(string filePath)
    {
        var relative = Path.GetRelativePath(RepositorySourceScan.RepositoryRoot(), filePath).Replace('\\', '/');
        return Adjustments.TryGetValue(relative, out var adjustment) ? adjustment : default;
    }

    private static IReadOnlyDictionary<string, (int Lines, int NonBlankLines)> Load()
    {
        var path = Path.Combine(
            RepositorySourceScan.RepositoryRoot(),
            "src",
            "dotnet",
            "Musoq.Evaluator.Planning.Tests",
            "Fixtures",
            "CSharpierLineBudgetBaseline.json"
        );
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.GetProperty("adjustments").EnumerateObject())
            result.Add(property.Name, (property.Value[0].GetInt32(), property.Value[1].GetInt32()));
        return result;
    }
}
