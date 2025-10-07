using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views
{
    public partial class CashTransactionDialog : Window
    {
        public decimal Amount { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public string CustomerSupplierName { get; private set; } = string.Empty;
        public string Notes { get; private set; } = string.Empty;

        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

        public CashTransactionDialog(TransactionType type, CashBox cashBox)
        {
            InitializeComponent();

            Title = type == TransactionType.Income ? "إضافة دخل للخزنة" : "إضافة مصروف من الخزنة";
            txtTransactionType.Text = type == TransactionType.Income ? "دخل" : "مصروف";
            txtTransactionType.Foreground = type == TransactionType.Income
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;

            txtCashBox.Text = cashBox.CashBoxName;

            Loaded += (_, __) => txtAmount.Focus();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            // استخدم نفس الثقافة في التحويل النهائي
            Amount = decimal.Parse(txtAmount.Text, NumberStyles.Number, _culture);
            Description = txtDescription.Text.Trim();
            CustomerSupplierName = txtCustomerSupplier.Text.Trim();
            Notes = txtNotes.Text.Trim();

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
            // المبلغ
            if (string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                MessageBox.Show("يرجى إدخال المبلغ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            // الوصف
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("يرجى إدخال وصف للحركة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescription.Focus();
                return false;
            }

            return true;
        }

        // السماح بالأرقام وعلامة الفاصل العشري حسب الثقافة + Backspace
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
