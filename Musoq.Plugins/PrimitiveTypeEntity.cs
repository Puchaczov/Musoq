namespace Musoq.Plugins;

/// <summary>
///     Represents primitive type entity.
/// </summary>
/// <typeparam name="TPrimitive">Type of the primitive.</typeparam>
public class PrimitiveTypeEntity<TPrimitive>(TPrimitive value)
{
    /// <summary>
    ///     Gets the value of the primitive.
    /// </summary>
    public TPrimitive Value { get; } = value;
}