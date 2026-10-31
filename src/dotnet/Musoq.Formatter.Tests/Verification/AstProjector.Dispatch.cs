using System;
using Musoq.Parser.Nodes;

namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Write(Node? value)
    {
        if (value == null)
        {
            Null();
            return;
        }
        switch (value)
        {
            case global::Musoq.Parser.Nodes.ExceptNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ExceptNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.ApplyFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.ApplyFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.DerivedTableFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.DerivedTableFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.JoinFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.JoinFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.IntersectNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.IntersectNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.UnionAllNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.UnionAllNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.UnionNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.UnionNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.AccessColumnNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.AccessColumnNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.AccessObjectArrayNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.AccessObjectArrayNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.AccessObjectKeyNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.AccessObjectKeyNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.AddNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.AddNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.AndNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.AndNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ArrayLiteralNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ArrayLiteralNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.BinaryIntegerNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.BinaryIntegerNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.BitwiseAndNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.BitwiseAndNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.BitwiseOrNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.BitwiseOrNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.BitwiseXorNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.BitwiseXorNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.BooleanNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.BooleanNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.CoalesceNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.CoalesceNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.CollectionInNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.CollectionInNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ContainsNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ContainsNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.DecimalNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.DecimalNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.DiffNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.DiffNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.DotNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.DotNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ElseNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ElseNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.EqualityNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.EqualityNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.FieldOrderedNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.FieldOrderedNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.AccessMethodFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.AccessMethodFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.AliasedFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.AliasedFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.ApplyNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.ApplyNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.ExpressionFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.ExpressionFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.InMemoryTableFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.InMemoryTableFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.JoinNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.JoinNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.PropertyFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.PropertyFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.ReferentialFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.ReferentialFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.SchemaFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.SchemaFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.SchemaMethodFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.SchemaMethodFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.UnpivotFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.UnpivotFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.From.ValuesFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.From.ValuesFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.FSlashNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.FSlashNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.GreaterNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.GreaterNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.GreaterOrEqualNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.GreaterOrEqualNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.HexIntegerNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.HexIntegerNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.HyphenNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.HyphenNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.IntegerNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.IntegerNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.AlignmentNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.AlignmentNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.ArrayTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.ArrayTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.BinarySwitchTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.BinarySwitchTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.BitsTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.BitsTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.ByteArrayTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.ByteArrayTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.ComputedFieldNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.ComputedFieldNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.FieldDefinitionNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.FieldDefinitionNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.InlineSchemaTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.InlineSchemaTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.PrimitiveTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.PrimitiveTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.RepeatUntilTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.RepeatUntilTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.SchemaReferenceTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.SchemaReferenceTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.StringTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.StringTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.SubstreamTypeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.SubstreamTypeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.IsDistinctFromNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.IsDistinctFromNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.LeftShiftNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.LeftShiftNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.LessNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.LessNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.LessOrEqualNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.LessOrEqualNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.LikeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.LikeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ModuloNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ModuloNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.NotNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.NotNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.OctalIntegerNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.OctalIntegerNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.OrNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.OrNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.PropertyValueNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.PropertyValueNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.QuantifiedSubqueryNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.QuantifiedSubqueryNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.RecordLiteralNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.RecordLiteralNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.RightShiftNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.RightShiftNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.RLikeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.RLikeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.RootNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.RootNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.SkipNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.SkipNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.StarNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.StarNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.StringConcatenationNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.StringConcatenationNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.TakeNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.TakeNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ThenNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ThenNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.WhenNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.WhenNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.WordNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.WordNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.AccessMethodNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.AccessMethodNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.AllColumnsNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.AllColumnsNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ArgsListNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ArgsListNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ArrayIndexNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ArrayIndexNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.BetweenNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.BetweenNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.CaseNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.CaseNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.CastNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.CastNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.CoupleNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.CoupleNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.CreateTableNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.CreateTableNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.CteExpressionNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.CteExpressionNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.CteInnerExpressionNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.CteInnerExpressionNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.DescNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.DescNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.DiagnosticCommandNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.DiagnosticCommandNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.EnumDeclarationNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.EnumDeclarationNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.EnumMemberNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.EnumMemberNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ExistsQueryNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ExistsQueryNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.FieldNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.FieldNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.GroupByNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.GroupByNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.HavingNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.HavingNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.IdentifierNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.IdentifierNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InQueryNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InQueryNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.BinarySchemaNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.BinarySchemaNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.FieldConstraintNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.FieldConstraintNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.FieldValueValidationNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.FieldValueValidationNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.TextFieldDefinitionNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.TextFieldDefinitionNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.InterpretationSchema.TextSchemaNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.InterpretationSchema.TextSchemaNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.IsNullNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.IsNullNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.NullNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.NullNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.OrderByNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.OrderByNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ParameterBlockNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ParameterBlockNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ParameterDeclarationNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ParameterDeclarationNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ParameterReferenceNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ParameterReferenceNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.QualifyNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.QualifyNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.QueryNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.QueryNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.RowPresenceNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.RowPresenceNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ScalarSubqueryNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ScalarSubqueryNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.ScriptVariableDeclarationNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.ScriptVariableDeclarationNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.SelectNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.SelectNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.SingleSetNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.SingleSetNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.StarReplaceItemNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.StarReplaceItemNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.StatementNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.StatementNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.StatementsArrayNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.StatementsArrayNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.StructuralTypeSyntaxNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.StructuralTypeSyntaxNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.WhereNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.WhereNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.WindowDefinitionNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.WindowDefinitionNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.WindowFrameBoundNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.WindowFrameBoundNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.WindowFrameNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.WindowFrameNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.WindowFunctionNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.WindowFunctionNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.WindowNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.WindowNode", static (writer, node) => writer.Project(node));
                break;
            case global::Musoq.Parser.Nodes.WindowSpecificationNode item:
                ProjectNode(item, "Musoq.Parser.Nodes.WindowSpecificationNode", static (writer, node) => writer.Project(node));
                break;
            default:
                throw new InvalidOperationException("Unreviewed parser node shape.");
        }
    }

    private void Write(global::Musoq.Parser.Nodes.From.UnpivotEntryNode? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.From.UnpivotEntryNode", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.From.ValuesFieldNode? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.From.ValuesFieldNode", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.From.ValuesRowNode? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.From.ValuesRowNode", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.InterpretationSchema.BinarySwitchCaseNode? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.InterpretationSchema.BinarySwitchCaseNode", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.InterpretationSchema.TextSwitchCaseNode? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.InterpretationSchema.TextSwitchCaseNode", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.RecordFieldNode? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.RecordFieldNode", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.StructuralTypeFieldSyntax? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.StructuralTypeFieldSyntax", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.CreateTableColumnDefinition? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.CreateTableColumnDefinition", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.CreateTableColumnModifier? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.CreateTableColumnModifier", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.StarRenameItemNode? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.StarRenameItemNode", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.CteColumnName? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.CteColumnName", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.InterpretationSchema.SchemaComment? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.InterpretationSchema.SchemaComment", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.From.PropertyFromNode.PropertyNameAndTypePair? value) =>
        ProjectNode(value, "Musoq.Parser.Nodes.From.PropertyFromNode+PropertyNameAndTypePair", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Nodes.ArgumentName value) =>
        ProjectValue(value, "Musoq.Parser.Nodes.ArgumentName", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Tokens.FunctionToken? value) =>
        ProjectNode(value, "Musoq.Parser.Tokens.FunctionToken", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Tokens.NumericAccessToken? value) =>
        ProjectNode(value, "Musoq.Parser.Tokens.NumericAccessToken", static (writer, node) => writer.Project(node));

    private void Write(global::Musoq.Parser.Tokens.KeyAccessToken? value) =>
        ProjectNode(value, "Musoq.Parser.Tokens.KeyAccessToken", static (writer, node) => writer.Project(node));
}
