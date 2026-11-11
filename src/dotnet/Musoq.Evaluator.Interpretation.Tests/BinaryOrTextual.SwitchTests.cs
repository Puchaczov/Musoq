using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryOrTextualSwitchTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void Query_SwitchPayload_MatchingCase_ShouldSelectBranchAlias()
    {
        var query = (
            "\n"
            + "            binary LoginPayload {\n"
            + "                UserId: int le\n"
            + "            };\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Login: LoginPayload,\n"
            + "                    _ => Raw: byte[Length]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Type, p.Payload.Case from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x04, 0x2A, 0x00, 0x00, 0x00 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(table, ("Type", typeof(byte)), ("p.Payload.Case", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)0x01, "Login"]);
    }

    [TestMethod]
    public void Query_SwitchPayload_MatchingCase_ShouldExposeNestedBranchField()
    {
        var query = (
            "\n"
            + "            binary LoginPayload {\n"
            + "                UserId: int le\n"
            + "            };\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Login: LoginPayload,\n"
            + "                    _ => Raw: byte[Length]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Login.UserId from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x04, 0x2A, 0x00, 0x00, 0x00 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Payload.Login.UserId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [42]);
    }

    [TestMethod]
    public void Query_SwitchPayload_DefaultCase_ShouldSelectRawBranch()
    {
        var query = (
            "\n"
            + "            binary LoginPayload {\n"
            + "                UserId: int le\n"
            + "            };\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Login: LoginPayload,\n"
            + "                    _ => Raw: byte[Length]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Case from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x09, 0x03, 0xAA, 0xBB, 0xCC };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Payload.Case", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Raw"]);
    }

    [TestMethod]
    public void Query_SwitchPayload_FilterByCase_ShouldReturnMatchingRows()
    {
        var query = (
            "\n"
            + "            binary LoginPayload {\n"
            + "                UserId: int le\n"
            + "            };\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Login: LoginPayload,\n"
            + "                    _ => Raw: byte[Length]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Type from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p\n"
            + "            where p.Payload.Case = 'Login'"
        );

        var loginData = new byte[] { 0x01, 0x04, 0x2A, 0x00, 0x00, 0x00 };
        var table = RunQuery(query, loginData);

        TableMaterializationTestHelper.AssertColumns(table, ("Type", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)0x01]);
    }

    [TestMethod]
    public void Query_SwitchPayload_MultipleCases_ShouldSelectMatchingMiddleBranch()
    {
        var query = (
            "\n"
            + "            binary LoginPayload {\n"
            + "                UserId: int le\n"
            + "            };\n"
            + "            binary DataPayload {\n"
            + "                Size: short le\n"
            + "            };\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Login: LoginPayload,\n"
            + "                    2 => Data: DataPayload,\n"
            + "                    _ => Raw: byte[Length]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Case, p.Payload.Data.Size from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x02, 0x02, 0x05, 0x00 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.Case", typeof(string)),
            ("p.Payload.Data.Size", typeof(short?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Data", (short)5]);
    }

    [TestMethod]
    public void Query_SwitchPayload_PrimitiveBranch_ShouldExposeBranchValue()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Code: int le,\n"
            + "                    _ => Raw: byte[Length]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Case, p.Payload.Code from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x04, 0x07, 0x00, 0x00, 0x00 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.Case", typeof(string)),
            ("p.Payload.Code", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Code", 7]);
    }

    [TestMethod]
    public void Query_SwitchPayload_DefaultCase_ShouldLeavePrimitiveBranchNull()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Code: int le,\n"
            + "                    _ => Raw: byte[Length]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Code, p.Payload.Raw from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x09, 0x03, 0xAA, 0xBB, 0xCC };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.Code", typeof(int?)),
            ("p.Payload.Raw", typeof(byte[]))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null, new byte[] { 0xAA, 0xBB, 0xCC }]);
    }

    [TestMethod]
    public void Query_SwitchPayload_SelectedBranch_ShouldAdvanceCursorExactlyOnce()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Code: byte,\n"
            + "                    _ => Raw: byte[1]\n"
            + "                },\n"
            + "                Tail: byte\n"
            + "            };\n"
            + "            select p.Payload.Code, p.Tail from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x07, 0x09 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Payload.Code", typeof(byte?)), ("Tail", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)0x07, (byte)0x09]);
    }

    [TestMethod]
    public void Query_SwitchPayload_StringSelector_ShouldMatchStringLabel()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Kind: string[1] ascii,\n"
            + "                Payload: switch Kind {\n"
            + "                    'A' => Letter: byte,\n"
            + "                    _ => Raw: byte[1]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Case, p.Payload.Letter from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x41, 0x07 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.Case", typeof(string)),
            ("p.Payload.Letter", typeof(byte?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Letter", (byte)0x07]);
    }

    [TestMethod]
    public void Query_SwitchPayload_ComputedBooleanSelector_ShouldMatchBooleanLabel()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                IsLogin: Type = 1,\n"
            + "                Payload: switch IsLogin {\n"
            + "                    true => Login: byte,\n"
            + "                    _ => Raw: byte[1]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Case, p.Payload.Login from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x07 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.Case", typeof(string)),
            ("p.Payload.Login", typeof(byte?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Login", (byte)0x07]);
    }

    [TestMethod]
    public void Query_SwitchPayload_BinaryAndOctalLabels_ShouldMatchTheirValues()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    0b1 => Binary: byte,\n"
            + "                    0o2 => Octal: byte\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Case from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x02, 0x07 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Payload.Case", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Octal"]);
    }

    [TestMethod]
    public void Query_SwitchPayload_FloatingPointLabel_ShouldMatchCompatibleSelector()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Type: float le,\n"
            + "                Payload: switch Type {\n"
            + "                    1.5 => Hit: byte,\n"
            + "                    _ => Raw: byte[1]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Case, p.Payload.Hit from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x00, 0x00, 0xC0, 0x3F, 0x07 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.Case", typeof(string)),
            ("p.Payload.Hit", typeof(byte?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Hit", (byte)0x07]);
    }

    [TestMethod]
    public void Query_SwitchPayload_NegativeSignedLabel_ShouldMatchCompatibleSelector()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Type: sbyte,\n"
            + "                Payload: switch Type {\n"
            + "                    -1 => Negative: byte,\n"
            + "                    _ => Raw: byte[1]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Case, p.Payload.Negative from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0xFF, 0x07 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.Case", typeof(string)),
            ("p.Payload.Negative", typeof(byte?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Negative", (byte)0x07]);
    }

    [TestMethod]
    public void Query_SwitchPayload_DefaultBranch_ShouldExposeRawBytes()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Code: int le,\n"
            + "                    _ => Raw: byte[Length]\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Raw from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x09, 0x03, 0xAA, 0xBB, 0xCC };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Payload.Raw", typeof(byte[])));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [new byte[] { 0xAA, 0xBB, 0xCC }]);
    }

    [TestMethod]
    public void Query_SwitchPayload_NoMatchNoDefault_ShouldThrow()
    {
        var query = (
            "\n"
            + "            binary LoginPayload {\n"
            + "                UserId: int le\n"
            + "            };\n"
            + "            binary Packet {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: switch Type {\n"
            + "                    1 => Login: LoginPayload\n"
            + "                }\n"
            + "            };\n"
            + "            select p.Payload.Case from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x09, 0x03, 0xAA, 0xBB, 0xCC };

        var exception = Assert.ThrowsExactly<ParseException>(() => RunQuery(query, data));

        Assert.AreEqual(ParseErrorCode.NoAlternativeMatched, exception.ErrorCode);
        Assert.AreEqual("Packet", exception.SchemaName);
        Assert.AreEqual("Payload", exception.FieldName);
        Assert.AreEqual(2, exception.Position);
        Assert.AreEqual("ISE0012", exception.FormattedErrorCode);
        StringAssert.Contains(exception.Details, "9");
    }

    private Table RunQuery(string query, byte[] content)
    {
        var entities = new[]
        {
            new BinaryEntity { Name = "packet.bin", Content = content },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        return TableMaterializationTestHelper.Materialize(vm.Run(CancellationToken.None));
    }
}
