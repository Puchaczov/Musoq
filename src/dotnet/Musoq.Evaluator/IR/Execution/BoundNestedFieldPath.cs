using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class BoundNestedFieldPath
{
    internal static NestedRootField? Resolve(ColumnRef column, RowShape shape, string alias)
    {
        if (string.IsNullOrWhiteSpace(column.Alias))
            return null;

        var current = string.Equals(column.Alias, alias, StringComparison.OrdinalIgnoreCase);
        var original = $"{column.Alias}.{column.ColumnName}";
        var relative = current ? column.ColumnName : original;
        NestedRootField? result = null;
        foreach (var field in shape.Fields)
        {
            var binding = new BoundField(field, alias, relative);
            if (current)
            {
                result = binding.Match(column.ColumnName, field.Name, result);
                result = binding.Match(original, field.QualifiedName, result);
                continue;
            }

            if (field.Name.StartsWith(column.Alias + ".", StringComparison.OrdinalIgnoreCase))
                result = binding.Match(original, field.Name, result);
            if (field.QualifiedName.StartsWith(column.Alias + ".", StringComparison.OrdinalIgnoreCase))
                result = binding.Match(original, field.QualifiedName, result);
            if (field.QualifiedName.StartsWith(alias + "." + column.Alias + ".", StringComparison.OrdinalIgnoreCase))
                result = binding.Match(alias + "." + original, field.QualifiedName, result);
        }
        return result;
    }

    private readonly record struct BoundField(FieldBinding Field, string Alias, string Relative)
    {
        internal NestedRootField? Match(string path, string prefix, NestedRootField? previous)
        {
            if (prefix.Length == 0 || path.Length <= prefix.Length ||
                !path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                path[prefix.Length] is not ('.' or '['))
                return previous;

            var memberPath = path[prefix.Length..];
            if (memberPath.StartsWith('.'))
                memberPath = memberPath[1..];
            if (IsLegacyScalarReference(memberPath))
                return previous;

            var consumed = Relative.Length - memberPath.Length;
            if (previous?.PrefixLength >= consumed)
                return previous;

            return new NestedRootField(Field, $"{Alias}.{Relative}", memberPath, consumed);
        }

        private bool IsLegacyScalarReference(string memberPath)
        {
            if (Field.GeneratedTypeName is not null ||
                Field.GeneratedMemberTypeNames.ContainsKey(memberPath) ||
                !string.Equals(memberPath, Field.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            var type = Field.Type.ResolveClrType();
            return type.GetProperty(memberPath) is null && type.GetField(memberPath) is null;
        }
    }
}
