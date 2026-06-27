using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PostfixCastEvaluatorTests : GenericEntityTestBase
{
    [TestMethod]
    public void PostfixCast_AllSupportedClrTargets_ShouldCastStringsThroughLibrary()
    {
        var source = new[]
        {
            CreateFullCastEntity()
        };
        var query = @"
            select
                BooleanText::Boolean as BoolValue,
                ByteText::Byte as ByteValue,
                SByteText::SByte as SByteValue,
                Int16Text::Int16 as Int16Value,
                UInt16Text::UInt16 as UInt16Value,
                Int32Text::Int32 as Int32Value,
                UInt32Text::UInt32 as UInt32Value,
                Int64Text::Int64 as Int64Value,
                UInt64Text::UInt64 as UInt64Value,
                SingleText::Single as SingleValue,
                DoubleText::Double as DoubleValue,
                DecimalText::Decimal as DecimalValue,
                CharText::Char as CharValue,
                ObjectValue::String as StringValue,
                DateTimeText::DateTime as DateTimeValue,
                DateTimeOffsetText::DateTimeOffset as DateTimeOffsetValue,
                TimeSpanText::TimeSpan as TimeSpanValue,
                GuidText::Guid as GuidValue
            from #schema.first()";

        var table = CreateAndRunVirtualMachine(query, source).Run();

        Assert.AreEqual(1, table.Count);
        AssertColumnTypes(table.Columns.Select(column => column.ColumnType).ToArray(),
        [
            typeof(bool?), typeof(byte?), typeof(sbyte?), typeof(short?), typeof(ushort?),
            typeof(int?), typeof(uint?), typeof(long?), typeof(ulong?), typeof(float?),
            typeof(double?), typeof(decimal?), typeof(char?), typeof(string), typeof(DateTime?),
            typeof(DateTimeOffset?), typeof(TimeSpan?), typeof(Guid?)
        ]);

        var row = table[0];
        Assert.AreEqual(true, row[0]);
        Assert.AreEqual((byte)255, row[1]);
        Assert.AreEqual((sbyte)-12, row[2]);
        Assert.AreEqual((short)-1234, row[3]);
        Assert.AreEqual((ushort)1234, row[4]);
        Assert.AreEqual(-123456, row[5]);
        Assert.AreEqual((uint)123456, row[6]);
        Assert.AreEqual(-1234567890123L, row[7]);
        Assert.AreEqual(1234567890123UL, row[8]);
        Assert.AreEqual(1.5f, (float)row[9]!);
        Assert.AreEqual(2.25d, row[10]);
        Assert.AreEqual(123.45m, row[11]);
        Assert.AreEqual('Z', row[12]);
        Assert.AreEqual("42", row[13]);
        Assert.AreEqual(DateTime.Parse("2024-06-15T13:45:30", CultureInfo.InvariantCulture), row[14]);
        Assert.AreEqual(DateTimeOffset.Parse("2024-06-15T13:45:30+02:00", CultureInfo.InvariantCulture), row[15]);
        Assert.AreEqual(TimeSpan.Parse("01:02:03", CultureInfo.InvariantCulture), row[16]);
        Assert.AreEqual(Guid.Parse("12345678-1234-1234-1234-123456789012"), row[17]);
    }

    [TestMethod]
    public void PostfixCast_TargetNameMatching_ShouldBeCaseInsensitive()
    {
        var table = CreateAndRunVirtualMachine(
            "select Int32Text::int32 from #schema.first()",
            [CreateFullCastEntity()]).Run();

        Assert.AreEqual(-123456, table[0][0]);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(0).ColumnType);
    }

    [TestMethod]
    public void PostfixCast_NullInput_ShouldReturnNull()
    {
        var table = CreateAndRunVirtualMachine(
            "select NullText::Int32, NullText::Guid, NullText::String from #schema.first()",
            [new CastEntity { NullText = null }]).Run();

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
        AssertColumnTypes(table.Columns.Select(column => column.ColumnType).ToArray(),
            [typeof(int?), typeof(Guid?), typeof(string)]);
    }

    [TestMethod]
    public void PostfixCast_InvalidText_ShouldReturnNullLikeToInt32()
    {
        var source = new[] { new CastEntity { InvalidText = "not_a_number" } };
        var castTable = CreateAndRunVirtualMachine(
            "select InvalidText::Int32 from #schema.first()",
            source).Run();

        var softTable = CreateAndRunVirtualMachine(
            "select ToInt32(InvalidText) from #schema.first()",
            source).Run();

        Assert.IsNull(castTable[0][0]);
        Assert.IsNull(softTable[0][0]);
    }

    [TestMethod]
    public void PostfixCast_RepeatedInvalidText_ShouldReturnNullForEveryOccurrence()
    {
        var table = CreateAndRunVirtualMachine(
            "select InvalidText::Int32, InvalidText::Int32 from #schema.first()",
            [new CastEntity { InvalidText = "not_a_number" }]).Run();

        Assert.IsNull(table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void PostfixCast_Overflow_ShouldReturnNull()
    {
        var table = CreateAndRunVirtualMachine(
            "select OverflowText::Byte from #schema.first()",
            [new CastEntity { OverflowText = "256" }]).Run();

        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void PostfixCast_RepeatedCastInWhereAndSelect_ShouldPreserveFiltering()
    {
        var rows = new[]
        {
            new CastEntity { Int32Text = "0" },
            new CastEntity { Int32Text = "2" },
            new CastEntity { Int32Text = "not_a_number" }
        };

        var table = CreateAndRunVirtualMachine(
            "select Int32Text::Int32 from #schema.first() where Int32Text::Int32 > 1",
            rows).Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0]);
    }

    [TestMethod]
    public void PostfixCast_UnknownTarget_ShouldReportUnsupportedSyntax()
    {
        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            "select Int32Text::INTEGER from #schema.first()",
            [CreateFullCastEntity()]));

        AssertSingleError(ex, DiagnosticCode.MQ2030_UnsupportedSyntax, DiagnosticPhase.Parse, "CLR type names only");
    }

    [TestMethod]
    public void PostfixCast_NestedAndParenthesizedCasts_ShouldExecuteLeftToRight()
    {
        var table = CreateAndRunVirtualMachine(
            "select Int32Text::Int32::String, (PrefixText + SuffixText)::Int32 from #schema.first()",
            [CreateFullCastEntity()]).Run();

        Assert.AreEqual("-123456", table[0][0]);
        Assert.AreEqual(123, table[0][1]);
    }

    [TestMethod]
    public void PostfixCast_CastsInWhereOrderByAndCase_ShouldExecute()
    {
        var rows = new[]
        {
            new CastEntity { Int32Text = "2", BooleanText = "true", OtherText = "100" },
            new CastEntity { Int32Text = "10", BooleanText = "false", OtherText = "200" },
            new CastEntity { Int32Text = "1", BooleanText = "true", OtherText = "300" }
        };
        var query = @"
            select case when BooleanText::Boolean then Int32Text::Int32 else OtherText::Int32 end
            from #schema.first()
            where Int32Text::Int32 > 1
            order by Int32Text::Int32 desc";

        var table = CreateAndRunVirtualMachine(query, rows).Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(200, table[0][0]);
        Assert.AreEqual(2, table[1][0]);
    }

    [TestMethod]
    public void PostfixCast_CastsInGroupByHavingAndAggregateInputs_ShouldExecute()
    {
        var rows = new[]
        {
            new CastEntity { Category = "A", Int32Text = "1", DecimalText = "1.25" },
            new CastEntity { Category = "A", Int32Text = "1", DecimalText = "1.75" },
            new CastEntity { Category = "B", Int32Text = "2", DecimalText = "10.00" }
        };
        var aggregateQuery = @"
            select Category, Sum(DecimalText::Decimal)
            from #schema.first()
            group by Category
            having Category::String = 'A'";
        var groupByQuery = @"
            select Int32Text::Int32, Count(1)
            from #schema.first()
            group by Int32Text::Int32
            order by Int32Text::Int32";

        var aggregateTable = CreateAndRunVirtualMachine(aggregateQuery, rows).Run();
        var groupByTable = CreateAndRunVirtualMachine(groupByQuery, rows).Run();

        Assert.AreEqual(1, aggregateTable.Count);
        Assert.AreEqual("A", aggregateTable[0][0]);
        Assert.AreEqual(3.00m, aggregateTable[0][1]);

        Assert.AreEqual(2, groupByTable.Count);
        Assert.AreEqual(1, groupByTable[0][0]);
        Assert.AreEqual(2, Convert.ToInt32(groupByTable[0][1], CultureInfo.InvariantCulture));
        Assert.AreEqual(2, groupByTable[1][0]);
        Assert.AreEqual(1, Convert.ToInt32(groupByTable[1][1], CultureInfo.InvariantCulture));
    }

    private static CastEntity CreateFullCastEntity()
    {
        return new CastEntity
        {
            BooleanText = "true",
            ByteText = "255",
            SByteText = "-12",
            Int16Text = "-1234",
            UInt16Text = "1234",
            Int32Text = "-123456",
            UInt32Text = "123456",
            Int64Text = "-1234567890123",
            UInt64Text = "1234567890123",
            SingleText = "1.5",
            DoubleText = "2.25",
            DecimalText = "123.45",
            CharText = "Z",
            ObjectValue = 42,
            DateTimeText = "2024-06-15T13:45:30",
            DateTimeOffsetText = "2024-06-15T13:45:30+02:00",
            TimeSpanText = "01:02:03",
            GuidText = "12345678-1234-1234-1234-123456789012",
            PrefixText = "12",
            SuffixText = "3"
        };
    }

    private static void AssertColumnTypes(Type[] actual, Type[] expected)
    {
        Assert.AreEqual(expected.Length, actual.Length);

        for (var i = 0; i < expected.Length; i++)
            Assert.AreEqual(expected[i], actual[i], $"Column {i} type mismatch.");
    }

    public sealed class CastEntity
    {
        public string BooleanText { get; init; } = string.Empty;

        public string ByteText { get; init; } = string.Empty;

        public string SByteText { get; init; } = string.Empty;

        public string Int16Text { get; init; } = string.Empty;

        public string UInt16Text { get; init; } = string.Empty;

        public string Int32Text { get; init; } = string.Empty;

        public string UInt32Text { get; init; } = string.Empty;

        public string Int64Text { get; init; } = string.Empty;

        public string UInt64Text { get; init; } = string.Empty;

        public string SingleText { get; init; } = string.Empty;

        public string DoubleText { get; init; } = string.Empty;

        public string DecimalText { get; init; } = string.Empty;

        public string CharText { get; init; } = string.Empty;

        public object? ObjectValue { get; init; }

        public string DateTimeText { get; init; } = string.Empty;

        public string DateTimeOffsetText { get; init; } = string.Empty;

        public string TimeSpanText { get; init; } = string.Empty;

        public string GuidText { get; init; } = string.Empty;

        public string? NullText { get; init; }

        public string InvalidText { get; init; } = string.Empty;

        public string OverflowText { get; init; } = string.Empty;

        public string PrefixText { get; init; } = string.Empty;

        public string SuffixText { get; init; } = string.Empty;

        public string OtherText { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;
    }
}
