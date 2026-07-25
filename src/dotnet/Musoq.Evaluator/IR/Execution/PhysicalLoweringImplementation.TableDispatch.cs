using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildPlanTable(
        PhysicalNode plan,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        var unwrapped = UnwrapSingleStatement(plan);
        var tableContext = new PhysicalToExecutionTableLoweringContext(
            unwrapped,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scopeAggregateVariables,
            scope);

        return _physicalLoweringFacade.BuildTable(
            tableContext,
            unsupportedPlan => TableBuildResult.Unsupported(CreateUnsupported(unsupportedPlan).UnsupportedReason!));
    }

    private bool CanBuildTableProducingMultiStatement(PhysicalMultiStatementNode multiStatement)
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

    private bool CanBuildIntermediateTableStatement(PhysicalNode statement)
    {
        var unwrapped = UnwrapSingleStatement(statement);

        return DecomposeSupportedPipeline(unwrapped) != null ||
               CanBuildIntermediateAggregateStatement(unwrapped);
    }

    private bool CanBuildIntermediateAggregateStatement(PhysicalNode statement)
    {
        return _aggregatePlanLowerer.CanBuildIntermediateAggregateStatement(statement);
    }
}
