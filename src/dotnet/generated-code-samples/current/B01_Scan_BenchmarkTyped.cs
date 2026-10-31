// === Parsed Query ===
/*
select r.Id as Id, r.Name as Name from bench.rows() r order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [r.Id]
    Project [r.Id as Id, r.Name as Name]
      SchemaScan [#bench.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, r.Name as Name]
      PhysicalSchemaScan [#bench.rows() as r]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Id: int <- property Id
      Name: string <- property Name
    GeneratedRecord [ResultRow0WithSortKeys]
      Id: int <- field Id
      Name: string <- field Name
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Id: int <- field Id
      Name: string <- field Name

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: BenchmarkSampleRow] -> rRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Id: r.Id, Name: r.Name)]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Id ASC]
    TransferOwnedRows [resultOrderRecords -> result: ResultRow0WithSortKeys]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0WithSortKeys]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B01_Scan_BenchmarkTyped
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

    public sealed class CompiledQuery : BaseOperations, ITypedRunnable<Musoq.Evaluator.Tests.BenchmarkProjectionOutput>, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Name", typeof(string), 1) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Name", typeof(string), 1)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public IEnumerable<Musoq.Evaluator.Tests.BenchmarkProjectionOutput> Run(TypedQueryRunOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            QueryRunContext? __musoqEnumerationRunContext = null;
            var __musoqInitialRunContext = QueryRunContext.Capture(options, this);
            return new QueryEnumerable<Musoq.Evaluator.Tests.BenchmarkProjectionOutput>((queryToken) =>
            {
                var queryContext = new QueryRunContext(queryToken, __musoqInitialRunContext.TypedParameters, __musoqInitialRunContext.PhaseChanged, __musoqInitialRunContext.DataSourceProgress, __musoqInitialRunContext.Sender, queryId: __musoqInitialRunContext.QueryId, queryProgress: __musoqInitialRunContext.QueryProgress, queryProgressOptions: __musoqInitialRunContext.QueryProgress == null ? null : __musoqInitialRunContext.QueryProgressOptions)
                {
                    TypedParameters = __musoqInitialRunContext.TypedParameters
                };
                __musoqEnumerationRunContext = queryContext;
                return ComputeRows_compiled_0(__musoqInitialRunContext.Provider, __musoqInitialRunContext.SourceRuntimeSettingsBySourceContextId, __musoqInitialRunContext.SourceExecutionPlans, __musoqInitialRunContext.Logger, queryContext);
            }, options.CancellationToken, onCompleted: __musoqInitialRunContext.QueryProgress == null ? null : (Action)(() => __musoqEnumerationRunContext?.CompleteQueryProgress()), onException: __musoqInitialRunContext.QueryProgress == null ? null : (Action<Exception>)(_ => __musoqEnumerationRunContext?.CompleteQueryProgress()), onDisposed: __musoqInitialRunContext.QueryProgress == null ? null : (Action)(() => __musoqEnumerationRunContext?.CompleteQueryProgress()));
        }

        public IEnumerable<Musoq.Evaluator.Tests.BenchmarkProjectionOutput> Run(CancellationToken token)
        {
            return Run(new TypedQueryRunOptions(token, TypedParameters, PhaseChanged, DataSourceProgress, QueryProgress, null));
        }

        private IEnumerable<ResultRow0WithSortKeys> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __rSchema = provider.GetSchema("#bench");
                var rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var rChunk in rRows)
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
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(r.Id, r.Name, resultOrderRecords.Count));
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
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(r.Id, r.Name, resultOrderRecords.Count));
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
                        resultOrderRecords.Add(new ResultRow0WithSortKeys(r.Id, r.Name, resultOrderRecords.Count));
                    }
                }

                resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                return resultOrderRecords;
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

        private IEnumerable<Musoq.Evaluator.Tests.BenchmarkProjectionOutput> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return new Musoq.Evaluator.Tests.BenchmarkProjectionOutput((int)__musoqShapeRow.Id, (string)__musoqShapeRow.Name);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        private sealed class ResultRow0WithSortKeys
        {
            public ResultRow0WithSortKeys(int Id, string Name, int __ordinal)
            {
                this.Id = Id;
                this.Name = Name;
                this.__ordinal = __ordinal;
            }

            public int Id { get; }
            public string Name { get; }
            public int __ordinal { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }
            public string Name { get; private set; }

            public ResultRow0(int __value0, string __value1)
            {
                Id = __value0;
                Name = __value1;
            }

            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Name, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Name" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class ResultRow0WithSortKeysComparer : IComparer<ResultRow0WithSortKeys>
        {
            public static readonly ResultRow0WithSortKeysComparer Instance = new ResultRow0WithSortKeysComparer();
            public int Compare(ResultRow0WithSortKeys left, ResultRow0WithSortKeys right)
            {
                var comparison = left.Id.CompareTo(right.Id);
                if (comparison != 0)
                    return comparison;
                return left.__ordinal.CompareTo(right.__ordinal);
            }
        }
    }
}
