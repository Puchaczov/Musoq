using System;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class AutomaticDateTimeComparisonTests
{
    [TestMethod]
    public void WhenComparingDateTimeColumnWithStringLiteral_ShouldAutomaticallyConvert()
    {
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate > '2023-01-01'";

        dynamic first = new ExpandoObject();
        first.EventDate = new DateTime(2023, 6, 15);
        first.Name = "Event 1";

        dynamic second = new ExpandoObject();
        second.EventDate = new DateTime(2022, 12, 31);
        second.Name = "Event 2";

        var vm = CreateAndRunVirtualMachine(query,
        [
            first, second
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Event 1", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingWithVariousDateFormats_ShouldAutomaticallyConvert()
    {
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate <= '2023-12-31'";

        dynamic first = new ExpandoObject();
        first.EventDate = new DateTime(2023, 12, 15);
        first.Name = "Event 1";

        dynamic second = new ExpandoObject();
        second.EventDate = new DateTime(2024, 1, 5);
        second.Name = "Event 2";

        var vm = CreateAndRunVirtualMachine(query,
        [
            first, second
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Event 1", table[0].Values[0]);
    }



    [TestMethod]
    public void WhenUsingDateTimeComparisonInCaseWhen_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name, " +
                             "case when EventDate > '2023-03-15' then 'Future' " +
                             "     when EventDate = '2023-03-15' then 'Present' " +
                             "     else 'Past' end as TimeCategory " +
                             "from Events()";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var results = table.Select(row => new { Name = row.Values[0] as string, Category = row.Values[1] as string })
            .OrderBy(x => x.Name).ToList();

        Assert.AreEqual("Earlier Event", results[0].Name);
        Assert.AreEqual("Past", results[0].Category);

        Assert.AreEqual("Equal Event", results[1].Name);
        Assert.AreEqual("Present", results[1].Category);

        Assert.AreEqual("Later Event", results[2].Name);
        Assert.AreEqual("Future", results[2].Category);
    }

    [TestMethod]
    public void WhenUsingDateTimeOffsetComparisonInCaseWhen_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[2];
        const string query = "table Events {" +
                             "  EventDate: datetimeoffset," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name, " +
                             "case when EventDate >= '2023-03-15T12:00:00+00:00' then 'Recent' " +
                             "     else 'Old' end as Category " +
                             "from Events()";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var recentCount = table.Count(row => row.Values[1] as string == "Recent");
        var oldCount = table.Count(row => row.Values[1] as string == "Old");

        Assert.AreEqual(2, recentCount);
        Assert.AreEqual(1, oldCount);
    }

    [TestMethod]
    public void WhenUsingTimeSpanComparisonInCaseWhen_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[4];
        const string query = "table Events {" +
                             "  Duration: timespan," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name, " +
                             "case when Duration < '02:00:00' then 'Short' " +
                             "     when Duration = '02:00:00' then 'Medium' " +
                             "     else 'Long' end as DurationCategory " +
                             "from Events()";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, "Duration"));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var results = table.Select(row => new { Name = row.Values[0] as string, Category = row.Values[1] as string })
            .OrderBy(x => x.Name).ToList();

        Assert.AreEqual("Earlier Event", results[0].Name);
        Assert.AreEqual("Short", results[0].Category);

        Assert.AreEqual("Equal Event", results[1].Name);
        Assert.AreEqual("Medium", results[1].Category);

        Assert.AreEqual("Later Event", results[2].Name);
        Assert.AreEqual("Long", results[2].Category);
    }

    [TestMethod]
    public void WhenUsingNullableDateTimeComparisonInCaseWhen_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[1];
        const string query = "table Events {" +
                             "  EventDate: datetime?," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name, " +
                             "case when EventDate <= '2023-03-15' then 'Early' " +
                             "     else 'Late' end as Timing " +
                             "from Events()";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var earlyCount = table.Count(row => row.Values[1] as string == "Early");
        var lateCount = table.Count(row => row.Values[1] as string == "Late");

        Assert.AreEqual(2, earlyCount);
        Assert.AreEqual(1, lateCount);
    }

    [TestMethod]
    public void WhenUsingAllOperatorsInCaseWhen_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };

        foreach (var op in operators)
        {
            var query = $"table Events {{ EventDate: {testData.TypeName}, Name: string }};" +
                        $"couple #test.whatever with table Events as Events; " +
                        $"select Name, " +
                        $"case when EventDate {op} '{testData.StringEqual}' then 'Match' " +
                        $"     else 'NoMatch' end as Result " +
                        $"from Events()";

            try
            {
                var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
                var table = vm.Run(TestContext.CancellationToken);

                Assert.IsNotNull(table, $"Table should not be null for operator {op}");
                Assert.AreEqual(3, table.Count, $"Should return 3 rows for operator {op}");

                foreach (var row in table)
                {
                    var result = row.Values[1] as string;
                    Assert.IsTrue(result == "Match" || result == "NoMatch",
                        $"Result should be either 'Match' or 'NoMatch' for operator {op}, got: {result}");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed for operator {op} in CASE WHEN: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void WhenUsingReversedComparisonInCaseWhen_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name, " +
                             "case when '2023-03-15' < EventDate then 'After' " +
                             "     when '2023-03-15' = EventDate then 'Same' " +
                             "     else 'Before' end as Position " +
                             "from Events()";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var results = table.Select(row => new { Name = row.Values[0] as string, Position = row.Values[1] as string })
            .OrderBy(x => x.Name).ToList();

        Assert.AreEqual("Earlier Event", results[0].Name);
        Assert.AreEqual("Before", results[0].Position);

        Assert.AreEqual("Equal Event", results[1].Name);
        Assert.AreEqual("Same", results[1].Position);

        Assert.AreEqual("Later Event", results[2].Name);
        Assert.AreEqual("After", results[2].Position);
    }

    [TestMethod]
    public void WhenUsingAllDateTimeTypesInCaseWhen_ShouldAutomaticallyConvert()
    {
        foreach (var dateTimeType in DateTimeTypes)
        {
            var fieldName = dateTimeType.TypeName.Contains("timespan") ? "Duration" : "EventDate";
            var query = $"table Events {{ {fieldName}: {dateTimeType.TypeName}, Name: string }};" +
                        $"couple #test.whatever with table Events as Events; " +
                        $"select Name, " +
                        $"case when {fieldName} > '{dateTimeType.StringEarlier}' then 'NotEarliest' " +
                        $"     else 'Earliest' end as Category " +
                        $"from Events()";

            try
            {
                var vm = CreateAndRunVirtualMachine(query, CreateTestData(dateTimeType, fieldName));
                var table = vm.Run(TestContext.CancellationToken);

                Assert.IsNotNull(table, $"Table should not be null for type {dateTimeType.TypeName}");
                Assert.AreEqual(3, table.Count, $"Should return 3 rows for type {dateTimeType.TypeName}");

                foreach (var row in table)
                {
                    var category = row.Values[1] as string;
                    Assert.IsTrue(category == "NotEarliest" || category == "Earliest",
                        $"Category should be either 'NotEarliest' or 'Earliest' for type {dateTimeType.TypeName}, got: {category}");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed for datetime type {dateTimeType.TypeName} in CASE WHEN: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void WhenUsingNestedCaseWhenWithDateTimeComparison_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name, " +
                             "case when EventDate > '2023-03-15' then " +
                             "    case when EventDate > '2023-05-01' then 'VeryFuture' else 'NearFuture' end " +
                             "else 'PastOrPresent' end as DetailedCategory " +
                             "from Events()";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var results = table.Select(row => new { Name = row.Values[0] as string, Category = row.Values[1] as string })
            .OrderBy(x => x.Name).ToList();

        Assert.AreEqual("Earlier Event", results[0].Name);
        Assert.AreEqual("PastOrPresent", results[0].Category);

        Assert.AreEqual("Equal Event", results[1].Name);
        Assert.AreEqual("PastOrPresent", results[1].Category);

        Assert.AreEqual("Later Event", results[2].Name);
        Assert.AreEqual("VeryFuture", results[2].Category);
    }



}
