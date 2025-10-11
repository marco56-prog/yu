using System;
using System.Diagnostics;

namespace AccountingSystem.Business
{
    /// <summary>
    /// عنصر في تقرير المبيعات (Invoice-level).
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{InvoiceNumber,nq} | {InvoiceDate:yyyy-MM-dd} | Net={NetTotal,nq}")]
    public class SalesReportItemDto
    {
        /// <summary>رقم الفاتورة</summary>
        public string InvoiceNumber { get; set; } = "";
        /// <summary>تاريخ الفاتورة</summary>
        public DateTime InvoiceDate { get; set; }
        /// <summary>اسم العميل</summary>
        public string CustomerName { get; set; } = "";
        /// <summary>الإجمالي قبل الضريبة والخصم</summary>
        public decimal SubTotal { get; set; }
        /// <summary>قيمة الضريبة</summary>
        public decimal TaxAmount { get; set; }
        /// <summary>قيمة الخصم</summary>
        public decimal DiscountAmount { get; set; }
        /// <summary>الصافي النهائي للفاتورة</summary>
        public decimal NetTotal { get; set; }
        /// <summary>عدد عناصر الفاتورة</summary>
        public int ItemsCount { get; set; }
        /// <summary>حالة الدفع (مدفوع/جزئي/غير مدفوع)</summary>
        public string PaymentStatus { get; set; } = "";

        public SalesReportItemDto() { }

        public SalesReportItemDto(
            string invoiceNumber,
            DateTime invoiceDate,
            string customerName,
            decimal subTotal,
            decimal taxAmount,
            decimal discountAmount,
            decimal netTotal,
            int itemsCount,
            string paymentStatus)
        {
            InvoiceNumber = invoiceNumber ?? "";
            InvoiceDate = invoiceDate;
            CustomerName = customerName ?? "";
            SubTotal = subTotal;
            TaxAmount = taxAmount;
            DiscountAmount = discountAmount;
            NetTotal = netTotal;
            ItemsCount = itemsCount;
            PaymentStatus = paymentStatus ?? "";
        }

        /// <summary>تنظيف نصوص الحقول (Trim) — اختياري قبل العرض/التخزين.</summary>
        public void Normalize()
        {
            InvoiceNumber = (InvoiceNumber ?? string.Empty).Trim();
            CustomerName = (CustomerName ?? string.Empty).Trim();
            PaymentStatus = (PaymentStatus ?? string.Empty).Trim();
        }
    }

