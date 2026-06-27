using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

internal static class SetOperationKeySyntaxFactory
{
    public static TypeSyntax CreateHashSetTypeSyntax(IReadOnlyList<Type> fieldTypes)
    {
        if (fieldTypes.Count <= 7)
            return SyntaxFactory.ParseTypeName(CreateHashSetTypeName(CreateKeyType(fieldTypes)));

        var typeNames = fieldTypes.Select(EvaluationHelper.GetCastableType);
        return SyntaxFactory.ParseTypeName($"HashSet<({string.Join(", ", typeNames)})>");
    }

    private static string CreateHashSetTypeName(Type keyType)
    {
        return $"HashSet<{EvaluationHelper.GetCastableType(keyType)}>";
    }

    private static Type CreateKeyType(IReadOnlyList<Type> fieldTypes)
    {
        if (fieldTypes.Count == 1)
            return fieldTypes[0];

        return fieldTypes.Count switch
        {
            2 => typeof(ValueTuple<,>).MakeGenericType(fieldTypes.ToArray()),
            3 => typeof(ValueTuple<,,>).MakeGenericType(fieldTypes.ToArray()),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(fieldTypes.ToArray()),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(fieldTypes.ToArray()),
            6 => typeof(ValueTuple<,,,,,>).MakeGenericType(fieldTypes.ToArray()),
            7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(fieldTypes.ToArray()),
            _ => throw new NotSupportedException(
                $"Hash set set-operation keys support at least one field. Found {fieldTypes.Count.ToString(CultureInfo.InvariantCulture)}.")
        };
    }
}
