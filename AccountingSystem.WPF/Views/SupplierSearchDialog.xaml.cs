using AccountingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AccountingSystem.WPF.Views
{
    public partial class SupplierSearchDialog : Window
    {
        public Supplier? SelectedSupplier { get; private set; }
        private readonly List<Supplier> _allSuppliers;
        private readonly List<Supplier> _filteredSuppliers;

        public SupplierSearchDialog(List<Supplier> suppliers)
        {
            InitializeComponent();
            
            _allSuppliers = suppliers ?? throw new ArgumentNullException(nameof(suppliers));
            _filteredSuppliers = new List<Supplier>(_allSuppliers);
            
            dgSuppliers.ItemsSource = _filteredSuppliers;
            txtSearch.Focus();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterSuppliers();
        }

        private void FilterSuppliers()
        {
            try
            {
                var searchText = txtSearch.Text?.ToLowerInvariant() ?? "";
                
                _filteredSuppliers.Clear();
                
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    _filteredSuppliers.AddRange(_allSuppliers);
                }
                else
                {
                    var filtered = _allSuppliers.Where(s => 
                        s.SupplierName?.ToLowerInvariant().Contains(searchText) == true ||
                        s.Phone?.ToLowerInvariant().Contains(searchText) == true ||
                        s.Address?.ToLowerInvariant().Contains(searchText) == true
                    );
                    
                    _filteredSuppliers.AddRange(filtered);
                }
                
                dgSuppliers.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            txtSearch.Focus();
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_filteredSuppliers.Count == 1)
                {
                    SelectedSupplier = _filteredSuppliers[0];
                    DialogResult = true;
                    Close();
                }
                else if (dgSuppliers.SelectedItem is Supplier supplier)
                {
                    SelectedSupplier = supplier;
                    DialogResult = true;
                    Close();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Down && dgSuppliers.Items.Count > 0)
            {
                dgSuppliers.SelectedIndex = 0;
                dgSuppliers.Focus();
                e.Handled = true;
            }
        }

        private void dgSuppliers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgSuppliers.SelectedItem is Supplier supplier)
            {
                SelectedSupplier = supplier;
                DialogResult = true;
                Close();
            }
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (dgSuppliers.SelectedItem is Supplier supplier)
            {
                SelectedSupplier = supplier;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("يرجى اختيار مورد من القائمة", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}