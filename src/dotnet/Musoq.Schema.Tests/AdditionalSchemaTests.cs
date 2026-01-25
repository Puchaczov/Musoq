using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Attributes;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;

namespace Musoq.Schema.Tests;

/// <summary>
///     Tests for Schema exceptions and low-coverage classes (Session 1 - Phase 2)
/// </summary>
[TestClass]
public class AdditionalSchemaTests
{
    #region InjectSourceNullReferenceException Tests

    [TestMethod]
    public void InjectSourceNullReferenceException_Constructor_ShouldSetMessage()
    {
        // Arrange & Act
        var exception = new InjectSourceNullReferenceException(typeof(string));

        // Assert
        Assert.Contains("System.String", exception.Message);
        Assert.Contains("Inject source is null", exception.Message);
    }

    [TestMethod]
    public void InjectSourceNullReferenceException_IsNullReferenceException()
    {
        // Arrange & Act
        var exception = new InjectSourceNullReferenceException(typeof(int));

        // Assert
        Assert.IsInstanceOfType(exception, typeof(NullReferenceException));
    }

    [TestMethod]
    public void InjectSourceNullReferenceException_WithComplexType_ShouldIncludeFullName()
    {
        // Arrange & Act
        var exception = new InjectSourceNullReferenceException(typeof(Dictionary<string, int>));

        // Assert
        Assert.Contains("Dictionary", exception.Message);
    }

    #endregion

    #region MethodResolutionException Tests

    [TestMethod]
    public void MethodResolutionException_Constructor_ShouldSetProperties()
    {
        // Arrange
        var methodName = "TestMethod";
        var providedTypes = new[] { "string", "int" };
        var availableSignatures = new[] { "TestMethod(string)", "TestMethod(int, int)" };
        var message = "Test message";

        // Act
        var exception = new MethodResolutionException(methodName, providedTypes, availableSignatures, message);

        // Assert
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.HasCount(2, exception.ProvidedParameterTypes);
        Assert.HasCount(2, exception.AvailableSignatures);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void MethodResolutionException_IsInvalidOperationException()
    {
        // Arrange & Act
        var exception = new MethodResolutionException("Test", Array.Empty<string>(), Array.Empty<string>(), "msg");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(InvalidOperationException));
    }

    [TestMethod]
    public void MethodResolutionException_ForUnresolvedMethod_WithParameters_ShouldCreateProperMessage()
    {
        // Arrange
        var methodName = "DoSomething";
        var providedTypes = new[] { "string", "int" };
        var availableSignatures = new[] { "DoSomething(string)", "DoSomething(bool, bool)" };

        // Act
        var exception = MethodResolutionException.ForUnresolvedMethod(methodName, providedTypes, availableSignatures);

        // Assert
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.Contains("Cannot resolve method", exception.Message);
        Assert.Contains("string, int", exception.Message);
        Assert.Contains("Available method signatures", exception.Message);
    }

    [TestMethod]
    public void MethodResolutionException_ForUnresolvedMethod_NoParameters_ShouldSayNoParameters()
    {
        // Arrange
        var methodName = "NoArgs";

        // Act
        var exception =
            MethodResolutionException.ForUnresolvedMethod(methodName, Array.Empty<string>(), Array.Empty<string>());

        // Assert
        Assert.Contains("no parameters", exception.Message);
        Assert.Contains("No methods available", exception.Message);
    }

    [TestMethod]
    public void MethodResolutionException_ForUnresolvedMethod_NoAvailableSignatures_ShouldSayNoMethods()
    {
        // Arrange & Act
        var exception = MethodResolutionException.ForUnresolvedMethod("Test", new[] { "int" }, Array.Empty<string>());

        // Assert
        Assert.Contains("No methods available with this name", exception.Message);
    }

    [TestMethod]
    public void MethodResolutionException_ForAmbiguousMethod_ShouldCreateProperMessage()
    {
        // Arrange
        var methodName = "Overloaded";
        var providedTypes = new[] { "object" };
        var matchingSignatures = new[] { "Overloaded(string)", "Overloaded(int)" };

        // Act
        var exception = MethodResolutionException.ForAmbiguousMethod(methodName, providedTypes, matchingSignatures);

        // Assert
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.Contains("ambiguous", exception.Message);
        Assert.Contains("Multiple method signatures match", exception.Message);
    }

