using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticRework078TypeAndCastContractTests : GenericEntityTestBase
{
    [TestMethod]
    public void TypeMatrix_ShouldPreservePrimitiveDateTimeComplexAndCollectionContracts()
    {
        var table = CreateAndRunVirtualMachine(
            """
            select
                42 as InferredInt,
                42d as InferredDecimal,
                .5 as Fraction,
                0xFF as HexValue,
                true as BoolValue,
                'text' as TextValue,
                e.IntValue as SourceInt,
                e.NullableIntValue as NullableInt,
                e.DateValue as DateValue,
                e.OffsetValue as OffsetValue,
                e.DurationValue as DurationValue,
                e.GuidValue as GuidValue,
                e.Nested.Name as NestedName,
                e.Values[1] as IndexedValue,
                e.Nested.Lookup['A'] as DictionaryValue
            from #schema.first() e
            """,
            [CreateEntity()]).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("InferredInt", typeof(int)),
            ("InferredDecimal", typeof(decimal)),
            ("Fraction", typeof(decimal)),
            ("HexValue", typeof(long)),
            ("BoolValue", typeof(bool)),
            ("TextValue", typeof(string)),
            ("SourceInt", typeof(int)),
            ("NullableInt", typeof(int?)),
            ("DateValue", typeof(DateTime)),
            ("OffsetValue", typeof(DateTimeOffset)),
            ("DurationValue", typeof(TimeSpan)),
            ("GuidValue", typeof(Guid)),
            ("NestedName", typeof(string)),
            ("IndexedValue", typeof(int)),
            ("DictionaryValue", typeof(string)));

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[]
            {
                42,
                42m,
                .5m,
                255L,
                true,
                "text",
                7,
                null,
                new DateTime(2024, 6, 15, 13, 45, 30),
                new DateTimeOffset(2024, 6, 15, 13, 45, 30, TimeSpan.FromHours(2)),
                new TimeSpan(1, 2, 3),
                Guid.Parse("12345678-1234-1234-1234-123456789012"),
                "nested",
                20,
                "B"
            });
    }

    [TestMethod]
    public void StrictCastMatrix_ShouldCoerceInvariantValuesAndRetainNullableTargetTypes()
    {
        var table = CreateAndRunVirtualMachine(
            """
            select
                e.IntValue::Int64 as Int64Value,
                e.DecimalValue::Double as DoubleValue,
                e.TextValue::int::string as ChainedValue,
                (e.IntValue + 1)::Decimal as ArithmeticValue,
                e.BoolText::Boolean as BoolValue,
                e.CharText::Char as CharValue,
                e.DateText::DateTime as DateValue,
                e.OffsetText::DateTimeOffset as OffsetValue,
                e.DurationText::TimeSpan as DurationValue,
                e.GuidText::Guid as GuidValue,
                e.DecimalValue::String as InvariantDecimalText,
                e.NullableIntValue::Int32 as NullValue
            from #schema.first() e
            """,
            [CreateEntity()]).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Int64Value", typeof(long?)),
            ("DoubleValue", typeof(double?)),
            ("ChainedValue", typeof(string)),
            ("ArithmeticValue", typeof(decimal?)),
            ("BoolValue", typeof(bool?)),
            ("CharValue", typeof(char?)),
            ("DateValue", typeof(DateTime?)),
            ("OffsetValue", typeof(DateTimeOffset?)),
            ("DurationValue", typeof(TimeSpan?)),
            ("GuidValue", typeof(Guid?)),
            ("InvariantDecimalText", typeof(string)),
            ("NullValue", typeof(int?)));

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[]
            {
                7L,
                12.5d,
                "7",
                8m,
                true,
                'Z',
                new DateTime(2024, 6, 15, 13, 45, 30),
                new DateTimeOffset(2024, 6, 15, 13, 45, 30, TimeSpan.FromHours(2)),
                new TimeSpan(1, 2, 3),
                Guid.Parse("12345678-1234-1234-1234-123456789012"),
                "12.5",
                null
            });
    }

    [TestMethod]
    public void NullAndOuterJoinCastMatrix_ShouldReturnNullWithoutChangingStringTargetShape()
    {
        var table = CreateAndRunVirtualMachine(
            """
            select a.Id as LeftId, b.Id as RightId, b.Id::String as RightText,
                   null::DateTime as NullDate, null::Guid as NullGuid
            from #schema.first() a
            left join #schema.second() b on a.Id = b.Id
            """,
            [new TypeContractEntity { Id = 1 }],
            [new TypeContractEntity { Id = 2 }]).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("LeftId", typeof(int)),
            ("RightId", typeof(int?)),
            ("RightText", typeof(string)),
            ("NullDate", typeof(DateTime?)),
            ("NullGuid", typeof(Guid?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { 1, null, null, null, null });
    }

    [TestMethod]
    public void DateTimeStringComparison_ShouldUseDocumentedAutomaticParsing()
    {
        var table = CreateAndRunVirtualMachine(
            "select e.Id from #schema.first() e where e.DateValue = '2024-06-15T13:45:30'",
            [CreateEntity()]).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("e.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7]);
    }

    [TestMethod]
    [DataRow("'not-a-number'::Int32", "Int32")]
    [DataRow("256::Byte", "Byte")]
    [DataRow("'not-a-guid'::Guid", "Guid")]
    [DataRow("1::TimeSpan", "TimeSpan")]
    public void InvalidConstantCast_ShouldFailDuringBindingWithExactLiteralLocation(
        string castExpression,
        string targetType)
    {
        var query = $"select {castExpression} from #schema.first()";
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            query,
            [CreateEntity()]));

        AssertSingleError(exception, DiagnosticCode.MQ3091_InvalidConstantCast, DiagnosticPhase.Bind);
        AssertHasGuidance(exception);
        AssertMessageContains(exception, targetType);

        var envelope = exception.PrimaryEnvelope;
        var expressionOffset = query.IndexOf(castExpression, StringComparison.Ordinal);
        Assert.AreEqual(expressionOffset, envelope.Offset);
        var literal = castExpression[..castExpression.IndexOf("::", StringComparison.Ordinal)];
        Assert.AreEqual(literal.Length, envelope.Length);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.IsNotNull(envelope.Snippet);
    }

    [TestMethod]
    [DataRow("INTEGER")]
    [DataRow("object")]
    [DataRow("nint")]
    [DataRow("nuint")]
    [DataRow("Int32?")]
    public void UnsupportedCastTargetMatrix_ShouldFailDuringBindingWithExactCastLocation(string targetType)
    {
        var castExpression = $"e.TextValue::{targetType}";
        var query = $"select {castExpression} from #schema.first() e";
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            query,
            [CreateEntity()]));

        AssertSingleError(
            exception,
            DiagnosticCode.MQ3090_UnsupportedCastTarget,
            DiagnosticPhase.Bind,
            "CLR type names and C# aliases only");
        AssertHasGuidance(exception);
        AssertMessageContains(exception, targetType);

        var envelope = exception.PrimaryEnvelope;
        var diagnosticExpression = castExpression["e.".Length..];
        var expressionOffset = query.IndexOf(diagnosticExpression, StringComparison.Ordinal);
        Assert.AreEqual(expressionOffset, envelope.Offset);
        Assert.AreEqual(diagnosticExpression.Length, envelope.Length);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.IsNotNull(envelope.Snippet);
    }

    [TestMethod]
    [DataRow("InvalidText", "Int32")]
    [DataRow("InvalidText", "DateTime")]
    [DataRow("InvalidText", "DateTimeOffset")]
    [DataRow("InvalidText", "TimeSpan")]
    [DataRow("InvalidText", "Guid")]
    [DataRow("ObjectValue", "Guid")]
    public void InvalidRuntimeCastMatrix_ShouldExposeSanitizedInternalFailure(string sourceProperty, string targetType)
    {
        var query = $"select e.{sourceProperty}::{targetType} from #schema.first() e";
        var exception = Assert.Throws<QueryExecutionException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(
                query,
                [CreateEntity()]);
            _ = vm.Run().Count;
        });

        AssertRuntimeError(exception, DiagnosticCode.MQ9002_InternalExecutionError);
        Assert.AreEqual(DiagnosticSourceKind.Internal, exception.Envelope!.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(exception.Envelope.CorrelationId));
        Assert.IsNull(exception.Envelope.Offset);
        Assert.IsNull(exception.Envelope.Length);
        StringAssert.Contains(exception.Envelope.Message, "Reference");
    }

    private static TypeContractEntity CreateEntity()
    {
        return new TypeContractEntity
        {
            Id = 7,
            IntValue = 7,
            NullableIntValue = null,
            DecimalValue = 12.5m,
            DateValue = new DateTime(2024, 6, 15, 13, 45, 30),
            OffsetValue = new DateTimeOffset(2024, 6, 15, 13, 45, 30, TimeSpan.FromHours(2)),
            DurationValue = new TimeSpan(1, 2, 3),
            GuidValue = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            TextValue = "7",
            BoolText = "true",
            CharText = "Z",
            DateText = "2024-06-15T13:45:30",
            OffsetText = "2024-06-15T13:45:30+02:00",
            DurationText = "01:02:03",
            GuidText = "12345678-1234-1234-1234-123456789012",
            InvalidText = "not-a-value",
            ObjectValue = 42,
            Nested = new NestedContractEntity
            {
                Name = "nested",
                Lookup = new Dictionary<string, string> { ["A"] = "B" }
            },
            Values = [10, 20, 30]
        };
    }

    public sealed class TypeContractEntity
    {
        public int Id { get; init; }

        public int IntValue { get; init; }

        public int? NullableIntValue { get; init; }

        public decimal DecimalValue { get; init; }

        public DateTime DateValue { get; init; }

        public DateTimeOffset OffsetValue { get; init; }

        public TimeSpan DurationValue { get; init; }

        public Guid GuidValue { get; init; }

        public string TextValue { get; init; } = string.Empty;

        public string BoolText { get; init; } = string.Empty;

        public string CharText { get; init; } = string.Empty;

        public string DateText { get; init; } = string.Empty;

        public string OffsetText { get; init; } = string.Empty;

        public string DurationText { get; init; } = string.Empty;

        public string GuidText { get; init; } = string.Empty;

        public string InvalidText { get; init; } = string.Empty;

        public object? ObjectValue { get; init; }

        public NestedContractEntity Nested { get; init; } = new();

        public int[] Values { get; init; } = [];
    }

    public sealed class NestedContractEntity
    {
        public string Name { get; init; } = string.Empty;

        public Dictionary<string, string> Lookup { get; init; } = new();
    }
}
