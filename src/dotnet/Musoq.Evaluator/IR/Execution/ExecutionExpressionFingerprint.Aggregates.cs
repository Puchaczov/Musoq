using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionExpressionFingerprint
{
    internal static void AppendAggregateAccumulator(StringBuilder builder, AggregateAccumulatorField accumulator)
    {
        builder
            .Append(accumulator.FieldName)
            .Append(':')
            .Append(ForAggregateType(accumulator.Kernel.KernelType))
            .Append(':')
            .Append(ForAggregateType(accumulator.Kernel.StateType))
            .Append(':')
            .Append(ForAggregateType(accumulator.InputType))
            .Append(':')
            .Append(ForAggregateType(accumulator.ResultType))
            .Append(':')
            .Append(accumulator.ParentDepth.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(accumulator.OwnerPrefixLength.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(accumulator.OwnerFieldName ?? string.Empty)
            .Append(';');
    }

    internal static string ForParallelAggregate(
        ExecutionExpression expression,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead => $"field:{NormalizeAlias(fieldRead.Alias, parallelAggregate)}:{NormalizeFieldName(fieldRead.FieldName, fieldRead.Alias, parallelAggregate)}:{ForAggregateType(fieldRead.ReturnType)}:{AggregateFieldAccessStrategy(fieldRead.AccessStrategy, fieldRead.Alias, parallelAggregate)}{(fieldRead.EnumType == null ? string.Empty : $":source={fieldRead.SourceReadType?.StableId ?? "-"}:enum={fieldRead.EnumType.Fingerprint}")}",
            ExecutionMemberRead memberRead => $"member:{memberRead.IsDynamic}:{memberRead.MemberName}:{ForParallelAggregate(memberRead.Receiver, parallelAggregate)}:{ForAggregateType(memberRead.ReturnType)}",
            ExecutionLiteral literal => $"literal:{NormalizeLiteralValue(literal.Value, parallelAggregate)}:{ForAggregateType(literal.ReturnType)}",
            ExecutionBinary binary => $"binary:{binary.Kind}:{ForParallelAggregate(binary.Left, parallelAggregate)}:{ForParallelAggregate(binary.Right, parallelAggregate)}:{ForAggregateType(binary.ReturnType)}",
            ExecutionUnary unary => $"unary:{unary.Kind}:{ForParallelAggregate(unary.Operand, parallelAggregate)}:{ForAggregateType(unary.ReturnType)}",
            ExecutionMethodCall methodCall => AggregateMethodCall(methodCall, parallelAggregate),
            ExecutionStrictCast strictCast => $"cast:{ForParallelAggregate(strictCast.Expression, parallelAggregate)}:{strictCast.TargetTypeName}:{ForAggregateType(strictCast.ReturnType)}",
            ExecutionMethodTargetReuseCandidate candidate => AggregateMethodCall(candidate.MethodCall, parallelAggregate),
            ExecutionArrayAccess arrayAccess => $"array:{ForParallelAggregate(arrayAccess.Array, parallelAggregate)}:{ForParallelAggregate(arrayAccess.Index, parallelAggregate)}:{ForAggregateType(arrayAccess.ReturnType)}",
            ExecutionIsNullCheck isNull => $"is-null:{isNull.IsNegated}:{ForParallelAggregate(isNull.Expression, parallelAggregate)}",
            ExecutionRowPresence rowPresence => $"presence:{rowPresence.Alias}:{rowPresence.IsPresent}:{ForParallelAggregate(rowPresence.PresenceSource, parallelAggregate)}",
            ExecutionInCheck inCheck => AggregateSequence(
                inCheck.IsNegated ? "not-in" : "in",
                inCheck.Values.Prepend(inCheck.Expression),
                inCheck.ReturnType,
                parallelAggregate),
            ExecutionPatternMatch patternMatch => $"pattern:{patternMatch.Kind}:{ForParallelAggregate(patternMatch.Expression, parallelAggregate)}:{ForParallelAggregate(patternMatch.Pattern, parallelAggregate)}:{ForAggregateType(patternMatch.ReturnType)}",
            ExecutionBetween between => AggregateSequence(
                "between",
                [between.Expression, between.Low, between.High],
                between.ReturnType,
                parallelAggregate),
            ExecutionCaseWhen caseWhen => AggregateCaseWhen(caseWhen, parallelAggregate),
            ExecutionCoalesce coalesce => AggregateSequence("coalesce", coalesce.Expressions, coalesce.ReturnType, parallelAggregate),
            ExecutionRowStream rows => rows.Kind == ExecutionRowStreamKind.Chunks
                ? $"chunked-rows:{ForAggregateVariable(rows.Variable, parallelAggregate)}"
                : $"rows:{rows.RowsAccess}:{ForAggregateVariable(rows.Variable, parallelAggregate)}",
            ExecutionScalarRowStream rows => $"scalar-row:{ForAggregateVariable(rows.Variable, parallelAggregate)}",
            ExecutionStoredTable storedTable => $"stored-table:{storedTable.TableIndex.ToString(CultureInfo.InvariantCulture)}",
            ExecutionStoredTableRows storedTableRows => $"stored-table-rows:{storedTableRows.TableIndex.ToString(CultureInfo.InvariantCulture)}:{storedTableRows.GeneratedRowShape?.TypeName ?? string.Empty}",
            ExecutionVariableRead variableRead => $"variable:{ForAggregateVariable(variableRead.Variable, parallelAggregate)}",
            ExecutionRowContextsRead rowContexts => $"contexts:{ForAggregateVariable(rowContexts.Row, parallelAggregate)}",
            ExecutionNullContextArray nullContextArray => $"null-contexts:{nullContextArray.Count.ToString(CultureInfo.InvariantCulture)}",
            ExecutionCompositeKey compositeKey => AggregateSequence("composite-key", compositeKey.Parts, compositeKey.ReturnType, parallelAggregate),
            ExecutionValueTupleKey valueTupleKey => AggregateSequence("value-tuple-key", valueTupleKey.Parts, valueTupleKey.ReturnType, parallelAggregate),
            ExecutionWindowValueRead windowValueRead => $"window:{ForAggregateVariable(windowValueRead.Results, parallelAggregate)}:{ForAggregateVariable(windowValueRead.Index, parallelAggregate)}:{ForAggregateType(windowValueRead.ReturnType)}",
            ExecutionAggregateCall aggregateCall => AggregateCall(aggregateCall, parallelAggregate),
            ExecutionGroupKeyRead groupKeyRead => $"group-key:{ForAggregateVariable(groupKeyRead.Group, parallelAggregate)}:{groupKeyRead.Key?.FieldName ?? groupKeyRead.KeyName}:{ForAggregateType(groupKeyRead.ReturnType)}",
            ExecutionAggregateCapturedValueRead capturedValueRead => $"captured-read:{ForAggregateVariable(capturedValueRead.Group, parallelAggregate)}:{capturedValueRead.CapturedField.FieldName}:{ForAggregateType(capturedValueRead.ReturnType)}",
            ExecutionAggregateResultRef aggregateRef => $"aggregate-ref:{aggregateRef.Identifier}:{aggregateRef.ReturnType.StableId}",
            ExecutionWindowResultRef windowRef => $"window-ref:{windowRef.WindowIndex}:{windowRef.ReturnType.StableId}",
            _ => $"{expression.GetType().FullName}:{expression}"
        };
    }

    internal static string ForAggregateMethod(ExecutionCallableRef method) => method.StableId;

    internal static string ForAggregateType(Type type)
    {
        if (!type.IsGenericType)
            return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        var builder = new StringBuilder();
        builder
            .Append(genericTypeDefinition.AssemblyQualifiedName ?? genericTypeDefinition.FullName ?? genericTypeDefinition.Name)
            .Append('<');

        foreach (var argument in type.GetGenericArguments())
        {
            builder
                .Append(ForAggregateType(argument))
                .Append(',');
        }

        builder.Append('>');
        return builder.ToString();
    }

    private static string AggregateMethodCall(
        ExecutionMethodCall methodCall,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var builder = new StringBuilder();
        builder
            .Append("call:")
            .Append(ForAggregateMethod(methodCall.Method))
            .Append(':');
        AppendEnumIntrinsicFingerprint(builder, methodCall);
        builder
            .Append(":target:")
            .Append(methodCall.Target is null ? "<null>" : ForAggregateType(methodCall.Target.Type))
            .Append(":source:")
            .Append(methodCall.InjectedSource is null ? "<null>" : ForParallelAggregate(methodCall.InjectedSource, parallelAggregate))
            .Append(":args:");

        foreach (var argument in methodCall.Arguments)
        {
            builder
                .Append(ForParallelAggregate(argument, parallelAggregate))
                .Append(';');
        }

        return builder.ToString();
    }

    private static string AggregateCall(
        ExecutionAggregateCall aggregateCall,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var builder = new StringBuilder();
        builder
            .Append("aggregate-call:")
            .Append(ForAggregateVariable(aggregateCall.Group, parallelAggregate))
            .Append(':')
            .Append(ForAggregateMethod(aggregateCall.Method))
            .Append(':');
        AppendAggregateAccumulator(builder, aggregateCall.Accumulator);
        builder.Append(":args:");
        foreach (var argument in aggregateCall.Arguments)
        {
            builder
                .Append(ForParallelAggregate(argument, parallelAggregate))
                .Append(';');
        }

        return builder.ToString();
    }

    private static string AggregateCaseWhen(
        ExecutionCaseWhen caseWhen,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var builder = new StringBuilder();
        builder.Append("case:");
        foreach (var branch in caseWhen.Branches)
        {
            builder
                .Append(ForParallelAggregate(branch.Condition, parallelAggregate))
                .Append("=>")
                .Append(ForParallelAggregate(branch.Result, parallelAggregate))
                .Append(';');
        }

        builder
            .Append("else:")
            .Append(caseWhen.ElseExpression is null ? "<null>" : ForParallelAggregate(caseWhen.ElseExpression, parallelAggregate))
            .Append(':')
            .Append(ForAggregateType(caseWhen.ReturnType));

        return builder.ToString();
    }

    private static string AggregateSequence(
        string kind,
        IEnumerable<ExecutionExpression> expressions,
        ExecutionTypeRef returnType,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var builder = new StringBuilder();
        builder
            .Append(kind)
            .Append(':')
            .Append(ForAggregateType(returnType))
            .Append(':');

        foreach (var expression in expressions)
        {
            builder
                .Append(ForParallelAggregate(expression, parallelAggregate))
                .Append(';');
        }

        return builder.ToString();
    }

    private static string NormalizeAlias(string? alias, ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        if (string.Equals(alias, parallelAggregate.Source.Name, StringComparison.Ordinal))
            return "$source";

        return alias ?? string.Empty;
    }

    private static string NormalizeLiteralValue(
        ExecutionConstantValue value,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        if (value.ToClrValue() is not string text)
            return value.ToString();

        if (!text.Contains('(', StringComparison.Ordinal))
            return text;

        return text.Replace(
            $"{parallelAggregate.Source.Name}.",
            "$source.",
            StringComparison.Ordinal);
    }

    private static string NormalizeFieldName(
        string fieldName,
        string? alias,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        if (alias != null && !string.Equals(alias, parallelAggregate.Source.Name, StringComparison.Ordinal))
            return fieldName;

        var aliasPrefix = $"{parallelAggregate.Source.Name}.";
        return fieldName.StartsWith(aliasPrefix, StringComparison.Ordinal)
            ? fieldName[aliasPrefix.Length..]
            : fieldName;
    }

    private static string AggregateFieldAccessStrategy(
        FieldAccessStrategy? accessStrategy,
        string? alias,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        return accessStrategy switch
        {
            null => string.Empty,
            ClrPropertyAccess property => $"clr:{NormalizeFieldName(property.PropertyName, alias, parallelAggregate)}",
            DirectScalarValueAccess => "scalar",
            GeneratedFieldAccess generated => $"generated:{generated.FieldName}",
            GeneratedRowContextAccess generatedContext => $"generated-row-context:{generatedContext.TypeName}.{generatedContext.Index.ToString(CultureInfo.InvariantCulture)}",
            GeneratedRowTypeAccess generatedRow => $"generated-row:{generatedRow.TypeName}.{generatedRow.FieldName}",
            GeneratedRowNestedAccess generatedNested => $"generated-row-nested:{generatedNested.TypeName}.{generatedNested.FieldName}.{NormalizeFieldName(generatedNested.PropertyPath, alias, parallelAggregate)}",
            ExpandoDictionaryAccess expando => $"expando:{expando.Key}",
            PositionalAccess positional => $"positional:{positional.Index.ToString(CultureInfo.InvariantCulture)}",
            ContextAccess context => $"context:{context.Index.ToString(CultureInfo.InvariantCulture)}",
            ReflectedMemberAccess reflected => $"reflected:{NormalizeFieldName(reflected.PropertyPath, alias, parallelAggregate)}",
            NestedClrPropertyAccess nested => $"nested-clr:{NormalizeFieldName(nested.PropertyPath, alias, parallelAggregate)}",
            NestedPositionalAccess nested => $"nested-positional:{nested.Index.ToString(CultureInfo.InvariantCulture)}:{NormalizeFieldName(nested.PropertyPath, alias, parallelAggregate)}",
            RuntimeDynamicMemberAccess runtimeDynamic => $"runtime-dynamic:{runtimeDynamic.MemberName}",
            RuntimeDynamicMemberPathAccess runtimePath =>
                $"runtime-dynamic-path:{runtimePath.RootFieldName}:{string.Join('.', runtimePath.Segments.Select(segment => $"{segment.MemberName}:{segment.IsDynamic}:{segment.ResultType.StableId}"))}",
            _ => accessStrategy.GetType().FullName ?? accessStrategy.GetType().Name
        };
    }

    internal static string ForAggregateVariable(
        ExecutionVariable variable,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        if (string.Equals(variable.Name, parallelAggregate.Source.Name, StringComparison.Ordinal))
            return $"$source:{ForAggregateType(variable.Type)}";

        if (string.Equals(variable.Name, parallelAggregate.Group.Name, StringComparison.Ordinal))
            return $"$group:{ForAggregateType(variable.Type)}";

        if (string.Equals(variable.Name, parallelAggregate.RootGroup.Name, StringComparison.Ordinal))
            return $"$root:{ForAggregateType(variable.Type)}";

        if (string.Equals(variable.Name, parallelAggregate.GroupsToFinalize.Name, StringComparison.Ordinal))
            return $"$groupsToFinalize:{ForAggregateType(variable.Type)}";

        return $"{variable.Name}:{ForAggregateType(variable.Type)}";
    }
}
