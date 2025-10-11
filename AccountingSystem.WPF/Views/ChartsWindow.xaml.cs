using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.Business;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;

namespace AccountingSystem.WPF.Views;

public partial class ChartsWindow : Window
{
    private readonly DispatcherTimer _updateTimer;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly IReportService? _reportService;

    // LiveCharts Properties for example charts
    public ISeries[] SalesSeries { get; set; } = Array.Empty<ISeries>();
    public string[] SalesLabels { get; set; } = Array.Empty<string>();

    public ChartsWindow()
    {
        InitializeComponent();
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _updateTimer.Tick += UpdateCurrentDateTime;
        _updateTimer.Start();

        LoadSampleData();
        UpdateStatusInfo();
    }

    public ChartsWindow(IUnitOfWork unitOfWork, IReportService? reportService)
    {
        InitializeComponent();
        _unitOfWork = unitOfWork;
        _reportService = reportService;

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _updateTimer.Tick += UpdateCurrentDateTime;
        _updateTimer.Start();

        _ = LoadChartsDataAsync();
        UpdateStatusInfo();
    }

    private void UpdateCurrentDateTime(object? sender, EventArgs e)
    {
        UpdateStatusInfo();
    }

    private void UpdateStatusInfo()
    {
        var currentTime = DateTime.Now;
        Title = $"التقارير المرئية - {currentTime:yyyy/MM/dd - HH:mm:ss}";
    }

    private void LoadSampleData()
    {
        // بيانات تجريبية - ستُستبدل ببيانات حقيقية لاحقاً
        try
        {
            // يمكن إضافة تحميل بيانات تجريبية هنا
            UpdateStatusInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private System.Threading.Tasks.Task LoadChartsDataAsync()
    {
        return System.Threading.Tasks.Task.Run(() =>
        {
            // تحميل البيانات الحقيقية من قاعدة البيانات
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // تحديث الواجهة
                    UpdateStatusInfo();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        _updateTimer?.Stop();
        base.OnClosed(e);
    }

    // Event Handlers
    private void btnRefreshData_Click(object sender, RoutedEventArgs e)
    {
        LoadSampleData();
        MessageBox.Show("تم تحديث البيانات بنجاح", "تحديث البيانات",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnExportReports_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MessageBox.Show("سيتم تصدير التقارير قريباً", "تصدير التقارير",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تصدير التقارير: {ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnChartSettings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("إعدادات الرسوم البيانية ستتوفر قريباً", "الإعدادات",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void cmbTimePeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem != null)
        {
            // تطبيق تصفية حسب الفترة الزمنية
            LoadSampleData();
        }
    }

    private void cmbChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem != null)
        {
            // تطبيق نوع الرسم البياني المحدد
            LoadSampleData();
        }
    }

    private void cmbDataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem != null)
        {
            // تطبيق نوع البيانات المحدد
            LoadSampleData();
        }
    }

    private void btnLoadSampleData_Click(object sender, RoutedEventArgs e)
    {
        LoadSampleData();
        MessageBox.Show("تم تحميل البيانات التجريبية", "البيانات التجريبية",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // Report Buttons
    private void btnDailySalesReport_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("تقرير المبيعات اليومي سيتوفر قريباً", "التقارير",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnSalesTrendsAnalysis_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("تحليل اتجاهات المبيعات سيتوفر قريباً", "التحليل",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnTopProductsReport_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("تقرير أفضل المنتجات سيتوفر قريباً", "التقارير",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnInventoryStatusReport_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("تقرير حالة المخزون سيتوفر قريباً", "التقارير",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnLowStockAlerts_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("تنبيهات نقص المخزون ستتوفر قريباً", "التنبيهات",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnProductProfitabilityAnalysis_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("تحليل ربحية المنتجات سيتوفر قريباً", "التحليل",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnCustomerBehaviorAnalysis_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("تحليل سلوك العملاء سيتوفر قريباً", "التحليل",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnMonthlyPerformanceComparison_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("مقارنة الأداء الشهري ستتوفر قريباً", "التحليل",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnSeasonalTrendsAnalysis_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("تحليل الاتجاهات الموسمية سيتوفر قريباً", "التحليل",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnSalesForecast_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("توقعات المبيعات ستتوفر قريباً", "التوقعات",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnDemandPlanning_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("تخطيط الطلب سيتوفر قريباً", "التخطيط",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnInteractiveDashboard_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("اللوحة التفاعلية ستتوفر قريباً", "اللوحة التفاعلية",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
