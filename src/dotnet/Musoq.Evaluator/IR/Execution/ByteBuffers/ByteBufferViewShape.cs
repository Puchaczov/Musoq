using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Private owned storage and logical length; never a datasource or public result type.</summary>
internal static class ByteBufferViewShape
{
    internal const string TypeName = ByteBufferStorageTypes.OwnedView;

    internal static bool IsView(ExecutionExpression expression) =>
        expression is ExecutionFieldRead { GeneratedTypeName: TypeName };

    internal static ExecutionExpression Storage(ExecutionExpression expression) => IsView(expression)
        ? new ExecutionByteBufferOperation(expression, ExecutionByteBufferViewAccess.Storage) : expression;

    internal static ExecutionPlanBuildResult Add(ExecutionPlanBuildResult result)
    {
        if (result.ExecutionPlan is not { } plan || !ExecutionIrAnalysis.FlattenExpressions(plan.Body)
                .OfType<ExecutionByteBufferOperation>().Any(operation => operation.Program?.Result == ExecutionByteBufferResultKind.View))
            return result;
        return result with { ExecutionPlan = plan with { Shapes = [..plan.Shapes, Create()] } };
    }

    private static GeneratedRecordShape Create() => new(TypeName,
    [
        new FieldBinding("Storage", "Storage", 0, typeof(byte[]), FieldNullability.Unknown, new GeneratedFieldAccess("Storage")),
        new FieldBinding("Offset", "Offset", 1, typeof(int), FieldNullability.NotNullable, new GeneratedFieldAccess("Offset")),
        new FieldBinding("AvailableBytes", "AvailableBytes", 2, typeof(int), FieldNullability.NotNullable, new GeneratedFieldAccess("AvailableBytes")),
        new FieldBinding("LogicalLength", "LogicalLength", 3, typeof(int), FieldNullability.NotNullable, new GeneratedFieldAccess("LogicalLength"))
    ], emitAsValueType: true);
}
