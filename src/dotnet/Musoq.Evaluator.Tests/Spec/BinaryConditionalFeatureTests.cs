using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema Conditional Fields (Section 4.5 of specification).
///     Tests 'when' clause for conditional field parsing.
/// </summary>
[TestClass]
public class BinaryConditionalFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.5: Chained Conditional Fields

    /// <summary>
    ///     Tests conditional field depending on another conditional field.
    /// </summary>
    [TestMethod]
    public void Binary_ChainedConditionalFields_ShouldWork()
    {
        var query = @"
            binary Data { 
                HasData: byte,
                Length: int le when HasData <> 0,
                Payload: byte[Length] when HasData <> 0
            };
            select d.HasData, d.Length, d.Payload from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0xAA, 0xBB, 0xCC };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        var payload = (byte[])table[0][2];
        Assert.HasCount(3, payload);
        Assert.AreEqual((byte)0xAA, payload[0]);
    }

    #endregion

    #region Section 4.5: Conditional in WHERE Clause

    /// <summary>
    ///     Tests filtering on conditional field value.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalField_InWhereClause_FiltersNulls()
    {
        var query = @"
            binary Data { 
                HasValue: byte,
                Value: int le when HasValue <> 0
            };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d
            where d.Value is not null";

        var entities = new[]
        {
            new BinaryEntity { Name = "with.bin", Content = new byte[] { 0x01, 0x2A, 0x00, 0x00, 0x00 } },
            new BinaryEntity { Name = "without.bin", Content = new byte[] { 0x00 } }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
    }

    #endregion

    #region Section 4.5: Basic When Clause

    /// <summary>
    ///     Tests conditional field present when condition is true.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalField_WhenTrue_ShouldParse()
    {
        var query = @"
            binary Data { 
                HasValue: byte,
                Value: int le when HasValue <> 0
            };
            select d.HasValue, d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[5];
        testData[0] = 0x01;
        BitConverter.GetBytes(12345).CopyTo(testData, 1);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(12345, table[0][1]);
    }

    /// <summary>
    ///     Tests conditional field skipped when condition is false.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalField_WhenFalse_ShouldBeNull()
    {
        var query = @"
            binary Data { 
                HasValue: byte,
                Value: int le when HasValue <> 0
            };
            select d.HasValue, d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    #endregion

    #region Section 4.5: Equality Conditions

    /// <summary>
    ///     Tests conditional field with equality comparison.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalField_EqualityCheck_ShouldWork()
    {
        var query = @"
            binary Message { 
                Type: byte,
                ErrorCode: short le when Type = 255
            };
            select m.Type, m.ErrorCode from #test.files() f
            cross apply Interpret(f.Content, 'Message') m";


        var testData = new byte[3];
        testData[0] = 0xFF;
        BitConverter.GetBytes((short)500).CopyTo(testData, 1);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)255, table[0][0]);
        Assert.AreEqual((short)500, table[0][1]);
    }

    /// <summary>
    ///     Tests conditional field skipped when equality fails.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalField_EqualityFails_ShouldBeNull()
    {
        var query = @"
            binary Message { 
                Type: byte,
                ErrorCode: short le when Type = 255
            };
            select m.Type, m.ErrorCode from #test.files() f
            cross apply Interpret(f.Content, 'Message') m";


        var testData = new byte[] { 0x01 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    #endregion

    #region Section 4.5: Multiple Conditional Fields

    /// <summary>
    ///     Tests multiple conditional fields with different conditions.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleConditionalFields_IndependentConditions()
    {
        var query = @"
            binary Packet { 
                Flags: byte,
                PayloadLen: int le when (Flags & 1) <> 0,
                Checksum: int le when (Flags & 2) <> 0
            };
            select p.Flags, p.PayloadLen, p.Checksum from #test.files() f
            cross apply Interpret(f.Content, 'Packet') p";


        var testData = new byte[9];
        testData[0] = 0x03;
        BitConverter.GetBytes(100).CopyTo(testData, 1);
        BitConverter.GetBytes(0xABCD).CopyTo(testData, 5);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual(100, table[0][1]);
        Assert.AreEqual(0xABCD, table[0][2]);
    }

    /// <summary>
    ///     Tests one conditional field present, one null.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleConditionalFields_PartialPresence()
    {
        var query = @"
            binary Packet { 
                Flags: byte,
                PayloadLen: int le when (Flags & 1) <> 0,
                Checksum: int le when (Flags & 2) <> 0
            };
            select p.Flags, p.PayloadLen, p.Checksum from #test.files() f
            cross apply Interpret(f.Content, 'Packet') p";


        var testData = new byte[5];
        testData[0] = 0x01;
        BitConverter.GetBytes(100).CopyTo(testData, 1);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(100, table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    #endregion

    #region Section 4.5: Conditional with Comparison Operators

    /// <summary>
    ///     Tests conditional field with greater-than comparison.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalField_GreaterThan_ShouldWork()
    {
        var query = @"
            binary Data { 
                Version: byte,
                ExtendedData: int le when Version > 1
            };
            select d.Version, d.ExtendedData from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[5];
        testData[0] = 0x02;
        BitConverter.GetBytes(999).CopyTo(testData, 1);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual(999, table[0][1]);
    }

    /// <summary>
    ///     Tests conditional field with less-than comparison.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalField_LessThan_ShouldWork()
    {
        var query = @"
            binary Data { 
                Version: byte,
                LegacyField: short le when Version < 3
            };
            select d.Version, d.LegacyField from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[3];
        testData[0] = 0x02;
        BitConverter.GetBytes((short)100).CopyTo(testData, 1);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual((short)100, table[0][1]);
    }

    #endregion
}
