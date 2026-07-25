using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly Stack<DescForType> _descTypes = new();

    internal void EnterDesc(DescForType type)
    {
        _descTypes.Push(type);
    }

    internal void ExitDesc()
    {
        _descTypes.Pop();
    }

    private bool IsDescribingSourceRuntimeSettings =>
        _descTypes.Count > 0 && _descTypes.Peek() == DescForType.Settings;

    private bool IsDescribingConstructors =>
        _descTypes.Count > 0 && _descTypes.Peek() == DescForType.Constructors;
}
