using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Data;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// نافذة تشخيصية للتحقق من عمل النظام
    /// </summary>
    public partial class DiagnosticWindow : Window
    {
        public DiagnosticWindow()
        {
            InitializeComponent();
            LoadDiagnosticInfo();
        }

        private void LoadDiagnosticInfo()
        {
            try
            {
                var info = $"""
                🔧 تشخيص النظام المحاسبي
                ========================
                📅 الوقت: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                💻 نظام التشغيل: {Environment.OSVersion}
                🏠 مجلد التطبيق: {AppDomain.CurrentDomain.BaseDirectory}
                📁 مجلد السجلات: {System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AccountingSystem")}
                🌐 الثقافة: {System.Globalization.CultureInfo.CurrentCulture.DisplayName}
                🔄 الاتجاه: {(FlowDirection == FlowDirection.RightToLeft ? "من اليمين إلى اليسار" : "من اليسار إلى اليمين")}
                ⚙️ NET Version: {Environment.Version}
                
                📊 حالة النظام: جاهز للاختبار
                """;

                txtDiagnosticInfo.Text = info;
            }
            catch (Exception ex)
            {
                txtDiagnosticInfo.Text = $"خطأ في تحميل معلومات التشخيص: {ex.Message}";
            }
        }

        private void TestMainWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
                txtDiagnosticInfo.Text += $"\n\n✅ تم فتح MainWindow بنجاح في {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                txtDiagnosticInfo.Text += $"\n\n❌ فشل في فتح MainWindow: {ex.Message}";
            }
        }

        private void TestDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var scope = App.ServiceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
                var canConnect = dbContext.Database.CanConnect();
                txtDiagnosticInfo.Text += $"\n\n{(canConnect ? "✅" : "❌")} اختبار قاعدة البيانات: {(canConnect ? "متصلة" : "غير متصلة")} - {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                txtDiagnosticInfo.Text += $"\n\n❌ خطأ في اختبار قاعدة البيانات: {ex.Message}";
            }
        }

        private void RefreshDiagnostic_Click(object sender, RoutedEventArgs e)
        {
            LoadDiagnosticInfo();
            txtDiagnosticInfo.Text += $"\n\n🔄 تم تحديث التشخيص في {DateTime.Now:HH:mm:ss}";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}