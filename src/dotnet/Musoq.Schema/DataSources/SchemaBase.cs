using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Musoq.Schema.Attributes;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;
using ConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Schema.DataSources;

public abstract class SchemaBase : ISchema
{
    private const string SourcePart = "_source";
    private const string TablePart = "_table";

    private readonly MethodsAggregator _aggregator;

    protected SchemaBase(string name, MethodsAggregator methodsAggregator)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "initializing a schema");

        Name = name;
        _aggregator = methodsAggregator ??
                      throw SchemaArgumentException.ForNullArgument(nameof(methodsAggregator), "initializing a schema");

        AddTable<SingleRowSchemaTable>("empty");
        AddSource<SingleRowSource>("empty");
    }

    private List<SchemaMethodInfo> ConstructorsMethods { get; } = [];
    private Dictionary<string, object?[]> AdditionalArguments { get; } = new();

    public string Name { get; }

    public virtual ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "getting a table by name");

        if (metadataContext == null)
            throw SchemaArgumentException.ForNullArgument(nameof(metadataContext), "getting a table by name");

        var tableName = $"{NormalizeSchemaMemberName(name)}{TablePart}";
        return ResolveAndCreate<ISchemaTable>(name, tableName, GetAvailableTableNames, parameters);
    }

    public virtual SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "describing a source");

        if (context == null)
            throw SchemaArgumentException.ForNullArgument(nameof(context), "describing a source");

        var table = GetTableByName(name, context.MetadataContext, parameters);
        return new SourceDescriptor
        {
            Identity = context.Identity,
            RowType = table.Metadata?.TableEntityType,
            Columns = table.Columns
        };
    }

    public virtual IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
        string name,
        SourceRuntimeSettingsDescribeContext context,
        params object?[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "describing source runtime settings");

        if (context == null)
            throw SchemaArgumentException.ForNullArgument(nameof(context), "describing source runtime settings");

        var tableName = $"{NormalizeSchemaMemberName(name)}{TablePart}";
        var sourceName = $"{NormalizeSchemaMemberName(name)}{SourcePart}";
        var requirements = new Dictionary<string, SourceRuntimeSettingRequirement>(StringComparer.Ordinal);

        foreach (var constructor in GetMatchingConstructors(tableName, parameters)
                     .Concat(GetMatchingConstructors(sourceName, parameters)))
        {
            var originConstructor = constructor.OriginConstructor;
            if (originConstructor == null)
                continue;

            foreach (var attribute in originConstructor.GetCustomAttributes<SourceRuntimeSettingAttribute>())
                AddOrMergeRequirement(requirements, attribute.ToRequirement());
        }

        return requirements.Values.ToArray();
    }

    public virtual SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "planning a source");

        if (request == null)
            throw SchemaArgumentException.ForNullArgument(nameof(request), "planning a source");

        return SourcePlanResult.RejectAll(request);
    }

    public virtual RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "getting a row source");

        if (executionContext == null)
            throw SchemaArgumentException.ForNullArgument(nameof(executionContext), "getting a row source");

        var sourceName = $"{NormalizeSchemaMemberName(name)}{SourcePart}";

        ValidateRequestedRowType<T>(name, executionContext, parameters);

        if (AdditionalArguments.TryGetValue(sourceName, out var argument))
            parameters = parameters.ExpandParameters(argument);

        return ResolveAndCreate<RowSource<T>>(name, sourceName, GetAvailableSourceNames, parameters, (ci, p) =>
        {
            if (ci.SupportsInterCommunicator)
                return p.ExpandParameters(executionContext);
            return p;
        });
    }

    protected void ValidateRequestedRowType<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        var metadataContext = new SourceMetadataContext(
            executionContext.QueryId,
            executionContext.EndWorkToken,
            executionContext.AllColumns,
            executionContext.SourceRuntimeSettings,
            executionContext.Logger);
        var table = GetTableByName(name, metadataContext, parameters);
        var entityType = table.Metadata?.TableEntityType;

        if (entityType == null || entityType == typeof(T))
            return;

        throw new InvalidOperationException(
            $"Schema table '{name}' declares row type '{entityType.FullName}', but source was requested as '{typeof(T).FullName}'.");
    }

    protected static RowSource<TRequested> EnsureSourceType<TRequested, TActual>(
        string name,
        RowSource<TActual> source)
    {
        if (typeof(TRequested) != typeof(TActual))
        {
            throw new InvalidOperationException(
                $"Schema source '{name}' produces row type '{typeof(TActual).FullName}', but source was requested as '{typeof(TRequested).FullName}'.");
        }

        return (RowSource<TRequested>)(object)source;
    }

    protected static RowSource<T> EnsureSourceType<T>(string name, object source)
    {
        if (source is RowSource<T> typedSource)
            return typedSource;

        var sourceType = source?.GetType().FullName ?? "<null>";
        throw new InvalidOperationException(
            $"Schema source '{name}' cannot be returned as '{typeof(RowSource<T>).FullName}'. Actual source type is '{sourceType}'.");
    }

    private T ResolveAndCreate<T>(
        string displayName,
        string resolvedName,
        Func<string> getAvailableNames,
        object?[] parameters,
        Func<ConstructorInfo, object?[], object?[]>? transformParameters = null) where T : class
    {
        var methods = GetConstructors(resolvedName).Select(c => c.ConstructorInfo).ToArray();

        if (methods.Length == 0)
        {
            var available = getAvailableNames();
            throw SchemaArgumentException.ForInvalidMethodName(displayName, available);
        }

        if (!TryMatchConstructorWithParams(methods, parameters, out var constructorInfo))
        {
            var availableSignatures = methods.Select(GetMethodSignature).ToArray();
            var providedTypes = parameters.Select(p => p?.GetType().Name ?? "null").ToArray();
            throw MethodResolutionException.ForUnresolvedMethod(displayName, providedTypes, availableSignatures);
        }

        if (transformParameters != null)
            parameters = transformParameters(constructorInfo, parameters);

        try
        {
            var originConstructor = constructorInfo.OriginConstructor ??
                throw new InvalidOperationException($"Constructor metadata for '{displayName}' has no origin constructor.");
            return (T)originConstructor.Invoke(parameters);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create {typeof(T).Name} '{displayName}': {ex.Message}", ex);
        }
    }

    private IEnumerable<ConstructorInfo> GetMatchingConstructors(string resolvedName, object?[] parameters)
    {
        return GetConstructors(resolvedName)
            .Select(static constructor => constructor.ConstructorInfo)
            .Where(constructor => ParamsMatchConstructor(constructor, parameters));
    }

    private static void AddOrMergeRequirement(
        IDictionary<string, SourceRuntimeSettingRequirement> requirements,
        SourceRuntimeSettingRequirement requirement)
    {
        if (!requirements.TryGetValue(requirement.Name, out var existing))
        {
            requirements.Add(requirement.Name, requirement);
            return;
        }

        var description = string.IsNullOrWhiteSpace(existing.Description)
            ? requirement.Description
            : existing.Description;
        requirements[requirement.Name] = existing with
        {
            Required = existing.Required || requirement.Required,
            Secret = existing.Secret || requirement.Secret,
            Phases = existing.Phases | requirement.Phases,
            Description = description
        };
    }

    public virtual SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
    {
        ArgumentNullException.ThrowIfNull(metadataContext);
        metadataContext.EndWorkToken.ThrowIfCancellationRequested();

        return ConstructorsMethods
            .Where(cm => cm.MethodName.Contains(TablePart, StringComparison.Ordinal))
            .Select(cm =>
            {
                var index = cm.MethodName.IndexOf(TablePart, StringComparison.Ordinal);
                var rawMethodName = cm.MethodName[..index];
                return new SchemaMethodInfo(rawMethodName, cm.ConstructorInfo);
            }).ToArray();
    }

    public virtual SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext)
    {
        return GetRawConstructors(metadataContext).Where(constr => constr.MethodName == methodName).ToArray();
    }

    public bool TryResolveAggregationMethod(string method, Type[] parameters, Type? entityType,
        [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        return _aggregator.TryResolveAggregationMethod(method, parameters, entityType, out methodInfo);
    }

    public bool TryResolveAggregationMethod(
        string method,
        Type[] parameters,
        Type? entityType,
        Func<MethodInfo, bool> methodFilter,
        [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        return _aggregator.TryResolveAggregationMethod(method, parameters, entityType, methodFilter, out methodInfo);
    }

    public bool TryResolveWindowFunction(string method, [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        return _aggregator.TryResolveWindowFunction(method, out methodInfo);
    }

    public bool TryResolveMethod(string method, Type[] parameters, Type? entityType, [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        return _aggregator.TryResolveMethod(method, parameters, entityType, out methodInfo);
    }

    public bool TryResolveRawMethod(string method, Type[] parameters, [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        return _aggregator.TryResolveRawMethod(method, parameters, out methodInfo);
    }

    public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> GetAllLibraryMethods()
    {
        return _aggregator.GetAllMethods();
    }

    public void AddTable<TType>(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "adding a table");

        AddToConstructors<TType>($"{NormalizeSchemaMemberName(name)}{TablePart}");
    }

    public void AddSource<TType>(string name, params object?[] args)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "adding a source");

        var sourceName = $"{NormalizeSchemaMemberName(name)}{SourcePart}";
        AddToConstructors<TType>(sourceName);
        AdditionalArguments.Add(sourceName, args ?? []);
    }

    private static string NormalizeSchemaMemberName(string name)
    {
        var normalized = new char[name.Length];
        for (var index = 0; index < name.Length; index++)
            normalized[index] = char.ToLowerInvariant(name[index]);

        return new string(normalized);
    }

    public SchemaMethodInfo[] GetConstructors(string methodName)
    {
        return GetConstructors().Where(constr => constr.MethodName == methodName).ToArray();
    }

    public virtual SchemaMethodInfo[] GetConstructors()
    {
        return ConstructorsMethods.ToArray();
    }

    private static bool ParamsMatchConstructor(ConstructorInfo constructor, object?[] parameters)
    {
        var matchingResult = true;

        if (parameters.Length != constructor.Arguments.Length)
            return false;

        for (var i = 0; i < parameters.Length && matchingResult; ++i)
            matchingResult &= ParameterMatches(constructor.Arguments[i].Type, parameters[i]);

        return matchingResult;
    }

    private static bool TryMatchConstructorWithParams(
        ConstructorInfo[] constructors,
        object?[] parameters,
        [NotNullWhen(true)] out ConstructorInfo? foundedConstructor)
    {
        foreach (var constructor in constructors)
        {
            if (!ParamsMatchConstructor(constructor, parameters))
                continue;

            foundedConstructor = constructor;
            return true;
        }

        foundedConstructor = null;
        return false;
    }

    private static bool ParameterMatches(Type parameterType, object? value)
    {
        if (value is not null)
            return parameterType.IsInstanceOfType(value);

        return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) is not null;
    }

    private void AddToConstructors<TType>(string name)
    {
        var schemaMethodInfos = TypeHelper
            .GetSchemaMethodInfosForType<TType>(name);

        ConstructorsMethods.AddRange(schemaMethodInfos);
    }

    private string GetAvailableTableNames() => GetAvailableNames(TablePart, "No tables available");

    private string GetAvailableSourceNames() => GetAvailableNames(SourcePart, "No sources available");

    private string GetAvailableNames(string suffix, string noItemsMessage)
    {
        var names = ConstructorsMethods
            .Where(cm => cm.MethodName.Contains(suffix, StringComparison.Ordinal))
            .Select(cm => cm.MethodName.Replace(suffix, string.Empty, StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        return names.Length == 0 ? noItemsMessage : string.Join(", ", names);
    }

    private static string GetMethodSignature(ConstructorInfo constructorInfo)
    {
        var originConstructor = constructorInfo.OriginConstructor ??
            throw new InvalidOperationException("Constructor metadata has no origin constructor.");
        var parameters = originConstructor.GetParameters();
        var paramTypes = parameters.Select(p => p.ParameterType.Name).ToArray();
        return $"({string.Join(", ", paramTypes)})";
    }
}
