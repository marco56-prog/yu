using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AccountingSystem.Models;
using AccountingSystem.Business;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    public partial class DiscountManagementWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDiscountService _discountService;

        public ObservableCollection<DiscountViewModel> Discounts { get; } = new();
        public ObservableCollection<PromoOfferViewModel> Offers { get; } = new();

        public DiscountManagementWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _discountService = _serviceProvider.GetRequiredService<IDiscountService>();

            DataContext = this;
            Loaded += async (_, __) => await LoadDataAsync();
        }

        private void btnAddDiscount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = Microsoft.VisualBasic.Interaction.InputBox(
                    "ادخل اسم الخصم الجديد:",
                    "إضافة خصم جديد",
                    "خصم جديد");

                if (!string.IsNullOrEmpty(result))
                {
                    var percentageResult = Microsoft.VisualBasic.Interaction.InputBox(
                        "ادخل نسبة الخصم (%):",
                        "نسبة الخصم",
                        "10");

                    if (!string.IsNullOrEmpty(percentageResult) && decimal.TryParse(percentageResult, out decimal discountPercentage))
                    {
                        if (discountPercentage >= 0 && discountPercentage <= 100)
                        {
                            Discounts.Add(new DiscountViewModel
                            {
                                Name = result,
                                Type = "نسبة مئوية",
                                Value = $"{discountPercentage}%",
                                StartDate = DateTime.Now,
                                EndDate = DateTime.Now.AddMonths(1),
                                Status = "نشط",
                                UsageCount = 0
                            });

                            MessageBox.Show($"تم إضافة خصم '{result}' بنسبة {discountPercentage}% بنجاح",
                                "نجح العملية", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("نسبة الخصم يجب أن تكون بين 0 و 100",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة الخصم: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnManageOffers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("هل تريد إضافة عرض جديد؟",
                    "إدارة العروض", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var offerName = Microsoft.VisualBasic.Interaction.InputBox(
                        "ادخل اسم العرض:",
                        "عرض جديد",
                        "عرض خاص");

                    if (!string.IsNullOrEmpty(offerName))
                    {
                        Offers.Add(new PromoOfferViewModel
                        {
                            Title = offerName,
                            Description = $"وصف العرض: {offerName}",
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now.AddDays(30),
                            Status = "نشط",
                            Type = "عرض خاص"
                        });

                        MessageBox.Show($"تم إضافة العرض '{offerName}' بنجاح",
                            "نجح العملية", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إدارة العروض: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load sample discount data
                await LoadDiscountsAsync();
                await LoadOffersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDiscountsAsync()
        {
            // Simulate loading discounts
            await Task.Delay(100);

            Discounts.Clear();
            Discounts.Add(new DiscountViewModel
            {
                Name = "خصم العملاء الجدد",
                Type = "نسبة مئوية",
                Value = "15%",
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now.AddDays(30),
                Status = "نشط",
                UsageCount = 156
            });

            Discounts.Add(new DiscountViewModel
            {
                Name = "خصم الكمية",
                Type = "مبلغ ثابت",
                Value = "50 ج.م",
                StartDate = DateTime.Now.AddDays(-15),
                EndDate = DateTime.Now.AddDays(45),
                Status = "نشط",
                UsageCount = 89
            });
        }

        private async Task LoadOffersAsync()
        {
            // Simulate loading promotional offers
            await Task.Delay(100);

            Offers.Clear();
            Offers.Add(new PromoOfferViewModel
            {
                Title = "عرض نهاية الأسبوع",
                Description = "خصم 25% على جميع المنتجات",
                Type = "عرض موسمي",
                StartDate = DateTime.Now.AddDays(-7),
                EndDate = DateTime.Now.AddDays(2),
                Status = "نشط"
            });
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddDiscount_Click(object sender, RoutedEventArgs e)
        {
            btnAddDiscount_Click(sender, e);
        }

        private void ManageOffer_Click(object sender, RoutedEventArgs e)
        {
            btnManageOffers_Click(sender, e);
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadDataAsync();
                MessageBox.Show("تم تحديث البيانات بنجاح", "تحديث البيانات",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEditDiscount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // يمكن إضافة منطق لاختيار الخصم المحدد من الجدول
                var selectedDiscount = Discounts.FirstOrDefault();
                if (selectedDiscount != null)
                {
                    var newName = Microsoft.VisualBasic.Interaction.InputBox(
                        $"تعديل اسم الخصم:",
                        "تعديل الخصم",
                        selectedDiscount.Name);

                    if (!string.IsNullOrEmpty(newName))
                    {
                        selectedDiscount.Name = newName;
                        MessageBox.Show("تم تعديل الخصم بنجاح", "نجح العملية",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("لا توجد خصومات للتعديل", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تعديل الخصم: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteDiscount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedDiscount = Discounts.FirstOrDefault();
                if (selectedDiscount != null)
                {
                    var result = MessageBox.Show($"هل تريد حذف الخصم '{selectedDiscount.Name}'؟",
                        "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Discounts.Remove(selectedDiscount);
                        MessageBox.Show("تم حذف الخصم بنجاح", "تم الحذف",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("لا توجد خصومات للحذف", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حذف الخصم: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportDiscounts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("هل تريد تصدير جميع الخصومات؟",
                    "تصدير الخصومات", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // محاكاة عملية التصدير
                    MessageBox.Show($"تم تصدير {Discounts.Count} خصم بنجاح\n\nالملف: C:\\Reports\\Discounts_Export.xlsx",
                        "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnApplyDiscount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedDiscount = Discounts.FirstOrDefault(d => d.Status == "نشط");
                if (selectedDiscount != null)
                {
                    selectedDiscount.UsageCount++;
                    MessageBox.Show($"تم تطبيق الخصم '{selectedDiscount.Name}'\nعدد مرات الاستخدام: {selectedDiscount.UsageCount}",
                        "تم التطبيق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("لا توجد خصومات نشطة للتطبيق", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تطبيق الخصم: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // View Models للبيانات
    public class DiscountViewModel
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Value { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "";
        public int UsageCount { get; set; }

        public string FormattedStartDate => StartDate.ToString("dd/MM/yyyy");
        public string FormattedEndDate => EndDate.ToString("dd/MM/yyyy");
    }

    public class PromoOfferViewModel
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "";

        public string FormattedDateRange => $"{StartDate:dd/MM} - {EndDate:dd/MM}";
    }
}