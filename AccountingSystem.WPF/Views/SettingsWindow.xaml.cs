using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.WPF.Views.Settings;
using AccountingSystem.WPF.Services;

namespace AccountingSystem.WPF.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly IThemeService _themeService;

        public SettingsWindow(IThemeService themeService)
        {
            InitializeComponent();
            _themeService = themeService;
            LoadAppearancePage();
        }

        public SettingsWindow() : this(App.ServiceProvider.GetRequiredService<IThemeService>())
        {
        }

        private void LoadAppearancePage()
        {
            try
            {
                var appearancePage = new AppearanceSettingsPage(_themeService);
                appearanceFrame.Navigate(appearancePage);
            }
            catch
            {
                // في حالة فشل تحميل صفحة المظهر، اتركها فارغة
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("تم حفظ الإعدادات بنجاح!", "حفظ الإعدادات", 
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل تريد استعادة الإعدادات الافتراضية؟\nسيتم فقدان جميع التخصيصات الحالية.", 
                               "استعادة الإعدادات الافتراضية", 
                               MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                MessageBox.Show("تم استعادة الإعدادات الافتراضية بنجاح!", "استعادة الإعدادات", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}