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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("BoolValue", typeof(bool?)),
            ("ByteValue", typeof(byte?)),
            ("SByteValue", typeof(sbyte?)),
            ("Int16Value", typeof(short?)),
            ("UInt16Value", typeof(ushort?)),
            ("Int32Value", typeof(int?)),
            ("UInt32Value", typeof(uint?)),
            ("Int64Value", typeof(long?)),
            ("UInt64Value", typeof(ulong?)),
            ("SingleValue", typeof(float?)),
            ("DoubleValue", typeof(double?)),
            ("DecimalValue", typeof(decimal?)),
            ("CharValue", typeof(char?)),
            ("StringValue", typeof(string)),
            ("DateTimeValue", typeof(DateTime?)),
            ("DateTimeOffsetValue", typeof(DateTimeOffset?)),
            ("TimeSpanValue", typeof(TimeSpan?)),
            ("GuidValue", typeof(Guid?)));

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [
                true,
                (byte)255,
                (sbyte)-12,
                (short)-1234,
                (ushort)1234,
                -123456,
                (uint)123456,
                -1234567890123L,
                1234567890123UL,
                1.5f,
                2.25d,
                123.45m,
                'Z',
                "42",
                DateTime.Parse("2024-06-15T13:45:30", CultureInfo.InvariantCulture),
                DateTimeOffset.Parse("2024-06-15T13:45:30+02:00", CultureInfo.InvariantCulture),
                TimeSpan.Parse("01:02:03", CultureInfo.InvariantCulture),
                Guid.Parse("12345678-1234-1234-1234-123456789012")
            ]);
    }

    [TestMethod]
    public void PostfixCast_AllSupportedCSharpAliases_ShouldMapToClrTargets()
    {
        var table = CreateAndRunVirtualMachine(
            @"
                select
                    BooleanText::bool as BoolValue,
                    ByteText::byte as ByteValue,
                    SByteText::sbyte as SByteValue,
                    Int16Text::short as Int16Value,
                    UInt16Text::ushort as UInt16Value,
                    Int32Text::int as Int32Value,
                    UInt32Text::uint as UInt32Value,
                    Int64Text::long as Int64Value,
                    UInt64Text::ulong as UInt64Value,
                    SingleText::float as SingleValue,
                    DoubleText::double as DoubleValue,
                    DecimalText::decimal as DecimalValue,
                    CharText::char as CharValue,
                    ObjectValue::string as StringValue
                from #schema.first()",
            [CreateFullCastEntity()]).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("BoolValue", typeof(bool?)),
            ("ByteValue", typeof(byte?)),
            ("SByteValue", typeof(sbyte?)),
            ("Int16Value", typeof(short?)),
            ("UInt16Value", typeof(ushort?)),
            ("Int32Value", typeof(int?)),
            ("UInt32Value", typeof(uint?)),
            ("Int64Value", typeof(long?)),
            ("UInt64Value", typeof(ulong?)),
            ("SingleValue", typeof(float?)),
            ("DoubleValue", typeof(double?)),
            ("DecimalValue", typeof(decimal?)),
            ("CharValue", typeof(char?)),
            ("StringValue", typeof(string)));

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [
                true,
                (byte)255,
                (sbyte)-12,
                (short)-1234,
                (ushort)1234,
                -123456,
                (uint)123456,
                -1234567890123L,
                1234567890123UL,
                1.5f,
                2.25d,
                123.45m,
                'Z',
                "42"
            ]);
    }

    [TestMethod]
    public void PostfixCast_CSharpAliases_ShouldMatchCaseInsensitively()
    {
        var table = CreateAndRunVirtualMachine(
            "select Int32Text::INT as IntValue, SingleText::Float as FloatValue, ObjectValue::STRING as StringValue from #schema.first()",
            [CreateFullCastEntity()]).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("IntValue", typeof(int?)),
            ("FloatValue", typeof(float?)),
            ("StringValue", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [-123456, 1.5f, "42"]);
    }

    [TestMethod]
    public void PostfixCast_StringAlias_ShouldUseInvariantToStringAndPreserveNull()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var table = CreateAndRunVirtualMachine(
                "select ObjectValue::string as TextValue, NullText::string as NullValue from #schema.first()",
                [new CastEntity { ObjectValue = 1234.5m, NullText = null }]).Run();

            TableMaterializationTestHelper.AssertColumns(
                table,
                ("TextValue", typeof(string)),
                ("NullValue", typeof(string)));
            TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { "1234.5", null });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [TestMethod]
    public void PostfixCast_CSharpAliases_ShouldWorkInFiltersAndNestedCasts()
    {
        var rows = new[]
        {
            new CastEntity { Int32Text = "1" },
            new CastEntity { Int32Text = "2" }
        };

        var table = CreateAndRunVirtualMachine(
            "select Int32Text::int::string as Value from #schema.first() where Int32Text::int > 1",
            rows).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["2"]);
    }

    [TestMethod]
    public void PostfixCast_TargetNameMatching_ShouldBeCaseInsensitive()
    {
        var table = CreateAndRunVirtualMachine(
            "select Int32Text::int32 as Value from #schema.first()",
            [CreateFullCastEntity()]).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [-123456]);
    }

    [TestMethod]
    public void PostfixCast_NullInput_ShouldReturnNull()
    {
        var table = CreateAndRunVirtualMachine(
            "select NullText::Int32 as IntValue, NullText::Guid as GuidValue, NullText::String as TextValue from #schema.first()",
            [new CastEntity { NullText = null }]).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("IntValue", typeof(int?)),
            ("GuidValue", typeof(Guid?)),
            ("TextValue", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null, null, null });
    }

    [TestMethod]
    public void PostfixCast_InvalidText_ShouldThrow()
    {
        var vm = CreateAndRunVirtualMachine(
            "select InvalidText::Int32 from #schema.first()",
            [new CastEntity { InvalidText = "not_a_number" }]);

        Assert.Throws<FormatException>(() => _ = vm.Run().Count);
    }

    [TestMethod]
    public void PostfixCast_RepeatedInvalidText_ShouldThrowOnFirstInvalidCast()
    {
        var vm = CreateAndRunVirtualMachine(
            "select InvalidText::Int32, InvalidText::Int32 from #schema.first()",
            [new CastEntity { InvalidText = "not_a_number" }]);

        Assert.Throws<FormatException>(() => _ = vm.Run().Count);
    }

    [TestMethod]
    public void PostfixCast_Overflow_ShouldThrow()
    {
        var vm = CreateAndRunVirtualMachine(
            "select OverflowText::Byte from #schema.first()",
            [new CastEntity { OverflowText = "256" }]);

        Assert.Throws<OverflowException>(() => _ = vm.Run().Count);
    }

    [TestMethod]
    public void PostfixCast_UnsupportedRuntimeConversion_ShouldThrow()
    {
        var vm = CreateAndRunVirtualMachine(
            "select ObjectValue::Guid from #schema.first()",
            [new CastEntity { ObjectValue = 42 }]);

        Assert.Throws<InvalidCastException>(() => _ = vm.Run().Count);
    }

    [TestMethod]
    public void PostfixCast_RepeatedCastInWhereAndSelect_ShouldPreserveFiltering()
    {
        var rows = new[]
        {
            new CastEntity { Int32Text = "0" },
            new CastEntity { Int32Text = "2" },
            new CastEntity { Int32Text = "1" }
        };

        var table = CreateAndRunVirtualMachine(
            "select Int32Text::Int32 as Value from #schema.first() where Int32Text::Int32 > 1",
            rows).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2]);
    }

    [TestMethod]
    public void PostfixCast_UnknownTarget_ShouldReportUnsupportedSyntax()
    {
        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            "select Int32Text::INTEGER from #schema.first()",
            [CreateFullCastEntity()]));

        AssertSingleError(ex, DiagnosticCode.MQ2030_UnsupportedSyntax, DiagnosticPhase.Parse, "CLR type names and C# aliases only");
    }

    [TestMethod]
    public void PostfixCast_NestedAndParenthesizedCasts_ShouldExecuteLeftToRight()
    {
        var table = CreateAndRunVirtualMachine(
            "select Int32Text::Int32::String as TextValue, (PrefixText + SuffixText)::Int32 as NumericValue from #schema.first()",
            [CreateFullCastEntity()]).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("TextValue", typeof(string)),
            ("NumericValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["-123456", 123]);
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
            select case when BooleanText::Boolean then Int32Text::Int32 else OtherText::Int32 end as Value
            from #schema.first()
            where Int32Text::Int32 > 1
            order by Int32Text::Int32 desc";

        var table = CreateAndRunVirtualMachine(query, rows).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [200], [2]);
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
            select Category, Sum(DecimalText::Decimal) as Total
            from #schema.first()
            group by Category
            having Category::String = 'A'";
        var groupByQuery = @"
            select Int32Text::Int32 as Value, Count(1) as Amount
            from #schema.first()
            group by Int32Text::Int32
            order by Int32Text::Int32";

        var aggregateTable = CreateAndRunVirtualMachine(aggregateQuery, rows).Run();
        var groupByTable = CreateAndRunVirtualMachine(groupByQuery, rows).Run();

        TableMaterializationTestHelper.AssertColumns(
            aggregateTable,
            ("Category", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(aggregateTable, ["A", 3.00m]);

        TableMaterializationTestHelper.AssertColumns(
            groupByTable,
            ("Value", typeof(int?)),
            ("Amount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(groupByTable, [1, 2L], [2, 1L]);
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
