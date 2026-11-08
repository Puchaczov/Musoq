using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class IntermediateProjectionPruningTests
{
    [TestMethod]
    public void IntermediatePruning_DeadPassthrough_RetainsVolatileAndPotentiallyThrowingExpressions()
    {
        var input = new PhysicalValuesScanNode("v", [], new OutputSchema(
            [new("Id", typeof(int), 0), new("Dead", typeof(int), 1), new("Volatile", typeof(int), 2), new("Nested", typeof(string), 3)]));
        var producer = new PhysicalProjectNode(
        [
            new("Id", new ColumnRef("v", "Id", typeof(int)), 0),
            new("Dead", new ColumnRef("v", "Dead", typeof(int)), 1),
            new("Volatile", new ColumnRef("v", "Volatile", typeof(int)) { Stability = ColumnStability.Volatile }, 2),
            new("MustThrow", new BinaryOp(BinaryOpKind.Divide, new Literal(1, typeof(int)),
                new Literal(0, typeof(int)), typeof(int)), 3),
            new("NestedLength", new ColumnRef("v", "Nested.Length", typeof(int)), 4)
        ], input);
        var identity = new IntermediateProducerId(42);
        var reference = new PhysicalCteRefNode("stage", "s", OutputSchema.Empty) { IntermediateProducerId = identity };
        var consumer = new PhysicalProjectNode([new("Id", new ColumnRef("s", "Id", typeof(int)), 0)], reference);
        var multi = new PhysicalMultiStatementNode([producer, consumer])
        {
            IntermediateProducers = [new(new IntermediateSourceBinding(identity, "stage"), 0)]
        };

        var result = ProjectionPruningCteDefinitions.PruneIntermediateOutputs(multi);

        Assert.AreEqual(1, result.PrunedFields);
        CollectionAssert.AreEqual(new[] { "Id", "Volatile", "MustThrow", "NestedLength" },
            result.Node.Statements[0].OutputSchema.Columns.Select(static column => column.Name).ToArray());
        var rewritten = (PhysicalCteRefNode)result.Node.Statements[1].Children.Single();
        CollectionAssert.AreEqual(new[] { "Id", "Volatile", "MustThrow", "NestedLength" },
            rewritten.OutputSchema.Columns.Select(static column => column.Name).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 },
            rewritten.OutputSchema.Columns.Select(static column => column.Index).ToArray());
    }
}
