namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Nodes.From.UnpivotFromNode value)
    {
        Member(nameof(value.Entries));
        Sequence(value.Entries, static (writer, item) => writer.Write(item));
        Member(nameof(value.KeepFields));
        Sequence(value.KeepFields, static (writer, item) => writer.Write(item));
        Member(nameof(value.NameColumn));
        Write(value.NameColumn);
        Member(nameof(value.Source));
        Write(value.Source);
        Member(nameof(value.ValueColumn));
        Write(value.ValueColumn);
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.ValuesFieldNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.Id));
        Write(value.Id);
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Nodes.From.ValuesFromNode value)
    {
        Member(nameof(value.Rows));
        Sequence(value.Rows, static (writer, item) => writer.Write(item));
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.From.ValuesRowNode value)
    {
        Member(nameof(value.Fields));
        Sequence(value.Fields, static (writer, item) => writer.Write(item));
        Member(nameof(value.Id));
        Write(value.Id);
    }

    private void Project(global::Musoq.Parser.Nodes.GroupByNode value)
    {
        Member(nameof(value.Fields));
        Sequence(value.Fields, static (writer, item) => writer.Write(item));
        Member(nameof(value.Having));
        Write(value.Having);
        Member(nameof(value.IsAll));
        Write(value.IsAll);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.HavingNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.InQueryNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Subquery));
        Write(value.Subquery);
    }

    private void Project(global::Musoq.Parser.Nodes.IntersectNode value)
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

    private void Project(global::Musoq.Parser.Nodes.IsDistinctFromNode value)
    {
        Member(nameof(value.IsNegated));
        Write(value.IsNegated);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.OrderByNode value)
    {
        Member(nameof(value.Fields));
        Sequence(value.Fields, static (writer, item) => writer.Write(item));
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.QualifyNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.QueryNode value)
    {
        Member(nameof(value.From));
        Write(value.From);
        Member(nameof(value.GroupBy));
        Write(value.GroupBy);
        Member(nameof(value.OrderBy));
        Write(value.OrderBy);
        Member(nameof(value.Qualify));
        Write(value.Qualify);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Select));
        Write(value.Select);
        Member(nameof(value.Skip));
        Write(value.Skip);
        Member(nameof(value.Take));
        Write(value.Take);
        Member(nameof(value.Where));
        Write(value.Where);
        Member(nameof(value.Window));
        Write(value.Window);
    }

    private void Project(global::Musoq.Parser.Nodes.RecordFieldNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.Id));
        Write(value.Id);
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Nodes.SelectNode value)
    {
        Member(nameof(value.Fields));
        Sequence(value.Fields, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsDistinct));
        Write(value.IsDistinct);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.SingleSetNode value)
    {
        Member(nameof(value.Query));
        Write(value.Query);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.SkipNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Expression));
        Write(value.Expression);
    }

    private void Project(global::Musoq.Parser.Nodes.TakeNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Expression));
        Write(value.Expression);
    }

    private void Project(global::Musoq.Parser.Nodes.UnionAllNode value)
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

    private void Project(global::Musoq.Parser.Nodes.UnionNode value)
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

    private void Project(global::Musoq.Parser.Nodes.WhereNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }
}
