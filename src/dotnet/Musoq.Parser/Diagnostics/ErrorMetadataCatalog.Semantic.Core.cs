using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildCoreMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3001_UnknownColumn,
            "The column name could not be resolved in any of the available data sources.",
            [
                "Check the column name for typos.",
                "Qualify the column with a table alias: alias.ColumnName."
            ],
            "Core Spec - Column References");

        yield return Entry(
            DiagnosticCode.MQ3002_AmbiguousColumn,
            "The column name matches columns in multiple data sources and is ambiguous.",
            ["Qualify the column with a table alias: alias.ColumnName."],
            "Core Spec - Column References");

        yield return Entry(
            DiagnosticCode.MQ3003_UnknownTable,
            "The referenced table or schema method could not be resolved in the selected schema.",
            [
                "Check the method name after the schema prefix, for example #schema.method().",
                "Verify the schema exposes this data source."
            ],
            "Core Spec - FROM Clause");

        yield return Entry(
            DiagnosticCode.MQ3004_UnknownFunction,
            "No function with this name and compatible arguments could be found.",
            [
                "Check the function name for typos.",
                "Verify the argument types match an available overload."
            ],
            "Core Spec - Functions");

        yield return Entry(
            DiagnosticCode.MQ3005_TypeMismatch,
            "The expression type does not match the expected type in this context.",
            [
                "Use an explicit conversion function such as ToInt32, ToDecimal, or ToString.",
                "Verify the column type matches the expected usage."
            ],
            "Core Spec - Type System");

        yield return Entry(
            DiagnosticCode.MQ3006_InvalidArgumentCount,
            "The function was called with the wrong number of arguments.",
            [
                "Check the function signature for the expected argument count.",
                "Verify you are calling the correct overload."
            ],
            "Core Spec - Functions");

        yield return Entry(
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            "The operator cannot be applied to the given operand types.",
            [
                "Convert operands to compatible types before comparing.",
                "For string date comparisons, parse to a numeric or date representation first."
            ],
            "Core Spec - Operator Type Rules");

        yield return Entry(
            DiagnosticCode.MQ3008_DivisionByZero,
            "Division by zero was detected in a constant expression.",
            ["Add a CASE WHEN check to guard against dividing by zero."],
            "Core Spec - Arithmetic Operators");

        yield return Entry(
            DiagnosticCode.MQ3009_NullReference,
            "A binding path may dereference a null value.",
            [
                "Add a null guard before accessing the member.",
                "Use a CASE expression or nullable-aware source shape when nulls are expected."
            ],
            "Core Spec - Null Handling");

        yield return Entry(
            DiagnosticCode.MQ3010_UnknownSchema,
            "The referenced schema could not be found in the registered schema providers.",
            [
                "Verify the schema name is correct: #schemaName.method().",
                "Ensure the schema provider is registered."
            ],
            "Core Spec - Schema References");

        yield return Entry(
            DiagnosticCode.MQ3011_AggregateNotAllowed,
            "An aggregate function appears in a context where aggregate evaluation is not allowed.",
            [
                "Move the aggregate to SELECT, HAVING, or another aggregate-aware clause.",
                "Use a non-aggregate expression in row-level filters such as WHERE."
            ],
            "Core Spec - GROUP BY and Aggregation");

        yield return Entry(
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            "Every selected column must be either aggregated or included in the GROUP BY clause.",
            [
                "Add the column to the GROUP BY clause.",
                "Wrap the column in an aggregate function such as Count, Sum, Min, or Max."
            ],
            "Core Spec - GROUP BY and Aggregation");

        yield return Entry(
            DiagnosticCode.MQ3013_CannotResolveMethod,
            "No method overload matches the argument types provided.",
            [
                "Use explicit type conversions such as ToInt32 or ToString.",
                "Check the method name for typos."
            ],
            "Core Spec - Method Resolution");

        yield return Entry(
            DiagnosticCode.MQ3014_InvalidPropertyAccess,
            "The referenced property does not exist on the object type.",
            [
                "Check the property name for typos.",
                "Verify the object type exposes this property."
            ],
            "Core Spec - Property Access");

        yield return Entry(
            DiagnosticCode.MQ3015_UnknownAlias,
            "The query references an alias that is not visible in the current scope.",
            [
                "Check the alias name for typos.",
                "Define the alias in the FROM or APPLY clause before referencing it."
            ],
            "Core Spec - Aliasing");

        yield return Entry(
            DiagnosticCode.MQ3016_CircularReference,
            "The query contains references that depend on each other in a cycle.",
            [
                "Break the cycle by introducing an intermediate projection.",
                "Ensure aliases do not refer back to expressions that depend on them."
            ],
            "Core Spec - Binding");

        yield return Entry(
            DiagnosticCode.MQ3017_ObjectNotArray,
            "Array access was attempted on an object that is not an array.",
            ["Verify the column or expression returns an array or IEnumerable type."],
            "Core Spec - Array Access");

        yield return Entry(
            DiagnosticCode.MQ3018_NoIndexer,
            "The object does not implement an indexer for bracket access.",
            ["Use a different access pattern or check the object type."],
            "Core Spec - Indexer Access");

        yield return Entry(
            DiagnosticCode.MQ3019_SetOperatorColumnCount,
            "Set operators require both queries to have the same number of columns.",
            ["Adjust the SELECT lists so both queries produce the same number of columns."],
            "Core Spec - Set Operators");

        yield return Entry(
            DiagnosticCode.MQ3020_SetOperatorColumnTypes,
            "Set operators require matching column types between the two queries.",
            ["Convert columns to matching types using ToInt32, ToString, or another explicit conversion."],
            "Core Spec - Set Operators");

        yield return Entry(
            DiagnosticCode.MQ3021_DuplicateAlias,
            "An alias with this name was already defined earlier in the query.",
            ["Use a different alias name for this source or expression."],
            "Core Spec - Aliasing");

        yield return Entry(
            DiagnosticCode.MQ3022_MissingAlias,
            "In multi-source queries, method calls must be qualified with a source alias so Musoq can choose which schema library implementation to invoke.",
            [
                "Prefix the method with the owning source alias, for example: a.ToDecimal(a.Id) or b.Sum(b.Amount).",
                "For aggregates, remember that the alias chooses the schema library implementation, not the input column source.",
                "If the aggregate is already aliased in SELECT, prefer that projection alias in ORDER BY instead of repeating the aggregate expression."
            ],
            "Core Spec - JOIN Clause and Aggregation");

        yield return Entry(
            DiagnosticCode.MQ3023_TableNotDefined,
            "The referenced table or data source is not defined in this query.",
            [
                "Verify the table alias or CTE name is correct.",
                "Ensure the data source is defined in a FROM clause."
            ],
            "Core Spec - FROM Clause");

        yield return Entry(
            DiagnosticCode.MQ3024_GroupByIndexOutOfRange,
            "A positional GROUP BY reference points outside the SELECT projection list.",
            [
                "Use a GROUP BY index between 1 and the number of selected columns.",
                "Prefer grouping by the expression or alias directly for clarity."
            ],
            "Core Spec - GROUP BY Clause");

        yield return Entry(
            DiagnosticCode.MQ3025_ColumnMustBeArray,
            "The expression must return an array or enumerable value for this operation.",
            [
                "Use a column that returns an array or IEnumerable value.",
                "Remove the array operation if the source value is scalar."
            ],
            "Core Spec - APPLY and Arrays");

        yield return Entry(
            DiagnosticCode.MQ3026_ColumnNotBindable,
            "The selected column cannot be bound as a table source.",
            [
                "Expose the property as bindable in the schema when it should be used as a nested table.",
                "Use a regular property reference if the value should stay scalar."
            ],
            "Core Spec - Bindable Properties");

        yield return Entry(
            DiagnosticCode.MQ3027_InvalidExpressionType,
            "The expression returns a type that is not valid for the current query construct.",
            [
                "Use an expression that returns the expected type.",
                "Add an explicit conversion before passing the expression to this construct."
            ],
            "Core Spec - Type System");

        yield return Entry(
            DiagnosticCode.MQ3028_UnknownProperty,
            "The property name could not be resolved on the referenced object.",
            [
                "Check the property name for typos.",
                "Verify the object type exposes this property before accessing it."
            ],
            "Core Spec - Property Access");

        yield return Entry(
            DiagnosticCode.MQ3029_UnresolvableMethod,
            "No method overload matches the provided argument types.",
            [
                "Check argument types and convert if necessary.",
                "Verify the method name is correct."
            ],
            "Core Spec - Method Resolution");

        yield return Entry(
            DiagnosticCode.MQ3030_ConstructionNotSupported,
            "This syntax or construction is not supported in the current version of Musoq.",
            [
                "Rewrite using a supported equivalent.",
                "Check the documentation for supported constructions."
            ],
            "Core Spec - Unsupported Constructions");

        yield return Entry(
            DiagnosticCode.MQ3031_SetOperatorMissingKeys,
            "Legacy set-operator missing-key diagnostic. Omitted keys and empty key lists now compare all projected values.",
            [
                "Omit the key list, or write (), to compare all projected values.",
                "Use an explicit key list such as UNION (key1, key2) only when comparing a subset."
            ],
            "Core Spec - Set Operators");

        yield return Entry(
            DiagnosticCode.MQ3032_ArithmeticOverflow,
            "A constant arithmetic expression overflowed its target numeric type.",
            [
                "Use a larger numeric type or a decimal literal.",
                "Reduce the literal value so it fits the target type."
            ],
            "Core Spec - Arithmetic Operators");

        yield return Entry(
            DiagnosticCode.MQ3033_InterpretFunctionOutsideApply,
            "Parse and Interpret functions can only be used inside CROSS APPLY or OUTER APPLY.",
            [
                "Move the function call to a CROSS APPLY or OUTER APPLY clause.",
                "Use TryParse in OUTER APPLY if parsing may fail."
            ],
            "Binary/Text Spec - Usage");

        yield return Entry(
            DiagnosticCode.MQ3034_AmbiguousAggregateOwner,
            "An unqualified aggregate call matched multiple source aliases with different aggregate implementations.",
            [
                "Prefix the aggregate with the intended source alias, for example: first.Sum(...) or second.Sum(...).",
                "If the aggregate appears in ORDER BY, alias it in SELECT first and order by that projection alias."
            ],
            "Core Spec - Aggregation");

        yield return Entry(
            DiagnosticCode.MQ3035_AmbiguousMethodOwner,
            "An unqualified method call matched multiple source aliases with different method implementations.",
            [
                "Prefix the method with the intended source alias, for example: first.MyMethod(...) or second.MyMethod(...).",
                "Choose the alias whose schema library should own the method implementation."
            ],
            "Core Spec - Method Resolution");
    }
}
