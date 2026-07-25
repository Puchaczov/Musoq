using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;


namespace Musoq.Evaluator.IR.Physical;

public sealed partial class PhysicalPlanBuilder
{
    private PhysicalCteNode LowerCte(CteNode node, PhysicalStrategyPlan strategyPlan)
    {
        var definitions = new PhysicalCteDefinition[node.Definitions.Length];

        for (var i = 0; i < node.Definitions.Length; i++)
        {
            var definition = node.Definitions[i];
            definitions[i] = new PhysicalCteDefinition(definition.Name, Lower(definition.Plan, strategyPlan));
        }

        return new PhysicalCteNode(definitions, Lower(node.Query, strategyPlan));
    }

    private PhysicalRecursiveCteNode LowerRecursiveCte(
        RecursiveCteNode node,
        PhysicalStrategyPlan strategyPlan)
    {
        return new PhysicalRecursiveCteNode(
            node.Name,
            Lower(node.Anchor, strategyPlan),
            Lower(node.RecursiveMember, strategyPlan),
            node.UnionKind,
            node.Keys, node.IdentityFieldIndexes,
            []);
    }

    private PhysicalMultiStatementNode LowerMultiStatement(MultiStatementNode node, PhysicalStrategyPlan strategyPlan)
    {
        var statements = new PhysicalNode[node.Statements.Length];

        for (var i = 0; i < node.Statements.Length; i++)
            statements[i] = Lower(node.Statements[i], strategyPlan);

        return new PhysicalMultiStatementNode(statements);
    }
}
