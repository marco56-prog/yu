using System.Windows;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views;

public partial class CategoryEditWindow : Window
{
    public Category Result => (Category)DataContext;

    public CategoryEditWindow(Category model)
    {
        InitializeComponent();
        DataContext = model ?? new Category { CategoryName = "" };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Result.CategoryName))
        {
            MessageBox.Show("اسم الفئة مطلوب.", "تنبيه");
            return;
        }
        DialogResult = true;
        Close();
    }
    
    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        Save_Click(sender, e);
    }
}