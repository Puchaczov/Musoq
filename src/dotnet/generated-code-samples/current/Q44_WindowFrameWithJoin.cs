// === Parsed Query ===
/*
select a.Name, b.City, Sum(a.Population) over (order by a.Name rows between 1 preceding and current row) as RunSum from #A.entities() a inner join #A.entities() b on a.Id = b.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Name as a.Name, a.Population as a.Population, a.Id as a.Id, b.City as b.City, b.Id as b.Id]
    Join [Inner] [(a.Id = b.Id)]
      SchemaScan [#A.entities() as a]
      SchemaScan [#A.entities() as b]
  Project [a.Name as Name, b.City as City, WindowRef(0) as RunSum]
    Window [Sum(idx:0; order: a.Name; args: a.Population; frame: rows between 1 preceding and current row)]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Name as a.Name, a.Population as a.Population, b.City as b.City]
    PhysicalHashJoin [Inner] [build: b.Id] [probe: a.Id]
      PhysicalSchemaScan [#A.entities() as a]
      PhysicalSchemaScan [#A.entities() as b]
  PhysicalProject [a.Name as Name, b.City as City, WindowRef(0) as RunSum]
    PhysicalWindow [Sum(idx:0; order: a.Name; args: a.Population; frame: rows between 1 preceding and current row)]
      PhysicalMaterialize
        PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
      Id: int <- property Id
    SourceEntity [b: BasicEntity]
      City: string <- property City
      Id: int <- property Id
    Generated [Statement0Row0]
      a.Name: string <- field a_Name
      a.Population: decimal <- field a_Population
      b.City: string <- field b_City
    TableRow [ab]
      a.Name: string <- field a_Name
      a.Population: decimal <- field a_Population
      b.City: string <- field b_City
    Generated [ResultRow0]
      Name: string <- field Name
      City: string <- field City
      RunSum: decimal? <- field RunSum
  StoredTableRepresentations
    [0] GeneratedColumnarBuffer; row=Statement0Row0; ownership=ConstructFresh; lifetime=Execution

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: BasicEntity] -> statement0_aRows
    SourceScan [b: BasicEntity] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0BHash: int -> BasicEntity]
    ChunkedForEach [b in statement0_bRows]
      HashAdd [statement0BHash[b.Id] += b]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0BHash[a.Id] -> statement0BHashMatches]
        ForEach [b in statement0BHashMatches]
          AppendRow [statement0 <- Statement0Row0(a.Name: a.Name, a.Population: a.Population, b.City: b.City)]
    StoreTable [statement0 -> _cteRowResults.Slot0: GeneratedColumnarBuffer<Statement0Row0>]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums <- resultWindowRows value ab.a.Population order by ab.a.Name ASC frame rows between 1 preceding and current row]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEachIndexed [windowIndex, ab in resultWindowRows]
      AppendShape [result <- ResultRow0(Name: ab.a.Name, City: ab.b.City, RunSum: resultSums[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q44_WindowFrameWithJoin
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
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("City", typeof(string), 1),
            new Column("RunSum", typeof(decimal?), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
            new Column("a.Population", typeof(decimal), 1),
            new Column("b.City", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Population", typeof(decimal), 1), new Column("Id", typeof(int), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Id", typeof(int), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var resultWindowRows = _cteRowResults.Slot0;
                var resultSumsOrderKeys = new WindowResultSumsOrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Statement0Row0 ab = resultWindowRows[windowIndex];
                    resultSumsOrderKeys[windowIndex] = new WindowResultSumsOrderKeysKey(ab.a_Name);
                }

                var resultSumsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSumsPartitions, resultSumsOrderKeys, false);
                var resultSums = new decimal? [resultWindowRows.Count];
                var resultSumsBatchValues = new decimal? [resultWindowRows.Count];
                for (int resultSumsCaptureIndex = 0; resultSumsCaptureIndex < resultWindowRows.Count; ++resultSumsCaptureIndex)
                {
                    Statement0Row0 ab = resultWindowRows[resultSumsCaptureIndex];
                    resultSumsBatchValues[resultSumsCaptureIndex] = (decimal?)ab.a_Population;
                }

                for (int resultSumsPartitionSetIndex = 0; resultSumsPartitionSetIndex < resultSumsPartitions.PartitionCount; ++resultSumsPartitionSetIndex)
                {
                    var resultSumsPartitionStart = resultSumsPartitions.GetStart(resultSumsPartitionSetIndex);
                    var resultSumsPartitionCount = resultSumsPartitions.GetLength(resultSumsPartitionSetIndex);
                    var resultSumsPartitionIndices = resultSumsPartitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSumsState = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSumsState);
                    {
                        var resultSumsBatchStarts = new int[resultSumsPartitionCount];
                        var resultSumsBatchEnds = new int[resultSumsPartitionCount];
                        for (int resultSumsBatchPosition = 0; resultSumsBatchPosition < resultSumsPartitionCount; ++resultSumsBatchPosition)
                        {
                            if ((resultSumsBatchPosition & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            resultSumsBatchStarts[resultSumsBatchPosition] = Math.Max(0, resultSumsBatchPosition - 1);
                            resultSumsBatchEnds[resultSumsBatchPosition] = resultSumsBatchPosition;
                        }

                        var resultSumsBatchContext = new Musoq.Plugins.WindowBatchContext<decimal?, decimal?>(resultSumsBatchValues, new System.ReadOnlySpan<int>(resultSumsPartitionIndices, resultSumsPartitionStart, resultSumsPartitionCount), resultSumsBatchStarts, resultSumsBatchEnds, resultSums, token);
                        if (!Musoq.Plugins.DecimalSumWindowBatch.TryEvaluate(in resultSumsBatchContext))
                        {
                            for (int resultSumsPartitionIndex = 0; resultSumsPartitionIndex < resultSumsPartitionCount; ++resultSumsPartitionIndex)
                            {
                                var resultSumsOutputIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPartitionIndex];
                                var resultSumsFrameStart = Math.Max(0, resultSumsPartitionIndex - 1);
                                var resultSumsFrameEnd = resultSumsPartitionIndex;
                                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSumsState);
                                for (var resultSumsFrameIndex = Math.Max(0, resultSumsFrameStart); resultSumsFrameIndex <= resultSumsFrameEnd && resultSumsFrameIndex < resultSumsPartitionCount; resultSumsFrameIndex++)
                                {
                                    var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsFrameIndex];
                                    Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSumsState, (decimal?)resultSumsBatchValues[resultSumsCurrentIndex]);
                                }

                                resultSums[resultSumsOutputIndex] = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSumsState);
                            }
                        }
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

                    Statement0Row0 ab = resultWindowRows[windowIndex];
                    __musoqFinalShapeRows.Add(new ResultRow0(ab.a_Name, ab.b_City, (decimal?)resultSums[windowIndex]));
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static StoredTableColumnBuffer0 BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_aSchema = provider.GetSchema("#A");
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__statement0_aSchema, "entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#A");
            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__statement0_bSchema, "entities", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new StoredTableColumnBuffer0();
            var statement0BHash = new Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>();
            foreach (var bChunk in statement0_bRows)
            {
                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkView)
                {
                    if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] bChunkViewArray)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                            int key = b.Id;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }

                        continue;
                    }

                    if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkViewList)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                            int key = b.Id;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                {
                    if ((bIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var b = bChunk[bIndex];
                    int key = b.Id;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                        }
                        else
                        {
                            matches.Add(b);
                        }
                    }
                }
            }

            foreach (var aChunk in statement0_aRows)
            {
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            int key = a.Id;
                            if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(a.Name, a.Population, b.City);
                                }
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            int key = a.Id;
                            if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(a.Name, a.Population, b.City);
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int aIndex = 0, aIndexCount = aChunk.Count; aIndex < aIndexCount; ++aIndex)
                {
                    if ((aIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var a = aChunk[aIndex];
                    int key = a.Id;
                    if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                    {
                        foreach (var b in statement0BHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0.Add(a.Name, a.Population, b.City);
                        }
                    }
                }
            }

            return statement0;
        }

        private sealed class CteRowResults
        {
            public StoredTableColumnBuffer0 Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, decimal? __value2)
            {
                Name = __value0;
                City = __value1;
                RunSum = __value2;
            }

            public string City { get; private set; }
            public override int Count => 3;
            public string Name { get; private set; }
            public decimal? RunSum { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.RunSum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.RunSum),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "City" => true,
                "RunSum" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.RunSum, ref visitor);
            }
        }

        private readonly struct Statement0Row0
        {
            private readonly StoredTableColumnBuffer0 __buffer;
            private readonly int __index;
            public Statement0Row0(StoredTableColumnBuffer0 buffer, int index)
            {
                this.__buffer = buffer;
                this.__index = index;
            }

            public string a_Name => __buffer.Column0[__index];
            public decimal a_Population => __buffer.Column1[__index];
            public string b_City => __buffer.Column2[__index];
        }

        private sealed class StoredTableColumn<T>
        {
            private const int ChunkSize = 1024;
            private int _chunkCount;
            private T[][] _chunks = Array.Empty<T[]>();
            private int _count;
            public StoredTableColumn(int capacity = 0)
            {
                EnsureCapacity(capacity);
            }

            public int Count => _count;

            public void Add(T value)
            {
                var chunkIndex = _count >> 10;
                if (chunkIndex == _chunkCount)
                    AddChunk();
                _chunks[chunkIndex][_count & (ChunkSize - 1)] = value;
                _count++;
            }

            public void EnsureCapacity(int capacity)
            {
                if (capacity <= 0)
                    return;
                var requiredChunks = ((capacity - 1) / ChunkSize) + 1;
                while (_chunkCount < requiredChunks)
                    AddChunk();
            }

            private void AddChunk()
            {
                if (_chunkCount == _chunks.Length)
                    Array.Resize(ref _chunks, _chunkCount == 0 ? 4 : _chunkCount * 2);
                _chunks[_chunkCount++] = new T[ChunkSize];
            }

            public T this[int index] => _chunks[index >> 10][index & (ChunkSize - 1)];
        }

        private sealed class StoredTableColumnBuffer0
        {
            public readonly StoredTableColumn<string> Column0;
            public readonly StoredTableColumn<decimal> Column1;
            public readonly StoredTableColumn<string> Column2;
            public StoredTableColumnBuffer0(int capacity = 0)
            {
                Column0 = new StoredTableColumn<string>(capacity);
                Column1 = new StoredTableColumn<decimal>(capacity);
                Column2 = new StoredTableColumn<string>(capacity);
            }

            public int Count => Column0.Count;

            public void Add(string value0, decimal value1, string value2)
            {
                Column0.Add(value0);
                Column1.Add(value1);
                Column2.Add(value2);
            }

            public void EnsureCapacity(int capacity)
            {
                Column0.EnsureCapacity(capacity);
                Column1.EnsureCapacity(capacity);
                Column2.EnsureCapacity(capacity);
            }

            public Statement0Row0 this[int index] => new Statement0Row0(this, index);
        }

        private readonly struct WindowResultSumsOrderKeysKey : System.IEquatable<WindowResultSumsOrderKeysKey>, System.IComparable<WindowResultSumsOrderKeysKey>
        {
            private readonly string _value0;
            public WindowResultSumsOrderKeysKey(string value0)
            {
                _value0 = value0;
            }

            public string RawValues => _value0;

            public int CompareTo(WindowResultSumsOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSumsOrderKeysKey other)
            {
                return System.String.Equals(_value0, other._value0, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSumsOrderKeysKey other && Equals(other);
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