    #endregion

    #region SchemaArgumentException Tests

    [TestMethod]
    public void SchemaArgumentException_Constructor_ShouldSetProperties()
    {
        // Arrange
        var argName = "testArg";
        var message = "Test message";

        // Act
        var exception = new SchemaArgumentException(argName, message);

        // Assert
        Assert.AreEqual(argName, exception.ParamName);
        Assert.Contains(message, exception.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");

        // Act
        var exception = new SchemaArgumentException("arg", "msg", inner);

        // Assert
        Assert.AreSame(inner, exception.InnerException);
    }

    [TestMethod]
    public void SchemaArgumentException_IsArgumentException()
    {
        // Arrange & Act
        var exception = new SchemaArgumentException("arg", "msg");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(ArgumentException));
    }

    [TestMethod]
    public void SchemaArgumentException_ForNullArgument_ShouldCreateProperMessage()
    {
        // Arrange
        var argName = "connectionString";
        var context = "initializing the database connection";

        // Act
        var exception = SchemaArgumentException.ForNullArgument(argName, context);

        // Assert
        Assert.AreEqual(argName, exception.ParamName);
        Assert.Contains("cannot be null", exception.Message);
        Assert.Contains(context, exception.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_ForEmptyString_ShouldCreateProperMessage()
    {
        // Arrange
        var argName = "tableName";
        var context = "querying the database";

        // Act
        var exception = SchemaArgumentException.ForEmptyString(argName, context);

        // Assert
        Assert.AreEqual(argName, exception.ParamName);
        Assert.Contains("cannot be empty", exception.Message);
        Assert.Contains(context, exception.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_ForInvalidMethodName_ShouldCreateProperMessage()
    {
        // Arrange
        var methodName = "InvalidMethod";
        var available = "Method1, Method2, Method3";

        // Act
        var exception = SchemaArgumentException.ForInvalidMethodName(methodName, available);

        // Assert
        Assert.Contains("not recognized", exception.Message);
        Assert.Contains(available, exception.Message);
    }

    #endregion

    #region SourceNotFoundException Tests

    [TestMethod]
    public void SourceNotFoundException_Constructor_ShouldSetMessage()
    {
        // Arrange
        var tableName = "users";

        // Act
        var exception = new SourceNotFoundException(tableName);

        // Assert
        Assert.AreEqual(tableName, exception.Message);
    }

    [TestMethod]
    public void SourceNotFoundException_IsException()
    {
        // Arrange & Act
        var exception = new SourceNotFoundException("test");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    #endregion

    #region TableNotFoundException Tests

    [TestMethod]
    public void TableNotFoundException_Constructor_ShouldSetMessage()
    {
        // Arrange
        var tableName = "orders";

        // Act
        var exception = new TableNotFoundException(tableName);

        // Assert
        Assert.AreEqual(tableName, exception.Message);
    }

    [TestMethod]
    public void TableNotFoundException_IsException()
    {
        // Arrange & Act
        var exception = new TableNotFoundException("test");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    #endregion

    #region SingleRowSchemaTable Tests

    [TestMethod]
    public void SingleRowSchemaTable_Columns_ShouldReturnOneColumn()
    {
        // Arrange
        var table = new SingleRowSchemaTable();

        // Act
        var columns = table.Columns;

        // Assert
        Assert.HasCount(1, columns);
    }

    [TestMethod]
    public void SingleRowSchemaTable_Column_ShouldHaveCorrectProperties()
    {
        // Arrange
        var table = new SingleRowSchemaTable();

        // Act
        var column = table.Columns[0];

        // Assert
        Assert.AreEqual("Column1", column.ColumnName);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual(typeof(string), column.ColumnType);
    }

    [TestMethod]
    public void SingleRowSchemaTable_GetColumnByName_ExistingColumn_ShouldReturnColumn()
    {
        // Arrange
        var table = new SingleRowSchemaTable();

        // Act
        var column = table.GetColumnByName("Column1");

        // Assert
        Assert.IsNotNull(column);
        Assert.AreEqual("Column1", column.ColumnName);
    }

    [TestMethod]
    public void SingleRowSchemaTable_GetColumnByName_NonExistingColumn_ShouldReturnNull()
    {
        // Arrange
        var table = new SingleRowSchemaTable();

        // Act
        var column = table.GetColumnByName("NonExistent");

        // Assert
        Assert.IsNull(column);
    }

    [TestMethod]
    public void SingleRowSchemaTable_GetColumnsByName_ExistingColumn_ShouldReturnArray()
    {
        // Arrange
        var table = new SingleRowSchemaTable();

        // Act
        var columns = table.GetColumnsByName("Column1");

        // Assert
        Assert.HasCount(1, columns);
    }

    [TestMethod]
    public void SingleRowSchemaTable_GetColumnsByName_NonExistingColumn_ShouldReturnEmptyArray()
    {
        // Arrange
        var table = new SingleRowSchemaTable();

        // Act
        var columns = table.GetColumnsByName("NonExistent");

        // Assert
        Assert.IsEmpty(columns);
    }

    [TestMethod]
    public void SingleRowSchemaTable_Metadata_ShouldReturnStringType()
    {
        // Arrange
        var table = new SingleRowSchemaTable();

        // Act
        var metadata = table.Metadata;

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(typeof(string), metadata.TableEntityType);
    }

    #endregion

    #region PluginSchemasAttribute Tests

    [TestMethod]
    public void PluginSchemasAttribute_Constructor_ShouldSetSchemas()
    {
        // Arrange & Act
        var attribute = new PluginSchemasAttribute("schema1", "schema2", "schema3");

        // Assert
        Assert.HasCount(3, attribute.Schemas);
        Assert.AreEqual("schema1", attribute.Schemas[0]);
        Assert.AreEqual("schema2", attribute.Schemas[1]);
        Assert.AreEqual("schema3", attribute.Schemas[2]);
    }

    [TestMethod]
    public void PluginSchemasAttribute_WithNoSchemas_ShouldHaveEmptyArray()
    {
        // Arrange & Act
        var attribute = new PluginSchemasAttribute();

        // Assert
        Assert.IsEmpty(attribute.Schemas);
    }

    [TestMethod]
    public void PluginSchemasAttribute_WithSingleSchema_ShouldWork()
    {
        // Arrange & Act
        var attribute = new PluginSchemasAttribute("onlyOne");

        // Assert
        Assert.HasCount(1, attribute.Schemas);
        Assert.AreEqual("onlyOne", attribute.Schemas[0]);
    }

    [TestMethod]
    public void PluginSchemasAttribute_IsAttributeUsageAssembly()
    {
        // Arrange
        var attributeType = typeof(PluginSchemasAttribute);

        // Act
        var usageAttribute =
            (AttributeUsageAttribute)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        Assert.IsNotNull(usageAttribute);
        Assert.AreEqual(AttributeTargets.Assembly, usageAttribute.ValidOn);
    }

    #endregion

    #region SingleRowSource Tests

    [TestMethod]
    public void SingleRowSource_Rows_ShouldEnumerateOneRow()
    {
        // Arrange
        var source = new SingleRowSource();

        // Act
        var rowCount = 0;
        foreach (var row in source.Rows) rowCount++;

        // Assert
        Assert.AreEqual(1, rowCount);
    }

    [TestMethod]
    public void SingleRowSource_Row_ShouldHaveColumn1()
    {
        // Arrange
        var source = new SingleRowSource();

        // Act
        IObjectResolver firstRow = null;
        foreach (var row in source.Rows)
        {
            firstRow = row;
            break;
        }

        // Assert
        Assert.IsNotNull(firstRow);
        Assert.IsTrue(firstRow.HasColumn("Column1"));
    }

    [TestMethod]
    public void SingleRowSource_Row_Column1Value_ShouldBeEmptyString()
    {
        // Arrange
        var source = new SingleRowSource();

        // Act
        object value = null;
        foreach (var row in source.Rows)
        {
            value = row["Column1"];
            break;
        }

        // Assert
        Assert.AreEqual(string.Empty, value);
    }

    #endregion
}
