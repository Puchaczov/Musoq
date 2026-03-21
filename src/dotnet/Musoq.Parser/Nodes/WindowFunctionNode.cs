using System;

namespace Musoq.Parser.Nodes;

public class WindowFunctionNode : Node
{
    private Type _returnTypeOverride;

    public WindowFunctionNode(AccessMethodNode functionCall, WindowSpecificationNode windowSpecification)
    {
        FunctionCall = functionCall;
        WindowSpecification = windowSpecification;
        WindowName = null;
        Id = $"{nameof(WindowFunctionNode)}{functionCall.Id}{windowSpecification.Id}";
    }

    public WindowFunctionNode(AccessMethodNode functionCall, string windowName)
    {
        FunctionCall = functionCall;
        WindowSpecification = null;
        WindowName = windowName;
        Id = $"{nameof(WindowFunctionNode)}{functionCall.Id}{windowName}";
    }

    public AccessMethodNode FunctionCall { get; }

    public WindowSpecificationNode WindowSpecification { get; }

    public string WindowName { get; }

    public bool IsNamedWindowReference => WindowName != null;

    public override Type ReturnType => _returnTypeOverride ?? FunctionCall.ReturnType;

    public override string Id { get; }

    public void SetReturnType(Type type)
    {
        _returnTypeOverride = type;
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var overPart = IsNamedWindowReference
            ? $"over {WindowName}"
            : $"over {WindowSpecification}";
        return $"{FunctionCall} {overPart}";
    }
}
