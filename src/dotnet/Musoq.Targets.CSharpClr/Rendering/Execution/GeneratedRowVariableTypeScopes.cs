using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IDisposable EnterGeneratedRowVariableType(
        ExecutionRenderContext context,
        string variableName,
        string generatedRowTypeName)
    {
        var previous = context.Session.GeneratedRowVariableTypeNamesByName;
        var current = previous.ToDictionary(
            static pair => pair.Key,
            static pair => new HashSet<string>(pair.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);

        if (!current.TryGetValue(variableName, out var typeNames))
        {
            typeNames = new HashSet<string>(StringComparer.Ordinal);
            current.Add(variableName, typeNames);
        }

        typeNames.Add(generatedRowTypeName);
        context.Session.GeneratedRowVariableTypeNamesByName = current;
        return new GeneratedRowVariableTypeScope(context.Session, previous);
    }

    private sealed class GeneratedRowVariableTypeScope(
        ExecutionRenderSession session,
        IReadOnlyDictionary<string, HashSet<string>> previous) : IDisposable
    {
        public void Dispose()
        {
            session.GeneratedRowVariableTypeNamesByName = previous;
        }
    }
}
