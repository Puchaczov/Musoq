using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

/// <summary>Reads each declaration kind explicitly, including combined aggregate/window declarations.</summary>
public static class SqlFunctionDeclarations
{
    private static readonly ConcurrentDictionary<MethodInfo, ImmutableArray<SqlFunctionDescriptor>> Cache = new();

    /// <summary>Reads immutable SQL contracts from one method without executing its library.</summary>
    public static ImmutableArray<SqlFunctionDescriptor> Read(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return Cache.GetOrAdd(method, ReadCore);
    }

    private static ImmutableArray<SqlFunctionDescriptor> ReadCore(MethodInfo method)
    {
        var declarations = ImmutableArray.CreateBuilder<SqlFunctionDescriptor>();
        var aggregate = method.GetCustomAttribute<AggregateFunctionAttribute>();
        var window = method.GetCustomAttribute<WindowFunctionAttribute>();
        if (aggregate is not null)
            declarations.Add(CreateAggregate(method, aggregate));
        if (window is not null)
            declarations.Add(CreateWindow(method, window));

        var scalar = method.GetCustomAttributes<BindableMethodAttribute>()
            .FirstOrDefault(static attribute => attribute is not AggregateFunctionAttribute and not WindowFunctionAttribute);
        if (scalar is not null)
            declarations.Add(CreateScalar(method, scalar));

        return declarations.ToImmutable();
    }

    private static SqlFunctionDescriptor CreateAggregate(MethodInfo method, AggregateFunctionAttribute attribute)
        => CreateScalar(method, attribute) with
        {
            Name = DeclaredName(method, attribute.Name), Kind = SqlFunctionKind.Aggregate,
            KernelType = attribute.KernelType,
            EmptyResultBehavior = attribute.EmptyResultBehavior,
            ExecutionPermissions = FunctionExecutionPermissionResolver.Resolve(method, SqlFunctionKind.Aggregate, attribute)
        };

    private static SqlFunctionDescriptor CreateWindow(MethodInfo method, WindowFunctionAttribute attribute)
    {
        if (attribute.KernelType is not null)
            return CreateScalar(method, attribute) with
            {
                Name = DeclaredName(method, attribute.Name), Kind = SqlFunctionKind.Window,
                Parameters = ReadParameters(method).Where(static parameter => !parameter.IsAggregateParent).ToImmutableArray(),
                KernelType = attribute.KernelType,
                WindowModes = attribute.Modes, RankingKind = attribute.RankingKind,
                WindowContextType = attribute.ContextType, RequiresWindowOrdering = attribute.RequiresOrderBy,
                WindowBatchKernelType = attribute.BatchKernelType,
                RequiresWindowPeerMetadata = attribute.RequiresPeerMetadata,
                EmptyResultBehavior = attribute.EmptyResultBehavior,
                ExecutionPermissions = FunctionExecutionPermissionResolver.Resolve(method, SqlFunctionKind.Window, attribute)
            };

        throw new InvalidOperationException($"Window declaration {method.DeclaringType?.FullName}.{method.Name} must register a concrete typed kernel; legacy factories are unsupported.");
    }

    private static SqlFunctionDescriptor CreateScalar(MethodInfo method, BindableMethodAttribute attribute)
        => new()
        {
            Name = method.Name, Kind = SqlFunctionKind.Scalar, Declaration = method,
            Parameters = ReadParameters(method), ResultType = method.ReturnType, Visibility = attribute.Visibility,
            ExecutionPermissions = FunctionExecutionPermissionResolver.Resolve(method, SqlFunctionKind.Scalar, attribute)
        };

    private static ImmutableArray<SqlFunctionParameter> ReadParameters(MethodInfo method)
        => method.GetParameters()
            .Where(static parameter => parameter.GetCustomAttribute<InjectTypeAttribute>() is null)
            .Select(static parameter => new SqlFunctionParameter(
                parameter.Name ?? "value", parameter.ParameterType, parameter.IsOptional,
                parameter.GetCustomAttribute<AggregateParentAttribute>() is not null))
            .ToImmutableArray();

    private static string DeclaredName(MethodInfo method, string? name)
        => string.IsNullOrWhiteSpace(name) ? method.Name : name;
}
