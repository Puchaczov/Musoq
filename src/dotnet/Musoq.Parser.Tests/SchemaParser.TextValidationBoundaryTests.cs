using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SchemaTextValidationBoundaryTests : SchemaParserTestsBase
{
    [TestMethod]
    [DataRow("repeat until ';' nested")]
    [DataRow("repeat repeat between '[' ']' nested escaped")]
    [DataRow("repeat rest greedy")]
    [DataRow("Child trim")]
    [DataRow("repeat Child upper")]
    public void InvalidModifiers_ShouldFailRecursively(string type)
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema($"text Invalid {{ Value: {type} }}"));
        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
    }

    [TestMethod]
    [DataRow("optional pattern '[bad'")]
    [DataRow("repeat optional pattern '(?=a)'")]
    [DataRow("optional pattern '(?<Name>a)' capture (name)")]
    [DataRow("repeat pattern '(?<Name>a)' capture (Name, Name)")]
    public void InvalidPatterns_ShouldFailBeforeAnyInput(string type)
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema($"text Unused {{ Value: {type} }}"));
        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
    }
}
