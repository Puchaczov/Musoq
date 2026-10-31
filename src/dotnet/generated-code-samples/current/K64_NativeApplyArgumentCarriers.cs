// === Parsed Query ===
/*
select a.Id as Id, b.Amount as Amount, b.Label as Label
from #native.rows() a
cross apply #native.echo(a.Amount, a.Label) b
order by Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Id as a.Id, a.Amount as a.Amount, a.Label as a.Label, b.Amount as b.Amount, b.Label as b.Label]
    Apply [Cross]
      SchemaScan [#native.rows() as a]
      SchemaScan [#native.echo(a.Amount, a.Label) as b]
  Sort [a.Id]
    Project [a.Id as Id, b.Amount as Amount, b.Label as Label]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Id as a.Id, b.Amount as b.Amount, b.Label as b.Label]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#native.rows() as a]
      PhysicalSchemaScan [#native.echo(a.Amount, a.Label) as b]
  PhysicalSort [a.Id]
    PhysicalProject [a.Id as Id, b.Amount as Amount, b.Label as Label]
      PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: NativeEntity]
      Id: int <- property Id
      Amount: decimal? <- property Amount
      Label: string <- property Label
    SourceEntity [b: NativeEntity]
      Amount: decimal? <- property Amount
      Label: string <- property Label
    Generated [Statement0Row0]
      a.Id: int <- field a_Id
      b.Amount: decimal? <- field b_Amount
      b.Label: string <- field b_Label
    TableRow [ab]
      a.Id: int <- field a_Id
      b.Amount: decimal? <- field b_Amount
      b.Label: string <- field b_Label
    Generated [ResultRow0]
      Id: int <- field Id
      Amount: decimal? <- field Amount
      Label: string <- field Label

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: NativeEntity] -> statement0_aRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [a in statement0_aRows]
      Let [aId: int = a.Id]
      SourceScan [b: NativeEntity] -> statement0_bRows
      ChunkedForEach [b in statement0_bRows]
        AppendRow [statement0 <- Statement0Row0(a.Id: aId, b.Amount: b.Amount, b.Label: b.Label)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ab in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: ab.a.Id, Amount: ab.b.Amount, Label: ab.b.Label)]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K64_NativeApplyArgumentCarriers
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
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Amount", typeof(decimal?), 1),
            new Column("Label", typeof(string), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("a.Id", typeof(int), 0),
            new Column("b.Amount", typeof(decimal?), 1),
            new Column("b.Label", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Amount", typeof(decimal?), 1), new Column("Label", typeof(string), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Amount", typeof(decimal?), 0), new Column("Label", typeof(string), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Amount, __musoqShapeRow.Label);
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

                    Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(ab.a_Id, ab.b_Amount, ab.b_Label) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
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
            var __statement0_aSchema = provider.GetSchema("#native");
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity>.Open(__statement0_aSchema, "rows", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var aChunk in statement0_aRows)
            {
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            int aId = a.Id;
                            var __statement0_bSchema = provider.GetSchema("#native");
                            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity>.Open(__statement0_bSchema, "echo", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_2, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create(a.Amount, global::Musoq.Schema.SourceArguments.Create(a.Label, global::Musoq.Schema.SourceArguments.Empty)));
                            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
                            foreach (var bChunk in statement0_bRows)
                            {
                                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity> bChunkView)
                                {
                                    if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity[] bChunkViewArray)
                                    {
                                        int bChunkViewOffset = bChunkView.Offset;
                                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                        {
                                            if ((bIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                            statement0.Add(new Statement0Row0(aId, b.Amount, b.Label));
                                        }

                                        continue;
                                    }

                                    if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity> bChunkViewList)
                                    {
                                        int bChunkViewOffset = bChunkView.Offset;
                                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                        {
                                            if ((bIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                                            statement0.Add(new Statement0Row0(aId, b.Amount, b.Label));
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
                                    statement0.Add(new Statement0Row0(aId, b.Amount, b.Label));
                                }
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            int aId = a.Id;
                            var __statement0_bSchema = provider.GetSchema("#native");
                            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity>.Open(__statement0_bSchema, "echo", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_2, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create(a.Amount, global::Musoq.Schema.SourceArguments.Create(a.Label, global::Musoq.Schema.SourceArguments.Empty)));
                            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
                            foreach (var bChunk in statement0_bRows)
                            {
                                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity> bChunkView)
                                {
                                    if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity[] bChunkViewArray)
                                    {
                                        int bChunkViewOffset = bChunkView.Offset;
                                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                        {
                                            if ((bIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                            statement0.Add(new Statement0Row0(aId, b.Amount, b.Label));
                                        }

                                        continue;
                                    }

                                    if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity> bChunkViewList)
                                    {
                                        int bChunkViewOffset = bChunkView.Offset;
                                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                        {
                                            if ((bIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                                            statement0.Add(new Statement0Row0(aId, b.Amount, b.Label));
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
                                    statement0.Add(new Statement0Row0(aId, b.Amount, b.Label));
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
                    int aId = a.Id;
                    var __statement0_bSchema = provider.GetSchema("#native");
                    var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity>.Open(__statement0_bSchema, "echo", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_2, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create(a.Amount, global::Musoq.Schema.SourceArguments.Create(a.Label, global::Musoq.Schema.SourceArguments.Empty)));
                    var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
                    foreach (var bChunk in statement0_bRows)
                    {
                        if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity> bChunkView)
                        {
                            if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity[] bChunkViewArray)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                    statement0.Add(new Statement0Row0(aId, b.Amount, b.Label));
                                }

                                continue;
                            }

                            if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTests.NativeEntity> bChunkViewList)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewList[bChunkViewOffset + bIndex];
                                    statement0.Add(new Statement0Row0(aId, b.Amount, b.Label));
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
                            statement0.Add(new Statement0Row0(aId, b.Amount, b.Label));
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
            public ResultRow0(int __value0, decimal? __value1, string __value2)
            {
                Id = __value0;
                Amount = __value1;
                Label = __value2;
            }

            public decimal? Amount { get; private set; }
            public override int Count => 3;
            public int Id { get; private set; }
            public string Label { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Amount = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Amount),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Amount" => true,
                "Label" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Amount, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Label, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, decimal? Amount, string Label)
            {
                this.Id = Id;
                this.Amount = Amount;
                this.Label = Label;
            }

            public decimal? Amount { get; }
            public int Id { get; }
            public string Label { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, decimal? __value1, string __value2)
            {
                a_Id = __value0;
                b_Amount = __value1;
                b_Label = __value2;
            }

            public int a_Id { get; }
            public decimal? b_Amount { get; }
            public string b_Label { get; }
        }
    }
}
