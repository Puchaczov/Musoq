// === Parsed Query ===
/*
select r.Id as Id, r.Amount + 0d as C0, r.Amount + 1d as C1, r.Amount + 2d as C2, r.Amount + 3d as C3, r.Amount + 4d as C4, r.Amount + 5d as C5, r.Amount + 6d as C6, r.Amount + 7d as C7, r.Amount + 8d as C8, r.Amount + 9d as C9, r.Amount + 10d as C10, r.Amount + 11d as C11, r.Amount + 12d as C12, r.Amount + 13d as C13, r.Amount + 14d as C14, r.Amount + 15d as C15, r.Amount + 16d as C16, r.Amount + 17d as C17, r.Amount + 18d as C18, r.Amount + 19d as C19, r.Amount + 20d as C20, r.Amount + 21d as C21, r.Amount + 22d as C22, r.Amount + 23d as C23, r.Amount + 24d as C24, r.Amount + 25d as C25, r.Amount + 26d as C26, r.Amount + 27d as C27, r.Amount + 28d as C28, r.Amount + 29d as C29, r.Amount + 30d as C30, r.Amount + 31d as C31, r.Amount + 32d as C32, r.Amount + 33d as C33, r.Amount + 34d as C34, r.Amount + 35d as C35, r.Amount + 36d as C36, r.Amount + 37d as C37, r.Amount + 38d as C38, r.Amount + 39d as C39, r.Amount + 40d as C40, r.Amount + 41d as C41, r.Amount + 42d as C42, r.Amount + 43d as C43, r.Amount + 44d as C44, r.Amount + 45d as C45, r.Amount + 46d as C46, r.Amount + 47d as C47, r.Amount + 48d as C48, r.Amount + 49d as C49, r.Amount + 50d as C50, r.Amount + 51d as C51, r.Amount + 52d as C52, r.Amount + 53d as C53, r.Amount + 54d as C54, r.Amount + 55d as C55, r.Amount + 56d as C56, r.Amount + 57d as C57, r.Amount + 58d as C58, r.Amount + 59d as C59, r.Amount + 60d as C60, r.Amount + 61d as C61, r.Amount + 62d as C62, r.Amount + 63d as C63 from bench.rows() r order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [r.Id]
    Project [r.Id as Id, (r.Amount + 0) as C0, (r.Amount + 1) as C1, (r.Amount + 2) as C2, (r.Amount + 3) as C3, (r.Amount + 4) as C4, (r.Amount + 5) as C5, (r.Amount + 6) as C6, (r.Amount + 7) as C7, (r.Amount + 8) as C8, (r.Amount + 9) as C9, (r.Amount + 10) as C10, (r.Amount + 11) as C11, (r.Amount + 12) as C12, (r.Amount + 13) as C13, (r.Amount + 14) as C14, (r.Amount + 15) as C15, (r.Amount + 16) as C16, (r.Amount + 17) as C17, (r.Amount + 18) as C18, (r.Amount + 19) as C19, (r.Amount + 20) as C20, (r.Amount + 21) as C21, (r.Amount + 22) as C22, (r.Amount + 23) as C23, (r.Amount + 24) as C24, (r.Amount + 25) as C25, (r.Amount + 26) as C26, (r.Amount + 27) as C27, (r.Amount + 28) as C28, (r.Amount + 29) as C29, (r.Amount + 30) as C30, (r.Amount + 31) as C31, (r.Amount + 32) as C32, (r.Amount + 33) as C33, (r.Amount + 34) as C34, (r.Amount + 35) as C35, (r.Amount + 36) as C36, (r.Amount + 37) as C37, (r.Amount + 38) as C38, (r.Amount + 39) as C39, (r.Amount + 40) as C40, (r.Amount + 41) as C41, (r.Amount + 42) as C42, (r.Amount + 43) as C43, (r.Amount + 44) as C44, (r.Amount + 45) as C45, (r.Amount + 46) as C46, (r.Amount + 47) as C47, (r.Amount + 48) as C48, (r.Amount + 49) as C49, (r.Amount + 50) as C50, (r.Amount + 51) as C51, (r.Amount + 52) as C52, (r.Amount + 53) as C53, (r.Amount + 54) as C54, (r.Amount + 55) as C55, (r.Amount + 56) as C56, (r.Amount + 57) as C57, (r.Amount + 58) as C58, (r.Amount + 59) as C59, (r.Amount + 60) as C60, (r.Amount + 61) as C61, (r.Amount + 62) as C62, (r.Amount + 63) as C63]
      SchemaScan [#bench.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, (r.Amount + 0) as C0, (r.Amount + 1) as C1, (r.Amount + 2) as C2, (r.Amount + 3) as C3, (r.Amount + 4) as C4, (r.Amount + 5) as C5, (r.Amount + 6) as C6, (r.Amount + 7) as C7, (r.Amount + 8) as C8, (r.Amount + 9) as C9, (r.Amount + 10) as C10, (r.Amount + 11) as C11, (r.Amount + 12) as C12, (r.Amount + 13) as C13, (r.Amount + 14) as C14, (r.Amount + 15) as C15, (r.Amount + 16) as C16, (r.Amount + 17) as C17, (r.Amount + 18) as C18, (r.Amount + 19) as C19, (r.Amount + 20) as C20, (r.Amount + 21) as C21, (r.Amount + 22) as C22, (r.Amount + 23) as C23, (r.Amount + 24) as C24, (r.Amount + 25) as C25, (r.Amount + 26) as C26, (r.Amount + 27) as C27, (r.Amount + 28) as C28, (r.Amount + 29) as C29, (r.Amount + 30) as C30, (r.Amount + 31) as C31, (r.Amount + 32) as C32, (r.Amount + 33) as C33, (r.Amount + 34) as C34, (r.Amount + 35) as C35, (r.Amount + 36) as C36, (r.Amount + 37) as C37, (r.Amount + 38) as C38, (r.Amount + 39) as C39, (r.Amount + 40) as C40, (r.Amount + 41) as C41, (r.Amount + 42) as C42, (r.Amount + 43) as C43, (r.Amount + 44) as C44, (r.Amount + 45) as C45, (r.Amount + 46) as C46, (r.Amount + 47) as C47, (r.Amount + 48) as C48, (r.Amount + 49) as C49, (r.Amount + 50) as C50, (r.Amount + 51) as C51, (r.Amount + 52) as C52, (r.Amount + 53) as C53, (r.Amount + 54) as C54, (r.Amount + 55) as C55, (r.Amount + 56) as C56, (r.Amount + 57) as C57, (r.Amount + 58) as C58, (r.Amount + 59) as C59, (r.Amount + 60) as C60, (r.Amount + 61) as C61, (r.Amount + 62) as C62, (r.Amount + 63) as C63]
      PhysicalSchemaScan [#bench.rows() as r]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Id: int <- property Id
      Amount: decimal <- property Amount
    GeneratedRecord [ResultRow0WithSortKeys]
      Id: int <- field Id
      C0: decimal <- field C0
      C1: decimal <- field C1
      C2: decimal <- field C2
      C3: decimal <- field C3
      C4: decimal <- field C4
      C5: decimal <- field C5
      C6: decimal <- field C6
      C7: decimal <- field C7
      C8: decimal <- field C8
      C9: decimal <- field C9
      C10: decimal <- field C10
      C11: decimal <- field C11
      C12: decimal <- field C12
      C13: decimal <- field C13
      C14: decimal <- field C14
      C15: decimal <- field C15
      C16: decimal <- field C16
      C17: decimal <- field C17
      C18: decimal <- field C18
      C19: decimal <- field C19
      C20: decimal <- field C20
      C21: decimal <- field C21
      C22: decimal <- field C22
      C23: decimal <- field C23
      C24: decimal <- field C24
      C25: decimal <- field C25
      C26: decimal <- field C26
      C27: decimal <- field C27
      C28: decimal <- field C28
      C29: decimal <- field C29
      C30: decimal <- field C30
      C31: decimal <- field C31
      C32: decimal <- field C32
      C33: decimal <- field C33
      C34: decimal <- field C34
      C35: decimal <- field C35
      C36: decimal <- field C36
      C37: decimal <- field C37
      C38: decimal <- field C38
      C39: decimal <- field C39
      C40: decimal <- field C40
      C41: decimal <- field C41
      C42: decimal <- field C42
      C43: decimal <- field C43
      C44: decimal <- field C44
      C45: decimal <- field C45
      C46: decimal <- field C46
      C47: decimal <- field C47
      C48: decimal <- field C48
      C49: decimal <- field C49
      C50: decimal <- field C50
      C51: decimal <- field C51
      C52: decimal <- field C52
      C53: decimal <- field C53
      C54: decimal <- field C54
      C55: decimal <- field C55
      C56: decimal <- field C56
      C57: decimal <- field C57
      C58: decimal <- field C58
      C59: decimal <- field C59
      C60: decimal <- field C60
      C61: decimal <- field C61
      C62: decimal <- field C62
      C63: decimal <- field C63
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Id: int <- field Id
      C0: decimal <- field C0
      C1: decimal <- field C1
      C2: decimal <- field C2
      C3: decimal <- field C3
      C4: decimal <- field C4
      C5: decimal <- field C5
      C6: decimal <- field C6
      C7: decimal <- field C7
      C8: decimal <- field C8
      C9: decimal <- field C9
      C10: decimal <- field C10
      C11: decimal <- field C11
      C12: decimal <- field C12
      C13: decimal <- field C13
      C14: decimal <- field C14
      C15: decimal <- field C15
      C16: decimal <- field C16
      C17: decimal <- field C17
      C18: decimal <- field C18
      C19: decimal <- field C19
      C20: decimal <- field C20
      C21: decimal <- field C21
      C22: decimal <- field C22
      C23: decimal <- field C23
      C24: decimal <- field C24
      C25: decimal <- field C25
      C26: decimal <- field C26
      C27: decimal <- field C27
      C28: decimal <- field C28
      C29: decimal <- field C29
      C30: decimal <- field C30
      C31: decimal <- field C31
      C32: decimal <- field C32
      C33: decimal <- field C33
      C34: decimal <- field C34
      C35: decimal <- field C35
      C36: decimal <- field C36
      C37: decimal <- field C37
      C38: decimal <- field C38
      C39: decimal <- field C39
      C40: decimal <- field C40
      C41: decimal <- field C41
      C42: decimal <- field C42
      C43: decimal <- field C43
      C44: decimal <- field C44
      C45: decimal <- field C45
      C46: decimal <- field C46
      C47: decimal <- field C47
      C48: decimal <- field C48
      C49: decimal <- field C49
      C50: decimal <- field C50
      C51: decimal <- field C51
      C52: decimal <- field C52
      C53: decimal <- field C53
      C54: decimal <- field C54
      C55: decimal <- field C55
      C56: decimal <- field C56
      C57: decimal <- field C57
      C58: decimal <- field C58
      C59: decimal <- field C59
      C60: decimal <- field C60
      C61: decimal <- field C61
      C62: decimal <- field C62
      C63: decimal <- field C63

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: BenchmarkSampleRow] -> rRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      Let [amount: decimal = r.Amount]
      AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Id: r.Id, C0: (amount + 0), C1: (amount + 1), C2: (amount + 2), C3: (amount + 3), C4: (amount + 4), C5: (amount + 5), C6: (amount + 6), C7: (amount + 7), C8: (amount + 8), C9: (amount + 9), C10: (amount + 10), C11: (amount + 11), C12: (amount + 12), C13: (amount + 13), C14: (amount + 14), C15: (amount + 15), C16: (amount + 16), C17: (amount + 17), C18: (amount + 18), C19: (amount + 19), C20: (amount + 20), C21: (amount + 21), C22: (amount + 22), C23: (amount + 23), C24: (amount + 24), C25: (amount + 25), C26: (amount + 26), C27: (amount + 27), C28: (amount + 28), C29: (amount + 29), C30: (amount + 30), C31: (amount + 31), C32: (amount + 32), C33: (amount + 33), C34: (amount + 34), C35: (amount + 35), C36: (amount + 36), C37: (amount + 37), C38: (amount + 38), C39: (amount + 39), C40: (amount + 40), C41: (amount + 41), C42: (amount + 42), C43: (amount + 43), C44: (amount + 44), C45: (amount + 45), C46: (amount + 46), C47: (amount + 47), C48: (amount + 48), C49: (amount + 49), C50: (amount + 50), C51: (amount + 51), C52: (amount + 52), C53: (amount + 53), C54: (amount + 54), C55: (amount + 55), C56: (amount + 56), C57: (amount + 57), C58: (amount + 58), C59: (amount + 59), C60: (amount + 60), C61: (amount + 61), C62: (amount + 62), C63: (amount + 63))]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Id ASC]
    MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B01_Wide_BenchmarkTable
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("C0", typeof(decimal), 1),
            new Column("C1", typeof(decimal), 2),
            new Column("C2", typeof(decimal), 3),
            new Column("C3", typeof(decimal), 4),
            new Column("C4", typeof(decimal), 5),
            new Column("C5", typeof(decimal), 6),
            new Column("C6", typeof(decimal), 7),
            new Column("C7", typeof(decimal), 8),
            new Column("C8", typeof(decimal), 9),
            new Column("C9", typeof(decimal), 10),
            new Column("C10", typeof(decimal), 11),
            new Column("C11", typeof(decimal), 12),
            new Column("C12", typeof(decimal), 13),
            new Column("C13", typeof(decimal), 14),
            new Column("C14", typeof(decimal), 15),
            new Column("C15", typeof(decimal), 16),
            new Column("C16", typeof(decimal), 17),
            new Column("C17", typeof(decimal), 18),
            new Column("C18", typeof(decimal), 19),
            new Column("C19", typeof(decimal), 20),
            new Column("C20", typeof(decimal), 21),
            new Column("C21", typeof(decimal), 22),
            new Column("C22", typeof(decimal), 23),
            new Column("C23", typeof(decimal), 24),
            new Column("C24", typeof(decimal), 25),
            new Column("C25", typeof(decimal), 26),
            new Column("C26", typeof(decimal), 27),
            new Column("C27", typeof(decimal), 28),
            new Column("C28", typeof(decimal), 29),
            new Column("C29", typeof(decimal), 30),
            new Column("C30", typeof(decimal), 31),
            new Column("C31", typeof(decimal), 32),
            new Column("C32", typeof(decimal), 33),
            new Column("C33", typeof(decimal), 34),
            new Column("C34", typeof(decimal), 35),
            new Column("C35", typeof(decimal), 36),
            new Column("C36", typeof(decimal), 37),
            new Column("C37", typeof(decimal), 38),
            new Column("C38", typeof(decimal), 39),
            new Column("C39", typeof(decimal), 40),
            new Column("C40", typeof(decimal), 41),
            new Column("C41", typeof(decimal), 42),
            new Column("C42", typeof(decimal), 43),
            new Column("C43", typeof(decimal), 44),
            new Column("C44", typeof(decimal), 45),
            new Column("C45", typeof(decimal), 46),
            new Column("C46", typeof(decimal), 47),
            new Column("C47", typeof(decimal), 48),
            new Column("C48", typeof(decimal), 49),
            new Column("C49", typeof(decimal), 50),
            new Column("C50", typeof(decimal), 51),
            new Column("C51", typeof(decimal), 52),
            new Column("C52", typeof(decimal), 53),
            new Column("C53", typeof(decimal), 54),
            new Column("C54", typeof(decimal), 55),
            new Column("C55", typeof(decimal), 56),
            new Column("C56", typeof(decimal), 57),
            new Column("C57", typeof(decimal), 58),
            new Column("C58", typeof(decimal), 59),
            new Column("C59", typeof(decimal), 60),
            new Column("C60", typeof(decimal), 61),
            new Column("C61", typeof(decimal), 62),
            new Column("C62", typeof(decimal), 63),
            new Column("C63", typeof(decimal), 64)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Amount", typeof(decimal), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.C0, __musoqShapeRow.C1, __musoqShapeRow.C2, __musoqShapeRow.C3, __musoqShapeRow.C4, __musoqShapeRow.C5, __musoqShapeRow.C6, __musoqShapeRow.C7, __musoqShapeRow.C8, __musoqShapeRow.C9, __musoqShapeRow.C10, __musoqShapeRow.C11, __musoqShapeRow.C12, __musoqShapeRow.C13, __musoqShapeRow.C14, __musoqShapeRow.C15, __musoqShapeRow.C16, __musoqShapeRow.C17, __musoqShapeRow.C18, __musoqShapeRow.C19, __musoqShapeRow.C20, __musoqShapeRow.C21, __musoqShapeRow.C22, __musoqShapeRow.C23, __musoqShapeRow.C24, __musoqShapeRow.C25, __musoqShapeRow.C26, __musoqShapeRow.C27, __musoqShapeRow.C28, __musoqShapeRow.C29, __musoqShapeRow.C30, __musoqShapeRow.C31, __musoqShapeRow.C32, __musoqShapeRow.C33, __musoqShapeRow.C34, __musoqShapeRow.C35, __musoqShapeRow.C36, __musoqShapeRow.C37, __musoqShapeRow.C38, __musoqShapeRow.C39, __musoqShapeRow.C40, __musoqShapeRow.C41, __musoqShapeRow.C42, __musoqShapeRow.C43, __musoqShapeRow.C44, __musoqShapeRow.C45, __musoqShapeRow.C46, __musoqShapeRow.C47, __musoqShapeRow.C48, __musoqShapeRow.C49, __musoqShapeRow.C50, __musoqShapeRow.C51, __musoqShapeRow.C52, __musoqShapeRow.C53, __musoqShapeRow.C54, __musoqShapeRow.C55, __musoqShapeRow.C56, __musoqShapeRow.C57, __musoqShapeRow.C58, __musoqShapeRow.C59, __musoqShapeRow.C60, __musoqShapeRow.C61, __musoqShapeRow.C62, __musoqShapeRow.C63);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
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
                                decimal amount = r.Amount;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(r.Id, (amount + 0m), (amount + 1m), (amount + 2m), (amount + 3m), (amount + 4m), (amount + 5m), (amount + 6m), (amount + 7m), (amount + 8m), (amount + 9m), (amount + 10m), (amount + 11m), (amount + 12m), (amount + 13m), (amount + 14m), (amount + 15m), (amount + 16m), (amount + 17m), (amount + 18m), (amount + 19m), (amount + 20m), (amount + 21m), (amount + 22m), (amount + 23m), (amount + 24m), (amount + 25m), (amount + 26m), (amount + 27m), (amount + 28m), (amount + 29m), (amount + 30m), (amount + 31m), (amount + 32m), (amount + 33m), (amount + 34m), (amount + 35m), (amount + 36m), (amount + 37m), (amount + 38m), (amount + 39m), (amount + 40m), (amount + 41m), (amount + 42m), (amount + 43m), (amount + 44m), (amount + 45m), (amount + 46m), (amount + 47m), (amount + 48m), (amount + 49m), (amount + 50m), (amount + 51m), (amount + 52m), (amount + 53m), (amount + 54m), (amount + 55m), (amount + 56m), (amount + 57m), (amount + 58m), (amount + 59m), (amount + 60m), (amount + 61m), (amount + 62m), (amount + 63m), resultOrderRecords.Count));
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
                                decimal amount = r.Amount;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(r.Id, (amount + 0m), (amount + 1m), (amount + 2m), (amount + 3m), (amount + 4m), (amount + 5m), (amount + 6m), (amount + 7m), (amount + 8m), (amount + 9m), (amount + 10m), (amount + 11m), (amount + 12m), (amount + 13m), (amount + 14m), (amount + 15m), (amount + 16m), (amount + 17m), (amount + 18m), (amount + 19m), (amount + 20m), (amount + 21m), (amount + 22m), (amount + 23m), (amount + 24m), (amount + 25m), (amount + 26m), (amount + 27m), (amount + 28m), (amount + 29m), (amount + 30m), (amount + 31m), (amount + 32m), (amount + 33m), (amount + 34m), (amount + 35m), (amount + 36m), (amount + 37m), (amount + 38m), (amount + 39m), (amount + 40m), (amount + 41m), (amount + 42m), (amount + 43m), (amount + 44m), (amount + 45m), (amount + 46m), (amount + 47m), (amount + 48m), (amount + 49m), (amount + 50m), (amount + 51m), (amount + 52m), (amount + 53m), (amount + 54m), (amount + 55m), (amount + 56m), (amount + 57m), (amount + 58m), (amount + 59m), (amount + 60m), (amount + 61m), (amount + 62m), (amount + 63m), resultOrderRecords.Count));
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
                        decimal amount = r.Amount;
                        resultOrderRecords.Add(new ResultRow0WithSortKeys(r.Id, (amount + 0m), (amount + 1m), (amount + 2m), (amount + 3m), (amount + 4m), (amount + 5m), (amount + 6m), (amount + 7m), (amount + 8m), (amount + 9m), (amount + 10m), (amount + 11m), (amount + 12m), (amount + 13m), (amount + 14m), (amount + 15m), (amount + 16m), (amount + 17m), (amount + 18m), (amount + 19m), (amount + 20m), (amount + 21m), (amount + 22m), (amount + 23m), (amount + 24m), (amount + 25m), (amount + 26m), (amount + 27m), (amount + 28m), (amount + 29m), (amount + 30m), (amount + 31m), (amount + 32m), (amount + 33m), (amount + 34m), (amount + 35m), (amount + 36m), (amount + 37m), (amount + 38m), (amount + 39m), (amount + 40m), (amount + 41m), (amount + 42m), (amount + 43m), (amount + 44m), (amount + 45m), (amount + 46m), (amount + 47m), (amount + 48m), (amount + 49m), (amount + 50m), (amount + 51m), (amount + 52m), (amount + 53m), (amount + 54m), (amount + 55m), (amount + 56m), (amount + 57m), (amount + 58m), (amount + 59m), (amount + 60m), (amount + 61m), (amount + 62m), (amount + 63m), resultOrderRecords.Count));
                    }
                }

                resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                foreach (var resultRecord in resultOrderRecords)
                {
                    __musoqFinalShapeRows.Add(new ResultShape0(resultRecord.Id, resultRecord.C0, resultRecord.C1, resultRecord.C2, resultRecord.C3, resultRecord.C4, resultRecord.C5, resultRecord.C6, resultRecord.C7, resultRecord.C8, resultRecord.C9, resultRecord.C10, resultRecord.C11, resultRecord.C12, resultRecord.C13, resultRecord.C14, resultRecord.C15, resultRecord.C16, resultRecord.C17, resultRecord.C18, resultRecord.C19, resultRecord.C20, resultRecord.C21, resultRecord.C22, resultRecord.C23, resultRecord.C24, resultRecord.C25, resultRecord.C26, resultRecord.C27, resultRecord.C28, resultRecord.C29, resultRecord.C30, resultRecord.C31, resultRecord.C32, resultRecord.C33, resultRecord.C34, resultRecord.C35, resultRecord.C36, resultRecord.C37, resultRecord.C38, resultRecord.C39, resultRecord.C40, resultRecord.C41, resultRecord.C42, resultRecord.C43, resultRecord.C44, resultRecord.C45, resultRecord.C46, resultRecord.C47, resultRecord.C48, resultRecord.C49, resultRecord.C50, resultRecord.C51, resultRecord.C52, resultRecord.C53, resultRecord.C54, resultRecord.C55, resultRecord.C56, resultRecord.C57, resultRecord.C58, resultRecord.C59, resultRecord.C60, resultRecord.C61, resultRecord.C62, resultRecord.C63));
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

        private sealed class ResultRow0 : Row
        {
            private const string __columnIndexPairs = "Id\n0\nC0\n1\nC1\n2\nC2\n3\nC3\n4\nC4\n5\nC5\n6\nC6\n7\nC7\n8\nC8\n9\nC9\n10\nC10\n11\nC11\n12\nC12\n13\nC13\n14\nC14\n15\nC15\n16\nC16\n17\nC17\n18\nC18\n19\nC19\n20\nC20\n21\nC21\n22\nC22\n23\nC23\n24\nC24\n25\nC25\n26\nC26\n27\nC27\n28\nC28\n29\nC29\n30\nC30\n31\nC31\n32\nC32\n33\nC33\n34\nC34\n35\nC35\n36\nC36\n37\nC37\n38\nC38\n39\nC39\n40\nC40\n41\nC41\n42\nC42\n43\nC43\n44\nC44\n45\nC45\n46\nC46\n47\nC47\n48\nC48\n49\nC49\n50\nC50\n51\nC51\n52\nC52\n53\nC53\n54\nC54\n55\nC55\n56\nC56\n57\nC57\n58\nC58\n59\nC59\n60\nC60\n61\nC61\n62\nC62\n63\nC63\n64";
            private static readonly Dictionary<string, int> __columnIndexes = CreateColumnIndexes();
            public ResultRow0(int __value0, decimal __value1, decimal __value2, decimal __value3, decimal __value4, decimal __value5, decimal __value6, decimal __value7, decimal __value8, decimal __value9, decimal __value10, decimal __value11, decimal __value12, decimal __value13, decimal __value14, decimal __value15, decimal __value16, decimal __value17, decimal __value18, decimal __value19, decimal __value20, decimal __value21, decimal __value22, decimal __value23, decimal __value24, decimal __value25, decimal __value26, decimal __value27, decimal __value28, decimal __value29, decimal __value30, decimal __value31, decimal __value32, decimal __value33, decimal __value34, decimal __value35, decimal __value36, decimal __value37, decimal __value38, decimal __value39, decimal __value40, decimal __value41, decimal __value42, decimal __value43, decimal __value44, decimal __value45, decimal __value46, decimal __value47, decimal __value48, decimal __value49, decimal __value50, decimal __value51, decimal __value52, decimal __value53, decimal __value54, decimal __value55, decimal __value56, decimal __value57, decimal __value58, decimal __value59, decimal __value60, decimal __value61, decimal __value62, decimal __value63, decimal __value64)
            {
                Id = __value0;
                C0 = __value1;
                C1 = __value2;
                C2 = __value3;
                C3 = __value4;
                C4 = __value5;
                C5 = __value6;
                C6 = __value7;
                C7 = __value8;
                C8 = __value9;
                C9 = __value10;
                C10 = __value11;
                C11 = __value12;
                C12 = __value13;
                C13 = __value14;
                C14 = __value15;
                C15 = __value16;
                C16 = __value17;
                C17 = __value18;
                C18 = __value19;
                C19 = __value20;
                C20 = __value21;
                C21 = __value22;
                C22 = __value23;
                C23 = __value24;
                C24 = __value25;
                C25 = __value26;
                C26 = __value27;
                C27 = __value28;
                C28 = __value29;
                C29 = __value30;
                C30 = __value31;
                C31 = __value32;
                C32 = __value33;
                C33 = __value34;
                C34 = __value35;
                C35 = __value36;
                C36 = __value37;
                C37 = __value38;
                C38 = __value39;
                C39 = __value40;
                C40 = __value41;
                C41 = __value42;
                C42 = __value43;
                C43 = __value44;
                C44 = __value45;
                C45 = __value46;
                C46 = __value47;
                C47 = __value48;
                C48 = __value49;
                C49 = __value50;
                C50 = __value51;
                C51 = __value52;
                C52 = __value53;
                C53 = __value54;
                C54 = __value55;
                C55 = __value56;
                C56 = __value57;
                C57 = __value58;
                C58 = __value59;
                C59 = __value60;
                C60 = __value61;
                C61 = __value62;
                C62 = __value63;
                C63 = __value64;
            }

            public decimal C0 { get; private set; }
            public decimal C1 { get; private set; }
            public decimal C10 { get; private set; }
            public decimal C11 { get; private set; }
            public decimal C12 { get; private set; }
            public decimal C13 { get; private set; }
            public decimal C14 { get; private set; }
            public decimal C15 { get; private set; }
            public decimal C16 { get; private set; }
            public decimal C17 { get; private set; }
            public decimal C18 { get; private set; }
            public decimal C19 { get; private set; }
            public decimal C2 { get; private set; }
            public decimal C20 { get; private set; }
            public decimal C21 { get; private set; }
            public decimal C22 { get; private set; }
            public decimal C23 { get; private set; }
            public decimal C24 { get; private set; }
            public decimal C25 { get; private set; }
            public decimal C26 { get; private set; }
            public decimal C27 { get; private set; }
            public decimal C28 { get; private set; }
            public decimal C29 { get; private set; }
            public decimal C3 { get; private set; }
            public decimal C30 { get; private set; }
            public decimal C31 { get; private set; }
            public decimal C32 { get; private set; }
            public decimal C33 { get; private set; }
            public decimal C34 { get; private set; }
            public decimal C35 { get; private set; }
            public decimal C36 { get; private set; }
            public decimal C37 { get; private set; }
            public decimal C38 { get; private set; }
            public decimal C39 { get; private set; }
            public decimal C4 { get; private set; }
            public decimal C40 { get; private set; }
            public decimal C41 { get; private set; }
            public decimal C42 { get; private set; }
            public decimal C43 { get; private set; }
            public decimal C44 { get; private set; }
            public decimal C45 { get; private set; }
            public decimal C46 { get; private set; }
            public decimal C47 { get; private set; }
            public decimal C48 { get; private set; }
            public decimal C49 { get; private set; }
            public decimal C5 { get; private set; }
            public decimal C50 { get; private set; }
            public decimal C51 { get; private set; }
            public decimal C52 { get; private set; }
            public decimal C53 { get; private set; }
            public decimal C54 { get; private set; }
            public decimal C55 { get; private set; }
            public decimal C56 { get; private set; }
            public decimal C57 { get; private set; }
            public decimal C58 { get; private set; }
            public decimal C59 { get; private set; }
            public decimal C6 { get; private set; }
            public decimal C60 { get; private set; }
            public decimal C61 { get; private set; }
            public decimal C62 { get; private set; }
            public decimal C63 { get; private set; }
            public decimal C7 { get; private set; }
            public decimal C8 { get; private set; }
            public decimal C9 { get; private set; }
            public override int Count => 65;
            public int Id { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C0 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C2 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C3 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C4 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C5 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C6 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C7 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C8 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C9 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C10 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    12 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C11 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    13 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C12 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    14 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C13 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    15 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C14 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    16 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C15 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    17 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C16 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    18 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C17 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    19 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C18 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    20 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C19 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    21 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C20 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    22 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C21 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    23 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C22 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    24 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C23 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    25 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C24 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    26 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C25 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    27 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C26 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    28 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C27 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    29 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C28 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    30 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C29 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    31 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C30 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    32 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C31 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    33 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C32 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    34 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C33 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    35 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C34 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    36 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C35 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    37 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C36 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    38 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C37 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    39 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C38 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    40 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C39 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    41 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C40 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    42 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C41 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    43 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C42 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    44 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C43 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    45 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C44 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    46 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C45 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    47 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C46 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    48 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C47 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    49 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C48 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    50 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C49 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    51 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C50 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    52 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C51 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    53 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C52 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    54 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C53 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    55 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C54 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    56 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C55 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    57 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C56 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    58 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C57 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    59 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C58 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    60 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C59 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    61 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C60 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    62 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C61 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    63 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C62 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    64 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C63 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C0),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C1),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C2),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C3),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C4),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C5),
                7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C6),
                8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C7),
                9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C8),
                10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C9),
                11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C10),
                12 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C11),
                13 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C12),
                14 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C13),
                15 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C14),
                16 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C15),
                17 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C16),
                18 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C17),
                19 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C18),
                20 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C19),
                21 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C20),
                22 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C21),
                23 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C22),
                24 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C23),
                25 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C24),
                26 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C25),
                27 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C26),
                28 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C27),
                29 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C28),
                30 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C29),
                31 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C30),
                32 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C31),
                33 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C32),
                34 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C33),
                35 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C34),
                36 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C35),
                37 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C36),
                38 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C37),
                39 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C38),
                40 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C39),
                41 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C40),
                42 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C41),
                43 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C42),
                44 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C43),
                45 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C44),
                46 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C45),
                47 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C46),
                48 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C47),
                49 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C48),
                50 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C49),
                51 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C50),
                52 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C51),
                53 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C52),
                54 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C53),
                55 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C54),
                56 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C55),
                57 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C56),
                58 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C57),
                59 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C58),
                60 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C59),
                61 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C60),
                62 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C61),
                63 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C62),
                64 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.C63),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => __columnIndexes.ContainsKey(name);
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.C0, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.C1, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.C2, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.C3, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.C4, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.C5, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(7, this.C6, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(8, this.C7, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(9, this.C8, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(10, this.C9, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(11, this.C10, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(12, this.C11, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(13, this.C12, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(14, this.C13, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(15, this.C14, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(16, this.C15, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(17, this.C16, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(18, this.C17, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(19, this.C18, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(20, this.C19, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(21, this.C20, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(22, this.C21, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(23, this.C22, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(24, this.C23, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(25, this.C24, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(26, this.C25, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(27, this.C26, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(28, this.C27, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(29, this.C28, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(30, this.C29, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(31, this.C30, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(32, this.C31, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(33, this.C32, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(34, this.C33, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(35, this.C34, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(36, this.C35, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(37, this.C36, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(38, this.C37, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(39, this.C38, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(40, this.C39, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(41, this.C40, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(42, this.C41, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(43, this.C42, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(44, this.C43, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(45, this.C44, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(46, this.C45, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(47, this.C46, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(48, this.C47, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(49, this.C48, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(50, this.C49, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(51, this.C50, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(52, this.C51, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(53, this.C52, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(54, this.C53, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(55, this.C54, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(56, this.C55, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(57, this.C56, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(58, this.C57, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(59, this.C58, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(60, this.C59, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(61, this.C60, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(62, this.C61, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(63, this.C62, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(64, this.C63, ref visitor);
            }

            private static Dictionary<string, int> CreateColumnIndexes()
            {
                var pairs = __columnIndexPairs.Split('\n');
                var indexes = new Dictionary<string, int>(pairs.Length / 2, StringComparer.Ordinal);
                for (var index = 0; index < pairs.Length; index += 2)
                    indexes.Add(pairs[index], int.Parse(pairs[index + 1], System.Globalization.CultureInfo.InvariantCulture));
                return indexes;
            }
        }

        private sealed class ResultRow0WithSortKeys
        {
            public ResultRow0WithSortKeys(int Id, decimal C0, decimal C1, decimal C2, decimal C3, decimal C4, decimal C5, decimal C6, decimal C7, decimal C8, decimal C9, decimal C10, decimal C11, decimal C12, decimal C13, decimal C14, decimal C15, decimal C16, decimal C17, decimal C18, decimal C19, decimal C20, decimal C21, decimal C22, decimal C23, decimal C24, decimal C25, decimal C26, decimal C27, decimal C28, decimal C29, decimal C30, decimal C31, decimal C32, decimal C33, decimal C34, decimal C35, decimal C36, decimal C37, decimal C38, decimal C39, decimal C40, decimal C41, decimal C42, decimal C43, decimal C44, decimal C45, decimal C46, decimal C47, decimal C48, decimal C49, decimal C50, decimal C51, decimal C52, decimal C53, decimal C54, decimal C55, decimal C56, decimal C57, decimal C58, decimal C59, decimal C60, decimal C61, decimal C62, decimal C63, int __ordinal)
            {
                this.Id = Id;
                this.C0 = C0;
                this.C1 = C1;
                this.C2 = C2;
                this.C3 = C3;
                this.C4 = C4;
                this.C5 = C5;
                this.C6 = C6;
                this.C7 = C7;
                this.C8 = C8;
                this.C9 = C9;
                this.C10 = C10;
                this.C11 = C11;
                this.C12 = C12;
                this.C13 = C13;
                this.C14 = C14;
                this.C15 = C15;
                this.C16 = C16;
                this.C17 = C17;
                this.C18 = C18;
                this.C19 = C19;
                this.C20 = C20;
                this.C21 = C21;
                this.C22 = C22;
                this.C23 = C23;
                this.C24 = C24;
                this.C25 = C25;
                this.C26 = C26;
                this.C27 = C27;
                this.C28 = C28;
                this.C29 = C29;
                this.C30 = C30;
                this.C31 = C31;
                this.C32 = C32;
                this.C33 = C33;
                this.C34 = C34;
                this.C35 = C35;
                this.C36 = C36;
                this.C37 = C37;
                this.C38 = C38;
                this.C39 = C39;
                this.C40 = C40;
                this.C41 = C41;
                this.C42 = C42;
                this.C43 = C43;
                this.C44 = C44;
                this.C45 = C45;
                this.C46 = C46;
                this.C47 = C47;
                this.C48 = C48;
                this.C49 = C49;
                this.C50 = C50;
                this.C51 = C51;
                this.C52 = C52;
                this.C53 = C53;
                this.C54 = C54;
                this.C55 = C55;
                this.C56 = C56;
                this.C57 = C57;
                this.C58 = C58;
                this.C59 = C59;
                this.C60 = C60;
                this.C61 = C61;
                this.C62 = C62;
                this.C63 = C63;
                this.__ordinal = __ordinal;
            }

            public decimal C0 { get; }
            public decimal C1 { get; }
            public decimal C10 { get; }
            public decimal C11 { get; }
            public decimal C12 { get; }
            public decimal C13 { get; }
            public decimal C14 { get; }
            public decimal C15 { get; }
            public decimal C16 { get; }
            public decimal C17 { get; }
            public decimal C18 { get; }
            public decimal C19 { get; }
            public decimal C2 { get; }
            public decimal C20 { get; }
            public decimal C21 { get; }
            public decimal C22 { get; }
            public decimal C23 { get; }
            public decimal C24 { get; }
            public decimal C25 { get; }
            public decimal C26 { get; }
            public decimal C27 { get; }
            public decimal C28 { get; }
            public decimal C29 { get; }
            public decimal C3 { get; }
            public decimal C30 { get; }
            public decimal C31 { get; }
            public decimal C32 { get; }
            public decimal C33 { get; }
            public decimal C34 { get; }
            public decimal C35 { get; }
            public decimal C36 { get; }
            public decimal C37 { get; }
            public decimal C38 { get; }
            public decimal C39 { get; }
            public decimal C4 { get; }
            public decimal C40 { get; }
            public decimal C41 { get; }
            public decimal C42 { get; }
            public decimal C43 { get; }
            public decimal C44 { get; }
            public decimal C45 { get; }
            public decimal C46 { get; }
            public decimal C47 { get; }
            public decimal C48 { get; }
            public decimal C49 { get; }
            public decimal C5 { get; }
            public decimal C50 { get; }
            public decimal C51 { get; }
            public decimal C52 { get; }
            public decimal C53 { get; }
            public decimal C54 { get; }
            public decimal C55 { get; }
            public decimal C56 { get; }
            public decimal C57 { get; }
            public decimal C58 { get; }
            public decimal C59 { get; }
            public decimal C6 { get; }
            public decimal C60 { get; }
            public decimal C61 { get; }
            public decimal C62 { get; }
            public decimal C63 { get; }
            public decimal C7 { get; }
            public decimal C8 { get; }
            public decimal C9 { get; }
            public int Id { get; }
            public int __ordinal { get; }
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

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, decimal C0, decimal C1, decimal C2, decimal C3, decimal C4, decimal C5, decimal C6, decimal C7, decimal C8, decimal C9, decimal C10, decimal C11, decimal C12, decimal C13, decimal C14, decimal C15, decimal C16, decimal C17, decimal C18, decimal C19, decimal C20, decimal C21, decimal C22, decimal C23, decimal C24, decimal C25, decimal C26, decimal C27, decimal C28, decimal C29, decimal C30, decimal C31, decimal C32, decimal C33, decimal C34, decimal C35, decimal C36, decimal C37, decimal C38, decimal C39, decimal C40, decimal C41, decimal C42, decimal C43, decimal C44, decimal C45, decimal C46, decimal C47, decimal C48, decimal C49, decimal C50, decimal C51, decimal C52, decimal C53, decimal C54, decimal C55, decimal C56, decimal C57, decimal C58, decimal C59, decimal C60, decimal C61, decimal C62, decimal C63)
            {
                this.Id = Id;
                this.C0 = C0;
                this.C1 = C1;
                this.C2 = C2;
                this.C3 = C3;
                this.C4 = C4;
                this.C5 = C5;
                this.C6 = C6;
                this.C7 = C7;
                this.C8 = C8;
                this.C9 = C9;
                this.C10 = C10;
                this.C11 = C11;
                this.C12 = C12;
                this.C13 = C13;
                this.C14 = C14;
                this.C15 = C15;
                this.C16 = C16;
                this.C17 = C17;
                this.C18 = C18;
                this.C19 = C19;
                this.C20 = C20;
                this.C21 = C21;
                this.C22 = C22;
                this.C23 = C23;
                this.C24 = C24;
                this.C25 = C25;
                this.C26 = C26;
                this.C27 = C27;
                this.C28 = C28;
                this.C29 = C29;
                this.C30 = C30;
                this.C31 = C31;
                this.C32 = C32;
                this.C33 = C33;
                this.C34 = C34;
                this.C35 = C35;
                this.C36 = C36;
                this.C37 = C37;
                this.C38 = C38;
                this.C39 = C39;
                this.C40 = C40;
                this.C41 = C41;
                this.C42 = C42;
                this.C43 = C43;
                this.C44 = C44;
                this.C45 = C45;
                this.C46 = C46;
                this.C47 = C47;
                this.C48 = C48;
                this.C49 = C49;
                this.C50 = C50;
                this.C51 = C51;
                this.C52 = C52;
                this.C53 = C53;
                this.C54 = C54;
                this.C55 = C55;
                this.C56 = C56;
                this.C57 = C57;
                this.C58 = C58;
                this.C59 = C59;
                this.C60 = C60;
                this.C61 = C61;
                this.C62 = C62;
                this.C63 = C63;
            }

            public decimal C0 { get; }
            public decimal C1 { get; }
            public decimal C10 { get; }
            public decimal C11 { get; }
            public decimal C12 { get; }
            public decimal C13 { get; }
            public decimal C14 { get; }
            public decimal C15 { get; }
            public decimal C16 { get; }
            public decimal C17 { get; }
            public decimal C18 { get; }
            public decimal C19 { get; }
            public decimal C2 { get; }
            public decimal C20 { get; }
            public decimal C21 { get; }
            public decimal C22 { get; }
            public decimal C23 { get; }
            public decimal C24 { get; }
            public decimal C25 { get; }
            public decimal C26 { get; }
            public decimal C27 { get; }
            public decimal C28 { get; }
            public decimal C29 { get; }
            public decimal C3 { get; }
            public decimal C30 { get; }
            public decimal C31 { get; }
            public decimal C32 { get; }
            public decimal C33 { get; }
            public decimal C34 { get; }
            public decimal C35 { get; }
            public decimal C36 { get; }
            public decimal C37 { get; }
            public decimal C38 { get; }
            public decimal C39 { get; }
            public decimal C4 { get; }
            public decimal C40 { get; }
            public decimal C41 { get; }
            public decimal C42 { get; }
            public decimal C43 { get; }
            public decimal C44 { get; }
            public decimal C45 { get; }
            public decimal C46 { get; }
            public decimal C47 { get; }
            public decimal C48 { get; }
            public decimal C49 { get; }
            public decimal C5 { get; }
            public decimal C50 { get; }
            public decimal C51 { get; }
            public decimal C52 { get; }
            public decimal C53 { get; }
            public decimal C54 { get; }
            public decimal C55 { get; }
            public decimal C56 { get; }
            public decimal C57 { get; }
            public decimal C58 { get; }
            public decimal C59 { get; }
            public decimal C6 { get; }
            public decimal C60 { get; }
            public decimal C61 { get; }
            public decimal C62 { get; }
            public decimal C63 { get; }
            public decimal C7 { get; }
            public decimal C8 { get; }
            public decimal C9 { get; }
            public int Id { get; }
        }
    }
}
