using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult? TryBuildFinalSideEffectApplyProjectionTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        if (multiStatement.Statements.Length < 2)
            return null;

        var producerIndex = multiStatement.Statements.Length - 2;
        var producerCteName = ResolveStatementCteName(producerIndex, indexes);
        if (string.IsNullOrWhiteSpace(producerCteName))
            return null;

        var finalPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[^1]));
        if (finalPipeline is not { Source: PhysicalCteRefNode cteRef, Filter: null } ||
            finalPipeline.PostOperations.Count != 0 ||
            !string.Equals(cteRef.CteName, producerCteName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var classifications = ClassifyMultiStatementCteReferences(multiStatement, indexes);
        if (!CanFuseFinalSideEffectApplyProjectionCte(producerCteName, classifications))
            return null;

        var producerPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[producerIndex]));
        if (producerPipeline == null ||
            producerPipeline.Filter != null ||
            producerPipeline.PostOperations.Count != 0 ||
            !CanInlineSideEffectApplyProjectionSource(producerPipeline.Source))
        {
            return null;
        }

        var rewrite = RewriteFinalJoinProjection(
            finalPipeline.Project,
            finalPipeline.Filter,
            producerPipeline.Project,
            cteRef);
        if (rewrite == null)
            return null;

        var prefix = BuildMultiStatementPrefix(
            multiStatement,
            producerIndex,
            indexes,
            scopeAggregateVariables,
            scope);
        if (!prefix.IsBuilt)
            return TableBuildResult.Unsupported(prefix.UnsupportedReason);

        var result = BuildTable(
            producerPipeline with
            {
                Project = rewrite.Project,
                Filter = rewrite.Filter
            },
            resultTableName,
            resultShapeName,
            indexes.CteIndexes,
            indexes.CteShapesByName,
            schemaFromIndex: DefaultSchemaFromIndex,
            scope: scope);
        if (!result.IsBuilt)
            return result;

        var tableIndex = ResolveStatementTableIndex(producerIndex, indexes);
        if (tableIndex < 0)
        {
            return TableBuildResult.Unsupported(
                $"Execution IR final APPLY projection fusion cannot resolve table storage slot for statement {producerIndex.ToString(CultureInfo.InvariantCulture)}.");
        }

        return TableBuildResult.Success(
            [..prefix.Shapes, ..result.Shapes],
            [..prefix.Nodes, CreateSingleUsePipelineFusionCandidate(tableIndex, result.Nodes)],
            result.Table,
            result.RowShape);
    }

    private CteDefinitionPrefixBuildResult BuildMultiStatementPrefix(
        PhysicalMultiStatementNode multiStatement,
        int exclusiveEndIndex,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        var shapes = new List<RowShape>();
        var nodes = new List<ExecutionNode>();
        var prefixSession = scope.DirectTableSink == null
            ? scope
            : scope.WithoutDirectTableSink();

        for (var index = 0; index < exclusiveEndIndex; index++)
        {
            var result = BuildStatementTable(
                multiStatement.Statements[index],
                CreateStatementTableName(indexes.StatementNamePrefix, index),
                CreateStatementShapeName(indexes.StatementNamePrefix, index),
                indexes,
                scopeAggregateVariables,
                prefixSession);
            if (!result.IsBuilt)
                return CteDefinitionPrefixBuildResult.Unsupported(result.UnsupportedReason);

            shapes.AddRange(result.Shapes);
            nodes.AddRange(result.Nodes);

            var tableIndex = ResolveStatementTableIndex(index, indexes);
            if (tableIndex < 0)
            {
                return CteDefinitionPrefixBuildResult.Unsupported(
                    $"Execution IR multi-statement prefix lowering cannot resolve table storage slot for statement {index.ToString(CultureInfo.InvariantCulture)}.");
            }

            var cteName = ResolveStatementCteName(index, indexes);
            if (!string.IsNullOrWhiteSpace(cteName))
                indexes.CteShapesByName[cteName] = result.RowShape;

            nodes.Add(new ExecutionStoreTable(result.Table, tableIndex));
        }

        return CteDefinitionPrefixBuildResult.Success(shapes, nodes);
    }

    private static bool CanFuseFinalSideEffectApplyProjectionCte(
        string cteName,
        IReadOnlyDictionary<string, CteReferenceClassification> classifications)
    {
        return classifications.TryGetValue(cteName, out var classification) &&
               classification.ReferenceCount == 1 &&
               !classification.Flags.HasFlag(CteOutputFlags.OrderSensitive) &&
               !classification.Flags.HasFlag(CteOutputFlags.Aggregate) &&
               !classification.Flags.HasFlag(CteOutputFlags.Window) &&
               !classification.Flags.HasFlag(CteOutputFlags.SetOperation);
    }

    private static bool CanInlineSideEffectApplyProjectionSource(PhysicalNode source)
    {
        return source switch
        {
            PhysicalNestedLoopApplyNode apply =>
                CanInlineSideEffectApplyProjectionSource(apply.Left) &&
                CanInlineSideEffectApplyProjectionSource(apply.Right),
            PhysicalSchemaScanNode { Arguments.Length: 0 } => true,
            PhysicalCteRefNode => true,
            PhysicalInterpretSourceNode => true,
            PhysicalPropertySourceNode => true,
            PhysicalAccessMethodSourceNode => true,
            _ => false
        };
    }
}
