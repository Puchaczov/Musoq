namespace Musoq.Parser.Nodes;

public class WindowFunctionNode : Node
{
    private Type? _returnTypeOverride;

    public WindowFunctionNode(AccessMethodNode functionCall, WindowSpecificationNode windowSpecification)
    {
        ArgumentNullException.ThrowIfNull(functionCall);
        ArgumentNullException.ThrowIfNull(windowSpecification);
        FunctionCall = functionCall;
        WindowSpecification = windowSpecification;
        WindowName = null;
        Id = $"{nameof(WindowFunctionNode)}{functionCall.Id}{windowSpecification.Id}";
    }

    public WindowFunctionNode(AccessMethodNode functionCall, string windowName)
    {
        ArgumentNullException.ThrowIfNull(functionCall);
        FunctionCall = functionCall;
        WindowSpecification = null;
        WindowName = windowName;
        Id = $"{nameof(WindowFunctionNode)}{functionCall.Id}{windowName}";
    }

    public AccessMethodNode FunctionCall { get; }

    public WindowSpecificationNode? WindowSpecification { get; }

    public string? WindowName { get; }

    public bool IsNamedWindowReference => WindowName != null;

    public override Type? ReturnType => _returnTypeOverride ?? FunctionCall.ReturnType;

    public override string Id { get; }

    public void SetReturnType(Type type)
    {
        _returnTypeOverride = type;
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        // Node hides object.ToString(), so interpolation of child nodes would
        // emit CLR type names instead of SQL without these explicit calls.
        var overPart = IsNamedWindowReference
            ? $"over {WindowName}"
            : $"over {WindowSpecification!.ToString()}";
        return $"{FunctionCall.ToString()} {overPart}";
    }
}
