using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution.Portability;

/// <summary>
/// Resolves CLR bindings only when a CLR-aware phase explicitly asks for them.
/// Execution references retain descriptors and stable identities, not reflection objects.
/// </summary>
internal static class ExecutionClrBindingResolver
{
    public static Type ResolveType(ExecutionPortableTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return ResolveTypeCore(descriptor, null);
    }

    public static Type ResolveType(
        ExecutionPortableTypeDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly> semanticAssemblies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(semanticAssemblies);

        return ResolveTypeCore(descriptor, semanticAssemblies);
    }

    private static Type ResolveTypeCore(
        ExecutionPortableTypeDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly>? semanticAssemblies)
    {

        if (string.Equals(descriptor.StableName, "host-opaque:dynamic-object", StringComparison.Ordinal))
            return typeof(object);

        return descriptor.Kind switch
        {
            ExecutionPortableTypeKind.Primitive => ResolvePrimitive(descriptor.StableName),
            ExecutionPortableTypeKind.Nullable => ResolveNullable(descriptor, semanticAssemblies),
            ExecutionPortableTypeKind.Array => ResolveArray(descriptor, semanticAssemblies),
            ExecutionPortableTypeKind.Sequence or
                ExecutionPortableTypeKind.List or
                ExecutionPortableTypeKind.Map or
                ExecutionPortableTypeKind.Set or
                ExecutionPortableTypeKind.Pair => ResolveContainer(descriptor, semanticAssemblies),
            ExecutionPortableTypeKind.ByRef => ResolveTypeCore(
                descriptor.Arguments.Single(),
                semanticAssemblies).MakeByRefType(),
            ExecutionPortableTypeKind.HostOpaque or ExecutionPortableTypeKind.ClrOnly =>
                ResolveClrIdentity(descriptor, semanticAssemblies),
            ExecutionPortableTypeKind.GeneratedRow => typeof(Tables.Row),
            ExecutionPortableTypeKind.GenericParameter => typeof(object),
            _ => throw Unsupported(
                $"Execution type descriptor '{descriptor.StableName}' has no CLR binding.")
        };
    }

    public static MethodInfo ResolveMethod(ExecutionPortableCallableDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return ResolveMethodCore(descriptor, null);
    }

    public static MethodInfo ResolveMethod(
        ExecutionPortableCallableDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly> semanticAssemblies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(semanticAssemblies);
        return ResolveMethodCore(descriptor, semanticAssemblies);
    }

    private static MethodInfo ResolveMethodCore(
        ExecutionPortableCallableDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly>? semanticAssemblies)
    {
        if (descriptor.DeclaringType is not { } declaringTypeDescriptor)
            throw Unsupported($"Callable '{descriptor.StableName}' has no declaring type.");

        var declaringType = ResolveTypeCore(declaringTypeDescriptor, semanticAssemblies);
        var candidates = declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => string.Equals(method.Name, descriptor.MethodName, StringComparison.Ordinal))
            .Where(method => method.IsStatic == descriptor.IsStatic)
            .Where(method => method.GetGenericArguments().Length == descriptor.GenericArity)
            .ToArray();

        var match = candidates
            .Select(method => TryCloseGenericMethod(method, descriptor, semanticAssemblies))
            .FirstOrDefault(method => method is not null &&
                string.Equals(ExecutionPortableSymbolFactory.FromMethod(method).StableName, descriptor.StableName,
                    StringComparison.Ordinal));
        if (match is not null)
            return match;

