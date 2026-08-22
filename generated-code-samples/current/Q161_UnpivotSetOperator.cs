// === Parsed Query ===
/*
unpivot #A.entities() s
                  on Metric in (s.Population as Population)
                  using Amount
                  keep s.Name as Name
                  union all (Name, Metric, Amount)
                  unpivot #B.entities() s
                  on Metric in (s.Money as Money)
                  using Amount
                  keep s.Name as Name
                  order by Name
*/

// === Logical Plan ===
/*
SetOp [UnionAll]
  MultiStatement
    Project [__unpivot.Name as Name, __unpivot.Metric as Metric, __unpivot.Amount as Amount]
      Unpivot [name: Metric; value: Amount; entries: s.Population as Population; keep: s.Name as Name] as __unpivot
        SchemaScan [#A.entities() as s]
  MultiStatement
    Sort [__unpivot.Name]
      Project [__unpivot.Name as Name, __unpivot.Metric as Metric, __unpivot.Amount as Amount]
        Unpivot [name: Metric; value: Amount; entries: s.Money as Money; keep: s.Name as Name] as __unpivot
          SchemaScan [#B.entities() as s]
*/

// === Physical Plan ===
/*
PhysicalSetOp [UnionAll]
  PhysicalMultiStatement
    PhysicalProject [__unpivot.Name as Name, __unpivot.Metric as Metric, __unpivot.Amount as Amount]
      PhysicalUnpivot [name: Metric; value: Amount; entries: s.Population as Population; keep: s.Name as Name] as __unpivot
        PhysicalSchemaScan [#A.entities() as s]
  PhysicalMultiStatement
    PhysicalSort [__unpivot.Name]
      PhysicalProject [__unpivot.Name as Name, __unpivot.Metric as Metric, __unpivot.Amount as Amount]
        PhysicalUnpivot [name: Metric; value: Amount; entries: s.Money as Money; keep: s.Name as Name] as __unpivot
          PhysicalSchemaScan [#B.entities() as s]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [s: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
      Money: decimal <- property Money
    UnknownShape [ValuesRowShape]
      Name: string <- field Name
      Metric: string <- field Metric
      Amount: decimal <- field Amount
    Generated [LeftRow0]
      Name: string <- field Name
      Metric: string <- field Metric
      Amount: decimal <- field Amount
    SourceEntity [s: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
      Money: decimal <- property Money
    UnknownShape [ValuesRowShape]
      Name: string <- field Name
      Metric: string <- field Metric
      Amount: decimal <- field Amount
    Generated [RightRow0]
      Name: string <- field Name
      Metric: string <- field Metric
      Amount: decimal <- field Amount

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:left]
    PhaseBoundary [From:left]
    PhaseBoundary [From]
    SourceScan [s: BasicEntity] -> left_sRows
    CreateRowBuffer [left: List<LeftRow0>]
    PhaseBoundary [Select:left]
    PhaseBoundary [Select]
    ChunkedForEach [s in left_sRows]
      ScopedBlock
        CreateGeneratedRow [__unpivot <- __unpivotUnpivot3E19C520Row0(Name: s.Name, Metric: 'Population', Amount: s.Population)]
        AppendRowBuffer [left <- LeftRow0(Name: __unpivot.Name, Metric: __unpivot.Metric, Amount: __unpivot.Amount)]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [s: BasicEntity] -> right_sRows
    CreateRowBuffer [right: List<RightRow0>]
    PhaseBoundary [Select:right]
    ChunkedForEach [s in right_sRows]
      ScopedBlock
        CreateGeneratedRow [__unpivot <- __unpivotUnpivotEE6A24A5Row0(Name: s.Name, Metric: 'Money', Amount: s.Money)]
        AppendRowBuffer [right <- RightRow0(Name: __unpivot.Name, Metric: __unpivot.Metric, Amount: __unpivot.Amount)]
    SortRowBuffer [right -> rightSorted by Name ASC]
    PhaseBoundary [End:right]
    SetOperation [result = left UnionAll rightSorted, AppendLoop]
    ReturnDeferredTable [result: LeftRow0 <- LeftShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q161_UnpivotSetOperator
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
        private static readonly Column[] __columns_compiled_left_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Metric", typeof(string), 1),
            new Column("Amount", typeof(decimal), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_s_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Population", typeof(decimal), 13), new Column("Money", typeof(decimal), 15) });
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
            return QueryRows.DeferredTable<LeftRow0>("result", __columns_compiled_left_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<LeftRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new LeftRow0(__musoqShapeRow.Name, __musoqShapeRow.Metric, __musoqShapeRow.Amount);
            }
        }

        private IEnumerable<LeftShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<LeftShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __left_sSchema = provider.GetSchema("#A");
                var left_sRowsSource = __left_sSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("s:1", sourceExecutionPlans["s:1"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var left_sRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(left_sRowsSource.Chunks, __musoqProgressContext, "s:1") : left_sRowsSource.Chunks;
                var left = new List<LeftRow0>();
                OnPhaseChanged("compiled:left", QueryPhase.Select);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var sChunk in left_sRows)
                {
                    if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> sChunkView)
                    {
                        if (sChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] sChunkViewArray)
                        {
                            int sChunkViewOffset = sChunkView.Offset;
                            for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                            {
                                if ((sIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                {
                                    __unpivotUnpivot3E19C520Row0 __unpivot = new __unpivotUnpivot3E19C520Row0(s.Name, "Population", s.Population);
                                    left.Add(new LeftRow0(__unpivot.Name, __unpivot.Metric, __unpivot.Amount));
                                }
                            }

                            continue;
                        }

                        if (sChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> sChunkViewList)
                        {
                            int sChunkViewOffset = sChunkView.Offset;
                            for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                            {
                                if ((sIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var s = sChunkViewList[sChunkViewOffset + sIndex];
                                {
                                    __unpivotUnpivot3E19C520Row0 __unpivot = new __unpivotUnpivot3E19C520Row0(s.Name, "Population", s.Population);
                                    left.Add(new LeftRow0(__unpivot.Name, __unpivot.Metric, __unpivot.Amount));
                                }
                            }

                            continue;
                        }
                    }

                    for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                    {
                        if ((sIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var s = sChunk[sIndex];
                        {
                            __unpivotUnpivot3E19C520Row0 __unpivot = new __unpivotUnpivot3E19C520Row0(s.Name, "Population", s.Population);
                            left.Add(new LeftRow0(__unpivot.Name, __unpivot.Metric, __unpivot.Amount));
                        }
                    }
                }

                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.Begin);
                OnPhaseChanged("compiled:right", QueryPhase.From);
                var __right_sSchema = provider.GetSchema("#B");
                var right_sRowsSource = __right_sSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("s:2", sourceExecutionPlans["s:2"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var right_sRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(right_sRowsSource.Chunks, __musoqProgressContext, "s:2") : right_sRowsSource.Chunks;
                var right = new List<RightRow0>();
                OnPhaseChanged("compiled:right", QueryPhase.Select);
                foreach (var sChunk in right_sRows)
                {
                    if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> sChunkView)
                    {
                        if (sChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] sChunkViewArray)
                        {
                            int sChunkViewOffset = sChunkView.Offset;
                            for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                            {
                                if ((sIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                {
                                    __unpivotUnpivotEE6A24A5Row0 __unpivot = new __unpivotUnpivotEE6A24A5Row0(s.Name, "Money", s.Money);
                                    right.Add(new RightRow0(__unpivot.Name, __unpivot.Metric, __unpivot.Amount));
                                }
                            }

                            continue;
                        }

                        if (sChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> sChunkViewList)
                        {
                            int sChunkViewOffset = sChunkView.Offset;
                            for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                            {
                                if ((sIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var s = sChunkViewList[sChunkViewOffset + sIndex];
                                {
                                    __unpivotUnpivotEE6A24A5Row0 __unpivot = new __unpivotUnpivotEE6A24A5Row0(s.Name, "Money", s.Money);
                                    right.Add(new RightRow0(__unpivot.Name, __unpivot.Metric, __unpivot.Amount));
                                }
                            }

                            continue;
                        }
                    }

                    for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                    {
                        if ((sIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var s = sChunk[sIndex];
                        {
                            __unpivotUnpivotEE6A24A5Row0 __unpivot = new __unpivotUnpivotEE6A24A5Row0(s.Name, "Money", s.Money);
                            right.Add(new RightRow0(__unpivot.Name, __unpivot.Metric, __unpivot.Amount));
                        }
                    }
                }

                var rightSortedRows = right.OrderBy((row) => row, RightRow0OrderBy_0AComparer.Instance);
                var rightSorted = new List<RightRow0>();
                rightSorted.EnsureCapacity(right.Count);
                foreach (var copiedRow in rightSortedRows)
                {
                    rightSorted.Add(copiedRow);
                }

                OnPhaseChanged("compiled:right", QueryPhase.End);
                foreach (var resultLeftRow in left)
                {
                    __musoqFinalShapeRows.Add(new LeftShape0((string)resultLeftRow.Name, (string)resultLeftRow.Metric, (decimal)resultLeftRow.Amount));
                }

                foreach (var resultRightRow in rightSorted)
                {
                    __musoqFinalShapeRows.Add(new LeftShape0((string)resultRightRow.Name, (string)resultRightRow.Metric, (decimal)resultRightRow.Amount));
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

        private sealed class LeftRow0 : Row
        {
            public LeftRow0(string __value0, string __value1, decimal __value2)
            {
                Name = __value0;
                Metric = __value1;
                Amount = __value2;
            }

            public decimal Amount { get; private set; }
            public override int Count => 3;
            public string Metric { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Metric = (string)value;
                        break;
                    case 2:
                        Amount = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Metric" => true,
                "Amount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Metric,
                2 => (object)Amount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Metric" => (object)Metric,
                "Amount" => (object)Amount,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class LeftShape0
        {
            public LeftShape0(string Name, string Metric, decimal Amount)
            {
                this.Name = Name;
                this.Metric = Metric;
                this.Amount = Amount;
            }

            public decimal Amount { get; }
            public string Metric { get; }
            public string Name { get; }
        }

        private sealed class RightRow0 : Row
        {
            public RightRow0(string __value0, string __value1, decimal __value2)
            {
                Name = __value0;
                Metric = __value1;
                Amount = __value2;
            }

            public decimal Amount { get; private set; }
            public override int Count => 3;
            public string Metric { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Metric = (string)value;
                        break;
                    case 2:
                        Amount = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Metric" => true,
                "Amount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Metric,
                2 => (object)Amount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Metric" => (object)Metric,
                "Amount" => (object)Amount,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class RightRow0OrderBy_0AComparer : IComparer<RightRow0>
        {
            public static readonly RightRow0OrderBy_0AComparer Instance = new RightRow0OrderBy_0AComparer();
            public int Compare(RightRow0 left, RightRow0 right)
            {
                var comparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
                if (comparison != 0)
                    return comparison;
                return 0;
            }
        }

        private sealed class __unpivotUnpivot3E19C520Row0 : Row
        {
            public __unpivotUnpivot3E19C520Row0(string __value0, string __value1, decimal __value2)
            {
                Name = __value0;
                Metric = __value1;
                Amount = __value2;
            }

            public decimal Amount { get; private set; }
            public override int Count => 3;
            public string Metric { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Metric = (string)value;
                        break;
                    case 2:
                        Amount = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Metric" => true,
                "Amount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Metric,
                2 => (object)Amount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Metric" => (object)Metric,
                "Amount" => (object)Amount,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class __unpivotUnpivotEE6A24A5Row0 : Row
        {
            public __unpivotUnpivotEE6A24A5Row0(string __value0, string __value1, decimal __value2)
            {
                Name = __value0;
                Metric = __value1;
                Amount = __value2;
            }

            public decimal Amount { get; private set; }
            public override int Count => 3;
            public string Metric { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Metric = (string)value;
                        break;
                    case 2:
                        Amount = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Metric" => true,
                "Amount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Metric,
                2 => (object)Amount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Metric" => (object)Metric,
                "Amount" => (object)Amount,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
