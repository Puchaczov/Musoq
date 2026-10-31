// === Parsed Query ===
/*
select a.Position as LeftPosition, b.Position as RightPosition from #schema.first() a inner join #schema.first() b on a.Number = b.Number and a.Text = b.Text order by LeftPosition, RightPosition
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Position as a.Position, a.Number as a.Number, a.Text as a.Text, b.Position as b.Position, b.Number as b.Number, b.Text as b.Text]
    Join [Inner] [((a.Number = b.Number) AND (a.Text = b.Text))]
      SchemaScan [#schema.first() as a]
      SchemaScan [#schema.first() as b]
  Sort [a.Position, b.Position]
    Project [a.Position as LeftPosition, b.Position as RightPosition]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Position as a.Position, b.Position as b.Position]
    PhysicalHashJoin [Inner] [build: b.Number, b.Text] [probe: a.Number, a.Text]
      PhysicalSchemaScan [#schema.first() as a]
      PhysicalSchemaScan [#schema.first() as b]
  PhysicalSort [a.Position, b.Position]
    PhysicalProject [a.Position as LeftPosition, b.Position as RightPosition]
      PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: NativeKeyInput]
      Position: int <- property Position
      Number: int? <- property Number
      Text: string <- property Text
    SourceEntity [b: NativeKeyInput]
      Position: int <- property Position
      Number: int? <- property Number
      Text: string <- property Text
    Generated [Statement0Row0]
      a.Position: int <- field a_Position
      b.Position: int <- field b_Position
    TableRow [ab]
      a.Position: int <- field a_Position
      b.Position: int <- field b_Position
    Generated [ResultRow0]
      LeftPosition: int <- field LeftPosition
      RightPosition: int <- field RightPosition

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: NativeKeyInput] -> statement0_aRows
    SourceScan [b: NativeKeyInput] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0BHash: ValueTuple<int?, string> -> NativeKeyInput]
    PhaseBoundary [Select]
    ChunkedForEach [b in statement0_bRows]
      HashAdd [statement0BHash[(b.Number, b.Text)] += b]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0BHash[(a.Number, a.Text)] -> statement0BHashMatches]
        ForEach [b in statement0BHashMatches]
          AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, b.Position: b.Position)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ab in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(LeftPosition: ab.a.Position, RightPosition: ab.b.Position)]
    SortOwnedBuffer [result by LeftPosition ASC, RightPosition ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K60_NativeResultCompositeJoin
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
            new Column("LeftPosition", typeof(int), 0),
            new Column("RightPosition", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("a.Position", typeof(int), 0),
            new Column("b.Position", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Number", typeof(int?), 1), new Column("Text", typeof(string), 2) });
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
                yield return new ResultRow0(__musoqShapeRow.LeftPosition, __musoqShapeRow.RightPosition);
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
                    result.Add(new ResultShape0(ab.a_Position, ab.b_Position) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.LeftPosition.CompareTo(right.LeftPosition);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.RightPosition.CompareTo(right.RightPosition);
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
            var __statement0_aSchema = provider.GetSchema("#schema");
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#schema");
            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0BHash = new Dictionary<ValueTuple<int?, string>, HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>>();
            foreach (var bChunk in statement0_bRows)
            {
                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput> bChunkView)
                {
                    if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput[] bChunkViewArray)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                            var key0 = b.Number;
                            var key1 = b.Text;
                            if (key0 == null || key1 == null)
                                continue;
                            var key = (key0, key1);
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }

                        continue;
                    }

                    if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput> bChunkViewList)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                            var key0 = b.Number;
                            var key1 = b.Text;
                            if (key0 == null || key1 == null)
                                continue;
                            var key = (key0, key1);
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>(b);
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
                    var key0 = b.Number;
                    var key1 = b.Text;
                    if (key0 == null || key1 == null)
                        continue;
                    var key = (key0, key1);
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>(b);
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
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            var key0 = a.Number;
                            var key1 = a.Text;
                            var key = (key0, key1);
                            if (key0 != null && key1 != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(a.Position, b.Position));
                                }
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            var key0 = a.Number;
                            var key1 = a.Text;
                            var key = (key0, key1);
                            if (key0 != null && key1 != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(a.Position, b.Position));
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
                    var key0 = a.Number;
                    var key1 = a.Text;
                    var key = (key0, key1);
                    if (key0 != null && key1 != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                    {
                        foreach (var b in statement0BHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0.Add(new Statement0Row0(a.Position, b.Position));
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
            public ResultRow0(int __value0, int __value1)
            {
                LeftPosition = __value0;
                RightPosition = __value1;
            }

            public override int Count => 2;
            public int LeftPosition { get; private set; }
            public int RightPosition { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LeftPosition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.RightPosition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LeftPosition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.RightPosition),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "LeftPosition" => true,
                "RightPosition" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.LeftPosition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.RightPosition, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int LeftPosition, int RightPosition)
            {
                this.LeftPosition = LeftPosition;
                this.RightPosition = RightPosition;
            }

            public int LeftPosition { get; }
            public int RightPosition { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, int __value1)
            {
                a_Position = __value0;
                b_Position = __value1;
            }

            public int a_Position { get; }
            public int b_Position { get; }
        }
    }
}
