using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Musoq.Targets.CSharpClr;

/// <summary>
/// Collects only the file-backed CLR assemblies named by one execution plan.
/// </summary>
internal sealed class CSharpClrReferenceAssemblyCollector
{
    private readonly CSharpClrExecutionBindingContext _bindingContext;
    private readonly IReadOnlySet<string> _preloadedAssemblyPaths;
    private readonly IReadOnlySet<string> _preloadedAssemblyNames;
    private readonly IReadOnlySet<string> _seededAssemblyNames;
    private readonly IReadOnlyDictionary<string, Assembly> _semanticAssembliesByName;
    private readonly Dictionary<string, Assembly> _assembliesByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Assembly> _assembliesByIdentity = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Assembly, string?> _normalizedAssemblyPaths = [];
    private readonly HashSet<string> _visitedDescriptorNames = new(StringComparer.Ordinal);
    private readonly HashSet<string> _visitedCallableNames = new(StringComparer.Ordinal);
    private readonly HashSet<Type> _visitedTypes = [];
    private readonly List<Assembly> _assemblies = [];

    private CSharpClrReferenceAssemblyCollector(
        CSharpClrExecutionBindingContext bindingContext,
        IReadOnlySet<string> preloadedAssemblyPaths,
        IEnumerable<Assembly> semanticReferences)
    {
        _bindingContext = bindingContext ?? throw new ArgumentNullException(nameof(bindingContext));
        _preloadedAssemblyPaths = NormalizePaths(preloadedAssemblyPaths);
        _preloadedAssemblyNames = _preloadedAssemblyPaths
            .Select(static path => Path.GetFileNameWithoutExtension(path))
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seededAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var semanticAssembliesByName = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in semanticReferences)
        {
            if (assembly.GetName().Name is not { Length: > 0 } name)
                continue;

            seededAssemblyNames.Add(name);
            semanticAssembliesByName.TryAdd(name, assembly);
        }

