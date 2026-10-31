// === Parsed Query ===
/*
select r.Id as Id, i.Value as Value, i.Ordinal as Position
from bench.rows() r
cross apply r.Items i with ordinality
order by r.Id, Position
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Items as r.Items, i.Value as i.Value, i.Ordinal as i.Ordinal]
    Apply [Cross, with ordinality]
      SchemaScan [#bench.rows() as r]
      PropertySource [r.Items as i] [apply: Cross] [type: Int32[]]
  Sort [r.Id, i.Ordinal]
    Project [r.Id as Id, i.Value as Value, i.Ordinal as Position]
      CteRef [ri as ri]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, i.Value as i.Value, i.Ordinal as i.Ordinal]
    PhysicalNestedLoopApply [Cross, with ordinality]
      PhysicalSchemaScan [#bench.rows() as r]
      PhysicalPropertySource [r.Items as i] [apply: Cross] [type: Int32[]]
  PhysicalSort [r.Id, i.Ordinal]
    PhysicalProject [r.Id as Id, i.Value as Value, i.Ordinal as Position]
      PhysicalCteRef [ri as ri]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Id: int <- property Id
      Items: int[] <- property Items
    SourceEntity [i: int]
      Value: int <- direct scalar value
      Ordinal: int <- apply ordinality iOrdinal
    Generated [Statement0Row0]
      r.Id: int <- field r_Id
      i.Value: int <- field i_Value
      i.Ordinal: int <- field i_Ordinal
    TableRow [ri]
      r.Id: int <- field r_Id
      i.Value: int <- field i_Value
      i.Ordinal: int <- field i_Ordinal
    Generated [ResultRow0]
      Id: int <- field Id
      Value: int <- field Value
      Position: int <- field Position

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: BenchmarkSampleRow] -> statement0_rRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [r in statement0_rRows]
      Let [rId: int = r.Id]
      EnumerableSource [r.Items -> statement0_iRows]
      ForEachWithOrdinality [iOrdinal, i in statement0_iRows]
        AppendRow [statement0 <- Statement0Row0(r.Id: rId, i.Value: i.Value, i.Ordinal: i.Ordinal)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ri in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: ri.r.Id, Value: ri.i.Value, Position: ri.i.Ordinal)]
    SortOwnedBuffer [result by Id ASC, Position ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B13_Apply_BenchmarkTable
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Value", typeof(int), 1),
            new Column("Position", typeof(int), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("r.Id", typeof(int), 0),
            new Column("i.Value", typeof(int), 1),
            new Column("i.Ordinal", typeof(int), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Items", typeof(int[]), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Value, __musoqShapeRow.Position);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 ri = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(ri.r_Id, ri.i_Value, ri.i_Ordinal) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.Position.CompareTo(right.Position);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_rSchema = provider.GetSchema("#bench");
            var statement0_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var rChunk in statement0_rRows)
            {
                if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkView)
                {
                    if (rChunkView.Source is Musoq.Evaluator.Tests.BenchmarkSampleRow[] rChunkViewArray)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewArray[rChunkViewOffset + rIndex];
                            int rId = r.Id;
                            var statement0_iRows = (IEnumerable<int>)r.Items ?? Array.Empty<int>();
                            var iOrdinalRows = statement0_iRows;
                            if (iOrdinalRows is int[] iOrdinalArray)
                                for (int iOrdinal = 0, iOrdinalCount = iOrdinalArray.Length; iOrdinal < iOrdinalCount; ++iOrdinal)
                                {
                                    if ((iOrdinal & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    int i = iOrdinalArray[iOrdinal];
                                    statement0.Add(new Statement0Row0(rId, i, iOrdinal));
                                }
                            else if (iOrdinalRows is List<int> iOrdinalConcreteList)
                                for (int iOrdinal = 0, iOrdinalCount = iOrdinalConcreteList.Count; iOrdinal < iOrdinalCount; ++iOrdinal)
                                {
                                    if ((iOrdinal & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    int i = iOrdinalConcreteList[iOrdinal];
                                    statement0.Add(new Statement0Row0(rId, i, iOrdinal));
                                }
                            else
                            {
                                int iOrdinal = 0;
                                foreach (var i in iOrdinalRows)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(rId, i, iOrdinal));
                                    ++iOrdinal;
                                }
                            }
                        }

                        continue;
                    }

                    if (rChunkView.Source is List<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkViewList)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewList[rChunkViewOffset + rIndex];
                            int rId = r.Id;
                            var statement0_iRows = (IEnumerable<int>)r.Items ?? Array.Empty<int>();
                            var iOrdinalRows = statement0_iRows;
                            if (iOrdinalRows is int[] iOrdinalArray)
                                for (int iOrdinal = 0, iOrdinalCount = iOrdinalArray.Length; iOrdinal < iOrdinalCount; ++iOrdinal)
                                {
                                    if ((iOrdinal & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    int i = iOrdinalArray[iOrdinal];
                                    statement0.Add(new Statement0Row0(rId, i, iOrdinal));
                                }
                            else if (iOrdinalRows is List<int> iOrdinalConcreteList)
                                for (int iOrdinal = 0, iOrdinalCount = iOrdinalConcreteList.Count; iOrdinal < iOrdinalCount; ++iOrdinal)
                                {
                                    if ((iOrdinal & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    int i = iOrdinalConcreteList[iOrdinal];
                                    statement0.Add(new Statement0Row0(rId, i, iOrdinal));
                                }
                            else
                            {
                                int iOrdinal = 0;
                                foreach (var i in iOrdinalRows)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(rId, i, iOrdinal));
                                    ++iOrdinal;
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                {
                    if ((rIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var r = rChunk[rIndex];
                    int rId = r.Id;
                    var statement0_iRows = (IEnumerable<int>)r.Items ?? Array.Empty<int>();
                    var iOrdinalRows = statement0_iRows;
                    if (iOrdinalRows is int[] iOrdinalArray)
                        for (int iOrdinal = 0, iOrdinalCount = iOrdinalArray.Length; iOrdinal < iOrdinalCount; ++iOrdinal)
                        {
                            if ((iOrdinal & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            int i = iOrdinalArray[iOrdinal];
                            statement0.Add(new Statement0Row0(rId, i, iOrdinal));
                        }
                    else if (iOrdinalRows is List<int> iOrdinalConcreteList)
                        for (int iOrdinal = 0, iOrdinalCount = iOrdinalConcreteList.Count; iOrdinal < iOrdinalCount; ++iOrdinal)
                        {
                            if ((iOrdinal & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            int i = iOrdinalConcreteList[iOrdinal];
                            statement0.Add(new Statement0Row0(rId, i, iOrdinal));
                        }
                    else
                    {
                        int iOrdinal = 0;
                        foreach (var i in iOrdinalRows)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0.Add(new Statement0Row0(rId, i, iOrdinal));
                            ++iOrdinal;
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
            public ResultRow0(int __value0, int __value1, int __value2)
            {
                Id = __value0;
                Value = __value1;
                Position = __value2;
            }

            public override int Count => 3;
            public int Id { get; private set; }
            public int Position { get; private set; }
            public int Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Value" => true,
                "Position" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Position, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, int Value, int Position)
            {
                this.Id = Id;
                this.Value = Value;
                this.Position = Position;
            }

            public int Id { get; }
            public int Position { get; }
            public int Value { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, int __value1, int __value2)
            {
                r_Id = __value0;
                i_Value = __value1;
                i_Ordinal = __value2;
            }

            public int i_Ordinal { get; }
            public int i_Value { get; }
            public int r_Id { get; }
        }
    }
}
