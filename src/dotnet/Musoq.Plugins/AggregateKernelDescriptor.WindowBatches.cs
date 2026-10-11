using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public sealed partial record AggregateKernelDescriptor
{
    /// <summary>Validates an explicitly registered typed batch operation.</summary>
    public static MethodInfo? CreateBatchForWindow(MethodInfo declaration)
    {
        var attribute = declaration.GetCustomAttribute<WindowFunctionAttribute>();
        if (attribute?.BatchKernelType is null)
            return null;
        var parameters = GetValueParameters(declaration);
        if (parameters.Length != 1 || (attribute.Modes & WindowEvaluationModes.Batch) == 0)
            throw CreateInvalidKernelException(declaration, "Typed batch registration requires one concrete value argument and Batch mode.");
        var kernel = ResolveKernelType(attribute.BatchKernelType, declaration);
        ValidateKernelSharedFields(kernel, declaration);
        var context = typeof(WindowBatchContext<,>).MakeGenericType(parameters[0].ParameterType, declaration.ReturnType);
        var operations = EnumerateStaticMethods(kernel, "TryEvaluate").Where(operation =>
            IsMatchingGetMethod(operation, context, typeof(bool))).ToArray();
        if (operations.Length != 1)
            throw CreateInvalidKernelException(declaration, "Batch kernel must expose one exact bool TryEvaluate(in WindowBatchContext<TInput, TResult>) operation.");
        return operations[0];
    }
}
