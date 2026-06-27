using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;

namespace Musoq.Schema;

public interface ISchema
{
    string Name { get; }

    ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters);

    SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters);

    IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
        string name,
        SourceRuntimeSettingsDescribeContext context,
        params object?[] parameters);

    SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters);

    RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters);

    SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext);

    SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext);

    bool TryResolveMethod(string method, Type[] parameters, Type? entityType, [NotNullWhen(true)] out MethodInfo? methodInfo);

    bool TryResolveRawMethod(string method, Type[] parameters, [NotNullWhen(true)] out MethodInfo? methodInfo);

    bool TryResolveAggregationMethod(string method, Type[] parameters, Type? entityType, [NotNullWhen(true)] out MethodInfo? methodInfo);

    bool TryResolveAggregationMethod(
        string method,
        Type[] parameters,
        Type? entityType,
        Func<MethodInfo, bool> methodFilter,
        [NotNullWhen(true)] out MethodInfo? methodInfo);

    bool TryResolveWindowFunction(string method, [NotNullWhen(true)] out MethodInfo? methodInfo);

    IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> GetAllLibraryMethods();
}
