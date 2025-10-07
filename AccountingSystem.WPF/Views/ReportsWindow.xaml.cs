using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    public partial class ReportsWindow : Window
    {
        public ReportsWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}