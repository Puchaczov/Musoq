using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_SelectInterpret_BinaryWithTextPayload_AsClause_ShouldChainParsing()
    {
        // Arrange: Binary container with embedded text that gets parsed
        // Note: 'until' consumes the delimiter, so no 'literal' needed after
        var query = @"
            text KeyValue {
                Key: until ':',
                Value: rest trim
            };
            binary ConfigPacket {
                Version: byte,
                Config: string[20] utf8 as KeyValue,
                Checksum: byte
            };
            select
                p.Version,
                p.Config.Key,
                p.Config.Value,
                p.Checksum
            from #test.files() f
            cross apply Interpret<ConfigPacket>(f.Content) p";

        // Build packet: Version=1, Config="host:localhost      " (20 bytes), Checksum=0xFF
        var testData = new byte[22];
        testData[0] = 1;
        var configText = "host:localhost".PadRight(20);
        Encoding.UTF8.GetBytes(configText).CopyTo(testData, 1);
        testData[21] = 0xFF;

        var entities = new[] { new BinaryEntity { Name = "config.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Version", typeof(byte)),
            ("p.Config.Key", typeof(string)),
            ("p.Config.Value", typeof(string)),
            ("p.Checksum", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, "host", "localhost", (byte)0xFF]);
    }


}
