using System.Collections.Generic;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static LoweringAttempt<ExecutionAppendRow> CreateWindowAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ProjectedField[] fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex,
        bool preserveContexts)
    {
        var values = new List<ExecutionRowValue>(fields.Length);

        foreach (var field in fields)
        {
            var expression = ConvertWindowProjectionExpression(field, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            if (!expression.IsBuilt)
                return LoweringAttempt<ExecutionAppendRow>.Unsupported(expression.UnsupportedReason);

            values.Add(new ExecutionRowValue(field.OutputName, expression.Value));
        }

        return LoweringAttempt<ExecutionAppendRow>.Built(new ExecutionAppendRow(
            resultTable,
            resultShape,
            values,
            preserveContexts ? CreateContextValues(sourceLookup) : [],
            SerialAppendMode,
            preserveContexts ? CreateContextLayout(sourceLookup) : null));
    }

    private LoweringAttempt<ExecutionBlock> CreateWindowAppendBlock(
        IrExpression? qualifyPredicate,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        if (qualifyPredicate == null)
            return LoweringAttempt<ExecutionBlock>.Built(CreateAppendBlock(appendRow));

        var condition = ConvertWindowExpression(qualifyPredicate, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        if (!condition.IsBuilt)
            return LoweringAttempt<ExecutionBlock>.Unsupported(condition.UnsupportedReason);

        if (!IrExpressionNullSemantics.IsBoolean(condition.Value.ReturnType.ResolveClrType()))
        {
            return LoweringAttempt<ExecutionBlock>.Unsupported(
                $"Execution IR window QUALIFY lowering requires a boolean predicate. Found {condition.Value.ReturnType.ResolveClrType().Name}.");
        }

        return LoweringAttempt<ExecutionBlock>.Built(CreateFilteredAppendBlock(
            condition.Value,
            appendRow));
    }

}
