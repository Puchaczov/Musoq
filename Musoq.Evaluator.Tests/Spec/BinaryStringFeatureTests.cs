using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema String Types (Section 4.2.4 of specification).
///     Tests string parsing with various encodings and modifiers.
/// </summary>
[TestClass]
public class BinaryStringFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.2.4: Dynamic String Size

    /// <summary>
    ///     Tests string with size from field reference.
    /// </summary>
    [TestMethod]
    public void Binary_StringDynamicSize_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { 
                Length: byte,
                Name: string[Length] utf8 
            };
            select d.Length, d.Name from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)5, table[0][0]);
        Assert.AreEqual("Hello", table[0][1]);
    }

    #endregion

    #region Section 4.2.4: Multiple Strings

    /// <summary>
    ///     Tests multiple string fields in one schema.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleStrings_ShouldParseSequentially()
    {
        var query = @"
            binary Record { 
                FirstName: string[10] utf8 trim,
                LastName: string[10] utf8 trim
            };
            select r.FirstName, r.LastName from #test.files() f
            cross apply Interpret(f.Content, 'Record') r";

        var firstName = "John".PadRight(10);
        var lastName = "Doe".PadRight(10);
        var testData = Encoding.UTF8.GetBytes(firstName + lastName);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("John", table[0][0]);
        Assert.AreEqual("Doe", table[0][1]);
    }

    #endregion

    #region Section 4.2.4: String in WHERE Clause

    /// <summary>
    ///     Tests filtering by string value.
    /// </summary>
    [TestMethod]
    public void Binary_StringInWhereClause_ShouldFilter()
    {
        var query = @"
            binary Header { Magic: string[4] ascii };
            select f.Name from #test.files() f
            cross apply Interpret(f.Content, 'Header') h
            where h.Magic = 'PNG '";

        var entities = new[]
        {
            new BinaryEntity { Name = "image.png", Content = Encoding.ASCII.GetBytes("PNG ") },
            new BinaryEntity { Name = "image.gif", Content = Encoding.ASCII.GetBytes("GIF8") }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("image.png", table[0][0]);
    }

    #endregion

    #region Section 4.2.4: UTF-8 Encoding

    /// <summary>
    ///     Tests UTF-8 string parsing.
    /// </summary>
    [TestMethod]
    public void Binary_StringUtf8_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Name: string[5] utf8 };
            select d.Name from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = Encoding.UTF8.GetBytes("Hello");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     Tests UTF-8 string with multi-byte characters.
    /// </summary>
    [TestMethod]
    public void Binary_StringUtf8MultiByteChars_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Text: string[6] utf8 };
            select d.Text from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = Encoding.UTF8.GetBytes("日本");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("日本", table[0][0]);
    }

    #endregion

    #region Section 4.2.4: ASCII Encoding

    /// <summary>
    ///     Tests ASCII string parsing.
    /// </summary>
    [TestMethod]
    public void Binary_StringAscii_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Code: string[4] ascii };
            select d.Code from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = Encoding.ASCII.GetBytes("TEST");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TEST", table[0][0]);
    }

    /// <summary>
    ///     Tests ASCII with numbers and special characters.
    /// </summary>
    [TestMethod]
    public void Binary_StringAsciiMixed_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: string[8] ascii };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = Encoding.ASCII.GetBytes("ABC-1234");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABC-1234", table[0][0]);
    }

    #endregion

    #region Section 4.2.4: Trim Modifier

    /// <summary>
    ///     Tests string with trim modifier removes trailing spaces.
    /// </summary>
    [TestMethod]
    public void Binary_StringTrim_ShouldRemoveTrailingSpaces()
    {
        var query = @"
            binary Data { Name: string[10] utf8 trim };
            select d.Name from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = Encoding.UTF8.GetBytes("Hello     ");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     Tests string with trim removes leading and trailing spaces.
    /// </summary>
    [TestMethod]
    public void Binary_StringTrim_ShouldRemoveLeadingAndTrailingSpaces()
    {
        var query = @"
            binary Data { Name: string[12] utf8 trim };
            select d.Name from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = Encoding.UTF8.GetBytes("  Testing   ");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Testing", table[0][0]);
    }

    /// <summary>
    ///     Tests string with rtrim (right trim only).
    /// </summary>
    [TestMethod]
    public void Binary_StringRtrim_ShouldRemoveOnlyTrailingSpaces()
    {
        var query = @"
            binary Data { Name: string[12] utf8 rtrim };
            select d.Name from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = Encoding.UTF8.GetBytes("  Testing   ");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("  Testing", table[0][0]);
    }

    /// <summary>
    ///     Tests string with ltrim (left trim only).
    /// </summary>
    [TestMethod]
    public void Binary_StringLtrim_ShouldRemoveOnlyLeadingSpaces()
    {
        var query = @"
            binary Data { Name: string[12] utf8 ltrim };
            select d.Name from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = Encoding.UTF8.GetBytes("  Testing   ");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Testing   ", table[0][0]);
    }

    #endregion

    #region Section 4.2.4: Nullterm Modifier

    /// <summary>
    ///     Tests null-terminated string.
    /// </summary>
    [TestMethod]
    public void Binary_StringNullterm_ShouldTruncateAtNull()
    {
        var query = @"
            binary Data { Name: string[10] utf8 nullterm };
            select d.Name from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x58, 0x58, 0x58, 0x58 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     Tests null-terminated string with immediate null.
    /// </summary>
    [TestMethod]
    public void Binary_StringNullterm_ImmediateNull_ShouldReturnEmpty()
    {
        var query = @"
            binary Data { Name: string[10] utf8 nullterm };
            select d.Name from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[] { 0x00, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58, 0x58 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("", table[0][0]);
    }

    /// <summary>
    ///     Tests string without null byte uses full buffer.
    /// </summary>
    [TestMethod]
    public void Binary_StringNullterm_NoNull_ShouldUseFullBuffer()
    {
        var query = @"
            binary Data { Name: string[5] utf8 nullterm };
            select d.Name from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = Encoding.UTF8.GetBytes("ABCDE");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABCDE", table[0][0]);
    }

    #endregion
}
