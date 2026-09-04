// === Parsed Query ===
/*
select City, RowNumber() over (partition by City order by Population desc) as rn, Sum(Population) over (partition by City) as total from #A.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.City as City, WindowRef(0) as rn, WindowRef(1) as total]
    Window [RowNumber(idx:0; partition: ko3iko.City; order: ko3iko.Population DESC), Sum(idx:1; partition: ko3iko.City; args: ko3iko.Population)]
      SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.City as City, WindowRef(0) as rn, WindowRef(1) as total]
    PhysicalWindow [RowNumber(idx:0; partition: ko3iko.City; order: ko3iko.Population DESC), Sum(idx:1; partition: ko3iko.City; args: ko3iko.Population)]
      PhysicalMaterialize
        PhysicalSchemaScan [#A.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      City: string <- property City
      Population: decimal <- property Population
    Generated [ResultRow0]
      City: string <- field City
      rn: long <- field rn
      total: decimal <- field total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows partition by ko3iko.City order by ko3iko.Population DESC]
    ComputeSumWindowKernel[WholePartition] [resultSums1 <- resultWindowRows value ko3iko.Population partition by ko3iko.City]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(City: ko3iko.City, rn: resultRowNumbers0[windowIndex], total: resultSums1[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q254_SharedStableWindowInputs
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("rn", typeof(long), 1),
            new Column("total", typeof(decimal), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Population", typeof(decimal), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.rn, __musoqShapeRow.total);
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultRowNumbers0PartitionKeys = new string[resultWindowRows.Count];
                var resultRowNumbers0OrderKeys = new WindowResultRowNumbers0OrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbers0WindowKeys(resultWindowRows, resultRowNumbers0PartitionKeys, resultRowNumbers0OrderKeys);
                var resultRowNumbers0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultRowNumbers0PartitionKeys);
                var resultRowNumbers0SortedPartitions = WindowFunctionHelpers.SortStructPartitionSet(resultRowNumbers0Partitions, resultRowNumbers0OrderKeys, false);
                var resultRowNumbers0 = new long[resultWindowRows.Count];
                for (int resultRowNumbers0PartitionSetIndex = 0; resultRowNumbers0PartitionSetIndex < resultRowNumbers0SortedPartitions.PartitionCount; ++resultRowNumbers0PartitionSetIndex)
                {
                    var resultRowNumbers0PartitionStart = resultRowNumbers0SortedPartitions.GetStart(resultRowNumbers0PartitionSetIndex);
                    var resultRowNumbers0PartitionCount = resultRowNumbers0SortedPartitions.GetLength(resultRowNumbers0PartitionSetIndex);
                    var resultRowNumbers0PartitionIndices = resultRowNumbers0SortedPartitions.Indices;
                    var resultRowNumbers0PartitionLimit = resultRowNumbers0PartitionCount;
                    for (int resultRowNumbers0PartitionIndex = 0; resultRowNumbers0PartitionIndex < resultRowNumbers0PartitionLimit; ++resultRowNumbers0PartitionIndex)
                    {
                        var resultRowNumbers0CurrentIndex = resultRowNumbers0PartitionIndices[resultRowNumbers0PartitionStart + resultRowNumbers0PartitionIndex];
                        resultRowNumbers0[resultRowNumbers0CurrentIndex] = resultRowNumbers0PartitionIndex + 1L;
                    }
                }

                var resultSums1 = new decimal[resultWindowRows.Count];
                for (int resultSums1PartitionSetIndex = 0; resultSums1PartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultSums1PartitionSetIndex)
                {
                    var resultSums1PartitionStart = resultRowNumbers0Partitions.GetStart(resultSums1PartitionSetIndex);
                    var resultSums1PartitionCount = resultRowNumbers0Partitions.GetLength(resultSums1PartitionSetIndex);
                    var resultSums1PartitionIndices = resultRowNumbers0Partitions.Indices;
                    decimal resultSums1Sum = default(decimal);
                    for (int resultSums1PartitionIndex = 0; resultSums1PartitionIndex < resultSums1PartitionCount; ++resultSums1PartitionIndex)
                    {
                        var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PartitionIndex];
                        Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultSums1CurrentIndex];
                        resultSums1Sum += (decimal)ko3iko.Population;
                    }

                    var resultSums1FinalValue = resultSums1Sum;
                    for (int resultSums1PartitionIndex = 0; resultSums1PartitionIndex < resultSums1PartitionCount; ++resultSums1PartitionIndex)
                    {
                        var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PartitionIndex];
                        resultSums1[resultSums1CurrentIndex] = resultSums1FinalValue;
                    }
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    __musoqFinalShapeRows.Add(new ResultShape0(ko3iko.City, (long)resultRowNumbers0[windowIndex], (decimal)resultSums1[windowIndex]));
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
        private static void ExtractResultRowNumbers0WindowKeys(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> resultWindowRows, string[] resultRowNumbers0PartitionKeys, WindowResultRowNumbers0OrderKeysKey[] resultRowNumbers0OrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                resultRowNumbers0PartitionKeys[windowIndex] = (string)ko3iko.City;
                resultRowNumbers0OrderKeys[windowIndex] = new WindowResultRowNumbers0OrderKeysKey(ko3iko.Population);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, long __value1, decimal __value2)
            {
                City = __value0;
                rn = __value1;
                total = __value2;
            }

            public string City { get; private set; }
            public override int Count => 3;
            public long rn { get; private set; }
            public decimal total { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        City = (string)value;
                        break;
                    case 1:
                        rn = (long)value;
                        break;
                    case 2:
                        total = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "rn" => true,
                "total" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)City,
                1 => (object)rn,
                2 => (object)total,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "City" => (object)City,
                "rn" => (object)rn,
                "total" => (object)total,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string City, long rn, decimal total)
            {
                this.City = City;
                this.rn = rn;
                this.total = total;
            }

            public string City { get; }
            public long rn { get; }
            public decimal total { get; }
        }

        private readonly struct WindowResultRowNumbers0OrderKeysKey : System.IEquatable<WindowResultRowNumbers0OrderKeysKey>, System.IComparable<WindowResultRowNumbers0OrderKeysKey>
        {
            private readonly decimal _value0;
            public WindowResultRowNumbers0OrderKeysKey(decimal value0)
            {
                _value0 = value0;
            }

            public int CompareTo(WindowResultRowNumbers0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultRowNumbers0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<decimal>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRowNumbers0OrderKeysKey other && Equals(other);
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
