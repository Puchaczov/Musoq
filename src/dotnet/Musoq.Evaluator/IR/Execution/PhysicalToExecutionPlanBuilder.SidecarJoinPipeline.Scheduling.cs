using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;
using AliasRefExtractor = Musoq.Evaluator.IR.Expressions.AliasRefExtractor;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionBlock CreateSidecarJoinRuntimeBody(
        IReadOnlyList<SidecarJoinRuntimeOperation> operations,
        RowShape baseShape,
        ExecutionBlock continuation)
    {
        var scheduled = TryScheduleSidecarJoinRuntimeOperations(operations, baseShape) ?? operations;
        var body = continuation;

        for (var index = scheduled.Count - 1; index >= 0; index--)
            body = CreateSidecarJoinOperationBlock(scheduled[index], body);

        return body;
    }

    private static IReadOnlyList<SidecarJoinRuntimeOperation>? TryScheduleSidecarJoinRuntimeOperations(
        IReadOnlyList<SidecarJoinRuntimeOperation> operations,
        RowShape baseShape)
    {
        if (operations.Count < 2)
            return operations;

        var activeAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSourceAlias(activeAliases, baseShape);
        var remaining = operations.ToList();
        var scheduled = new List<SidecarJoinRuntimeOperation>(operations.Count);

        while (remaining.Count > 0)
        {
            var candidateIndex = FindNextSidecarJoinOperationIndex(remaining, activeAliases);
            if (candidateIndex < 0)
                return null;

            var operation = remaining[candidateIndex];
            remaining.RemoveAt(candidateIndex);
            scheduled.Add(operation);

            if (operation is not SidecarJoinRuntimeStep step)
                continue;

            foreach (var alias in step.IntroducedAliases)
                activeAliases.Add(alias);
        }

        return scheduled;
    }

    private static int FindNextSidecarJoinOperationIndex(
        IReadOnlyList<SidecarJoinRuntimeOperation> operations,
        IReadOnlySet<string> activeAliases)
    {
        var firstReadyIndex = -1;

        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            if (!operation.RequiredAliases.All(activeAliases.Contains))
                continue;

            firstReadyIndex = firstReadyIndex < 0 ? index : firstReadyIndex;
            if (CanHoistSidecarJoinOperation(operation))
                return index;
        }

        return firstReadyIndex;
    }

    private static bool CanHoistSidecarJoinOperation(SidecarJoinRuntimeOperation operation)
    {
        return operation switch
        {
            SidecarJoinRuntimeGuard => true,
            SidecarJoinRuntimeStep { Sidecar.Kind: CteSidecarIndexKind.KeySet, Residual: null, Filter: null } => true,
            _ => false
        };
    }

    private static ExecutionBlock CreateSidecarJoinOperationBlock(
        SidecarJoinRuntimeOperation operation,
        ExecutionBlock continuation)
    {
        return operation switch
        {
            SidecarJoinRuntimeStep step => CreateSidecarJoinStepBlock(step, continuation),
            SidecarJoinRuntimeGuard guard => CreateConditionalJoinBlock(guard.Predicate, guard.SourceLookup, continuation),
            _ => throw new InvalidOperationException($"Sidecar join runtime operation '{operation.GetType().Name}' is not supported.")
        };
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
            ExecutionRawExpression raw => TryCollectRawExpressionAliases(raw.Expression, aliases),
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

    private static bool TryCollectRawExpressionAliases(IrExpression expression, ISet<string> aliases)
    {
        foreach (var alias in AliasRefExtractor.Extract(expression))
        {
            if (string.IsNullOrWhiteSpace(alias))
                return false;

            aliases.Add(alias);
        }

        return true;
    }

    private static void AddSourceAlias(ISet<string> aliases, RowShape shape)
    {
        if (RowShapeLookup.TryResolveSourceAlias(shape, out var alias))
            aliases.Add(alias);
    }
}
