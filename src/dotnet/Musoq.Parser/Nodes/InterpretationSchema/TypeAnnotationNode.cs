using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Base class for all type annotations in interpretation schemas.
///     Represents the type specification portion of a field definition.
/// </summary>
public abstract class TypeAnnotationNode : Node
{
    /// <summary>
    ///     Gets the .NET type that this annotation represents.
    /// </summary>
    public abstract Type ClrType { get; }

    /// <summary>
    ///     Gets whether this type has a fixed size in bytes.
    /// </summary>
    public abstract bool IsFixedSize { get; }

    /// <summary>
    ///     Gets the fixed size in bytes, or null if size is dynamic.
    /// </summary>
    public abstract int? FixedSizeBytes { get; }
}
