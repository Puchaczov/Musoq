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
      PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: Match('\d+', ko3iko.Name)]
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
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultLibraryBase0: LibraryBase]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ParallelFilterProjectLoop [ko3iko in ko3ikoRows where (Match('\d+', ko3iko.Name) = TRUE); threshold 4096, maxDegree 24]
      ParallelProject
        If [(Match('\d+', ko3iko.Name) = TRUE)]
          Let [population: decimal = ko3iko.Population]
          Let [id: int = ko3iko.Id]
          AppendShape [result <- ResultShape0(RawPath: 'C:\new\test', Escaped: ''', UnaryValue: (-1 * population), ArithmeticValue: (population + 0), BitwiseValue: ((id & 3) | (id << 2)), ShiftedValue: (id >> 1), HexValue: 16, BinaryValue: 10, OctalValue: 15, DecimalValue: 18,5, InRange: ((population >= 1) AND (population <= 2000000)), CaseValue: CASE WHEN (population > 0) THEN 'positive' ELSE 'zero' END, NullPropagation: (NULL + population), NullFallback: population)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Population", typeof(decimal), 13), new Column("Id", typeof(int), 18) });
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
            this.OnPhaseChanged("compiled", QueryPhase.Where);
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            if (__musoqTableSourceRows is not IReadOnlyList<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>> _)
            {
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) => (Operators.SqlCompare<bool?, bool>((bool?)__resultLibraryBase0.Match("\\d+", ko3iko.Name), true, (bool? __sqlLeft, bool __sqlRight) => (__sqlLeft == __sqlRight))) == true, (ko3iko) => new ResultRow0("C:\\new\\test", "'", (-1 * ko3iko.Population), (ko3iko.Population + 0), ((ko3iko.Id & 3) | (ko3iko.Id << 2)), (ko3iko.Id >> 1), 16L, 10L, 15L, 18.5m, ((ko3iko.Population >= 1) && (ko3iko.Population <= 2000000)), (ko3iko.Population > 0) ? (string)"positive" : (string)"zero", (null + ko3iko.Population), ko3iko.Population), token), token, onCompleted: () =>
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

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) => (Operators.SqlCompare<bool?, bool>((bool?)__resultLibraryBase0.Match("\\d+", ko3iko.Name), true, (bool? __sqlLeft, bool __sqlRight) => (__sqlLeft == __sqlRight))) == true, (ko3iko) => new ResultRow0("C:\\new\\test", "'", (-1 * ko3iko.Population), (ko3iko.Population + 0), ((ko3iko.Id & 3) | (ko3iko.Id << 2)), (ko3iko.Id >> 1), 16L, 10L, 15L, 18.5m, ((ko3iko.Population >= 1) && (ko3iko.Population <= 2000000)), (ko3iko.Population > 0) ? (string)"positive" : (string)"zero", (null + ko3iko.Population), ko3iko.Population), token)), token, onCompleted: () =>
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        RawPath = (string)value;
                        break;
                    case 1:
                        Escaped = (string)value;
                        break;
                    case 2:
                        UnaryValue = (decimal)value;
                        break;
                    case 3:
                        ArithmeticValue = (decimal)value;
                        break;
                    case 4:
                        BitwiseValue = (int)value;
                        break;
                    case 5:
                        ShiftedValue = (int)value;
                        break;
                    case 6:
                        HexValue = (long)value;
                        break;
                    case 7:
                        BinaryValue = (long)value;
                        break;
                    case 8:
                        OctalValue = (long)value;
                        break;
                    case 9:
                        DecimalValue = (decimal)value;
                        break;
                    case 10:
                        InRange = (bool)value;
                        break;
                    case 11:
                        CaseValue = (string)value;
                        break;
                    case 12:
                        NullPropagation = (decimal?)value;
                        break;
                    case 13:
                        NullFallback = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

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
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)RawPath,
                1 => (object)Escaped,
                2 => (object)UnaryValue,
                3 => (object)ArithmeticValue,
                4 => (object)BitwiseValue,
                5 => (object)ShiftedValue,
                6 => (object)HexValue,
                7 => (object)BinaryValue,
                8 => (object)OctalValue,
                9 => (object)DecimalValue,
                10 => (object)InRange,
                11 => (object)CaseValue,
                12 => (object)NullPropagation,
                13 => (object)NullFallback,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "RawPath" => (object)RawPath,
                "Escaped" => (object)Escaped,
                "UnaryValue" => (object)UnaryValue,
                "ArithmeticValue" => (object)ArithmeticValue,
                "BitwiseValue" => (object)BitwiseValue,
                "ShiftedValue" => (object)ShiftedValue,
                "HexValue" => (object)HexValue,
                "BinaryValue" => (object)BinaryValue,
                "OctalValue" => (object)OctalValue,
                "DecimalValue" => (object)DecimalValue,
                "InRange" => (object)InRange,
                "CaseValue" => (object)CaseValue,
                "NullPropagation" => (object)NullPropagation,
                "NullFallback" => (object)NullFallback,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string RawPath, string Escaped, decimal UnaryValue, decimal ArithmeticValue, int BitwiseValue, int ShiftedValue, long HexValue, long BinaryValue, long OctalValue, decimal DecimalValue, bool InRange, string CaseValue, decimal? NullPropagation, decimal NullFallback)
            {
                this.RawPath = RawPath;
                this.Escaped = Escaped;
                this.UnaryValue = UnaryValue;
                this.ArithmeticValue = ArithmeticValue;
                this.BitwiseValue = BitwiseValue;
                this.ShiftedValue = ShiftedValue;
                this.HexValue = HexValue;
                this.BinaryValue = BinaryValue;
                this.OctalValue = OctalValue;
                this.DecimalValue = DecimalValue;
                this.InRange = InRange;
                this.CaseValue = CaseValue;
                this.NullPropagation = NullPropagation;
                this.NullFallback = NullFallback;
            }

            public decimal ArithmeticValue { get; }
            public long BinaryValue { get; }
            public int BitwiseValue { get; }
            public string CaseValue { get; }
            public decimal DecimalValue { get; }
            public string Escaped { get; }
            public long HexValue { get; }
            public bool InRange { get; }
            public decimal NullFallback { get; }
            public decimal? NullPropagation { get; }
            public long OctalValue { get; }
            public string RawPath { get; }
            public int ShiftedValue { get; }
            public decimal UnaryValue { get; }
        }
    }
}
