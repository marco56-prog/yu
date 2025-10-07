using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views
{
    public partial class CashTransferDialog : Window
    {
        public decimal Amount { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public string Notes { get; private set; } = string.Empty;
        public CashBox? TargetCashBox { get; private set; }

        private readonly CashBox _sourceCashBox;
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

        public CashTransferDialog(List<CashBox> allCashBoxes, CashBox sourceCashBox)
        {
            InitializeComponent();

            _sourceCashBox = sourceCashBox ?? throw new ArgumentNullException(nameof(sourceCashBox));
            txtSourceCashBox.Text = $"{_sourceCashBox.CashBoxName} (الرصيد: {_sourceCashBox.CurrentBalance:N2} ج.م)";

            // استبعاد خزنة المصدر
            var targetBoxes = (allCashBoxes ?? new List<CashBox>())
                              .Where(cb => cb.CashBoxId != _sourceCashBox.CashBoxId)
                              .OrderBy(cb => cb.CashBoxName)
                              .ToList();

            cmbTargetCashBox.ItemsSource = targetBoxes;
            if (targetBoxes.Any())
                cmbTargetCashBox.SelectedIndex = 0;

            Loaded += (_, __) => txtAmount.Focus();
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            Amount = decimal.Parse(txtAmount.Text, NumberStyles.Number, _culture);
            Description = txtDescription.Text.Trim();
            Notes = txtNotes.Text.Trim();
            TargetCashBox = cmbTargetCashBox.SelectedItem as CashBox;

            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            // وجهة
            if (cmbTargetCashBox.SelectedItem is not CashBox target)
            {
                MessageBox.Show("يرجى اختيار خزنة الوجهة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbTargetCashBox.Focus();
                return false;
            }

            // المبلغ
            if (string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                MessageBox.Show("يرجى إدخال مبلغ التحويل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAmount.Focus();
                return false;
            }

            if (!decimal.TryParse(txtAmount.Text, NumberStyles.Number, _culture, out var amount) || amount <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAmount.Focus();
                txtAmount.SelectAll();
                return false;
            }

            // لا يتجاوز رصيد المصدر
            if (amount > _sourceCashBox.CurrentBalance)
            {
                MessageBox.Show("مبلغ التحويل أكبر من رصيد خزنة المصدر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAmount.Focus();
                txtAmount.SelectAll();
                return false;
            }

            // الوصف
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("يرجى إدخال وصف للتحويل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescription.Focus();
                return false;
            }

            return true;
        }

        // السماح فقط بالأرقام والفاصل العشري حسب ثقافة النظام + Backspace
        private void txtAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var sep = _culture.NumberFormat.NumberDecimalSeparator;
            var pattern = $"^[0-9{Regex.Escape(sep)}]+$";
            e.Handled = !Regex.IsMatch(e.Text, pattern);
        }

        private void txtAmount_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!decimal.TryParse(text, NumberStyles.Number, _culture, out _))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
