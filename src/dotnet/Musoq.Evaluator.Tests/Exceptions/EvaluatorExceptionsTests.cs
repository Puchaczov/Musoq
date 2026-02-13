using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Tests.Exceptions;

/// <summary>
///     Comprehensive tests for all exception classes in Musoq.Evaluator.Exceptions
/// </summary>
[TestClass]
public class EvaluatorExceptionsTests
{
    #region AmbiguousColumnException Tests

    [TestMethod]
    public void AmbiguousColumnException_WhenCreated_ShouldContainColumnAndAliases()
    {
        // Arrange
        var column = "Name";
        var alias1 = "table1";
        var alias2 = "table2";

        // Act
        var exception = new AmbiguousColumnException(column, alias1, alias2);

        // Assert
        Assert.Contains(column, exception.Message);
        Assert.Contains(alias1, exception.Message);
        Assert.Contains(alias2, exception.Message);
        Assert.Contains("Ambiguous", exception.Message);
    }

    #endregion

    #region ColumnMustBeAnArrayOrImplementIEnumerableException Tests

    [TestMethod]
    public void ColumnMustBeAnArrayOrImplementIEnumerableException_ShouldHaveCorrectMessage()
    {
        // Act
        var exception = new ColumnMustBeAnArrayOrImplementIEnumerableException();

        // Assert
        Assert.Contains("array", exception.Message);
        Assert.Contains("IEnumerable", exception.Message);
    }

    #endregion

    #region ColumnMustBeMarkedAsBindablePropertyAsTableException Tests

    [TestMethod]
    public void ColumnMustBeMarkedAsBindablePropertyAsTableException_ShouldHaveCorrectMessage()
    {
        // Act
        var exception = new ColumnMustBeMarkedAsBindablePropertyAsTableException();

        // Assert
        Assert.Contains("BindablePropertyAsTable", exception.Message);
    }

    #endregion

    #region ConstructionNotYetSupported Tests

