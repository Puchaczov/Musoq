using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    private static IEnumerable<string> EnumerateSourceFamilyFiles(string directory, string searchPattern)
    {
        const string declaredTypePrefix = "declares:";
        if (!searchPattern.StartsWith(declaredTypePrefix, StringComparison.Ordinal))
        {
            var files = EvaluatorTestSources.FamilyDirectories(directory)
                .SelectMany(root => Directory.EnumerateFiles(root, searchPattern)
                    .Concat(searchPattern.StartsWith("GeneratedCodeSamplesCatalog", StringComparison.Ordinal)
                        ? Directory.EnumerateFiles(root, searchPattern.Replace("GeneratedCodeSamplesCatalog", "EvaluatorSampleFixtures", StringComparison.Ordinal))
                        : []))
                .ToArray();
            return RequireSourceFamilyFiles(directory, searchPattern, files);
        }

        var typeName = searchPattern[declaredTypePrefix.Length..];
        var declarations = EvaluatorTestSources.FamilyDirectories(directory)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file))
                .GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Any(declaration => declaration.Identifier.ValueText == typeName &&
                                    declaration.Modifiers.ToFullString()
                                        .Contains("partial", StringComparison.Ordinal)))
            .ToArray();
        return RequireSourceFamilyFiles(directory, searchPattern, declarations);
    }

    private static bool IsSplitLibraryTestFamily(string directory)
    {
        var sourceRoot = Path.Combine(Musoq.Evaluator.Tests.Architecture.RepositorySourceScan.RepositoryRoot(), "src", "dotnet");
        var relative = Path.GetRelativePath(sourceRoot, directory);
        var project = relative.Split(Path.DirectorySeparatorChar)[0];
        return project.StartsWith("Musoq.Converter.", StringComparison.Ordinal) ||
               project.StartsWith("Musoq.Plugins.", StringComparison.Ordinal);
    }

    private static string[] RequireSourceFamilyFiles(string directory, string searchPattern, string[] files)
    {
        if (files.Length == 0 && (EvaluatorTestSources.IsEvaluatorFamily(directory) || IsSplitLibraryTestFamily(directory)))
            throw new InvalidOperationException($"Migrated test source family is empty: {directory}/{searchPattern}");

        return files;
    }
}
