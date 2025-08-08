using System;

namespace Musoq.Parser.Nodes;

public class WindowFunctionNode : Node
{
    public string FunctionName { get; }
    public ArgsListNode Arguments { get; }
    public WindowSpecificationNode WindowSpecification { get; }

    public WindowFunctionNode(string functionName, ArgsListNode arguments, WindowSpecificationNode windowSpecification)
    {
        FunctionName = functionName;
        Arguments = arguments;
        WindowSpecification = windowSpecification;
        Id = $"{nameof(WindowFunctionNode)}{GetHashCode()}";
    }

    public override Type ReturnType => typeof(object);

    public override string Id { get; }

    public override string ToString()
    {
        var args = Arguments?.ToString() ?? "";
        var over = WindowSpecification != null ? $" OVER ({WindowSpecification})" : "";
        return $"{FunctionName}({args}){over}";
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}