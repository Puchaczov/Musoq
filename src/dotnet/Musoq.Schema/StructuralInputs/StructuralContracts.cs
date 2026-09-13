using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Musoq.Schema.Attributes;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.StructuralInputs;

/// <summary>Classifies a structural input node.</summary>
public enum StructuralTypeKind
{
    Scalar,
    Record,
    Collection
}
/// <summary>Defines the exact collection shapes accepted by structural receivers.</summary>
public static class StructuralCollectionContract
{
    public static bool TryGetElementType(Type type, out Type elementType)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.IsArray && type.GetArrayRank() == 1)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() is var definition &&
            (definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>)))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    public static bool IsSupportedReceiver(Type type) => TryGetElementType(type, out _);
}
/// <summary>Describes whether a field default is absent or explicitly supplied.</summary>
public sealed class StructuralDefaultDescriptor
{
    private StructuralDefaultDescriptor(bool hasValue, object? value, string? canonicalText)
    {
        HasValue = hasValue;
        Value = value;
        CanonicalText = canonicalText;
    }

    /// <summary>Gets a descriptor for an absent default.</summary>
    public static StructuralDefaultDescriptor Absent { get; } = new(false, null, null);

    /// <summary>Creates a descriptor for a supplied default, including an explicit null.</summary>
    public static StructuralDefaultDescriptor Create(object? value, string? canonicalText = null)
    {
        return new StructuralDefaultDescriptor(true, value, canonicalText ?? Format(value));
    }

    /// <summary>Gets whether the declaration supplies a default.</summary>
    public bool HasValue { get; }

    /// <summary>Gets the metadata value. It is null for an explicit null default.</summary>
    public object? Value { get; }

    /// <summary>Gets canonical SQL text for the default. Explicit null is the text `null`; absent defaults expose a null metadata cell.</summary>
    public string? CanonicalText { get; }

    private static string Format(object? value) => StructuralSqlLiteralFormatter.Format(value);
}

/// <summary>Describes one named field in a structural record.</summary>
public sealed class StructuralFieldDescriptor
{
    public StructuralFieldDescriptor(
        string name,
        StructuralTypeDescriptor type,
        bool required,
        StructuralDefaultDescriptor? @default = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(type);
        Name = name;
        Type = type;
        Required = required;
        Default = @default ?? StructuralDefaultDescriptor.Absent;
    }

    public string Name { get; }
    public StructuralTypeDescriptor Type { get; }
    public bool Required { get; }
    public StructuralDefaultDescriptor Default { get; }
    public bool HasDefault => Default.HasValue;
}

/// <summary>Immutable description of a scalar, record, or collection input node.</summary>
public sealed class StructuralTypeDescriptor
{
    private StructuralTypeDescriptor(
        StructuralTypeKind kind,
        Type? scalarType,
        IReadOnlyList<StructuralFieldDescriptor>? fields,
        StructuralTypeDescriptor? elementType,
        bool nullable,
        Type? clrType)
    {
        Kind = kind;
        ScalarType = scalarType;
        Fields = new ReadOnlyCollection<StructuralFieldDescriptor>((fields ?? []).ToArray());
        ElementType = elementType;
        IsNullable = nullable;
        ClrType = clrType;
    }

    public StructuralTypeKind Kind { get; }
    public Type? ScalarType { get; }
    public IReadOnlyList<StructuralFieldDescriptor> Fields { get; }
    public StructuralTypeDescriptor? ElementType { get; }
    public bool IsNullable { get; }
    public Type? ClrType { get; }

    /// <summary>Gets the canonical CoreValueType used for public StructuralValue metadata. Generated execution carriers are separate.</summary>
    public Type CoreValueType => Kind switch
    {
        StructuralTypeKind.Scalar => GetScalarStorageType(),
        StructuralTypeKind.Record => typeof(StructuralValue),
        StructuralTypeKind.Collection => StorageTypeForCollection(),
        _ => throw new InvalidOperationException("Unknown structural type kind.")
    };


    private Type GetScalarStorageType()
    {
        if (ScalarType == null)
            return typeof(object);

        if (IsNullable && ScalarType.IsValueType && Nullable.GetUnderlyingType(ScalarType) == null)
            return typeof(Nullable<>).MakeGenericType(ScalarType);

        return ScalarType;
    }

    private Type StorageTypeForCollection()
    {
        return (ElementType ?? throw new InvalidOperationException("A collection must have an element type.")).CoreValueType.MakeArrayType();
    }

