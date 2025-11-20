using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Exceptions;
using System;
using System.Linq;

namespace Musoq.Schema.Tests;

[TestClass]
public class PluginLoadExceptionTests
{
    [TestMethod]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var pluginName = "TestPlugin";
        var message = "Plugin load failed";
        var pluginPath = "/path/to/plugin.dll";
        var availablePlugins = new[] { "Plugin1", "Plugin2" };

        // Act
        var exception = new PluginLoadException(pluginName, message, pluginPath, availablePlugins);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(pluginPath, exception.PluginPath);
        Assert.AreEqual(2, exception.AvailablePlugins.Length);
        Assert.IsTrue(exception.AvailablePlugins.Contains("Plugin1"));
        Assert.IsTrue(exception.AvailablePlugins.Contains("Plugin2"));
    }

    [TestMethod]
    public void ForPluginNotFound_WithAvailablePlugins_ShouldCreateAppropriateException()
    {
        // Arrange
        var pluginName = "MissingPlugin";
        var availablePlugins = new[] { "FilePlugin", "GitPlugin", "DatabasePlugin" };

        // Act
        var exception = PluginLoadException.ForPluginNotFound(pluginName, availablePlugins);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.IsTrue(exception.Message.Contains($"Plugin '{pluginName}' was not found"));
        Assert.IsTrue(exception.Message.Contains("Available plugins:"));
        Assert.IsTrue(exception.Message.Contains("- FilePlugin"));
        Assert.IsTrue(exception.Message.Contains("- GitPlugin"));
        Assert.IsTrue(exception.Message.Contains("- DatabasePlugin"));
        Assert.IsTrue(exception.Message.Contains("Plugin name spelling"));
        Assert.IsTrue(exception.Message.Contains("Plugin installation"));
        Assert.AreEqual(3, exception.AvailablePlugins.Length);
    }

    [TestMethod]
    public void ForPluginNotFound_WithNoAvailablePlugins_ShouldCreateAppropriateException()
    {
        // Arrange
        var pluginName = "MissingPlugin";
        var availablePlugins = new string[0];

        // Act
        var exception = PluginLoadException.ForPluginNotFound(pluginName, availablePlugins);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.IsTrue(exception.Message.Contains($"Plugin '{pluginName}' was not found"));
        Assert.IsTrue(exception.Message.Contains("No plugins are currently loaded"));
        Assert.IsFalse(exception.Message.Contains("Available plugins:"));
        Assert.AreEqual(0, exception.AvailablePlugins.Length);
    }

    [TestMethod]
    public void ForLoadFailure_ShouldCreateAppropriateException()
    {
        // Arrange
        var pluginName = "FailedPlugin";
        var pluginPath = "/path/to/failed.dll";
        var innerException = new System.IO.FileLoadException("Could not load assembly");

        // Act
        var exception = PluginLoadException.ForLoadFailure(pluginName, pluginPath, innerException);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.AreEqual(pluginPath, exception.PluginPath);
        Assert.AreEqual(innerException, exception.InnerException);
        Assert.IsTrue(exception.Message.Contains($"Failed to load plugin '{pluginName}'"));
        Assert.IsTrue(exception.Message.Contains($"from '{pluginPath}'"));
        Assert.IsTrue(exception.Message.Contains("Missing dependencies"));
        Assert.IsTrue(exception.Message.Contains("Incompatible .NET version"));
        Assert.IsTrue(exception.Message.Contains("Corrupted assembly file"));
        Assert.IsTrue(exception.Message.Contains("Security restrictions"));
    }

    [TestMethod]
    public void ForRegistrationFailure_ShouldCreateAppropriateException()
    {
        // Arrange
        var pluginName = "BadPlugin";
        var innerException = new TypeLoadException("Invalid plugin type");

        // Act
        var exception = PluginLoadException.ForRegistrationFailure(pluginName, innerException);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.AreEqual(innerException, exception.InnerException);
        Assert.IsTrue(exception.Message.Contains($"Failed to register plugin '{pluginName}'"));
        Assert.IsTrue(exception.Message.Contains("loaded but could not be properly initialized"));
        Assert.IsTrue(exception.Message.Contains("Invalid plugin configuration"));
        Assert.IsTrue(exception.Message.Contains("Missing required interfaces"));
        Assert.IsTrue(exception.Message.Contains("Initialization errors"));
        Assert.IsTrue(exception.Message.Contains("Dependency conflicts"));
    }

    [TestMethod]
    public void ForDuplicateRegistration_ShouldCreateAppropriateException()
    {
        // Arrange
        var pluginName = "DuplicatePlugin";
        var existingPluginInfo = "Version 1.0, loaded from /path/to/existing.dll";

        // Act
        var exception = PluginLoadException.ForDuplicateRegistration(pluginName, existingPluginInfo);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.IsTrue(exception.Message.Contains($"plugin named '{pluginName}' is already registered"));
        Assert.IsTrue(exception.Message.Contains($"Existing plugin: {existingPluginInfo}"));
        Assert.IsTrue(exception.Message.Contains("Plugin names must be unique"));
        Assert.IsTrue(exception.Message.Contains("Use a different plugin name"));
        Assert.IsTrue(exception.Message.Contains("Unregister the existing plugin"));
        Assert.IsTrue(exception.Message.Contains("duplicate plugin installations"));
    }

    [TestMethod]
    public void ForInvalidInterface_ShouldCreateAppropriateException()
    {
        // Arrange
        var pluginName = "InvalidPlugin";
        var expectedInterface = "IDataSourcePlugin";

        // Act
        var exception = PluginLoadException.ForInvalidInterface(pluginName, expectedInterface);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.IsTrue(exception.Message.Contains($"Plugin '{pluginName}' does not implement"));
        Assert.IsTrue(exception.Message.Contains($"required interface '{expectedInterface}'"));
        Assert.IsTrue(exception.Message.Contains("implement the proper interfaces"));
        Assert.IsTrue(exception.Message.Contains("Implements the correct interface"));
        Assert.IsTrue(exception.Message.Contains("proper method signatures"));
        Assert.IsTrue(exception.Message.Contains("compatible Musoq libraries"));
    }

    [TestMethod]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var pluginName = "ErrorPlugin";
        var message = "Plugin error";
        var innerException = new InvalidOperationException("Inner error");
        var pluginPath = "/path/to/plugin.dll";

        // Act
        var exception = new PluginLoadException(pluginName, message, innerException, pluginPath);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(innerException, exception.InnerException);
        Assert.AreEqual(pluginPath, exception.PluginPath);
    }

    [TestMethod]
    public void Constructor_WithNullParameters_ShouldUseEmptyValues()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new PluginLoadException(null, message, null, null);

        // Assert
        Assert.AreEqual(string.Empty, exception.PluginName);
        Assert.AreEqual(string.Empty, exception.PluginPath);
        Assert.AreEqual(0, exception.AvailablePlugins.Length);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void ForPluginNotFound_WithNullAvailablePlugins_ShouldHandleGracefully()
    {
        // Arrange
        var pluginName = "TestPlugin";

        // Act
        var exception = PluginLoadException.ForPluginNotFound(pluginName, null);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.IsTrue(exception.Message.Contains("No plugins are currently loaded"));
        Assert.AreEqual(0, exception.AvailablePlugins.Length);
    }

    [TestMethod]
    public void ForLoadFailure_WithNullInnerException_ShouldStillCreateException()
    {
        // Arrange
        var pluginName = "TestPlugin";
        var pluginPath = "/path/to/plugin.dll";

        // Act
        var exception = PluginLoadException.ForLoadFailure(pluginName, pluginPath, null);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.AreEqual(pluginPath, exception.PluginPath);
        Assert.IsNull(exception.InnerException);
        Assert.IsTrue(exception.Message.Contains("Failed to load plugin"));
    }

    [TestMethod]
    public void ForRegistrationFailure_WithNullInnerException_ShouldStillCreateException()
    {
        // Arrange
        var pluginName = "TestPlugin";

        // Act
        var exception = PluginLoadException.ForRegistrationFailure(pluginName, null);

        // Assert
        Assert.AreEqual(pluginName, exception.PluginName);
        Assert.IsNull(exception.InnerException);
        Assert.IsTrue(exception.Message.Contains("Failed to register plugin"));
    }
}