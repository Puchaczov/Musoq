namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Nodes.NotNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Expression));
        Write(value.Expression);
    }

    private void Project(global::Musoq.Parser.Nodes.NullNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.OctalIntegerNode value)
    {
        Member(nameof(value.ObjValue));
        Write(value.ObjValue);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.OrNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.PropertyValueNode value)
    {
        Member(nameof(value.PropertyInfo));
        Unbound(value.PropertyInfo);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Nodes.QuantifiedSubqueryNode value)
    {
        Member(nameof(value.Comparison));
        Write(value.Comparison);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Quantifier));
        Write(value.Quantifier);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Subquery));
        Write(value.Subquery);
    }

    private void Project(global::Musoq.Parser.Nodes.RecordLiteralNode value)
    {
        Member(nameof(value.Fields));
        Sequence(value.Fields, static (writer, item) => writer.Write(item));
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Args));
        Sequence(value.Args, static (writer, item) => writer.Write(item));
        Member(nameof(value.ArgumentNames));
        Sequence(value.ArgumentNames, static (writer, item) => writer.Optional(item, static (writer, item) => writer.Write(item)));
        Member(nameof(value.HasNamedArguments));
        Write(value.HasNamedArguments);
    }

    private void Project(global::Musoq.Parser.Nodes.RightShiftNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.RLikeNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.RowPresenceNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.IsPresent));
        Write(value.IsPresent);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.ScalarSubqueryNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Subquery));
        Write(value.Subquery);
    }

    private void Project(global::Musoq.Parser.Nodes.StarNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.StarReplaceItemNode value)
    {
        Member(nameof(value.ColumnName));
        Write(value.ColumnName);
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.StringConcatenationNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.ThenNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Expression));
        Write(value.Expression);
    }

    private void Project(global::Musoq.Parser.Nodes.WhenNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Expression));
        Write(value.Expression);
    }

    private void Project(global::Musoq.Parser.Nodes.WordNode value)
    {
        Member(nameof(value.ObjValue));
        Write(value.ObjValue);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Value));
        Write(value.Value);
    }

    private void Project(global::Musoq.Parser.Nodes.StarRenameItemNode value)
    {
        Member(nameof(value.SourceName));
        Write(value.SourceName);
        Member(nameof(value.TargetName));
        Write(value.TargetName);
    }

    private void Project(global::Musoq.Parser.Nodes.ArgumentName value)
    {
        Member(nameof(value.HasName));
        Write(value.HasName);
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Tokens.FunctionToken value)
    {
        Member(nameof(value.TokenType));
        Write(value.TokenType);
        Member(nameof(value.Value));
        Write(value.Value);
    }
}
