using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    private static void AssertWindowResult(Table table, string name, decimal expected)
    {
        var row = table.Single(r => (string)r.Values[0] == name);
        Assert.AreEqual(expected, Convert.ToDecimal(row.Values[^1]),
            $"Window result for '{name}': expected {expected}, got {Convert.ToDecimal(row.Values[^1])}");
    }

    private static void AssertWindowIntResult(Table table, string name, int expected)
    {
        var row = table.Single(r => (string)r.Values[0] == name);
        Assert.AreEqual(expected, Convert.ToInt32(row.Values[^1]),
            $"Window result for '{name}': expected {expected}, got {Convert.ToInt32(row.Values[^1])}");
    }

    private static void AssertPartitionedWindowResult(Table table, string city, string name, decimal expected)
    {
        var row = table.Single(r => (string)r.Values[0] == city && (string)r.Values[1] == name);
        Assert.AreEqual(expected, Convert.ToDecimal(row.Values[^1]),
            $"Window result for '{city}/{name}': expected {expected}, got {Convert.ToDecimal(row.Values[^1])}");
    }
}
