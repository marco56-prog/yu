// File: SalesInvoicePrintViewModel.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.ViewModels;

/// <summary>
/// ViewModel for Sales Invoice printing
/// </summary>
public sealed class SalesInvoicePrintViewModel
{
    // --------- الحقول/الخصائص الأصلية (بدون تغيير أسماء) ---------
    public string InvoiceNumber { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerAddress { get; set; } = "";

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal GrandTotal { get; set; }

    public IReadOnlyList<SalesInvoiceLineViewModel> Lines { get; set; } = Array.Empty<SalesInvoiceLineViewModel>();
    // -------------------------------------------------------------

    // ===================== خصائص إضافية للعرض ====================
    public string InvoiceDateText => InvoiceDate.ToString("yyyy/MM/dd HH:mm");
    public string SubTotalText => SubTotal.ToString("N2");
    public string DiscountAmountText => DiscountAmount.ToString("N2");
    public string TaxAmountText => TaxAmount.ToString("N2");
    public string NetTotalText => NetTotal.ToString("N2");
    public string PaidAmountText => PaidAmount.ToString("N2");
    public string RemainingAmountText => RemainingAmount.ToString("N2");
    public string PreviousBalanceText => PreviousBalance.ToString("N2");
    public string GrandTotalText => GrandTotal.ToString("N2");

    public int LinesCount => Lines?.Count ?? 0;

    /// <summary>حالة السداد كاملة</summary>
    public bool IsFullyPaid => Math.Round(GrandTotal - PaidAmount, 2, MidpointRounding.AwayFromZero) <= 0m;

    /// <summary>نسبة المدفوع بالنسبة للإجمالي</summary>
    public string PaidPercentText
    {
        get
        {
            var denom = GrandTotal <= 0m ? 1m : GrandTotal;
            var pct = (PaidAmount / denom) * 100m;
            return $"{Math.Clamp(pct, 0m, 100m):F1}%";
        }
    }

    /// <summary>نسبة الضريبة إن أمكن استنتاجها (تقريبية)</summary>
    public decimal? TaxPercent
    {
        get
        {
            // لو SubTotal>0 نقدر نقرب نسبة الضريبة
            if (SubTotal > 0m)
            {
                var pct = (TaxAmount / SubTotal) * 100m;
                return Math.Round(pct, 2, MidpointRounding.AwayFromZero);
            }
            return null;
        }
    }
    // ============================================================

    /// <summary>
    /// Creates print ViewModel from Sales Invoice entity
    /// </summary>
    public static SalesInvoicePrintViewModel FromEntity(SalesInvoice invoice)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));

        var items = invoice.Items ?? Enumerable.Empty<SalesInvoiceItem>();
        var lines = new List<SalesInvoiceLineViewModel>();
        int serial = 1;

        foreach (var line in items)
        {
            if (line == null) continue;

            var productName = line.Product?.ProductName ?? "غير محدد";
            var unitName = line.Unit?.UnitName ?? "غير محدد";

            // ضمان قيم سليمة
            var qty = line.Quantity;
            var unitPrice = line.UnitPrice;
            var discount = line.DiscountAmount;
            var net = line.NetAmount;

            // لو NetAmount غير مضبوط في الداتا، نقدر نحتسبه كبديل (اختياري)
            if (net == 0m && qty > 0m && unitPrice >= 0m)
            {
                var calc = (qty * unitPrice) - discount;
                net = Math.Max(0m, Math.Round(calc, 2, MidpointRounding.AwayFromZero));
            }

            lines.Add(new SalesInvoiceLineViewModel
            {
                SerialNo = serial++,
                ProductName = productName,
                UnitName = unitName,
                Quantity = qty,
                UnitPrice = unitPrice,
                DiscountAmount = discount,
                NetAmount = net
            });
        }

        // قراءة آمنة لبيانات العميل
        var customerName = invoice.Customer?.CustomerName ?? "عميل نقدي";
        var customerPhone = invoice.Customer?.Phone ?? "";
        var customerAddress = invoice.Customer?.Address ?? "";

        // أرصدة وآمان التقريب
        var subTotal = Round2(invoice.SubTotal);
        var discountAmount = Round2(invoice.DiscountAmount);
        var taxAmount = Round2(invoice.TaxAmount);
        var netTotal = Round2(invoice.NetTotal);
        var previousBalance = Round2(invoice.Customer?.Balance ?? 0m);

        // إجمالي الفاتورة + الرصيد السابق
        var grandTotal = Round2(netTotal + previousBalance);

        var paid = Round2(invoice.PaidAmount);
        var remaining = Round2(grandTotal - paid);
        if (remaining < 0m) remaining = 0m; // لا تسمح بقيمة سالبة في الباقي

        return new SalesInvoicePrintViewModel
        {
            InvoiceNumber = invoice.InvoiceNumber ?? "",
            InvoiceDate = invoice.InvoiceDate,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            CustomerAddress = customerAddress,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            TaxAmount = taxAmount,
            NetTotal = netTotal,
            PaidAmount = paid,
            RemainingAmount = remaining,
            PreviousBalance = previousBalance,
            GrandTotal = grandTotal,
            Lines = lines
        };
    }

    private static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// ViewModel for Sales Invoice Line printing
/// </summary>
public sealed class SalesInvoiceLineViewModel
{
    // إضافة رقم تسلسلي للسطر (اختياري للعرض)
    public int SerialNo { get; set; }

    public string ProductName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }

    // خصائص عرض جاهزة
    public string QuantityText => Quantity.ToString("N2");
    public string UnitPriceText => UnitPrice.ToString("N2");
    public string DiscountAmountText => DiscountAmount.ToString("N2");
    public string NetAmountText => NetAmount.ToString("N2");

    /// <summary>قيمة قبل الخصم (اختيارية للعرض)</summary>
    public decimal LineGross => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
    public string LineGrossText => LineGross.ToString("N2");
}
