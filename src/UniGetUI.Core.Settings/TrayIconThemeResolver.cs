namespace UniGetUI.Core.SettingsEngine;

internal static class TrayIconThemeResolver
{
    internal static bool ShouldUseLightIcon(string? preference, bool systemUsesLightTheme)
    {
        if (string.Equals(preference, "light", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(preference, "dark", StringComparison.OrdinalIgnoreCase))
            return false;

        return systemUsesLightTheme;
    }
}
