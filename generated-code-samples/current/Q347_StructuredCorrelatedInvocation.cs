// === Parsed Query ===
/*
param(suffix: string = '!')

select m.PatternId
from values {
    (Pattern: 'TODO'),
} p
cross apply #inputs.match(
    'TODO!',
    patterns: array {
        (Id: 'todo', Pattern: p.Pattern + $suffix),
    }
) m
*/

// === Logical Plan ===
/*
MultiStatement
  Project [p.Pattern as p.Pattern, m.PatternId as m.PatternId]
    Apply [Cross]
      ValuesScan [1 rows as p]
      SchemaScan [#inputs.match('TODO!', array { (Id: 'todo', Pattern: (p.Pattern || $suffix)) }) as m]
  Project [m.PatternId as m.PatternId]
    CteRef [pm as pm]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [p.Pattern as p.Pattern, m.PatternId as m.PatternId]
    PhysicalNestedLoopApply [Cross]
      PhysicalValuesScan [1 rows as p]
      PhysicalSchemaScan [#inputs.match('TODO!', array { (Id: 'todo', Pattern: (p.Pattern || $suffix)) }) as m]
  PhysicalProject [m.PatternId as m.PatternId]
    PhysicalCteRef [pm as pm]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Pattern: string <- field Pattern
    SourceEntity [m: PatternMatchRow]
      PatternId: string <- property PatternId
    Generated [Statement0Row0]
      p.Pattern: string <- field p_Pattern
      m.PatternId: string <- field m_PatternId
    TableRow [pm]
      p.Pattern: string <- field p_Pattern
      m.PatternId: string <- field m_PatternId
    Generated [ResultRow0]
      m.PatternId: string <- field m_PatternId
  StructuralPreparation
    Musoq.Examples.DataSources.StructuredInputs.PatternInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.PatternInput@Musoq.Examples.DataSources.StructuredInputs(primitive:string,primitive:string,primitive:string); shape=(Id: string?, Pattern: string?); origin=Inline; lifetime=Inline; metrics=Runtime; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-,ExecutionLiteral { ReturnType = string, Value = string:006C00690074006500720061006C }

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    CreateValuesRows [statement0_pRows: pValues54644649Row0 x 1]
    CreateTable [statement0: Statement0Row0]
    ForEach [p in statement0_pRows]
      Let [pPattern: string = p.Pattern]
      Let [__musoqStructural_statement0_m_0: string = 'todo']
      Let [__musoqStructural_statement0_m_1: string = (pPattern || $suffix)]
      PrepareStructuralInput [__musoqStructural_statement0_m_2: Musoq.Examples.DataSources.StructuredInputs.PatternInput <- (Id: __musoqStructural_statement0_m_0, Pattern: __musoqStructural_statement0_m_1); lifetime Inline; shape (Id: string?, Pattern: string?)]
      Let [__musoqStructural_statement0_m_3: IReadOnlyList<PatternInput> = array { __musoqStructural_statement0_m_2 }]
      SourceScan [m: PatternMatchRow] -> statement0_mRows
      ChunkedForEach [m in statement0_mRows]
        AppendRow [statement0 <- Statement0Row0(p.Pattern: pPattern, m.PatternId: m.PatternId)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [pm in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(m.PatternId: pm.m.PatternId)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q347_StructuredCorrelatedInvocation
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("m.PatternId", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_statement0_0 = new Column[]
        {
            new Column("p.Pattern", typeof(string), 0),
            new Column("m.PatternId", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_m_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("PatternId", typeof(string), 0) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("suffix", "string", "string", typeof(string), false, false, null, null, true, ScriptParameterDefaultKind.Literal, "!")
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("suffix", "string", "string", typeof(string), false, false, null, null, true, ScriptParameterDefaultKind.Literal, "!"))
        };
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.m_PatternId);
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
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                var paramSuffix = ScriptParameterBinder.GetOptional<string>(__musoqExecutionState.Parameters, "suffix", "!");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, new string[] { "suffix" });
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, paramSuffix);
                OnPhaseChanged("compiled", QueryPhase.Select);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 pm = __storedTable0Rows[__storedTable0Index];
                    __musoqFinalShapeRows.Add(new ResultShape0(pm.m_PatternId));
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
            pValues54644649Row0[] statement0_pRows = new pValues54644649Row0[]
            {
                new pValues54644649Row0("TODO")
            };
            var statement0 = new List<Statement0Row0>();
            foreach (var p in statement0_pRows)
            {
                token.ThrowIfCancellationRequested();
                string pPattern = p.Pattern;
                string __musoqStructural_statement0_m_0 = "todo";
                string __musoqStructural_statement0_m_1 = (pPattern + paramSuffix);
                Musoq.Examples.DataSources.StructuredInputs.PatternInput __musoqStructural_statement0_m_2 = new Musoq.Examples.DataSources.StructuredInputs.PatternInput(__musoqStructural_statement0_m_0, __musoqStructural_statement0_m_1, "literal");
                IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqStructural_statement0_m_3 = new Musoq.Examples.DataSources.StructuredInputs.PatternInput[]
                {
                    __musoqStructural_statement0_m_2
                };
                var __statement0_mSchema = provider.GetSchema("#inputs");
                var statement0_mRowsSourceContext = new SourceExecutionContext("m:1", sourceExecutionPlans["m:1"], token, __schemaColumns_compiled_m_1, sourceRuntimeSettingsBySourceContextId["m:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> statement0_mRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.MatchSource statement0_mRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MatchSource("TODO!", __musoqCheckStructural_389b095bd9c4e750(__musoqStructural_statement0_m_3, token), statement0_mRowsSourceContext);
                    statement0_mRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.MatchSource, Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(__statement0_mSchema, "match", statement0_mRowsSourceInstance, statement0_mRowsSourceContext, "#inputs", "m", "m:1");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (global::Musoq.Schema.Exceptions.DataSourceLifecycleException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "match", "m", "m:1", exception);
                }

                var statement0_mRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(statement0_mRowsSource.Chunks, __musoqProgressContext, "m:1") : statement0_mRowsSource.Chunks;
                foreach (var mChunk in statement0_mRows)
                {
                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mChunkView)
                    {
                        if (mChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow[] mChunkViewArray)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                statement0.Add(new Statement0Row0(pPattern, m.PatternId));
                            }

                            continue;
                        }

                        if (mChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mChunkViewList)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                statement0.Add(new Statement0Row0(pPattern, m.PatternId));
                            }

                            continue;
                        }
                    }

                    for (int mIndex = 0, mIndexCount = mChunk.Count; mIndex < mIndexCount; ++mIndex)
                    {
                        if ((mIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var m = mChunk[mIndex];
                        statement0.Add(new Statement0Row0(pPattern, m.PatternId));
                    }
                }
            }

            return statement0;
        }

        private static IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqCheckStructural_389b095bd9c4e750(IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            if (value != null)
            {
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                    if (__musoqStructural_collection1[__musoqStructural_index0].Id != null)
                        __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_collection1[__musoqStructural_index0].Id.Length * 2L));
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                    if (__musoqStructural_collection1[__musoqStructural_index0].Pattern != null)
                        __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_collection1[__musoqStructural_index0].Pattern.Length * 2L));
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                    if (__musoqStructural_collection1[__musoqStructural_index0].Mode != null)
                        __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_collection1[__musoqStructural_index0].Mode.Length * 2L));
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "m.argument[1]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "m:1");
            return value;
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0)
            {
                m_PatternId = __value0;
            }

            public override int Count => 1;
            public string m_PatternId { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        m_PatternId = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "m.PatternId" => true,
                "m_PatternId" => true,
                "PatternId" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)m_PatternId,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "m.PatternId" => (object)m_PatternId,
                "m_PatternId" => (object)m_PatternId,
                "PatternId" => (object)m_PatternId,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string m_PatternId)
            {
                this.m_PatternId = m_PatternId;
            }

            public string m_PatternId { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1)
            {
                p_Pattern = __value0;
                m_PatternId = __value1;
            }

            public string m_PatternId { get; }
            public string p_Pattern { get; }
        }

        private sealed class pValues54644649Row0 : Row
        {
            public pValues54644649Row0(string __value0)
            {
                Pattern = __value0;
            }

            public override int Count => 1;
            public string Pattern { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Pattern = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Pattern" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Pattern,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Pattern" => (object)Pattern,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
