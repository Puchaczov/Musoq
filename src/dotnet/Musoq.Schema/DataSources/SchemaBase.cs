using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
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
    private Dictionary<string, object[]> AdditionalArguments { get; } = new();

    public string Name { get; }

    public virtual ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "getting a table by name");

        if (runtimeContext == null)
            throw SchemaArgumentException.ForNullArgument(nameof(runtimeContext), "getting a table by name");

        var tableName = $"{name.ToLowerInvariant()}{TablePart}";
        return ResolveAndCreate<ISchemaTable>(name, tableName, GetAvailableTableNames, parameters);
    }

    public virtual RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "getting a row source");

        if (runtimeContext == null)
            throw SchemaArgumentException.ForNullArgument(nameof(runtimeContext), "getting a row source");

        var sourceName = $"{name.ToLowerInvariant()}{SourcePart}";

        if (AdditionalArguments.TryGetValue(sourceName, out var argument))
            parameters = parameters.ExpandParameters(argument);

        return ResolveAndCreate<RowSource>(name, sourceName, GetAvailableSourceNames, parameters, (ci, p) =>
        {
            if (ci.SupportsInterCommunicator)
                return p.ExpandParameters(runtimeContext);
            return p;
        });
    }

    private T ResolveAndCreate<T>(
        string displayName,
        string resolvedName,
        Func<string> getAvailableNames,
        object[] parameters,
        Func<ConstructorInfo, object[], object[]> transformParameters = null) where T : class
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
            return (T)constructorInfo.OriginConstructor.Invoke(parameters);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create {typeof(T).Name} '{displayName}': {ex.Message}", ex);
        }
    }

    public virtual SchemaMethodInfo[] GetRawConstructors(RuntimeContext runtimeContext)
    {
        runtimeContext.EndWorkToken.ThrowIfCancellationRequested();

        return ConstructorsMethods
            .Where(cm => cm.MethodName.Contains(TablePart))
            .Select(cm =>
            {
                var index = cm.MethodName.IndexOf(TablePart, StringComparison.Ordinal);
                var rawMethodName = cm.MethodName[..index];
                return new SchemaMethodInfo(rawMethodName, cm.ConstructorInfo);
            }).ToArray();
    }

    public virtual SchemaMethodInfo[] GetRawConstructors(string methodName, RuntimeContext runtimeContext)
    {
        return GetRawConstructors(runtimeContext).Where(constr => constr.MethodName == methodName).ToArray();
    }

    public bool TryResolveAggregationMethod(string method, Type[] parameters, Type entityType,
        out MethodInfo methodInfo)
    {
        var found = _aggregator.TryResolveMethod(method, parameters, entityType, out methodInfo);

        if (found)
            return methodInfo.GetCustomAttribute<AggregationMethodAttribute>() != null;

        return false;
    }

    public bool TryResolveWindowFunction(string method, out MethodInfo methodInfo)
    {
        return _aggregator.TryResolveWindowFunction(method, out methodInfo);
    }

    public bool TryResolveMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo)
    {
        return _aggregator.TryResolveMethod(method, parameters, entityType, out methodInfo);
    }

    public bool TryResolveRawMethod(string method, Type[] parameters, out MethodInfo methodInfo)
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

        AddToConstructors<TType>($"{name.ToLowerInvariant()}{TablePart}");
    }

    public void AddSource<TType>(string name, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "adding a source");

        var sourceName = $"{name.ToLowerInvariant()}{SourcePart}";
        AddToConstructors<TType>(sourceName);
        AdditionalArguments.Add(sourceName, args ?? []);
    }

    public SchemaMethodInfo[] GetConstructors(string methodName)
    {
        return GetConstructors().Where(constr => constr.MethodName == methodName).ToArray();
    }

    public virtual SchemaMethodInfo[] GetConstructors()
    {
        return ConstructorsMethods.ToArray();
    }

    private bool ParamsMatchConstructor(ConstructorInfo constructor, object[] parameters)
    {
        var matchingResult = true;

        if (parameters.Length != constructor.Arguments.Length)
            return false;

        for (var i = 0; i < parameters.Length && matchingResult; ++i)
            matchingResult &=
                constructor.Arguments[i].Type.IsInstanceOfType(parameters[i]);

        return matchingResult;
    }

    private bool TryMatchConstructorWithParams(ConstructorInfo[] constructors, object[] parameters,
        out ConstructorInfo foundedConstructor)
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
            .Where(cm => cm.MethodName.Contains(suffix))
            .Select(cm => cm.MethodName.Replace(suffix, string.Empty))
            .Distinct()
            .ToArray();

        return names.Length == 0 ? noItemsMessage : string.Join(", ", names);
    }

    private static string GetMethodSignature(ConstructorInfo constructorInfo)
    {
        var parameters = constructorInfo.OriginConstructor.GetParameters();
        var paramTypes = parameters.Select(p => p.ParameterType.Name).ToArray();
        return $"({string.Join(", ", paramTypes)})";
    }
}
