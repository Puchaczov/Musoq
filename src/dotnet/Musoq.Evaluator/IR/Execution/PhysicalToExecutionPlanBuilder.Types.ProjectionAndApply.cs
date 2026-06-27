using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record ProjectionFieldCollectionResult(
        bool Supported,
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

    private sealed record NullExtendedProjectionContext(
        string ResultShapeName,
        ExecutionVariable ResultTable,
        ProjectedField[] Fields,
        IReadOnlyDictionary<string, RowShape> SourceLookup,
        string NullAlias);

    private sealed record NullExtendedProjectedValue(
        string OutputName,
        int OutputIndex,
        Type ResultType,
        FieldNullability Nullability,
        ExecutionExpression MatchedValue,
        ExecutionExpression UnmatchedValue);

    private sealed record NullExtendedProjectionBuildResult(
        bool Supported,
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

    private sealed record FullOuterNullExtendedProjectedValue(
        string OutputName,
        int OutputIndex,
        Type ResultType,
        FieldNullability Nullability,
        ExecutionExpression MatchedValue,
        ExecutionExpression LeftOnlyValue,
        ExecutionExpression RightOnlyValue);

    private sealed record FullOuterNullExtendedProjectionBuildResult(
        bool Supported,
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

    private sealed record OuterApplyFilterBuildResult(
        bool Supported,
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

    private sealed record OuterApplyNullSubstitutionResult(
        bool Supported,
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

    private sealed record OuterApplyArgumentSubstitutionResult(
        bool Supported,
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

    private sealed record OuterApplyCaseElseSubstitutionResult(
        bool Supported,
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

    private sealed record JoinSourcesBuildResult(
        bool Supported,
        JoinSources Source,
        string UnsupportedReason)
    {
        public static JoinSourcesBuildResult Success(JoinSource left, JoinSource right)
        {
            return new JoinSourcesBuildResult(true, new JoinSources(left, right), string.Empty);
        }

        public static JoinSourcesBuildResult Unsupported(string reason)
        {
            var empty = SourceBuildResult.Unsupported(reason).Source;
            return new JoinSourcesBuildResult(false, new JoinSources(empty, empty), reason);
        }
    }
}
