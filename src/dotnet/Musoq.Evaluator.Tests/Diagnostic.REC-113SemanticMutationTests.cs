using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.IR;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC113SemanticMutationTests
{
    private const string RecursiveSeed =
        "with recursive counter (Value) as (" +
        "select seed.Value from values {( Value: 1 )} seed union all " +
        "select c.Value + 1 from counter c where c.Value < 3) " +
        "select Value from counter";

    private const string RecursiveAnchor =
        "select seed.Value from values {( Value: 1 )} seed";

    private const string RecursiveMember =
        "select c.Value + 1 from counter c where c.Value < 3";

    private static readonly DiagnosticCode[] StableForbiddenCodes =
    [
        DiagnosticCode.MQ8001_CodeGenerationFailed,
        DiagnosticCode.MQ9001_InternalCompilerError,
        DiagnosticCode.MQ9002_InternalExecutionError
    ];

    [TestMethod]
    public void SemanticMutationMatrix_ShouldHonorFrozenContract()
    {
        var candidates = CandidateCases;
        Assert.HasCount(48, candidates);

        foreach (var candidate in candidates)
        {
            var seed = Analyze(candidate.SeedQuery);
            Assert.IsTrue(
                seed.IsSuccess,
                $"{candidate.Id} seed is not valid: {FormatDiagnostics(seed)}\n{candidate.SeedQuery}");

            var result = Analyze(candidate.Query);
            var errors = result.Errors.ToArray();
            if (candidate.ExpectedCode is null)
            {
                Assert.IsTrue(result.IsSuccess, $"{candidate.Id}: {FormatDiagnostics(result)}");
                Assert.IsEmpty(errors, $"{candidate.Id}: {FormatDiagnostics(result)}");
                continue;
            }

            Assert.IsFalse(result.IsSuccess, $"{candidate.Id} unexpectedly succeeded.");
            Assert.HasCount(1, errors, $"{candidate.Id}: {FormatDiagnostics(result)}");

            var diagnostic = errors[0];
            Assert.AreEqual(candidate.ExpectedCode.Value, diagnostic.Code, candidate.Id);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity, candidate.Id);
            Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase, candidate.Id);
            Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, candidate.Id);
            Assert.IsTrue(diagnostic.Location.IsValid, candidate.Id);
            Assert.IsTrue(diagnostic.EndLocation.IsValid, candidate.Id);
            Assert.IsTrue(diagnostic.Span.Start >= 0, candidate.Id);
            Assert.IsTrue(diagnostic.Span.End <= candidate.Query.Length, candidate.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet), candidate.Id);
            Assert.IsTrue(
                diagnostic.Message.Contains(candidate.MessageToken, StringComparison.OrdinalIgnoreCase) ||
                candidate.Query.Substring(diagnostic.Span.Start, diagnostic.Span.Length)
                    .Contains(candidate.MessageToken, StringComparison.OrdinalIgnoreCase),
                $"{candidate.Id}: token '{candidate.MessageToken}' was not located in '{diagnostic.Message}'.");
            Assert.IsFalse(
                StableForbiddenCodes.Contains(diagnostic.Code),
                $"{candidate.Id}: semantic mutation reached a code-generation/internal fallback.");
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainEightFamiliesOfSixCases()
    {
        var groups = CandidateCases
            .GroupBy(static candidate => candidate.Family, StringComparer.Ordinal)
            .ToArray();

        Assert.HasCount(8, groups);
        Assert.IsTrue(groups.All(static group => group.Count() == 6));
        Assert.IsTrue(CandidateCases.All(static candidate => !string.IsNullOrWhiteSpace(candidate.SeedQuery)));
        Assert.IsTrue(CandidateCases.All(static candidate => !string.IsNullOrWhiteSpace(candidate.Query)));
        Assert.AreEqual(32, CandidateCases.Count(static candidate => candidate.ExpectedCode is not null));
        Assert.AreEqual(16, CandidateCases.Count(static candidate => candidate.ExpectedCode is null));
    }

    [TestMethod]
    public void NormalizedSemanticFailures_ShouldStopBeforePhysicalPlanning()
    {
        var ids = new[] { "G01", "G02", "G03", "G04", "G05" };
        foreach (var candidate in CandidateCases.Where(candidate => ids.Contains(candidate.Id)))
        {
            var buildItems = PlanOnlyBuildItems.Create(candidate.Query);
            var errors = buildItems.DiagnosticContext.Errors.ToArray();

            Assert.IsNotEmpty(errors, candidate.Id);
            Assert.AreEqual(candidate.ExpectedCode, errors[0].Code, candidate.Id);
            Assert.IsNull(buildItems.LogicalPlan, candidate.Id);
            Assert.IsNull(buildItems.PhysicalPlan, candidate.Id);
            Assert.IsFalse(
                errors.Any(error => StableForbiddenCodes.Contains(error.Code)),
                $"{candidate.Id}: {FormatDiagnostics(errors)}");
        }
    }

    [TestMethod]
    public void AggregateOwnerAndResultOrdering_ShouldRemainDistinct()
    {
        const string ambiguous =
            "select AmbiguousAgg(b.Population) as AggValue from #A.entities() a " +
            "inner join #B.entities() b on a.City = b.City group by a.City";

        var ambiguousResult = new QueryAnalyzer(
                CreateAmbiguousAggregateSchemaProvider(),
                compilationOptions: new CompilationOptions(usePrimitiveTypeValidation: false))
            .Analyze(ambiguous);
        var errors = ambiguousResult.Errors.ToArray();

        Assert.HasCount(1, errors, FormatDiagnostics(ambiguousResult));
        Assert.AreEqual(DiagnosticCode.MQ3034_AmbiguousAggregateOwner, errors[0].Code);
        Assert.AreEqual(DiagnosticPhase.Bind, errors[0].Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, errors[0].SourceKind);
        StringAssert.Contains(errors[0].Message, "AmbiguousAgg");
        Assert.IsFalse(errors.Any(error => error.Code == DiagnosticCode.MQ3012_NonAggregateInSelect));

        const string ordered =
            "select City, Count(*) as Rows from #A.entities() " +
            "group by City having Count(*) > 0 order by Rows desc skip 0 take 2";
        var orderedResult = Analyze(ordered);
        Assert.IsTrue(orderedResult.IsSuccess, FormatDiagnostics(orderedResult));
        Assert.IsEmpty(orderedResult.Errors);
    }

    private static IReadOnlyList<SemanticCandidate> CandidateCases { get; } =
    [
        Invalid(
            "A01", "GROUPING",
            "select City, Count(*) as Rows from #A.Entities() group by City",
            "select City, Name, Count(*) as Rows from #A.Entities() group by City",
            DiagnosticCode.MQ3012_NonAggregateInSelect, "Name"),
        Invalid(
            "A02", "GROUPING",
            "select City, Count(*) as Rows from #A.Entities() group by City",
            "select Count(*) from #A.Entities() group by Sum(Population)",
            DiagnosticCode.MQ3092_AggregateInGroupBy, "Sum"),
        Invalid(
            "A03", "GROUPING",
            "select City, Count(*) as Rows from #A.Entities() group by City",
            "select City, Count(*) from #A.Entities() group by 3",
            DiagnosticCode.MQ3024_GroupByIndexOutOfRange, "3"),
        Invalid(
            "A04", "GROUPING",
            "select City, Count(*) as Rows from #A.Entities() group by City",
            "select City, Count(*) from #A.Entities() group by Country",
            DiagnosticCode.MQ3012_NonAggregateInSelect, "City"),
        Valid(
            "A05", "GROUPING",
            "select City, Count(*) from #A.Entities() group by all"),
        Valid(
            "A06", "GROUPING",
            "select City, Count(*) from #A.Entities() group by 1"),

        Invalid(
            "B01", "WINDOWS",
            "select Name, RowNumber() over (order by Name) from #A.Entities()",
            "select Name, RowNumber() over () from #A.Entities()",
            DiagnosticCode.MQ3099_WindowOrderByRequired, "RowNumber"),
        Invalid(
            "B02", "WINDOWS",
            "select Name, Sum(Population) over (order by Population) from #A.Entities()",
            "select Sum(Population) over (range between unbounded preceding and current row) from #A.Entities()",
            DiagnosticCode.MQ3052_RangeFrameRequiresOrderBy, "RANGE"),
        Invalid(
            "B03", "WINDOWS",
            "select Name, Sum(Population) over (order by Population rows between unbounded preceding and current row) from #A.Entities()",
            "select Sum(Population) over (order by Name rows between unbounded following and current row) from #A.Entities()",
            DiagnosticCode.MQ3053_InvalidWindowFrameBounds, "UNBOUNDED FOLLOWING"),
        Invalid(
            "B04", "WINDOWS",
            "select Name, Sum(Population) over (order by Population range between 1 preceding and current row) from #A.Entities()",
            "select Sum(Population) over (order by Name range between 1 preceding and current row) from #A.Entities()",
            DiagnosticCode.MQ3098_InvalidRangeFrameOrderKey, "RANGE"),
        Invalid(
            "B05", "WINDOWS",
            "select Name, RowNumber() over (order by Name) from #A.Entities()",
            "select Name from #A.Entities() where RowNumber() over (order by Name) = 1",
            DiagnosticCode.MQ3101_WindowFunctionInFilter, "RowNumber"),
        Valid(
            "B06", "WINDOWS",
            "select Name, Sum(Population) over (order by Population rows between unbounded preceding and current row) from #A.Entities()"),

        Invalid(
            "C01", "SETS",
            "select Id from #A.Entities() union select Id from #B.Entities()",
            "select Id, Name from #A.Entities() union select Id from #B.Entities()",
            DiagnosticCode.MQ3019_SetOperatorColumnCount, "columns"),
        Invalid(
            "C02", "SETS",
            "select Id from #A.Entities() union select Id from #B.Entities()",
            "select Id from #A.Entities() union select Name from #B.Entities()",
            DiagnosticCode.MQ3020_SetOperatorColumnTypes, "types"),
        Invalid(
            "C03", "SETS",
            "select Name from #A.Entities() except select City from #B.Entities()",
            "select Name, City from #A.Entities() except select City from #B.Entities()",
            DiagnosticCode.MQ3019_SetOperatorColumnCount, "columns"),
        Invalid(
            "C04", "SETS",
            "select Name from #A.Entities() intersect select City from #B.Entities()",
            "select Name from #A.Entities() intersect select Id from #B.Entities()",
            DiagnosticCode.MQ3020_SetOperatorColumnTypes, "types"),
        Valid(
            "C05", "SETS",
            "select Name from #A.Entities() union all select City as Name from #B.Entities() order by Name"),
        Valid(
            "C06", "SETS",
            "with merged as (select Id from #A.Entities() union all select Id from #B.Entities()) select Id from merged order by Id"),

        Invalid(
            "D01", "RECURSIVE_SHAPE",
            RecursiveSeed,
            "with counter (Value) as (" + RecursiveAnchor + " union all " + RecursiveMember + ") select Value from counter",
            DiagnosticCode.MQ3072_RecursiveCteRequiresKeyword, "WITH RECURSIVE"),
        Invalid(
            "D02", "RECURSIVE_SHAPE",
            RecursiveSeed,
            "with recursive counter (Value) as (select c.Value from counter c) select Value from counter",
            DiagnosticCode.MQ3073_InvalidRecursiveCteShape, "top-level UNION"),
        Invalid(
            "D03", "RECURSIVE_SHAPE",
            RecursiveSeed,
            "with recursive counter (Value) as (select c.Value from counter c union all " + RecursiveMember + ") select Value from counter",
            DiagnosticCode.MQ3074_InvalidRecursiveCteReference, "anchor"),
        Invalid(
            "D04", "RECURSIVE_SHAPE",
            RecursiveSeed,
            "with recursive counter (Value) as (" + RecursiveAnchor + " union all select distinct c.Value + 1 from counter c where c.Value < 3) select Value from counter",
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "DISTINCT"),
        Invalid(
            "D05", "RECURSIVE_SHAPE",
            RecursiveSeed,
            "with recursive counter (Value) as (" + RecursiveAnchor + " union all select c.Value + 1, c.Value from counter c where c.Value < 3) select Value from counter",
            DiagnosticCode.MQ3076_RecursiveCteOutputMismatch, "projects 2"),
        Valid("D06", "RECURSIVE_SHAPE", RecursiveSeed),

        Invalid(
            "E01", "RECURSIVE_RESTRICTIONS",
            RecursiveSeed,
            "with recursive counter (Value) as (" + RecursiveAnchor + " union all select c.Value + 1 from counter c group by c.Value) select Value from counter",
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "GROUP BY"),
        Invalid(
            "E02", "RECURSIVE_RESTRICTIONS",
            RecursiveSeed,
            "with recursive counter (Value) as (" + RecursiveAnchor + " union all select RowNumber() over ranked from counter c window ranked as (order by c.Value)) select Value from counter",
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "WINDOW"),
        Invalid(
            "E03", "RECURSIVE_RESTRICTIONS",
            RecursiveSeed,
            "with recursive counter (Value) as (" + RecursiveAnchor + " union all select c.Value + 1 from counter c qualify RowNumber() over (order by c.Value) = 1) select Value from counter",
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "QUALIFY"),
        Invalid(
            "E04", "RECURSIVE_RESTRICTIONS",
            RecursiveSeed,
            "with recursive counter (Value) as (" + RecursiveAnchor + " union all select a.Value + b.Value from counter a inner join counter b on a.Value = b.Value) select Value from counter",
            DiagnosticCode.MQ3074_InvalidRecursiveCteReference, "exactly once"),
        Invalid(
            "E05", "RECURSIVE_RESTRICTIONS",
            RecursiveSeed,
            "with recursive counter (Value) as (" + RecursiveAnchor + " union all select (c.Value + 1)::Decimal from counter c where c.Value < 3) select Value from counter",
            DiagnosticCode.MQ3076_RecursiveCteOutputMismatch, "Decimal"),
        Valid(
            "E06", "RECURSIVE_RESTRICTIONS",
            "with recursive counter (Value) as (" + RecursiveAnchor + " union all " + RecursiveMember + ") select Count(Value) from counter"),

        Invalid(
            "F01", "QUALIFY_AGGREGATION",
            "select Name from #A.Entities()",
            "select Name from #A.Entities() qualify Name = 'Alice'",
            DiagnosticCode.MQ3050_QualifyRequiresWindowFunction, "QUALIFY"),
        Invalid(
            "F02", "QUALIFY_AGGREGATION",
            "select City, Count(*) from #A.Entities() group by City",
            "select City, Count(*) from #A.Entities() group by City having RowNumber() over (order by City) = 1",
            DiagnosticCode.MQ3101_WindowFunctionInFilter, "RowNumber"),
        Invalid(
            "F03", "QUALIFY_AGGREGATION",
            "select Name from #A.Entities()",
            "select Name from #A.Entities() where Count(*) > 0",
            DiagnosticCode.MQ3011_AggregateNotAllowed, "Count"),
        Invalid(
            "F04", "QUALIFY_AGGREGATION",
            "select Length(Name) from #A.Entities()",
            "select Length(Name) filter (where Country = 'PL') from #A.Entities()",
            DiagnosticCode.MQ3051_FilterOnNonAggregate, "FILTER"),
        Valid(
            "F05", "QUALIFY_AGGREGATION",
            "select City, Count(*) as Rows from #A.Entities() group by City having Count(*) > 0"),
        Valid(
            "F06", "QUALIFY_AGGREGATION",
            "select Name, RowNumber() over (order by Name) as rn from #A.Entities() qualify rn <= 3"),

        Invalid(
            "G01", "NORMALIZATION",
            "with merged as (select Id from #A.Entities() union all select Id from #B.Entities()) select Id from merged",
            "with merged as (select Id, City from #A.Entities() union select Id from #B.Entities()) select Id from merged",
            DiagnosticCode.MQ3019_SetOperatorColumnCount, "columns"),
        Invalid(
            "G02", "NORMALIZATION",
            "with merged as (select Id from #A.Entities() union all select Id from #B.Entities()) select Id from merged",
            "with merged as (select Id from #A.Entities() union select Name from #B.Entities()) select Id from merged",
            DiagnosticCode.MQ3020_SetOperatorColumnTypes, "types"),
        Invalid(
            "G03", "NORMALIZATION",
            "with grouped as (select City, Count(*) as Rows from #A.Entities() group by City) select City from grouped",
            "with grouped as (select City, Name, Count(*) as Rows from #A.Entities() group by City) select City from grouped",
            DiagnosticCode.MQ3012_NonAggregateInSelect, "Name"),
        Invalid(
            "G04", "NORMALIZATION",
            "select Name from #A.Entities()",
            "select q.Name from (select Name, Sum(Population) over (order by Name rows between unbounded following and current row) as Total from #A.Entities()) q",
            DiagnosticCode.MQ3053_InvalidWindowFrameBounds, "UNBOUNDED FOLLOWING"),
        Invalid(
            "G05", "NORMALIZATION",
            RecursiveSeed,
            "with recursive counter (Value) as (" + RecursiveAnchor + " union all select c.Value + 1, c.Value from counter c where c.Value < 3) select Count(Value) from counter",
            DiagnosticCode.MQ3076_RecursiveCteOutputMismatch, "projects 2"),
        Valid(
            "G06", "NORMALIZATION",
            "with grouped as (select City, Count(*) as Rows from #A.Entities() group by City) select City, Rows, RowNumber() over (order by City) as Position from grouped"),

        Valid(
            "H01", "RESULT_CONTEXT",
            "select City, Count(*) as Rows from #A.Entities() group by City order by Rows desc"),
        Valid(
            "H02", "RESULT_CONTEXT",
            "select City, Count(*) as Rows, RowNumber() over (order by City) as Position from #A.Entities() group by City order by City"),
        Valid(
            "H03", "RESULT_CONTEXT",
            "select Name, RowNumber() over (order by Name) as rn from #A.Entities() qualify rn <= 3 order by Name skip 1 take 1"),
        Valid(
            "H04", "RESULT_CONTEXT",
            "select City, Count(*) as Rows from #A.Entities() group by City having Count(*) > 0 order by Rows"),
        Valid(
            "H05", "RESULT_CONTEXT",
            "select Name from #A.Entities() union select City as Name from #B.Entities() order by Name"),
        Valid(
            "H06", "RESULT_CONTEXT",
            "select Sum(Population) as Total from #A.Entities() order by Total"),
    ];

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = []
            })).Analyze(query);
    }

    private static GenericSchemaProvider CreateAmbiguousAggregateSchemaProvider()
    {
        var sourceA = new[] { new BasicEntity("Warsaw", "Poland", 100) };
        var sourceB = new[] { new BasicEntity("Warsaw", "Poland", 200) };

        return new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            ["#A"] = CreateSchema<AggregateOwnerAmbiguityTests.AggregateLibraryA>(sourceA),
            ["#B"] = CreateSchema<AggregateOwnerAmbiguityTests.AggregateLibraryB>(sourceB)
        });
    }

    private static GenericSchema<TLibrary> CreateSchema<TLibrary>(BasicEntity[] source)
        where TLibrary : Musoq.Plugins.LibraryBase, new()
    {
        return new GenericSchema<TLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["entities"] = (new BasicEntityTable(), new MultiRowSource<BasicEntity>(source))
        });
    }

    private static SemanticCandidate Invalid(
        string id,
        string family,
        string seedQuery,
        string query,
        DiagnosticCode code,
        string messageToken)
    {
        return new SemanticCandidate(id, family, seedQuery, query, code, messageToken);
    }

    private static SemanticCandidate Valid(string id, string family, string query)
    {
        return new SemanticCandidate(id, family, query, query, null, string.Empty);
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return FormatDiagnostics(result.Errors);
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(
            " | ",
            diagnostics.Select(static diagnostic =>
                $"{diagnostic.Code}: {diagnostic.Message} at {diagnostic.Span}"));
    }

    private sealed record SemanticCandidate(
        string Id,
        string Family,
        string SeedQuery,
        string Query,
        DiagnosticCode? ExpectedCode,
        string MessageToken);
}
