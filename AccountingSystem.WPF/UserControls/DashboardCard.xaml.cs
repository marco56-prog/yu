using System.Windows.Controls;
using AccountingSystem.WPF.ViewModels;

namespace AccountingSystem.WPF.UserControls
{
    public partial class DashboardCard : UserControl
    {
        public DashboardCard()
        {
            InitializeComponent();
        }

        public DashboardCard(DashboardCardViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}