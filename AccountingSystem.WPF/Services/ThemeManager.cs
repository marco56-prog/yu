using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// الثيمات المتاحة في التطبيق
    /// </summary>
    public enum AppTheme
    {
        Light,
        Dark
    }

    /// <summary>
    /// مدير الثيمات - يسمح بتبديل الثيمات أثناء التشغيل بطريقة آمنة
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public event EventHandler<AppTheme>? ThemeChanged;

        private AppTheme _currentTheme = AppTheme.Light;

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ThemeChanged?.Invoke(this, value);
                }
            }
        }

        private ThemeManager() { }

        /// <summary>
        /// تطبيق ثيم جديد
        /// </summary>
        /// <param name="theme">نوع الثيم</param>
        public void SetTheme(AppTheme theme)
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null) return;

                var dicts = app.Resources.MergedDictionaries;
                
                // إزالة أي قاموس ثيم سابق
                var oldTheme = dicts.FirstOrDefault(d => 
                    d.Source != null && 
                    d.Source.OriginalString.Contains("/Themes/Theme."));
                
                if (oldTheme != null)
                {
                    dicts.Remove(oldTheme);
                }

                // إضافة الثيم الجديد
                var themeName = theme.ToString();
                var uri = new Uri($"pack://application:,,,/Resources/Themes/Theme.{themeName}.xaml", UriKind.Absolute);
                var newTheme = new ResourceDictionary { Source = uri };
                
                dicts.Add(newTheme);
                
                CurrentTheme = theme;

                // حفظ إعدادات المستخدم
                try
                {
                    AccountingSystem.WPF.Properties.Settings.Default.AppTheme = theme.ToString();
                    AccountingSystem.WPF.Properties.Settings.Default.Save();
                }
                catch
                {
                    // تجاهل أخطاء الحفظ
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"فشل في تطبيق الثيم: {ex.Message}");
                MessageBox.Show($"خطأ في تطبيق الثيم: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// تبديل الثيم بين Light و Dark
        /// </summary>
        public void ToggleTheme()
        {
            var newTheme = _currentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            SetTheme(newTheme);
        }

        /// <summary>
        /// تحميل الثيم المحفوظ من الإعدادات
        /// </summary>
        public void LoadSavedTheme()
        {
            try
            {
                var savedTheme = AccountingSystem.WPF.Properties.Settings.Default.AppTheme;
                if (Enum.TryParse<AppTheme>(savedTheme, out var theme))
                {
                    SetTheme(theme);
                }
                else
                {
                    SetTheme(AppTheme.Light); // الثيم الافتراضي
                }
            }
            catch
            {
                SetTheme(AppTheme.Light); // الثيم الافتراضي في حالة الخطأ
            }
        }

        /// <summary>
        /// تهيئة الثيم الافتراضي
        /// </summary>
        public void Initialize()
        {
            LoadSavedTheme();
        }

        /// <summary>
        /// الحصول على اسم الثيم للعرض
        /// </summary>
        /// <param name="theme">نوع الثيم</param>
        /// <returns>اسم الثيم بالعربية</returns>
        public string GetThemeDisplayName(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Light => "الثيم الفاتح",
                AppTheme.Dark => "الثيم الداكن",
                _ => "غير معروف"
            };
        }

        /// <summary>
        /// الحصول على لون من الثيم الحالي
        /// </summary>
        /// <param name="colorKey">مفتاح اللون</param>
        /// <returns>اللون أو null إذا لم يوجد</returns>
        public Color? GetThemeColor(string colorKey)
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null) return null;

                if (app.Resources[colorKey] is Color color)
                {
                    return color;
                }
            }
            catch
            {
                // تجاهل الأخطاء
            }
            return null;
        }

        /// <summary>
        /// الحصول على فرشاة من الثيم الحالي
        /// </summary>
        /// <param name="brushKey">مفتاح الفرشاة</param>
        /// <returns>الفرشاة أو null إذا لم توجد</returns>
        public Brush? GetThemeBrush(string brushKey)
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null) return null;

                if (app.Resources[brushKey] is Brush brush)
                {
                    return brush;
                }
            }
            catch
            {
                // تجاهل الأخطاء
            }
            return null;
        }
    }
}