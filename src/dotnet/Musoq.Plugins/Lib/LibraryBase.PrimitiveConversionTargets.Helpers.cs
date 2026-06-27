namespace Musoq.Plugins;

public partial class LibraryBase
{
    private static bool IsNullConversionInput(object? value)
    {
        return value is null or DBNull;
    }
}
