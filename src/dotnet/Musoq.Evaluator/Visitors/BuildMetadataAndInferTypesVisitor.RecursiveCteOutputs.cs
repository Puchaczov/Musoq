using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private string? _activeRecursiveCteName;

    internal void VisitRecursiveCteBoundary(string cteName, SetOperatorNode boundary)
    {
        var previousName = _activeRecursiveCteName;
        _activeRecursiveCteName = cteName;
        try
        {
            boundary.Accept(this);
        }
        finally
        {
            _activeRecursiveCteName = previousName;
        }
    }
}
