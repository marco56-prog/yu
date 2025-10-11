using AccountingSystem.Models;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AccountingSystem.WPF.Views
{
    public partial class PostInvoiceDialog : Window
    {
        private readonly CultureInfo _culture;
        public SalesInvoice Invoice { get; set; }

        // نتيجة موحّدة بعد الإغلاق
        public PostInvoiceResult Result { get; private set; } = new();

        // getters القديمة (متروكة للتوافق)
        public bool GetPrintInvoice() => chkPrintInvoice?.IsChecked ?? false;
        public bool GetCollectPayment() => chkCollectPayment?.IsChecked ?? false;
        public bool GetOpenNewInvoice() => chkOpenNewInvoice?.IsChecked ?? false;
        public decimal GetPaymentAmount() => ParseDecimal(txtPaymentAmount?.Text);

        public PostInvoiceDialog(SalesInvoice invoice)
        {
            try
            {
                InitializeComponent();
                Invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));

                _culture = new CultureInfo("ar-EG");
                _culture.NumberFormat.CurrencySymbol = "ج.م";

                FlowDirection = FlowDirection.RightToLeft;
                Title = "عمليات ما بعد الحفظ - فاتورة رقم: " + invoice.InvoiceNumber;

                // منع الإدخال غير الرقمي + دعم لصق الأرقام فقط
                txtPaymentAmount.PreviewTextInput += TxtPaymentAmount_PreviewTextInput;
                DataObject.AddPastingHandler(txtPaymentAmount, TxtPaymentAmount_OnPaste);
                txtPaymentAmount.PreviewKeyDown += TxtPaymentAmount_PreviewKeyDown;
                txtPaymentAmount.LostFocus += TxtPaymentAmount_LostFocus;

                LoadInvoiceData();
                // تحديد الخيارات الافتراضية
                if (Invoice.RemainingAmount > 0)
                {
                    chkCollectPayment.IsChecked = true;
                    txtPaymentAmount.Text = Invoice.RemainingAmount.ToString("N2");
                }

                Loaded += (_, __) =>
                {
                    if (chkCollectPayment?.IsChecked == true)
                    {
                        txtPaymentAmount?.Focus();
                        txtPaymentAmount?.SelectAll();
                    }
                    else
                    {
                        btnProceed?.Focus();
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة ما بعد الحفظ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"PostInvoiceDialog constructor error: {ex}");
                throw;
            }
        }

        private void LoadInvoiceData()
        {
            try
            {
                if (lblInvoiceNumber == null || lblCustomerName == null || lblNetTotal == null ||
                    lblRemainingAmount == null || txtPaymentAmount == null)
                {
                    throw new InvalidOperationException("بعض عناصر الواجهة غير موجودة - تأكد من استدعاء InitializeComponent()");
                }

                lblInvoiceNumber.Text = string.IsNullOrWhiteSpace(Invoice.InvoiceNumber) ? "غير محدد" : Invoice.InvoiceNumber;
                lblCustomerName.Text = Invoice.Customer?.CustomerName ?? "غير محدد";
                lblNetTotal.Text = Invoice.NetTotal.ToString("C", _culture);
                lblRemainingAmount.Text = Invoice.RemainingAmount.ToString("C", _culture);

                // افتراضي: كامل المتبقي (لو فيه متبقي)
                var defaultPay = Math.Max(0m, Invoice.RemainingAmount);
                txtPaymentAmount.Text = defaultPay.ToString("0.##", CultureInfo.InvariantCulture);

                // إخفاء قسم التحصيل إذا مسددة بالكامل
                if (Invoice.RemainingAmount <= 0)
                {
                    chkCollectPayment.IsChecked = false;
                    chkCollectPayment.IsEnabled = false;
                    grpPayment.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // يظل ظاهر بناءً على اختيار المستخدم
                    grpPayment.Visibility = chkCollectPayment.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                }

                UpdateRemainingAmount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات الفاتورة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"LoadInvoiceData error: {ex}");
                throw;
            }
        }

        // ===== إدخال رقمي فقط (أرقام عربية/إنجليزية + فواصل ونقطة) =====
        private static readonly Regex _allowed = new(@"^[0-9\u0660-\u0669\u06F0-\u06F9\.\,\u066B\u066C\-]+$", RegexOptions.Compiled);

        private void TxtPaymentAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !_allowed.IsMatch(e.Text);

        private void TxtPaymentAmount_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // منع المسافة
            if (e.Key == Key.Space) e.Handled = true;
        }

        private void TxtPaymentAmount_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!IsNumericLike(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private static bool IsNumericLike(string? s)
            => !string.IsNullOrWhiteSpace(s) && Regex.IsMatch(s, @"^[0-9\u0660-\u0669\u06F0-\u06F9\.\,\u066B\u066C\-]+$");

        // ===== تطبيع وتحويل نص إلى decimal يدعم العربية =====
        private static string NormalizeToLatin(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "0";

            var s = input.Trim();

            // تحويل الأرقام العربية-الهندية والفارسية إلى لاتينية
            s = s
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9');

            // توحيد الفواصل العشرية إلى نقطة
            s = s.Replace('٫', '.').Replace('،', '.').Replace(',', '.');

            // إزالة أي رموز عملة أو مسافات زائدة
            s = Regex.Replace(s, @"[^\d\.\-]", "");

            return string.IsNullOrWhiteSpace(s) ? "0" : s;
        }

        private static decimal ParseDecimal(string? input)
        {
            var norm = NormalizeToLatin(input);
            return decimal.TryParse(norm, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var d)
                ? d : 0m;
        }

        // ===== أحداث الواجهة =====
        private void chkCollectPayment_Checked(object sender, RoutedEventArgs e)
        {
            grpPayment.Visibility = Visibility.Visible;

            // افتراضي: كامل المتبقي
            txtPaymentAmount.Text = Math.Max(0m, Invoice.RemainingAmount).ToString("0.##", CultureInfo.InvariantCulture);
            txtPaymentAmount.Focus();
            txtPaymentAmount.SelectAll();
            UpdateRemainingAmount();
        }

        private void chkCollectPayment_Unchecked(object sender, RoutedEventArgs e)
        {
            grpPayment.Visibility = Visibility.Collapsed;
            txtPaymentAmount.Text = "0";
            UpdateRemainingAmount();
        }

        private void txtPaymentAmount_TextChanged(object sender, TextChangedEventArgs e)
            => UpdateRemainingAmount();

        private void TxtPaymentAmount_LostFocus(object? sender, RoutedEventArgs e)
        {
            var val = Math.Max(0m, ParseDecimal(txtPaymentAmount.Text));
            txtPaymentAmount.Text = val.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void UpdateRemainingAmount()
        {
            if (lblNewRemainingAmount == null) return;

            var pay = Math.Max(0m, ParseDecimal(txtPaymentAmount?.Text));
            // للعرض فقط نقيد المتبقي بأقل من أو يساوي المتبقي الأصلي
            var newRemaining = Invoice.RemainingAmount - pay;
            if (newRemaining < 0) newRemaining = 0;

            lblNewRemainingAmount.Text = newRemaining.ToString("C", _culture);

            // تلوين دلالي
            lblNewRemainingAmount.Foreground =
                newRemaining == 0 ? Brushes.Green :
                pay == 0 ? Brushes.Red :
                Brushes.Orange;
        }

        private void btnProceed_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var collect = chkCollectPayment?.IsChecked == true;
                var pay = ParseDecimal(txtPaymentAmount?.Text);

                if (collect)
                {
                    if (pay < 0)
                    {
                        MessageBox.Show("مبلغ الدفع لا يمكن أن يكون أقل من صفر.", "تحذير",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtPaymentAmount?.Focus();
                        return;
                    }

                    if (pay > Invoice.RemainingAmount)
                    {
                        // السماح بالدفع الزائد باختيار المستخدم
                        var result = MessageBox.Show(
                            "مبلغ الدفع أكبر من المبلغ المتبقي. هل تريد المتابعة؟",
                            "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                        {
                            txtPaymentAmount?.Focus();
                            txtPaymentAmount?.SelectAll();
                            return;
                        }
                    }
                }
                else
                {
                    pay = 0m; // لا يوجد تحصيل
                }

                // املأ النتيجة الموحّدة
                Result = new PostInvoiceResult
                {
                    PrintInvoice = chkPrintInvoice?.IsChecked == true,
                    CollectPayment = collect,
                    OpenNewInvoice = chkOpenNewInvoice?.IsChecked == true,
                    PaymentAmount = pay,
                    NewRemaining = Math.Max(0m, Invoice.RemainingAmount - Math.Max(0m, pay))
                };

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
            Result = new PostInvoiceResult
            {
                PrintInvoice = false,
                CollectPayment = false,
                OpenNewInvoice = false,
                PaymentAmount = 0m,
                NewRemaining = Invoice?.RemainingAmount ?? 0m
            };

            DialogResult = false;
            Close();
        }
    }

    public class PostInvoiceResult
    {
        public bool PrintInvoice { get; set; }
        public bool CollectPayment { get; set; }
        public bool OpenNewInvoice { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal NewRemaining { get; set; }
    }
}
