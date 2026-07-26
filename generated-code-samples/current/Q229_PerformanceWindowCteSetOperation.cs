// === Parsed Query ===
/*
with ranked as (
    select Name, Country
    from #A.entities()
)
select Name, Country,
       RowNumber() over (partition by Country order by Name) as BranchRank
from ranked
union (Name, Country, BranchRank)
    select Name, Country,
           RowNumber() over (partition by Country order by Name) as BranchRank
    from #B.entities()
order by Country, BranchRank, Name
*/

// === Logical Plan ===
/*
Cte
  Definition [ranked]
    MultiStatement
      Project [ko3iko.Name as Name, ko3iko.Country as Country]
        SchemaScan [#A.entities() as ko3iko]
  Query
    SetOp [Union]
      MultiStatement
        Project [ranked.Name as Name, ranked.Country as Country, WindowRef(0) as BranchRank]
          Window [RowNumber(idx:0; partition: ranked.Country; order: ranked.Name)]
            CteRef [ranked as ranked]
      MultiStatement
        Sort [gougbq.Country, WindowRef(0), gougbq.Name]
          Project [gougbq.Name as Name, gougbq.Country as Country, WindowRef(0) as BranchRank]
            Window [RowNumber(idx:0; partition: gougbq.Country; order: gougbq.Name)]
              SchemaScan [#B.entities() as gougbq]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [ranked]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Name as Name, ko3iko.Country as Country]
        PhysicalSchemaScan [#A.entities() as ko3iko]
  Query
    PhysicalSetOp [Union]
      PhysicalMultiStatement
        PhysicalProject [ranked.Name as Name, ranked.Country as Country, WindowRef(0) as BranchRank]
          PhysicalWindow [RowNumber(idx:0; partition: ranked.Country; order: ranked.Name)]
            PhysicalMaterialize
              PhysicalCteRef [ranked as ranked]
      PhysicalMultiStatement
        PhysicalSort [gougbq.Country, WindowRef(0), gougbq.Name]
          PhysicalProject [gougbq.Name as Name, gougbq.Country as Country, WindowRef(0) as BranchRank]
            PhysicalWindow [RowNumber(idx:0; partition: gougbq.Country; order: gougbq.Name)]
              PhysicalMaterialize
                PhysicalSchemaScan [#B.entities() as gougbq]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Country: string <- property Country
    Generated [Cte0Row0]
      Name: string <- field Name
      Country: string <- field Country
    TableRow [ranked]
      Name: string <- field Name
      Country: string <- field Country
    Generated [LeftRow0]
      Name: string <- field Name
      Country: string <- field Country
      BranchRank: long <- field BranchRank
    SourceEntity [gougbq: BasicEntity]
      Name: string <- property Name
      Country: string <- property Country
    Generated [RightRow0]
      Name: string <- field Name
      Country: string <- field Country
      BranchRank: long <- field BranchRank

  Body
    SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
    CreateTable [cte0: Cte0Row0]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      AppendRow [cte0 <- Cte0Row0(Name: ko3iko.Name, Country: ko3iko.Country)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    Materialize [_cteRowResults.Slot0 -> leftWindowRows]
    ComputeRowNumberWindow [leftRowNumbers <- leftWindowRows partition by ranked.Country order by ranked.Name ASC]
    CreateRowBuffer [left: List<LeftRow0>]
    ForEachIndexed [windowIndex, ranked in leftWindowRows]
      AppendRowBuffer [left <- LeftRow0(Name: ranked.Name, Country: ranked.Country, BranchRank: leftRowNumbers[windowIndex])]
    SourceScan [gougbq: BasicEntity] -> right_gougbqRows
    MaterializeChunked [right_gougbqRows -> rightWindowRows]
    ComputeRowNumberWindow [rightRowNumbers <- rightWindowRows partition by gougbq.Country order by gougbq.Name ASC]
    CreateRowBuffer [right: List<RightRow0>]
    ForEachIndexed [windowIndex, gougbq in rightWindowRows]
      AppendRowBuffer [right <- RightRow0(Name: gougbq.Name, Country: gougbq.Country, BranchRank: rightRowNumbers[windowIndex])]
    SortRowBuffer [right -> rightSorted by Country ASC, BranchRank ASC, Name ASC]
    SetOperation [result = left Union rightSorted, HashSet]
    ReturnDeferredTable [result: LeftRow0 <- LeftShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q229_PerformanceWindowCteSetOperation
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Country", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_left_2 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Country", typeof(string), 1),
            new Column("BranchRank", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Country", typeof(string), 12) });
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
            return QueryRows.DeferredTable<LeftRow0>("result", __columns_compiled_left_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<LeftRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new LeftRow0(__musoqShapeRow.Name, __musoqShapeRow.Country, __musoqShapeRow.BranchRank);
            }
        }

        private IEnumerable<LeftShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:left", QueryPhase.Begin);
            OnPhaseChanged("compiled:right", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<LeftShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var leftWindowRows = EvaluationHelper.MaterializeGeneratedRows<Cte0Row0>(_cteRowResults.Slot0);
                var leftRowNumbersPartitionKeys = new string[leftWindowRows.Count];
                var leftRowNumbersOrderKeys = new WindowLeftRowNumbersOrderKeysKey[leftWindowRows.Count];
                ExtractLeftRowNumbersWindowKeys(leftWindowRows, leftRowNumbersPartitionKeys, leftRowNumbersOrderKeys);
                var leftRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(leftWindowRows.Count, leftRowNumbersPartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(leftRowNumbersPartitions, leftRowNumbersOrderKeys, false);
                var leftRowNumbers = new long[leftWindowRows.Count];
                for (int leftRowNumbersPartitionSetIndex = 0; leftRowNumbersPartitionSetIndex < leftRowNumbersPartitions.PartitionCount; ++leftRowNumbersPartitionSetIndex)
                {
                    var leftRowNumbersPartitionStart = leftRowNumbersPartitions.GetStart(leftRowNumbersPartitionSetIndex);
                    var leftRowNumbersPartitionCount = leftRowNumbersPartitions.GetLength(leftRowNumbersPartitionSetIndex);
                    var leftRowNumbersPartitionIndices = leftRowNumbersPartitions.Indices;
                    var leftRowNumbersPartitionLimit = leftRowNumbersPartitionCount;
                    for (int leftRowNumbersPartitionIndex = 0; leftRowNumbersPartitionIndex < leftRowNumbersPartitionLimit; ++leftRowNumbersPartitionIndex)
                    {
                        var leftRowNumbersCurrentIndex = leftRowNumbersPartitionIndices[leftRowNumbersPartitionStart + leftRowNumbersPartitionIndex];
                        leftRowNumbers[leftRowNumbersCurrentIndex] = leftRowNumbersPartitionIndex + 1L;
                    }
                }

                var left = new List<LeftRow0>(leftWindowRows.Count);
                AppendLeftWindowRows(leftWindowRows, left, leftRowNumbers);
                var __right_gougbqSchema = provider.GetSchema("#B");
                var right_gougbqRowsSource = __right_gougbqSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("gougbq:3", sourceExecutionPlans["gougbq:3"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["gougbq:3"], logger, OnDataSourceProgress), Array.Empty<object>());
                var right_gougbqRows = right_gougbqRowsSource.Chunks;
                var rightWindowRows = EvaluationHelper.MaterializeChunkedRowsList(right_gougbqRows);
                var rightRowNumbersPartitionKeys = new string[rightWindowRows.Count];
                var rightRowNumbersOrderKeys = new WindowRightRowNumbersOrderKeysKey[rightWindowRows.Count];
                ExtractRightRowNumbersWindowKeys(rightWindowRows, rightRowNumbersPartitionKeys, rightRowNumbersOrderKeys);
                var rightRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(rightWindowRows.Count, rightRowNumbersPartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(rightRowNumbersPartitions, rightRowNumbersOrderKeys, false);
                var rightRowNumbers = new long[rightWindowRows.Count];
                for (int rightRowNumbersPartitionSetIndex = 0; rightRowNumbersPartitionSetIndex < rightRowNumbersPartitions.PartitionCount; ++rightRowNumbersPartitionSetIndex)
                {
                    var rightRowNumbersPartitionStart = rightRowNumbersPartitions.GetStart(rightRowNumbersPartitionSetIndex);
                    var rightRowNumbersPartitionCount = rightRowNumbersPartitions.GetLength(rightRowNumbersPartitionSetIndex);
                    var rightRowNumbersPartitionIndices = rightRowNumbersPartitions.Indices;
                    var rightRowNumbersPartitionLimit = rightRowNumbersPartitionCount;
                    for (int rightRowNumbersPartitionIndex = 0; rightRowNumbersPartitionIndex < rightRowNumbersPartitionLimit; ++rightRowNumbersPartitionIndex)
                    {
                        var rightRowNumbersCurrentIndex = rightRowNumbersPartitionIndices[rightRowNumbersPartitionStart + rightRowNumbersPartitionIndex];
                        rightRowNumbers[rightRowNumbersCurrentIndex] = rightRowNumbersPartitionIndex + 1L;
                    }
                }

                var right = new List<RightRow0>(rightWindowRows.Count);
                AppendRightWindowRows1(rightWindowRows, right, rightRowNumbers);
                var rightSortedRows = right.OrderBy((row) => row, RightRow0OrderBy_1A_2A_0AComparer.Instance);
                var rightSorted = new List<RightRow0>();
                rightSorted.EnsureCapacity(right.Count);
                foreach (var copiedRow in rightSortedRows)
                {
                    rightSorted.Add(copiedRow);
                }

                var resultKeys = new HashSet<ValueTuple<string, string, long>>(left.Count + rightSorted.Count);
                foreach (var resultLeftRow in left)
                {
                    resultKeys.Add(((string)resultLeftRow.Name, (string)resultLeftRow.Country, (long)resultLeftRow.BranchRank));
                    __musoqFinalShapeRows.Add(new LeftShape0((string)resultLeftRow.Name, (string)resultLeftRow.Country, (long)resultLeftRow.BranchRank));
                }

                foreach (var resultRightRow in rightSorted)
                {
                    if (resultKeys.Add(((string)resultRightRow.Name, (string)resultRightRow.Country, (long)resultRightRow.BranchRank)))
                    {
                        __musoqFinalShapeRows.Add(new LeftShape0((string)resultRightRow.Name, (string)resultRightRow.Country, (long)resultRightRow.BranchRank));
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.End);
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
        private static void AppendLeftWindowRows(IReadOnlyList<Cte0Row0> leftWindowRows, List<LeftRow0> left, long[] leftRowNumbers)
        {
            for (int windowIndex = 0; windowIndex < leftWindowRows.Count; ++windowIndex)
            {
                Cte0Row0 ranked = leftWindowRows[windowIndex];
                left.Add(new LeftRow0(ranked.Name, ranked.Country, (long)leftRowNumbers[windowIndex]));
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void AppendRightWindowRows1(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rightWindowRows, List<RightRow0> right, long[] rightRowNumbers)
        {
            for (int windowIndex = 0; windowIndex < rightWindowRows.Count; ++windowIndex)
            {
                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity gougbq = rightWindowRows[windowIndex];
                right.Add(new RightRow0(gougbq.Name, gougbq.Country, (long)rightRowNumbers[windowIndex]));
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __cte0_ko3ikoSchema = provider.GetSchema("#A");
            var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte0_ko3ikoRows = cte0_ko3ikoRowsSource.Chunks;
            var cte0 = new List<Cte0Row0>();
            foreach (var ko3ikoChunk in cte0_ko3ikoRows)
            {
                if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                {
                    if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                    {
                        int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                            cte0.Add(new Cte0Row0(ko3iko.Name, ko3iko.Country));
                        }

                        continue;
                    }

                    if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                    {
                        int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                            cte0.Add(new Cte0Row0(ko3iko.Name, ko3iko.Country));
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
                    cte0.Add(new Cte0Row0(ko3iko.Name, ko3iko.Country));
                }
            }

            return cte0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ExtractLeftRowNumbersWindowKeys(IReadOnlyList<Cte0Row0> leftWindowRows, string[] leftRowNumbersPartitionKeys, WindowLeftRowNumbersOrderKeysKey[] leftRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < leftWindowRows.Count; ++windowIndex)
            {
                Cte0Row0 ranked = leftWindowRows[windowIndex];
                leftRowNumbersPartitionKeys[windowIndex] = (string)ranked.Country;
                leftRowNumbersOrderKeys[windowIndex] = new WindowLeftRowNumbersOrderKeysKey(ranked.Name);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ExtractRightRowNumbersWindowKeys(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rightWindowRows, string[] rightRowNumbersPartitionKeys, WindowRightRowNumbersOrderKeysKey[] rightRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < rightWindowRows.Count; ++windowIndex)
            {
                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity gougbq = rightWindowRows[windowIndex];
                rightRowNumbersPartitionKeys[windowIndex] = (string)gougbq.Country;
                rightRowNumbersOrderKeys[windowIndex] = new WindowRightRowNumbersOrderKeysKey(gougbq.Name);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, string __value1)
            {
                Name = __value0;
                Country = __value1;
            }

            public string Country { get; }
            public string Name { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class LeftRow0 : Row
        {
            public LeftRow0(string __value0, string __value1, long __value2)
            {
                Name = __value0;
                Country = __value1;
                BranchRank = __value2;
            }

            public long BranchRank { get; private set; }
            public override int Count => 3;
            public string Country { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Country = (string)value;
                        break;
                    case 2:
                        BranchRank = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Country" => true,
                "BranchRank" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Country,
                2 => (object)BranchRank,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Country" => (object)Country,
                "BranchRank" => (object)BranchRank,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class LeftShape0
        {
            public LeftShape0(string Name, string Country, long BranchRank)
            {
                this.Name = Name;
                this.Country = Country;
                this.BranchRank = BranchRank;
            }

            public long BranchRank { get; }
            public string Country { get; }
            public string Name { get; }
        }

        private sealed class RightRow0 : Row
        {
            public RightRow0(string __value0, string __value1, long __value2)
            {
                Name = __value0;
                Country = __value1;
                BranchRank = __value2;
            }

            public long BranchRank { get; private set; }
            public override int Count => 3;
            public string Country { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Country = (string)value;
                        break;
                    case 2:
                        BranchRank = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Country" => true,
                "BranchRank" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Country,
                2 => (object)BranchRank,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Country" => (object)Country,
                "BranchRank" => (object)BranchRank,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class RightRow0OrderBy_1A_2A_0AComparer : IComparer<RightRow0>
        {
            public static readonly RightRow0OrderBy_1A_2A_0AComparer Instance = new RightRow0OrderBy_1A_2A_0AComparer();
            public int Compare(RightRow0 left, RightRow0 right)
            {
                var comparison = StringComparer.Ordinal.Compare(left.Country, right.Country);
                if (comparison != 0)
                    return comparison;
                comparison = left.BranchRank.CompareTo(right.BranchRank);
                if (comparison != 0)
                    return comparison;
                comparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
                if (comparison != 0)
                    return comparison;
                return 0;
            }
        }

        private readonly struct WindowLeftRowNumbersOrderKeysKey : System.IEquatable<WindowLeftRowNumbersOrderKeysKey>, System.IComparable<WindowLeftRowNumbersOrderKeysKey>
        {
            private readonly string _value0;
            public WindowLeftRowNumbersOrderKeysKey(string value0)
            {
                _value0 = value0;
            }

            public int CompareTo(WindowLeftRowNumbersOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowLeftRowNumbersOrderKeysKey other)
            {
                return System.String.Equals(_value0, other._value0, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowLeftRowNumbersOrderKeysKey other && Equals(other);
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

        private readonly struct WindowRightRowNumbersOrderKeysKey : System.IEquatable<WindowRightRowNumbersOrderKeysKey>, System.IComparable<WindowRightRowNumbersOrderKeysKey>
        {
            private readonly string _value0;
            public WindowRightRowNumbersOrderKeysKey(string value0)
            {
                _value0 = value0;
            }

            public int CompareTo(WindowRightRowNumbersOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowRightRowNumbersOrderKeysKey other)
            {
                return System.String.Equals(_value0, other._value0, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowRightRowNumbersOrderKeysKey other && Equals(other);
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
