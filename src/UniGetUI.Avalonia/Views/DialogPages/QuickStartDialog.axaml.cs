using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageLoader;
using CoreSettings = UniGetUI.Core.SettingsEngine.Settings;

namespace UniGetUI.Avalonia.Views.DialogPages;

public partial class QuickStartDialog : ImmersiveDialog
{
    private readonly List<(ToggleSwitch Toggle, IPackageManager Manager)> _managerToggles = [];
    private readonly bool _isFirstRun;
    private bool _isScanning;

    public QuickStartDialog(bool isFirstRun = false)
    {
        _isFirstRun = isFirstRun;
        InitializeComponent();

        bool advancedMode = _isFirstRun
            ? string.Equals(
                CoreSettings.GetValue(CoreSettings.K.SettingsExperienceMode),
                "Advanced",
                StringComparison.OrdinalIgnoreCase)
            : !string.Equals(
                CoreSettings.GetValue(CoreSettings.K.SettingsExperienceMode),
                "Beginner",
                StringComparison.OrdinalIgnoreCase);
        BeginnerModeButton.IsChecked = !advancedMode;
        AdvancedModeButton.IsChecked = advancedMode;

        BuildManagerChoices();
    }

    private void BuildManagerChoices()
    {
        foreach (IPackageManager manager in PEInterface.Managers)
        {
            string status = manager.IsReady()
                ? CoreTools.Translate("Ready")
                : manager.Status.Found
                    ? CoreTools.Translate("Not ready")
                    : CoreTools.Translate("Not found");
            string description = manager.Properties.Description.Split("<br>")[0];

            var label = new StackPanel
            {
                Spacing = 2,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = manager.DisplayName,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    },
                    new TextBlock
                    {
                        Text = $"{status} · {description}",
                        FontSize = 12,
                        Opacity = 0.72,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    },
                },
            };

            var toggle = new ToggleSwitch
            {
                OnContent = "",
                OffContent = "",
                IsChecked = manager.IsEnabled(),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            AutomationProperties.SetName(toggle, $"{manager.DisplayName}, {status}");

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 16,
                MinHeight = 52,
                Children =
                {
                    label,
                    toggle,
                },
            };
            Grid.SetColumn(toggle, 1);

            ManagerPanel.Children.Add(new Border
            {
                Padding = new Avalonia.Thickness(8, 5),
                Child = row,
            });
            _managerToggles.Add((toggle, manager));
        }

        NoManagersText.IsVisible = _managerToggles.Count == 0;
    }

    private async void ScanButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isScanning)
            return;

        _isScanning = true;
        ScanButton.IsEnabled = false;
        foreach (var (toggle, _) in _managerToggles)
            toggle.IsEnabled = false;

        ScanProgress.IsVisible = true;
        ScanResultText.IsVisible = true;
        ScanResultText.Text = CoreTools.Translate("Scanning installed packages and checking for updates...");

        try
        {
            await SaveChoicesAsync();

            InstalledPackagesLoader installedLoader = InstalledPackagesLoader.Instance;
            await installedLoader.ReloadPackagesListingOnly();

            UpgradablePackagesLoader? updatesLoader = UpgradablePackagesLoader.Instance;
            if (updatesLoader is not null)
                await updatesLoader.ReloadPackagesListingOnly();

            ScanResultText.Text = string.Format(
                CoreTools.Translate("Found {0} installed packages and {1} available updates."),
                installedLoader.Count(),
                updatesLoader?.Count() ?? 0);

            if (
                installedLoader.LastLoadReportedFailures
                || updatesLoader?.LastLoadReportedFailures == true
            )
            {
                ScanResultText.Text += " " + CoreTools.Translate(
                    "Some package managers could not complete the scan. You can check their status in Settings.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Quick Start package scan failed.");
            Logger.Error(ex);
            ScanResultText.Text = CoreTools.Translate(
                "The scan could not finish. You can still use UniGetUI and try again from the package pages.");
        }
        finally
        {
            ScanProgress.IsVisible = false;
            ScanButton.IsVisible = false;
            FinishButton.IsVisible = true;
            FinishButton.IsEnabled = true;
            _isScanning = false;
        }
    }

    private async Task SaveChoicesAsync()
    {
        bool advancedMode = AdvancedModeButton.IsChecked == true;
        CoreSettings.SetValue(
            CoreSettings.K.SettingsExperienceMode,
            advancedMode ? "Advanced" : "Beginner");

        List<IPackageManager> changedManagers = [];
        foreach (var (toggle, manager) in _managerToggles)
        {
            bool enabled = toggle.IsChecked == true;
            if (manager.IsEnabled() != enabled)
            {
                changedManagers.Add(manager);
                CoreSettings.SetDictionaryItem(
                    CoreSettings.K.DisabledManagers,
                    manager.Name,
                    !enabled);
            }
        }

        await Task.WhenAll(changedManagers.Select(manager => Task.Run(manager.Initialize)));
    }

    private void SkipButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void FinishButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        CoreSettings.Set(CoreSettings.K.QuickStartCompleted, true);
        base.OnClosed(e);
    }
}
