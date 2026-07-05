/*
raw query string

with cte as (select Name, City, Population from #A.entities() where Population > 0) select c.Name, a.Country from cte c inner join #A.entities() a on c.Name = a.Name
*/

/*
logical plan representation string

Cte
  Definition [cte]
    MultiStatement
      Project [ko3iko.Name as Name, ko3iko.City as City, ko3iko.Population as Population]
        Filter [(ko3iko.Population > 0)]
          SchemaScan [#A.entities() as ko3iko]
  Query
    MultiStatement
      Project [c.Name as c.Name, a.Name as a.Name, a.Country as a.Country]
        Join [Inner] [(c.Name = a.Name)]
          CteRef [cte as c]
          SchemaScan [#A.entities() as a]
      Project [c.Name as c.Name, a.Country as a.Country]
        CteRef [ca as ca]
*/

/*
physical plan representation string

PhysicalCte
  Definition [cte]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Name as Name, ko3iko.City as City, ko3iko.Population as Population]
        PhysicalFilter [(ko3iko.Population > 0)]
          PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: (ko3iko.Population > 0)]
  Query
    PhysicalMultiStatement
      PhysicalProject [c.Name as c.Name, a.Name as a.Name, a.Country as a.Country]
        PhysicalHashJoin [Inner] [build: c.Name] [probe: a.Name]
          PhysicalCteRef [cte as c]
          PhysicalSchemaScan [#A.entities() as a]
      PhysicalProject [c.Name as c.Name, a.Country as a.Country]
        PhysicalCteRef [ca as ca]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
    TableRow [c]
      Name: string <- field Name
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      Country: string <- property Country
    Generated [ResultRow0]
      c.Name: string <- field c_Name
      a.Country: string <- field a_Country

  Body
    SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
    CreateHash [cte0HashSidecar0Name: string -> Row]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      If [(ko3iko.Population > 0)]
        CreateHashPayload [cte0SidecarPayload0 <- Cte0HashPayload0(Name: ko3iko.Name)]
        HashAdd [cte0HashSidecar0Name[ko3iko.Name] += cte0SidecarPayload0]
    StoreCteIndex [cte0HashSidecar0Name -> _cteIndexResults.Slot0 Hash]
    CtePhase [cte1]
    SourceScan [a: BasicEntity] -> aRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    LoadCteIndex [cHash <- _cteIndexResults.Slot0 Hash: string]
    ChunkedForEach [a in aRows]
      HashProbe [cHash[a.Name] -> cHashMatches]
        ForEach [c in cHashMatches]
          AppendShape [result <- ResultShape0(c.Name: c.Name, a.Country: a.Country)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q15_CteWithJoin
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
            new Column("c.Name", typeof(string), 0),
            new Column("a.Country", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Country", typeof(string), 12) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Population", typeof(decimal), 13) });
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
                yield return new ResultRow0(__musoqShapeRow.c_Name, __musoqShapeRow.a_Country);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_ko3ikoRows = cte0_ko3ikoRowsSource.Chunks;
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
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name);
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
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name);
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
                            Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name);
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
                var __aSchema = provider.GetSchema("#A");
                var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:2", sourceExecutionPlans["a:2"], token, __schemaColumns_compiled_a_1, sourceRuntimeSettingsBySourceContextId["a:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var aRows = aRowsSource.Chunks;
                var cHash = _cteIndexResults.Slot0;
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
                                string key = a.Name;
                                if (key != null && cHash.TryGetValue(key, out var cHashMatches))
                                {
                                    foreach (var c in cHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(c.Name, a.Country));
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
                                if (key != null && cHash.TryGetValue(key, out var cHashMatches))
                                {
                                    foreach (var c in cHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(c.Name, a.Country));
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
                        if (key != null && cHash.TryGetValue(key, out var cHashMatches))
                        {
                            foreach (var c in cHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultShape0(c.Name, a.Country));
                            }
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
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

        private readonly struct Cte0HashPayload0
        {
            public readonly string Name;
            public Cte0HashPayload0(string Name)
            {
                this.Name = Name;
            }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<string, HashJoinBucket<Cte0HashPayload0>> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                c_Name = __value0;
                a_Country = __value1;
            }

            public override int Count => 2;
            public string a_Country { get; private set; }
            public string c_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        c_Name = (string)value;
                        break;
                    case 1:
                        a_Country = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "c.Name" => true,
                "c_Name" => true,
                "Name" => true,
                "a.Country" => true,
                "a_Country" => true,
                "Country" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)c_Name,
                1 => (object)a_Country,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "c.Name" => (object)c_Name,
                "c_Name" => (object)c_Name,
                "Name" => (object)c_Name,
                "a.Country" => (object)a_Country,
                "a_Country" => (object)a_Country,
                "Country" => (object)a_Country,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string c_Name, string a_Country)
            {
                this.c_Name = c_Name;
                this.a_Country = a_Country;
            }

            public string a_Country { get; }
            public string c_Name { get; }
        }
    }
}
