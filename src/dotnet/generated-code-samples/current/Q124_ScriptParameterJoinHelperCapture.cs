// === Parsed Query ===
/*
param(suffix: string, fallback: string)
              select a.Name, Coalesce(b.Name + $suffix, $fallback) as MatchedName
              from #A.entities() a
              left outer join #B.entities() b on a.City + $suffix = b.City + $suffix
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Name as a.Name, a.City as a.City, b.Name as b.Name, b.City as b.City]
    Join [LeftOuter] [((a.City || $suffix) = (b.City || $suffix))]
      SchemaScan [#A.entities() as a]
      SchemaScan [#B.entities() as b]
  Project [a.Name as Name, Coalesce((b.Name || $suffix), $fallback) as MatchedName]
    CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Name as a.Name, b.Name as b.Name]
    PhysicalHashJoin [LeftOuter] [build: (b.City || $suffix)] [probe: (a.City || $suffix)]
      PhysicalSchemaScan [#A.entities() as a]
      PhysicalSchemaScan [#B.entities() as b]
  PhysicalProject [a.Name as Name, Coalesce((b.Name || $suffix), $fallback) as MatchedName]
    PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      City: string <- property City
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      City: string <- property City
    Generated [Statement0Row0]
      a.Name: string <- field a_Name
      b.Name: string <- field b_Name
    TableRow [ab]
      a.Name: string <- field a_Name
      b.Name: string <- field b_Name
    Generated [ResultRow0]
      Name: string <- field Name
      MatchedName: string <- field MatchedName

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: BasicEntity] -> statement0_aRows
    SourceScan [b: BasicEntity] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0BHash: string -> BasicEntity]
    ChunkedForEach [b in statement0_bRows]
      HashAdd [statement0BHash[(b.City || $suffix)] += b]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0BHash[(a.City || $suffix)] -> statement0BHashMatches] [match: statement0BHashHasMatch]
        ForEach [b in statement0BHashMatches]
          Assign [statement0BHashHasMatch = TRUE]
          AppendRow [statement0 <- Statement0Row0(a.Name: a.Name, b.Name: b.Name)]
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(a.Name: a.Name, b.Name: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateObject [__resultLibraryBase0: LibraryBase]
    PhaseBoundary [Select]
    ForEach [ab in _cteRowResults.Slot0]
      AppendShape [result <- ResultRow0(Name: ab.a.Name, MatchedName: Coalesce((ab.b.Name || $suffix), $fallback))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q124_ScriptParameterJoinHelperCapture
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("MatchedName", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
            new Column("b.Name", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("suffix", "string", "string", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null),
            new ScriptParameterContract("fallback", "string", "string", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null)
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("suffix", "string", "string", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null)),
            new ScriptParameterDefinition(new ScriptParameterContract("fallback", "string", "string", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null))
        };
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var paramSuffix = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState, "suffix");
                var paramFallback = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState, "fallback");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "suffix", "fallback" });
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, paramSuffix);
                var __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();
                OnPhaseChanged("compiled", QueryPhase.Select);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];
                    __musoqFinalShapeRows.Add(new ResultRow0(ab.a_Name, (string)__resultLibraryBase0.Coalesce<string>((ab.b_Name + paramSuffix), paramFallback)));
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
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, string paramSuffix)
        {
            var __statement0_aSchema = provider.GetSchema("#A");
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__statement0_aSchema, "entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#B");
            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__statement0_bSchema, "entities", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0BHash = new Dictionary<string, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>();
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
                            string key = (b.City + paramSuffix);
                            if (key == null)
                                continue;
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
                            string key = (b.City + paramSuffix);
                            if (key == null)
                                continue;
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
                    string key = (b.City + paramSuffix);
                    if (key == null)
                        continue;
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
                            bool statement0BHashHasMatch = false;
                            string key = (a.City + paramSuffix);
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0BHashHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.Name, b.Name));
                                }
                            }

                            if (!statement0BHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Name, null));
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
                            bool statement0BHashHasMatch = false;
                            string key = (a.City + paramSuffix);
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0BHashHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.Name, b.Name));
                                }
                            }

                            if (!statement0BHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Name, null));
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
                    bool statement0BHashHasMatch = false;
                    string key = (a.City + paramSuffix);
                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                    {
                        foreach (var b in statement0BHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0BHashHasMatch = true;
                            statement0.Add(new Statement0Row0(a.Name, b.Name));
                        }
                    }

                    if (!statement0BHashHasMatch)
                    {
                        statement0.Add(new Statement0Row0(a.Name, null));
                    }
                }
            }

            return statement0;
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                Name = __value0;
                MatchedName = __value1;
            }

            public override int Count => 2;
            public string MatchedName { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.MatchedName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.MatchedName),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "MatchedName" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.MatchedName, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1)
            {
                a_Name = __value0;
                b_Name = __value1;
            }

            public string a_Name { get; }
            public string b_Name { get; }
        }
    }
}
