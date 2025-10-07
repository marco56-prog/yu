using System.Windows;
using AccountingSystem.WPF.ViewModels;

namespace AccountingSystem.WPF.Views
{
    public partial class NotificationCenterWindow : Window
    {
        public NotificationCenterWindow(NotificationCenterViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is NotificationCenterViewModel viewModel)
            {
                await viewModel.LoadNotificationsAsync();
            }
        }
    }
}