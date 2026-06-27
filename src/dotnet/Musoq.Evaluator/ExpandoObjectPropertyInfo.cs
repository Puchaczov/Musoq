using System.Dynamic;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator;

internal sealed class ExpandoObjectPropertyInfo(string name, Type propertyType) : PropertyInfo
{
    public override PropertyAttributes Attributes => PropertyAttributes.None;
    public override bool CanRead => true;
    public override bool CanWrite => false;

    public override Type PropertyType { get; } = propertyType;

    public override Type DeclaringType => typeof(ExpandoObject);

    public override string Name { get; } = name;

    public override Type? ReflectedType { get; } = typeof(ExpandoObject);

    public override object[] GetCustomAttributes(bool inherit)
    {
        return [];
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return [];
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        return false;
    }

    public override MethodInfo[] GetAccessors(bool nonPublic)
    {
        return [];
    }

    public override MethodInfo GetGetMethod(bool nonPublic)
    {
        return null!;
    }

    public override ParameterInfo[] GetIndexParameters()
    {
        return [];
    }

    public override MethodInfo GetSetMethod(bool nonPublic)
    {
        return null!;
    }

    public override object GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index,
        CultureInfo? culture)
    {
        if (index is { Length: > 0 })
            throw new TargetParameterCountException("Expando object metadata properties do not support index parameters.");

        if (obj is IDictionary<string, object?> values)
            return values.TryGetValue(Name, out var value) ? value! : null!;

        throw new TargetException(
            $"Expando object metadata property '{Name}' can only read values from dictionary-backed expando rows.");
    }

    public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index,
        CultureInfo? culture)
    {
        throw new NotSupportedException($"Expando object metadata property '{Name}' is read-only.");
    }
}
