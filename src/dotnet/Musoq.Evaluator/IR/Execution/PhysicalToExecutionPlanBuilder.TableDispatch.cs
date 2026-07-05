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
        bool scopeAggregateVariables = false,
        PhysicalToExecutionLoweringSession? session = null)
    {
        session ??= new PhysicalToExecutionLoweringSession(ResolveExecutionStrategies());
        var unwrapped = UnwrapSingleStatement(plan);
        var tableContext = new PhysicalToExecutionTableLoweringContext(
            unwrapped,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scopeAggregateVariables,
            session);

        if (CreatePhysicalLoweringRegistry().TryBuildTable(tableContext, out var result))
            return result;

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
