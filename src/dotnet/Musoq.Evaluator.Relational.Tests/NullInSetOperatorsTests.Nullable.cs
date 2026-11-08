using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class NullInSetOperatorsTests
{
    [TestMethod]
    [DataRow("select v.Value as Value from values { (Value: null), (Value: 1) } v union all select 2 as Value from values { (Marker: 0) } w")]
    [DataRow("select 2 as Value from values { (Marker: 0) } w union all select v.Value as Value from values { (Value: null), (Value: 1) } v")]
    [DataRow("select v.Value as Value from values { (Value: null), (Value: 1) } v union select 1+1 as Value from values { (Marker: 0) } w")]
    [DataRow("select 1+1 as Value from values { (Marker: 0) } w union select v.Value as Value from values { (Value: null), (Value: 1) } v")]
    public void NullableSetOperands_ShouldPreserveNullableOutputAndValues(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { null }, new object?[] { 1 }, new object?[] { 2 });
    }

    [TestMethod]
    public void ChainedNullableSetOperands_ShouldPropagateNullableOutput()
    {
        const string query = "select 2 as Value from values { (Marker: 0) } w union all select v.Value as Value from values { (Value: null), (Value: 1) } v union all select 3 as Value from values { (Marker: 0) } x";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { null }, new object?[] { 1 }, new object?[] { 2 }, new object?[] { 3 });
    }

}
