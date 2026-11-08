using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    private static void AssertFinalShapeResult(
        ExecutionPlan plan,
        string expectedTableName,
        string expectedShapeName,
        params string[] expectedColumnNames)
    {
        Assert.IsNotNull(plan.FinalResult);
        Assert.AreEqual(expectedTableName, plan.FinalResult.TableName);
        Assert.AreEqual(expectedShapeName, plan.FinalResult.Shape.TypeName);
        CollectionAssert.AreEqual(
            expectedColumnNames,
            plan.FinalResult.ColumnMetadata.Fields.Select(static field => field.Name).ToArray());
    }
}
