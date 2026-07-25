using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private ExecutionPlanBuildResult BuildMultiStatement(
        PhysicalMultiStatementNode multiStatement,
        string identifier,
        LoweringScope scope)
    {
        var indexes = CreateMultiStatementIndexes(multiStatement);
        var result = BuildMultiStatementTable(
            multiStatement,
            "result",
            "ResultRow0",
            indexes,
            scopeAggregateVariables: true,
            scope);

        if (!result.IsBuilt)
            return ExecutionPlanBuildResult.CreateUnsupported(result.UnsupportedReason);

        return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, result));
    }

    private TableBuildResult BuildMultiStatementTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        var shapes = new List<RowShape>();
        var nodes = new List<ExecutionNode>();
        ExecutionVariable? finalTable = null;
        GeneratedRowShape? finalShape = null;

        if (scope.RecursiveCteSink != null)
        {
            var directRecursiveProjection = TryBuildSingleUseProjectionChainTable(
                multiStatement,
                resultTableName,
                resultShapeName,
                indexes,
                scopeAggregateVariables,
                scope);
            if (directRecursiveProjection != null)
                return directRecursiveProjection;

            return TableBuildResult.Unsupported(
                $"Recursive CTE '{scope.RecursiveCteSink.CteName}' branch cannot be lowered to its direct sink. " +
                $"Statements: {string.Join(", ", multiStatement.Statements.Select(static statement => statement.GetType().Name))}.");
        }

        var finalAggregateProjection = TryBuildFinalAggregateProjectionTable(
            multiStatement,
            resultTableName,
            resultShapeName,
            indexes,
            scopeAggregateVariables,
            scope);
        if (finalAggregateProjection != null)
            return finalAggregateProjection;

        var finalJoinAggregateProjection = TryBuildFinalJoinAggregateProjectionTable(
            multiStatement,
            resultTableName,
            resultShapeName,
            indexes,
            scopeAggregateVariables,
            scope);
        if (finalJoinAggregateProjection != null)
            return finalJoinAggregateProjection;

        var finalJoinProjection = TryBuildFinalJoinProjectionTable(
            multiStatement,
            resultTableName,
            resultShapeName,
            indexes,
            scope);
        if (finalJoinProjection != null)
            return finalJoinProjection;

        var finalSideEffectApplyProjection = TryBuildFinalSideEffectApplyProjectionTable(
            multiStatement,
            resultTableName,
            resultShapeName,
            indexes,
            scopeAggregateVariables,
            scope);
        if (finalSideEffectApplyProjection != null)
            return finalSideEffectApplyProjection;

        var sidecarJoinChain = TryBuildSingleUseSidecarJoinChainTable(
            multiStatement,
            resultTableName,
            resultShapeName,
            indexes,
            scopeAggregateVariables,
            scope);
        if (sidecarJoinChain != null)
            return sidecarJoinChain;

        var finalProjectionChain = TryBuildSingleUseProjectionChainTable(
            multiStatement,
            resultTableName,
            resultShapeName,
            indexes,
            scopeAggregateVariables,
            scope);
        if (finalProjectionChain != null)
            return finalProjectionChain;

        for (var index = 0; index < multiStatement.Statements.Length; index++)
        {
            var statement = multiStatement.Statements[index];
            var isFinalStatement = index == multiStatement.Statements.Length - 1;
            var statementTableName = isFinalStatement
                ? resultTableName
                : CreateStatementTableName(indexes.StatementNamePrefix, index);
            var statementShapeName = isFinalStatement
                ? resultShapeName
                : CreateStatementShapeName(indexes.StatementNamePrefix, index);
            var result = BuildStatementTable(
                statement,
                statementTableName,
                statementShapeName,
                indexes,
                scopeAggregateVariables,
                scope);

            if (!result.IsBuilt)
                return result;

            shapes.AddRange(result.Shapes);
            nodes.AddRange(result.Nodes);

            if (isFinalStatement)
            {
                finalTable = result.Table;
                finalShape = result.RowShape;
                continue;
            }

            var tableIndex = ResolveStatementTableIndex(index, indexes);
            if (tableIndex < 0)
            {
                return TableBuildResult.Unsupported(
                    $"Execution IR multi-statement lowering cannot resolve table storage slot for statement {index.ToString(CultureInfo.InvariantCulture)}.");
            }

            var cteName = ResolveStatementCteName(index, indexes);
            if (!string.IsNullOrWhiteSpace(cteName))
                indexes.CteShapesByName[cteName] = result.RowShape;

            nodes.Add(new ExecutionStoreTable(result.Table, tableIndex));
        }

        if (finalTable == null || finalShape == null)
            return TableBuildResult.Unsupported("Execution IR multi-statement lowering requires at least one statement.");

        return TableBuildResult.Success(shapes, nodes, finalTable, finalShape);
    }

    private TableBuildResult BuildStatementTable(
        PhysicalNode statement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        return BuildPlanTable(
            statement,
            resultTableName,
            resultShapeName,
            indexes.CteIndexes,
            indexes.CteShapesByName,
            schemaFromIndex: DefaultSchemaFromIndex,
            scopeAggregateVariables: scopeAggregateVariables,
            scope: scope);
    }
}
