// === Parsed Query ===
/*
with recursive walk (Id) as (select Id from values {{ Id: 1 }} seed union all select w.Id + 1 from walk w where w.Id < 3) select Id, RowNumber() over (order by Id) as Ordinal from walk union all select Id, RowNumber() over (order by Id) from walk where Id = 3
*/

// === Logical Plan ===
/*
Cte
  Definition [walk]
    RecursiveCte [walk] [All]
      Anchor
        MultiStatement
          Project [seed.Id as Id]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(w.Id + 1) as w.Id + 1]
            Filter [(w.Id < 3)]
              CteRef [walk as w]
  Query
    SetOp [UnionAll]
      MultiStatement
        Project [walk.Id as Id, WindowRef(0) as Ordinal]
          Window [RowNumber(idx:0; order: walk.Id)]
            CteRef [walk as walk]
      MultiStatement
        Project [walk.Id as Id, WindowRef(0) as Musoq.Parser.Nodes.AccessMethodNode over Musoq.Parser.Nodes.WindowSpecificationNode]
          Window [RowNumber(idx:0; order: walk.Id)]
            Filter [(walk.Id = 3)]
              CteRef [walk as walk]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [walk]
    PhysicalRecursiveCte [walk] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Id as Id]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(w.Id + 1) as w.Id + 1]
            PhysicalFilter [(w.Id < 3)]
              PhysicalCteRef [walk as w]
  Query
    PhysicalSetOp [UnionAll]
      PhysicalMultiStatement
        PhysicalProject [walk.Id as Id, WindowRef(0) as Ordinal]
          PhysicalWindow [RowNumber(idx:0; order: walk.Id)]
            PhysicalMaterialize
              PhysicalCteRef [walk as walk]
      PhysicalMultiStatement
        PhysicalProject [walk.Id as Id, WindowRef(0) as Musoq.Parser.Nodes.AccessMethodNode over Musoq.Parser.Nodes.WindowSpecificationNode]
          PhysicalWindow [RowNumber(idx:0; order: walk.Id)]
            PhysicalMaterialize
              PhysicalFilter [(walk.Id = 3)]
                PhysicalCteRef [walk as walk]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
    Generated [Cte0Row0]
      Id: int <- field Id
    TableRow [w]
      Id: int <- field Id
    TableRow [walk]
      Id: int <- field Id
    Generated [LeftRow0]
      Id: int <- field Id
      Ordinal: long <- field Ordinal
    TableRow [walk]
      Id: int <- field Id
    Generated [RightRow0]
      Id: int <- field Id
      Musoq.Parser.Nodes.AccessMethodNode over Musoq.Parser.Nodes.WindowSpecificationNode: long <- field Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode

  Body
    RecursiveCte [walk; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValues0C8F87F6Row0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: seed.Id); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [w in cte0CurrentFrontier]
          If [(w.Id < 3)]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: (w.Id + 1)); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    Materialize [_cteRowResults.Slot0 -> leftWindowRows]
    ComputeRowNumberWindow [leftRowNumbers <- leftWindowRows order by walk.Id ASC]
    CreateRowBuffer [left: List<LeftRow0>]
    ForEachIndexed [windowIndex, walk in leftWindowRows]
      AppendRowBuffer [left <- LeftRow0(Id: walk.Id, Ordinal: leftRowNumbers[windowIndex])]
    MaterializeFiltered [_cteRowResults.Slot0 where (walk.Id = 3) -> rightWindowRows]
    ComputeRowNumberWindow [rightRowNumbers <- rightWindowRows order by walk.Id ASC]
    CreateRowBuffer [right: List<RightRow0>]
    ForEachIndexed [windowIndex, walk in rightWindowRows]
      AppendRowBuffer [right <- RightRow0(Id: walk.Id, Musoq.Parser.Nodes.AccessMethodNode over Musoq.Parser.Nodes.WindowSpecificationNode: rightRowNumbers[windowIndex])]
    SetOperation [result = left UnionAll right, AppendLoop]
    ReturnDeferredTable [result: LeftRow0 <- LeftShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q214_RecursiveOuterWindowAndSet
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
        private static readonly Column[] __columns_compiled_left_0 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Ordinal", typeof(long), 1)
        };
        private static readonly Column[] __columns_compiled_right_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Musoq.Parser.Nodes.AccessMethodNode over Musoq.Parser.Nodes.WindowSpecificationNode", typeof(long), 1)
        };
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
            return QueryRows.DeferredTable<LeftRow0>("result", __columns_compiled_left_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<LeftRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new LeftRow0(__musoqShapeRow.Id, __musoqShapeRow.Ordinal);
            }
        }

        private IEnumerable<LeftShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
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
                var cte0 = new List<Cte0Row0>();
                var cte0CurrentFrontier = new List<Cte0Row0>();
                var cte0NextFrontier = new List<Cte0Row0>();
                int __cte0Iteration = 0;
                int __cte0CancellationCounter = 0;
                seedValues0C8F87F6Row0[] cte0CurrentFrontier_seedRows = new seedValues0C8F87F6Row0[]
                {
                    new seedValues0C8F87F6Row0(1)
                };
                foreach (var seed in cte0CurrentFrontier_seedRows)
                {
                    token.ThrowIfCancellationRequested();
                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                    {
                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                    }

                    cte0CurrentFrontier.Add(new Cte0Row0(seed.Id));
                }

                cte0.AddRange(cte0CurrentFrontier);
                while (cte0CurrentFrontier.Count > 0)
                {
                    if ((__cte0Iteration & 63) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    if (__cte0Iteration >= 1000)
                    {
                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                    }

                    __cte0Iteration++;
                    cte0NextFrontier.Clear();
                    for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                    {
                        if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte0Row0 w = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                        if ((w.Id < 3))
                        {
                            ++__cte0CancellationCounter;
                            if ((__cte0CancellationCounter & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte0NextFrontier.Add(new Cte0Row0((w.Id + 1)));
                        }
                    }

                    cte0.AddRange(cte0NextFrontier);
                    var __cte0FrontierSwap = cte0CurrentFrontier;
                    cte0CurrentFrontier = cte0NextFrontier;
                    cte0NextFrontier = __cte0FrontierSwap;
                }

                _cteRowResults.Slot0 = cte0;
                var leftWindowRows = EvaluationHelper.MaterializeGeneratedRows<Cte0Row0>(_cteRowResults.Slot0);
                var leftRowNumbersOrderKeys = new WindowLeftRowNumbersOrderKeysKey[leftWindowRows.Count];
                ExtractLeftRowNumbersWindowKeys(leftWindowRows, leftRowNumbersOrderKeys);
                var leftRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(leftWindowRows.Count, null);
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
                var rightWindowRows = EvaluationHelper.MaterializeFilteredGeneratedRows<Cte0Row0>(_cteRowResults.Slot0, walk => (walk.Id == 3));
                var rightRowNumbersOrderKeys = new WindowRightRowNumbersOrderKeysKey[rightWindowRows.Count];
                ExtractRightRowNumbersWindowKeys(rightWindowRows, rightRowNumbersOrderKeys);
                var rightRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(rightWindowRows.Count, null);
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
                foreach (var resultLeftRow in left)
                {
                    __musoqFinalShapeRows.Add(new LeftShape0((int)resultLeftRow.Id, (long)resultLeftRow.Ordinal));
                }

                foreach (var resultRightRow in right)
                {
                    __musoqFinalShapeRows.Add(new LeftShape0((int)resultRightRow.Id, (long)resultRightRow.Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode));
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
                Cte0Row0 walk = leftWindowRows[windowIndex];
                left.Add(new LeftRow0(walk.Id, (long)leftRowNumbers[windowIndex]));
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void AppendRightWindowRows1(IReadOnlyList<Cte0Row0> rightWindowRows, List<RightRow0> right, long[] rightRowNumbers)
        {
            for (int windowIndex = 0; windowIndex < rightWindowRows.Count; ++windowIndex)
            {
                Cte0Row0 walk = rightWindowRows[windowIndex];
                right.Add(new RightRow0(walk.Id, (long)rightRowNumbers[windowIndex]));
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ExtractLeftRowNumbersWindowKeys(IReadOnlyList<Cte0Row0> leftWindowRows, WindowLeftRowNumbersOrderKeysKey[] leftRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < leftWindowRows.Count; ++windowIndex)
            {
                Cte0Row0 walk = leftWindowRows[windowIndex];
                leftRowNumbersOrderKeys[windowIndex] = new WindowLeftRowNumbersOrderKeysKey(walk.Id);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ExtractRightRowNumbersWindowKeys(IReadOnlyList<Cte0Row0> rightWindowRows, WindowRightRowNumbersOrderKeysKey[] rightRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < rightWindowRows.Count; ++windowIndex)
            {
                Cte0Row0 walk = rightWindowRows[windowIndex];
                rightRowNumbersOrderKeys[windowIndex] = new WindowRightRowNumbersOrderKeysKey(walk.Id);
            }
        }

        private readonly struct Cte0Row0
        {
            public Cte0Row0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class LeftRow0 : Row
        {
            public LeftRow0(int __value0, long __value1)
            {
                Id = __value0;
                Ordinal = __value1;
            }

            public override int Count => 2;
            public int Id { get; private set; }
            public long Ordinal { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        Ordinal = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Ordinal" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Ordinal,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Ordinal" => (object)Ordinal,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class LeftShape0
        {
            public LeftShape0(int Id, long Ordinal)
            {
                this.Id = Id;
                this.Ordinal = Ordinal;
            }

            public int Id { get; }
            public long Ordinal { get; }
        }

        private sealed class RightRow0 : Row
        {
            public RightRow0(int __value0, long __value1)
            {
                Id = __value0;
                Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode = __value1;
            }

            public override int Count => 2;
            public int Id { get; private set; }
            public long Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Musoq.Parser.Nodes.AccessMethodNode over Musoq.Parser.Nodes.WindowSpecificationNode" => true,
                "Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode" => true,
                "WindowSpecificationNode" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Musoq.Parser.Nodes.AccessMethodNode over Musoq.Parser.Nodes.WindowSpecificationNode" => (object)Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode,
                "Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode" => (object)Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode,
                "WindowSpecificationNode" => (object)Musoq_Parser_Nodes_AccessMethodNode_over_Musoq_Parser_Nodes_WindowSpecificationNode,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private readonly struct WindowLeftRowNumbersOrderKeysKey : System.IEquatable<WindowLeftRowNumbersOrderKeysKey>, System.IComparable<WindowLeftRowNumbersOrderKeysKey>
        {
            private readonly int _value0;
            public WindowLeftRowNumbersOrderKeysKey(int value0)
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
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowLeftRowNumbersOrderKeysKey other && Equals(other);
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

        private readonly struct WindowRightRowNumbersOrderKeysKey : System.IEquatable<WindowRightRowNumbersOrderKeysKey>, System.IComparable<WindowRightRowNumbersOrderKeysKey>
        {
            private readonly int _value0;
            public WindowRightRowNumbersOrderKeysKey(int value0)
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
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowRightRowNumbersOrderKeysKey other && Equals(other);
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

        private sealed class seedValues0C8F87F6Row0 : Row
        {
            public seedValues0C8F87F6Row0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;
            public int Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
