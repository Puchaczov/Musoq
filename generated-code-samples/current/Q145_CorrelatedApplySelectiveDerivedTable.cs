// === Parsed Query ===
/*
SELECT a.City, d.City
              FROM #A.entities() a
              CROSS APPLY (
                  SELECT b.City, b.Country
                  FROM #B.entities() b
                  WHERE b.Country = a.Country
                    AND b.City = a.City
              ) d
*/

// === Logical Plan ===
/*
Cte
  Definition [_dt_1]
    MultiStatement
      Project [b.City as b.City, b.Country as b.Country]
        SchemaScan [#B.entities() as b]
  Query
    MultiStatement
      Project [a.City as a.City, a.Country as a.Country, d.City as d.City, d.Country as d.Country]
        Join [Inner] [((d.Country = a.Country) AND (d.City = a.City))]
          SchemaScan [#A.entities() as a]
          CteRef [_dt_1 as d]
      Project [a.City as a.City, d.City as d.City]
        CteRef [ad as ad]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_dt_1]
    PhysicalMultiStatement
      PhysicalProject [b.City as b.City, b.Country as b.Country]
        PhysicalSchemaScan [#B.entities() as b]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.City as a.City, a.Country as a.Country, d.City as d.City, d.Country as d.Country]
        PhysicalHashJoin [Inner] [build: d.Country, d.City] [probe: a.Country, a.City]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [_dt_1 as d]
      PhysicalProject [a.City as a.City, d.City as d.City]
        PhysicalCteRef [ad as ad]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      City: string <- property City
      Country: string <- property Country
    SourceEntity [b: BasicEntity]
      City: string <- property City
      Country: string <- property Country
    HashPayload [DHashPayload0]
      b.City: string <- field b_City
    TableRow [d]
      b.City: string <- field b_City
      b.Country: string <- field b_Country
    Generated [ResultRow0]
      a.City: string <- field a_City
      d.City: string <- field d_City

  Body
    CtePhase [cte0]
    CtePhase [cte1]
    SourceScan [a: BasicEntity] -> aRows
    SourceScan [b: BasicEntity] -> cte0_bRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateHash [dHash: ValueTuple<string, string> -> Row]
    ChunkedForEach [b in cte0_bRows]
      Let [key0: string = b.Country]
      Let [key1: string = b.City]
      ContinueIf [((key0 = NULL) OR (key1 = NULL))]
      Let [key: ValueTuple<string, string> = (key0, key1)]
      CreateHashPayload [d <- DHashPayload0(b.City: b.City)]
      HashAdd [dHash[key] += d]
    ChunkedForEach [a in aRows]
      HashProbe [dHash[(a.Country, a.City)] -> dHashMatches]
        ForEach [d in dHashMatches]
          AppendShape [result <- ResultShape0(a.City: a.City, d.City: d.b.City)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q145_CorrelatedApplySelectiveDerivedTable
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("a.City", typeof(string), 0),
            new Column("d.City", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11), new Column("Country", typeof(string), 12) });
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_City, __musoqShapeRow.d_City);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __aSchema = provider.GetSchema("#A");
                var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var aRows = aRowsSource.Chunks;
                var __cte0_bSchema = provider.GetSchema("#B");
                var cte0_bRowsSource = __cte0_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_bRows = cte0_bRowsSource.Chunks;
                var dHash = new Dictionary<ValueTuple<string, string>, HashJoinBucket<DHashPayload0>>();
                foreach (var bChunk in cte0_bRows)
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
                                string key0 = b.Country;
                                string key1 = b.City;
                                if (((key0 == null) || (key1 == null)))
                                {
                                    continue;
                                }

                                ValueTuple<string, string> key = (key0, key1);
                                DHashPayload0 d = new DHashPayload0(b.City);
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(dHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<DHashPayload0>(d);
                                    }
                                    else
                                    {
                                        matches.Add(d);
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
                                string key0 = b.Country;
                                string key1 = b.City;
                                if (((key0 == null) || (key1 == null)))
                                {
                                    continue;
                                }

                                ValueTuple<string, string> key = (key0, key1);
                                DHashPayload0 d = new DHashPayload0(b.City);
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(dHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<DHashPayload0>(d);
                                    }
                                    else
                                    {
                                        matches.Add(d);
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
                        string key0 = b.Country;
                        string key1 = b.City;
                        if (((key0 == null) || (key1 == null)))
                        {
                            continue;
                        }

                        ValueTuple<string, string> key = (key0, key1);
                        DHashPayload0 d = new DHashPayload0(b.City);
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(dHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<DHashPayload0>(d);
                            }
                            else
                            {
                                matches.Add(d);
                            }
                        }
                    }
                }

                foreach (var aChunk in aRows)
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
                                var key0 = a.Country;
                                var key1 = a.City;
                                var key = (key0, key1);
                                if (key0 != null && key1 != null && dHash.TryGetValue(key, out var dHashMatches))
                                {
                                    foreach (var d in dHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.City, d.b_City));
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
                                var key0 = a.Country;
                                var key1 = a.City;
                                var key = (key0, key1);
                                if (key0 != null && key1 != null && dHash.TryGetValue(key, out var dHashMatches))
                                {
                                    foreach (var d in dHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.City, d.b_City));
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
                        var key0 = a.Country;
                        var key1 = a.City;
                        var key = (key0, key1);
                        if (key0 != null && key1 != null && dHash.TryGetValue(key, out var dHashMatches))
                        {
                            foreach (var d in dHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultShape0(a.City, d.b_City));
                            }
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
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

        private readonly struct DHashPayload0
        {
            public readonly string b_City;
            public DHashPayload0(string b_City)
            {
                this.b_City = b_City;
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                a_City = __value0;
                d_City = __value1;
            }

            public override int Count => 2;
            public string a_City { get; private set; }
            public string d_City { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_City = (string)value;
                        break;
                    case 1:
                        d_City = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.City" => true,
                "a_City" => true,
                "d.City" => true,
                "d_City" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_City,
                1 => (object)d_City,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.City" => (object)a_City,
                "a_City" => (object)a_City,
                "d.City" => (object)d_City,
                "d_City" => (object)d_City,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string a_City, string d_City)
            {
                this.a_City = a_City;
                this.d_City = d_City;
            }

            public string a_City { get; }
            public string d_City { get; }
        }
    }
}
