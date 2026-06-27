using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;


namespace Musoq.Evaluator.IR.Physical;

public sealed partial class PhysicalPlanBuilder
{
    private PhysicalWindowNode LowerWindow(WindowNode node, PhysicalStrategyPlan strategyPlan)
    {
        var input = Lower(node.Input, strategyPlan);
        return new PhysicalWindowNode(node.Registrations, input);
    }

    private PhysicalSetOperationNode LowerSetOperation(SetOperationNode node, PhysicalStrategyPlan strategyPlan)
    {
        var left = Lower(node.Left, strategyPlan);
        var right = Lower(node.Right, strategyPlan);
        var columns = left.OutputSchema.Columns;
        var keys = node.Keys;

        if (keys.Length == 0)
        {
            var allIndexes = new int[columns.Length];
            var allTypes = new Type[columns.Length];

            for (var i = 0; i < columns.Length; i++)
            {
                allIndexes[i] = columns[i].Index;
                allTypes[i] = columns[i].Type;
            }

            return new PhysicalSetOperationNode(node.Kind, left, right, allIndexes, allTypes);
        }

        var projectFields = FindTopProjectFields(node.Left);
        var keyIndexes = new int[keys.Length];
        var keyTypes = new Type[keys.Length];

        for (var i = 0; i < keys.Length; i++)
        {
            if (!TryResolveKey(keys[i], projectFields, columns, out var index, out var type))
                throw new InvalidOperationException(
                    $"Set operation key '{keys[i]}' does not match any output column of the left input.");
            keyIndexes[i] = index;
            keyTypes[i] = type;
        }

        return new PhysicalSetOperationNode(node.Kind, left, right, keyIndexes, keyTypes);
    }
}
