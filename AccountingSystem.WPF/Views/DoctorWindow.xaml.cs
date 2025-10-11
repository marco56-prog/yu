using System.Windows;
using System.Windows.Input;
using AccountingSystem.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.WPF.Views
{
    public partial class DoctorWindow : Window
    {
        private readonly BaseViewModel _viewModel;
        private readonly ILogger<DoctorWindow> _logger;

        public DoctorWindow(BaseViewModel viewModel, ILogger<DoctorWindow> logger)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _logger = logger;
            DataContext = _viewModel;

            _logger.LogInformation("نافذة طبيب النظام تم إنشاؤها بنجاح");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("إغلاق نافذة طبيب النظام");
                Close();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إغلاق نافذة طبيب النظام");
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // اختصارات لوحة المفاتيح
            if (e.Key == Key.F5)
            {
                if (_viewModel is SimpleDoctorViewModel doctorVm)
                {
                    doctorVm.RefreshCommand.Execute(null);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);

            // تطبيق إعدادات RTL
            FlowDirection = FlowDirection.RightToLeft;
        }
    }
}