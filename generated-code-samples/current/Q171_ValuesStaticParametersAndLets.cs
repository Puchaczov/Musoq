// === Parsed Query ===
/*
param(baseScore: int, suffix: string = '-ok')
              let bonus: int = 5
              from values {
                  { Name: 'first' + $suffix, Score: $baseScore },
                  { Name: 'second' + $suffix, Score: $baseScore + $bonus }
              } scores
              select scores.Name, scores.Score
*/

// === Logical Plan ===
/*
MultiStatement
  Project [scores.Name as scores.Name, scores.Score as scores.Score]
    ValuesScan [2 rows as scores]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [scores.Name as scores.Name, scores.Score as scores.Score]
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
      scores.Name: string <- field scores_Name
      scores.Score: int <- field scores_Score

  Body
    CreateValuesRows [scoresRows: scoresValues946DD734Row0 x 2]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [scores in scoresRows]
      AppendShape [result <- ResultShape0(scores.Name: scores.Name, scores.Score: scores.Score)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("scores.Name", typeof(string), 0),
            new Column("scores.Score", typeof(int), 1)
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
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

        public event DataSourceEventHandler DataSourceProgress;
        public event QueryPhaseEventHandler PhaseChanged;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.scores_Name, __musoqShapeRow.scores_Score);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                var paramBaseScore = ScriptParameterBinder.GetRequired<int>(__musoqExecutionState.Parameters, "baseScore");
                var paramSuffix = ScriptParameterBinder.GetOptional<string>(__musoqExecutionState.Parameters, "suffix", "-ok");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, new string[] { "baseScore", "suffix" });
                const int letBonus = 5;
                scoresValues946DD734Row0[] scoresRows = new scoresValues946DD734Row0[]
                {
                    new scoresValues946DD734Row0(("first" + paramSuffix), paramBaseScore),
                    new scoresValues946DD734Row0(("second" + paramSuffix), (paramBaseScore + letBonus))
                };
                foreach (var scores in scoresRows)
                {
                    token.ThrowIfCancellationRequested();
                    yield return new ResultShape0(scores.Name, scores.Score);
                }
            }
            finally
            {
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
            public ResultRow0(string __value0, int __value1)
            {
                scores_Name = __value0;
                scores_Score = __value1;
            }

            public override int Count => 2;
            public string scores_Name { get; private set; }
            public int scores_Score { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        scores_Name = (string)value;
                        break;
                    case 1:
                        scores_Score = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "scores.Name" => true,
                "scores_Name" => true,
                "Name" => true,
                "scores.Score" => true,
                "scores_Score" => true,
                "Score" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)scores_Name,
                1 => (object)scores_Score,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "scores.Name" => (object)scores_Name,
                "scores_Name" => (object)scores_Name,
                "Name" => (object)scores_Name,
                "scores.Score" => (object)scores_Score,
                "scores_Score" => (object)scores_Score,
                "Score" => (object)scores_Score,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string scores_Name, int scores_Score)
            {
                this.scores_Name = scores_Name;
                this.scores_Score = scores_Score;
            }

            public string scores_Name { get; }
            public int scores_Score { get; }
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Score = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Score" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Score,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Score" => (object)Score,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
