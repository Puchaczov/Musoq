namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Nodes.AccessColumnNode value)
    {
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.IntendedTypeName));
        Write(value.IntendedTypeName);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.SourceColumnIndex));
        Optional(value.SourceColumnIndex, static (writer, item) => writer.Write(item));
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Nodes.AccessMethodNode value)
    {
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ArgsCount));
        Write(value.ArgsCount);
        Member(nameof(value.Arguments));
        Write(value.Arguments);
        Member(nameof(value.CanSkipInjectSource));
        Write(value.CanSkipInjectSource);
        Member(nameof(value.ExtraAggregateArguments));
        Write(value.ExtraAggregateArguments);
        Member(nameof(value.FilterExpression));
        Write(value.FilterExpression);
        Member(nameof(value.FilterExpressionText));
        Write(value.FilterExpressionText);
        Member(nameof(value.FunctionToken));
        Write(value.FunctionToken);
        Member(nameof(value.HasFilter));
        Write(value.HasFilter);
        Member(nameof(value.IsAggregate));
        Write(value.IsAggregate);
        Member(nameof(value.IsCompilerGenerated));
        Write(value.IsCompilerGenerated);
        Member(nameof(value.IsDistinct));
        Write(value.IsDistinct);
        Member(nameof(value.IsPivotGenerated));
        Write(value.IsPivotGenerated);
        Member(nameof(value.IsScalarSubqueryValueWrapper));
        Write(value.IsScalarSubqueryValueWrapper);
        Member(nameof(value.Method));
        Unbound(value.Method);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TypeParameter));
        Write(value.TypeParameter);
    }

    private void Project(global::Musoq.Parser.Nodes.AccessObjectArrayNode value)
    {
        Member(nameof(value.ColumnType));
        Write(value.ColumnType);
        Member(nameof(value.IntendedTypeName));
        Write(value.IntendedTypeName);
        Member(nameof(value.IsColumnAccess));
        Write(value.IsColumnAccess);
        Member(nameof(value.ObjectName));
        Write(value.ObjectName);
        Member(nameof(value.PropertyInfo));
        Unbound(value.PropertyInfo);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TableAlias));
        Write(value.TableAlias);
        Member(nameof(value.Token));
        Write(value.Token);
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Nodes.AccessObjectKeyNode value)
    {
        Member(nameof(value.ColumnType));
        Write(value.ColumnType);
        Member(nameof(value.DestinationKind));
        Write(value.DestinationKind);
        Member(nameof(value.IsColumnAccess));
        Write(value.IsColumnAccess);
        Member(nameof(value.ObjectName));
        Write(value.ObjectName);
        Member(nameof(value.PropertyInfo));
        Unbound(value.PropertyInfo);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TableAlias));
        Write(value.TableAlias);
        Member(nameof(value.Token));
        Write(value.Token);
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Nodes.AddNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.AllColumnsNode value)
    {
        Member(nameof(value.Alias));
        Write(value.Alias);
        Member(nameof(value.ExcludeColumns));
        Sequence(value.ExcludeColumns, static (writer, item) => writer.Write(item));
        Member(nameof(value.HasModifiers));
        Write(value.HasModifiers);
        Member(nameof(value.IsNotLike));
        Write(value.IsNotLike);
        Member(nameof(value.LikePattern));
        Write(value.LikePattern);
        Member(nameof(value.RenameItems));
        Sequence(value.RenameItems, static (writer, item) => writer.Write(item));
        Member(nameof(value.ReplaceItems));
        Sequence(value.ReplaceItems, static (writer, item) => writer.Write(item));
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.AndNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.ArgsListNode value)
    {
        Member(nameof(value.Args));
        Sequence(value.Args, static (writer, item) => writer.Write(item));
        Member(nameof(value.ArgumentNames));
        Sequence(value.ArgumentNames, static (writer, item) => writer.Optional(item, static (writer, item) => writer.Write(item)));
        Member(nameof(value.HasNamedArguments));
        Write(value.HasNamedArguments);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.ArrayIndexNode value)
    {
        Member(nameof(value.Array));
        Write(value.Array);
        Member(nameof(value.Index));
        Write(value.Index);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.ArrayLiteralNode value)
    {
        Member(nameof(value.Elements));
        Sequence(value.Elements, static (writer, item) => writer.Write(item));
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Args));
        Sequence(value.Args, static (writer, item) => writer.Write(item));
        Member(nameof(value.ArgumentNames));
        Sequence(value.ArgumentNames, static (writer, item) => writer.Optional(item, static (writer, item) => writer.Write(item)));
        Member(nameof(value.HasNamedArguments));
        Write(value.HasNamedArguments);
    }

    private void Project(global::Musoq.Parser.Nodes.BetweenNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.Max));
        Write(value.Max);
        Member(nameof(value.Min));
        Write(value.Min);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.BinaryIntegerNode value)
    {
        Member(nameof(value.ObjValue));
        Write(value.ObjValue);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.BitwiseAndNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.BitwiseOrNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.BitwiseXorNode value)
    {
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.BooleanNode value)
    {
        Member(nameof(value.ObjValue));
        Write(value.ObjValue);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Value));
        Write(value.Value);
    }

    private void Project(global::Musoq.Parser.Nodes.CaseNode value)
    {
        Member(nameof(value.Else));
        Write(value.Else);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.WhenThenPairs));
        Sequence(value.WhenThenPairs, static (writer, item) => writer.Pair(item, static (writer, pair) => { writer.Member("When"); writer.Write(pair.When); writer.Member("Then"); writer.Write(pair.Then); }));
    }

    private void Project(global::Musoq.Parser.Nodes.CastNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TargetTypeName));
        Write(value.TargetTypeName);
    }

    private void Project(global::Musoq.Parser.Nodes.CoalesceNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }

    private void Project(global::Musoq.Parser.Nodes.CollectionInNode value)
    {
        Member(nameof(value.Collection));
        Write(value.Collection);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Left));
        Write(value.Left);
        Member(nameof(value.Right));
        Write(value.Right);
    }
}
