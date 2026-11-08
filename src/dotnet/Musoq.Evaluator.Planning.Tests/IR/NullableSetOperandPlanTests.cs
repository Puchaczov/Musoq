using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class NullableSetOperandPlanTests : BasicEntityTestBase
{
    [TestMethod]
    public void NullableSetBinding_ShouldInsertTypedLiftIntoLogicalPlan()
    {
        const string query = "select v.Value as Value from values { (Value: null), (Value: 1) } v union all select 2 as Value from values { (Marker: 0) } w";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()), LoggerResolver, TestCompilationOptions);
        Assert.Contains("2::Int32", inspection.LogicalPlanText);
    }
}
