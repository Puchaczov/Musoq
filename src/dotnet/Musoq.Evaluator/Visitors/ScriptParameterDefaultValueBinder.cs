using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class ScriptParameterDefaultValueBinder
{
    public static bool TryBind(
        ParameterDeclarationNode declaration,
        Type parameterType,
        out object? value,
        out string error)
    {
        value = null;
        error = string.Empty;

        if (parameterType.IsArray && declaration.HasDefaultValue)
        {
            error = $"Collection parameter '{declaration.Name}' cannot declare a default value.";
            return false;
        }

        if (!declaration.HasDefaultValue)
            return true;

        if (declaration.DefaultValue is NullNode)
            return ConvertDefaultValue(declaration, parameterType, null, out value, out error);

        if (declaration.DefaultValue is not ConstantValueNode constantValue)
        {
            error = $"Parameter '{declaration.Name}' default must be a primitive constant or null.";
            return false;
        }

        return ConvertDefaultValue(declaration, parameterType, constantValue.ObjValue, out value, out error);
    }

    private static bool ConvertDefaultValue(
        ParameterDeclarationNode declaration,
        Type parameterType,
        object? rawValue,
        out object? value,
        out string error)
    {
        var result = ScriptValueConverter.ConvertValue(
            "Parameter",
            declaration.Name,
            declaration.DeclaredTypeName,
            parameterType,
            rawValue);

        value = result.Value;
        error = result.Error;
        return result.Success;
    }
}
