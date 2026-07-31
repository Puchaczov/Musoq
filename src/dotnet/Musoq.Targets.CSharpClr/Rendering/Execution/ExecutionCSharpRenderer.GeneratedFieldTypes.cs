using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static TypeSyntax CreateGeneratedFieldTypeSyntax(FieldBinding field)
    {
        return string.IsNullOrWhiteSpace(field.GeneratedTypeName)
            ? CreateTypeSyntax(field.Type)
            : SyntaxFactory.ParseTypeName(field.GeneratedTypeName);
    }
}
