using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution.Lowering;
using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;
using Musoq.Evaluator.IR.Execution.Lowering.Ctes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Compatibility composition for the physical lowering implementation.
/// The public lowering coordinators consume the narrow domain interfaces and
/// do not receive delegates bound to this implementation type.
/// </summary>
internal sealed partial class PhysicalLoweringImplementation
{
    LoweringAttempt<LoweredTable> IApplyLoweringService.BuildApplyTable(
        PhysicalNestedLoopApplyNode apply,
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        IReadOnlyDictionary<string, RowShape>? inheritedSourceLookup,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildApplyTable(
            apply,
            pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            inheritedSourceLookup,
            scope));

    LoweringAttempt<LoweredTable> IJoinLoweringService.BuildNestedLoopJoinTable(
        PhysicalNestedLoopJoinNode join,
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildNestedLoopJoinTable(
            join,
            pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope));

    LoweringAttempt<LoweredTable> IJoinLoweringService.BuildHashJoinTable(
        PhysicalHashJoinNode join,
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildHashJoinTable(
            join,
            pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope));

    LoweringAttempt<LoweredTable> IJoinLoweringService.BuildSortMergeJoinTable(
        PhysicalSortMergeJoinNode join,
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildSortMergeJoinTable(
            join,
            pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope));

    CteSupportedPipeline? IPipelineLoweringService.DecomposeSupportedPipeline(PhysicalNode node) =>
        DecomposeSupportedPipeline(node);

    SetOperationPipeline? IPipelineLoweringService.DecomposeSetOperationPipeline(PhysicalNode node) =>
        DecomposeSetOperationPipeline(node);

    LoweringAttempt<ExecutionPlan> IPipelineLoweringService.BuildPipeline(
        CteSupportedPipeline pipeline,
        string identifier,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildPipeline(pipeline, identifier, scope));

    LoweringAttempt<ExecutionPlan> IPipelineLoweringService.BuildSetOperation(
        SetOperationPipeline pipeline,
        string identifier,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildSetOperation(pipeline, identifier, scope));

    LoweringAttempt<LoweredTable> IPipelineLoweringService.BuildTable(
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildTable(
            pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope));

    LoweringAttempt<LoweredTable> IPipelineLoweringService.BuildSetOperationTable(
        SetOperationPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildSetOperationTable(
            pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope));

    LoweringAttempt<ExecutionPlan> ICteLoweringService.BuildCte(
        PhysicalCteNode cte,
        string identifier,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildCte(cte, identifier, scope));

    LoweringAttempt<ExecutionPlan> IMultiStatementLoweringService.BuildMultiStatement(
        PhysicalMultiStatementNode statement,
        string identifier,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildMultiStatement(statement, identifier, scope));

    bool IMultiStatementLoweringService.CanBuildTableProducingMultiStatement(
        PhysicalMultiStatementNode statement) =>
        CanBuildTableProducingMultiStatement(statement);

    MultiStatementIndexes IMultiStatementLoweringService.CreateMultiStatementIndexes(
        PhysicalMultiStatementNode statement,
        IReadOnlyDictionary<string, int>? existingCteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? existingCteShapesByName,
        string? statementNamePrefix) =>
        CreateMultiStatementIndexes(
            statement,
            existingCteIndexes,
            existingCteShapesByName,
            statementNamePrefix);

    LoweringAttempt<LoweredTable> IMultiStatementLoweringService.BuildMultiStatementTable(
        PhysicalMultiStatementNode statement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope) =>
        LoweringAttemptConversions.From(BuildMultiStatementTable(
            statement,
            resultTableName,
            resultShapeName,
            indexes,
            scopeAggregateVariables,
            scope));

    LoweringAttempt<ExecutionPlan> IDescLoweringService.BuildDesc(
        PhysicalDescNode desc,
        string identifier) =>
        LoweringAttemptConversions.From(BuildDesc(desc, identifier));
}
