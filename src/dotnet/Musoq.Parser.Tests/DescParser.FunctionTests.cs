using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;


[TestClass]
public class DescParserFunctionTests : DescParserTestBase
{
    [TestMethod]
    public void DescFunctionsSchema_ShouldParse()
    {
        var desc = ParseDescNode("desc functions #schema");
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchema_WithSemicolon_ShouldParse()
    {
        var query = "desc functions #schema;";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchema_CaseInsensitive_ShouldParse()
    {
        var query = "DESC FUNCTIONS #Schema";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#Schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchema_MixedCase_ShouldParse()
    {
        var query = "DeSc FuNcTiOnS #MySchema";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#MySchema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchema_WithWhitespace_ShouldParse()
    {
        var query = "   desc    functions    #schema   ";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchema_WithComment_ShouldParse()
    {
        var query = @"
            -- This is a comment
            desc functions #schema
            -- Another comment
        ";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchema_WithMultilineFormat_ShouldParse()
    {
        var query = @"
            desc
                functions
                    #schema
        ";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchema_WithComplexSchemaName_ShouldParse()
    {
        var query = "desc functions #myComplexSchema123";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#myComplexSchema123", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctions_WithoutSchema_ShouldFail()
    {
        var query = "desc functions";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void DescFunctions_InvalidSchemaName_ShouldFail()
    {
        var query = "desc functions 123invalid";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void DescFunctionsWithoutHash_ShouldSucceed()
    {
        var query = "desc functions schema";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchemaMethod_ShouldParse()
    {
        var query = "desc functions #schema.method";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchemaMethodWithParentheses_ShouldParse()
    {
        var query = "desc functions #schema.method()";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchemaMethodWithArguments_ShouldParse()
    {
        var query = "desc functions #schema.method('arg1', 123, true)";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchemaMethodWithSemicolon_ShouldParse()
    {
        var query = "desc functions #schema.method();";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchemaMethod_CaseInsensitive_ShouldParse()
    {
        var query = "DESC FUNCTIONS #Schema.Method";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.FunctionsForSchema, desc.Type);
        Assert.AreEqual("#Schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescFunctionsSchemaWithMethodAccess_ShouldProduceFunctionsForSchema()
    {
        var query = "desc functions #schema.method()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.FunctionsForSchema, descNode.Type);

        var schemaFromNode = (SchemaFromNode)descNode.From;
        Assert.AreEqual("#schema", schemaFromNode.Schema);
        Assert.AreEqual(string.Empty, schemaFromNode.Method,
            "desc functions should ignore the .method() part and behave like desc functions schema");
    }

    [TestMethod]
    public void DescFunctionsSchemaWithMethodAccessNoParens_ShouldProduceFunctionsForSchema()
    {
        var query = "desc functions #schema.method";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.FunctionsForSchema, descNode.Type);

        var schemaFromNode = (SchemaFromNode)descNode.From;
        Assert.AreEqual("#schema", schemaFromNode.Schema);
        Assert.AreEqual(string.Empty, schemaFromNode.Method,
            "desc functions should ignore the .method part and behave like desc functions schema");
    }

    [TestMethod]
    public void DescFunctionsSchemaWithMethodAccessAndArgs_ShouldProduceFunctionsForSchema()
    {
        var query = "desc functions #schema.method('arg1', 123)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.FunctionsForSchema, descNode.Type);

        var schemaFromNode = (SchemaFromNode)descNode.From;
        Assert.AreEqual("#schema", schemaFromNode.Schema);
        Assert.AreEqual(string.Empty, schemaFromNode.Method,
            "desc functions should ignore the .method('arg1', 123) part and behave like desc functions schema");
    }

    [TestMethod]
    public void DescFunctionsSchemaWithoutHash_MethodAccess_ShouldProduceFunctionsForSchema()
    {
        var query = "desc functions schema.method()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.FunctionsForSchema, descNode.Type);

        var schemaFromNode = (SchemaFromNode)descNode.From;
        Assert.AreEqual("#schema", schemaFromNode.Schema);
        Assert.AreEqual(string.Empty, schemaFromNode.Method,
            "desc functions should ignore the .method() part and behave like desc functions schema");
    }

    [TestMethod]
    public void DescFunctionsSchema_ShouldProduceFunctionsForSchema_WithEmptyMethod()
    {
        var query = "desc functions #schema";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.FunctionsForSchema, descNode.Type);

        var schemaFromNode = (SchemaFromNode)descNode.From;
        Assert.AreEqual("#schema", schemaFromNode.Schema);
        Assert.AreEqual(string.Empty, schemaFromNode.Method, "desc functions schema should have empty method");
    }
}
