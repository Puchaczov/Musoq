// === Parsed Query ===
/*
select RowNumber() as RowNo, Self.Array[0] as FirstItem, Self.Array[-1] as LastItem, Self.Array[10] as MissingItem, Name[0] as FirstCharacter, Self.Dictionary['A'] as DictionaryValue, Self.Self.Name as NestedName, ToString(Self) as EntityText from #A.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [RowNumber() as RowNo, ko3iko.Array[0] as FirstItem, ko3iko.Array[-1] as LastItem, ko3iko.Array[10] as MissingItem, ko3iko.Name[0] as FirstCharacter, ko3iko.Self.Dictionary['A'] as DictionaryValue, ko3iko.Self.Self.Name as NestedName, ToString(ko3iko.Self) as EntityText]
    SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [RowNumber() as RowNo, ko3iko.Array[0] as FirstItem, ko3iko.Array[-1] as LastItem, ko3iko.Array[10] as MissingItem, ko3iko.Name[0] as FirstCharacter, ko3iko.Self.Dictionary['A'] as DictionaryValue, ko3iko.Self.Self.Name as NestedName, ToString(ko3iko.Self) as EntityText]
    PhysicalSchemaScan [#A.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
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
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateObject [__resultLibraryBase0: LibraryBase]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [array: int[] = ko3iko.Array]
      Let [self: BasicEntity = ko3iko.Self]
      AppendShape [result <- ResultRow0(RowNo: RowNumber(), FirstItem: GetTypedReadOnlyListElement(array, 0), LastItem: GetTypedReadOnlyListElement(array, -1), MissingItem: GetTypedReadOnlyListElement(array, 10), FirstCharacter: GetTypedStringCharacter(ko3iko.Name, 0), DictionaryValue: GetTypedDictionaryValue(self?.Dictionary, 'A'), NestedName: self?.Self?.Name, EntityText: ToString(self))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Self", typeof(Musoq.Evaluator.Tests.Schema.Basic.BasicEntity), 1), new Column("Array", typeof(int[]), 2) });
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
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var stats = new Musoq.Evaluator.AmendableQueryStats();
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
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
                                int[] array = ko3iko.Array;
                                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity self = ko3iko.Self;
                                yield return new ResultRow0((int)__resultLibraryBase0.RowNumber(stats.IncrementRowNumber()), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(array, 0), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(array, -1), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(array, 10), (char)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedStringCharacter(ko3iko.Name, 0), (string)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, string>, string, string>(self?.Dictionary, "A"), (self?.Self)?.Name, (string)__resultLibraryBase0.ToString<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(self));
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
                                int[] array = ko3iko.Array;
                                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity self = ko3iko.Self;
                                yield return new ResultRow0((int)__resultLibraryBase0.RowNumber(stats.IncrementRowNumber()), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(array, 0), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(array, -1), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(array, 10), (char)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedStringCharacter(ko3iko.Name, 0), (string)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, string>, string, string>(self?.Dictionary, "A"), (self?.Self)?.Name, (string)__resultLibraryBase0.ToString<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(self));
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
                        int[] array = ko3iko.Array;
                        Musoq.Evaluator.Tests.Schema.Basic.BasicEntity self = ko3iko.Self;
                        yield return new ResultRow0((int)__resultLibraryBase0.RowNumber(stats.IncrementRowNumber()), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(array, 0), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(array, -1), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(array, 10), (char)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedStringCharacter(ko3iko.Name, 0), (string)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, string>, string, string>(self?.Dictionary, "A"), (self?.Self)?.Name, (string)__resultLibraryBase0.ToString<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(self));
                    }
                }
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.RowNo = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.FirstItem = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LastItem = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.MissingItem = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, char>(this.FirstCharacter = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<char, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.DictionaryValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.NestedName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.EntityText = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.RowNo),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.FirstItem),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LastItem),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.MissingItem),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, char>(this.FirstCharacter),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.DictionaryValue),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.NestedName),
                7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.EntityText),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
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
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.RowNo, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.FirstItem, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.LastItem, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.MissingItem, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.FirstCharacter, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.DictionaryValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.NestedName, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(7, this.EntityText, ref visitor);
            }
        }
    }
}
