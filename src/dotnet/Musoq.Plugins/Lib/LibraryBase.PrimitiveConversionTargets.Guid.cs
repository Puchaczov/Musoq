using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public Guid? ToGuid(string? value)
    {
        if (Guid.TryParse(value, out var result))
            return result;

        return null;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public Guid? ToGuid(Guid? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public Guid? ToGuid(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        if (value is Guid guid)
            return guid;

        return ToGuid(value!.ToString());
    }
}
