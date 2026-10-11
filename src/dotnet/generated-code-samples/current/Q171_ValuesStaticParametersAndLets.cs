// === Parsed Query ===
/*
param(baseScore: int, suffix: string = '-ok')
              let bonus: int = 5
              from values {
                  ( Name: 'first' + $suffix, Score: $baseScore ),
                  ( Name: 'second' + $suffix, Score: $baseScore + $bonus )
              } scores
              select scores.Name, scores.Score
*/

// === Logical Plan ===
/*
MultiStatement
  Project [scores.Name as Name, scores.Score as Score]
    ValuesScan [2 rows as scores]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [scores.Name as Name, scores.Score as Score]
    PhysicalValuesScan [2 rows as scores]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Name: string <- field Name
      Score: int <- field Score
    Generated [ResultRow0]
      Name: string <- field Name
      Score: int <- field Score

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    CreateValuesRows [scoresRows: scoresValues946DD734Row0 x 2]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [scores in scoresRows]
      AppendShape [result <- ResultRow0(Name: scores.Name, Score: scores.Score)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q171_ValuesStaticParametersAndLets
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
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Score", typeof(int), 1)
        };
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("baseScore", "int", "int", typeof(int), false, false, null, null, false, ScriptParameterDefaultKind.None, null),
            new ScriptParameterContract("suffix", "string", "string", typeof(string), false, false, null, null, true, ScriptParameterDefaultKind.Literal, "-ok")
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("baseScore", "int", "int", typeof(int), false, false, null, null, false, ScriptParameterDefaultKind.None, null)),
            new ScriptParameterDefinition(new ScriptParameterContract("suffix", "string", "string", typeof(string), false, false, null, null, true, ScriptParameterDefaultKind.Literal, "-ok"))
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var paramBaseScore = ScriptParameterBinder.GetRequired<int>(__musoqExecutionState, "baseScore");
                var paramSuffix = ScriptParameterBinder.GetOptional<string>(__musoqExecutionState, "suffix", "-ok");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "baseScore", "suffix" });
                const int letBonus = 5;
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                scoresValues946DD734Row0[] scoresRows = new scoresValues946DD734Row0[]
                {
                    new scoresValues946DD734Row0(("first" + paramSuffix), paramBaseScore),
                    new scoresValues946DD734Row0(("second" + paramSuffix), (paramBaseScore + letBonus))
                };
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var scores in scoresRows)
                {
                    token.ThrowIfCancellationRequested();
                    yield return new ResultRow0(scores.Name, scores.Score);
                }
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
            public ResultRow0(string __value0, int __value1)
            {
                Name = __value0;
                Score = __value1;
            }

            public override int Count => 2;
            public string Name { get; private set; }
            public int Score { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Score = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Score),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Score" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Score, ref visitor);
            }
        }

        private sealed class scoresValues946DD734Row0 : Row
        {
            public scoresValues946DD734Row0(string __value0, int __value1)
            {
                Name = __value0;
                Score = __value1;
            }

            public override int Count => 2;
            public string Name { get; private set; }
            public int Score { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Score = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Score),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Score" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Score, ref visitor);
            }
        }
    }
}
