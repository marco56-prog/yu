using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// Base class لنوافذ البحث - يحتوي على العناصر المشتركة
    /// </summary>
    public partial class SearchDialogBase : Window
    {
        protected SearchDialogBase()
        {
            InitializeComponent();
        }

        protected virtual void txtSearch_TextChanged(object sender, TextChangedEventArgs e) { }
        protected virtual void Window_PreviewKeyDown(object sender, KeyEventArgs e) { }
        protected virtual void btnOK_Click(object sender, RoutedEventArgs e) { }
        protected virtual void btnCancel_Click(object sender, RoutedEventArgs e) { }
        protected virtual void dgResults_MouseDoubleClick(object sender, MouseButtonEventArgs e) { }
        protected virtual void dgResults_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        protected virtual void ClearSearch(object sender, RoutedEventArgs e) { }
    }

    /// <summary>
    /// نافذة بحث عامة قابلة للتخصيص لأي نوع من البيانات
    /// تدعم: البحث الفوري، الفلترة، اختصارات الكيبورد، التنقل بالأسهم
    /// </summary>
    /// <typeparam name="T">نوع البيانات المراد البحث فيها</typeparam>
    public class SearchDialog<T> : SearchDialogBase where T : class
    {
        #region Fields

        private readonly ObservableCollection<T> _originalItems;
        private readonly ObservableCollection<T> _filteredItems;
        private readonly ICollectionView _collectionView;
        private readonly SearchConfiguration<T> _config;

        private string _searchText = "";

        #endregion

        #region Constructor

        public SearchDialog(List<T> items, SearchConfiguration<T> config, string title = "بحث")
        {
            InitializeComponent();

            _config = config ?? throw new ArgumentNullException(nameof(config));
            _originalItems = new ObservableCollection<T>(items ?? throw new ArgumentNullException(nameof(items)));
            _filteredItems = new ObservableCollection<T>(_originalItems);

            Title = title;
            lblTitle.Text = title;
            lblItemCount.Text = $"عدد العناصر: {_originalItems.Count}";

            // إعداد CollectionView للفلترة والترتيب
            _collectionView = CollectionViewSource.GetDefaultView(_filteredItems);
            
            // إعداد الأعمدة حسب التكوين
            SetupColumns();

            // ربط البيانات
            dgResults.ItemsSource = _collectionView;

            // ركّز على البحث
            txtSearch.Focus();

            Loaded += (_, __) => txtSearch.Focus();
        }

        #endregion

        #region Properties

        /// <summary>
        /// العنصر المختار من المستخدم
        /// </summary>
        public T? SelectedItem { get; private set; }

        #endregion

        #region Setup Methods

        private void SetupColumns()
        {
            dgResults.Columns.Clear();

            foreach (var column in _config.Columns)
            {
                var gridColumn = new DataGridTextColumn
                {
                    Header = column.Header,
                    Binding = new Binding(column.PropertyPath),
                    Width = column.Width == 0 ? new DataGridLength(1, DataGridLengthUnitType.Star) : new DataGridLength(column.Width),
                    IsReadOnly = true
                };

                // تطبيق تنسيق مخصص إن وُجد
                if (!string.IsNullOrEmpty(column.StringFormat))
                {
                    if (gridColumn.Binding is Binding binding)
                        binding.StringFormat = column.StringFormat;
                }

                dgResults.Columns.Add(gridColumn);
            }
        }

        #endregion

        #region Search Logic

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = txtSearch.Text?.Trim() ?? "";
            PerformSearch();
        }

        private void PerformSearch()
        {
            try
            {
                _filteredItems.Clear();

                if (string.IsNullOrWhiteSpace(_searchText))
                {
                    // إذا لم يكن هناك نص بحث، أظهر كل العناصر
                    foreach (var item in _originalItems)
                        _filteredItems.Add(item);
                }
                else
                {
                    // تطبيق البحث حسب التكوين
                    var searchResults = _originalItems.Where(item => _config.SearchFunction(item, _searchText));
                    
                    foreach (var item in searchResults)
                        _filteredItems.Add(item);
                }

                // تحديث عداد النتائج
                lblItemCount.Text = $"عدد النتائج: {_filteredItems.Count} من {_originalItems.Count}";

                // اختيار أول عنصر تلقائياً
                if (_filteredItems.Count > 0)
                {
                    dgResults.SelectedIndex = 0;
                    dgResults.ScrollIntoView(_filteredItems[0]);
                }

                // تحديث حالة الأزرار
                btnOK.IsEnabled = _filteredItems.Count > 0;
                
                UpdateStatusMessage();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"خطأ في البحث: {ex.Message}";
                lblStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void UpdateStatusMessage()
        {
            if (_filteredItems.Count == 0 && !string.IsNullOrWhiteSpace(_searchText))
            {
                lblStatus.Text = "لا توجد نتائج مطابقة للبحث";
                lblStatus.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else if (_filteredItems.Count > 0)
            {
                lblStatus.Text = "استخدم الأسهم للتنقل، Enter للاختيار";
                lblStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                lblStatus.Text = "اكتب للبحث أو استخدم الأسهم للتصفح";
                lblStatus.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        #endregion

        #region Keyboard Navigation

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (_filteredItems.Count > 0)
                    {
                        SelectCurrentItem();
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    DialogResult = false;
                    e.Handled = true;
                    break;

                case Key.Down:
                    NavigateDown();
                    e.Handled = true;
                    break;

                case Key.Up:
                    NavigateUp();
                    e.Handled = true;
                    break;

                case Key.PageDown:
                    NavigatePage(true);
                    e.Handled = true;
                    break;

                case Key.PageUp:
                    NavigatePage(false);
                    e.Handled = true;
                    break;

                case Key.Home:
                    NavigateToStart();
                    e.Handled = true;
                    break;

                case Key.End:
                    NavigateToEnd();
                    e.Handled = true;
                    break;

                case Key.F3:
                    if (Keyboard.Modifiers == ModifierKeys.None)
                    {
                        FindNext();
                        e.Handled = true;
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        FindPrevious();
                        e.Handled = true;
                    }
                    break;

                default:
                    // إذا لم تكن حروف التحكم، ركّز على البحث
                    if (e.Key >= Key.A && e.Key <= Key.Z || 
                        e.Key >= Key.D0 && e.Key <= Key.D9 ||
                        e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 ||
                        e.Key == Key.Space)
                    {
                        if (!txtSearch.IsKeyboardFocused)
                        {
                            txtSearch.Focus();
                        }
                    }
                    break;
            }
        }

        private void NavigateDown()
        {
            if (dgResults.SelectedIndex < _filteredItems.Count - 1)
            {
                dgResults.SelectedIndex++;
                ScrollToSelected();
            }
        }

        private void NavigateUp()
        {
            if (dgResults.SelectedIndex > 0)
            {
                dgResults.SelectedIndex--;
                ScrollToSelected();
            }
        }

        private void NavigatePage(bool down)
        {
            var itemsPerPage = Math.Max(1, (int)(dgResults.ActualHeight / 25)); // تقدير تقريبي
            var currentIndex = dgResults.SelectedIndex;
            var newIndex = down 
                ? Math.Min(_filteredItems.Count - 1, currentIndex + itemsPerPage)
                : Math.Max(0, currentIndex - itemsPerPage);
            
            dgResults.SelectedIndex = newIndex;
            ScrollToSelected();
        }

        private void NavigateToStart()
        {
            if (_filteredItems.Count > 0)
            {
                dgResults.SelectedIndex = 0;
                ScrollToSelected();
            }
        }

        private void NavigateToEnd()
        {
            if (_filteredItems.Count > 0)
            {
                dgResults.SelectedIndex = _filteredItems.Count - 1;
                ScrollToSelected();
            }
        }

        private void ScrollToSelected()
        {
            if (dgResults.SelectedItem != null)
            {
                dgResults.ScrollIntoView(dgResults.SelectedItem);
            }
        }

        private void FindNext()
        {
            // بحث عن التكرار التالي لنص البحث (يمكن تطويره لاحقاً)
            if (_filteredItems.Count > 0 && dgResults.SelectedIndex < _filteredItems.Count - 1)
            {
                NavigateDown();
            }
        }

        private void FindPrevious()
        {
            // بحث عن التكرار السابق لنص البحث (يمكن تطويره لاحقاً)
            if (_filteredItems.Count > 0 && dgResults.SelectedIndex > 0)
            {
                NavigateUp();
            }
        }

        #endregion

        #region Event Handlers

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            SelectCurrentItem();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void dgResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrentItem();
        }

        private void dgResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnOK.IsEnabled = dgResults.SelectedItem != null;
        }

        private void SelectCurrentItem()
        {
            if (dgResults.SelectedItem is T selectedItem)
            {
                SelectedItem = selectedItem;
                DialogResult = true;
            }
        }

        #endregion

        #region Helper Methods

        private void ClearSearch()
        {
            txtSearch.Clear();
            txtSearch.Focus();
        }

        #endregion
    }

    /// <summary>
    /// تكوين نافذة البحث
    /// </summary>
    /// <typeparam name="T">نوع البيانات</typeparam>
    public class SearchConfiguration<T>
    {
        /// <summary>
        /// دالة البحث - تتلقى العنصر ونص البحث وترجع هل يطابق أم لا
        /// </summary>
        public Func<T, string, bool> SearchFunction { get; set; } = (item, searchText) => true;

        /// <summary>
        /// الأعمدة المراد عرضها
        /// </summary>
        public List<SearchColumnConfig> Columns { get; set; } = new();

        /// <summary>
        /// خيار لترتيب النتائج (يمكن تطويره لاحقاً)
        /// </summary>
        public string? DefaultSortProperty { get; set; }

        public bool DefaultSortDescending { get; set; } = false;
    }

    /// <summary>
    /// تكوين عمود في نافذة البحث
    /// </summary>
    public class SearchColumnConfig
    {
        public string Header { get; set; } = "";
        public string PropertyPath { get; set; } = "";
        public double Width { get; set; } = 0; // 0 = Auto width
        public string? StringFormat { get; set; }
    }

    /// <summary>
    /// Factory لإنشاء تكوينات البحث الشائعة
    /// </summary>
    public static class SearchConfigurations
    {
        public static SearchConfiguration<Customer> CreateCustomerConfig()
        {
            return new SearchConfiguration<Customer>
            {
                SearchFunction = (customer, searchText) =>
                    customer.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    customer.CustomerCode.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (customer.Phone?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true),
                
                Columns = new List<SearchColumnConfig>
                {
                    new() { Header = "الكود", PropertyPath = "CustomerCode", Width = 100 },
                    new() { Header = "اسم العميل", PropertyPath = "CustomerName", Width = 250 },
                    new() { Header = "الهاتف", PropertyPath = "Phone", Width = 120 },
                    new() { Header = "الرصيد", PropertyPath = "Balance", Width = 100, StringFormat = "{0:N2}" },
                    new() { Header = "العنوان", PropertyPath = "Address" }
                }
            };
        }

        public static SearchConfiguration<Product> CreateProductConfig()
        {
            return new SearchConfiguration<Product>
            {
                SearchFunction = (product, searchText) =>
                    product.ProductName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    product.ProductCode.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (product.Barcode?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true),
                
                Columns = new List<SearchColumnConfig>
                {
                    new() { Header = "الكود", PropertyPath = "ProductCode", Width = 100 },
                    new() { Header = "اسم المنتج", PropertyPath = "ProductName", Width = 250 },
                    new() { Header = "باركود", PropertyPath = "Barcode", Width = 120 },
                    new() { Header = "سعر البيع", PropertyPath = "SalePrice", Width = 100, StringFormat = "{0:N2}" },
                    new() { Header = "المخزون", PropertyPath = "CurrentStock", Width = 80, StringFormat = "{0:N2}" }
                }
            };
        }

        public static SearchConfiguration<Supplier> CreateSupplierConfig()
        {
            return new SearchConfiguration<Supplier>
            {
                SearchFunction = (supplier, searchText) =>
                    supplier.SupplierName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    supplier.SupplierCode.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (supplier.Phone?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true),
                
                Columns = new List<SearchColumnConfig>
                {
                    new() { Header = "الكود", PropertyPath = "SupplierCode", Width = 100 },
                    new() { Header = "اسم المورد", PropertyPath = "SupplierName", Width = 250 },
                    new() { Header = "الهاتف", PropertyPath = "Phone", Width = 120 },
                    new() { Header = "الرصيد", PropertyPath = "Balance", Width = 100, StringFormat = "{0:N2}" },
                    new() { Header = "العنوان", PropertyPath = "Address" }
                }
            };
        }

        public static SearchConfiguration<SalesInvoice> CreateSalesInvoiceConfig()
        {
            return new SearchConfiguration<SalesInvoice>
            {
                SearchFunction = (invoice, searchText) =>
                    invoice.InvoiceNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (invoice.Customer?.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true),
                
                Columns = new List<SearchColumnConfig>
                {
                    new() { Header = "رقم الفاتورة", PropertyPath = "InvoiceNumber", Width = 120 },
                    new() { Header = "العميل", PropertyPath = "Customer.CustomerName", Width = 200 },
                    new() { Header = "التاريخ", PropertyPath = "InvoiceDate", Width = 100, StringFormat = "{0:yyyy-MM-dd}" },
                    new() { Header = "صافي المبلغ", PropertyPath = "NetTotal", Width = 120, StringFormat = "{0:N2}" },
                    new() { Header = "الحالة", PropertyPath = "Status", Width = 80 }
                }
            };
        }

        // يمكن إضافة المزيد من التكوينات للكيانات الأخرى مثل المخازن والمندوبين...
        
        public static SearchConfiguration<T> CreateGenericConfig<T>(
            Func<T, string, bool> searchFunc, 
            params (string Header, string PropertyPath, double Width, string? Format)[] columns)
        {
            return new SearchConfiguration<T>
            {
                SearchFunction = searchFunc,
                Columns = columns.Select(c => new SearchColumnConfig 
                { 
                    Header = c.Header, 
                    PropertyPath = c.PropertyPath, 
                    Width = c.Width,
                    StringFormat = c.Format 
                }).ToList()
            };
        }
    }
}