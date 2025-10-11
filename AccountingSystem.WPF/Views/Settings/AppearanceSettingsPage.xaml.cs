using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AccountingSystem.WPF.Services;

namespace AccountingSystem.WPF.Views.Settings
{
    public partial class AppearanceSettingsPage : Page
    {
        private readonly IThemeService _themeService;

        public AppearanceSettingsPage(IThemeService themeService)
        {
            InitializeComponent();
            _themeService = themeService;
            InitializeComboBoxes();
            LoadCurrentSettings();
        }

        private void InitializeComboBoxes()
        {
            // تعبئة قائمة الثيمات
            cmbTheme.ItemsSource = new[]
            {
                new { Value = ThemeKind.Win11, Display = "وندوز 11 (حديث)" },
                new { Value = ThemeKind.Modern, Display = "حديث (عصري)" },
                new { Value = ThemeKind.Classic, Display = "كلاسيكي" },
                new { Value = ThemeKind.Win7, Display = "وندوز 7" }
            };
            cmbTheme.DisplayMemberPath = "Display";
            cmbTheme.SelectedValuePath = "Value";

            // تعبئة قائمة الألوان
            cmbAccent.ItemsSource = new[]
            {
                new { Value = AccentKind.Blue, Display = "أزرق" },
                new { Value = AccentKind.Green, Display = "أخضر" },
                new { Value = AccentKind.Teal, Display = "أزرق مخضر" },
                new { Value = AccentKind.Purple, Display = "بنفسجي" },
                new { Value = AccentKind.Amber, Display = "عنبري/برتقالي" },
                new { Value = AccentKind.Red, Display = "أحمر" }
            };
            cmbAccent.DisplayMemberPath = "Display";
            cmbAccent.SelectedValuePath = "Value";

            // تعبئة قائمة أحجام الخطوط
            cmbFont.ItemsSource = new[]
            {
                new { Value = FontScaleKind.Scale90, Display = "صغير (90%)" },
                new { Value = FontScaleKind.Scale100, Display = "عادي (100%)" },
                new { Value = FontScaleKind.Scale110, Display = "كبير (110%)" },
                new { Value = FontScaleKind.Scale125, Display = "كبير جداً (125%)" }
            };
            cmbFont.DisplayMemberPath = "Display";
            cmbFont.SelectedValuePath = "Value";
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // تحميل الإعدادات الحالية
                cmbTheme.SelectedValue = _themeService.Current.Theme;
                cmbAccent.SelectedValue = _themeService.Current.Accent;
                cmbFont.SelectedValue = _themeService.Current.FontScale;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الإعدادات الحالية: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbTheme.SelectedValue == null || cmbAccent.SelectedValue == null || cmbFont.SelectedValue == null)
                {
                    MessageBox.Show("يرجى اختيار جميع الإعدادات", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var options = new ThemeOptions
                {
                    Theme = (ThemeKind)cmbTheme.SelectedValue,
                    Accent = (AccentKind)cmbAccent.SelectedValue,
                    FontScale = (FontScaleKind)cmbFont.SelectedValue
                };

                _themeService.ApplyAll(options);

                MessageBox.Show("تم تطبيق إعدادات المظهر بنجاح!", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تطبيق الإعدادات: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "هل تريد استعادة إعدادات المظهر الافتراضية؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var defaultOptions = new ThemeOptions
                    {
                        Theme = ThemeKind.Win11,
                        Accent = AccentKind.Blue,
                        FontScale = FontScaleKind.Scale100
                    };

                    _themeService.ApplyAll(defaultOptions);
                    LoadCurrentSettings(); // إعادة تحميل واجهة الإعدادات

                    MessageBox.Show("تم استعادة الإعدادات الافتراضية!", "نجح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في استعادة الإعدادات: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}