using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core019FunctionOwnerResolutionTests : BasicEntityTestBase
{
    [TestMethod]
    public void UnqualifiedUniqueMethod_ShouldUseOwnerIndependentOfArgumentAlias()
    {
        const string query = "select UniqueToA(b.Population) from #A.entities() a " +
                             "inner join #B.entities() b on a.City = b.City";

        var table = CreateAndRunVirtualMachine(
                query,
                schemaProvider: CreateProvider<MethodOwnerAutoResolutionTests.UniqueMethodLibraryA,
                    MethodOwnerAutoResolutionTests.SharedOnlyLibraryB>(
                    [new BasicEntity("Warsaw", "Poland", 100)],
                    [new BasicEntity("Warsaw", "Poland", 200)]))
            .Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(400m, table[0].Values[0]);
    }

    [TestMethod]
    public void UnqualifiedCommonAggregate_ShouldResolveWithArgumentFromOtherAlias()
    {
        const string query = "select a.City, Sum(b.Population) as TotalPopulation " +
                             "from #A.entities() a inner join #B.entities() b on a.City = b.City " +
                             "group by a.City";

        var table = CreateAndRunVirtualMachine(
                query,
                schemaProvider: CreateProvider<MethodOwnerAutoResolutionTests.SharedOnlyLibraryA,
                    MethodOwnerAutoResolutionTests.SharedOnlyLibraryB>(
                    [new BasicEntity("Warsaw", "Poland", 100)],
                    [new BasicEntity("Warsaw", "Poland", 200)]))
            .Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Warsaw", table[0].Values[0]);
        Assert.AreEqual(200m, table[0].Values[1]);
    }

    [TestMethod]
    public void ExplicitMethodOwner_ShouldAllowArgumentFromOtherAlias()
    {
        const string query = "select a.AmbiguousMethod(b.Population) from #A.entities() a " +
                             "inner join #B.entities() b on a.City = b.City";

        var table = CreateAndRunVirtualMachine(
                query,
                schemaProvider: CreateProvider<MethodOwnerAutoResolutionTests.AmbiguousMethodLibraryA,
                    MethodOwnerAutoResolutionTests.AmbiguousMethodLibraryB>(
                    [new BasicEntity("Warsaw", "Poland", 100)],
                    [new BasicEntity("Warsaw", "Poland", 200)]))
            .Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(20m, table[0].Values[0]);
    }

    [TestMethod]
    public void DifferentUnqualifiedMethodImplementations_ShouldReportOneStableDiagnostic()
    {
        const string query = "select AmbiguousMethod(a.Population) from #A.entities() a " +
                             "inner join #B.entities() b on a.City = b.City";

        var result = Analyze<MethodOwnerAutoResolutionTests.AmbiguousMethodLibraryA,
            MethodOwnerAutoResolutionTests.AmbiguousMethodLibraryB>(query);

        AssertSingleAmbiguityDiagnostic(
            result,
            query,
            DiagnosticCode.MQ3035_AmbiguousMethodOwner,
            "Method call 'AmbiguousMethod(a.Population)' is ambiguous because multiple source aliases expose different implementations: 'a', 'b'.",
            "AmbiguousMethod",
            "An unqualified method call matched multiple source aliases with different method implementations.",
            "Core Spec - Method Resolution",
            [
                "Prefix the method with the intended source alias, for example: first.MyMethod(...) or second.MyMethod(...).",
                "Choose the alias whose schema library should own the method implementation."
            ]);
    }

    [TestMethod]
    public void SameSourceInjectedImplementationThroughTwoAliases_ShouldReportOneStableDiagnostic()
    {
        const string query = "select GetCountry() from #A.entities() a " +
                             "inner join #B.entities() b on a.City = b.City";

        var result = Analyze<Library, Library>(query);

        AssertSingleAmbiguityDiagnostic(
            result,
            query,
            DiagnosticCode.MQ3035_AmbiguousMethodOwner,
            "Method call 'GetCountry()' is ambiguous because multiple source aliases expose different implementations: 'a', 'b'.",
            "GetCountry",
            "An unqualified method call matched multiple source aliases with different method implementations.",
            "Core Spec - Method Resolution",
            [
                "Prefix the method with the intended source alias, for example: first.MyMethod(...) or second.MyMethod(...).",
                "Choose the alias whose schema library should own the method implementation."
            ]);
    }

    [TestMethod]
    public void DifferentUnqualifiedAggregateImplementations_ShouldReportOneStableDiagnostic()
    {
        const string query = "select AmbiguousAgg(b.Population) as AggValue from #A.entities() a " +
                             "inner join #B.entities() b on a.City = b.City group by a.City";

        var result = Analyze<AggregateOwnerAmbiguityTests.AggregateLibraryA,
            AggregateOwnerAmbiguityTests.AggregateLibraryB>(query);

        AssertSingleAmbiguityDiagnostic(
            result,
            query,
            DiagnosticCode.MQ3034_AmbiguousAggregateOwner,
            "Aggregate call 'AmbiguousAgg(b.Population)' is ambiguous because multiple source aliases expose different implementations: 'a', 'b'.",
            "AmbiguousAgg",
            "An unqualified aggregate call matched multiple source aliases with different aggregate implementations.",
            "Core Spec - Aggregation",
            [
                "Prefix the aggregate with the intended source alias, for example: first.Sum(...) or second.Sum(...).",
                "If the aggregate appears in ORDER BY, alias it in SELECT first and order by that projection alias."
            ]);
    }

    private static void AssertSingleAmbiguityDiagnostic(
        QueryAnalysisResult result,
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        string expectedExpression,
        string expectedExplanation,
        string expectedDocsReference,
        string[] expectedFixes)
    {
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var diagnostics = result.Errors.ToArray();
        Assert.HasCount(1, diagnostics, FormatDiagnostics(result));

        var diagnostic = diagnostics[0];
        var expectedStart = query.IndexOf(expectedExpression, StringComparison.Ordinal);
        var expectedSpan = new TextSpan(expectedStart, expectedExpression.Length);

        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsEmpty(diagnostic.Arguments);
        Assert.IsEmpty(diagnostic.SuggestedFixes);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedSpan.End, envelope.EndOffset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.AreEqual(expectedMessage, envelope.Message);
        Assert.AreEqual(expectedExplanation, envelope.Explanation);
        Assert.AreEqual(expectedDocsReference, envelope.DocsReference);
        CollectionAssert.AreEqual(expectedFixes, envelope.SuggestedFixes.ToArray());
        Assert.HasCount(expectedFixes.Length, envelope.Actions);
        CollectionAssert.AreEqual(expectedFixes, envelope.Actions.Select(static action => action.Title).ToArray());
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
    }

    private static QueryAnalysisResult Analyze<TLeftLibrary, TRightLibrary>(string query)
        where TLeftLibrary : LibraryBase, new()
        where TRightLibrary : LibraryBase, new()
    {
        return new QueryAnalyzer(
                CreateProvider<TLeftLibrary, TRightLibrary>(),
                compilationOptions: new CompilationOptions(usePrimitiveTypeValidation: false))
            .Analyze(query);
    }

    private static GenericSchemaProvider CreateProvider<TLeftLibrary, TRightLibrary>(
        BasicEntity[]? left = null,
        BasicEntity[]? right = null)
        where TLeftLibrary : LibraryBase, new()
        where TRightLibrary : LibraryBase, new()
    {
        left ??= [new BasicEntity("Warsaw", "Poland", 100)];
        right ??= [new BasicEntity("Warsaw", "Poland", 200)];

        return new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            ["#A"] = CreateSchema<TLeftLibrary>(left),
            ["#B"] = CreateSchema<TRightLibrary>(right)
        });
    }

    private static GenericSchema<TLibrary> CreateSchema<TLibrary>(BasicEntity[] source)
        where TLibrary : LibraryBase, new()
    {
        return new GenericSchema<TLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["entities"] = (new BasicEntityTable(), new MultiRowSource<BasicEntity>(source))
        });
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
