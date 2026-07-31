using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool ContainsRowScopedRead(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionFieldRead => true,
            ExecutionWindowValueRead => true,
            ExecutionAggregateCall => true,
            ExecutionGroupKeyRead => true,
            ExecutionBinary binary => ContainsRowScopedRead(binary.Left) || ContainsRowScopedRead(binary.Right),
            ExecutionUnary unary => ContainsRowScopedRead(unary.Operand),
            ExecutionStrictCast strictCast => ContainsRowScopedRead(strictCast.Expression),
            ExecutionMethodCall method => method.Arguments.Any(ContainsRowScopedRead) ||
                                          (method.InjectedSource != null &&
                                           ContainsRowScopedRead(method.InjectedSource)),
            ExecutionIsNullCheck isNull => ContainsRowScopedRead(isNull.Expression),
            ExecutionInCheck inCheck => ContainsRowScopedRead(inCheck.Expression) ||
                                        inCheck.Values.Any(ContainsRowScopedRead),
            ExecutionPatternMatch patternMatch => ContainsRowScopedRead(patternMatch.Expression) ||
                                                  ContainsRowScopedRead(patternMatch.Pattern),
            ExecutionBetween between => ContainsRowScopedRead(between.Expression) ||
                                        ContainsRowScopedRead(between.Low) ||
                                        ContainsRowScopedRead(between.High),
            ExecutionCaseWhen caseWhen => caseWhen.Branches.Any(branch =>
                                                 ContainsRowScopedRead(branch.Condition) ||
                                                 ContainsRowScopedRead(branch.Result)) ||
                                             (caseWhen.ElseExpression != null &&
                                              ContainsRowScopedRead(caseWhen.ElseExpression)),
            ExecutionCoalesce coalesce => coalesce.Expressions.Any(ContainsRowScopedRead),
            ExecutionCompositeKey compositeKey => compositeKey.Parts.Any(ContainsRowScopedRead),
            ExecutionValueTupleKey valueTupleKey => valueTupleKey.Parts.Any(ContainsRowScopedRead),
            _ => false
        };
    }

    private static ExecutionRankingWindowFunction? ResolveRankingFunction(string functionName)
    {
        var normalized = functionName.Replace("_", string.Empty, StringComparison.Ordinal);
        if (string.Equals(normalized, "RowNumber", StringComparison.OrdinalIgnoreCase))
            return ExecutionRankingWindowFunction.RowNumber;

        if (string.Equals(normalized, "Rank", StringComparison.OrdinalIgnoreCase))
            return ExecutionRankingWindowFunction.Rank;

        if (string.Equals(normalized, "DenseRank", StringComparison.OrdinalIgnoreCase))
            return ExecutionRankingWindowFunction.DenseRank;

        return null;
    }

    private static ExecutionOffsetWindowFunction? ResolveOffsetFunction(string functionName)
    {
        var normalized = functionName.Replace("_", string.Empty, StringComparison.Ordinal);
        if (string.Equals(normalized, "Lag", StringComparison.OrdinalIgnoreCase))
            return ExecutionOffsetWindowFunction.Lag;

        if (string.Equals(normalized, "Lead", StringComparison.OrdinalIgnoreCase))
            return ExecutionOffsetWindowFunction.Lead;

        return null;
    }

    private static bool IsNtileWindowFunction(string functionName)
    {
        var normalized = NormalizeWindowFunctionName(functionName);
        return string.Equals(normalized, "Ntile", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBuiltInValueAccessWindowFunction(string functionName)
    {
        var normalized = NormalizeWindowFunctionName(functionName).ToUpperInvariant();
        return normalized is "FIRSTVALUE" or "LASTVALUE" or "NTHVALUE";
    }

    private static int? TryGetBuiltInPluginWindowArgumentCount(string functionName)
    {
        var normalized = NormalizeWindowFunctionName(functionName).ToUpperInvariant();
        return normalized switch
        {
            "SUM" or "COUNT" or "AVG" or "MIN" or "MAX" => 1,
            "FIRSTVALUE" or "LASTVALUE" => 1,
            "NTHVALUE" => 2,
            _ => null
        };
    }

    private static string CreateRankingResultVariableName(
        string resultTableName,
        ExecutionRankingWindowFunction function,
        int windowIndex,
        WindowResultNameMode mode)
    {
        var name = function switch
        {
            ExecutionRankingWindowFunction.RowNumber => $"{resultTableName}RowNumbers",
            ExecutionRankingWindowFunction.Rank => $"{resultTableName}Ranks",
            ExecutionRankingWindowFunction.DenseRank => $"{resultTableName}DenseRanks",
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
        };

        return FormatWindowResultVariableName(name, windowIndex, mode);
    }

    private static string CreateOffsetResultVariableName(
        string resultTableName,
        ExecutionOffsetWindowFunction function,
        int windowIndex,
        WindowResultNameMode mode)
    {
        var name = function switch
        {
            ExecutionOffsetWindowFunction.Lag => $"{resultTableName}Lags",
            ExecutionOffsetWindowFunction.Lead => $"{resultTableName}Leads",
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
        };

        return FormatWindowResultVariableName(name, windowIndex, mode);
    }

    private static string CreatePluginResultVariableName(
        string resultTableName,
        string functionName,
        int windowIndex,
        WindowResultNameMode mode)
    {
        var name = $"{resultTableName}{NormalizeWindowFunctionName(functionName)}s";
        return FormatWindowResultVariableName(name, windowIndex, mode);
    }

    private static string NormalizeWindowFunctionName(string functionName)
    {
        return functionName.Replace("_", string.Empty, StringComparison.Ordinal);
    }

    private static string FormatWindowResultVariableName(string name, int windowIndex, WindowResultNameMode mode)
    {
        return mode switch
        {
            WindowResultNameMode.Standard => name,
            WindowResultNameMode.IndexedByWindow => $"{name}{windowIndex.ToString(CultureInfo.InvariantCulture)}",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static LoweringAttempt<OptionalValue<ExecutionExpression>> CreateWindowPartitionKey(
        IrExpression[] partitionKeys,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields)
    {
        if (partitionKeys.Length == 0)
            return LoweringAttempt<OptionalValue<ExecutionExpression>>.Built(OptionalValue<ExecutionExpression>.None());

        var parts = new List<ExecutionExpression>(partitionKeys.Length);
        foreach (var partitionKey in partitionKeys)
        {
            var expression = ConvertWindowInputExpression(partitionKey, sourceLookup, aggregateSourceFields);
            parts.Add(expression);
        }

        if (parts.Count == 1)
            return LoweringAttempt<OptionalValue<ExecutionExpression>>.Built(OptionalValue<ExecutionExpression>.Some(parts[0]));

        var partTypes = parts.Select(part => part.ReturnType.ResolveClrType()).ToArray();
        if (partTypes.Length is >= 2 and <= 7 &&
            partTypes.All(WindowRegistrationLoweringHelpers.CanUseTypedWindowKeyElement) &&
            ValueTupleTypeShape.TryCreate(partTypes, out var partitionKeyType))
        {
            return LoweringAttempt<OptionalValue<ExecutionExpression>>.Built(
                OptionalValue<ExecutionExpression>.Some(new ExecutionValueTupleKey(parts, partitionKeyType)));
        }

        return LoweringAttempt<OptionalValue<ExecutionExpression>>.Built(
            OptionalValue<ExecutionExpression>.Some(new ExecutionCompositeKey(parts)));
    }

    private static LoweringAttempt<IReadOnlyList<ExecutionWindowOrderKey>> CreateWindowOrderKeys(
        OrderField[] orderKeys,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields)
    {
        var keys = new List<ExecutionWindowOrderKey>(orderKeys.Length);
        foreach (var orderKey in orderKeys)
        {
            var expression = ConvertWindowInputExpression(orderKey.Expression, sourceLookup, aggregateSourceFields);
            keys.Add(new ExecutionWindowOrderKey(expression, orderKey.Descending, orderKey.NullOrdering));
        }

        return LoweringAttempt<IReadOnlyList<ExecutionWindowOrderKey>>.Built(keys);
    }

}
