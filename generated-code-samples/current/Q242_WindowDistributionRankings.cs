// === Parsed Query ===
/*
select Name, City, PercentRank() over (partition by City order by NullableValue desc nulls last, Country) as PercentRankValue, CumeDist() over (partition by City order by NullableValue desc nulls last, Country) as CumeDistValue from #A.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Name as Name, ko3iko.City as City, WindowRef(0) as PercentRankValue, WindowRef(1) as CumeDistValue]
    Window [PercentRank(idx:0; partition: ko3iko.City; order: ko3iko.NullableValue DESC NULLS LAST, ko3iko.Country), CumeDist(idx:1; partition: ko3iko.City; order: ko3iko.NullableValue DESC NULLS LAST, ko3iko.Country)]
      SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as Name, ko3iko.City as City, WindowRef(0) as PercentRankValue, WindowRef(1) as CumeDistValue]
    PhysicalWindow [PercentRank(idx:0; partition: ko3iko.City; order: ko3iko.NullableValue DESC NULLS LAST, ko3iko.Country), CumeDist(idx:1; partition: ko3iko.City; order: ko3iko.NullableValue DESC NULLS LAST, ko3iko.Country)]
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
      Country: string <- property Country
      NullableValue: int? <- property NullableValue
    Generated [ResultRow0]
      Name: string <- field Name
      City: string <- field City
      PercentRankValue: double <- field PercentRankValue
      CumeDistValue: double <- field CumeDistValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    WindowKernelPlan [hash partition/per-partition sort; kernels 2; ranking|resultWindowRows|resultPercentRanks0Partitions|resultPercentRanks0Partitions|resultPercentRanks0PartitionKeys|resultPercentRanks0OrderKeys]
      ComputePercentRankWindow [resultPercentRanks0 <- resultWindowRows partition by ko3iko.City order by ko3iko.NullableValue DESC NULLS LAST, ko3iko.Country ASC]
      ComputeCumeDistWindow [resultCumeDists1 <- resultWindowRows partition by ko3iko.City order by ko3iko.NullableValue DESC NULLS LAST, ko3iko.Country ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Name: ko3iko.Name, City: ko3iko.City, PercentRankValue: resultPercentRanks0[windowIndex], CumeDistValue: resultCumeDists1[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q242_WindowDistributionRankings
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
            new Column("Name", typeof(string), 0),
            new Column("City", typeof(string), 1),
            new Column("PercentRankValue", typeof(double), 2),
            new Column("CumeDistValue", typeof(double), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1), new Column("Country", typeof(string), 2), new Column("NullableValue", typeof(int?), 3) });
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
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.City, __musoqShapeRow.PercentRankValue, __musoqShapeRow.CumeDistValue);
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
                var resultPercentRanks0PartitionKeys = new string[resultWindowRows.Count];
                var resultPercentRanks0OrderKeys = new WindowResultPercentRanks0OrderKeysKey[resultWindowRows.Count];
                ExtractResultPercentRanks0WindowKeys(resultWindowRows, resultPercentRanks0PartitionKeys, resultPercentRanks0OrderKeys);
                var resultPercentRanks0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultPercentRanks0PartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultPercentRanks0Partitions, resultPercentRanks0OrderKeys, false);
                var resultPercentRanks0 = new double[resultWindowRows.Count];
                var resultCumeDists1 = new double[resultWindowRows.Count];
                for (int resultPercentRanks0WindowPlanPartitionSetIndex = 0; resultPercentRanks0WindowPlanPartitionSetIndex < resultPercentRanks0Partitions.PartitionCount; ++resultPercentRanks0WindowPlanPartitionSetIndex)
                {
                    var resultPercentRanks0WindowPlanPartitionStart = resultPercentRanks0Partitions.GetStart(resultPercentRanks0WindowPlanPartitionSetIndex);
                    var resultPercentRanks0WindowPlanPartitionCount = resultPercentRanks0Partitions.GetLength(resultPercentRanks0WindowPlanPartitionSetIndex);
                    var resultPercentRanks0WindowPlanPartitionIndices = resultPercentRanks0Partitions.Indices;
                    for (int resultPercentRanks0PeerStart = 0; resultPercentRanks0PeerStart < resultPercentRanks0WindowPlanPartitionCount;)
                    {
                        var resultPercentRanks0CurrentIndex = resultPercentRanks0WindowPlanPartitionIndices[resultPercentRanks0WindowPlanPartitionStart + resultPercentRanks0PeerStart];
                        var resultPercentRanks0PeerEnd = resultPercentRanks0PeerStart;
                        while (resultPercentRanks0PeerEnd + 1 < resultPercentRanks0WindowPlanPartitionCount)
                        {
                            var resultPercentRanks0CandidateIndex = resultPercentRanks0WindowPlanPartitionIndices[resultPercentRanks0WindowPlanPartitionStart + resultPercentRanks0PeerEnd + 1];
                            if (!resultPercentRanks0OrderKeys[resultPercentRanks0CurrentIndex].PeerEquals(resultPercentRanks0OrderKeys[resultPercentRanks0CandidateIndex]))
                                break;
                            resultPercentRanks0PeerEnd++;
                        }

                        for (int resultPercentRanks0PeerIndex = resultPercentRanks0PeerStart; resultPercentRanks0PeerIndex <= resultPercentRanks0PeerEnd; ++resultPercentRanks0PeerIndex)
                        {
                            resultPercentRanks0CurrentIndex = resultPercentRanks0WindowPlanPartitionIndices[resultPercentRanks0WindowPlanPartitionStart + resultPercentRanks0PeerIndex];
                            resultPercentRanks0[resultPercentRanks0CurrentIndex] = resultPercentRanks0WindowPlanPartitionCount == 1 ? 0d : (double)resultPercentRanks0PeerStart / (resultPercentRanks0WindowPlanPartitionCount - 1);
                            resultCumeDists1[resultPercentRanks0CurrentIndex] = (double)(resultPercentRanks0PeerEnd + 1) / resultPercentRanks0WindowPlanPartitionCount;
                        }

                        resultPercentRanks0PeerStart = resultPercentRanks0PeerEnd + 1;
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
                    __musoqFinalShapeRows.Add(new ResultShape0(ko3iko.Name, ko3iko.City, (double)resultPercentRanks0[windowIndex], (double)resultCumeDists1[windowIndex]));
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
        private static void ExtractResultPercentRanks0WindowKeys(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> resultWindowRows, string[] resultPercentRanks0PartitionKeys, WindowResultPercentRanks0OrderKeysKey[] resultPercentRanks0OrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                resultPercentRanks0PartitionKeys[windowIndex] = (string)ko3iko.City;
                resultPercentRanks0OrderKeys[windowIndex] = new WindowResultPercentRanks0OrderKeysKey(ko3iko.NullableValue, ko3iko.Country);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, double __value2, double __value3)
            {
                Name = __value0;
                City = __value1;
                PercentRankValue = __value2;
                CumeDistValue = __value3;
            }

            public string City { get; private set; }
            public override int Count => 4;
            public double CumeDistValue { get; private set; }
            public string Name { get; private set; }
            public double PercentRankValue { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        City = (string)value;
                        break;
                    case 2:
                        PercentRankValue = (double)value;
                        break;
                    case 3:
                        CumeDistValue = (double)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "City" => true,
                "PercentRankValue" => true,
                "CumeDistValue" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)City,
                2 => (object)PercentRankValue,
                3 => (object)CumeDistValue,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "City" => (object)City,
                "PercentRankValue" => (object)PercentRankValue,
                "CumeDistValue" => (object)CumeDistValue,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, string City, double PercentRankValue, double CumeDistValue)
            {
                this.Name = Name;
                this.City = City;
                this.PercentRankValue = PercentRankValue;
                this.CumeDistValue = CumeDistValue;
            }

            public string City { get; }
            public double CumeDistValue { get; }
            public string Name { get; }
            public double PercentRankValue { get; }
        }

        private readonly struct WindowResultPercentRanks0OrderKeysKey : System.IEquatable<WindowResultPercentRanks0OrderKeysKey>, System.IComparable<WindowResultPercentRanks0OrderKeysKey>
        {
            private readonly int? _value0;
            private readonly string _value1;
            public WindowResultPercentRanks0OrderKeysKey(int? value0, string value1)
            {
                _value0 = value0;
                _value1 = value1;
            }

            public int CompareTo(WindowResultPercentRanks0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                var comparison1 = CompareValue1(_value1, other._value1);
                if (comparison1 != 0)
                    return comparison1;
                return 0;
            }

            public bool PeerEquals(WindowResultPercentRanks0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0) && System.String.Equals(_value1, other._value1, System.StringComparison.Ordinal);
            }

            public bool Equals(WindowResultPercentRanks0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0) && System.String.Equals(_value1, other._value1, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultPercentRanks0OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                hash.Add(_value1, System.StringComparer.Ordinal);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : 1;
                if (!right.HasValue)
                    return -1;
                var comparison = left.Value.CompareTo(right.Value);
                return -comparison;
            }

            private static int CompareValue1(string left, string right)
            {
                if (left == null)
                    return right == null ? 0 : -1;
                if (right == null)
                    return 1;
                var comparison = System.String.CompareOrdinal(left, right);
                return comparison;
            }
        }
    }
}
