// === Parsed Query ===
/*
from values {
                  (
                      PlainInt: 10,
                      UIntValue: 11ui,
                      LongValue: 12l,
                      ULongValue: 13ul,
                      ShortValue: 14s,
                      UShortValue: 15us,
                      SByteValue: 16b,
                      ByteValue: 17ub,
                      DecimalValue: 18.5d,
                      HexValue: 0x10,
                      BinaryValue: 0b1010,
                      OctalValue: 0o17
                  )
              } literals
              select literals.PlainInt,
                     literals.UIntValue,
                     literals.LongValue,
                     literals.ULongValue,
                     literals.ShortValue,
                     literals.UShortValue,
                     literals.SByteValue,
                     literals.ByteValue,
                     literals.DecimalValue,
                     literals.HexValue,
                     literals.BinaryValue,
                     literals.OctalValue
*/

// === Logical Plan ===
/*
MultiStatement
  Project [literals.PlainInt as PlainInt, literals.UIntValue as UIntValue, literals.LongValue as LongValue, literals.ULongValue as ULongValue, literals.ShortValue as ShortValue, literals.UShortValue as UShortValue, literals.SByteValue as SByteValue, literals.ByteValue as ByteValue, literals.DecimalValue as DecimalValue, literals.HexValue as HexValue, literals.BinaryValue as BinaryValue, literals.OctalValue as OctalValue]
    ValuesScan [1 rows as literals]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [literals.PlainInt as PlainInt, literals.UIntValue as UIntValue, literals.LongValue as LongValue, literals.ULongValue as ULongValue, literals.ShortValue as ShortValue, literals.UShortValue as UShortValue, literals.SByteValue as SByteValue, literals.ByteValue as ByteValue, literals.DecimalValue as DecimalValue, literals.HexValue as HexValue, literals.BinaryValue as BinaryValue, literals.OctalValue as OctalValue]
    PhysicalValuesScan [1 rows as literals]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      PlainInt: int <- field PlainInt
      UIntValue: uint <- field UIntValue
      LongValue: long <- field LongValue
      ULongValue: ulong <- field ULongValue
      ShortValue: short <- field ShortValue
      UShortValue: ushort <- field UShortValue
      SByteValue: sbyte <- field SByteValue
      ByteValue: byte <- field ByteValue
      DecimalValue: decimal <- field DecimalValue
      HexValue: long <- field HexValue
      BinaryValue: long <- field BinaryValue
      OctalValue: long <- field OctalValue
    Generated [ResultRow0]
      PlainInt: int <- field PlainInt
      UIntValue: uint <- field UIntValue
      LongValue: long <- field LongValue
      ULongValue: ulong <- field ULongValue
      ShortValue: short <- field ShortValue
      UShortValue: ushort <- field UShortValue
      SByteValue: sbyte <- field SByteValue
      ByteValue: byte <- field ByteValue
      DecimalValue: decimal <- field DecimalValue
      HexValue: long <- field HexValue
      BinaryValue: long <- field BinaryValue
      OctalValue: long <- field OctalValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    CreateValuesRows [literalsRows: literalsValuesDE6D03F2Row0 x 1]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [literals in literalsRows]
      AppendShape [result <- ResultRow0(PlainInt: literals.PlainInt, UIntValue: literals.UIntValue, LongValue: literals.LongValue, ULongValue: literals.ULongValue, ShortValue: literals.ShortValue, UShortValue: literals.UShortValue, SByteValue: literals.SByteValue, ByteValue: literals.ByteValue, DecimalValue: literals.DecimalValue, HexValue: literals.HexValue, BinaryValue: literals.BinaryValue, OctalValue: literals.OctalValue)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q119_ValuesNumericLiterals
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
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("PlainInt", typeof(int), 0),
            new Column("UIntValue", typeof(uint), 1),
            new Column("LongValue", typeof(long), 2),
            new Column("ULongValue", typeof(ulong), 3),
            new Column("ShortValue", typeof(short), 4),
            new Column("UShortValue", typeof(ushort), 5),
            new Column("SByteValue", typeof(sbyte), 6),
            new Column("ByteValue", typeof(byte), 7),
            new Column("DecimalValue", typeof(decimal), 8),
            new Column("HexValue", typeof(long), 9),
            new Column("BinaryValue", typeof(long), 10),
            new Column("OctalValue", typeof(long), 11)
        };
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                literalsValuesDE6D03F2Row0[] literalsRows = new literalsValuesDE6D03F2Row0[]
                {
                    new literalsValuesDE6D03F2Row0(10, 11u, 12L, 13ul, 14, 15, 16, 17, 18.5m, 16L, 10L, 15L)
                };
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var literals in literalsRows)
                {
                    token.ThrowIfCancellationRequested();
                    yield return new ResultRow0(literals.PlainInt, literals.UIntValue, literals.LongValue, literals.ULongValue, literals.ShortValue, literals.UShortValue, literals.SByteValue, literals.ByteValue, literals.DecimalValue, literals.HexValue, literals.BinaryValue, literals.OctalValue);
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
            public ResultRow0(int __value0, uint __value1, long __value2, ulong __value3, short __value4, ushort __value5, sbyte __value6, byte __value7, decimal __value8, long __value9, long __value10, long __value11)
            {
                PlainInt = __value0;
                UIntValue = __value1;
                LongValue = __value2;
                ULongValue = __value3;
                ShortValue = __value4;
                UShortValue = __value5;
                SByteValue = __value6;
                ByteValue = __value7;
                DecimalValue = __value8;
                HexValue = __value9;
                BinaryValue = __value10;
                OctalValue = __value11;
            }

            public long BinaryValue { get; private set; }
            public byte ByteValue { get; private set; }
            public override int Count => 12;
            public decimal DecimalValue { get; private set; }
            public long HexValue { get; private set; }
            public long LongValue { get; private set; }
            public long OctalValue { get; private set; }
            public int PlainInt { get; private set; }
            public sbyte SByteValue { get; private set; }
            public short ShortValue { get; private set; }
            public uint UIntValue { get; private set; }
            public ulong ULongValue { get; private set; }
            public ushort UShortValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.PlainInt = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, uint>(this.UIntValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<uint, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.LongValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ulong>(this.ULongValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<ulong, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.ShortValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ushort>(this.UShortValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<ushort, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, sbyte>(this.SByteValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<sbyte, T>(value)),
                    7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.ByteValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.DecimalValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.HexValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.BinaryValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.OctalValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.PlainInt),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, uint>(this.UIntValue),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.LongValue),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ulong>(this.ULongValue),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.ShortValue),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ushort>(this.UShortValue),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, sbyte>(this.SByteValue),
                7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.ByteValue),
                8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.DecimalValue),
                9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.HexValue),
                10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.BinaryValue),
                11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.OctalValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "PlainInt" => true,
                "UIntValue" => true,
                "LongValue" => true,
                "ULongValue" => true,
                "ShortValue" => true,
                "UShortValue" => true,
                "SByteValue" => true,
                "ByteValue" => true,
                "DecimalValue" => true,
                "HexValue" => true,
                "BinaryValue" => true,
                "OctalValue" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.PlainInt, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.UIntValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.LongValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.ULongValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.ShortValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.UShortValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.SByteValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(7, this.ByteValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(8, this.DecimalValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(9, this.HexValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(10, this.BinaryValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(11, this.OctalValue, ref visitor);
            }
        }

        private sealed class literalsValuesDE6D03F2Row0 : Row
        {
            public literalsValuesDE6D03F2Row0(int __value0, uint __value1, long __value2, ulong __value3, short __value4, ushort __value5, sbyte __value6, byte __value7, decimal __value8, long __value9, long __value10, long __value11)
            {
                PlainInt = __value0;
                UIntValue = __value1;
                LongValue = __value2;
                ULongValue = __value3;
                ShortValue = __value4;
                UShortValue = __value5;
                SByteValue = __value6;
                ByteValue = __value7;
                DecimalValue = __value8;
                HexValue = __value9;
                BinaryValue = __value10;
                OctalValue = __value11;
            }

            public long BinaryValue { get; private set; }
            public byte ByteValue { get; private set; }
            public override int Count => 12;
            public decimal DecimalValue { get; private set; }
            public long HexValue { get; private set; }
            public long LongValue { get; private set; }
            public long OctalValue { get; private set; }
            public int PlainInt { get; private set; }
            public sbyte SByteValue { get; private set; }
            public short ShortValue { get; private set; }
            public uint UIntValue { get; private set; }
            public ulong ULongValue { get; private set; }
            public ushort UShortValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.PlainInt = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, uint>(this.UIntValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<uint, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.LongValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ulong>(this.ULongValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<ulong, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.ShortValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ushort>(this.UShortValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<ushort, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, sbyte>(this.SByteValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<sbyte, T>(value)),
                    7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.ByteValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.DecimalValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.HexValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.BinaryValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.OctalValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.PlainInt),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, uint>(this.UIntValue),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.LongValue),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ulong>(this.ULongValue),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.ShortValue),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ushort>(this.UShortValue),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, sbyte>(this.SByteValue),
                7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.ByteValue),
                8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.DecimalValue),
                9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.HexValue),
                10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.BinaryValue),
                11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.OctalValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "PlainInt" => true,
                "UIntValue" => true,
                "LongValue" => true,
                "ULongValue" => true,
                "ShortValue" => true,
                "UShortValue" => true,
                "SByteValue" => true,
                "ByteValue" => true,
                "DecimalValue" => true,
                "HexValue" => true,
                "BinaryValue" => true,
                "OctalValue" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.PlainInt, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.UIntValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.LongValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.ULongValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.ShortValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.UShortValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.SByteValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(7, this.ByteValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(8, this.DecimalValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(9, this.HexValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(10, this.BinaryValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(11, this.OctalValue, ref visitor);
            }
        }
    }
}
