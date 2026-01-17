using System;

namespace Musoq.Schema.Attributes;

/// <summary>
///     Attribute to mark available schemas within the plugin
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class PluginSchemasAttribute : Attribute
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="schemas">Schemas names.</param>
    public PluginSchemasAttribute(params string[] schemas)
    {
        Schemas = schemas;
    }

    /// <summary>
    ///     Schemas names.
    /// </summary>
    public string[] Schemas { get; }
}