using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// Interaction logic for BankAccountsWindow.xaml
    /// </summary>
    public partial class BankAccountsWindow : Window
    {
        // مجموعة كل الحسابات
        private readonly ObservableCollection<BankAccountItem> _allAccounts = new();
        // عرض قابل للتصفية/الفرز مرتبط بالجريد
        private ICollectionView? _accountsView;

        public BankAccountsWindow()
        {
            InitializeComponent();
            Loaded += BankAccountsWindow_Loaded;
        }

        private void BankAccountsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // تهيئة البيانات والعرض
            LoadSampleData();
            _accountsView = CollectionViewSource.GetDefaultView(_allAccounts);
            _accountsView.Filter = AccountsFilter;

            dgBankAccounts.ItemsSource = _accountsView;

            UpdateNoDataPanelVisibility();
        }

        #region Data & Filtering

        private void LoadSampleData()
        {
            // بيانات مبدئية للتجربة – تقدر تبدلها بتحميل فعلي من قاعدة البيانات/خدمة
            _allAccounts.Clear();

            _allAccounts.Add(new BankAccountItem
            {
                BankName = "البنك الأهلي",
                AccountNumber = "123-456-789",
                AccountName = "حساب الشركة الرئيسي",
                AccountType = "جاري",
                Currency = "₪",
                CurrentBalance = 150_000m,
                OpenDate = new DateTime(2022, 03, 01).ToString("yyyy/MM/dd"),
                Status = "نشط"
            });

            _allAccounts.Add(new BankAccountItem
            {
                BankName = "البنك العربي",
                AccountNumber = "987-654-321",
                AccountName = "حساب المصروفات",
                AccountType = "توفير",
                Currency = "₪",
                CurrentBalance = 42_750m,
                OpenDate = new DateTime(2023, 02, 10).ToString("yyyy/MM/dd"),
                Status = "نشط"
            });

            _allAccounts.Add(new BankAccountItem
            {
                BankName = "بنك فلسطين",
                AccountNumber = "555-888-222",
                AccountName = "حساب الرواتب",
                AccountType = "جاري",
                Currency = "₪",
                CurrentBalance = 18_000m,
                OpenDate = new DateTime(2021, 11, 20).ToString("yyyy/MM/dd"),
                Status = "مجمّد"
            });

            _allAccounts.Add(new BankAccountItem
            {
                BankName = "بنك القدس",
                AccountNumber = "222-111-333",
                AccountName = "حساب الطوارئ",
                AccountType = "توفير",
                Currency = "₪",
                CurrentBalance = 275_000m,
                OpenDate = new DateTime(2020, 07, 05).ToString("yyyy/MM/dd"),
                Status = "نشط"
            });

            // تحديث كروت الملخص (لو حابب تربطها بديناميكيات، هنا حساب بسيط سريع)
            // ملاحظة: الكروت في XAML حالياً نصوص ثابتة، سيبها زي ما هي أو اربطها لو هتحدّثها دYNAMIC
        }

        private bool AccountsFilter(object obj)
        {
            if (obj is not BankAccountItem item) return false;

            // نص البحث
            var q = (txtSearch?.Text ?? string.Empty).Trim();
            var hasQuery = !string.IsNullOrWhiteSpace(q);

            // فلتر البنك
            var bankFilter = (cmbBankFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "جميع البنوك";
            var matchBank = bankFilter == "جميع البنوك" || string.Equals(item.BankName, bankFilter, StringComparison.OrdinalIgnoreCase);

            // فلترة بالنص: البنك أو رقم الحساب أو اسم الحساب
            var matchQuery = true;
            if (hasQuery)
            {
                var lower = q.ToLower();
                matchQuery =
                    (item.BankName?.ToLower().Contains(lower) ?? false) ||
                    (item.AccountNumber?.ToLower().Contains(lower) ?? false) ||
                    (item.AccountName?.ToLower().Contains(lower) ?? false);
            }

            return matchBank && matchQuery;
        }

        private void RefreshFilter()
        {
            _accountsView?.Refresh();
            UpdateNoDataPanelVisibility();
        }

        private void UpdateNoDataPanelVisibility()
        {
            var hasAny = _accountsView?.Cast<object>().Any() ?? _allAccounts.Any();
            if (pnlNoData != null)
                pnlNoData.Visibility = hasAny ? Visibility.Collapsed : Visibility.Visible;
        }

        #endregion

        #region Event Handlers (names unchanged)

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            RefreshFilter();
        }

        private void btnNewBankAccount_Click(object sender, RoutedEventArgs e)
        {
            // هنا تفتح نافذة إضافة حساب أو تعرض فورم – عاملناها إشعار بسيط مؤقتاً
            MessageBox.Show("فتح نموذج إضافة حساب مصرفي جديد (Placeholder).", "حساب جديد", MessageBoxButton.OK, MessageBoxImage.Information);

            // مثال: إضافة حساب جديد سريع للتجربة
            var next = new BankAccountItem
            {
                BankName = "البنك الأهلي",
                AccountNumber = $"AC-{DateTime.Now:HHmmss}",
                AccountName = "حساب جديد",
                AccountType = "جاري",
                Currency = "₪",
                CurrentBalance = 0m,
                OpenDate = DateTime.Now.ToString("yyyy/MM/dd"),
                Status = "نشط"
            };
            _allAccounts.Add(next);
            RefreshFilter();
        }

        private void btnBankTransfer_Click(object sender, RoutedEventArgs e)
        {
            // هنا تفتح نافذة تحويل بين الحسابات – عاملناها إشعار مؤقت
            MessageBox.Show("فتح نموذج التحويل البنكي (Placeholder).", "تحويل بنكي", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Helper model (simple row DTO for the grid)

        // نموذج بسيط لعنصر في الجريد مطابق لأسماء الأعمدة في XAML
        private sealed class BankAccountItem
        {
            public string? BankName { get; set; }
            public string? AccountNumber { get; set; }
            public string? AccountName { get; set; }
            public string? AccountType { get; set; }
            public string? Currency { get; set; }
            public decimal CurrentBalance { get; set; }
            public string? OpenDate { get; set; }   // مهيأة كنص yyyy/MM/dd زي ما الجريد يعرض
            public string? Status { get; set; }
        }

        #endregion
    }
}
