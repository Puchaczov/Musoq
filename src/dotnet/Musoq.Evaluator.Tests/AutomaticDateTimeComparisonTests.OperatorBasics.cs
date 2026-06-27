using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class AutomaticDateTimeComparisonTests
{
    [TestMethod]
    public void WhenComparingDateTimeWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[0];
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };

        foreach (var op in operators) TestBasicOperatorFunctionality(dateTimeType, op, "EventDate");
    }

    [TestMethod]
    public void WhenComparingDateTimeOffsetWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[2];
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };

        foreach (var op in operators) TestBasicOperatorFunctionality(dateTimeType, op, "EventDate");
    }

    [TestMethod]
    public void WhenComparingTimeSpanWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[4];
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };

        foreach (var op in operators) TestBasicOperatorFunctionality(dateTimeType, op, "Duration");
    }

    [TestMethod]
    public void WhenComparingNullableDateTimeWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[1];
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };

        foreach (var op in operators) TestBasicOperatorFunctionality(dateTimeType, op, "EventDate");
    }

    [TestMethod]
    public void WhenComparingNullableDateTimeOffsetWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[3];
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };

        foreach (var op in operators) TestBasicOperatorFunctionality(dateTimeType, op, "EventDate");
    }

    [TestMethod]
    public void WhenComparingNullableTimeSpanWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[5];
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };

        foreach (var op in operators) TestBasicOperatorFunctionality(dateTimeType, op, "Duration");
    }

    [TestMethod]
    public void WhenComparingReversedWithAllTypes_ShouldAutomaticallyConvert()
    {
        foreach (var dateTimeType in DateTimeTypes)
        {
            var fieldName = dateTimeType.TypeName.Contains("timespan") ? "Duration" : "EventDate";
            TestReversedComparison(dateTimeType, fieldName);
        }
    }



    [TestMethod]
    public void WhenComparingWithEqualityOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate = '2023-03-15'";

        var result = RunComparisonTest(query, testData);
        Assert.AreEqual("Equal Event", result);
    }

    [TestMethod]
    public void WhenComparingWithNotEqualOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate <> '2023-01-01'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.Contains("Equal Event", results);
        Assert.Contains("Later Event", results);
    }

    [TestMethod]
    public void WhenComparingWithGreaterThanOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate > '2023-01-01'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.Contains("Equal Event", results);
        Assert.Contains("Later Event", results);
    }

    [TestMethod]
    public void WhenComparingWithLessThanOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate < '2023-06-15'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.Contains("Earlier Event", results);
        Assert.Contains("Equal Event", results);
    }

    [TestMethod]
    public void WhenComparingWithGreaterOrEqualOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate >= '2023-03-15'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.Contains("Equal Event", results);
        Assert.Contains("Later Event", results);
    }

    [TestMethod]
    public void WhenComparingWithLessOrEqualOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        const string query = "table Events {" +
                             "  EventDate: datetime," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate <= '2023-03-15'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.Contains("Earlier Event", results);
        Assert.Contains("Equal Event", results);
    }



    [TestMethod]
    public void WhenComparingDateTimeOffsetWithSpecificOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[2];
        const string query = "table Events {" +
                             "  EventDate: datetimeoffset," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate = '2023-03-15T12:00:00+00:00'";

        var result = RunComparisonTest(query, testData);
        Assert.AreEqual("Equal Event", result);
    }

    [TestMethod]
    public void WhenComparingTimeSpanWithSpecificOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[4];
        const string query = "table Events {" +
                             "  Duration: timespan," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where Duration >= '02:00:00'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, "Duration"));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.Contains("Equal Event", results);
        Assert.Contains("Later Event", results);
    }



}
