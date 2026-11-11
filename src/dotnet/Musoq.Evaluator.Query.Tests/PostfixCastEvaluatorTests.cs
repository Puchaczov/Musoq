using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class PostfixCastEvaluatorTests : GenericEntityTestBase
{
    [TestMethod]
    [TestProperty("CandidateId", "P1-100")]
    public void PostfixCast_InlineStringLiteral_ShouldReturnNullableInt32Metadata()
    {
        const string query = "select '42'::Int32 as Value from values { (N: 0) } v";

        using var compiled = CreateAndRunVirtualMachine(query, new[] { CreateFullCastEntity() });
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [42]);
    }

    [TestMethod]
    public void PostfixCast_AllSupportedClrTargets_ShouldCastStringsThroughLibrary()
    {
        var source = new[] { CreateFullCastEntity() };
        var query = (
            "\n"
            + "            select\n"
            + "                BooleanText::Boolean as BoolValue,\n"
            + "                ByteText::Byte as ByteValue,\n"
            + "                SByteText::SByte as SByteValue,\n"
            + "                Int16Text::Int16 as Int16Value,\n"
            + "                UInt16Text::UInt16 as UInt16Value,\n"
            + "                Int32Text::Int32 as Int32Value,\n"
            + "                UInt32Text::UInt32 as UInt32Value,\n"
            + "                Int64Text::Int64 as Int64Value,\n"
            + "                UInt64Text::UInt64 as UInt64Value,\n"
            + "                SingleText::Single as SingleValue,\n"
            + "                DoubleText::Double as DoubleValue,\n"
            + "                DecimalText::Decimal as DecimalValue,\n"
            + "                CharText::Char as CharValue,\n"
            + "                NumericValue::String as StringValue,\n"
            + "                DateTimeText::DateTime as DateTimeValue,\n"
            + "                DateTimeOffsetText::DateTimeOffset as DateTimeOffsetValue,\n"
            + "                TimeSpanText::TimeSpan as TimeSpanValue,\n"
            + "                GuidText::Guid as GuidValue\n"
            + "            from #schema.first()"
        );

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
            ("GuidValue", typeof(Guid?))
        );

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
                Guid.Parse("12345678-1234-1234-1234-123456789012"),
            ]
        );
    }

    [TestMethod]
    public void PostfixCast_AllSupportedCSharpAliases_ShouldMapToClrTargets()
    {
        var table = CreateAndRunVirtualMachine(
                (
                    "\n"
                    + "                select\n"
                    + "                    BooleanText::bool as BoolValue,\n"
                    + "                    ByteText::byte as ByteValue,\n"
                    + "                    SByteText::sbyte as SByteValue,\n"
                    + "                    Int16Text::short as Int16Value,\n"
                    + "                    UInt16Text::ushort as UInt16Value,\n"
                    + "                    Int32Text::int as Int32Value,\n"
                    + "                    UInt32Text::uint as UInt32Value,\n"
                    + "                    Int64Text::long as Int64Value,\n"
                    + "                    UInt64Text::ulong as UInt64Value,\n"
                    + "                    SingleText::float as SingleValue,\n"
                    + "                    DoubleText::double as DoubleValue,\n"
                    + "                    DecimalText::decimal as DecimalValue,\n"
                    + "                    CharText::char as CharValue,\n"
                    + "                    NumericValue::string as StringValue\n"
                    + "                from #schema.first()"
                ),
                [CreateFullCastEntity()]
            )
            .Run();

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
            ("StringValue", typeof(string))
        );

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
            ]
        );
    }

    [TestMethod]
    public void PostfixCast_CSharpAliases_ShouldMatchCaseInsensitively()
    {
        var table = CreateAndRunVirtualMachine(
                "select Int32Text::INT as IntValue, SingleText::Float as FloatValue, NumericValue::STRING as StringValue from #schema.first()",
                [CreateFullCastEntity()]
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("IntValue", typeof(int?)),
            ("FloatValue", typeof(float?)),
            ("StringValue", typeof(string))
        );
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
                    "select NumericValue::string as TextValue, NullText::string as NullValue from #schema.first()",
                    [new CastEntity { NumericValue = 1234.5m, NullText = null }]
                )
                .Run();

            TableMaterializationTestHelper.AssertColumns(
                table,
                ("TextValue", typeof(string)),
                ("NullValue", typeof(string))
            );
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
            new CastEntity { Int32Text = "2" },
        };

        var table = CreateAndRunVirtualMachine(
                "select Int32Text::int::string as Value from #schema.first() where Int32Text::int > 1",
                rows
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["2"]);
    }

    [TestMethod]
    public void PostfixCast_TargetNameMatching_ShouldBeCaseInsensitive()
    {
        var table = CreateAndRunVirtualMachine(
                "select Int32Text::int32 as Value from #schema.first()",
                [CreateFullCastEntity()]
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [-123456]);
    }

    [TestMethod]
    public void PostfixCast_NullInput_ShouldReturnNull()
    {
        var table = CreateAndRunVirtualMachine(
                "select NullText::Int32 as IntValue, NullText::Guid as GuidValue, NullText::String as TextValue from #schema.first()",
                [new CastEntity { NullText = null }]
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("IntValue", typeof(int?)),
            ("GuidValue", typeof(Guid?)),
            ("TextValue", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null, null, null });
    }

    [TestMethod]
    public void PostfixCast_InvalidText_ShouldThrow()
    {
        var vm = CreateAndRunVirtualMachine(
            "select InvalidText::Int32 from #schema.first()",
            [new CastEntity { InvalidText = "not_a_number" }]
        );

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run().Count);
        AssertRuntimeError(exception, DiagnosticCode.MQ9002_InternalExecutionError);
    }

    [TestMethod]
    public void PostfixCast_RepeatedInvalidText_ShouldThrowOnFirstInvalidCast()
    {
        var vm = CreateAndRunVirtualMachine(
            "select InvalidText::Int32, InvalidText::Int32 from #schema.first()",
            [new CastEntity { InvalidText = "not_a_number" }]
        );

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run().Count);
        AssertRuntimeError(exception, DiagnosticCode.MQ9002_InternalExecutionError);
    }

    [TestMethod]
    public void PostfixCast_Overflow_ShouldThrow()
    {
        var vm = CreateAndRunVirtualMachine(
            "select OverflowText::Byte from #schema.first()",
            [new CastEntity { OverflowText = "256" }]
        );

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run().Count);
        AssertRuntimeError(exception, DiagnosticCode.MQ7018_NumericOverflow);
    }

    [TestMethod]
    public void PostfixCast_UnsupportedRuntimeConversion_ShouldThrow()
    {
        var vm = CreateAndRunVirtualMachine(
            "select NumericValue::Guid from #schema.first()",
            [new CastEntity { NumericValue = 42 }]
        );

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run().Count);
        AssertRuntimeError(exception, DiagnosticCode.MQ9002_InternalExecutionError);
    }

    [TestMethod]
    public void PostfixCast_RepeatedCastInWhereAndSelect_ShouldPreserveFiltering()
    {
        var rows = new[]
        {
            new CastEntity { Int32Text = "0" },
            new CastEntity { Int32Text = "2" },
            new CastEntity { Int32Text = "1" },
        };

        var table = CreateAndRunVirtualMachine(
                "select Int32Text::Int32 as Value from #schema.first() where Int32Text::Int32 > 1",
                rows
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2]);
    }

    [TestMethod]
    public void PostfixCast_UnknownTarget_ShouldReportUnsupportedCastTarget()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select Int32Text::INTEGER from #schema.first()", [CreateFullCastEntity()])
        );

        AssertSingleError(
            ex,
            DiagnosticCode.MQ3090_UnsupportedCastTarget,
            DiagnosticPhase.Bind,
            "CLR type names and C# aliases only"
        );
    }

    [TestMethod]
    public void PostfixCast_NestedAndParenthesizedCasts_ShouldExecuteLeftToRight()
    {
        var table = CreateAndRunVirtualMachine(
                "select Int32Text::Int32::String as TextValue, (PrefixText + SuffixText)::Int32 as NumericValue from #schema.first()",
                [CreateFullCastEntity()]
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("TextValue", typeof(string)),
            ("NumericValue", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["-123456", 123]);
    }

    [TestMethod]
    public void PostfixCast_CastsInWhereOrderByAndCase_ShouldExecute()
    {
        var rows = new[]
        {
            new CastEntity
            {
                Int32Text = "2",
                BooleanText = "true",
                OtherText = "100",
            },
            new CastEntity
            {
                Int32Text = "10",
                BooleanText = "false",
                OtherText = "200",
            },
            new CastEntity
            {
                Int32Text = "1",
                BooleanText = "true",
                OtherText = "300",
            },
        };
        var query = (
            "\n"
            + "            select case when BooleanText::Boolean then Int32Text::Int32 else OtherText::Int32 end as Value\n"
            + "            from #schema.first()\n"
            + "            where Int32Text::Int32 > 1\n"
            + "            order by Int32Text::Int32 desc"
        );

        var table = CreateAndRunVirtualMachine(query, rows).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [200], [2]);
    }

    [TestMethod]
    public void PostfixCast_CastsInGroupByHavingAndAggregateInputs_ShouldExecute()
    {
        var rows = new[]
        {
            new CastEntity
            {
                Category = "A",
                Int32Text = "1",
                DecimalText = "1.25",
            },
            new CastEntity
            {
                Category = "A",
                Int32Text = "1",
                DecimalText = "1.75",
            },
            new CastEntity
            {
                Category = "B",
                Int32Text = "2",
                DecimalText = "10.00",
            },
        };
        var aggregateQuery = (
            "\n"
            + "            select Category, Sum(DecimalText::Decimal) as Total\n"
            + "            from #schema.first()\n"
            + "            group by Category\n"
            + "            having Category::String = 'A'"
        );
        var groupByQuery = (
            "\n"
            + "            select Int32Text::Int32 as Value, Count(1) as Amount\n"
            + "            from #schema.first()\n"
            + "            group by Int32Text::Int32\n"
            + "            order by Int32Text::Int32"
        );

        var aggregateTable = CreateAndRunVirtualMachine(aggregateQuery, rows).Run();
        var groupByTable = CreateAndRunVirtualMachine(groupByQuery, rows).Run();

        TableMaterializationTestHelper.AssertColumns(
            aggregateTable,
            ("Category", typeof(string)),
            ("Total", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(aggregateTable, ["A", 3.00m]);

        TableMaterializationTestHelper.AssertColumns(groupByTable, ("Value", typeof(int?)), ("Amount", typeof(long)));
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
            NumericValue = 42,
            DateTimeText = "2024-06-15T13:45:30",
            DateTimeOffsetText = "2024-06-15T13:45:30+02:00",
            TimeSpanText = "01:02:03",
            GuidText = "12345678-1234-1234-1234-123456789012",
            PrefixText = "12",
            SuffixText = "3",
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

        public decimal? NumericValue { get; init; }

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
