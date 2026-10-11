namespace Musoq.Plugins;

internal sealed class ScalarSubqueryCardinalityException()
    : InvalidOperationException("Scalar subquery returned more than one row.");
