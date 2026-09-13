using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LikeQueryConformanceTests : BasicEntityTestBase
{
    private static readonly string[] CultureNames = [string.Empty, "en-US", "pl-PL", "tr-TR"];

    public static IEnumerable<object[]> CatalogCases =>
        from testCase in LikeQueryConformanceCatalog.Cases
        from cultureName in CultureNames
        select new object[] { testCase.Id, cultureName };

    [TestMethod]
    [DynamicData(nameof(CatalogCases))]
    public void CatalogCase_ShouldMatchHardCodedSnapshot(string caseId, string cultureName)
    {
        var testCase = LikeQueryConformanceCatalog.Cases.Single(candidate => candidate.Id == caseId);
        var culture = string.IsNullOrEmpty(cultureName)
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(cultureName);

        RunUnderCulture(testCase, culture);
    }

    [TestMethod]
    public void Catalog_ShouldCoverEveryBehaviorDimensionIntroducedInR09()
    {
        var cases = LikeQueryConformanceCatalog.Cases;
        var missing = new List<string>();

        AddMissingValues(cases.Select(testCase => testCase.Shape), missing);
        AddMissingValues(
            cases.Select(testCase => testCase.Source)
                .Concat(LikeQueryConformanceCatalog.ContextCoverage
                    .Where(item => item.PatternSource.HasValue)
                    .Select(item => item.PatternSource!.Value)),
            missing);
        AddMissingValues(cases.Select(testCase => testCase.Domain), missing);
        AddMissingValues(cases.Select(testCase => testCase.Polarity), missing);
        AddMissingValues(cases.Select(testCase => testCase.NullCase), missing);
        AddMissingValues(cases.Select(testCase => testCase.ExpectedStrategy), missing);

        Assert.IsEmpty(missing, $"Missing LIKE conformance dimensions: {string.Join(", ", missing)}");
    }

    private void RunUnderCulture(LikeQueryConformanceCase testCase, CultureInfo culture)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var sources = new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = testCase.Rows };
            var query = CreateAndRunVirtualMachine(testCase.Query, sources);
            if (testCase.Parameters is not null)
            {
                foreach (var parameter in testCase.Parameters)
                    query.Parameters[parameter.Key] = parameter.Value;
            }

            AssertSnapshot(testCase, culture, query.Run(TestContext.CancellationToken));
            if (testCase.CacheSensitive)
                AssertSnapshot(testCase, culture, query.Run(TestContext.CancellationToken));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static void AssertSnapshot(LikeQueryConformanceCase testCase, CultureInfo culture, Table table)
    {
        var expected = FormatSnapshot(testCase.ExpectedColumns, testCase.GetExpectedRows(culture));
        var actualColumns = table.Columns.Select(column => (column.ColumnName, column.ColumnType)).ToArray();
        var actualRows = table.Select(row => row.Values).ToArray();
        var actual = FormatSnapshot(actualColumns, actualRows);

        Assert.AreEqual(expected, actual, $"LIKE conformance case '{testCase.Id}' failed under '{culture.Name}'.");
    }

    private static string FormatSnapshot(IReadOnlyList<(string Name, Type Type)> columns, IReadOnlyList<object?[]> rows)
    {
        var schema = string.Join(",", columns.Select(column => $"{column.Name}:{column.Type.FullName}"));
        var values = string.Join("\n", rows.Select(row => string.Join("|", row.Select(FormatValue))));
        return string.Concat(schema, "\n", values);
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "<null>",
            string text => string.Concat("string:", text.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)),
            _ => string.Concat(value.GetType().FullName, ":", Convert.ToString(value, CultureInfo.InvariantCulture))
        };
    }

    private static void AddMissingValues<T>(IEnumerable<T> values, ICollection<string> missing, params T[] deferredValues)
        where T : struct, Enum
    {
        var present = values.ToHashSet();
        var deferred = deferredValues.ToHashSet();
        foreach (var expected in Enum.GetValues<T>())
        {
            if (!present.Contains(expected) && !deferred.Contains(expected))
                missing.Add($"{typeof(T).Name}.{expected}");
        }
    }
}