        throw Unsupported(
            $"Callable descriptor '{descriptor.StableName}' could not be bound to a CLR method.");
    }

    private static MethodInfo? TryCloseGenericMethod(
        MethodInfo method,
        ExecutionPortableCallableDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly>? semanticAssemblies)
    {
        if (!method.IsGenericMethodDefinition)
            return method;

        var genericParameters = method.GetGenericArguments();
        var inferredArguments = new Type?[genericParameters.Length];
        var parameters = method.GetParameters();
        if (parameters.Length != descriptor.ParameterTypes.Count)
            return null;

        for (var index = 0; index < parameters.Length; index++)
        {
            var actualType = ResolveTypeCore(descriptor.ParameterTypes[index], semanticAssemblies);
            if (!TryInferGenericArguments(
                    parameters[index].ParameterType,
                    actualType,
                    genericParameters,
                    inferredArguments))
            {
                return null;
            }
        }

        if (inferredArguments.Any(static argument => argument is null))
            return null;

        try
        {
            return method.MakeGenericMethod(inferredArguments.Select(static argument => argument!).ToArray());
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool TryInferGenericArguments(
        Type parameterPattern,
        Type actualType,
        IReadOnlyList<Type> genericParameters,
        IList<Type?> inferredArguments)
    {
        if (parameterPattern.IsGenericParameter)
        {
            var index = Array.IndexOf(genericParameters.ToArray(), parameterPattern);
            if (index < 0)
                return false;

            if (inferredArguments[index] is null)
            {
                inferredArguments[index] = actualType;
                return true;
            }

            return inferredArguments[index] == actualType;
        }

        if (parameterPattern.IsArray)
        {
            return actualType.IsArray &&
                   parameterPattern.GetArrayRank() == actualType.GetArrayRank() &&
                   TryInferGenericArguments(
                       parameterPattern.GetElementType()!,
                       actualType.GetElementType()!,
                       genericParameters,
                       inferredArguments);
        }

        if (parameterPattern.IsByRef)
        {
            return actualType.IsByRef && TryInferGenericArguments(
                parameterPattern.GetElementType()!,
                actualType.GetElementType()!,
                genericParameters,
                inferredArguments);
        }

        if (parameterPattern.IsGenericType && actualType.IsGenericType)
        {
            var parameterArguments = parameterPattern.GetGenericArguments();
            var actualArguments = actualType.GetGenericArguments();
            if (parameterArguments.Length != actualArguments.Length)
                return false;

            for (var index = 0; index < parameterArguments.Length; index++)
            {
                if (!TryInferGenericArguments(
                        parameterArguments[index],
                        actualArguments[index],
                        genericParameters,
                        inferredArguments))
                {
                    return false;
                }
            }

            return true;
        }

        return true;
    }

    private static Type ResolvePrimitive(string stableName)
    {
        return stableName switch
        {
            "primitive:bool" => typeof(bool),
            "primitive:uint8" => typeof(byte),
            "primitive:int8" => typeof(sbyte),
            "primitive:int16" => typeof(short),
            "primitive:uint16" => typeof(ushort),
            "primitive:int32" => typeof(int),
            "primitive:uint32" => typeof(uint),
            "primitive:int64" => typeof(long),
            "primitive:uint64" => typeof(ulong),
            "primitive:float32" => typeof(float),
            "primitive:float64" => typeof(double),
            "primitive:decimal" => typeof(decimal),
            "primitive:char" => typeof(char),
            "primitive:string" => typeof(string),
            "primitive:datetime" => typeof(DateTime),
            "primitive:datetimeoffset" => typeof(DateTimeOffset),
            "primitive:guid" => typeof(Guid),
            "primitive:timespan" => typeof(TimeSpan),
            "primitive:void" => typeof(void),
            _ => throw Unsupported($"Unknown primitive descriptor '{stableName}'.")
        };
    }

    private static Type ResolveNullable(
        ExecutionPortableTypeDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly>? semanticAssemblies) =>
        typeof(Nullable<>).MakeGenericType(
            ResolveTypeCore(descriptor.Arguments.Single(), semanticAssemblies));

    private static Type ResolveArray(
        ExecutionPortableTypeDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly>? semanticAssemblies)
    {
        var element = ResolveTypeCore(descriptor.Arguments.Single(), semanticAssemblies);
        return descriptor.ArrayRank.GetValueOrDefault(1) == 1
            ? element.MakeArrayType()
            : element.MakeArrayType(descriptor.ArrayRank!.Value);
    }

    private static Type ResolveContainer(
        ExecutionPortableTypeDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly>? semanticAssemblies)
    {
        var arguments = descriptor.Arguments
            .Select(argument => ResolveTypeCore(argument, semanticAssemblies))
            .ToArray();
        var definition = descriptor.Container?.Kind ??
                        (descriptor.Kind switch
                        {
                            ExecutionPortableTypeKind.Sequence => ExecutionPortableContainerKind.Sequence,
                            ExecutionPortableTypeKind.List => ExecutionPortableContainerKind.List,
                            ExecutionPortableTypeKind.Map => ExecutionPortableContainerKind.Map,
                            ExecutionPortableTypeKind.Set => ExecutionPortableContainerKind.Set,
                            ExecutionPortableTypeKind.Pair => ExecutionPortableContainerKind.Pair,
                            _ => throw Unsupported($"Unknown container descriptor '{descriptor.StableName}'.")
                        });

        var genericDefinition = descriptor.Container?.BindingKind switch
        {
            ExecutionPortableContainerBindingKind.Enumerable => typeof(IEnumerable<>),
            ExecutionPortableContainerBindingKind.ReadOnlyCollection => typeof(IReadOnlyCollection<>),
            ExecutionPortableContainerBindingKind.ReadOnlyList => typeof(IReadOnlyList<>),
            ExecutionPortableContainerBindingKind.Collection => typeof(ICollection<>),
            ExecutionPortableContainerBindingKind.ListInterface => typeof(IList<>),
            ExecutionPortableContainerBindingKind.List => typeof(List<>),
            ExecutionPortableContainerBindingKind.ReadOnlyDictionary => typeof(IReadOnlyDictionary<,>),
            ExecutionPortableContainerBindingKind.DictionaryInterface => typeof(IDictionary<,>),
            ExecutionPortableContainerBindingKind.Dictionary => typeof(Dictionary<,>),
            ExecutionPortableContainerBindingKind.HashSet => typeof(HashSet<>),
            ExecutionPortableContainerBindingKind.KeyValuePair => typeof(KeyValuePair<,>),
            _ => definition switch
            {
                ExecutionPortableContainerKind.Sequence => typeof(IReadOnlyList<>),
                ExecutionPortableContainerKind.List => typeof(List<>),
                ExecutionPortableContainerKind.Map => descriptor.Container?.IsMutable == false
                    ? typeof(IReadOnlyDictionary<,>)
                    : typeof(Dictionary<,>),
                ExecutionPortableContainerKind.Set => typeof(HashSet<>),
                ExecutionPortableContainerKind.Pair => typeof(KeyValuePair<,>),
                _ => throw new ArgumentOutOfRangeException()
            }
        };

        return genericDefinition.MakeGenericType(arguments);
    }

    private static Type ResolveClrIdentity(
        ExecutionPortableTypeDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly>? semanticAssemblies)
    {
        var stableName = descriptor.StableName;
        var identity = stableName[(stableName.IndexOf(':') + 1)..];
        var genericStart = identity.IndexOf('<');
        if (genericStart >= 0)
            identity = identity[..genericStart];

        var separator = identity.LastIndexOf('@');
        var typeName = separator < 0 ? identity : identity[..separator];
        var assemblyName = separator < 0 ? null : identity[(separator + 1)..];
        if (typeName == "Musoq.Parser.Nodes.NullNode+NullType")
            return typeof(object);

        Type? type = null;
        if (assemblyName is not null &&
            semanticAssemblies is not null &&
            semanticAssemblies.TryGetValue(assemblyName, out var semanticAssembly))
        {
            type = semanticAssembly.GetType(typeName, throwOnError: false);
        }

        var assemblyQualifiedTypeName = assemblyName is null ? typeName : $"{typeName}, {assemblyName}";
        type ??= Type.GetType(assemblyQualifiedTypeName, throwOnError: false) ??
                 Type.GetType(typeName, throwOnError: false);
        if (type is null && semanticAssemblies is not null)
        {
            foreach (var candidate in semanticAssemblies.Values)
            {
                type = candidate.GetType(typeName, throwOnError: false);
                if (type is not null)
                    break;
            }
        }
        if (type is null)
            throw Unsupported($"CLR type descriptor '{stableName}' could not be resolved.");

        if (descriptor.Arguments.Count == 0)
            return type;

        if (!type.IsGenericTypeDefinition)
            throw Unsupported(
                $"CLR type descriptor '{stableName}' resolved to non-generic type '{type}'.");

        return type.MakeGenericType(
            descriptor.Arguments
                .Select(argument => ResolveTypeCore(argument, semanticAssemblies))
                .ToArray());
    }

    private static NotSupportedException Unsupported(string message) => new(message);
}
