using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static BuildResult<ExecutionAppendRow> CreateWindowAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ProjectedField[] fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex,
        bool preserveContexts)
    {
        var values = new List<ExecutionRowValue>(fields.Length);

        foreach (var field in fields)
        {
            var expression = ConvertWindowProjectionExpression(field, sourceLookup, windowResults, windowIndex);
            if (!expression.Supported)
                return BuildResult<ExecutionAppendRow>.Unsupported(expression.UnsupportedReason);

            values.Add(new ExecutionRowValue(field.OutputName, expression.Value));
        }

        return BuildResult<ExecutionAppendRow>.Success(new ExecutionAppendRow(
            resultTable,
            resultShape,
            values,
            preserveContexts ? CreateContextValues(sourceLookup) : [],
            SerialAppendMode,
            preserveContexts ? CreateContextLayout(sourceLookup) : null));
    }

    private BuildResult<ExecutionBlock> CreateWindowAppendBlock(
        IrExpression? qualifyPredicate,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        if (qualifyPredicate == null)
            return BuildResult<ExecutionBlock>.Success(CreateAppendBlock(appendRow));

        var condition = ConvertWindowExpression(qualifyPredicate, sourceLookup, windowResults, windowIndex);
        if (!condition.Supported)
            return BuildResult<ExecutionBlock>.Unsupported(condition.UnsupportedReason);

        if (condition.Value.ReturnType != typeof(bool))
        {
            return BuildResult<ExecutionBlock>.Unsupported(
                $"Execution IR window QUALIFY lowering requires a boolean predicate. Found {condition.Value.ReturnType.Name}.");
        }

        return BuildResult<ExecutionBlock>.Success(CreateFilteredAppendBlock(
            condition.Value,
            appendRow));
    }

}
