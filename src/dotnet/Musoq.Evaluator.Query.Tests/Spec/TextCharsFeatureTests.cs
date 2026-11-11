using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Text Schema Fixed-Width 'chars' Capture (Section 5.5 of specification).
///     Tests capturing exactly N characters.
/// </summary>
[TestClass]
public class TextCharsFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 5.5: Chars in WHERE Clause

    /// <summary>
    ///     Tests filtering by chars-captured field.
    /// </summary>
    [TestMethod]
    public void Text_Chars_InWhereClause_ShouldFilter()
    {
        var query = (
            "\n"
            + "            text Record {\n"
            + "                Type: chars[1],\n"
            + "                Data: rest\n"
            + "            };\n"
            + "            select r.Data from #test.lines() l\n"
            + "            cross apply Parse<Record>(l.Line) r\n"
            + "            where r.Type = 'A'"
        );

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "AData1" },
            new TextEntity { Name = "line2", Text = "BData2" },
            new TextEntity { Name = "line3", Text = "AData3" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var values = new HashSet<string> { (string)table[0].ReadValue(0), (string)table[1].ReadValue(0) };
        Assert.Contains("Data1", values);
        Assert.Contains("Data3", values);
    }

    #endregion

    #region Section 5.5: Chars with Mixed Field Types

    /// <summary>
    ///     Tests chars mixed with until.
    /// </summary>
    [TestMethod]
    public void Text_Chars_MixedWithUntil_ShouldParseBoth()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Fixed: chars[4],\n"
            + "                Delimiter: chars[1],\n"
            + "                Variable: until ';',\n"
            + "                Rest: rest\n"
            + "            };\n"
            + "            select d.Fixed, d.Delimiter, d.Variable, d.Rest from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "ABCD:variable;rest" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABCD", table[0].ReadValue(0));
        Assert.AreEqual(":", table[0].ReadValue(1));
        Assert.AreEqual("variable", table[0].ReadValue(2));
        Assert.AreEqual("rest", table[0].ReadValue(3));
    }

    #endregion

    #region Section 5.5: Multiple Lines

    /// <summary>
    ///     Tests chars parsing across multiple lines.
    /// </summary>
    [TestMethod]
    public void Text_Chars_MultipleRows_ShouldParseAll()
    {
        var query = (
            "\n"
            + "            text Record {\n"
            + "                Code: chars[3] trim,\n"
            + "                Value: chars[5] trim\n"
            + "            };\n"
            + "            select r.Code, r.Value from #test.lines() l\n"
            + "            cross apply Parse<Record>(l.Line) r\n"
            + "            order by r.Code"
        );

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "ABC  123  " },
            new TextEntity { Name = "line2", Text = "DEF  456  " },
            new TextEntity { Name = "line3", Text = "GHI  789  " },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("ABC", table[0].ReadValue(0));
        Assert.AreEqual("123", table[0].ReadValue(1));
    }

    #endregion

    #region Section 5.5: Basic Chars

    /// <summary>
    ///     Tests basic fixed-width character capture.
    /// </summary>
    [TestMethod]
    public void Text_Chars_BasicCapture_ShouldReturnExactLength()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Code: chars[4]\n"
            + "            };\n"
            + "            select d.Code from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "ABCD" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABCD", table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests chars with extra content after.
    /// </summary>
    [TestMethod]
    public void Text_Chars_WithExtraContent_ShouldCaptureExactly()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                First: chars[5],\n"
            + "                Rest: rest\n"
            + "            };\n"
            + "            select d.First, d.Rest from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "HelloWorld" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0].ReadValue(0));
        Assert.AreEqual("World", table[0].ReadValue(1));
    }

    /// <summary>
    ///     Tests chars with single character.
    /// </summary>
    [TestMethod]
    public void Text_Chars_SingleCharacter_ShouldCapture()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Char: chars[1],\n"
            + "                Rest: rest\n"
            + "            };\n"
            + "            select d.Char, d.Rest from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "XYZ" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("X", table[0].ReadValue(0));
        Assert.AreEqual("YZ", table[0].ReadValue(1));
    }

    #endregion

    #region Section 5.5: Multiple Chars Fields

    /// <summary>
    ///     Tests multiple fixed-width fields in sequence.
    /// </summary>
    [TestMethod]
    public void Text_Chars_MultipleFields_ShouldParseSequentially()
    {
        var query = (
            "\n"
            + "            text FixedRecord {\n"
            + "                Id: chars[4],\n"
            + "                Code: chars[3],\n"
            + "                Status: chars[2]\n"
            + "            };\n"
            + "            select r.Id, r.Code, r.Status from #test.lines() l\n"
            + "            cross apply Parse<FixedRecord>(l.Line) r"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "1234ABCOK" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("1234", table[0].ReadValue(0));
        Assert.AreEqual("ABC", table[0].ReadValue(1));
        Assert.AreEqual("OK", table[0].ReadValue(2));
    }

    /// <summary>
    ///     Tests COBOL-style fixed record parsing.
    /// </summary>
    [TestMethod]
    public void Text_Chars_CobolStyleRecord_ShouldParse()
    {
        var query = (
            "\n"
            + "            text CobolRecord {\n"
            + "                CustomerId: chars[10],\n"
            + "                Name: chars[30],\n"
            + "                Balance: chars[12]\n"
            + "            };\n"
            + "            select r.CustomerId, r.Name, r.Balance from #test.lines() l\n"
            + "            cross apply Parse<CobolRecord>(l.Line) r"
        );

        var text = "CUST000001John Doe                      000000125000";
        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = text },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("CUST000001", table[0].ReadValue(0));
    }

    #endregion

    #region Section 5.5: Chars with Trim Modifier

    /// <summary>
    ///     Tests chars with trim modifier.
    /// </summary>
    [TestMethod]
    public void Text_Chars_WithTrim_ShouldRemovePadding()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Name: chars[10] trim\n"
            + "            };\n"
            + "            select d.Name from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "  John    " },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("John", table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests chars with rtrim modifier.
    /// </summary>
    [TestMethod]
    public void Text_Chars_WithRtrim_ShouldRemoveTrailingOnly()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Name: chars[10] rtrim\n"
            + "            };\n"
            + "            select d.Name from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "  John    " },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("  John", table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests chars with ltrim modifier.
    /// </summary>
    [TestMethod]
    public void Text_Chars_WithLtrim_ShouldRemoveLeadingOnly()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Name: chars[10] ltrim\n"
            + "            };\n"
            + "            select d.Name from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "  John    " },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("John    ", table[0].ReadValue(0));
    }

    #endregion
}
