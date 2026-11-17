using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static MemberDeclarationSyntax CreateOrderRecordComparerClass(ExecutionOrderRecordList orderRecords)
    {
        return CreateOrderRecordComparerClass(orderRecords.RecordShape, orderRecords.Keys);
    }

    private static MemberDeclarationSyntax CreateOrderRecordComparerClass(ExecutionCreateBoundedRecordList orderRecords)
    {
        return CreateOrderRecordComparerClass(
            orderRecords.RecordShape,
            orderRecords.Keys,
            orderRecords.UsesSingleRecordBuffer
        );
    }

    private static MemberDeclarationSyntax CreateOrderRecordComparerClass(
        GeneratedRecordShape recordShape,
        IReadOnlyList<ExecutionOrderField> keys,
        bool emitSingleRecordBuffer = false
    )
    {
        var typeName = CreateOrderRecordComparerTypeName(recordShape);
        var recordTypeName = recordShape.TypeName;
        var body = new List<string>
        {
            $"private sealed class {typeName} : IComparer<{recordTypeName}>",
            "{",
            $"    public static readonly {typeName} Instance = new {typeName}();",
            string.Empty,
            $"    public int Compare({recordTypeName} left, {recordTypeName} right)",
            "    {",
        };

        for (var index = 0; index < keys.Count; index++)
        {
            var key = keys[index];
            var field = recordShape.Fields[key.OutputIndex];
            AddOrderRecordComparisonStatements(body, index, key, field);
        }

        body.Add("        return left.__ordinal.CompareTo(right.__ordinal);");
        body.Add("    }");
        if (emitSingleRecordBuffer)
            body.Add(EmitSingleRecordBuffer(recordTypeName));
        body.Add("}");

        return SyntaxFactory.ParseMemberDeclaration(string.Join(Environment.NewLine, body))!;
    }

    private static string EmitSingleRecordBuffer(string recordTypeName) =>
        $$"""
            public struct SingleRecordBuffer
            {
                private {{recordTypeName}} _best;
                private bool _hasRecord;

                public int Count { get; private set; }

                public void Add({{recordTypeName}} candidate)
                {
                    Count++;
                    if (!_hasRecord || Instance.Compare(candidate, _best) < 0)
                    {
                        _best = candidate;
                        _hasRecord = true;
                    }
                }

                public Enumerator GetEnumerator() => new Enumerator(_best, _hasRecord);

                public struct Enumerator({{recordTypeName}} record, bool hasRecord)
                {
                    private bool _pending = hasRecord;
                    public {{recordTypeName}} Current => record;

                    public bool MoveNext()
                    {
                        var pending = _pending;
                        _pending = false;
                        return pending;
                    }
                }
            }
            """;

    private static string CreateOrderRecordComparisonExpression(FieldBinding field, Type keyType)
    {
        var fieldName = EscapeIdentifier(GetGeneratedFieldName(field));
        var left = $"left.{fieldName}";
        var right = $"right.{fieldName}";
        var nullableType = Nullable.GetUnderlyingType(keyType);

        if (keyType == typeof(string))
            return $"StringComparer.Ordinal.Compare({left}, {right})";

        if (nullableType != null)
            return $"Nullable.Compare({left}, {right})";

        if (!keyType.IsValueType)
            return $"Comparer<{EvaluationHelper.GetCastableType(keyType)}>.Default.Compare({left}, {right})";

        return $"{left}.CompareTo({right})";
    }

    private static string CreateOrderRecordComparerTypeName(GeneratedRecordShape recordShape)
    {
        return $"{recordShape.TypeName}Comparer";
    }
}
