using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static partial class BuildMetadataAndInferTypesVisitorUtilities
{
    internal static ISchemaTable CreateEmptyTable()
    {
        return new DynamicTable([]);
    }

    internal static FieldNode[] ResolveFieldsForCache(FieldNode[] leftFields, FieldNode[] rightFields)
    {
        var resolved = new FieldNode[leftFields.Length];

        for (var i = 0; i < leftFields.Length; i++)
            resolved[i] = leftFields[i].Expression.ReturnType is NullNode.NullType
                ? rightFields[i]
                : leftFields[i];

        return resolved;
    }

    internal static void PrepareAndThrowUnknownColumnExceptionMessage(string identifier, ISchemaColumn[] columns,
        TextSpan span = default)
    {
        var library = new TransitionLibrary();
        var candidates = new StringBuilder();

        var candidatesColumns = columns.Where(col =>
            library.Soundex(col.ColumnName) == library.Soundex(identifier) ||
            library.LevenshteinDistance(col.ColumnName, identifier) < 3).ToArray();

        for (var i = 0; i < candidatesColumns.Length - 1; i++)
        {
            var candidate = candidatesColumns[i];
            candidates.Append(candidate.ColumnName);
            candidates.Append(", ");
        }

        if (candidatesColumns.Length > 0)
        {
            candidates.Append(candidatesColumns[^1].ColumnName);

            throw new UnknownColumnOrAliasException(
                identifier,
                $"Did you mean to use [{candidates}]?",
                span);
        }

        throw new UnknownColumnOrAliasException(identifier, string.Empty, span);
    }

    internal static void PrepareAndThrowUnknownPropertyExceptionMessage(string identifier, PropertyInfo[] properties,
        TextSpan span = default)
    {
        var library = new TransitionLibrary();
        var candidates = new StringBuilder();

        var candidatesProperties = properties.Where(prop =>
            library.Soundex(prop.Name) == library.Soundex(identifier) ||
            library.LevenshteinDistance(prop.Name, identifier) < 3).ToArray();

        for (var i = 0; i < candidatesProperties.Length - 1; i++)
        {
            var candidate = candidatesProperties[i];
            candidates.Append(candidate.Name);
            candidates.Append(", ");
        }

        if (candidatesProperties.Length > 0)
        {
            candidates.Append(candidatesProperties[^1].Name);

            throw new UnknownPropertyException(
                identifier,
                $"Did you mean to use [{candidates}]?",
                span);
        }

        throw new UnknownPropertyException(identifier, "unknown", span);
    }

    internal static bool IsInterpretFunction(string functionName)
    {
        return functionName.Equals("Interpret", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("TryParse", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("PartialInterpret", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("PartialParse", StringComparison.OrdinalIgnoreCase);
    }

    internal static void ThrowIfOldInterpretSyntax(string functionName, ArgsListNode args)
    {
        if (args.Args.Length < 2)
            return;

        var lastArg = args.Args[^1];
        var schemaName = lastArg switch
        {
            StringNode sn => sn.Value,
            WordNode wn => wn.Value,
            _ => null
        };

        if (schemaName == null)
            return;

        throw new InvalidOperationException(
            $"The syntax '{functionName}(data, ''{schemaName}'')' is no longer supported. Use '{functionName}<{schemaName}>(data)' instead.");
    }

    internal static bool IsInterpretOrParseFunction(string methodName)
    {
        return methodName.Equals("Interpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryParse", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("PartialInterpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("PartialParse", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool AreSameMethod(MethodInfo left, MethodInfo right)
    {
        return left.Module.Equals(right.Module) && left.MetadataToken == right.MetadataToken;
    }

    internal static Type[] GetArgumentTypes(ArgsListNode args)
    {
        var argTypes = new Type[args.Args.Length];

        for (var i = 0; i < args.Args.Length; i++)
            argTypes[i] = args.Args[i].ReturnType ?? typeof(object);

        return argTypes;
    }

    internal static string? ExtractSchemaNameFromArgs(ArgsListNode args, string? functionName = null)
    {
        var schemaArgIndex = functionName?.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) == true ? 2 : 1;

        if (args.Args.Length <= schemaArgIndex)
            throw new InvalidOperationException(
                $"Interpret function '{functionName ?? "unknown"}' requires at least {schemaArgIndex + 1} arguments, got {args.Args.Length}.");

        var schemaArg = args.Args[schemaArgIndex];

        if (schemaArg is StringNode stringNode)
            return stringNode.Value;

        if (schemaArg is WordNode wordNode)
            return wordNode.Value;

        if (schemaArg is IdentifierNode identifierNode)
            throw new InvalidOperationException(
                $"Schema name '{identifierNode.Name}' must be quoted. Use '{functionName ?? "Parse"}(source, \'{identifierNode.Name}\')' instead of '{functionName ?? "Parse"}(source, {identifierNode.Name})'.");

        throw new InvalidOperationException(
            $"Expected schema name as a quoted string at argument index {schemaArgIndex}, got {schemaArg?.GetType().Name ?? "null"}.");
    }

    internal static Exception SetOperatorDoesNotHaveKeysException(string setOperator)
    {
        return new SetOperatorMustHaveKeyColumnsException(setOperator);
    }

    private static readonly FrozenDictionary<string, string> DialectColumnHints =
        new[] { "TOP", "FIRST", "LIMIT" }.ToDictionary(
            keyword => keyword,
            keyword => $"Musoq does not support {keyword}. Use TAKE after the FROM clause instead. " +
                       "Example: SELECT Name FROM #schema.method() alias TAKE 5",
            StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    internal static string? GetDialectColumnHint(string identifier)
    {
        return DialectColumnHints.GetValueOrDefault(identifier);
    }
}
