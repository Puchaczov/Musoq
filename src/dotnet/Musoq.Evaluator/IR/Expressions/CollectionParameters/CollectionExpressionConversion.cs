using Musoq.Parser.Nodes;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private CollectionInCheck ConvertCollectionInNode(CollectionInNode node)
    {
        var expression = Convert(node.Left);
        var collection = Convert(node.Collection) as ScriptParameterRef ??
                         throw new InvalidOperationException("Collection IN requires a script parameter reference.");
        var elementType = collection.ReturnType.GetElementType() ??
                          throw new InvalidOperationException(
                              $"Collection IN parameter '${collection.Name}' is missing an element type.");

        return new CollectionInCheck(expression, collection, elementType, RequireReturnType(node));
    }
}
