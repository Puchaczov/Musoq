using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

internal static class GeneratedRowContextPruner
{
    public static ExecutionPlan Prune(ExecutionPlan plan)
    {
        var contextCarryingTypeNames = ResolveContextCarryingGeneratedRowTypeNames(plan);
        if (contextCarryingTypeNames.Count == 0)
            return plan;

        var contextConsumerTypeNames = ResolveContextConsumerGeneratedRowTypeNames(
            plan,
            contextCarryingTypeNames);
        var hasContextWritesToContextFreeShapes = HasContextWritesToContextFreeShapes(plan);
        if (contextConsumerTypeNames.Count == contextCarryingTypeNames.Count &&
            !hasContextWritesToContextFreeShapes)
        {
            return plan;
        }

        var prunedShapesByTypeName = plan.Shapes
            .SelectMany(GetGeneratedRowShapes)
            .Where(shape => contextCarryingTypeNames.Contains(shape.TypeName) &&
                            !contextConsumerTypeNames.Contains(shape.TypeName))
            .ToDictionary(
                static shape => shape.TypeName,
                static shape => shape with { Contexts = [] },
                StringComparer.Ordinal);
        var prunedHashPayloadShapesByTypeName = plan.Shapes
            .OfType<HashPayloadShape>()
            .Where(shape => contextCarryingTypeNames.Contains(shape.TypeName) &&
                            !contextConsumerTypeNames.Contains(shape.TypeName))
            .GroupBy(static shape => shape.TypeName, StringComparer.Ordinal)
            .Select(static group => group.First())
            .ToDictionary(
                static shape => shape.TypeName,
                static shape => shape with { Contexts = [] },
                StringComparer.Ordinal);

        foreach (var appendRow in ExecutionIrAnalysis.CollectNodes<ExecutionAppendRow>(plan.Body))
        {
            if (ShouldPruneContextWrite(
                    appendRow.RowShape,
                    appendRow.Contexts,
                    appendRow.ContextLayout,
                    contextCarryingTypeNames,
                    contextConsumerTypeNames))
            {
                prunedShapesByTypeName.TryAdd(appendRow.RowShape.TypeName, appendRow.RowShape with { Contexts = [] });
            }
        }

        foreach (var createRow in ExecutionIrAnalysis.CollectNodes<ExecutionCreateGeneratedRow>(plan.Body))
        {
            if (ShouldPruneContextWrite(
                    createRow.RowShape,
                    createRow.Contexts,
                    createRow.ContextLayout,
                    contextCarryingTypeNames,
                    contextConsumerTypeNames))
            {
                prunedShapesByTypeName.TryAdd(createRow.RowShape.TypeName, createRow.RowShape with { Contexts = [] });
            }
        }

        return prunedShapesByTypeName.Count == 0 && prunedHashPayloadShapesByTypeName.Count == 0
            ? plan
            : new Rewriter(prunedShapesByTypeName, prunedHashPayloadShapesByTypeName).RewritePlan(plan);
    }

