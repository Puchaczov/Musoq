using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static partial class FinalOutputPlanner
{
    private static FinalOutputPlan SelectOwnedTerminalBuffer(ExecutionPlan plan, FinalOutputPlan output)
    {
        if (!output.IsSupported || output.UsesGeneratedRow || !output.BufferFinalRows ||
            ExecutionIrAnalysis.FlattenNodes(plan.Body).Any(static node =>
                node is ExecutionParallelBlock or ExecutionParallelFilterProjectLoop))
            return output;

        var writer = plan.Body.Nodes.LastOrDefault(node => node switch
        {
            ExecutionSortTable sort => sort.Target.Name == output.Result.TableName,
            ExecutionMaterializeRecordListToTable materialize => materialize.Target.Name == output.Result.TableName,
            _ => false
        });
        var owned = writer switch
        {
            ExecutionSortTable sort => PlanOwnedShapeRows(plan.Body, output, sort),
            ExecutionMaterializeRecordListToTable materialize => PlanOwnedRecords(plan.Body, output, materialize),
            _ => null
        };
        if (owned != null && !HasTerminalResultUse(plan.Body, writer!, output.Result.TableName))
            return output;
        return owned == null ? output : output with { OwnedBuffer = owned };
    }

    private static bool HasTerminalResultUse(ExecutionBlock body, ExecutionNode writer, string target)
    {
        if (body.Nodes.SkipWhile(node => !ReferenceEquals(node, writer)).Skip(1)
            .Any(static node => node is not (ExecutionReturnTable or ExecutionPhaseBoundary)))
            return false;
        foreach (var node in ExecutionIrAnalysis.FlattenNodes(body))
        {
            if (ReferenceEquals(node, writer) || node is ExecutionReturnTable returned && returned.Table.Name == target)
                continue;
            if (ExecutionNodeFacts.GetDirectVariableReferences(node).Any(variable => variable.Name == target) ||
                ExecutionIrAnalysis.ContainsVariableUse(ExecutionIrAnalysis.GetNodeExpressions(node), target))
                return false;
        }
        return true;
    }

    private static OwnedTerminalBuffer? PlanOwnedShapeRows(ExecutionBlock body, FinalOutputPlan output,
        ExecutionSortTable sort)
    {
        if (sort.RenumberFieldIndexes.Count != 0 || sort.Keys.Count == 0 ||
            sort.Keys.Any(key => key.OutputIndex < 0 || key.OutputIndex >= output.Result.ShapeFields.Count) ||
            !output.SourceBuffers.ContainsKey(sort.Source.Name) ||
            ExecutionIrAnalysis.CollectNodes<ExecutionAppendRow>(body).Any(append => append.Table.Name == sort.Source.Name &&
                (append.Values.Count != output.Result.ShapeFields.Count ||
                 !CanUseFinalShapeSourceBuffer(append.RowShape, output.Result.ShapeFields))) ||
            body.Nodes.OfType<ExecutionCreateTable>().Count(create => create.Table.Name == sort.Source.Name) != 1 ||
            !HasExclusiveBufferUses(body, sort.Source.Name, sort, IsShapeBufferWrite))
            return null;

        var sequence = "__musoqSequence";
        var names = output.Result.ShapeFields.Select(ExecutionSymbolicNamePolicy.GetGeneratedFieldName).ToHashSet(StringComparer.Ordinal);
        while (names.Contains(sequence))
            sequence += "_";
        return new OwnedTerminalBuffer(sort.Source.Name, output.Result.ShapeTypeName, sequence);
    }

    private static bool IsShapeBufferWrite(ExecutionNode node, string source)
        => node is ExecutionCreateTable create && create.Table.Name == source ||
           node is ExecutionAppendRow append && append.Table.Name == source ||
           node is ExecutionEnsureTableCapacity ensure && ensure.Table.Name == source;

    private static OwnedTerminalBuffer? PlanOwnedRecords(ExecutionBlock body, FinalOutputPlan output,
        ExecutionMaterializeRecordListToTable materialize)
    {
        if (materialize.RenumberFieldIndexes.Count != 0 ||
            materialize.FieldIndexes.Count != output.Result.ShapeFields.Count ||
            body.Nodes.OfType<ExecutionCreateRecordList>().Count(create => create.List.Name == materialize.Source.Name) != 1 ||
            body.Nodes.OfType<ExecutionOrderRecordList>().Count(order =>
                order.Source.Name == materialize.Source.Name && order.Selection is ExecutionFullOrderRecordSelection) != 1)
            return null;

        for (var index = 0; index < materialize.FieldIndexes.Count; index++)
        {
            var sourceIndex = materialize.FieldIndexes[index];
            if ((uint)sourceIndex >= (uint)materialize.RecordShape.Fields.Count)
                return null;
            var source = materialize.RecordShape.Fields[sourceIndex];
            var target = output.Result.ShapeFields[index];
            if (source.Type != target.Type ||
                ExecutionSymbolicNamePolicy.GetGeneratedFieldName(source) != ExecutionSymbolicNamePolicy.GetGeneratedFieldName(target))
                return null;
        }
        if (!HasExclusiveBufferUses(body, materialize.Source.Name, materialize, IsRecordBufferWrite))
            return null;
        return new OwnedTerminalBuffer(materialize.Source.Name, materialize.RecordShape.TypeName, null);
    }

    private static bool IsRecordBufferWrite(ExecutionNode node, string source)
        => node is ExecutionCreateRecordList create && create.List.Name == source ||
           node is ExecutionAppendRecord append && append.List.Name == source ||
           node is ExecutionOrderRecordList order && order.Source.Name == source &&
           order.Selection is ExecutionFullOrderRecordSelection;

    private static bool HasExclusiveBufferUses(ExecutionBlock body, string source, ExecutionNode terminal,
        Func<ExecutionNode, string, bool> isWrite)
    {
        foreach (var node in ExecutionIrAnalysis.FlattenNodes(body))
        {
            if (node is ExecutionAppendRow or ExecutionAppendRecord &&
                ExecutionIrAnalysis.ContainsVariableUse(ExecutionIrAnalysis.GetNodeExpressions(node), source))
                return false;
            if (ReferenceEquals(node, terminal) || isWrite(node, source))
                continue;
            if (ExecutionNodeFacts.GetDirectVariableReferences(node).Any(variable => variable.Name == source) ||
                ExecutionIrAnalysis.ContainsVariableUse(ExecutionIrAnalysis.GetNodeExpressions(node), source))
                return false;
        }
        return true;
    }
}
