using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AccountingSystem.WPF.Windows
{
    /// <summary>
    /// نافذة مساعدة اختصارات لوحة المفاتيح
    /// </summary>
    public partial class KeyboardShortcutsHelpWindow : Window
    {
        #region Constructor

        public KeyboardShortcutsHelpWindow()
        {
            InitializeHelp();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// تهيئة النافذة
        /// </summary>
        private void InitializeHelp()
        {
            Width = 800;
            Height = 600;
            Title = "مساعدة اختصارات لوحة المفاتيح";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            CreateContent();
        }

        /// <summary>
        /// إنشاء محتوى النافذة
        /// </summary>
        private void CreateContent()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // العنوان
            var titleBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(41, 128, 185)),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var titleText = new TextBlock
            {
                Text = "🎯 اختصارات لوحة المفاتيح",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            titleBorder.Child = titleText;
            Grid.SetRow(titleBorder, 0);
            mainGrid.Children.Add(titleBorder);

            // منطقة المحتوى
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(20, 10, 20, 10)
            };

            var contentStack = new StackPanel();
            AddGeneralShortcuts(contentStack);
            AddNavigationShortcuts(contentStack);
            AddDataEntryShortcuts(contentStack);

            scrollViewer.Content = contentStack;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // منطقة الأزرار
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };

            var closeButton = new Button
            {
                Content = "إغلاق",
                Width = 100,
                Height = 35,
                Margin = new Thickness(5, 0, 5, 0)
            };
            closeButton.Click += (s, e) => Close();

            buttonPanel.Children.Add(closeButton);
            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        #endregion

        #region Shortcut Sections

        private void AddGeneralShortcuts(StackPanel parent)
        {
            var section = CreateSection("🔧 اختصارات عامة");
            
            var shortcuts = new[]
            {
                new ShortcutInfo("F1", "عرض المساعدة", "عرض نافذة المساعدة للنافذة الحالية"),
                new ShortcutInfo("F2", "حفظ", "حفظ البيانات الحالية"),
                new ShortcutInfo("F3", "بحث", "فتح نافذة البحث"),
                new ShortcutInfo("F4", "عمليات خاصة", "عرض قائمة العمليات الخاصة"),
                new ShortcutInfo("F5", "تحديث", "تحديث البيانات من قاعدة البيانات"),
                new ShortcutInfo("F12", "أرشفة/حذف", "أرشفة أو حذف العنصر المحدد"),
                new ShortcutInfo("Escape", "إغلاق/إلغاء", "إغلاق النافذة أو إلغاء العملية الحالية")
            };

            AddShortcutsToSection(section, shortcuts);
            parent.Children.Add(section);
        }

        private void AddNavigationShortcuts(StackPanel parent)
        {
            var section = CreateSection("🧭 اختصارات التنقل");
            
            var shortcuts = new[]
            {
                new ShortcutInfo("Page Up", "السجل السابق", "الانتقال للسجل السابق"),
                new ShortcutInfo("Page Down", "السجل التالي", "الانتقال للسجل التالي"),
                new ShortcutInfo("Home", "أول سجل", "الانتقال لأول سجل في القائمة"),
                new ShortcutInfo("End", "آخر سجل", "الانتقال لآخر سجل في القائمة"),
                new ShortcutInfo("Tab", "الحقل التالي", "الانتقال للحقل التالي"),
                new ShortcutInfo("Enter", "تأكيد/التالي", "تأكيد الإدخال أو الانتقال للسجل التالي")
            };

            AddShortcutsToSection(section, shortcuts);
            parent.Children.Add(section);
        }

        private void AddDataEntryShortcuts(StackPanel parent)
        {
            var section = CreateSection("📝 اختصارات إدخال البيانات");
            
            var shortcuts = new[]
            {
                new ShortcutInfo("F6", "اختيار العميل", "فتح نافذة اختيار العميل"),
                new ShortcutInfo("F7", "اختيار المنتج", "فتح نافذة اختيار المنتج"),
                new ShortcutInfo("F8", "حساب الإجمالي", "حساب إجمالي الفاتورة"),
                new ShortcutInfo("F9", "خصم", "تطبيق خصم على السطر أو الفاتورة"),
                new ShortcutInfo("F10", "ضريبة", "حساب وتطبيق الضريبة"),
                new ShortcutInfo("Insert", "سطر جديد", "إضافة سطر جديد"),
                new ShortcutInfo("Delete", "حذف", "حذف المحدد")
            };

            AddShortcutsToSection(section, shortcuts);
            parent.Children.Add(section);
        }

        #endregion

        #region Helper Methods

        private Border CreateSection(string title)
        {
            var sectionBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Margin = new Thickness(0, 10, 0, 10),
                CornerRadius = new CornerRadius(5)
            };

            var sectionStack = new StackPanel();

            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(236, 240, 241)),
                Padding = new Thickness(15, 10, 15, 10)
            };

            var headerText = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };

            headerBorder.Child = headerText;
            sectionStack.Children.Add(headerBorder);
            sectionBorder.Child = sectionStack;
            
            return sectionBorder;
        }

        private void AddShortcutsToSection(Border section, ShortcutInfo[] shortcuts)
        {
            var sectionStack = (StackPanel)section.Child;
            
            var grid = new Grid
            {
                Margin = new Thickness(15, 10, 15, 10)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < shortcuts.Length; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                AddGridCell(grid, i, 0, shortcuts[i].Key, FontWeights.Normal, Color.FromRgb(52, 152, 219));
                AddGridCell(grid, i, 1, shortcuts[i].Function, FontWeights.Medium, Color.FromRgb(44, 62, 80));
                AddGridCell(grid, i, 2, shortcuts[i].Description, FontWeights.Normal, Color.FromRgb(127, 140, 141));
            }

            sectionStack.Children.Add(grid);
        }

        private void AddGridCell(Grid grid, int row, int column, string text, FontWeight weight, Color color)
        {
            var border = new Border
            {
                Padding = new Thickness(8, 4, 8, 4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                FontWeight = weight,
                Foreground = new SolidColorBrush(color),
                TextWrapping = TextWrapping.Wrap
            };

            border.Child = textBlock;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);
            grid.Children.Add(border);
        }

        #endregion
    }

    /// <summary>
    /// معلومات الاختصار
    /// </summary>
    public class ShortcutInfo
    {
        public string Key { get; set; }
        public string Function { get; set; }
        public string Description { get; set; }

        public ShortcutInfo(string key, string function, string description)
        {
            Key = key;
            Function = function;
            Description = description;
        }
    }
}