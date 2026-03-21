using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;


[TestClass]
public class DescParser_ColumnTests : DescParserTestBase
{
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
