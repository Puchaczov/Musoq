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
        var query = (
            "\n"
            + "            binary Data { \n"
            + "                Length: byte,\n"
            + "                Name: string[Length] utf8 \n"
            + "            };\n"
            + "            select d.Length, d.Name from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual((byte)5, table[0].ReadValue(0));
        Assert.AreEqual("Hello", table[0].ReadValue(1));
    }

    #endregion

    #region Section 4.2.4: Multiple Strings

    /// <summary>
    ///     Tests multiple string fields in one schema.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleStrings_ShouldParseSequentially()
    {
        var query = (
            "\n"
            + "            binary Record { \n"
            + "                FirstName: string[10] utf8 trim,\n"
            + "                LastName: string[10] utf8 trim\n"
            + "            };\n"
            + "            select r.FirstName, r.LastName from #test.files() f\n"
            + "            cross apply Interpret<Record>(f.Content) r"
        );

        var firstName = "John".PadRight(10);
        var lastName = "Doe".PadRight(10);
        var testData = Encoding.UTF8.GetBytes(firstName + lastName);

        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("Doe", table[0].ReadValue(1));
    }

    #endregion

    #region Section 4.2.4: String in WHERE Clause

    /// <summary>
    ///     Tests filtering by string value.
    /// </summary>
    [TestMethod]
    public void Binary_StringInWhereClause_ShouldFilter()
    {
        var query = (
            "\n"
            + "            binary Header { Magic: string[4] ascii };\n"
            + "            select f.Name from #test.files() f\n"
            + "            cross apply Interpret<Header>(f.Content) h\n"
            + "            where h.Magic = 'PNG '"
        );

        var entities = new[]
        {
            new BinaryEntity { Name = "image.png", Content = "PNG "u8.ToArray() },
            new BinaryEntity { Name = "image.gif", Content = "GIF8"u8.ToArray() },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("image.png", table[0].ReadValue(0));
    }

    #endregion

    #region Section 4.2.4: UTF-8 Encoding

    /// <summary>
    ///     Tests UTF-8 string parsing.
    /// </summary>
    [TestMethod]
    public void Binary_StringUtf8_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Name: string[5] utf8 };\n"
            + "            select d.Name from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "Hello"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
    }

    /// <summary>
    ///     Tests UTF-8 string with multi-byte characters.
    /// </summary>
    [TestMethod]
    public void Binary_StringUtf8MultiByteChars_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Text: string[6] utf8 };\n"
            + "            select d.Text from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "日本"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("日本", table[0].ReadValue(0));
    }

    #endregion

    #region Section 4.2.4: ASCII Encoding

    /// <summary>
    ///     Tests ASCII string parsing.
    /// </summary>
    [TestMethod]
    public void Binary_StringAscii_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Code: string[4] ascii };\n"
            + "            select d.Code from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "TEST"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("TEST", table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests ASCII with numbers and special characters.
    /// </summary>
    [TestMethod]
    public void Binary_StringAsciiMixed_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: string[8] ascii };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "ABC-1234"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("ABC-1234", table[0].ReadValue(0));
    }

    #endregion

    #region Section 4.2.4: Trim Modifier

    /// <summary>
    ///     Tests string with trim modifier removes trailing spaces.
    /// </summary>
    [TestMethod]
    public void Binary_StringTrim_ShouldRemoveTrailingSpaces()
    {
        var query = (
            "\n"
            + "            binary Data { Name: string[10] utf8 trim };\n"
            + "            select d.Name from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "Hello     "u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
    }

    /// <summary>
    ///     Tests string with trim removes leading and trailing spaces.
    /// </summary>
    [TestMethod]
    public void Binary_StringTrim_ShouldRemoveLeadingAndTrailingSpaces()
    {
        var query = (
            "\n"
            + "            binary Data { Name: string[12] utf8 trim };\n"
            + "            select d.Name from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "  Testing   "u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("Testing", table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests string with rtrim (right trim only).
    /// </summary>
    [TestMethod]
    public void Binary_StringRtrim_ShouldRemoveOnlyTrailingSpaces()
    {
        var query = (
            "\n"
            + "            binary Data { Name: string[12] utf8 rtrim };\n"
            + "            select d.Name from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "  Testing   "u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("  Testing", table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests string with ltrim (left trim only).
    /// </summary>
    [TestMethod]
    public void Binary_StringLtrim_ShouldRemoveOnlyLeadingSpaces()
    {
        var query = (
            "\n"
            + "            binary Data { Name: string[12] utf8 ltrim };\n"
            + "            select d.Name from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "  Testing   "u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("Testing   ", table[0].ReadValue(0));
    }

    #endregion

    #region Section 4.2.4: Nullterm Modifier

    /// <summary>
    ///     Tests null-terminated string.
    /// </summary>
    [TestMethod]
    public void Binary_StringNullterm_ShouldTruncateAtNull()
    {
        var query = (
            "\n"
            + "            binary Data { Name: string[10] utf8 nullterm };\n"
            + "            select d.Name from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "Hello\u0000XXXX"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
    }

    /// <summary>
    ///     Tests null-terminated string with immediate null.
    /// </summary>
    [TestMethod]
    public void Binary_StringNullterm_ImmediateNull_ShouldReturnEmpty()
    {
        var query = (
            "\n"
            + "            binary Data { Name: string[10] utf8 nullterm };\n"
            + "            select d.Name from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "\u0000XXXXXXXXX"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("", table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests string without null byte uses full buffer.
    /// </summary>
    [TestMethod]
    public void Binary_StringNullterm_NoNull_ShouldUseFullBuffer()
    {
        var query = (
            "\n"
            + "            binary Data { Name: string[5] utf8 nullterm };\n"
            + "            select d.Name from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = "ABCDE"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
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
        Assert.AreEqual("ABCDE", table[0].ReadValue(0));
    }

    #endregion
}
