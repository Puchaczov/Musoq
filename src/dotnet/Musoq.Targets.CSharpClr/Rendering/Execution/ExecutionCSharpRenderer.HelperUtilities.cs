using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<string> CollectDeclaredVariableNames(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectDeclaredVariableNames(block);
    }

    private static GenericNameSyntax CreateEnumerableTypeSyntax(TypeSyntax itemType)
    {
        return SyntaxFactory.GenericName(nameof(IEnumerable<>))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(itemType)));
    }

    private static TypeSyntax CreateAggregateRowsParameterType(ExecutionExpression sourceRows, TypeSyntax rowType)
    {
        return ExecutionRowStreams.IsChunked(sourceRows)
            ? CreateEnumerableTypeSyntax(CreateReadOnlyListTypeSyntax(rowType))
            : CreateEnumerableTypeSyntax(rowType);
    }
}
