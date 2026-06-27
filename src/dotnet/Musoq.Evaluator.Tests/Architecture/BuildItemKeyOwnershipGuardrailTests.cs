using System.Linq;
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
}
