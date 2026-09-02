// === Parsed Query ===
/*
table TypeMatrix {
                        ByteCol: byte,
                        SByteCol: sbyte,
                        ShortCol: short,
                        IntCol: Int,
                        LongCol: long,
                        UShortCol: ushort,
                        UIntCol: uint,
                        ULongCol: ulong,
                        FloatCol: float,
                        DoubleCol: double,
                        DecimalCol: decimal,
                        MoneyCol: money,
                        BoolCol: boolean,
                        BitCol: bit,
                        CharCol: char,
                        StringCol: STRING,
                        DateTimeCol: datetime,
                        DateTimeOffsetCol: datetimeoffset?,
                        TimeSpanCol: timespan,
                        GuidCol: guid,
                        ObjectCol: object,
                        FullyQualified: System.Int32,
                        NullableInt: int?,
                    };
                    couple #unknown.rows with table TypeMatrix as Typed;
                    select ByteCol, SByteCol, ShortCol, IntCol, LongCol, UShortCol, UIntCol,
                        ULongCol, FloatCol, DoubleCol, DecimalCol, MoneyCol, BoolCol, BitCol,
                        CharCol, StringCol, DateTimeCol, DateTimeOffsetCol, TimeSpanCol, GuidCol,
                        ObjectCol,
                        FullyQualified, NullableInt
                    from Typed()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.ByteCol as ByteCol, ko3iko.SByteCol as SByteCol, ko3iko.ShortCol as ShortCol, ko3iko.IntCol as IntCol, ko3iko.LongCol as LongCol, ko3iko.UShortCol as UShortCol, ko3iko.UIntCol as UIntCol, ko3iko.ULongCol as ULongCol, ko3iko.FloatCol as FloatCol, ko3iko.DoubleCol as DoubleCol, ko3iko.DecimalCol as DecimalCol, ko3iko.MoneyCol as MoneyCol, ko3iko.BoolCol as BoolCol, ko3iko.BitCol as BitCol, ko3iko.CharCol as CharCol, ko3iko.StringCol as StringCol, ko3iko.DateTimeCol as DateTimeCol, ko3iko.DateTimeOffsetCol as DateTimeOffsetCol, ko3iko.TimeSpanCol as TimeSpanCol, ko3iko.GuidCol as GuidCol, ko3iko.ObjectCol as ObjectCol, ko3iko.FullyQualified as FullyQualified, ko3iko.NullableInt as NullableInt]
    SchemaScan [#unknown.rows() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.ByteCol as ByteCol, ko3iko.SByteCol as SByteCol, ko3iko.ShortCol as ShortCol, ko3iko.IntCol as IntCol, ko3iko.LongCol as LongCol, ko3iko.UShortCol as UShortCol, ko3iko.UIntCol as UIntCol, ko3iko.ULongCol as ULongCol, ko3iko.FloatCol as FloatCol, ko3iko.DoubleCol as DoubleCol, ko3iko.DecimalCol as DecimalCol, ko3iko.MoneyCol as MoneyCol, ko3iko.BoolCol as BoolCol, ko3iko.BitCol as BitCol, ko3iko.CharCol as CharCol, ko3iko.StringCol as StringCol, ko3iko.DateTimeCol as DateTimeCol, ko3iko.DateTimeOffsetCol as DateTimeOffsetCol, ko3iko.TimeSpanCol as TimeSpanCol, ko3iko.GuidCol as GuidCol, ko3iko.ObjectCol as ObjectCol, ko3iko.FullyQualified as FullyQualified, ko3iko.NullableInt as NullableInt]
    PhysicalSchemaScan [#unknown.rows() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: SpecificationTypeMatrixEntity]
      ByteCol: byte <- property ByteCol
      SByteCol: sbyte <- property SByteCol
      ShortCol: short <- property ShortCol
      IntCol: int <- property IntCol
      LongCol: long <- property LongCol
      UShortCol: ushort <- property UShortCol
      UIntCol: uint <- property UIntCol
      ULongCol: ulong <- property ULongCol
      FloatCol: float <- property FloatCol
      DoubleCol: double <- property DoubleCol
      DecimalCol: decimal <- property DecimalCol
      MoneyCol: decimal <- property MoneyCol
      BoolCol: bool <- property BoolCol
      BitCol: bool <- property BitCol
      CharCol: char <- property CharCol
      StringCol: string <- property StringCol
      DateTimeCol: DateTime <- property DateTimeCol
      DateTimeOffsetCol: DateTimeOffset? <- property DateTimeOffsetCol
      TimeSpanCol: TimeSpan <- property TimeSpanCol
      GuidCol: Guid <- property GuidCol
      ObjectCol: object <- property ObjectCol
      FullyQualified: int <- property FullyQualified
      NullableInt: int? <- property NullableInt
    Generated [ResultRow0]
      ByteCol: byte <- field ByteCol
      SByteCol: sbyte <- field SByteCol
      ShortCol: short <- field ShortCol
      IntCol: int <- field IntCol
      LongCol: long <- field LongCol
      UShortCol: ushort <- field UShortCol
      UIntCol: uint <- field UIntCol
      ULongCol: ulong <- field ULongCol
      FloatCol: float <- field FloatCol
      DoubleCol: double <- field DoubleCol
      DecimalCol: decimal <- field DecimalCol
      MoneyCol: decimal <- field MoneyCol
      BoolCol: bool <- field BoolCol
      BitCol: bool <- field BitCol
      CharCol: char <- field CharCol
      StringCol: string <- field StringCol
      DateTimeCol: DateTime <- field DateTimeCol
      DateTimeOffsetCol: DateTimeOffset? <- field DateTimeOffsetCol
      TimeSpanCol: TimeSpan <- field TimeSpanCol
      GuidCol: Guid <- field GuidCol
      ObjectCol: object <- field ObjectCol
      FullyQualified: int <- field FullyQualified
      NullableInt: int? <- field NullableInt

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: SpecificationTypeMatrixEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultShape0(ByteCol: ko3iko.ByteCol, SByteCol: ko3iko.SByteCol, ShortCol: ko3iko.ShortCol, IntCol: ko3iko.IntCol, LongCol: ko3iko.LongCol, UShortCol: ko3iko.UShortCol, UIntCol: ko3iko.UIntCol, ULongCol: ko3iko.ULongCol, FloatCol: ko3iko.FloatCol, DoubleCol: ko3iko.DoubleCol, DecimalCol: ko3iko.DecimalCol, MoneyCol: ko3iko.MoneyCol, BoolCol: ko3iko.BoolCol, BitCol: ko3iko.BitCol, CharCol: ko3iko.CharCol, StringCol: ko3iko.StringCol, DateTimeCol: ko3iko.DateTimeCol, DateTimeOffsetCol: ko3iko.DateTimeOffsetCol, TimeSpanCol: ko3iko.TimeSpanCol, GuidCol: ko3iko.GuidCol, ObjectCol: ko3iko.ObjectCol, FullyQualified: ko3iko.FullyQualified, NullableInt: ko3iko.NullableInt)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q319_SpecTableTypeMatrix
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
            new Column("ByteCol", typeof(byte), 0),
            new Column("SByteCol", typeof(sbyte), 1),
            new Column("ShortCol", typeof(short), 2),
            new Column("IntCol", typeof(int), 3),
            new Column("LongCol", typeof(long), 4),
            new Column("UShortCol", typeof(ushort), 5),
            new Column("UIntCol", typeof(uint), 6),
            new Column("ULongCol", typeof(ulong), 7),
            new Column("FloatCol", typeof(float), 8),
            new Column("DoubleCol", typeof(double), 9),
            new Column("DecimalCol", typeof(decimal), 10),
            new Column("MoneyCol", typeof(decimal), 11),
            new Column("BoolCol", typeof(bool), 12),
            new Column("BitCol", typeof(bool), 13),
            new Column("CharCol", typeof(char), 14),
            new Column("StringCol", typeof(string), 15),
            new Column("DateTimeCol", typeof(DateTime), 16),
            new Column("DateTimeOffsetCol", typeof(DateTimeOffset?), 17),
            new Column("TimeSpanCol", typeof(TimeSpan), 18),
            new Column("GuidCol", typeof(Guid), 19),
            new Column("ObjectCol", typeof(object), 20),
            new Column("FullyQualified", typeof(int), 21),
            new Column("NullableInt", typeof(int?), 22)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("ByteCol", typeof(byte), 0), new Column("SByteCol", typeof(sbyte), 1), new Column("ShortCol", typeof(short), 2), new Column("IntCol", typeof(int), 3), new Column("LongCol", typeof(long), 4), new Column("UShortCol", typeof(ushort), 5), new Column("UIntCol", typeof(uint), 6), new Column("ULongCol", typeof(ulong), 7), new Column("FloatCol", typeof(float), 8), new Column("DoubleCol", typeof(double), 9), new Column("DecimalCol", typeof(decimal), 10), new Column("MoneyCol", typeof(decimal), 11), new Column("BoolCol", typeof(bool), 12), new Column("BitCol", typeof(bool), 13), new Column("CharCol", typeof(char), 14), new Column("StringCol", typeof(string), 15), new Column("DateTimeCol", typeof(DateTime), 16), new Column("DateTimeOffsetCol", typeof(DateTimeOffset?), 17), new Column("TimeSpanCol", typeof(TimeSpan), 18), new Column("GuidCol", typeof(Guid), 19), new Column("ObjectCol", typeof(object), 20), new Column("FullyQualified", typeof(int), 21), new Column("NullableInt", typeof(int?), 22) });
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
            var __ko3ikoSchema = provider.GetSchema("#unknown");
            var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.SpecificationTypeMatrixEntity>("rows", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.SpecificationTypeMatrixEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.SpecificationTypeMatrixEntity, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0(ko3iko.ByteCol, ko3iko.SByteCol, ko3iko.ShortCol, ko3iko.IntCol, ko3iko.LongCol, ko3iko.UShortCol, ko3iko.UIntCol, ko3iko.ULongCol, ko3iko.FloatCol, ko3iko.DoubleCol, ko3iko.DecimalCol, ko3iko.MoneyCol, ko3iko.BoolCol, ko3iko.BitCol, ko3iko.CharCol, ko3iko.StringCol, ko3iko.DateTimeCol, ko3iko.DateTimeOffsetCol, ko3iko.TimeSpanCol, ko3iko.GuidCol, ko3iko.ObjectCol, ko3iko.FullyQualified, ko3iko.NullableInt), token), token, onCompleted: () =>
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
            private static readonly Action<ResultRow0, object>[] __assigners = new Action<ResultRow0, object>[]
            {
                static (row, value) => row.ByteCol = (byte)value,
                static (row, value) => row.SByteCol = (sbyte)value,
                static (row, value) => row.ShortCol = (short)value,
                static (row, value) => row.IntCol = (int)value,
                static (row, value) => row.LongCol = (long)value,
                static (row, value) => row.UShortCol = (ushort)value,
                static (row, value) => row.UIntCol = (uint)value,
                static (row, value) => row.ULongCol = (ulong)value,
                static (row, value) => row.FloatCol = (float)value,
                static (row, value) => row.DoubleCol = (double)value,
                static (row, value) => row.DecimalCol = (decimal)value,
                static (row, value) => row.MoneyCol = (decimal)value,
                static (row, value) => row.BoolCol = (bool)value,
                static (row, value) => row.BitCol = (bool)value,
                static (row, value) => row.CharCol = (char)value,
                static (row, value) => row.StringCol = (string)value,
                static (row, value) => row.DateTimeCol = (DateTime)value,
                static (row, value) => row.DateTimeOffsetCol = (DateTimeOffset?)value,
                static (row, value) => row.TimeSpanCol = (TimeSpan)value,
                static (row, value) => row.GuidCol = (Guid)value,
                static (row, value) => row.ObjectCol = value,
                static (row, value) => row.FullyQualified = (int)value,
                static (row, value) => row.NullableInt = (int?)value
            };
            private const string __columnIndexPairs = "ByteCol\n0\nSByteCol\n1\nShortCol\n2\nIntCol\n3\nLongCol\n4\nUShortCol\n5\nUIntCol\n6\nULongCol\n7\nFloatCol\n8\nDoubleCol\n9\nDecimalCol\n10\nMoneyCol\n11\nBoolCol\n12\nBitCol\n13\nCharCol\n14\nStringCol\n15\nDateTimeCol\n16\nDateTimeOffsetCol\n17\nTimeSpanCol\n18\nGuidCol\n19\nObjectCol\n20\nFullyQualified\n21\nNullableInt\n22";
            private static readonly Dictionary<string, int> __columnIndexes = CreateColumnIndexes();
            public ResultRow0(byte __value0, sbyte __value1, short __value2, int __value3, long __value4, ushort __value5, uint __value6, ulong __value7, float __value8, double __value9, decimal __value10, decimal __value11, bool __value12, bool __value13, char __value14, string __value15, DateTime __value16, DateTimeOffset? __value17, TimeSpan __value18, Guid __value19, object __value20, int __value21, int? __value22)
            {
                ByteCol = __value0;
                SByteCol = __value1;
                ShortCol = __value2;
                IntCol = __value3;
                LongCol = __value4;
                UShortCol = __value5;
                UIntCol = __value6;
                ULongCol = __value7;
                FloatCol = __value8;
                DoubleCol = __value9;
                DecimalCol = __value10;
                MoneyCol = __value11;
                BoolCol = __value12;
                BitCol = __value13;
                CharCol = __value14;
                StringCol = __value15;
                DateTimeCol = __value16;
                DateTimeOffsetCol = __value17;
                TimeSpanCol = __value18;
                GuidCol = __value19;
                ObjectCol = __value20;
                FullyQualified = __value21;
                NullableInt = __value22;
            }

            public bool BitCol { get; private set; }
            public bool BoolCol { get; private set; }
            public byte ByteCol { get; private set; }
            public char CharCol { get; private set; }
            public override int Count => 23;
            public DateTime DateTimeCol { get; private set; }
            public DateTimeOffset? DateTimeOffsetCol { get; private set; }
            public decimal DecimalCol { get; private set; }
            public double DoubleCol { get; private set; }
            public float FloatCol { get; private set; }
            public int FullyQualified { get; private set; }
            public Guid GuidCol { get; private set; }
            public int IntCol { get; private set; }
            public long LongCol { get; private set; }
            public decimal MoneyCol { get; private set; }
            public int? NullableInt { get; private set; }
            public object ObjectCol { get; private set; }
            public sbyte SByteCol { get; private set; }
            public short ShortCol { get; private set; }
            public string StringCol { get; private set; }
            public TimeSpan TimeSpanCol { get; private set; }
            public uint UIntCol { get; private set; }
            public ulong ULongCol { get; private set; }
            public ushort UShortCol { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                if ((uint)columnNumber >= (uint)__assigners.Length)
                    throw new IndexOutOfRangeException();
                __assigners[columnNumber](this, value);
            }

            public override bool HasColumn(string name) => __columnIndexes.ContainsKey(name);
            private static Dictionary<string, int> CreateColumnIndexes()
            {
                var pairs = __columnIndexPairs.Split('\n');
                var indexes = new Dictionary<string, int>(pairs.Length / 2, StringComparer.Ordinal);
                for (var index = 0; index < pairs.Length; index += 2)
                    indexes.Add(pairs[index], int.Parse(pairs[index + 1], System.Globalization.CultureInfo.InvariantCulture));
                return indexes;
            }

            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)ByteCol,
                1 => (object)SByteCol,
                2 => (object)ShortCol,
                3 => (object)IntCol,
                4 => (object)LongCol,
                5 => (object)UShortCol,
                6 => (object)UIntCol,
                7 => (object)ULongCol,
                8 => (object)FloatCol,
                9 => (object)DoubleCol,
                10 => (object)DecimalCol,
                11 => (object)MoneyCol,
                12 => (object)BoolCol,
                13 => (object)BitCol,
                14 => (object)CharCol,
                15 => (object)StringCol,
                16 => (object)DateTimeCol,
                17 => (object)DateTimeOffsetCol,
                18 => (object)TimeSpanCol,
                19 => (object)GuidCol,
                20 => (object)ObjectCol,
                21 => (object)FullyQualified,
                22 => (object)NullableInt,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => __columnIndexes.TryGetValue(name, out var columnIndex) ? this[columnIndex] : throw new KeyNotFoundException(name);
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte ByteCol, sbyte SByteCol, short ShortCol, int IntCol, long LongCol, ushort UShortCol, uint UIntCol, ulong ULongCol, float FloatCol, double DoubleCol, decimal DecimalCol, decimal MoneyCol, bool BoolCol, bool BitCol, char CharCol, string StringCol, DateTime DateTimeCol, DateTimeOffset? DateTimeOffsetCol, TimeSpan TimeSpanCol, Guid GuidCol, object ObjectCol, int FullyQualified, int? NullableInt)
            {
                this.ByteCol = ByteCol;
                this.SByteCol = SByteCol;
                this.ShortCol = ShortCol;
                this.IntCol = IntCol;
                this.LongCol = LongCol;
                this.UShortCol = UShortCol;
                this.UIntCol = UIntCol;
                this.ULongCol = ULongCol;
                this.FloatCol = FloatCol;
                this.DoubleCol = DoubleCol;
                this.DecimalCol = DecimalCol;
                this.MoneyCol = MoneyCol;
                this.BoolCol = BoolCol;
                this.BitCol = BitCol;
                this.CharCol = CharCol;
                this.StringCol = StringCol;
                this.DateTimeCol = DateTimeCol;
                this.DateTimeOffsetCol = DateTimeOffsetCol;
                this.TimeSpanCol = TimeSpanCol;
                this.GuidCol = GuidCol;
                this.ObjectCol = ObjectCol;
                this.FullyQualified = FullyQualified;
                this.NullableInt = NullableInt;
            }

            public bool BitCol { get; }
            public bool BoolCol { get; }
            public byte ByteCol { get; }
            public char CharCol { get; }
            public DateTime DateTimeCol { get; }
            public DateTimeOffset? DateTimeOffsetCol { get; }
            public decimal DecimalCol { get; }
            public double DoubleCol { get; }
            public float FloatCol { get; }
            public int FullyQualified { get; }
            public Guid GuidCol { get; }
            public int IntCol { get; }
            public long LongCol { get; }
            public decimal MoneyCol { get; }
            public int? NullableInt { get; }
            public object ObjectCol { get; }
            public sbyte SByteCol { get; }
            public short ShortCol { get; }
            public string StringCol { get; }
            public TimeSpan TimeSpanCol { get; }
            public uint UIntCol { get; }
            public ulong ULongCol { get; }
            public ushort UShortCol { get; }
        }
    }
}
