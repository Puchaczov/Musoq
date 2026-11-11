using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_SelectInterpret_BinaryWithTextPayload_AsClause_ShouldChainParsing()
    {
        // Arrange: Binary container with embedded text that gets parsed
        // Note: 'until' consumes the delimiter, so no 'literal' needed after
        var query = (
            "\n"
            + "            text KeyValue {\n"
            + "                Key: until ':',\n"
            + "                Value: rest trim\n"
            + "            };\n"
            + "            binary ConfigPacket {\n"
            + "                Version: byte,\n"
            + "                Config: string[20] utf8 as KeyValue,\n"
            + "                Checksum: byte\n"
            + "            };\n"
            + "            select\n"
            + "                p.Version,\n"
            + "                p.Config.Key,\n"
            + "                p.Config.Value,\n"
            + "                p.Checksum\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<ConfigPacket>(f.Content) p"
        );

        // Build packet: Version=1, Config="host:localhost      " (20 bytes), Checksum=0xFF
        var testData = new byte[22];
        testData[0] = 1;
        var configText = "host:localhost".PadRight(20);
        Encoding.UTF8.GetBytes(configText).CopyTo(testData, 1);
        testData[21] = 0xFF;

        var entities = new[]
        {
            new BinaryEntity { Name = "config.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Version", typeof(byte)),
            ("p.Config.Key", typeof(string)),
            ("p.Config.Value", typeof(string)),
            ("Checksum", typeof(byte))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, "host", "localhost", (byte)0xFF]);
    }
}
