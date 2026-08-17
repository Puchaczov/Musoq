using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class StarModifierTests
{
    [TestMethod]
    public void WhenStarDuplicateReplace_ShouldThrowWithOrderHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace (1 as Name) replace (2 as City) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2041_InvalidStarModifierOrder,
            "Duplicate or out-of-order star modifier");
    }

    [TestMethod]
    public void WhenStarDuplicateLike_ShouldThrowWithOrderHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * like '%a' like '%b' from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2041_InvalidStarModifierOrder,
            "Duplicate or out-of-order star modifier");
    }

    [TestMethod]
    public void WhenStarReplaceBeforeLike_ShouldThrowWrongOrder()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace (1 as Name) like '%a' from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2041_InvalidStarModifierOrder,
            "Duplicate or out-of-order star modifier");
    }

    [TestMethod]
    public void WhenStarReplaceBeforeExclude_ShouldThrowWrongOrder()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace (1 as Name) exclude (City) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2041_InvalidStarModifierOrder,
            "Duplicate or out-of-order star modifier");
    }

    [TestMethod]
    public void WhenStarExcludeTypoExclud_ShouldSuggestCorrection()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * exclud (Name) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "Did you mean EXCLUDE or REPLACE");
    }

    [TestMethod]
    public void WhenStarExcludeTypoExlude_ShouldSuggestCorrection()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * exlude (Name) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "Did you mean EXCLUDE or REPLACE");
    }

    [TestMethod]
    public void WhenStarReplaceTypoReplac_ShouldSuggestCorrection()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replac (1 as Name) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "Did you mean EXCLUDE or REPLACE");
    }

    [TestMethod]
    public void WhenStarReplaceTypoRplace_ShouldSuggestCorrection()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * rplace (1 as Name) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "Did you mean EXCLUDE or REPLACE");
    }

    [TestMethod]
    public void WhenStarReplaceTrailingComma_ShouldThrow()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace (1 as Name,) from #A.entities()");

        AssertHasError(result, "Trailing comma in REPLACE list");
    }

    [TestMethod]
    public void WhenAliasedStarExcludeMissingParentheses_ShouldThrow()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select a.* exclude Name from #A.entities() a");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "EXCLUDE requires a parenthesized column list");
    }

    [TestMethod]
    public void WhenAliasedStarReplaceMissingAs_ShouldThrow()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select a.* replace (Name) from #A.entities() a");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "Expected AS keyword after expression in REPLACE item");
    }



    private static BasicSchemaProvider<BasicEntity> CreateSchemaProvider()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Warsaw", "Poland", 100) { Money = 1000.50m }] }
        };
        return new BasicSchemaProvider<BasicEntity>(sources);
    }

    private static QueryAnalyzer CreateAnalyzer()
    {
        return new QueryAnalyzer(CreateSchemaProvider());
    }

    private static void AssertHasError(QueryAnalysisResult result, string context)
    {
        Assert.IsNotEmpty(result.Errors,
            $"Expected an error diagnostic ({context}) but query succeeded.");
    }

    private static void AssertHasErrorWithMessage(
        QueryAnalysisResult result,
        DiagnosticCode expectedCode,
        string expectedMessageSubstring)
    {
        var match = DiagnosticContractTestAssertions.AssertSingleError(
            result, expectedCode, expectedMessageSubstring);

        StringAssert.Contains(
            match.Message,
            expectedMessageSubstring,
            $"Expected message containing '{expectedMessageSubstring}' but got: '{match.Message}'");
    }



    [TestMethod]
    public void WhenStarExclude_WithWhereClause_ShouldFilterThenExcludeColumns()
    {
        const string query = "select * exclude (City) from #A.entities() a where a.Population > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 50m },
                    new BasicEntity("february", 70m) { City = "Paris", Country = "FR", Population = 200m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
    }

    [TestMethod]
    public void WhenStarExclude_WithOrderBy_ShouldSortCorrectly()
    {
        const string query = "select * exclude (City) from #A.entities() a order by a.Population desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Population = 100m },
                    new BasicEntity("february", 70m) { City = "Paris", Population = 300m },
                    new BasicEntity("march", 90m) { City = "Berlin", Population = 200m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));

        var popIdx = columnNames.FindIndex(c => c.Contains("Population"));
        Assert.AreEqual(300m, table[0].Values[popIdx]);
        Assert.AreEqual(200m, table[1].Values[popIdx]);
        Assert.AreEqual(100m, table[2].Values[popIdx]);
    }

    [TestMethod]
    public void WhenStarExclude_WithGroupBy_ShouldWork()
    {
        const string query = @"
            select a.Country, Count(a.Country) as Cnt
            from #A.entities() a
            group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Country = "UK", City = "London" },
                    new BasicEntity("february", 70m) { Country = "UK", City = "Paris" },
                    new BasicEntity("march", 90m) { Country = "FR", City = "Berlin" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var uk = table.Single(r => (string)r.Values[0] == "UK");
        Assert.AreEqual(2, Convert.ToInt32(uk.Values[1]));
    }

    [TestMethod]
    public void WhenStarReplace_WithGroupBy_ShouldUseReplacedValue()
    {
        const string query = @"
            select a.Country, Sum(a.Population) as TotalPop
            from #A.entities() a
            group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Country = "UK", Population = 100m },
                    new BasicEntity("february", 70m) { Country = "UK", Population = 200m },
                    new BasicEntity("march", 90m) { Country = "FR", Population = 300m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var uk = table.Single(r => (string)r.Values[0] == "UK");
        Assert.AreEqual(300m, Convert.ToDecimal(uk.Values[1]));
    }

    [TestMethod]
    public void WhenStarExclude_WithInnerJoin_ShouldExcludeFromJoinedResult()
    {
        const string query = @"
            select a.* exclude (City) from #A.entities() a
            inner join #B.entities() b on a.Country = b.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 100m }
                ]
            },
            {
                "#B", [
                    new BasicEntity("february", 70m) { Country = "UK" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Country")));
    }

    [TestMethod]
    public void WhenStarExclude_WithLeftJoin_ShouldExcludeFromJoinedResult()
    {
        const string query = @"
            select a.* exclude (City) from #A.entities() a
            left outer join #B.entities() b on a.Country = b.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 100m },
                    new BasicEntity("february", 70m) { City = "Paris", Country = "FR", Population = 200m }
                ]
            },
            {
                "#B", [
                    new BasicEntity("march", 90m) { Country = "UK" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
    }

}
