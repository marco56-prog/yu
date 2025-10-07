using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AccountingSystem.WPF.Views;

/// <summary>
/// نافذة عروض الأسعار
/// </summary>
public partial class QuotationsWindow : Window
{
    public ObservableCollection<QuotationViewModel> Quotations { get; set; }

    public QuotationsWindow()
    {
        InitializeComponent();
        
        Quotations = new ObservableCollection<QuotationViewModel>();
        LoadSampleData();
        
        lvQuotations.ItemsSource = Quotations;
        
        // إخفاء رسالة "لا توجد بيانات" إذا كانت هناك بيانات
        if (Quotations.Count > 0)
        {
            pnlNoData.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadSampleData()
    {
        // بيانات تجريبية لعروض الأسعار
        Quotations.Add(new QuotationViewModel
        {
            QuotationNumber = "Q-2024-001",
            CustomerName = "أحمد محمد عبد الله",
            Date = "2024-09-20",
            ExpiryDate = "2024-10-20",
            TotalAmount = "₪ 15,750",
            Status = "في الانتظار",
            StatusColor = new SolidColorBrush(Colors.Orange)
        });

        Quotations.Add(new QuotationViewModel
        {
            QuotationNumber = "Q-2024-002", 
            CustomerName = "مريم أحمد سالم",
            Date = "2024-09-18",
            ExpiryDate = "2024-10-18",
            TotalAmount = "₪ 8,500",
            Status = "مقبول",
            StatusColor = new SolidColorBrush(Colors.Green)
        });

        Quotations.Add(new QuotationViewModel
        {
            QuotationNumber = "Q-2024-003",
            CustomerName = "نور الدين عبد الرحمن",
            Date = "2024-09-15",
            ExpiryDate = "2024-09-15",
            TotalAmount = "₪ 22,300",
            Status = "منتهي الصلاحية",
            StatusColor = new SolidColorBrush(Colors.Red)
        });
    }

    private void btnNewQuotation_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم فتح نافذة إنشاء عرض أسعار جديد قريباً!", 
            "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox?.Text == "البحث في عروض الأسعار...")
        {
            textBox.Text = "";
            textBox.Foreground = new SolidColorBrush(Colors.Black);
        }
    }

    private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (string.IsNullOrWhiteSpace(textBox?.Text))
        {
            textBox!.Text = "البحث في عروض الأسعار...";
            textBox.Foreground = new SolidColorBrush(Colors.Gray);
        }
    }
}

public class QuotationViewModel
{
    public string QuotationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Brush StatusColor { get; set; } = new SolidColorBrush(Colors.Gray);
}