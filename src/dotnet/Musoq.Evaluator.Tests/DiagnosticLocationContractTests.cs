using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticLocationContractTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void UnknownLocations_ShouldRemainUnknownThroughThePublicEnvelope(bool sourceContract)
    {
        IDiagnosticException exception = sourceContract
            ? new SourcePlanContractException("Missing member")
            : new QuerySyntaxException("Missing schema", null, DiagnosticCode.MQ4003_UndefinedSchemaReference);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(exception.ToDiagnostic());

        Assert.IsNull(envelope.Offset);
        Assert.IsNull(envelope.Length);
        Assert.IsNull(envelope.Line);
        Assert.AreEqual(sourceContract ? DiagnosticPhase.Bind : DiagnosticPhase.Schema, envelope.Phase);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RealInsertionPoints_ShouldRetainZeroOffsetAndLength(bool sourceContract)
    {
        IDiagnosticException exception = sourceContract
            ? new SourcePlanContractException("Missing member", new TextSpan(0, 0))
            : new QuerySyntaxException("Missing schema", new TextSpan(0, 0));
        var envelope = MusoqErrorEnvelope.FromDiagnostic(exception.ToDiagnostic());

        Assert.AreEqual(0, envelope.Offset);
        Assert.AreEqual(0, envelope.Length);
        Assert.AreEqual(1, envelope.Line);
    }

    [TestMethod]
    public void KnownLocations_ShouldUseTheSuppliedQueryText()
    {
        var exception = new QuerySyntaxException("Missing schema", new TextSpan(7, 0));
        var envelope = MusoqErrorEnvelope.FromDiagnostic(exception.ToDiagnostic(new SourceText("select\n")));

        Assert.AreEqual(7, envelope.Offset);
        Assert.AreEqual(0, envelope.Length);
        Assert.AreEqual(2, envelope.Line);
        Assert.AreEqual(1, envelope.Column);
    }

    [TestMethod]
    public void UnlocatedTextSchemaReference_ShouldNotInventAnInsertionPoint()
    {
        var visitor = new SchemaDefinitionVisitor(new SchemaRegistry());
        var schema = new TextSchemaNode("Container",
            [new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "Missing")]);
        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(schema));

        Assert.IsNull(exception.Span);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(exception.ToDiagnostic());
        Assert.IsNull(envelope.Offset);
        Assert.AreEqual(DiagnosticPhase.Schema, envelope.Phase);
    }
}
