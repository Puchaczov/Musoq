using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning.Subqueries;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class ExistsProjectionTests
{
    [TestMethod]
    public void Prepare_ShouldKeepIntermediateProducerAndDiscardOnlyTheFinalProjection()
    {
        var key = new Literal(2, typeof(int));
        var producer = new ProjectNode(
            [new ProjectedField("K", key, 0)],
            new ValuesScanNode("v", [], OutputSchema.Empty)
        );
        var binding = new IntermediateSourceBinding(new IntermediateProducerId(0), "joined");
        var source = new CteRefNode("joined", "j", producer.OutputSchema)
        {
            IntermediateProducerId = binding.ProducerId,
        };
        var output = new ProjectNode([new ProjectedField("S", new ColumnRef("j", "K", typeof(int)), 0)], source);
        var input = new MultiStatementNode([producer, output]) { IntermediateProducers = [new(binding, 0)] };

        var prepared = (MultiStatementNode)ExistsProjection.Prepare(input);

        Assert.AreSame(producer, prepared.Statements[0]);
        Assert.AreEqual(input.IntermediateProducers, prepared.IntermediateProducers);
        var result = (ProjectNode)prepared.Statements[1];
        Assert.AreSame(source, result.Input);
        Assert.AreEqual(1, ((Literal)result.Fields[0].Expression).Value);
    }
}
