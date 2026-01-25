using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema Computed Fields (Section 4.6 of specification).
///     Tests fields derived from expressions without consuming input.
/// </summary>
[TestClass]
public class BinaryComputedFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.6: Chained Computed Fields

    /// <summary>
    ///     Tests computed field derived from another computed field.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedField_ChainedComputation_ShouldCalculate()
    {
        var query = @"
            binary Data { 
                Base: int le,
                Step1: = Base * 2,
                Step2: = Step1 + 10
            };
            select d.Base, d.Step1, d.Step2 from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d";

        var data = new byte[] { 0x05, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(20, table[0][2]);
    }

    #endregion

    #region Section 4.6: Computed Fields in WHERE Clause

    /// <summary>
    ///     Tests filtering by computed field.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedField_InWhereClause_ShouldFilter()
    {
        var query = @"
            binary Data { 
                Value: int le,
                Category: = Value / 10
            };
            select d.Value, d.Category from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d
            where d.Category = 2";

        var data1 = new byte[] { 0x0F, 0x00, 0x00, 0x00 };
        var data2 = new byte[] { 0x19, 0x00, 0x00, 0x00 };
        var data3 = new byte[] { 0x23, 0x00, 0x00, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = data1 },
            new BinaryEntity { Name = "2.bin", Data = data2 },
            new BinaryEntity { Name = "3.bin", Data = data3 }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(25, table[0][0]);
        Assert.AreEqual(2, table[0][1]);
    }

    #endregion

    #region Section 4.6: String Computed Fields

    /// <summary>
    ///     Tests computed field producing string description.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedField_StringExpression_ShouldWork()
    {
        var query = @"
            binary Header { 
                Version: byte,
                VersionLabel: = 'v' + ToString(Version)
            };
            select h.Version, h.VersionLabel from #test.bytes() b
            cross apply Interpret(b.Content, 'Header') h";

        var data = new byte[] { 0x02 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual("v2", table[0][1]);
    }

    #endregion

    #region Section 4.6: Computed Fields with Aggregate-Like Functions

    /// <summary>
    ///     Tests computed field with conditional expression.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedField_ConditionalExpression_ShouldWork()
    {
        var query = @"
            binary Data { 
                Value: int le,
                IsPositive: = Value > 0
            };
            select d.Value, d.IsPositive from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d";

        var data = new byte[] { 0x0A, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.IsTrue((bool?)table[0][1]);
    }

    #endregion

    #region Section 4.6: Basic Computed Fields

    /// <summary>
    ///     Tests computed field from constant.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedField_Constant_ShouldReturnValue()
    {
        var query = @"
            binary Data { 
                Value: int le,
                Constant: = 42
            };
            select d.Value, d.Constant from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d";

        var data = new byte[] { 0x01, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(42, table[0][1]);
    }

    /// <summary>
    ///     Tests computed field derived from another field.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedField_FromField_ShouldCalculate()
    {
        var query = @"
            binary Data { 
                Value: int le,
                Doubled: = Value * 2
            };
            select d.Value, d.Doubled from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d";

        var data = new byte[] { 0x0A, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(20, table[0][1]);
    }

    /// <summary>
    ///     Tests computed field with multiple input fields.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedField_FromMultipleFields_ShouldCalculate()
    {
        var query = @"
            binary Data { 
                A: int le,
                B: int le,
                Sum: = A + B,
                Product: = A * B
            };
            select d.A, d.B, d.Sum, d.Product from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d";

        var data = new byte[]
        {
            0x05, 0x00, 0x00, 0x00,
            0x03, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5, table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual(8, table[0][2]);
        Assert.AreEqual(15, table[0][3]);
    }

    #endregion

    #region Section 4.6: Computed Fields No Input Consumption

    /// <summary>
    ///     Tests that computed fields don't consume input bytes.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedField_ShouldNotConsumeInput()
    {
        var query = @"
            binary Data { 
                First: int le,
                Computed: = First * 10,
                Second: int le
            };
            select d.First, d.Computed, d.Second from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d";

        var data = new byte[]
        {
            0x01, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(2, table[0][2]);
    }

    /// <summary>
    ///     Tests multiple computed fields interspersed with regular fields.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedField_InterspersedWithRegular_ShouldWork()
    {
        var query = @"
            binary Data { 
                A: byte,
                ComputedA: = A * 2,
                B: byte,
                ComputedB: = B * 3,
                Sum: = ComputedA + ComputedB
            };
            select d.A, d.ComputedA, d.B, d.ComputedB, d.Sum from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d";

        var data = new byte[] { 0x04, 0x05 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)4, table[0][0]);
        Assert.AreEqual(8, table[0][1]);
        Assert.AreEqual((byte)5, table[0][2]);
        Assert.AreEqual(15, table[0][3]);
        Assert.AreEqual(23, table[0][4]);
    }

    #endregion
}
