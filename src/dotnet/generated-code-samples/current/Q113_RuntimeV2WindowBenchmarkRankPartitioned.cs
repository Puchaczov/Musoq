// === Parsed Query ===
/*
SELECT Name,
                     Department,
                     Rank() over (partition by Department order by Salary desc) as rn
              FROM #test.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Name as Name, ko3iko.Department as Department, WindowRef(0) as rn]
    Window [Rank(idx:0; partition: ko3iko.Department; order: ko3iko.Salary DESC)]
      SchemaScan [#test.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as Name, ko3iko.Department as Department, WindowRef(0) as rn]
    PhysicalWindow [Rank(idx:0; partition: ko3iko.Department; order: ko3iko.Salary DESC)]
      PhysicalMaterialize
        PhysicalSchemaScan [#test.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2RegressionEntity]
      Name: string <- property Name
      Department: string <- property Department
      Salary: int <- property Salary
    Generated [ResultRow0]
      Name: string <- field Name
      Department: string <- field Department
      rn: long <- field rn

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeRankWindow [resultRanks <- resultWindowRows partition by ko3iko.Department order by ko3iko.Salary DESC]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultRow0(Name: ko3iko.Name, Department: ko3iko.Department, rn: resultRanks[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q113_RuntimeV2WindowBenchmarkRankPartitioned
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Department", typeof(string), 1),
            new Column("rn", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Department", typeof(string), 1), new Column("Salary", typeof(int), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#test");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultRanksIntOrderBuilder = new Musoq.Evaluator.Helpers.WindowIntOrderBuilder<string>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity ko3iko = resultWindowRows[windowIndex];
                    resultRanksIntOrderBuilder.Add((string)ko3iko.Department, (int)ko3iko.Salary, windowIndex);
                }

                var resultRanksPartitions = resultRanksIntOrderBuilder.ToSortedPartitionSet(true);
                var resultRanksOrderKeys = resultRanksIntOrderBuilder.OrderKeys;
                var resultRanks = new long[resultWindowRows.Count];
                for (int resultRanksPartitionSetIndex = 0; resultRanksPartitionSetIndex < resultRanksPartitions.PartitionCount; ++resultRanksPartitionSetIndex)
                {
                    var resultRanksPartitionStart = resultRanksPartitions.GetStart(resultRanksPartitionSetIndex);
                    var resultRanksPartitionCount = resultRanksPartitions.GetLength(resultRanksPartitionSetIndex);
                    var resultRanksPartitionIndices = resultRanksPartitions.Indices;
                    long resultRanksDenseRank = 1L;
                    for (int resultRanksPeerStart = 0; resultRanksPeerStart < resultRanksPartitionCount;)
                    {
                        var resultRanksCurrentIndex = resultRanksPartitionIndices[resultRanksPartitionStart + resultRanksPeerStart];
                        var resultRanksPeerEnd = resultRanksPeerStart;
                        while (resultRanksPeerEnd + 1 < resultRanksPartitionCount)
                        {
                            var resultRanksCandidateIndex = resultRanksPartitionIndices[resultRanksPartitionStart + resultRanksPeerEnd + 1];
                            if (!System.Collections.Generic.EqualityComparer<int>.Default.Equals(resultRanksOrderKeys[resultRanksCurrentIndex], resultRanksOrderKeys[resultRanksCandidateIndex]))
                                break;
                            resultRanksPeerEnd++;
                        }

                        for (int resultRanksPeerIndex = resultRanksPeerStart; resultRanksPeerIndex <= resultRanksPeerEnd; ++resultRanksPeerIndex)
                        {
                            resultRanksCurrentIndex = resultRanksPartitionIndices[resultRanksPartitionStart + resultRanksPeerIndex];
                            var resultRanksContext = new Musoq.Plugins.WindowRankingContext(resultRanksPeerIndex, resultRanksPeerStart, resultRanksPeerEnd, resultRanksDenseRank, resultRanksPartitionCount, token);
                            resultRanks[resultRanksCurrentIndex] = Musoq.Plugins.RankKernel.Get(in resultRanksContext);
                        }

                        resultRanksDenseRank++;
                        resultRanksPeerStart = resultRanksPeerEnd + 1;
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(resultWindowRows.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity ko3iko = resultWindowRows[windowIndex];
                    __musoqFinalShapeRows.Add(new ResultRow0(ko3iko.Name, ko3iko.Department, (long)resultRanks[windowIndex]));
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, long __value2)
            {
                Name = __value0;
                Department = __value1;
                rn = __value2;
            }

            public override int Count => 3;
            public string Department { get; private set; }
            public string Name { get; private set; }
            public long rn { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Department = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.rn = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Department),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.rn),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Department" => true,
                "rn" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Department, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.rn, ref visitor);
            }
        }
    }
}
