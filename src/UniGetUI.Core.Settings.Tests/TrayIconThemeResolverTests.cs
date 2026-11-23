using UniGetUI.Core.SettingsEngine;

namespace UniGetUI.Core.SettingsEngine.Tests;

public sealed class TrayIconThemeResolverTests
{
    [Theory]
    [InlineData("light", false, true)]
    [InlineData("LIGHT", false, true)]
    [InlineData("dark", true, false)]
    [InlineData("auto", true, true)]
    [InlineData("auto", false, false)]
    [InlineData("unexpected", true, true)]
    [InlineData(null, false, false)]
    public void ShouldUseLightIconHonorsPreferenceAndFallsBackToSystem(
        string? preference,
        bool systemUsesLightTheme,
        bool expected)
    {
        Assert.Equal(
            expected,
            TrayIconThemeResolver.ShouldUseLightIcon(preference, systemUsesLightTheme));
    }
}
