using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class ColumnarStoredTableSyntaxFactory
{
    internal static string CreateBufferTypeName(int tableIndex) =>
        $"StoredTableColumnBuffer{tableIndex.ToString(CultureInfo.InvariantCulture)}";

    internal static string CreateColumnName(int index) => $"Column{index.ToString(CultureInfo.InvariantCulture)}";

    internal static ClassDeclarationSyntax CreateColumnClass() =>
        (ClassDeclarationSyntax)
            SyntaxFactory.ParseMemberDeclaration(
                (
                    "private sealed class StoredTableColumn<T>\n"
                    + "{\n"
                    + "    private const int ChunkSize = 1024;\n"
                    + "    private T[][] _chunks = Array.Empty<T[]>();\n"
                    + "    private int _chunkCount;\n"
                    + "    private int _count;\n"
                    + "\n"
                    + "    public StoredTableColumn(int capacity = 0)\n"
                    + "    {\n"
                    + "        EnsureCapacity(capacity);\n"
                    + "    }\n"
                    + "\n"
                    + "    public int Count => _count;\n"
                    + "\n"
                    + "    public T this[int index] => _chunks[index >> 10][index & (ChunkSize - 1)];\n"
                    + "\n"
                    + "    public void Add(T value)\n"
                    + "    {\n"
                    + "        var chunkIndex = _count >> 10;\n"
                    + "        if (chunkIndex == _chunkCount)\n"
                    + "            AddChunk();\n"
                    + "\n"
                    + "        _chunks[chunkIndex][_count & (ChunkSize - 1)] = value;\n"
                    + "        _count++;\n"
                    + "    }\n"
                    + "\n"
                    + "    public void EnsureCapacity(int capacity)\n"
                    + "    {\n"
                    + "        if (capacity <= 0)\n"
                    + "            return;\n"
                    + "\n"
                    + "        var requiredChunks = ((capacity - 1) / ChunkSize) + 1;\n"
                    + "        while (_chunkCount < requiredChunks)\n"
                    + "            AddChunk();\n"
                    + "    }\n"
                    + "\n"
                    + "    private void AddChunk()\n"
                    + "    {\n"
                    + "        if (_chunkCount == _chunks.Length)\n"
                    + "            Array.Resize(ref _chunks, _chunkCount == 0 ? 4 : _chunkCount * 2);\n"
                    + "\n"
                    + "        _chunks[_chunkCount++] = new T[ChunkSize];\n"
                    + "    }\n"
                    + "}"
                )
            )!;

    internal static ClassDeclarationSyntax CreateBufferClass(TypedStoredTableResult result)
    {
        var typeName = CreateBufferTypeName(result.TableIndex);
        var fields = result.RowShape.Fields.OrderBy(static field => field.OutputIndex).ToArray();
        var members = new List<MemberDeclarationSyntax>();
        for (var index = 0; index < fields.Length; index++)
        {
            members.Add(
                SyntaxFactory
                    .FieldDeclaration(
                        SyntaxFactory
                            .VariableDeclaration(CreateColumnTypeSyntax(fields[index].Type.RequireClrType()))
                            .WithVariables(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(CreateColumnName(index))
                                )
                            )
                    )
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
                        )
                    )
            );
        }

        var constructorStatements = fields
            .Select(
                (field, index) =>
                    (StatementSyntax)
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName(CreateColumnName(index)),
                                SyntaxFactory
                                    .ObjectCreationExpression(CreateColumnTypeSyntax(field.Type.RequireClrType()))
                                    .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName("capacity")))
                            )
                        )
            )
            .ToArray();
        var constructor = SyntaxFactory
            .ConstructorDeclaration(typeName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory
                            .Parameter(SyntaxFactory.Identifier("capacity"))
                            .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                            .WithDefault(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(0)
                                    )
                                )
                            )
                    )
                )
            )
            .WithBody(SyntaxFactory.Block(constructorStatements));
        members.Add(constructor);

        var countExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(CreateColumnName(0)),
            SyntaxFactory.IdentifierName("Count")
        );
        members.Add(
            SyntaxFactory
                .PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)), "Count")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(countExpression))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
        );

        var rowViewType = SyntaxFactory.ParseTypeName(result.RowShape.TypeName);
        var indexer = SyntaxFactory
            .IndexerDeclaration(rowViewType)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                SyntaxFactory.BracketedParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory
                            .Parameter(SyntaxFactory.Identifier("index"))
                            .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                    )
                )
            )
            .WithExpressionBody(
                SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory
                        .ObjectCreationExpression(rowViewType)
                        .WithArgumentList(
                            CreateArgumentList(SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName("index"))
                        )
                )
            )
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        members.Add(indexer);

        var appendParameters = fields
            .Select(
                (field, index) =>
                    SyntaxFactory
                        .Parameter(SyntaxFactory.Identifier($"value{index.ToString(CultureInfo.InvariantCulture)}"))
                        .WithType(CreateTypeSyntax(field.Type.RequireClrType()))
            )
            .ToArray();
        var appendStatements = fields
            .Select(
                (_, index) =>
                    (StatementSyntax)
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory
                                .InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(CreateColumnName(index)),
                                        SyntaxFactory.IdentifierName("Add")
                                    )
                                )
                                .WithArgumentList(
                                    CreateArgumentList(
                                        SyntaxFactory.IdentifierName(
                                            $"value{index.ToString(CultureInfo.InvariantCulture)}"
                                        )
                                    )
                                )
                        )
            )
            .ToArray();
        members.Add(
            SyntaxFactory
                .MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Add")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(appendParameters)))
                .WithBody(SyntaxFactory.Block(appendStatements))
        );

        var ensureCapacityStatements = fields
            .Select(
                (_, index) =>
                    (StatementSyntax)
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory
                                .InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(CreateColumnName(index)),
                                        SyntaxFactory.IdentifierName("EnsureCapacity")
                                    )
                                )
                                .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName("capacity")))
                        )
            )
            .ToArray();
        members.Add(
            SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    "EnsureCapacity"
                )
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory
                                .Parameter(SyntaxFactory.Identifier("capacity"))
                                .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                        )
                    )
                )
                .WithBody(SyntaxFactory.Block(ensureCapacityStatements))
        );

        return SyntaxFactory
            .ClassDeclaration(typeName)
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.SealedKeyword)
                )
            )
            .WithMembers(SyntaxFactory.List(members));
    }

    internal static StructDeclarationSyntax CreateRowView(GeneratedRowShape shape, int tableIndex)
    {
        var bufferTypeName = CreateBufferTypeName(tableIndex);
        var members = new List<MemberDeclarationSyntax>
        {
            SyntaxFactory
                .FieldDeclaration(
                    SyntaxFactory
                        .VariableDeclaration(SyntaxFactory.IdentifierName(bufferTypeName))
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator("__buffer"))
                        )
                )
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
                    )
                ),
            SyntaxFactory
                .FieldDeclaration(
                    SyntaxFactory
                        .VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator("__index"))
                        )
                )
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
                    )
                ),
        };
        var constructor = SyntaxFactory
            .ConstructorDeclaration(shape.TypeName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList([
                        SyntaxFactory
                            .Parameter(SyntaxFactory.Identifier("buffer"))
                            .WithType(SyntaxFactory.IdentifierName(bufferTypeName)),
                        SyntaxFactory
                            .Parameter(SyntaxFactory.Identifier("index"))
                            .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))),
                    ])
                )
            )
            .WithBody(
                SyntaxFactory.Block(
                    new StatementSyntax[]
                    {
                        CreateRowViewAssignment("__buffer", "buffer"),
                        CreateRowViewAssignment("__index", "index"),
                    }
                )
            );
        members.Add(constructor);

        var fields = shape.Fields.OrderBy(static field => field.OutputIndex).ToArray();
        for (var index = 0; index < fields.Length; index++)
        {
            var column = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("__buffer"),
                SyntaxFactory.IdentifierName(CreateColumnName(index))
            );
            var value = CreateElementAccess(column, SyntaxFactory.IdentifierName("__index"));
            members.Add(
                SyntaxFactory
                    .PropertyDeclaration(
                        CreateTypeSyntax(fields[index].Type.RequireClrType()),
                        GeneratedRowNamingPolicy.GetGeneratedFieldName(fields[index])
                    )
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(value))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            );
        }

        return SyntaxFactory
            .StructDeclaration(shape.TypeName)
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
                )
            )
            .WithMembers(SyntaxFactory.List(members));
    }

    private static ExpressionStatementSyntax CreateRowViewAssignment(string targetName, string valueName) =>
        SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName(targetName)
                ),
                SyntaxFactory.IdentifierName(valueName)
            )
        );

    private static TypeSyntax CreateColumnTypeSyntax(Type elementType) =>
        SyntaxFactory
            .GenericName(SyntaxFactory.Identifier("StoredTableColumn"))
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(CreateTypeSyntax(elementType)))
            );
}
