using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Targets.Execution.Analysis;

internal static class TargetRuntimeContractBuilder
{
    public static TargetRuntimeContract Build(
        ExecutionPlan plan,
        ExecutionTargetCompatibilityReport compatibilityReport,
        IReadOnlyList<TargetSourceRuntimeMetadata>? sourceRuntimeMetadata = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(compatibilityReport);

        var nodes = ExecutionIrAnalysis.FlattenNodes(plan.Body).ToArray();
        var sourceAccess = CreateSourceAccess(nodes, sourceRuntimeMetadata);
        var pluginInvocations = compatibilityReport.Requirements
            .Where(static requirement => requirement.Kind == ExecutionTargetRequirementKind.PluginInvocation)
            .Where(static requirement => requirement.CallableSymbol != null)
            .Select(static requirement => new TargetPluginInvocationContract(requirement.Detail, requirement.CallableSymbol!))
            .GroupBy(static invocation => (invocation.Detail, StableName: invocation.Callable.StableName))
            .Select(static group => group.First())
            .OrderBy(static invocation => invocation.Detail, StringComparer.Ordinal)
            .ThenBy(static invocation => invocation.Callable.StableName, StringComparer.Ordinal)
            .ToArray();
        var rowShapes = CreateRowShapes(plan);
        var typeSymbols = compatibilityReport.Requirements
            .Select(static requirement => requirement.TypeSymbol)
            .Where(static symbol => symbol != null)
            .Select(static symbol => symbol!)
            .ToArray();

        return new TargetRuntimeContract(
            plan.Identifier,
            sourceAccess,
            pluginInvocations,
            rowShapes,
            CreateNullBehavior(rowShapes, typeSymbols),
            new TargetCancellationContract(
                RequiresCancellationToken: true,
                RequiresParallelCancellation: nodes.Any(static node => node.GetType().Name.Contains("Parallel", StringComparison.Ordinal))),
            new TargetDiagnosticsContract(
                RequiresBuildDiagnostics: compatibilityReport.HasRequirements,
                RequiresSourceDiagnostics: sourceAccess.Count > 0,
                RequiresRuntimeExceptionDiagnostics: true),
            new TargetProfilingContract(
                SupportsSourceBoundaryProfiling: sourceAccess.Count > 0,
                SupportsOperatorProfiling: nodes.Length > 0,
                SourceBoundaryCount: sourceAccess.Count,
                OperatorCount: nodes.Length));
    }