    public static StructuralTypeDescriptor Scalar(Type type, bool nullable = false)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new StructuralTypeDescriptor(StructuralTypeKind.Scalar, type, null, null, nullable, type);
    }

    public static StructuralTypeDescriptor Record(
        Type type,
        IEnumerable<StructuralFieldDescriptor> fields,
        bool nullable = false)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(fields);
        return new StructuralTypeDescriptor(
            StructuralTypeKind.Record,
            null,
            fields.ToArray(),
            null,
            nullable,
            type);
    }

    public static StructuralTypeDescriptor Collection(
        Type type,
        StructuralTypeDescriptor elementType,
        bool nullable = false)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(elementType);
        return new StructuralTypeDescriptor(
            StructuralTypeKind.Collection,
            null,
            null,
            elementType,
            nullable,
            type);
    }

    /// <summary>Builds a closed structural descriptor from a permitted CLR type graph.</summary>
    private static readonly ConditionalWeakTable<Type, DescriptorHolder> DescriptorCache = new();

    public static StructuralTypeDescriptor FromClrType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            return DescriptorCache.GetValue(type, static key => new DescriptorHolder(FromClrType(key, false, new HashSet<Type>()))).Value;
        return FromClrType(type, false, new HashSet<Type>());
    }

    /// <summary>Builds a descriptor and explicitly controls nullability of its root node.</summary>
    public static StructuralTypeDescriptor FromClrType(Type type, bool nullable)
    {
        ArgumentNullException.ThrowIfNull(type);
        return FromClrType(type, nullable, new HashSet<Type>());
    }

    public string ToCanonicalSql()
    {
        var text = Kind switch
        {
            StructuralTypeKind.Scalar => ScalarType == null ? "object" : ScalarName(ScalarType),
            StructuralTypeKind.Record => $"({string.Join(", ", Fields.Select(static field =>
                $"{field.Name}: {field.Type.ToCanonicalSql()}{DefaultSuffix(field)}"))})",
            StructuralTypeKind.Collection => $"{ElementType!.ToCanonicalSql()}[]",
            _ => throw new InvalidOperationException("Unknown structural type kind.")
        };
        return IsNullable && (Kind != StructuralTypeKind.Scalar || ScalarType?.IsValueType == true) ? $"{text}?" : text;
    }

    public override string ToString() => ToCanonicalSql();

    private sealed class DescriptorHolder(StructuralTypeDescriptor value)
    {
        public StructuralTypeDescriptor Value { get; } = value;
    }
    private static StructuralTypeDescriptor FromClrType(Type type, bool nullableRoot, ISet<Type> ancestors)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
        {
            var inner = FromClrType(nullableType, nullableRoot, ancestors);
            return MakeNullable(inner);
        }

        if (IsScalar(type))
            return Scalar(type, nullableRoot || !type.IsValueType);

        var nullable = nullableRoot;

        if (StructuralCollectionContract.TryGetElementType(type, out var elementType))
        {
            if (!ancestors.Add(type))
                throw new InvalidOperationException($"Cyclic structural type '{type.FullName}' is not supported.");
            var element = FromClrType(elementType, false, ancestors);
            ancestors.Remove(type);
            return Collection(type, element, nullable);
        }

        if (!ancestors.Add(type))
            throw new InvalidOperationException($"Cyclic structural type '{type.FullName}' is not supported.");
        var constructor = StructuralInputMetadata.SelectConstructor(type);
        var fields = constructor.GetParameters()
            .Select(parameter =>
            {
                var fieldType = FromClrType(parameter.ParameterType, false, ancestors);
                var @default = parameter.HasDefaultValue
                    ? StructuralDefaultDescriptor.Create(parameter.DefaultValue, FormatDefault(parameter.DefaultValue))
                    : StructuralDefaultDescriptor.Absent;
                return new StructuralFieldDescriptor(
                    parameter.Name ?? throw new InvalidOperationException("Structural constructor parameter has no name."),
                    fieldType,
                    !parameter.HasDefaultValue,
                    @default);
            })
            .ToArray();
        ancestors.Remove(type);
        return Record(type, fields, nullableRoot);
    }

    private static StructuralTypeDescriptor MakeNullable(StructuralTypeDescriptor descriptor)
    {
        return descriptor.Kind switch
        {
            StructuralTypeKind.Scalar => Scalar(descriptor.ScalarType!, true),
            StructuralTypeKind.Record => Record(descriptor.ClrType!, descriptor.Fields, true),
            StructuralTypeKind.Collection => Collection(descriptor.ClrType!, descriptor.ElementType!, true),
            _ => throw new InvalidOperationException("Unknown structural type kind.")
        };
    }
    private static bool IsScalar(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
            type == typeof(Guid) || type == typeof(object);
    }

    private static string DefaultSuffix(StructuralFieldDescriptor field)
    {
        return field.HasDefault ? $" = {field.Default.CanonicalText ?? "null"}" : string.Empty;
    }

    private static string FormatDefault(object? value) => StructuralSqlLiteralFormatter.Format(value);

    private static string ScalarName(Type type)
    {
        if (type == typeof(bool)) return "bool";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(short)) return "short";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(int)) return "int";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(long)) return "long";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(char)) return "char";
        if (type == typeof(string)) return "string";
        if (type == typeof(DateTime)) return "datetime";
        if (type == typeof(DateTimeOffset)) return "datetimeoffset";
        if (type == typeof(TimeSpan)) return "timespan";
        if (type == typeof(Guid)) return "guid";
        return type.Name;
    }
}

