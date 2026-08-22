using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_InterpretComposedBinaryAndText_WhenCompiledRepeatedly_ShouldReturnIsolatedEquivalentResults()
    {
        const string query = @"
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

        var data = new byte[22];
        data[0] = 1;
        Encoding.UTF8.GetBytes("host:localhost".PadRight(20)).CopyTo(data, 1);
        data[21] = 0xFF;
        var provider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] = [new BinaryEntity { Name = "config.bin", Content = data }]
            });

        for (var compilation = 0; compilation < 3; compilation++)
        {
            var compiled = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                provider,
                LoggerResolver,
                TestCompilationOptions);

            var table = compiled.Run(CancellationToken.None);
            TableMaterializationTestHelper.AssertColumns(
                table,
                ("p.Version", typeof(byte)),
                ("p.Config.Key", typeof(string)),
                ("p.Config.Value", typeof(string)),
                ("p.Checksum", typeof(byte)));
            TableMaterializationTestHelper.AssertRowsInOrder(
                table,
                [(byte)1, "host", "localhost", (byte)0xFF]);
        }
    }
}
