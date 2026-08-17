using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    #region Session 8: Code Generation Coverage Tests

    /// <summary>
    ///     Tests that conditional value type fields become nullable.
    /// </summary>
    [TestMethod]
    public void Query_SelectInterpret_ConditionalValueType_ShouldBeNullable()
    {
        var query = @"
            binary Message {
                HasValue: byte,
                Value: int le when HasValue <> 0
            };
            select
                m.HasValue,
                m.Value
            from #test.files() f
            cross apply Interpret<Message>(f.Content) m";


        var data = new byte[] { 0x00 };
        var entities = new[] { new BinaryEntity { Name = "msg.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    /// <summary>
    ///     Tests nested schema property access in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_SelectInterpret_NestedSchema_ShouldAccessNestedProperties()
    {
        var query = @"
            binary Inner {
                X: short le,
                Y: short le
            };
            binary Outer {
                Id: byte,
                Point: Inner
            };
            select
                o.Id,
                o.Point.X,
                o.Point.Y
            from #test.files() f
            cross apply Interpret<Outer>(f.Content) o";

        var data = new byte[]
        {
            0x42,
            0x0A, 0x00,
            0x14, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "data.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x42, table[0][0]);
        Assert.AreEqual((short)10, table[0][1]);
        Assert.AreEqual((short)20, table[0][2]);
    }

    #endregion

    #region Session 9: Real-World Format Tests

    /// <summary>
    ///     Tests parsing a simple TLV (Type-Length-Value) structure.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_TlvStructure_ShouldParse()
    {
        var query = @"
            binary TlvRecord {
                Type: byte,
                Length: byte,
                Value: byte[Length]
            };
            select
                t.Type,
                t.Length,
                t.Value
            from #test.files() f
            cross apply Interpret<TlvRecord>(f.Content) t";

        var data = new byte[]
        {
            0x01,
            0x03,
            0xAA, 0xBB, 0xCC
        };
        var entities = new[] { new BinaryEntity { Name = "tlv.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((byte)0x03, table[0][1]);
        var value = (byte[])table[0][2];
        Assert.HasCount(3, value);
        Assert.AreEqual((byte)0xAA, value[0]);
    }

    /// <summary>
    ///     Tests parsing a log line with timestamp and message.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleLogLine_ShouldParse()
    {
        var query = @"
            text LogLine {
                Timestamp: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                l.Timestamp,
                l.Level,
                l.Message
            from #test.lines() f
            cross apply Parse<LogLine>(f.Line) l";

        var entities = new[]
            { new TextEntity { Name = "log.txt", Text = "2024-01-15T10:30:00 INFO Application started successfully" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2024-01-15T10:30:00", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("Application started successfully", table[0][2]);
    }

    /// <summary>
    ///     Tests parsing environment variable format.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_EnvVariable_ShouldParse()
    {
        var query = @"
            text EnvVar {
                Name: until '=',
                Value: rest
            };
            select
                e.Name,
                e.Value
            from #test.lines() f
            cross apply Parse<EnvVar>(f.Line) e";

        var entities = new[]
        {
            new TextEntity { Name = "env1", Text = "PATH=/usr/bin:/usr/local/bin" },
            new TextEntity { Name = "env2", Text = "HOME=/home/user" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var names = new HashSet<string> { (string)table[0][0], (string)table[1][0] };
        var values = new HashSet<string> { (string)table[0][1], (string)table[1][1] };
        Assert.Contains("PATH", names);
        Assert.Contains("HOME", names);
        Assert.Contains("/usr/bin:/usr/local/bin", values);
        Assert.Contains("/home/user", values);
    }

    #endregion
}