    private static HashSet<string> ResolveContextCarryingGeneratedRowTypeNames(ExecutionPlan plan)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var shape in plan.Shapes.SelectMany(GetGeneratedRowShapes))
            if (shape.Contexts.Count > 0)
                result.Add(shape.TypeName);

        foreach (var shape in plan.Shapes.OfType<HashPayloadShape>())
            if (shape.Contexts.Count > 0)
                result.Add(shape.TypeName);

        foreach (var appendRow in ExecutionIrAnalysis.CollectNodes<ExecutionAppendRow>(plan.Body))
            if (appendRow.Contexts.Count > 0 || appendRow.ContextLayout != null)
                result.Add(appendRow.RowShape.TypeName);

        foreach (var parallelProject in ExecutionIrAnalysis.CollectNodes<ExecutionParallelFilterProjectLoop>(plan.Body))
            if (parallelProject.AppendRow.Contexts.Count > 0 || parallelProject.AppendRow.ContextLayout != null)
                result.Add(parallelProject.AppendRow.RowShape.TypeName);

        foreach (var createRow in ExecutionIrAnalysis.CollectNodes<ExecutionCreateGeneratedRow>(plan.Body))
            if (createRow.Contexts.Count > 0 || createRow.ContextLayout != null)
                result.Add(createRow.RowShape.TypeName);

        foreach (var createPayload in ExecutionIrAnalysis.CollectNodes<ExecutionCreateHashPayload>(plan.Body))
            if (createPayload.PayloadShape.Contexts.Count > 0)
                result.Add(createPayload.PayloadShape.TypeName);

        return result;
    }

    private static bool HasContextWritesToContextFreeShapes(ExecutionPlan plan)
    {
        return ExecutionIrAnalysis
                   .CollectNodes<ExecutionAppendRow>(plan.Body)
                   .Any(static appendRow => appendRow.RowShape.Contexts.Count == 0 &&
                                            (appendRow.Contexts.Count > 0 || appendRow.ContextLayout != null)) ||
               ExecutionIrAnalysis
                   .CollectNodes<ExecutionParallelFilterProjectLoop>(plan.Body)
                   .Any(static parallelProject => parallelProject.AppendRow.RowShape.Contexts.Count == 0 &&
                                                  (parallelProject.AppendRow.Contexts.Count > 0 ||
                                                   parallelProject.AppendRow.ContextLayout != null)) ||
               ExecutionIrAnalysis
                   .CollectNodes<ExecutionCreateGeneratedRow>(plan.Body)
                   .Any(static createRow => createRow.RowShape.Contexts.Count == 0 &&
                                            (createRow.Contexts.Count > 0 || createRow.ContextLayout != null));
    }

    private static bool ShouldPruneContextWrite(
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout,
        IReadOnlySet<string> contextCarryingTypeNames,
        IReadOnlySet<string> contextConsumerTypeNames)
    {
        return (rowShape.Contexts.Count == 0 && (contexts.Count > 0 || contextLayout != null)) ||
               (contextCarryingTypeNames.Contains(rowShape.TypeName) &&
                !contextConsumerTypeNames.Contains(rowShape.TypeName));
    }

    private static IEnumerable<GeneratedRowShape> GetGeneratedRowShapes(RowShape shape)
    {
        return shape switch
        {
            GeneratedRowShape generated => [generated],
            ValuesRowShape values => [values.GeneratedShape],
            _ => []
        };
    }

    private static HashSet<string> ResolveContextConsumerGeneratedRowTypeNames(
        ExecutionPlan plan,
        IReadOnlySet<string> contextCarryingTypeNames)
    {
        var variableTypeNamesByName = CollectGeneratedRowVariableTypeNames(plan.Body);
        var result = ResolveDirectContextConsumerGeneratedRowTypeNames(
            plan.Body,
            variableTypeNamesByName,
            contextCarryingTypeNames);
        var contextWrites = CollectContextWriteConsumers(
            plan.Body,
            variableTypeNamesByName,
            contextCarryingTypeNames);

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var contextWrite in contextWrites)
            {
                if (!result.Contains(contextWrite.TargetTypeName))
                    continue;

                foreach (var sourceTypeName in contextWrite.SourceTypeNames)
                    changed |= result.Add(sourceTypeName);
            }
        }

        return result;
    }

    private static HashSet<string> ResolveDirectContextConsumerGeneratedRowTypeNames(
        ExecutionBlock block,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        IReadOnlySet<string> contextCarryingTypeNames)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var expression in FlattenNonContextWriteExpressions(block))
            AddDirectContextConsumer(
                expression,
                variableTypeNamesByName,
                contextCarryingTypeNames,
                result);

        return result;
    }

    private static IReadOnlyList<ContextWriteConsumer> CollectContextWriteConsumers(
        ExecutionBlock block,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        IReadOnlySet<string> contextCarryingTypeNames)
    {
        var result = new List<ContextWriteConsumer>();

        foreach (var appendRow in ExecutionIrAnalysis.CollectNodes<ExecutionAppendRow>(block))
            AddContextWriteConsumer(
                result,
                appendRow.RowShape.TypeName,
                appendRow.Contexts,
                appendRow.ContextLayout,
                variableTypeNamesByName,
                contextCarryingTypeNames);

        foreach (var parallelProject in ExecutionIrAnalysis.CollectNodes<ExecutionParallelFilterProjectLoop>(block))
            AddContextWriteConsumer(
                result,
                parallelProject.AppendRow.RowShape.TypeName,
                parallelProject.AppendRow.Contexts,
                parallelProject.AppendRow.ContextLayout,
                variableTypeNamesByName,
                contextCarryingTypeNames);

        foreach (var createRow in ExecutionIrAnalysis.CollectNodes<ExecutionCreateGeneratedRow>(block))
            AddContextWriteConsumer(
                result,
                createRow.RowShape.TypeName,
                createRow.Contexts,
                createRow.ContextLayout,
                variableTypeNamesByName,
                contextCarryingTypeNames);

        return result;
    }

    private static void AddContextWriteConsumer(
        ICollection<ContextWriteConsumer> result,
        string targetTypeName,
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        IReadOnlySet<string> contextCarryingTypeNames)
    {
        if (contexts.Count == 0 && contextLayout == null)
            return;

        var sourceTypeNames = new HashSet<string>(StringComparer.Ordinal);
        AddContextExpressionConsumers(
            contexts,
            contextLayout,
            variableTypeNamesByName,
            contextCarryingTypeNames,
            sourceTypeNames);

        if (sourceTypeNames.Count > 0)
            result.Add(new ContextWriteConsumer(targetTypeName, sourceTypeNames));
    }

    private static void AddDirectContextConsumer(
        ExecutionExpression expression,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        IReadOnlySet<string> contextCarryingTypeNames,
        HashSet<string> result)
    {
        switch (expression)
        {
            case ExecutionFieldRead { AccessStrategy: GeneratedRowContextAccess generatedContext }:
                if (contextCarryingTypeNames.Contains(generatedContext.TypeName))
                    result.Add(generatedContext.TypeName);
                break;

            case ExecutionFieldRead { AccessStrategy: ContextAccess, Alias: { } alias }:
                TryAddAliasGeneratedRowTypes(
                    alias,
                    variableTypeNamesByName,
                    contextCarryingTypeNames,
                    result);
                break;

            case ExecutionRowContextsRead rowContextsRead:
                TryAddVariableGeneratedRowTypes(
                    rowContextsRead.Row,
                    variableTypeNamesByName,
                    contextCarryingTypeNames,
                    result);
                break;
        }
    }

    private static IEnumerable<ExecutionExpression> FlattenNonContextWriteExpressions(ExecutionBlock block)
    {
        foreach (var node in ExecutionIrAnalysis.FlattenNodes(block))
        {
            foreach (var expression in GetNonContextWriteLocalExpressions(node))
            {
                foreach (var childExpression in ExecutionIrAnalysis.FlattenExpressions(expression))
                    yield return childExpression;
            }
        }
    }

    private static IEnumerable<ExecutionExpression> GetNonContextWriteLocalExpressions(ExecutionNode node)
    {
        return node switch
        {
            ExecutionAppendRow appendRow => appendRow.Values.Select(static value => value.Value),
            ExecutionCreateGeneratedRow createRow => createRow.Values.Select(static value => value.Value),
            ExecutionParallelFilterProjectLoop parallelProject => new[]
                {
                    parallelProject.SourceRows
                }
                .Concat(parallelProject.Predicate == null ? [] : [parallelProject.Predicate])
                .Concat(parallelProject.AppendRow.Values.Select(static value => value.Value)),
            _ => ExecutionIrAnalysis.GetNodeExpressions(node)
        };
    }

    private static void AddContextExpressionConsumers(
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        IReadOnlySet<string> contextCarryingTypeNames,
        HashSet<string> result)
    {
        foreach (var context in contexts)
            AddContextExpressionConsumer(context, variableTypeNamesByName, contextCarryingTypeNames, result);

        if (contextLayout == null)
            return;

        foreach (var segment in contextLayout.Segments)
            if (segment.Kind == ExecutionContextSegmentKind.Row)
                AddContextExpressionConsumer(segment.Value, variableTypeNamesByName, contextCarryingTypeNames, result);
    }

    private static void AddContextExpressionConsumer(
        ExecutionExpression expression,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        IReadOnlySet<string> contextCarryingTypeNames,
        HashSet<string> result)
    {
        foreach (var childExpression in ExecutionIrAnalysis.FlattenExpressions(expression))
        {
            if (childExpression is ExecutionVariableRead variableRead)
            {
                TryAddVariableGeneratedRowTypes(
                    variableRead.Variable,
                    variableTypeNamesByName,
                    contextCarryingTypeNames,
                    result);
            }

            AddDirectContextConsumer(
                childExpression,
                variableTypeNamesByName,
                contextCarryingTypeNames,
                result);
        }
    }

    private static Dictionary<string, HashSet<string>> CollectGeneratedRowVariableTypeNames(ExecutionBlock block)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var node in ExecutionIrAnalysis.FlattenNodes(block))
        {
            foreach (var variable in ExecutionNodeFacts.GetDeclaredVariables(node))
            {
                if (!string.IsNullOrWhiteSpace(variable.GeneratedRowTypeName))
                    AddVariableType(result, variable.Name, variable.GeneratedRowTypeName);
            }

            AddGeneratedRowVariableTypeFromNode(result, node);
            AddGeneratedRowItemTypeFromSource(result, node);
        }

        return result;
    }

    private static void AddGeneratedRowVariableTypeFromNode(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionNode node)
    {
        switch (node)
        {
            case ExecutionLet let:
                AddGeneratedRowVariableTypeFromSource(variableTypeNamesByName, let.Variable, let.Value);
                break;
            case ExecutionAssign assign:
                AddGeneratedRowVariableTypeFromSource(variableTypeNamesByName, assign.Variable, assign.Value);
                break;
            case ExecutionMaterializeList { GeneratedRowShape: { } shape } materialize:
                AddVariableType(variableTypeNamesByName, materialize.Buffer.Name, shape.TypeName);
                break;
            case ExecutionMaterializeFilteredList { GeneratedRowShape: { } shape } materialize:
                AddVariableType(variableTypeNamesByName, materialize.Buffer.Name, shape.TypeName);
                break;
        }
    }

    private static void AddGeneratedRowVariableTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionVariable variable,
        ExecutionExpression source)
    {
        if (TryResolveGeneratedRowTypeName(source, out var typeName))
            AddVariableType(variableTypeNamesByName, variable.Name, typeName);
    }

    private static void AddGeneratedRowItemTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionNode node)
    {
        switch (node)
        {
            case ExecutionForEach loop:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, loop.Item, loop.Source);
                break;
            case ExecutionForEachWithOrdinality loop:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, loop.Item, loop.Source);
                break;
            case ExecutionForEachIndexed loop:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, loop.Item, loop.Source);
                break;
            case ExecutionMaterializeFilteredList materialize:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, materialize.Item, materialize.Source);
                break;
        }
    }

    private static void AddGeneratedRowItemTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionVariable item,
        ExecutionExpression source)
    {
        if (TryResolveGeneratedRowTypeName(source, out var typeName))
        {
            AddVariableType(variableTypeNamesByName, item.Name, typeName);
            return;
        }

        if (source is ExecutionVariableRead variableRead &&
            variableTypeNamesByName.TryGetValue(variableRead.Variable.Name, out var typeNames))
        {
            foreach (var sourceTypeName in typeNames)
                AddVariableType(variableTypeNamesByName, item.Name, sourceTypeName);
        }
    }

    private static void AddGeneratedRowItemTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionVariable item,
        ExecutionVariable source)
    {
        if (!string.IsNullOrWhiteSpace(source.GeneratedRowTypeName))
        {
            AddVariableType(variableTypeNamesByName, item.Name, source.GeneratedRowTypeName);
            return;
        }

        if (!variableTypeNamesByName.TryGetValue(source.Name, out var typeNames))
            return;

        foreach (var sourceTypeName in typeNames)
            AddVariableType(variableTypeNamesByName, item.Name, sourceTypeName);
    }

    private static bool TryResolveGeneratedRowTypeName(ExecutionExpression source, out string typeName)
    {
        switch (source)
        {
            case ExecutionStoredTableRows { GeneratedRowShape: { } shape }:
                typeName = shape.TypeName;
                return true;
            case ExecutionVariableRead { Variable.GeneratedRowTypeName: { } generatedRowTypeName }:
                typeName = generatedRowTypeName;
                return true;
            case ExecutionRowStream { Variable.GeneratedRowTypeName: { } generatedRowTypeName }:
                typeName = generatedRowTypeName;
                return true;
            default:
                typeName = string.Empty;
                return false;
        }
    }

    private static void AddVariableType(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        string variableName,
        string typeName)
    {
        if (!variableTypeNamesByName.TryGetValue(variableName, out var typeNames))
        {
            typeNames = new HashSet<string>(StringComparer.Ordinal);
            variableTypeNamesByName.Add(variableName, typeNames);
        }

        typeNames.Add(typeName);
    }

    private static bool TryAddAliasGeneratedRowTypes(
        string? alias,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        IReadOnlySet<string> contextCarryingTypeNames,
        HashSet<string> result)
    {
        return !string.IsNullOrWhiteSpace(alias) &&
               TryAddGeneratedRowTypes(alias, variableTypeNamesByName, contextCarryingTypeNames, result);
    }

    private static bool TryAddVariableGeneratedRowTypes(
        ExecutionVariable variable,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        IReadOnlySet<string> contextCarryingTypeNames,
        HashSet<string> result)
    {
        if (!string.IsNullOrWhiteSpace(variable.GeneratedRowTypeName))
        {
            if (contextCarryingTypeNames.Contains(variable.GeneratedRowTypeName))
                result.Add(variable.GeneratedRowTypeName);
            return true;
        }

        return TryAddGeneratedRowTypes(variable.Name, variableTypeNamesByName, contextCarryingTypeNames, result);
    }

    private static bool TryAddGeneratedRowTypes(
        string variableName,
        IReadOnlyDictionary<string, HashSet<string>> variableTypeNamesByName,
        IReadOnlySet<string> contextCarryingTypeNames,
        HashSet<string> result)
    {
        if (!variableTypeNamesByName.TryGetValue(variableName, out var typeNames))
            return false;

        foreach (var typeName in typeNames)
            if (contextCarryingTypeNames.Contains(typeName))
                result.Add(typeName);

        return true;
    }

    private sealed record ContextWriteConsumer(
        string TargetTypeName,
        IReadOnlySet<string> SourceTypeNames);

    private sealed class Rewriter(
        IReadOnlyDictionary<string, GeneratedRowShape> prunedShapesByTypeName,
        IReadOnlyDictionary<string, HashPayloadShape> prunedHashPayloadShapesByTypeName) : ExecutionIrRewriter
    {
        public override ExecutionPlan RewritePlan(ExecutionPlan plan)
        {
            var body = RewriteBlock(plan.Body);
            var shapes = RewriteShapes(plan.Shapes);
            var finalResult = RewriteFinalResult(plan.FinalResult);

            return ReferenceEquals(body, plan.Body) &&
                   ReferenceEquals(shapes, plan.Shapes) &&
                   ReferenceEquals(finalResult, plan.FinalResult)
                ? plan
                : plan with { Shapes = shapes, Body = body, FinalResult = finalResult };
        }

        private FinalShapeResult? RewriteFinalResult(FinalShapeResult? finalResult)
        {
            if (finalResult == null ||
                !TryRewriteGeneratedRowShape(finalResult.Shape, out var rowShape))
            {
                return finalResult;
            }

            return finalResult with
            {
                Shape = rowShape,
                ColumnMetadata = new ExecutionColumnMetadata(
                    finalResult.ColumnMetadata.ReferenceName,
                    rowShape.Fields
                        .Select(static field => ExecutionColumnMetadataFields.FromFieldBinding(field))
                        .ToArray(),
                    finalResult.ColumnMetadata.Kind)
            };
        }

        protected override ExecutionNode RewriteCreateTable(ExecutionCreateTable node)
        {
            var rewritten = (ExecutionCreateTable)base.RewriteCreateTable(node);
            return TryRewriteGeneratedRowShape(rewritten.RowShape, out var rowShape)
                ? rewritten with { RowShape = rowShape }
                : rewritten;
        }

        protected override ExecutionNode RewriteCreateValuesRows(ExecutionCreateValuesRows node)
        {
            var rewritten = (ExecutionCreateValuesRows)base.RewriteCreateValuesRows(node);
            return TryRewriteGeneratedRowShape(rewritten.RowShape, out var rowShape)
                ? rewritten with { RowShape = rowShape }
                : rewritten;
        }

        protected override ExecutionNode RewriteCreateGeneratedRow(ExecutionCreateGeneratedRow node)
        {
            var rewritten = (ExecutionCreateGeneratedRow)base.RewriteCreateGeneratedRow(node);
            return TryRewriteGeneratedRowShape(rewritten.RowShape, out var rowShape)
                ? rewritten with { RowShape = rowShape, Contexts = [], ContextLayout = null }
                : rewritten;
        }

        protected override ExecutionNode RewriteCreateHashPayload(ExecutionCreateHashPayload node)
        {
            var rewritten = (ExecutionCreateHashPayload)base.RewriteCreateHashPayload(node);
            return TryRewriteHashPayloadShape(rewritten.PayloadShape, out var payloadShape)
                ? rewritten with
                {
                    PayloadShape = payloadShape,
                    Values = rewritten.Values.Take(payloadShape.Fields.Count).ToArray()
                }
                : rewritten;
        }

        protected override ExecutionNode RewriteCteSidecarAppendRewriteCandidate(
            ExecutionCteSidecarAppendRewriteCandidate node)
        {
            var rewritten = (ExecutionCteSidecarAppendRewriteCandidate)base.RewriteCteSidecarAppendRewriteCandidate(node);
            var indexes = RewriteCteSidecarAppendIndexes(rewritten.Indexes);
            return ReferenceEquals(indexes, rewritten.Indexes)
                ? rewritten
                : rewritten with { Indexes = indexes };
        }

        protected override ExecutionNode RewriteAppendRow(ExecutionAppendRow node)
        {
            var rewritten = (ExecutionAppendRow)base.RewriteAppendRow(node);
            return TryRewriteGeneratedRowShape(rewritten.RowShape, out var rowShape)
                ? rewritten with { RowShape = rowShape, Contexts = [], ContextLayout = null }
                : rewritten;
        }

        protected override ExecutionNode RewriteMaterializeList(ExecutionMaterializeList node)
        {
            var rewritten = (ExecutionMaterializeList)base.RewriteMaterializeList(node);
            return rewritten.GeneratedRowShape is not null &&
                   TryRewriteGeneratedRowShape(rewritten.GeneratedRowShape, out var rowShape)
                ? rewritten with { GeneratedRowShape = rowShape }
                : rewritten;
        }

        protected override ExecutionNode RewriteMaterializeFilteredList(ExecutionMaterializeFilteredList node)
        {
            var rewritten = (ExecutionMaterializeFilteredList)base.RewriteMaterializeFilteredList(node);
            return rewritten.GeneratedRowShape is not null &&
                   TryRewriteGeneratedRowShape(rewritten.GeneratedRowShape, out var rowShape)
                ? rewritten with { GeneratedRowShape = rowShape }
                : rewritten;
        }

        protected override ExecutionNode RewriteProjectTable(ExecutionProjectTable node)
        {
            var rewritten = (ExecutionProjectTable)base.RewriteProjectTable(node);
            return TryRewriteGeneratedRowShape(rewritten.RowShape, out var rowShape)
                ? rewritten with { RowShape = rowShape }
                : rewritten;
        }

        protected override ExecutionNode RewriteMaterializeRecordListToTable(
            ExecutionMaterializeRecordListToTable node)
        {
            var rewritten = (ExecutionMaterializeRecordListToTable)base.RewriteMaterializeRecordListToTable(node);
            return TryRewriteGeneratedRowShape(rewritten.RowShape, out var rowShape)
                ? rewritten with { RowShape = rowShape }
                : rewritten;
        }

        protected override ExecutionExpression RewriteStoredTableRows(ExecutionStoredTableRows expression)
        {
            var rewritten = (ExecutionStoredTableRows)base.RewriteStoredTableRows(expression);
            return rewritten.GeneratedRowShape is not null &&
                   TryRewriteGeneratedRowShape(rewritten.GeneratedRowShape, out var rowShape)
                ? rewritten with { GeneratedRowShape = rowShape }
                : rewritten;
        }

        private IReadOnlyList<RowShape> RewriteShapes(IReadOnlyList<RowShape> shapes)
        {
            RowShape[]? rewritten = null;

            for (var index = 0; index < shapes.Count; index++)
            {
                var shape = shapes[index];
                var current = RewriteShape(shape);

                if (ReferenceEquals(current, shape) && rewritten == null)
                    continue;

                rewritten ??= CopyPrefix(shapes, index);
                rewritten[index] = current;
            }

            return rewritten ?? shapes;
        }

        private RowShape RewriteShape(RowShape shape)
        {
            return shape switch
            {
                GeneratedRowShape generated when TryRewriteGeneratedRowShape(generated, out var pruned) => pruned,
                ValuesRowShape values when TryRewriteGeneratedRowShape(values.GeneratedShape, out var pruned) =>
                    values with { GeneratedShape = pruned },
                HashPayloadShape payload when TryRewriteHashPayloadShape(payload, out var pruned) => pruned,
                _ => shape
            };
        }

        private bool TryRewriteGeneratedRowShape(GeneratedRowShape shape, out GeneratedRowShape pruned)
        {
            return prunedShapesByTypeName.TryGetValue(shape.TypeName, out pruned!);
        }

        private bool TryRewriteHashPayloadShape(HashPayloadShape shape, out HashPayloadShape pruned)
        {
            return prunedHashPayloadShapesByTypeName.TryGetValue(shape.TypeName, out pruned!);
        }

        private IReadOnlyList<ExecutionCteSidecarAppendIndexSpec> RewriteCteSidecarAppendIndexes(
            IReadOnlyList<ExecutionCteSidecarAppendIndexSpec> indexes)
        {
            ExecutionCteSidecarAppendIndexSpec[]? rewritten = null;

            for (var index = 0; index < indexes.Count; index++)
            {
                var current = indexes[index];
                var next = RewriteCteSidecarAppendIndex(current);
                if (ReferenceEquals(next, current) && rewritten == null)
                    continue;

                if (rewritten == null)
                {
                    rewritten = new ExecutionCteSidecarAppendIndexSpec[indexes.Count];
                    for (var prefix = 0; prefix < index; prefix++)
                        rewritten[prefix] = indexes[prefix];
                }

                rewritten[index] = next;
            }

            return rewritten ?? indexes;
        }

        private ExecutionCteSidecarAppendIndexSpec RewriteCteSidecarAppendIndex(
            ExecutionCteSidecarAppendIndexSpec index)
        {
            if (index.PayloadShape == null ||
                !TryRewriteHashPayloadShape(index.PayloadShape, out var payloadShape))
            {
                return index;
            }

            return index with
            {
                PayloadShape = payloadShape,
                PayloadValues = index.PayloadValues.Take(payloadShape.Fields.Count).ToArray()
            };
        }

        private static RowShape[] CopyPrefix(IReadOnlyList<RowShape> shapes, int length)
        {
            var copy = new RowShape[shapes.Count];
            for (var index = 0; index < length; index++)
                copy[index] = shapes[index];

            return copy;
        }
    }
}