    private static IReadOnlyList<TargetSourceAccessContract> CreateSourceAccess(
        IReadOnlyList<ExecutionNode> nodes,
        IReadOnlyList<TargetSourceRuntimeMetadata>? sourceRuntimeMetadata)
    {
        var sources = new List<TargetSourceAccessContract>();
        var metadataByContext = (sourceRuntimeMetadata ?? [])
            .ToDictionary(static metadata => metadata.SourceContextId, StringComparer.Ordinal);

        foreach (var node in nodes)
        {
            switch (node)
            {
                case ExecutionSourceScan sourceScan:
                    metadataByContext.TryGetValue(sourceScan.Binding.RuntimeContextId, out var sourceMetadata);
                    sources.Add(new TargetSourceAccessContract(
                        "schema-source",
                        sourceScan.Binding.RuntimeContextId,
                        sourceScan.Binding.SchemaName,
                        sourceScan.Binding.MethodName,
                        sourceScan.Rows.Type.Descriptor,
                        sourceScan.Binding.SourceType?.Descriptor,
                        sourceScan.Binding.Arguments.Select(static argument => argument.ReturnType.Descriptor).ToArray(),
                        CreateFields(sourceScan.Binding.Fields),
                        sourceMetadata?.AcceptedOperations,
                        sourceMetadata?.RuntimeSettings));
                    break;
                case ExecutionInterpretSource interpret:
                    sources.Add(new TargetSourceAccessContract(
                        "interpret-source",
                        interpret.SchemaName,
                        interpret.SchemaName,
                        interpret.InterpreterTypeName,
                        interpret.Rows.Type.Descriptor,
                        null,
                        [],
                        [],
                        [],
                        []));
                    break;
                case ExecutionEnumerableSource enumerable:
                    sources.Add(new TargetSourceAccessContract(
                        "enumerable-source",
                        enumerable.Rows.Name,
                        "<runtime>",
                        enumerable.EnumerableType.DisplayName,
                        enumerable.Rows.Type.Descriptor,
                        enumerable.EnumerableType.Descriptor,
                        [],
                        [],
                        [],
                        []));
                    break;
            }
        }

        return sources
            .OrderBy(static source => source.SourceContextId, StringComparer.Ordinal)
            .ThenBy(static source => source.Kind, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<TargetRowShapeContract> CreateRowShapes(ExecutionPlan plan)
    {
        var shapes = plan.Shapes
            .Concat(plan.FinalResult != null ? [plan.FinalResult.Shape] : [])
            .Select(CreateRowShape)
            .GroupBy(static shape => (shape.Kind, shape.Name))
            .Select(static group => group.First())
            .OrderBy(static shape => shape.Name, StringComparer.Ordinal)
            .ThenBy(static shape => shape.Kind, StringComparer.Ordinal)
            .ToArray();

        return shapes;
    }

    private static TargetRowShapeContract CreateRowShape(RowShape shape)
    {
        return shape switch
        {
            SourceEntityShape source => new TargetRowShapeContract(
                nameof(SourceEntityShape),
                source.Name,
                source.EntityType.Descriptor,
                CreateFields(source.Fields)),
            GeneratedRowShape generated => CreateGeneratedShape(nameof(GeneratedRowShape), generated.TypeName, generated.Fields, generated.Contexts),
            GeneratedRecordShape generated => CreateGeneratedShape(nameof(GeneratedRecordShape), generated.TypeName, generated.Fields, []),
            HashPayloadShape payload => CreateGeneratedShape(nameof(HashPayloadShape), payload.TypeName, payload.Fields, payload.Contexts),
            AggregateGroupShape aggregate => new TargetRowShapeContract(
                nameof(AggregateGroupShape),
                aggregate.TypeName,
                CreateGeneratedRowSymbol(aggregate.TypeName, CreateAggregateFields(aggregate)),
                CreateAggregateFields(aggregate)),
            ValuesRowShape values => CreateRowShape(values.GeneratedShape) with
            {
                Kind = nameof(ValuesRowShape),
                Name = values.Name
            },
            ExpandoAdapterShape expando => new TargetRowShapeContract(
                nameof(ExpandoAdapterShape),
                expando.Name,
                expando.RuntimeType.Descriptor,
                CreateFields(expando.Fields)),
            TableRowShape table => new TargetRowShapeContract(
                nameof(TableRowShape),
                table.Name,
                null,
                CreateFields(table.Fields.Concat(table.Contexts))),
            _ => new TargetRowShapeContract(
                shape.GetType().Name,
                shape.Name,
                null,
                CreateFields(shape.Fields))
        };
    }

    private static TargetRowShapeContract CreateGeneratedShape(
        string kind,
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        IReadOnlyList<FieldBinding> contexts)
    {
        return new TargetRowShapeContract(
            kind,
            typeName,
            CreateGeneratedRowSymbol(typeName, CreateFields(fields.Concat(contexts))),
            CreateFields(fields.Concat(contexts)));
    }

    private static ExecutionPortableTypeDescriptor CreateGeneratedRowSymbol(
        string typeName,
        IReadOnlyList<TargetFieldContract> fields)
    {
        return ExecutionPortableSymbolFactory.GeneratedRow(
            typeName,
            fields.Select(static field => new ExecutionPortableRowFieldDescriptor(
                field.Name,
                field.Type,
                field.Nullability)));
    }

    private static IReadOnlyList<TargetFieldContract> CreateAggregateFields(AggregateGroupShape aggregate)
    {
        var fields = new List<TargetFieldContract>();

        fields.AddRange(aggregate.Keys.Select((key, index) => CreateSyntheticField(
            index, key.FieldName, key.FieldName, key.Type, FieldNullability.Unknown)));
        fields.AddRange(aggregate.CapturedFields.Select((captured, index) => CreateSyntheticField(
            aggregate.Keys.Count + index, captured.FieldName, captured.FieldName, captured.Type, FieldNullability.Unknown)));
        fields.AddRange(aggregate.Accumulators.Select((accumulator, index) => CreateSyntheticField(
            aggregate.Keys.Count + aggregate.CapturedFields.Count + index,
            accumulator.FieldName,
            accumulator.FieldName,
            accumulator.ResultType,
            FieldNullability.Unknown)));

        return fields
            .OrderBy(static field => field.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<TargetFieldContract> CreateFields(IEnumerable<FieldBinding> fields)
    {
        return fields
            .Select(static field => new TargetFieldContract(
                field.OutputIndex,
                field.Name,
                field.QualifiedName,
                field.Type.Descriptor,
                field.ColumnType.Descriptor,
                field.Nullability.ToString(),
                field.ReadModifiers))
            .OrderBy(static field => field.Index)
            .ThenBy(static field => field.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static TargetFieldContract CreateSyntheticField(
        int index,
        string name,
        string qualifiedName,
        ExecutionTypeRef type,
        FieldNullability nullability)
    {
        var symbol = type.Descriptor;
        return new TargetFieldContract(index, name, qualifiedName, symbol, symbol, nullability.ToString(), null);
    }

    private static TargetNullBehaviorContract CreateNullBehavior(
        IReadOnlyList<TargetRowShapeContract> rowShapes,
        IReadOnlyList<ExecutionPortableTypeDescriptor> typeSymbols)
    {
        var allSymbols = rowShapes
            .SelectMany(static shape => shape.Fields)
            .SelectMany(static field => new[] { field.Type, field.PublicType })
            .Concat(typeSymbols)
            .ToArray();

        return new TargetNullBehaviorContract(
            UsesNullableValueTypes: allSymbols.Any(static symbol => ContainsKind(symbol, ExecutionPortableTypeKind.Nullable)),
            UsesObjectNulls: allSymbols.Any(static symbol =>
                string.Equals(symbol.StableName, "host-opaque:dynamic-object", StringComparison.Ordinal)),
            UsesFieldNullabilityMetadata: rowShapes
                .SelectMany(static shape => shape.Fields)
                .Any(static field => !string.Equals(field.Nullability, FieldNullability.Unknown.ToString(), StringComparison.Ordinal)),
            Semantics: "clr-null-and-sql-null-compatible");
    }

    private static bool ContainsKind(
        ExecutionPortableTypeDescriptor symbol,
        ExecutionPortableTypeKind kind)
    {
        return symbol.Kind == kind || symbol.Arguments.Any(argument => ContainsKind(argument, kind));
    }
}
