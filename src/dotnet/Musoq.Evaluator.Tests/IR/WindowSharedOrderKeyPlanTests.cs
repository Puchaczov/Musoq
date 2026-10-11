using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class WindowSharedOrderKeyPlanTests : BasicEntityTestBase
{
    [TestMethod]
    public void RowsAndRange_ShouldRetainSharedOrderKeyProducerInExecutionPlan()
    {
        const string query = "select Sum(v.Id) over (order by v.Id rows between unbounded preceding and current row) as RowsTotal, Sum(v.Id) over (order by v.Id range between unbounded preceding and current row) as RangeTotal from #A.Entities() v";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] }), LoggerResolver, TestCompilationOptions);
        var kernels = ExecutionIrAnalysis.FlattenNodes(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionWindowAggregateKernel>().ToArray();

        Assert.HasCount(2, kernels);
        Assert.IsTrue(kernels[0].OrderKeyArray!.IsShared);
        Assert.IsTrue(kernels[0].OrderKeyArray!.ShouldExtract);
        Assert.IsTrue(kernels[1].OrderKeyArray!.IsShared);
        Assert.IsFalse(kernels[1].OrderKeyArray!.ShouldExtract);
        Assert.AreEqual(kernels[0].OrderKeyArray!.Variable.Name, kernels[1].OrderKeyArray!.Variable.Name);
        var shape = kernels[0].OrderKeyArray!.Shape!;
        Assert.IsTrue(shape.IsGeneratedOrderKey);
        Assert.IsFalse(string.IsNullOrWhiteSpace(shape.GeneratedElementTypeName));
        Assert.HasCount(1, shape.GeneratedParts!);
        Assert.AreEqual(typeof(int), shape.GeneratedParts![0].Type.ResolveClrType());
        Assert.AreEqual(typeof(int), shape.RawValueType!.ResolveClrType());
    }
}
