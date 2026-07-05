using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave17GuardrailTests
{
    private static readonly Regex StaticConcurrentDictionaryField = new(
        @"private\s+static\s+readonly\s+ConcurrentDictionary<(?<key>[^,>]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void StaticRuntimeCaches_ShouldUseCacheContractUnlessTypeKeyed()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator");

        var offenders = productionFiles
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Select(item => new
            {
                item.File,
                item.Line,
                item.Text,
                Match = StaticConcurrentDictionaryField.Match(item.Text)
            })
            .Where(item => item.Match.Success)
            .Where(item => !string.Equals(
                item.Match.Groups["key"].Value.Trim(),
                "Type",
                StringComparison.Ordinal))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Static runtime caches keyed by user/input-shaped values should use BoundedRuntimeCache or another explicit cache contract: " +
            string.Join(Environment.NewLine, offenders));
    }
}
