using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionBlock CreateSidecarJoinRuntimeBody(
        IReadOnlyList<SidecarJoinRuntimeOperation> operations,
        RowShape baseShape,
        ExecutionBlock continuation)
    {
        return new SidecarJoinRuntimePlanner(
                CreateSidecarJoinStepBlock,
                static (guard, body) => CreateConditionalJoinBlock(guard.Predicate, guard.SourceLookup, body))
            .CreateRuntimeBody(operations, baseShape, continuation);
    }

    private static IReadOnlySet<string> CreateSidecarJoinIntroducedAliases(
        CteSidecarIndexSpec sidecar,
        RowShape buildShape)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (sidecar.Kind == CteSidecarIndexKind.Hash)
            AddSourceAlias(aliases, buildShape);

        return aliases;
    }

    private static IReadOnlySet<string> CreateSidecarJoinRequiredAliases(
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlySet<string> aliasesIntroducedByOperation,
        params IrExpression?[] expressions)
    {
        if (!TryCollectSidecarJoinRequiredAliases(sourceLookup, expressions, out var aliases))
            return new HashSet<string>(sourceLookup.Keys, StringComparer.OrdinalIgnoreCase);

        aliases.ExceptWith(aliasesIntroducedByOperation);
        return aliases;
    }

    private static bool TryCreateSidecarJoinRuntimeGuard(
        IrExpression predicate,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        int ordinal,
        [NotNullWhen(true)] out SidecarJoinRuntimeGuard? guard)
    {
        if (TryCollectSidecarJoinRequiredAliases(sourceLookup, [predicate], out var requiredAliases))
        {
            guard = new SidecarJoinRuntimeGuard(predicate, sourceLookup, requiredAliases, ordinal);
            return true;
        }

        guard = null;
        return false;
    }

    private static bool TryCollectSidecarJoinRequiredAliases(
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IEnumerable<IrExpression?> expressions,
        out HashSet<string> aliases)
    {
        aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var expression in expressions)
        {
            if (expression == null)
                continue;

            var executionExpression = ExecutionExpressionConverter.Convert(expression, sourceLookup);
            if (!TryCollectExecutionExpressionAliases(executionExpression, aliases))
                return false;
        }

        return true;
    }

    private static bool TryCollectExecutionExpressionAliases(
        ExecutionExpression expression,
        ISet<string> aliases)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead => TryAddFieldAlias(fieldRead, aliases),
            ExecutionBinary binary => TryCollectExecutionExpressionAliases(binary.Left, aliases) &&
                                      TryCollectExecutionExpressionAliases(binary.Right, aliases),
            ExecutionUnary unary => TryCollectExecutionExpressionAliases(unary.Operand, aliases),
            ExecutionStrictCast strictCast => TryCollectExecutionExpressionAliases(strictCast.Expression, aliases),
            ExecutionMethodCall method => method.Arguments.All(argument => TryCollectExecutionExpressionAliases(argument, aliases)) &&
                                          (method.InjectedSource == null || TryCollectExecutionExpressionAliases(method.InjectedSource, aliases)),
            ExecutionArrayAccess array => TryCollectExecutionExpressionAliases(array.Array, aliases) &&
                                          TryCollectExecutionExpressionAliases(array.Index, aliases),
            ExecutionIsNullCheck isNull => TryCollectExecutionExpressionAliases(isNull.Expression, aliases),
            ExecutionRowPresence rowPresence => TryAddRowPresenceAlias(rowPresence, aliases) &&
                                                TryCollectExecutionExpressionAliases(rowPresence.PresenceSource, aliases),
            ExecutionInCheck inCheck => TryCollectExecutionExpressionAliases(inCheck.Expression, aliases) &&
                                        inCheck.Values.All(value => TryCollectExecutionExpressionAliases(value, aliases)),
            ExecutionPatternMatch pattern => TryCollectExecutionExpressionAliases(pattern.Expression, aliases) &&
                                             TryCollectExecutionExpressionAliases(pattern.Pattern, aliases),
            ExecutionBetween between => TryCollectExecutionExpressionAliases(between.Expression, aliases) &&
                                        TryCollectExecutionExpressionAliases(between.Low, aliases) &&
                                        TryCollectExecutionExpressionAliases(between.High, aliases),
            ExecutionCaseWhen caseWhen => caseWhen.Branches.All(branch => TryCollectExecutionExpressionAliases(branch.Condition, aliases) &&
                                                                          TryCollectExecutionExpressionAliases(branch.Result, aliases)) &&
                                          (caseWhen.ElseExpression == null || TryCollectExecutionExpressionAliases(caseWhen.ElseExpression, aliases)),
            ExecutionCoalesce coalesce => coalesce.Expressions.All(expression => TryCollectExecutionExpressionAliases(expression, aliases)),
            ExecutionCompositeKey key => key.Parts.All(part => TryCollectExecutionExpressionAliases(part, aliases)),
            ExecutionValueTupleKey key => key.Parts.All(part => TryCollectExecutionExpressionAliases(part, aliases)),
            ExecutionAggregateCall aggregate => aggregate.Arguments.All(argument => TryCollectExecutionExpressionAliases(argument, aliases)),
            _ => true
        };
    }

    private static bool TryAddFieldAlias(ExecutionFieldRead fieldRead, ISet<string> aliases)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            return false;

        aliases.Add(fieldRead.Alias);
        return true;
    }

    private static bool TryAddRowPresenceAlias(ExecutionRowPresence rowPresence, ISet<string> aliases)
    {
        if (string.IsNullOrWhiteSpace(rowPresence.Alias))
            return false;

        aliases.Add(rowPresence.Alias);
        return true;
    }

    private static void AddSourceAlias(ISet<string> aliases, RowShape shape)
    {
        if (RowShapeLookup.TryResolveSourceAlias(shape, out var alias))
            aliases.Add(alias);
    }
}
