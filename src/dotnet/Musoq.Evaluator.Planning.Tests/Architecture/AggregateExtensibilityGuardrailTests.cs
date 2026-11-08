using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class AggregateExtensibilityGuardrailTests
{
    private static readonly Regex BuiltInAggregateNameLiteral = new(
        "\"(?:Count|Sum|Avg|Min|Max|StDev|StDevp|Variance|Variancep|AggregateValues|SumIncome|SumOutcome)\"|nameof\\s*\\([^)]*\\b(?:Count|Sum|Avg|Min|Max|StDev|Variance)\\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex CountSpecificWildcardBinding = new(
        "\\b(?:IsCountWildcardCall|CountWildcard|CountOnlyWildcard)\\b|\"Count\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ParserAggregateCaseRewrite = new(
        "\\b(?:CaseNode|WhenNode|ThenNode)\\b|new\\s+Case",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RegexBasedAggregateIdentity = new(
        "\\bRegex\\b|RegularExpressions|RegexOptions",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void ScalarSubqueryRewrite_ShouldNotClassifyAggregatesByBuiltInNames()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.FilesUnder(
            root,
            "src/dotnet/Musoq.Evaluator/Visitors",
            "SubqueryToCteRewriteVisitor.ScalarSubqueries*.cs");

        var offenders = FindMatches(root, files, BuiltInAggregateNameLiteral);

        Assert.IsEmpty(
            offenders,
            "Scalar subquery normalization must defer aggregate-vs-scalar classification to metadata binding: " +
            string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void WildcardAggregateBinding_ShouldNotSpecialCaseCount()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        string[] files =
        [
            Path.Combine(root, "src", "dotnet", "Musoq.Evaluator", "Visitors", "BuildMetadataAndInferTypesVisitor.Methods.Creation.cs"),
            Path.Combine(root, "src", "dotnet", "Musoq.Parser", "Parser.Methods.cs"),
            Path.Combine(root, "src", "dotnet", "Musoq.Parser", "PivotQueryBuilder.cs")
        ];

        var offenders = FindMatches(root, files, CountSpecificWildcardBinding);

        Assert.IsEmpty(
            offenders,
            "Aggregate(*) wildcard binding must stay metadata-backed and must not special-case Count: " +
            string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void ParserAggregateFilterAndPivot_ShouldNotRewriteArgumentsThroughCaseNodes()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        string[] files =
        [
            Path.Combine(root, "src", "dotnet", "Musoq.Parser", "Parser.Methods.cs"),
            Path.Combine(root, "src", "dotnet", "Musoq.Parser", "PivotQueryBuilder.cs")
        ];

        var offenders = FindMatches(root, files, ParserAggregateCaseRewrite);

        Assert.IsEmpty(
            offenders,
            "FILTER and PIVOT aggregate semantics must flow through aggregate filter metadata, not parser-time CASE argument rewrites: " +
            string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void AggregateBindingIdentity_ShouldNotUseRegexNormalization()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var files = new[]
            {
                Path.Combine(root, "src", "dotnet", "Musoq.Evaluator", "IR", "Bindings", "AggregateCallIdentity.cs"),
                Path.Combine(root, "src", "dotnet", "Musoq.Evaluator", "IR", "Expressions", "AggregateRefRewriter.cs")
            }
            .Concat(RepositorySourceScan.FilesUnder(
                root,
                "src/dotnet/Musoq.Evaluator/IR/Expressions",
                "AggregateRefRewriter.*.cs"));

        var offenders = FindMatches(root, files, RegexBasedAggregateIdentity);

        Assert.IsEmpty(
            offenders,
            "Aggregate binding identity must stay syntax-aware and must not use regex qualifier stripping: " +
            string.Join(Environment.NewLine, offenders));
    }

    private static string[] FindMatches(string repositoryRoot, IEnumerable<string> files, Regex pattern)
    {
        return files
            .Where(File.Exists)
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item => pattern.IsMatch(item.Text))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();
    }
}
