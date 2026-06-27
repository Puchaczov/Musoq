using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Real-world-format interpretation tests over tiny hand-authored byte fixtures.
///     Each fixture is intentionally minimal but recognizable: enough structure to test
///     Musoq parsing behavior, not full file-format compliance.
/// </summary>
[TestClass]
public partial class BinaryRealWorldFormatTests : BinaryOrTextualEvaluatorTestBase
{
    /// <summary>
    ///     Test-local fixture builder for endian-aware writes and byte concatenation.
    /// </summary>
    private sealed class ByteWriter
    {
        private readonly List<byte> _bytes = [];

        public ByteWriter U8(int value)
        {
            _bytes.Add((byte)value);
            return this;
        }

        public ByteWriter Raw(params byte[] values)
        {
            _bytes.AddRange(values);
            return this;
        }

        public ByteWriter U16Le(int value)
        {
            _bytes.Add((byte)(value & 0xFF));
            _bytes.Add((byte)((value >> 8) & 0xFF));
            return this;
        }

        public ByteWriter U16Be(int value)
        {
            _bytes.Add((byte)((value >> 8) & 0xFF));
            _bytes.Add((byte)(value & 0xFF));
            return this;
        }

        public ByteWriter U32Le(long value)
        {
            _bytes.Add((byte)(value & 0xFF));
            _bytes.Add((byte)((value >> 8) & 0xFF));
            _bytes.Add((byte)((value >> 16) & 0xFF));
            _bytes.Add((byte)((value >> 24) & 0xFF));
            return this;
        }

        public ByteWriter U32Be(long value)
        {
            _bytes.Add((byte)((value >> 24) & 0xFF));
            _bytes.Add((byte)((value >> 16) & 0xFF));
            _bytes.Add((byte)((value >> 8) & 0xFF));
            _bytes.Add((byte)(value & 0xFF));
            return this;
        }

        public ByteWriter Ascii(string value)
        {
            _bytes.AddRange(Encoding.ASCII.GetBytes(value));
            return this;
        }

        public ByteWriter Utf8(string value)
        {
            _bytes.AddRange(Encoding.UTF8.GetBytes(value));
            return this;
        }

        public byte[] ToArray()
        {
            return _bytes.ToArray();
        }
    }

    private static ByteWriter Bytes()
    {
        return new ByteWriter();
    }

    private Table RunQuery(string query, byte[] content)
    {
        return RunQuery(query, new BinaryEntity { Name = "fixture.bin", Content = content });
    }

    private Table RunQuery(string query, params BinaryEntity[] entities)
    {
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        return TableMaterializationTestHelper.Materialize(vm.Run(CancellationToken.None));
    }
}
