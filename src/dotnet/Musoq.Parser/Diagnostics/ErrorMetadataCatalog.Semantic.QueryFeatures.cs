using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildQueryFeatureMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3049_InSubqueryMultipleColumns,
            "An IN subquery must return exactly one column so Musoq can match it against the left-hand expression.",
            [
                "Remove extra columns from the subquery SELECT list so it returns a single column.",
                "Use separate IN conditions combined with AND when filtering multiple columns."
            ],
            "Core Spec - IN Subqueries");

        yield return Entry(
            DiagnosticCode.MQ3050_QualifyRequiresWindowFunction,
            "The QUALIFY clause filters rows based on window function results and must reference at least one window function.",
            [
                "Add a window function to the QUALIFY expression.",
                "Use WHERE instead if you want to filter on a non-window expression."
            ],
            "Core Spec - QUALIFY");

        yield return Entry(
            DiagnosticCode.MQ3051_FilterOnNonAggregate,
            "The FILTER clause can only be applied to aggregate functions.",
            [
                "Remove FILTER from the non-aggregate function call.",
                "Use a CASE expression inside an aggregate argument for conditional aggregation."
            ],
            "Core Spec - FILTER");

        yield return Entry(
            DiagnosticCode.MQ3052_RangeFrameRequiresOrderBy,
            "A RANGE window frame requires an ORDER BY clause in the window specification.",
            [
                "Add an ORDER BY clause to the window specification.",
                "Use ROWS instead of RANGE if ordering is not required."
            ],
            "Core Spec - Window Frames");

        yield return Entry(
            DiagnosticCode.MQ3053_InvalidWindowFrameBounds,
            "The window frame start bound must not be logically after the end bound.",
            [
                "Swap the start and end bounds so the start is logically before the end.",
                "Use the ordering: UNBOUNDED PRECEDING, N PRECEDING, CURRENT ROW, N FOLLOWING, UNBOUNDED FOLLOWING."
            ],
            "Core Spec - Window Frames");

        yield return Entry(
            DiagnosticCode.MQ3054_StarModifierInInSubquery,
            "Star modifiers cannot be used inside an IN subquery because the subquery must expose exactly one stable column.",
            [
                "Replace the star modifier with an explicit column reference.",
                "Extract complex projection logic outside the IN subquery."
            ],
            "Core Spec - IN Subqueries");

        yield return Entry(
            DiagnosticCode.MQ3055_InvalidValuesSource,
            "Inline VALUES sources are strongly typed tables and each row must have a compatible field shape.",
            [
                "Make every VALUES row use the same fields.",
                "Use only literals, NULL, or literal arithmetic inside VALUES fields.",
                "Use numeric suffixes when you need a specific numeric type."
            ],
            "Core Spec - Inline VALUES Sources");

        yield return Entry(
            DiagnosticCode.MQ3056_DuplicateScriptParameterBlock,
            "A script can have only one parameter block.",
            [
                "Merge all declarations into the first param(...) block.",
                "Place the single param(...) block at the beginning of the script."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ3057_ScriptParameterBlockAfterStatement,
            "Script parameters are a script preamble and must be declared before query statements.",
            [
                "Move param(...) before SELECT, WITH, FROM, TABLE, COUPLE, or DESC.",
                "Omit the parameter block entirely if the script does not need parameters."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ3058_DuplicateScriptParameterName,
            "The same script parameter name was declared more than once.",
            [
                "Remove the duplicate declaration.",
                "Use a distinct name for each parameter."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ3059_UndeclaredScriptParameter,
            "A parameter reference was used without a matching declaration in the param(...) block.",
            [
                "Add the parameter to the param(...) block.",
                "Check the parameter name for typos; parameter names are case-sensitive."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ3060_UnsupportedScriptParameterType,
            "The declared script parameter type is not one of the supported primitive query types.",
            [
                "Use a supported primitive type such as string, bool, int, decimal, datetime, guid, or a nullable form like int?.",
                "Do not declare complex object or array parameters in the param(...) block."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ3061_InvalidScriptParameterDefault,
            "The script parameter default cannot be bound to the declared parameter type.",
            [
                "Use only primitive constants or null as defaults.",
                "For guid, datetime, datetimeoffset, and timespan defaults, use a parseable string literal."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument,
            "Script parameters used as data-source arguments must be direct references. A default is needed only when the provider requires the value to resolve compile-time source metadata; computed or nested expressions are not supported.",
            [
                "Pass the parameter directly, for example: #Files.All($path).",
                "If source metadata requires the value, declare a default, for example: param(path: string = '/tmp'), or make the provider metadata independent of runtime source arguments."
            ],
            "Core Spec - Script Parameters");

        yield return Entry(
            DiagnosticCode.MQ3063_DuplicateScriptSymbolName,
            "A script variable and another script symbol use the same name, making $name ambiguous.",
            [
                "Rename one of the script symbols.",
                "Keep param(...) names and let names unique within the script."
            ],
            "Core Spec - Script Variables");

        yield return Entry(
            DiagnosticCode.MQ3064_UnsupportedScriptVariableType,
            "The declared script variable type is not one of the supported primitive query types.",
            [
                "Use a supported primitive type such as string, bool, int, decimal, datetime, guid, or a nullable value type like int?.",
                "Do not declare complex object or array script variables."
            ],
            "Core Spec - Script Variables");

        yield return Entry(
            DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
            "The script variable initializer cannot be evaluated as a compile-time constant for the declared type.",
            [
                "Use literals, constant operators, and earlier let variables only.",
                "Move runtime expressions, data-source columns, and function calls into the query body."
            ],
            "Core Spec - Script Variables");

        yield return Entry(
            DiagnosticCode.MQ3066_ScriptVariableUsedBeforeDeclaration,
            "A script variable initializer references a let variable that has not been declared yet.",
            [
                "Move the referenced let declaration earlier in the script.",
                "Check the script variable name for typos; names are case-sensitive."
            ],
            "Core Spec - Script Variables");
    }
}
