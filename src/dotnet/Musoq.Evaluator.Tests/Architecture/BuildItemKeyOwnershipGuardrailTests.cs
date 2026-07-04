using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

/// <summary>
/// Wave 2 guardrails: build item keys must live in the central registry, and BuildItems accessors
/// must reference those constants instead of inline string literals.
/// </summary>
[TestClass]
public sealed class BuildItemKeyOwnershipGuardrailTests
{
    private const string BuildDirectory = "src/dotnet/Musoq.Converter/Build";
    private const string KeyRegistryFileName = "BuildItemKeys.cs";

    private static readonly Regex UpperSnakeStringLiteral = new("\"[A-Z][A-Z0-9_]{2,}\"", RegexOptions.Compiled);
    private static readonly Regex BuildItemsDictionaryContract = new(
        "\\b(?:I?Dictionary)<string, ?object>\\b.*\\bBuildItems\\b|\\bBuildItems\\b.*\\b(?:I?Dictionary)<string, ?object>\\b",
        RegexOptions.Compiled);
    private static readonly Regex BuildItemsMethodParameter = new(
        @"\b(?:public|internal|private|protected|static|sealed|partial|async|virtual|override|readonly|extern|new|unsafe|\s)+[\w<>\[\], ?]+\s+\w+\s*\([^)]*\bBuildItems\b[^)]*\)",
        RegexOptions.Compiled);

    [TestMethod]
    public void BuildItemsAccessors_ShouldNotContainInlineKeyLiterals()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var accessorFiles = RepositorySourceScan
            .FilesUnder(repositoryRoot, BuildDirectory, "BuildItems*.cs")
            .ToArray();

        var offenders = accessorFiles
            .Select(file => new
            {
                File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                Hits = RepositorySourceScan.CountMatchingLines(file, UpperSnakeStringLiteral)
            })
            .Where(entry => entry.Hits > 0)
            .Select(entry => $"{entry.File}: {entry.Hits}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "BuildItems accessor files must reference BuildItemKeys constants, not inline key literals: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void BuildItemKeysRegistry_ShouldOwnEveryBuildItemKeyLiteral()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var registryFile = RepositorySourceScan
            .FilesUnder(repositoryRoot, BuildDirectory, KeyRegistryFileName)
            .Single();

        var registryKeys = RepositorySourceScan.DistinctMatchCount([registryFile], UpperSnakeStringLiteral);

        Assert.IsGreaterThanOrEqualTo(
            38,
            registryKeys,
            $"BuildItemKeys must declare every build item key constant; only found {registryKeys}.");
    }

    [TestMethod]
    public void BuildItemsRawDictionaryContract_ShouldRemainCompatibilityOnly()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan
            .ProductionSourceFiles(repositoryRoot, "Musoq.Converter", "Musoq.Evaluator")
            .Where(file => !Path.GetFileName(file).StartsWith("BuildItems", System.StringComparison.Ordinal))
            .ToArray();

        var offenders = files
            .Select(file => new
            {
                File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                Hits = RepositorySourceScan.CountMatchingLines(file, BuildItemsDictionaryContract)
            })
            .Where(entry => entry.Hits > 0)
            .Select(entry => $"{entry.File}: {entry.Hits}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "BuildItems inherits Dictionary<string, object> for public compatibility only. " +
            "Production code must consume typed BuildItems members or BuildArtifactStore helpers instead: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void BuildItemsDictionaryInheritance_ShouldStayDocumentedAsLegacyCompatibility()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var buildItemsFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Converter", "Build", "BuildItems.cs");
        var text = File.ReadAllText(buildItemsFile);

        Assert.Contains("public partial class BuildItems : Dictionary<string, object>", text);
        Assert.Contains("legacy compatibility shell", text);
        Assert.Contains("typed stage artifacts", text);
    }

    [TestMethod]
    public void BuildItemsMethodParameters_ShouldRemainBoundaryAdaptersOnly()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Evaluator");

        var offenders = productionFiles
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item => BuildItemsMethodParameter.IsMatch(item.Text))
            .Where(item => !IsAllowedBuildItemsBoundary(item.File, item.Text))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "BuildItems parameters are allowed only at build-chain boundaries and explicit compatibility adapters: " +
            string.Join(System.Environment.NewLine, offenders));
    }

    private static bool IsAllowedBuildItemsBoundary(string relativePath, string text)
    {
        if (relativePath.StartsWith("src/dotnet/Musoq.Converter/Build/", System.StringComparison.Ordinal))
            return text.Contains("Build(BuildItems items)", System.StringComparison.Ordinal) ||
                   text.Contains("From(BuildItems items)", System.StringComparison.Ordinal);

        if (relativePath.StartsWith("src/dotnet/Musoq.Converter/InstanceCreator", System.StringComparison.Ordinal))
            return true;

        return relativePath is
            "src/dotnet/Musoq.Converter/CompiledQueryArtifactSupport.cs" or
            "src/dotnet/Musoq.Converter/Diagnostics/DiagnosticSqlCommandCompiler.cs";
    }
}
