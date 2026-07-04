using System.Collections.Generic;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    public QueryResultMode QueryResultMode
    {
        get => GetValueOrDefault(BuildItemKeys.QueryResultMode, QueryResultMode.Table);
        set => SetRequired(BuildItemKeys.QueryResultMode, value);
    }

    public QueryMethodRenderMetadata QueryMethodRenderMetadata
    {
        get => GetValueOrDefault(BuildItemKeys.QueryMethodRenderMetadata, QueryMethodRenderMetadata.Unknown);
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
