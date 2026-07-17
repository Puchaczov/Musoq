using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record InterpretSourceValidationResult(
    bool Supported,
    string UnsupportedReason)
{
    public static InterpretSourceValidationResult Success()
    {
        return new InterpretSourceValidationResult(true, string.Empty);
    }

    public static InterpretSourceValidationResult Unsupported(string reason)
    {
        return new InterpretSourceValidationResult(false, reason);
    }
}

internal sealed record SourceBuildResult(
    bool Supported,
    JoinSource Source,
    string UnsupportedReason)
{
    public static SourceBuildResult Success(JoinSource source)
    {
        return new SourceBuildResult(true, source, string.Empty);
    }

    public static SourceBuildResult Unsupported(string reason)
    {
        return new SourceBuildResult(
            false,
            new JoinSource(
                new PhysicalMultiStatementNode([]),
                new GeneratedRowShape(string.Empty, []),
                new ExecutionVariable(string.Empty, typeof(object)),
                [],
                new ExecutionVariableRead(new ExecutionVariable(string.Empty, typeof(object))),
                [],
                0),
            reason);
    }
}

internal sealed record JoinSourcesBuildResult(
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

internal sealed record FilteredSource(PhysicalNode Source, IrExpression Predicate);

internal sealed record HashJoinTableLowering(
    ExecutionVariable ResultTable,
    GeneratedRowShape ResultShape,
    ExecutionBlock MatchedBody,
    ExecutionBlock? NoMatchBody = null,
    ExecutionVariable? HasMatch = null,
    IReadOnlyList<ExecutionNode>? PreludeNodes = null);

internal sealed record OuterNestedLoopSides(JoinSource Outer, JoinSource Inner);

internal readonly record struct JoinKeyExpressions(
    IrExpression Left,
    IrExpression Right);

internal readonly record struct AsOfJoinPredicateParts(
    JoinKeyExpressions[] EqualityKeys,
    IrExpression LeftInequalityKey,
    IrExpression RightInequalityKey,
    BinaryOpKind ComparisonKind);

internal readonly record struct NormalizedAsOfJoinKey(
    IrExpression Left,
    IrExpression Right,
    BinaryOpKind Kind);

internal sealed record AsOfProbeBuildResult(
    bool Supported,
    GeneratedRowShape ResultShape,
    ExecutionAsOfProbe Probe,
    string UnsupportedReason)
{
    public static AsOfProbeBuildResult Success(
        GeneratedRowShape resultShape,
        ExecutionAsOfProbe probe)
    {
        return new AsOfProbeBuildResult(true, resultShape, probe, string.Empty);
    }

    public static AsOfProbeBuildResult Unsupported(string reason)
    {
        return new AsOfProbeBuildResult(
            false,
            new GeneratedRowShape(string.Empty, []),
            new ExecutionAsOfProbe(
                new ExecutionVariable(string.Empty, typeof(object)),
                new ExecutionVariable(string.Empty, typeof(object)),
                new ExecutionVariableRead(new ExecutionVariable(string.Empty, typeof(object))),
                [],
                new ExecutionLiteral(null, typeof(object)),
                new ExecutionLiteral(null, typeof(object)),
                BinaryOpKind.Equal,
                ExecutionBlock.Empty,
                ComparisonKeyType: (ExecutionTypeRef?)null),
            reason);
    }
}
