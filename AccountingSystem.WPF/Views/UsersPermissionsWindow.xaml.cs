using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using AccountingSystem.Business;

namespace AccountingSystem.WPF.Views
{
    public partial class UsersPermissionsWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UsersPermissionsWindow> _logger;

        public UsersPermissionsWindow(IServiceProvider serviceProvider, ILogger<UsersPermissionsWindow> logger)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            try
            {
                LoadUsersAndPermissions();
                _logger.LogInformation("تم تحميل نافذة إدارة المستخدمين والصلاحيات بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل نافذة إدارة المستخدمين والصلاحيات");
                MessageBox.Show("حدث خطأ في تحميل البيانات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUsersAndPermissions()
        {
            try
            {
                // تحميل بيانات المستخدمين والصلاحيات
                // هنا يمكن تحميل المستخدمين ومجموعات الصلاحيات وسجل النشاطات
                _logger.LogInformation("تم تحميل بيانات المستخدمين والصلاحيات");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات المستخدمين والصلاحيات");
                throw;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("إغلاق نافذة إدارة المستخدمين والصلاحيات");
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إغلاق نافذة إدارة المستخدمين والصلاحيات");
            }
        }
    }
}