using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

internal sealed record TargetArtifactSemanticFacts
{
    public TargetArtifactSemanticFacts(
        QueryResultMode queryResultMode,
        Type? outputType,
        IReadOnlyList<ScriptParameterDefinition>? scriptParameterDefinitions,
        IReadOnlyList<ScriptVariableDefinition>? scriptVariableDefinitions,
        IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]>? usedColumns,
        IReadOnlyDictionary<string, ISchemaColumn[]>? pipelineInferredColumns,
        IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest>? sourcePlanRequestsPerSchema)
    {
        QueryResultMode = queryResultMode;
        OutputType = outputType;
        ScriptParameterDefinitions = Freeze(scriptParameterDefinitions);
        ScriptVariableDefinitions = Freeze(scriptVariableDefinitions);
        UsedColumns = FreezeSchemaColumnsBySource(usedColumns);
        PipelineInferredColumns = pipelineInferredColumns is null
            ? null
            : FreezeSchemaColumnsByAlias(pipelineInferredColumns);
        SourcePlanRequestsPerSchema = FreezeDictionary(sourcePlanRequestsPerSchema);
        PortableOutputTypeName = FormatTypeName(outputType);
        PortableScriptParameters = Freeze(scriptParameterDefinitions?.Select(CreateParameterFact));
        PortableScriptVariables = Freeze(scriptVariableDefinitions?.Select(CreateVariableFact));
        PortableUsedColumns = Freeze((usedColumns ?? new Dictionary<SchemaFromNode, ISchemaColumn[]>())
            .Select(static entry => new TargetArtifactSourceColumnsFact(
                CreateSourceFact(entry.Key),
                Freeze(entry.Value.Select(CreateColumnFact)))));
        PortablePipelineInferredColumns = pipelineInferredColumns is null
            ? null
            : Freeze(pipelineInferredColumns
                .Select(static entry => new TargetArtifactAliasColumnsFact(
                    entry.Key,
                    Freeze(entry.Value.Select(CreateColumnFact)))));
        PortableSourcePlanSignatures = Freeze((sourcePlanRequestsPerSchema ?? new Dictionary<SchemaFromNode, SourcePlanRequest>())
            .Select(static entry => CreateSourcePlanFact(entry.Key, entry.Value)));
    }

    public QueryResultMode QueryResultMode { get; }

    public Type? OutputType { get; }

    public IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions { get; }

    public IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions { get; }

    public IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> UsedColumns { get; }

    public IReadOnlyDictionary<string, ISchemaColumn[]>? PipelineInferredColumns { get; }

    public IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest> SourcePlanRequestsPerSchema { get; }

    public string? PortableOutputTypeName { get; }

    public IReadOnlyList<TargetArtifactScriptParameterFact> PortableScriptParameters { get; }

    public IReadOnlyList<TargetArtifactScriptVariableFact> PortableScriptVariables { get; }

    public IReadOnlyList<TargetArtifactSourceColumnsFact> PortableUsedColumns { get; }

    public IReadOnlyList<TargetArtifactAliasColumnsFact>? PortablePipelineInferredColumns { get; }

    public IReadOnlyList<TargetArtifactSourcePlanFact> PortableSourcePlanSignatures { get; }

    public static TargetArtifactSemanticFacts Empty { get; } = new(
        QueryResultMode.Table,
        null,
        [],
        [],
        new Dictionary<SchemaFromNode, ISchemaColumn[]>(),
        null,
        new Dictionary<SchemaFromNode, SourcePlanRequest>());

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }

    private static TargetArtifactScriptParameterFact CreateParameterFact(
        ScriptParameterDefinition parameter)
    {
        return new TargetArtifactScriptParameterFact(
            parameter.Name,
            FormatTypeName(parameter.ParameterType) ?? "<null>",
            parameter.HasDefaultValue,
            FormatTypeName(parameter.DefaultValue?.GetType()) ?? "<null>");
    }

    private static TargetArtifactScriptVariableFact CreateVariableFact(
        ScriptVariableDefinition variable)
    {
        return new TargetArtifactScriptVariableFact(
            variable.Name,
            FormatTypeName(variable.VariableType) ?? "<null>",
            variable.CanUseConstKeyword,
            FormatTypeName(variable.Value?.GetType()) ?? "<null>");
    }

    private static TargetArtifactSourceFact CreateSourceFact(SchemaFromNode source)
    {
        return new TargetArtifactSourceFact(
            source.Id ?? string.Empty,
            source.Schema ?? string.Empty,
            source.Method ?? string.Empty,
            source.Alias ?? string.Empty,
            source.QueryId.ToString(CultureInfo.InvariantCulture));
    }

    private static TargetArtifactColumnFact CreateColumnFact(ISchemaColumn column)
    {
        return new TargetArtifactColumnFact(
            column.ColumnIndex,
            column.ColumnName,
            column.IntendedTypeName,
            FormatTypeName(column.ColumnType) ?? "<null>",
            FreezeDictionary(column.ReadModifiers));
    }

    private static TargetArtifactSourceColumnRefFact CreateSourceColumnRefFact(
        SourceColumnRef column)
    {
        return new TargetArtifactSourceColumnRefFact(
            column.Name,
            FreezeDictionary(column.ReadModifiers));
    }

    private static TargetArtifactOrderByFact CreateOrderByFact(OrderByExpression order)
    {
        return new TargetArtifactOrderByFact(
            CreateSourceColumnRefFact(order.Column),
            order.Direction.ToString());
    }

    private static TargetArtifactSourcePlanFact CreateSourcePlanFact(
        SchemaFromNode source,
        SourcePlanRequest request)
    {
        return new TargetArtifactSourcePlanFact(
            CreateSourceFact(source),
            request.Identity.SchemaName,
            request.Identity.MethodName,
            request.Identity.SourceContextId,
            request.Identity.Alias,
            Freeze(request.RequiredColumns.Select(CreateSourceColumnRefFact)),
            Freeze(request.OrderBy.Select(CreateOrderByFact)),
            request.Skip?.ToString(CultureInfo.InvariantCulture) ?? "<null>",
            request.Take?.ToString(CultureInfo.InvariantCulture) ?? "<null>",
            FormatTypeName(request.Predicate?.GetType()) ?? "<null>");
    }

    private static string? FormatTypeName(Type? type)
    {
        return type?.AssemblyQualifiedName;
    }

    private static IReadOnlyDictionary<TKey, TValue> FreezeDictionary<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue>? values)
        where TKey : notnull
    {
        return new ReadOnlyDictionary<TKey, TValue>(
            values is null
                ? new Dictionary<TKey, TValue>()
                : new Dictionary<TKey, TValue>(values));
    }

    private static IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> FreezeSchemaColumnsBySource(
        IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]>? values)
    {
        return new ReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]>(
            values is null
                ? new Dictionary<SchemaFromNode, ISchemaColumn[]>()
                : values.ToDictionary(
                    static entry => entry.Key,
                    static entry => entry.Value.ToArray()));
    }

    private static IReadOnlyDictionary<string, ISchemaColumn[]> FreezeSchemaColumnsByAlias(
        IReadOnlyDictionary<string, ISchemaColumn[]> values)
    {
        return new ReadOnlyDictionary<string, ISchemaColumn[]>(
            values.ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value.ToArray(),
                StringComparer.Ordinal));
    }
}

