using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class AsOfStructSourcePlanTests : BasicEntityTestBase
{
    [TestMethod]
    public void StructAsOfCandidate_ShouldCarryGeneratedReferenceShapeAndOriginalContext()
    {
        const string query = "select l.Id as LeftId, r.ReadId() as MatchedId from #stress.rows('left') l asof join #stress.rows('right') r on l.KeyValue >= r.KeyValue tie break by r.Priority asc nulls last";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(),
            new SpecStressAsofTieBreakSchemaProvider(), LoggerResolver, TestCompilationOptions);
        var index = ExecutionIrAnalysis.FlattenNodes(inspection.ExecutionPlan!.Body).OfType<ExecutionCreateAsOfIndex>().Single();
        var shape = inspection.ExecutionPlan.Shapes.OfType<GeneratedRowShape>().Single(shape => shape.TypeName == index.Candidate.GeneratedRowTypeName);

        Assert.IsFalse(shape.EmitAsValueType);
        Assert.HasCount(2, shape.Fields);
        Assert.AreEqual(typeof(int?), shape.Fields.Single(field => field.Name == "Priority").Type.RequireClrType());
        Assert.HasCount(1, shape.Contexts);
        Assert.AreEqual(typeof(SpecStressAsofTieBreakRow), shape.Contexts[0].Type.RequireClrType());
    }
}
