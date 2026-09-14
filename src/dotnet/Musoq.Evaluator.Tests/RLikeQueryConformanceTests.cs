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
public sealed class RLikeQueryConformanceTests : BasicEntityTestBase
{
    private static readonly string[] CultureNames = [string.Empty, "en-US", "pl-PL", "tr-TR"];

    public static IEnumerable<object[]> CatalogCases =>
        from testCase in RLikeQueryConformanceCatalog.Cases
        from cultureName in CultureNames
        select new object[] { testCase.Id, cultureName };

    [TestMethod]
    [DynamicData(nameof(CatalogCases))]
    public void CatalogCase_ShouldMatchHardCodedSnapshot(string caseId, string cultureName)
    {
        var testCase = RLikeQueryConformanceCatalog.Cases.Single(candidate => candidate.Id == caseId);
        var culture = string.IsNullOrEmpty(cultureName)
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(cultureName);

        RunUnderCulture(testCase, culture);
    }

    [TestMethod]
    public void Catalog_ShouldCoverEveryBehaviorDimension()
    {
        var cases = RLikeQueryConformanceCatalog.Cases;
        var missing = new List<string>();
        AddMissingValues(cases.Select(testCase => testCase.Family), missing);
        AddMissingValues(
            cases.Select(testCase => testCase.Source)
                .Concat(RLikeQueryConformanceCatalog.ContextCoverage
                    .Where(item => item.PatternSource.HasValue)
                    .Select(item => item.PatternSource!.Value)),
            missing);
        AddMissingValues(cases.Select(testCase => testCase.Domain), missing);
        AddMissingValues(cases.Select(testCase => testCase.Polarity), missing);
        AddMissingValues(cases.Select(testCase => testCase.NullCase), missing);
        AddMissingValues(cases.Select(testCase => testCase.ExpectedStrategy), missing);

        Assert.IsEmpty(missing, $"Missing RLIKE conformance dimensions: {string.Join(", ", missing)}");
    }

    private void RunUnderCulture(RLikeQueryConformanceCase testCase, CultureInfo culture)
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

    private static void AssertSnapshot(
        RLikeQueryConformanceCase testCase,
        CultureInfo culture,
        Table table)
    {
        var expected = FormatSnapshot(testCase.GetExpectedRows(culture));
        var actual = FormatSnapshot(table.Select(row => row.Values).ToArray());
        Assert.AreEqual(expected, actual, $"RLIKE case '{testCase.Id}' failed under '{culture.Name}'.");

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Name", typeof(string)),
            ("Matched", typeof(bool)));
    }

    private static string FormatSnapshot(IReadOnlyList<object?[]> rows) => string.Join(
        "\n",
        rows.Select(row => string.Join("|", row.Select(FormatValue))));

    private static string FormatValue(object? value) => value switch
    {
        null => "<null>",
        string text => string.Concat("string:", text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)),
        _ => string.Concat(value.GetType().FullName, ":", Convert.ToString(value, CultureInfo.InvariantCulture))
    };

    private static void AddMissingValues<T>(IEnumerable<T> values, ICollection<string> missing)
        where T : struct, Enum
    {
        var present = values.ToHashSet();
        foreach (var expected in Enum.GetValues<T>())
        {
            if (!present.Contains(expected))
                missing.Add($"{typeof(T).Name}.{expected}");
        }
    }
}
