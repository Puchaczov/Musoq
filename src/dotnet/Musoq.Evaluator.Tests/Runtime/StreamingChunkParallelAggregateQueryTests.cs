using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class StreamingChunkParallelAggregateQueryTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    static StreamingChunkParallelAggregateQueryTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void StreamingRowSource_WhenParallelGroupedAggregateRuns_ShouldNotMaterializeAllChunksBeforeAggregating()
    {
        const string query = "select City, Count(Signal(City)) from #schema.rows() group by City";
        var compilationOptions = new CompilationOptions(
            ParallelizationMode.Full,
            usePrimitiveTypeValidation: false,
            maxDegreeOfParallelismOverride: 2,
            forceTableResultMaterialization: true);
        var probe = new AggregateStreamingProbe();
        var rowSource = new BlockingSecondChunkRowSource(probe);
        var schema = new GenericSchema<StreamingAggregateLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "rows", (new GenericEntityTable<StreamingAggregateEntity>(), rowSource) }
            });
        var schemaProvider = new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            { "#schema", schema }
        });

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            compilationOptions);

        Assert.Contains("Parallel.ForEach<IReadOnlyList<", inspection.GeneratedCSharpCode);
        Assert.Contains("ParallelSingleKeyAggregateChunkWorker_", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("EvaluationHelper.GetParallelAggregationRowsOrEmpty", inspection.GeneratedCSharpCode);

        StreamingAggregateLibrary.Probe = probe;
        try
        {
            var vm = InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                _loggerResolver,
                compilationOptions);

            var table = vm.Run(CancellationToken.None);
            var rows = table.ToDictionary(static row => (string)row[0], static row => Convert.ToInt64(row[1]));

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(2L, rows["A"]);
            Assert.AreEqual(1L, rows["B"]);
            Assert.IsTrue(probe.FirstAggregateInputObserved);
            Assert.AreEqual(2, rowSource.ChunksWritten);
        }
        finally
        {
            StreamingAggregateLibrary.Probe = null;
        }
    }

    public sealed record StreamingAggregateEntity(string City);

    private sealed class BlockingSecondChunkRowSource(AggregateStreamingProbe probe)
        : RowSourceBase<StreamingAggregateEntity>
    {
        public int ChunksWritten { get; private set; }

        protected override void CollectChunks(IChunkWriter<StreamingAggregateEntity> writer)
        {
            writer.Write(
            [
                new StreamingAggregateEntity("A"),
                new StreamingAggregateEntity("A")
            ]);
            ChunksWritten++;

            if (!probe.WaitForFirstAggregateInput(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("The aggregate loop did not process the first chunk before the source needed the second chunk.");

            writer.Write([new StreamingAggregateEntity("B")]);
            ChunksWritten++;
        }
    }

    public sealed class StreamingAggregateLibrary : LibraryBase
    {
        public static AggregateStreamingProbe? Probe { get; set; }

        [BindableMethod]
        public string Signal(string value)
        {
            Probe?.Signal();
            return value;
        }
    }

    public sealed class AggregateStreamingProbe
    {
        private readonly ManualResetEventSlim _firstAggregateInputObserved = new(false);

        public bool FirstAggregateInputObserved => _firstAggregateInputObserved.IsSet;

        public void Signal()
        {
            _firstAggregateInputObserved.Set();
        }

        public bool WaitForFirstAggregateInput(TimeSpan timeout)
        {
            return _firstAggregateInputObserved.Wait(timeout);
        }
    }
}
