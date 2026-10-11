using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Tests.Common;

/// <summary>Converts heterogeneous fixture inputs into native slots before query execution.</summary>
public static class NativeParameterTestInputs
{
    /// <summary>Supplies a fixture value using the declaration's concrete nullability and shape.</summary>
    public static void Set(object target, IEnumerable<object>? definitions, string name, object? value)
    {
        ArgumentNullException.ThrowIfNull(target);
        var definition = definitions?.FirstOrDefault(item =>
            string.Equals((string?)item.GetType().GetProperty("Name")?.GetValue(item), name, StringComparison.Ordinal));
        var declared = (Type?)definition?.GetType().GetProperty("ParameterType")?.GetValue(definition);
        var contract = definition?.GetType().GetProperty("Contract")?.GetValue(definition);
        var structured = contract?.GetType().GetProperty("IsStructured")?.GetValue(contract) is true;
        if (structured || value is JsonElement)
        {
            var json = value is JsonElement element ? element : JsonSerializer.SerializeToElement(ToFixtureJsonValue(value),
                new JsonSerializerOptions { IncludeFields = true });
            Invoke(target.GetType().GetMethod("SetJson")!, target, [name, json]);
            return;
        }
        var actual = value?.GetType();
        var elementType = actual?.IsArray == true ? actual.GetElementType() : actual?.GetInterfaces()
            .Append(actual).FirstOrDefault(type => type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))?.GetGenericArguments()[0];
        if (value is null && declared?.IsArray == true)
            elementType = declared.GetElementType();
        if (elementType is not null)
        {
            Invoke(target.GetType().GetMethod("SetCollection")!.MakeGenericMethod(elementType), target, [name, value]);
            return;
        }
        var type = actual ?? declared ?? typeof(string);
        var underlying = declared is null ? null : Nullable.GetUnderlyingType(declared);
        if (actual == underlying)
            type = declared!;
        if (value is null && type.IsValueType && Nullable.GetUnderlyingType(type) is null)
            type = typeof(Nullable<>).MakeGenericType(type);
        Invoke(target.GetType().GetMethod("Set")!.MakeGenericMethod(type), target, [name, value]);
    }

    /// <summary>Captures fixture dictionaries into the requested native snapshot type.</summary>
    public static TSnapshot Capture<TSnapshot>(IEnumerable<KeyValuePair<string, object?>> values,
        IEnumerable<object>? definitions = null)
    {
        var type = typeof(TSnapshot).Assembly.GetType("Musoq.Evaluator.TypedParameterValues", throwOnError: true)!;
        var target = Activator.CreateInstance(type)!;
        foreach (var pair in values)
            Set(target, definitions, pair.Key, pair.Value);
        return (TSnapshot)Invoke(type.GetMethod("Capture")!, target, [default(System.Threading.CancellationToken)])!;
    }

    private static object? ToFixtureJsonValue(object? value) => value switch
    {
        StructuralValue { Kind: StructuralTypeKind.Record } record => record.Fields.ToDictionary(
            static field => field.Key, static field => ToFixtureJsonValue(field.Value)),
        StructuralValue { Kind: StructuralTypeKind.Collection } collection =>
            collection.Elements.Select(ToFixtureJsonValue).ToArray(),
        StructuralValue scalar => scalar.Scalar,
        IEnumerable<StructuralValue> collection => collection.Select(ToFixtureJsonValue).ToArray(),
        _ => value
    };

    private static object? Invoke(MethodInfo method, object target, object?[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
