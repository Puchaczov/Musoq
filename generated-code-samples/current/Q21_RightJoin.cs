/*
raw query string

select a.Name, b.Country from #A.entities() a right outer join #A.entities() b on a.Id = b.Id
*/

/*
logical plan representation string

MultiStatement
  Project [a.Name as a.Name, a.Id as a.Id, b.Country as b.Country, b.Id as b.Id]
    Join [RightOuter] [(a.Id = b.Id)]
      SchemaScan [#A.entities() as a]
      SchemaScan [#A.entities() as b]
  Project [a.Name as a.Name, b.Country as b.Country]
    CteRef [ab as ab]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [a.Name as a.Name, a.Id as a.Id, b.Country as b.Country, b.Id as b.Id]
    PhysicalHashJoin [RightOuter] [build: a.Id] [probe: b.Id]
      PhysicalSchemaScan [#A.entities() as a]
      PhysicalSchemaScan [#A.entities() as b]
  PhysicalProject [a.Name as a.Name, b.Country as b.Country]
    PhysicalCteRef [ab as ab]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      Id: int <- property Id
    SourceEntity [b: BasicEntity]
      Country: string <- property Country
      Id: int <- property Id
    Generated [ResultRow0]
      a.Name: string <- field a_Name
      b.Country: string <- field b_Country

  Body
    CtePhase [cte0]
    SourceScan [a: BasicEntity] -> aRows
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateHash [aHash: int? -> BasicEntity]
    ChunkedForEach [a in aRows]
      HashAdd [aHash[a.Id] += a]
    ChunkedForEach [b in bRows]
      HashProbe [aHash[b.Id] -> aHashMatches] [match: aHashHasMatch]
        ForEach [a in aHashMatches]
          Assign [aHashHasMatch = TRUE]
          AppendShape [result <- ResultShape0(a.Name: a.Name, b.Country: b.Country)]
      HashProbeNoMatch
        AppendShape [result <- ResultShape0(a.Name: NULL, b.Country: b.Country)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q21_RightJoin
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
            new Column("b.Country", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Id", typeof(int), 18) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Country", typeof(string), 12), new Column("Id", typeof(int), 18) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_Name, __musoqShapeRow.b_Country);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __aSchema = provider.GetSchema("#A");
                var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var aRows = aRowsSource.Chunks;
                var __bSchema = provider.GetSchema("#A");
                var bRowsSource = __bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var bRows = bRowsSource.Chunks;
                var aHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>();
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
                                int? key = a.Id;
                                if (key == null)
                                    continue;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(aHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(a);
                                    }
                                    else
                                    {
                                        matches.Add(a);
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
                                int? key = a.Id;
                                if (key == null)
                                    continue;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(aHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(a);
                                    }
                                    else
                                    {
                                        matches.Add(a);
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
                        int? key = a.Id;
                        if (key == null)
                            continue;
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(aHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(a);
                            }
                            else
                            {
                                matches.Add(a);
                            }
                        }
                    }
                }

                foreach (var bChunk in bRows)
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
                                bool aHashHasMatch = false;
                                int? key = b.Id;
                                if (key != null && aHash.TryGetValue(key, out var aHashMatches))
                                {
                                    foreach (var a in aHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        aHashHasMatch = true;
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.Name, b.Country));
                                    }
                                }

                                if (!aHashHasMatch)
                                {
                                    __musoqFinalShapeRows.Add(new ResultShape0(null, b.Country));
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
                                bool aHashHasMatch = false;
                                int? key = b.Id;
                                if (key != null && aHash.TryGetValue(key, out var aHashMatches))
                                {
                                    foreach (var a in aHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        aHashHasMatch = true;
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.Name, b.Country));
                                    }
                                }

                                if (!aHashHasMatch)
                                {
                                    __musoqFinalShapeRows.Add(new ResultShape0(null, b.Country));
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
                        bool aHashHasMatch = false;
                        int? key = b.Id;
                        if (key != null && aHash.TryGetValue(key, out var aHashMatches))
                        {
                            foreach (var a in aHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                aHashHasMatch = true;
                                __musoqFinalShapeRows.Add(new ResultShape0(a.Name, b.Country));
                            }
                        }

                        if (!aHashHasMatch)
                        {
                            __musoqFinalShapeRows.Add(new ResultShape0(null, b.Country));
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                a_Name = __value0;
                b_Country = __value1;
            }

            public override int Count => 2;
            public string a_Name { get; private set; }
            public string b_Country { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_Name = (string)value;
                        break;
                    case 1:
                        b_Country = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.Name" => true,
                "a_Name" => true,
                "Name" => true,
                "b.Country" => true,
                "b_Country" => true,
                "Country" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_Name,
                1 => (object)b_Country,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.Name" => (object)a_Name,
                "a_Name" => (object)a_Name,
                "Name" => (object)a_Name,
                "b.Country" => (object)b_Country,
                "b_Country" => (object)b_Country,
                "Country" => (object)b_Country,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string a_Name, string b_Country)
            {
                this.a_Name = a_Name;
                this.b_Country = b_Country;
            }

            public string a_Name { get; }
            public string b_Country { get; }
        }
    }
}
