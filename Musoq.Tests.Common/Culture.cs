using System.Globalization;

namespace Musoq.Tests.Common
{
    public static class Culture
    {
        public static CultureInfo DefaultCulture { get; } = CultureInfo.GetCultureInfo("pl-PL");

        public static void ApplyWithDefaultCulture() => Apply(DefaultCulture);

        public static void Apply(CultureInfo culture)
        {
            CultureInfo.CurrentCulture
                = CultureInfo.CurrentUICulture =
                    CultureInfo.DefaultThreadCurrentCulture =
                        CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}
