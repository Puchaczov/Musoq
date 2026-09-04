// === Parsed Query ===
/*
select Name, Ntile(2) over ranked as Bucket, FirstValue(Name) over ranked as FirstName, LastValue(Name) over ranked as LastName, NthValue(Name, 1) over ranked as NthName, Min(Population) over ranked as MinPopulation, Max(Population) over ranked as MaxPopulation from #A.entities() window ranked as (order by Name)
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Name as Name, WindowRef(0) as Bucket, WindowRef(1) as FirstName, WindowRef(2) as LastName, WindowRef(3) as NthName, WindowRef(4) as MinPopulation, WindowRef(5) as MaxPopulation]
    Window [Ntile(idx:0; order: ko3iko.Name; args: 2), FirstValue(idx:1; order: ko3iko.Name; args: ko3iko.Name), LastValue(idx:2; order: ko3iko.Name; args: ko3iko.Name), NthValue(idx:3; order: ko3iko.Name; args: ko3iko.Name, 1), Min(idx:4; order: ko3iko.Name; args: ko3iko.Population), Max(idx:5; order: ko3iko.Name; args: ko3iko.Population)]
      SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as Name, WindowRef(0) as Bucket, WindowRef(1) as FirstName, WindowRef(2) as LastName, WindowRef(3) as NthName, WindowRef(4) as MinPopulation, WindowRef(5) as MaxPopulation]
    PhysicalWindow [Ntile(idx:0; order: ko3iko.Name; args: 2), FirstValue(idx:1; order: ko3iko.Name; args: ko3iko.Name), LastValue(idx:2; order: ko3iko.Name; args: ko3iko.Name), NthValue(idx:3; order: ko3iko.Name; args: ko3iko.Name, 1), Min(idx:4; order: ko3iko.Name; args: ko3iko.Population), Max(idx:5; order: ko3iko.Name; args: ko3iko.Population)]
      PhysicalMaterialize
        PhysicalSchemaScan [#A.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
    Generated [ResultRow0]
      Name: string <- field Name
      Bucket: long <- field Bucket
      FirstName: string <- field FirstName
      LastName: string <- field LastName
      NthName: string <- field NthName
      MinPopulation: decimal? <- field MinPopulation
      MaxPopulation: decimal? <- field MaxPopulation

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeNtileWindow [resultNtiles0 <- resultWindowRows value 2 order by ko3iko.Name ASC]
    ComputeFirstValueWindow [resultFirstValues1 <- resultWindowRows value ko3iko.Name order by ko3iko.Name ASC frame range between unbounded preceding and current row]
    ComputeLastValueWindow [resultLastValues2 <- resultWindowRows value ko3iko.Name order by ko3iko.Name ASC frame range between unbounded preceding and current row]
    ComputeNthValueWindow [resultNthValues3 <- resultWindowRows value ko3iko.Name order by ko3iko.Name ASC frame range between unbounded preceding and current row args 1]
    ComputeMinWindowKernel[BoundedRows] [resultMins4 <- resultWindowRows value ko3iko.Population order by ko3iko.Name ASC frame range between unbounded preceding and current row]
    ComputeMaxWindowKernel[BoundedRows] [resultMaxs5 <- resultWindowRows value ko3iko.Population order by ko3iko.Name ASC frame range between unbounded preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Name: ko3iko.Name, Bucket: resultNtiles0[windowIndex], FirstName: resultFirstValues1[windowIndex], LastName: resultLastValues2[windowIndex], NthName: resultNthValues3[windowIndex], MinPopulation: resultMins4[windowIndex], MaxPopulation: resultMaxs5[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q290_SpecCoreNamedWindowAnalytics
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Bucket", typeof(long), 1),
            new Column("FirstName", typeof(string), 2),
            new Column("LastName", typeof(string), 3),
            new Column("NthName", typeof(string), 4),
            new Column("MinPopulation", typeof(decimal?), 5),
            new Column("MaxPopulation", typeof(decimal?), 6)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Population", typeof(decimal), 1) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

        public event DataSourceEventHandler DataSourceProgress;
        public event QueryPhaseEventHandler PhaseChanged;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Bucket, __musoqShapeRow.FirstName, __musoqShapeRow.LastName, __musoqShapeRow.NthName, __musoqShapeRow.MinPopulation, __musoqShapeRow.MaxPopulation);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultNtiles0OrderKeys = new WindowResultNtiles0OrderKeysKey[resultWindowRows.Count];
                var resultNtiles0Buckets = new int[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    resultNtiles0OrderKeys[windowIndex] = new WindowResultNtiles0OrderKeysKey(ko3iko.Name);
                    resultNtiles0Buckets[windowIndex] = (int)2;
                }

                var resultNtiles0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, null);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultNtiles0Partitions, resultNtiles0OrderKeys, false);
                var resultNtiles0 = new long[resultWindowRows.Count];
                for (int resultNtiles0PartitionSetIndex = 0; resultNtiles0PartitionSetIndex < resultNtiles0Partitions.PartitionCount; ++resultNtiles0PartitionSetIndex)
                {
                    var resultNtiles0PartitionStart = resultNtiles0Partitions.GetStart(resultNtiles0PartitionSetIndex);
                    var resultNtiles0PartitionCount = resultNtiles0Partitions.GetLength(resultNtiles0PartitionSetIndex);
                    var resultNtiles0PartitionIndices = resultNtiles0Partitions.Indices;
                    var resultNtiles0BucketCount = 0;
                    for (int resultNtiles0PartitionIndex = 0; resultNtiles0PartitionIndex < resultNtiles0PartitionCount; ++resultNtiles0PartitionIndex)
                    {
                        var resultNtiles0CurrentIndex = resultNtiles0PartitionIndices[resultNtiles0PartitionStart + resultNtiles0PartitionIndex];
                        if (resultNtiles0BucketCount == 0)
                            resultNtiles0BucketCount = resultNtiles0Buckets[resultNtiles0CurrentIndex];
                        var resultNtiles0Position = resultNtiles0PartitionIndex + 1;
                        if (resultNtiles0BucketCount <= 0)
                        {
                            resultNtiles0[resultNtiles0CurrentIndex] = 1L;
                            continue;
                        }

                        var resultNtiles0RowsPerBucket = resultNtiles0PartitionCount / resultNtiles0BucketCount;
                        var resultNtiles0ExtraRows = resultNtiles0PartitionCount % resultNtiles0BucketCount;
                        var resultNtiles0LargeGroupBoundary = resultNtiles0ExtraRows * (resultNtiles0RowsPerBucket + 1);
                        resultNtiles0[resultNtiles0CurrentIndex] = resultNtiles0Position <= resultNtiles0LargeGroupBoundary ? ((resultNtiles0Position - 1) / (resultNtiles0RowsPerBucket + 1)) + 1L : ((resultNtiles0Position - 1 - resultNtiles0LargeGroupBoundary) / resultNtiles0RowsPerBucket) + resultNtiles0ExtraRows + 1L;
                    }
                }

                var resultFirstValues1Values = new string[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    resultFirstValues1Values[windowIndex] = (string)ko3iko.Name;
                }

                var resultFirstValues1 = new string[resultWindowRows.Count];
                for (int resultFirstValues1PartitionSetIndex = 0; resultFirstValues1PartitionSetIndex < resultNtiles0Partitions.PartitionCount; ++resultFirstValues1PartitionSetIndex)
                {
                    var resultFirstValues1PartitionStart = resultNtiles0Partitions.GetStart(resultFirstValues1PartitionSetIndex);
                    var resultFirstValues1PartitionCount = resultNtiles0Partitions.GetLength(resultFirstValues1PartitionSetIndex);
                    var resultFirstValues1PartitionIndices = resultNtiles0Partitions.Indices;
                    for (int resultFirstValues1PartitionIndex = 0; resultFirstValues1PartitionIndex < resultFirstValues1PartitionCount; ++resultFirstValues1PartitionIndex)
                    {
                        var resultFirstValues1CurrentIndex = resultFirstValues1PartitionIndices[resultFirstValues1PartitionStart + resultFirstValues1PartitionIndex];
                        var resultFirstValues1FrameStart = 0;
                        var resultFirstValues1FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultFirstValues1PartitionIndices, resultFirstValues1PartitionStart, resultFirstValues1PartitionCount, resultFirstValues1PartitionIndex);
                        var resultFirstValues1SourcePartitionIndex = resultFirstValues1FrameStart;
                        resultFirstValues1[resultFirstValues1CurrentIndex] = resultFirstValues1FrameStart <= resultFirstValues1FrameEnd ? (string)resultFirstValues1Values[resultFirstValues1PartitionIndices[resultFirstValues1PartitionStart + resultFirstValues1SourcePartitionIndex]] : default(string);
                    }
                }

                var resultLastValues2Values = new string[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    resultLastValues2Values[windowIndex] = (string)ko3iko.Name;
                }

                var resultLastValues2 = new string[resultWindowRows.Count];
                for (int resultLastValues2PartitionSetIndex = 0; resultLastValues2PartitionSetIndex < resultNtiles0Partitions.PartitionCount; ++resultLastValues2PartitionSetIndex)
                {
                    var resultLastValues2PartitionStart = resultNtiles0Partitions.GetStart(resultLastValues2PartitionSetIndex);
                    var resultLastValues2PartitionCount = resultNtiles0Partitions.GetLength(resultLastValues2PartitionSetIndex);
                    var resultLastValues2PartitionIndices = resultNtiles0Partitions.Indices;
                    for (int resultLastValues2PartitionIndex = 0; resultLastValues2PartitionIndex < resultLastValues2PartitionCount; ++resultLastValues2PartitionIndex)
                    {
                        var resultLastValues2CurrentIndex = resultLastValues2PartitionIndices[resultLastValues2PartitionStart + resultLastValues2PartitionIndex];
                        var resultLastValues2FrameStart = 0;
                        var resultLastValues2FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultLastValues2PartitionIndices, resultLastValues2PartitionStart, resultLastValues2PartitionCount, resultLastValues2PartitionIndex);
                        var resultLastValues2SourcePartitionIndex = resultLastValues2FrameEnd;
                        resultLastValues2[resultLastValues2CurrentIndex] = resultLastValues2FrameStart <= resultLastValues2FrameEnd ? (string)resultLastValues2Values[resultLastValues2PartitionIndices[resultLastValues2PartitionStart + resultLastValues2SourcePartitionIndex]] : default(string);
                    }
                }

                var resultNthValues3Values = new string[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    resultNthValues3Values[windowIndex] = (string)ko3iko.Name;
                }

                var resultNthValues3 = new string[resultWindowRows.Count];
                for (int resultNthValues3PartitionSetIndex = 0; resultNthValues3PartitionSetIndex < resultNtiles0Partitions.PartitionCount; ++resultNthValues3PartitionSetIndex)
                {
                    var resultNthValues3PartitionStart = resultNtiles0Partitions.GetStart(resultNthValues3PartitionSetIndex);
                    var resultNthValues3PartitionCount = resultNtiles0Partitions.GetLength(resultNthValues3PartitionSetIndex);
                    var resultNthValues3PartitionIndices = resultNtiles0Partitions.Indices;
                    for (int resultNthValues3PartitionIndex = 0; resultNthValues3PartitionIndex < resultNthValues3PartitionCount; ++resultNthValues3PartitionIndex)
                    {
                        var resultNthValues3CurrentIndex = resultNthValues3PartitionIndices[resultNthValues3PartitionStart + resultNthValues3PartitionIndex];
                        var resultNthValues3FrameStart = 0;
                        var resultNthValues3FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultNthValues3PartitionIndices, resultNthValues3PartitionStart, resultNthValues3PartitionCount, resultNthValues3PartitionIndex);
                        var resultNthValues3Nth = 1;
                        var resultNthValues3SourcePartitionIndex = resultNthValues3FrameStart + resultNthValues3Nth - 1;
                        resultNthValues3[resultNthValues3CurrentIndex] = resultNthValues3Nth > 0 && resultNthValues3SourcePartitionIndex <= resultNthValues3FrameEnd ? (string)resultNthValues3Values[resultNthValues3PartitionIndices[resultNthValues3PartitionStart + resultNthValues3SourcePartitionIndex]] : default(string);
                    }
                }

                var resultMins4 = new decimal? [resultWindowRows.Count];
                for (int resultMins4PartitionSetIndex = 0; resultMins4PartitionSetIndex < resultNtiles0Partitions.PartitionCount; ++resultMins4PartitionSetIndex)
                {
                    var resultMins4PartitionStart = resultNtiles0Partitions.GetStart(resultMins4PartitionSetIndex);
                    var resultMins4PartitionCount = resultNtiles0Partitions.GetLength(resultMins4PartitionSetIndex);
                    var resultMins4PartitionIndices = resultNtiles0Partitions.Indices;
                    var resultMins4DequeValues = System.Buffers.ArrayPool<decimal>.Shared.Rent(resultMins4PartitionCount);
                    var resultMins4DequeIndices = System.Buffers.ArrayPool<int>.Shared.Rent(resultMins4PartitionCount);
                    var resultMins4DequeHead = 0;
                    var resultMins4DequeTail = 0;
                    var resultMins4DequeFrameEnd = -1;
                    for (int resultMins4PartitionIndex = 0; resultMins4PartitionIndex < resultMins4PartitionCount; ++resultMins4PartitionIndex)
                    {
                        var resultMins4CurrentIndex = resultMins4PartitionIndices[resultMins4PartitionStart + resultMins4PartitionIndex];
                        var resultMins4FrameStart = 0;
                        var resultMins4FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultMins4PartitionIndices, resultMins4PartitionStart, resultMins4PartitionCount, resultMins4PartitionIndex);
                        while (resultMins4DequeFrameEnd < resultMins4FrameEnd)
                        {
                            ++resultMins4DequeFrameEnd;
                            var resultMins4FrameValueIndex = resultMins4PartitionIndices[resultMins4PartitionStart + resultMins4DequeFrameEnd];
                            Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultMins4FrameValueIndex];
                            var resultMins4Value = ko3iko.Population;
                            if (true)
                            {
                                while (resultMins4DequeTail > resultMins4DequeHead && resultMins4DequeValues[resultMins4DequeTail - 1].CompareTo(resultMins4Value) >= 0)
                                    --resultMins4DequeTail;
                                resultMins4DequeValues[resultMins4DequeTail] = resultMins4Value;
                                resultMins4DequeIndices[resultMins4DequeTail] = resultMins4DequeFrameEnd;
                                ++resultMins4DequeTail;
                            }
                        }

                        while (resultMins4DequeHead < resultMins4DequeTail && resultMins4DequeIndices[resultMins4DequeHead] < resultMins4FrameStart)
                            ++resultMins4DequeHead;
                        resultMins4[resultMins4CurrentIndex] = resultMins4DequeHead < resultMins4DequeTail ? (decimal?)resultMins4DequeValues[resultMins4DequeHead] : default(decimal?);
                    }

                    System.Buffers.ArrayPool<decimal>.Shared.Return(resultMins4DequeValues, false);
                    System.Buffers.ArrayPool<int>.Shared.Return(resultMins4DequeIndices, false);
                }

                var resultMaxs5 = new decimal? [resultWindowRows.Count];
                for (int resultMaxs5PartitionSetIndex = 0; resultMaxs5PartitionSetIndex < resultNtiles0Partitions.PartitionCount; ++resultMaxs5PartitionSetIndex)
                {
                    var resultMaxs5PartitionStart = resultNtiles0Partitions.GetStart(resultMaxs5PartitionSetIndex);
                    var resultMaxs5PartitionCount = resultNtiles0Partitions.GetLength(resultMaxs5PartitionSetIndex);
                    var resultMaxs5PartitionIndices = resultNtiles0Partitions.Indices;
                    var resultMaxs5DequeValues = System.Buffers.ArrayPool<decimal>.Shared.Rent(resultMaxs5PartitionCount);
                    var resultMaxs5DequeIndices = System.Buffers.ArrayPool<int>.Shared.Rent(resultMaxs5PartitionCount);
                    var resultMaxs5DequeHead = 0;
                    var resultMaxs5DequeTail = 0;
                    var resultMaxs5DequeFrameEnd = -1;
                    for (int resultMaxs5PartitionIndex = 0; resultMaxs5PartitionIndex < resultMaxs5PartitionCount; ++resultMaxs5PartitionIndex)
                    {
                        var resultMaxs5CurrentIndex = resultMaxs5PartitionIndices[resultMaxs5PartitionStart + resultMaxs5PartitionIndex];
                        var resultMaxs5FrameStart = 0;
                        var resultMaxs5FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultMaxs5PartitionIndices, resultMaxs5PartitionStart, resultMaxs5PartitionCount, resultMaxs5PartitionIndex);
                        while (resultMaxs5DequeFrameEnd < resultMaxs5FrameEnd)
                        {
                            ++resultMaxs5DequeFrameEnd;
                            var resultMaxs5FrameValueIndex = resultMaxs5PartitionIndices[resultMaxs5PartitionStart + resultMaxs5DequeFrameEnd];
                            Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultMaxs5FrameValueIndex];
                            var resultMaxs5Value = ko3iko.Population;
                            if (true)
                            {
                                while (resultMaxs5DequeTail > resultMaxs5DequeHead && resultMaxs5DequeValues[resultMaxs5DequeTail - 1].CompareTo(resultMaxs5Value) <= 0)
                                    --resultMaxs5DequeTail;
                                resultMaxs5DequeValues[resultMaxs5DequeTail] = resultMaxs5Value;
                                resultMaxs5DequeIndices[resultMaxs5DequeTail] = resultMaxs5DequeFrameEnd;
                                ++resultMaxs5DequeTail;
                            }
                        }

                        while (resultMaxs5DequeHead < resultMaxs5DequeTail && resultMaxs5DequeIndices[resultMaxs5DequeHead] < resultMaxs5FrameStart)
                            ++resultMaxs5DequeHead;
                        resultMaxs5[resultMaxs5CurrentIndex] = resultMaxs5DequeHead < resultMaxs5DequeTail ? (decimal?)resultMaxs5DequeValues[resultMaxs5DequeHead] : default(decimal?);
                    }

                    System.Buffers.ArrayPool<decimal>.Shared.Return(resultMaxs5DequeValues, false);
                    System.Buffers.ArrayPool<int>.Shared.Return(resultMaxs5DequeIndices, false);
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    __musoqFinalShapeRows.Add(new ResultShape0(ko3iko.Name, (long)resultNtiles0[windowIndex], (string)resultFirstValues1[windowIndex], (string)resultLastValues2[windowIndex], (string)resultNthValues3[windowIndex], (decimal?)resultMins4[windowIndex], (decimal?)resultMaxs5[windowIndex]));
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, long __value1, string __value2, string __value3, string __value4, decimal? __value5, decimal? __value6)
            {
                Name = __value0;
                Bucket = __value1;
                FirstName = __value2;
                LastName = __value3;
                NthName = __value4;
                MinPopulation = __value5;
                MaxPopulation = __value6;
            }

            public long Bucket { get; private set; }
            public override int Count => 7;
            public string FirstName { get; private set; }
            public string LastName { get; private set; }
            public decimal? MaxPopulation { get; private set; }
            public decimal? MinPopulation { get; private set; }
            public string Name { get; private set; }
            public string NthName { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Bucket = (long)value;
                        break;
                    case 2:
                        FirstName = (string)value;
                        break;
                    case 3:
                        LastName = (string)value;
                        break;
                    case 4:
                        NthName = (string)value;
                        break;
                    case 5:
                        MinPopulation = (decimal?)value;
                        break;
                    case 6:
                        MaxPopulation = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Bucket" => true,
                "FirstName" => true,
                "LastName" => true,
                "NthName" => true,
                "MinPopulation" => true,
                "MaxPopulation" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Bucket,
                2 => (object)FirstName,
                3 => (object)LastName,
                4 => (object)NthName,
                5 => (object)MinPopulation,
                6 => (object)MaxPopulation,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Bucket" => (object)Bucket,
                "FirstName" => (object)FirstName,
                "LastName" => (object)LastName,
                "NthName" => (object)NthName,
                "MinPopulation" => (object)MinPopulation,
                "MaxPopulation" => (object)MaxPopulation,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, long Bucket, string FirstName, string LastName, string NthName, decimal? MinPopulation, decimal? MaxPopulation)
            {
                this.Name = Name;
                this.Bucket = Bucket;
                this.FirstName = FirstName;
                this.LastName = LastName;
                this.NthName = NthName;
                this.MinPopulation = MinPopulation;
                this.MaxPopulation = MaxPopulation;
            }

            public long Bucket { get; }
            public string FirstName { get; }
            public string LastName { get; }
            public decimal? MaxPopulation { get; }
            public decimal? MinPopulation { get; }
            public string Name { get; }
            public string NthName { get; }
        }

        private readonly struct WindowResultNtiles0OrderKeysKey : System.IEquatable<WindowResultNtiles0OrderKeysKey>, System.IComparable<WindowResultNtiles0OrderKeysKey>
        {
            private readonly string _value0;
            public WindowResultNtiles0OrderKeysKey(string value0)
            {
                _value0 = value0;
            }

            public int CompareTo(WindowResultNtiles0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultNtiles0OrderKeysKey other)
            {
                return System.String.Equals(_value0, other._value0, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultNtiles0OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0, System.StringComparer.Ordinal);
                return hash.ToHashCode();
            }

            private static int CompareValue0(string left, string right)
            {
                if (left == null)
                    return right == null ? 0 : -1;
                if (right == null)
                    return 1;
                var comparison = System.String.CompareOrdinal(left, right);
                return comparison;
            }
        }
    }
}
