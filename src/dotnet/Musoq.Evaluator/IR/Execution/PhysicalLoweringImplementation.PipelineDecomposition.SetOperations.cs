using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static SetOperationPipeline? DecomposeSetOperationPipeline(PhysicalNode node)
    {
        var operations = new List<PostOperation>();
        var current = PeelPostOperations(node, operations);

        return current is PhysicalSetOperationNode setOperation
            ? new SetOperationPipeline(
                setOperation,
                CreatePostOperations(operations, CreateProjectedFields(setOperation.OutputSchema)))
            : null;
    }

    private static SetOperationArmNames CreateSetOperationArmNames(string resultTableName, string resultShapeName)
    {
        if (string.Equals(resultTableName, "result", StringComparison.Ordinal) &&
            string.Equals(resultShapeName, "ResultRow0", StringComparison.Ordinal))
        {
            return new SetOperationArmNames("left", "LeftRow0", "right", "RightRow0");
        }

        return new SetOperationArmNames(
            $"{resultTableName}Left",
            CreateSetOperationArmShapeName(resultShapeName, "Left"),
            $"{resultTableName}Right",
            CreateSetOperationArmShapeName(resultShapeName, "Right"));
    }

    private static string CreateSetOperationArmShapeName(string resultShapeName, string armName)
    {
        const string rowSuffix = "Row0";

        if (!resultShapeName.EndsWith(rowSuffix, StringComparison.Ordinal))
            return $"{resultShapeName}{armName}";

        return $"{resultShapeName[..^rowSuffix.Length]}{armName}{rowSuffix}";
    }
}
