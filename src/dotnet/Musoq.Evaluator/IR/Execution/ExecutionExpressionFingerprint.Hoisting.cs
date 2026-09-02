using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionExpressionFingerprint
{
    internal static string ForHoist(ExecutionExpression expression) =>
        expression switch
        {
            ExecutionFieldRead fieldRead => $"field:{fieldRead.Alias}:{fieldRead.FieldName}:{HoistType(fieldRead.ReturnType)}:{fieldRead.AccessStrategy}:stability={fieldRead.Stability}{(fieldRead.EnumType == null ? string.Empty : $":source={fieldRead.SourceReadType?.StableId ?? "-"}:enum={fieldRead.EnumType.Fingerprint}")}",
            ExecutionMemberRead memberRead => $"member:{memberRead.IsDynamic}:{memberRead.MemberName}:{ForHoist(memberRead.Receiver)}:{HoistType(memberRead.ReturnType)}:stable={Musoq.Evaluator.IR.Analysis.ExpressionStabilityAnalyzer.IsStable(memberRead)}",
            ExecutionLiteral literal => $"literal:{HoistType(literal.ReturnType)}:{literal.Value}",
            ExecutionBinary binary => $"binary:{binary.Kind}:{ForHoist(binary.Left)}:{ForHoist(binary.Right)}:{HoistType(binary.ReturnType)}",
            ExecutionUnary unary => $"unary:{unary.Kind}:{ForHoist(unary.Operand)}:{HoistType(unary.ReturnType)}",
            ExecutionMethodCall methodCall => HoistMethodCall(methodCall),
            ExecutionStrictCast strictCast => $"cast:{ForHoist(strictCast.Expression)}:{strictCast.TargetTypeName}:{HoistType(strictCast.ReturnType)}",
            ExecutionMethodTargetReuseCandidate candidate => HoistMethodCall(candidate.MethodCall),
            ExecutionArrayAccess arrayAccess => $"array:{ForHoist(arrayAccess.Array)}:{ForHoist(arrayAccess.Index)}:{HoistType(arrayAccess.ElementType)}:{HoistType(arrayAccess.ReturnType)}",
            ExecutionIsNullCheck isNull => $"isnull:{isNull.IsNegated}:{ForHoist(isNull.Expression)}:{HoistType(isNull.ReturnType)}",
            ExecutionRowPresence rowPresence => $"presence:{rowPresence.Alias}:{rowPresence.IsPresent}:{ForHoist(rowPresence.PresenceSource)}",
            ExecutionInCheck inCheck => $"in:{inCheck.IsNegated}:{ForHoist(inCheck.Expression)}:{string.Join(",", inCheck.Values.Select(ForHoist))}:{HoistType(inCheck.ReturnType)}",
            ExecutionCollectionInCheck collectionInCheck => $"collection-in:{ForHoist(collectionInCheck.Expression)}:{collectionInCheck.Collection.Name}:{HoistType(collectionInCheck.ElementType)}:{HoistType(collectionInCheck.ReturnType)}",
            ExecutionPatternMatch patternMatch => $"pattern:{patternMatch.Kind}:{ForHoist(patternMatch.Expression)}:{ForHoist(patternMatch.Pattern)}:{HoistType(patternMatch.ReturnType)}",
            ExecutionBetween between => $"between:{ForHoist(between.Expression)}:{ForHoist(between.Low)}:{ForHoist(between.High)}:{HoistType(between.ReturnType)}",
            ExecutionVariableRead variableRead => $"variable:{variableRead.Variable.Name}:{HoistType(variableRead.ReturnType)}",
            ExecutionScalarRowStream rows => $"scalar-row:{rows.Variable.Name}:{HoistType(rows.ReturnType)}",
            ExecutionRowContextsRead rowContextsRead => $"contexts:{rowContextsRead.Row.Name}",
            ExecutionNullContextArray nullContextArray => $"null-context:{nullContextArray.Count.ToString(CultureInfo.InvariantCulture)}",
            ExecutionContextArray contextArray => $"context-array:{string.Join(",", contextArray.Segments.Select(static segment => ForHoist(segment.Value)))}",
            ExecutionCompositeKey compositeKey => $"composite:{string.Join(",", compositeKey.Parts.Select(ForHoist))}",
            ExecutionValueTupleKey valueTupleKey => $"tuple:{string.Join(",", valueTupleKey.Parts.Select(ForHoist))}:{HoistType(valueTupleKey.ReturnType)}",
            ExecutionAggregateCall aggregateCall => $"aggregate:{HoistMethod(aggregateCall.Method)}:{string.Join(",", aggregateCall.Arguments.Select(ForHoist))}:{HoistType(aggregateCall.ReturnType)}",
            ExecutionGroupKeyRead groupKeyRead => $"group-key:{groupKeyRead.Group.Name}:{groupKeyRead.KeyName}:{HoistType(groupKeyRead.ReturnType)}",
            ExecutionAggregateCapturedValueRead captured => $"captured:{captured.Group.Name}:{captured.ValueName}:{HoistType(captured.ReturnType)}",
            ExecutionWindowValueRead window => $"window:{window.Results.Name}:{window.Index.Name}:{HoistType(window.ReturnType)}",
            _ => expression.ToString() ?? expression.GetType().FullName ?? expression.GetType().Name
        };

    private static string HoistMethodCall(ExecutionMethodCall methodCall)
    {
        var builder = new StringBuilder();
        builder.Append("method:");
        builder.Append(HoistMethod(methodCall.Method));
        builder.Append(":stable=");
        builder.Append(methodCall.Method.Descriptor.IsStable);
        AppendEnumIntrinsicFingerprint(builder, methodCall);
        builder.Append(':');
        builder.Append(methodCall.Target == null
            ? string.Empty
            : $"{methodCall.Target.Name}:{HoistType(methodCall.Target.Type)}");
        builder.Append(':');
        builder.Append(string.Join(",", methodCall.Arguments.Select(ForHoist)));
        builder.Append(':');
        builder.Append(methodCall.InjectedSource == null
            ? string.Empty
            : ForHoist(methodCall.InjectedSource));
        builder.Append(':');
        builder.Append(HoistType(methodCall.ReturnType));
        return builder.ToString();
    }

    private static void AppendEnumIntrinsicFingerprint(StringBuilder builder, ExecutionMethodCall methodCall)
    {
        builder.Append(":enum-intrinsic=");
        builder.Append(methodCall.EnumIntrinsic?.ToString() ?? "-");
        builder.Append(':');
        builder.Append(methodCall.OperandEnumType?.Fingerprint ?? "-");
        builder.Append(':');
        if (methodCall.EnumMask is { } mask)
        {
            builder.Append(mask.Kind);
            builder.Append(':');
            builder.Append(mask.RawValue.ToString("X16", CultureInfo.InvariantCulture));
        }
        else
        {
            builder.Append('-');
        }
    }

    private static string HoistMethod(ExecutionCallableRef method) => method.StableId;

    private static string HoistType(Type type)
    {
        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var genericType = type.GetGenericTypeDefinition();
        return $"{genericType.FullName}[{string.Join(",", type.GetGenericArguments().Select(HoistType))}]";
    }

    private static string HoistType(ExecutionTypeRef type) => HoistType(type.ResolveClrType());
}
