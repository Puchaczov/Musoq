using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.CodeGeneration;

public readonly record struct QueryMethodRenderResult(
    string MethodName,
    MethodDeclarationSyntax MethodDeclaration,
    QueryMethodRenderMetadata Metadata)
{
    public QueryMethodRenderResult(string methodName, MethodDeclarationSyntax methodDeclaration)
        : this(methodName, methodDeclaration, QueryMethodRenderMetadata.Unknown)
    {
    }
}
