using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;


[TestClass]
public class DescParserSchemaAndMethodTests : DescParserTestBase
{
    [TestMethod]
    public void DescSchema_ShouldParse()
    {
        var desc = ParseDescNode("desc #schema");
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.Schema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
        Assert.HasCount(0, source.Parameters.Args);
    }

    [TestMethod]
    public void DescSchemaMethod_ShouldParse()
    {
        var desc = ParseDescNode("desc #schema.method");
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.Constructors, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
        Assert.HasCount(0, source.Parameters.Args);
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_ShouldParse()
    {
        var desc = ParseDescNode("desc #schema.method()");
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
        Assert.HasCount(0, source.Parameters.Args);
    }

    [TestMethod]
    public void DescSchemaMethodWithArguments_ShouldParse()
    {
        var desc = ParseDescNode("desc #schema.method('arg1', 123, true)");
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
        Assert.HasCount(3, source.Parameters.Args);
        Assert.AreEqual("'arg1', 123, true", source.Parameters.ToString());
    }

    [TestMethod]
    public void DescWithSemicolon_ShouldParse()
    {
        var desc = ParseDescNode("desc #schema.method();");
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
    }

    [TestMethod]
    public void DescWithComment_ShouldParse()
    {
        var query = @"
            -- This is a comment
            desc #schema.method()
            -- Another comment
        ";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
    }

    [TestMethod]
    public void DescWithWhitespace_ShouldParse()
    {
        var query = "   desc    #schema.method()   ";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
    }

    [TestMethod]
    public void DescWithMultilineFormat_ShouldParse()
    {
        var query = @"
            desc
                #schema.method()
        ";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
    }

    [TestMethod]
    public void DescWithInvalidSyntax_MissingSchema_ShouldFail()
    {
        var query = "desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void DescWithInvalidSyntax_InvalidSchemaName_ShouldFail()
    {
        var query = "desc 123invalid";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void DescWithoutHash_ShouldSucceed()
    {
        var query = "desc schema";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.Schema, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
        Assert.HasCount(0, source.Parameters.Args);
    }

    [TestMethod]
    public void DescWithInvalidSyntax_DotWithoutMethod_ShouldFail()
    {
        var query = "desc #schema.";

        var parser = new Parser(new Lexer(query, true));

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }

    [TestMethod]
    public void DescCaseInsensitive_Uppercase_ShouldParse()
    {
        var query = "DESC #schema.method()";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
    }

    [TestMethod]
    public void DescCaseInsensitive_MixedCase_ShouldParse()
    {
        var query = "DeSc #schema.method()";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
    }

    [TestMethod]
    public void DescWithComplexMethodName_ShouldParse()
    {
        var query = "desc #mySchema.someComplexMethod123()";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#mySchema", source.Schema);
        Assert.AreEqual("someComplexMethod123", source.Method);
    }

    [TestMethod]
    public void DescWithNumericArgument_ShouldParse()
    {
        var query = "desc #schema.method(42)";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
        Assert.HasCount(1, source.Parameters.Args);
        Assert.AreEqual("42", source.Parameters.ToString());
    }

    [TestMethod]
    public void DescWithStringArgument_ShouldParse()
    {
        var query = "desc #schema.method('test string')";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
        Assert.HasCount(1, source.Parameters.Args);
        Assert.AreEqual("'test string'", source.Parameters.ToString());
    }

    [TestMethod]
    public void DescWithMultipleStringArguments_ShouldParse()
    {
        var query = "desc #schema.method('arg1', 'arg2', 'arg3')";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
        Assert.HasCount(3, source.Parameters.Args);
        Assert.AreEqual("'arg1', 'arg2', 'arg3'", source.Parameters.ToString());
    }

    [TestMethod]
    public void DescWithMixedArguments_ShouldParse()
    {
        var query = "desc #schema.method('text', 123, 45.67, true, false)";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
        Assert.HasCount(5, source.Parameters.Args);
        Assert.AreEqual("'text', 123, 45.67, true, false", source.Parameters.ToString());
    }

    [TestMethod]
    public void DescSchemaOnly_ShouldParse()
    {
        var query = "desc #MySchema";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.Schema, desc.Type);
        Assert.AreEqual("#MySchema", source.Schema);
        Assert.AreEqual(string.Empty, source.Method);
    }

    [TestMethod]
    public void DescMethodWithoutArgs_ShouldParse()
    {
        var query = "desc #schema.getData";

        var desc = ParseDescNode(query);
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.Constructors, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("getData", source.Method);
        Assert.HasCount(0, source.Parameters.Args);
    }

    [TestMethod]
    public void DescWithNestedFunctionInArgument_ShouldParseAsArgument()
    {
        var desc = ParseDescNode("desc #schema.method(GetValue())");
        var source = GetSchemaFromNode(desc);

        Assert.AreEqual(DescForType.SpecificConstructor, desc.Type);
        Assert.AreEqual("#schema", source.Schema);
        Assert.AreEqual("method", source.Method);
        Assert.HasCount(1, source.Parameters.Args);
        Assert.AreEqual("GetValue()", source.Parameters.ToString());
    }

}
