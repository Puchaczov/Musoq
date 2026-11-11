using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema Inheritance (Section 8.3 of specification).
///     Tests the 'extends' keyword for schema inheritance.
/// </summary>
[TestClass]
public class BinaryInheritanceFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 8.3: Basic Extends

    /// <summary>
    ///     Tests basic schema inheritance with extends keyword.
    /// </summary>
    [TestMethod]
    public void Binary_Extends_BasicInheritance_ShouldIncludeParentFields()
    {
        var query = (
            "\n"
            + "            binary Base { \n"
            + "                Id: byte,\n"
            + "                Version: byte\n"
            + "            };\n"
            + "            binary Extended extends Base { \n"
            + "                Flags: byte\n"
            + "            };\n"
            + "            select e.Id, e.Version, e.Flags from #test.files() b\n"
            + "            cross apply Interpret<Extended>(b.Content) e"
        );

        var testData = new byte[] { 0x01, 0x02, 0x03 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0].ReadValue(0));
        Assert.AreEqual((byte)2, table[0].ReadValue(1));
        Assert.AreEqual((byte)3, table[0].ReadValue(2));
    }

    #endregion

    #region Section 8.3: Extends with Computed Fields

    /// <summary>
    ///     Tests child schema with computed fields referencing parent fields.
    /// </summary>
    [TestMethod]
    public void Binary_Extends_ComputedFieldFromParent_ShouldCompute()
    {
        var query = (
            "\n"
            + "            binary Base { \n"
            + "                Value: int le\n"
            + "            };\n"
            + "            binary Derived extends Base { \n"
            + "                Doubled: = Value * 2\n"
            + "            };\n"
            + "            select d.Value, d.Doubled from #test.files() b\n"
            + "            cross apply Interpret<Derived>(b.Content) d"
        );

        var testData = BitConverter.GetBytes(25);
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(25, table[0].ReadValue(0));
        Assert.AreEqual(50, table[0].ReadValue(1));
    }

    #endregion

    #region Section 8.3: Extends with Additional Data Fields

    /// <summary>
    ///     Tests extends where child adds data fields that reference parent fields.
    /// </summary>
    [TestMethod]
    public void Binary_Extends_ChildUsesParentLength_ShouldReadData()
    {
        var query = (
            "\n"
            + "            binary Header { \n"
            + "                MsgType: byte,\n"
            + "                Length: short le\n"
            + "            };\n"
            + "            binary Packet extends Header { \n"
            + "                Data: byte[Length]\n"
            + "            };\n"
            + "            select p.MsgType, p.Length, p.Data from #test.files() b\n"
            + "            cross apply Interpret<Packet>(b.Content) p"
        );

        var testData = new byte[] { 0x01, 0x03, 0x00, 0xAA, 0xBB, 0xCC };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0].ReadValue(0));
        Assert.AreEqual((short)3, table[0].ReadValue(1));
        var data = (byte[])table[0].ReadValue(2);
        Assert.HasCount(3, data);
        Assert.AreEqual((byte)0xAA, data[0]);
        Assert.AreEqual((byte)0xBB, data[1]);
        Assert.AreEqual((byte)0xCC, data[2]);
    }

    #endregion

    #region Section 8.3: Multi-Level Inheritance

    /// <summary>
    ///     Tests multi-level inheritance chain: Grandchild extends Child extends Base.
    /// </summary>
    [TestMethod]
    public void Binary_Extends_MultiLevel_ShouldIncludeAllAncestorFields()
    {
        var query = (
            "\n"
            + "            binary Base { \n"
            + "                Id: byte\n"
            + "            };\n"
            + "            binary Child extends Base { \n"
            + "                Version: byte\n"
            + "            };\n"
            + "            binary Grandchild extends Child { \n"
            + "                Flags: byte\n"
            + "            };\n"
            + "            select g.Id, g.Version, g.Flags from #test.files() b\n"
            + "            cross apply Interpret<Grandchild>(b.Content) g"
        );

        var testData = new byte[] { 0x0A, 0x02, 0xFF };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)10, table[0].ReadValue(0));
        Assert.AreEqual((byte)2, table[0].ReadValue(1));
        Assert.AreEqual((byte)255, table[0].ReadValue(2));
    }

    #endregion

    #region Section 8.3: Extends in WHERE Clause

    /// <summary>
    ///     Tests filtering by inherited field in WHERE clause.
    /// </summary>
    [TestMethod]
    public void Binary_Extends_FilterByParentField_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Header { \n"
            + "                Type: byte,\n"
            + "                Length: byte\n"
            + "            };\n"
            + "            binary Message extends Header { \n"
            + "                Payload: byte[Length]\n"
            + "            };\n"
            + "            select m.Type, m.Length from #test.files() b\n"
            + "            cross apply Interpret<Message>(b.Content) m\n"
            + "            where m.Type = 2"
        );

        var entities = new[]
        {
            new BinaryEntity { Name = "type1.bin", Content = [0x01, 0x02, 0xAA, 0xBB] },
            new BinaryEntity { Name = "type2.bin", Content = [0x02, 0x01, 0xCC] },
            new BinaryEntity { Name = "type3.bin", Content = [0x02, 0x03, 0xDD, 0xEE, 0xFF] },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)2, table[0].ReadValue(0));
        Assert.AreEqual((byte)2, table[1].ReadValue(0));
    }

    #endregion

    #region Section 8.3: Extends with Sibling Schemas

    /// <summary>
    ///     Tests two child schemas extending the same parent, used with different data.
    /// </summary>
    [TestMethod]
    public void Binary_Extends_SiblingSchemas_ShouldParseIndependently()
    {
        var query = (
            "\n"
            + "            binary Base { \n"
            + "                Type: byte,\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            binary TypeA extends Base { \n"
            + "                Extra: short le\n"
            + "            };\n"
            + "            select a.Type, a.Value, a.Extra from #test.files() b\n"
            + "            cross apply Interpret<TypeA>(b.Content) a"
        );

        var testData = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00, 0x14, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0].ReadValue(0));
        Assert.AreEqual(10, table[0].ReadValue(1));
        Assert.AreEqual((short)20, table[0].ReadValue(2));
    }

    #endregion
}
