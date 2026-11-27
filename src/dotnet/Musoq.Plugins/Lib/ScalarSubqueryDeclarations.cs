using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
#pragma warning disable CS1591
    [AggregateFunction(
        typeof(ScalarSubqueryAggregateKernel<>),
        Name = ScalarSubqueryAggregateName,
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        Visibility = FunctionVisibility.CompilerOnly
    )]
    public T __ScalarSubqueryValue<T>(T value, [AggregateParent] int parent = 0)
        where T : class => AggregateDeclaration<T>();

    [AggregateFunction(
        typeof(NullableScalarSubqueryAggregateKernel<>),
        Name = ScalarSubqueryAggregateName,
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        Visibility = FunctionVisibility.CompilerOnly
    )]
    public T? __ScalarSubqueryValue<T>(T? value, [AggregateParent] int parent = 0)
        where T : struct => AggregateDeclaration<T?>();

    [AggregateFunction(
        typeof(CorrelatedScalarSubqueryAggregateKernel<>),
        Name = CorrelatedScalarSubqueryAggregateName,
        Inline = false,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom,
        Visibility = FunctionVisibility.CompilerOnly
    )]
    public CorrelatedScalarSubqueryResult<T> __CorrelatedScalarSubqueryValue<T>(
        T value,
        [AggregateParent] int parent = 0
    )
        where T : class => AggregateDeclaration<CorrelatedScalarSubqueryResult<T>>();

    [AggregateFunction(
        typeof(NullableCorrelatedScalarSubqueryAggregateKernel<>),
        Name = CorrelatedScalarSubqueryAggregateName,
        Inline = false,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom,
        Visibility = FunctionVisibility.CompilerOnly
    )]
    public CorrelatedScalarSubqueryResult<T?> __CorrelatedScalarSubqueryValue<T>(
        T? value,
        [AggregateParent] int parent = 0
    )
        where T : struct => AggregateDeclaration<CorrelatedScalarSubqueryResult<T?>>();

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public T __CorrelatedScalarSubqueryResult<T>(CorrelatedScalarSubqueryResult<T> result) =>
        ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public T __CorrelatedScalarSubqueryResult<T>(CorrelatedScalarSubqueryResult<T>? result) =>
        ExtractCorrelatedScalarSubqueryResult(result);
#pragma warning restore CS1591
}
