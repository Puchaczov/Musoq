using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private SourceBuildResult BuildApplySource(
        PhysicalNode source,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        string? sourceRowsScope,
        LoweringScope scope)
    {
        if (source is PhysicalNestedLoopApplyNode apply)
            return BuildNestedApplySource(apply, cteIndexes, cteShapesByName, schemaFromIndex, sourceLookup, scope);

        if (source is not (PhysicalSchemaScanNode or PhysicalCteRefNode or PhysicalInterpretSourceNode or PhysicalPropertySourceNode or PhysicalAccessMethodSourceNode or PhysicalValuesScanNode))
        {
            return SourceBuildResult.Unsupported(
                $"Execution IR apply lowering currently supports flat schema-scan, CTE-ref, interpret-source, property-source, access-method-source, values, or nested apply inputs. Found {source.GetType().Name}.");
        }

        if (source is PhysicalInterpretSourceNode interpret)
        {
            var interpretSource = ValidateInterpretSource(interpret);
            if (!interpretSource.IsBuilt)
                return SourceBuildResult.Unsupported(interpretSource.UnsupportedReason);
        }

        var shape = ResolveSourceShape(source, cteIndexes, cteShapesByName);
        if (shape == null)
            return SourceBuildResult.Unsupported($"Execution IR apply lowering cannot resolve source shape for {source.GetType().Name}.");

        var variable = CreateSourceVariable(source, shape, cteShapesByName);
        var setup = CreateSourceSetup(source, shape, variable, schemaFromIndex, sourceLookup, cteIndexes, sourceRowsScope);
        var rows = CreateSourceRowsExpression(
            source,
            shape,
            cteIndexes,
            cteShapesByName,
            sourceRowsScope,
            scope);
        var schemaSourceCount = source is PhysicalSchemaScanNode ? 1 : 0;
        var canReuseSetupAcrossApplyRows = CanReuseSourceSetupAcrossApplyRows(source);

        return SourceBuildResult.Success(new JoinSource(
            source,
            shape,
            variable,
            setup,
            rows,
            [shape],
            schemaSourceCount,
            canReuseSetupAcrossApplyRows));
    }

    private SourceBuildResult BuildNestedApplySource(
        PhysicalNestedLoopApplyNode apply,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        LoweringScope scope,
        NestedApplyGeneratedRowPreservation generatedRowPreservation = NestedApplyGeneratedRowPreservation.Disabled)
    {
        var shouldPreserveGeneratedRows = generatedRowPreservation == NestedApplyGeneratedRowPreservation.Enabled;
        var projection = CreateNestedSourceProjectionFields(apply, cteIndexes, cteShapesByName);
        if (!projection.IsBuilt)
            return SourceBuildResult.Unsupported(projection.UnsupportedReason);

        var sourceAlias = CreateNestedApplySourceAlias(apply, schemaFromIndex);
        var tableName = $"{sourceAlias}Table";
        var shapeName = CreateNestedSourceShapeName(sourceAlias);
        var project = new PhysicalProjectNode(projection.Value, apply);
        var pipeline = new CteSupportedPipeline(project, apply, null, []);
        var table = BuildApplyTable(
            apply,
            pipeline,
            tableName,
            shapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            inheritedSourceLookup: sourceLookup,
            scope: scope);

        if (!table.IsBuilt)
            return SourceBuildResult.Unsupported(table.UnsupportedReason);

        var transitionShape = shouldPreserveGeneratedRows
            ? CreateTypedMaterializedTransitionTableRowShape(sourceAlias, table.RowShape)
            : CreateMaterializedTransitionTableRowShape(sourceAlias, table.RowShape);
        var source = shouldPreserveGeneratedRows
            ? new ExecutionVariable(sourceAlias, typeof(Row), table.RowShape.TypeName)
            : new ExecutionVariable(sourceAlias, typeof(Row));
        var rows = new ExecutionRowStream(
            table.Table,
            ExecutionRowStreamKind.Rows,
            ExecutionRowStreamRowsAccess.TableRows);
        var shapes = table.Shapes.Concat([transitionShape]).ToArray();

        return SourceBuildResult.Success(new JoinSource(
            apply,
            transitionShape,
            source,
            table.Nodes.ToList(),
            rows,
            shapes,
            CountSchemaScans(apply),
                GeneratedRowShape: shouldPreserveGeneratedRows ? table.RowShape : null));
    }

    private static bool CanReuseSourceSetupAcrossApplyRows(PhysicalNode source)
    {
        return source is PhysicalValuesScanNode;
    }

    private OuterApplyFilterBuildResult CreateOuterApplyAppendBlocks(
        PhysicalFilterNode? filter,
        ExecutionAppendRow matchedAppendRow,
        ExecutionAppendRow unmatchedAppendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        string rightAlias,
        IDirectTableSink? directTableSink = null)
    {
        var matchedAppendBlock = CreateOuterJoinMatchedAppendBlock(
            filter,
            matchedAppendRow,
            sourceLookup,
            directTableSink);
        var unmatchedAppend = directTableSink?.CreateAppend(unmatchedAppendRow) ?? unmatchedAppendRow;
        if (filter == null)
            return OuterApplyFilterBuildResult.Success(
                matchedAppendBlock,
                new ExecutionBlock([unmatchedAppend]));

        var condition = SubstituteRowPresenceAliases(
            ExecutionExpressionConverter.Convert(filter.Predicate, sourceLookup),
            CreateNullExtendedPresenceMap(sourceLookup, rightAlias));
        var referencesRight = ReferencesExecutionAlias(condition, rightAlias) ||
                              ReferencesAlias(filter.Predicate, rightAlias);
        if (!referencesRight)
        {
            return OuterApplyFilterBuildResult.Success(
                matchedAppendBlock,
                CreateFilteredAppendBlock(condition, unmatchedAppend));
        }

        var unmatchedCondition = CreateUnmatchedOuterApplyFilterCondition(condition, rightAlias);
        if (!unmatchedCondition.IsBuilt)
            return OuterApplyFilterBuildResult.Unsupported(unmatchedCondition.UnsupportedReason);

        return OuterApplyFilterBuildResult.Success(
            matchedAppendBlock,
            CreateFilteredAppendBlock(unmatchedCondition.Value, unmatchedAppend));
    }

    private static LoweringAttempt<ExecutionExpression> CreateUnmatchedOuterApplyFilterCondition(
        ExecutionExpression condition,
        string rightAlias)
    {
        var substituted = SubstituteOuterApplyRightAlias(condition, rightAlias);
        if (!substituted.IsBuilt)
            return LoweringAttempt<ExecutionExpression>.Unsupported(substituted.UnsupportedReason);

        if (substituted.IsUnknown)
            return LoweringAttempt<ExecutionExpression>.Built(new ExecutionLiteral(false, typeof(bool)));

        var normalized = CreateOuterApplyBooleanCondition(substituted.Expression, rightAlias);
        return normalized.IsBuilt
            ? LoweringAttempt<ExecutionExpression>.Built(normalized.Value)
            : LoweringAttempt<ExecutionExpression>.Unsupported(normalized.UnsupportedReason);
    }

    private ExecutionBlock CreateOuterJoinMatchedAppendBlock(
        PhysicalFilterNode? filter,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IDirectTableSink? directTableSink = null)
    {
        var outputAppend = directTableSink?.CreateAppend(appendRow) ?? appendRow;
        if (filter == null)
            return new ExecutionBlock([outputAppend]);

        var condition = SubstituteRowPresenceAliases(
            ExecutionExpressionConverter.Convert(filter.Predicate, sourceLookup),
            CreateAllPresentMap(sourceLookup));

        return CreateFilteredAppendBlock(condition, outputAppend);
    }

    private static LoweringAttempt<ExecutionExpression> CreateOuterApplyBooleanCondition(
        ExecutionExpression expression,
        string rightAlias)
    {
        var normalized = OuterApplyNullSubstitutionService.NormalizeBooleanOperand(expression);
        if (normalized.IsBuilt)
            return normalized;

        return LoweringAttempt<ExecutionExpression>.Unsupported(
            $"Execution IR outer apply lowering produced non-boolean unmatched filter expression for right apply alias '{rightAlias}'.");
    }

    private static string FormatTypeName(Type type)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        return nullableUnderlying == null
            ? type.Name
            : $"{nullableUnderlying.Name}?";
    }

}
