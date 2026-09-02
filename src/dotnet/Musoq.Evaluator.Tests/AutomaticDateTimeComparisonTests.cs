// ReSharper disable UnusedAutoPropertyAccessor.Local
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class AutomaticDateTimeComparisonTests : UnknownQueryTestsBase
{

    [TestMethod]
    public void WhenComparingReversedStringDateTimeWithBasicOperators_ShouldAutomaticallyConvert()
    {
        var testData = DateTimeTypes[0];
        TestReversedComparison(testData, "EventDate");
    }



    private sealed class DateTimeTypeTestData
    {
        public string TypeName { get; init; } = string.Empty;
        public required object EarlierValue { get; init; }
        public required object LaterValue { get; init; }
        public required object EqualValue { get; init; }
        public string StringEarlier { get; init; } = string.Empty;
        public string StringLater { get; set; } = string.Empty;
        public string StringEqual { get; init; } = string.Empty;
    }

    private static readonly DateTimeTypeTestData[] DateTimeTypes =
    [
        new()
        {
            TypeName = "datetime",
            EarlierValue = new DateTime(2023, 1, 1),
            LaterValue = new DateTime(2023, 6, 15),
            EqualValue = new DateTime(2023, 3, 15),
            StringEarlier = "2023-01-01",
            StringLater = "2023-06-15",
            StringEqual = "2023-03-15"
        },
        new()
        {
            TypeName = "datetime?",
            EarlierValue = new DateTime(2023, 1, 1),
            LaterValue = new DateTime(2023, 6, 15),
            EqualValue = new DateTime(2023, 3, 15),
            StringEarlier = "2023-01-01",
            StringLater = "2023-06-15",
            StringEqual = "2023-03-15"
        },
        new()
        {
            TypeName = "datetimeoffset",
            EarlierValue = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero),
            LaterValue = new DateTimeOffset(2023, 6, 15, 12, 0, 0, TimeSpan.Zero),
            EqualValue = new DateTimeOffset(2023, 3, 15, 12, 0, 0, TimeSpan.Zero),
            StringEarlier = "2023-01-01T12:00:00+00:00",
            StringLater = "2023-06-15T12:00:00+00:00",
            StringEqual = "2023-03-15T12:00:00+00:00"
        },
        new()
        {
            TypeName = "datetimeoffset?",
            EarlierValue = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero),
            LaterValue = new DateTimeOffset(2023, 6, 15, 12, 0, 0, TimeSpan.Zero),
            EqualValue = new DateTimeOffset(2023, 3, 15, 12, 0, 0, TimeSpan.Zero),
            StringEarlier = "2023-01-01T12:00:00+00:00",
            StringLater = "2023-06-15T12:00:00+00:00",
            StringEqual = "2023-03-15T12:00:00+00:00"
        },
        new()
        {
            TypeName = "timespan",
            EarlierValue = new TimeSpan(1, 0, 0),
            LaterValue = new TimeSpan(3, 0, 0),
            EqualValue = new TimeSpan(2, 0, 0),
            StringEarlier = "01:00:00",
            StringLater = "03:00:00",
            StringEqual = "02:00:00"
        },
        new()
        {
            TypeName = "timespan?",
            EarlierValue = new TimeSpan(1, 0, 0),
            LaterValue = new TimeSpan(3, 0, 0),
            EqualValue = new TimeSpan(2, 0, 0),
            StringEarlier = "01:00:00",
            StringLater = "03:00:00",
            StringEqual = "02:00:00"
        }
    ];

    private sealed class OperatorTestData
    {
        public string Operator { get; set; } = string.Empty;
        public string ExpectedMatchedEvent { get; set; } = string.Empty;
        public string FieldValue { get; set; } = string.Empty;
        public string StringValue { get; set; } = string.Empty;
    }


    private void TestBasicOperatorFunctionality(DateTimeTypeTestData testData, string op, string fieldName)
    {
        var query = $"table Events {{ {fieldName}: {testData.TypeName}, Name: string }};" +
                    $"couple #test.whatever with table Events as Events; " +
                    $"select Name from Events() where {fieldName} {op} '{testData.StringEqual}'";

        try
        {
            var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, fieldName));
            var table = vm.Run();
            var actualEvents = table
                .Select(static row => row.Values[0])
                .Cast<string>()
                .OrderBy(static name => name)
                .ToArray();

            CollectionAssert.AreEqual(
                GetExpectedEventsForOperator(op),
                actualEvents,
                $"Unexpected rows for {testData.TypeName} with operator {op}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Test failed for {testData.TypeName} with operator {op}: {ex.Message}");
        }
    }

    private void TestReversedComparison(DateTimeTypeTestData testData, string fieldName)
    {
        var operators = new[] { "=", ">" };

        foreach (var op in operators)
        {
            var reversedOp = GetReversedOperator(op);
            var query = $"table Events {{ {fieldName}: {testData.TypeName}, Name: string }};" +
                        $"couple #test.whatever with table Events as Events; " +
                        $"select Name from Events() where '{testData.StringEqual}' {reversedOp} {fieldName}";

            try
            {
                var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, fieldName));
                var table = vm.Run();

                Assert.IsNotNull(table,
                    $"Table should not be null for {testData.TypeName} with reversed operator {reversedOp}");
                Assert.IsGreaterThanOrEqualTo(0, table.Count,
                    $"Table count should be non-negative for {testData.TypeName} with reversed operator {reversedOp}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed for {testData.TypeName} with reversed operator {reversedOp}: {ex.Message}");
            }
        }
    }

    private void TestDateTimeComparison(DateTimeTypeTestData testData, OperatorTestData opData, bool isReversed)
    {
        var fieldName = testData.TypeName.Contains("timespan") ? "Duration" : "EventDate";
        var stringValue = GetPropertyValue(testData, opData.StringValue);

        string query;
        if (isReversed)
        {
            var reversedOp = GetReversedOperator(opData.Operator);
            query = $"table Events {{ {fieldName}: {testData.TypeName}, Name: string }};" +
                    $"couple #test.whatever with table Events as Events; " +
                    $"select Name from Events() where '{stringValue}' {reversedOp} {fieldName}";
        }
        else
        {
            query = $"table Events {{ {fieldName}: {testData.TypeName}, Name: string }};" +
                    $"couple #test.whatever with table Events as Events; " +
                    $"select Name from Events() where {fieldName} {opData.Operator} '{stringValue}'";
        }

        try
        {
            var result = RunComparisonTest(query, testData);
            Assert.AreEqual(opData.ExpectedMatchedEvent, result,
                $"Failed for {testData.TypeName} with operator {opData.Operator} (reversed: {isReversed})");
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"Test failed for {testData.TypeName} with operator {opData.Operator} (reversed: {isReversed}): {ex.Message}");
        }
    }

    private void TestAllOperatorsForType(DateTimeTypeTestData testData, string fieldName)
    {
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };

        foreach (var op in operators)
        {
            var query = $"table Events {{ {fieldName}: {testData.TypeName}, Name: string }};" +
                        $"couple #test.whatever with table Events as Events; " +
                        $"select Name from Events() where {fieldName} {op} '{testData.StringEqual}'";

            try
            {
                var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, fieldName));
                var table = vm.Run();
                var actualEvents = table
                    .Select(static row => row.Values[0])
                    .Cast<string>()
                    .OrderBy(static name => name)
                    .ToArray();

                CollectionAssert.AreEqual(
                    GetExpectedEventsForOperator(op),
                    actualEvents,
                    $"Unexpected rows for {testData.TypeName} with operator {op}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed for {testData.TypeName} with operator {op}: {ex.Message}");
            }
        }
    }

    private void TestReversedOperators(DateTimeTypeTestData testData)
    {
        var fieldName = testData.TypeName.Contains("timespan") ? "Duration" : "EventDate";
        var operators = new[] { "=", ">", "<", ">=", "<=", "<>" };

        foreach (var op in operators)
        {
            var reversedOp = GetReversedOperator(op);
            var query = $"table Events {{ {fieldName}: {testData.TypeName}, Name: string }};" +
                        $"couple #test.whatever with table Events as Events; " +
                        $"select Name from Events() where '{testData.StringEqual}' {reversedOp} {fieldName}";

            try
            {
                var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData, fieldName));
                var table = vm.Run();
                var actualEvents = table
                    .Select(static row => row.Values[0])
                    .Cast<string>()
                    .OrderBy(static name => name)
                    .ToArray();

                CollectionAssert.AreEqual(
                    GetExpectedEventsForOperator(op),
                    actualEvents,
                    $"Unexpected rows for {testData.TypeName} with reversed operator {reversedOp}");
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

    private static string[] GetExpectedEventsForOperator(string op)
    {
        return op switch
        {
            "=" => ["Equal Event"],
            ">" => ["Later Event"],
            "<" => ["Earlier Event"],
            ">=" => ["Equal Event", "Later Event"],
            "<=" => ["Earlier Event", "Equal Event"],
            "<>" => ["Earlier Event", "Later Event"],
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported comparison operator.")
        };
    }

    private object? GetPropertyValue(DateTimeTypeTestData testData, string propertyName)
    {
        var property = typeof(DateTimeTypeTestData).GetProperty(propertyName);
        return property?.GetValue(testData);
    }

    private string RunComparisonTest(string query, DateTimeTypeTestData testData)
    {
        var vm = CreateAndRunVirtualMachine(query, CreateTestData(testData));
        var table = vm.Run();

        Assert.IsGreaterThan(0, table.Count, "Query should return at least one result");
            return table[0].Values[0] as string ??
                   throw new AssertFailedException("Expected comparison result to be a string.");
    }

    private List<dynamic> CreateTestData(DateTimeTypeTestData testData, string? fieldName = null)
    {
        fieldName ??= testData.TypeName.Contains("timespan") ? "Duration" : "EventDate";

        dynamic earlier = new ExpandoObject();
        ((IDictionary<string, object>)earlier)[fieldName] = testData.EarlierValue;
        ((IDictionary<string, object>)earlier)["Name"] = "Earlier Event";

        dynamic equal = new ExpandoObject();
        ((IDictionary<string, object>)equal)[fieldName] = testData.EqualValue;
        ((IDictionary<string, object>)equal)["Name"] = "Equal Event";

        dynamic later = new ExpandoObject();
        ((IDictionary<string, object>)later)[fieldName] = testData.LaterValue;
        ((IDictionary<string, object>)later)["Name"] = "Later Event";

        return [earlier, equal, later];
    }


}
