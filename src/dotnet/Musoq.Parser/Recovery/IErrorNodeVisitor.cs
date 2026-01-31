namespace Musoq.Parser.Recovery;

/// <summary>
///     Interface for visitors that can handle error nodes.
/// </summary>
public interface IErrorNodeVisitor
{
    /// <summary>
    ///     Visits an error node.
    /// </summary>
    void VisitError(ErrorNode node);
}
