using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private const int AggregateTableCompletionNodeCount = 5;

    private static TableBuildResult CompleteAggregateTableBuild(AggregateTableCompletion completion)
    {
        var nodes = new List<ExecutionNode>(
            completion.SourceSetup.Count +
            completion.Aggregate.LibraryNodes.Count +
            AggregateTableCompletionNodeCount);
        nodes.AddRange(completion.SourceSetup);
        nodes.AddRange(completion.Aggregate.LibraryNodes);
        nodes.Add(CreateTable(completion.ResultTable, completion.ResultShape));
        nodes.Add(completion.ContextCreation);
        nodes.Add(completion.Accumulation);
        nodes.Add(new ExecutionEnsureTableCapacity(
            completion.ResultTable,
            ExecutionCapacityHintCandidates.CreateCollectionCountCandidate(
                completion.ResultTable,
                completion.GroupsToFinalize)));
        nodes.Add(new ExecutionForEach(
            completion.FinalGroup,
            new ExecutionVariableRead(completion.GroupsToFinalize),
            completion.FinalBlock));

        return CompleteTableBuild(
            CreateAggregateResultShapes(completion.SourceShapes, completion.Aggregate.Group.Plan, completion.ResultShape),
            nodes,
            completion.ResultTable,
            completion.ResultShape,
            completion.PostOperations,
            completion.IsDistinct);
    }

    private ExecutionBlock CreateAggregateAccumulationBlock(
        PhysicalFilterNode? filter,
        RowShape sourceShape,
        ExecutionBlock body)
    {
        if (filter == null)
            return body;

        var condition = ExecutionExpressionConverter.Convert(filter.Predicate, sourceShape);
        return new ExecutionBlock([new ExecutionIf(condition, body)]);
    }

    private ExecutionBlock CreateAggregateAccumulationBlock(
        PhysicalFilterNode? filter,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        ExecutionBlock body)
    {
        if (filter == null)
            return body;

        var condition = ExecutionExpressionConverter.Convert(filter.Predicate, sourceLookup);
        return new ExecutionBlock([new ExecutionIf(condition, body)]);
    }

    private static LoweringAttempt<ExecutionAppendRow> CreateAggregateAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ProjectedField[] fields,
        AggregateFinalizationContext context)
    {
        var values = new List<ExecutionRowValue>();

        foreach (var field in fields)
        {
            var value = ConvertAggregateFinalProjectionExpression(field.Expression, context);
            if (!value.IsBuilt)
                return LoweringAttempt<ExecutionAppendRow>.Unsupported(
                    $"Execution IR aggregate-only lowering cannot convert projection {field.OutputName}={IrExpressionPrinter.Print(field.Expression)}. {value.UnsupportedReason}");

            values.Add(new ExecutionRowValue(
                field.OutputName,
                value.Value));
        }

        return LoweringAttempt<ExecutionAppendRow>.Built(new ExecutionAppendRow(
            resultTable,
            resultShape,
            values,
            SerialAppendMode));
    }

    private static LoweringAttempt<ExecutionAppendRow> CreateGroupedAggregateAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ProjectedField[] fields,
        AggregateFinalizationContext context)
    {
        var values = new List<ExecutionRowValue>(fields.Length);

        foreach (var field in fields)
        {
            var value = ConvertAggregateFinalProjectionExpression(field.Expression, context);
            if (!value.IsBuilt)
                return LoweringAttempt<ExecutionAppendRow>.Unsupported(
                    $"Execution IR grouped aggregate lowering cannot convert projection {field.OutputName}={IrExpressionPrinter.Print(field.Expression)}. {value.UnsupportedReason}");

            values.Add(new ExecutionRowValue(
                field.OutputName,
                value.Value));
        }

        return LoweringAttempt<ExecutionAppendRow>.Built(new ExecutionAppendRow(
            resultTable,
            resultShape,
            values,
            SerialAppendMode));
    }

}
