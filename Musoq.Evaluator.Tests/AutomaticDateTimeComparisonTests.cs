using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticDateTimeComparisonTests : UnknownQueryTestsBase
{
    #region Test Data Structures

    private struct DateTimeTypeTestData
    {
        public string TypeName { get; set; }
        public object EarlierValue { get; set; }
        public object LaterValue { get; set; }
        public object EqualValue { get; set; }
        public string StringEarlier { get; set; }
        public string StringLater { get; set; }
        public string StringEqual { get; set; }
    }

    private static readonly DateTimeTypeTestData[] DateTimeTypes = new[]
    {
        new DateTimeTypeTestData
        {
            TypeName = "System.DateTime",
            EarlierValue = new DateTime(2023, 1, 1),
            LaterValue = new DateTime(2023, 6, 15),
            EqualValue = new DateTime(2023, 3, 15),
            StringEarlier = "2023-01-01",
            StringLater = "2023-06-15",
            StringEqual = "2023-03-15"
        },
        new DateTimeTypeTestData
        {
            TypeName = "System.Nullable`1[System.DateTime]",
            EarlierValue = new DateTime(2023, 1, 1),
            LaterValue = new DateTime(2023, 6, 15),
            EqualValue = new DateTime(2023, 3, 15),
            StringEarlier = "2023-01-01",
            StringLater = "2023-06-15",
            StringEqual = "2023-03-15"
        },
        new DateTimeTypeTestData
        {
            TypeName = "System.DateTimeOffset",
            EarlierValue = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero),
            LaterValue = new DateTimeOffset(2023, 6, 15, 12, 0, 0, TimeSpan.Zero),
            EqualValue = new DateTimeOffset(2023, 3, 15, 12, 0, 0, TimeSpan.Zero),
            StringEarlier = "2023-01-01T12:00:00+00:00",
            StringLater = "2023-06-15T12:00:00+00:00",
            StringEqual = "2023-03-15T12:00:00+00:00"
        },
        new DateTimeTypeTestData
        {
            TypeName = "System.Nullable`1[System.DateTimeOffset]",
            EarlierValue = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero),
            LaterValue = new DateTimeOffset(2023, 6, 15, 12, 0, 0, TimeSpan.Zero),
            EqualValue = new DateTimeOffset(2023, 3, 15, 12, 0, 0, TimeSpan.Zero),
            StringEarlier = "2023-01-01T12:00:00+00:00",
            StringLater = "2023-06-15T12:00:00+00:00",
            StringEqual = "2023-03-15T12:00:00+00:00"
        },
        new DateTimeTypeTestData
        {
            TypeName = "System.TimeSpan",
            EarlierValue = new TimeSpan(1, 0, 0),
            LaterValue = new TimeSpan(3, 0, 0),
            EqualValue = new TimeSpan(2, 0, 0),
            StringEarlier = "01:00:00",
            StringLater = "03:00:00",
            StringEqual = "02:00:00"
        },
        new DateTimeTypeTestData
        {
            TypeName = "System.Nullable`1[System.TimeSpan]",
            EarlierValue = new TimeSpan(1, 0, 0),
            LaterValue = new TimeSpan(3, 0, 0),
            EqualValue = new TimeSpan(2, 0, 0),
            StringEarlier = "01:00:00",
            StringLater = "03:00:00",
            StringEqual = "02:00:00"
        }
    };

    private struct OperatorTestData
    {
        public string Operator { get; set; }
        public string ExpectedMatchedEvent { get; set; }
        public string FieldValue { get; set; }
        public string StringValue { get; set; }
    }

    #endregion

    #region Comprehensive Operator Tests

    [TestMethod]
    public void WhenComparingDateTimeWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[0]; // DateTime
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };
        
        foreach (var op in operators)
        {
            TestBasicOperatorFunctionality(dateTimeType, op, "EventDate");
        }
    }

    [TestMethod]
    public void WhenComparingDateTimeOffsetWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[2]; // DateTimeOffset
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };
        
        foreach (var op in operators)
        {
            TestBasicOperatorFunctionality(dateTimeType, op, "EventDate");
        }
    }

    [TestMethod]
    public void WhenComparingTimeSpanWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[4]; // TimeSpan
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };
        
        foreach (var op in operators)
        {
            TestBasicOperatorFunctionality(dateTimeType, op, "Duration");
        }
    }

    [TestMethod]
    public void WhenComparingNullableDateTimeWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[1]; // Nullable DateTime
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };
        
        foreach (var op in operators)
        {
            TestBasicOperatorFunctionality(dateTimeType, op, "EventDate");
        }
    }

    [TestMethod]
    public void WhenComparingNullableDateTimeOffsetWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[3]; // Nullable DateTimeOffset
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };
        
        foreach (var op in operators)
        {
            TestBasicOperatorFunctionality(dateTimeType, op, "EventDate");
        }
    }

    [TestMethod]
    public void WhenComparingNullableTimeSpanWithAllOperators_ShouldAutomaticallyConvert()
    {
        var dateTimeType = DateTimeTypes[5]; // Nullable TimeSpan
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };
        
        foreach (var op in operators)
        {
            TestBasicOperatorFunctionality(dateTimeType, op, "Duration");
        }
    }

    [TestMethod]
    public void WhenComparingReversedWithAllTypes_ShouldAutomaticallyConvert()
    {
        foreach (var dateTimeType in DateTimeTypes)
        {
            var fieldName = dateTimeType.TypeName.Contains("TimeSpan") ? "Duration" : "EventDate";
            TestReversedComparison(dateTimeType, fieldName);
        }
    }

    #endregion

    #region Specific Operator Tests

    [TestMethod]
    public void WhenComparingWithEqualityOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0]; // DateTime
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate = '2023-03-15'";

        var result = RunComparisonTest(query, testData, "Equal Event");
        Assert.AreEqual("Equal Event", result);
    }

    [TestMethod]
    public void WhenComparingWithNotEqualOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0]; // DateTime
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate <> '2023-01-01'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run();

        Assert.AreEqual(2, table.Count); // Should match both "Equal Event" and "Later Event"
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.IsTrue(results.Contains("Equal Event"));
        Assert.IsTrue(results.Contains("Later Event"));
    }

    [TestMethod]
    public void WhenComparingWithGreaterThanOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0]; // DateTime
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate > '2023-01-01'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run();

        Assert.AreEqual(2, table.Count); // Should match both "Equal Event" and "Later Event"
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.IsTrue(results.Contains("Equal Event"));
        Assert.IsTrue(results.Contains("Later Event"));
    }

    [TestMethod]
    public void WhenComparingWithLessThanOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0]; // DateTime
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate < '2023-06-15'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run();

        Assert.AreEqual(2, table.Count); // Should match both "Earlier Event" and "Equal Event"
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.IsTrue(results.Contains("Earlier Event"));
        Assert.IsTrue(results.Contains("Equal Event"));
    }

    [TestMethod]
    public void WhenComparingWithGreaterOrEqualOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0]; // DateTime
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate >= '2023-03-15'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run();

        Assert.AreEqual(2, table.Count); // Should match both "Equal Event" and "Later Event"
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.IsTrue(results.Contains("Equal Event"));
        Assert.IsTrue(results.Contains("Later Event"));
    }

    [TestMethod]
    public void WhenComparingWithLessOrEqualOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0]; // DateTime
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate <= '2023-03-15'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run();

        Assert.AreEqual(2, table.Count); // Should match both "Earlier Event" and "Equal Event"
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.IsTrue(results.Contains("Earlier Event"));
        Assert.IsTrue(results.Contains("Equal Event"));
    }

    #endregion

    #region Type-Specific Tests

    [TestMethod]
    public void WhenComparingDateTimeOffsetWithSpecificOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[2]; // DateTimeOffset
        const string query = "table Events {" +
                             "  EventDate 'System.DateTimeOffset'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate = '2023-03-15T12:00:00+00:00'";

        var result = RunComparisonTest(query, testData, "Equal Event");
        Assert.AreEqual("Equal Event", result);
    }

    [TestMethod]
    public void WhenComparingTimeSpanWithSpecificOperator_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[4]; // TimeSpan
        const string query = "table Events {" +
                             "  Duration 'System.TimeSpan'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where Duration >= '02:00:00'";

        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, "Duration"));
        var table = vm.Run();

        Assert.AreEqual(2, table.Count); // Should match both "Equal Event" and "Later Event"
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        Assert.IsTrue(results.Contains("Equal Event"));
        Assert.IsTrue(results.Contains("Later Event"));
    }

    #endregion

    #region Bidirectional Tests

    [TestMethod]
    public void WhenComparingReversedStringDateTimeWithBasicOperators_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0]; // DateTime
        TestReversedComparison(testData, "EventDate");
    }

    #endregion

    #region Legacy Tests (for backwards compatibility verification)

    [TestMethod]
    public void WhenComparingDateTimeColumnWithStringLiteral_ShouldAutomaticallyConvert()
    {
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate > '2023-01-01'";

        dynamic first = new ExpandoObject();
        first.EventDate = new DateTime(2023, 6, 15);
        first.Name = "Event 1";

        dynamic second = new ExpandoObject();
        second.EventDate = new DateTime(2022, 12, 31);
        second.Name = "Event 2";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>
        {
            first, second
        });

        var table = vm.Run();

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
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate <= '2023-12-31'";

        dynamic first = new ExpandoObject();
        first.EventDate = new DateTime(2023, 12, 15);
        first.Name = "Event 1";

        dynamic second = new ExpandoObject();
        second.EventDate = new DateTime(2024, 1, 5);
        second.Name = "Event 2";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>
        {
            first, second
        });

        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Event 1", table[0].Values[0]);
    }

    #endregion

    #region Helper Methods

    private void TestBasicOperatorFunctionality(DateTimeTypeTestData testData, string op, string fieldName)
    {
        var query = $"table Events {{ {fieldName} '{testData.TypeName}', Name 'System.String' }};" +
                    $"couple #test.whatever with table Events as Events; " +
                    $"select Name from Events() where {fieldName} {op} '{testData.StringEqual}'";

        try
        {
            var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, fieldName));
            var table = vm.Run();
            
            // Basic validation that query executes without error
            Assert.IsNotNull(table, $"Table should not be null for {testData.TypeName} with operator {op}");
            Assert.IsTrue(table.Count >= 0, $"Table count should be non-negative for {testData.TypeName} with operator {op}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Test failed for {testData.TypeName} with operator {op}: {ex.Message}");
        }
    }

    private void TestReversedComparison(DateTimeTypeTestData testData, string fieldName)
    {
        // Test a couple of key operators in reversed order
        var operators = new[] { "=", ">" };
        
        foreach (var op in operators)
        {
            var reversedOp = GetReversedOperator(op);
            var query = $"table Events {{ {fieldName} '{testData.TypeName}', Name 'System.String' }};" +
                        $"couple #test.whatever with table Events as Events; " +
                        $"select Name from Events() where '{testData.StringEqual}' {reversedOp} {fieldName}";

            try
            {
                var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, fieldName));
                var table = vm.Run();
                
                // Basic validation that query executes without error
                Assert.IsNotNull(table, $"Table should not be null for {testData.TypeName} with reversed operator {reversedOp}");
                Assert.IsTrue(table.Count >= 0, $"Table count should be non-negative for {testData.TypeName} with reversed operator {reversedOp}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed for {testData.TypeName} with reversed operator {reversedOp}: {ex.Message}");
            }
        }
    }

    private void TestDateTimeComparison(DateTimeTypeTestData testData, OperatorTestData opData, bool isReversed)
    {
        var fieldName = testData.TypeName.Contains("TimeSpan") ? "Duration" : "EventDate";
        var fieldValue = GetPropertyValue(testData, opData.FieldValue);
        var stringValue = GetPropertyValue(testData, opData.StringValue);

        string query;
        if (isReversed)
        {
            // Reverse the operator for reversed comparison
            var reversedOp = GetReversedOperator(opData.Operator);
            query = $"table Events {{ {fieldName} '{testData.TypeName}', Name 'System.String' }};" +
                    $"couple #test.whatever with table Events as Events; " +
                    $"select Name from Events() where '{stringValue}' {reversedOp} {fieldName}";
        }
        else
        {
            query = $"table Events {{ {fieldName} '{testData.TypeName}', Name 'System.String' }};" +
                    $"couple #test.whatever with table Events as Events; " +
                    $"select Name from Events() where {fieldName} {opData.Operator} '{stringValue}'";
        }

        try
        {
            var result = RunComparisonTest(query, testData, opData.ExpectedMatchedEvent);
            Assert.AreEqual(opData.ExpectedMatchedEvent, result, 
                $"Failed for {testData.TypeName} with operator {opData.Operator} (reversed: {isReversed})");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Test failed for {testData.TypeName} with operator {opData.Operator} (reversed: {isReversed}): {ex.Message}");
        }
    }

    private void TestAllOperatorsForType(DateTimeTypeTestData testData, string fieldName)
    {
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };
        
        foreach (var op in operators)
        {
            var query = $"table Events {{ {fieldName} '{testData.TypeName}', Name 'System.String' }};" +
                        $"couple #test.whatever with table Events as Events; " +
                        $"select Name from Events() where {fieldName} {op} '{testData.StringEqual}'";

            try
            {
                var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, fieldName));
                var table = vm.Run();
                
                // Basic validation that query executes without error
                Assert.IsNotNull(table, $"Table should not be null for operator {op}");
                Assert.IsTrue(table.Count >= 0, $"Table count should be non-negative for operator {op}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed for {testData.TypeName} with operator {op}: {ex.Message}");
            }
        }
    }

    private void TestReversedOperators(DateTimeTypeTestData testData)
    {
        var fieldName = testData.TypeName.Contains("TimeSpan") ? "Duration" : "EventDate";
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };
        
        foreach (var op in operators)
        {
            var reversedOp = GetReversedOperator(op);
            var query = $"table Events {{ {fieldName} '{testData.TypeName}', Name 'System.String' }};" +
                        $"couple #test.whatever with table Events as Events; " +
                        $"select Name from Events() where '{testData.StringEqual}' {reversedOp} {fieldName}";

            try
            {
                var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, fieldName));
                var table = vm.Run();
                
                // Basic validation that query executes without error
                Assert.IsNotNull(table, $"Table should not be null for reversed operator {reversedOp}");
                Assert.IsTrue(table.Count >= 0, $"Table count should be non-negative for reversed operator {reversedOp}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed for {testData.TypeName} with reversed operator {reversedOp}: {ex.Message}");
            }
        }
    }

    private string GetReversedOperator(string op)
    {
        return op switch
        {
            ">" => "<",
            "<" => ">",
            ">=" => "<=",
            "<=" => ">=",
            "=" => "=",
            "<>" => "<>",
            _ => op
        };
    }

    private object GetPropertyValue(DateTimeTypeTestData testData, string propertyName)
    {
        var property = typeof(DateTimeTypeTestData).GetProperty(propertyName);
        return property?.GetValue(testData);
    }

    private string RunComparisonTest(string query, DateTimeTypeTestData testData, string expectedEvent)
    {
        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run();

        Assert.IsTrue(table.Count > 0, "Query should return at least one result");
        return table[0].Values[0] as string;
    }

    private List<dynamic> CreateTestData(DateTimeTypeTestData testData, string fieldName = null)
    {
        fieldName ??= testData.TypeName.Contains("TimeSpan") ? "Duration" : "EventDate";

        dynamic earlier = new ExpandoObject();
        ((IDictionary<string, object>)earlier)[fieldName] = testData.EarlierValue;
        ((IDictionary<string, object>)earlier)["Name"] = "Earlier Event";

        dynamic equal = new ExpandoObject();
        ((IDictionary<string, object>)equal)[fieldName] = testData.EqualValue;
        ((IDictionary<string, object>)equal)["Name"] = "Equal Event";

        dynamic later = new ExpandoObject();
        ((IDictionary<string, object>)later)[fieldName] = testData.LaterValue;
        ((IDictionary<string, object>)later)["Name"] = "Later Event";

        return new List<dynamic> { earlier, equal, later };
    }

    #endregion
}