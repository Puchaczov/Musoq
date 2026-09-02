using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    [TestMethod]
    public void Query_PartialInterpret_CrossApplyValidData_ShouldReturnPartialResultShape()
    {
        var query = @"
            binary DebugPacket {
                Magic: int le,
                Version: byte
            };
            select
                p.ParsedFields,
                p.ErrorField,
                p.ErrorMessage,
                p.BytesConsumed
            from #test.files() f
            cross apply PartialInterpret<DebugPacket>(f.Content) p";

        var data = new byte[5];
        BitConverter.GetBytes(0x12345678).CopyTo(data, 0);
        data[4] = 7;
        var entities = new[] { new BinaryEntity { Name = "valid.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.ParsedFields", typeof(Dictionary<string, object?>)),
            ("p.ErrorField", typeof(string)),
            ("p.ErrorMessage", typeof(string)),
            ("p.BytesConsumed", typeof(int)));
        Assert.AreEqual(1, table.Count);
        var parsedFields = (Dictionary<string, object?>)table[0][0]!;
        Assert.AreEqual(0x12345678, parsedFields["Magic"]);
        Assert.AreEqual((byte)7, parsedFields["Version"]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
        Assert.AreEqual(5, table[0][3]);
    }

    [TestMethod]
    public void Query_PartialInterpret_CrossApplyMalformedData_ShouldReturnErrorMetadata()
    {
        var query = @"
            binary DebugPacket {
                Magic: int le,
                Version: byte
            };
            select
                p.ParsedFields,
                p.ErrorField,
                p.ErrorMessage,
                p.BytesConsumed
            from #test.files() f
            cross apply PartialInterpret<DebugPacket>(f.Content) p";

        var data = BitConverter.GetBytes(0x12345678);
        var entities = new[] { new BinaryEntity { Name = "truncated.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.ParsedFields", typeof(Dictionary<string, object?>)),
            ("p.ErrorField", typeof(string)),
            ("p.ErrorMessage", typeof(string)),
            ("p.BytesConsumed", typeof(int)));
        Assert.AreEqual(1, table.Count);
        var parsedFields = (Dictionary<string, object?>)table[0][0]!;
        Assert.AreEqual(0x12345678, parsedFields["Magic"]);
        Assert.IsFalse(parsedFields.ContainsKey("Version"));
        Assert.AreEqual("Version", table[0][1]);
        StringAssert.Contains((string)table[0][2]!, "ISE0001");
        Assert.AreEqual(4, table[0][3]);
    }

    [TestMethod]
    public void Query_PartialInterpret_OutsideApply_ShouldReportInterpretFunctionOutsideApply()
    {
        var query = @"
            binary DebugPacket {
                Magic: int le
            };
            select PartialInterpret<DebugPacket>(f.Content)
            from #test.files() f";

        var entities = new[] { new BinaryEntity { Name = "valid.bin", Content = BitConverter.GetBytes(1) } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3033_InterpretFunctionOutsideApply, DiagnosticPhase.Bind);
    }
}
