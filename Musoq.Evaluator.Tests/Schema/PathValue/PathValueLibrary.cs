using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Schema.PathValue;

public class PathValueLibrary : LibraryBase
{
    [BindableMethod]
    public string GetPath(string path)
    {
        return path;
    }

    [BindableMethod]
    public object GetValue(object value)
    {
        return value;
    }
}
