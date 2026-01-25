using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public class ExceptionTests
{
    #region SourceNotFoundException Tests

    [TestMethod]
    public void SourceNotFoundException_WithTableName_ShouldSetMessage()
    {
        var ex = new SourceNotFoundException("TestTable");

        Assert.AreEqual("TestTable", ex.Message);
    }

    [TestMethod]
    public void SourceNotFoundException_IsException()
    {
        var ex = new SourceNotFoundException("TestTable");

        Assert.IsInstanceOfType<Exception>(ex);
    }

    [TestMethod]
    public void SourceNotFoundException_WithEmptyString_ShouldAllowEmpty()
    {
        var ex = new SourceNotFoundException("");

        Assert.AreEqual("", ex.Message);
    }

    #endregion

    #region TableNotFoundException Tests

    [TestMethod]
    public void TableNotFoundException_WithTableName_ShouldSetMessage()
    {
        var ex = new TableNotFoundException("MyTable");

        Assert.AreEqual("MyTable", ex.Message);
    }

    [TestMethod]
    public void TableNotFoundException_IsException()
    {
        var ex = new TableNotFoundException("MyTable");

        Assert.IsInstanceOfType<Exception>(ex);
    }

    [TestMethod]
    public void TableNotFoundException_WithEmptyString_ShouldAllowEmpty()
    {
        var ex = new TableNotFoundException("");

        Assert.AreEqual("", ex.Message);
    }

    #endregion

    #region InjectSourceNullReferenceException Tests

    [TestMethod]
    public void InjectSourceNullReferenceException_WithType_ShouldSetMessage()
    {
        var ex = new InjectSourceNullReferenceException(typeof(string));

        Assert.Contains("System.String", ex.Message);
        Assert.Contains("Inject source is null", ex.Message);
    }

    [TestMethod]
    public void InjectSourceNullReferenceException_IsNullReferenceException()
    {
        var ex = new InjectSourceNullReferenceException(typeof(int));

        Assert.IsInstanceOfType<NullReferenceException>(ex);
    }

    [TestMethod]
    public void InjectSourceNullReferenceException_WithCustomType_IncludesFullName()
    {
        var ex = new InjectSourceNullReferenceException(typeof(ExceptionTests));

        Assert.Contains("Musoq.Schema.Tests.ExceptionTests", ex.Message);
    }

    #endregion

    #region SchemaArgumentException Tests

    [TestMethod]
    public void SchemaArgumentException_WithArgumentNameAndMessage_ShouldSetProperties()
    {
        var ex = new SchemaArgumentException("testArg", "Test message");

        Assert.AreEqual("testArg", ex.ParamName);
        Assert.Contains("Test message", ex.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_IsArgumentException()
    {
        var ex = new SchemaArgumentException("arg", "msg");

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }

    [TestMethod]
    public void SchemaArgumentException_WithInnerException_ShouldSetInnerException()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex = new SchemaArgumentException("testArg", "Outer message", inner);

        Assert.AreEqual("testArg", ex.ParamName);
        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual("Inner error", ex.InnerException.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_ForNullArgument_ShouldCreateDescriptiveMessage()
    {
        var ex = SchemaArgumentException.ForNullArgument("columnName", "querying the database");

        Assert.AreEqual("columnName", ex.ParamName);
        Assert.Contains("columnName", ex.Message);
        Assert.Contains("cannot be null", ex.Message);
        Assert.Contains("querying the database", ex.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_ForEmptyString_ShouldCreateDescriptiveMessage()
    {
        var ex = SchemaArgumentException.ForEmptyString("tableName", "creating a table");

        Assert.AreEqual("tableName", ex.ParamName);
        Assert.Contains("tableName", ex.Message);
        Assert.Contains("cannot be empty", ex.Message);
        Assert.Contains("creating a table", ex.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_ForInvalidMethodName_ShouldCreateDescriptiveMessage()
    {
        var ex = SchemaArgumentException.ForInvalidMethodName("InvalidMethod", "Method1, Method2, Method3");

        Assert.AreEqual("methodName", ex.ParamName);
        Assert.Contains("InvalidMethod", ex.Message);
        Assert.Contains("Method1", ex.Message);
        Assert.Contains("not recognized", ex.Message);
    }

    #endregion

    #region MethodResolutionException Tests

    [TestMethod]
    public void MethodResolutionException_WithAllParameters_ShouldSetProperties()
    {
        var providedParams = new[] { "int", "string" };
        var availableSignatures = new[] { "Method(int, int)", "Method(string, string)" };

        var ex = new MethodResolutionException("TestMethod", providedParams, availableSignatures, "Test message");

        Assert.AreEqual("TestMethod", ex.MethodName);
        CollectionAssert.AreEqual(providedParams, ex.ProvidedParameterTypes);
        CollectionAssert.AreEqual(availableSignatures, ex.AvailableSignatures);
        Assert.AreEqual("Test message", ex.Message);
    }

    [TestMethod]
    public void MethodResolutionException_IsInvalidOperationException()
    {
        var ex = new MethodResolutionException("M", [], [], "msg");

        Assert.IsInstanceOfType<InvalidOperationException>(ex);
    }

    [TestMethod]
    public void MethodResolutionException_ForUnresolvedMethod_ShouldCreateDescriptiveMessage()
    {
        var providedParams = new[] { "int", "string" };
        var availableSignatures = new[] { "Calculate(int, int)", "Calculate(decimal, decimal)" };

        var ex = MethodResolutionException.ForUnresolvedMethod("Calculate", providedParams, availableSignatures);

        Assert.AreEqual("Calculate", ex.MethodName);
        Assert.Contains("Calculate", ex.Message);
        Assert.Contains("int, string", ex.Message);
        Assert.Contains("Calculate(int, int)", ex.Message);
        Assert.Contains("Cannot resolve method", ex.Message);
    }

    [TestMethod]
    public void MethodResolutionException_ForUnresolvedMethod_WithNoParams_ShouldHandleEmpty()
    {
        var ex = MethodResolutionException.ForUnresolvedMethod("GetValue", [], []);

        Assert.AreEqual("GetValue", ex.MethodName);
        Assert.Contains("no parameters", ex.Message);
        Assert.Contains("No methods available", ex.Message);
    }

    [TestMethod]
    public void MethodResolutionException_ForAmbiguousMethod_ShouldCreateDescriptiveMessage()
    {
        var providedParams = new[] { "int" };
        var matchingSignatures = new[] { "Process(int)", "Process(long)" };

        var ex = MethodResolutionException.ForAmbiguousMethod("Process", providedParams, matchingSignatures);

        Assert.AreEqual("Process", ex.MethodName);
        Assert.Contains("Process", ex.Message);
        Assert.Contains("ambiguous", ex.Message);
        Assert.Contains("Process(int)", ex.Message);
        Assert.Contains("Process(long)", ex.Message);
        Assert.Contains("Multiple method signatures match", ex.Message);
    }

    [TestMethod]
    public void MethodResolutionException_ForAmbiguousMethod_PreservesArrays()
    {
        var providedParams = new[] { "double", "float" };
        var matchingSignatures = new[] { "Add(double, double)", "Add(float, float)", "Add(decimal, decimal)" };

        var ex = MethodResolutionException.ForAmbiguousMethod("Add", providedParams, matchingSignatures);

        Assert.HasCount(2, ex.ProvidedParameterTypes);
        Assert.HasCount(3, ex.AvailableSignatures);
    }

    #endregion

    #region Exception Throw and Catch Tests

    [TestMethod]
    public void SourceNotFoundException_CanBeThrownAndCaught()
    {
        var exceptionCaught = false;

        try
        {
            throw new SourceNotFoundException("MissingSource");
        }
        catch (SourceNotFoundException ex)
        {
            exceptionCaught = true;
            Assert.AreEqual("MissingSource", ex.Message);
        }

        Assert.IsTrue(exceptionCaught);
    }

    [TestMethod]
    public void TableNotFoundException_CanBeThrownAndCaught()
    {
        var exceptionCaught = false;

        try
        {
            throw new TableNotFoundException("MissingTable");
        }
        catch (TableNotFoundException ex)
        {
            exceptionCaught = true;
            Assert.AreEqual("MissingTable", ex.Message);
        }

        Assert.IsTrue(exceptionCaught);
    }

    [TestMethod]
    public void SchemaArgumentException_CanBeCaughtAsArgumentException()
    {
        var exceptionCaught = false;

        try
        {
            throw SchemaArgumentException.ForNullArgument("param", "testing");
        }
        catch (ArgumentException ex)
        {
            exceptionCaught = true;
            Assert.AreEqual("param", ex.ParamName);
        }

        Assert.IsTrue(exceptionCaught);
    }

    [TestMethod]
    public void MethodResolutionException_CanBeCaughtAsInvalidOperationException()
    {
        var exceptionCaught = false;

        try
        {
            throw MethodResolutionException.ForUnresolvedMethod("Unknown", [], []);
        }
        catch (InvalidOperationException)
        {
            exceptionCaught = true;
        }

        Assert.IsTrue(exceptionCaught);
    }

    [TestMethod]
    public void InjectSourceNullReferenceException_CanBeCaughtAsNullReferenceException()
    {
        var exceptionCaught = false;

        try
        {
            throw new InjectSourceNullReferenceException(typeof(string));
        }
        catch (NullReferenceException)
        {
            exceptionCaught = true;
        }

        Assert.IsTrue(exceptionCaught);
    }

    #endregion

    #region ParseException Tests

    [TestMethod]
    public void ParseException_BasicConstructor_ShouldSetAllProperties()
    {
        var ex = new ParseException(
            ParseErrorCode.InsufficientData,
            "TestSchema",
            "TestField",
            42,
            "Not enough bytes");

        Assert.AreEqual(ParseErrorCode.InsufficientData, ex.ErrorCode);
        Assert.AreEqual("TestSchema", ex.SchemaName);
        Assert.AreEqual("TestField", ex.FieldName);
        Assert.AreEqual(42, ex.Position);
        Assert.AreEqual("Not enough bytes", ex.Details);
    }

    [TestMethod]
    public void ParseException_FormattedErrorCode_ShouldFormatCorrectly()
    {
        var ex = new ParseException(
            ParseErrorCode.InsufficientData,
            "Schema",
            null,
            0,
            "details");

        Assert.AreEqual("ISE0001", ex.FormattedErrorCode);
    }

    [TestMethod]
    public void ParseException_FormattedErrorCode_ShouldPadToFourDigits()
    {
        var testCases = new (ParseErrorCode code, string expected)[]
        {
            (ParseErrorCode.InsufficientData, "ISE0001"),
            (ParseErrorCode.ValidationFailed, "ISE0002"),
            (ParseErrorCode.PatternMismatch, "ISE0003"),
            (ParseErrorCode.LiteralMismatch, "ISE0004"),
            (ParseErrorCode.DelimiterNotFound, "ISE0005"),
            (ParseErrorCode.ExpectedDelimiter, "ISE0006"),
            (ParseErrorCode.InvalidSize, "ISE0007"),
            (ParseErrorCode.InvalidPosition, "ISE0008"),
            (ParseErrorCode.MaxIterationsExceeded, "ISE0009"),
            (ParseErrorCode.EncodingError, "ISE0010"),
            (ParseErrorCode.ExpectedWhitespace, "ISE0011"),
            (ParseErrorCode.NoAlternativeMatched, "ISE0012"),
            (ParseErrorCode.InvalidSchemaReference, "ISE0013"),
            (ParseErrorCode.FieldReferenceError, "ISE0014"),
            (ParseErrorCode.GeneralError, "ISE0015")
        };

        foreach (var (code, expected) in testCases)
        {
            var ex = new ParseException(code, "Schema", null, 0, "details");
            Assert.AreEqual(expected, ex.FormattedErrorCode, $"Error code {code} should format as {expected}");
        }
    }

    [TestMethod]
    public void ParseException_Message_ShouldIncludeAllComponents()
    {
        var ex = new ParseException(
            ParseErrorCode.PatternMismatch,
            "BinaryHeader",
            "MagicNumber",
            16,
            "Expected 0x4D5A but found 0x0000");

        Assert.Contains("ISE0003", ex.Message);
        Assert.Contains("BinaryHeader", ex.Message);
        Assert.Contains("MagicNumber", ex.Message);
        Assert.Contains("16", ex.Message);
        Assert.Contains("Expected 0x4D5A but found 0x0000", ex.Message);
    }

    [TestMethod]
    public void ParseException_WithNullFieldName_ShouldNotIncludeFieldPart()
    {
        var ex = new ParseException(
            ParseErrorCode.InvalidSize,
            "TestSchema",
            null,
            0,
            "Size was negative");

        Assert.Contains("TestSchema", ex.Message);
        Assert.DoesNotContain("TestSchema.", ex.Message);
    }

    [TestMethod]
    public void ParseException_WithFieldName_ShouldIncludeFieldPart()
    {
        var ex = new ParseException(
            ParseErrorCode.InvalidSize,
            "TestSchema",
            "Length",
            0,
            "Size was negative");

        Assert.Contains("TestSchema.Length", ex.Message);
    }

    [TestMethod]
    public void ParseException_WithInnerException_ShouldPreserveInnerException()
    {
        var innerException = new InvalidOperationException("Inner failure");
        var ex = new ParseException(
            ParseErrorCode.EncodingError,
            "TextSchema",
            "Content",
            100,
            "UTF-8 decoding failed",
            innerException);

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException);
        Assert.AreEqual("Inner failure", ex.InnerException.Message);
    }

    [TestMethod]
    public void ParseException_IsException()
    {
        var ex = new ParseException(
            ParseErrorCode.GeneralError,
            "Schema",
            null,
            0,
            "General error");

        Assert.IsInstanceOfType<Exception>(ex);
    }

    [TestMethod]
    public void ParseException_CanBeCaughtAsException()
    {
        var exceptionCaught = false;

        try
        {
            throw new ParseException(
                ParseErrorCode.DelimiterNotFound,
                "LogSchema",
                "Timestamp",
                0,
                "Expected ':' delimiter");
        }
        catch (Exception ex)
        {
            exceptionCaught = true;
            Assert.IsInstanceOfType<ParseException>(ex);
        }

        Assert.IsTrue(exceptionCaught);
    }

    [TestMethod]
    public void ParseException_Position_CanBeZero()
    {
        var ex = new ParseException(
            ParseErrorCode.InsufficientData,
            "Schema",
            null,
            0,
            "Empty input");

        Assert.AreEqual(0, ex.Position);
    }

    [TestMethod]
    public void ParseException_Position_CanBeLargeValue()
    {
        var ex = new ParseException(
            ParseErrorCode.InsufficientData,
            "Schema",
            null,
            1_000_000,
            "Unexpected end of file");

        Assert.AreEqual(1_000_000, ex.Position);
    }

    [TestMethod]
    public void ParseException_AllErrorCodes_ShouldCreateValidException()
    {
        var errorCodes = (ParseErrorCode[])Enum.GetValues(typeof(ParseErrorCode));

        foreach (var code in errorCodes)
        {
            var ex = new ParseException(code, "TestSchema", "TestField", 0, "Test details");

            Assert.IsNotNull(ex);
            Assert.AreEqual(code, ex.ErrorCode);
            Assert.IsFalse(string.IsNullOrEmpty(ex.Message));
            Assert.IsFalse(string.IsNullOrEmpty(ex.FormattedErrorCode));
        }
    }

    #endregion
}
