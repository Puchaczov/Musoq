namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Nodes.ContainsNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.ToCompareExpression));
        Write(value.ToCompareExpression);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.DecimalNode value)
    {
        Member(nameof(value.ObjValue));
        Write(value.ObjValue);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Value));
        Write(value.Value);
    }

    private void Project(global::Musoq.Parser.Nodes.DiffNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.DotNode value)
    {
        Member(nameof(value.IntendedTypeName));
        Write(value.IntendedTypeName);
        Member(nameof(value.IsNullSafePath));
        Write(value.IsNullSafePath);
        Member(nameof(value.IsTheMostInner));
        Write(value.IsTheMostInner);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Root));
        Write(value.Root);
        Member(nameof(value.Expression));
        Write(value.Expression);
    }

    private void Project(global::Musoq.Parser.Nodes.ElseNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Expression));
        Write(value.Expression);
    }

    private void Project(global::Musoq.Parser.Nodes.EqualityNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.FSlashNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.GreaterNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.GreaterOrEqualNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.HexIntegerNode value)
    {
        Member(nameof(value.ObjValue));
        Write(value.ObjValue);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.HyphenNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.IdentifierNode value)
    {
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.InNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.IntegerNode value)
    {
        Member(nameof(value.ObjValue));
        Write(value.ObjValue);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.IsNullNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.IsNegated));
        Write(value.IsNegated);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.LeftShiftNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.LessNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.LessOrEqualNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.LikeNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.ModuloNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }
}
