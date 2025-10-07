using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Business;
using AccountingSystem.Models;
using AccountingSystem.Data;

namespace AccountingSystem.WPF.Views;

public partial class ProductsWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProductService _productService;
    private readonly AccountingDbContext _context;
    private readonly ObservableCollection<Product> _products;
    private List<Product> _allProducts;

    public ProductsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _productService = serviceProvider.GetRequiredService<IProductService>();
        _context = serviceProvider.GetRequiredService<AccountingDbContext>();
        _products = new ObservableCollection<Product>();
        _allProducts = new List<Product>();
        dgProducts.ItemsSource = _products;
        
        Loaded += async (s, e) => await LoadData();
        SetupCurrencyDisplay();
    }

    private void SetupCurrencyDisplay()
    {
        var currencySymbol = _context.SystemSettings
            .FirstOrDefault(s => s.SettingKey == "CurrencySymbol")?.SettingValue ?? "ج.م";
        
        Title = $"إدارة المنتجات والمخزون - العملة: {currencySymbol}";
    }

    private async Task LoadData()
    {
        try
        {
            lblStatus.Text = "جاري تحميل بيانات المنتجات...";
            var products = await _productService.GetAllProductsAsync();
            _allProducts = products.ToList();
            _products.Clear();
            
            foreach (var product in _allProducts)
            {
                _products.Add(product);
            }
            
            lblCount.Text = $"عدد المنتجات: {_products.Count}";
            
            // حساب إجمالي قيمة المخزون بالجنيه المصري
            var totalStockValue = _allProducts.Sum(p => p.CurrentStock * p.PurchasePrice);
            var currencySymbol = _context.SystemSettings
                .FirstOrDefault(s => s.SettingKey == "CurrencySymbol")?.SettingValue ?? "ج.م";
            
            lblStatus.Text = "تم تحميل بيانات المنتجات بنجاح";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatus.Text = "خطأ في تحميل البيانات";
        }
    }

    private void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var productForm = new Window
            {
                Title = "منتج جديد",
                Width = 500,
                Height = 400,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            
            var content = new TextBlock
            {
                Text = "نموذج إضافة منتج جديد\n\nالمميزات المتاحة:\n- إدخال بيانات المنتج\n- تحديد الوحدات\n- ضبط الأسعار\n\nيتم التطوير...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                TextAlignment = TextAlignment.Center
            };
            
            productForm.Content = content;
            productForm.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح نافذة المنتج الجديد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }    private void btnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgProducts.SelectedItem is Product selectedProduct)
        {
            MessageBox.Show($"تعديل المنتج: {selectedProduct.ProductName}\n" +
                          $"المخزون الحالي: {selectedProduct.CurrentStock:N2}\n" +
                          $"سعر البيع: {selectedProduct.SalePrice:N2} ج.م", 
                          "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("يرجى اختيار منتج للتعديل", "تنبيه", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void btnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgProducts.SelectedItem is Product selectedProduct)
        {
            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف المنتج '{selectedProduct.ProductName}'؟\n\n" +
                $"المخزون الحالي: {selectedProduct.CurrentStock:N2}\n" +
                $"قيمة المخزون: {(selectedProduct.CurrentStock * selectedProduct.PurchasePrice):N2} ج.م", 
                "تأكيد الحذف", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _productService.DeleteProductAsync(selectedProduct.ProductId);
                    await LoadData();
                    lblStatus.Text = $"تم حذف المنتج '{selectedProduct.ProductName}' بنجاح";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف المنتج: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("يرجى اختيار منتج للحذف", "تنبيه", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadData();
        lblStatus.Text = "تم تحديث البيانات";
    }

    private void btnStockAdjustment_Click(object sender, RoutedEventArgs e)
    {
        if (dgProducts.SelectedItem is Product selectedProduct)
        {
            MessageBox.Show($"تسوية مخزون المنتج: {selectedProduct.ProductName}\n" +
                          $"المخزون الحالي: {selectedProduct.CurrentStock:N2}\n" +
                          $"الحد الأدنى: {selectedProduct.MinimumStock:N2}\n\n" +
                          "سيتم تطوير نافذة تسوية المخزون قريباً", 
                          "تسوية المخزون", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("يرجى اختيار منتج لتسوية مخزونه", "تنبيه", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (_allProducts == null) return;

        var searchText = txtSearch?.Text?.Trim() ?? string.Empty;
        var filtered = _allProducts.AsEnumerable();

        // تطبيق فلتر البحث
        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(p => 
                p.ProductName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.ProductCode.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            );
        }

        // تطبيق فلتر الفئات (سيتم تفعيله عند إضافة ComboBox بشكل صحيح)
        // if (cmbCategoryFilter?.SelectedItem is ComboBoxItem categoryItem && categoryItem.Tag != null)
        // {
        //     var categoryId = (int)categoryItem.Tag;
        //     filtered = filtered.Where(p => p.CategoryId == categoryId);
        // }

        // تحديث القائمة
        _products.Clear();
        foreach (var product in filtered)
        {
            _products.Add(product);
        }

        lblCount.Text = $"عدد المنتجات: {_products.Count}";
    }

    private void cmbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _context?.Dispose();
    }
}