    /// <summary>
    /// عنصر في تقرير المخزون (Product-level).
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ProductCode,nq} | {ProductName,nq} | Stock={CurrentStock}")]
    public class InventoryReportItemDto
    {
        /// <summary>كود الصنف</summary>
        public string ProductCode { get; set; } = "";
        /// <summary>اسم الصنف</summary>
        public string ProductName { get; set; } = "";
        /// <summary>اسم الفئة</summary>
        public string CategoryName { get; set; } = "";
        /// <summary>اسم الوحدة</summary>
        public string UnitName { get; set; } = "";
        /// <summary>الرصيد الحالي بالمخزون</summary>
        public decimal CurrentStock { get; set; }
        /// <summary>حد الطلب الأدنى</summary>
        public decimal MinimumStock { get; set; }
        /// <summary>سعر الشراء</summary>
        public decimal PurchasePrice { get; set; }
        /// <summary>سعر البيع</summary>
        public decimal SalePrice { get; set; }
        /// <summary>قيمة المخزون (سعر بيع × الرصيد الحالي)</summary>
        public decimal StockValue { get; set; }
        /// <summary>الحالة (منخفض/متوفر/منعدم... إلخ)</summary>
        public string Status { get; set; } = "";

        public InventoryReportItemDto() { }

        public InventoryReportItemDto(
            string productCode,
            string productName,
            string categoryName,
            string unitName,
            decimal currentStock,
            decimal minimumStock,
            decimal purchasePrice,
            decimal salePrice,
            decimal stockValue,
            string status)
        {
            ProductCode = productCode ?? "";
            ProductName = productName ?? "";
            CategoryName = categoryName ?? "";
            UnitName = unitName ?? "";
            CurrentStock = currentStock;
            MinimumStock = minimumStock;
            PurchasePrice = purchasePrice;
            SalePrice = salePrice;
            StockValue = stockValue;
            Status = status ?? "";
        }

        /// <summary>تنظيف نصوص الحقول (Trim) — اختياري.</summary>
        public void Normalize()
        {
            ProductCode = (ProductCode ?? string.Empty).Trim();
            ProductName = (ProductName ?? string.Empty).Trim();
            CategoryName = (CategoryName ?? string.Empty).Trim();
            UnitName = (UnitName ?? string.Empty).Trim();
            Status = (Status ?? string.Empty).Trim();
        }
    }

    /// <summary>
    /// عنصر في تقرير الأرباح (period aggregate).
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{Period,nq} | Rev={Revenue} | Net={NetProfit}")]
    public class ProfitReportItemDto
    {
        /// <summary>تاريخ/بداية الفترة</summary>
        public DateTime Date { get; set; }
        /// <summary>اسم الفترة (يوم/أسبوع/شهر)</summary>
        public string Period { get; set; } = "";
        /// <summary>الإيراد</summary>
        public decimal Revenue { get; set; }
        /// <summary>تكلفة البضاعة المباعة</summary>
        public decimal CostOfGoodsSold { get; set; }
        /// <summary>مجمل الربح</summary>
        public decimal GrossProfit { get; set; }
        /// <summary>المصروفات</summary>
        public decimal Expenses { get; set; }
        /// <summary>صافي الربح</summary>
        public decimal NetProfit { get; set; }
        /// <summary>هامش الربح (%)</summary>
        public decimal ProfitMargin { get; set; }

        public ProfitReportItemDto() { }

        public ProfitReportItemDto(
            DateTime date,
            string period,
            decimal revenue,
            decimal costOfGoodsSold,
            decimal grossProfit,
            decimal expenses,
            decimal netProfit,
            decimal profitMargin)
        {
            Date = date;
            Period = period ?? "";
            Revenue = revenue;
            CostOfGoodsSold = costOfGoodsSold;
            GrossProfit = grossProfit;
            Expenses = expenses;
            NetProfit = netProfit;
            ProfitMargin = profitMargin;
        }

        /// <summary>تنظيف نصوص الحقول (Trim) — اختياري.</summary>
        public void Normalize()
        {
            Period = (Period ?? string.Empty).Trim();
        }
    }

    /// <summary>
    /// أفضل المنتجات مبيعًا.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("#{Rank} {ProductCode,nq} | {ProductName,nq} | Qty={TotalQuantitySold} | Sales={TotalSalesValue}")]
    public class TopProductDto
    {
        /// <summary>الترتيب</summary>
        public int Rank { get; set; }
        /// <summary>كود المنتج</summary>
        public string ProductCode { get; set; } = "";
        /// <summary>اسم المنتج</summary>
        public string ProductName { get; set; } = "";
        /// <summary>إجمالي الكمية المباعة</summary>
        public decimal TotalQuantitySold { get; set; }
        /// <summary>إجمالي قيمة المبيعات</summary>
        public decimal TotalSalesValue { get; set; }
        /// <summary>إجمالي الربح</summary>
        public decimal TotalProfit { get; set; }
        /// <summary>عدد معاملات البيع</summary>
        public int SalesTransactions { get; set; }
        /// <summary>متوسط سعر البيع</summary>
        public decimal AveragePrice { get; set; }

        public TopProductDto() { }

        public TopProductDto(
            int rank,
            string productCode,
            string productName,
            decimal totalQuantitySold,
            decimal totalSalesValue,
            decimal totalProfit,
            int salesTransactions,
            decimal averagePrice)
        {
            Rank = rank;
            ProductCode = productCode ?? "";
            ProductName = productName ?? "";
            TotalQuantitySold = totalQuantitySold;
            TotalSalesValue = totalSalesValue;
            TotalProfit = totalProfit;
            SalesTransactions = salesTransactions;
            AveragePrice = averagePrice;
        }

        /// <summary>تنظيف نصوص الحقول (Trim) — اختياري.</summary>
        public void Normalize()
        {
            ProductCode = (ProductCode ?? string.Empty).Trim();
            ProductName = (ProductName ?? string.Empty).Trim();
        }
    }

    /// <summary>
    /// ملخص مالي عام.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("Sales={TotalSales} | Purchases={TotalPurchases} | Net={NetProfit} | Invoices={TotalInvoices}")]
    public class FinancialSummaryDto
    {
        /// <summary>إجمالي المبيعات</summary>
        public decimal TotalSales { get; set; }
        /// <summary>إجمالي المشتريات</summary>
        public decimal TotalPurchases { get; set; }
        /// <summary>صافي الربح</summary>
        public decimal NetProfit { get; set; }
        /// <summary>إجمالي عدد الفواتير</summary>
        public int TotalInvoices { get; set; }

        public FinancialSummaryDto() { }

        public FinancialSummaryDto(decimal totalSales, decimal totalPurchases, decimal netProfit, int totalInvoices)
        {
            TotalSales = totalSales;
            TotalPurchases = totalPurchases;
            NetProfit = netProfit;
            TotalInvoices = totalInvoices;
        }
    }

    /// <summary>
    /// نوع تقرير المبيعات.
    /// </summary>
    public enum SalesReportType
    {
        Daily,
        Weekly,
        Monthly
    }
}
