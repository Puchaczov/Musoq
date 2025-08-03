using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Exceptions;
using System;

namespace Musoq.Schema.Tests;

[TestClass]
public class DataSourceConnectionExceptionTests
{
    [TestMethod]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var dataSourceName = "test-source";
        var dataSourceType = "file";
        var message = "Connection failed";
        var connectionString = "path=/test/file.csv";

        // Act
        var exception = new DataSourceConnectionException(dataSourceName, dataSourceType, message, connectionString);

        // Assert
        Assert.AreEqual(dataSourceName, exception.DataSourceName);
        Assert.AreEqual(dataSourceType, exception.DataSourceType);
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(connectionString, exception.ConnectionString);
    }

    [TestMethod]
    public void ForConnectionFailure_ShouldCreateAppropriateException()
    {
        // Arrange
        var dataSourceName = "test-db";
        var dataSourceType = "database";
        var innerException = new InvalidOperationException("Network timeout");
        var connectionString = "server=localhost;database=test";

        // Act
        var exception = DataSourceConnectionException.ForConnectionFailure(dataSourceName, dataSourceType, innerException, connectionString);

        // Assert
        Assert.AreEqual(dataSourceName, exception.DataSourceName);
        Assert.AreEqual(dataSourceType, exception.DataSourceType);
        Assert.AreEqual(connectionString, exception.ConnectionString);
        Assert.AreEqual(innerException, exception.InnerException);
        Assert.IsTrue(exception.Message.Contains("Failed to connect"));
        Assert.IsTrue(exception.Message.Contains("Network connectivity"));
        Assert.IsTrue(exception.Message.Contains("Connection parameters"));
    }

    [TestMethod]
    public void ForConnectionFailure_WithFileType_ShouldProvideFileAdvice()
    {
        // Arrange
        var dataSourceName = "csv-source";
        var dataSourceType = "file";
        var innerException = new System.IO.FileNotFoundException();

        // Act
        var exception = DataSourceConnectionException.ForConnectionFailure(dataSourceName, dataSourceType, innerException);

        // Assert
        Assert.IsTrue(exception.Message.Contains("file path exists and is accessible"));
    }

    [TestMethod]
    public void ForConnectionFailure_WithGitType_ShouldProvideGitAdvice()
    {
        // Arrange
        var dataSourceName = "repo-source";
        var dataSourceType = "git";
        var innerException = new InvalidOperationException();

        // Act
        var exception = DataSourceConnectionException.ForConnectionFailure(dataSourceName, dataSourceType, innerException);

        // Assert
        Assert.IsTrue(exception.Message.Contains("repository path is valid and accessible"));
    }

    [TestMethod]
    public void ForConnectionFailure_WithWebType_ShouldProvideWebAdvice()
    {
        // Arrange
        var dataSourceName = "api-source";
        var dataSourceType = "web";
        var innerException = new System.Net.Http.HttpRequestException();

        // Act
        var exception = DataSourceConnectionException.ForConnectionFailure(dataSourceName, dataSourceType, innerException);

        // Assert
        Assert.IsTrue(exception.Message.Contains("URL accessibility and network connectivity"));
    }

    [TestMethod]
    public void ForInitializationFailure_ShouldCreateAppropriateException()
    {
        // Arrange
        var dataSourceName = "plugin-source";
        var dataSourceType = "custom";
        var innerException = new TypeLoadException("Could not load plugin");

        // Act
        var exception = DataSourceConnectionException.ForInitializationFailure(dataSourceName, dataSourceType, innerException);

        // Assert
        Assert.AreEqual(dataSourceName, exception.DataSourceName);
        Assert.AreEqual(dataSourceType, exception.DataSourceType);
        Assert.AreEqual(innerException, exception.InnerException);
        Assert.IsTrue(exception.Message.Contains("Failed to initialize"));
        Assert.IsTrue(exception.Message.Contains("plugin installation"));
        Assert.IsTrue(exception.Message.Contains("Configuration parameters"));
        Assert.IsTrue(exception.Message.Contains("Required dependencies"));
    }

    [TestMethod]
    public void ForInvalidParameters_ShouldCreateAppropriateException()
    {
        // Arrange
        var dataSourceName = "param-source";
        var dataSourceType = "file";
        var parameterIssue = "File path cannot be empty";

        // Act
        var exception = DataSourceConnectionException.ForInvalidParameters(dataSourceName, dataSourceType, parameterIssue);

        // Assert
        Assert.AreEqual(dataSourceName, exception.DataSourceName);
        Assert.AreEqual(dataSourceType, exception.DataSourceType);
        Assert.IsTrue(exception.Message.Contains("Invalid parameters"));
        Assert.IsTrue(exception.Message.Contains(parameterIssue));
        Assert.IsTrue(exception.Message.Contains("File sources typically require a valid file path"));
        Assert.IsTrue(exception.Message.Contains("refer to the documentation"));
    }

    [TestMethod]
    public void ForInvalidParameters_WithDatabaseType_ShouldProvideDatabaseAdvice()
    {
        // Arrange
        var dataSourceName = "db-source";
        var dataSourceType = "database";
        var parameterIssue = "Invalid connection string format";

        // Act
        var exception = DataSourceConnectionException.ForInvalidParameters(dataSourceName, dataSourceType, parameterIssue);

        // Assert
        Assert.IsTrue(exception.Message.Contains("Database sources require a valid connection string"));
    }

    [TestMethod]
    public void ForInvalidParameters_WithWebType_ShouldProvideWebAdvice()
    {
        // Arrange
        var dataSourceName = "web-source";
        var dataSourceType = "http";
        var parameterIssue = "Invalid URL format";

        // Act
        var exception = DataSourceConnectionException.ForInvalidParameters(dataSourceName, dataSourceType, parameterIssue);

        // Assert
        Assert.IsTrue(exception.Message.Contains("Web sources require a valid URL"));
    }

    [TestMethod]
    public void ForTimeout_ShouldCreateAppropriateException()
    {
        // Arrange
        var dataSourceName = "slow-source";
        var dataSourceType = "database";
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var exception = DataSourceConnectionException.ForTimeout(dataSourceName, dataSourceType, timeout);

        // Assert
        Assert.AreEqual(dataSourceName, exception.DataSourceName);
        Assert.AreEqual(dataSourceType, exception.DataSourceType);
        Assert.IsTrue(exception.Message.Contains("timed out after 30 seconds"));
        Assert.IsTrue(exception.Message.Contains("Increasing the timeout value"));
        Assert.IsTrue(exception.Message.Contains("network connectivity"));
        Assert.IsTrue(exception.Message.Contains("data source performance"));
    }

    [TestMethod]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var dataSourceName = "error-source";
        var dataSourceType = "file";
        var message = "Test error";
        var innerException = new ArgumentException("Inner error");

        // Act
        var exception = new DataSourceConnectionException(dataSourceName, dataSourceType, message, innerException);

        // Assert
        Assert.AreEqual(innerException, exception.InnerException);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void Constructor_WithNullParameters_ShouldUseEmptyStrings()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new DataSourceConnectionException(null, null, message, null);

        // Assert
        Assert.AreEqual(string.Empty, exception.DataSourceName);
        Assert.AreEqual(string.Empty, exception.DataSourceType);
        Assert.AreEqual(string.Empty, exception.ConnectionString);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void ForInvalidParameters_WithUnknownType_ShouldProvideGenericAdvice()
    {
        // Arrange
        var dataSourceName = "unknown-source";
        var dataSourceType = "unknown";
        var parameterIssue = "Some parameter issue";

        // Act
        var exception = DataSourceConnectionException.ForInvalidParameters(dataSourceName, dataSourceType, parameterIssue);

        // Assert
        Assert.IsTrue(exception.Message.Contains("Check the parameter format required for this data source type"));
    }
}