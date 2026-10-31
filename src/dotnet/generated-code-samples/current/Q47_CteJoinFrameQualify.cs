// === Parsed Query ===
/*
with base as ( select Name, City, Population from #A.entities() where Population > 0) select b.Name, a.City, Sum(b.Population) over (partition by a.City order by b.Name rows between unbounded preceding and current row) as RunSum from base b inner join #A.entities() a on b.Name = a.Name qualify Sum(b.Population) over (partition by a.City order by b.Name rows between unbounded preceding and current row) > 100
*/

// === Logical Plan ===
/*
Cte
  Definition [base]
    MultiStatement
      Project [ko3iko.Name as Name, ko3iko.City as City, ko3iko.Population as Population]
        Filter [(ko3iko.Population > 0)]
          SchemaScan [#A.entities() as ko3iko]
  Query
    MultiStatement
      Project [b.Name as b.Name, b.Population as b.Population, a.Name as a.Name, a.City as a.City]
        Join [Inner] [(b.Name = a.Name)]
          CteRef [base as b]
          SchemaScan [#A.entities() as a]
      Project [b.Name as Name, a.City as City, WindowRef(0) as RunSum]
        Qualify [(WindowRef(0) > 100)]
          Window [Sum(idx:0; partition: a.City; order: b.Name; args: b.Population; frame: rows between unbounded preceding and current row)]
            CteRef [ba as ba]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [base]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Name as Name, ko3iko.City as City, ko3iko.Population as Population]
        PhysicalFilter [(ko3iko.Population > 0)]
          PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: (ko3iko.Population > 0)]
  Query
    PhysicalMultiStatement
      PhysicalProject [b.Name as b.Name, b.Population as b.Population, a.City as a.City]
        PhysicalHashJoin [Inner] [build: b.Name] [probe: a.Name]
          PhysicalCteRef [base as b]
          PhysicalSchemaScan [#A.entities() as a]
      PhysicalProject [b.Name as Name, a.City as City, WindowRef(0) as RunSum]
        PhysicalQualify [(WindowRef(0) > 100)]
          PhysicalWindow [Sum(idx:0; partition: a.City; order: b.Name; args: b.Population; frame: rows between unbounded preceding and current row)]
            PhysicalMaterialize
              PhysicalCteRef [ba as ba]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
      Population: decimal <- field Population
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
      Population: decimal <- field Population
    TableRow [b]
      Name: string <- field Name
      Population: decimal <- field Population
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      City: string <- property City
    Generated [Statement0Row0]
      b.Name: string <- field b_Name
      b.Population: decimal <- field b_Population
      a.City: string <- field a_City
    TableRow [ba]
      b.Name: string <- field b_Name
      b.Population: decimal <- field b_Population
      a.City: string <- field a_City
    Generated [ResultRow0]
      Name: string <- field Name
      City: string <- field City
      RunSum: decimal? <- field RunSum
  StoredTableRepresentations
    [1] GeneratedColumnarBuffer; row=Statement0Row0; ownership=ConstructFresh; lifetime=Execution

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
    CreateHash [cte0HashSidecar0Name: string -> Row]
    PhaseBoundary [Where:cte0]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      If [(ko3iko.Population > 0)]
        CreateHashPayload [cte0SidecarPayload0 <- Cte0HashPayload0(Name: ko3iko.Name, Population: ko3iko.Population)]
        HashAdd [cte0HashSidecar0Name[ko3iko.Name] += cte0SidecarPayload0]
    StoreCteIndex [cte0HashSidecar0Name -> _cteIndexResults.Slot0 Hash]
    PhaseBoundary [Select:cte0]
    PhaseBoundary [End:cte0]
    SourceScan [a: BasicEntity] -> statement0_aRows
    CreateTable [statement0: Statement0Row0]
    LoadCteIndex [statement0BHash <- _cteIndexResults.Slot0 Hash: string]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0BHash[a.Name] -> statement0BHashMatches]
        ForEach [b in statement0BHashMatches]
          AppendRow [statement0 <- Statement0Row0(b.Name: b.Name, b.Population: b.Population, a.City: a.City)]
    StoreTable [statement0 -> _cteRowResults.Slot1: GeneratedColumnarBuffer<Statement0Row0>]
    PhaseBoundary [End:cte1]
    Materialize [_cteRowResults.Slot1 -> resultWindowRows]
    ComputeSumWindowKernel[Running] [resultSums <- resultWindowRows value ba.b.Population partition by ba.a.City order by ba.b.Name ASC frame rows between unbounded preceding and current row]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEachIndexed [windowIndex, ba in resultWindowRows]
      If [(resultSums[windowIndex] > 100)]
        AppendShape [result <- ResultRow0(Name: ba.b.Name, City: ba.a.City, RunSum: resultSums[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q47_CteJoinFrameQualify
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
            new Column("b.Name", typeof(string), 0),
            new Column("b.Population", typeof(decimal), 1),
            new Column("a.City", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1) });
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
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults);
                var resultWindowRows = _cteRowResults.Slot1;
                var resultSumsPartitionBuilder = new Musoq.Evaluator.Helpers.WindowPartitionBuilder<string>(resultWindowRows.Count);
                var resultSumsOrderKeys = new WindowResultSumsOrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Statement0Row0 ba = resultWindowRows[windowIndex];
                    string resultSumsPartitionKeysValue = (string)ba.a_City;
                    resultSumsPartitionBuilder.Add(resultSumsPartitionKeysValue, windowIndex);
                    resultSumsOrderKeys[windowIndex] = new WindowResultSumsOrderKeysKey(ba.b_Name);
                }

                var resultSumsPartitions = resultSumsPartitionBuilder.ToPartitionSet();
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSumsPartitions, resultSumsOrderKeys, false);
                var resultSums = new decimal? [resultWindowRows.Count];
                for (int resultSumsPartitionSetIndex = 0; resultSumsPartitionSetIndex < resultSumsPartitions.PartitionCount; ++resultSumsPartitionSetIndex)
                {
                    var resultSumsPartitionStart = resultSumsPartitions.GetStart(resultSumsPartitionSetIndex);
                    var resultSumsPartitionCount = resultSumsPartitions.GetLength(resultSumsPartitionSetIndex);
                    var resultSumsPartitionIndices = resultSumsPartitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSumsState = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSumsState);
                    for (int resultSumsPartitionIndex = 0; resultSumsPartitionIndex < resultSumsPartitionCount; ++resultSumsPartitionIndex)
                    {
                        var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPartitionIndex];
                        Statement0Row0 ba = resultWindowRows[resultSumsCurrentIndex];
                        Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSumsState, (decimal?)ba.b_Population);
                        resultSums[resultSumsCurrentIndex] = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSumsState);
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

                    Statement0Row0 ba = resultWindowRows[windowIndex];
                    if ((Operators.SqlCompare<decimal?, int>((decimal?)resultSums[windowIndex], 100, (decimal? __sqlLeft, int __sqlRight) => (__sqlLeft > __sqlRight))) == true)
                    {
                        __musoqFinalShapeRows.Add(new ResultRow0(ba.b_Name, ba.a_City, (decimal?)resultSums[windowIndex]));
                    }
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
        private static StoredTableColumnBuffer1 BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                var cte0_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                var cte0HashSidecar0Name = new Dictionary<string, HashJoinBucket<Cte0HashPayload0>>();
                foreach (var ko3ikoChunk in cte0_ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                if ((ko3iko.Population > 0))
                                {
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name, ko3iko.Population);
                                    string cte0HashSidecar0NameKey0 = ko3iko.Name;
                                    if (cte0HashSidecar0NameKey0 != null)
                                    {
                                        {
                                            ref var cte0HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Name, cte0HashSidecar0NameKey0, out var cte0HashSidecar0NameBucket0Exists);
                                            if (!cte0HashSidecar0NameBucket0Exists)
                                            {
                                                cte0HashSidecar0NameBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                            }
                                            else
                                            {
                                                cte0HashSidecar0NameBucket0.Add(cte0SidecarPayload0);
                                            }
                                        }
                                    }
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                if ((ko3iko.Population > 0))
                                {
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name, ko3iko.Population);
                                    string cte0HashSidecar0NameKey0 = ko3iko.Name;
                                    if (cte0HashSidecar0NameKey0 != null)
                                    {
                                        {
                                            ref var cte0HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Name, cte0HashSidecar0NameKey0, out var cte0HashSidecar0NameBucket0Exists);
                                            if (!cte0HashSidecar0NameBucket0Exists)
                                            {
                                                cte0HashSidecar0NameBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                            }
                                            else
                                            {
                                                cte0HashSidecar0NameBucket0.Add(cte0SidecarPayload0);
                                            }
                                        }
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        if ((ko3iko.Population > 0))
                        {
                            Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name, ko3iko.Population);
                            string cte0HashSidecar0NameKey0 = ko3iko.Name;
                            if (cte0HashSidecar0NameKey0 != null)
                            {
                                {
                                    ref var cte0HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Name, cte0HashSidecar0NameKey0, out var cte0HashSidecar0NameBucket0Exists);
                                    if (!cte0HashSidecar0NameBucket0Exists)
                                    {
                                        cte0HashSidecar0NameBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                    }
                                    else
                                    {
                                        cte0HashSidecar0NameBucket0.Add(cte0SidecarPayload0);
                                    }
                                }
                            }
                        }
                    }
                }

                _cteIndexResults.Slot0 = cte0HashSidecar0Name;
                var __statement0_aSchema = provider.GetSchema("#A");
                var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__statement0_aSchema, "entities", new SourceExecutionContext("a:2", sourceExecutionPlans["a:2"], token, __schemaColumns_compiled_a_1, sourceRuntimeSettingsBySourceContextId["a:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:2") : statement0_aRowsSource.Chunks;
                var statement0 = new StoredTableColumnBuffer1();
                var statement0BHash = _cteIndexResults.Slot0;
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
                                string key = a.Name;
                                if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                {
                                    foreach (var b in statement0BHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0.Add(b.Name, b.Population, a.City);
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
                                string key = a.Name;
                                if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                {
                                    foreach (var b in statement0BHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0.Add(b.Name, b.Population, a.City);
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
                        string key = a.Name;
                        if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                        {
                            foreach (var b in statement0BHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                statement0.Add(b.Name, b.Population, a.City);
                            }
                        }
                    }
                }

                return statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        private readonly struct Cte0HashPayload0
        {
            public readonly string Name;
            public readonly decimal Population;
            public Cte0HashPayload0(string Name, decimal Population)
            {
                this.Name = Name;
                this.Population = Population;
            }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<string, HashJoinBucket<Cte0HashPayload0>> Slot0;
        }

        private sealed class CteRowResults
        {
            public StoredTableColumnBuffer1 Slot1;
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
            private readonly StoredTableColumnBuffer1 __buffer;
            private readonly int __index;
            public Statement0Row0(StoredTableColumnBuffer1 buffer, int index)
            {
                this.__buffer = buffer;
                this.__index = index;
            }

            public string b_Name => __buffer.Column0[__index];
            public decimal b_Population => __buffer.Column1[__index];
            public string a_City => __buffer.Column2[__index];
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

        private sealed class StoredTableColumnBuffer1
        {
            public readonly StoredTableColumn<string> Column0;
            public readonly StoredTableColumn<decimal> Column1;
            public readonly StoredTableColumn<string> Column2;
            public StoredTableColumnBuffer1(int capacity = 0)
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
