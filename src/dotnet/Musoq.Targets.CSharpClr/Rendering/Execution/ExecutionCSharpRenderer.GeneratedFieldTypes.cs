using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static TypeSyntax CreateGeneratedFieldTypeSyntax(FieldBinding field)
    {
        if (!string.IsNullOrWhiteSpace(field.GeneratedTypeName))
            return SyntaxFactory.ParseTypeName(field.GeneratedTypeName);
        return CreateTypeSyntax(field.Type);
    }
}
