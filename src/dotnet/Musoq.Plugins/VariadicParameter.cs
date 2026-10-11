using System.Reflection;
using System.Runtime.CompilerServices;

namespace Musoq.Plugins;

/// <summary>Compilation-time metadata for concretely typed variadic declarations.</summary>
public static class VariadicParameter
{
    /// <summary>Identifies array and native collection parameter declarations.</summary>
    public static bool IsVariadic(ParameterInfo parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.IsDefined(typeof(ParamArrayAttribute), false) ||
               parameter.IsDefined(typeof(ParamCollectionAttribute), false);
    }

    /// <summary>Identifies a concrete or generic read-only span.</summary>
    public static bool IsNativeSpan(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>);
    }

    /// <summary>Returns the declared variadic element type.</summary>
    public static Type? ElementType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.IsArray)
            return type.GetElementType();
        return IsNativeSpan(type) ? type.GetGenericArguments()[0] : null;
    }
}
