using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator.Tests.Architecture;

/// <summary>
/// Shared source-scanning helpers used by internal contract guardrail tests.
/// Scans on-disk production sources so loose-contract ratchets stay independent of runtime state.
/// </summary>
internal static class RepositorySourceScan
{
    public static string RepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src", "dotnet", "Musoq.Evaluator")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from the test output directory.");
    }

    public static IReadOnlyList<string> ProductionSourceFiles(string repositoryRoot, params string[] projectNames)
    {
        var separator = Path.DirectorySeparatorChar;
        var roots = projectNames.Length == 0
            ? [Path.Combine(repositoryRoot, "src", "dotnet")]
            : projectNames.Select(name => Path.Combine(repositoryRoot, "src", "dotnet", name)).ToArray();

        return roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{separator}bin{separator}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{separator}obj{separator}", StringComparison.Ordinal))
            .Where(file => !file.Contains(".Tests", StringComparison.Ordinal))
            .Where(file => !file.Contains(".Benchmarks", StringComparison.Ordinal))
            .ToArray();
    }

    public static IReadOnlyList<string> FilesUnder(string repositoryRoot, string relativeDirectory, string searchPattern)
    {
        var directory = Path.Combine(repositoryRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories).ToArray()
            : [];
    }

    public static int CountMatchingLines(string filePath, Regex pattern) => File
        .ReadLines(filePath)
        .Count(pattern.IsMatch);

    public static int CountMatchingLines(IEnumerable<string> filePaths, Regex pattern) => filePaths
        .Sum(file => CountMatchingLines(file, pattern));

    public static int DistinctMatchCount(IEnumerable<string> filePaths, Regex pattern) => filePaths
        .SelectMany(file => pattern.Matches(File.ReadAllText(file)).Select(match => match.Value))
        .Distinct(StringComparer.Ordinal)
        .Count();

    public static string ToRelative(string repositoryRoot, string file) => Path
        .GetRelativePath(repositoryRoot, file)
        .Replace(Path.DirectorySeparatorChar, '/');
}
