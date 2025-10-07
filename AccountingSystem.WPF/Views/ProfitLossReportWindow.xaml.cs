using System.Windows;

namespace AccountingSystem.WPF.Views
{
	public partial class ProfitLossReportWindow : Window
	{
		public ProfitLossReportWindow()
		{
			InitializeComponent();
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
