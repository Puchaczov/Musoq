using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    [TestMethod]
    public void Query_NegativeDynamicByteArraySize_ShouldThrowInvalidSize()
    {
        const string query = @"
            binary Packet {
                Length: sbyte,
                Payload: byte[Length]
            };
            select p.Payload from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        AssertInvalidSize(query);
    }

    [TestMethod]
    public void Query_NegativeDynamicStringSize_ShouldThrowInvalidSize()
    {
        const string query = @"
            binary Packet {
                Length: sbyte,
                Payload: string[Length] utf8
            };
            select p.Payload from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        AssertInvalidSize(query);
    }

    [TestMethod]
    public void Query_NegativeDynamicRawSubstreamSize_ShouldThrowInvalidSize()
    {
        const string query = @"
            binary Packet {
                Length: sbyte,
                Payload: substream[Length] raw
            };
            select p.Payload from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        AssertInvalidSize(query);
    }

    [TestMethod]
    public void Query_MixedBinaryTextApplyAliases_ShouldCompileAndReturnProjectedFields()
    {
        const string query = @"
            binary MessageFrame {
                MsgType: byte,
                PayloadLen: byte,
                Payload: byte[PayloadLen]
            };
            text CommandPayload {
                Command: until ';',
                Argument: rest
            };
            binary TelemetryPayload {
                SensorId: byte,
                Reading: byte
            };
            select
                frame.MsgType,
                command.Command,
                command.Argument,
                telemetry.SensorId,
                telemetry.Reading
            from #test.files() f
            cross apply Interpret<MessageFrame>(f.Content) frame
            outer apply TryParse<CommandPayload>(ToText(frame.Payload, 'utf8')) command
            outer apply TryInterpret<TelemetryPayload>(frame.Payload) telemetry";

        var payload = "PING;42"u8.ToArray();
        var data = new byte[2 + payload.Length];
        data[0] = 0x01;
        data[1] = (byte)payload.Length;
        payload.CopyTo(data.AsSpan(2));

        var table = RunBinaryQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual("PING", table[0][1]);
        Assert.AreEqual("42", table[0][2]);
        Assert.AreEqual((byte)'P', table[0][3]);
        Assert.AreEqual((byte)'I', table[0][4]);
    }

    private static void AssertInvalidSize(string query)
    {
        var exception = Assert.Throws<ParseException>(() => RunBinaryQuery(query, [0xFF]));

        Assert.AreEqual("ISE0007", exception.FormattedErrorCode);
    }

    private static Table RunBinaryQuery(string query, byte[] content)
    {
        var entities = new[] { new BinaryEntity { Name = "packet.bin", Content = content } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        return TableMaterializationTestHelper.Materialize(vm.Run(CancellationToken.None));
    }
}
