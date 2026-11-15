using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SchemaNullPredicateTests : SchemaParserTestsBase
{
    [TestMethod]
    [DataRow("IS NULL", false)]
    [DataRow("IS NOT NULL", true)]
    public void SchemaNullPredicate_ShouldRetainOperandAndNegation(string predicate, bool negated)
    {
        var schema = ParseBinarySchema(
            $"binary Rec {{ Flag: byte, Optional: byte when Flag = 1, Present: Optional {predicate} }}"
        );

        var field = Assert.IsInstanceOfType<ComputedFieldNode>(schema.Fields[2]);
        var expression = Assert.IsInstanceOfType<IsNullNode>(field.Expression);

        Assert.AreEqual(negated, expression.IsNegated);
        Assert.AreEqual("Optional", Assert.IsInstanceOfType<IdentifierNode>(expression.Expression).Name);
    }

    [TestMethod]
    public void SchemaKeywordFieldFollowedByIs_ShouldRemainAFieldReference()
    {
        var schema = ParseBinarySchema("binary Rec { Case: byte, Present: Case IS NOT NULL }");

        var field = Assert.IsInstanceOfType<ComputedFieldNode>(schema.Fields[1]);
        var predicate = Assert.IsInstanceOfType<IsNullNode>(field.Expression);

        Assert.AreEqual("case", Assert.IsInstanceOfType<IdentifierNode>(predicate.Expression).Name);
    }

    [TestMethod]
    [DataRow("Flag IS")]
    [DataRow("Flag IS NOT")]
    [DataRow("Flag IS true")]
    public void IncompleteSchemaNullPredicate_ShouldExpectNull(string expression)
    {
        var error = Assert.Throws<SyntaxException>(() =>
            ParseBinarySchema($"binary Rec {{ Flag: byte, Missing: {expression} }}")
        );

        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, error.Code);
        StringAssert.Contains(error.Message, "Null");
    }
}
