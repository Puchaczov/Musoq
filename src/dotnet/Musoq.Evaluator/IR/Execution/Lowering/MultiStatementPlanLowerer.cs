using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering;

internal interface IMultiStatementLoweringService
{
    LoweringAttempt<ExecutionPlan> BuildMultiStatement(
        PhysicalMultiStatementNode statement,
        string identifier,
        LoweringScope scope);

    bool CanBuildTableProducingMultiStatement(PhysicalMultiStatementNode statement);

    MultiStatementIndexes CreateMultiStatementIndexes(
        PhysicalMultiStatementNode statement,
        IReadOnlyDictionary<string, int>? existingCteIndexes = null,
        IReadOnlyDictionary<string, GeneratedRowShape>? existingCteShapesByName = null,
        string? statementNamePrefix = null);

    LoweringAttempt<LoweredTable> BuildMultiStatementTable(
        PhysicalMultiStatementNode statement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope);
}

internal sealed class MultiStatementPlanLowerer(
    IMultiStatementLoweringService service)
{
    public LoweringAttempt<ExecutionPlan> TryBuildPlan(PhysicalToExecutionLoweringContext context)
    {
        return context.Plan is PhysicalMultiStatementNode statement
            ? service.BuildMultiStatement(
                statement,
                context.Identifier,
                context.Scope)
            : LoweringAttempt<ExecutionPlan>.NoMatch();
    }

    public LoweringAttempt<LoweredTable> TryBuildTable(PhysicalToExecutionTableLoweringContext context)
    {
        if (context.Plan is not PhysicalMultiStatementNode statement ||
            !service.CanBuildTableProducingMultiStatement(statement))
        {
            return LoweringAttempt<LoweredTable>.NoMatch();
        }

        return service.BuildMultiStatementTable(
            statement,
            context.ResultTableName,
            context.ResultShapeName,
            service.CreateMultiStatementIndexes(
                statement,
                context.CteIndexes,
                context.CteShapesByName,
                ResolveStatementNamePrefix(context.ResultTableName)),
            context.ScopeAggregateVariables,
            context.Scope);
    }

    private static string? ResolveStatementNamePrefix(string resultTableName) =>
        string.Equals(resultTableName, "result", StringComparison.Ordinal)
            ? null
            : resultTableName;
}
