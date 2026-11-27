using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretationIdentifierTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("SchemaName")]
    [DataRow("InterpretAt")]
    [DataRow("IsFieldComplete")]
    [DataRow("WriteJson")]
    [DataRow("__completedFieldNames")]
    [DataRow("_parsedValue")]
    [DataRow("Rec")]
    [DataRow("Field")]
    public void BinaryField_ShouldBeIndependentOfGeneratedInterpreterNames(string field)
    {
        var sql = $$"""
            binary Rec { {{field}}: byte };
            let b: byte[] = array { 41ub };
            select r.{{field}} from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)41]);
    }

    [TestMethod]
    [DataRow("SchemaName")]
    [DataRow("ParseAt")]
    [DataRow("ParsePosition")]
    [DataRow("IsFieldComplete")]
    [DataRow("WriteJson")]
    [DataRow("__completedFieldNames")]
    [DataRow("_parsedValue")]
    [DataRow("Rec")]
    [DataRow("Field")]
    public void TextField_ShouldBeIndependentOfGeneratedInterpreterNames(string field)
    {
        var sql = $$"""
            text Rec { {{field}}: rest };
            select r.{{field}} from values { (S: 'X') } s cross apply Parse<Rec>(s.S) r
            """;

        AssertQueryRows(sql, ["X"]);
    }

    [TestMethod]
    [DataRow("CaptureResult_C", "V", "Tail")]
    [DataRow("Rec", "CaptureResult_C", "Tail")]
    [DataRow("Rec", "V", "CaptureResult_C")]
    [DataRow("Rec", "V", "Tail")]
    public void CaptureResult_ShouldAllowAuthoredNamesAcrossAllThreeScopes(string schema, string group, string tail)
    {
        var sql = $$"""
            text {{schema}} { C: pattern '(?<{{group}}>.)' capture ({{group}}), {{tail}}: rest };
            select r.C.{{group}}, r.{{tail}} from values { (S: 'X!') } s cross apply Parse<{{schema}}>(s.S) r
            """;

        AssertQueryRows(sql, ["X", "!"]);
    }

    [TestMethod]
    [DataRow("Child", "ParentContext")]
    [DataRow("Child", "__parentContext")]
    [DataRow("ParentContext", "Data")]
    [DataRow("Child", "Data")]
    public void ParentContext_ShouldCoexistWithAuthoredFieldAndTypeNames(string schema, string field)
    {
        var sql = $$"""
            binary {{schema}} { {{field}}: byte, V: = parent.Tag };
            binary Rec { Tag: byte, C: {{schema}} };
            let b: byte[] = array { 33ub, 42ub };
            select r.C.{{field}}, r.C.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)42, (byte)33]);
    }

    [TestMethod]
    public void NestedKeywordNamedField_ShouldBeAvailableInAComputedExpression()
    {
        const string sql = """
            binary Child { class: byte };
            binary Rec { Body: Child, V: = Body.class };
            let b: byte[] = array { 41ub };
            select r.Body.class, r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)41, (byte)41]);
    }

    [TestMethod]
    public void RepeatedKeywordNamedTextSchema_ShouldRetainItsMemberInApply()
    {
        const string sql = """
            text class { V: chars[1] };
            text Rec { Rows: repeat class until end };
            select e.V from values { (S: 'XY') } s cross apply Parse<Rec>(s.S) r cross apply r.Rows e
            """;

        AssertQueryRows(sql, ["X"], ["Y"]);
    }

    [TestMethod]
    public void Substream_ShouldUseTheCursorWhenAFieldIsNamedParsePosition()
    {
        const string sql = """
            binary Child { V: byte };
            binary Rec { ParsePosition: byte, B: substream[1] as Child };
            let b: byte[] = array { 41ub, 126ub };
            select r.ParsedFields.ParsePosition, r.ParsedFields.B.V, r.BytesConsumed
            from values { (N: 0) } s cross apply PartialInterpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)41, (byte)126, 2]);
    }

    [TestMethod]
    [DataRow("SchemaName")]
    [DataRow("WriteJson")]
    [DataRow("InterpretAt")]
    [DataRow("class")]
    public void BinarySchemaName_ShouldBeUsableAsAnInterpretationType(string schema)
    {
        var sql = $$"""
            binary {{schema}} { V: byte };
            let b: byte[] = array { 41ub };
            select r.V from values { (N: 0) } s cross apply Interpret<{{schema}}>($b) r
            """;

        AssertQueryRows(sql, [(byte)41]);
    }

    [TestMethod]
    [DataRow("SchemaName")]
    [DataRow("WriteJson")]
    [DataRow("ParseAt")]
    [DataRow("class")]
    public void TextSchemaName_ShouldBeUsableAsAnInterpretationType(string schema)
    {
        var sql = $$"""
            text {{schema}} { V: rest };
            select r.V from values { (S: 'X') } s cross apply Parse<{{schema}}>(s.S) r
            """;

        AssertQueryRows(sql, ["X"]);
    }
}
