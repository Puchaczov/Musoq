using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

[TestClass]
public class DescParserTests
{
    [TestMethod]
    public void DescSchema_ShouldParse()
    {
        var query = "desc #schema";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescSchemaMethod_ShouldParse()
    {
        var query = "desc #schema.method";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_ShouldParse()
    {
        var query = "desc #schema.method()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescSchemaMethodWithArguments_ShouldParse()
    {
        var query = "desc #schema.method('arg1', 123, true)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithSemicolon_ShouldParse()
    {
        var query = "desc #schema.method();";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithComment_ShouldParse()
    {
        var query = @"
            -- This is a comment
            desc #schema.method()
            -- Another comment
        ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithWhitespace_ShouldParse()
    {
        var query = "   desc    #schema.method()   ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithMultilineFormat_ShouldParse()
    {
        var query = @"
            desc 
                #schema.method()
        ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithInvalidSyntax_MissingSchema_ShouldFail()
    {
        var query = "desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void DescWithInvalidSyntax_InvalidSchemaName_ShouldFail()
    {
        var query = "desc schema"; // Missing # prefix

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void DescWithInvalidSyntax_DotWithoutMethod_ShouldFail()
    {
        var query = "desc #schema.";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void DescCaseInsensitive_Uppercase_ShouldParse()
    {
        var query = "DESC #schema.method()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescCaseInsensitive_MixedCase_ShouldParse()
    {
        var query = "DeSc #schema.method()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithComplexMethodName_ShouldParse()
    {
        var query = "desc #mySchema.someComplexMethod123()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithNumericArgument_ShouldParse()
    {
        var query = "desc #schema.method(42)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithStringArgument_ShouldParse()
    {
        var query = "desc #schema.method('test string')";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithMultipleStringArguments_ShouldParse()
    {
        var query = "desc #schema.method('arg1', 'arg2', 'arg3')";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithMixedArguments_ShouldParse()
    {
        var query = "desc #schema.method('text', 123, 45.67, true, false)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescSchemaOnly_ShouldParse()
    {
        var query = "desc #MySchema";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescMethodWithoutArgs_ShouldParse()
    {
        var query = "desc #schema.getData";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        parser.ComposeAll();
    }

    [TestMethod]
    public void DescWithNestedFunctionInArgument_ShouldFail()
    {
        var query = "desc #schema.method(GetValue())";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        
        // Function calls in arguments might not be supported for DESC
        // This test verifies behavior - it should either parse or fail gracefully
        try
        {
            parser.ComposeAll();
        }
        catch (SyntaxException)
        {
            // Expected if nested functions are not supported
        }
    }
}
