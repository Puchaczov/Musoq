// === Parsed Query ===
/*
select RowNumber() as RowNo, Self.Array[0] as FirstItem, Self.Array[-1] as LastItem, Self.Array[10] as MissingItem, Name[0] as FirstCharacter, Self.Dictionary['A'] as DictionaryValue, Self.Self.Name as NestedName, ToString(Self) as EntityText from #A.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [RowNumber() as RowNo, ko3iko.Array[0] as FirstItem, ko3iko.Array[-1] as LastItem, ko3iko.Array[10] as MissingItem, Name[0] as FirstCharacter, ko3iko.Self.Dictionary['A'] as DictionaryValue, ko3iko.Self.Self.Name as NestedName, ToString(ko3iko.Self) as EntityText]
    SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [RowNumber() as RowNo, ko3iko.Array[0] as FirstItem, ko3iko.Array[-1] as LastItem, ko3iko.Array[10] as MissingItem, Name[0] as FirstCharacter, ko3iko.Self.Dictionary['A'] as DictionaryValue, ko3iko.Self.Self.Name as NestedName, ToString(ko3iko.Self) as EntityText]
    PhysicalSchemaScan [#A.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Self: BasicEntity <- property Self
      Array: int[] <- property Array
    Generated [ResultRow0]
      RowNo: int <- field RowNo
      FirstItem: int <- field FirstItem
      LastItem: int <- field LastItem
      MissingItem: int <- field MissingItem
      FirstCharacter: char <- field FirstCharacter
      DictionaryValue: string <- field DictionaryValue
      NestedName: string <- field NestedName
      EntityText: string <- field EntityText

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultLibraryBase0: LibraryBase]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultShape0(RowNo: RowNumber(), FirstItem: ko3iko.Array[0], LastItem: ko3iko.Array[-1], MissingItem: ko3iko.Array[10], FirstCharacter: Name[0], DictionaryValue: ko3iko.Self.Dictionary['A'], NestedName: ko3iko.Self.Self.Name, EntityText: ToString(ko3iko.Self))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q276_SpecCoreRowAndMemberAccess
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("RowNo", typeof(int), 0),
            new Column("FirstItem", typeof(int), 1),
            new Column("LastItem", typeof(int), 2),
            new Column("MissingItem", typeof(int), 3),
            new Column("FirstCharacter", typeof(char), 4),
            new Column("DictionaryValue", typeof(string), 5),
            new Column("NestedName", typeof(string), 6),
            new Column("EntityText", typeof(string), 7)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Self", typeof(Musoq.Evaluator.Tests.Schema.Basic.BasicEntity), 14), new Column("Array", typeof(int[]), 20) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            var stats = new Musoq.Evaluator.AmendableQueryStats();
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
            this.OnPhaseChanged("compiled", QueryPhase.Begin);
            this.OnPhaseChanged("compiled", QueryPhase.From);
            var __ko3ikoSchema = provider.GetSchema("#A");
            var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
            var __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0((int)__resultLibraryBase0.RowNumber(stats.IncrementRowNumber()), (int)SafeArrayAccess.GetIndexedElement(ko3iko.Array, 0, typeof(int)), (int)SafeArrayAccess.GetIndexedElement(ko3iko.Array, -1, typeof(int)), (int)SafeArrayAccess.GetIndexedElement(ko3iko.Array, 10, typeof(int)), (char)SafeArrayAccess.GetIndexedElement(Name, 0, typeof(char)), (string)SafeArrayAccess.GetIndexedElement(ko3iko.Self.Dictionary, "A", typeof(string)), ko3iko.Self.Self.Name, (string)__resultLibraryBase0.ToString(ko3iko.Self)), token), token, onCompleted: () =>
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
            public ResultRow0(int __value0, int __value1, int __value2, int __value3, char __value4, string __value5, string __value6, string __value7)
            {
                RowNo = __value0;
                FirstItem = __value1;
                LastItem = __value2;
                MissingItem = __value3;
                FirstCharacter = __value4;
                DictionaryValue = __value5;
                NestedName = __value6;
                EntityText = __value7;
            }

            public override int Count => 8;
            public string DictionaryValue { get; private set; }
            public string EntityText { get; private set; }
            public char FirstCharacter { get; private set; }
            public int FirstItem { get; private set; }
            public int LastItem { get; private set; }
            public int MissingItem { get; private set; }
            public string NestedName { get; private set; }
            public int RowNo { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        RowNo = (int)value;
                        break;
                    case 1:
                        FirstItem = (int)value;
                        break;
                    case 2:
                        LastItem = (int)value;
                        break;
                    case 3:
                        MissingItem = (int)value;
                        break;
                    case 4:
                        FirstCharacter = (char)value;
                        break;
                    case 5:
                        DictionaryValue = (string)value;
                        break;
                    case 6:
                        NestedName = (string)value;
                        break;
                    case 7:
                        EntityText = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "RowNo" => true,
                "FirstItem" => true,
                "LastItem" => true,
                "MissingItem" => true,
                "FirstCharacter" => true,
                "DictionaryValue" => true,
                "NestedName" => true,
                "EntityText" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)RowNo,
                1 => (object)FirstItem,
                2 => (object)LastItem,
                3 => (object)MissingItem,
                4 => (object)FirstCharacter,
                5 => (object)DictionaryValue,
                6 => (object)NestedName,
                7 => (object)EntityText,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "RowNo" => (object)RowNo,
                "FirstItem" => (object)FirstItem,
                "LastItem" => (object)LastItem,
                "MissingItem" => (object)MissingItem,
                "FirstCharacter" => (object)FirstCharacter,
                "DictionaryValue" => (object)DictionaryValue,
                "NestedName" => (object)NestedName,
                "EntityText" => (object)EntityText,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int RowNo, int FirstItem, int LastItem, int MissingItem, char FirstCharacter, string DictionaryValue, string NestedName, string EntityText)
            {
                this.RowNo = RowNo;
                this.FirstItem = FirstItem;
                this.LastItem = LastItem;
                this.MissingItem = MissingItem;
                this.FirstCharacter = FirstCharacter;
                this.DictionaryValue = DictionaryValue;
                this.NestedName = NestedName;
                this.EntityText = EntityText;
            }

            public string DictionaryValue { get; }
            public string EntityText { get; }
            public char FirstCharacter { get; }
            public int FirstItem { get; }
            public int LastItem { get; }
            public int MissingItem { get; }
            public string NestedName { get; }
            public int RowNo { get; }
        }
    }
}