internal sealed record TargetArtifactScriptParameterFact(
    string Name,
    string TypeName,
    bool HasDefaultValue,
    string DefaultValueTypeName);

internal sealed record TargetArtifactScriptVariableFact(
    string Name,
    string TypeName,
    bool CanUseConstKeyword,
    string ValueTypeName);

internal sealed record TargetArtifactSourceFact(
    string Id,
    string Schema,
    string Method,
    string Alias,
    string QueryId);

internal sealed record TargetArtifactColumnFact(
    int ColumnIndex,
    string ColumnName,
    string? IntendedTypeName,
    string ColumnTypeName,
    IReadOnlyDictionary<string, string> ReadModifiers);

internal sealed record TargetArtifactSourceColumnsFact(
    TargetArtifactSourceFact Source,
    IReadOnlyList<TargetArtifactColumnFact> Columns);

internal sealed record TargetArtifactAliasColumnsFact(
    string Alias,
    IReadOnlyList<TargetArtifactColumnFact> Columns);

internal sealed record TargetArtifactSourceColumnRefFact(
    string Name,
    IReadOnlyDictionary<string, string> ReadModifiers);

internal sealed record TargetArtifactOrderByFact(
    TargetArtifactSourceColumnRefFact Column,
    string Direction);

internal sealed record TargetArtifactSourcePlanFact(
    TargetArtifactSourceFact Source,
    string IdentitySchemaName,
    string IdentityMethodName,
    string IdentitySourceContextId,
    string IdentityAlias,
    IReadOnlyList<TargetArtifactSourceColumnRefFact> RequiredColumns,
    IReadOnlyList<TargetArtifactOrderByFact> OrderBy,
    string Skip,
    string Take,
    string PredicateTypeName);
