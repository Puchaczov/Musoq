// === Parsed Query ===
/*
select Name, City, Rank() over (partition by City order by Population desc) as rnk, DenseRank() over (partition by City order by Population desc) as dense_rnk from #A.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Name as Name, ko3iko.City as City, WindowRef(0) as rnk, WindowRef(1) as dense_rnk]
    Window [Rank(idx:0; partition: ko3iko.City; order: ko3iko.Population DESC), DenseRank(idx:1; partition: ko3iko.City; order: ko3iko.Population DESC)]
      SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as Name, ko3iko.City as City, WindowRef(0) as rnk, WindowRef(1) as dense_rnk]
    PhysicalWindow [Rank(idx:0; partition: ko3iko.City; order: ko3iko.Population DESC), DenseRank(idx:1; partition: ko3iko.City; order: ko3iko.Population DESC)]
      PhysicalMaterialize
        PhysicalSchemaScan [#A.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Population: decimal <- property Population
    Generated [ResultRow0]
      Name: string <- field Name
      City: string <- field City
      rnk: long <- field rnk
      dense_rnk: long <- field dense_rnk

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    WindowKernelPlan [hash partition/per-partition sort; kernels 2; ranking|resultWindowRows|resultRanks0Partitions|resultRanks0Partitions|resultRanks0PartitionKeys|resultRanks0OrderKeys]
      ComputeRankWindow [resultRanks0 <- resultWindowRows partition by ko3iko.City order by ko3iko.Population DESC]
      ComputeDenseRankWindow [resultDenseRanks1 <- resultWindowRows partition by ko3iko.City order by ko3iko.Population DESC]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultRow0(Name: ko3iko.Name, City: ko3iko.City, rnk: resultRanks0[windowIndex], dense_rnk: resultDenseRanks1[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q26_WindowRankDenseRank
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
            new Column("City", typeof(string), 1),
            new Column("rnk", typeof(long), 2),
            new Column("dense_rnk", typeof(long), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1), new Column("Population", typeof(decimal), 2) });
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
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultRanks0PartitionKeys = new string[resultWindowRows.Count];
                var resultRanks0OrderKeys = new WindowResultRanks0OrderKeysKey[resultWindowRows.Count];
                ExtractResultRanks0WindowKeys(resultWindowRows, resultRanks0PartitionKeys, resultRanks0OrderKeys);
                var resultRanks0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultRanks0PartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRanks0Partitions, resultRanks0OrderKeys, false);
                var resultRanks0 = new long[resultWindowRows.Count];
                var resultDenseRanks1 = new long[resultWindowRows.Count];
                for (int resultRanks0WindowPlanPartitionSetIndex = 0; resultRanks0WindowPlanPartitionSetIndex < resultRanks0Partitions.PartitionCount; ++resultRanks0WindowPlanPartitionSetIndex)
                {
                    var resultRanks0WindowPlanPartitionStart = resultRanks0Partitions.GetStart(resultRanks0WindowPlanPartitionSetIndex);
                    var resultRanks0WindowPlanPartitionCount = resultRanks0Partitions.GetLength(resultRanks0WindowPlanPartitionSetIndex);
                    var resultRanks0WindowPlanPartitionIndices = resultRanks0Partitions.Indices;
                    long resultRanks0DenseRank = 1L;
                    for (int resultRanks0PeerStart = 0; resultRanks0PeerStart < resultRanks0WindowPlanPartitionCount;)
                    {
                        var resultRanks0CurrentIndex = resultRanks0WindowPlanPartitionIndices[resultRanks0WindowPlanPartitionStart + resultRanks0PeerStart];
                        var resultRanks0PeerEnd = resultRanks0PeerStart;
                        while (resultRanks0PeerEnd + 1 < resultRanks0WindowPlanPartitionCount)
                        {
                            var resultRanks0CandidateIndex = resultRanks0WindowPlanPartitionIndices[resultRanks0WindowPlanPartitionStart + resultRanks0PeerEnd + 1];
                            if (!resultRanks0OrderKeys[resultRanks0CurrentIndex].PeerEquals(resultRanks0OrderKeys[resultRanks0CandidateIndex]))
                                break;
                            resultRanks0PeerEnd++;
                        }

                        for (int resultRanks0PeerIndex = resultRanks0PeerStart; resultRanks0PeerIndex <= resultRanks0PeerEnd; ++resultRanks0PeerIndex)
                        {
                            resultRanks0CurrentIndex = resultRanks0WindowPlanPartitionIndices[resultRanks0WindowPlanPartitionStart + resultRanks0PeerIndex];
                            var resultRanks0Context = new Musoq.Plugins.WindowRankingContext(resultRanks0PeerIndex, resultRanks0PeerStart, resultRanks0PeerEnd, resultRanks0DenseRank, resultRanks0WindowPlanPartitionCount, token);
                            resultRanks0[resultRanks0CurrentIndex] = Musoq.Plugins.RankKernel.Get(in resultRanks0Context);
                            var resultDenseRanks1Context = new Musoq.Plugins.WindowRankingContext(resultRanks0PeerIndex, resultRanks0PeerStart, resultRanks0PeerEnd, resultRanks0DenseRank, resultRanks0WindowPlanPartitionCount, token);
                            resultDenseRanks1[resultRanks0CurrentIndex] = Musoq.Plugins.DenseRankKernel.Get(in resultDenseRanks1Context);
                        }

                        resultRanks0DenseRank++;
                        resultRanks0PeerStart = resultRanks0PeerEnd + 1;
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

                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    __musoqFinalShapeRows.Add(new ResultRow0(ko3iko.Name, ko3iko.City, (long)resultRanks0[windowIndex], (long)resultDenseRanks1[windowIndex]));
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
        private static void ExtractResultRanks0WindowKeys(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> resultWindowRows, string[] resultRanks0PartitionKeys, WindowResultRanks0OrderKeysKey[] resultRanks0OrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                resultRanks0PartitionKeys[windowIndex] = (string)ko3iko.City;
                resultRanks0OrderKeys[windowIndex] = new WindowResultRanks0OrderKeysKey(ko3iko.Population);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, long __value2, long __value3)
            {
                Name = __value0;
                City = __value1;
                rnk = __value2;
                dense_rnk = __value3;
            }

            public string City { get; private set; }
            public override int Count => 4;
            public string Name { get; private set; }
            public long dense_rnk { get; private set; }
            public long rnk { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.rnk = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.dense_rnk = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.rnk),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.dense_rnk),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "City" => true,
                "rnk" => true,
                "dense_rnk" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.rnk, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.dense_rnk, ref visitor);
            }
        }

        private readonly struct WindowResultRanks0OrderKeysKey : System.IEquatable<WindowResultRanks0OrderKeysKey>, System.IComparable<WindowResultRanks0OrderKeysKey>
        {
            private readonly decimal _value0;
            public WindowResultRanks0OrderKeysKey(decimal value0)
            {
                _value0 = value0;
            }

            public decimal RawValues => _value0;

            public int CompareTo(WindowResultRanks0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool PeerEquals(WindowResultRanks0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<decimal>.Default.Equals(_value0, other._value0);
            }

            public bool Equals(WindowResultRanks0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<decimal>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRanks0OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(decimal left, decimal right)
            {
                var comparison = left.CompareTo(right);
                return -comparison;
            }
        }
    }
}
