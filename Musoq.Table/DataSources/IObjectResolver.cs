namespace Musoq.Schema.DataSources
{
    public interface IObjectResolver
    {
        object Context { get; }

        object this[string name] { get; }

        object this[int index] { get; }

        bool HasColumn(string name);
    }
}