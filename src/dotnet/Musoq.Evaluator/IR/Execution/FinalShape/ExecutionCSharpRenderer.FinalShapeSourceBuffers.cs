using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax CreateFinalShapeSourceBufferDeclaration(
        ExecutionCreateTable createTable,
        FinalShapeSourceBuffer buffer)
    {
        var listType = CreateListTypeSyntax(buffer.ShapeTypeName);
        var arguments = createTable.CapacityHint == null
            ? SyntaxFactory.ArgumentList()
            : CreateArgumentList([RenderCapacityHint(createTable.CapacityHint)]);

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createTable.Table.Name,
            SyntaxFactory.ObjectCreationExpression(listType).WithArgumentList(arguments));
    }
}
