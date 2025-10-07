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
    /// نافذة بحث العملاء
    /// </summary>
    public partial class CustomerSearchDialog : Window
    {
        #region Fields
        
        private readonly ObservableCollection<Customer> _originalItems;
        private readonly ObservableCollection<Customer> _filteredItems;
        private readonly ICollectionView _collectionView;
        private string _searchText = "";

        #endregion

        #region Constructor

        public CustomerSearchDialog(List<Customer> customers)
        {
            InitializeComponent();

            _originalItems = new ObservableCollection<Customer>(customers ?? new List<Customer>());
            _filteredItems = new ObservableCollection<Customer>(_originalItems);

            lblItemCount.Text = $"عدد العملاء: {_originalItems.Count}";

            _collectionView = CollectionViewSource.GetDefaultView(_filteredItems);
            dgResults.ItemsSource = _collectionView;

            txtSearch.Focus();
        }

        #endregion

        #region Properties

        /// <summary>
        /// العميل المختار
        /// </summary>
        public Customer? SelectedCustomer { get; private set; }

        #endregion

        #region Event Handlers

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = txtSearch.Text?.Trim() ?? "";
            PerformSearch();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter when _filteredItems.Count > 0:
                    SelectCurrentItem();
                    e.Handled = true;
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
            }
        }

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

        private void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            txtSearch.Focus();
        }

        #endregion

        #region Private Methods

        private void PerformSearch()
        {
            try
            {
                _filteredItems.Clear();

                if (string.IsNullOrWhiteSpace(_searchText))
                {
                    foreach (var item in _originalItems)
                        _filteredItems.Add(item);
                }
                else
                {
                    var searchResults = _originalItems.Where(customer =>
                        customer.CustomerName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                        customer.CustomerId.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                        (customer.Phone?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false));

                    foreach (var item in searchResults)
                        _filteredItems.Add(item);
                }

                lblItemCount.Text = $"عدد النتائج: {_filteredItems.Count} من {_originalItems.Count}";

                if (_filteredItems.Count > 0)
                {
                    dgResults.SelectedIndex = 0;
                    dgResults.ScrollIntoView(_filteredItems[0]);
                }

                btnOK.IsEnabled = _filteredItems.Count > 0;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"خطأ في البحث: {ex.Message}";
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

        private void ScrollToSelected()
        {
            if (dgResults.SelectedItem != null)
            {
                dgResults.ScrollIntoView(dgResults.SelectedItem);
            }
        }

        private void SelectCurrentItem()
        {
            if (dgResults.SelectedItem is Customer selectedCustomer)
            {
                SelectedCustomer = selectedCustomer;
                DialogResult = true;
            }
        }

        #endregion
    }
}