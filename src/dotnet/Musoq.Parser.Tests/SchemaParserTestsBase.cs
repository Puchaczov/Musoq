using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

public abstract class SchemaParserTestsBase
{
    protected static Node ParseSchema(string schema)
    {
        var lexer = new Lexer(schema, true);
        var parser = new SchemaParser(lexer);
        return parser.ParseSchema();
    }

    protected static BinarySchemaNode ParseBinarySchema(string schema)
    {
        var result = ParseSchema(schema);
        Assert.IsInstanceOfType(result, typeof(BinarySchemaNode));
        return (BinarySchemaNode)result;
    }

    protected static TextSchemaNode ParseTextSchema(string schema)
    {
        var result = ParseSchema(schema);
        Assert.IsInstanceOfType(result, typeof(TextSchemaNode));
        return (TextSchemaNode)result;
    }

    protected static TypeAnnotationNode ParseFieldType(string schema)
    {
        var result = ParseBinarySchema(schema);
        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field, "Expected FieldDefinitionNode");
        return field.TypeAnnotation;
    }
}
