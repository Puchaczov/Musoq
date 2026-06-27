using System.Collections.Generic;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    public QueryResultMode QueryResultMode
    {
        get => TryGetValue(BuildItemKeys.QueryResultMode, out var value) && value is QueryResultMode mode
            ? mode
            : QueryResultMode.Table;
        set => SetRequired(BuildItemKeys.QueryResultMode, value);
    }

    public QueryMethodRenderMetadata QueryMethodRenderMetadata
    {
        get => TryGetValue(BuildItemKeys.QueryMethodRenderMetadata, out var value) && value is QueryMethodRenderMetadata metadata
            ? metadata
            : QueryMethodRenderMetadata.Unknown;
        set => SetRequired(BuildItemKeys.QueryMethodRenderMetadata, value);
    }

    public Type? OutputType
    {
        get => GetOptional<Type>(BuildItemKeys.OutputType);
        set => SetOptional(BuildItemKeys.OutputType, value);
    }

    public IReadOnlyList<Type> AdditionalReferenceTypes
    {
        get => GetListOrEmpty<Type>(BuildItemKeys.AdditionalReferenceTypes);
        set => SetRequired(BuildItemKeys.AdditionalReferenceTypes, value);
    }
}
