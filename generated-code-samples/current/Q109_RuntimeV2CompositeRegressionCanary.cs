// === Parsed Query ===
/*
SELECT Name,
                     Department,
                     Salary,
                     ExpensiveCompute(Value) as Computed,
                     Sum(ToDecimal(Salary)) over (partition by Department order by Salary) as RunningSalary,
                     Rank() over (partition by Department order by Salary desc) as SalaryRank
              FROM #test.entities()
              WHERE Contains(Email, 'gmail')
                    AND StartsWith(FirstName, 'A')
                    AND ExpensiveCompute(Value) > 50
              ORDER BY Department, Salary desc, Computed desc
              SKIP 10 TAKE 20
*/

// === Logical Plan ===
/*
MultiStatement
  Take [20]
    Skip [10]
      Sort [ko3iko.Department, ko3iko.Salary DESC, ExpensiveCompute(ko3iko.Value) DESC]
        Project [ko3iko.Name as Name, ko3iko.Department as Department, ko3iko.Salary as Salary, ExpensiveCompute(ko3iko.Value) as Computed, WindowRef(0) as RunningSalary, WindowRef(1) as SalaryRank]
          Window [Sum(idx:0; partition: ko3iko.Department; order: ko3iko.Salary; args: ToDecimal(ko3iko.Salary)), Rank(idx:1; partition: ko3iko.Department; order: ko3iko.Salary DESC)]
            Filter [(((Contains(ko3iko.Email, 'gmail') = TRUE) AND (StartsWith(ko3iko.FirstName, 'A') = TRUE)) AND (ExpensiveCompute(ko3iko.Value) > 50))]
              SchemaScan [#test.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalTopOffset [skip 10, take 20] [ko3iko.Department, ko3iko.Salary DESC, ExpensiveCompute(ko3iko.Value) DESC]
    PhysicalProject [ko3iko.Name as Name, ko3iko.Department as Department, ko3iko.Salary as Salary, ExpensiveCompute(ko3iko.Value) as Computed, WindowRef(0) as RunningSalary, WindowRef(1) as SalaryRank]
      PhysicalWindow [Sum(idx:0; partition: ko3iko.Department; order: ko3iko.Salary; args: ToDecimal(ko3iko.Salary)), Rank(idx:1; partition: ko3iko.Department; order: ko3iko.Salary DESC)]
        PhysicalMaterialize
          PhysicalFilter [(((Contains(ko3iko.Email, 'gmail') = TRUE) AND (StartsWith(ko3iko.FirstName, 'A') = TRUE)) AND (ExpensiveCompute(ko3iko.Value) > 50))]
            PhysicalSchemaScan [#test.entities() as ko3iko] [pushdown: Contains(ko3iko.Email, 'gmail'), StartsWith(ko3iko.FirstName, 'A')]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2RegressionEntity]
      Name: string <- property Name
      FirstName: string <- property FirstName
      Email: string <- property Email
      Value: int <- property Value
      Department: string <- property Department
      Salary: int <- property Salary
    Generated [ResultRow0]
      Name: string <- field Name
      Department: string <- field Department
      Salary: int <- field Salary
      Computed: int <- field Computed
      RunningSalary: decimal <- field RunningSalary
      SalaryRank: long <- field SalaryRank

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows
    CreateObject [__resultRuntimeV2RegressionLibrary0: RuntimeV2RegressionLibrary]
    MaterializeFilteredChunked [ko3ikoRows where (((Contains(ko3iko.Email, 'gmail') = TRUE) AND (StartsWith(ko3iko.FirstName, 'A') = TRUE)) AND (ExpensiveCompute(ko3iko.Value) > 50)) -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums0 <- resultWindowRows value ToDecimal(ko3iko.Salary) partition by ko3iko.Department order by ko3iko.Salary ASC frame range between unbounded preceding and current row]
    ComputeRankWindow [resultRanks1 <- resultWindowRows partition by ko3iko.Department order by ko3iko.Salary DESC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Where]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Name: ko3iko.Name, Department: ko3iko.Department, Salary: ko3iko.Salary, Computed: ExpensiveCompute(ko3iko.Value), RunningSalary: resultSums0[windowIndex], SalaryRank: resultRanks1[windowIndex])]
    TopOffsetShapeRows [result -> resultTopOffset by Department ASC, Salary DESC, Computed DESC, skip 10, take 20, BoundedHeap]
    ReturnDeferredTable [resultTopOffset: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q109_RuntimeV2CompositeRegressionCanary
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
            new Column("Department", typeof(string), 1),
            new Column("Salary", typeof(int), 2),
            new Column("Computed", typeof(int), 3),
            new Column("RunningSalary", typeof(decimal), 4),
            new Column("SalaryRank", typeof(long), 5)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 1), new Column("FirstName", typeof(string), 2), new Column("Email", typeof(string), 4), new Column("Value", typeof(int), 5), new Column("Department", typeof(string), 7), new Column("Salary", typeof(int), 8) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultTopOffset", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Department, __musoqShapeRow.Salary, __musoqShapeRow.Computed, __musoqShapeRow.RunningSalary, __musoqShapeRow.SalaryRank);
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
                var __ko3ikoSchema = provider.GetSchema("#test");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var __resultRuntimeV2RegressionLibrary0 = new Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionLibrary();
                var resultWindowRows = new List<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>();
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
                                {
                                    if ((((((ko3iko.Email == null || "gmail" == null) ? (bool?)null : ko3iko.Email.Contains("gmail", StringComparison.OrdinalIgnoreCase)) == true) && (((ko3iko.FirstName == null || "A" == null) ? (bool?)null : ko3iko.FirstName.StartsWith("A", StringComparison.OrdinalIgnoreCase)) == true)) && ((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value) > 50)))
                                    {
                                        resultWindowRows.Add(ko3iko);
                                    }
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
                                {
                                    if ((((((ko3iko.Email == null || "gmail" == null) ? (bool?)null : ko3iko.Email.Contains("gmail", StringComparison.OrdinalIgnoreCase)) == true) && (((ko3iko.FirstName == null || "A" == null) ? (bool?)null : ko3iko.FirstName.StartsWith("A", StringComparison.OrdinalIgnoreCase)) == true)) && ((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value) > 50)))
                                    {
                                        resultWindowRows.Add(ko3iko);
                                    }
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
                        {
                            if ((((((ko3iko.Email == null || "gmail" == null) ? (bool?)null : ko3iko.Email.Contains("gmail", StringComparison.OrdinalIgnoreCase)) == true) && (((ko3iko.FirstName == null || "A" == null) ? (bool?)null : ko3iko.FirstName.StartsWith("A", StringComparison.OrdinalIgnoreCase)) == true)) && ((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value) > 50)))
                            {
                                resultWindowRows.Add(ko3iko);
                            }
                        }
                    }
                }

                var resultSums0PartitionKeys = new string[resultWindowRows.Count];
                var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity ko3iko = resultWindowRows[windowIndex];
                    resultSums0PartitionKeys[windowIndex] = (string)ko3iko.Department;
                    resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(ko3iko.Salary);
                }

                var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultSums0PartitionKeys);
                var resultSums0SortedPartitions = WindowFunctionHelpers.SortStructPartitionSet(resultSums0Partitions, resultSums0OrderKeys, false);
                var resultSums0 = new decimal[resultWindowRows.Count];
                for (int resultSums0PartitionSetIndex = 0; resultSums0PartitionSetIndex < resultSums0SortedPartitions.PartitionCount; ++resultSums0PartitionSetIndex)
                {
                    var resultSums0PartitionStart = resultSums0SortedPartitions.GetStart(resultSums0PartitionSetIndex);
                    var resultSums0PartitionCount = resultSums0SortedPartitions.GetLength(resultSums0PartitionSetIndex);
                    var resultSums0PartitionIndices = resultSums0SortedPartitions.Indices;
                    var resultSums0PrefixSum = System.Buffers.ArrayPool<decimal>.Shared.Rent(resultSums0PartitionCount + 1);
                    resultSums0PrefixSum[0] = default(decimal);
                    for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount; ++resultSums0PartitionIndex)
                    {
                        var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PartitionIndex];
                        Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity ko3iko = resultWindowRows[resultSums0CurrentIndex];
                        var resultSums0Value = ((decimal?)ko3iko.Salary);
                        resultSums0PrefixSum[resultSums0PartitionIndex + 1] = resultSums0Value.HasValue ? resultSums0PrefixSum[resultSums0PartitionIndex] + (decimal)resultSums0Value.Value : resultSums0PrefixSum[resultSums0PartitionIndex];
                    }

                    for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount; ++resultSums0PartitionIndex)
                    {
                        var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PartitionIndex];
                        var resultSums0FrameStart = 0;
                        var resultSums0FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount, resultSums0PartitionIndex);
                        var resultSums0FramePrefixStart = Math.Max(0, resultSums0FrameStart);
                        var resultSums0FramePrefixEnd = Math.Max(0, resultSums0FrameEnd + 1);
                        resultSums0[resultSums0CurrentIndex] = resultSums0PrefixSum[resultSums0FramePrefixEnd] - resultSums0PrefixSum[resultSums0FramePrefixStart];
                    }

                    System.Buffers.ArrayPool<decimal>.Shared.Return(resultSums0PrefixSum, false);
                }

                var resultRanks1OrderKeys = new WindowResultRanks1OrderKeysKey[resultWindowRows.Count];
                ExtractResultRanks1WindowKeys(resultWindowRows, resultRanks1OrderKeys);
                var resultRanks1SortedPartitions = WindowFunctionHelpers.SortStructPartitionSet(resultSums0Partitions, resultRanks1OrderKeys, false);
                var resultRanks1 = new long[resultWindowRows.Count];
                for (int resultRanks1PartitionSetIndex = 0; resultRanks1PartitionSetIndex < resultRanks1SortedPartitions.PartitionCount; ++resultRanks1PartitionSetIndex)
                {
                    var resultRanks1PartitionStart = resultRanks1SortedPartitions.GetStart(resultRanks1PartitionSetIndex);
                    var resultRanks1PartitionCount = resultRanks1SortedPartitions.GetLength(resultRanks1PartitionSetIndex);
                    var resultRanks1PartitionIndices = resultRanks1SortedPartitions.Indices;
                    long resultRanks1Rank = 1L;
                    for (int resultRanks1PartitionIndex = 0; resultRanks1PartitionIndex < resultRanks1PartitionCount; ++resultRanks1PartitionIndex)
                    {
                        var resultRanks1CurrentIndex = resultRanks1PartitionIndices[resultRanks1PartitionStart + resultRanks1PartitionIndex];
                        if (resultRanks1PartitionIndex > 0)
                        {
                            var resultRanks1PreviousIndex = resultRanks1PartitionIndices[resultRanks1PartitionStart + resultRanks1PartitionIndex - 1];
                            if (!resultRanks1OrderKeys[resultRanks1CurrentIndex].PeerEquals(resultRanks1OrderKeys[resultRanks1PreviousIndex]))
                                resultRanks1Rank = resultRanks1PartitionIndex + 1L;
                        }

                        resultRanks1[resultRanks1CurrentIndex] = resultRanks1Rank;
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                OnPhaseChanged("compiled", QueryPhase.Where);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity ko3iko = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(ko3iko.Name, ko3iko.Department, ko3iko.Salary, (int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value), (decimal)resultSums0[windowIndex], (long)resultRanks1[windowIndex]));
                }

                var resultTopOffsetRows = EvaluationHelper.SelectTopOffsetRecords(result, 10, 20, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Department, right.Department);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.Salary.CompareTo(right.Salary);
                    comparison = -comparison;
                    if (comparison != 0)
                        return comparison;
                    comparison = left.Computed.CompareTo(right.Computed);
                    comparison = -comparison;
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultTopOffsetRowsRow in resultTopOffsetRows)
                {
                    __musoqFinalShapeRows.Add(resultTopOffsetRowsRow);
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
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
        private static void ExtractResultRanks1WindowKeys(IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity> resultWindowRows, WindowResultRanks1OrderKeysKey[] resultRanks1OrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity ko3iko = resultWindowRows[windowIndex];
                resultRanks1OrderKeys[windowIndex] = new WindowResultRanks1OrderKeysKey(ko3iko.Salary);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, int __value2, int __value3, decimal __value4, long __value5)
            {
                Name = __value0;
                Department = __value1;
                Salary = __value2;
                Computed = __value3;
                RunningSalary = __value4;
                SalaryRank = __value5;
            }

            public int Computed { get; private set; }
            public override int Count => 6;
            public string Department { get; private set; }
            public string Name { get; private set; }
            public decimal RunningSalary { get; private set; }
            public int Salary { get; private set; }
            public long SalaryRank { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Department = (string)value;
                        break;
                    case 2:
                        Salary = (int)value;
                        break;
                    case 3:
                        Computed = (int)value;
                        break;
                    case 4:
                        RunningSalary = (decimal)value;
                        break;
                    case 5:
                        SalaryRank = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Department" => true,
                "Salary" => true,
                "Computed" => true,
                "RunningSalary" => true,
                "SalaryRank" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Department,
                2 => (object)Salary,
                3 => (object)Computed,
                4 => (object)RunningSalary,
                5 => (object)SalaryRank,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Department" => (object)Department,
                "Salary" => (object)Salary,
                "Computed" => (object)Computed,
                "RunningSalary" => (object)RunningSalary,
                "SalaryRank" => (object)SalaryRank,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, string Department, int Salary, int Computed, decimal RunningSalary, long SalaryRank)
            {
                this.Name = Name;
                this.Department = Department;
                this.Salary = Salary;
                this.Computed = Computed;
                this.RunningSalary = RunningSalary;
                this.SalaryRank = SalaryRank;
            }

            public int Computed { get; }
            public string Department { get; }
            public string Name { get; }
            public decimal RunningSalary { get; }
            public int Salary { get; }
            public long SalaryRank { get; }
        }

        private readonly struct WindowResultRanks1OrderKeysKey : System.IEquatable<WindowResultRanks1OrderKeysKey>, System.IComparable<WindowResultRanks1OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultRanks1OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int CompareTo(WindowResultRanks1OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool PeerEquals(WindowResultRanks1OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public bool Equals(WindowResultRanks1OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRanks1OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return -comparison;
            }
        }

        private readonly struct WindowResultSums0OrderKeysKey : System.IEquatable<WindowResultSums0OrderKeysKey>, System.IComparable<WindowResultSums0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultSums0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int CompareTo(WindowResultSums0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSums0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSums0OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return comparison;
            }
        }
    }
}
