// === Parsed Query ===
/*
SELECT FirstName, LastName, Email
              FROM #test.entities()
              SKIP 100 TAKE 100
*/

// === Logical Plan ===
/*
MultiStatement
  Take [100]
    Skip [100]
      Project [ko3iko.FirstName as FirstName, ko3iko.LastName as LastName, ko3iko.Email as Email]
        SchemaScan [#test.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalTake [100]
    PhysicalSkip [100]
      PhysicalProject [ko3iko.FirstName as FirstName, ko3iko.LastName as LastName, ko3iko.Email as Email]
        PhysicalSchemaScan [#test.entities() as ko3iko]
*/

// === Execution Plan ===
/*
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
    Let [__resultSkipRemaining: int = 100]
    Let [__resultTakeRemaining: int = 100]
    ChunkedForEach [ko3iko in ko3ikoRows]
      If [(__resultSkipRemaining > 0)]
        Assign [__resultSkipRemaining = (__resultSkipRemaining - 1)]
        Continue
      If [(__resultTakeRemaining <= 0)]
        Break
      AppendShape [result <- ResultShape0(FirstName: ko3iko.FirstName, LastName: ko3iko.LastName, Email: ko3iko.Email)]
      Assign [__resultTakeRemaining = (__resultTakeRemaining - 1)]
      If [(__resultTakeRemaining <= 0)]
        Break
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q103_RuntimeV2SkipTakeNoOrder
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
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.FirstName, __musoqShapeRow.LastName, __musoqShapeRow.Email);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __ko3ikoSchema = provider.GetSchema("#test");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = ko3ikoRowsSource.Chunks;
                int __resultSkipRemaining = 100;
                int __resultTakeRemaining = 100;
                {
                    foreach (var ko3ikoChunk in ko3ikoRows)
                    {
                        if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity> ko3ikoChunkView)
                        {
                            if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity[] ko3ikoChunkViewArray)
                            {
                                int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                {
                                    if ((ko3ikoIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                    if ((__resultSkipRemaining > 0))
                                    {
                                        __resultSkipRemaining = (__resultSkipRemaining - 1);
                                        continue;
                                    }

                                    if ((__resultTakeRemaining <= 0))
                                    {
                                        goto __ko3ikoChunkLoopEnd;
                                    }

                                    yield return new ResultShape0(ko3iko.FirstName, ko3iko.LastName, ko3iko.Email);
                                    __resultTakeRemaining = (__resultTakeRemaining - 1);
                                    if ((__resultTakeRemaining <= 0))
                                    {
                                        goto __ko3ikoChunkLoopEnd;
                                    }
                                }

                                continue;
                            }

                            if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity> ko3ikoChunkViewList)
                            {
                                int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                {
                                    if ((ko3ikoIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                    if ((__resultSkipRemaining > 0))
                                    {
                                        __resultSkipRemaining = (__resultSkipRemaining - 1);
                                        continue;
                                    }

                                    if ((__resultTakeRemaining <= 0))
                                    {
                                        goto __ko3ikoChunkLoopEnd;
                                    }

                                    yield return new ResultShape0(ko3iko.FirstName, ko3iko.LastName, ko3iko.Email);
                                    __resultTakeRemaining = (__resultTakeRemaining - 1);
                                    if ((__resultTakeRemaining <= 0))
                                    {
                                        goto __ko3ikoChunkLoopEnd;
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
                            if ((__resultSkipRemaining > 0))
                            {
                                __resultSkipRemaining = (__resultSkipRemaining - 1);
                                continue;
                            }

                            if ((__resultTakeRemaining <= 0))
                            {
                                goto __ko3ikoChunkLoopEnd;
                            }

                            yield return new ResultShape0(ko3iko.FirstName, ko3iko.LastName, ko3iko.Email);
                            __resultTakeRemaining = (__resultTakeRemaining - 1);
                            if ((__resultTakeRemaining <= 0))
                            {
                                goto __ko3ikoChunkLoopEnd;
                            }
                        }
                    }

                    __ko3ikoChunkLoopEnd:
                        ;
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
