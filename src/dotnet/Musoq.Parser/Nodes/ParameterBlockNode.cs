using System.Linq;

namespace Musoq.Parser.Nodes;

public class ParameterBlockNode : Node
{
    public ParameterBlockNode(ParameterDeclarationNode[] parameters)
        : this(parameters, default)
    {
    }

    public ParameterBlockNode(ParameterDeclarationNode[] parameters, TextSpan span)
    {
        Parameters = parameters ?? Array.Empty<ParameterDeclarationNode>();
        Id = $"{nameof(ParameterBlockNode)}{string.Join(",", Parameters.Select(parameter => parameter.Id))}";
        Span = span;
        FullSpan = span;
    }

    public ParameterDeclarationNode[] Parameters { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"param ({string.Join(", ", Parameters.Select(parameter => parameter.ToString()))})";
    }
}
