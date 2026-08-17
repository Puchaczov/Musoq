using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal interface IMultiStatementLoweringOperations : IMultiStatementLoweringService
{
}

internal sealed class MultiStatementLoweringService(IMultiStatementLoweringOperations operations) : IMultiStatementLoweringService
{
    public LoweringAttempt<ExecutionPlan> BuildMultiStatement(
        PhysicalMultiStatementNode statement,
        string identifier,
        LoweringScope scope) => operations.BuildMultiStatement(statement, identifier, scope);

    public bool CanBuildTableProducingMultiStatement(PhysicalMultiStatementNode statement) =>
        operations.CanBuildTableProducingMultiStatement(statement);

    public MultiStatementIndexes CreateMultiStatementIndexes(
        PhysicalMultiStatementNode statement,
        IReadOnlyDictionary<string, int>? existingCteIndexes = null,
        IReadOnlyDictionary<string, GeneratedRowShape>? existingCteShapesByName = null,
        string? statementNamePrefix = null) => operations.CreateMultiStatementIndexes(
            statement,
            existingCteIndexes,
            existingCteShapesByName,
            statementNamePrefix);

    public LoweringAttempt<LoweredTable> BuildMultiStatementTable(
        PhysicalMultiStatementNode statement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope) => operations.BuildMultiStatementTable(
            statement,
            resultTableName,
            resultShapeName,
            indexes,
            scopeAggregateVariables,
            scope);
}
