using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(WindowFunctionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var typedVisitor = (BuildMetadataAndInferTypesVisitor)Visitor;
        typedVisitor.InsideWindowFunction = true;
        try
        {
            if (node.FunctionCall.Arguments != null)
            {
                foreach (var arg in node.FunctionCall.Arguments.Args)
                    arg.Accept(this);
            }

            node.FunctionCall.FilterExpression?.Accept(this);
            node.WindowSpecification?.Accept(this);
            node.Accept(Visitor);
        }
        finally
        {
            typedVisitor.InsideWindowFunction = false;
        }
    }
}
