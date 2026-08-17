using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    [TestMethod]
    public void Spec_StableAlias_ApplyExamples_ShouldParseAndReportTheDocumentedBoundaryError()
    {
        const string validQuery = "select a1.Name, item.Value from a.b() a1 cross apply a1.Column item take 10";
        const string invalidQuery = "select a1.Name from a.b() a1 cross apply a1.Column take 10";

        var valid = new QueryAnalyzer(CreateProvider()).ValidateSyntax(validQuery);
        Assert.IsFalse(valid.HasErrors, FormatDiagnostics(valid.Diagnostics, validQuery));

        var invalid = new QueryAnalyzer(CreateProvider()).ValidateSyntax(invalidQuery);
        Assert.HasCount(1, invalid.Errors, FormatDiagnostics(invalid.Diagnostics, invalidQuery));
        var diagnostic = invalid.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(
            "The CROSS APPLY source requires an alias before TAKE.",
            diagnostic.Message);
        var sourceEnd = invalidQuery.IndexOf("a1.Column", StringComparison.Ordinal) + "a1.Column".Length;
        Assert.AreEqual(sourceEnd, diagnostic.Location.Offset);
        Assert.AreEqual(sourceEnd, diagnostic.EndLocation.Offset);
        Assert.AreEqual(0, diagnostic.Span.Length);
    }

    [TestMethod]
    public void Spec_StableAlias_ConcreteApplyExample_ShouldAnalyzeCompileAndExecute()
    {
        const string query =
            "select a1.Name, item.Name from #A.Entities() a1 " +
            "cross apply a1.Children item order by a1.Name, item.Name take 10";

        var analysis = new QueryAnalyzer(CreateProvider()).Analyze(query);
        Assert.IsFalse(analysis.HasErrors, FormatDiagnostics(analysis.Diagnostics, query));

        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            $"SpecStableAliasApply_{Guid.NewGuid():N}",
            CreateProvider(),
            LoggerResolver,
            TestCompilationOptions);

        Assert.IsTrue(build.Succeeded, FormatDiagnostics(build.Diagnostics, query));
        Assert.IsEmpty(build.Diagnostics, FormatDiagnostics(build.Diagnostics, query));

        using var table = build.CompiledQuery!.Run(TokenSource.Token);
        CollectionAssert.AreEqual(
            new[] { "word:child1", "word:child2" },
            table.Select(row => $"{row[0]}:{row[1]}").ToArray());
    }

    [TestMethod]
    public void Spec_StableAlias_BracketedReservedWords_ShouldCompileAndExecute()
    {
        const string bracketedQuery =
            "select [take].Name, [where].Name from #A.Entities() [take] " +
            "cross apply [take].Children [where] order by [where].Name take 10";
        var bracketedBuild = InstanceCreator.CompileWithDiagnostics(
            bracketedQuery,
            $"SpecStableAliasBracketed_{Guid.NewGuid():N}",
            CreateProvider(),
            LoggerResolver,
            TestCompilationOptions);

        Assert.IsTrue(bracketedBuild.Succeeded, FormatDiagnostics(bracketedBuild.Diagnostics, bracketedQuery));
        Assert.IsEmpty(bracketedBuild.Diagnostics, FormatDiagnostics(bracketedBuild.Diagnostics, bracketedQuery));
        using (var bracketedTable = bracketedBuild.CompiledQuery!.Run(TokenSource.Token))
        {
            CollectionAssert.AreEqual(
                new[] { "word:child1", "word:child2" },
                bracketedTable.Select(row => $"{row[0]}:{row[1]}").ToArray());
        }
    }

    [TestMethod]
    public void Spec_StableAlias_DerivedAndValuesSources_ShouldCompileAndExecute()
    {
        AssertValidSourceQuery(
            "select d.Name from (select a.Name from #A.Entities() a) d",
            "Derived");
        AssertValidSourceQuery(
            "from values { { Name: 'inline' } } valuesSource select valuesSource.Name",
            "Values");
        AssertValidSourceQuery(
            "select a.Name, valuesSource.Name from #A.Entities() a cross join values { { Name: 'inline' } } valuesSource",
            "ValuesJoin");
    }

    private void AssertValidSourceQuery(string query, string label)
    {
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            $"SpecStableAliasSource_{label}_{Guid.NewGuid():N}",
            CreateProvider(),
            LoggerResolver,
            TestCompilationOptions);

        Assert.IsTrue(build.Succeeded, FormatDiagnostics(build.Diagnostics, query));
        Assert.IsEmpty(build.Errors, FormatDiagnostics(build.Diagnostics, query));
        Assert.IsFalse(build.Diagnostics.Any(static diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ2035_MissingRequiredAlias),
            FormatDiagnostics(build.Diagnostics, query));
        using var table = build.CompiledQuery!.Run(TokenSource.Token);
        Assert.IsTrue(table.Count > 0, query);
    }

    [TestMethod]
    public void Spec_StableAlias_CompilationShouldKeepTheRequiredAliasAsTheOnlyError()
    {
        const string query = "select a1.Name from a.b() a1 cross apply a1.Column take 10";
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            $"SpecStableAliasError_{Guid.NewGuid():N}",
            CreateProvider(),
            LoggerResolver,
            TestCompilationOptions);

        Assert.IsFalse(build.Succeeded);
        Assert.IsNull(build.CompiledQuery);
        Assert.HasCount(1, build.Errors, FormatDiagnostics(build.Diagnostics, query));
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, build.Errors[0].Code);
        Assert.IsEmpty(build.Warnings);
        Assert.HasCount(1, build.ToEnvelopes());
    }

    private static string FormatDiagnostics(System.Collections.Generic.IEnumerable<Diagnostic> diagnostics, string query)
    {
        return $"{query}{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))}";
    }
}
