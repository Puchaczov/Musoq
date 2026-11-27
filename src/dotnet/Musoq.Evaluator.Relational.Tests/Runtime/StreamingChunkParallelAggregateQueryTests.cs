using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class StreamingChunkParallelAggregateQueryTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    [TestMethod]
    public void StreamingRowSource_WhenParallelGroupedDistinctAggregateRuns_ShouldMergeValuesAcrossChunks()
    {
        const string query = "select City, Count(distinct Signal(Name)) from #schema.rows() group by City";
        var compilationOptions = new CompilationOptions(
            ParallelizationMode.Full,
            usePrimitiveTypeValidation: false,
            maxDegreeOfParallelismOverride: 2,
            forceTableResultMaterialization: true
        );
        using var watchdog = new TestWatchdog();
        var probe = new AggregateStreamingProbe(watchdog);
        var rowSource = new BlockingSecondChunkRowSource(probe);
        var schema = new GenericSchema<StreamingAggregateLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "rows", (new GenericEntityTable<StreamingAggregateEntity>(), rowSource) },
            },
            executionPermissions: SourceExecutionPermissions.ConcurrentRowProcessing
        );
        var schemaProvider = new GenericSchemaProvider(new Dictionary<string, ISchema> { { "#schema", schema } });

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            compilationOptions
        );

        Assert.Contains("Parallel.ForEach<IReadOnlyList<", inspection.GeneratedCSharpCode);
        Assert.Contains("ParallelSingleKeyAggregateChunkWorker_", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("EvaluationHelper.GetParallelAggregationRowsOrEmpty", inspection.GeneratedCSharpCode);

        StreamingAggregateLibrary.Probe = probe;
        try
        {
            using var vm = InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                _loggerResolver,
                compilationOptions
            );

            using var table = vm.Run(watchdog.Token);
            var rows = table.ToDictionary(
                static row => (string)row.ReadValue(0),
                static row => Convert.ToInt64(row.ReadValue(1))
            );

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

    public sealed record StreamingAggregateEntity(string City, string Name);

    private sealed class BlockingSecondChunkRowSource(AggregateStreamingProbe probe)
        : RowSourceBase<StreamingAggregateEntity>
    {
        public int ChunksWritten { get; private set; }

        protected override void CollectChunks(IChunkWriter<StreamingAggregateEntity> writer)
        {
            writer.Write([new StreamingAggregateEntity("A", "shared"), new StreamingAggregateEntity("A", "first")]);
            ChunksWritten++;

            probe.WaitForFirstAggregateInput();

            writer.Write([new StreamingAggregateEntity("A", "shared"), new StreamingAggregateEntity("B", "only")]);
            ChunksWritten++;
        }
    }

    public sealed class StreamingAggregateLibrary : LibraryBase
    {
        public static AggregateStreamingProbe? Probe { get; set; }

        [BindableMethod(ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations)]
        public string Signal(string value)
        {
            Probe?.Signal();
            return value;
        }
    }

    public sealed class AggregateStreamingProbe(TestWatchdog watchdog)
    {
        private readonly TaskCompletionSource _firstAggregateInputObserved = TestWatchdog.Signal();

        public bool FirstAggregateInputObserved => _firstAggregateInputObserved.Task.IsCompleted;

        public void Signal()
        {
            _firstAggregateInputObserved.TrySetResult();
        }

        public void WaitForFirstAggregateInput() =>
            _firstAggregateInputObserved.Task.WaitAsync(watchdog.Token).GetAwaiter().GetResult();
    }
}
