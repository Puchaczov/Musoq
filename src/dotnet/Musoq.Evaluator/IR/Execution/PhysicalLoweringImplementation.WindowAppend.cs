using System.Collections.Generic;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionNode[] CreateQualifiedPositions(
        ExecutionVariable source,
        ExecutionVariable index,
        ExecutionVariable buffer,
        ExecutionVariable mask,
        ExecutionVariable positions,
        ExecutionVariable survivors,
        GeneratedRowShape? shape,
        IReadOnlyList<WindowComputationBuildResult> position,
        ExecutionExpression predicate
    )
    {
        var mark = new ExecutionArrayAssign(
            mask,
            new ExecutionVariableRead(index),
            new ExecutionLiteral(true, typeof(bool)),
            typeof(bool)
        );
        // The generic operation is bound by its typed delegate. Its row type is
        // supplied by the same source binding that types the window buffer.
        var selectRows = (
            (Func<IEnumerable<object>, bool[], IEnumerable<object>>)Helpers.WindowFunctionHelpers.QualifiedRows
        )
            .Method.GetGenericMethodDefinition()
            .MakeGenericMethod(source.Type.ResolveClrType());
        var selected = new ExecutionMethodCall(
            selectRows,
            [new ExecutionVariableRead(buffer), new ExecutionVariableRead(mask)],
            null,
            selectRows.ReturnType
        );
        if (shape is not null)
            selected = selected with
            {
                GeneratedGenericArguments = new Dictionary<int, string> { [0] = shape.TypeName },
                GeneratedResultTypeName = $"System.Collections.Generic.IEnumerable<{shape.TypeName}>",
            };
        return
        [
            new ExecutionCreateBooleanArray(mask, buffer),
            new ExecutionForEachIndexed(
                source,
                index,
                buffer,
                ExecutionRowAccessMode.Direct,
                new ExecutionBlock([new ExecutionIf(predicate, new ExecutionBlock([mark]))])
            ),
            CreateMaterializeListNode(selected, survivors, shape),
            .. CreateWindowKernelPlanNodes(position),
            new ExecutionLet(
                positions,
                new ExecutionMethodCall(
                    ((Func<long[], bool[], long[]>)Helpers.WindowFunctionHelpers.NumberQualifiedRows).Method,
                    [new ExecutionVariableRead(position[0].Results), new ExecutionVariableRead(mask)],
                    null,
                    typeof(long[])
                )
            ),
        ];
    }

    private static LoweringAttempt<ExecutionAppendRow> CreateWindowAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ProjectedField[] fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex,
        bool preserveContexts
    )
    {
        var values = new List<ExecutionRowValue>(fields.Length);

        foreach (var field in fields)
        {
            var expression = ConvertWindowProjectionExpression(
                field,
                sourceLookup,
                aggregateSourceFields,
                windowResults,
                windowIndex
            );
            if (!expression.IsBuilt)
                return LoweringAttempt<ExecutionAppendRow>.Unsupported(expression.UnsupportedReason);

            values.Add(new ExecutionRowValue(field.OutputName, expression.Value));
        }

        return LoweringAttempt<ExecutionAppendRow>.Built(
            new ExecutionAppendRow(
                resultTable,
                resultShape,
                values,
                preserveContexts ? CreateContextValues(sourceLookup) : [],
                SerialAppendMode,
                preserveContexts ? CreateContextLayout(sourceLookup) : null
            )
        );
    }

    private LoweringAttempt<ExecutionBlock> CreateWindowAppendBlock(
        IrExpression? qualifyPredicate,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex
    )
    {
        if (qualifyPredicate == null)
            return LoweringAttempt<ExecutionBlock>.Built(CreateAppendBlock(appendRow));

        var condition = ConvertWindowExpression(
            qualifyPredicate,
            sourceLookup,
            aggregateSourceFields,
            windowResults,
            windowIndex
        );
        if (!condition.IsBuilt)
            return LoweringAttempt<ExecutionBlock>.Unsupported(condition.UnsupportedReason);

        if (!IrExpressionNullSemantics.IsBoolean(condition.Value.ReturnType.ResolveClrType()))
        {
            return LoweringAttempt<ExecutionBlock>.Unsupported(
                $"Execution IR window QUALIFY lowering requires a boolean predicate. Found {condition.Value.ReturnType.ResolveClrType().Name}."
            );
        }

        return LoweringAttempt<ExecutionBlock>.Built(CreateFilteredAppendBlock(condition.Value, appendRow));
    }
}
