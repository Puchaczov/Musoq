using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class StressTestsRound2InterpretationSchemasTests
{
    #region Category 4: Array & Cross Apply Edge Cases

    /// <summary>
    ///     Primitive array with Count=0 via cross apply should produce no rows.
    /// </summary>
    [TestMethod]
    public void R2_Binary_PrimitiveArrayCountZero_CrossApply_ShouldProduceNoRows()
    {
        var query = (
            "\n"
            + "            binary Container {\n"
            + "                Count: byte,\n"
            + "                Values: int le[Count]\n"
            + "            };\n"
            + "            select v.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Container>(f.Content) c\n"
            + "            cross apply c.Values v"
        );

        var data = new byte[] { 0x00 }; // Count=0
        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = data },
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

        Assert.AreEqual(0, table.Count);
    }

    /// <summary>
    ///     Array of schemas with conditional fields + WHERE filter on the conditional value.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ArrayConditionalFields_WhereFilter()
    {
        var query = (
            "\n"
            + "            binary Item {\n"
            + "                Tag: byte,\n"
            + "                Val: int le when Tag <> 0\n"
            + "            };\n"
            + "            binary Box {\n"
            + "                Count: byte,\n"
            + "                Items: Item[Count]\n"
            + "            };\n"
            + "            select i.Tag, i.Val\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Box>(f.Content) b\n"
            + "            cross apply b.Items i\n"
            + "            where i.Val is not null\n"
            + "            order by i.Tag asc"
        );

        using var ms = new MemoryStream();
        ms.WriteByte(4); // 4 items
        // Item 1: Tag=1, Val=10
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes(10));
        // Item 2: Tag=0, Val=null
        ms.WriteByte(0);
        // Item 3: Tag=3, Val=30
        ms.WriteByte(3);
        ms.Write(BitConverter.GetBytes(30));
        // Item 4: Tag=0, Val=null
        ms.WriteByte(0);

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = ms.ToArray() },
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

        // Only items with Tag<>0 pass filter
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0].ReadValue(0));
        Assert.AreEqual(10, table[0].ReadValue(1));
        Assert.AreEqual((byte)3, table[1].ReadValue(0));
        Assert.AreEqual(30, table[1].ReadValue(1));
    }

    /// <summary>
    ///     Cross apply on schema array + GROUP BY + HAVING.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ArrayCrossApply_GroupByHaving()
    {
        var query = (
            "\n"
            + "            binary Entry {\n"
            + "                Cat: byte,\n"
            + "                Score: short le\n"
            + "            };\n"
            + "            binary DataFile {\n"
            + "                Count: byte,\n"
            + "                Entries: Entry[Count]\n"
            + "            };\n"
            + "            select e.Cat, Count(e.Cat) as Cnt, Sum(e.Score) as Total\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<DataFile>(f.Content) d\n"
            + "            cross apply d.Entries e\n"
            + "            group by e.Cat\n"
            + "            having Count(e.Cat) > 1\n"
            + "            order by e.Cat asc"
        );

        using var ms = new MemoryStream();
        ms.WriteByte(5); // 5 entries
        // Cat=1 Score=10
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)10));
        // Cat=2 Score=20
        ms.WriteByte(2);
        ms.Write(BitConverter.GetBytes((short)20));
        // Cat=1 Score=15
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)15));
        // Cat=1 Score=25
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)25));
        // Cat=2 Score=30
        ms.WriteByte(2);
        ms.Write(BitConverter.GetBytes((short)30));

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = ms.ToArray() },
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

        // Cat=1: 3 entries (10+15+25=50), Cat=2: 2 entries (20+30=50)
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0].ReadValue(0));
        Assert.AreEqual(3, Convert.ToInt32(table[0].ReadValue(1)));
        Assert.AreEqual((short)50, table[0].ReadValue(2));
        Assert.AreEqual((byte)2, table[1].ReadValue(0));
        Assert.AreEqual(2, Convert.ToInt32(table[1].ReadValue(1)));
        Assert.AreEqual((short)50, table[1].ReadValue(2));
    }

    /// <summary>
    ///     Array size derived from a computed field expression.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ArraySizeFromComputedField()
    {
        var query = (
            "\n"
            + "            binary Rec {\n"
            + "                Half: byte,\n"
            + "                FullCount: Half * 2,\n"
            + "                Items: short le[FullCount]\n"
            + "            };\n"
            + "            select r.Half, r.FullCount, i.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Rec>(f.Content) r\n"
            + "            cross apply r.Items i\n"
            + "            order by i.Value asc"
        );

        using var ms = new MemoryStream();
        ms.WriteByte(2); // Half=2 → FullCount=4
        ms.Write(BitConverter.GetBytes((short)40));
        ms.Write(BitConverter.GetBytes((short)10));
        ms.Write(BitConverter.GetBytes((short)30));
        ms.Write(BitConverter.GetBytes((short)20));

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = ms.ToArray() },
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

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual((short)10, table[0].ReadValue(2));
        Assert.AreEqual((short)20, table[1].ReadValue(2));
        Assert.AreEqual((short)30, table[2].ReadValue(2));
        Assert.AreEqual((short)40, table[3].ReadValue(2));
    }

    /// <summary>
    ///     Multiple files with schema arrays: each file has its own data, aggregated across all.
    /// </summary>
    [TestMethod]
    public void R2_Binary_MultipleFiles_ArrayAggregation()
    {
        var query = (
            "\n"
            + "            binary Pkg {\n"
            + "                Count: byte,\n"
            + "                Vals: short le[Count]\n"
            + "            };\n"
            + "            select Sum(v.Value) as Total\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Pkg>(f.Content) p\n"
            + "            cross apply p.Vals v"
        );

        // File 1: Count=2, Vals=[10, 20]
        using var ms1 = new MemoryStream();
        ms1.WriteByte(2);
        ms1.Write(BitConverter.GetBytes((short)10));
        ms1.Write(BitConverter.GetBytes((short)20));

        // File 2: Count=3, Vals=[30, 40, 50]
        using var ms2 = new MemoryStream();
        ms2.WriteByte(3);
        ms2.Write(BitConverter.GetBytes((short)30));
        ms2.Write(BitConverter.GetBytes((short)40));
        ms2.Write(BitConverter.GetBytes((short)50));

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "b.bin", Content = ms2.ToArray() },
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
        Assert.AreEqual((short)150, table[0].ReadValue(0)); // 10+20+30+40+50
    }

    /// <summary>
    ///     Schema with two separate arrays — cross apply each independently.
    /// </summary>
    [TestMethod]
    public void R2_Binary_TwoArrayFields_IndependentCrossApply()
    {
        var query = (
            "\n"
            + "            binary Rec {\n"
            + "                ACount: byte,\n"
            + "                Vals: short le[ACount],\n"
            + "                BCount: byte,\n"
            + "                Bs: int le[BCount]\n"
            + "            };\n"
            + "            select v.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Rec>(f.Content) r\n"
            + "            cross apply r.Vals v\n"
            + "            order by v.Value asc"
        );

        using var ms = new MemoryStream();
        ms.WriteByte(3); // ACount=3
        ms.Write(BitConverter.GetBytes((short)30));
        ms.Write(BitConverter.GetBytes((short)10));
        ms.Write(BitConverter.GetBytes((short)20));
        ms.WriteByte(2); // BCount=2
        ms.Write(BitConverter.GetBytes(100));
        ms.Write(BitConverter.GetBytes(200));

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = ms.ToArray() },
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

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((short)10, table[0].ReadValue(0));
        Assert.AreEqual((short)20, table[1].ReadValue(0));
        Assert.AreEqual((short)30, table[2].ReadValue(0));
    }

    #endregion
}
