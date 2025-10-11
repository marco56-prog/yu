using System.Windows;
using System.Windows.Controls;
using AccountingSystem.WPF.Services;

namespace AccountingSystem.WPF.UserControls
{
    /// <summary>
    /// عنصر تحكم لتبديل الثيمات
    /// </summary>
    public partial class ThemeToggleControl : UserControl
    {
        public ThemeToggleControl()
        {
            InitializeComponent();

            // ربط الحدث لتغيير الثيم
            ThemeManager.Instance.ThemeChanged += (s, theme) =>
            {
                UpdateButtonsState();
            };

            UpdateButtonsState();
        }

        private void LightThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.SetTheme(AppTheme.Light);
        }

        private void DarkThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.SetTheme(AppTheme.Dark);
        }

        private void ToggleThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ToggleTheme();
        }

        private void UpdateButtonsState()
        {
            // التأكد من أن العناصر موجودة قبل تحديثها
            var lightButton = FindName("LightThemeButton") as Button;
            var darkButton = FindName("DarkThemeButton") as Button;

            if (lightButton == null || darkButton == null) return;

            var currentTheme = ThemeManager.Instance.CurrentTheme;
            lightButton.IsEnabled = currentTheme != AppTheme.Light;
            darkButton.IsEnabled = currentTheme != AppTheme.Dark;
        }
    }
}