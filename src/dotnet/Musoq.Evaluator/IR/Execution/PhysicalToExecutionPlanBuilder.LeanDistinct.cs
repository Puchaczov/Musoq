using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static bool CanUseLeanDistinct(SingleKeyAggregatePipeline pipeline)
    {
        if (pipeline is not { Bindings.Length: 0, HavingPredicate: null, Project.IsDistinct: true })
            return false;

        return pipeline.Project.Fields.Length == 1 &&
               Equals(pipeline.Project.Fields[0].Expression, pipeline.GroupKey) &&
               CanUseLeanDistinctKey(pipeline.GroupKey);
    }

    private static bool CanUseLeanDistinct(ValueTupleAggregatePipeline pipeline)
    {
        if (pipeline is not { Bindings.Length: 0, HavingPredicate: null, Project.IsDistinct: true } ||
            pipeline.Project.Fields.Length != pipeline.GroupKeys.Length)
        {
            return false;
        }

        for (var index = 0; index < pipeline.GroupKeys.Length; index++)
        {
            if (!Equals(pipeline.Project.Fields[index].Expression, pipeline.GroupKeys[index]) ||
                !CanUseLeanDistinctKey(pipeline.GroupKeys[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanUseLeanDistinctKey(IrExpression expression)
    {
        return expression is ColumnRef or Literal;
    }

    private TableBuildResult BuildLeanDistinctTable(
        ProjectedField[] fields,
        IReadOnlyList<PostOperation> postOperations,
        PhysicalFilterNode? filter,
        SingleKeyAggregateExecutionSource source,
        ExecutionExpression distinctKey,
        string resultTableName,
        string resultShapeName,
        bool scopeDistinctVariables)
    {
        var outputFields = NormalizeProjectedFieldIndexes(fields);
        var resultShape = CreateGeneratedShape(resultShapeName, outputFields);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var distinctSet = CreateLeanDistinctSetVariable(resultTableName, scopeDistinctVariables, distinctKey.ReturnType.ClrType);
        var appendRow = CreateAppendRow(resultTable, resultShape, outputFields, source.Lookup);
        var appendWhenNewKey = CreateLeanDistinctAppendBlock(distinctSet, distinctKey, appendRow);
        var accumulationBlock = CreateAggregateAccumulationBlock(
            filter,
            source.Lookup,
            appendWhenNewKey);
        var loop = source.CreateLoop(accumulationBlock);

        var nodes = new List<ExecutionNode>(source.Setup.Count + 4);
        nodes.AddRange(source.Setup);
        nodes.Add(CreateTable(resultTable, resultShape));
        nodes.Add(new ExecutionCreateKeySet(distinctSet, distinctKey.ReturnType.ClrType, CreateRowsCapacityCandidate(distinctSet, loop.Source)));
        nodes.Add(loop);

        return CompleteTableBuild(
            [..source.Shapes, resultShape],
            nodes,
            resultTable,
            resultShape,
            postOperations);
    }

    private static ExecutionVariable CreateLeanDistinctSetVariable(
        string resultTableName,
        bool scopeDistinctVariables,
        Type keyType)
    {
        var scopeName = CreateAggregateScopeName(resultTableName, scopeDistinctVariables);
        return CreateAggregateVariable(scopeName, "distinctKeys", typeof(HashSet<>).MakeGenericType(keyType));
    }

    private ExecutionBlock CreateLeanDistinctAppendBlock(
        ExecutionVariable distinctSet,
        ExecutionExpression distinctKey,
        ExecutionAppendRow appendRow)
    {
        var addCall = new ExecutionMethodCall(
            GetHashSetAddMethod(distinctKey.ReturnType.ClrType),
            [distinctKey],
            null,
            typeof(bool),
            null,
            distinctSet);

        return new ExecutionBlock([new ExecutionIf(addCall, CreateAppendBlock(appendRow))]);
    }

    private static MethodInfo GetHashSetAddMethod(Type keyType)
    {
        return typeof(HashSet<>)
            .MakeGenericType(keyType)
            .GetMethod(nameof(HashSet<>.Add), [keyType])
            ?? throw new InvalidOperationException($"Could not resolve HashSet<{keyType.Name}>.Add for lean distinct lowering.");
    }
}
