using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema Check Constraints (Section 4.9 of specification).
///     Tests validation constraints on parsed field values.
/// </summary>
[TestClass]
public class BinaryCheckConstraintFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.9: Basic Check Constraint

    /// <summary>
    ///     Tests check constraint with exact value match that passes.
    /// </summary>
    [TestMethod]
    public void Binary_CheckConstraint_ValidValue_ShouldPass()
    {
        var query = @"
            binary Header { 
                Magic: int le check Magic = 0x12345678
            };
            select h.Magic from #test.files() b
            cross apply Interpret(b.Content, 'Header') h";

        var testData = new byte[] { 0x78, 0x56, 0x34, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0][0]);
    }

    #endregion

    #region Section 4.9: Range Validation

    /// <summary>
    ///     Tests check constraint with range validation that passes.
    /// </summary>
    [TestMethod]
    public void Binary_CheckConstraint_RangeValid_ShouldPass()
    {
        var query = @"
            binary Data { 
                Version: byte check Version >= 1 AND Version <= 10
            };
            select d.Version from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[] { 0x05 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)5, table[0][0]);
    }

    #endregion

    #region Section 4.9: Check Constraint with TryInterpret

    /// <summary>
    ///     Tests that TryInterpret returns null when check constraint fails.
    /// </summary>
    [TestMethod]
    public void Binary_CheckConstraint_FailedWithTryInterpret_ShouldReturnNull()
    {
        var query = @"
            binary Header { 
                Magic: int le check Magic = 0xDEADBEEF
            };
            select h.Magic from #test.files() b
            outer apply TryInterpret(b.Content, 'Header') h";

        var testData = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    #endregion

    #region Section 4.9: Multiple Check Constraints

    /// <summary>
    ///     Tests schema with multiple check constraints on different fields.
    /// </summary>
    [TestMethod]
    public void Binary_CheckConstraint_MultipleFields_ShouldValidateAll()
    {
        var query = @"
            binary Record { 
                Magic: int le check Magic = 0xCAFE,
                Version: byte check Version >= 1,
                Length: short le
            };
            select r.Magic, r.Version, r.Length from #test.files() b
            cross apply Interpret(b.Content, 'Record') r";

        var testData = new byte[]
        {
            0xFE, 0xCA, 0x00, 0x00,
            0x03,
            0x0A, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xCAFE, table[0][0]);
        Assert.AreEqual((byte)3, table[0][1]);
        Assert.AreEqual((short)10, table[0][2]);
    }

    #endregion

    #region Section 4.9: Check with Filter to Valid Files Only

    /// <summary>
    ///     Tests using TryInterpret with check to filter valid files.
    /// </summary>
    [TestMethod]
    public void Binary_CheckConstraint_FilterValidFilesOnly_ShouldWork()
    {
        var query = @"
            binary ValidFile { 
                Magic: int le check Magic = 0xABCD,
                Size: int le
            };
            select b.Name, h.Size from #test.files() b
            outer apply TryInterpret(b.Content, 'ValidFile') h
            where h.Magic is not null";

        var validData = new byte[] { 0xCD, 0xAB, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00 };
        var invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "valid.bin", Content = validData },
            new BinaryEntity { Name = "invalid.bin", Content = invalidData }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("valid.bin", table[0][0]);
        Assert.AreEqual(100, table[0][1]);
    }

    #endregion
}
