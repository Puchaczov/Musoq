namespace Musoq.Plugins;

/// <summary>
///     Provides optional aggregate capability descriptors for a window function factory.
/// </summary>
public interface IWindowAggregateCapabilityProvider
{
    /// <summary>
    ///     Returns a capability descriptor for the requested shape, or null when the shape should use fallback dispatch.
    /// </summary>
    /// <param name="context">Requested function and type shape.</param>
    /// <returns>A supported capability descriptor, or null.</returns>
    WindowAggregateCapability? GetCapability(WindowAggregateCapabilityContext context);
}
