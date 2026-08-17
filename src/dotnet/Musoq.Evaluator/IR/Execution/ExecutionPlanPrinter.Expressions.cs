using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;
public static partial class ExecutionPlanPrinter
{
    private static string FormatExpressionList(IReadOnlyList<ExecutionExpression> expressions) =>
        string.Join(", ", expressions.Select(FormatExpression));
    private static string FormatExpression(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead => string.IsNullOrEmpty(fieldRead.Alias)
                ? fieldRead.FieldName
                : $"{fieldRead.Alias}.{fieldRead.FieldName}",
            ExecutionMemberRead memberRead =>
                $"{FormatExpression(memberRead.Receiver)}.{memberRead.MemberName}",
            ExecutionScriptParameterRead parameterRead => $"${parameterRead.Name}",
            ExecutionScriptVariableRead variableRead => $"${variableRead.Name}",
            ExecutionLiteral literal => FormatLiteral(literal.Value.ToClrValue()),
            ExecutionBinary binary => $"({FormatExpression(binary.Left)} {FormatBinaryOperator(binary.Kind)} {FormatExpression(binary.Right)})",
            ExecutionUnary unary => FormatUnaryExpression(unary),
            ExecutionMethodCall method => FormatMethodCall(method),
            ExecutionStrictCast strictCast => $"{FormatExpression(strictCast.Expression)}::{strictCast.TargetTypeName}",
            ExecutionMethodTargetReuseCandidate candidate => FormatMethodTargetReuseCandidate(candidate),
            ExecutionArrayAccess arrayAccess => $"{FormatExpression(arrayAccess.Array)}[{FormatExpression(arrayAccess.Index)}]",
            ExecutionIndexedHashRowCreate indexedCreate => $"IndexedHashRow({indexedCreate.Row.Name}, {indexedCreate.Index.Name})",
            ExecutionIndexedHashRowRowRead rowRead => $"{rowRead.IndexedRow.Name}.Row",
            ExecutionIndexedHashRowIndexRead indexRead => $"{indexRead.IndexedRow.Name}.Index",
            ExecutionIsNullCheck isNull => FormatIsNullCheck(isNull),
            ExecutionRowPresence rowPresence => rowPresence.IsPresent
                ? $"{rowPresence.Alias} IS PRESENT"
                : $"{rowPresence.Alias} IS MISSING",
            ExecutionInCheck inCheck => FormatInCheck(inCheck),
            ExecutionCollectionInCheck collectionInCheck => FormatCollectionInCheck(collectionInCheck),
            ExecutionPatternMatch patternMatch => FormatPatternMatch(patternMatch),
            ExecutionBetween between => FormatBetween(between),
            ExecutionCaseWhen caseWhen => FormatCaseWhen(caseWhen),
            ExecutionCoalesce coalesce => FormatCoalesce(coalesce),
            ExecutionRowStream { Kind: ExecutionRowStreamKind.Chunks } rows => rows.Variable.Name,
            ExecutionRowStream { RowsAccess: ExecutionRowStreamRowsAccess.TableRows } rows => TryGetTypedRowBuffer(rows.Variable.Name, out _)
                ? rows.Variable.Name
                : $"{rows.Variable.Name}.Rows",
            ExecutionRowStream rows => rows.Variable.Name,
            ExecutionScalarRowStream rows => rows.Variable.Name,
            ExecutionStoredTable storedTable => FormatStoredTableRead(storedTable.TableIndex),
            ExecutionStoredTableRows storedRows => FormatStoredTableRowsRead(storedRows),
            ExecutionVariableRead variableRead => variableRead.Variable.Name,
            ExecutionRowContextsRead contextsRead => $"{contextsRead.Row.Name}.Contexts",
            ExecutionNullContextArray nullContextArray => $"new object[{nullContextArray.Count}]",
            ExecutionContextArray contextArray => $"contexts({string.Join(", ", contextArray.Segments.Select(static segment => FormatExpression(segment.Value)))})",
            ExecutionCompositeKey compositeKey => FormatCompositeKey(compositeKey),
            ExecutionValueTupleKey valueTupleKey => FormatTupleExpression(valueTupleKey.Parts),
            ExecutionWindowValueRead windowValueRead => $"{windowValueRead.Results.Name}[{windowValueRead.Index.Name}]",
            ExecutionAggregateCall aggregateCall => FormatAggregateCall(aggregateCall),
            ExecutionGroupKeyRead groupKeyRead => $"{groupKeyRead.Group.Name}.{groupKeyRead.KeyName}",
            ExecutionAggregateCapturedValueRead capturedValueRead => $"{capturedValueRead.Group.Name}.{capturedValueRead.CapturedField.FieldName}",
            ExecutionAggregateResultRef aggregateRef => aggregateRef.DisplayName ?? aggregateRef.Identifier,
            ExecutionWindowResultRef windowRef => $"window[{windowRef.WindowIndex}]",
            _ => $"UnknownExpression({expression.GetType().Name})"
        };
    }

    private static string FormatStoredTableRead(int tableIndex)
    {
        return TryGetTypedStoredTableSlot(tableIndex, out var generatedRowTypeName)
            ? $"{FormatCteRowResultSlot(tableIndex)}: {generatedRowTypeName}"
            : $"_tableResults[{tableIndex}]";
    }

    private static string FormatStoredTableRowsRead(ExecutionStoredTableRows storedRows)
    {
        if (TryGetTypedStoredTableSlot(storedRows.TableIndex, out _))
            return FormatCteRowResultSlot(storedRows.TableIndex);

        return storedRows.GeneratedRowShape == null
            ? $"_tableResults[{storedRows.TableIndex}].Rows"
            : $"CastGeneratedRows<{storedRows.GeneratedRowShape.TypeName}>(_tableResults[{storedRows.TableIndex}].Rows)";
    }

    private static string FormatUnaryExpression(ExecutionUnary unary)
    {
        var operand = FormatExpression(unary.Operand);

        return unary.Kind switch
        {
            UnaryOpKind.Not => $"NOT {operand}",
            UnaryOpKind.Negate => $"-{operand}",
            _ => $"?{operand}"
        };
    }

    private static string FormatMethodCall(ExecutionMethodCall method)
    {
        var builder = new StringBuilder();
        builder.Append(method.Method.MethodName);
        builder.Append('(');

        for (var index = 0; index < method.Arguments.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(FormatExpression(method.Arguments[index]));
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static string FormatMethodTargetReuseCandidate(ExecutionMethodTargetReuseCandidate candidate)
    {
        var method = candidate.MethodCall;
        var target = method.Target == null ? "none" : method.Target.Name;
        var cache = method.Cache == null ? string.Empty : $", cache {method.Cache.Name}";
        return $"Candidate({FormatMethodCall(method)} -> target {target}{cache})";
    }

    private static string FormatIsNullCheck(ExecutionIsNullCheck isNull)
    {
        var suffix = isNull.IsNegated ? "IS NOT NULL" : "IS NULL";
        return $"{FormatExpression(isNull.Expression)} {suffix}";
    }

    private static string FormatInCheck(ExecutionInCheck inCheck)
    {
        return $"{FormatExpression(inCheck.Expression)} IN ({FormatExpressionList(inCheck.Values)})";
    }

    private static string FormatPatternMatch(ExecutionPatternMatch patternMatch)
    {
        var keyword = patternMatch.Kind switch
        {
            PatternKind.Like => "LIKE",
            PatternKind.RLike => "RLIKE",
            _ => patternMatch.Kind.ToString()
        };

        return $"{FormatExpression(patternMatch.Expression)} {keyword} {FormatExpression(patternMatch.Pattern)}";
    }

    private static string FormatBetween(ExecutionBetween between)
    {
        return $"{FormatExpression(between.Expression)} BETWEEN {FormatExpression(between.Low)} AND {FormatExpression(between.High)}";
    }

    private static string FormatCaseWhen(ExecutionCaseWhen caseWhen)
    {
        var builder = new StringBuilder("CASE");

        foreach (var branch in caseWhen.Branches)
            builder.Append(System.Globalization.CultureInfo.InvariantCulture, $" WHEN {FormatExpression(branch.Condition)} THEN {FormatExpression(branch.Result)}");

        if (caseWhen.ElseExpression != null)
            builder.Append(System.Globalization.CultureInfo.InvariantCulture, $" ELSE {FormatExpression(caseWhen.ElseExpression)}");

        builder.Append(" END");
        return builder.ToString();
    }

    private static string FormatCoalesce(ExecutionCoalesce coalesce)
    {
        return $"COALESCE({FormatExpressionList(coalesce.Expressions)})";
    }

    private static string FormatLiteral(object? value)
    {
        return value switch
        {
            null => "NULL",
            string text => $"'{text}'",
            bool flag => flag ? "TRUE" : "FALSE",
            _ => value.ToString() ?? "NULL"
        };
    }

    private static string FormatBinaryOperator(BinaryOpKind kind)
    {
        return kind switch
        {
            BinaryOpKind.Add => "+",
            BinaryOpKind.Subtract => "-",
            BinaryOpKind.Multiply => "*",
            BinaryOpKind.Divide => "/",
            BinaryOpKind.Modulo => "%",
            BinaryOpKind.And => "AND",
            BinaryOpKind.Or => "OR",
            BinaryOpKind.Equal => "=",
            BinaryOpKind.NotEqual => "<>",
            BinaryOpKind.IsDistinctFrom => "IS DISTINCT FROM",
            BinaryOpKind.IsNotDistinctFrom => "IS NOT DISTINCT FROM",
            BinaryOpKind.GreaterThan => ">",
            BinaryOpKind.LessThan => "<",
            BinaryOpKind.GreaterOrEqual => ">=",
            BinaryOpKind.LessOrEqual => "<=",
            BinaryOpKind.BitwiseAnd => "&",
            BinaryOpKind.BitwiseOr => "|",
            BinaryOpKind.BitwiseXor => "^",
            BinaryOpKind.LeftShift => "<<",
            BinaryOpKind.RightShift => ">>",
            BinaryOpKind.StringConcatenate => "||",
            _ => "?"
        };
    }
}
