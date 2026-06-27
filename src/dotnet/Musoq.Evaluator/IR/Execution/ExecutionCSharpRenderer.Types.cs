using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private enum StreamingPluginWindowMode
    {
        Running,
        WholePartition
    }

    internal enum GeneratedRowContextConstructor
    {
        NoContext,
        ContextArray,
        SingleContext,
        SingleContexts,
        ContextRow,
        TwoSingleContexts,
        TwoContextRows,
        TwoContextArrays,
        ContextArrayAndSingleContext,
        ContextRowAndSingleContext,
        SingleContextAndContextArray,
        SingleContextAndContextRow,
        ContextRowAndContextArray,
        ContextArrayAndContextRow
    }

    private sealed record GeneratedRowConstructorSignature(
        IReadOnlyList<ParameterSyntax> Parameters,
        IReadOnlyList<StatementSyntax> ContextAssignments);

    private readonly record struct GeneratedRowContextParameter(
        string ParameterName,
        string FieldName,
        TypeSyntax Type);

    private sealed record ValueTupleAggregateHelper(
        string PopulateFunctionName,
        string FinalizeFunctionName,
        ExecutionCreateValueTupleAggregateContext Context,
        ExecutionSourceLoop AccumulationLoop,
        ExecutionEnsureTableCapacity EnsureCapacity,
        ExecutionForEach FinalizationLoop);

    private sealed record ReflectedMemberAccessor(
        string Key,
        string VariableName,
        Type SourceType,
        string PropertyPath);

    private readonly record struct GeneratedRowOrderComparerInput(
        string SourceName,
        string TargetName,
        GeneratedRowShape RowShape,
        IReadOnlyList<ExecutionOrderField> Keys);
}
