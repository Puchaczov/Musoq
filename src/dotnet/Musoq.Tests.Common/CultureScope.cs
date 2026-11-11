using System;
using System.Globalization;

namespace Musoq.Tests.Common;

/// <summary>Owns the current execution context's culture without changing process defaults.</summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _originalCulture;
    private readonly CultureInfo _originalUiCulture;
    private bool _disposed;

    public CultureScope(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUiCulture;
        _disposed = true;
    }
}
