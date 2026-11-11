using System.IO;
using System.Text.RegularExpressions;
using Musoq.Evaluator.Tests.Architecture;

namespace Musoq.Evaluator.Tests;

internal static class GeneratedCodeSnapshotText
{
    private static readonly Regex GeneratedNamespace = new(
        @"(?m)^namespace (?!Musoq\.Generated\.Interpreters\b)[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*",
        RegexOptions.Compiled
    );

    public static string SamplesDirectory =>
        Path.Combine(RepositorySourceScan.RepositoryRoot(), "src", "dotnet", "generated-code-samples", "current");

    public static string NormalizeForComparison(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd();
        return GeneratedNamespace.Replace(normalized, "namespace __generated_sample_namespace__");
    }
}
