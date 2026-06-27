using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record MultiStatementNode(LogicalNode[] Statements) : LogicalNode(
    OutputSchemaFactory.ForStatements(Statements, static statement => statement.OutputSchema))
{
    public override IReadOnlyList<LogicalNode> Children => Statements;
}
