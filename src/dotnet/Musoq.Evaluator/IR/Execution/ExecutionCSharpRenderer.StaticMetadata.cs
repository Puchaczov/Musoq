using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private void EnsureConstantInSetFields(ExecutionPlan plan)
    {
        foreach (var constantSet in CollectConstantInSets(plan.Body))
        {
            if (constantSet.Kind is ExecutionConstantInSetKind.Switch)
                continue;

            if (RenderSession.ConstantInSetFieldNames.ContainsKey(constantSet))
                continue;

            var fieldName = CreateIdentifierCandidate($"__inSet_{plan.Identifier}_{RenderSession.ConstantInSetFields.Count}", 0);
            RenderSession.ConstantInSetFieldNames.Add(constantSet, fieldName);
            RenderSession.ConstantInSetFields.Add(new ConstantInSetField(fieldName, constantSet));
        }
    }

    private void EnsureStaticMetadataFields(ExecutionPlan plan)
    {
        foreach (var metadata in CollectStaticMetadata(plan.Body))
            EnsureStaticMetadataField(plan.Identifier, metadata);
    }

    private void EnsureStaticMetadataField(string planIdentifier, ExecutionColumnMetadata metadata)
    {
        var key = CreateStaticMetadataKey(metadata);
        if (RenderSession.StaticMetadataFieldNames.ContainsKey(key))
            return;

        var prefix = metadata.Kind == ExecutionColumnMetadataKind.TableColumns
            ? "__columns"
            : "__schemaColumns";
        var fieldName = CreateIdentifierCandidate(
            $"{prefix}_{planIdentifier}_{metadata.ReferenceName}_{RenderSession.StaticMetadataFields.Count}",
            0);

        RenderSession.StaticMetadataFieldNames.Add(key, fieldName);
        RenderSession.StaticMetadataFields.Add(new StaticMetadataField(fieldName, metadata));
    }

    private bool TryGetStaticMetadataFieldName(ExecutionColumnMetadata metadata, out string fieldName)
    {
        return RenderSession.StaticMetadataFieldNames.TryGetValue(CreateStaticMetadataKey(metadata), out fieldName!);
    }

    internal bool TryGetTableColumnMetadataFieldName(
        string referenceName,
        IReadOnlyList<ExecutionColumnMetadataField> fields,
        out string fieldName)
    {
        return TryGetStaticMetadataFieldName(
            new ExecutionColumnMetadata(referenceName, fields, ExecutionColumnMetadataKind.TableColumns),
            out fieldName);
    }

    private static string CreateStaticMetadataKey(ExecutionColumnMetadata metadata)
    {
        var builder = new StringBuilder();
        builder
            .Append(metadata.Kind)
            .Append(':');
        builder.Append(metadata.Fields.Count);

        foreach (var field in metadata.Fields)
        {
            builder
                .Append(':')
                .Append(field.Index)
                .Append(':');
            AppendMetadataKeyPart(builder, field.Name);
            builder.Append(':');
            AppendMetadataKeyPart(builder, field.Type.AssemblyQualifiedName ?? field.Type.FullName ?? field.Type.Name);
            ReadModifierMetadata.AppendKey(builder, field.ReadModifiers);
        }

        return builder.ToString();
    }

    private static void AppendMetadataKeyPart(StringBuilder builder, string value)
    {
        builder
            .Append(value.Length)
            .Append(':')
            .Append(value);
    }

    private static IEnumerable<ExecutionColumnMetadata> CollectStaticMetadata(ExecutionBlock block)
    {
        foreach (var node in FlattenNodes(block))
        {
            switch (node)
            {
                case ExecutionSourceScan sourceScan:
                    yield return ResolveSourceSchemaColumnMetadata(sourceScan);
                    break;
                case ExecutionCreateTable createTable:
                    yield return ResolveTableColumnMetadata(createTable);
                    break;
                case ExecutionMaterializeRecordListToTable materialize:
                    yield return CreateColumnMetadata(
                        materialize.Target.Name,
                        materialize.RowShape.Fields,
                        ExecutionColumnMetadataKind.TableColumns);
                    break;
                case ExecutionProjectTable project:
                    yield return CreateColumnMetadata(
                        project.Target.Name,
                        project.RowShape.Fields,
                        ExecutionColumnMetadataKind.TableColumns);
                    break;
                case var tableOperation when
                    ExecutionNodeFacts.TryGetTablePostOperation(tableOperation, out var operation) &&
                    operation.ColumnMetadata != null:
                    yield return operation.ColumnMetadata;
                    break;
                case ExecutionReturnDesc { Type: Musoq.Evaluator.IR.Logical.Nodes.DescType.Query, QueryColumnMetadata: not null } desc:
                    yield return desc.QueryColumnMetadata;
                    break;
            }
        }
    }


    private static ExecutionColumnMetadata ResolveSourceSchemaColumnMetadata(ExecutionSourceScan sourceScan)
    {
        return sourceScan.Binding.InferredColumnsMetadata
               ?? CreateColumnMetadata(
                   CreateSourceScanLocalName(sourceScan),
                   sourceScan.Binding.Fields,
                   ExecutionColumnMetadataKind.SourceSchemaColumns);
    }

    private static ExecutionColumnMetadata ResolveTableColumnMetadata(ExecutionCreateTable createTable)
    {
        return createTable.ColumnMetadata
               ?? CreateColumnMetadata(
                   createTable.Table.Name,
                   createTable.RowShape.Fields,
                   ExecutionColumnMetadataKind.TableColumns);
    }

    private static ExecutionColumnMetadata CreateColumnMetadata(
        string referenceName,
        IReadOnlyList<FieldBinding> fields,
        ExecutionColumnMetadataKind kind)
    {
        return new ExecutionColumnMetadata(
            referenceName,
            fields
                .Select(static field => ExecutionColumnMetadataFields.FromFieldBinding(field))
                .ToArray(),
            kind);
    }

    private static FieldDeclarationSyntax CreateConstantInSetField(ConstantInSetField field)
    {
        var constantSet = field.ConstantSet;
        var fieldType = CreateConstantInSetFieldType(constantSet);
        var initializer = CreateConstantInSetFieldInitializer(constantSet);

        return SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(fieldType)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(EscapeIdentifier(field.Name))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(initializer)))))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
    }

    private static TypeSyntax CreateConstantInSetFieldType(ExecutionConstantInSet constantSet)
    {
        var elementType = constantSet.ElementType;
        return constantSet.Kind switch
        {
            ExecutionConstantInSetKind.HashSet => CreateTypeSyntax(typeof(HashSet<>).MakeGenericType(elementType)),
            ExecutionConstantInSetKind.FrozenSet => CreateTypeSyntax(typeof(FrozenSet<>).MakeGenericType(elementType)),
            _ => CreateTypeSyntax(elementType.MakeArrayType())
        };
    }

    private static ExpressionSyntax CreateConstantInSetFieldInitializer(ExecutionConstantInSet constantSet)
    {
        return constantSet.Kind switch
        {
            ExecutionConstantInSetKind.HashSet => CreateConstantHashSetCreation(constantSet),
            ExecutionConstantInSetKind.FrozenSet => CreateConstantFrozenSetCreation(constantSet),
            _ => CreateConstantArrayCreation(constantSet)
        };
    }

    private static FieldDeclarationSyntax CreateStaticMetadataField(StaticMetadataField field)
    {
        var fieldType = field.Metadata.Kind == ExecutionColumnMetadataKind.TableColumns
            ? SyntaxFactory.ParseTypeName("Column[]")
            : SyntaxFactory.ParseTypeName("IReadOnlyCollection<ISchemaColumn>");
        ExpressionSyntax initializer = field.Metadata.Kind == ExecutionColumnMetadataKind.TableColumns
            ? CreateColumnArrayCreation(field.Metadata.Fields)
            : CreateReadOnlySchemaColumnsCreation(field.Metadata.Fields);

        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(fieldType)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(EscapeIdentifier(field.Name))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(initializer)))))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
    }

    private static InvocationExpressionSyntax CreateReadOnlySchemaColumnsCreation(
        IReadOnlyList<ExecutionColumnMetadataField> fields)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Array)),
                    SyntaxFactory.IdentifierName(nameof(Array.AsReadOnly))))
            .WithArgumentList(CreateArgumentList(CreateSchemaColumnArrayCreation(fields)));
    }

    private static ArrayCreationExpressionSyntax CreateSchemaColumnArrayCreation(
        IReadOnlyList<ExecutionColumnMetadataField> fields)
    {
        return CreateArrayCreation(
            nameof(ISchemaColumn),
            fields.Select(CreateSchemaColumnCreation));
    }

    private static ArrayCreationExpressionSyntax CreateColumnArrayCreation(
        IReadOnlyList<ExecutionColumnMetadataField> fields)
    {
        return CreateArrayCreation(
            nameof(Column),
            fields.Select(CreateColumnCreation));
    }

    private static ObjectCreationExpressionSyntax CreateConstantHashSetCreation(ExecutionConstantInSet constantSet)
    {
        var hashSetType = typeof(HashSet<>).MakeGenericType(constantSet.ElementType);

        return SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(hashSetType))
            .WithArgumentList(CreateArgumentList(CreateConstantArrayCreation(constantSet)));
    }

    private static InvocationExpressionSyntax CreateConstantFrozenSetCreation(ExecutionConstantInSet constantSet)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateConstantArrayCreation(constantSet),
                    SyntaxFactory.IdentifierName(nameof(FrozenSet.ToFrozenSet))))
            .WithArgumentList(SyntaxFactory.ArgumentList());
    }

    private static ArrayCreationExpressionSyntax CreateConstantArrayCreation(ExecutionConstantInSet constantSet)
    {
        return CreateArrayCreation(
            constantSet.ElementType,
            constantSet.Values.Select(RenderLiteral));
    }
}
