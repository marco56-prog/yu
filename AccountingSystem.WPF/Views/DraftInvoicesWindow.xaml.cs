using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using AccountingSystem.Business;

namespace AccountingSystem.WPF.Views
{
    public partial class DraftInvoicesWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DraftInvoicesWindow> _logger;

        public DraftInvoicesWindow(IServiceProvider serviceProvider, ILogger<DraftInvoicesWindow> logger)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                LoadDraftInvoices();
                _logger.LogInformation("تم تحميل نافذة الفواتير المحفوظة بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل نافذة الفواتير المحفوظة");
                MessageBox.Show("حدث خطأ في تحميل البيانات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDraftInvoices()
        {
            try
            {
                // تحميل بيانات الفواتير المحفوظة
                // هنا يمكن تحميل الفواتير المحفوظة من قاعدة البيانات
                _logger.LogInformation("تم تحميل الفواتير المحفوظة");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الفواتير المحفوظة");
                throw;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("إغلاق نافذة الفواتير المحفوظة");
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إغلاق نافذة الفواتير المحفوظة");
            }
        }
    }
}
