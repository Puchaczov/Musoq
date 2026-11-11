using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Musoq.Evaluator.Tests.Architecture;

namespace Musoq.Evaluator.Tests;

internal static class EvaluatorTestSources
{
    private static readonly string[] ProjectNames =
    [
        "Musoq.Evaluator.Tests",
        "Musoq.Evaluator.Tests.Common",
        "Musoq.Evaluator.Query.Tests",
        "Musoq.Evaluator.Relational.Tests",
        "Musoq.Evaluator.Planning.Tests",
        "Musoq.Evaluator.Interpretation.Tests",
        "Musoq.Evaluator.Diagnostics.Tests",
        "Musoq.Evaluator.CodeGeneration.Tests",
        "Musoq.Evaluator.Runtime.Tests",
    ];

    public static IReadOnlyList<string> Directories()
    {
        var sourceRoot = Path.Combine(RepositorySourceScan.RepositoryRoot(), "src", "dotnet");
        var directories = ProjectNames
            .Select(name => Path.Combine(sourceRoot, name))
            .Where(directory => File.Exists(Path.Combine(directory, Path.GetFileName(directory) + ".csproj")))
            .ToArray();

        if (directories.Length == 0)
            throw new DirectoryNotFoundException("No evaluator test or fixture projects were found.");

        return directories;
    }

    public static string FilePath(string relativePath)
    {
        var matches = Directories()
            .Select(directory => Path.Combine(directory, relativePath))
            .Where(File.Exists)
            .ToArray();

        if (matches.Length != 1)
            throw new FileNotFoundException(
                $"Expected one evaluator source for {relativePath}, found {matches.Length}."
            );

        return matches[0];
    }

    public static IEnumerable<string> Files(string searchPattern = "*.cs")
    {
        return Directories()
            .SelectMany(directory => Directory.EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories))
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"));
    }

    public static IReadOnlyList<string> FamilyDirectories(string directory)
    {
        if (TryGetRelativeFamilyPath(directory, out var relative))
        {
            var matches = Directories()
                .Select(root => Path.GetFullPath(Path.Combine(root, relative)))
                .Where(Directory.Exists)
                .ToArray();

            if (matches.Length == 0)
                throw new DirectoryNotFoundException($"No evaluator source family directories for {directory}.");

            return matches;
        }

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        return [directory];
    }

    public static bool IsEvaluatorFamily(string directory)
    {
        return TryGetRelativeFamilyPath(directory, out _);
    }

    private static bool TryGetRelativeFamilyPath(string directory, out string relative)
    {
        var originalRoot = Path.Combine(
            RepositorySourceScan.RepositoryRoot(),
            "src",
            "dotnet",
            "Musoq.Evaluator.Tests"
        );
        relative = Path.GetRelativePath(originalRoot, directory);
        return relative == "." || !relative.StartsWith("..", StringComparison.Ordinal);
    }
}
