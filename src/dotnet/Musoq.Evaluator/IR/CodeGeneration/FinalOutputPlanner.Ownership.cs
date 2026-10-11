using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static partial class FinalOutputPlanner
{
    public static FinalOutputPlan SelectTableRows(ExecutionPlan plan, FinalOutputPlan output)
    {
        if (!output.IsSupported || output.UsesGeneratedRow ||
            !HasFreshTerminalWrites(plan.Body, output.Result))
        {
            return output;
        }

        return output with { RowRepresentation = FinalOutputRowRepresentation.GeneratedRow };
    }

    private static bool HasFreshTerminalWrites(ExecutionBlock body, TableViaRowsResultInfo result)
    {
        var foundAppend = false;
        foreach (var node in ExecutionIrAnalysis.FlattenNodes(body))
        {
            if (node is ExecutionParallelBlock or ExecutionParallelFilterProjectLoop &&
                ExecutionIrAnalysis.ContainsVariableUse(node, result.TableName))
            {
                return false;
            }

            if (ExecutionIrAnalysis.ContainsVariableUse(ExecutionIrAnalysis.GetNodeExpressions(node), result.TableName))
                return false;

            if (node is ExecutionAppendRow append && append.Table.Name == result.TableName)
            {
                if (append.RowShape.TypeName != result.RowTypeName ||
                    append.Values.Count != result.ShapeFields.Count ||
                    !CanUseFinalShapeSourceBuffer(append.RowShape, result.ShapeFields))
                {
                    return false;
                }

                foundAppend = true;
                continue;
            }

            if (node is ExecutionReturnTable { Table.Name: var returned } && returned == result.TableName ||
                node is ExecutionEnsureTableCapacity { Table.Name: var ensured } && ensured == result.TableName)
            {
                continue;
            }

            if (ExecutionNodeFacts.GetDirectVariableReferences(node).Any(variable => variable.Name == result.TableName))
                return false;
        }

        return foundAppend;
    }
}
