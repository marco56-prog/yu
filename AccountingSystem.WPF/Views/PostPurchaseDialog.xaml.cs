using AccountingSystem.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace AccountingSystem.WPF.Views
{
    public partial class PostPurchaseDialog : Window
    {
        private readonly CultureInfo _culture;
        public PurchaseInvoice Invoice { get; set; }
        
        public bool GetPrintInvoice() => chkPrintInvoice.IsChecked ?? false;
        public bool GetRecordPayment() => chkRecordPayment.IsChecked ?? false;
        public bool GetOpenNewInvoice() => chkOpenNewInvoice.IsChecked ?? false;
        public decimal GetPaymentAmount() => decimal.TryParse(txtPaymentAmount.Text, out var amount) ? amount : 0;

        public PostPurchaseDialog(PurchaseInvoice invoice)
        {
            InitializeComponent();
            Invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
            
            _culture = new CultureInfo("ar-EG") { NumberFormat = { CurrencySymbol = "ج.م" } };
            
            LoadInvoiceData();
        }

        private void LoadInvoiceData()
        {
            try
            {
                lblInvoiceNumber.Text = Invoice.InvoiceNumber ?? "غير محدد";
                lblSupplierName.Text = Invoice.Supplier?.SupplierName ?? "غير محدد";
                lblNetTotal.Text = Invoice.NetTotal.ToString("C", _culture);
                lblRemainingAmount.Text = Invoice.RemainingAmount.ToString("C", _culture);
                
                // تعيين المبلغ الافتراضي للدفع (كامل المتبقي)
                txtPaymentAmount.Text = Invoice.RemainingAmount.ToString("F2");
                UpdateRemainingAmount();
                
                // إخفاء قسم الدفع إذا كانت الفاتورة مسددة بالكامل
                if (Invoice.RemainingAmount <= 0)
                {
                    chkRecordPayment.IsChecked = false;
                    chkRecordPayment.IsEnabled = false;
                    grpPayment.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات الفاتورة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkRecordPayment_Checked(object sender, RoutedEventArgs e)
        {
            grpPayment.Visibility = Visibility.Visible;
            txtPaymentAmount.Focus();
        }

        private void chkRecordPayment_Unchecked(object sender, RoutedEventArgs e)
        {
            grpPayment.Visibility = Visibility.Collapsed;
        }

        private void txtPaymentAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRemainingAmount();
        }

        private void UpdateRemainingAmount()
        {
            try
            {
                if (decimal.TryParse(txtPaymentAmount.Text, out var paymentAmount))
                {
                    var newRemaining = Invoice.RemainingAmount - paymentAmount;
                    if (newRemaining < 0) newRemaining = 0;
                    
                    lblNewRemainingAmount.Text = newRemaining.ToString("C", _culture);
                    lblNewRemainingAmount.Foreground = newRemaining == 0 ? 
                        System.Windows.Media.Brushes.Green : 
                        System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    lblNewRemainingAmount.Text = Invoice.RemainingAmount.ToString("C", _culture);
                    lblNewRemainingAmount.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch
            {
                lblNewRemainingAmount.Text = "خطأ في الحساب";
                lblNewRemainingAmount.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void btnProceed_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من صحة البيانات
                if (GetRecordPayment())
                {
                    if (GetPaymentAmount() < 0)
                    {
                        MessageBox.Show("مبلغ الدفع لا يمكن أن يكون أقل من صفر", "تحذير", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtPaymentAmount.Focus();
                        return;
                    }

                    if (GetPaymentAmount() > Invoice.RemainingAmount)
                    {
                        var result = MessageBox.Show(
                            "مبلغ الدفع أكبر من المبلغ المتبقي. هل تريد المتابعة؟", 
                            "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.No)
                        {
                            txtPaymentAmount.Focus();
                            return;
                        }
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في المعالجة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}