using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution.Lowering.ProjectionAndApply;

internal sealed record ProjectionFieldCollectionResult(
    bool IsBuilt,
    string UnsupportedReason)
{
    public static ProjectionFieldCollectionResult Success()
    {
        return new ProjectionFieldCollectionResult(true, string.Empty);
    }

    public static ProjectionFieldCollectionResult Unsupported(string reason)
    {
        return new ProjectionFieldCollectionResult(false, reason);
    }
}

internal sealed record NullExtendedProjectionContext(
    string ResultShapeName,
    ExecutionVariable ResultTable,
    ProjectedField[] Fields,
    IReadOnlyDictionary<string, RowShape> SourceLookup,
    string NullAlias,
    IReadOnlyDictionary<string, ExecutionExpression>? NullAliasFieldDefaults = null);

internal sealed record NullExtendedProjectedValue(
    string OutputName,
    int OutputIndex,
    Type ResultType,
    FieldNullability Nullability,
    ExecutionExpression MatchedValue,
    ExecutionExpression UnmatchedValue);

internal sealed record NullExtendedProjectionBuildResult(
    bool IsBuilt,
    GeneratedRowShape ResultShape,
    ExecutionAppendRow MatchedAppendRow,
    ExecutionAppendRow UnmatchedAppendRow,
    string UnsupportedReason)
{
    public static NullExtendedProjectionBuildResult Success(
        GeneratedRowShape resultShape,
        ExecutionAppendRow matchedAppendRow,
        ExecutionAppendRow unmatchedAppendRow)
    {
        return new NullExtendedProjectionBuildResult(true, resultShape, matchedAppendRow, unmatchedAppendRow, string.Empty);
    }

    public static NullExtendedProjectionBuildResult Unsupported(string reason)
    {
        var emptyTable = new ExecutionVariable(string.Empty, typeof(object));
        var emptyShape = new GeneratedRowShape(string.Empty, []);
        var emptyAppendRow = new ExecutionAppendRow(emptyTable, emptyShape, []);

        return new NullExtendedProjectionBuildResult(false, emptyShape, emptyAppendRow, emptyAppendRow, reason);
    }
}

internal sealed record FullOuterNullExtendedProjectedValue(
    string OutputName,
    int OutputIndex,
    Type ResultType,
    FieldNullability Nullability,
    ExecutionExpression MatchedValue,
    ExecutionExpression LeftOnlyValue,
    ExecutionExpression RightOnlyValue);

internal sealed record FullOuterNullExtendedProjectionBuildResult(
    bool IsBuilt,
    GeneratedRowShape ResultShape,
    ExecutionAppendRow MatchedAppendRow,
    ExecutionAppendRow LeftOnlyAppendRow,
    ExecutionAppendRow RightOnlyAppendRow,
    string UnsupportedReason)
{
    public static FullOuterNullExtendedProjectionBuildResult Success(
        GeneratedRowShape resultShape,
        ExecutionAppendRow matchedAppendRow,
        ExecutionAppendRow leftOnlyAppendRow,
        ExecutionAppendRow rightOnlyAppendRow)
    {
        return new FullOuterNullExtendedProjectionBuildResult(
            true,
            resultShape,
            matchedAppendRow,
            leftOnlyAppendRow,
            rightOnlyAppendRow,
            string.Empty);
    }

    public static FullOuterNullExtendedProjectionBuildResult Unsupported(string reason)
    {
        var emptyTable = new ExecutionVariable(string.Empty, typeof(object));
        var emptyShape = new GeneratedRowShape(string.Empty, []);
        var emptyAppendRow = new ExecutionAppendRow(emptyTable, emptyShape, []);

        return new FullOuterNullExtendedProjectionBuildResult(false, emptyShape, emptyAppendRow, emptyAppendRow, emptyAppendRow, reason);
    }
}

internal sealed record OuterApplyFilterBuildResult(
    bool IsBuilt,
    ExecutionBlock MatchedAppendBlock,
    ExecutionBlock UnmatchedAppendBlock,
    string UnsupportedReason)
{
    public static OuterApplyFilterBuildResult Success(
        ExecutionBlock matchedAppendBlock,
        ExecutionBlock unmatchedAppendBlock)
    {
        return new OuterApplyFilterBuildResult(true, matchedAppendBlock, unmatchedAppendBlock, string.Empty);
    }

    public static OuterApplyFilterBuildResult Unsupported(string reason)
    {
        return new OuterApplyFilterBuildResult(false, ExecutionBlock.Empty, ExecutionBlock.Empty, reason);
    }
}

internal sealed record OuterApplyNullSubstitutionResult(
    bool IsBuilt,
    bool IsUnknown,
    ExecutionExpression Expression,
    string UnsupportedReason)
{
    public static OuterApplyNullSubstitutionResult Known(ExecutionExpression expression)
    {
        return new OuterApplyNullSubstitutionResult(true, false, expression, string.Empty);
    }

    public static OuterApplyNullSubstitutionResult Unknown()
    {
        return new OuterApplyNullSubstitutionResult(true, true, new ExecutionLiteral(false, typeof(bool)), string.Empty);
    }

    public static OuterApplyNullSubstitutionResult Unsupported(string reason)
    {
        return new OuterApplyNullSubstitutionResult(false, true, new ExecutionLiteral(false, typeof(bool)), reason);
    }
}

internal sealed record OuterApplyArgumentSubstitutionResult(
    bool IsBuilt,
    IReadOnlyList<ExecutionExpression> Expressions,
    bool HasUnknown,
    string UnsupportedReason)
{
    public static OuterApplyArgumentSubstitutionResult Success(
        IReadOnlyList<ExecutionExpression> expressions,
        bool hasUnknown)
    {
        return new OuterApplyArgumentSubstitutionResult(true, expressions, hasUnknown, string.Empty);
    }

    public static OuterApplyArgumentSubstitutionResult Unsupported(string reason)
    {
        return new OuterApplyArgumentSubstitutionResult(false, [], true, reason);
    }
}

internal sealed record OuterApplyCaseElseSubstitutionResult(
    bool IsBuilt,
    bool IsUnknown,
    ExecutionExpression? Expression,
    string UnsupportedReason)
{
    public static OuterApplyCaseElseSubstitutionResult Known(ExecutionExpression? expression)
    {
        return new OuterApplyCaseElseSubstitutionResult(true, false, expression, string.Empty);
    }

    public static OuterApplyCaseElseSubstitutionResult Unknown()
    {
        return new OuterApplyCaseElseSubstitutionResult(true, true, null, string.Empty);
    }

    public static OuterApplyCaseElseSubstitutionResult Unsupported(string reason)
    {
        return new OuterApplyCaseElseSubstitutionResult(false, true, null, reason);
    }
}
