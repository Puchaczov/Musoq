#nullable enable

using System;
using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

/// <summary>
///     Represents a registered interpretation schema within a query batch.
///     Contains the AST node and generated type information.
/// </summary>
public class SchemaRegistration
{
    /// <summary>
    ///     Creates a new schema registration.
    /// </summary>
    /// <param name="name">The schema name.</param>
    /// <param name="node">The AST node representing the schema definition.</param>
    public SchemaRegistration(string name, Node node)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    /// <summary>
    ///     Gets the name of the schema.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the AST node for the schema definition.
    /// </summary>
    public Node Node { get; }

    /// <summary>
    ///     Gets or sets the generated type for this schema.
    ///     Set after code generation is complete (only when a separate assembly is compiled).
    /// </summary>
    public Type? GeneratedType { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified name of the generated interpreter type.
    ///     Used when the interpreter source is embedded in the main assembly.
    /// </summary>
    public string? GeneratedTypeName { get; set; }

    /// <summary>
    ///     Gets a value indicating whether this is a binary schema.
    /// </summary>
    public bool IsBinarySchema => Node is BinarySchemaNode;

    /// <summary>
    ///     Gets a value indicating whether this is a text schema.
    /// </summary>
    public bool IsTextSchema => Node is TextSchemaNode;
}

/// <summary>
///     Registry for tracking interpretation schema definitions within a query batch.
///     Schemas must be defined before they can be referenced by other schemas or queries.
/// </summary>
public class SchemaRegistry
{
    private readonly List<SchemaRegistration> _orderedSchemas = [];
    private readonly Dictionary<string, SchemaRegistration> _schemas = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets all registered schemas in the order they were defined.
    /// </summary>
    public IReadOnlyList<SchemaRegistration> Schemas => _orderedSchemas;

    /// <summary>
    ///     Gets the number of registered schemas.
    /// </summary>
    public int Count => _schemas.Count;

    /// <summary>
    ///     Registers a schema definition.
    /// </summary>
    /// <param name="name">The unique name of the schema.</param>
    /// <param name="node">The AST node representing the schema definition.</param>
    /// <exception cref="ArgumentNullException">Thrown when name or node is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a schema with the same name is already registered.</exception>
    public void Register(string name, Node node)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (_schemas.ContainsKey(name))
            throw new InvalidOperationException(
                $"Schema '{name}' is already defined. Schema names must be unique within a query batch.");

        var registration = new SchemaRegistration(name, node);
        _schemas[name] = registration;
        _orderedSchemas.Add(registration);
    }

    /// <summary>
    ///     Attempts to get a registered schema by name.
    /// </summary>
    /// <param name="name">The schema name to look up.</param>
    /// <param name="registration">The schema registration if found; otherwise, null.</param>
    /// <returns>True if the schema was found; otherwise, false.</returns>
    public bool TryGetSchema(string name, out SchemaRegistration? registration)
    {
        return _schemas.TryGetValue(name, out registration);
    }

    /// <summary>
    ///     Gets a registered schema by name.
    /// </summary>
    /// <param name="name">The schema name to look up.</param>
    /// <returns>The schema registration.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the schema is not found.</exception>
    public SchemaRegistration GetSchema(string name)
    {
        if (!_schemas.TryGetValue(name, out var registration))
            throw new KeyNotFoundException($"Schema '{name}' is not defined. Ensure it is declared before use.");
        return registration;
    }

    /// <summary>
    ///     Gets a value indicating whether a schema with the specified name is registered.
    /// </summary>
    /// <param name="name">The schema name to check.</param>
    /// <returns>True if the schema exists; otherwise, false.</returns>
    public bool ContainsSchema(string name)
    {
        return _schemas.ContainsKey(name);
    }

    /// <summary>
    ///     Validates that a schema reference is valid (schema exists and was defined before the reference).
    /// </summary>
    /// <param name="referencedName">The name of the referenced schema.</param>
    /// <param name="referencingName">The name of the schema making the reference.</param>
    /// <exception cref="InvalidOperationException">Thrown when the reference is invalid.</exception>
    public void ValidateReference(string referencedName, string referencingName)
    {
        if (!_schemas.TryGetValue(referencedName, out var referenced))
            throw new InvalidOperationException(
                $"Schema '{referencingName}' references undefined schema '{referencedName}'. " +
                $"Schemas must be defined before they can be referenced.");


        var referencedIndex = _orderedSchemas.IndexOf(referenced);
        var referencingIndex = -1;
        for (var i = 0; i < _orderedSchemas.Count; i++)
            if (_orderedSchemas[i].Name.Equals(referencingName, StringComparison.OrdinalIgnoreCase))
            {
                referencingIndex = i;
                break;
            }

        if (referencingIndex >= 0 && referencedIndex >= referencingIndex)
            throw new InvalidOperationException(
                $"Schema '{referencingName}' references '{referencedName}', but '{referencedName}' " +
                $"is defined after '{referencingName}'. Referenced schemas must be defined first.");
    }

    /// <summary>
    ///     Clears all registered schemas.
    /// </summary>
    public void Clear()
    {
        _schemas.Clear();
        _orderedSchemas.Clear();
    }
}
