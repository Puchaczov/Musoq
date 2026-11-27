using System.Globalization;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private const string SwitchCaseProperty = "Case";

    private string GenerateBinarySwitchReadCode(string localVar, string fieldName, BinarySwitchTypeNode switchType)
    {
        var className = _registry.BinaryCompositions.Name(switchType);
        var builder = new StringBuilder();
        AppendBinarySwitchBody(builder, localVar, fieldName, switchType);
        builder.Append(GenerateBinarySwitchConstruction($"var {localVar}", localVar, className, switchType));
        return builder.ToString();
    }

    private void AppendBinarySwitchBody(
        StringBuilder builder,
        string localVar,
        string fieldName,
        BinarySwitchTypeNode switchType
    )
    {
        var selectorType = BinarySchemaExpressionTypes.ResolveSwitchSelector(switchType, _expressionFields, _registry);
        var selectorVar = GenerateConditionExpression(switchType.SelectorExpression);
        if (switchType.SelectorExpression is not IdentifierNode)
        {
            var captured = NextStatusName("__musoqSelector");
            builder.AppendLine(CultureInfo.InvariantCulture, $"var {captured} = {selectorVar};");
            selectorVar = captured;
        }

        builder.AppendLine(CultureInfo.InvariantCulture, $"string {SwitchCaseVariable(localVar)} = null;");
        foreach (var switchCase in switchType.Cases)
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"{GetSwitchBranchClrTypeName(fieldName, switchCase)} {SwitchBranchHolder(localVar, switchCase.BranchAlias)} = default;"
            );

        var isFirstCase = true;
        foreach (var switchCase in switchType.Cases)
        {
            if (switchCase.IsDefault)
                continue;

            var matches = GenerateScalarLiteralEquality(selectorType, selectorVar, switchCase.CaseValue!);
            var keyword = isFirstCase ? "if" : "else if";
            builder.AppendLine(CultureInfo.InvariantCulture, $"{keyword} ({matches})");
            AppendBinarySwitchBranch(builder, localVar, switchCase, fieldName);
            isFirstCase = false;
        }

        if (switchType.DefaultCase is { } defaultCase)
        {
            if (!isFirstCase)
                builder.AppendLine("else");
            AppendBinarySwitchBranch(builder, localVar, defaultCase, fieldName);
            return;
        }

        if (isFirstCase)
        {
            AppendBinarySwitchNoMatch(builder, fieldName, selectorVar);
            return;
        }

        builder.AppendLine("else");
        AppendBinarySwitchNoMatch(builder, fieldName, selectorVar);
    }

    private void AppendBinarySwitchNoMatch(StringBuilder builder, string fieldName, string selectorVar)
    {
        builder.AppendLine("{");
        builder.AppendLine(
            StatusFailure(
                $"global::Musoq.Schema.Interpreters.ParseFailure.ForBytes("
                    + $"Musoq.Schema.Interpreters.ParseErrorCode.NoAlternativeMatched, SchemaName, "
                    + $"\"{EscapeString(fieldName)}\", ParsePosition, "
                    + $"\"No switch branch matched selector value \" + "
                    + $"System.Convert.ToString({selectorVar}, System.Globalization.CultureInfo.InvariantCulture))"
            )
        );
        builder.AppendLine("}");
    }

    private void AppendBinarySwitchBranch(
        StringBuilder builder,
        string localVar,
        BinarySwitchCaseNode switchCase,
        string fieldName
    )
    {
        var holder = SwitchBranchHolder(localVar, switchCase.BranchAlias);
        var temp = $"{holder}_value";

        builder.AppendLine("{");
        var readName =
            switchCase.BranchType is SchemaReferenceTypeNode
                ? fieldName
                : SwitchBranchFieldName(fieldName, switchCase.BranchAlias);
        builder.Append(Indent(GenerateBinaryValueRead(temp, readName, switchCase.BranchType), 1));
        builder.AppendLine(CultureInfo.InvariantCulture, $"    {holder} = {temp};");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"    {SwitchCaseVariable(localVar)} = \"{switchCase.BranchAlias}\";"
        );
        builder.AppendLine("}");
    }

    private string GenerateBinarySwitchConstruction(
        string assignmentTarget,
        string localVar,
        string className,
        BinarySwitchTypeNode switchType
    )
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"{assignmentTarget} = new {className}");
        builder.AppendLine("{");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    {SwitchCaseProperty} = {SwitchCaseVariable(localVar)},");

        var branchInitializers = switchType
            .Cases.Select(c =>
                $"    {EscapeCSharpIdentifier(c.BranchAlias)} = {SwitchBranchHolder(localVar, c.BranchAlias)}"
            )
            .ToArray();

        for (var i = 0; i < branchInitializers.Length; i++)
        {
            var comma = i < branchInitializers.Length - 1 ? "," : "";
            builder.AppendLine(CultureInfo.InvariantCulture, $"{branchInitializers[i]}{comma}");
        }

        builder.AppendLine("};");
        return builder.ToString();
    }

    private string GenerateSwitchNestedClass(string className, BinarySwitchTypeNode switchType)
    {
        var fieldName = className["Switch_".Length..];
        var builder = new StringBuilder();
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"/// Generated tagged-union value for binary switch '{className}'."
        );
        builder.AppendLine("/// </summary>");
        builder.AppendLine(CultureInfo.InvariantCulture, $"public sealed class {className}");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>Gets the selected branch alias.</summary>");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    public string {SwitchCaseProperty} {{ get; init; }}");
        builder.AppendLine();

        foreach (var switchCase in switchType.Cases)
        {
            var branchTypeName = GetSwitchBranchClrTypeName(fieldName, switchCase);
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"    /// <summary>Gets the '{switchCase.BranchAlias}' branch value; non-null only when selected.</summary>"
            );
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"    public {branchTypeName} {EscapeCSharpIdentifier(switchCase.BranchAlias)} {{ get; init; }}"
            );
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string SwitchCaseVariable(string localVar)
    {
        return $"{localVar}_case";
    }

    private static string SwitchBranchHolder(string localVar, string branchAlias)
    {
        return $"{localVar}_{EscapeCSharpIdentifier(branchAlias).TrimStart('@')}";
    }

    private static string SwitchBranchFieldName(string fieldName, string alias) => $"{fieldName}_{alias}";

    private string GetSwitchBranchClrTypeName(string fieldName, BinarySwitchCaseNode branch)
    {
        var type = branch.BranchType;
        var clrTypeName = GetBinaryValueClrTypeName(SwitchBranchFieldName(fieldName, branch.BranchAlias), type);
        return type is PrimitiveTypeNode or BitsTypeNode ? $"{clrTypeName}?" : clrTypeName;
    }
}
