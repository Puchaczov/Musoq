using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Schema.DataSources;

public abstract class SchemaBase : ISchema
{
    private const string SourcePart = "_source";
    private const string TablePart = "_table";

    private readonly MethodsAggregator _aggregator;

    private IDictionary<string, Reflection.ConstructorInfo[]> Constructors { get; } = new Dictionary<string, Reflection.ConstructorInfo[]>();
    private List<SchemaMethodInfo> ConstructorsMethods { get; } = [];
    private IDictionary<string, object[]> AdditionalArguments { get; } = new Dictionary<string, object[]>();

    protected SchemaBase(string name, MethodsAggregator methodsAggregator)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "initializing a schema");
        
        Name = name;
        _aggregator = methodsAggregator ?? throw SchemaArgumentException.ForNullArgument(nameof(methodsAggregator), "initializing a schema");

        AddSource<SingleRowSource>("empty");
        AddTable<SingleRowSchemaTable>("empty");
    }

    public string Name { get; }

    public void AddSource<TType>(string name, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "adding a source");

        var sourceName = $"{name.ToLowerInvariant()}{SourcePart}";
        AddToConstructors<TType>(sourceName);
        AdditionalArguments.Add(sourceName, args ?? []);
    }

    public void AddTable<TType>(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "adding a table");

        AddToConstructors<TType>($"{name.ToLowerInvariant()}{TablePart}");
    }

    public virtual ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "getting a table by name");

        if (runtimeContext == null)
            throw SchemaArgumentException.ForNullArgument(nameof(runtimeContext), "getting a table by name");

        var tableName = $"{name.ToLowerInvariant()}{TablePart}";
        var methods = GetConstructors(tableName).Select(c => c.ConstructorInfo).ToArray();

        if (methods.Length == 0)
        {
            var availableTables = GetAvailableTableNames();
            throw SchemaArgumentException.ForInvalidMethodName(name, availableTables);
        }

        if (!TryMatchConstructorWithParams(methods, parameters ?? [], out var constructorInfo))
        {
            var availableSignatures = methods.Select(m => GetMethodSignature(m)).ToArray();
            var providedTypes = parameters?.Select(p => p?.GetType().Name ?? "null").ToArray() ?? [];
            throw MethodResolutionException.ForUnresolvedMethod(name, providedTypes, availableSignatures);
        }

        try
        {
            return (ISchemaTable)constructorInfo.OriginConstructor.Invoke(parameters ?? []);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create table '{name}': {ex.Message}", ex);
        }
    }

    public virtual RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "getting a row source");

        if (runtimeContext == null)
            throw SchemaArgumentException.ForNullArgument(nameof(runtimeContext), "getting a row source");

        var sourceName = $"{name.ToLowerInvariant()}{SourcePart}";
        var methods = GetConstructors(sourceName).Select(c => c.ConstructorInfo).ToArray();

        if (methods.Length == 0)
        {
            var availableSources = GetAvailableSourceNames();
            throw SchemaArgumentException.ForInvalidMethodName(name, availableSources);
        }

        if (AdditionalArguments.TryGetValue(sourceName, out var argument))
            parameters = parameters.ExpandParameters(argument);

        if (!TryMatchConstructorWithParams(methods, parameters ?? [], out var constructorInfo))
        {
            var availableSignatures = methods.Select(m => GetMethodSignature(m)).ToArray();
            var providedTypes = parameters?.Select(p => p?.GetType().Name ?? "null").ToArray() ?? [];
            throw MethodResolutionException.ForUnresolvedMethod(name, providedTypes, availableSignatures);
        }

        if (constructorInfo.SupportsInterCommunicator)
            parameters = parameters.ExpandParameters(runtimeContext);

        try
        {
            return (RowSource)constructorInfo.OriginConstructor.Invoke(parameters ?? []);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create row source '{name}': {ex.Message}", ex);
        }
    }

    public SchemaMethodInfo[] GetConstructors(string methodName)
    {
        return GetConstructors().Where(constr => constr.MethodName == methodName).ToArray();
    }

    public virtual SchemaMethodInfo[] GetConstructors()
    {
        return ConstructorsMethods.ToArray();
    }

    public SchemaMethodInfo[] GetRawConstructors()
    {
        return ConstructorsMethods
            .Where(cm => cm.MethodName.Contains(TablePart))
            .Select(cm => {
                var index = cm.MethodName.IndexOf(TablePart, StringComparison.Ordinal);
                var rawMethodName = cm.MethodName[..index];
                return new SchemaMethodInfo(rawMethodName, cm.ConstructorInfo);
            }).ToArray();
    }

    public SchemaMethodInfo[] GetRawConstructors(string methodName)
    {
        return GetRawConstructors().Where(constr => constr.MethodName == methodName).ToArray();
    }

    public bool TryResolveAggregationMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo)
    {
        var found = _aggregator.TryResolveMethod(method, parameters, entityType, out methodInfo);

        if (found)
            return methodInfo.GetCustomAttribute<AggregationMethodAttribute>() != null;

        return false;
    }

    public bool TryResolveMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo)
    {
        return _aggregator.TryResolveMethod(method, parameters, entityType, out methodInfo);
    }

    public bool TryResolveRawMethod(string method, Type[] parameters, out MethodInfo methodInfo)
    {
        return _aggregator.TryResolveRawMethod(method, parameters, out methodInfo);
    }

    private bool ParamsMatchConstructor(Reflection.ConstructorInfo constructor, object[] parameters)
    {
        var matchingResult = true;

        if (parameters.Length != constructor.Arguments.Length)
            return false;

        for (var i = 0; i < parameters.Length && matchingResult; ++i)
        {
            matchingResult &= 
                constructor.Arguments[i].Type.IsInstanceOfType(parameters[i]);
        }

        return matchingResult;
    }

    private bool TryMatchConstructorWithParams(Reflection.ConstructorInfo[] constructors, object[] parameters, out Reflection.ConstructorInfo foundedConstructor)
    {
        foreach(var constructor in constructors)
        {
            if(ParamsMatchConstructor(constructor, parameters))
            {
                foundedConstructor = constructor;
                return true;
            }
        }

        foundedConstructor = null;
        return false;
    }

    private void AddToConstructors<TType>(string name)
    {
        var schemaMethodInfos = TypeHelper
            .GetSchemaMethodInfosForType<TType>(name);

        ConstructorsMethods.AddRange(schemaMethodInfos);

        var schemaMethods = schemaMethodInfos
            .Select(schemaMethod => schemaMethod.ConstructorInfo)
            .ToArray();

        Constructors.Add(name, schemaMethods);
    }

    private string GetAvailableTableNames()
    {
        var tableNames = ConstructorsMethods
            .Where(cm => cm.MethodName.Contains(TablePart))
            .Select(cm => cm.MethodName.Replace(TablePart, ""))
            .Distinct()
            .ToArray();

        return tableNames.Length == 0 ? "No tables available" : string.Join(", ", tableNames);
    }

    private string GetAvailableSourceNames()
    {
        var sourceNames = ConstructorsMethods
            .Where(cm => cm.MethodName.Contains(SourcePart))
            .Select(cm => cm.MethodName.Replace(SourcePart, ""))
            .Distinct()
            .ToArray();

        return sourceNames.Length == 0 ? "No sources available" : string.Join(", ", sourceNames);
    }

    private static string GetMethodSignature(Reflection.ConstructorInfo constructorInfo)
    {
        var parameters = constructorInfo.OriginConstructor.GetParameters();
        var paramTypes = parameters.Select(p => p.ParameterType.Name).ToArray();
        return $"({string.Join(", ", paramTypes)})";
    }
}