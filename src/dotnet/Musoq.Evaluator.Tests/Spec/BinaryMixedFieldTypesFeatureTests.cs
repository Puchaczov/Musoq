using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schemas combining multiple field types
///     in realistic scenarios. Tests schemas with int + string + byte array,
///     computed + when combined, deep nesting, and schema arrays with various features.
/// </summary>
[TestClass]
public class BinaryMixedFieldTypesFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Mixed: Int + String + ByteArray

    /// <summary>
    ///     Tests a schema combining integer, string, and byte array fields.
    /// </summary>
    [TestMethod]
    public void Binary_IntStringByteArray_ShouldParseAllFieldTypes()
    {
        var query = @"
            binary FileHeader {
                Version: int le,
                NameLen: byte,
                Name: string[NameLen] utf8,
                DataLen: byte,
                Data: byte[DataLen]
            };
            select d.Version, d.Name, d.DataLen from #test.files() b
            cross apply Interpret(b.Content, 'FileHeader') d";

        var name = Encoding.UTF8.GetBytes("test");
        var data = new byte[] { 0xAA, 0xBB, 0xCC };
        var testData = new byte[] { 0x01, 0x00, 0x00, 0x00 }
            .Concat([(byte)name.Length])
            .Concat(name)
            .Concat([(byte)data.Length])
            .Concat(data)
            .ToArray();

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("test", table[0][1]);
        Assert.AreEqual((byte)3, table[0][2]);
    }

    #endregion

    #region Mixed: Computed + Conditional

    /// <summary>
    ///     Tests a schema combining computed fields with conditional fields.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedAndConditional_ShouldWorkTogether()
    {
        var query = @"
            binary Packet {
                Type: byte,
                Length: short le,
                Payload: byte[Length] when Type <> 0,
                TotalSize: = Length + 3
            };
            select p.Type, p.Length, p.TotalSize from #test.files() b
            cross apply Interpret(b.Content, 'Packet') p";

        var testData = new byte[]
        {
            0x01,
            0x03, 0x00,
            0xAA, 0xBB, 0xCC
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((short)3, table[0][1]);
        Assert.AreEqual(6, table[0][2]);
    }

    #endregion

    #region Mixed: Three-Level Deep Nesting

    /// <summary>
    ///     Tests three levels of schema nesting.
    /// </summary>
    [TestMethod]
    public void Binary_ThreeLevelNesting_ShouldResolveAllLevels()
    {
        var query = @"
            binary Inner { Value: short le };
            binary Middle { Data: Inner };
            binary Outer { Container: Middle };
            select o.Container.Data.Value from #test.files() b
            cross apply Interpret(b.Content, 'Outer') o";

        var testData = new byte[] { 0x39, 0x05 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)0x0539, table[0][0]);
    }

    #endregion

    #region Mixed: Schema Array with Computed Fields

    /// <summary>
    ///     Tests schema array where each element has a computed field.
    /// </summary>
    [TestMethod]
    public void Binary_SchemaArrayWithComputed_ShouldComputePerElement()
    {
        var query = @"
            binary Item {
                Width: byte,
                Height: byte,
                Area: = Width * Height
            };
            binary Container { Count: byte, Items: Item[Count] };
            select i.Width, i.Height, i.Area from #test.files() b
            cross apply Interpret(b.Content, 'Container') c
            cross apply c.Items i
            order by i.Width";

        var testData = new byte[]
        {
            0x03,
            0x02, 0x03,
            0x04, 0x05,
            0x06, 0x07
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual((byte)3, table[0][1]);
        Assert.AreEqual((byte)(2 * 3), table[0][2]);
        Assert.AreEqual((byte)4, table[1][0]);
        Assert.AreEqual((byte)5, table[1][1]);
        Assert.AreEqual((byte)(4 * 5), table[1][2]);
        Assert.AreEqual((byte)6, table[2][0]);
        Assert.AreEqual((byte)7, table[2][1]);
        Assert.AreEqual((byte)(6 * 7), table[2][2]);
    }

    #endregion

    #region Mixed: Schema with Multiple String Fields

    /// <summary>
    ///     Tests schema with multiple independently-sized string fields.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleStrings_ShouldParseEachIndependently()
    {
        var query = @"
            binary Record {
                FirstLen: byte,
                First: string[FirstLen] utf8,
                LastLen: byte,
                Last: string[LastLen] utf8
            };
            select r.First, r.Last from #test.files() b
            cross apply Interpret(b.Content, 'Record') r";

        var first = Encoding.UTF8.GetBytes("John");
        var last = Encoding.UTF8.GetBytes("Doe");
        var testData = new[] { (byte)first.Length }
            .Concat(first)
            .Concat([(byte)last.Length])
            .Concat(last)
            .ToArray();

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("John", table[0][0]);
        Assert.AreEqual("Doe", table[0][1]);
    }

    #endregion

    #region Mixed: Schema Array with WHERE Filtering

    /// <summary>
    ///     Tests filtering schema array elements with WHERE clause.
    /// </summary>
    [TestMethod]
    public void Binary_SchemaArrayWithWhere_ShouldFilterElements()
    {
        var query = @"
            binary Entry { Type: byte, Value: short le };
            binary DataList { Count: byte, Entries: Entry[Count] };
            select e.Value from #test.files() b
            cross apply Interpret(b.Content, 'DataList') t
            cross apply t.Entries e
            where e.Type = 2
            order by e.Value asc";

        var testData = new byte[]
        {
            0x04,
            0x01, 0x0A, 0x00,
            0x02, 0x0B, 0x00,
            0x01, 0x0C, 0x00,
            0x02, 0x0D, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((short)0x000B, table[0][0]);
        Assert.AreEqual((short)0x000D, table[1][0]);
    }

    #endregion

    #region Mixed: Schema Array with ORDER BY

    /// <summary>
    ///     Tests ordering schema array elements by a parsed field.
    /// </summary>
    [TestMethod]
    public void Binary_SchemaArrayWithOrderBy_ShouldSortResults()
    {
        var query = @"
            binary Record { Id: byte, Score: short le };
            binary List { Count: byte, Records: Record[Count] };
            select r.Id, r.Score from #test.files() b
            cross apply Interpret(b.Content, 'List') l
            cross apply l.Records r
            order by r.Score desc";

        var testData = new byte[]
        {
            0x03,
            0x01, 0x0A, 0x00,
            0x02, 0x1E, 0x00,
            0x03, 0x14, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual((short)30, table[0][1]);
        Assert.AreEqual((byte)3, table[1][0]);
        Assert.AreEqual((short)20, table[1][1]);
        Assert.AreEqual((byte)1, table[2][0]);
        Assert.AreEqual((short)10, table[2][1]);
    }

    #endregion

    #region Mixed: Conditional with Different Types

    /// <summary>
    ///     Tests conditional field producing different types based on type discriminator.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalWithDifferentPayloads_ShouldSelectCorrectBranch()
    {
        var query = @"
            binary Message {
                Type: byte,
                ShortPayload: byte when Type = 1,
                IntPayload: int le when Type = 2
            };
            select m.Type, m.ShortPayload, m.IntPayload from #test.files() b
            cross apply Interpret(b.Content, 'Message') m";

        var testData = new byte[]
        {
            0x01,
            0xAB
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)0xAB, table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    #endregion
}
