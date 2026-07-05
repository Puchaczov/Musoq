/*
raw query string

select Name, Population, Sum(Population) over (order by Name rows between 1 preceding and current row) as RunSum from #A.entities()
*/

/*
logical plan representation string

MultiStatement
  Project [ko3iko.Name as Name, ko3iko.Population as Population, WindowRef(0) as RunSum]
    Window [Sum(idx:0; order: ko3iko.Name; args: ko3iko.Population; frame: rows between 1 preceding and current row)]
      SchemaScan [#A.entities() as ko3iko]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as Name, ko3iko.Population as Population, WindowRef(0) as RunSum]
    PhysicalWindow [Sum(idx:0; order: ko3iko.Name; args: ko3iko.Population; frame: rows between 1 preceding and current row)]
      PhysicalMaterialize
        PhysicalSchemaScan [#A.entities() as ko3iko]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
    Generated [ResultRow0]
      Name: string <- field Name
      Population: decimal <- field Population
      RunSum: decimal <- field RunSum

  Body
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums <- resultWindowRows value ko3iko.Population order by ko3iko.Name ASC frame rows between 1 preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Name: ko3iko.Name, Population: ko3iko.Population, RunSum: resultSums[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q42_WindowFrameRowsSum
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
            new Column("Name", typeof(string), 0),
            new Column("Population", typeof(decimal), 1),
            new Column("RunSum", typeof(decimal), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Population", typeof(decimal), 13) });
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
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Population, __musoqShapeRow.RunSum);
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
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultSumsOrderKeys = new WindowResultSumsOrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    resultSumsOrderKeys[windowIndex] = new WindowResultSumsOrderKeysKey(ko3iko.Name);
                }

                var resultSumsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, null);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSumsPartitions, resultSumsOrderKeys, false);
                var resultSums = new decimal[resultWindowRows.Count];
                for (int resultSumsPartitionSetIndex = 0; resultSumsPartitionSetIndex < resultSumsPartitions.PartitionCount; ++resultSumsPartitionSetIndex)
                {
                    var resultSumsPartitionStart = resultSumsPartitions.GetStart(resultSumsPartitionSetIndex);
                    var resultSumsPartitionCount = resultSumsPartitions.GetLength(resultSumsPartitionSetIndex);
                    var resultSumsPartitionIndices = resultSumsPartitions.Indices;
                    var resultSumsPrefixSum = System.Buffers.ArrayPool<decimal>.Shared.Rent(resultSumsPartitionCount + 1);
                    resultSumsPrefixSum[0] = default(decimal);
                    for (int resultSumsPartitionIndex = 0; resultSumsPartitionIndex < resultSumsPartitionCount; ++resultSumsPartitionIndex)
                    {
                        var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPartitionIndex];
                        Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[resultSumsCurrentIndex];
                        var resultSumsValue = ko3iko.Population;
                        resultSumsPrefixSum[resultSumsPartitionIndex + 1] = resultSumsPrefixSum[resultSumsPartitionIndex] + (decimal)resultSumsValue;
                    }

                    for (int resultSumsPartitionIndex = 0; resultSumsPartitionIndex < resultSumsPartitionCount; ++resultSumsPartitionIndex)
                    {
                        var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPartitionIndex];
                        var resultSumsFrameStart = Math.Max(0, resultSumsPartitionIndex - 1);
                        var resultSumsFrameEnd = resultSumsPartitionIndex;
                        var resultSumsFramePrefixStart = Math.Max(0, resultSumsFrameStart);
                        var resultSumsFramePrefixEnd = Math.Max(0, resultSumsFrameEnd + 1);
                        resultSums[resultSumsCurrentIndex] = resultSumsPrefixSum[resultSumsFramePrefixEnd] - resultSumsPrefixSum[resultSumsFramePrefixStart];
                    }

                    System.Buffers.ArrayPool<decimal>.Shared.Return(resultSumsPrefixSum, false);
                }

                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = resultWindowRows[windowIndex];
                    __musoqFinalShapeRows.Add(new ResultShape0(ko3iko.Name, ko3iko.Population, (decimal)resultSums[windowIndex]));
                }

                return __musoqFinalShapeRows;
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
            public ResultRow0(string __value0, decimal __value1, decimal __value2)
            {
                Name = __value0;
                Population = __value1;
                RunSum = __value2;
            }

            public override int Count => 3;
            public string Name { get; private set; }
            public decimal Population { get; private set; }
            public decimal RunSum { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Population = (decimal)value;
                        break;
                    case 2:
                        RunSum = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Population" => true,
                "RunSum" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Population,
                2 => (object)RunSum,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Population" => (object)Population,
                "RunSum" => (object)RunSum,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, decimal Population, decimal RunSum)
            {
                this.Name = Name;
                this.Population = Population;
                this.RunSum = RunSum;
            }

            public string Name { get; }
            public decimal Population { get; }
            public decimal RunSum { get; }
        }

        private readonly struct WindowResultSumsOrderKeysKey : System.IEquatable<WindowResultSumsOrderKeysKey>, System.IComparable<WindowResultSumsOrderKeysKey>
        {
            private readonly string _value0;
            public WindowResultSumsOrderKeysKey(string value0)
            {
                _value0 = value0;
            }

            public int CompareTo(WindowResultSumsOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSumsOrderKeysKey other)
            {
                return System.String.Equals(_value0, other._value0, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSumsOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0, System.StringComparer.Ordinal);
                return hash.ToHashCode();
            }

            private static int CompareValue0(string left, string right)
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
