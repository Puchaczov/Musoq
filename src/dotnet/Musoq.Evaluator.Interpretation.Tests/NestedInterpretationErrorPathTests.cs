using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class NestedInterpretationErrorPathTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("P", "Child", "P.P")]
    [DataRow("p", "Child", "P.p")]
    [DataRow("P", "substream[1] as Child", "P.P")]
    [DataRow("Q", "Child", "P.Q")]
    public void PartialBinaryFailure_ShouldIncludeEachEmbeddingField(string inner, string type, string path)
    {
        var sql = $$"""
            binary Child { {{inner}}: int le };
            binary Rec { _: byte, P: {{type}} };
            let b: byte[] = array { 1ub, 2ub };
            select r.IsSuccess, r.ErrorField, r.BytesConsumed
            from values { (N: 0) } s cross apply PartialInterpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [false, path, 1]);
    }

    [TestMethod]
    public void PartialTextFailure_ShouldIncludeBothFieldsWithTheSameName()
    {
        const string sql = """
            text Child { P: literal '!' };
            text Rec { P: Child };
            select r.IsSuccess, r.ErrorField, r.BytesConsumed from values { (S: '?') } s cross apply PartialParse<Rec>(s.S) r
            """;

        AssertQueryRows(sql, [false, "P.P", 0]);
    }
}
