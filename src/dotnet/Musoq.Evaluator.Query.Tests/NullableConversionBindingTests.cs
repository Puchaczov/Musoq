using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class NullableConversionBindingTests
{
    [TestMethod]
    [DataRow("ToBin(42::int)", "101010")]
    [DataRow("ToBin(null::int)", null)]
    [DataRow("ToBin(1.25::decimal)", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("ToOcta(42::int)", "52")]
    [DataRow("ToOcta(null::int)", null)]
    [DataRow("ToOcta(1.25::decimal)", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("ToDec(42::int)", "42")]
    [DataRow("ToDec(null::int)", null)]
    [DataRow("ToDec(1.25::decimal)", "CONVERSION_NOT_SUPPORTED")]
    [DataRow("ToBin(null)", null)]
    public void RadixConversion_ShouldPreservePresentAndMissingValues(string expression, string? expected)
    {
        AssertConversion(expression, expected);
    }

    [TestMethod]
    [DataRow("true::bool", "01")]
    [DataRow("null::bool", null)]
    [DataRow("(-1)::short", "FFFF")]
    [DataRow("null::short", null)]
    [DataRow("65535::ushort", "FFFF")]
    [DataRow("null::ushort", null)]
    [DataRow("(-1)::int", "FFFFFFFF")]
    [DataRow("null::int", null)]
    [DataRow("4294967295L::uint", "FFFFFFFF")]
    [DataRow("null::uint", null)]
    [DataRow("(-1)::long", "FFFFFFFFFFFFFFFF")]
    [DataRow("null::long", null)]
    [DataRow("'18446744073709551615'::ulong", "FFFFFFFFFFFFFFFF")]
    [DataRow("null::ulong", null)]
    [DataRow("1ui", "01000000")]
    public void ToBytes_ShouldUseTheRequestedWidth(string value, string? expected)
    {
        AssertConversion($"ToHex(ToBytes({value}))", expected);
    }

    [TestMethod]
    [DataRow("'AéZ', Length('é'), 1", "C3A9")]
    [DataRow("'ABéZ', 1::int, 2::int", "C3A9")]
    [DataRow("'AéZ', 0::int, 2::int", "")]
    [DataRow("'AéZ', null::int, 1", null)]
    [DataRow("'AéZ', 1, null::int", null)]
    [DataRow("null, 1::int, 1", null)]
    public void GetBytes_ShouldEncodeTheRequestedSlice(string arguments, string? expected)
    {
        AssertConversion($"ToHex(GetBytes({arguments}))", expected);
    }

    [TestMethod]
    [DataRow("ToBytes(5ub)", DiagnosticCode.MQ3089_AmbiguousCallableOverload)]
    [DataRow("ToBytes(null)", DiagnosticCode.MQ3089_AmbiguousCallableOverload)]
    [DataRow("ToDec(null)", DiagnosticCode.MQ3089_AmbiguousCallableOverload)]
    [DataRow("ToOcta(null)", DiagnosticCode.MQ3089_AmbiguousCallableOverload)]
    [DataRow("Repeat('x', null::int)", DiagnosticCode.MQ3088_NoMatchingCallableOverload)]
    public void CallableWithUnsupportedArguments_ShouldReportDiagnostic(string expression, DiagnosticCode expected)
    {
        var failure = Assert.Throws<MusoqQueryException>(() => Compile(expression));
        Assert.AreEqual(expected, failure.PrimaryEnvelope.Code);
    }

    private static void AssertConversion(string expression, string? expected)
    {
        using var query = Compile(expression);
        using var table = query.Run();
        Assert.AreEqual(expected, table.Single().GetValue<string>(0));
    }

    private static CompiledQuery Compile(string expression) =>
        InstanceCreator.CompileForExecution(
            $"select {expression} from values {{ (Seed: 0) }} v",
            nameof(NullableConversionBindingTests),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
}
