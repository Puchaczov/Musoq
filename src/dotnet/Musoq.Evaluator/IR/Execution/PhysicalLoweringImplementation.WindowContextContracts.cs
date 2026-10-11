using System.Reflection;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionCallableRef? CreateWindowBatchLibraryOperation(MethodInfo declaration)
    {
        try
        {
            var operation = AggregateKernelDescriptor.CreateBatchForWindow(declaration);
            return operation is null ? null : ExecutionClrBindingFactory.FromClr(operation);
        }
        catch (InvalidOperationException exception)
        {
            throw new SourcePlanContractException(exception.Message, TextSpan.Empty);
        }
    }

    private static ExecutionCallableRef? CreateWindowContextLibraryOperation(WindowRegistration registration)
    {
        var declaration = registration.Function;
        if (declaration?.GetCustomAttribute<WindowFunctionAttribute>() is not { ContextType: not null } attribute)
            return null;
        if (attribute.ContextType == typeof(WindowFrameContext<,>) &&
            registration.OrderKeys.Any(static key => key.Expression.ReturnType == typeof(object) ||
                key.Expression.ReturnType.ContainsGenericParameters || key.Expression.ReturnType.IsInterface))
            throw new SourcePlanContractException("Typed order-key contexts require concrete, non-erased ORDER BY keys.", TextSpan.Empty);
        var parameters = declaration.GetParameters();
        for (var index = 0; index < registration.ValueArguments.Length; index++)
        {
            var parameter = parameters[index];
            if (parameter.ParameterType == typeof(object) ||
                parameter.ParameterType.IsInterface && registration.ValueArguments[index].ReturnType.IsValueType)
                throw new SourcePlanContractException(
                    $"Window '{registration.FunctionName}' parameter '{parameter.Name}' requires an erased or boxed argument. Declare a concrete typed overload.",
                    TextSpan.Empty);
        }
        var mode = ResolveWindowAggregateMode(CreatePluginWindowFrame(registration)).Mode;
        var required = mode switch
        {
            ExecutionWindowAggregateMode.WholePartition => WindowEvaluationModes.WholePartition,
            ExecutionWindowAggregateMode.Running => WindowEvaluationModes.Running,
            _ => WindowEvaluationModes.RecomputeFrames
        };
        if ((attribute.Modes & required) == 0)
            throw new SourcePlanContractException(
                $"Window '{registration.FunctionName}' does not support evaluation mode {required}.", TextSpan.Empty);
        try
        {
            return ExecutionClrBindingFactory.FromClr(AggregateKernelDescriptor.CreateContextForWindow(declaration, WindowContextOperationBinding.ResolveRawOrderKeyType(registration.OrderKeys.Select(static key => key.Expression.ReturnType).ToArray())));
        }
        catch (InvalidOperationException exception)
        {
            throw new SourcePlanContractException(exception.Message, TextSpan.Empty);
        }
    }
}
