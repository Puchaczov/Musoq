using System;
using System.Collections;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

internal static class TableMaterializationTestHelper
{
    public static Table Materialize(Table table)
    {
        _ = table.Count;
        return table;
    }

    public static void AssertColumns(Table table, params (string Name, Type Type)[] expected)
    {
        var columns = table.Columns.ToArray();

        Assert.AreEqual(expected.Length, columns.Length, "Unexpected column count.");

        for (var index = 0; index < expected.Length; index++)
        {
            Assert.AreEqual(expected[index].Name, columns[index].ColumnName, $"Unexpected column name at index {index}.");
            Assert.AreEqual(expected[index].Type, columns[index].ColumnType, $"Unexpected column type at index {index}.");
        }
    }

    public static void AssertRowsInOrder(Table table, params object?[][] expected)
    {
        var actual = table.Select(static row => row.Values).ToArray();

        Assert.AreEqual(expected.Length, actual.Length, "Unexpected row count.");

        for (var index = 0; index < expected.Length; index++)
            AssertRowEquals(expected[index], actual[index], $"Unexpected row at index {index}.");
    }

    public static void AssertRowsUnordered(Table table, params object?[][] expected)
    {
        var remaining = table.Select(static row => row.Values).ToList();

        Assert.AreEqual(expected.Length, remaining.Count, "Unexpected row count.");

        foreach (var expectedRow in expected)
        {
            var matchIndex = remaining.FindIndex(actualRow => RowEquals(expectedRow, actualRow));

            if (matchIndex < 0)
                Assert.Fail(
                    $"Expected row was not found: {FormatRow(expectedRow)}. Actual rows: {string.Join(", ", remaining.Select(FormatRow))}");

            remaining.RemoveAt(matchIndex);
        }
    }

    private static void AssertRowEquals(object?[] expected, object?[] actual, string message)
    {
        Assert.AreEqual(expected.Length, actual.Length, $"{message} Unexpected value count.");

        for (var index = 0; index < expected.Length; index++)
            Assert.IsTrue(
                ValuesEqual(expected[index], actual[index]),
                $"{message} Unexpected value at column {index}. Expected: {FormatValue(expected[index])}; actual: {FormatValue(actual[index])}.");
    }

    private static bool RowEquals(object?[] expected, object?[] actual)
    {
        return expected.Length == actual.Length &&
               expected.Zip(actual, ValuesEqual).All(static result => result);
    }

    private static bool ValuesEqual(object? expected, object? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        if (expected is string)
            return expected.Equals(actual);

        if (expected is IEnumerable expectedSequence && actual is IEnumerable actualSequence)
        {
            var expectedItems = expectedSequence.Cast<object?>().ToArray();
            var actualItems = actualSequence.Cast<object?>().ToArray();

            return expectedItems.Length == actualItems.Length &&
                   expectedItems.Zip(actualItems, ValuesEqual).All(static result => result);
        }

        if (expected is IStructuralEquatable structural)
            return structural.Equals(actual, StructuralComparisons.StructuralEqualityComparer);

        return expected.Equals(actual);
    }

    private static string FormatRow(object?[] row)
    {
        return $"[{string.Join(", ", row.Select(FormatValue))}]";
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "<null>",
            string text => $"\"{text}\"",
            IEnumerable enumerable when value is not string => $"[{string.Join(", ", enumerable.Cast<object?>().Select(FormatValue))}]",
            _ => $"{value} ({value.GetType().Name})"
        };
    }
}
