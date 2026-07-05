/*
raw query string

select a.Name, b.City, Sum(a.Population) over (order by a.Name rows between 1 preceding and current row) as RunSum from #A.entities() a inner join #A.entities() b on a.Id = b.Id
*/

/*
logical plan representation string

MultiStatement
  Project [a.Name as a.Name, a.Population as a.Population, a.Id as a.Id, b.City as b.City, b.Id as b.Id]
    Join [Inner] [(a.Id = b.Id)]
      SchemaScan [#A.entities() as a]
      SchemaScan [#A.entities() as b]
  Project [a.Name as a.Name, b.City as b.City, WindowRef(0) as RunSum]
    Window [Sum(idx:0; order: a.Name; args: a.Population; frame: rows between 1 preceding and current row)]
      CteRef [ab as ab]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [a.Name as a.Name, a.Population as a.Population, a.Id as a.Id, b.City as b.City, b.Id as b.Id]
    PhysicalHashJoin [Inner] [build: b.Id] [probe: a.Id]
      PhysicalSchemaScan [#A.entities() as a]
      PhysicalSchemaScan [#A.entities() as b]
  PhysicalProject [a.Name as a.Name, b.City as b.City, WindowRef(0) as RunSum]
    PhysicalWindow [Sum(idx:0; order: a.Name; args: a.Population; frame: rows between 1 preceding and current row)]
      PhysicalMaterialize
        PhysicalCteRef [ab as ab]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
      Id: int <- property Id
    SourceEntity [b: BasicEntity]
      City: string <- property City
      Id: int <- property Id
    Generated [Statement0Row0]
      a.Name: string <- field a_Name
      a.Population: decimal <- field a_Population
      a.Id: int <- field a_Id
      b.City: string <- field b_City
      b.Id: int <- field b_Id
    TableRow [ab]
      a.Name: string <- field a_Name
      a.Population: decimal <- field a_Population
      a.Id: int <- field a_Id
      b.City: string <- field b_City
      b.Id: int <- field b_Id
    Generated [ResultRow0]
      a.Name: string <- field a_Name
      b.City: string <- field b_City
      RunSum: decimal <- field RunSum

  Body
    SourceScan [a: BasicEntity] -> statement0_aRows
    SourceScan [b: BasicEntity] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0BHash: int -> BasicEntity]
    ChunkedForEach [b in statement0_bRows]
      HashAdd [statement0BHash[b.Id] += b]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0BHash[a.Id] -> statement0BHashMatches]
        ForEach [b in statement0BHashMatches]
          AppendRow [statement0 <- Statement0Row0(a.Name: a.Name, a.Population: a.Population, a.Id: a.Id, b.City: b.City, b.Id: b.Id)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums <- resultWindowRows value ab.a.Population order by ab.a.Name ASC frame rows between 1 preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ab in resultWindowRows]
      AppendShape [result <- ResultShape0(a.Name: ab.a.Name, b.City: ab.b.City, RunSum: resultSums[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q44_WindowFrameWithJoin
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
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
            new Column("b.City", typeof(string), 1),
            new Column("RunSum", typeof(decimal), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
            new Column("a.Population", typeof(decimal), 1),
            new Column("a.Id", typeof(int), 2),
            new Column("b.City", typeof(string), 3),
            new Column("b.Id", typeof(int), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Population", typeof(decimal), 13), new Column("Id", typeof(int), 18) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11), new Column("Id", typeof(int), 18) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_Name, __musoqShapeRow.b_City, __musoqShapeRow.RunSum);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Statement0Row0>(_cteRowResults.Slot0);
                var resultSumsOrderKeys = new WindowResultSumsOrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Statement0Row0 ab = resultWindowRows[windowIndex];
                    resultSumsOrderKeys[windowIndex] = new WindowResultSumsOrderKeysKey(ab.a_Name);
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
                        Statement0Row0 ab = resultWindowRows[resultSumsCurrentIndex];
                        var resultSumsValue = ab.a_Population;
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

                    Statement0Row0 ab = resultWindowRows[windowIndex];
                    __musoqFinalShapeRows.Add(new ResultShape0(ab.a_Name, ab.b_City, (decimal)resultSums[windowIndex]));
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __statement0_aSchema = provider.GetSchema("#A");
            var statement0_aRowsSource = __statement0_aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_aRows = statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#A");
            var statement0_bRowsSource = __statement0_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_bRows = statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0BHash = new Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>();
            foreach (var bChunk in statement0_bRows)
            {
                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkView)
                {
                    if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] bChunkViewArray)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                            int key = b.Id;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }

                        continue;
                    }

                    if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkViewList)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                            int key = b.Id;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                {
                    if ((bIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var b = bChunk[bIndex];
                    int key = b.Id;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                        }
                        else
                        {
                            matches.Add(b);
                        }
                    }
                }
            }

            foreach (var aChunk in statement0_aRows)
            {
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            int key = a.Id;
                            if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(a.Name, a.Population, a.Id, b.City, b.Id));
                                }
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            int key = a.Id;
                            if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(a.Name, a.Population, a.Id, b.City, b.Id));
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int aIndex = 0, aIndexCount = aChunk.Count; aIndex < aIndexCount; ++aIndex)
                {
                    if ((aIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var a = aChunk[aIndex];
                    int key = a.Id;
                    if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                    {
                        foreach (var b in statement0BHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0.Add(new Statement0Row0(a.Name, a.Population, a.Id, b.City, b.Id));
                        }
                    }
                }
            }

            return statement0;
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, decimal __value2)
            {
                a_Name = __value0;
                b_City = __value1;
                RunSum = __value2;
            }

            public override int Count => 3;
            public decimal RunSum { get; private set; }
            public string a_Name { get; private set; }
            public string b_City { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_Name = (string)value;
                        break;
                    case 1:
                        b_City = (string)value;
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
                "a.Name" => true,
                "a_Name" => true,
                "Name" => true,
                "b.City" => true,
                "b_City" => true,
                "City" => true,
                "RunSum" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_Name,
                1 => (object)b_City,
                2 => (object)RunSum,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.Name" => (object)a_Name,
                "a_Name" => (object)a_Name,
                "Name" => (object)a_Name,
                "b.City" => (object)b_City,
                "b_City" => (object)b_City,
                "City" => (object)b_City,
                "RunSum" => (object)RunSum,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string a_Name, string b_City, decimal RunSum)
            {
                this.a_Name = a_Name;
                this.b_City = b_City;
                this.RunSum = RunSum;
            }

            public decimal RunSum { get; }
            public string a_Name { get; }
            public string b_City { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, decimal __value1, int __value2, string __value3, int __value4)
            {
                a_Name = __value0;
                a_Population = __value1;
                a_Id = __value2;
                b_City = __value3;
                b_Id = __value4;
            }

            public int a_Id { get; }
            public string a_Name { get; }
            public decimal a_Population { get; }
            public string b_City { get; }
            public int b_Id { get; }
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
