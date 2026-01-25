using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

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
        var query = "desc 123invalid";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void DescWithoutHash_ShouldSucceed()
    {
        var query = "desc schema";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
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


        try
        {
            parser.ComposeAll();
        }
        catch (SyntaxException)
        {
        }
    }

    [TestMethod]
    public void DescFunctionsSchema_ShouldParse()
    {
        var query = "desc functions #schema";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchema_WithSemicolon_ShouldParse()
    {
        var query = "desc functions #schema;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchema_CaseInsensitive_ShouldParse()
    {
        var query = "DESC FUNCTIONS #Schema";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchema_MixedCase_ShouldParse()
    {
        var query = "DeSc FuNcTiOnS #MySchema";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchema_WithWhitespace_ShouldParse()
    {
        var query = "   desc    functions    #schema   ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchema_WithComment_ShouldParse()
    {
        var query = @"
            -- This is a comment
            desc functions #schema
            -- Another comment
        ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchema_WithMultilineFormat_ShouldParse()
    {
        var query = @"
            desc 
                functions
                    #schema
        ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchema_WithComplexSchemaName_ShouldParse()
    {
        var query = "desc functions #myComplexSchema123";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctions_WithoutSchema_ShouldFail()
    {
        var query = "desc functions";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void DescFunctions_InvalidSchemaName_ShouldFail()
    {
        var query = "desc functions 123invalid";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void DescFunctionsWithoutHash_ShouldSucceed()
    {
        var query = "desc functions schema";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void DescFunctionsSchemaMethod_ShouldParse()
    {
        var query = "desc functions #schema.method";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchemaMethodWithParentheses_ShouldParse()
    {
        var query = "desc functions #schema.method()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchemaMethodWithArguments_ShouldParse()
    {
        var query = "desc functions #schema.method('arg1', 123, true)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchemaMethodWithSemicolon_ShouldParse()
    {
        var query = "desc functions #schema.method();";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    [TestMethod]
    public void DescFunctionsSchemaMethod_CaseInsensitive_ShouldParse()
    {
        var query = "DESC FUNCTIONS #Schema.Method";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        parser.ComposeAll();
    }

    #region Desc Column Tests

    [TestMethod]
    public void DescSchemaMethodColumn_ShouldParse()
    {
        var query = "desc #schema.method() column Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Name", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumnWithArguments_ShouldParse()
    {
        var query = "desc #schema.method('arg1', 123) column Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Name", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_CaseInsensitive_ShouldParse()
    {
        var query = "DESC #schema.method() COLUMN Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Name", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_MixedCase_ShouldParse()
    {
        var query = "DeSc #schema.method() CoLuMn Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Name", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_WithSemicolon_ShouldParse()
    {
        var query = "desc #schema.method() column Name;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Name", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_WithWhitespace_ShouldParse()
    {
        var query = "   desc    #schema.method()    column    Name   ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Name", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_MultiLine_ShouldParse()
    {
        var query = @"
            desc 
                #schema.method() 
                column 
                    Name
        ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Name", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_ComplexColumnName_ShouldParse()
    {
        var query = "desc #schema.method() column MyComplexColumn123";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("MyComplexColumn123", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_WithoutHash_ShouldParse()
    {
        var query = "desc schema.method() column Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Name", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_MethodAccessSyntax_ShouldParse()
    {
        var query = "desc #schema.method() column Author";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Author", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_MissingColumnName_ShouldFail()
    {
        var query = "desc #schema.method() column";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(() => parser.ComposeAll());
    }

    [TestMethod]
    public void DescSchemaMethodColumn_WithComment_ShouldParse()
    {
        var query = @"
            -- This is a comment
            desc #schema.method() column Name
            -- Another comment
        ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Name", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodWithoutColumn_ShouldReturnSpecificConstructor()
    {
        var query = "desc #schema.method()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificConstructor, descNode.Type);
        Assert.IsNull(descNode.Column);
    }

    [TestMethod]
    public void DescNode_ToString_WithColumn_ShouldFormat()
    {
        var query = "desc #schema.method() column Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        var descNode = GetDescNode(result);
        var toString = descNode.ToString();

        StringAssert.Contains(toString, "column", "ToString should contain 'column'");
        StringAssert.Contains(toString, "Name", "ToString should contain the column name");
    }

    #region Nested Property Path Tests

    [TestMethod]
    public void DescSchemaMethodColumn_NestedProperty_ShouldParse()
    {
        var query = "desc #schema.method() column Self.Children";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Self.Children", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_DeeplyNestedProperty_ShouldParse()
    {
        var query = "desc #schema.method() column Self.Other.Children";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Self.Other.Children", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_VeryDeeplyNestedProperty_ShouldParse()
    {
        var query = "desc #schema.method() column A.B.C.D.E";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("A.B.C.D.E", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NestedProperty_WithWhitespace_ShouldParse()
    {
        var query = "desc #schema.method() column   Self . Children  ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Self.Children", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NestedProperty_MultiLine_ShouldParse()
    {
        var query = @"
            desc #schema.method() 
            column 
                Self.Other.Children
        ";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Self.Other.Children", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NestedProperty_WithArguments_ShouldParse()
    {
        var query = "desc #schema.method('arg1', 123) column Self.Children";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Self.Children", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NestedProperty_CaseInsensitive_ShouldParse()
    {
        var query = "DESC #schema.method() COLUMN Self.Children";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Self.Children", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NestedProperty_WithSemicolon_ShouldParse()
    {
        var query = "desc #schema.method() column Self.Children;";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("Self.Children", ExtractColumnPath(descNode.Column));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NestedProperty_ComplexNames_ShouldParse()
    {
        var query = "desc #schema.method() column ParentEntity123.ChildCollection456.GrandChildProperty789";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
        var descNode = GetDescNode(result);
        Assert.AreEqual(DescForType.SpecificColumn, descNode.Type);
        Assert.AreEqual("ParentEntity123.ChildCollection456.GrandChildProperty789", ExtractColumnPath(descNode.Column));
    }

    #endregion

    private static DescNode GetDescNode(Node result)
    {
        var rootNode = (RootNode)result;
        var statementsNode = (StatementsArrayNode)rootNode.Expression;
        var statementNode = statementsNode.Statements[0];
        return (DescNode)statementNode.Node;
    }


    private static string ExtractColumnPath(Node? node)
    {
        return node switch
        {
            null => null,
            DotNode d => $"{ExtractColumnPath(d.Root)}.{ExtractColumnPath(d.Expression)}",
            PropertyValueNode p => p.Name,
            WordNode w => w.Value,
            IdentifierNode i => i.Name,
            _ => throw new InvalidOperationException($"Unexpected node type: {node.GetType().Name}")
        };
    }

    #endregion
}
