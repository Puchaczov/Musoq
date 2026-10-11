using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Musoq.Plugins;

public sealed partial record AggregateKernelDescriptor
{
    private static Type ResolveKernelType(Type kernelType, MethodInfo declaration)
    {
        if (!kernelType.ContainsGenericParameters)
            return kernelType;

        if (!kernelType.IsGenericTypeDefinition || declaration.ContainsGenericParameters)
            throw CreateInvalidKernelException(declaration, "Kernel type parameters must be concretized before execution binding.");

        var arguments = declaration.GetGenericArguments();
        if (arguments.Length != kernelType.GetGenericArguments().Length)
            throw CreateInvalidKernelException(declaration, "Kernel type parameters cannot be inferred from the closed declaration.");

        try { return kernelType.MakeGenericType(arguments); }
        catch (ArgumentException exception)
        {
            throw CreateInvalidKernelException(declaration, $"Kernel specialization violates its type constraints: {exception.Message}");
        }
    }

    private static void ValidateKernelState(Type kernelType, Type stateType, MethodInfo declaration)
    {
        if (stateType == typeof(object) || stateType.IsInterface || stateType.IsAbstract ||
            stateType.IsByRefLike || stateType.ContainsGenericParameters)
            throw CreateInvalidKernelException(declaration, "State must be a concrete state type owned by each group or partition.");

        ValidateKernelSharedFields(kernelType, declaration);
    }

    private static void ValidateKernelSharedFields(Type kernelType, MethodInfo declaration)
    {
        var sharedFields = kernelType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (sharedFields.Any(static field => !field.IsLiteral &&
            (!field.IsInitOnly || !IsImmutableSharedValue(field.FieldType, []))))
            throw CreateInvalidKernelException(declaration, "Kernel state cannot reside in shared static fields.");
    }

    private static bool IsImmutableSharedValue(Type type, HashSet<Type> visited)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            return true;
        if (!type.IsValueType)
            return false;
        if (!visited.Add(type))
            return true;
        return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .All(field => IsImmutableSharedValue(field.FieldType, visited));
    }

    private static MethodInfo ResolveResetMethod(Type kernelType, Type stateType, MethodInfo declaration)
    {
        var methods = EnumerateStaticMethods(kernelType, "Reset").ToArray();
        if (methods.Length == 1 && IsMatchingResetMethod(methods[0], stateType))
            return methods[0];

        throw CreateInvalidKernelException(declaration, $"Aggregate kernel {kernelType.FullName} must expose Reset(ref State).");
    }

    private static bool IsMatchingResetMethod(MethodInfo method, Type stateType)
    {
        var parameters = method.GetParameters();
        return method.ReturnType == typeof(void) && parameters.Length == 1 &&
               IsMutableStateParameter(parameters[0], stateType);
    }

    private static bool IsMutableStateParameter(ParameterInfo parameter, Type stateType)
        => IsByRefStateParameter(parameter, stateType) && !parameter.IsIn && !parameter.IsOut;

    private static bool IsReadOnlyStateParameter(ParameterInfo parameter, Type stateType)
        => IsByRefStateParameter(parameter, stateType) && parameter.IsIn && !parameter.IsOut;
}
