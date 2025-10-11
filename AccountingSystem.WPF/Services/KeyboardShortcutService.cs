using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AccountingSystem.WPF.Helpers;
using AccountingSystem.WPF.Services;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة اختصارات لوحة المفاتيح المتقدمة مع دعم شامل للتنقل
    /// </summary>
    public interface IKeyboardShortcutService
    {
        void RegisterShortcut(KeyGesture gesture, string action, Action callback);
        void UnregisterShortcut(KeyGesture gesture);
        void EnableShortcuts();
        void DisableShortcuts();
        bool ProcessKeyInput(KeyEventArgs e);
        Dictionary<string, List<KeyboardShortcut>> GetAllShortcuts();
        void ShowShortcutsHelp();
    }

    public class KeyboardShortcutService : IKeyboardShortcutService
    {
        private const string ComponentName = "KeyboardShortcutService";

        private readonly Dictionary<KeyGesture, KeyboardShortcut> _shortcuts;
        private bool _isEnabled = true;

        public KeyboardShortcutService()
        {
            _shortcuts = new Dictionary<KeyGesture, KeyboardShortcut>();
            RegisterDefaultShortcuts();

            ComprehensiveLogger.LogInfo("تم تهيئة خدمة اختصارات لوحة المفاتيح", ComponentName);
        }

        public void RegisterShortcut(KeyGesture gesture, string action, Action callback)
        {
            try
            {
                var shortcut = new KeyboardShortcut
                {
                    Gesture = gesture,
                    Action = action,
                    Callback = callback,
                    Category = GetActionCategory(action)
                };

                _shortcuts[gesture] = shortcut;

                ComprehensiveLogger.LogInfo($"تم تسجيل اختصار جديد: {gesture} - {action}", ComponentName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل تسجيل الاختصار: {action}", ex, ComponentName);
            }
        }

        public void UnregisterShortcut(KeyGesture gesture)
        {
            try
            {
                if (_shortcuts.Remove(gesture))
                {
                    ComprehensiveLogger.LogInfo($"تم إلغاء تسجيل الاختصار: {gesture}", ComponentName);
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل إلغاء تسجيل الاختصار: {gesture}", ex, ComponentName);
            }
        }

        public void EnableShortcuts()
        {
            _isEnabled = true;
            ComprehensiveLogger.LogInfo("تم تفعيل اختصارات لوحة المفاتيح", ComponentName);
        }

        public void DisableShortcuts()
        {
            _isEnabled = false;
            ComprehensiveLogger.LogInfo("تم تعطيل اختصارات لوحة المفاتيح", ComponentName);
        }

        public bool ProcessKeyInput(KeyEventArgs e)
        {
            if (!_isEnabled) return false;

            try
            {
                var gesture = new KeyGesture(e.Key, Keyboard.Modifiers);

                if (_shortcuts.TryGetValue(gesture, out var shortcut))
                {
                    shortcut.Callback?.Invoke();

                    ComprehensiveLogger.LogInfo($"تم تنفيذ الاختصار: {gesture} - {shortcut.Action}", ComponentName);

                    e.Handled = true;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("خطأ في معالجة إدخال لوحة المفاتيح", ex, ComponentName);
                return false;
            }
        }

        public Dictionary<string, List<KeyboardShortcut>> GetAllShortcuts()
        {
            return _shortcuts.Values
                .GroupBy(s => s.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public void ShowShortcutsHelp()
        {
            try
            {
                var helpWindow = new Windows.KeyboardShortcutsHelpWindow();
                helpWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل عرض نافذة مساعدة الاختصارات", ex, ComponentName);
            }
        }

        private void RegisterDefaultShortcuts()
        {
            try
            {
                // اختصارات عامة
                RegisterShortcut(new KeyGesture(Key.F1), "عرض المساعدة", ShowShortcutsHelp);
                RegisterShortcut(new KeyGesture(Key.F5), "تحديث", () => RefreshCurrentView());
                RegisterShortcut(new KeyGesture(Key.F6), "البحث", () => FocusSearchBox());
                RegisterShortcut(new KeyGesture(Key.Escape), "إغلاق", () => CloseCurrentDialog());

                // اختصارات النوافذ
                RegisterShortcut(new KeyGesture(Key.N, ModifierKeys.Control), "فاتورة جديدة", () => CreateNewInvoice());
                RegisterShortcut(new KeyGesture(Key.S, ModifierKeys.Control), "حفظ", () => SaveCurrent());
                RegisterShortcut(new KeyGesture(Key.P, ModifierKeys.Control), "طباعة", () => PrintCurrent());
                RegisterShortcut(new KeyGesture(Key.F, ModifierKeys.Control), "بحث متقدم", () => OpenAdvancedSearch());

                // اختصارات التنقل
                RegisterShortcut(new KeyGesture(Key.Tab, ModifierKeys.Control), "النافذة التالية", () => NextWindow());
                RegisterShortcut(new KeyGesture(Key.Tab, ModifierKeys.Control | ModifierKeys.Shift), "النافذة السابقة", () => PreviousWindow());
                RegisterShortcut(new KeyGesture(Key.Home, ModifierKeys.Control), "الصفحة الرئيسية", () => GoToHomePage());

                // اختصارات الفواتير
                RegisterShortcut(new KeyGesture(Key.F2), "تحرير", () => EditCurrentItem());
                RegisterShortcut(new KeyGesture(Key.F3), "حذف", () => DeleteCurrentItem());
                RegisterShortcut(new KeyGesture(Key.F4), "تفاصيل", () => ShowItemDetails());
                RegisterShortcut(new KeyGesture(Key.F9), "حساب المجموع", () => CalculateTotal());
                RegisterShortcut(new KeyGesture(Key.F10), "إضافة صنف", () => AddNewItem());

                // اختصارات سريعة للأرقام
                RegisterShortcut(new KeyGesture(Key.F11), "عميل جديد", () => CreateNewCustomer());
                RegisterShortcut(new KeyGesture(Key.F12), "منتج جديد", () => CreateNewProduct());

                ComprehensiveLogger.LogInfo("تم تسجيل الاختصارات الافتراضية", ComponentName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تسجيل الاختصارات الافتراضية", ex, ComponentName);
            }
        }

        private static string GetActionCategory(string action)
        {
            return action switch
            {
                var a when a.Contains("فاتورة") => "الفواتير",
                var a when a.Contains("عميل") => "العملاء",
                var a when a.Contains("منتج") => "المنتجات",
                var a when a.Contains("بحث") => "البحث والتنقل",
                var a when a.Contains("حفظ") || a.Contains("طباعة") => "الملفات",
                var a when a.Contains("نافذة") || a.Contains("صفحة") => "التنقل",
                _ => "عام"
            };
        }

        #region Default Action Implementations

        private static void RefreshCurrentView()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow is IRefreshable refreshable)
            {
                refreshable.Refresh();
            }
        }

        private static void FocusSearchBox()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow is ISearchable searchable)
            {
                searchable.FocusSearchBox();
            }
        }

        private static void CloseCurrentDialog()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow != Application.Current.MainWindow)
            {
                activeWindow?.Close();
            }
        }

        private static void CreateNewInvoice()
        {
            try
            {
                // سيتم تنفيذه لاحقاً - فتح نافذة فاتورة جديدة
                MessageBox.Show("سيتم فتح نافذة فاتورة جديدة", "اختصار سريع", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل إنشاء فاتورة جديدة من الاختصار", ex, "KeyboardShortcuts");
            }
        }

        private static void SaveCurrent()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow is ISaveable saveable)
            {
                saveable.Save();
            }
        }

        private static void PrintCurrent()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow is IPrintable printable)
            {
                printable.Print();
            }
        }

        private static void OpenAdvancedSearch()
        {
            // سيتم تنفيذه لاحقاً
            MessageBox.Show("سيتم فتح نافذة البحث المتقدم", "اختصار سريع", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void NextWindow()
        {
            var windows = Application.Current.Windows.OfType<Window>().ToList();
            var activeIndex = windows.FindIndex(w => w.IsActive);
            if (activeIndex >= 0 && windows.Count > 1)
            {
                var nextIndex = (activeIndex + 1) % windows.Count;
                windows[nextIndex].Activate();
            }
        }

        private static void PreviousWindow()
        {
            var windows = Application.Current.Windows.OfType<Window>().ToList();
            var activeIndex = windows.FindIndex(w => w.IsActive);
            if (activeIndex >= 0 && windows.Count > 1)
            {
                var prevIndex = activeIndex == 0 ? windows.Count - 1 : activeIndex - 1;
                windows[prevIndex].Activate();
            }
        }

        private static void GoToHomePage()
        {
            Application.Current.MainWindow?.Activate();
        }

        private static void EditCurrentItem()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow is IEditable editable)
            {
                editable.Edit();
            }
        }

        private static void DeleteCurrentItem()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow is IDeletable deletable)
            {
                deletable.Delete();
            }
        }

        private static void ShowItemDetails()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow is IDetailViewable detailable)
            {
                detailable.ShowDetails();
            }
        }

        private static void CalculateTotal()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow is ICalculatable calculatable)
            {
                calculatable.CalculateTotal();
            }
        }

        private static void AddNewItem()
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow is IItemAddable addable)
            {
                addable.AddNewItem();
            }
        }

        private static void CreateNewCustomer()
        {
            // سيتم تنفيذه لاحقاً
            MessageBox.Show("سيتم فتح نافذة عميل جديد", "اختصار سريع", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void CreateNewProduct()
        {
            // سيتم تنفيذه لاحقاً
            MessageBox.Show("سيتم فتح نافذة منتج جديد", "اختصار سريع", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }

    #region Data Classes and Interfaces

    public class KeyboardShortcut
    {
        public KeyGesture Gesture { get; set; } = new KeyGesture(Key.None);
        public string Action { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Action? Callback { get; set; }
        public string Description => $"{Gesture} - {Action}";
    }

    // واجهات للتفاعل مع النوافذ
    public interface IRefreshable
    {
        void Refresh();
    }

    public interface ISearchable
    {
        void FocusSearchBox();
    }

    public interface ISaveable
    {
        void Save();
    }

    public interface IPrintable
    {
        void Print();
    }

    public interface IEditable
    {
        void Edit();
    }

    public interface IDeletable
    {
        void Delete();
    }

    public interface IDetailViewable
    {
        void ShowDetails();
    }

    public interface ICalculatable
    {
        void CalculateTotal();
    }

    public interface IItemAddable
    {
        void AddNewItem();
    }

    #endregion
}