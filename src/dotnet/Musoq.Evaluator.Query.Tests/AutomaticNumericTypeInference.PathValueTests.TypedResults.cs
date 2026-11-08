using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.PathValue;

namespace Musoq.Evaluator.Tests;

public partial class AutomaticNumericTypeInferencePathValueTests
{
    [TestMethod]
    public void WhenMixingIntLongDoubleInArithmetic_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: double?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path, Value * 2 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "int", Value = 5 },
            new() { Path = "long", Value = 10L },
            new() { Path = "double", Value = 8.2 }
        };

        var table = RunNativeNumericQuery<double>(query, entities);

        TableMaterializationTestHelper.AssertColumns(table, ("Path", typeof(string)), ("Value * 2", typeof(double?)));
        Assert.AreEqual(3, table.Count);

        var intRow = table.First(r => (string)r.ReadValue(0) == "int");
        Assert.AreEqual(10d, intRow.GetValue<double?>(1));

        var longRow = table.First(r => (string)r.ReadValue(0) == "long");
        Assert.AreEqual(20d, longRow.GetValue<double?>(1));

        var doubleRow = table.First(r => (string)r.ReadValue(0) == "double");
        Assert.AreEqual(16.4, (double)doubleRow.ReadValue(1), 0.001);
    }

}
