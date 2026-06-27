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
public sealed class StreamingChunkParallelProjectionQueryTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    static StreamingChunkParallelProjectionQueryTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void StreamingRowSource_WhenParallelized_ShouldProjectChunksConcurrentlyAndKeepOrder()
    {
        const string query = "select Synchronize(Value) from #schema.rows()";
        var compilationOptions = new CompilationOptions(
            ParallelizationMode.Full,
            usePrimitiveTypeValidation: false,
            maxDegreeOfParallelismOverride: 2,
            forceTableResultMaterialization: true);
        var probe = new ProjectionConcurrencyProbe();
        var rows = Enumerable.Range(0, 4_096).Select(value => new StreamingProjectionEntity(value)).ToArray();
        var rowSource = new StreamingProjectionRowSource(rows, chunkSize: 2_048);
        var schema = new GenericSchema<StreamingProjectionLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "rows", (new GenericEntityTable<StreamingProjectionEntity>(), rowSource) }
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

        Assert.Contains(
            "EvaluationHelper.ProjectChunkedRowsParallel<",
            inspection.GeneratedCSharpCode);

        StreamingProjectionLibrary.Probe = probe;

        try
        {
            var vm = InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                _loggerResolver,
                compilationOptions);

            var table = vm.Run(CancellationToken.None);

            Assert.AreEqual(rows.Length, table.Count);
            CollectionAssert.AreEqual(
                rows.Select(row => row.Value).Cast<object>().ToArray(),
                table.Select(row => row[0]).ToArray());
            Assert.IsTrue(
                probe.MaxConcurrentCalls >= 2,
                $"Expected chunk projection to overlap work, but max concurrency was {probe.MaxConcurrentCalls}.");
        }
        finally
        {
            StreamingProjectionLibrary.Probe = null;
        }
    }

    public sealed record StreamingProjectionEntity(int Value);

    private sealed class StreamingProjectionRowSource(IReadOnlyList<StreamingProjectionEntity> rows, int chunkSize)
        : RowSourceBase<StreamingProjectionEntity>
    {
        protected override void CollectChunks(IChunkWriter<StreamingProjectionEntity> writer)
        {
            for (var index = 0; index < rows.Count; index += chunkSize)
            {
                var count = Math.Min(chunkSize, rows.Count - index);
                var chunk = new StreamingProjectionEntity[count];
                for (var chunkIndex = 0; chunkIndex < count; chunkIndex++)
                    chunk[chunkIndex] = rows[index + chunkIndex];

                writer.Write(chunk);
            }
        }
    }

    public sealed class StreamingProjectionLibrary : LibraryBase
    {
        public static ProjectionConcurrencyProbe? Probe { get; set; }

        [BindableMethod]
        public int Synchronize(int value)
        {
            return Probe?.Synchronize(value) ?? value;
        }
    }

    public sealed class ProjectionConcurrencyProbe
    {
        private readonly ManualResetEventSlim _twoCallsStarted = new(false);
        private int _currentCalls;
        private int _maxConcurrentCalls;

        public int MaxConcurrentCalls => Volatile.Read(ref _maxConcurrentCalls);

        public int Synchronize(int value)
        {
            if (value is not (0 or 2_048))
                return value;

            var current = Interlocked.Increment(ref _currentCalls);
            UpdateMax(current);

            if (current >= 2)
                _twoCallsStarted.Set();
            else
                if (!_twoCallsStarted.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Expected two chunk workers to run concurrently.");

            Thread.Sleep(10);
            Interlocked.Decrement(ref _currentCalls);
            return value;
        }

        private void UpdateMax(int current)
        {
            while (true)
            {
                var observed = Volatile.Read(ref _maxConcurrentCalls);
                if (observed >= current)
                    return;

                if (Interlocked.CompareExchange(ref _maxConcurrentCalls, current, observed) == observed)
                    return;
            }
        }
    }
}
