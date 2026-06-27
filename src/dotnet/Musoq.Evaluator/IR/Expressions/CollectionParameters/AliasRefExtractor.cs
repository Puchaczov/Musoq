using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class AliasRefExtractor
{
    protected override IReadOnlyList<string> VisitCollectionInCheck(CollectionInCheck node)
    {
        Visit(node.Expression);
        return Enumerable.ToArray<string>(_aliases);
    }
}
