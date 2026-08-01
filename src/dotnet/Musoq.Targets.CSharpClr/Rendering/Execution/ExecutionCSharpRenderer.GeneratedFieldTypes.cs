using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static TypeSyntax CreateGeneratedFieldTypeSyntax(FieldBinding field)
    {
        // Expando adapters intentionally keep nested runtime values dynamic. The
        // adapter is the legacy dictionary boundary; concrete source rows and
        // their runtime-member reads are rendered through typed CLR references.
        if (field.AccessStrategy is ExpandoDictionaryAccess &&
            DynamicEntityBoundary.IsDynamicMetaObjectProvider(field.Type.RequireClrType()))
        {
            return SyntaxFactory.IdentifierName("dynamic");
        }

        // Text/binary switch shapes can carry an ExpandoObject as a generated
        // value without an ExpandoDictionaryAccess field strategy. Preserve the
        // legacy dynamic boundary for that adapter-only value as well.
        if (field.Type.RequireClrType() == DynamicEntityBoundary.ExpandoType)
        {
            return SyntaxFactory.IdentifierName("dynamic");
        }

        return string.IsNullOrWhiteSpace(field.GeneratedTypeName)
            ? CreateTypeSyntax(field.Type)
            : SyntaxFactory.ParseTypeName(field.GeneratedTypeName);
    }
}
