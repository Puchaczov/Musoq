using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema.Interpreters;
using ComponentBinaryEntity = Musoq.Evaluator.Tests.Components.BinaryEntity;
using ComponentTextEntity = Musoq.Evaluator.Tests.Components.TextEntity;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    [TestMethod]
    public void Query_BinaryStringAsText_ShouldExposeNestedValuesAndContinueToTrailer()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest trim
            };
            binary Envelope {
                Length: byte,
                Payload: string[Length] utf8 as KeyValue,
                Trailer: short le
            };
            select e.Length, e.Payload.Key, e.Payload.Value, e.Trailer
            from #test.files() f
            cross apply Interpret<Envelope>(f.Content) e";

        var payload = Encoding.UTF8.GetBytes("name=Łódź");
        var data = new byte[payload.Length + 3];
        data[0] = (byte)payload.Length;
        Buffer.BlockCopy(payload, 0, data, 1, payload.Length);
        data[^2] = 0x34;
        data[^1] = 0x12;

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                { "#test", [new BinaryEntity { Name = "envelope.bin", Content = data }] }
            });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("e.Length", typeof(byte)),
            ("e.Payload.Key", typeof(string)),
            ("e.Payload.Value", typeof(string)),
            ("e.Trailer", typeof(short)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [(byte)payload.Length, "name", "Łódź", (short)0x1234]);
    }

    [TestMethod]
    public void Query_InterpretedNestedArray_ShouldExpandThroughApplyAndRespectSqlFilterAndOrder()
    {
        const string query = @"
            binary Item {
                Id: byte,
                Score: short le
            };
            binary Batch {
                Count: byte,
                Items: Item[Count]
            };
            select f.Name, i.Id, i.Score
            from #test.files() f
            cross apply Interpret<Batch>(f.Content) b
            cross apply b.Items i
            where i.Score >= 20
            order by i.Score desc";

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                {
                    "#test",
                    [
                        new BinaryEntity
                        {
                            Name = "a.bin",
                            Content = CreateBatch((1, 10), (2, 20), (3, 30))
                        },
                        new BinaryEntity
                        {
                            Name = "b.bin",
                            Content = CreateBatch((4, 40), (5, 5))
                        }
                    ]
                }
            });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("f.Name", typeof(string)),
            ("i.Id", typeof(byte)),
            ("i.Score", typeof(short)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["b.bin", (byte)4, (short)40],
            ["a.bin", (byte)3, (short)30],
            ["a.bin", (byte)2, (short)20]);
    }

    [TestMethod]
    public void Query_BinaryAndTextCtes_ShouldJoinFilterGroupAndOrderTogether()
    {
        const string query = @"
            binary BinRecord {
                Id: byte,
                Score: short le
            };
            text TxtRecord {
                Id: until ':',
                Label: rest trim
            };
            with BinData as (
                select ToString(b.Id) as BinId, b.Score as Score
                from #bin.files() bf
                cross apply Interpret<BinRecord>(bf.Content) b
                where b.Score >= 100
            ),
            TextData as (
                select r.Id as TxtId, r.Label as Label
                from #txt.files() tf
                cross apply Parse<TxtRecord>(tf.Text) r
            )
            select bd.BinId, Count(t.Label) as MatchCount, Sum(bd.Score) as ScoreTotal
            from BinData bd
            inner join TextData t on bd.BinId = t.TxtId
            where t.Label <> 'ignore'
            group by bd.BinId
            order by bd.BinId desc";

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
            {
                {
                    "#bin",
                    [
                        new ComponentBinaryEntity { Name = "1.bin", Content = [1, 100, 0] },
                        new ComponentBinaryEntity { Name = "2.bin", Content = [2, 200, 0] },
                        new ComponentBinaryEntity { Name = "3.bin", Content = [3, 50, 0] }
                    ]
                }
            },
            new Dictionary<string, IEnumerable<ComponentTextEntity>>
            {
                {
                    "#txt",
                    [
                        new ComponentTextEntity { Name = "1.txt", Text = "1:Alpha" },
                        new ComponentTextEntity { Name = "1-ignore.txt", Text = "1:ignore" },
                        new ComponentTextEntity { Name = "2.txt", Text = "2:Beta" },
                        new ComponentTextEntity { Name = "3.txt", Text = "3:Gamma" }
                    ]
                }
            });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("bd.BinId", typeof(string)),
            ("MatchCount", typeof(long)),
            ("ScoreTotal", typeof(short?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["2", 1L, (short)200],
            ["1", 1L, (short)100]);
    }

    [TestMethod]
    public void Query_BinaryTextBoundaryFailure_ShouldReportQualifiedFieldPath()
    {
        const string query = @"
            text KeyValue {
                Key: until '=',
                Value: chars[3]
            };
            binary Envelope {
                Length: byte,
                Payload: string[Length] utf8 as KeyValue
            };
            select e.Payload.Key
            from #test.files() f
            cross apply Interpret<Envelope>(f.Content) e";

        var schemaProvider = CreateBinaryProvider(
            "malformed.bin",
            [6, (byte)'k', (byte)'e', (byte)'y', (byte)'=', (byte)'a', (byte)'b']);
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var exception = Assert.ThrowsExactly<ParseException>(() =>
        {
            var table = vm.Run(CancellationToken.None);
            _ = table.Count;
        });

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
        Assert.AreEqual("KeyValue", exception.SchemaName);
        Assert.AreEqual("Payload.Value", exception.FieldName);
        Assert.AreEqual(4, exception.Position);
        StringAssert.Contains(exception.Message, "Payload.Value");
    }

    [TestMethod]
    public void Query_PartialInterpret_BinaryTextBoundaryFailure_ShouldRetainParentFieldsAndMetadata()
    {
        const string query = @"
            text KeyValue {
                Key: until '=',
                Value: chars[3]
            };
            binary Envelope {
                Header: byte,
                Length: byte,
                Payload: string[Length] utf8 as KeyValue,
                Trailer: byte
            };
            select p.ParsedFields, p.ErrorField, p.ErrorMessage, p.BytesConsumed
            from #test.files() f
            cross apply PartialInterpret<Envelope>(f.Content) p";

        var schemaProvider = CreateBinaryProvider(
            "partial-malformed.bin",
            [7, 6, (byte)'k', (byte)'e', (byte)'y', (byte)'=', (byte)'a', (byte)'b']);
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var parsedFields = (Dictionary<string, object?>)table[0][0]!;
        Assert.AreEqual((byte)7, parsedFields["Header"]);
        Assert.AreEqual((byte)6, parsedFields["Length"]);
        Assert.IsFalse(parsedFields.ContainsKey("Payload"));
        Assert.AreEqual("Payload.Value", table[0][1]);
        StringAssert.Contains((string)table[0][2]!, "Payload.Value");
        StringAssert.Contains((string)table[0][2]!, "ISE0001");
        Assert.AreEqual(8, table[0][3]);
    }

    [TestMethod]
    public void Query_SchemaDefinitions_ShouldRemainScopedToEachCompilationBatch()
    {
        const string firstQuery = @"
            binary BatchScoped {
                Value: byte
            };
            select p.Value
            from #test.files() f
            cross apply Interpret<BatchScoped>(f.Content) p";
        const string secondQuery = @"
            binary BatchScoped {
                Value: short le
            };
            select p.Value
            from #test.files() f
            cross apply Interpret<BatchScoped>(f.Content) p";

        var firstVm = CompileGeneratedQuery(
            firstQuery,
            Guid.NewGuid().ToString(),
            CreateBinaryProvider("byte.bin", [7]),
            LoggerResolver,
            TestCompilationOptions);
        var firstTable = firstVm.Run(CancellationToken.None);
        Assert.AreEqual((byte)7, firstTable[0][0]);

        var secondVm = CompileGeneratedQuery(
            secondQuery,
            Guid.NewGuid().ToString(),
            CreateBinaryProvider("short.bin", [0x34, 0x12]),
            LoggerResolver,
            TestCompilationOptions);
        var secondTable = secondVm.Run(CancellationToken.None);
        Assert.AreEqual((short)0x1234, secondTable[0][0]);
    }

    private static byte[] CreateBatch(params (byte Id, short Score)[] items)
    {
        var data = new byte[1 + (items.Length * 3)];
        data[0] = (byte)items.Length;

        for (var i = 0; i < items.Length; i++)
        {
            var offset = 1 + (i * 3);
            data[offset] = items[i].Id;
            data[offset + 1] = (byte)items[i].Score;
            data[offset + 2] = (byte)(items[i].Score >> 8);
        }

        return data;
    }

    private static BinarySchemaProvider CreateBinaryProvider(string name, byte[] content)
    {
        return new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                { "#test", [new BinaryEntity { Name = name, Content = content }] }
            });
    }
}
