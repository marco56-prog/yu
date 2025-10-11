using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using AccountingSystem.Models;
using AccountingSystem.WPF.Helpers;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// محرك الحسابات التلقائية للفواتير مع دعم الخصم العام والضرائب الديناميكية
    /// SubTotal → TotalDiscount → BaseForTax → Tax → NetTotal → Remaining
    /// </summary>
    public interface IAutoCalculationsEngine
    {
        CalculationResult CalculateInvoiceTotals(CalculationInput input);
        LineCalculationResult CalculateLineTotal(LineCalculationInput input);
        DiscountCalculationResult CalculateDiscount(DiscountCalculationInput input);
        TaxCalculationResult CalculateTax(TaxCalculationInput input);
        bool ValidateCalculations(CalculationResult result);
        event EventHandler<CalculationChangedEventArgs>? CalculationChanged;
    }

    public class AutoCalculationsEngine : IAutoCalculationsEngine, INotifyPropertyChanged
    {
        private const string ComponentName = "AutoCalculationsEngine";
        private readonly CultureInfo _culture;

        public event EventHandler<CalculationChangedEventArgs>? CalculationChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public AutoCalculationsEngine()
        {
            _culture = new CultureInfo("ar-EG");
            _culture.NumberFormat.CurrencySymbol = "ج.م";
        }

        public CalculationResult CalculateInvoiceTotals(CalculationInput input)
        {
            try
            {
                ComprehensiveLogger.LogBusinessOperation("بدء حساب إجماليات الفاتورة",
                    $"المكون: {ComponentName}, عدد الأصناف: {input.Items?.Count ?? 0}");

                var result = new CalculationResult();

                // Step 1: Calculate SubTotal from all items
                result.SubTotal = CalculateSubTotal(input.Items ?? new List<InvoiceItemCalculation>());

                // Step 2: Calculate Total Line Discounts
                result.LineDiscounts = CalculateLineDiscounts(input.Items ?? new List<InvoiceItemCalculation>());

                // Step 3: Calculate Global Discount
                result.GlobalDiscount = CalculateGlobalDiscount(result.SubTotal, input.GlobalDiscountAmount, input.GlobalDiscountIsPercentage);

                // Step 4: Total Discount
                result.TotalDiscount = result.LineDiscounts + result.GlobalDiscount;

                // Step 5: Base amount for tax calculation
                result.BaseForTax = input.TaxOnNetOfDiscount
                    ? Math.Max(0, result.SubTotal - result.TotalDiscount)
                    : result.SubTotal;

                // Step 6: Calculate Tax
                var taxResult = CalculateTax(new TaxCalculationInput
                {
                    BaseAmount = result.BaseForTax,
                    TaxRate = input.TaxRate,
                    TaxRounding = input.TaxRounding
                });
                result.TaxAmount = taxResult.TaxAmount;
                result.TaxDetails = taxResult.Details;

                // Step 7: Net Total
                result.NetTotal = result.BaseForTax + result.TaxAmount;

                // Step 8: Remaining Amount
                result.RemainingAmount = Math.Max(0, result.NetTotal - input.PaidAmount);

                // Step 9: Additional calculations
                result.PaidPercentage = result.NetTotal > 0 ? (input.PaidAmount / result.NetTotal) * 100 : 0;
                result.DiscountPercentage = result.SubTotal > 0 ? (result.TotalDiscount / result.SubTotal) * 100 : 0;

                // Step 10: Validation
                result.IsValid = ValidateCalculations(result);
                result.CalculatedAt = DateTime.Now;

                // Fire event
                OnCalculationChanged(new CalculationChangedEventArgs
                {
                    CalculationType = CalculationType.InvoiceTotal,
                    Result = result,
                    IsValid = result.IsValid
                });

                ComprehensiveLogger.LogBusinessOperation("تم حساب إجماليات الفاتورة بنجاح",
                    $"المكون: {ComponentName}, الإجمالي: {result.SubTotal:C}, الصافي: {result.NetTotal:C}");

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل حساب إجماليات الفاتورة", ex, ComponentName);
                return CalculationResult.Error($"خطأ في الحسابات: {ex.Message}");
            }
        }

        public LineCalculationResult CalculateLineTotal(LineCalculationInput input)
        {
            try
            {
                var result = new LineCalculationResult();

                // Basic validation
                if (input.Quantity <= 0 || input.UnitPrice < 0)
                {
                    return LineCalculationResult.Error("قيم غير صالحة للكمية أو السعر");
                }

                // Step 1: Gross Amount
                result.GrossAmount = input.Quantity * input.UnitPrice;

                // Step 2: Calculate Discount
                var discountResult = CalculateDiscount(new DiscountCalculationInput
                {
                    BaseAmount = result.GrossAmount,
                    DiscountAmount = input.DiscountAmount,
                    DiscountIsPercentage = input.DiscountIsPercentage
                });

                result.DiscountAmount = discountResult.DiscountValue;
                result.DiscountPercentage = discountResult.DiscountPercentage;

                // Step 3: Net Amount
                result.NetAmount = Math.Max(0, result.GrossAmount - result.DiscountAmount);

                // Step 4: Unit calculations
                result.NetUnitPrice = input.Quantity > 0 ? result.NetAmount / input.Quantity : 0;
                result.DiscountPerUnit = input.Quantity > 0 ? result.DiscountAmount / input.Quantity : 0;

                // Step 5: Validation
                result.IsValid = result.NetAmount >= 0 && result.DiscountAmount <= result.GrossAmount;

                ComprehensiveLogger.LogBusinessOperation("تم حساب إجمالي السطر",
                    $"المكون: {ComponentName}, الكمية: {input.Quantity}, السعر: {input.UnitPrice:C}, الصافي: {result.NetAmount:C}");

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل حساب إجمالي السطر", ex, ComponentName);
                return LineCalculationResult.Error($"خطأ في حساب السطر: {ex.Message}");
            }
        }

        public DiscountCalculationResult CalculateDiscount(DiscountCalculationInput input)
        {
            try
            {
                var result = new DiscountCalculationResult();

                if (input.BaseAmount <= 0)
                {
                    return result; // No discount on zero or negative amounts
                }

                if (input.DiscountIsPercentage)
                {
                    // Percentage discount
                    if (input.DiscountAmount > 100)
                    {
                        return DiscountCalculationResult.Error("نسبة الخصم لا يمكن أن تتجاوز 100%");
                    }

                    result.DiscountPercentage = input.DiscountAmount;
                    result.DiscountValue = Math.Round(input.BaseAmount * (input.DiscountAmount / 100m), 2);
                }
                else
                {
                    // Fixed amount discount
                    if (input.DiscountAmount > input.BaseAmount)
                    {
                        return DiscountCalculationResult.Error("قيمة الخصم تتجاوز المبلغ الأساسي");
                    }

                    result.DiscountValue = input.DiscountAmount;
                    result.DiscountPercentage = input.BaseAmount > 0 ? (input.DiscountAmount / input.BaseAmount) * 100 : 0;
                }

                result.NetAmount = input.BaseAmount - result.DiscountValue;
                result.IsValid = result.DiscountValue >= 0 && result.DiscountValue <= input.BaseAmount;

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل حساب الخصم", ex, ComponentName);
                return DiscountCalculationResult.Error($"خطأ في حساب الخصم: {ex.Message}");
            }
        }

        public TaxCalculationResult CalculateTax(TaxCalculationInput input)
        {
            try
            {
                var result = new TaxCalculationResult();

                if (input.BaseAmount <= 0 || input.TaxRate <= 0)
                {
                    return result; // No tax on zero/negative amounts or zero rate
                }

                // Calculate raw tax amount
                var rawTaxAmount = input.BaseAmount * (input.TaxRate / 100m);

                // Apply rounding based on configuration
                result.TaxAmount = input.TaxRounding switch
                {
                    TaxRounding.Normal => Math.Round(rawTaxAmount, 2),
                    TaxRounding.RoundUp => Math.Ceiling(rawTaxAmount * 100) / 100,
                    TaxRounding.RoundDown => Math.Floor(rawTaxAmount * 100) / 100,
                    TaxRounding.ToNearest5 => Math.Round(rawTaxAmount * 20, MidpointRounding.AwayFromZero) / 20,
                    TaxRounding.ToNearest10 => Math.Round(rawTaxAmount * 10, MidpointRounding.AwayFromZero) / 10,
                    _ => Math.Round(rawTaxAmount, 2)
                };

                // Tax details
                result.Details = new TaxCalculationDetails
                {
                    BaseAmount = input.BaseAmount,
                    TaxRate = input.TaxRate,
                    RawTaxAmount = rawTaxAmount,
                    RoundedTaxAmount = result.TaxAmount,
                    RoundingDifference = result.TaxAmount - rawTaxAmount,
                    EffectiveTaxRate = input.BaseAmount > 0 ? (result.TaxAmount / input.BaseAmount) * 100 : 0
                };

                result.TotalIncludingTax = input.BaseAmount + result.TaxAmount;
                result.IsValid = result.TaxAmount >= 0;

                ComprehensiveLogger.LogBusinessOperation("تم حساب الضريبة",
                    $"المكون: {ComponentName}, الأساس: {input.BaseAmount:C}, المعدل: {input.TaxRate}%, الضريبة: {result.TaxAmount:C}");

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل حساب الضريبة", ex, ComponentName);
                return TaxCalculationResult.Error($"خطأ في حساب الضريبة: {ex.Message}");
            }
        }

        public bool ValidateCalculations(CalculationResult result)
        {
            try
            {
                // Basic validations
                if (result.SubTotal < 0 || result.NetTotal < 0 || result.TaxAmount < 0)
                    return false;

                if (result.TotalDiscount < 0 || result.TotalDiscount > result.SubTotal)
                    return false;

                if (result.BaseForTax < 0)
                    return false;

                // Logical validations
                var expectedNet = result.BaseForTax + result.TaxAmount;
                if (Math.Abs(result.NetTotal - expectedNet) > 0.01m) // Allow for small rounding differences
                    return false;

                ComprehensiveLogger.LogBusinessOperation("تم التحقق من صحة الحسابات", $"المكون: {ComponentName}, جميع الحسابات صحيحة");
                return true;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل التحقق من صحة الحسابات", ex, ComponentName);
                return false;
            }
        }

        #region Private Helper Methods

        private decimal CalculateSubTotal(List<InvoiceItemCalculation> items)
        {
            return items?.Sum(item => item.Quantity * item.UnitPrice) ?? 0;
        }

        private decimal CalculateLineDiscounts(List<InvoiceItemCalculation> items)
        {
            return items?.Sum(item => item.DiscountAmount) ?? 0;
        }

        private decimal CalculateGlobalDiscount(decimal subTotal, decimal discountAmount, bool isPercentage)
        {
            if (subTotal <= 0 || discountAmount <= 0)
                return 0;

            return isPercentage
                ? Math.Round(subTotal * (discountAmount / 100m), 2)
                : Math.Min(discountAmount, subTotal);
        }

        private void OnCalculationChanged(CalculationChangedEventArgs e)
        {
            CalculationChanged?.Invoke(this, e);
            OnPropertyChanged(nameof(CalculationChanged));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region Data Transfer Objects

    public class CalculationInput
    {
        public List<InvoiceItemCalculation>? Items { get; set; }
        public decimal GlobalDiscountAmount { get; set; }
        public bool GlobalDiscountIsPercentage { get; set; }
        public decimal TaxRate { get; set; }
        public bool TaxOnNetOfDiscount { get; set; } = true;
        public TaxRounding TaxRounding { get; set; } = TaxRounding.Normal;
        public decimal PaidAmount { get; set; }
    }

    public class InvoiceItemCalculation
    {
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool DiscountIsPercentage { get; set; }
    }

    public class LineCalculationInput
    {
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool DiscountIsPercentage { get; set; }
    }

    public class DiscountCalculationInput
    {
        public decimal BaseAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool DiscountIsPercentage { get; set; }
    }

    public class TaxCalculationInput
    {
        public decimal BaseAmount { get; set; }
        public decimal TaxRate { get; set; }
        public TaxRounding TaxRounding { get; set; } = TaxRounding.Normal;
    }

    #endregion

    #region Result Classes

    public class CalculationResult
    {
        public decimal SubTotal { get; set; }
        public decimal LineDiscounts { get; set; }
        public decimal GlobalDiscount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal BaseForTax { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetTotal { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal PaidPercentage { get; set; }
        public decimal DiscountPercentage { get; set; }
        public bool IsValid { get; set; }
        public DateTime CalculatedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public TaxCalculationDetails? TaxDetails { get; set; }

        public static CalculationResult Error(string message)
        {
            return new CalculationResult
            {
                IsValid = false,
                ErrorMessage = message,
                CalculatedAt = DateTime.Now
            };
        }
    }

    public class LineCalculationResult
    {
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal NetUnitPrice { get; set; }
        public decimal DiscountPerUnit { get; set; }
        public decimal DiscountPercentage { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public static LineCalculationResult Error(string message)
        {
            return new LineCalculationResult
            {
                IsValid = false,
                ErrorMessage = message
            };
        }
    }

    public class DiscountCalculationResult
    {
        public decimal DiscountValue { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal NetAmount { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public static DiscountCalculationResult Error(string message)
        {
            return new DiscountCalculationResult
            {
                IsValid = false,
                ErrorMessage = message
            };
        }
    }

    public class TaxCalculationResult
    {
        public decimal TaxAmount { get; set; }
        public decimal TotalIncludingTax { get; set; }
        public TaxCalculationDetails? Details { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public static TaxCalculationResult Error(string message)
        {
            return new TaxCalculationResult
            {
                IsValid = false,
                ErrorMessage = message
            };
        }
    }

    public class TaxCalculationDetails
    {
        public decimal BaseAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal RawTaxAmount { get; set; }
        public decimal RoundedTaxAmount { get; set; }
        public decimal RoundingDifference { get; set; }
        public decimal EffectiveTaxRate { get; set; }
    }

    #endregion

    #region Enums and Events

    public enum TaxRounding
    {
        Normal,
        RoundUp,
        RoundDown,
        ToNearest5,
        ToNearest10
    }

    public enum CalculationType
    {
        InvoiceTotal,
        LineTotal,
        Discount,
        Tax
    }

    public class CalculationChangedEventArgs : EventArgs
    {
        public CalculationType CalculationType { get; set; }
        public object? Result { get; set; }
        public bool IsValid { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    #endregion
}