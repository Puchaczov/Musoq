// === Parsed Query ===
/*
from values {
                  {
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
                  }
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
  Project [literals.PlainInt as literals.PlainInt, literals.UIntValue as literals.UIntValue, literals.LongValue as literals.LongValue, literals.ULongValue as literals.ULongValue, literals.ShortValue as literals.ShortValue, literals.UShortValue as literals.UShortValue, literals.SByteValue as literals.SByteValue, literals.ByteValue as literals.ByteValue, literals.DecimalValue as literals.DecimalValue, literals.HexValue as literals.HexValue, literals.BinaryValue as literals.BinaryValue, literals.OctalValue as literals.OctalValue]
    ValuesScan [1 rows as literals]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [literals.PlainInt as literals.PlainInt, literals.UIntValue as literals.UIntValue, literals.LongValue as literals.LongValue, literals.ULongValue as literals.ULongValue, literals.ShortValue as literals.ShortValue, literals.UShortValue as literals.UShortValue, literals.SByteValue as literals.SByteValue, literals.ByteValue as literals.ByteValue, literals.DecimalValue as literals.DecimalValue, literals.HexValue as literals.HexValue, literals.BinaryValue as literals.BinaryValue, literals.OctalValue as literals.OctalValue]
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
      literals.PlainInt: int <- field literals_PlainInt
      literals.UIntValue: uint <- field literals_UIntValue
      literals.LongValue: long <- field literals_LongValue
      literals.ULongValue: ulong <- field literals_ULongValue
      literals.ShortValue: short <- field literals_ShortValue
      literals.UShortValue: ushort <- field literals_UShortValue
      literals.SByteValue: sbyte <- field literals_SByteValue
      literals.ByteValue: byte <- field literals_ByteValue
      literals.DecimalValue: decimal <- field literals_DecimalValue
      literals.HexValue: long <- field literals_HexValue
      literals.BinaryValue: long <- field literals_BinaryValue
      literals.OctalValue: long <- field literals_OctalValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    CreateValuesRows [literalsRows: literalsValuesDE6D03F2Row0 x 1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [literals in literalsRows]
      AppendShape [result <- ResultShape0(literals.PlainInt: literals.PlainInt, literals.UIntValue: literals.UIntValue, literals.LongValue: literals.LongValue, literals.ULongValue: literals.ULongValue, literals.ShortValue: literals.ShortValue, literals.UShortValue: literals.UShortValue, literals.SByteValue: literals.SByteValue, literals.ByteValue: literals.ByteValue, literals.DecimalValue: literals.DecimalValue, literals.HexValue: literals.HexValue, literals.BinaryValue: literals.BinaryValue, literals.OctalValue: literals.OctalValue)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("literals.PlainInt", typeof(int), 0),
            new Column("literals.UIntValue", typeof(uint), 1),
            new Column("literals.LongValue", typeof(long), 2),
            new Column("literals.ULongValue", typeof(ulong), 3),
            new Column("literals.ShortValue", typeof(short), 4),
            new Column("literals.UShortValue", typeof(ushort), 5),
            new Column("literals.SByteValue", typeof(sbyte), 6),
            new Column("literals.ByteValue", typeof(byte), 7),
            new Column("literals.DecimalValue", typeof(decimal), 8),
            new Column("literals.HexValue", typeof(long), 9),
            new Column("literals.BinaryValue", typeof(long), 10),
            new Column("literals.OctalValue", typeof(long), 11)
        };
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.literals_PlainInt, __musoqShapeRow.literals_UIntValue, __musoqShapeRow.literals_LongValue, __musoqShapeRow.literals_ULongValue, __musoqShapeRow.literals_ShortValue, __musoqShapeRow.literals_UShortValue, __musoqShapeRow.literals_SByteValue, __musoqShapeRow.literals_ByteValue, __musoqShapeRow.literals_DecimalValue, __musoqShapeRow.literals_HexValue, __musoqShapeRow.literals_BinaryValue, __musoqShapeRow.literals_OctalValue);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
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
                    yield return new ResultShape0(literals.PlainInt, literals.UIntValue, literals.LongValue, literals.ULongValue, literals.ShortValue, literals.UShortValue, literals.SByteValue, literals.ByteValue, literals.DecimalValue, literals.HexValue, literals.BinaryValue, literals.OctalValue);
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
                literals_PlainInt = __value0;
                literals_UIntValue = __value1;
                literals_LongValue = __value2;
                literals_ULongValue = __value3;
                literals_ShortValue = __value4;
                literals_UShortValue = __value5;
                literals_SByteValue = __value6;
                literals_ByteValue = __value7;
                literals_DecimalValue = __value8;
                literals_HexValue = __value9;
                literals_BinaryValue = __value10;
                literals_OctalValue = __value11;
            }

            public override int Count => 12;
            public long literals_BinaryValue { get; private set; }
            public byte literals_ByteValue { get; private set; }
            public decimal literals_DecimalValue { get; private set; }
            public long literals_HexValue { get; private set; }
            public long literals_LongValue { get; private set; }
            public long literals_OctalValue { get; private set; }
            public int literals_PlainInt { get; private set; }
            public sbyte literals_SByteValue { get; private set; }
            public short literals_ShortValue { get; private set; }
            public uint literals_UIntValue { get; private set; }
            public ulong literals_ULongValue { get; private set; }
            public ushort literals_UShortValue { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        literals_PlainInt = (int)value;
                        break;
                    case 1:
                        literals_UIntValue = (uint)value;
                        break;
                    case 2:
                        literals_LongValue = (long)value;
                        break;
                    case 3:
                        literals_ULongValue = (ulong)value;
                        break;
                    case 4:
                        literals_ShortValue = (short)value;
                        break;
                    case 5:
                        literals_UShortValue = (ushort)value;
                        break;
                    case 6:
                        literals_SByteValue = (sbyte)value;
                        break;
                    case 7:
                        literals_ByteValue = (byte)value;
                        break;
                    case 8:
                        literals_DecimalValue = (decimal)value;
                        break;
                    case 9:
                        literals_HexValue = (long)value;
                        break;
                    case 10:
                        literals_BinaryValue = (long)value;
                        break;
                    case 11:
                        literals_OctalValue = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "literals.PlainInt" => true,
                "literals_PlainInt" => true,
                "PlainInt" => true,
                "literals.UIntValue" => true,
                "literals_UIntValue" => true,
                "UIntValue" => true,
                "literals.LongValue" => true,
                "literals_LongValue" => true,
                "LongValue" => true,
                "literals.ULongValue" => true,
                "literals_ULongValue" => true,
                "ULongValue" => true,
                "literals.ShortValue" => true,
                "literals_ShortValue" => true,
                "ShortValue" => true,
                "literals.UShortValue" => true,
                "literals_UShortValue" => true,
                "UShortValue" => true,
                "literals.SByteValue" => true,
                "literals_SByteValue" => true,
                "SByteValue" => true,
                "literals.ByteValue" => true,
                "literals_ByteValue" => true,
                "ByteValue" => true,
                "literals.DecimalValue" => true,
                "literals_DecimalValue" => true,
                "DecimalValue" => true,
                "literals.HexValue" => true,
                "literals_HexValue" => true,
                "HexValue" => true,
                "literals.BinaryValue" => true,
                "literals_BinaryValue" => true,
                "BinaryValue" => true,
                "literals.OctalValue" => true,
                "literals_OctalValue" => true,
                "OctalValue" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)literals_PlainInt,
                1 => (object)literals_UIntValue,
                2 => (object)literals_LongValue,
                3 => (object)literals_ULongValue,
                4 => (object)literals_ShortValue,
                5 => (object)literals_UShortValue,
                6 => (object)literals_SByteValue,
                7 => (object)literals_ByteValue,
                8 => (object)literals_DecimalValue,
                9 => (object)literals_HexValue,
                10 => (object)literals_BinaryValue,
                11 => (object)literals_OctalValue,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "literals.PlainInt" => (object)literals_PlainInt,
                "literals_PlainInt" => (object)literals_PlainInt,
                "PlainInt" => (object)literals_PlainInt,
                "literals.UIntValue" => (object)literals_UIntValue,
                "literals_UIntValue" => (object)literals_UIntValue,
                "UIntValue" => (object)literals_UIntValue,
                "literals.LongValue" => (object)literals_LongValue,
                "literals_LongValue" => (object)literals_LongValue,
                "LongValue" => (object)literals_LongValue,
                "literals.ULongValue" => (object)literals_ULongValue,
                "literals_ULongValue" => (object)literals_ULongValue,
                "ULongValue" => (object)literals_ULongValue,
                "literals.ShortValue" => (object)literals_ShortValue,
                "literals_ShortValue" => (object)literals_ShortValue,
                "ShortValue" => (object)literals_ShortValue,
                "literals.UShortValue" => (object)literals_UShortValue,
                "literals_UShortValue" => (object)literals_UShortValue,
                "UShortValue" => (object)literals_UShortValue,
                "literals.SByteValue" => (object)literals_SByteValue,
                "literals_SByteValue" => (object)literals_SByteValue,
                "SByteValue" => (object)literals_SByteValue,
                "literals.ByteValue" => (object)literals_ByteValue,
                "literals_ByteValue" => (object)literals_ByteValue,
                "ByteValue" => (object)literals_ByteValue,
                "literals.DecimalValue" => (object)literals_DecimalValue,
                "literals_DecimalValue" => (object)literals_DecimalValue,
                "DecimalValue" => (object)literals_DecimalValue,
                "literals.HexValue" => (object)literals_HexValue,
                "literals_HexValue" => (object)literals_HexValue,
                "HexValue" => (object)literals_HexValue,
                "literals.BinaryValue" => (object)literals_BinaryValue,
                "literals_BinaryValue" => (object)literals_BinaryValue,
                "BinaryValue" => (object)literals_BinaryValue,
                "literals.OctalValue" => (object)literals_OctalValue,
                "literals_OctalValue" => (object)literals_OctalValue,
                "OctalValue" => (object)literals_OctalValue,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int literals_PlainInt, uint literals_UIntValue, long literals_LongValue, ulong literals_ULongValue, short literals_ShortValue, ushort literals_UShortValue, sbyte literals_SByteValue, byte literals_ByteValue, decimal literals_DecimalValue, long literals_HexValue, long literals_BinaryValue, long literals_OctalValue)
            {
                this.literals_PlainInt = literals_PlainInt;
                this.literals_UIntValue = literals_UIntValue;
                this.literals_LongValue = literals_LongValue;
                this.literals_ULongValue = literals_ULongValue;
                this.literals_ShortValue = literals_ShortValue;
                this.literals_UShortValue = literals_UShortValue;
                this.literals_SByteValue = literals_SByteValue;
                this.literals_ByteValue = literals_ByteValue;
                this.literals_DecimalValue = literals_DecimalValue;
                this.literals_HexValue = literals_HexValue;
                this.literals_BinaryValue = literals_BinaryValue;
                this.literals_OctalValue = literals_OctalValue;
            }

            public long literals_BinaryValue { get; }
            public byte literals_ByteValue { get; }
            public decimal literals_DecimalValue { get; }
            public long literals_HexValue { get; }
            public long literals_LongValue { get; }
            public long literals_OctalValue { get; }
            public int literals_PlainInt { get; }
            public sbyte literals_SByteValue { get; }
            public short literals_ShortValue { get; }
            public uint literals_UIntValue { get; }
            public ulong literals_ULongValue { get; }
            public ushort literals_UShortValue { get; }
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        PlainInt = (int)value;
                        break;
                    case 1:
                        UIntValue = (uint)value;
                        break;
                    case 2:
                        LongValue = (long)value;
                        break;
                    case 3:
                        ULongValue = (ulong)value;
                        break;
                    case 4:
                        ShortValue = (short)value;
                        break;
                    case 5:
                        UShortValue = (ushort)value;
                        break;
                    case 6:
                        SByteValue = (sbyte)value;
                        break;
                    case 7:
                        ByteValue = (byte)value;
                        break;
                    case 8:
                        DecimalValue = (decimal)value;
                        break;
                    case 9:
                        HexValue = (long)value;
                        break;
                    case 10:
                        BinaryValue = (long)value;
                        break;
                    case 11:
                        OctalValue = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

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
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)PlainInt,
                1 => (object)UIntValue,
                2 => (object)LongValue,
                3 => (object)ULongValue,
                4 => (object)ShortValue,
                5 => (object)UShortValue,
                6 => (object)SByteValue,
                7 => (object)ByteValue,
                8 => (object)DecimalValue,
                9 => (object)HexValue,
                10 => (object)BinaryValue,
                11 => (object)OctalValue,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "PlainInt" => (object)PlainInt,
                "UIntValue" => (object)UIntValue,
                "LongValue" => (object)LongValue,
                "ULongValue" => (object)ULongValue,
                "ShortValue" => (object)ShortValue,
                "UShortValue" => (object)UShortValue,
                "SByteValue" => (object)SByteValue,
                "ByteValue" => (object)ByteValue,
                "DecimalValue" => (object)DecimalValue,
                "HexValue" => (object)HexValue,
                "BinaryValue" => (object)BinaryValue,
                "OctalValue" => (object)OctalValue,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
