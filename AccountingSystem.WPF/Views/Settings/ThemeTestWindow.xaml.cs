using System;
using System.Windows;
using System.Windows.Controls;
using AccountingSystem.Business;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.WPF.Services;

namespace AccountingSystem.WPF.Views.Settings;

public partial class ThemeTestWindow : Window
{
    private readonly IThemeService _themeService;

    public ThemeTestWindow()
    {
        InitializeComponent();

        // الحصول على خدمة الثيمات من DI container
        _themeService = App.ServiceProvider.GetRequiredService<IThemeService>();
    }

    private void ChangeTheme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string themeStr)
            return;

        if (Enum.TryParse<ThemeKind>(themeStr, out var theme))
            _themeService.ApplyTheme(theme);
    }

    private void ChangeAccent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string accentStr)
            return;

        if (Enum.TryParse<AccentKind>(accentStr, out var accent))
            _themeService.ApplyAccent(accent);
    }

    private void ChangeScale_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string scaleStr)
            return;

        if (Enum.TryParse<FontScaleKind>(scaleStr, out var scale))
            _themeService.ApplyFontScale(scale);
    }
}