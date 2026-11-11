using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class GeneratedRowColumnVisitation
{
    private const string ColumnRead = "global::Musoq.Evaluator.Tables.TypedColumnRead";
    private const string ColumnVisit = "global::Musoq.Evaluator.Tables.TypedColumnVisit";

    public static MethodDeclarationSyntax CreateWrite(
        IReadOnlyList<FieldBinding> fields,
        System.Func<FieldBinding, string> fieldName,
        System.Func<FieldBinding, TypeSyntax> fieldType
    )
    {
        var writes = fields.Select(
            (field, index) =>
                $"{index} => {ColumnRead}.Read<T, {ReadType(field, fieldType)}>("
                + $"this.{ExecutionSyntaxFactory.EscapeIdentifier(fieldName(field))} = "
                + $"{ColumnRead}.Read<{ReadType(field, fieldType)}, T>(value)),"
        );
        return (MethodDeclarationSyntax)
            SyntaxFactory.ParseMemberDeclaration(
                $"public override void AssignValue<T>(int columnNumber, T value)\n{{\n    {ColumnRead}.Discard<T>(columnNumber switch\n    {{\n        {string.Join("\n", writes)}\n        _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))\n    }});\n}}"
            )!;
    }

    public static MethodDeclarationSyntax CreateRead(
        IReadOnlyList<FieldBinding> fields,
        System.Func<FieldBinding, string> fieldName,
        System.Func<FieldBinding, TypeSyntax> fieldType
    )
    {
        var reads = fields.Select(
            (field, index) =>
                $"{index} => {ColumnRead}.Read<T, {ReadType(field, fieldType)}>({ReadValue(field, fieldName, fieldType)}),"
        );
        return (MethodDeclarationSyntax)
            SyntaxFactory.ParseMemberDeclaration(
                $"public override T GetValue<T>(int columnIndex) => columnIndex switch\n{{\n    {string.Join("\n", reads)}\n    _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))\n}};"
            )!;
    }

    public static MethodDeclarationSyntax Create(
        IReadOnlyList<FieldBinding> fields,
        System.Func<FieldBinding, string> fieldName,
        System.Func<FieldBinding, TypeSyntax> fieldType
    )
    {
        var calls = fields.Select(
            (field, index) => $"{ColumnVisit}.Apply({index}, {ReadValue(field, fieldName, fieldType)}, ref visitor);"
        );
        return (MethodDeclarationSyntax)
            SyntaxFactory.ParseMemberDeclaration(
                $"public override void VisitColumns<TVisitor>(ref TVisitor visitor)\n{{\n    {string.Join("\n", calls)}\n}}"
            )!;
    }

    private static string ReadType(FieldBinding field, System.Func<FieldBinding, TypeSyntax> fieldType)
    {
        var type = fieldType(field).ToString();
        if (type == "dynamic")
            return Musoq.Evaluator.Helpers.EvaluationHelper.GetCastableType(field.Type.RequireClrType());
        return type;
    }

    private static string ReadValue(
        FieldBinding field,
        System.Func<FieldBinding, string> fieldName,
        System.Func<FieldBinding, TypeSyntax> fieldType
    )
    {
        var value = "this." + ExecutionSyntaxFactory.EscapeIdentifier(fieldName(field));
        if (fieldType(field).ToString() == "dynamic")
            return $"({ReadType(field, fieldType)}){value}";
        return value;
    }
}
