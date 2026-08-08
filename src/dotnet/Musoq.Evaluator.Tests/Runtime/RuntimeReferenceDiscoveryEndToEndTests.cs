using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Microsoft.CodeAnalysis;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class RuntimeReferenceDiscoveryEndToEndTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    [TestMethod]
    [DataRow("select ProcessName, ProcessorAffinity from os.processes()")]
    [DataRow("select ProcessName, ProcessorAffinity from #os.processes()")]
    [DataRow("select ProcessName from #os.processes()")]
    public void ProcessRowType_ReferencedByGeneratedSource_CompilesAndRunsWithOnlyProcessReference(string query)
    {
        var compilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false);
        var schemaProvider = CreateProcessSchemaProvider();

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            compilationOptions);

        StringAssert.Contains(
            inspection.GeneratedCSharpCode,
            "GetRowSource<System.Diagnostics.Process>");

        using var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            compilationOptions);

        using var table = compiled.Run();
        Assert.AreEqual(0, table.Count);

        var columns = table.Columns.ToArray();
        var expectedColumns = query.Contains("ProcessorAffinity", StringComparison.Ordinal)
            ? new[]
            {
                (nameof(Process.ProcessName), typeof(string)),
                ("ProcessorAffinity", typeof(IntPtr))
            }
            : new[] { (nameof(Process.ProcessName), typeof(string)) };

        Assert.AreEqual(expectedColumns.Length, columns.Length);
        for (var index = 0; index < expectedColumns.Length; index++)
        {
            Assert.AreEqual(expectedColumns[index].Item1, columns[index].ColumnName);
            Assert.AreEqual(expectedColumns[index].Item2, columns[index].ColumnType);
        }

        var analyzed = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            compilationOptions);
        var referenceFileNames = analyzed.RenderingArtifacts.Compilation.References
            .OfType<PortableExecutableReference>()
            .Select(reference => Path.GetFileName(reference.FilePath))
            .Where(static name => name is not null)
            .Select(static name => name!)
            .ToArray();

        CollectionAssert.Contains(referenceFileNames, "System.Diagnostics.Process.dll");
        Assert.AreEqual(1, referenceFileNames.Count(
            static name => string.Equals(name, "System.Diagnostics.Process.dll", StringComparison.OrdinalIgnoreCase)));
        CollectionAssert.DoesNotContain(referenceFileNames, "System.Xml.dll");
        CollectionAssert.DoesNotContain(referenceFileNames, "System.Net.Http.dll");

        var primitiveBaseline = InstanceCreator.CreateForAnalyze(
            "select 1 from #primitive.values()",
            Guid.NewGuid().ToString(),
            CreatePrimitiveSchemaProvider(),
            _loggerResolver,
            compilationOptions);
        var baselineReferenceFileNames = GetReferenceFileNames(primitiveBaseline);
        var planReferenceDelta = referenceFileNames
            .Except(baselineReferenceFileNames, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        CollectionAssert.AreEqual(new[] { "System.Diagnostics.Process.dll" }, planReferenceDelta);

        var repeated = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            compilationOptions);
        var firstProcessReference = GetProcessReference(analyzed);
        var repeatedProcessReference = GetProcessReference(repeated);
        Assert.AreSame(firstProcessReference, repeatedProcessReference);
    }

    private static string[] GetReferenceFileNames(Musoq.Converter.Build.BuildItems items)
    {
        return items.RenderingArtifacts.Compilation.References
            .OfType<PortableExecutableReference>()
            .Select(reference => Path.GetFileName(reference.FilePath))
            .Where(static name => name is not null)
            .Select(static name => name!)
            .ToArray();
    }

    private static PortableExecutableReference GetProcessReference(Musoq.Converter.Build.BuildItems items)
    {
        return items.RenderingArtifacts.Compilation.References
                   .OfType<PortableExecutableReference>()
                   .Single(reference => string.Equals(
                       Path.GetFileName(reference.FilePath),
                       "System.Diagnostics.Process.dll",
                       StringComparison.OrdinalIgnoreCase));
    }

    private static ISchemaProvider CreateProcessSchemaProvider()
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "processes", (new ProcessTable(), new EmptyProcessRowSource()) }
            });

        return new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            { "os", schema },
            { "#os", schema }
        });
    }

    private static ISchemaProvider CreatePrimitiveSchemaProvider()
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "values", (new PrimitiveTable(), new EmptyPrimitiveRowSource()) }
            });

        return new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            { "#primitive", schema }
        });
    }

    private sealed class ProcessTable : ISchemaTable
    {
        private const string ProcessorAffinityColumnName = "ProcessorAffinity";

        public ISchemaColumn[] Columns { get; } =
        [
            new Musoq.Schema.DataSources.SchemaColumn(nameof(Process.ProcessName), 0, typeof(string)),
            new Musoq.Schema.DataSources.SchemaColumn(ProcessorAffinityColumnName, 1, typeof(IntPtr))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(Process));

        public ISchemaColumn? GetColumnByName(string name)
        {
            if (string.Equals(name, nameof(Process.ProcessName), StringComparison.OrdinalIgnoreCase))
                return Columns[0];

            return string.Equals(name, ProcessorAffinityColumnName, StringComparison.OrdinalIgnoreCase)
                ? Columns[1]
                : null;
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            var column = GetColumnByName(name);
            return column is null ? [] : [column];
        }
    }

    private sealed class EmptyProcessRowSource : RowSourceBase<Process>
    {
        protected override void CollectChunks(IChunkWriter<Process> writer)
        {
        }
    }

    private sealed class PrimitiveTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new Musoq.Schema.DataSources.SchemaColumn("Value", 0, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(int));

        public ISchemaColumn? GetColumnByName(string name) =>
            string.Equals(name, "Value", StringComparison.OrdinalIgnoreCase) ? Columns[0] : null;

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            var column = GetColumnByName(name);
            return column is null ? [] : [column];
        }
    }

    private sealed class EmptyPrimitiveRowSource : RowSourceBase<int>
    {
        protected override void CollectChunks(IChunkWriter<int> writer)
        {
        }
    }
}
