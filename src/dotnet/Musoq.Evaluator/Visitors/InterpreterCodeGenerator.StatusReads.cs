using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string _statusResultType = string.Empty;
    private int _statusReadIndex;
    private string? _statusFailureLabel;
    private readonly HashSet<string> _statusNames = new(System.StringComparer.Ordinal);

    private void BeginStatusMethod(string type)
    {
        _statusResultType = type;
        _statusReadIndex = 0;
        _statusFailureLabel = null;
        _statusNames.Clear();
        foreach (var field in _expressionFields.Where(static field => field.Name != "_"))
            _statusNames.Add(GetLocalVarName(field.Name));
    }

    private string NextStatusName(string prefix)
    {
        string name;
        do { name = prefix + _statusReadIndex++; }
        while (!_statusNames.Add(name));
        return name;
    }

    private string StatusFailure(string error) => _statusFailureLabel is { } label
        ? $"goto {label};"
        : $"return global::Musoq.Schema.Interpreters.ParseAttempt<{_statusResultType}>.Failed(__parsedValue, {error});";

    private string StatusCheck(string expression)
    {
        var error = NextStatusName("__musoqReadFailure");
        return $"if ({expression} is {{ }} {error}) {{ {StatusFailure(error)} }}\n";
    }

    private string StatusRead(string variable, string expression)
    {
        var result = NextStatusName("__musoqReadResult");
        return $"var {result} = {expression};\n" +
               $"if (!{result}.Succeeded) {{ {StatusFailure(result + ".Failure!")} }}\n" +
               $"var {variable} = {result}.Value!;\n";
    }

    private string TextStatusRead(string variable, string expression, bool discard)
    {
        if (!discard)
            return StatusRead(variable, expression);
        var result = NextStatusName("__musoqReadResult");
        return $"var {result} = {expression};\n" +
               $"if (!{result}.Succeeded) {{ {StatusFailure(result + ".Failure!")} }}\n";
    }
}
