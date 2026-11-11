using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Musoq.Evaluator;

namespace Musoq.Tests.Common;

/// <summary>Supplies heterogeneous test inputs through the actual typed parameter APIs.</summary>
public static class NativeParameterFixture
{
    private sealed record ScalarShape(Action<TypedParameterValues, string, object?> Set,
        Action<TypedParameterValues, string> SetNull);
    private interface ICollectionShape
    {
        Type ArrayType { get; }
        bool Accepts(object value);
        void Set(TypedParameterValues target, string name, object? value);
    }
    private sealed class CollectionShape<T> : ICollectionShape
    {
        public Type ArrayType => typeof(T[]);
        public bool Accepts(object value) => value is IReadOnlyList<T>;
        public void Set(TypedParameterValues target, string name, object? value) => target.SetCollection(name, (IReadOnlyList<T>?)value);
    }

    private static readonly IReadOnlyDictionary<Type, ScalarShape> Scalars;
    private static readonly IReadOnlyList<ICollectionShape> Collections;

    static NativeParameterFixture()
    {
        var scalars = new Dictionary<Type, ScalarShape>();
        var collections = new List<ICollectionShape>();
        AddStruct<byte>(scalars, collections);
        AddStruct<sbyte>(scalars, collections);
        AddStruct<short>(scalars, collections);
        AddStruct<ushort>(scalars, collections);
        AddStruct<int>(scalars, collections);
        AddStruct<uint>(scalars, collections);
        AddStruct<long>(scalars, collections);
        AddStruct<ulong>(scalars, collections);
        AddStruct<float>(scalars, collections);
        AddStruct<double>(scalars, collections);
        AddStruct<decimal>(scalars, collections);
        AddStruct<bool>(scalars, collections);
        AddStruct<char>(scalars, collections);
        AddStruct<DateTime>(scalars, collections);
        AddStruct<DateTimeOffset>(scalars, collections);
        AddStruct<TimeSpan>(scalars, collections);
        AddStruct<Guid>(scalars, collections);
        AddStruct<DateOnly>(scalars, collections);
        AddStruct<TimeOnly>(scalars, collections);
        AddStruct<Int128>(scalars, collections);
        AddStruct<UInt128>(scalars, collections);
        AddStruct<Half>(scalars, collections);
        AddStruct<System.Numerics.BigInteger>(scalars, collections);
        AddReference<string>(scalars, collections);
        AddReference<object>(scalars, collections);
        Scalars = scalars;
        Collections = collections;
    }

    private static void AddStruct<T>(Dictionary<Type, ScalarShape> scalars, List<ICollectionShape> collections) where T : struct
    {
        scalars.Add(typeof(T), new((target, name, value) => target.Set(name, (T)value!),
            (target, name) => target.Set<T?>(name, null)));
        scalars.Add(typeof(T?), new((target, name, value) => target.Set(name, (T?)value),
            (target, name) => target.Set<T?>(name, null)));
        collections.Add(new CollectionShape<T>());
        collections.Add(new CollectionShape<T?>());
    }

    private static void AddReference<T>(Dictionary<Type, ScalarShape> scalars, List<ICollectionShape> collections) where T : class
    {
        scalars.Add(typeof(T), new((target, name, value) => target.Set(name, (T?)value),
            (target, name) => target.Set<T?>(name, null)));
        collections.Add(new CollectionShape<T>());
    }

    public static void Set(TypedParameterValues target, IEnumerable<ScriptParameterDefinition>? definitions, string name, object? value) =>
        Set<object?>(target, definitions, name, value);

    public static void Set<T>(TypedParameterValues target, IEnumerable<ScriptParameterDefinition>? definitions, string name, T value)
    {
        ArgumentNullException.ThrowIfNull(target);
        var definition = definitions?.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal));
        if (definition?.Contract.IsStructured == true || value is JsonElement)
        {
            var json = value is JsonElement element ? element : JsonSerializer.SerializeToElement(
                NativeParameterTestInputs.ToFixtureJsonValue(value), new JsonSerializerOptions { IncludeFields = true });
            target.SetJson(name, json);
            return;
        }
        if (value is null)
        {
            SetNull(target, definition?.ParameterType ?? typeof(string), name);
            return;
        }
        foreach (var shape in Collections)
        {
            if (!shape.Accepts(value)) continue;
            shape.Set(target, name, value);
            return;
        }
        var actual = value.GetType();
        var declared = definition?.ParameterType;
        var type = declared is not null && Nullable.GetUnderlyingType(declared) == actual ? declared : actual;
        if (Scalars.TryGetValue(type, out var scalar))
        {
            scalar.Set(target, name, value);
            return;
        }
        target.Set(name, value);
    }

    private static void SetNull(TypedParameterValues target, Type declared, string name)
    {
        var collection = Collections.FirstOrDefault(shape => shape.ArrayType == declared);
        if (collection is not null)
        {
            collection.Set(target, name, null);
            return;
        }
        if (!Scalars.TryGetValue(declared, out var scalar))
            throw new ArgumentException("Unregistered null parameter fixture type: " + declared, nameof(declared));
        scalar.SetNull(target, name);
    }

    public static TypedParameterSnapshot Capture(IEnumerable<KeyValuePair<string, object?>> values,
        IEnumerable<ScriptParameterDefinition>? definitions = null)
    {
        var target = new TypedParameterValues();
        foreach (var pair in values) Set(target, definitions, pair.Key, pair.Value);
        return target.Capture();
    }
}