/// <summary>Deterministic reflection metadata for structural input contracts.</summary>
public static class StructuralInputMetadata
{
    /// <summary>Selects the permitted public constructor for a structural type.</summary>
    private static readonly ConditionalWeakTable<Type, ConstructorHolder> ConstructorCache = new();

    public static ConstructorInfo SelectConstructor(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return ConstructorCache.GetValue(type, static key => new ConstructorHolder(SelectConstructorCore(key))).Value;
    }

    private static ConstructorInfo SelectConstructorCore(Type type)
    {
        if (type.IsAbstract || type.ContainsGenericParameters)
            throw new InvalidOperationException($"Type '{type.FullName}' is not a closed constructible structural type.");

        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        var marked = constructors
            .Where(static constructor => constructor.GetCustomAttribute<StructuralInputConstructorAttribute>() != null)
            .ToArray();
        if (marked.Length == 1)
            return EnsureRecordConstructor(marked[0]);
        if (marked.Length > 1)
            throw new InvalidOperationException($"Type '{type.FullName}' has multiple structural input constructors.");
        if (constructors.Length == 1)
            return EnsureRecordConstructor(constructors[0]);
        throw new InvalidOperationException($"Type '{type.FullName}' must have one public structural input constructor.");
    }

    private static ConstructorInfo EnsureRecordConstructor(ConstructorInfo constructor)
    {
        if (constructor.GetParameters().Any(static parameter => parameter.ParameterType == typeof(SourceExecutionContext)))
            throw new InvalidOperationException(
                $"Structural record constructor '{constructor}' cannot receive {nameof(SourceExecutionContext)}.");
        return constructor;
    }
    private sealed class ConstructorHolder(ConstructorInfo value)
    {
        public ConstructorInfo Value { get; } = value;
    }

    /// <summary>Creates the immutable contract for a source constructor.</summary>
    public static StructuralInputContract CreateContract(
        string sourceName,
        ConstructorInfo constructor,
        StructuralInputLimits? limits = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentNullException.ThrowIfNull(constructor);

        var reflectedParameters = constructor.GetParameters();
        var contextParameters = reflectedParameters
            .Where(static parameter => parameter.ParameterType == typeof(SourceExecutionContext))
            .ToArray();
        if (contextParameters.Length > 1 ||
            contextParameters.Length == 1 && contextParameters[0].Position != reflectedParameters.Length - 1)
            throw new InvalidOperationException(
                $"Source constructor '{constructor}' may contain at most one final {nameof(SourceExecutionContext)} parameter.");

        var parameters = reflectedParameters
            .Where(static parameter => parameter.ParameterType != typeof(SourceExecutionContext))
            .Select(parameter =>
            {
                var type = StructuralTypeDescriptor.FromClrType(parameter.ParameterType, !parameter.ParameterType.IsValueType);
                var @default = parameter.HasDefaultValue
                    ? StructuralDefaultDescriptor.Create(parameter.DefaultValue, StructuralSqlLiteralFormatter.Format(parameter.DefaultValue))
                    : StructuralDefaultDescriptor.Absent;
                return new StructuralParameterDescriptor(
                    parameter.Name ?? throw new InvalidOperationException("Structural source parameter has no name."),
                    type,
                    !parameter.HasDefaultValue,
                    @default);
            })
            .ToArray();
        return new StructuralInputContract(sourceName, parameters, limits);
    }
}
/// <summary>Immutable Core-owned structural value tree for host boundaries.</summary>
public sealed class StructuralValue
{
    private StructuralValue(
        StructuralTypeKind kind,
        object? scalar,
        IReadOnlyDictionary<string, StructuralValue>? fields,
        IReadOnlyList<StructuralValue>? elements)
    {
        Kind = kind;
        Scalar = scalar;
        Fields = fields == null
            ? new ReadOnlyDictionary<string, StructuralValue>(new Dictionary<string, StructuralValue>(StringComparer.OrdinalIgnoreCase))
            : new ReadOnlyDictionary<string, StructuralValue>(new Dictionary<string, StructuralValue>(fields, StringComparer.OrdinalIgnoreCase));
        Elements = new ReadOnlyCollection<StructuralValue>((elements ?? []).ToArray());
    }

