// === Parsed Query ===
/*
select R'C:\new\test' as RawPath, '\'' as Escaped,
                     -Population as UnaryValue,
                     Population + 2 * 3 % 2 as ArithmeticValue,
                     (Id & 3) | (Id << 2) as BitwiseValue,
                     Id >> 1 as ShiftedValue,
                     0x10 as HexValue,
                     0b1010 as BinaryValue,
                     0o17 as OctalValue,
                     18.5d as DecimalValue,
                     Population between 1 and 2000000 as InRange,
                     case when Population > 0 then 'positive' else 'zero' end as CaseValue,
                     null + Population as NullPropagation,
                     null ?? Population as NullFallback
              from #A.entities()
              where Match('\\d+', Name)
*/

// === Logical Plan ===
/*
MultiStatement
  Project ['C:\new\test' as RawPath, ''' as Escaped, (-1 * ko3iko.Population) as UnaryValue, (ko3iko.Population + 0) as ArithmeticValue, ((ko3iko.Id & 3) | (ko3iko.Id << 2)) as BitwiseValue, (ko3iko.Id >> 1) as ShiftedValue, 16 as HexValue, 10 as BinaryValue, 15 as OctalValue, 18,5 as DecimalValue, ((ko3iko.Population >= 1) AND (ko3iko.Population <= 2000000)) as InRange, CASE WHEN (ko3iko.Population > 0) THEN 'positive' ELSE 'zero' END as CaseValue, (NULL + ko3iko.Population) as NullPropagation, ko3iko.Population as NullFallback]
    Filter [(Match('\d+', ko3iko.Name) = TRUE)]
      SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject ['C:\new\test' as RawPath, ''' as Escaped, (-1 * ko3iko.Population) as UnaryValue, (ko3iko.Population + 0) as ArithmeticValue, ((ko3iko.Id & 3) | (ko3iko.Id << 2)) as BitwiseValue, (ko3iko.Id >> 1) as ShiftedValue, 16 as HexValue, 10 as BinaryValue, 15 as OctalValue, 18,5 as DecimalValue, ((ko3iko.Population >= 1) AND (ko3iko.Population <= 2000000)) as InRange, CASE WHEN (ko3iko.Population > 0) THEN 'positive' ELSE 'zero' END as CaseValue, (NULL + ko3iko.Population) as NullPropagation, ko3iko.Population as NullFallback]
    PhysicalFilter [(Match('\d+', ko3iko.Name) = TRUE)]
      PhysicalSchemaScan [#A.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
      Id: int <- property Id
    Generated [ResultRow0]
      RawPath: string <- field RawPath
      Escaped: string <- field Escaped
      UnaryValue: decimal <- field UnaryValue
      ArithmeticValue: decimal <- field ArithmeticValue
      BitwiseValue: int <- field BitwiseValue
      ShiftedValue: int <- field ShiftedValue
      HexValue: long <- field HexValue
      BinaryValue: long <- field BinaryValue
      OctalValue: long <- field OctalValue
      DecimalValue: decimal <- field DecimalValue
      InRange: bool <- field InRange
      CaseValue: string <- field CaseValue
      NullPropagation: decimal? <- field NullPropagation
      NullFallback: decimal <- field NullFallback

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateObject [__resultLibraryBase0: LibraryBase]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      If [(Match('\d+', ko3iko.Name) = TRUE)]
        Let [population: decimal = ko3iko.Population]
        Let [id: int = ko3iko.Id]
        AppendShape [result <- ResultRow0(RawPath: 'C:\new\test', Escaped: ''', UnaryValue: (-1 * population), ArithmeticValue: (population + 0), BitwiseValue: ((id & 3) | (id << 2)), ShiftedValue: (id >> 1), HexValue: 16, BinaryValue: 10, OctalValue: 15, DecimalValue: 18,5, InRange: ((population >= 1) AND (population <= 2000000)), CaseValue: CASE WHEN (population > 0) THEN 'positive' ELSE 'zero' END, NullPropagation: (NULL + population), NullFallback: population)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q268_SpecCoreOperatorsAndRawLiterals
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
            new Column("RawPath", typeof(string), 0),
            new Column("Escaped", typeof(string), 1),
            new Column("UnaryValue", typeof(decimal), 2),
            new Column("ArithmeticValue", typeof(decimal), 3),
            new Column("BitwiseValue", typeof(int), 4),
            new Column("ShiftedValue", typeof(int), 5),
            new Column("HexValue", typeof(long), 6),
            new Column("BinaryValue", typeof(long), 7),
            new Column("OctalValue", typeof(long), 8),
            new Column("DecimalValue", typeof(decimal), 9),
            new Column("InRange", typeof(bool), 10),
            new Column("CaseValue", typeof(string), 11),
            new Column("NullPropagation", typeof(decimal?), 12),
            new Column("NullFallback", typeof(decimal), 13)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Population", typeof(decimal), 1), new Column("Id", typeof(int), 2) });
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
                OnPhaseChanged("compiled", QueryPhase.Where);
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
                                if ((Operators.SqlCompare<bool?, bool>((bool?)__resultLibraryBase0.Match("\\d+", ko3iko.Name), true, (bool? __sqlLeft, bool __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                {
                                    decimal population = ko3iko.Population;
                                    int id = ko3iko.Id;
                                    yield return new ResultRow0("C:\\new\\test", "'", (-1 * population), (population + 0), ((id & 3) | (id << 2)), (id >> 1), 16L, 10L, 15L, 18.5m, ((population >= 1) && (population <= 2000000)), (population > 0) ? (string)"positive" : (string)"zero", (null + population), population);
                                }
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
                                if ((Operators.SqlCompare<bool?, bool>((bool?)__resultLibraryBase0.Match("\\d+", ko3iko.Name), true, (bool? __sqlLeft, bool __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                {
                                    decimal population = ko3iko.Population;
                                    int id = ko3iko.Id;
                                    yield return new ResultRow0("C:\\new\\test", "'", (-1 * population), (population + 0), ((id & 3) | (id << 2)), (id >> 1), 16L, 10L, 15L, 18.5m, ((population >= 1) && (population <= 2000000)), (population > 0) ? (string)"positive" : (string)"zero", (null + population), population);
                                }
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
                        if ((Operators.SqlCompare<bool?, bool>((bool?)__resultLibraryBase0.Match("\\d+", ko3iko.Name), true, (bool? __sqlLeft, bool __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                        {
                            decimal population = ko3iko.Population;
                            int id = ko3iko.Id;
                            yield return new ResultRow0("C:\\new\\test", "'", (-1 * population), (population + 0), ((id & 3) | (id << 2)), (id >> 1), 16L, 10L, 15L, 18.5m, ((population >= 1) && (population <= 2000000)), (population > 0) ? (string)"positive" : (string)"zero", (null + population), population);
                        }
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
            public ResultRow0(string __value0, string __value1, decimal __value2, decimal __value3, int __value4, int __value5, long __value6, long __value7, long __value8, decimal __value9, bool __value10, string __value11, decimal? __value12, decimal __value13)
            {
                RawPath = __value0;
                Escaped = __value1;
                UnaryValue = __value2;
                ArithmeticValue = __value3;
                BitwiseValue = __value4;
                ShiftedValue = __value5;
                HexValue = __value6;
                BinaryValue = __value7;
                OctalValue = __value8;
                DecimalValue = __value9;
                InRange = __value10;
                CaseValue = __value11;
                NullPropagation = __value12;
                NullFallback = __value13;
            }

            public decimal ArithmeticValue { get; private set; }
            public long BinaryValue { get; private set; }
            public int BitwiseValue { get; private set; }
            public string CaseValue { get; private set; }
            public override int Count => 14;
            public decimal DecimalValue { get; private set; }
            public string Escaped { get; private set; }
            public long HexValue { get; private set; }
            public bool InRange { get; private set; }
            public decimal NullFallback { get; private set; }
            public decimal? NullPropagation { get; private set; }
            public long OctalValue { get; private set; }
            public string RawPath { get; private set; }
            public int ShiftedValue { get; private set; }
            public decimal UnaryValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.RawPath = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Escaped = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.UnaryValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.ArithmeticValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BitwiseValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.ShiftedValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.HexValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.BinaryValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.OctalValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.DecimalValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.InRange = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.CaseValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    12 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.NullPropagation = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    13 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.NullFallback = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.RawPath),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Escaped),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.UnaryValue),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.ArithmeticValue),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BitwiseValue),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.ShiftedValue),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.HexValue),
                7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.BinaryValue),
                8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.OctalValue),
                9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.DecimalValue),
                10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.InRange),
                11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.CaseValue),
                12 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.NullPropagation),
                13 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.NullFallback),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "RawPath" => true,
                "Escaped" => true,
                "UnaryValue" => true,
                "ArithmeticValue" => true,
                "BitwiseValue" => true,
                "ShiftedValue" => true,
                "HexValue" => true,
                "BinaryValue" => true,
                "OctalValue" => true,
                "DecimalValue" => true,
                "InRange" => true,
                "CaseValue" => true,
                "NullPropagation" => true,
                "NullFallback" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.RawPath, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Escaped, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.UnaryValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.ArithmeticValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.BitwiseValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.ShiftedValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.HexValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(7, this.BinaryValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(8, this.OctalValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(9, this.DecimalValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(10, this.InRange, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(11, this.CaseValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(12, this.NullPropagation, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(13, this.NullFallback, ref visitor);
            }
        }
    }
}
