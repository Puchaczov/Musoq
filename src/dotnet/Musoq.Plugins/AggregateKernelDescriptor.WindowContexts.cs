using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public sealed partial record AggregateKernelDescriptor
{
    /// <summary>Validates a direct operation receiving exact typed frame storage and SQL arguments.</summary>
    public static MethodInfo CreateContextForWindow(MethodInfo declaration, Type? orderKeyType = null)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        var window = declaration.GetCustomAttribute<WindowFunctionAttribute>();
        if (window?.KernelType is null || window.ContextType != typeof(WindowFrameContext<>) && window.ContextType != typeof(WindowFrameContext<,>))
            throw CreateInvalidKernelException(declaration, "Window context must register WindowFrameContext<> or WindowFrameContext<,> and a concrete kernel.");

        var parameters = GetValueParameters(declaration);
        if (parameters.Length == 0 || parameters[0].ParameterType == typeof(object) ||
            parameters[0].ParameterType.ContainsGenericParameters)
            throw CreateInvalidKernelException(declaration, "Window frame storage requires a concrete input value type.");

        var kernel = ResolveKernelType(window.KernelType, declaration);
        ValidateKernelSharedFields(kernel, declaration);
        var context = ResolveWindowContextType(window.ContextType, parameters[0].ParameterType, orderKeyType, declaration);
        var operations = EnumerateStaticMethods(kernel, "Get")
            .Where(method => IsMatchingWindowContextOperation(method, declaration.ReturnType, context, parameters)).ToArray();
        if (operations.Length != 1)
            throw CreateInvalidKernelException(declaration, $"Window kernel must expose one exact Get(in WindowFrameContext<T>, args...) operation returning its declared result. Expected context: {context}.");
        return operations[0];
    }

    private static Type ResolveWindowContextType(Type contextDefinition, Type valueType, Type? orderKeyType,
        MethodInfo declaration)
    {
        if (contextDefinition == typeof(WindowFrameContext<>))
            return contextDefinition.MakeGenericType(valueType);
        if (orderKeyType is null || orderKeyType == typeof(object) || orderKeyType.ContainsGenericParameters)
            throw CreateInvalidKernelException(declaration, "Typed order-key contexts require concrete ORDER BY keys.");
        return contextDefinition.MakeGenericType(valueType, orderKeyType);
    }

    private static bool IsMatchingWindowContextOperation(MethodInfo operation, Type result, Type context,
        System.Collections.Generic.IReadOnlyList<ParameterInfo> declarationParameters)
    {
        var parameters = operation.GetParameters();
        if (operation.ReturnType != result || parameters.Length != declarationParameters.Count ||
            !IsReadOnlyStateParameter(parameters[0], context))
            return false;
        for (var index = 1; index < parameters.Length; index++)
        {
            if (parameters[index].ParameterType != declarationParameters[index].ParameterType)
                return false;
        }
        return true;
    }
}
