using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingSystem.Models;
using AccountingSystem.WPF.Models;
using AccountingSystem.WPF.Views;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// نافذة إدخال صنف جديد للفاتورة - مبسطة
    /// </summary>
    public partial class SimpleItemEntryDialog : Window
    {
        #region Fields

        private readonly List<Product> _allProducts;
        private readonly List<Customer> _allCustomers;
        private Product? _selectedProduct;
        private decimal _unitPrice = 0;
        private decimal _quantity = 1;
        private decimal _discount = 0;

        #endregion

        #region Properties

        /// <summary>
        /// صنف الفاتورة النتيجة
        /// </summary>
        public SalesInvoiceItem? ResultItem { get; private set; }

        #endregion

        #region Constructor

        public SimpleItemEntryDialog(List<Product> products, List<Customer> customers, int customerId = 0)
        {
            InitializeComponent();
            
            _allProducts = products ?? new List<Product>();
            _allCustomers = customers ?? new List<Customer>();
            
            FlowDirection = FlowDirection.RightToLeft;
            Title = "إضافة صنف جديد";

            // تحديد العميل الافتراضي
            if (customerId > 0)
            {
                var customer = _allCustomers.FirstOrDefault(c => c.CustomerId == customerId);
                if (customer != null)
                {
                    txtCustomer.Text = customer.CustomerName;
                    txtCustomer.Tag = customer.CustomerId;
                }
            }

            LoadUI();
            txtProduct.Focus();
        }

        #endregion

        #region Event Handlers

        private void txtProduct_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                ShowProductSearch();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                txtQuantity.Focus();
                e.Handled = true;
            }
        }

        private void txtCustomer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                ShowCustomerSearch();
                e.Handled = true;
            }
        }

        private void txtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtQuantity.Text, out var qty))
            {
                _quantity = qty;
                CalculateTotal();
            }
        }

        private void txtPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtPrice.Text, out var price))
            {
                _unitPrice = price;
                CalculateTotal();
            }
        }

        private void txtDiscount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtDiscount.Text, out var discount))
            {
                _discount = discount;
                CalculateTotal();
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateAndSave())
            {
                DialogResult = true;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
        }

        #endregion

        #region Private Methods

        private void LoadUI()
        {
            txtQuantity.Text = "1";
            txtDiscount.Text = "0";
            lblTotal.Text = "0.00";
        }

        private void ShowProductSearch()
        {
            try
            {
                var productItems = _allProducts.Select(p => ProductSearchItem.FromProduct(p)).ToList();
                var searchDialog = new ProductSearchDialog(productItems)
                {
                    Owner = this
                };

                if (searchDialog.ShowDialog() == true && searchDialog.Selected != null)
                {
                    SelectProduct(searchDialog.Selected);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بحث المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowCustomerSearch()
        {
            try
            {
                var searchDialog = new CustomerSearchDialog(_allCustomers)
                {
                    Owner = this
                };

                if (searchDialog.ShowDialog() == true && searchDialog.SelectedCustomer != null)
                {
                    var customer = searchDialog.SelectedCustomer;
                    txtCustomer.Text = customer.CustomerName;
                    txtCustomer.Tag = customer.CustomerId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بحث العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectProduct(ProductSearchItem productItem)
        {
            _selectedProduct = _allProducts.FirstOrDefault(p => p.ProductId == productItem.ProductId);
            if (_selectedProduct == null) return;

            // تحديث الواجهة
            txtProduct.Text = _selectedProduct.ProductName;
            txtProduct.Tag = _selectedProduct.ProductId;
            
            // تعيين السعر الافتراضي
            _unitPrice = _selectedProduct.SalePrice;
            txtPrice.Text = _unitPrice.ToString("N2");

            // إظهار معلومات المخزون
            lblStock.Text = $"المتوفر: {_selectedProduct.CurrentStock:N2}";

            CalculateTotal();
            txtQuantity.Focus();
        }

        private void CalculateTotal()
        {
            var subtotal = _quantity * _unitPrice;
            var total = subtotal - _discount;
            lblTotal.Text = total.ToString("N2");
        }

        private bool ValidateAndSave()
        {
            // تحقق من اختيار المنتج
            if (_selectedProduct == null)
            {
                MessageBox.Show("يرجى اختيار المنتج", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtProduct.Focus();
                return false;
            }

            // تحقق من الكمية
            if (_quantity <= 0)
            {
                MessageBox.Show("يرجى إدخال كمية صحيحة", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtQuantity.Focus();
                return false;
            }

            // تحقق من السعر
            if (_unitPrice <= 0)
            {
                MessageBox.Show("يرجى إدخال سعر صحيح", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrice.Focus();
                return false;
            }

            // تحقق من المخزون
            if (_quantity > _selectedProduct.CurrentStock)
            {
                var result = MessageBox.Show(
                    $"الكمية المطلوبة ({_quantity:N2}) أكبر من المتوفر ({_selectedProduct.CurrentStock:N2})\nهل تريد المتابعة؟",
                    "تحذير مخزون",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    txtQuantity.Focus();
                    return false;
                }
            }

            // إنشاء صنف الفاتورة
            CreateResultItem();
            return true;
        }

        private void CreateResultItem()
        {
            var subtotal = _quantity * _unitPrice;
            var total = subtotal - _discount;

            ResultItem = new SalesInvoiceItem
            {
                ProductId = _selectedProduct!.ProductId,
                Quantity = _quantity,
                UnitPrice = _unitPrice,
                Discount = _discount,
                LineTotal = total,
                Product = _selectedProduct
            };
        }

        #endregion
    }
}