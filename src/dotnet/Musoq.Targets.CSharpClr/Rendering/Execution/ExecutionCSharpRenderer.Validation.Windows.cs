using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private static bool CanRenderWindowAggregateKernel(ExecutionWindowAggregateKernel kernel)
    {
        return ExecutionNodeFacts.TryGetWindowComputation(kernel, out var window) &&
               CanRenderWindowComputationCommon(window, requireOrderKeys: false) &&
               kernel.Results.Type.RequireClrType() == kernel.Descriptor.ResultType.RequireClrType().MakeArrayType() &&
               CanRenderWindowAggregateMode(kernel) &&
               CanReferenceType(kernel.Descriptor.InputType) &&
               CanReferenceType(kernel.Descriptor.ResultType) &&
               kernel.Descriptor.LibraryKernel is not null &&
               CanRenderWindowAggregateFunction(kernel.Descriptor.Function) &&
               CanRenderExpression(kernel.Value) &&
               (kernel.FilterPredicate == null || CanRenderExpression(kernel.FilterPredicate));
    }

    private static bool CanRenderWindowAggregateMode(ExecutionWindowAggregateKernel kernel)
    {
        if (kernel.Descriptor.Mode != ExecutionWindowAggregateMode.BoundedRows)
            return true;

        return kernel.Frame != null;
    }

    private static bool CanRenderWindowAggregateFunction(ExecutionWindowAggregateFunction function)
    {
        return function == ExecutionWindowAggregateFunction.Library;
    }

}
