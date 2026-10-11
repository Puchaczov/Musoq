// === Parsed Query ===
/*
table LegacyInvoiceRow {
                        InvoiceNo: string encoding 'windows-1250' trim,
                        CustomerName: string encoding 'windows-1250' trim,
                        Total: decimal culture 'pl-PL' format '#,##0.00',
                        Attachment: string source codec 'base64',
                    };
                    couple #readmods.records with table LegacyInvoiceRow as Invoices;
                    select InvoiceNo, CustomerName, Total, Attachment
                    from Invoices()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.InvoiceNo as InvoiceNo, ko3iko.CustomerName as CustomerName, ko3iko.Total as Total, ko3iko.Attachment as Attachment]
    SchemaScan [#readmods.records() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.InvoiceNo as InvoiceNo, ko3iko.CustomerName as CustomerName, ko3iko.Total as Total, ko3iko.Attachment as Attachment]
    PhysicalSchemaScan [#readmods.records() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: SpecificationReadModifiersEntity]
      InvoiceNo: string <- property InvoiceNo
      CustomerName: string <- property CustomerName
      Total: decimal <- property Total
      Attachment: string <- property Attachment
    Generated [ResultRow0]
      InvoiceNo: string <- field InvoiceNo
      CustomerName: string <- field CustomerName
      Total: decimal <- field Total
      Attachment: string <- field Attachment

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: SpecificationReadModifiersEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultRow0(InvoiceNo: ko3iko.InvoiceNo, CustomerName: ko3iko.CustomerName, Total: ko3iko.Total, Attachment: ko3iko.Attachment)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q320_SpecTableReadModifiers
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
            new Column("InvoiceNo", typeof(string), 0),
            new Column("CustomerName", typeof(string), 1),
            new Column("Total", typeof(decimal), 2),
            new Column("Attachment", typeof(string), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new global::Musoq.Schema.DataSources.SchemaColumn("InvoiceNo", 0, typeof(string), new Dictionary<string, string>() { { "encoding", "windows-1250" }, { "trim", "" } }), new global::Musoq.Schema.DataSources.SchemaColumn("CustomerName", 1, typeof(string), new Dictionary<string, string>() { { "encoding", "windows-1250" }, { "trim", "" } }), new global::Musoq.Schema.DataSources.SchemaColumn("Total", 2, typeof(decimal), new Dictionary<string, string>() { { "culture", "pl-PL" }, { "format", "#,##0.00" } }), new global::Musoq.Schema.DataSources.SchemaColumn("Attachment", 3, typeof(string), new Dictionary<string, string>() { { "source.codec", "base64" } }) });
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
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
            this.OnPhaseChanged("compiled", QueryPhase.Begin);
            this.OnPhaseChanged("compiled", QueryPhase.From);
            var __ko3ikoSchema = provider.GetSchema("#readmods");
            var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.SpecificationReadModifiersEntity>.Open(__ko3ikoSchema, "records", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.SpecificationReadModifiersEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.SpecificationReadModifiersEntity, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0(ko3iko.InvoiceNo, ko3iko.CustomerName, ko3iko.Total, ko3iko.Attachment), token), token, onCompleted: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onException: (Exception _) =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onDisposed: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            });
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
            public ResultRow0(string __value0, string __value1, decimal __value2, string __value3)
            {
                InvoiceNo = __value0;
                CustomerName = __value1;
                Total = __value2;
                Attachment = __value3;
            }

            public string Attachment { get; private set; }
            public override int Count => 4;
            public string CustomerName { get; private set; }
            public string InvoiceNo { get; private set; }
            public decimal Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.InvoiceNo = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.CustomerName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Attachment = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.InvoiceNo),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.CustomerName),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Total),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Attachment),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "InvoiceNo" => true,
                "CustomerName" => true,
                "Total" => true,
                "Attachment" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.InvoiceNo, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.CustomerName, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Attachment, ref visitor);
            }
        }
    }
}
