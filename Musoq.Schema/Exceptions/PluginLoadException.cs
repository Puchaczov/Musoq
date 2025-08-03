using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.Exceptions;

/// <summary>
/// Exception thrown when plugin loading or registration fails.
/// Provides detailed information about plugin issues and resolution steps.
/// </summary>
public class PluginLoadException : InvalidOperationException
{
    public string PluginName { get; }
    public string PluginPath { get; }
    public string[] AvailablePlugins { get; }

    public PluginLoadException(string pluginName, string message, string pluginPath = null, string[] availablePlugins = null)
        : base(message)
    {
        PluginName = pluginName ?? string.Empty;
        PluginPath = pluginPath ?? string.Empty;
        AvailablePlugins = availablePlugins ?? new string[0];
    }

    public PluginLoadException(string pluginName, string message, Exception innerException, string pluginPath = null, string[] availablePlugins = null)
        : base(message, innerException)
    {
        PluginName = pluginName ?? string.Empty;
        PluginPath = pluginPath ?? string.Empty;
        AvailablePlugins = availablePlugins ?? new string[0];
    }

    public static PluginLoadException ForPluginNotFound(string pluginName, string[] availablePlugins)
    {
        var availableText = availablePlugins?.Length > 0
            ? $"\n\nAvailable plugins:\n{string.Join("\n", availablePlugins.Select(p => $"- {p}"))}"
            : "\n\nNo plugins are currently loaded.";

        var message = $"Plugin '{pluginName}' was not found.{availableText}" +
                     "\n\nPlease check:\n" +
                     "- Plugin name spelling\n" +
                     "- Plugin installation\n" +
                     "- Plugin registration\n" +
                     "- Assembly loading path";

        return new PluginLoadException(pluginName, message, availablePlugins: availablePlugins);
    }

    public static PluginLoadException ForLoadFailure(string pluginName, string pluginPath, Exception innerException)
    {
        var message = $"Failed to load plugin '{pluginName}' from '{pluginPath}'. " +
                     "This usually indicates a compatibility or dependency issue. " +
                     "\n\nPossible causes:\n" +
                     "- Missing dependencies\n" +
                     "- Incompatible .NET version\n" +
                     "- Corrupted assembly file\n" +
                     "- Security restrictions\n" +
                     "\nPlease ensure the plugin is compatible and all dependencies are available.";

        return new PluginLoadException(pluginName, message, innerException, pluginPath);
    }

    public static PluginLoadException ForRegistrationFailure(string pluginName, Exception innerException)
    {
        var message = $"Failed to register plugin '{pluginName}'. " +
                     "The plugin was loaded but could not be properly initialized. " +
                     "\n\nThis may be due to:\n" +
                     "- Invalid plugin configuration\n" +
                     "- Missing required interfaces\n" +
                     "- Initialization errors\n" +
                     "- Dependency conflicts\n" +
                     "\nPlease check the plugin implementation and configuration.";

        return new PluginLoadException(pluginName, message, innerException);
    }

    public static PluginLoadException ForDuplicateRegistration(string pluginName, string existingPluginInfo)
    {
        var message = $"A plugin named '{pluginName}' is already registered. " +
                     $"Existing plugin: {existingPluginInfo}" +
                     "\n\nPlugin names must be unique. Please:\n" +
                     "- Use a different plugin name\n" +
                     "- Unregister the existing plugin first\n" +
                     "- Check for duplicate plugin installations";

        return new PluginLoadException(pluginName, message);
    }

    public static PluginLoadException ForInvalidInterface(string pluginName, string expectedInterface)
    {
        var message = $"Plugin '{pluginName}' does not implement the required interface '{expectedInterface}'. " +
                     "All plugins must implement the proper interfaces to be compatible with Musoq. " +
                     "\n\nPlease ensure the plugin:\n" +
                     "- Implements the correct interface\n" +
                     "- Has proper method signatures\n" +
                     "- Is compiled against compatible Musoq libraries";

        return new PluginLoadException(pluginName, message);
    }
}