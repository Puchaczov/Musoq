using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryOrTextualSwitchTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void Query_SwitchPayload_MatchingCase_ShouldSelectBranchAlias()
    {
        var query = @"
            binary LoginPayload {
                UserId: int le
            };
            binary Packet {
                Type: byte,
                Length: byte,
                Payload: switch Type {
                    1 => Login: LoginPayload,
                    _ => Raw: byte[Length]
                }
            };
            select p.Type, p.Payload.Case from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x04, 0x2A, 0x00, 0x00, 0x00 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual("Login", table[0][1]);
    }

    [TestMethod]
    public void Query_SwitchPayload_MatchingCase_ShouldExposeNestedBranchField()
    {
        var query = @"
            binary LoginPayload {
                UserId: int le
            };
            binary Packet {
                Type: byte,
                Length: byte,
                Payload: switch Type {
                    1 => Login: LoginPayload,
                    _ => Raw: byte[Length]
                }
            };
            select p.Payload.Login.UserId from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x04, 0x2A, 0x00, 0x00, 0x00 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
    }

    [TestMethod]
    public void Query_SwitchPayload_DefaultCase_ShouldSelectRawBranch()
    {
        var query = @"
            binary LoginPayload {
                UserId: int le
            };
            binary Packet {
                Type: byte,
                Length: byte,
                Payload: switch Type {
                    1 => Login: LoginPayload,
                    _ => Raw: byte[Length]
                }
            };
            select p.Payload.Case from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x09, 0x03, 0xAA, 0xBB, 0xCC };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Raw", table[0][0]);
    }

    [TestMethod]
    public void Query_SwitchPayload_FilterByCase_ShouldReturnMatchingRows()
    {
        var query = @"
            binary LoginPayload {
                UserId: int le
            };
            binary Packet {
                Type: byte,
                Length: byte,
                Payload: switch Type {
                    1 => Login: LoginPayload,
                    _ => Raw: byte[Length]
                }
            };
            select p.Type from #test.files() f
            cross apply Interpret<Packet>(f.Content) p
            where p.Payload.Case = 'Login'";

        var loginData = new byte[] { 0x01, 0x04, 0x2A, 0x00, 0x00, 0x00 };
        var table = RunQuery(query, loginData);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
    }

    [TestMethod]
    public void Query_SwitchPayload_MultipleCases_ShouldSelectMatchingMiddleBranch()
    {
        var query = @"
            binary LoginPayload {
                UserId: int le
            };
            binary DataPayload {
                Size: short le
            };
            binary Packet {
                Type: byte,
                Length: byte,
                Payload: switch Type {
                    1 => Login: LoginPayload,
                    2 => Data: DataPayload,
                    _ => Raw: byte[Length]
                }
            };
            select p.Payload.Case, p.Payload.Data.Size from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x02, 0x02, 0x05, 0x00 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Data", table[0][0]);
        Assert.AreEqual((short)5, table[0][1]);
    }

    [TestMethod]
    public void Query_SwitchPayload_PrimitiveBranch_ShouldExposeBranchValue()
    {
        var query = @"
            binary Packet {
                Type: byte,
                Length: byte,
                Payload: switch Type {
                    1 => Code: int le,
                    _ => Raw: byte[Length]
                }
            };
            select p.Payload.Case, p.Payload.Code from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x04, 0x07, 0x00, 0x00, 0x00 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Code", table[0][0]);
        Assert.AreEqual(7, table[0][1]);
    }

    [TestMethod]
    public void Query_SwitchPayload_DefaultBranch_ShouldExposeRawBytes()
    {
        var query = @"
            binary Packet {
                Type: byte,
                Length: byte,
                Payload: switch Type {
                    1 => Code: int le,
                    _ => Raw: byte[Length]
                }
            };
            select p.Payload.Raw from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x09, 0x03, 0xAA, 0xBB, 0xCC };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC }, (byte[])table[0][0]);
    }

    [TestMethod]
    public void Query_SwitchPayload_NoMatchNoDefault_ShouldThrow()
    {
        var query = @"
            binary LoginPayload {
                UserId: int le
            };
            binary Packet {
                Type: byte,
                Length: byte,
                Payload: switch Type {
                    1 => Login: LoginPayload
                }
            };
            select p.Payload.Case from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x09, 0x03, 0xAA, 0xBB, 0xCC };

        Assert.Throws<InvalidOperationException>(() => RunQuery(query, data));
    }

    private Table RunQuery(string query, byte[] content)
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
