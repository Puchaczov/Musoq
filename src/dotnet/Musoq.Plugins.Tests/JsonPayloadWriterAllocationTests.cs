using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class JsonPayloadWriterAllocationTests
{
    [TestMethod]
    public void ConcreteNumericWriting_WhenWarmed_ShouldAllocateNoBoxesOrInvocationObjects()
    {
        long allocated = -1;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var stream = new MemoryStream(512);
                using var writer = new Utf8JsonWriter(stream);
                for (var index = 0; index < 10_000; index++)
                    WriteNumbers(writer, stream);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var index = 0; index < 10_000; index++)
                    WriteNumbers(writer, stream);
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual("[42,1.25,9]", System.Text.Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length));
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        using (ExecutionContext.SuppressFlow()) thread.Start();
        thread.Join();
        Assert.IsNull(failure);
        Assert.AreEqual(0L, allocated);
    }

    [TestMethod]
    public void NullableNumericToJson_ShouldAllocateOnlyTheStandardSerializedResult()
    {
        var library = new LibraryBase();
        decimal? amount = 1.25m;
        ulong? count = 9;
        for (var index = 0; index < 10_000; index++)
        {
            _ = JsonSerializer.Serialize(amount);
            _ = JsonSerializer.Serialize(count);
            _ = library.ToJson(amount);
            _ = library.ToJson(count);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            _ = JsonSerializer.Serialize(amount);
            _ = JsonSerializer.Serialize(count);
        }
        var standard = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            _ = library.ToJson(amount);
            _ = library.ToJson(count);
        }
        var actual = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(standard, actual);
        Assert.AreEqual("1.25", library.ToJson(amount));
        Assert.AreEqual("9", library.ToJson(count));
    }

    private static void WriteNumbers(Utf8JsonWriter writer, MemoryStream stream)
    {
        stream.SetLength(0);
        stream.Position = 0;
        writer.Reset(stream);
        writer.WriteStartArray();
        var integer = 42;
        decimal? amount = 1.25m;
        ulong? count = 9;
        JsonPayloadWriter.Write(writer, in integer);
        JsonPayloadWriter.Write(writer, in amount);
        JsonPayloadWriter.Write(writer, in count);
        writer.WriteEndArray();
        writer.Flush();
    }
}
