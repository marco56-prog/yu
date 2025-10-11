using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using AccountingSystem.Business;

namespace AccountingSystem.WPF.Views
{
    public partial class LoyaltyProgramWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LoyaltyProgramWindow> _logger;

        public LoyaltyProgramWindow(IServiceProvider serviceProvider, ILogger<LoyaltyProgramWindow> logger)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                LoadLoyaltyProgramData();
                _logger.LogInformation("تم تحميل نافذة برنامج الولاء بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل نافذة برنامج الولاء");
                MessageBox.Show("حدث خطأ في تحميل البيانات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLoyaltyProgramData()
        {
            try
            {
                // تحميل بيانات برنامج الولاء
                // هنا يمكن تحميل بيانات العملاء والنقاط والمكافآت
                _logger.LogInformation("تم تحميل بيانات برنامج الولاء");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات برنامج الولاء");
                throw;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("إغلاق نافذة برنامج الولاء");
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إغلاق نافذة برنامج الولاء");
            }
        }
    }
}