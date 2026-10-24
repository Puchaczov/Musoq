using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class JsonPayloadConverterTests
{
    [TestMethod]
    public void NestedPayloadArrays_ShouldUseDeclaredPayloadWriter()
    {
        var payloads = new[] { new[] { new Payload<int>() }, new[] { new Payload<int>() } };
        Assert.AreEqual("[[{\"Value\":7}],[{\"Value\":7}]]", JsonSerializer.Serialize(payloads));
    }

    [JsonConverter(typeof(JsonPayloadConverterFactory))]
    public sealed class Payload<T> : IJsonPayload
    {
        public int InterpreterState => 99;
        public void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Value", 7);
            writer.WriteEndObject();
        }
    }
}
