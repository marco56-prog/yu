using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Business;

namespace AccountingSystem.WPF.Services
{
    public enum ThemeKind { Modern, Classic, Win7, Win11 }
    public enum AccentKind { Blue, Green, Amber, Red, Purple, Teal }
    public enum FontScaleKind { Scale90, Scale100, Scale110, Scale125 }

    public sealed class ThemeOptions
    {
        public ThemeKind Theme { get; set; } = ThemeKind.Win11;
        public AccentKind Accent { get; set; } = AccentKind.Blue;
        public FontScaleKind FontScale { get; set; } = FontScaleKind.Scale100;
    }

    public interface IThemeService
    {
        ThemeOptions Current { get; }
        void ApplyAll(ThemeOptions options, bool save = true);
        void ApplyTheme(ThemeKind theme, bool save = true);
        void ApplyAccent(AccentKind accent, bool save = true);
        void ApplyFontScale(FontScaleKind scale, bool save = true);
        void LoadSavedTheme();
    }

    public sealed class ThemeService : IThemeService
    {
        private readonly Application _app;
        private readonly ISystemSettingsService _settingsService;
        private ResourceDictionary? _themeDict, _accentDict, _fontDict;
        public ThemeOptions Current { get; private set; } = new();

        public ThemeService(ISystemSettingsService settingsService)
        {
            _app = Application.Current ?? throw new InvalidOperationException("Application.Current is null");
            _settingsService = settingsService;
            Current = new ThemeOptions();
        }

        public void LoadSavedTheme()
        {
            try
            {
                Task.Run(async () =>
                {
                    // تحميل الإعدادات المحفوظة
                    var savedTheme = await _settingsService.GetSettingValueAsync("ThemeKind") ?? "Win11";
                    var savedAccent = await _settingsService.GetSettingValueAsync("AccentKind") ?? "Blue";
                    var savedFontScale = await _settingsService.GetSettingValueAsync("FontScaleKind") ?? "Scale100";

                    var options = new ThemeOptions
                    {
                        Theme = Enum.TryParse<ThemeKind>(savedTheme, out var theme) ? theme : ThemeKind.Win11,
                        Accent = Enum.TryParse<AccentKind>(savedAccent, out var accent) ? accent : AccentKind.Blue,
                        FontScale = Enum.TryParse<FontScaleKind>(savedFontScale, out var scale) ? scale : FontScaleKind.Scale100
                    };

                    Application.Current.Dispatcher.Invoke(() => ApplyAll(options, save: false));
                });
            }
            catch
            {
                // في حالة حدوث خطأ، استخدم الإعدادات الافتراضية
                ApplyAll(new ThemeOptions(), save: false);
            }
        }

        private void SaveSettings(ThemeOptions options)
        {
            try
            {
                // تشغيل العمليات بشكل متتالي بدلاً من متزامن لتجنب تضارب Entity tracking
                Task.Run(async () =>
                {
                    try
                    {
                        // حفظ الإعدادات بشكل متتالي
                        await _settingsService.SetSettingAsync("ThemeKind", options.Theme.ToString());
                        await Task.Delay(50); // تأخير صغير لضمان عدم التداخل

                        await _settingsService.SetSettingAsync("AccentKind", options.Accent.ToString());
                        await Task.Delay(50);

                        await _settingsService.SetSettingAsync("FontScaleKind", options.FontScale.ToString());
                    }
                    catch (Exception ex)
                    {
                        // تسجيل الخطأ للتشخيص
                        System.Diagnostics.Debug.WriteLine($"خطأ في حفظ إعدادات الثيم: {ex.Message}");
                    }
                });
            }
            catch
            {
                // تجاهل أخطاء الحفظ الخارجية
            }
        }

        public void ApplyAll(ThemeOptions options, bool save = true)
        {
            ApplyTheme(options.Theme, save: false);
            ApplyAccent(options.Accent, save: false);
            ApplyFontScale(options.FontScale, save: false);
            Current = options;
            if (save) SaveSettings(Current);
        }

        public void ApplyTheme(ThemeKind theme, bool save = true)
        {
            var uri = new Uri($"pack://application:,,,/Themes/Themes/{theme}.xaml");
            _themeDict = ReplaceDictionary(_themeDict, uri);
            Current.Theme = theme;
            if (save) SaveSettings(Current);
        }

        public void ApplyAccent(AccentKind accent, bool save = true)
        {
            var uri = new Uri($"pack://application:,,,/Themes/Accents/{accent}.xaml");
            _accentDict = ReplaceDictionary(_accentDict, uri);
            Current.Accent = accent;
            if (save) SaveSettings(Current);
        }

        public void ApplyFontScale(FontScaleKind scale, bool save = true)
        {
            var uri = new Uri($"pack://application:,,,/Themes/Fonts/{scale}.xaml");
            _fontDict = ReplaceDictionary(_fontDict, uri);
            Current.FontScale = scale;
            if (save) SaveSettings(Current);
        }

        private ResourceDictionary ReplaceDictionary(ResourceDictionary? oldDict, Uri newSource)
        {
            var md = _app.Resources.MergedDictionaries;
            if (oldDict != null) md.Remove(oldDict);

            try
            {
                var fresh = new ResourceDictionary { Source = newSource };
                md.Add(fresh);
                return fresh;
            }
            catch
            {
                // في حالة فشل تحميل القاموس، لا تفعل شيئاً
                return oldDict ?? new ResourceDictionary();
            }
        }
    }
}