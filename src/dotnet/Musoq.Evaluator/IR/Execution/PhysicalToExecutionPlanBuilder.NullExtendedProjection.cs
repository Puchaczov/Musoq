using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static NullExtendedProjectionBuildResult CreateNullExtendedProjection(NullExtendedProjectionContext context)
    {
        var projectedValues = new List<NullExtendedProjectedValue>(context.Fields.Length);
        var matchedPresence = CreateAllPresentMap(context.SourceLookup);
        var unmatchedPresence = CreateNullExtendedPresenceMap(context.SourceLookup, context.NullAlias);

        foreach (var field in context.Fields)
        {
            var baseValue = ConvertProjectedExpression(field, context.SourceLookup);
            var matchedValue = SubstituteRowPresenceAliases(baseValue, matchedPresence);
            var unmatchedBaseValue = SubstituteRowPresenceAliases(baseValue, unmatchedPresence);

            var unmatched = SubstituteOuterApplyRightAlias(unmatchedBaseValue, context.NullAlias);
            if (!unmatched.Supported)
            {
                return NullExtendedProjectionBuildResult.Unsupported(
                    unmatched.UnsupportedReason);
            }

            var resultType = unmatched.IsUnknown
                ? LiftNullExtendedProjectionType(matchedValue.ReturnType)
                : ResolveNullExtendedProjectionType(matchedValue.ReturnType, unmatched.Expression.ReturnType);
            var isNullable = unmatched.IsUnknown || IsLiftedNullableResult(matchedValue.ReturnType, resultType);
            var unmatchedValue = unmatched.IsUnknown
                ? new ExecutionLiteral(null, resultType)
                : EnsureProjectionValueType(unmatched.Expression, resultType);
            matchedValue = EnsureProjectionValueType(matchedValue, resultType);

            projectedValues.Add(new NullExtendedProjectedValue(
                field.OutputName,
                field.OutputIndex,
                resultType,
                isNullable ? FieldNullability.Nullable : FieldNullability.Unknown,
                matchedValue,
                unmatchedValue));
        }

        var resultShape = CreateNullExtendedGeneratedShape(
            context.ResultShapeName,
            projectedValues,
            context.SourceLookup);
        var matchedAppendRow = CreateNullExtendedAppendRow(
            context.ResultTable,
            resultShape,
            projectedValues,
            static projectedValue => projectedValue.MatchedValue,
            CreateContextValues(context.SourceLookup),
            CreateContextLayout(context.SourceLookup));
        var unmatchedAppendRow = CreateNullExtendedAppendRow(
            context.ResultTable,
            resultShape,
            projectedValues,
            static projectedValue => projectedValue.UnmatchedValue,
            CreateContextValues(context.SourceLookup, context.NullAlias),
            CreateContextLayout(context.SourceLookup, context.NullAlias));

        return NullExtendedProjectionBuildResult.Success(resultShape, matchedAppendRow, unmatchedAppendRow);
    }

    private static GeneratedRowShape CreateNullExtendedGeneratedShape(
        string typeName,
        IReadOnlyList<NullExtendedProjectedValue> fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);

        return new GeneratedRowShape(
            typeName,
            fields.Select(field => new FieldBinding(
                field.OutputName,
                field.OutputName,
                field.OutputIndex,
                field.ResultType,
                field.Nullability,
                new GeneratedFieldAccess(CreateGeneratedFieldName(field.OutputName, field.OutputIndex, usedFieldNames)))).ToArray(),
            CreateContextBindings(sourceLookup));
    }

    private static FullOuterNullExtendedProjectionBuildResult CreateFullOuterNullExtendedProjection(
        NullExtendedProjectionContext context,
        string leftAlias,
        string rightAlias)
    {
        var projectedValues = new List<FullOuterNullExtendedProjectedValue>(context.Fields.Length);
        var matchedPresence = CreateAllPresentMap(context.SourceLookup);
        var leftOnlyPresence = CreateNullExtendedPresenceMap(context.SourceLookup, rightAlias);
        var rightOnlyPresence = CreateNullExtendedPresenceMap(context.SourceLookup, leftAlias);

        foreach (var field in context.Fields)
        {
            var baseValue = ConvertProjectedExpression(field, context.SourceLookup);
            var matchedValue = SubstituteRowPresenceAliases(baseValue, matchedPresence);
            var leftOnlyBaseValue = SubstituteRowPresenceAliases(baseValue, leftOnlyPresence);
            var rightOnlyBaseValue = SubstituteRowPresenceAliases(baseValue, rightOnlyPresence);
            var leftOnly = SubstituteOuterApplyRightAlias(leftOnlyBaseValue, rightAlias);
            if (!leftOnly.Supported)
                return FullOuterNullExtendedProjectionBuildResult.Unsupported(leftOnly.UnsupportedReason);

            var rightOnly = SubstituteOuterApplyRightAlias(rightOnlyBaseValue, leftAlias);
            if (!rightOnly.Supported)
                return FullOuterNullExtendedProjectionBuildResult.Unsupported(rightOnly.UnsupportedReason);

            var resultType = ResolveFullOuterProjectionType(
                matchedValue.ReturnType,
                leftOnly.IsUnknown ? null : leftOnly.Expression.ReturnType,
                rightOnly.IsUnknown ? null : rightOnly.Expression.ReturnType);
            var isNullable = leftOnly.IsUnknown ||
                             rightOnly.IsUnknown ||
                             IsLiftedNullableResult(matchedValue.ReturnType, resultType);
            var leftOnlyValue = leftOnly.IsUnknown
                ? new ExecutionLiteral(null, resultType)
                : EnsureProjectionValueType(leftOnly.Expression, resultType);
            var rightOnlyValue = rightOnly.IsUnknown
                ? new ExecutionLiteral(null, resultType)
                : EnsureProjectionValueType(rightOnly.Expression, resultType);
            matchedValue = EnsureProjectionValueType(matchedValue, resultType);

            projectedValues.Add(new FullOuterNullExtendedProjectedValue(
                field.OutputName,
                field.OutputIndex,
                resultType,
                isNullable ? FieldNullability.Nullable : FieldNullability.Unknown,
                matchedValue,
                leftOnlyValue,
                rightOnlyValue));
        }

        var resultShape = CreateFullOuterNullExtendedGeneratedShape(
            context.ResultShapeName,
            projectedValues,
            context.SourceLookup);
        var matchedAppendRow = CreateFullOuterNullExtendedAppendRow(
            context.ResultTable,
            resultShape,
            projectedValues,
            static projectedValue => projectedValue.MatchedValue,
            CreateContextValues(context.SourceLookup),
            CreateContextLayout(context.SourceLookup));
        var leftOnlyAppendRow = CreateFullOuterNullExtendedAppendRow(
            context.ResultTable,
            resultShape,
            projectedValues,
            static projectedValue => projectedValue.LeftOnlyValue,
            CreateContextValues(context.SourceLookup, rightAlias),
            CreateContextLayout(context.SourceLookup, rightAlias));
        var rightOnlyAppendRow = CreateFullOuterNullExtendedAppendRow(
            context.ResultTable,
            resultShape,
            projectedValues,
            static projectedValue => projectedValue.RightOnlyValue,
            CreateContextValues(context.SourceLookup, leftAlias),
            CreateContextLayout(context.SourceLookup, leftAlias));

        return FullOuterNullExtendedProjectionBuildResult.Success(
            resultShape,
            matchedAppendRow,
            leftOnlyAppendRow,
            rightOnlyAppendRow);
    }

    private static GeneratedRowShape CreateFullOuterNullExtendedGeneratedShape(
        string typeName,
        IReadOnlyList<FullOuterNullExtendedProjectedValue> fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);

        return new GeneratedRowShape(
            typeName,
            fields.Select(field => new FieldBinding(
                field.OutputName,
                field.OutputName,
                field.OutputIndex,
                field.ResultType,
                field.Nullability,
                new GeneratedFieldAccess(CreateGeneratedFieldName(field.OutputName, field.OutputIndex, usedFieldNames)))).ToArray(),
            CreateContextBindings(sourceLookup));
    }

    private static ExecutionAppendRow CreateFullOuterNullExtendedAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        IReadOnlyList<FullOuterNullExtendedProjectedValue> projectedValues,
        Func<FullOuterNullExtendedProjectedValue, ExecutionExpression> resolveValue,
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout)
    {
        return new ExecutionAppendRow(
            resultTable,
            resultShape,
            projectedValues
                .Select(projectedValue => new ExecutionRowValue(projectedValue.OutputName, resolveValue(projectedValue)))
                .ToArray(),
            contexts,
            SerialAppendMode,
            contextLayout);
    }

    private static ExecutionAppendRow CreateNullExtendedAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        IReadOnlyList<NullExtendedProjectedValue> projectedValues,
        Func<NullExtendedProjectedValue, ExecutionExpression> resolveValue,
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout)
    {
        return new ExecutionAppendRow(
            resultTable,
            resultShape,
            projectedValues
                .Select(projectedValue => new ExecutionRowValue(projectedValue.OutputName, resolveValue(projectedValue)))
                .ToArray(),
            contexts,
            SerialAppendMode,
            contextLayout);
    }

    private static Type LiftNullExtendedProjectionType(Type type)
    {
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
            return type;

        return typeof(Nullable<>).MakeGenericType(type);
    }

    private static Type ResolveFullOuterProjectionType(
        Type matchedType,
        Type? leftOnlyType,
        Type? rightOnlyType)
    {
        var resultType = matchedType;
        resultType = leftOnlyType == null
            ? LiftNullExtendedProjectionType(resultType)
            : ResolveNullExtendedProjectionType(resultType, leftOnlyType);
        resultType = rightOnlyType == null
            ? LiftNullExtendedProjectionType(resultType)
            : ResolveNullExtendedProjectionType(resultType, rightOnlyType);

        return resultType;
    }

    private static Type ResolveNullExtendedProjectionType(Type matchedType, Type unmatchedType)
    {
        if (matchedType == unmatchedType)
            return matchedType;

        if (Nullable.GetUnderlyingType(matchedType) == unmatchedType)
            return matchedType;

        if (Nullable.GetUnderlyingType(unmatchedType) == matchedType)
            return unmatchedType;

        return matchedType.IsValueType && Nullable.GetUnderlyingType(matchedType) == null
            ? LiftNullExtendedProjectionType(matchedType)
            : matchedType;
    }

    private static ExecutionExpression EnsureProjectionValueType(
        ExecutionExpression expression,
        Type resultType)
    {
        return expression.ReturnType == resultType
            ? expression
            : expression with { ReturnType = resultType };
    }

    private static bool IsLiftedNullableResult(Type originalType, Type resultType)
    {
        return Nullable.GetUnderlyingType(resultType) == originalType;
    }
}
