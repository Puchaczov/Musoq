namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private string GetScriptParameterLocalName(string name)
    {
        if (_scriptParameterLocalNames.TryGetValue(name, out var localName))
            return localName;

        throw new InvalidOperationException(
            $"Script parameter '{name}' is not declared in render context.");
    }
}
