using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private enum NestedApplyGeneratedRowPreservation
    {
        Disabled,
        Enabled
    }

    private SourceBuildResult BuildApplySource(
        PhysicalNode source,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        string? sourceRowsScope)
    {
        if (source is PhysicalNestedLoopApplyNode apply)
            return BuildNestedApplySource(apply, cteIndexes, cteShapesByName, schemaFromIndex, sourceLookup);

        if (source is not (PhysicalSchemaScanNode or PhysicalCteRefNode or PhysicalInterpretSourceNode or PhysicalPropertySourceNode or PhysicalAccessMethodSourceNode or PhysicalValuesScanNode))
        {
            return SourceBuildResult.Unsupported(
                $"Execution IR apply lowering currently supports flat schema-scan, CTE-ref, interpret-source, property-source, access-method-source, values, or nested apply inputs. Found {source.GetType().Name}.");
        }

        if (source is PhysicalInterpretSourceNode interpret)
        {
            var interpretSource = ValidateInterpretSource(interpret);
            if (!interpretSource.Supported)
                return SourceBuildResult.Unsupported(interpretSource.UnsupportedReason);
        }

        var shape = ResolveSourceShape(source, cteIndexes, cteShapesByName);
        if (shape == null)
            return SourceBuildResult.Unsupported($"Execution IR apply lowering cannot resolve source shape for {source.GetType().Name}.");

        var variable = CreateSourceVariable(source, shape, cteShapesByName);
        var setup = CreateSourceSetup(source, shape, variable, schemaFromIndex, sourceLookup, cteIndexes, sourceRowsScope);
        var rows = CreateSourceRowsExpression(source, shape, cteIndexes, cteShapesByName, sourceRowsScope);
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
        NestedApplyGeneratedRowPreservation generatedRowPreservation = NestedApplyGeneratedRowPreservation.Disabled)
    {
        var shouldPreserveGeneratedRows = generatedRowPreservation == NestedApplyGeneratedRowPreservation.Enabled;
        var projection = CreateNestedSourceProjectionFields(apply, cteIndexes, cteShapesByName);
        if (!projection.Supported)
            return SourceBuildResult.Unsupported(projection.UnsupportedReason);

        var sourceAlias = CreateNestedApplySourceAlias(apply, schemaFromIndex);
        var tableName = $"{sourceAlias}Table";
        var shapeName = CreateNestedSourceShapeName(sourceAlias);
        var project = new PhysicalProjectNode(projection.Value, apply);
        var pipeline = new SupportedPipeline(project, apply, null, []);
        var table = BuildApplyTable(
            apply,
            pipeline,
            tableName,
            shapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            sourceLookup);

        if (!table.Supported)
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
        string rightAlias)
    {
        var matchedAppendBlock = CreateOuterJoinMatchedAppendBlock(filter, matchedAppendRow, sourceLookup);
        if (filter == null)
            return OuterApplyFilterBuildResult.Success(matchedAppendBlock, CreateAppendBlock(unmatchedAppendRow));

        var condition = SubstituteRowPresenceAliases(
            ExecutionExpressionConverter.Convert(filter.Predicate, sourceLookup),
            CreateNullExtendedPresenceMap(sourceLookup, rightAlias));
        var referencesRight = ReferencesExecutionAlias(condition, rightAlias) ||
                              ReferencesAlias(filter.Predicate, rightAlias);
        if (!referencesRight)
        {
            return OuterApplyFilterBuildResult.Success(
                matchedAppendBlock,
                CreateFilteredAppendBlock(condition, unmatchedAppendRow));
        }

        var unmatchedCondition = CreateUnmatchedOuterApplyFilterCondition(condition, rightAlias);
        if (!unmatchedCondition.Supported)
            return OuterApplyFilterBuildResult.Unsupported(unmatchedCondition.UnsupportedReason);

        return OuterApplyFilterBuildResult.Success(
            matchedAppendBlock,
            CreateFilteredAppendBlock(unmatchedCondition.Value, unmatchedAppendRow));
    }

    private static BuildResult<ExecutionExpression> CreateUnmatchedOuterApplyFilterCondition(
        ExecutionExpression condition,
        string rightAlias)
    {
        var substituted = SubstituteOuterApplyRightAlias(condition, rightAlias);
        if (!substituted.Supported)
            return BuildResult<ExecutionExpression>.Unsupported(substituted.UnsupportedReason);

        if (substituted.IsUnknown)
            return BuildResult<ExecutionExpression>.Success(new ExecutionLiteral(false, typeof(bool)));

        var normalized = CreateOuterApplyBooleanCondition(substituted.Expression, rightAlias);
        return normalized.Supported
            ? BuildResult<ExecutionExpression>.Success(normalized.Value)
            : BuildResult<ExecutionExpression>.Unsupported(normalized.UnsupportedReason);
    }

    private ExecutionBlock CreateOuterJoinMatchedAppendBlock(
        PhysicalFilterNode? filter,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        if (filter == null)
            return CreateAppendBlock(appendRow);

        var condition = SubstituteRowPresenceAliases(
            ExecutionExpressionConverter.Convert(filter.Predicate, sourceLookup),
            CreateAllPresentMap(sourceLookup));

        return CreateFilteredAppendBlock(condition, appendRow);
    }

    private static BuildResult<ExecutionExpression> CreateOuterApplyBooleanCondition(
        ExecutionExpression expression,
        string rightAlias)
    {
        var normalized = NormalizeOuterApplyBooleanOperand(expression);
        if (normalized.Supported)
            return normalized;

        return BuildResult<ExecutionExpression>.Unsupported(
            $"Execution IR outer apply lowering produced non-boolean unmatched filter expression for right apply alias '{rightAlias}'.");
    }

    private static BuildResult<ExecutionExpression> NormalizeOuterApplyBooleanOperand(ExecutionExpression expression)
    {
        if (expression.ReturnType == typeof(bool))
            return BuildResult<ExecutionExpression>.Success(expression);

        if (Nullable.GetUnderlyingType(expression.ReturnType) == typeof(bool))
        {
            return BuildResult<ExecutionExpression>.Success(new ExecutionBinary(
                BinaryOpKind.Equal,
                expression,
                new ExecutionLiteral(true, typeof(bool)),
                typeof(bool)));
        }

        return BuildResult<ExecutionExpression>.Unsupported(
            $"Execution IR outer apply lowering expected a boolean expression but found {FormatTypeName(expression.ReturnType)}.");
    }

    private static ExecutionLiteral CreateOuterApplyNullLiteral(Type returnType)
    {
        return new ExecutionLiteral(null, LiftOuterApplyNullSubstitutionType(returnType));
    }

    private static Type LiftOuterApplyNullSubstitutionType(Type type)
    {
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
            return type;

        return typeof(Nullable<>).MakeGenericType(type);
    }

    private static string FormatTypeName(Type type)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        return nullableUnderlying == null
            ? type.Name
            : $"{nullableUnderlying.Name}?";
    }

}
