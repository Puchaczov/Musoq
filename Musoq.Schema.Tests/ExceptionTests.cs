using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Exceptions;

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
}