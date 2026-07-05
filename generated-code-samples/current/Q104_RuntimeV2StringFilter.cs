/*
raw query string

SELECT FirstName, LastName, Email
              FROM #test.entities()
              WHERE Contains(Email, 'gmail') AND StartsWith(FirstName, 'A')
*/

/*
logical plan representation string

MultiStatement
  Project [ko3iko.FirstName as FirstName, ko3iko.LastName as LastName, ko3iko.Email as Email]
    Filter [((Contains(ko3iko.Email, 'gmail') IS NOT NULL AND (Contains(ko3iko.Email, 'gmail') = TRUE)) AND (StartsWith(ko3iko.FirstName, 'A') IS NOT NULL AND (StartsWith(ko3iko.FirstName, 'A') = TRUE)))]
      SchemaScan [#test.entities() as ko3iko]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [ko3iko.FirstName as FirstName, ko3iko.LastName as LastName, ko3iko.Email as Email]
    PhysicalFilter [((Contains(ko3iko.Email, 'gmail') IS NOT NULL AND (Contains(ko3iko.Email, 'gmail') = TRUE)) AND (StartsWith(ko3iko.FirstName, 'A') IS NOT NULL AND (StartsWith(ko3iko.FirstName, 'A') = TRUE)))]
      PhysicalSchemaScan [#test.entities() as ko3iko] [pushdown: Contains(ko3iko.Email, 'gmail'), StartsWith(ko3iko.FirstName, 'A')]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2RegressionEntity]
      FirstName: string <- property FirstName
      LastName: string <- property LastName
      Email: string <- property Email
    Generated [ResultRow0]
      FirstName: string <- field FirstName
      LastName: string <- field LastName
      Email: string <- field Email

  Body
    SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ParallelFilterProjectLoop [ko3iko in ko3ikoRows where ((Contains(ko3iko.Email, 'gmail') IS NOT NULL AND (Contains(ko3iko.Email, 'gmail') = TRUE)) AND (StartsWith(ko3iko.FirstName, 'A') IS NOT NULL AND (StartsWith(ko3iko.FirstName, 'A') = TRUE))); threshold 4096, maxDegree 24]
      ParallelProject
        Let [email: string = ko3iko.Email]
        Let [firstName: string = ko3iko.FirstName]
        Let [contains: bool? = Contains(email, 'gmail')]
        If [((contains IS NOT NULL AND (contains = TRUE)) AND (StartsWith(firstName, 'A') IS NOT NULL AND (StartsWith(firstName, 'A') = TRUE)))]
          AppendShape [result <- ResultShape0(FirstName: firstName, LastName: ko3iko.LastName, Email: email)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q104_RuntimeV2StringFilter
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
            new Column("FirstName", typeof(string), 0),
            new Column("LastName", typeof(string), 1),
            new Column("Email", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("FirstName", typeof(string), 2), new Column("LastName", typeof(string), 3), new Column("Email", typeof(string), 4) });
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
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.Select);
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
            var __ko3ikoSchema = provider.GetSchema("#test");
            var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var ko3ikoRows = ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            if (__musoqTableSourceRows is not IReadOnlyList<IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>> _)
            {
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) =>
                {
                    string email = ko3iko.Email;
                    string firstName = ko3iko.FirstName;
                    bool? contains = ((email == null || "gmail" == null) ? (bool?)null : email.Contains("gmail", StringComparison.OrdinalIgnoreCase));
                    if ((((contains != null) && (contains == true)) && ((((firstName == null || "A" == null) ? (bool?)null : firstName.StartsWith("A", StringComparison.OrdinalIgnoreCase)) != null) && (((firstName == null || "A" == null) ? (bool?)null : firstName.StartsWith("A", StringComparison.OrdinalIgnoreCase)) == true))))
                    {
                        return new ResultRow0(firstName, ko3iko.LastName, email);
                    }

                    return null;
                }, token), token, onCompleted: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }, onDisposed: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                });
            }

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) =>
            {
                string email = ko3iko.Email;
                string firstName = ko3iko.FirstName;
                bool? contains = ((email == null || "gmail" == null) ? (bool?)null : email.Contains("gmail", StringComparison.OrdinalIgnoreCase));
                if ((((contains != null) && (contains == true)) && ((((firstName == null || "A" == null) ? (bool?)null : firstName.StartsWith("A", StringComparison.OrdinalIgnoreCase)) != null) && (((firstName == null || "A" == null) ? (bool?)null : firstName.StartsWith("A", StringComparison.OrdinalIgnoreCase)) == true))))
                {
                    return new ResultRow0(firstName, ko3iko.LastName, email);
                }

                return null;
            }, token)), token, onCompleted: () =>
            {
                OnPhaseChanged("compiled", QueryPhase.End);
            }, onDisposed: () =>
            {
                OnPhaseChanged("compiled", QueryPhase.End);
            });
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
            public ResultRow0(string __value0, string __value1, string __value2)
            {
                FirstName = __value0;
                LastName = __value1;
                Email = __value2;
            }

            public override int Count => 3;
            public string Email { get; private set; }
            public string FirstName { get; private set; }
            public string LastName { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        FirstName = (string)value;
                        break;
                    case 1:
                        LastName = (string)value;
                        break;
                    case 2:
                        Email = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "FirstName" => true,
                "LastName" => true,
                "Email" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)FirstName,
                1 => (object)LastName,
                2 => (object)Email,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "FirstName" => (object)FirstName,
                "LastName" => (object)LastName,
                "Email" => (object)Email,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string FirstName, string LastName, string Email)
            {
                this.FirstName = FirstName;
                this.LastName = LastName;
                this.Email = Email;
            }

            public string Email { get; }
            public string FirstName { get; }
            public string LastName { get; }
        }
    }
}
