namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Nodes.CteExpressionNode value)
    {
        Member(nameof(value.InnerExpression));
        Sequence(value.InnerExpression, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsRecursive));
        Write(value.IsRecursive);
        Member(nameof(value.OuterExpression));
        Write(value.OuterExpression);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.CteInnerExpressionNode value)
    {
        Member(nameof(value.ColumnNames));
        Sequence(value.ColumnNames, static (writer, item) => writer.Write(item));
        Member(nameof(value.Columns));
        Sequence(value.Columns, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsRecursiveDefinition));
        Write(value.IsRecursiveDefinition);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Value));
        Write(value.Value);
    }

    private void Project(global::Musoq.Parser.Nodes.ExceptNode value)
    {
        Member(nameof(value.IsNested));
        Write(value.IsNested);
        Member(nameof(value.IsTheLastOne));
        Write(value.IsTheLastOne);
        Member(nameof(value.KeySpans));
        Sequence(value.KeySpans, static (writer, item) => writer.Write(item));
        Member(nameof(value.Keys));
        Sequence(value.Keys, static (writer, item) => writer.Write(item));
        Member(nameof(value.ResultOrderBy));
        Write(value.ResultOrderBy);
        Member(nameof(value.ResultSkip));
        Write(value.ResultSkip);
        Member(nameof(value.ResultTableName));
        Write(value.ResultTableName);
        Member(nameof(value.ResultTake));
        Write(value.ResultTake);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.ExistsQueryNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Subquery));
        Write(value.Subquery);
    }

    private void Project(global::Musoq.Parser.Nodes.FieldNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.FieldName));
        Write(value.FieldName);
        Member(nameof(value.FieldOrder));
        Write(value.FieldOrder);
        Member(nameof(value.HasExplicitFieldName));
        Write(value.HasExplicitFieldName);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.FieldOrderedNode value)
    {
        Member(nameof(value.NullOrdering));
        Write(value.NullOrdering);
        Member(nameof(value.Order));
        Write(value.Order);
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.FieldName));
        Write(value.FieldName);
        Member(nameof(value.FieldOrder));
        Write(value.FieldOrder);
        Member(nameof(value.HasExplicitFieldName));
        Write(value.HasExplicitFieldName);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.AccessMethodFromNode value)
    {
        Member(nameof(value.AccessMethod));
        Write(value.AccessMethod);
        Member(nameof(value.SourceAlias));
        Write(value.SourceAlias);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.AliasedFromNode value)
    {
        Member(nameof(value.Args));
        Write(value.Args);
        Member(nameof(value.Identifier));
        Write(value.Identifier);
        Member(nameof(value.TypeParameter));
        Write(value.TypeParameter);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.ApplyFromNode value)
    {
        Member(nameof(value.ApplyType));
        Write(value.ApplyType);
        Member(nameof(value.WithOrdinality));
        Write(value.WithOrdinality);
        Member(nameof(value.Source));
        Write(value.Source);
        Member(nameof(value.With));
        Write(value.With);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.ApplyNode value)
    {
        Member(nameof(value.Apply));
        Write(value.Apply);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Alias));
        Write(value.Alias);
    }

    private void Project(global::Musoq.Parser.Nodes.From.DerivedTableFromNode value)
    {
        Member(nameof(value.AllowsCorrelation));
        Write(value.AllowsCorrelation);
        Member(nameof(value.Query));
        Write(value.Query);
        Member(nameof(value.VariableName));
        Write(value.VariableName);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.ExpressionFromNode value)
    {
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.InMemoryTableFromNode value)
    {
        Member(nameof(value.VariableName));
        Write(value.VariableName);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.JoinFromNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.JoinType));
        Write(value.JoinType);
        Member(nameof(value.TieBreak));
        Write(value.TieBreak);
        Member(nameof(value.WithOrdinality));
        Write(value.WithOrdinality);
        Member(nameof(value.Source));
        Write(value.Source);
        Member(nameof(value.With));
        Write(value.With);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.JoinNode value)
    {
        Member(nameof(value.Join));
        Write(value.Join);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Alias));
        Write(value.Alias);
    }

    private void Project(global::Musoq.Parser.Nodes.From.PropertyFromNode value)
    {
        Member(nameof(value.FirstProperty));
        Write(value.FirstProperty);
        Member(nameof(value.PropertiesChain));
        Sequence(value.PropertiesChain, static (writer, item) => writer.Write(item));
        Member(nameof(value.SourceAlias));
        Write(value.SourceAlias);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.ReferentialFromNode value)
    {
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.SchemaFromNode value)
    {
        Member(nameof(value.Method));
        Write(value.Method);
        Member(nameof(value.Parameters));
        Write(value.Parameters);
        Member(nameof(value.Schema));
        Write(value.Schema);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.SchemaMethodFromNode value)
    {
        Member(nameof(value.Method));
        Write(value.Method);
        Member(nameof(value.Schema));
        Write(value.Schema);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.UnpivotEntryNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.Id));
        Write(value.Id);
        Member(nameof(value.NameValue));
        Write(value.NameValue);
    }
}
