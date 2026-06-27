using System.Collections.Generic;

using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalMultiStatementNode(
    PhysicalNode[] Statements) : PhysicalNode(
        OutputSchemaFactory.ForStatements(Statements, static statement => statement.OutputSchema))
{
    public override IReadOnlyList<PhysicalNode> Children => Statements;
}
