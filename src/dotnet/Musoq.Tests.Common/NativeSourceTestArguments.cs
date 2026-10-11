using Musoq.Schema;

namespace Musoq.Tests.Common;

/// <summary>Adapts historical fixture callbacks exclusively inside the test harness.</summary>
public static class NativeSourceTestArguments
{
    public static object?[] Read<TArguments>(TArguments arguments)
        where TArguments : struct, ISourceArguments
    {
        var visitor = new FixtureVisitor(new object?[arguments.Count]);
        arguments.Visit(ref visitor);
        return visitor.Values;
    }

    private readonly struct FixtureVisitor(object?[] values) : IColumnValueVisitor
    {
        public object?[] Values => values;
        public void Visit<T>(int columnIndex, in T value) => values[columnIndex] = value;
    }
}
