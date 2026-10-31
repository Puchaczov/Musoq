// === Parsed Query ===
/*
binary Body { Value: int le };
binary Packet {
    Kind: byte,
    Body: substream[4] as Body exact,
    Count: byte,
    Tail: byte[Count]
};
text Entry { Level: until '|', Number: until '|', Label: rest }; select t.Level as Level, Sum(p.Body.Value::long) as Total, Count(*) as Count
from bench.rows() r
cross apply Interpret<Packet>(r.Payload) p
cross apply p.Tail b
cross apply Parse<Entry>(r.Text) t
inner join bench.lookup() d on r.Key = d.Key
group by t.Level
order by t.Level
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Key as r.Key, r.Payload as r.Payload, r.Text as r.Text, p.Body as p.Body, p.Tail as p.Tail]
    Apply [Cross]
      SchemaScan [#bench.rows() as r]
      InterpretSource [#Packet(r.Payload) as p]
  Project [rp.r.Key as r.Key, rp.r.Payload as r.Payload, rp.r.Text as r.Text, rp.p.Body as p.Body, rp.p.Tail as p.Tail]
    Apply [Cross]
      CteRef [rp as rp]
      PropertySource [rp.p.Tail as b] [apply: Cross] [type: Byte[]]
  Project [rpb.r.Key as r.Key, rpb.r.Payload as r.Payload, rpb.r.Text as r.Text, rpb.p.Body as p.Body, rpb.p.Tail as p.Tail, t.Level as t.Level]
    Apply [Cross]
      CteRef [rpb as rpb]
      InterpretSource [#Entry(rpb.r.Text) as t]
  Project [rpbt.r.Key as r.Key, rpbt.r.Payload as r.Payload, rpbt.r.Text as r.Text, rpbt.p.Body as p.Body, rpbt.p.Tail as p.Tail, rpbt.t.Level as t.Level, d.Key as Key]
    Join [Inner] [(rpbt.r.Key = d.Key)]
      CteRef [rpbt as rpbt]
      SchemaScan [#bench.lookup() as d]
  Project [t.Level as t.Level, AggRef(r.Count(*)) as r.Count(*), AggRef(r.Sum(p.Body.Value::Int64)) as r.Sum(p.Body.Value::Int64)]
    Aggregate [keys: t.Level] [aggs: Count(*), Sum(Value::Int64)]
      CteRef [rpbtd as rpbtd]
  Sort [t.Level]
    Project [t.Level as Level, r.Sum(p.Body.Value::Int64) as Total, r.Count(*) as Count]
      CteRef [rpbtdScore as rpbtdScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Key as r.Key, r.Text as r.Text, p.Body as p.Body, p.Tail as p.Tail]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#bench.rows() as r]
      PhysicalInterpretSource [#Packet(r.Payload) as p]
  PhysicalProject [rp.r.Key as r.Key, rp.r.Text as r.Text, rp.p.Body as p.Body]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [rp as rp]
      PhysicalPropertySource [rp.p.Tail as b] [apply: Cross] [type: Byte[]]
  PhysicalProject [rpb.r.Key as r.Key, rpb.p.Body as p.Body, t.Level as t.Level]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [rpb as rpb]
      PhysicalInterpretSource [#Entry(rpb.r.Text) as t]
  PhysicalProject [rpbt.p.Body as p.Body, rpbt.t.Level as t.Level]
    PhysicalHashJoin [Inner] [build: rpbt.r.Key] [probe: d.Key]
      PhysicalCteRef [rpbt as rpbt]
      PhysicalSchemaScan [#bench.lookup() as d]
  PhysicalProject [t.Level as t.Level, AggRef(r.Count(*)) as r.Count(*), AggRef(r.Sum(p.Body.Value::Int64)) as r.Sum(p.Body.Value::Int64)]
    PhysicalSingleKeyAggregate [key: t.Level (String)] [aggs: Count(*), Sum(Value::Int64)]
      PhysicalCteRef [rpbtd as rpbtd]
  PhysicalSort [t.Level]
    PhysicalProject [t.Level as Level, r.Sum(p.Body.Value::Int64) as Total, r.Count(*) as Count]
      PhysicalCteRef [rpbtdScore as rpbtdScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Key: int <- property Key
      Payload: byte[] <- property Payload
      Text: string <- property Text
    SourceEntity [p: object]
      Kind: byte <- property Kind
      Body: object <- property Body
      Body.Value: int <- nested property Body.Value
      Count: byte <- property Count
      Tail: byte[] <- property Tail
    Generated [Statement0Row0]
      r.Key: int <- field r_Key
      r.Text: string <- field r_Text
      p.Body: object <- field p_Body
      p.Tail: byte[] <- field p_Tail
    TableRow [rp]
      r.Key: int <- field r_Key
      r.Text: string <- field r_Text
      p.Body: object <- field p_Body
      p.Tail: byte[] <- field p_Tail
    SourceEntity [b: byte]
      Value: byte <- direct scalar value
    Generated [Statement1Row0]
      r.Key: int <- field r_Key
      r.Text: string <- field r_Text
      p.Body: object <- field p_Body
    TableRow [rpb]
      r.Key: int <- field r_Key
      r.Text: string <- field r_Text
      p.Body: object <- field p_Body
    SourceEntity [t: object]
      Level: string <- property Level
      Number: string <- property Number
      Label: string <- property Label
    Generated [Statement2Row0]
      r.Key: int <- field r_Key
      p.Body: object <- field p_Body
      t.Level: string <- field t_Level
    TableRow [rpbt]
      r.Key: int <- field r_Key
      p.Body: object <- field p_Body
      t.Level: string <- field t_Level
    SourceEntity [d: BenchmarkSampleRow]
      Key: int <- property Key
    Generated [Statement3Row0]
      p.Body: object <- field p_Body
      t.Level: string <- field t_Level
    TableRow [rpbtd]
      p.Body: object <- field p_Body
      t.Level: string <- field t_Level
    AggregateGroup [Statement4AggregateGroup; keys: 1; typed aggs: 2]
    Generated [Statement4Row0]
      t.Level: string <- field t_Level
      r.Count(*): long <- field r_Count___
      r.Sum(p.Body.Value::Int64): long? <- field r_Sum_p_Body_Value__Int64_
    TableRow [rpbtdScore]
      t.Level: string <- field t_Level
      r.Count(*): long <- field r_Count___
      r.Sum(p.Body.Value::Int64): long? <- field r_Sum_p_Body_Value__Int64_
    Generated [ResultRow0]
      Level: string <- field Level
      Total: long? <- field Total
      Count: long <- field Count_

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: BenchmarkSampleRow] -> statement0_rRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [r in statement0_rRows]
      Let [rKey: int = r.Key]
      Let [rText: string = r.Text]
      InterpretSource [Packet.Interpret(r.Payload) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(r.Key: rKey, r.Text: rText, p.Body: p.Body, p.Tail: p.Tail)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateTable [statement1: Statement1Row0]
    ForEach [rp in _cteRowResults.Slot0]
      Let [rprKey: int = rp.r.Key]
      Let [rprText: string = rp.r.Text]
      EnumerableSource [rp.p.Tail -> statement1_bRows]
      ForEach [b in statement1_bRows]
        AppendRow [statement1 <- Statement1Row0(r.Key: rprKey, r.Text: rprText, p.Body: rp.p.Body)]
    StoreTable [statement1 -> _cteRowResults.Slot1: List<Statement1Row0>]
    CreateTable [statement2: Statement2Row0]
    ForEach [rpb in _cteRowResults.Slot1]
      Let [rpbrKey: int = rpb.r.Key]
      InterpretSource [Entry.Parse(rpb.r.Text) -> statement2_tRows]
      ScalarForEach [t in statement2_tRows]
        AppendRow [statement2 <- Statement2Row0(r.Key: rpbrKey, p.Body: rpb.p.Body, t.Level: t.Level)]
    StoreTable [statement2 -> _cteRowResults.Slot2: List<Statement2Row0>]
    SourceScan [d: BenchmarkSampleRow] -> statement3_dRows
    CreateTable [statement3: Statement3Row0]
    CreateHash [statement3RpbtHash: int -> Row; capacity: _cteRowResults.Slot2.Count]
    ForEach [rpbt in _cteRowResults.Slot2]
      HashAdd [statement3RpbtHash[rpbt.r.Key] += rpbt]
    ChunkedForEach [d in statement3_dRows]
      HashProbe [statement3RpbtHash[d.Key] -> statement3RpbtHashMatches]
        ForEach [rpbt in statement3RpbtHashMatches]
          AppendRow [statement3 <- Statement3Row0(p.Body: rpbt.p.Body, t.Level: rpbt.t.Level)]
    StoreTable [statement3 -> _cteRowResults.Slot3: List<Statement3Row0>]
    CreateTable [statement4: Statement4Row0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [statement4Groups: string -> Statement4AggregateGroup]
    ForEach [rpbtd in _cteRowResults.Slot3]
      GetOrAddSingleKeyAggregateGroup [statement4Group = statement4Groups[rpbtd.t.Level] by t.Level; typed: Statement4AggregateGroup]
      TypedAggregateSet [Set(statement4Group.__agg0)]
      Let [p_Body_Value: int = rpbtd.p.Body.Value]
      Let [p_Body_ValueInt64: long? = p_Body_Value::Int64]
      TypedAggregateSet [Set(statement4Group.__agg1, p_Body_ValueInt64)]
    EnsureCapacity [statement4 <- statement4GroupsToFinalize.Count]
    ForEach [statement4FinalGroup in statement4GroupsToFinalize]
      AppendRow [statement4 <- Statement4Row0(t.Level: statement4FinalGroup.t.Level, r.Count(*): r.Count(*), r.Sum(p.Body.Value::Int64): r.Sum(p.Body.Value::Int64))]
    StoreTable [statement4 -> _cteRowResults.Slot4: List<Statement4Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [rpbtdScore in _cteRowResults.Slot4]
      AppendShape [result <- ResultShape0(Level: rpbtdScore.t.Level, Total: rpbtdScore.r.Sum(p.Body.Value::Int64), Count: rpbtdScore.r.Count(*))]
    SortOwnedBuffer [result by Level ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X08_PacketAnalytics_BenchmarkTable
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
        private static readonly Column[] __columns_compiled_result_7 = new Column[]
        {
            new Column("Level", typeof(string), 0),
            new Column("Total", typeof(long?), 1),
            new Column("Count", typeof(long), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("r.Key", typeof(int), 0),
            new Column("r.Text", typeof(string), 1),
            new Column("p.Body", typeof(Musoq.Generated.Interpreters.Body), 2),
            new Column("p.Tail", typeof(byte[]), 3)
        };
        private static readonly Column[] __columns_compiled_statement1_2 = new Column[]
        {
            new Column("r.Key", typeof(int), 0),
            new Column("r.Text", typeof(string), 1),
            new Column("p.Body", typeof(Musoq.Generated.Interpreters.Body), 2)
        };
        private static readonly Column[] __columns_compiled_statement2_3 = new Column[]
        {
            new Column("r.Key", typeof(int), 0),
            new Column("p.Body", typeof(Musoq.Generated.Interpreters.Body), 1),
            new Column("t.Level", typeof(string), 2)
        };
        private static readonly Column[] __columns_compiled_statement3_5 = new Column[]
        {
            new Column("p.Body", typeof(Musoq.Generated.Interpreters.Body), 0),
            new Column("t.Level", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_statement4_6 = new Column[]
        {
            new Column("t.Level", typeof(string), 0),
            new Column("r.Count(*)", typeof(long), 1),
            new Column("r.Sum(p.Body.Value::Int64)", typeof(long?), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_4 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0), new Column("Payload", typeof(byte[]), 1), new Column("Text", typeof(string), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_7, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Level, __musoqShapeRow.Total, __musoqShapeRow.Count_);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot2 = BuildCte2(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot3 = BuildCte3(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                _cteRowResults.Slot4 = BuildCte4(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable4Rows = _cteRowResults.Slot4;
                for (int __storedTable4Index = 0; __storedTable4Index < __storedTable4Rows.Count; ++__storedTable4Index)
                {
                    if ((__storedTable4Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement4Row0 rpbtdScore = __storedTable4Rows[__storedTable4Index];
                    result.Add(new ResultShape0(rpbtdScore.t_Level, rpbtdScore.r_Sum_p_Body_Value__Int64_, rpbtdScore.r_Count___) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Level, right.Level);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_rSchema = provider.GetSchema("#bench");
            var statement0_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var rChunk in statement0_rRows)
            {
                if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkView)
                {
                    if (rChunkView.Source is Musoq.Evaluator.Tests.BenchmarkSampleRow[] rChunkViewArray)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewArray[rChunkViewOffset + rIndex];
                            int rKey = r.Key;
                            string rText = r.Text;
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var statement0_pRows = _interpreter_Packet.Interpret(r.Payload);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(rKey, rText, p.Body, p.Tail));
                            }
                        }

                        continue;
                    }

                    if (rChunkView.Source is List<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkViewList)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewList[rChunkViewOffset + rIndex];
                            int rKey = r.Key;
                            string rText = r.Text;
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var statement0_pRows = _interpreter_Packet.Interpret(r.Payload);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(rKey, rText, p.Body, p.Tail));
                            }
                        }

                        continue;
                    }
                }

                for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                {
                    if ((rIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var r = rChunk[rIndex];
                    int rKey = r.Key;
                    string rText = r.Text;
                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                    var statement0_pRows = _interpreter_Packet.Interpret(r.Payload);
                    if (statement0_pRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var p = statement0_pRows;
                        statement0.Add(new Statement0Row0(rKey, rText, p.Body, p.Tail));
                    }
                }
            }

            return statement0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement1Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var statement1 = new List<Statement1Row0>();
            var __storedTable0Rows = _cteRowResults.Slot0;
            for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
            {
                if ((__storedTable0Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Statement0Row0 rp = __storedTable0Rows[__storedTable0Index];
                int rprKey = rp.r_Key;
                string rprText = rp.r_Text;
                var statement1_bRows = (IEnumerable<byte>)rp.p_Tail ?? Array.Empty<byte>();
                var bIndexRows = statement1_bRows;
                if (bIndexRows is byte[] bIndexArray)
                    for (int bIndex = 0, bIndexCount = bIndexArray.Length; bIndex < bIndexCount; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        byte b = bIndexArray[bIndex];
                        statement1.Add(new Statement1Row0(rprKey, rprText, rp.p_Body));
                    }
                else if (bIndexRows is List<byte> bIndexConcreteList)
                    for (int bIndex = 0, bIndexCount = bIndexConcreteList.Count; bIndex < bIndexCount; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        byte b = bIndexConcreteList[bIndex];
                        statement1.Add(new Statement1Row0(rprKey, rprText, rp.p_Body));
                    }
                else
                {
                    int bIndex = 0;
                    foreach (var b in bIndexRows)
                    {
                        token.ThrowIfCancellationRequested();
                        statement1.Add(new Statement1Row0(rprKey, rprText, rp.p_Body));
                        ++bIndex;
                    }
                }
            }

            return statement1;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement2Row0> BuildCte2(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var statement2 = new List<Statement2Row0>();
            var __storedTable1Rows = _cteRowResults.Slot1;
            for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
            {
                if ((__storedTable1Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Statement1Row0 rpb = __storedTable1Rows[__storedTable1Index];
                int rpbrKey = rpb.r_Key;
                var _interpreter_Entry = new Musoq.Generated.Interpreters.Entry();
                var statement2_tRows = _interpreter_Entry.Parse(rpb.r_Text);
                if (statement2_tRows != null)
                {
                    token.ThrowIfCancellationRequested();
                    var t = statement2_tRows;
                    statement2.Add(new Statement2Row0(rpbrKey, rpb.p_Body, t.Level));
                }
            }

            return statement2;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement3Row0> BuildCte3(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement3_dSchema = provider.GetSchema("#bench");
            var statement3_dRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__statement3_dSchema, "lookup", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_4, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement3_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(statement3_dRowsSource.Chunks, __musoqProgressContext, "d:1") : statement3_dRowsSource.Chunks;
            var statement3 = new List<Statement3Row0>();
            var statement3RpbtHash = new Dictionary<int, HashJoinBucket<Statement2Row0>>(_cteRowResults.Slot2.Count);
            var __storedTable2Rows = _cteRowResults.Slot2;
            for (int __storedTable2Index = 0; __storedTable2Index < __storedTable2Rows.Count; ++__storedTable2Index)
            {
                if ((__storedTable2Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Statement2Row0 rpbt = __storedTable2Rows[__storedTable2Index];
                int key = rpbt.r_Key;
                {
                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement3RpbtHash, key, out var matchesExists);
                    if (!matchesExists)
                    {
                        matches = new HashJoinBucket<Statement2Row0>(rpbt);
                    }
                    else
                    {
                        matches.Add(rpbt);
                    }
                }
            }

            foreach (var dChunk in statement3_dRows)
            {
                if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BenchmarkSampleRow> dChunkView)
                {
                    if (dChunkView.Source is Musoq.Evaluator.Tests.BenchmarkSampleRow[] dChunkViewArray)
                    {
                        int dChunkViewOffset = dChunkView.Offset;
                        for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                        {
                            if ((dIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var d = dChunkViewArray[dChunkViewOffset + dIndex];
                            int key = d.Key;
                            if (statement3RpbtHash.TryGetValue(key, out var statement3RpbtHashMatches))
                            {
                                foreach (var rpbt in statement3RpbtHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement3.Add(new Statement3Row0(rpbt.p_Body, rpbt.t_Level));
                                }
                            }
                        }

                        continue;
                    }

                    if (dChunkView.Source is List<Musoq.Evaluator.Tests.BenchmarkSampleRow> dChunkViewList)
                    {
                        int dChunkViewOffset = dChunkView.Offset;
                        for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                        {
                            if ((dIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var d = dChunkViewList[dChunkViewOffset + dIndex];
                            int key = d.Key;
                            if (statement3RpbtHash.TryGetValue(key, out var statement3RpbtHashMatches))
                            {
                                foreach (var rpbt in statement3RpbtHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement3.Add(new Statement3Row0(rpbt.p_Body, rpbt.t_Level));
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int dIndex = 0, dIndexCount = dChunk.Count; dIndex < dIndexCount; ++dIndex)
                {
                    if ((dIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var d = dChunk[dIndex];
                    int key = d.Key;
                    if (statement3RpbtHash.TryGetValue(key, out var statement3RpbtHashMatches))
                    {
                        foreach (var rpbt in statement3RpbtHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement3.Add(new Statement3Row0(rpbt.p_Body, rpbt.t_Level));
                        }
                    }
                }
            }

            return statement3;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement4Row0> BuildCte4(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var statement4 = new List<Statement4Row0>();
            var statement4GroupsToFinalize = new List<Statement4AggregateGroup>();
            var statement4Groups = new Dictionary<string, Statement4AggregateGroup>();
            Statement4AggregateGroup statement4NullGroup = null;
            var __storedTable3Rows = _cteRowResults.Slot3;
            for (int __storedTable3Index = 0; __storedTable3Index < __storedTable3Rows.Count; ++__storedTable3Index)
            {
                if ((__storedTable3Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Statement3Row0 rpbtd = __storedTable3Rows[__storedTable3Index];
                string groupKey = rpbtd.t_Level;
                Statement4AggregateGroup statement4Group = null;
                if (groupKey != null)
                {
                    ref var statement4GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement4Groups, groupKey, out var statement4GroupExists);
                    if (!statement4GroupExists)
                    {
                        statement4GroupRef = new Statement4AggregateGroup(groupKey);
                        statement4GroupsToFinalize.Add(statement4GroupRef);
                    }

                    statement4Group = statement4GroupRef;
                }
                else
                {
                    if (statement4NullGroup == null)
                    {
                        statement4NullGroup = new Statement4AggregateGroup(null);
                        statement4GroupsToFinalize.Add(statement4NullGroup);
                    }

                    statement4Group = statement4NullGroup;
                }

                statement4Group.__agg0.Count = checked(statement4Group.__agg0.Count + 1L);
                int p_Body_Value = rpbtd.p_Body.Value;
                long? p_Body_ValueInt64 = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt64(p_Body_Value);
                {
                    var __agg1Input = (long?)p_Body_ValueInt64;
                    if (__agg1Input.HasValue)
                    {
                        var __agg1Current = __agg1Input.GetValueOrDefault();
                        statement4Group.__agg1.Value = statement4Group.__agg1.HasValue ? checked(statement4Group.__agg1.Value + __agg1Current) : __agg1Current;
                        statement4Group.__agg1.HasValue = true;
                    }
                }
            }

            statement4.EnsureCapacity(statement4GroupsToFinalize.Count);
            foreach (var statement4FinalGroup in statement4GroupsToFinalize)
            {
                token.ThrowIfCancellationRequested();
                statement4.Add(new Statement4Row0(statement4FinalGroup.__key0, statement4FinalGroup.__agg0.Count, statement4FinalGroup.__agg1.HasValue ? (long?)statement4FinalGroup.__agg1.Value : null));
            }

            return statement4;
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
            public List<Statement1Row0> Slot1;
            public List<Statement2Row0> Slot2;
            public List<Statement3Row0> Slot3;
            public List<Statement4Row0> Slot4;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, long? __value1, long __value2)
            {
                Level = __value0;
                Total = __value1;
                Count_ = __value2;
            }

            public override int Count => 3;
            public long Count_ { get; private set; }
            public string Level { get; private set; }
            public long? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Level = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Count_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Level),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Count_),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Level" => true,
                "Total" => true,
                "Count" => true,
                "Count_" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Level, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Count_, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Level, long? Total, long Count_)
            {
                this.Level = Level;
                this.Total = Total;
                this.Count_ = Count_;
            }

            public long Count_ { get; }
            public string Level { get; }
            public long? Total { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, string __value1, Musoq.Generated.Interpreters.Body __value2, byte[] __value3)
            {
                r_Key = __value0;
                r_Text = __value1;
                p_Body = __value2;
                p_Tail = __value3;
            }

            public Musoq.Generated.Interpreters.Body p_Body { get; }
            public byte[] p_Tail { get; }
            public int r_Key { get; }
            public string r_Text { get; }
        }

        private sealed class Statement1Row0
        {
            public Statement1Row0(int __value0, string __value1, Musoq.Generated.Interpreters.Body __value2)
            {
                r_Key = __value0;
                r_Text = __value1;
                p_Body = __value2;
            }

            public Musoq.Generated.Interpreters.Body p_Body { get; }
            public int r_Key { get; }
            public string r_Text { get; }
        }

        private sealed class Statement2Row0
        {
            public Statement2Row0(int __value0, Musoq.Generated.Interpreters.Body __value1, string __value2)
            {
                r_Key = __value0;
                p_Body = __value1;
                t_Level = __value2;
            }

            public Musoq.Generated.Interpreters.Body p_Body { get; }
            public int r_Key { get; }
            public string t_Level { get; }
        }

        private sealed class Statement3Row0
        {
            public Statement3Row0(Musoq.Generated.Interpreters.Body __value0, string __value1)
            {
                p_Body = __value0;
                t_Level = __value1;
            }

            public Musoq.Generated.Interpreters.Body p_Body { get; }
            public string t_Level { get; }
        }

        private sealed class Statement4AggregateGroup
        {
            public Musoq.Plugins.CountAllAggregateKernel.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<long>.State __agg1;
            public readonly string __key0;
            public Statement4AggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.CountAllAggregateKernel.Reset(ref this.__agg0);
                Musoq.Plugins.SumAggregateKernel<long>.Reset(ref this.__agg1);
            }

            public void MergeFrom(Statement4AggregateGroup source)
            {
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<long>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class Statement4Row0
        {
            public Statement4Row0(string __value0, long __value1, long? __value2)
            {
                t_Level = __value0;
                r_Count___ = __value1;
                r_Sum_p_Body_Value__Int64_ = __value2;
            }

            public long r_Count___ { get; }
            public long? r_Sum_p_Body_Value__Int64_ { get; }
            public string t_Level { get; }
        }
    }
}

// === SyntaxTree:  ===
// <auto-generated>
// This code was generated by Musoq Interpretation Schema code generator.
// Do not modify this file directly.
// </auto-generated>
#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Dynamic;
using Musoq.Schema.Interpreters;

namespace Musoq.Generated.Interpreters
{
    /// <summary>
    /// Generated interpreter for binary schema 'Body'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Body : BytesInterpreterBase<Body>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Body value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Value"))
            {
                writer.WritePropertyName("Value");
                var fieldValue = this.Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Value field value.</summary>
        public int Value { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Body";

        /// <inheritdoc/>
        public override Body InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Body();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Value");
            var _value = ReadInt32Le(data);
            __parsedValue.Value = _value;
            RecordParsedField("Value");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Packet'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Packet : BytesInterpreterBase<Packet>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Packet value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Kind"))
            {
                writer.WritePropertyName("Kind");
                var fieldValue = this.Kind;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Body"))
            {
                writer.WritePropertyName("Body");
                var fieldValue = this.Body;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Count"))
            {
                writer.WritePropertyName("Count");
                var fieldValue = this.Count;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Tail"))
            {
                writer.WritePropertyName("Tail");
                var fieldValue = this.Tail;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Kind field value.</summary>
        public byte Kind { get; set; }
        /// <summary>Gets the Body field value.</summary>
        public Body Body { get; set; }
        /// <summary>Gets the Count field value.</summary>
        public byte Count { get; set; }
        /// <summary>Gets the Tail field value.</summary>
        public byte[] Tail { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Packet";

        /// <inheritdoc/>
        public override Packet InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Packet();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Kind");
            var _kind = ReadByte(data);
            __parsedValue.Kind = _kind;
            RecordParsedField("Kind");
            SetCurrentField("Body");
            var __body_substreamLength = 4;
            var __body_substreamSlice = ReadSubstreamSlice(data, __body_substreamLength);
            var __body_substreamStart = ParsePosition;
            var __body_substreamInterpreter = new Body();
            var _body = InterpretNestedAt(__body_substreamInterpreter, __body_substreamSlice, 0, "Body");
            EnsureSubstreamFullyConsumed("Body", __body_substreamLength, __body_substreamInterpreter);
            ParsePosition = __body_substreamStart + __body_substreamLength;
            BitOffset = 0;
            __parsedValue.Body = _body;
            RecordParsedField("Body");
            SetCurrentField("Count");
            var _count = ReadByte(data);
            __parsedValue.Count = _count;
            RecordParsedField("Count");
            SetCurrentField("Tail");
            var _tail = ReadBytes(data, ResolveExpressionSize<byte>(() => checked((byte?)(_count)), ParseErrorCode.InvalidSize));
            __parsedValue.Tail = _tail;
            RecordParsedField("Tail");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for text schema 'Entry'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Entry : TextInterpreterBase<Entry>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Entry value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Level"))
            {
                writer.WritePropertyName("Level");
                var fieldValue = this.Level;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Number"))
            {
                writer.WritePropertyName("Number");
                var fieldValue = this.Number;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Label"))
            {
                writer.WritePropertyName("Label");
                var fieldValue = this.Label;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Level field value.</summary>
        public string? Level { get; set; }
        /// <summary>Gets the Number field value.</summary>
        public string? Number { get; set; }
        /// <summary>Gets the Label field value.</summary>
        public string? Label { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Entry";

        /// <inheritdoc/>
        public override Entry ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new Entry();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Level");
            var _level = ReadUntil(data, "|", fieldName: "Level");
            __parsedValue.Level = _level;
            RecordParsedField("Level");
            SetCurrentField("Number");
            var _number = ReadUntil(data, "|", fieldName: "Number");
            __parsedValue.Number = _number;
            RecordParsedField("Number");
            SetCurrentField("Label");
            var _label = ReadRest(data, fieldName: "Label");
            __parsedValue.Label = _label;
            RecordParsedField("Label");
            return __parsedValue;
        }
    }
}
