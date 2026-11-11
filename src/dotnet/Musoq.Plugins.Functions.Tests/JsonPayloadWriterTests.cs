using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class JsonPayloadWriterTests
{
    [TestMethod]
    public void ConcreteNumericWriting_ShouldSerializeTypedValues()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        var integer = 42;
        decimal? amount = 1.25m;
        ulong? count = 9;
        JsonPayloadWriter.Write(writer, in integer);
        JsonPayloadWriter.Write(writer, in amount);
        JsonPayloadWriter.Write(writer, in count);
        writer.WriteEndArray();
        writer.Flush();

        Assert.AreEqual("[42,1.25,9]", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [TestMethod]
    public void NullableNumericToJson_ShouldSerializePresentValues()
    {
        var library = new LibraryBase();
        decimal? amount = 1.25m;
        ulong? count = 9;

        Assert.AreEqual("1.25", library.ToJson(amount));
        Assert.AreEqual("9", library.ToJson(count));
    }
}
