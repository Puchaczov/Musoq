using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.Visitors;

/// <summary>Compile-time C# emission shared by query and embedded-schema concatenation.</summary>
internal sealed class StringConcatenationCodeEmitter(string callbackTypeName)
{
    private readonly SortedSet<int> _arities = [];
    private int _expressionCount;

    internal void RegisterArity(int arity)
    {
        if (arity > 4)
            _arities.Add(arity);
    }

    internal string Emit(Musoq.Parser.Nodes.Node expression, Func<Musoq.Parser.Nodes.Node, string> render)
    {
        var operands = new List<string>();
        CollectOperands(expression, render, operands);
        return Emit(operands);
    }

    private static void CollectOperands(Musoq.Parser.Nodes.Node expression,
        Func<Musoq.Parser.Nodes.Node, string> render, List<string> operands)
    {
        if (expression is Musoq.Parser.Nodes.StringConcatenationNode concatenation)
        {
            CollectOperands(concatenation.Left, render, operands);
            CollectOperands(concatenation.Right, render, operands);
            return;
        }
        operands.Add(render(expression));
    }

    internal string Emit(IReadOnlyList<string> operands)
    {
        ArgumentNullException.ThrowIfNull(operands);
        var prefix = $"__musoqConcat{_expressionCount++}_";
        var names = Enumerable.Range(0, operands.Count).Select(index => prefix + index).ToArray();
        var captures = operands.Select((operand, index) => $"((string)({operand})) is var {names[index]}");
        var present = names.Select(name => $"{name} != null");
        var result = EmitResult(names);
        return $"({string.Join(" && ", captures.Concat(present))} ? {result} : (string)null)";
    }

    private string EmitResult(IReadOnlyList<string> names)
    {
        if (names.Count <= 4)
            return $"global::System.String.Concat({string.Join(", ", names)})";
        _arities.Add(names.Count);
        var length = string.Join(" + ", names.Select(name => name + ".Length"));
        return $"global::System.String.Create(checked({length}), ({string.Join(", ", names)}), {callbackTypeName}.Write{names.Count})";
    }

    internal string EmitCallbackClass()
    {
        if (_arities.Count == 0)
            return string.Empty;
        var code = new StringBuilder();
        code.AppendLine(CultureInfo.InvariantCulture, $"internal static class {callbackTypeName} {{");
        code.AppendLine(CultureInfo.InvariantCulture, $"static {callbackTypeName}() {{ }}");
        foreach (var arity in _arities)
            EmitCallback(code, arity);
        code.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        code.AppendLine("internal static void Initialize() {");
        foreach (var arity in _arities)
            code.AppendLine(CultureInfo.InvariantCulture, $"_ = Write{arity};");
        code.AppendLine("}");
        code.AppendLine("}");
        return code.ToString();
    }

    private static void EmitCallback(StringBuilder code, int arity)
    {
        var tuple = $"({string.Join(", ", Enumerable.Repeat("string", arity))})";
        code.AppendLine(CultureInfo.InvariantCulture,
            $"internal static readonly global::System.Buffers.SpanAction<char, {tuple}> Write{arity} = Write{arity}Core;");
        code.AppendLine(CultureInfo.InvariantCulture,
            $"private static void Write{arity}Core(global::System.Span<char> destination, {tuple} state) {{");
        for (var index = 1; index <= arity; index++)
        {
            var item = $"state.Item{index}";
            code.AppendLine(CultureInfo.InvariantCulture,
                $"global::System.MemoryExtensions.AsSpan({item}).CopyTo(destination);");
            if (index != arity)
                code.AppendLine(CultureInfo.InvariantCulture, $"destination = destination.Slice({item}.Length);");
        }
        code.AppendLine("}");
    }
}
