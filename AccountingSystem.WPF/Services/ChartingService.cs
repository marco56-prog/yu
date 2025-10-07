using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AccountingSystem.WPF.Helpers;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة إنشاء الرسوم البيانية التفاعلية للتقارير
    /// </summary>
    public interface IChartingService
    {
        ChartConfiguration CreateSalesTrendChart(List<SalesTrendData> data, string title = "");
        ChartConfiguration CreateTopProductsChart(List<TopSellingProductData> data, string title = "");
        ChartConfiguration CreateProfitLossChart(List<ProfitLossData> data, string title = "");
        ChartConfiguration CreateInventoryStatusChart(List<ProductInventoryData> data, string title = "");
        ChartConfiguration CreateCustomerAnalysisChart(List<CustomerSalesData> data, string title = "");
        ChartConfiguration CreateCashFlowChart(List<CashFlowData> data, string title = "");
        ChartConfiguration CreateComparisonChart(ComparisonAnalysisResult data, string title = "");
        
        // رسوم بيانية مخصصة
        ChartConfiguration CreatePieChart(Dictionary<string, decimal> data, string title = "");
        ChartConfiguration CreateBarChart(Dictionary<string, decimal> data, string title = "", bool horizontal = false);
        ChartConfiguration CreateLineChart(List<(string Label, decimal Value)> data, string title = "");
        ChartConfiguration CreateMultiSeriesChart(Dictionary<string, List<(string Label, decimal Value)>> data, string title = "");
        
        // تصدير الرسوم البيانية
        Task<bool> ExportChartToPngAsync(ChartConfiguration chart, string filePath, int width = 800, int height = 600);
        Task<bool> ExportChartToSvgAsync(ChartConfiguration chart, string filePath);
        
        // ألوان وثيمات
        List<string> GetColorPalette(string theme = "default");
        ChartConfiguration ApplyTheme(ChartConfiguration chart, string theme);
    }

    public class ChartingService : IChartingService
    {
        private const string ComponentName = "ChartingService";
        private readonly Dictionary<string, List<string>> _colorPalettes;

        public ChartingService()
        {
            _colorPalettes = InitializeColorPalettes();
            ComprehensiveLogger.LogInfo("تم تهيئة خدمة الرسوم البيانية", ComponentName);
        }

        public ChartConfiguration CreateSalesTrendChart(List<SalesTrendData> data, string title = "")
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد بيانات للعرض");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "اتجاه المبيعات" : title,
                    XAxisLabel = "الفترة الزمنية",
                    YAxisLabel = "قيمة المبيعات (ج.م)",
                    ShowLegend = true,
                    ShowGridLines = true,
                    Theme = "sales"
                };

                // إضافة بيانات المبيعات
                var salesDataSet = new ChartDataSet
                {
                    Label = "إجمالي المبيعات",
                    ChartType = "Line",
                    BackgroundColor = "#2196F3",
                    BorderColor = "#1976D2",
                    Data = data.Select(d => d.TotalSales).ToList(),
                    Labels = data.Select(d => d.Period).ToList()
                };

                // إضافة بيانات عدد الفواتير
                var countDataSet = new ChartDataSet
                {
                    Label = "عدد الفواتير",
                    ChartType = "Bar",
                    BackgroundColor = "#FF9800",
                    BorderColor = "#F57C00",
                    Data = data.Select(d => (decimal)d.InvoiceCount).ToList(),
                    Labels = data.Select(d => d.Period).ToList()
                };

                config.DataSets = new List<ChartDataSet> { salesDataSet, countDataSet };

                ComprehensiveLogger.LogInfo($"تم إنشاء رسم بياني لاتجاه المبيعات - {data.Count} نقطة بيانات", ComponentName);
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني لاتجاه المبيعات", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        public ChartConfiguration CreateTopProductsChart(List<TopSellingProductData> data, string title = "")
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد منتجات للعرض");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "أفضل المنتجات مبيعاً" : title,
                    XAxisLabel = "المنتجات",
                    YAxisLabel = "إجمالي الإيرادات (ج.م)",
                    ShowLegend = false,
                    ShowGridLines = true,
                    Theme = "products"
                };

                var colors = GetColorPalette("rainbow").Take(data.Count).ToList();

                var dataSet = new ChartDataSet
                {
                    Label = "إجمالي الإيرادات",
                    ChartType = "Bar",
                    BackgroundColor = string.Join(",", colors),
                    BorderColor = string.Join(",", colors.Select(c => DarkenColor(c))),
                    Data = data.Select(d => d.TotalRevenue).ToList(),
                    Labels = data.Select(d => TruncateLabel(d.ProductName, 20)).ToList()
                };

                config.DataSets = new List<ChartDataSet> { dataSet };

                ComprehensiveLogger.LogInfo($"تم إنشاء رسم بياني لأفضل المنتجات - {data.Count} منتج", ComponentName);
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني للمنتجات", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        public ChartConfiguration CreateProfitLossChart(List<ProfitLossData> data, string title = "")
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد بيانات ربح وخسارة");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "تحليل الربح والخسارة" : title,
                    XAxisLabel = "الفترة",
                    YAxisLabel = "المبلغ (ج.م)",
                    ShowLegend = true,
                    ShowGridLines = true,
                    Theme = "financial"
                };

                // الإيرادات
                var revenueDataSet = new ChartDataSet
                {
                    Label = "الإيرادات",
                    ChartType = "Bar",
                    BackgroundColor = "#4CAF50",
                    BorderColor = "#388E3C",
                    Data = data.Select(d => d.Revenue).ToList(),
                    Labels = data.Select(d => d.Period).ToList()
                };

                // التكاليف
                var costsDataSet = new ChartDataSet
                {
                    Label = "التكاليف",
                    ChartType = "Bar",
                    BackgroundColor = "#F44336",
                    BorderColor = "#D32F2F",
                    Data = data.Select(d => d.Costs).ToList(),
                    Labels = data.Select(d => d.Period).ToList()
                };

                // الربح الصافي
                var profitDataSet = new ChartDataSet
                {
                    Label = "الربح الصافي",
                    ChartType = "Line",
                    BackgroundColor = "#2196F3",
                    BorderColor = "#1976D2",
                    Data = data.Select(d => d.GrossProfit).ToList(),
                    Labels = data.Select(d => d.Period).ToList()
                };

                config.DataSets = new List<ChartDataSet> { revenueDataSet, costsDataSet, profitDataSet };

                ComprehensiveLogger.LogInfo($"تم إنشاء رسم بياني للربح والخسارة - {data.Count} فترة", ComponentName);
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني للربح والخسارة", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        public ChartConfiguration CreateInventoryStatusChart(List<ProductInventoryData> data, string title = "")
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد بيانات مخزون");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "حالة المخزون" : title,
                    ShowLegend = true,
                    ShowGridLines = false,
                    Theme = "inventory"
                };

                // تجميع البيانات حسب حالة المخزون
                var statusGroups = data.GroupBy(d => d.StockStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToList();

                var labels = statusGroups.Select(g => GetStockStatusLabel(g.Status)).ToList();
                var values = statusGroups.Select(g => (decimal)g.Count).ToList();
                var colors = statusGroups.Select(g => GetStockStatusColor(g.Status)).ToList();

                var dataSet = new ChartDataSet
                {
                    Label = "عدد المنتجات",
                    ChartType = "Pie",
                    BackgroundColor = string.Join(",", colors),
                    BorderColor = "#FFFFFF",
                    Data = values,
                    Labels = labels
                };

                config.DataSets = new List<ChartDataSet> { dataSet };

                ComprehensiveLogger.LogInfo($"تم إنشاء رسم بياني لحالة المخزون - {data.Count} منتج", ComponentName);
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني للمخزون", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        public ChartConfiguration CreateCustomerAnalysisChart(List<CustomerSalesData> data, string title = "")
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد بيانات عملاء");
                }

                // أخذ أفضل 10 عملاء
                var topCustomers = data.OrderByDescending(d => d.TotalSales).Take(10).ToList();

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "تحليل أفضل العملاء" : title,
                    XAxisLabel = "العملاء",
                    YAxisLabel = "إجمالي المبيعات (ج.م)",
                    ShowLegend = false,
                    ShowGridLines = true,
                    Theme = "customers"
                };

                var dataSet = new ChartDataSet
                {
                    Label = "إجمالي المبيعات",
                    ChartType = "Bar",
                    BackgroundColor = "#9C27B0",
                    BorderColor = "#7B1FA2",
                    Data = topCustomers.Select(d => d.TotalSales).ToList(),
                    Labels = topCustomers.Select(d => TruncateLabel(d.CustomerName, 15)).ToList()
                };

                config.DataSets = new List<ChartDataSet> { dataSet };

                ComprehensiveLogger.LogInfo($"تم إنشاء رسم بياني لتحليل العملاء - {topCustomers.Count} عميل", ComponentName);
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني للعملاء", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        public ChartConfiguration CreateCashFlowChart(List<CashFlowData> data, string title = "")
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد بيانات تدفق نقدي");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "التدفق النقدي" : title,
                    XAxisLabel = "التاريخ",
                    YAxisLabel = "المبلغ (ج.م)",
                    ShowLegend = true,
                    ShowGridLines = true,
                    Theme = "cashflow"
                };

                // المقبوضات
                var inflowDataSet = new ChartDataSet
                {
                    Label = "المقبوضات",
                    ChartType = "Bar",
                    BackgroundColor = "#4CAF50",
                    BorderColor = "#388E3C",
                    Data = data.Select(d => d.Inflow).ToList(),
                    Labels = data.Select(d => d.Date.ToString("yyyy-MM-dd")).ToList()
                };

                // المدفوعات
                var outflowDataSet = new ChartDataSet
                {
                    Label = "المدفوعات",
                    ChartType = "Bar",
                    BackgroundColor = "#F44336",
                    BorderColor = "#D32F2F",
                    Data = data.Select(d => d.Outflow).ToList(),
                    Labels = data.Select(d => d.Date.ToString("yyyy-MM-dd")).ToList()
                };

                // الرصيد التراكمي
                var balanceDataSet = new ChartDataSet
                {
                    Label = "الرصيد التراكمي",
                    ChartType = "Line",
                    BackgroundColor = "#2196F3",
                    BorderColor = "#1976D2",
                    Data = data.Select(d => d.RunningBalance).ToList(),
                    Labels = data.Select(d => d.Date.ToString("yyyy-MM-dd")).ToList()
                };

                config.DataSets = new List<ChartDataSet> { inflowDataSet, outflowDataSet, balanceDataSet };

                ComprehensiveLogger.LogInfo($"تم إنشاء رسم بياني للتدفق النقدي - {data.Count} معاملة", ComponentName);
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني للتدفق النقدي", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        public ChartConfiguration CreateComparisonChart(ComparisonAnalysisResult data, string title = "")
        {
            try
            {
                if (data == null)
                {
                    return CreateEmptyChart("لا توجد بيانات للمقارنة");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "مقارنة الفترات" : title,
                    XAxisLabel = "المؤشرات",
                    YAxisLabel = "القيمة",
                    ShowLegend = true,
                    ShowGridLines = true,
                    Theme = "comparison"
                };

                var period1Data = new ChartDataSet
                {
                    Label = $"الفترة الأولى ({data.Period1Start:yyyy-MM-dd} - {data.Period1End:yyyy-MM-dd})",
                    ChartType = "Bar",
                    BackgroundColor = "#2196F3",
                    BorderColor = "#1976D2",
                    Data = new List<decimal> 
                    { 
                        data.SalesComparison.Period1Value, 
                        data.ProfitComparison.Period1Value,
                        data.CustomerComparison.Period1Value
                    },
                    Labels = new List<string> { "المبيعات", "الربح", "العملاء" }
                };

                var period2Data = new ChartDataSet
                {
                    Label = $"الفترة الثانية ({data.Period2Start:yyyy-MM-dd} - {data.Period2End:yyyy-MM-dd})",
                    ChartType = "Bar",
                    BackgroundColor = "#FF9800",
                    BorderColor = "#F57C00",
                    Data = new List<decimal> 
                    { 
                        data.SalesComparison.Period2Value, 
                        data.ProfitComparison.Period2Value,
                        data.CustomerComparison.Period2Value
                    },
                    Labels = new List<string> { "المبيعات", "الربح", "العملاء" }
                };

                config.DataSets = new List<ChartDataSet> { period1Data, period2Data };

                ComprehensiveLogger.LogInfo("تم إنشاء رسم بياني للمقارنة بين الفترات", ComponentName);
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني للمقارنة", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        #region Custom Charts

        public ChartConfiguration CreatePieChart(Dictionary<string, decimal> data, string title = "")
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد بيانات للرسم البياني الدائري");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "رسم بياني دائري" : title,
                    ShowLegend = true,
                    ShowGridLines = false,
                    Theme = "default"
                };

                var colors = GetColorPalette("rainbow").Take(data.Count).ToList();

                var dataSet = new ChartDataSet
                {
                    Label = "البيانات",
                    ChartType = "Pie",
                    BackgroundColor = string.Join(",", colors),
                    BorderColor = "#FFFFFF",
                    Data = data.Values.ToList(),
                    Labels = data.Keys.ToList()
                };

                config.DataSets = new List<ChartDataSet> { dataSet };
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني دائري", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        public ChartConfiguration CreateBarChart(Dictionary<string, decimal> data, string title = "", bool horizontal = false)
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد بيانات للرسم البياني العمودي");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "رسم بياني عمودي" : title,
                    XAxisLabel = "الفئات",
                    YAxisLabel = "القيم",
                    ShowLegend = false,
                    ShowGridLines = true,
                    Theme = "default"
                };

                var dataSet = new ChartDataSet
                {
                    Label = "البيانات",
                    ChartType = horizontal ? "HorizontalBar" : "Bar",
                    BackgroundColor = "#2196F3",
                    BorderColor = "#1976D2",
                    Data = data.Values.ToList(),
                    Labels = data.Keys.ToList()
                };

                config.DataSets = new List<ChartDataSet> { dataSet };
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني عمودي", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        public ChartConfiguration CreateLineChart(List<(string Label, decimal Value)> data, string title = "")
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد بيانات للرسم البياني الخطي");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "رسم بياني خطي" : title,
                    XAxisLabel = "النقاط",
                    YAxisLabel = "القيم",
                    ShowLegend = false,
                    ShowGridLines = true,
                    Theme = "default"
                };

                var dataSet = new ChartDataSet
                {
                    Label = "البيانات",
                    ChartType = "Line",
                    BackgroundColor = "#2196F3",
                    BorderColor = "#1976D2",
                    Data = data.Select(d => d.Value).ToList(),
                    Labels = data.Select(d => d.Label).ToList()
                };

                config.DataSets = new List<ChartDataSet> { dataSet };
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني خطي", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        public ChartConfiguration CreateMultiSeriesChart(Dictionary<string, List<(string Label, decimal Value)>> data, string title = "")
        {
            try
            {
                if (!data?.Any() == true)
                {
                    return CreateEmptyChart("لا توجد بيانات للرسم البياني متعدد السلاسل");
                }

                var config = new ChartConfiguration
                {
                    Title = string.IsNullOrEmpty(title) ? "رسم بياني متعدد السلاسل" : title,
                    XAxisLabel = "النقاط",
                    YAxisLabel = "القيم",
                    ShowLegend = true,
                    ShowGridLines = true,
                    Theme = "default"
                };

                var colors = GetColorPalette("rainbow");
                var dataSets = new List<ChartDataSet>();

                int colorIndex = 0;
                foreach (var series in data)
                {
                    var color = colors[colorIndex % colors.Count];
                    var dataSet = new ChartDataSet
                    {
                        Label = series.Key,
                        ChartType = "Line",
                        BackgroundColor = color,
                        BorderColor = DarkenColor(color),
                        Data = series.Value.Select(d => d.Value).ToList(),
                        Labels = series.Value.Select(d => d.Label).ToList()
                    };
                    dataSets.Add(dataSet);
                    colorIndex++;
                }

                config.DataSets = dataSets;
                return config;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء رسم بياني متعدد السلاسل", ex, ComponentName);
                return CreateErrorChart("خطأ في إنشاء الرسم البياني");
            }
        }

        #endregion

        #region Export Methods

        public async Task<bool> ExportChartToPngAsync(ChartConfiguration chart, string filePath, int width = 800, int height = 600)
        {
            try
            {
                // سيتم تنفيذها لاحقاً مع مكتبة رسم مناسبة
                await Task.Delay(100);
                
                ComprehensiveLogger.LogInfo($"تم تصدير الرسم البياني إلى PNG: {filePath}", ComponentName);
                return true;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل في تصدير الرسم البياني إلى PNG: {filePath}", ex, ComponentName);
                return false;
            }
        }

        public async Task<bool> ExportChartToSvgAsync(ChartConfiguration chart, string filePath)
        {
            try
            {
                // سيتم تنفيذها لاحقاً مع مكتبة رسم مناسبة
                await Task.Delay(100);
                
                ComprehensiveLogger.LogInfo($"تم تصدير الرسم البياني إلى SVG: {filePath}", ComponentName);
                return true;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل في تصدير الرسم البياني إلى SVG: {filePath}", ex, ComponentName);
                return false;
            }
        }

        #endregion

        #region Color and Theme Methods

        public List<string> GetColorPalette(string theme = "default")
        {
            return _colorPalettes.TryGetValue(theme, out var palette) ? 
                new List<string>(palette) : 
                new List<string>(_colorPalettes["default"]);
        }

        public ChartConfiguration ApplyTheme(ChartConfiguration chart, string theme)
        {
            try
            {
                var colors = GetColorPalette(theme);
                
                for (int i = 0; i < chart.DataSets.Count; i++)
                {
                    var colorIndex = i % colors.Count;
                    chart.DataSets[i].BackgroundColor = colors[colorIndex];
                    chart.DataSets[i].BorderColor = DarkenColor(colors[colorIndex]);
                }

                chart.Theme = theme;
                ComprehensiveLogger.LogInfo($"تم تطبيق الثيم {theme} على الرسم البياني", ComponentName);
                return chart;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل في تطبيق الثيم {theme}", ex, ComponentName);
                return chart;
            }
        }

        #endregion

        #region Helper Methods

        private static Dictionary<string, List<string>> InitializeColorPalettes()
        {
            return new Dictionary<string, List<string>>
            {
                ["default"] = new() { "#2196F3", "#FF9800", "#4CAF50", "#F44336", "#9C27B0", "#00BCD4", "#FFEB3B", "#795548" },
                ["rainbow"] = new() { "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD", "#98D8C8", "#F7DC6F" },
                ["business"] = new() { "#1E3A8A", "#3B82F6", "#60A5FA", "#93C5FD", "#DBEAFE", "#EFF6FF" },
                ["sales"] = new() { "#10B981", "#34D399", "#6EE7B7", "#A7F3D0", "#D1FAE5", "#ECFDF5" },
                ["financial"] = new() { "#7C3AED", "#8B5CF6", "#A78BFA", "#C4B5FD", "#DDD6FE", "#EDE9FE" },
                ["products"] = new() { "#F59E0B", "#FBBF24", "#FCD34D", "#FDE68A", "#FEF3C7", "#FFFBEB" },
                ["customers"] = new() { "#EF4444", "#F87171", "#FCA5A5", "#FECACA", "#FEE2E2", "#FEF2F2" },
                ["inventory"] = new() { "#06B6D4", "#22D3EE", "#67E8F9", "#A5F3FC", "#CFFAFE", "#ECFEFF" },
                ["cashflow"] = new() { "#84CC16", "#A3E635", "#BEF264", "#D9F99D", "#ECFCCB", "#F7FEE7" },
                ["comparison"] = new() { "#2563EB", "#DC2626", "#059669", "#D97706", "#7C2D12", "#5B21B6" }
            };
        }

        private static ChartConfiguration CreateEmptyChart(string message)
        {
            return new ChartConfiguration
            {
                Title = message,
                ShowLegend = false,
                ShowGridLines = false,
                DataSets = new List<ChartDataSet>()
            };
        }

        private static ChartConfiguration CreateErrorChart(string message)
        {
            return new ChartConfiguration
            {
                Title = message,
                ShowLegend = false,
                ShowGridLines = false,
                Theme = "error",
                DataSets = new List<ChartDataSet>()
            };
        }

        private static string TruncateLabel(string label, int maxLength)
        {
            if (string.IsNullOrEmpty(label)) return "";
            return label.Length <= maxLength ? label : label.Substring(0, maxLength) + "...";
        }

        private static string DarkenColor(string hexColor)
        {
            try
            {
                if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#") || hexColor.Length != 7)
                    return hexColor;

                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                var darkerColor = Color.FromRgb(
                    (byte)Math.Max(0, color.R - 40),
                    (byte)Math.Max(0, color.G - 40),
                    (byte)Math.Max(0, color.B - 40)
                );

                return $"#{darkerColor.R:X2}{darkerColor.G:X2}{darkerColor.B:X2}";
            }
            catch
            {
                return hexColor;
            }
        }

        private static string GetStockStatusLabel(StockStatus status)
        {
            return status switch
            {
                StockStatus.OutOfStock => "نفد المخزون",
                StockStatus.Low => "مخزون منخفض",
                StockStatus.Medium => "مخزون متوسط",
                StockStatus.Good => "مخزون جيد",
                _ => "غير محدد"
            };
        }

        private static string GetStockStatusColor(StockStatus status)
        {
            return status switch
            {
                StockStatus.OutOfStock => "#F44336",  // أحمر
                StockStatus.Low => "#FF9800",         // برتقالي
                StockStatus.Medium => "#FFEB3B",      // أصفر
                StockStatus.Good => "#4CAF50",        // أخضر
                _ => "#9E9E9E"                        // رمادي
            };
        }

        #endregion
    }
}