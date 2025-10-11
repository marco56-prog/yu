using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using AccountingSystem.WPF.Helpers;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة إدارة الثيمات مع دعم Win7/Modern/Dark وتحديث تلقائي بدون إعادة تحميل
    /// </summary>
    public interface IThemeIntegrationService
    {
        ThemeInfo CurrentTheme { get; }
        IReadOnlyList<ThemeInfo> AvailableThemes { get; }
        bool ApplyTheme(string themeName);
        bool ApplyTheme(ThemeInfo theme);
        void RefreshTheme();
        bool SaveThemePreference(string themeName);
        string LoadThemePreference();
        event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    }

    public class ThemeIntegrationService : IThemeIntegrationService, INotifyPropertyChanged
    {
        private const string ComponentName = "ThemeIntegrationService";
        private const string ThemePreferenceKey = "UserPreferredTheme";

        private ThemeInfo _currentTheme;
        private readonly Dictionary<string, ThemeInfo> _themes;

        public ThemeInfo CurrentTheme
        {
            get => _currentTheme;
            private set => SetProperty(ref _currentTheme, value);
        }

        public IReadOnlyList<ThemeInfo> AvailableThemes { get; }

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ThemeIntegrationService()
        {
            _themes = new Dictionary<string, ThemeInfo>();
            InitializeThemes();
            AvailableThemes = _themes.Values.ToList().AsReadOnly();

            // Set default theme
            _currentTheme = _themes["Modern"];

            ComprehensiveLogger.LogUIOperation("تم تهيئة خدمة إدارة الثيمات", ComponentName,
                $"عدد الثيمات المتاحة: {_themes.Count}");
        }

        public bool ApplyTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName) || !_themes.ContainsKey(themeName))
            {
                ComprehensiveLogger.LogError($"ثيم غير موجود: {themeName}", null, ComponentName);
                return false;
            }

            return ApplyTheme(_themes[themeName]);
        }

        public bool ApplyTheme(ThemeInfo theme)
        {
            try
            {
                if (theme == null)
                {
                    ComprehensiveLogger.LogError("معلومات الثيم فارغة", null, ComponentName);
                    return false;
                }

                ComprehensiveLogger.LogUIOperation($"بدء تطبيق الثيم: {theme.Name}", ComponentName);

                var app = Application.Current;
                if (app == null)
                {
                    ComprehensiveLogger.LogError("تطبيق WPF غير متاح", null, ComponentName);
                    return false;
                }

                // Clear existing theme resources
                ClearExistingThemeResources(app);

                // Apply new theme resources
                ApplyThemeResources(app, theme);

                // Update current theme
                var previousTheme = CurrentTheme;
                CurrentTheme = theme;

                // Save preference
                SaveThemePreference(theme.Name);

                // Fire event
                OnThemeChanged(new ThemeChangedEventArgs
                {
                    PreviousTheme = previousTheme,
                    NewTheme = theme,
                    Applied = true
                });

                ComprehensiveLogger.LogUIOperation($"تم تطبيق الثيم بنجاح: {theme.Name}", ComponentName);
                return true;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل تطبيق الثيم: {theme?.Name}", ex, ComponentName);
                return false;
            }
        }

        public void RefreshTheme()
        {
            try
            {
                ApplyTheme(CurrentTheme);
                ComprehensiveLogger.LogUIOperation("تم تحديث الثيم الحالي", ComponentName, CurrentTheme.Name);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تحديث الثيم", ex, ComponentName);
            }
        }

        public bool SaveThemePreference(string themeName)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings.Remove(ThemePreferenceKey);
                config.AppSettings.Settings.Add(ThemePreferenceKey, themeName);
                config.Save(ConfigurationSaveMode.Modified);

                ComprehensiveLogger.LogUIOperation("تم حفظ تفضيل الثيم", ComponentName, themeName);
                return true;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل حفظ تفضيل الثيم", ex, ComponentName);
                return false;
            }
        }

        public string LoadThemePreference()
        {
            try
            {
                var preference = ConfigurationManager.AppSettings[ThemePreferenceKey];
                if (!string.IsNullOrWhiteSpace(preference) && _themes.ContainsKey(preference))
                {
                    ComprehensiveLogger.LogUIOperation("تم تحميل تفضيل الثيم", ComponentName, preference);
                    return preference;
                }

                ComprehensiveLogger.LogUIOperation("استخدام الثيم الافتراضي", ComponentName);
                return "Modern";
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تحميل تفضيل الثيم", ex, ComponentName);
                return "Modern";
            }
        }

        #region Private Methods

        private void InitializeThemes()
        {
            // Windows 7 Classic Theme
            _themes["Win7"] = new ThemeInfo
            {
                Name = "Win7",
                DisplayName = "ويندوز 7 كلاسيك",
                Description = "ثيم ويندوز 7 الكلاسيكي مع الألوان التقليدية",
                Resources = CreateWin7ThemeResources()
            };

            // Modern Theme (Default)
            _themes["Modern"] = new ThemeInfo
            {
                Name = "Modern",
                DisplayName = "حديث",
                Description = "ثيم حديث بألوان زاهية ومظهر عصري",
                Resources = CreateModernThemeResources()
            };

            // Dark Theme
            _themes["Dark"] = new ThemeInfo
            {
                Name = "Dark",
                DisplayName = "داكن",
                Description = "ثيم داكن مريح للعينين في الإضاءة المنخفضة",
                Resources = CreateDarkThemeResources()
            };

            // High Contrast Theme
            _themes["HighContrast"] = new ThemeInfo
            {
                Name = "HighContrast",
                DisplayName = "عالي التباين",
                Description = "ثيم عالي التباين لسهولة القراءة",
                Resources = CreateHighContrastThemeResources()
            };
        }

        private Dictionary<string, object> CreateWin7ThemeResources()
        {
            return new Dictionary<string, object>
            {
                // Background Colors
                ["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                ["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                ["CardBackgroundBrush"] = new SolidColorBrush(Colors.White),

                // Primary Colors
                ["PrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0, 102, 204)),
                ["PrimaryDarkBrush"] = new SolidColorBrush(Color.FromRgb(0, 82, 164)),
                ["AccentBrush"] = new SolidColorBrush(Color.FromRgb(51, 153, 255)),

                // Text Colors
                ["TextBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                ["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                ["TextOnPrimaryBrush"] = new SolidColorBrush(Colors.White),

                // Border and Lines
                ["BorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                ["DividerBrush"] = new SolidColorBrush(Color.FromRgb(229, 229, 229)),

                // Status Colors
                ["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                ["WarningBrush"] = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                ["DangerBrush"] = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                ["InfoBrush"] = new SolidColorBrush(Color.FromRgb(23, 162, 184)),

                // Interactive States
                ["HoverBrush"] = new SolidColorBrush(Color.FromRgb(229, 243, 255)),
                ["PressedBrush"] = new SolidColorBrush(Color.FromRgb(204, 229, 255)),
                ["DisabledBrush"] = new SolidColorBrush(Color.FromRgb(248, 249, 250)),

                // Font Sizes
                ["FontSizeSmall"] = 11.0,
                ["FontSizeNormal"] = 12.0,
                ["FontSizeLarge"] = 14.0,
                ["FontSizeXLarge"] = 16.0
            };
        }

        private Dictionary<string, object> CreateModernThemeResources()
        {
            return new Dictionary<string, object>
            {
                // Background Colors
                ["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                ["SurfaceBrush"] = new SolidColorBrush(Colors.White),
                ["CardBackgroundBrush"] = new SolidColorBrush(Colors.White),

                // Primary Colors
                ["PrimaryBrush"] = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                ["PrimaryDarkBrush"] = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                ["AccentBrush"] = new SolidColorBrush(Color.FromRgb(255, 87, 34)),

                // Text Colors
                ["TextBrush"] = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                ["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                ["TextOnPrimaryBrush"] = new SolidColorBrush(Colors.White),

                // Border and Lines
                ["BorderBrush"] = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                ["DividerBrush"] = new SolidColorBrush(Color.FromRgb(233, 236, 239)),

                // Status Colors
                ["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                ["WarningBrush"] = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                ["DangerBrush"] = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                ["InfoBrush"] = new SolidColorBrush(Color.FromRgb(3, 169, 244)),

                // Interactive States  
                ["HoverBrush"] = new SolidColorBrush(Color.FromRgb(240, 248, 255)),
                ["PressedBrush"] = new SolidColorBrush(Color.FromRgb(224, 242, 254)),
                ["DisabledBrush"] = new SolidColorBrush(Color.FromRgb(248, 249, 250)),

                // Font Sizes
                ["FontSizeSmall"] = 12.0,
                ["FontSizeNormal"] = 13.0,
                ["FontSizeLarge"] = 15.0,
                ["FontSizeXLarge"] = 18.0
            };
        }

        private Dictionary<string, object> CreateDarkThemeResources()
        {
            return new Dictionary<string, object>
            {
                // Background Colors
                ["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(18, 18, 18)),
                ["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                ["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(37, 37, 37)),

                // Primary Colors
                ["PrimaryBrush"] = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                ["PrimaryDarkBrush"] = new SolidColorBrush(Color.FromRgb(66, 165, 245)),
                ["AccentBrush"] = new SolidColorBrush(Color.FromRgb(255, 138, 101)),

                // Text Colors
                ["TextBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                ["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                ["TextOnPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(18, 18, 18)),

                // Border and Lines
                ["BorderBrush"] = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                ["DividerBrush"] = new SolidColorBrush(Color.FromRgb(48, 48, 48)),

                // Status Colors
                ["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(129, 199, 132)),
                ["WarningBrush"] = new SolidColorBrush(Color.FromRgb(255, 183, 77)),
                ["DangerBrush"] = new SolidColorBrush(Color.FromRgb(239, 83, 80)),
                ["InfoBrush"] = new SolidColorBrush(Color.FromRgb(79, 195, 247)),

                // Interactive States
                ["HoverBrush"] = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                ["PressedBrush"] = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                ["DisabledBrush"] = new SolidColorBrush(Color.FromRgb(24, 24, 24)),

                // Font Sizes
                ["FontSizeSmall"] = 12.0,
                ["FontSizeNormal"] = 13.0,
                ["FontSizeLarge"] = 15.0,
                ["FontSizeXLarge"] = 18.0
            };
        }

        private Dictionary<string, object> CreateHighContrastThemeResources()
        {
            return new Dictionary<string, object>
            {
                // Background Colors
                ["BackgroundBrush"] = new SolidColorBrush(Colors.White),
                ["SurfaceBrush"] = new SolidColorBrush(Colors.White),
                ["CardBackgroundBrush"] = new SolidColorBrush(Colors.White),

                // Primary Colors
                ["PrimaryBrush"] = new SolidColorBrush(Colors.Black),
                ["PrimaryDarkBrush"] = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                ["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 255)),

                // Text Colors
                ["TextBrush"] = new SolidColorBrush(Colors.Black),
                ["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                ["TextOnPrimaryBrush"] = new SolidColorBrush(Colors.White),

                // Border and Lines
                ["BorderBrush"] = new SolidColorBrush(Colors.Black),
                ["DividerBrush"] = new SolidColorBrush(Color.FromRgb(128, 128, 128)),

                // Status Colors
                ["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(0, 128, 0)),
                ["WarningBrush"] = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                ["DangerBrush"] = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
                ["InfoBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 255)),

                // Interactive States
                ["HoverBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                ["PressedBrush"] = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                ["DisabledBrush"] = new SolidColorBrush(Color.FromRgb(245, 245, 245)),

                // Font Sizes
                ["FontSizeSmall"] = 13.0,
                ["FontSizeNormal"] = 14.0,
                ["FontSizeLarge"] = 16.0,
                ["FontSizeXLarge"] = 20.0
            };
        }

        private void ClearExistingThemeResources(Application app)
        {
            try
            {
                // Remove theme-specific resource dictionaries
                var resourcesToRemove = new List<ResourceDictionary>();

                foreach (var resource in app.Resources.MergedDictionaries)
                {
                    if (resource.Source?.ToString().Contains("Theme") == true)
                    {
                        resourcesToRemove.Add(resource);
                    }
                }

                foreach (var resource in resourcesToRemove)
                {
                    app.Resources.MergedDictionaries.Remove(resource);
                }

                // Clear individual theme resources
                var keysToRemove = new List<object>();
                foreach (var key in app.Resources.Keys)
                {
                    if (key.ToString()?.EndsWith("Brush") == true ||
                        key.ToString()?.StartsWith("FontSize") == true)
                    {
                        keysToRemove.Add(key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    app.Resources.Remove(key);
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل مسح موارد الثيم السابق", ex, ComponentName);
            }
        }

        private void ApplyThemeResources(Application app, ThemeInfo theme)
        {
            try
            {
                foreach (var resource in theme.Resources)
                {
                    app.Resources[resource.Key] = resource.Value;
                }

                // Force refresh of all windows
                foreach (Window window in app.Windows)
                {
                    try
                    {
                        window.InvalidateVisual();
                        window.UpdateLayout();
                    }
                    catch
                    {
                        // Ignore individual window refresh errors
                    }
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تطبيق موارد الثيم الجديد", ex, ComponentName);
                throw;
            }
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            ThemeChanged?.Invoke(this, e);
            OnPropertyChanged(nameof(CurrentTheme));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    #region Supporting Classes

    public class ThemeInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Resources { get; set; } = new();
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public ThemeInfo? PreviousTheme { get; set; }
        public ThemeInfo? NewTheme { get; set; }
        public bool Applied { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    #endregion
}