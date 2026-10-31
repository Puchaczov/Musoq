using System.Text.Json;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

internal static class ParsingFailureAssertion
{
    internal static void Compare(Action execute, JsonElement failure, string id)
    {
        var exception = Assert.Throws<ParseException>(execute);
        Assert.AreEqual(Enum.Parse<ParseErrorCode>(failure.GetProperty("code").GetString()!), exception.ErrorCode, id);
        Assert.AreEqual(failure.GetProperty("field").GetString(), exception.FieldName, id);
    }
}
