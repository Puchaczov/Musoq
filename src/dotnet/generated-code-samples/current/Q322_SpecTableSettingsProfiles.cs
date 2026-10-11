// === Parsed Query ===
/*
table SettingsRow { Token: string };
                    couple #settings.items with settings blue as SettingsOnly;
                    couple #settings.items with table SettingsRow and settings red as TableFirst;
                    couple #settings.items with settings green and table SettingsRow as SettingsFirst;
                    select a.Token, b.Token, c.Token
                    from SettingsOnly() a
                    cross join TableFirst() b
                    cross join SettingsFirst() c
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Token as a.Token, b.Token as b.Token]
    Join [Cross] [TRUE]
      SchemaScan [#settings.items() as a]
      SchemaScan [#settings.items() as b]
  Project [ab.a.Token as a.Token, ab.b.Token as b.Token, c.Token as Token]
    Join [Cross] [TRUE]
      CteRef [ab as ab]
      SchemaScan [#settings.items() as c]
  Project [a.Token as Token, b.Token as Token, c.Token as Token]
    CteRef [abc as abc]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Token as a.Token, b.Token as b.Token]
    PhysicalNestedLoopJoin [Cross] [TRUE]
      PhysicalSchemaScan [#settings.items() as a]
      PhysicalSchemaScan [#settings.items() as b]
  PhysicalProject [ab.a.Token as a.Token, ab.b.Token as b.Token, c.Token as Token]
    PhysicalNestedLoopJoin [Cross] [TRUE]
      PhysicalCteRef [ab as ab]
      PhysicalSchemaScan [#settings.items() as c]
  PhysicalProject [a.Token as Token, b.Token as Token, c.Token as Token]
    PhysicalCteRef [abc as abc]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: SettingsEntity]
      Token: string <- property Token
    SourceEntity [b: SettingsEntity]
      Token: string <- property Token
    Generated [Statement0Row0]
      a.Token: string <- field a_Token
      b.Token: string <- field b_Token
    TableRow [ab]
      a.Token: string <- field a_Token
      b.Token: string <- field b_Token
    SourceEntity [c: SettingsEntity]
      Token: string <- property Token
    Generated [ResultRow0]
      Token: string <- field Token
      Token: string <- field Token_1
      Token: string <- field Token_2

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: SettingsEntity] -> statement0_aRows
    SourceScan [b: SettingsEntity] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    MaterializeChunked [statement0_bRows -> bRowsBuffer]
    ChunkedForEach [a in statement0_aRows]
      Let [aToken: string = a.Token]
      ForEach [b in bRowsBuffer]
        AppendRow [statement0 <- Statement0Row0(a.Token: aToken, b.Token: b.Token)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [c: SettingsEntity] -> cRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    MaterializeChunked [cRows -> cRowsBuffer]
    ForEach [ab in _cteRowResults.Slot0]
      Let [abaToken: string = ab.a.Token]
      Let [abbToken: string = ab.b.Token]
      ForEach [c in cRowsBuffer]
        AppendShape [result <- ResultRow0(Token: abaToken, Token: abbToken, Token: c.Token)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q322_SpecTableSettingsProfiles
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
            new Column("Token", typeof(string), 0),
            new Column("Token", typeof(string), 1),
            new Column("Token", typeof(string), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("a.Token", typeof(string), 0),
            new Column("b.Token", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Token", typeof(string), 0) });
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
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    var __cSchema = provider.GetSchema("#settings");
                    var cRowsSource = SourceInvocation<Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTests.SettingsEntity>.Open(__cSchema, "items", new SourceExecutionContext("c:1", sourceExecutionPlans["c:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["c:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var cRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTests.SettingsEntity>(cRowsSource.Chunks, __musoqProgressContext, "c:1") : cRowsSource.Chunks;
                    var cRowsBuffer = EvaluationHelper.MaterializeChunkedRows(cRows);
                    var __storedTable0Rows = _cteRowResults.Slot0;
                    for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                    {
                        if ((__storedTable0Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];
                        string abaToken = ab.a_Token;
                        string abbToken = ab.b_Token;
                        foreach (var c in cRowsBuffer)
                        {
                            token.ThrowIfCancellationRequested();
                            __musoqFinalShapeRows.Add(new ResultRow0(abaToken, abbToken, c.Token));
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
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
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_aSchema = provider.GetSchema("#settings");
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTests.SettingsEntity>.Open(__statement0_aSchema, "items", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTests.SettingsEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#settings");
            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTests.SettingsEntity>.Open(__statement0_bSchema, "items", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTests.SettingsEntity>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var bRowsBuffer = EvaluationHelper.MaterializeChunkedRows(statement0_bRows);
            foreach (var aChunk in statement0_aRows)
            {
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTests.SettingsEntity> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTests.SettingsEntity[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            string aToken = a.Token;
                            foreach (var b in bRowsBuffer)
                            {
                                token.ThrowIfCancellationRequested();
                                statement0.Add(new Statement0Row0(aToken, b.Token));
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTests.SettingsEntity> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            string aToken = a.Token;
                            foreach (var b in bRowsBuffer)
                            {
                                token.ThrowIfCancellationRequested();
                                statement0.Add(new Statement0Row0(aToken, b.Token));
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
                    string aToken = a.Token;
                    foreach (var b in bRowsBuffer)
                    {
                        token.ThrowIfCancellationRequested();
                        statement0.Add(new Statement0Row0(aToken, b.Token));
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
            public ResultRow0(string __value0, string __value1, string __value2)
            {
                Token = __value0;
                Token_1 = __value1;
                Token_2 = __value2;
            }

            public override int Count => 3;
            public string Token { get; private set; }
            public string Token_1 { get; private set; }
            public string Token_2 { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Token = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Token_1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Token_2 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Token),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Token_1),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Token_2),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Token" => true,
                "Token_1" => true,
                "Token_2" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Token, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Token_1, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Token_2, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1)
            {
                a_Token = __value0;
                b_Token = __value1;
            }

            public string a_Token { get; }
            public string b_Token { get; }
        }
    }
}