    public StructuralTypeKind Kind { get; }
    public object? Scalar { get; }
    public IReadOnlyDictionary<string, StructuralValue> Fields { get; }
    public IReadOnlyList<StructuralValue> Elements { get; }

    public static StructuralValue FromScalar(object? value) => new(StructuralTypeKind.Scalar, value, null, null);

    public static StructuralValue FromRecord(IEnumerable<KeyValuePair<string, StructuralValue>> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var dictionary = new Dictionary<string, StructuralValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field.Key);
            ArgumentNullException.ThrowIfNull(field.Value);
            if (!dictionary.TryAdd(field.Key, field.Value))
                throw new ArgumentException($"Duplicate structural field '{field.Key}'.", nameof(fields));
        }

        return new StructuralValue(StructuralTypeKind.Record, null, dictionary, null);
    }

    public static StructuralValue FromCollection(IEnumerable<StructuralValue> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        return new StructuralValue(StructuralTypeKind.Collection, null, null, elements.ToArray());
    }
}

/// <summary>Immutable constructor metadata used by binding and typed lowering.</summary>
public sealed class StructuralConstructorDescriptor
{
    public StructuralConstructorDescriptor(
        ConstructorInfo constructor,
        IReadOnlyList<StructuralFieldDescriptor> parameters)
    {
        Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        Parameters = new ReadOnlyCollection<StructuralFieldDescriptor>((parameters ?? throw new ArgumentNullException(nameof(parameters))).ToArray());
    }

    public ConstructorInfo Constructor { get; }
    public IReadOnlyList<StructuralFieldDescriptor> Parameters { get; }
}

/// <summary>Describes one source parameter and its structural contract.</summary>
public sealed class StructuralParameterDescriptor
{
    public StructuralParameterDescriptor(
        string name,
        StructuralTypeDescriptor type,
        bool required,
        StructuralDefaultDescriptor? @default = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Required = required;
        Default = @default ?? StructuralDefaultDescriptor.Absent;
    }

    public string Name { get; }
    public StructuralTypeDescriptor Type { get; }
    public bool Required { get; }
    public StructuralDefaultDescriptor Default { get; }
    public bool HasDefault => Default.HasValue;
}

/// <summary>Immutable source signature metadata for structural arguments.</summary>
public sealed class StructuralInputContract
{
    public StructuralInputContract(
        string sourceName,
        IEnumerable<StructuralParameterDescriptor> parameters,
        StructuralInputLimits? limits = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        SourceName = sourceName;
        Parameters = new ReadOnlyCollection<StructuralParameterDescriptor>((parameters ?? throw new ArgumentNullException(nameof(parameters))).ToArray());
        Limits = limits ?? StructuralInputLimits.Default;
    }

    public string SourceName { get; }
    public IReadOnlyList<StructuralParameterDescriptor> Parameters { get; }
    public StructuralInputLimits Limits { get; }
}

/// <summary>Default safety bounds for one structural input root.</summary>
public readonly record struct StructuralInputLimits
{
    public StructuralInputLimits(int maxDepth, int maxNodes, long maxStringBytes)
    {
        if (maxDepth <= 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
        if (maxNodes <= 0) throw new ArgumentOutOfRangeException(nameof(maxNodes));
        if (maxStringBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxStringBytes));
        MaxDepth = maxDepth;
        MaxNodes = maxNodes;
        MaxStringBytes = maxStringBytes;
    }

    public static StructuralInputLimits Default { get; } = new(32, 100_000, 67_108_864);
    public int MaxDepth { get; }
    public int MaxNodes { get; }
    public long MaxStringBytes { get; }
}
/// <summary>Immutable indexed field mapping used by a structural construction loop.</summary>
public sealed class StructuralConstructionPlan
{
    public StructuralConstructionPlan(
        Type targetType,
        StructuralConstructorDescriptor constructor,
        IEnumerable<int> sourceFieldIndexes)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        SourceFieldIndexes = new ReadOnlyCollection<int>((sourceFieldIndexes ?? throw new ArgumentNullException(nameof(sourceFieldIndexes))).ToArray());
    }

    public Type TargetType { get; }
    public StructuralConstructorDescriptor Constructor { get; }
    public IReadOnlyList<int> SourceFieldIndexes { get; }
}
