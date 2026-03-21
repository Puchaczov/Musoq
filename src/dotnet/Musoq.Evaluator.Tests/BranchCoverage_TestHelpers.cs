using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.Api;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region Entity Types

    public class SimpleEntity
    {
        public string Name { get; set; }

        public string City { get; set; }
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

    private sealed class TestSchemaProvider : ISchemaProvider
    {
        private readonly string _schemaName;
        private readonly ISchema _schema;

        public TestSchemaProvider(string schemaName, ISchema schema = null)
        {
            _schemaName = schemaName;
            _schema = schema;
        }

        public ISchema GetSchema(string schema)
        {
            return _schema ?? throw new InvalidOperationException($"Schema '{schema}' not found");
        }
    }

    private sealed class TestSchema : ISchema
    {
        public TestSchema(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters) =>
            throw new NotImplementedException();

        public RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters) =>
            throw new NotImplementedException();

        public SchemaMethodInfo[] GetRawConstructors(RuntimeContext runtimeContext) =>
            throw new NotImplementedException();

        public SchemaMethodInfo[] GetRawConstructors(string methodName, RuntimeContext runtimeContext) =>
            throw new NotImplementedException();

        public bool TryResolveMethod(string method, Type[] parameters, Type entityType, out System.Reflection.MethodInfo methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveRawMethod(string method, Type[] parameters, out System.Reflection.MethodInfo methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveAggregationMethod(string method, Type[] parameters, Type entityType, out System.Reflection.MethodInfo methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveWindowFunction(string method, out System.Reflection.MethodInfo methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public IReadOnlyDictionary<string, IReadOnlyList<System.Reflection.MethodInfo>> GetAllLibraryMethods() =>
            new Dictionary<string, IReadOnlyList<System.Reflection.MethodInfo>>();
    }

    #endregion
}
