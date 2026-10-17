using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Tests;

[TestClass]
public sealed class ArrayProjectionComparerTests
{
    private static CompiledQuery Compile() => InstanceCreator.CompileForExecution(
        "binary Values { Raw: byte[2] }; select r.Raw.Length, r.Raw[0], r.Raw[1] from #fixture.bytes() f cross apply Interpret<Values>(f.Content) r",
        "ArrayComparer", new BinaryFixtureSource([1, 2]), new FixtureLogger());

    [TestMethod]
    public void Reversed_element_mapping_cannot_pass_the_original_array_assertion()
    {
        using var assertion = JsonDocument.Parse("{\"expected\":{\"kind\":\"array\",\"value\":[{\"kind\":\"integer\",\"value\":\"1\"},{\"kind\":\"integer\",\"value\":\"2\"}]},\"resultColumns\":{\"length\":0,\"elements\":[2,1]}}");
        using var query = Compile();
        using var result = query.Run();
        var actual = AssertionValues.Read(assertion.RootElement, result[0], 0);
        Assert.Throws<AssertFailedException>(() => AssertionComparer.Compare(assertion.RootElement, actual, "reversed"));
    }

    [TestMethod]
    public void Extra_decoded_elements_cannot_be_hidden_by_a_short_mapping()
    {
        using var assertion = JsonDocument.Parse("{\"resultColumns\":{\"length\":0,\"elements\":[1]}}");
        using var query = Compile();
        using var result = query.Run();
        Assert.Throws<AssertFailedException>(() => AssertionValues.Read(assertion.RootElement, result[0], 0));
    }
}
