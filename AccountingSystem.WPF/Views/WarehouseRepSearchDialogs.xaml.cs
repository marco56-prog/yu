using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// نافذة بحث المستودعات
    /// </summary>
    public partial class WarehouseSearchDialog : Window
    {
        #region Fields

        private readonly ObservableCollection<Warehouse> _originalItems;
        private readonly ObservableCollection<Warehouse> _filteredItems;
        private string _searchText = "";

        #endregion

        #region Constructor

        public WarehouseSearchDialog(List<Warehouse> warehouses)
        {
            InitializeComponent();

            _originalItems = new ObservableCollection<Warehouse>(warehouses ?? new List<Warehouse>());
            _filteredItems = new ObservableCollection<Warehouse>(_originalItems);

            lblItemCount.Text = $"عدد المستودعات: {_originalItems.Count}";

            dgResults.ItemsSource = _filteredItems;
            txtSearch.Focus();
        }

        #endregion

        #region Properties

        /// <summary>
        /// المستودع المختار
        /// </summary>
        public Warehouse? SelectedWarehouse { get; private set; }

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
                    var searchResults = _originalItems.Where(warehouse =>
                        warehouse.WarehouseName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                        warehouse.WarehouseCode.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                        (warehouse.Address?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false));

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
            if (dgResults.SelectedItem is Warehouse selectedWarehouse)
            {
                SelectedWarehouse = selectedWarehouse;
                DialogResult = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// نافذة بحث المندوبين
    /// </summary>
    public partial class RepresentativeSearchDialog : Window
    {
        #region Fields

        private readonly ObservableCollection<Representative> _originalItems;
        private readonly ObservableCollection<Representative> _filteredItems;
        private string _searchText = "";

        #endregion

        #region Constructor

        public RepresentativeSearchDialog(List<Representative> representatives)
        {
            InitializeComponent();

            _originalItems = new ObservableCollection<Representative>(representatives ?? new List<Representative>());
            _filteredItems = new ObservableCollection<Representative>(_originalItems);

            lblItemCount.Text = $"عدد المندوبين: {_originalItems.Count}";

            dgResults.ItemsSource = _filteredItems;
            txtSearch.Focus();
        }

        #endregion

        #region Properties

        /// <summary>
        /// المندوب المختار
        /// </summary>
        public Representative? SelectedRepresentative { get; private set; }

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
                    var searchResults = _originalItems.Where(rep =>
                        rep.RepresentativeName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                        rep.RepresentativeCode.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                        (rep.Phone?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false));

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
            if (dgResults.SelectedItem is Representative selectedRep)
            {
                SelectedRepresentative = selectedRep;
                DialogResult = true;
            }
        }

        #endregion
    }
}