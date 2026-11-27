using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed partial class Core022GroupByAggregationTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("", "null::int")]
    [DataRow("param(n: int = 2) ", "$n")]
    [DataRow("let n: int = 2; ", "$n")]
    [DataRow("", "'all',null::int")]
    public void ConstantGroupingKeys_OnEmptyInput_ShouldReturnTheSingleAggregate(string script, string keys)
    {
        var sql = script + $"select Count(1) as N from values {{(K:1),(K:1)}} v where v.K=0 group by {keys}";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [0L]);
    }

    [TestMethod]
    public void GroupedMembershipRetainsBothCollectionParameters()
    {
        const string sql =
            "param(x: int[] = array { 1 },y: int[] = array { 2 }) "
            + "select s.K in $x as L,s.K in $y as R from values { (K:1) } s group by all";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [true, false]);
    }

    [TestMethod]
    [DataRow("(s.A or s.B) is null", "s.A or (s.B is null)")]
    [DataRow("(s.A or s.B) is distinct from true", "s.A or (s.B is distinct from true)")]
    [DataRow("(s.A or s.B) in (false)", "s.A or (s.B in (false))")]
    public void GroupingPreservesTheOperandOfAPostfixPredicate(string left, string right)
    {
        var sql = $"select {left} as L,{right} as R from values {{ (A:true,B:false) }} s group by all";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [false, true]);
    }

    [TestMethod]
    public void GroupedCallsPreserveNumericArgumentTypes()
    {
        const string sql = "select GetTypeName(2i) as L,GetTypeName(2d) as R from values { (K:3) } s group by all";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, ["System.Int32", "System.Decimal"]);
    }

    [TestMethod]
    public void GroupedDivisionKeepsBothNumericLiteralTypes()
    {
        const string sql = "select s.K/2i as L,s.K/2d as R from values { (K:3) } s group by all";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("L", typeof(int)), ("R", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, 1.5m]);
    }

    [TestMethod]
    [DataRow("all")]
    [DataRow("s.A*(s.B+s.C),s.A*s.B+s.C")]
    public void GroupedArithmeticPreservesOperatorNesting(string keys)
    {
        var sql = $"select s.A*(s.B+s.C) as P,s.A*s.B+s.C as Q from values {{ (A:8,B:4,C:2) }} s group by {keys}";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [48, 34]);
    }

    [TestMethod]
    public void GroupedLogicalOperatorsPreserveOperatorNesting()
    {
        const string sql =
            "select (s.A=8 or s.B=2) and s.C=3 as P,s.A=8 or (s.B=2 and s.C=3) as Q"
            + " from values { (A:8,B:4,C:2) } s group by all";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [false, true]);
    }

    [TestMethod]
    public void GroupingDistinguishesAQuotedListSeparatorFromTwoMembers()
    {
        const string sql =
            "select s.S in (r'a'', ''b') as A,s.S in ('a','b') as B from values { (S:'a') } s group by all";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [false, true]);
    }

    [TestMethod]
    public void CompositeGroupKeyUsesItsBoundMembershipSet()
    {
        const string sql =
            "select s.G,s.S in ('a','c') as P,Count(*) as N"
            + " from values { (G:1,S:'a'),(G:1,S:'b'),(G:2,S:null) } s group by s.G,s.S in ('a','c')";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, true, 1L], [1, false, 1L], [2, null, 1L]);
    }

    [TestMethod]
    [DataRow("x.S='a b',x.S='ab'")]
    [DataRow("all")]
    public void GroupedComparisons_ReadKeysWithTheirOriginalLiteralWhitespace(string keys)
    {
        var sql = $"select x.S='a b' as L,x.S='ab' as U from values {{ (S:'a b') }} x group by {keys}";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [true, false]);
    }

    [TestMethod]
    public void GroupedColumnComparison_WithDifferentLiteral_IsComputedFromTheGroupedColumn()
    {
        const string sql = "select x.S='ab' as U from values { (S:'a b') } x group by x.S,x.S='a b'";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [false]);
    }

    [TestMethod]
    public void AggregateFamilies_GroupedRows_ShouldReturnTypedValues()
    {
        const string query = (
            "select\n"
            + "    Country,\n"
            + "    Count(Name) as NamedRows,\n"
            + "    Count(*) as Rows,\n"
            + "    Sum(Population) as Total,\n"
            + "    Avg(Population) as Average,\n"
            + "    Min(Population) as Minimum,\n"
            + "    Max(Population) as Maximum,\n"
            + "    AggregateValues(Name, '|') as Names\n"
            + "from #A.Entities()\n"
            + "group by Country\n"
            + "order by Country"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity
                    {
                        Name = "Alice",
                        Country = "DE",
                        Population = 5m,
                    },
                    new BasicEntity
                    {
                        Name = "Bob",
                        Country = "DE",
                        Population = 15m,
                    },
                    new BasicEntity
                    {
                        Name = "Carla",
                        Country = "PL",
                        Population = 20m,
                    }
                )
            )
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("NamedRows", typeof(long)),
            ("Rows", typeof(long)),
            ("Total", typeof(decimal?)),
            ("Average", typeof(decimal?)),
            ("Minimum", typeof(decimal?)),
            ("Maximum", typeof(decimal?)),
            ("Names", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", 2L, 2L, 20m, 10m, 5m, 15m, "Alice|Bob"],
            ["PL", 1L, 1L, 20m, 20m, 20m, 20m, "Carla"]
        );
    }

    [TestMethod]
    public void ParentLevelAggregates_GroupedByMonthAndCity_ShouldRepeatMonthTotals()
    {
        const string query = (
            "select\n"
            + "    Month,\n"
            + "    City,\n"
            + "    Count(City) as CityRows,\n"
            + "    Count(City, 1) as MonthRows,\n"
            + "    Sum(Money) as CityTotal,\n"
            + "    Sum(Money, 1) as MonthTotal,\n"
            + "    Avg(Money, 1) as MonthAverage\n"
            + "from #A.Entities()\n"
            + "group by Month, City\n"
            + "order by Month, City"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity
                    {
                        Month = "Feb",
                        City = "Warsaw",
                        Money = 40m,
                    },
                    new BasicEntity
                    {
                        Month = "Jan",
                        City = "Krakow",
                        Money = 30m,
                    },
                    new BasicEntity
                    {
                        Month = "Jan",
                        City = "Warsaw",
                        Money = 10m,
                    },
                    new BasicEntity
                    {
                        Month = "Jan",
                        City = "Warsaw",
                        Money = 20m,
                    }
                )
            )
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Month", typeof(string)),
            ("City", typeof(string)),
            ("CityRows", typeof(long)),
            ("MonthRows", typeof(long)),
            ("CityTotal", typeof(decimal?)),
            ("MonthTotal", typeof(decimal?)),
            ("MonthAverage", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Feb", "Warsaw", 1L, 1L, 40m, 40m, 40m],
            ["Jan", "Krakow", 1L, 3L, 30m, 60m, 20m],
            ["Jan", "Warsaw", 2L, 3L, 30m, 60m, 20m]
        );
    }

    [TestMethod]
    public void AggregateWithoutGroupBy_ShouldProduceOneGlobalRow()
    {
        const string query = (
            "select\n"
            + "    Count(*) as Rows,\n"
            + "    Count(Name) as NamedRows,\n"
            + "    Sum(Population) as Total,\n"
            + "    Avg(Population) as Average\n"
            + "from #A.Entities()"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity { Name = "A", Population = 10m },
                    new BasicEntity { Name = null, Population = 20m },
                    new BasicEntity { Name = "C", Population = 30m }
                )
            )
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Rows", typeof(long)),
            ("NamedRows", typeof(long)),
            ("Total", typeof(decimal?)),
            ("Average", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3L, 2L, 60m, 20m]);
    }

    [TestMethod]
    public void NullGrouping_MultiColumnKeys_ShouldKeepNullTuplesDistinct()
    {
        const string query = (
            "select Country, City, Count(City) as NamedCities, Count(*) as Rows\n"
            + "from #A.Entities()\n"
            + "group by Country, City"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity
                    {
                        Name = "A",
                        Country = null,
                        City = null,
                    },
                    new BasicEntity
                    {
                        Name = "B",
                        Country = null,
                        City = "Warsaw",
                    },
                    new BasicEntity
                    {
                        Name = "C",
                        Country = "PL",
                        City = null,
                    },
                    new BasicEntity
                    {
                        Name = "D",
                        Country = "PL",
                        City = null,
                    },
                    new BasicEntity
                    {
                        Name = "E",
                        Country = "PL",
                        City = "Warsaw",
                    }
                )
            )
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("NamedCities", typeof(long)),
            ("Rows", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [null, null, 0L, 1L],
            [null, "Warsaw", 1L, 1L],
            ["PL", null, 0L, 2L],
            ["PL", "Warsaw", 1L, 1L]
        );
    }

    [TestMethod]
    [DataRow("group by 'constant'")]
    [DataRow("group by '1'")]
    [DataRow("group by 1 + 0")]
    public void NonOrdinalConstants_ShouldProduceOneGroup(string groupByClause)
    {
        var query = $"select Count(*) as Rows, Sum(Population) as Total from #A.Entities() {groupByClause}";

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(new BasicEntity { Population = 10m }, new BasicEntity { Population = 20m })
            )
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Rows", typeof(long)), ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2L, 30m]);
    }

    [TestMethod]
    public void GroupByAll_WindowProjection_ShouldExcludeWindowFromGroupingKeys()
    {
        const string query = (
            "select City, Count(*) as Rows, RowNumber() over (order by City) as Position\n"
            + "from #A.Entities()\n"
            + "group by all\n"
            + "order by City"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity { City = "Berlin" },
                    new BasicEntity { City = "Berlin" },
                    new BasicEntity { City = "Paris" }
                )
            )
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Rows", typeof(long)),
            ("Position", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Berlin", 2L, 1L], ["Paris", 1L, 2L]);
    }

    [TestMethod]
    public void GroupByAll_RenameProjection_ShouldGroupFinalOutputExpressions()
    {
        const string query = (
            "select * like 'C%' rename (City as CityKey), Count(*) as Rows\n"
            + "from #A.Entities()\n"
            + "group by all\n"
            + "order by Country, City"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity { Country = "DE", City = "Berlin" },
                    new BasicEntity { Country = "DE", City = "Berlin" },
                    new BasicEntity { Country = "PL", City = "Warsaw" }
                )
            )
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("CityKey", typeof(string)),
            ("Country", typeof(string)),
            ("Rows", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Berlin", "DE", 2L], ["Warsaw", "PL", 1L]);
    }

    [TestMethod]
    public void GroupByAll_OnlyWindowExpression_ShouldUseOneConstantGroup()
    {
        const string query = (
            "select RowNumber() over (order by 1) as Position\n" + "from #A.Entities()\n" + "group by all"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(new BasicEntity { City = "Berlin" }, new BasicEntity { City = "Paris" })
            )
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1L]);
    }

    [TestMethod]
    public void NonAggregateRestriction_ShouldReportExactDiagnosticForMissingColumn()
    {
        const string query = "select Name, City, Count(1) from #A.Entities() group by City";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            "Column 'Name' must appear in the GROUP BY clause or be used in an aggregate function. Current GROUP BY columns: City.",
            "Name",
            "Every selected column must be either aggregated or included in the GROUP BY clause.",
            [
                "Add the column to the GROUP BY clause.",
                "Wrap the column in an aggregate function such as Count, Sum, Min, or Max.",
            ]
        );
    }

    [TestMethod]
    public void AggregateInGroupBy_ShouldReportExactDiagnostic()
    {
        const string query = "select Count(*) from #A.Entities() group by Sum(Population)";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3092_AggregateInGroupBy,
            "GROUP BY expressions cannot contain aggregate functions or aggregate SELECT aliases.",
            "Sum",
            "An aggregate expression is not valid inside GROUP BY.",
            ["Group by the input expression instead.", "Move the aggregate expression to SELECT or HAVING."]
        );
    }

    [TestMethod]
    public void GroupByOrdinalOutOfRange_ShouldReportExactDiagnostic()
    {
        const string query = "select City, Count(*) from #A.Entities() group by 3";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3024_GroupByIndexOutOfRange,
            "GROUP BY position 3 is out of range. SELECT projection contains 2 field(s).",
            "3",
            "A positional GROUP BY reference points outside the SELECT projection list.",
            [
                "Use a GROUP BY index between 1 and the number of selected columns.",
                "Prefer grouping by the expression or alias directly for clarity.",
            ],
            "Core Spec - GROUP BY Clause"
        );
    }

    [TestMethod]
    public void QualifiedNonAggregateColumnFromAnotherAlias_ShouldNotSatisfyGroupingByName()
    {
        const string query = (
            "select a.City, b.City, Count(*) as Rows\n"
            + "from #A.Entities() a\n"
            + "inner join #B.Entities() b on a.Id = b.Id\n"
            + "group by a.City"
        );

        var result = Analyze(query);
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var diagnostic = result.Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ3012_NonAggregateInSelect, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(
            "Column 'City' must appear in the GROUP BY clause or be used in an aggregate function. Current GROUP BY columns: City.",
            diagnostic.Message
        );
        var cityStart = query.IndexOf("b.City", StringComparison.Ordinal) + "b.".Length;
        Assert.AreEqual(new TextSpan(cityStart, "City".Length), diagnostic.Span);
    }

    private static void AssertSemanticDiagnosticContract(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        string expectedExpression,
        string expectedExplanation,
        string[] expectedFixes,
        string expectedDocsReference = "Core Spec - GROUP BY and Aggregation"
    )
    {
        var result = Analyze(query);
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));

        var diagnostic = errors[0];
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

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedSpan.End, envelope.EndOffset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.AreEqual(expectedExplanation, envelope.Explanation);
        Assert.AreEqual(expectedDocsReference, envelope.DocsReference);
        CollectionAssert.AreEqual(expectedFixes, envelope.SuggestedFixes.ToArray());
        Assert.HasCount(expectedFixes.Length, envelope.Actions);
        CollectionAssert.AreEqual(expectedFixes, envelope.Actions.Select(static action => action.Title).ToArray());
        Assert.IsTrue(
            envelope.Actions.All(static action =>
                action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null
            )
        );
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [], ["#B"] = [] }
        );

        return new QueryAnalyzer(provider).Analyze(query);
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(
            " | ",
            result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")
        );
    }
}
