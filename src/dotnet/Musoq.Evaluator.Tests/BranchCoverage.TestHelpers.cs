using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region Entity Types

    public class SimpleEntity
    {
        public string Name { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
    }

    #endregion

    #region Test Helpers

    private sealed class TestIndexedList : IndexedList<Key, Row>
    {
        public void AddRowWithIndex(Key key, Row row)
        {
            Rows.Add(row);
            var rowIndex = Rows.Count - 1;

            if (!Indexes.TryGetValue(key, out var indices))
            {
                indices = [];
                Indexes[key] = indices;
            }

            indices.Add(rowIndex);
        }
    }

    private sealed class TestSchemaProvider(ISchema? schema = null) : ISchemaProvider
    {
        public ISchema GetSchema(string schema1)
        {
            return schema ?? throw new InvalidOperationException($"Schema '{schema1}' not found");
        }
    }

    private sealed class TestSchema(string name) : ISchema
    {
        public string Name { get; } = name;

        public ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters) =>
            throw new NotImplementedException();

        public SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters) =>
            throw new NotImplementedException();

        public IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
            string name,
            SourceRuntimeSettingsDescribeContext context,
            params object?[] parameters) =>
            [];

        public SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters) =>
            SourcePlanResult.RejectAll(request);

        public RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters) =>
            throw new NotImplementedException();

        public SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) =>
            throw new NotImplementedException();

        public SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext) =>
            throw new NotImplementedException();

        public bool TryResolveMethod(string method, Type[] parameters, Type? entityType, [NotNullWhen(true)] out System.Reflection.MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveRawMethod(string method, Type[] parameters, [NotNullWhen(true)] out System.Reflection.MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveAggregationMethod(string method, Type[] parameters, Type? entityType, [NotNullWhen(true)] out System.Reflection.MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveAggregationMethod(
            string method,
            Type[] parameters,
            Type? entityType,
            Func<System.Reflection.MethodInfo, bool> methodFilter,
            [NotNullWhen(true)] out System.Reflection.MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveWindowFunction(string method, [NotNullWhen(true)] out System.Reflection.MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public IReadOnlyDictionary<string, IReadOnlyList<System.Reflection.MethodInfo>> GetAllLibraryMethods() =>
            new Dictionary<string, IReadOnlyList<System.Reflection.MethodInfo>>();
    }

    #endregion
}
