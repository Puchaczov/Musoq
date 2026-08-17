using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class StarModifierTests
{
    [TestMethod]
    public void WhenStarLikeMatchesNoColumns_ShouldThrow()
    {
        const string query = "select * like 'zzz%' from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3045_StarLikeMatchedNoColumns, DiagnosticPhase.Bind, "zzz%");
    }

    [TestMethod]
    public void WhenStarExcludeDuplicateColumn_ShouldThrow()
    {
        const string query = "select * exclude (City, City) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3046_StarExcludeDuplicateColumn, DiagnosticPhase.Bind, "City");
    }

    [TestMethod]
    public void WhenStarReplaceDuplicateColumn_ShouldThrow()
    {
        const string query = "select * replace (1 as City, 2 as City) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3047_StarReplaceDuplicateColumn, DiagnosticPhase.Bind, "City");
    }

    [TestMethod]
    public void WhenStarReplaceTargetsExcludedColumn_ShouldThrow()
    {
        const string query = "select * like 'N%' replace (1 as City) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3048_StarReplaceTargetsRemovedColumn, DiagnosticPhase.Bind, "City");
    }

    [TestMethod]
    public void WhenStarExcludeCaseInsensitive_ShouldWork()
    {
        const string query = "select * exclude (city) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(8, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
    }

    [TestMethod]
    public void WhenStarLikeUnderscoreWildcard_ShouldMatchSingleChar()
    {
        const string query = "select * like '_d' from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Id = 42 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table.Columns.Count());
        Assert.Contains("Id", table.Columns.First().ColumnName);
    }

    [TestMethod]
    public void WhenStarWithModifiers_AndOtherExplicitColumns_ShouldWork()
    {
        const string query = "select * exclude (City), 'extra' as Extra from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.AreEqual("Extra", columnNames.Last());
    }

    [TestMethod]
    public void WhenStarExcludePreservesColumnOrder_ShouldMaintainOrder()
    {
        const string query = "select * exclude (Country, Money) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                    {
                        City = "London", Country = "UK", Population = 100m, Id = 1
                    }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(7, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        var nameIdx = columnNames.FindIndex(c => c.Contains("Name"));
        var cityIdx = columnNames.FindIndex(c => c.Contains("City"));
        var populationIdx = columnNames.FindIndex(c => c.Contains("Population"));
        var monthIdx = columnNames.FindIndex(c => c.Contains("Month"));

        Assert.IsLessThan(cityIdx, nameIdx);
        Assert.IsLessThan(populationIdx, cityIdx);
        Assert.IsLessThan(monthIdx, populationIdx);
    }

    [TestMethod]
    public void WhenStarReplacePreservesColumnPosition_ShouldKeepOriginalPosition()
    {
        const string query = "select * replace (Population * 10 as Population) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Population = 5m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(9, table.Columns.Count());

        var populationIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Population")).i;
        Assert.AreEqual(3, populationIdx);
        Assert.AreEqual(50m, table[0].Values[populationIdx]);
    }

    [TestMethod]
    public void WhenStarNoModifiers_ShouldExpandNormally()
    {
        const string query = "select * from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());
    }

    // ============================================================================
    // Parser syntax error tests — mistyped star modifiers
    // ============================================================================


    [TestMethod]
    public void WhenStarExcludeMissingParentheses_ShouldThrowWithUsageHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * exclude Name from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "EXCLUDE requires a parenthesized column list");
    }

    [TestMethod]
    public void WhenStarExcludeEmptyList_ShouldThrowWithUsageHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * exclude () from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2003_InvalidExpression,
            "EXCLUDE list must contain at least one column name");
    }

    [TestMethod]
    public void WhenStarExcludeTrailingComma_ShouldThrow()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * exclude (Name,) from #A.entities()");

        AssertHasError(result, "Trailing comma in EXCLUDE list");
    }

    [TestMethod]
    public void WhenStarExcludeNumberAsColumnName_ShouldThrow()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * exclude (123) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "Expected a column name");
    }

    [TestMethod]
    public void WhenStarReplaceMissingParentheses_ShouldThrowWithUsageHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace 1 as Name from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "REPLACE requires a parenthesized list");
    }

    [TestMethod]
    public void WhenStarReplaceEmptyList_ShouldThrowWithUsageHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace () from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2003_InvalidExpression,
            "REPLACE list must contain at least one replacement");
    }

    [TestMethod]
    public void WhenStarReplaceMissingAs_ShouldThrowWithUsageHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace (Name) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "Expected AS keyword after expression in REPLACE item");
    }

    [TestMethod]
    public void WhenStarReplaceMissingColumnAfterAs_ShouldThrow()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace (123 as) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2001_UnexpectedToken,
            "Expected a column name");
    }

    [TestMethod]
    public void WhenStarLikeMissingPattern_ShouldThrowWithUsageHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * like from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2003_InvalidExpression,
            "Expected a string pattern after LIKE");
    }

    [TestMethod]
    public void WhenStarLikeNumericPattern_ShouldThrowWithUsageHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * like 123 from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2003_InvalidExpression,
            "Expected a string pattern after LIKE");
    }

    [TestMethod]
    public void WhenStarNotLikeMissingPattern_ShouldThrowWithUsageHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * not like from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2003_InvalidExpression,
            "Expected a string pattern after LIKE");
    }

    [TestMethod]
    public void WhenStarDuplicateExclude_ShouldThrowWithOrderHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * exclude (Name) exclude (City) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2041_InvalidStarModifierOrder,
            "Duplicate or out-of-order star modifier");
    }

}
