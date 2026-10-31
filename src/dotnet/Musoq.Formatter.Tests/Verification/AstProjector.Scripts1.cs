namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Nodes.CoupleNode value)
    {
        Member(nameof(value.MappedSchemaName));
        Write(value.MappedSchemaName);
        Member(nameof(value.ProfileName));
        Write(value.ProfileName);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.SchemaMethodNode));
        Write(value.SchemaMethodNode);
        Member(nameof(value.TableName));
        Write(value.TableName);
    }

    private void Project(global::Musoq.Parser.Nodes.CreateTableNode value)
    {
        Member(nameof(value.Columns));
        Sequence(value.Columns, static (writer, item) => writer.Write(item));
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TableTypePairs));
        Sequence(value.TableTypePairs, static (writer, item) => writer.Pair(item, static (writer, pair) => { writer.Member("ColumnName"); writer.Write(pair.ColumnName); writer.Member("TypeName"); writer.Write(pair.TypeName); }));
    }

    private void Project(global::Musoq.Parser.Nodes.DescNode value)
    {
        Member(nameof(value.Column));
        Write(value.Column);
        Member(nameof(value.From));
        Write(value.From);
        Member(nameof(value.Query));
        Write(value.Query);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Type));
        Write(value.Type);
    }

    private void Project(global::Musoq.Parser.Nodes.DiagnosticCommandNode value)
    {
        Member(nameof(value.InnerQueryText));
        Write(AstParsing.Parse(value.InnerQueryText));
        Member(nameof(value.Kind));
        Write(value.Kind);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.EnumDeclarationNode value)
    {
        Member(nameof(value.IsFlags));
        Write(value.IsFlags);
        Member(nameof(value.Members));
        Sequence(value.Members, static (writer, item) => writer.Write(item));
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.UnderlyingTypeName));
        Write(value.UnderlyingTypeName);
    }

    private void Project(global::Musoq.Parser.Nodes.EnumMemberNode value)
    {
        Member(nameof(value.LiteralText));
        Write(value.LiteralText);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.RawValue));
        Write(value.RawValue);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.ParameterBlockNode value)
    {
        Member(nameof(value.Parameters));
        Sequence(value.Parameters, static (writer, item) => writer.Write(item));
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.ParameterDeclarationNode value)
    {
        Member(nameof(value.DeclaredTypeName));
        Write(value.DeclaredTypeName);
        Member(nameof(value.DefaultValue));
        Write(value.DefaultValue);
        Member(nameof(value.HasDefaultValue));
        Write(value.HasDefaultValue);
        Member(nameof(value.IsNullable));
        Write(value.IsNullable);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TypeName));
        Write(value.TypeName);
        Member(nameof(value.TypeSyntax));
        Write(value.TypeSyntax);
    }

    private void Project(global::Musoq.Parser.Nodes.ParameterReferenceNode value)
    {
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.RootNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Expression));
        Write(value.Expression);
    }

    private void Project(global::Musoq.Parser.Nodes.ScriptVariableDeclarationNode value)
    {
        Member(nameof(value.DeclaredTypeName));
        Write(value.DeclaredTypeName);
        Member(nameof(value.Initializer));
        Write(value.Initializer);
        Member(nameof(value.IsInferred));
        Write(value.IsInferred);
        Member(nameof(value.IsNullable));
        Write(value.IsNullable);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TypeName));
        Write(value.TypeName);
        Member(nameof(value.TypeSyntax));
        Write(value.TypeSyntax);
    }

    private void Project(global::Musoq.Parser.Nodes.StatementNode value)
    {
        Member(nameof(value.Node));
        Write(value.Node);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.StatementsArrayNode value)
    {
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Statements));
        Sequence(value.Statements, static (writer, item) => writer.Write(item));
    }

    private void Project(global::Musoq.Parser.Nodes.StructuralTypeSyntaxNode value)
    {
        Member(nameof(value.ElementType));
        Write(value.ElementType);
        Member(nameof(value.Fields));
        Sequence(value.Fields, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsNullable));
        Write(value.IsNullable);
        Member(nameof(value.Kind));
        Write(value.Kind);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.ScalarName));
        Write(value.ScalarName);
    }

    private void Project(global::Musoq.Parser.Nodes.StructuralTypeFieldSyntax value)
    {
        Member(nameof(value.DefaultValue));
        Write(value.DefaultValue);
        Member(nameof(value.HasDefault));
        Write(value.HasDefault);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.Type));
        Write(value.Type);
    }

    private void Project(global::Musoq.Parser.Nodes.CreateTableColumnDefinition value)
    {
        Member(nameof(value.ColumnName));
        Write(value.ColumnName);
        Member(nameof(value.ReadModifierSpans));
        Dictionary(value.ReadModifierSpans, static (writer, key) => writer.Write(key), static (writer, item) => writer.Write(item));
        Member(nameof(value.ReadModifiers));
        Sequence(value.ReadModifiers, static (writer, item) => writer.Write(item));
        Member(nameof(value.TypeName));
        Write(value.TypeName);
    }

    private void Project(global::Musoq.Parser.Nodes.CreateTableColumnModifier value)
    {
        Member(nameof(value.Key));
        Write(value.Key);
        Member(nameof(value.Value));
        Write(value.Value);
    }
}
