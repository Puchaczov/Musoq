namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Specifies how a length-bounded binary substream is interpreted.
/// </summary>
public enum SubstreamMode
{
    /// <summary>
    ///     Returns the bounded slice as a raw <c>byte[]</c> without further parsing.
    /// </summary>
    Raw,

    /// <summary>
    ///     Parses the bounded slice with the target type and requires the nested parser
    ///     to consume exactly the declared length.
    /// </summary>
    Exact,

    /// <summary>
    ///     Parses the bounded slice with the target type and permits unconsumed trailing
    ///     bytes inside the substream.
    /// </summary>
    Lax
}
