using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSubstreamTests
{
    [TestMethod]
    [TestProperty("CandidateId", "P2-024")]
    public void ExactSubstream_ShouldRetainPrimitiveTypeAndResumeAtTrailer()
    {
        const string query = "binary PayloadBody { Value: short le }; binary Packet { Length: byte, Payload: substream[Length] as PayloadBody exact, Trailer: byte }; select p.Payload.Value as Value, p.Trailer as Trailer from #test.Entities() source cross apply Interpret<Packet>(source.Content) p";
        var entities = new[]
        {
            new BinaryEntity { Name = "substream-exact", Content = [2, 52, 18, 127] }
        };
        var sources = new Dictionary<string, IEnumerable<BinaryEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), new BinarySchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("Value", typeof(short)),
            ("Trailer", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { (short)4660, (byte)127 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0011")]
    public void ExactAndLaxSubstreams_ShouldPreserveNestedByteMetadata()
    {
        const string query = "binary Inner {\r\n    Value: byte\r\n}\r\nbinary Packet {\r\n    ExactLength: byte,\r\n    ExactPayload: substream[ExactLength] as Inner exact,\r\n    LaxLength: byte,\r\n    LaxPayload: substream[LaxLength] as Inner lax,\r\n    Trailer: byte\r\n}\r\nselect e.Name as PacketName, p.ExactPayload.Value as ExactValue, p.LaxPayload.Value as LaxValue, p.Trailer as Trailer\r\nfrom #test.entities() e\r\ncross apply Interpret<Packet>(e.Content) p";
        var entities = new[]
        {
            new BinaryEntity { Name = "long-lax", Content = [1, 17, 3, 34, 51, 68, 238] },
            new BinaryEntity { Name = "single", Content = [1, 122, 1, 196, 85] }
        };
        var sources = new Dictionary<string, IEnumerable<BinaryEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), new BinarySchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("PacketName", typeof(string)),
            ("ExactValue", typeof(byte)),
            ("LaxValue", typeof(byte)),
            ("Trailer", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "long-lax", (byte)17, (byte)34, (byte)238 },
            new object?[] { "single", (byte)122, (byte)196, (byte)85 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-SubstreamNestedFieldRetainsBinaryPrimitiveType")]
    public void SubstreamNestedFieldRetainsBinaryPrimitiveType()
    {
        const string query = "binary PayloadBody { Value: short le }; binary Packet { Length: byte, Payload: substream[Length] as PayloadBody }; select p.Payload.Value from #test.Entities() source cross apply Interpret<Packet>(source.Content) p";

        var sources = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<Musoq.Evaluator.Tests.Components.BinaryEntity>>
        {
            ["#test"] = new[]
            {
                new Musoq.Evaluator.Tests.Components.BinaryEntity
                {
                    Name = "substream-minimal",
                    Content = System.Convert.FromHexString("023412")
                }
            }
        };
        using var compiled = CompileGeneratedQuery(
            query,
            System.Guid.NewGuid().ToString(),
            new Musoq.Evaluator.Tests.Components.BinarySchemaProvider(sources),
            LoggerResolver,
            TestCompilationOptions);
        using var result = TableMaterializationTestHelper.Materialize(compiled.Run());

        var columns = System.Linq.Enumerable.ToArray(result.Columns);
        Assert.AreEqual(1, columns.Length);
        Assert.AreEqual(typeof(short), columns[0].ColumnType);
        TableMaterializationTestHelper.AssertRowsUnordered(result, [new object?[] { (short)0x1234 }]);
    }
}
