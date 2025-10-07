using System;
using System.Windows;
using System.IO;

namespace AccountingSystem.WPF
{
    public static class Program
    {
        [STAThread]
        public static int Main()
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                return app.Run();
            }
            catch (Exception ex)
            {
                try
                {
                    var errorMsg = $"خطأ في Main: {ex.Message}\n\nFull Exception:\n{ex}";
                    
                    // محاولة كتابة الخطأ في ملف
                    var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    Directory.CreateDirectory(logsDir);
                    File.WriteAllText(Path.Combine(logsDir, "main_error.log"), errorMsg);
                    
                    // عرض رسالة خطأ
                    MessageBox.Show(errorMsg, "خطأ في تشغيل التطبيق", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch
                {
                    // حتى لو فشل عرض الخطأ
                }
                
                return 1;
            }
        }
    }
}
