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
        var roots =
            projectNames.Length == 0
                ? [Path.Combine(repositoryRoot, "src", "dotnet")]
                : projectNames.Select(name => Path.Combine(repositoryRoot, "src", "dotnet", name)).ToArray();
        return roots
            .SelectMany(root =>
            {
                if (!Directory.Exists(root))
                    throw new DirectoryNotFoundException(
                        "Required production source directory does not exist: " + root
                    );
                var files = Directory
                    .GetFiles(root, "*.cs", SearchOption.AllDirectories)
                    .Where(file => IsProductionFile(repositoryRoot, file))
                    .ToArray();
                if (files.Length == 0)
                    throw new InvalidOperationException("The required production source inventory is empty: " + root);
                return files;
            })
            .ToArray();
    }

    private static bool IsProductionFile(string repositoryRoot, string file)
    {
        var relative = ToRelative(repositoryRoot, file);
        return !relative.StartsWith("src/dotnet/generated-code-samples/", StringComparison.Ordinal)
            && !relative.Contains("/bin/", StringComparison.Ordinal)
            && !relative.Contains("/obj/", StringComparison.Ordinal)
            && !relative.Contains(".Tests", StringComparison.Ordinal)
            && !relative.Contains(".Benchmarks", StringComparison.Ordinal);
    }

    public static bool IsGeneratedCodeSample(string repositoryRoot, string file) =>
        ToRelative(repositoryRoot, file).StartsWith("src/dotnet/generated-code-samples/", StringComparison.Ordinal);

    public static IReadOnlyList<string> FilesUnder(
        string repositoryRoot,
        string relativeDirectory,
        string searchPattern
    )
    {
        var directory = Path.Combine(repositoryRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
        if (files.Length == 0)
            throw new InvalidOperationException(
                "The required source family is empty: " + directory + "/" + searchPattern
            );
        return files;
    }

    public static int CountMatchingLines(string filePath, Regex pattern) =>
        File.ReadLines(filePath).Count(pattern.IsMatch);

    public static int CountMatchingLines(IEnumerable<string> filePaths, Regex pattern) =>
        filePaths.Sum(file => CountMatchingLines(file, pattern));

    public static int DistinctMatchCount(IEnumerable<string> filePaths, Regex pattern) =>
        filePaths
            .SelectMany(file => pattern.Matches(File.ReadAllText(file)).Select(match => match.Value))
            .Distinct(StringComparer.Ordinal)
            .Count();

    public static string ToRelative(string repositoryRoot, string file) =>
        Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');
}
