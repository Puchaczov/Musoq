using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Optimization;

/// <summary>
/// Template-based code generation for common query patterns.
/// Targets 20-30% reduction in generated code size and complexity.
/// </summary>
public static class CodeGenerationTemplates
{
    /// <summary>
    /// Template for simple SELECT projection without joins or aggregations.
    /// </summary>
    public static string SimpleSelectTemplate(
        string className,
        string sourceExpression,
        IEnumerable<string> fieldExpressions,
        string filterExpression = null)
    {
        var fieldsCode = string.Join(",\n                ", fieldExpressions.Select(f => $"({f})"));
        var filterCode = string.IsNullOrEmpty(filterExpression) ? "" : $"if ({filterExpression}) ";

        return $@"
public class {className} : IRunnable
{{
    public ISchemaProvider Provider {{ get; set; }}
    public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables {{ get; set; }}
    public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation {{ get; set; }}
    public ILogger Logger {{ get; set; }}

    public Table Run(CancellationToken token)
    {{
        var results = new List<object[]>();
        foreach (var row in {sourceExpression}.Rows)
        {{
            {filterCode}{{
                results.Add(new object[] {{ {fieldsCode} }});
            }}
        }}
        return new Table(""QueryResult"", results);
    }}
}}";
    }

    /// <summary>
    /// Template for aggregation queries with GROUP BY.
    /// </summary>
    public static string AggregationTemplate(
        string className,
        string sourceExpression,
        IEnumerable<string> groupByFields,
        IEnumerable<string> aggregationFields,
        string filterExpression = null)
    {
        var groupByCode = string.Join(", ", groupByFields);
        var aggregationCode = string.Join(",\n                ", aggregationFields);
        var filterCode = string.IsNullOrEmpty(filterExpression) ? "" : $"if ({filterExpression}) ";

        return $@"
public class {className} : IRunnable
{{
    public ISchemaProvider Provider {{ get; set; }}
    public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables {{ get; set; }}
    public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation {{ get; set; }}
    public ILogger Logger {{ get; set; }}

    public Table Run(CancellationToken token)
    {{
        var groups = new Dictionary<object, AggregationState>();
        
        foreach (var row in {sourceExpression}.Rows)
        {{
            {filterCode}{{
                var groupKey = new object[] {{ {groupByCode} }};
                var keyHash = ComputeHash(groupKey);
                
                if (!groups.TryGetValue(keyHash, out var state))
                {{
                    state = new AggregationState();
                    groups[keyHash] = state;
                }}
                
                state.Accumulate(row);
            }}
        }}
        
        var results = new List<object[]>();
        foreach (var group in groups.Values)
        {{
            results.Add(new object[] {{ {aggregationCode} }});
        }}
        return new Table(""QueryResult"", results);
    }}
}}";
    }

    /// <summary>
    /// Template for INNER JOIN operations.
    /// </summary>
    public static string InnerJoinTemplate(
        string className,
        string leftSourceExpression,
        string rightSourceExpression,
        string joinCondition,
        IEnumerable<string> fieldExpressions)
    {
        var fieldsCode = string.Join(",\n                ", fieldExpressions);

        return $@"
public class {className} : IRunnable
{{
    public ISchemaProvider Provider {{ get; set; }}
    public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables {{ get; set; }}
    public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation {{ get; set; }}
    public ILogger Logger {{ get; set; }}

    public Table Run(CancellationToken token)
    {{
        var results = new List<object[]>();
        foreach (var leftRow in {leftSourceExpression}.Rows)
        {{
            foreach (var rightRow in {rightSourceExpression}.Rows)
            {{
                if ({joinCondition})
                {{
                    results.Add(new object[] {{ {fieldsCode} }});
                }}
            }}
        }}
        return new Table(""QueryResult"", results);
    }}
}}";
    }

    /// <summary>
    /// Template for optimized field access with type-specific casting.
    /// </summary>
    public static string FieldAccessTemplate(string fieldName, Type fieldType, string sourceAlias = "row")
    {
        var typeName = TypeCacheManager.GetCachedCastableTypeName(fieldType);
        
        return fieldType switch
        {
            _ when fieldType == typeof(string) => $"{sourceAlias}[\"{fieldName}\"] as string",
            _ when fieldType == typeof(int) => $"Convert.ToInt32({sourceAlias}[\"{fieldName}\"])",
            _ when fieldType == typeof(long) => $"Convert.ToInt64({sourceAlias}[\"{fieldName}\"])",
            _ when fieldType == typeof(double) => $"Convert.ToDouble({sourceAlias}[\"{fieldName}\"])",
            _ when fieldType == typeof(bool) => $"Convert.ToBoolean({sourceAlias}[\"{fieldName}\"])",
            _ when fieldType == typeof(decimal) => $"Convert.ToDecimal({sourceAlias}[\"{fieldName}\"])",
            _ when fieldType == typeof(DateTime) => $"Convert.ToDateTime({sourceAlias}[\"{fieldName}\"])",
            _ => $"({typeName}){sourceAlias}[\"{fieldName}\"]"
        };
    }

    /// <summary>
    /// Template for optimized type casting with null handling.
    /// </summary>
    public static string SafeCastTemplate(string expression, Type targetType)
    {
        var typeName = TypeCacheManager.GetCachedCastableTypeName(targetType);
        
        if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
        {
            // Non-nullable value type
            return $"({expression} ?? default({typeName}))";
        }
        else
        {
            // Reference type or nullable value type
            return $"({expression} as {typeName})";
        }
    }

