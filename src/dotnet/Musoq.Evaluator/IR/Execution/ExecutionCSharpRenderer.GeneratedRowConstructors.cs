using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static Dictionary<string, IReadOnlySet<GeneratedRowContextConstructor>>
        CollectGeneratedRowConstructorUsages(ExecutionBlock block)
    {
        var usages = new Dictionary<string, HashSet<GeneratedRowContextConstructor>>(StringComparer.Ordinal);
        var contextReadTypeNames = CollectGeneratedRowTypesWithContextReads(block);
        contextReadTypeNames.UnionWith(CollectGeneratedRowTypesUsedAsRowContexts(block));

        foreach (var appendRow in FlattenNodes(block).OfType<ExecutionAppendRow>().Select(NormalizeLazyContextSegments))
            AddGeneratedRowConstructorUsage(
                usages,
                appendRow.RowShape.TypeName,
                !contextReadTypeNames.Contains(appendRow.RowShape.TypeName)
                    ? GeneratedRowContextConstructor.NoContext
                    : ResolveGeneratedRowContextConstructor(appendRow));

        foreach (var createRow in FlattenNodes(block).OfType<ExecutionCreateGeneratedRow>().Select(NormalizeLazyContextSegments))
            AddGeneratedRowConstructorUsage(
                usages,
                createRow.RowShape.TypeName,
                !contextReadTypeNames.Contains(createRow.RowShape.TypeName)
                    ? GeneratedRowContextConstructor.NoContext
                    : ResolveGeneratedRowContextConstructor(createRow.ContextLayout, createRow.Contexts.Count));

        foreach (var valuesRows in FlattenNodes(block).OfType<ExecutionCreateValuesRows>())
            AddGeneratedRowConstructorUsage(
                usages,
                valuesRows.RowShape.TypeName,
                GeneratedRowContextConstructor.NoContext);

        foreach (var materialize in FlattenNodes(block).OfType<ExecutionMaterializeRecordListToTable>())
            AddGeneratedRowConstructorUsage(
                usages,
                materialize.RowShape.TypeName,
                GeneratedRowContextConstructor.NoContext);

        foreach (var projectTable in FlattenNodes(block).OfType<ExecutionProjectTable>())
            AddGeneratedRowConstructorUsage(
                usages,
                projectTable.RowShape.TypeName,
                projectTable.RowShape.Contexts.Count == 0 ||
                !contextReadTypeNames.Contains(projectTable.RowShape.TypeName)
                    ? GeneratedRowContextConstructor.NoContext
                    : GeneratedRowContextConstructor.ContextArray);

        return usages.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlySet<GeneratedRowContextConstructor>)pair.Value,
            StringComparer.Ordinal);
    }

    private static HashSet<string> CollectGeneratedRowTypesWithContextReads(ExecutionBlock block)
    {
        var variableTypeNamesByName = CollectGeneratedRowVariableTypeNames(block);
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var expression in ExecutionIrAnalysis.FlattenExpressions(block))
        {
            switch (expression)
            {
                case ExecutionFieldRead { AccessStrategy: GeneratedRowContextAccess generatedContext }:
                    result.Add(generatedContext.TypeName);
                    break;
                case ExecutionFieldRead { AccessStrategy: ContextAccess, Alias: { } alias }:
                    AddGeneratedRowTypeNames(alias, variableTypeNamesByName, result);
                    break;
                case ExecutionRowContextsRead { Row.GeneratedRowTypeName: { } generatedRowTypeName }:
                    result.Add(generatedRowTypeName);
                    break;
            }
        }

        return result;
    }

    private static IReadOnlySet<string> CollectGeneratedRowTypesUsedAsRowContexts(ExecutionBlock block)
    {
        var rowTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var appendRow in FlattenNodes(block).OfType<ExecutionAppendRow>().Select(NormalizeLazyContextSegments))
            CollectGeneratedRowTypesUsedAsRowContexts(appendRow.ContextLayout, rowTypeNames);

        foreach (var createRow in FlattenNodes(block).OfType<ExecutionCreateGeneratedRow>().Select(NormalizeLazyContextSegments))
            CollectGeneratedRowTypesUsedAsRowContexts(createRow.ContextLayout, rowTypeNames);

        foreach (var parallelProject in FlattenNodes(block).OfType<ExecutionParallelFilterProjectLoop>())
        {
            var appendRow = NormalizeLazyContextSegments(parallelProject.AppendRow);
            CollectGeneratedRowTypesUsedAsRowContexts(appendRow.ContextLayout, rowTypeNames);
        }

        foreach (var expression in ExecutionIrAnalysis.FlattenExpressions(block))
            CollectGeneratedRowTypesUsedAsRowContexts(expression, rowTypeNames);

        return rowTypeNames;
    }

    private static IReadOnlySet<string> CollectGeneratedRowTypesUsedAtPublicBoundary(ExecutionBlock block)
    {
        var rowShapesByTableName = CreateTableRowShapeMap(block);
        var rowTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var returnTable in FlattenNodes(block).OfType<ExecutionReturnTable>())
        {
            if (rowShapesByTableName.TryGetValue(returnTable.Table.Name, out var rowShape))
                rowTypeNames.Add(rowShape.TypeName);
        }

        return rowTypeNames;
    }

    private static void CollectGeneratedRowTypesUsedAsRowContexts(
        ExecutionContextLayout? contextLayout,
        HashSet<string> rowTypeNames)
    {
        if (contextLayout == null)
            return;

        CollectGeneratedRowTypesUsedAsRowContexts(contextLayout.Segments, rowTypeNames);
    }

    private static void CollectGeneratedRowTypesUsedAsRowContexts(
        IEnumerable<ExecutionContextSegment> segments,
        HashSet<string> rowTypeNames)
    {
        foreach (var segment in segments)
        {
            if (segment.Kind == ExecutionContextSegmentKind.Row)
                AddGeneratedRowTypeName(segment.Value, rowTypeNames);

            CollectGeneratedRowTypesUsedAsRowContexts(segment.Value, rowTypeNames);
        }
    }

    private static void CollectGeneratedRowTypesUsedAsRowContexts(
        ExecutionExpression expression,
        HashSet<string> rowTypeNames)
    {
        switch (expression)
        {
            case ExecutionRowContextsRead contextsRead:
                AddGeneratedRowTypeName(contextsRead.Row.GeneratedRowTypeName, rowTypeNames);
                break;
            case ExecutionContextArray contextArray:
                CollectGeneratedRowTypesUsedAsRowContexts(contextArray.Segments, rowTypeNames);
                break;
        }
    }

    private static void AddGeneratedRowConstructorUsage(
        Dictionary<string, HashSet<GeneratedRowContextConstructor>> usages,
        string typeName,
        GeneratedRowContextConstructor constructor)
    {
        if (!usages.TryGetValue(typeName, out var constructors))
        {
            constructors = [];
            usages.Add(typeName, constructors);
        }

        constructors.Add(constructor);
    }

    private static void AddGeneratedRowTypeName(
        ExecutionExpression expression,
        HashSet<string> rowTypeNames)
    {
        if (expression is ExecutionVariableRead variableRead)
            AddGeneratedRowTypeName(variableRead.Variable.GeneratedRowTypeName, rowTypeNames);
    }

    private static void AddGeneratedRowTypeNames(
        string variableName,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        HashSet<string> rowTypeNames)
    {
        if (!variableTypeNamesByName.TryGetValue(variableName, out var typeNames))
            return;

        foreach (var typeName in typeNames)
            AddGeneratedRowTypeName(typeName, rowTypeNames);
    }

    private static void AddGeneratedRowTypeName(
        string? rowTypeName,
        HashSet<string> rowTypeNames)
    {
        if (!string.IsNullOrWhiteSpace(rowTypeName))
            rowTypeNames.Add(rowTypeName);
    }

    private static GeneratedRowContextConstructor ResolveGeneratedRowContextConstructor(ExecutionAppendRow appendRow)
    {
        return ResolveGeneratedRowContextConstructor(appendRow.ContextLayout, appendRow.Contexts.Count);
    }

    private static GeneratedRowContextConstructor ResolveGeneratedRowContextConstructor(
        ExecutionContextLayout? contextLayout,
        int contextCount)
    {
        var layoutConstructor = ResolveContextLayoutConstructor(contextLayout, contextCount);
        if (layoutConstructor.HasValue)
            return layoutConstructor.Value;

        return contextCount == 0
            ? GeneratedRowContextConstructor.NoContext
            : GeneratedRowContextConstructor.ContextArray;
    }

    private static GeneratedRowContextConstructor? ResolveContextLayoutConstructor(
        ExecutionContextLayout? contextLayout,
        int contextCount)
    {
        if (contextLayout == null ||
            contextLayout.Segments.Count == 0 ||
            contextLayout.Segments.Sum(static segment => segment.Count) != contextCount)
        {
            return null;
        }

        if (contextLayout.Segments.Count > 2)
            return contextLayout.Segments.All(static segment => segment.Kind == ExecutionContextSegmentKind.Single)
                ? GeneratedRowContextConstructor.SingleContexts
                : null;

        if (contextLayout.Segments.Count == 1)
            return ResolveSingleSegmentConstructor(contextLayout.Segments[0].Kind);

        return ResolveTwoSegmentConstructor(
            contextLayout.Segments[0].Kind,
            contextLayout.Segments[1].Kind);
    }

    private static GeneratedRowContextConstructor? ResolveSingleSegmentConstructor(ExecutionContextSegmentKind kind)
    {
        return kind switch
        {
            ExecutionContextSegmentKind.Single => GeneratedRowContextConstructor.SingleContext,
            ExecutionContextSegmentKind.Array => GeneratedRowContextConstructor.ContextArray,
            ExecutionContextSegmentKind.Row => GeneratedRowContextConstructor.ContextRow,
            _ => null
        };
    }

    private static GeneratedRowContextConstructor? ResolveTwoSegmentConstructor(
        ExecutionContextSegmentKind left,
        ExecutionContextSegmentKind right)
    {
        return (left, right) switch
        {
            (ExecutionContextSegmentKind.Single, ExecutionContextSegmentKind.Single) =>
                GeneratedRowContextConstructor.TwoSingleContexts,
            (ExecutionContextSegmentKind.Row, ExecutionContextSegmentKind.Row) =>
                GeneratedRowContextConstructor.TwoContextRows,
            (ExecutionContextSegmentKind.Array, ExecutionContextSegmentKind.Array) =>
                GeneratedRowContextConstructor.TwoContextArrays,
            (ExecutionContextSegmentKind.Array, ExecutionContextSegmentKind.Single) =>
                GeneratedRowContextConstructor.ContextArrayAndSingleContext,
            (ExecutionContextSegmentKind.Row, ExecutionContextSegmentKind.Single) =>
                GeneratedRowContextConstructor.ContextRowAndSingleContext,
            (ExecutionContextSegmentKind.Single, ExecutionContextSegmentKind.Array) =>
                GeneratedRowContextConstructor.SingleContextAndContextArray,
            (ExecutionContextSegmentKind.Single, ExecutionContextSegmentKind.Row) =>
                GeneratedRowContextConstructor.SingleContextAndContextRow,
            (ExecutionContextSegmentKind.Row, ExecutionContextSegmentKind.Array) =>
                GeneratedRowContextConstructor.ContextRowAndContextArray,
            (ExecutionContextSegmentKind.Array, ExecutionContextSegmentKind.Row) =>
                GeneratedRowContextConstructor.ContextArrayAndContextRow,
            _ => null
        };
    }

}
