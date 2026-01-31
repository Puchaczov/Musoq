namespace Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

/// <summary>
///     Traverses a query subtree and visits all nodes to extract CTE references.
///     Used in conjunction with <see cref="CteReferenceExtractor" />.
/// </summary>
public class CteReferenceExtractorTraverser : RawTraverseVisitor<CteReferenceExtractor>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CteReferenceExtractorTraverser" /> class.
    /// </summary>
    /// <param name="extractor">The CTE reference extractor to use.</param>
    public CteReferenceExtractorTraverser(CteReferenceExtractor extractor)
        : base(extractor)
    {
    }
}
