// === Parsed Query ===
/*
from values {
                  { Name: 'Newtonsoft.Json', Approved: true, Score: 10ui },
                  { Name: 'Legacy.Package', Approved: false, Score: 20ui }
              } packages
              where packages.Approved = false
              select packages.Name, packages.Score
*/

// === Logical Plan ===
/*
MultiStatement
  Project [packages.Name as packages.Name, packages.Score as packages.Score]
    Filter [(packages.Approved = FALSE)]
      ValuesScan [2 rows as packages]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [packages.Name as packages.Name, packages.Score as packages.Score]
    PhysicalFilter [(packages.Approved = FALSE)]
      PhysicalValuesScan [2 rows as packages]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Name: string <- field Name
      Approved: bool <- field Approved
      Score: uint <- field Score
    Generated [ResultRow0]
      packages.Name: string <- field packages_Name
      packages.Score: uint <- field packages_Score

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    CreateValuesRows [packagesRows: packagesValuesF78867FBRow0 x 2]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ForEach [packages in packagesRows]
      If [(packages.Approved = FALSE)]
        AppendShape [result <- ResultShape0(packages.Name: packages.Name, packages.Score: packages.Score)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q117_ValuesRowLiterals
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
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("packages.Name", typeof(string), 0),
            new Column("packages.Score", typeof(uint), 1)
        };
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
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.packages_Name, __musoqShapeRow.packages_Score);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                packagesValuesF78867FBRow0[] packagesRows = new packagesValuesF78867FBRow0[]
                {
                    new packagesValuesF78867FBRow0("Newtonsoft.Json", true, 10u),
                    new packagesValuesF78867FBRow0("Legacy.Package", false, 20u)
                };
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var packages in packagesRows)
                {
                    token.ThrowIfCancellationRequested();
                    if ((packages.Approved == false))
                    {
                        yield return new ResultShape0(packages.Name, packages.Score);
                    }
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
            public ResultRow0(string __value0, uint __value1)
            {
                packages_Name = __value0;
                packages_Score = __value1;
            }

            public override int Count => 2;
            public string packages_Name { get; private set; }
            public uint packages_Score { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        packages_Name = (string)value;
                        break;
                    case 1:
                        packages_Score = (uint)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "packages.Name" => true,
                "packages_Name" => true,
                "Name" => true,
                "packages.Score" => true,
                "packages_Score" => true,
                "Score" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)packages_Name,
                1 => (object)packages_Score,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "packages.Name" => (object)packages_Name,
                "packages_Name" => (object)packages_Name,
                "Name" => (object)packages_Name,
                "packages.Score" => (object)packages_Score,
                "packages_Score" => (object)packages_Score,
                "Score" => (object)packages_Score,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string packages_Name, uint packages_Score)
            {
                this.packages_Name = packages_Name;
                this.packages_Score = packages_Score;
            }

            public string packages_Name { get; }
            public uint packages_Score { get; }
        }

        private sealed class packagesValuesF78867FBRow0 : Row
        {
            public packagesValuesF78867FBRow0(string __value0, bool __value1, uint __value2)
            {
                Name = __value0;
                Approved = __value1;
                Score = __value2;
            }

            public bool Approved { get; private set; }
            public override int Count => 3;
            public string Name { get; private set; }
            public uint Score { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Approved = (bool)value;
                        break;
                    case 2:
                        Score = (uint)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Approved" => true,
                "Score" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Approved,
                2 => (object)Score,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Approved" => (object)Approved,
                "Score" => (object)Score,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
