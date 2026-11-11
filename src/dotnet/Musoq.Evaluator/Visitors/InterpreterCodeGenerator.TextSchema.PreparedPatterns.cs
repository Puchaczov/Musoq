using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private const string PreparedPatternType = "global::Musoq.Schema.Interpreters.PreparedTextPattern";
    private readonly Dictionary<TextFieldDefinitionNode, PreparedPatternMember> _textPatterns = new(
        ReferenceEqualityComparer.Instance
    );
    private readonly Dictionary<string, string> _textLookaheads = new(StringComparer.Ordinal);

    private void AppendPreparedPatternMembers(
        StringBuilder builder,
        IReadOnlyList<BoundTextField> fields,
        string className
    )
    {
        _textPatterns.Clear();
        _textLookaheads.Clear();
        var names = new HashSet<string>(fields.Select(static field => field.Name), StringComparer.Ordinal)
        {
            className,
        };
        names.UnionWith(fields.Select(static field => field.LocalVariableName));
        var index = 0;
        string AllocateMemberName()
        {
            var name = "__musoqPattern" + index++.ToString(CultureInfo.InvariantCulture);
            while (!names.Add(name))
                name += "_";
            return name;
        }

        foreach (var field in fields)
        {
            if (field.PreparedPattern is { } pattern && !_textPatterns.ContainsKey(field.Source))
            {
                var name = AllocateMemberName();
                _textPatterns.Add(field.Source, new PreparedPatternMember(name, pattern));
                AppendPatternMember(builder, name, pattern);
            }

            foreach (var arm in field.SwitchArms)
            {
                if (arm.Pattern is not { } lookahead || _textLookaheads.ContainsKey(lookahead))
                    continue;
                var name = AllocateMemberName();
                _textLookaheads.Add(lookahead, name);
                var escaped = InterpretationStringLiterals.EscapeCSharpRegexString(lookahead);
                AppendGeneratedLine(
                    builder,
                    $"    private static readonly {PreparedPatternType} {name} = {PreparedPatternType}.CreateLookahead(@\"{escaped}\");"
                );
            }
        }
    }

    private static void AppendPatternMember(StringBuilder builder, string name, BoundTextPattern pattern)
    {
        var text = InterpretationStringLiterals.EscapeCSharpRegexString(pattern.Pattern);
        var groups =
            pattern.CaptureGroups.Count == 0
                ? string.Empty
                : ", ["
                    + string.Join(
                        ", ",
                        pattern.CaptureGroups.Select(static group =>
                            $"\"{InterpretationStringLiterals.EscapeCSharpString(group)}\""
                        )
                    )
                    + "]";
        AppendGeneratedLine(
            builder,
            string.Create(
                CultureInfo.InvariantCulture,
                $"    private static readonly {PreparedPatternType} {name} = new(@\"{text}\", (global::Musoq.Schema.Interpreters.TextPatternModifiers){(int)pattern.Modifiers}{groups});"
            )
        );
    }

    private sealed record PreparedPatternMember(string Name, BoundTextPattern Pattern);
}
