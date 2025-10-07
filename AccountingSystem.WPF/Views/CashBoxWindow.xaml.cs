using System.Windows;
using AccountingSystem.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views;

/// <summary>
/// Interaction logic for CashBoxWindow.xaml
/// </summary>
public partial class CashBoxWindow : Window
{
    public CashBoxWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = serviceProvider.GetRequiredService<CashBoxViewModel>();
    }
}
