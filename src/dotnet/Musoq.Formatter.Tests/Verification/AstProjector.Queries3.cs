namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Nodes.WindowDefinitionNode value)
    {
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Specification));
        Write(value.Specification);
    }

    private void Project(global::Musoq.Parser.Nodes.WindowFrameBoundNode value)
    {
        Member(nameof(value.BoundType));
        Write(value.BoundType);
        Member(nameof(value.Offset));
        Write(value.Offset);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.WindowFrameNode value)
    {
        Member(nameof(value.End));
        Write(value.End);
        Member(nameof(value.FrameType));
        Write(value.FrameType);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Start));
        Write(value.Start);
    }

    private void Project(global::Musoq.Parser.Nodes.WindowFunctionNode value)
    {
        Member(nameof(value.FunctionCall));
        Write(value.FunctionCall);
        Member(nameof(value.IsNamedWindowReference));
        Write(value.IsNamedWindowReference);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.WindowName));
        Write(value.WindowName);
        Member(nameof(value.WindowSpecification));
        Write(value.WindowSpecification);
    }

    private void Project(global::Musoq.Parser.Nodes.WindowNode value)
    {
        Member(nameof(value.Definitions));
        Sequence(value.Definitions, static (writer, item) => writer.Write(item));
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.WindowSpecificationNode value)
    {
        Member(nameof(value.Frame));
        Write(value.Frame);
        Member(nameof(value.OrderByFields));
        Sequence(value.OrderByFields, static (writer, item) => writer.Write(item));
        Member(nameof(value.PartitionFields));
        Sequence(value.PartitionFields, static (writer, item) => writer.Write(item));
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.CteColumnName value)
    {
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Nodes.From.PropertyFromNode.PropertyNameAndTypePair value)
    {
        Member(nameof(value.IntendedTypeName));
        Write(value.IntendedTypeName);
        Member(nameof(value.PropertyName));
        Write(value.PropertyName);
        Member(nameof(value.PropertyType));
        Write(value.PropertyType);
    }
}
