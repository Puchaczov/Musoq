using System.Globalization;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private const string SwitchCaseProperty = "Case";

    private string GenerateBinarySwitchReadCode(string localVar, string fieldName, BinarySwitchTypeNode switchType)
    {
        var className = GetOrRegisterSwitchClassName(fieldName, switchType);
        var builder = new StringBuilder();
        AppendBinarySwitchBody(builder, localVar, switchType);
        builder.Append(GenerateBinarySwitchConstruction($"var {localVar}", localVar, className, switchType));
        return builder.ToString();
    }

    private string GenerateBinarySwitchReadCodeInner(string localVar, string fieldName, BinarySwitchTypeNode switchType)
    {
        var className = GetOrRegisterSwitchClassName(fieldName, switchType);
        var builder = new StringBuilder();
        AppendBinarySwitchBody(builder, localVar, switchType);
        builder.Append(GenerateBinarySwitchConstruction(localVar, localVar, className, switchType));
        return builder.ToString();
    }

    private void AppendBinarySwitchBody(StringBuilder builder, string localVar, BinarySwitchTypeNode switchType)
    {
        var selectorVar = GetLocalVarName(switchType.Selector);

        builder.AppendLine(CultureInfo.InvariantCulture, $"string {SwitchCaseVariable(localVar)} = null;");
        foreach (var switchCase in switchType.Cases)
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{GetClrTypeName(switchCase.BranchType)} {SwitchBranchHolder(localVar, switchCase.BranchAlias)} = default;");

        var isFirstCase = true;
        foreach (var switchCase in switchType.Cases)
        {
            if (switchCase.IsDefault)
                continue;

            var caseLiteral = GenerateConditionExpression(switchCase.CaseValue!);
            var keyword = isFirstCase ? "if" : "else if";
            builder.AppendLine(CultureInfo.InvariantCulture, $"{keyword} ({selectorVar} == {caseLiteral})");
            AppendBinarySwitchBranch(builder, localVar, switchCase);
            isFirstCase = false;
        }

        if (switchType.DefaultCase is { } defaultCase)
        {
            if (!isFirstCase)
                builder.AppendLine("else");
            AppendBinarySwitchBranch(builder, localVar, defaultCase);
            return;
        }

        if (isFirstCase)
            return;

        builder.AppendLine("else");
        builder.AppendLine("{");
        var throwStatement =
            "    throw new System.InvalidOperationException(\"No switch branch matched selector value \" + " +
            selectorVar + ");";
        builder.AppendLine(throwStatement);
        builder.AppendLine("}");
    }

    private void AppendBinarySwitchBranch(StringBuilder builder, string localVar, BinarySwitchCaseNode switchCase)
    {
        var holder = SwitchBranchHolder(localVar, switchCase.BranchAlias);
        var temp = $"{holder}_value";

        builder.AppendLine("{");
        builder.Append(Indent(GenerateBinarySwitchBranchRead(temp, switchCase.BranchType), 1));
        builder.AppendLine(CultureInfo.InvariantCulture, $"    {holder} = {temp};");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    {SwitchCaseVariable(localVar)} = \"{switchCase.BranchAlias}\";");
        builder.AppendLine("}");
    }

    private string GenerateBinarySwitchBranchRead(string branchVar, TypeAnnotationNode branchType)
    {
        var builder = new StringBuilder();
        switch (branchType)
        {
            case PrimitiveTypeNode primitiveType:
                builder.AppendLine(CultureInfo.InvariantCulture,
                    $"var {branchVar} = {GetPrimitiveReadMethod(primitiveType)}(data);");
                break;

            case ByteArrayTypeNode byteArrayType:
                var size = GenerateSizeExpression(byteArrayType.SizeExpression);
                builder.AppendLine(CultureInfo.InvariantCulture, $"var {branchVar} = ReadBytes(data, {size});");
                break;

            case SchemaReferenceTypeNode schemaRef:
                var interpreterVar = $"{branchVar}_interpreter";
                builder.AppendLine(CultureInfo.InvariantCulture, $"var {interpreterVar} = new {schemaRef.SchemaName}();");
                builder.AppendLine(CultureInfo.InvariantCulture,
                    $"var {branchVar} = {interpreterVar}.InterpretAt(data, ParsePosition);");
                builder.AppendLine(CultureInfo.InvariantCulture, $"ParsePosition = {interpreterVar}.BytesConsumed;");
                break;

            default:
                throw CreateUnsupportedCodeGenerationException(
                    branchVar,
                    branchType,
                    "binary switch branch type");
        }

        return builder.ToString();
    }

    private string GenerateBinarySwitchConstruction(
        string assignmentTarget,
        string localVar,
        string className,
        BinarySwitchTypeNode switchType)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"{assignmentTarget} = new {className}");
        builder.AppendLine("{");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    {SwitchCaseProperty} = {SwitchCaseVariable(localVar)},");

        var branchInitializers = switchType.Cases
            .Select(c => $"    {EscapeCSharpIdentifier(c.BranchAlias)} = {SwitchBranchHolder(localVar, c.BranchAlias)}")
            .ToArray();

        for (var i = 0; i < branchInitializers.Length; i++)
        {
            var comma = i < branchInitializers.Length - 1 ? "," : "";
            builder.AppendLine(CultureInfo.InvariantCulture, $"{branchInitializers[i]}{comma}");
        }

        builder.AppendLine("};");
        return builder.ToString();
    }

    private string GetOrRegisterSwitchClassName(string fieldName, BinarySwitchTypeNode switchType)
    {
        var className = SwitchClassName(fieldName);
        if (_switchSchemas.All(x => x.ClassName != className))
            _switchSchemas.Add((className, switchType));
        return className;
    }

    private string GenerateSwitchNestedClass(string className, BinarySwitchTypeNode switchType)
    {
        var builder = new StringBuilder();
        builder.AppendLine("/// <summary>");
        builder.AppendLine(CultureInfo.InvariantCulture, $"/// Generated tagged-union value for binary switch '{className}'.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine(CultureInfo.InvariantCulture, $"public sealed class {className}");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>Gets the selected branch alias.</summary>");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    public string {SwitchCaseProperty} {{ get; init; }}");
        builder.AppendLine();

        foreach (var switchCase in switchType.Cases)
        {
            var branchTypeName = GetClrTypeName(switchCase.BranchType);
            builder.AppendLine(CultureInfo.InvariantCulture, $"    /// <summary>Gets the '{switchCase.BranchAlias}' branch value; non-null only when selected.</summary>");
            builder.AppendLine(CultureInfo.InvariantCulture, $"    public {branchTypeName} {EscapeCSharpIdentifier(switchCase.BranchAlias)} {{ get; init; }}");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string SwitchClassName(string fieldName)
    {
        return $"Switch_{fieldName}";
    }

    private static string SwitchCaseVariable(string localVar)
    {
        return $"{localVar}_case";
    }

    private static string SwitchBranchHolder(string localVar, string branchAlias)
    {
        return $"{localVar}_{EscapeCSharpIdentifier(branchAlias)}";
    }
}
