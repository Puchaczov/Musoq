using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateStrictCastExpression(CastNode cast)
        => GenerateStrictCastExpression(cast, GenerateConditionExpression(cast.Expression));

    private string GenerateStrictCastExpression(CastNode cast, string operand)
    {
        var target = BinarySchemaStrictCasts.Resolve(cast);
        _intrinsicCallCount++;
        var operation = $"global::Musoq.Evaluator.Helpers.StrictCastRuntime.To{target.CanonicalName}({operand})";
        var factory = _registry.TryGetSchema(_currentSchemaName, out var registration) && registration?.Node is TextSchemaNode
            ? "ForText" : "ForBytes";
        return $"{_intrinsicTypeName}.Invoke(() => {operation}, (__code, __error) => ParseException.{factory}(__code, SchemaName, \"{EscapeString(_activeSchemaFieldName)}\", ParsePosition, __error.Message, __error))";
    }
}
