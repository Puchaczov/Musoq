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
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultRow0(Name: ko3iko.Name, Bucket: resultNtiles0[windowIndex], FirstName: resultFirstValues1[windowIndex], LastName: resultLastValues2[windowIndex], NthName: resultNthValues3[windowIndex], MinPopulation: resultMins4[windowIndex], MaxPopulation: resultMaxs5[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
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
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;

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
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultNtiles0OrderKeys = new WindowResultNtiles0OrderKeysKey[resultWindowRows.Count];
                var resultNtiles0Values = new int[resultWindowRows.Count];
                for (int resultNtiles0ExtractionIndex = 0; resultNtiles0ExtractionIndex < resultWindowRows.Count; ++resultNtiles0ExtractionIndex)
                {
                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultNtiles0ExtractionIndex];
                    resultNtiles0OrderKeys[resultNtiles0ExtractionIndex] = new WindowResultNtiles0OrderKeysKey(ko3iko.Name);
                    resultNtiles0Values[resultNtiles0ExtractionIndex] = (int)(int)2;
                }

                var resultNtiles0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultNtiles0Partitions, resultNtiles0OrderKeys, false);
                var resultNtiles0 = new long[resultWindowRows.Count];
                for (int resultNtiles0Partition = 0; resultNtiles0Partition < resultNtiles0Partitions.PartitionCount; ++resultNtiles0Partition)
                {
                    var resultNtiles0Start = resultNtiles0Partitions.GetStart(resultNtiles0Partition);
                    var resultNtiles0Count = resultNtiles0Partitions.GetLength(resultNtiles0Partition);
                    var resultNtiles0Indices = resultNtiles0Partitions.Indices;
                    for (int resultNtiles0Position = 0; resultNtiles0Position < resultNtiles0Count; ++resultNtiles0Position)
                    {
                        if ((resultNtiles0Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultNtiles0Current = resultNtiles0Indices[resultNtiles0Start + resultNtiles0Position];
                        var resultNtiles0FrameStart = 0;
                        var resultNtiles0FrameEnd = resultNtiles0Position;
                        var resultNtiles0Context = new Musoq.Plugins.WindowFrameContext<int>(resultNtiles0Values, new System.ReadOnlySpan<int>(resultNtiles0Indices, resultNtiles0Start, resultNtiles0Count), resultNtiles0Position, resultNtiles0FrameStart, resultNtiles0FrameEnd, token);
                        resultNtiles0[resultNtiles0Current] = Musoq.Plugins.NtileKernel.Get(in resultNtiles0Context);
                    }
                }

                var resultFirstValues1Values = new string[resultWindowRows.Count];
                for (int resultFirstValues1ExtractionIndex = 0; resultFirstValues1ExtractionIndex < resultWindowRows.Count; ++resultFirstValues1ExtractionIndex)
                {
                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultFirstValues1ExtractionIndex];
                    resultFirstValues1Values[resultFirstValues1ExtractionIndex] = (string)(string)ko3iko.Name;
                }

                var resultFirstValues1 = new string[resultWindowRows.Count];
                for (int resultFirstValues1Partition = 0; resultFirstValues1Partition < resultNtiles0Partitions.PartitionCount; ++resultFirstValues1Partition)
                {
                    var resultFirstValues1Start = resultNtiles0Partitions.GetStart(resultFirstValues1Partition);
                    var resultFirstValues1Count = resultNtiles0Partitions.GetLength(resultFirstValues1Partition);
                    var resultFirstValues1Indices = resultNtiles0Partitions.Indices;
                    for (int resultFirstValues1Position = 0; resultFirstValues1Position < resultFirstValues1Count; ++resultFirstValues1Position)
                    {
                        if ((resultFirstValues1Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultFirstValues1Current = resultFirstValues1Indices[resultFirstValues1Start + resultFirstValues1Position];
                        var resultFirstValues1FrameStart = 0;
                        var resultFirstValues1FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultFirstValues1Indices, resultFirstValues1Start, resultFirstValues1Count, resultFirstValues1Position);
                        var resultFirstValues1Context = new Musoq.Plugins.WindowFrameContext<string>(resultFirstValues1Values, new System.ReadOnlySpan<int>(resultFirstValues1Indices, resultFirstValues1Start, resultFirstValues1Count), resultFirstValues1Position, resultFirstValues1FrameStart, resultFirstValues1FrameEnd, token);
                        resultFirstValues1[resultFirstValues1Current] = Musoq.Plugins.FirstReferenceValueKernel<string>.Get(in resultFirstValues1Context);
                    }
                }

                var resultLastValues2Values = new string[resultWindowRows.Count];
                for (int resultLastValues2ExtractionIndex = 0; resultLastValues2ExtractionIndex < resultWindowRows.Count; ++resultLastValues2ExtractionIndex)
                {
                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultLastValues2ExtractionIndex];
                    resultLastValues2Values[resultLastValues2ExtractionIndex] = (string)(string)ko3iko.Name;
                }

                var resultLastValues2 = new string[resultWindowRows.Count];
                for (int resultLastValues2Partition = 0; resultLastValues2Partition < resultNtiles0Partitions.PartitionCount; ++resultLastValues2Partition)
                {
                    var resultLastValues2Start = resultNtiles0Partitions.GetStart(resultLastValues2Partition);
                    var resultLastValues2Count = resultNtiles0Partitions.GetLength(resultLastValues2Partition);
                    var resultLastValues2Indices = resultNtiles0Partitions.Indices;
                    for (int resultLastValues2Position = 0; resultLastValues2Position < resultLastValues2Count; ++resultLastValues2Position)
                    {
                        if ((resultLastValues2Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultLastValues2Current = resultLastValues2Indices[resultLastValues2Start + resultLastValues2Position];
                        var resultLastValues2FrameStart = 0;
                        var resultLastValues2FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultLastValues2Indices, resultLastValues2Start, resultLastValues2Count, resultLastValues2Position);
                        var resultLastValues2Context = new Musoq.Plugins.WindowFrameContext<string>(resultLastValues2Values, new System.ReadOnlySpan<int>(resultLastValues2Indices, resultLastValues2Start, resultLastValues2Count), resultLastValues2Position, resultLastValues2FrameStart, resultLastValues2FrameEnd, token);
                        resultLastValues2[resultLastValues2Current] = Musoq.Plugins.LastReferenceValueKernel<string>.Get(in resultLastValues2Context);
                    }
                }

                var resultNthValues3Values = new string[resultWindowRows.Count];
                var resultNthValues3Argument0 = new int[resultWindowRows.Count];
                for (int resultNthValues3ExtractionIndex = 0; resultNthValues3ExtractionIndex < resultWindowRows.Count; ++resultNthValues3ExtractionIndex)
                {
                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultNthValues3ExtractionIndex];
                    resultNthValues3Values[resultNthValues3ExtractionIndex] = (string)(string)ko3iko.Name;
                    resultNthValues3Argument0[resultNthValues3ExtractionIndex] = (int)(int)1;
                }

                var resultNthValues3 = new string[resultWindowRows.Count];
                for (int resultNthValues3Partition = 0; resultNthValues3Partition < resultNtiles0Partitions.PartitionCount; ++resultNthValues3Partition)
                {
                    var resultNthValues3Start = resultNtiles0Partitions.GetStart(resultNthValues3Partition);
                    var resultNthValues3Count = resultNtiles0Partitions.GetLength(resultNthValues3Partition);
                    var resultNthValues3Indices = resultNtiles0Partitions.Indices;
                    for (int resultNthValues3Position = 0; resultNthValues3Position < resultNthValues3Count; ++resultNthValues3Position)
                    {
                        if ((resultNthValues3Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultNthValues3Current = resultNthValues3Indices[resultNthValues3Start + resultNthValues3Position];
                        var resultNthValues3FrameStart = 0;
                        var resultNthValues3FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultNthValues3Indices, resultNthValues3Start, resultNthValues3Count, resultNthValues3Position);
                        var resultNthValues3Context = new Musoq.Plugins.WindowFrameContext<string>(resultNthValues3Values, new System.ReadOnlySpan<int>(resultNthValues3Indices, resultNthValues3Start, resultNthValues3Count), resultNthValues3Position, resultNthValues3FrameStart, resultNthValues3FrameEnd, token);
                        resultNthValues3[resultNthValues3Current] = Musoq.Plugins.NthReferenceValueKernel<string>.Get(in resultNthValues3Context, resultNthValues3Argument0[resultNthValues3Current]);
                    }
                }

                var resultMins4 = new decimal? [resultWindowRows.Count];
                for (int resultMins4PartitionSetIndex = 0; resultMins4PartitionSetIndex < resultNtiles0Partitions.PartitionCount; ++resultMins4PartitionSetIndex)
                {
                    var resultMins4PartitionStart = resultNtiles0Partitions.GetStart(resultMins4PartitionSetIndex);
                    var resultMins4PartitionCount = resultNtiles0Partitions.GetLength(resultMins4PartitionSetIndex);
                    var resultMins4PartitionIndices = resultNtiles0Partitions.Indices;
                    Musoq.Plugins.MinAggregateKernel<decimal>.State resultMins4State = default(Musoq.Plugins.MinAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.MinAggregateKernel<decimal>.Reset(ref resultMins4State);
                    for (int resultMins4PartitionIndex = 0; resultMins4PartitionIndex < resultMins4PartitionCount;)
                    {
                        var resultMins4PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultMins4PartitionIndices, resultMins4PartitionStart, resultMins4PartitionCount, resultMins4PartitionIndex);
                        for (int resultMins4PeerIndex = resultMins4PartitionIndex; resultMins4PeerIndex <= resultMins4PeerEnd; ++resultMins4PeerIndex)
                        {
                            var resultMins4CurrentIndex = resultMins4PartitionIndices[resultMins4PartitionStart + resultMins4PeerIndex];
                            Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultMins4CurrentIndex];
                            Musoq.Plugins.MinAggregateKernel<decimal>.Set(ref resultMins4State, (decimal?)ko3iko.Population);
                        }

                        var resultMins4PeerValue = Musoq.Plugins.MinAggregateKernel<decimal>.Get(in resultMins4State);
                        for (int resultMins4PeerIndex = resultMins4PartitionIndex; resultMins4PeerIndex <= resultMins4PeerEnd; ++resultMins4PeerIndex)
                        {
                            var resultMins4CurrentIndex = resultMins4PartitionIndices[resultMins4PartitionStart + resultMins4PeerIndex];
                            resultMins4[resultMins4CurrentIndex] = resultMins4PeerValue;
                        }

                        resultMins4PartitionIndex = resultMins4PeerEnd + 1;
                    }
                }

                var resultMaxs5 = new decimal? [resultWindowRows.Count];
                for (int resultMaxs5PartitionSetIndex = 0; resultMaxs5PartitionSetIndex < resultNtiles0Partitions.PartitionCount; ++resultMaxs5PartitionSetIndex)
                {
                    var resultMaxs5PartitionStart = resultNtiles0Partitions.GetStart(resultMaxs5PartitionSetIndex);
                    var resultMaxs5PartitionCount = resultNtiles0Partitions.GetLength(resultMaxs5PartitionSetIndex);
                    var resultMaxs5PartitionIndices = resultNtiles0Partitions.Indices;
                    Musoq.Plugins.MaxAggregateKernel<decimal>.State resultMaxs5State = default(Musoq.Plugins.MaxAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.MaxAggregateKernel<decimal>.Reset(ref resultMaxs5State);
                    for (int resultMaxs5PartitionIndex = 0; resultMaxs5PartitionIndex < resultMaxs5PartitionCount;)
                    {
                        var resultMaxs5PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultNtiles0OrderKeys, resultMaxs5PartitionIndices, resultMaxs5PartitionStart, resultMaxs5PartitionCount, resultMaxs5PartitionIndex);
                        for (int resultMaxs5PeerIndex = resultMaxs5PartitionIndex; resultMaxs5PeerIndex <= resultMaxs5PeerEnd; ++resultMaxs5PeerIndex)
                        {
                            var resultMaxs5CurrentIndex = resultMaxs5PartitionIndices[resultMaxs5PartitionStart + resultMaxs5PeerIndex];
                            Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultMaxs5CurrentIndex];
                            Musoq.Plugins.MaxAggregateKernel<decimal>.Set(ref resultMaxs5State, (decimal?)ko3iko.Population);
                        }

                        var resultMaxs5PeerValue = Musoq.Plugins.MaxAggregateKernel<decimal>.Get(in resultMaxs5State);
                        for (int resultMaxs5PeerIndex = resultMaxs5PartitionIndex; resultMaxs5PeerIndex <= resultMaxs5PeerEnd; ++resultMaxs5PeerIndex)
                        {
                            var resultMaxs5CurrentIndex = resultMaxs5PartitionIndices[resultMaxs5PartitionStart + resultMaxs5PeerIndex];
                            resultMaxs5[resultMaxs5CurrentIndex] = resultMaxs5PeerValue;
                        }

                        resultMaxs5PartitionIndex = resultMaxs5PeerEnd + 1;
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(resultWindowRows.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    __musoqFinalShapeRows.Add(new ResultRow0(ko3iko.Name, (long)resultNtiles0[windowIndex], (string)resultFirstValues1[windowIndex], (string)resultLastValues2[windowIndex], (string)resultNthValues3[windowIndex], (decimal?)resultMins4[windowIndex], (decimal?)resultMaxs5[windowIndex]));
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Bucket = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.FirstName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.LastName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.NthName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.MinPopulation = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.MaxPopulation = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Bucket),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.FirstName),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.LastName),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.NthName),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.MinPopulation),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.MaxPopulation),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
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
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Bucket, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.FirstName, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.LastName, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.NthName, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.MinPopulation, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.MaxPopulation, ref visitor);
            }
        }

        private readonly struct WindowResultNtiles0OrderKeysKey : System.IEquatable<WindowResultNtiles0OrderKeysKey>, System.IComparable<WindowResultNtiles0OrderKeysKey>
        {
            private readonly string _value0;
            public WindowResultNtiles0OrderKeysKey(string value0)
            {
                _value0 = value0;
            }

            public string RawValues => _value0;

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