    [TestMethod]
    public void ConstructionNotYetSupported_ShouldContainCustomMessage()
    {
        // Arrange
        var message = "Feature X is not supported";

        // Act
        var exception = new ConstructionNotYetSupported(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    #endregion

    #region DotNetNotFoundException Tests

    [TestMethod]
    public void DotNetNotFoundException_ShouldBeCreatable()
    {
        // Act
        var exception = new DotNetNotFoundException();

        // Assert
        Assert.IsNotNull(exception);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    #endregion

    #region FieldLinkIndexOutOfRangeException Tests

    [TestMethod]
    public void FieldLinkIndexOutOfRangeException_ShouldContainIndexAndGroupInfo()
    {
        // Arrange
        var index = 5;
        var groups = 3;

        // Act
        var exception = new FieldLinkIndexOutOfRangeException(index, groups);

        // Assert
        Assert.Contains("5", exception.Message);
        Assert.Contains("3", exception.Message);
        Assert.Contains("group", exception.Message);
    }

    #endregion

    #region FromNodeIsNull Tests

    [TestMethod]
    public void FromNodeIsNull_ShouldHaveCorrectMessage()
    {
        // Act
        var exception = new FromNodeIsNull();

        // Assert
        Assert.Contains("FROM clause is missing", exception.Message);
    }

    #endregion

    #region ObjectDoesNotImplementIndexerException Tests

    [TestMethod]
    public void ObjectDoesNotImplementIndexerException_ShouldContainMessage()
    {
        // Arrange
        var message = "Type MyType does not have an indexer";

        // Act
        var exception = new ObjectDoesNotImplementIndexerException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    #endregion

    #region ObjectIsNotAnArrayException Tests

    [TestMethod]
    public void ObjectIsNotAnArrayException_ShouldContainMessage()
    {
        // Arrange
        var message = "Object is not an array";

        // Act
        var exception = new ObjectIsNotAnArrayException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    #endregion

    #region SetOperatorMustHaveKeyColumnsException Tests

    [TestMethod]
    public void SetOperatorMustHaveKeyColumnsException_ShouldContainOperatorName()
    {
        // Arrange
        var setOperator = "EXCEPT";

        // Act
        var exception = new SetOperatorMustHaveKeyColumnsException(setOperator);

        // Assert
        Assert.Contains(setOperator, exception.Message);
        Assert.Contains("keys", exception.Message);
    }

    #endregion

    #region SetOperatorMustHaveSameQuantityOfColumnsException Tests

    [TestMethod]
    public void SetOperatorMustHaveSameQuantityOfColumnsException_ShouldHaveCorrectMessage()
    {
        // Act
        var exception = new SetOperatorMustHaveSameQuantityOfColumnsException();

        // Assert
        Assert.Contains("same quantity", exception.Message);
    }

    #endregion

    #region TableIsNotDefinedException Tests

    [TestMethod]
    public void TableIsNotDefinedException_ShouldContainTableName()
    {
        // Arrange
        var tableName = "NonExistentTable";

        // Act
        var exception = new TableIsNotDefinedException(tableName);

        // Assert
        Assert.Contains(tableName, exception.Message);
        Assert.Contains("not defined", exception.Message);
    }

    #endregion

    #region TypeNotFoundException Tests

    [TestMethod]
    public void TypeNotFoundException_ShouldContainMessage()
    {
        // Arrange
        var message = "Type 'CustomType' was not found";

        // Act
        var exception = new TypeNotFoundException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    #endregion

    #region UnknownColumnOrAliasException Tests

    [TestMethod]
    public void UnknownColumnOrAliasException_ShouldContainMessage()
    {
        // Arrange
        var message = "Column 'xyz' is not known";

        // Act
        var exception = new UnknownColumnOrAliasException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    #endregion

    #region UnknownPropertyException Tests

    [TestMethod]
    public void UnknownPropertyException_ShouldContainMessage()
    {
        // Arrange
        var message = "Property 'Name' is unknown";

        // Act
        var exception = new UnknownPropertyException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    #endregion

    #region UnresolvableMethodException Tests

    [TestMethod]
    public void UnresolvableMethodException_ShouldContainMessage()
    {
        // Arrange
        var message = "Cannot resolve method 'DoSomething'";

        // Act
        var exception = new UnresolvableMethodException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    #endregion

    #region CannotResolveMethodException Tests

    [TestMethod]
    public void CannotResolveMethodException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Cannot resolve method TestMethod";

        // Act
        var exception = new CannotResolveMethodException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void CannotResolveMethodException_CreateForNullArguments_ShouldContainMethodName()
    {
        // Arrange
        var methodName = "TestMethod";

        // Act
        var exception = CannotResolveMethodException.CreateForNullArguments(methodName);

        // Assert
        Assert.Contains(methodName, exception.Message);
        Assert.Contains("null arguments", exception.Message);
    }

    #endregion

    #region QueryExecutionException Tests

    [TestMethod]
    public void QueryExecutionException_BasicConstructor_ShouldSetProperties()
    {
        // Arrange
        var context = "TestQuery";
        var phase = "Execution";
        var message = "Test error";

        // Act
        var exception = new QueryExecutionException(context, phase, message);

        // Assert
        Assert.AreEqual(context, exception.QueryContext);
        Assert.AreEqual(phase, exception.ExecutionPhase);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void QueryExecutionException_WithInnerException_ShouldSetProperties()
    {
        // Arrange
        var context = "TestQuery";
        var phase = "Execution";
        var message = "Test error";
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new QueryExecutionException(context, phase, message, innerException);

        // Assert
        Assert.AreEqual(context, exception.QueryContext);
        Assert.AreEqual(phase, exception.ExecutionPhase);
        Assert.AreEqual(innerException, exception.InnerException);
    }

    [TestMethod]
    public void QueryExecutionException_ForNullRunnable_ShouldCreateCorrectException()
    {
        // Act
        var exception = QueryExecutionException.ForNullRunnable();

        // Assert
        Assert.AreEqual("CompiledQuery", exception.QueryContext);
        Assert.AreEqual("Initialization", exception.ExecutionPhase);
        Assert.Contains("null", exception.Message);
    }

    [TestMethod]
    public void QueryExecutionException_ForExecutionFailure_ShouldWrapInnerException()
    {
        // Arrange
        var phase = "DataFetch";
        var innerException = new InvalidOperationException("Data source error");

        // Act
        var exception = QueryExecutionException.ForExecutionFailure(phase, innerException);

        // Assert
        Assert.AreEqual("CompiledQuery", exception.QueryContext);
        Assert.AreEqual(phase, exception.ExecutionPhase);
        Assert.AreEqual(innerException, exception.InnerException);
    }

    [TestMethod]
    public void QueryExecutionException_ForCancellationFailure_ShouldWrapInnerException()
    {
        // Arrange
        var phase = "Cleanup";
        var innerException = new InvalidOperationException("Cleanup error");

        // Act
        var exception = QueryExecutionException.ForCancellationFailure(phase, innerException);

        // Assert
        Assert.AreEqual("CompiledQuery", exception.QueryContext);
        Assert.AreEqual(phase, exception.ExecutionPhase);
        Assert.Contains("cancelled", exception.Message);
    }

    #endregion

    #region VisitorException Tests

    [TestMethod]
    public void VisitorException_BasicConstructor_ShouldSetProperties()
    {
        // Arrange
        var visitorName = "TestVisitor";
        var operation = "Visit";
        var message = "Error occurred";

        // Act
        var exception = new VisitorException(visitorName, operation, message);

        // Assert
        Assert.AreEqual(visitorName, exception.VisitorName);
        Assert.AreEqual(operation, exception.Operation);
        Assert.Contains(visitorName, exception.Message);
        Assert.Contains(operation, exception.Message);
    }

    [TestMethod]
    public void VisitorException_WithInnerException_ShouldWrapIt()
    {
        // Arrange
        var visitorName = "TestVisitor";
        var operation = "Visit";
        var message = "Error occurred";
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new VisitorException(visitorName, operation, message, innerException);

        // Assert
        Assert.AreEqual(innerException, exception.InnerException);
    }

    [TestMethod]
    public void VisitorException_WithNullVisitorName_ShouldUseDefault()
    {
        // Act
        var exception = new VisitorException(null, "operation", "message");

        // Assert
        Assert.AreEqual("Unknown", exception.VisitorName);
    }

    [TestMethod]
    public void VisitorException_WithNullOperation_ShouldUseDefault()
    {
        // Act
        var exception = new VisitorException("visitor", null, "message");

        // Assert
        Assert.AreEqual("Unknown", exception.Operation);
    }

    [TestMethod]
    public void VisitorException_CreateForStackUnderflow_ShouldContainCounts()
    {
        // Arrange
        var visitorName = "TestVisitor";
        var operation = "Pop";
        var expected = 3;
        var actual = 1;

        // Act
        var exception = VisitorException.CreateForStackUnderflow(visitorName, operation, expected, actual);

        // Assert
        Assert.AreEqual(visitorName, exception.VisitorName);
        Assert.AreEqual(operation, exception.Operation);
        Assert.Contains("3", exception.Message);
        Assert.Contains("1", exception.Message);
        Assert.Contains("Stack underflow", exception.Message);
    }

    [TestMethod]
    public void VisitorException_CreateForNullNode_ShouldContainNodeType()
    {
        // Arrange
        var visitorName = "TestVisitor";
        var operation = "VisitNode";
        var nodeType = "SelectNode";

        // Act
        var exception = VisitorException.CreateForNullNode(visitorName, operation, nodeType);

        // Assert
        Assert.AreEqual(visitorName, exception.VisitorName);
        Assert.AreEqual(operation, exception.Operation);
        Assert.Contains(nodeType, exception.Message);
        Assert.Contains("null", exception.Message);
    }

    [TestMethod]
    public void VisitorException_CreateForInvalidNodeType_ShouldContainBothTypes()
    {
        // Arrange
        var visitorName = "TestVisitor";
        var operation = "Cast";
        var expectedType = "SelectNode";
        var actualType = "WhereNode";

        // Act
        var exception = VisitorException.CreateForInvalidNodeType(visitorName, operation, expectedType, actualType);

        // Assert
        Assert.Contains(expectedType, exception.Message);
        Assert.Contains(actualType, exception.Message);
    }

    [TestMethod]
    public void VisitorException_CreateForProcessingFailure_WithSuggestion()
    {
        // Arrange
        var visitorName = "TestVisitor";
        var operation = "Process";
        var context = "Failed to process node";
        var suggestion = "Try rewriting the query";

        // Act
        var exception = VisitorException.CreateForProcessingFailure(visitorName, operation, context, suggestion);

        // Assert
        Assert.Contains(context, exception.Message);
        Assert.Contains(suggestion, exception.Message);
    }

    [TestMethod]
    public void VisitorException_CreateForProcessingFailure_WithoutSuggestion()
    {
        // Arrange
        var visitorName = "TestVisitor";
        var operation = "Process";
        var context = "Failed to process node";

        // Act
        var exception = VisitorException.CreateForProcessingFailure(visitorName, operation, context);

        // Assert
        Assert.Contains(context, exception.Message);
    }

    #endregion

    #region InvalidQueryExpressionTypeException Tests

    [TestMethod]
    public void InvalidQueryExpressionTypeException_WithExpressionDescription_ShouldContainType()
    {
        // Arrange
        var description = "column1";
        var invalidType = typeof(object);
        var context = "SELECT clause";

        // Act
        var exception = new InvalidQueryExpressionTypeException(description, invalidType, context);

        // Assert
        Assert.Contains(description, exception.Message);
        Assert.Contains("System.Object", exception.Message);
        Assert.Contains(context, exception.Message);
    }

    [TestMethod]
    public void InvalidQueryExpressionTypeException_WithNullType_ShouldHandleGracefully()
    {
        // Act
        var exception = new InvalidQueryExpressionTypeException("expr", null, "context");

        // Assert
        Assert.Contains("null", exception.Message);
    }

    #endregion
}
