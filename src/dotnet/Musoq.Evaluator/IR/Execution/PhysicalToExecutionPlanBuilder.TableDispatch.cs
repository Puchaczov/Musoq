using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildPlanTable(
        PhysicalNode plan,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null,
        int schemaFromIndex = DefaultSchemaFromIndex,
        bool scopeAggregateVariables = false)
    {
        var unwrapped = UnwrapSingleStatement(plan);
        var tableContext = new PhysicalToExecutionTableLoweringContext(
            unwrapped,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scopeAggregateVariables);
        if (unwrapped is PhysicalMultiStatementNode multiStatement && CanBuildTableProducingMultiStatement(multiStatement))
        {
            var multiStatementIndexes = CreateMultiStatementIndexes(
                multiStatement,
                cteIndexes,
                cteShapesByName,
                ResolveStatementNamePrefix(resultTableName));

            return BuildMultiStatementTable(
                multiStatement,
                resultTableName,
                resultShapeName,
                multiStatementIndexes,
                scopeAggregateVariables);
        }

        var setOperationPipeline = DecomposeSetOperationPipeline(unwrapped);
        if (setOperationPipeline != null)
        {
            return BuildSetOperationTable(
                setOperationPipeline,
                resultTableName,
                resultShapeName,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex);
        }

        if (new WindowLoweringCoordinator(this).TryBuildTable(tableContext, out var windowResult))
            return windowResult;

        var pipeline = DecomposeSupportedPipeline(unwrapped);
        if (pipeline != null)
        {
            return BuildTable(
                pipeline,
                resultTableName,
                resultShapeName,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex);
        }

        if (new AggregateLoweringCoordinator(this).TryBuildTable(tableContext, out var aggregateResult))
            return aggregateResult;

        return TableBuildResult.Unsupported(CreateUnsupported(unwrapped).UnsupportedReason!);
    }

    private static bool CanBuildTableProducingMultiStatement(PhysicalMultiStatementNode multiStatement)
    {
        if (multiStatement.Statements.Length < 2)
            return false;

        for (var index = 0; index < multiStatement.Statements.Length - 1; index++)
        {
            if (!CanBuildIntermediateTableStatement(multiStatement.Statements[index]))
                return false;
        }

        return true;
    }

    private static bool CanBuildIntermediateTableStatement(PhysicalNode statement)
    {
        var unwrapped = UnwrapSingleStatement(statement);

        return DecomposeSupportedPipeline(unwrapped) != null ||
               CanBuildIntermediateAggregateStatement(unwrapped);
    }

    private static bool CanBuildIntermediateAggregateStatement(PhysicalNode statement)
    {
        return AggregateLoweringCoordinator.CanBuildIntermediateAggregateStatement(statement);
    }
}
