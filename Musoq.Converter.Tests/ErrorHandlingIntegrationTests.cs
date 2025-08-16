using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Exceptions;

namespace Musoq.Converter.Tests;

[TestClass]
public class ErrorHandlingIntegrationTests
{
    [TestMethod]
    public void WhenQueryContainsBackslash_ShouldProvideHelpfulError()
    {
        const string queryWithBackslash = "select 5 \\ 2 from #test.source()";
        
        var exception = Assert.ThrowsException<AstValidationException>(() =>
        {
            InstanceCreator.CompileForExecution(queryWithBackslash, "TestAssembly", null, null);
        });
        
        // Debug output
        Console.WriteLine($"Exception Message: {exception.Message}");
        Console.WriteLine($"Inner Exception: {exception.InnerException?.GetType()?.Name}");
        Console.WriteLine($"Inner Exception Message: {exception.InnerException?.Message}");
        
        Assert.IsNotNull(exception.InnerException);
        Assert.IsInstanceOfType(exception.InnerException, typeof(QueryValidationException));
        
        var validationException = (QueryValidationException)exception.InnerException;
        Assert.IsTrue(validationException.Message.Contains("problematic characters") && validationException.Message.Contains("\\"));
    }
    
    [TestMethod]
    public void WhenQueryContainsQuestionMark_ShouldProvideHelpfulError()
    {
        const string queryWithQuestionMark = "select id from #test.source() where name = ?";
        
        var exception = Assert.ThrowsException<AstValidationException>(() =>
        {
            InstanceCreator.CompileForExecution(queryWithQuestionMark, "TestAssembly", null, null);
        });
        
        // Debug output
        Console.WriteLine($"Exception Message: {exception.Message}");
        Console.WriteLine($"Inner Exception: {exception.InnerException?.GetType()?.Name}");
        Console.WriteLine($"Inner Exception Message: {exception.InnerException?.Message}");
        
        Assert.IsNotNull(exception.InnerException);
        Assert.IsInstanceOfType(exception.InnerException, typeof(QueryValidationException));
        
        var validationException = (QueryValidationException)exception.InnerException;
        Assert.IsTrue(validationException.Message.Contains("problematic characters") && validationException.Message.Contains("?"));
    }
    
    [TestMethod]
    public void WhenQueryIsEmpty_ShouldProvideHelpfulError()
    {
        const string emptyQuery = "";
        
        var exception = Assert.ThrowsException<AstValidationException>(() =>
        {
            InstanceCreator.CompileForExecution(emptyQuery, "TestAssembly", null, null);
        });
        
        // Debug output
        Console.WriteLine($"Exception Message: {exception.Message}");
        Console.WriteLine($"Inner Exception: {exception.InnerException?.GetType()?.Name}");
        
        // Should be caught by the early null check, not validation
        Assert.IsTrue(exception.Message.Contains("RawQuery cannot be null or whitespace"));
    }
    
    [TestMethod] 
    public void WhenQueryHasUnbalancedParentheses_ShouldProvideHelpfulError()
    {
        const string unbalancedQuery = "select sum(column from #test.source()";
        
        var exception = Assert.ThrowsException<AstValidationException>(() =>
        {
            InstanceCreator.CompileForExecution(unbalancedQuery, "TestAssembly", null, null);
        });
        
        Assert.IsNotNull(exception.InnerException);
        Assert.IsInstanceOfType(exception.InnerException, typeof(QueryValidationException));
        
        var validationException = (QueryValidationException)exception.InnerException;
        Assert.IsTrue(validationException.Message.Contains("Missing") && validationException.Message.Contains("closing parenthesis"));
    }
    
    [TestMethod]
    public void WhenQueryIsValid_ShouldPassValidation()
    {
        const string validQuery = "select 1 as Value";
        
        // This should not throw an exception during validation
        // (it might fail later due to missing schema, but validation should pass)
        var exception = Assert.ThrowsException<AstValidationException>(() =>
        {
            InstanceCreator.CompileForExecution(validQuery, "TestAssembly", null, null);
        });
        
        // Should fail for a different reason (missing schema/compilation), not validation
        Assert.IsFalse(exception.Message.Contains("Query validation failed"));
    }
}