    /// <summary>
    /// Template for method invocation with cached reflection.
    /// </summary>
    public static string MethodInvocationTemplate(
        string targetExpression,
        string methodName,
        Type targetType,
        IEnumerable<string> arguments)
    {
        var argsCode = string.Join(", ", arguments);
        
        // For known common methods, use direct calls instead of reflection
        return methodName.ToLowerInvariant() switch
        {
            "tostring" => $"{targetExpression}?.ToString()",
            "length" when targetType == typeof(string) => $"({targetExpression} as string)?.Length ?? 0",
            "count" => $"{targetExpression}?.Count() ?? 0",
            _ => $"InvokeMethod({targetExpression}, \"{methodName}\", new object[] {{ {argsCode} }})"
        };
    }

    /// <summary>
    /// Template for creating optimized column metadata arrays.
    /// </summary>
    public static string ColumnMetadataTemplate(IEnumerable<(string Name, Type Type, int Index)> columns)
    {
        var columnDefs = columns.Select((col, index) => 
            $"new Column(\"{col.Name}\", typeof({TypeCacheManager.GetCachedCastableTypeName(col.Type)}), {col.Index})");
            
        return $"new ISchemaColumn[] {{ {string.Join(", ", columnDefs)} }}";
    }

    /// <summary>
    /// Template for expression tree compilation hint.
    /// </summary>
    public static string ExpressionTreeTemplate(string expression, Type inputType, Type outputType)
    {
        var inputTypeName = TypeCacheManager.GetCachedCastableTypeName(inputType);
        var outputTypeName = TypeCacheManager.GetCachedCastableTypeName(outputType);
        
        return $@"
private static readonly Func<{inputTypeName}, {outputTypeName}> _compiled_{Guid.NewGuid():N} = 
    CompileExpression<{inputTypeName}, {outputTypeName}>({expression});";
    }

    /// <summary>
    /// Template for hash-based grouping optimization.
    /// </summary>
    public static string HashGroupingTemplate(IEnumerable<string> groupByFields)
    {
        var fieldsCode = string.Join(" ^ ", groupByFields.Select(f => $"({f})?.GetHashCode() ?? 0"));
        
        return $@"
private static int ComputeGroupHash(object[] values)
{{
    unchecked
    {{
        return {fieldsCode};
    }}
}}";
    }

    /// <summary>
    /// Template for staged transformation classes.
    /// </summary>
    public static string StagedTransformationTemplate(
        string className,
        string stageName,
        string inputType,
        string outputType,
        string transformationLogic)
    {
        return $@"
public class {stageName}_{className} : IQueryStage<{inputType}, {outputType}>
{{
    public IEnumerable<{outputType}> Execute(IEnumerable<{inputType}> input)
    {{
        foreach (var item in input)
        {{
            {transformationLogic}
        }}
    }}
}}";
    }

    /// <summary>
    /// Template for optimized query execution pipeline.
    /// </summary>
    public static string QueryPipelineTemplate(
        string className,
        IEnumerable<string> stageNames,
        string sourceExpression)
    {
        var pipelineStages = stageNames.Aggregate(sourceExpression, 
            (current, stage) => $"new {stage}().Execute({current})");
            
        return $@"
public class {className} : ICompiledQuery
{{
    public void Run()
    {{
        var pipeline = {pipelineStages};
        
        foreach (var result in pipeline)
        {{
            yield return result;
        }}
    }}
}}";
    }
}

/// <summary>
/// Interface for staged query transformations.
/// </summary>
public interface IQueryStage<TInput, TOutput>
{
    IEnumerable<TOutput> Execute(IEnumerable<TInput> input);
}

/// <summary>
/// Utility class for template pattern detection and selection.
/// </summary>
public static class TemplateSelector
{
    /// <summary>
    /// Determines the optimal template for a given query pattern.
    /// </summary>
    public static QueryTemplate SelectTemplate(QueryPattern pattern)
    {
        if (pattern.HasAggregations && pattern.HasJoins)
            return QueryTemplate.ComplexAggregationWithJoins;
        if (pattern.HasAggregations)
            return QueryTemplate.SimpleAggregation;
        if (pattern.HasJoins)
            return QueryTemplate.SimpleJoin;
        if (pattern.HasComplexFiltering)
            return QueryTemplate.FilteredProjection;
        
        return QueryTemplate.SimpleProjection;
    }
}

/// <summary>
/// Enumeration of available query templates.
/// </summary>
public enum QueryTemplate
{
    SimpleProjection,
    FilteredProjection,
    SimpleJoin,
    SimpleAggregation,
    ComplexAggregationWithJoins,
    StagedTransformation
}

/// <summary>
/// Query pattern analysis for template selection.
/// </summary>
public class QueryPattern
{
    public bool HasJoins { get; set; }
    public bool HasAggregations { get; set; }
    public bool HasComplexFiltering { get; set; }
    public bool HasGroupBy { get; set; }
    public bool HasOrderBy { get; set; }
    public int ComplexityScore { get; set; }
    public string[] RequiredFields { get; set; } = Array.Empty<string>();
    public Type[] RequiredTypes { get; set; } = Array.Empty<Type>();
}