        _seededAssemblyNames = seededAssemblyNames;
        _semanticAssembliesByName = semanticAssembliesByName;
    }

    internal static IReadOnlyList<Assembly> Collect(
        ExecutionTargetCompatibilityReport compatibilityReport,
        CSharpClrExecutionBindingContext bindingContext,
        IEnumerable<Assembly> semanticReferences,
        IEnumerable<Type> additionalReferenceTypes,
        Type? outputType,
        IReadOnlySet<string> preloadedAssemblyPaths)
    {
        ArgumentNullException.ThrowIfNull(compatibilityReport);
        ArgumentNullException.ThrowIfNull(semanticReferences);
        ArgumentNullException.ThrowIfNull(additionalReferenceTypes);
        ArgumentNullException.ThrowIfNull(preloadedAssemblyPaths);

        var semanticAssemblyArray = semanticReferences.ToArray();
        var collector = new CSharpClrReferenceAssemblyCollector(
            bindingContext,
            preloadedAssemblyPaths,
            semanticAssemblyArray);

        foreach (var assembly in semanticAssemblyArray)
            collector.AddAssembly(assembly, skipPreloaded: false);

        foreach (var requirement in compatibilityReport.Requirements)
        {
            if (requirement.TypeSymbol is { } typeSymbol)
                collector.VisitDescriptor(typeSymbol, requirement.Detail);

            if (requirement.CallableSymbol is { } callableSymbol)
                collector.VisitCallable(callableSymbol, requirement.Detail);
        }

        foreach (var referenceType in additionalReferenceTypes)
            collector.VisitRuntimeType(referenceType, $"additional reference type '{referenceType}'");

        if (outputType is not null)
            collector.VisitRuntimeType(outputType, $"typed output '{outputType}'");

        return collector._assemblies.ToArray();
    }

    private void VisitDescriptor(ExecutionPortableTypeDescriptor descriptor, string requirementDetail)
    {
        if (!_visitedDescriptorNames.Add(descriptor.StableName))
            return;

        foreach (var argument in descriptor.Arguments)
            VisitDescriptor(argument, requirementDetail);

        foreach (var field in descriptor.Fields)
            VisitDescriptor(field.Type, requirementDetail);

        if (descriptor.Kind is ExecutionPortableTypeKind.Primitive or
            ExecutionPortableTypeKind.GenericParameter or
            ExecutionPortableTypeKind.GeneratedRow)
        {
            return;
        }

        if (descriptor.Kind is ExecutionPortableTypeKind.Nullable or
            ExecutionPortableTypeKind.Array or
            ExecutionPortableTypeKind.Sequence or
            ExecutionPortableTypeKind.List or
            ExecutionPortableTypeKind.Map or
            ExecutionPortableTypeKind.Set or
            ExecutionPortableTypeKind.Pair or
            ExecutionPortableTypeKind.ByRef ||
            IsDescriptorAssemblyPreloaded(descriptor))
        {
            return;
        }

        Type type;
        try
        {
            type = _bindingContext.BindType(descriptor, _semanticAssembliesByName);
        }
        catch (Exception exception) when (IsExpectedBindingFailure(exception))
        {
            throw CreateFailure(descriptor.StableName, requirementDetail, exception);
        }

        VisitRuntimeType(type, requirementDetail);
    }

    private void VisitCallable(ExecutionPortableCallableDescriptor descriptor, string requirementDetail)
    {
        if (!_visitedCallableNames.Add(descriptor.StableName))
            return;

        if (descriptor.DeclaringType is { } declaringType)
            VisitDescriptor(declaringType, requirementDetail);

        if (descriptor.ReturnType is { } returnType)
            VisitDescriptor(returnType, requirementDetail);

        foreach (var parameterType in descriptor.ParameterTypes)
            VisitDescriptor(parameterType, requirementDetail);

        if (descriptor.DeclaringType is { } preloadedDeclaringType &&
            IsDescriptorPreloaded(preloadedDeclaringType) &&
            (descriptor.ReturnType is null || IsDescriptorPreloaded(descriptor.ReturnType)) &&
            descriptor.ParameterTypes.All(IsDescriptorPreloaded))
        {
            return;
        }

        if (descriptor.DeclaringType is { } seededDeclaringType &&
            IsDescriptorAssemblySeeded(seededDeclaringType))
        {
            return;
        }

        MethodInfo method;
        try
        {
            method = _bindingContext.BindMethod(descriptor, _semanticAssembliesByName);
        }
        catch (Exception exception) when (IsExpectedBindingFailure(exception))
        {
            throw CreateFailure(descriptor.StableName, requirementDetail, exception);
        }

        AddAssembly(
            method.Module.Assembly,
            skipPreloaded: true,
            required: true,
            requirementDetail);
        if (method.DeclaringType is { } runtimeDeclaringType)
            VisitRuntimeType(runtimeDeclaringType, requirementDetail);
        VisitRuntimeType(method.ReturnType, requirementDetail);
        foreach (var parameter in method.GetParameters())
            VisitRuntimeType(parameter.ParameterType, requirementDetail);

        if (method.IsGenericMethod)
        {
            foreach (var parameter in method.GetGenericMethodDefinition().GetGenericArguments())
            {
                foreach (var constraint in parameter.GetGenericParameterConstraints())
                    VisitRuntimeType(constraint, requirementDetail);
            }

            foreach (var argument in method.GetGenericArguments())
                VisitRuntimeType(argument, requirementDetail);
        }
    }

    private void VisitRuntimeType(Type type, string requirementDetail)
    {
        if (type is null)
            return;

        if (type.IsGenericParameter)
        {
            foreach (var constraint in type.GetGenericParameterConstraints())
                VisitRuntimeType(constraint, requirementDetail);

            return;
        }

        if (!_visitedTypes.Add(type))
            return;

        AddAssembly(type.Assembly, skipPreloaded: true, required: true, requirementDetail);

        if (type.IsByRef || type.IsPointer || type.IsArray)
        {
            if (type.GetElementType() is { } elementType)
                VisitRuntimeType(elementType, requirementDetail);

            return;
        }

        if (type.IsGenericType)
        {
            VisitRuntimeType(type.GetGenericTypeDefinition(), requirementDetail);
            foreach (var argument in type.GetGenericArguments())
                VisitRuntimeType(argument, requirementDetail);
        }

        if (IsAssemblyPreloaded(type.Assembly))
            return;

        if (type.IsGenericTypeDefinition)
        {
            foreach (var parameter in type.GetGenericArguments())
            {
                foreach (var constraint in parameter.GetGenericParameterConstraints())
                    VisitRuntimeType(constraint, requirementDetail);
            }
        }

        if (type.BaseType is { } baseType)
            VisitRuntimeType(baseType, requirementDetail);

        foreach (var interfaceType in type.GetInterfaces())
            VisitRuntimeType(interfaceType, requirementDetail);
    }

    private void AddAssembly(
        Assembly assembly,
        bool skipPreloaded,
        bool required = false,
        string? requirementDetail = null)
    {
        var location = GetNormalizedAssemblyPath(assembly);
        if (string.IsNullOrWhiteSpace(location))
        {
            if (required)
            {
                throw CreateFailure(
                    GetAssemblyIdentity(assembly),
                    requirementDetail ?? "execution-plan CLR reference",
                    "the assembly has no file-backed location",
                    new InvalidOperationException("The required CLR assembly has no file-backed location."));
            }

            return;
        }

        if (skipPreloaded && _preloadedAssemblyPaths.Contains(location))
            return;

        if (_assembliesByPath.ContainsKey(location))
            return;

        if (required && !File.Exists(location))
        {
            throw CreateFailure(
                GetAssemblyIdentity(assembly),
                requirementDetail ?? "execution-plan CLR reference",
                $"the assembly file '{location}' does not exist",
                new FileNotFoundException("The required CLR assembly file does not exist.", location));
        }

        var identity = GetAssemblyIdentity(assembly);
        if (_assembliesByIdentity.ContainsKey(identity))
            return;

        _assembliesByPath.Add(location, assembly);
        _assembliesByIdentity.Add(identity, assembly);
        _assemblies.Add(assembly);
    }

    private static IReadOnlySet<string> NormalizePaths(IEnumerable<string> paths)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            if (!string.IsNullOrWhiteSpace(path))
                normalized.Add(NormalizePath(path));
        }

        return normalized;
    }

    private static string NormalizePath(string path) => Path.GetFullPath(path);

    private string? GetNormalizedAssemblyPath(Assembly assembly)
    {
        if (_normalizedAssemblyPaths.TryGetValue(assembly, out var path))
            return path;

        try
        {
            path = string.IsNullOrWhiteSpace(assembly.Location)
                ? null
                : NormalizePath(assembly.Location);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException)
        {
            if (!string.IsNullOrWhiteSpace(assembly.Location))
                throw CreateFailure(
                    GetAssemblyIdentity(assembly),
                    "execution-plan CLR reference",
                    "the assembly location could not be normalized",
                    exception);

            path = null;
        }

        _normalizedAssemblyPaths.Add(assembly, path);
        return path;
    }

    private bool IsAssemblyPreloaded(Assembly assembly)
    {
        var path = GetNormalizedAssemblyPath(assembly);
        return path is not null && _preloadedAssemblyPaths.Contains(path);
    }

    private bool IsDescriptorAssemblyPreloaded(ExecutionPortableTypeDescriptor descriptor)
    {
        var stableName = descriptor.StableName;
        var separator = stableName.IndexOf('@');
        if (separator < 0)
            return string.Equals(stableName, "host-opaque:dynamic-object", StringComparison.Ordinal);

        return IsStableAssemblyNameInSet(stableName, separator, _preloadedAssemblyNames);
    }

    private bool IsDescriptorPreloaded(ExecutionPortableTypeDescriptor descriptor)
    {
        if (descriptor.Kind is ExecutionPortableTypeKind.Primitive or
            ExecutionPortableTypeKind.GenericParameter or
            ExecutionPortableTypeKind.GeneratedRow)
        {
            return true;
        }

        if (descriptor.Kind is ExecutionPortableTypeKind.Nullable or
            ExecutionPortableTypeKind.Array or
            ExecutionPortableTypeKind.Sequence or
            ExecutionPortableTypeKind.List or
            ExecutionPortableTypeKind.Map or
            ExecutionPortableTypeKind.Set or
            ExecutionPortableTypeKind.Pair or
            ExecutionPortableTypeKind.ByRef)
        {
            return descriptor.Arguments.All(IsDescriptorPreloaded);
        }

        return IsDescriptorAssemblyPreloaded(descriptor) &&
               descriptor.Arguments.All(IsDescriptorPreloaded);
    }

    private bool IsDescriptorAssemblySeeded(ExecutionPortableTypeDescriptor descriptor)
    {
        var stableName = descriptor.StableName;
        var separator = stableName.IndexOf('@');
        if (separator < 0)
            return false;

        return IsStableAssemblyNameInSet(stableName, separator, _seededAssemblyNames);
    }

    private static bool IsStableAssemblyNameInSet(
        string stableName,
        int separator,
        IReadOnlySet<string> assemblyNames)
    {
        var end = stableName.IndexOf('<', separator + 1);
        var length = (end < 0 ? stableName.Length : end) - separator - 1;
        var assemblyName = stableName.AsSpan(separator + 1, length);
        foreach (var candidate in assemblyNames)
        {
            if (assemblyName.Equals(candidate.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsExpectedBindingFailure(Exception exception) =>
        exception is ArgumentException or
            BadImageFormatException or
            FileLoadException or
            FileNotFoundException or
            InvalidOperationException or
            NotSupportedException or
            TypeLoadException;

    private static CSharpClrReferenceDiscoveryException CreateFailure(
        string assemblyIdentity,
        string requirementDetail,
        Exception exception)
    {
        var reason = exception switch
        {
            FileNotFoundException => "the assembly file could not be found",
            BadImageFormatException => "the assembly file is not a valid CLR metadata image",
            FileLoadException => "the assembly file could not be loaded",
            InvalidOperationException or NotSupportedException or TypeLoadException or ArgumentException =>
                "the CLR descriptor could not be resolved",
            _ => exception.Message
        };

        return new CSharpClrReferenceDiscoveryException(
            assemblyIdentity,
            requirementDetail,
            reason,
            exception);
    }

    private static CSharpClrReferenceDiscoveryException CreateFailure(
        string assemblyIdentity,
        string requirementDetail,
        string reason,
        Exception exception) =>
        new(assemblyIdentity, requirementDetail, reason, exception);

    private static string GetAssemblyIdentity(Assembly assembly) =>
        assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();
}
