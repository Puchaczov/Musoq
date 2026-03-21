using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class StarModifierTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenStarExcludeSingleColumn_ShouldRemoveColumn()
    {
        const string query = "select * exclude (City) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                    {
                        City = "London", Country = "UK", Population = 9000000m, Id = 1
                    }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(8, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Name")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Country")));
    }

    [TestMethod]
    public void WhenStarExcludeMultipleColumns_ShouldRemoveColumns()
    {
        const string query = "select * exclude (City, Country, Population) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 9000000m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(6, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Country")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Population")));
    }

    [TestMethod]
    public void WhenAliasedStarExclude_ShouldRemoveColumn()
    {
        const string query = "select a.* exclude (City) from #A.entities() a";

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
    public void WhenStarReplaceSingleColumn_ShouldSubstituteExpression()
    {
        const string query = "select * replace (Population * 2 as Population) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Population = 100m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        var populationIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Population")).i;
        Assert.AreEqual(200m, table[0].Values[populationIdx]);
    }

    [TestMethod]
    public void WhenStarReplaceMultipleColumns_ShouldSubstituteExpressions()
    {
        const string query =
            "select * replace (Population * 2 as Population, Money + 10 as Money) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Population = 100m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        var populationIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Population")).i;
        var moneyIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Money")).i;

        Assert.AreEqual(200m, table[0].Values[populationIdx]);
        Assert.AreEqual(60m, table[0].Values[moneyIdx]);
    }

    [TestMethod]
    public void WhenStarLikePattern_ShouldFilterColumns()
    {
        const string query = "select * like 'C%' from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsTrue(columnNames.All(c => c.Contains("City") || c.Contains("Country")));
    }

    [TestMethod]
    public void WhenStarNotLikePattern_ShouldExcludeMatchingColumns()
    {
        const string query = "select * not like 'C%' from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Country")));
    }

    [TestMethod]
    public void WhenStarLikeWithExclude_ShouldCompose()
    {
        const string query = "select * like '%o%' exclude (Money, Month) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 9000000m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("Money")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Month")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Country")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Population")));
    }

    [TestMethod]
    public void WhenStarExcludeWithReplace_ShouldCompose()
    {
        const string query =
            "select * exclude (City, Country) replace (Population * 2 as Population) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 100m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Country")));

        var populationIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Population")).i;
        Assert.AreEqual(200m, table[0].Values[populationIdx]);
    }

    [TestMethod]
    public void WhenStarLikeExcludeReplace_ShouldComposeAll()
    {
        const string query =
            "select * like '%o%' exclude (Country) replace (Population * 3 as Population) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Country = "UK", Population = 100m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("Country")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Population")));

        var populationIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Population")).i;
        Assert.AreEqual(300m, table[0].Values[populationIdx]);
    }

    [TestMethod]
    public void WhenStarExcludeNonExistentColumn_ShouldThrow()
    {
        const string query = "select * exclude (NonExistent) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3041_StarExcludeColumnNotFound, DiagnosticPhase.Bind, "NonExistent");
    }

    [TestMethod]
    public void WhenStarExcludeAllColumns_ShouldThrow()
    {
        const string query =
            "select * exclude (Name, City, Country, Population, Money, Month, Time, Id, NullableValue) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3043_StarExcludeRemovesAllColumns, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenStarReplaceNonExistentColumn_ShouldThrow()
    {
        const string query = "select * replace (1 as NonExistent) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3042_StarReplaceColumnNotFound, DiagnosticPhase.Bind, "NonExistent");
    }

    [TestMethod]
    public void WhenStarColumnInBothExcludeAndReplace_ShouldThrow()
    {
        const string query = "select * exclude (City) replace (1 as City) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3044_StarColumnInBothExcludeAndReplace, DiagnosticPhase.Bind, "City");
    }

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

    #region Parser Syntax Error Tests

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

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2030_UnsupportedSyntax,
            "Duplicate or out-of-order star modifier");
    }

    [TestMethod]
    public void WhenStarDuplicateReplace_ShouldThrowWithOrderHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace (1 as Name) replace (2 as City) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2030_UnsupportedSyntax,
            "Duplicate or out-of-order star modifier");
    }

    [TestMethod]
    public void WhenStarDuplicateLike_ShouldThrowWithOrderHint()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * like '%a' like '%b' from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2030_UnsupportedSyntax,
            "Duplicate or out-of-order star modifier");
    }

    [TestMethod]
    public void WhenStarReplaceBeforeLike_ShouldThrowWrongOrder()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace (1 as Name) like '%a' from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2030_UnsupportedSyntax,
            "Duplicate or out-of-order star modifier");
    }

    [TestMethod]
    public void WhenStarReplaceBeforeExclude_ShouldThrowWrongOrder()
    {
        var analyzer = CreateAnalyzer();
        var result = analyzer.ValidateSyntax("select * replace (1 as Name) exclude (City) from #A.entities()");

        AssertHasErrorWithMessage(result, DiagnosticCode.MQ2030_UnsupportedSyntax,
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

    #endregion

    #region Test Helpers

    private static ISchemaProvider CreateSchemaProvider()
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
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected an error ({context}) but query succeeded.");
    }

    private static void AssertHasErrorWithMessage(
        QueryAnalysisResult result,
        DiagnosticCode expectedCode,
        string expectedMessageSubstring)
    {
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected error code {expectedCode} but query succeeded.");

        var match = result.Errors.FirstOrDefault(e => e.Code == expectedCode);

        if (match == null)
        {
            var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Fail(
                $"Expected error code {expectedCode} with message containing '{expectedMessageSubstring}' but got:\n{errorDetails}");
        }

        StringAssert.Contains(
            match.Message,
            expectedMessageSubstring,
            $"Expected message containing '{expectedMessageSubstring}' but got: '{match.Message}'");
    }

    #endregion
}
