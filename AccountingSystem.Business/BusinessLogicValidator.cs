using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Models;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Business
{
    /// <summary>
    /// خدمة التحقق من منطق الأعمال - تضيف طبقة إضافية من التحقق والأمان
    /// </summary>
    public interface IBusinessLogicValidator
    {
        Task<BusinessValidationResult> ValidateSalesInvoiceAsync(SalesInvoice invoice);
        Task<BusinessValidationResult> ValidatePurchaseInvoiceAsync(PurchaseInvoice invoice);
        Task<BusinessValidationResult> ValidateCustomerAsync(Customer customer);
        Task<BusinessValidationResult> ValidateSupplierAsync(Supplier supplier);
        Task<BusinessValidationResult> ValidateProductAsync(Product product);
        Task<BusinessValidationResult> ValidateStockTransactionAsync(int productId, decimal quantity, string operation);
    }

    /// <summary>
    /// تنفيذ خدمة التحقق من منطق الأعمال
    /// </summary>
    public class BusinessLogicValidator : IBusinessLogicValidator
    {
        private readonly ILogger<BusinessLogicValidator> _logger;

        public BusinessLogicValidator(ILogger<BusinessLogicValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// التحقق من صحة فاتورة بيع
        /// </summary>
        public async Task<BusinessValidationResult> ValidateSalesInvoiceAsync(SalesInvoice invoice)
        {
            var result = new BusinessValidationResult();

            try
            {
                // التحقق من null
                if (invoice == null)
                {
                    result.AddError("الفاتورة غير موجودة", "Invoice");
                    return result;
                }

                // التحقق من العميل
                if (invoice.CustomerId <= 0)
                {
                    result.AddError("يجب تحديد العميل", "CustomerId");
                }

                // التحقق من التاريخ
                if (invoice.InvoiceDate == default)
                {
                    result.AddError("يجب تحديد تاريخ الفاتورة", "InvoiceDate");
                }
                else if (invoice.InvoiceDate > DateTime.Now.AddDays(1))
                {
                    result.AddError("تاريخ الفاتورة لا يمكن أن يكون في المستقبل", "InvoiceDate");
                }

                // التحقق من البنود
                if (invoice.Items == null || !invoice.Items.Any())
                {
                    result.AddError("يجب إضافة بند واحد على الأقل للفاتورة", "Items");
                }
                else
                {
                    // التحقق من كل بند
                    for (int i = 0; i < invoice.Items.Count; i++)
                    {
                        var item = invoice.Items.ElementAt(i);

                        if (item.ProductId <= 0)
                        {
                            result.AddError($"البند {i + 1}: يجب تحديد المنتج", $"Items[{i}].ProductId");
                        }

                        if (item.Quantity <= 0)
                        {
                            result.AddError($"البند {i + 1}: الكمية يجب أن تكون أكبر من صفر", $"Items[{i}].Quantity");
                        }

                        if (item.UnitPrice < 0)
                        {
                            result.AddError($"البند {i + 1}: سعر الوحدة لا يمكن أن يكون سالباً", $"Items[{i}].UnitPrice");
                        }

                        if (item.DiscountPercentage < 0 || item.DiscountPercentage > 100)
                        {
                            result.AddError($"البند {i + 1}: نسبة الخصم يجب أن تكون بين 0 و 100", $"Items[{i}].DiscountPercentage");
                        }

                        // التحقق من الحسابات
                        var expectedTotal = item.Quantity * item.UnitPrice;
                        var expectedDiscount = expectedTotal * (item.DiscountPercentage / 100);
                        var expectedNet = expectedTotal - expectedDiscount;

                        if (Math.Abs(item.TotalPrice - expectedTotal) > 0.01m)
                        {
                            result.AddWarning($"البند {i + 1}: إجمالي المبلغ غير صحيح", $"Items[{i}].TotalPrice");
                        }

                        if (Math.Abs(item.DiscountAmount - expectedDiscount) > 0.01m)
                        {
                            result.AddWarning($"البند {i + 1}: مبلغ الخصم غير صحيح", $"Items[{i}].DiscountAmount");
                        }

                        if (Math.Abs(item.NetAmount - expectedNet) > 0.01m)
                        {
                            result.AddWarning($"البند {i + 1}: صافي المبلغ غير صحيح", $"Items[{i}].NetAmount");
                        }
                    }
                }

                // التحقق من الإجماليات
                if (invoice.TotalAmount < 0)
                {
                    result.AddError("إجمالي الفاتورة لا يمكن أن يكون سالباً", "TotalAmount");
                }

                if (invoice.DiscountAmount < 0)
                {
                    result.AddError("مبلغ الخصم لا يمكن أن يكون سالباً", "DiscountAmount");
                }

                if (invoice.NetTotal < 0)
                {
                    result.AddWarning("صافي الفاتورة سالب - تحقق من الحسابات", "NetAmount");
                }

                // التحقق من المدفوع
                if (invoice.PaidAmount < 0)
                {
                    result.AddError("المبلغ المدفوع لا يمكن أن يكون سالباً", "PaidAmount");
                }

                if (invoice.PaidAmount > invoice.NetTotal)
                {
                    result.AddWarning("المبلغ المدفوع أكبر من صافي الفاتورة", "PaidAmount");
                }

                _logger.LogInformation(
                    "Sales invoice validation completed. Errors: {ErrorCount}, Warnings: {WarningCount}",
                    result.Errors.Count, result.Warnings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من فاتورة البيع");
                result.AddError($"خطأ في التحقق: {ex.Message}", "General");
            }

            return result;
        }

        /// <summary>
        /// التحقق من صحة فاتورة شراء
        /// </summary>
        public async Task<BusinessValidationResult> ValidatePurchaseInvoiceAsync(PurchaseInvoice invoice)
        {
            var result = new BusinessValidationResult();

            try
            {
                if (invoice == null)
                {
                    result.AddError("الفاتورة غير موجودة", "Invoice");
                    return result;
                }

                if (invoice.SupplierId <= 0)
                {
                    result.AddError("يجب تحديد المورد", "SupplierId");
                }

                if (invoice.InvoiceDate == default)
                {
                    result.AddError("يجب تحديد تاريخ الفاتورة", "InvoiceDate");
                }

                if (invoice.Items == null || !invoice.Items.Any())
                {
                    result.AddError("يجب إضافة بند واحد على الأقل للفاتورة", "Items");
                }

                // نفس فحوصات البنود كفاتورة البيع
                if (invoice.Items != null)
                {
                    for (int i = 0; i < invoice.Items.Count; i++)
                    {
                        var item = invoice.Items.ElementAt(i);

                        if (item.ProductId <= 0)
                            result.AddError($"البند {i + 1}: يجب تحديد المنتج", $"Items[{i}].ProductId");

                        if (item.Quantity <= 0)
                            result.AddError($"البند {i + 1}: الكمية يجب أن تكون أكبر من صفر", $"Items[{i}].Quantity");

                        if (item.UnitCost < 0)
                            result.AddError($"البند {i + 1}: سعر الوحدة لا يمكن أن يكون سالباً", $"Items[{i}].UnitCost");
                    }
                }

                _logger.LogInformation(
                    "Purchase invoice validation completed. Errors: {ErrorCount}, Warnings: {WarningCount}",
                    result.Errors.Count, result.Warnings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من فاتورة الشراء");
                result.AddError($"خطأ في التحقق: {ex.Message}", "General");
            }

            return result;
        }

        /// <summary>
        /// التحقق من صحة بيانات العميل
        /// </summary>
        public async Task<BusinessValidationResult> ValidateCustomerAsync(Customer customer)
        {
            var result = new BusinessValidationResult();

            try
            {
                if (customer == null)
                {
                    result.AddError("بيانات العميل غير موجودة", "Customer");
                    return result;
                }

                // التحقق من الاسم
                if (string.IsNullOrWhiteSpace(customer.CustomerName))
                {
                    result.AddError("يجب إدخال اسم العميل", "CustomerName");
                }
                else if (!ValidationService.IsValidName(customer.CustomerName))
                {
                    result.AddError("اسم العميل غير صحيح", "CustomerName");
                }

                // التحقق من الهاتف
                if (!string.IsNullOrWhiteSpace(customer.Phone) && 
                    !ValidationService.IsValidPhone(customer.Phone))
                {
                    result.AddError("رقم الهاتف غير صحيح", "Phone");
                }

                // التحقق من البريد الإلكتروني
                if (!string.IsNullOrWhiteSpace(customer.Email) && 
                    !ValidationService.IsValidEmail(customer.Email))
                {
                    result.AddError("البريد الإلكتروني غير صحيح", "Email");
                }

                // التحقق من حد الائتمان
                if (customer.CreditLimit < 0)
                {
                    result.AddError("حد الائتمان لا يمكن أن يكون سالباً", "CreditLimit");
                }

                // التحقق من الرصيد
                if (customer.Balance < 0)
                {
                    result.AddWarning("رصيد العميل سالب (دائن)", "Balance");
                }

                _logger.LogInformation("Customer validation completed for: {CustomerName}", customer.CustomerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من بيانات العميل");
                result.AddError($"خطأ في التحقق: {ex.Message}", "General");
            }

            return result;
        }

        /// <summary>
        /// التحقق من صحة بيانات المورد
        /// </summary>
        public async Task<BusinessValidationResult> ValidateSupplierAsync(Supplier supplier)
        {
            var result = new BusinessValidationResult();

            try
            {
                if (supplier == null)
                {
                    result.AddError("بيانات المورد غير موجودة", "Supplier");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                {
                    result.AddError("يجب إدخال اسم المورد", "SupplierName");
                }
                else if (!ValidationService.IsValidName(supplier.SupplierName))
                {
                    result.AddError("اسم المورد غير صحيح", "SupplierName");
                }

                if (!string.IsNullOrWhiteSpace(supplier.Phone) && 
                    !ValidationService.IsValidPhone(supplier.Phone))
                {
                    result.AddError("رقم الهاتف غير صحيح", "Phone");
                }

                if (!string.IsNullOrWhiteSpace(supplier.Email) && 
                    !ValidationService.IsValidEmail(supplier.Email))
                {
                    result.AddError("البريد الإلكتروني غير صحيح", "Email");
                }

                if (supplier.Balance < 0)
                {
                    result.AddWarning("رصيد المورد سالب (مدين)", "Balance");
                }

                _logger.LogInformation("Supplier validation completed for: {SupplierName}", supplier.SupplierName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من بيانات المورد");
                result.AddError($"خطأ في التحقق: {ex.Message}", "General");
            }

            return result;
        }

        /// <summary>
        /// التحقق من صحة بيانات المنتج
        /// </summary>
        public async Task<BusinessValidationResult> ValidateProductAsync(Product product)
        {
            var result = new BusinessValidationResult();

            try
            {
                if (product == null)
                {
                    result.AddError("بيانات المنتج غير موجودة", "Product");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(product.ProductName))
                {
                    result.AddError("يجب إدخال اسم المنتج", "ProductName");
                }
                else if (!ValidationService.IsValidName(product.ProductName, 2, 200))
                {
                    result.AddError("اسم المنتج غير صحيح", "ProductName");
                }

                if (string.IsNullOrWhiteSpace(product.ProductCode))
                {
                    result.AddError("يجب إدخال كود المنتج", "ProductCode");
                }
                else if (!ValidationService.IsValidProductCode(product.ProductCode))
                {
                    result.AddError("كود المنتج غير صحيح", "ProductCode");
                }

                if (product.SalePrice < 0)
                {
                    result.AddError("سعر البيع لا يمكن أن يكون سالباً", "SalePrice");
                }

                if (product.PurchasePrice < 0)
                {
                    result.AddError("سعر الشراء لا يمكن أن يكون سالباً", "PurchasePrice");
                }

                if (product.SalePrice < product.PurchasePrice)
                {
                    result.AddWarning("سعر البيع أقل من سعر الشراء", "SalePrice");
                }

                if (product.CurrentStock < 0)
                {
                    result.AddError("المخزون الحالي لا يمكن أن يكون سالباً", "CurrentStock");
                }

                if (product.MinimumStock < 0)
                {
                    result.AddError("الحد الأدنى للمخزون لا يمكن أن يكون سالباً", "MinimumStock");
                }

                if (product.CurrentStock < product.MinimumStock)
                {
                    result.AddWarning("المخزون الحالي أقل من الحد الأدنى", "CurrentStock");
                }

                _logger.LogInformation("Product validation completed for: {ProductName}", product.ProductName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من بيانات المنتج");
                result.AddError($"خطأ في التحقق: {ex.Message}", "General");
            }

            return result;
        }

        /// <summary>
        /// التحقق من صحة عملية على المخزون
        /// </summary>
        public async Task<BusinessValidationResult> ValidateStockTransactionAsync(
            int productId, decimal quantity, string operation)
        {
            var result = new BusinessValidationResult();

            try
            {
                if (productId <= 0)
                {
                    result.AddError("معرف المنتج غير صحيح", "ProductId");
                }

                if (quantity <= 0)
                {
                    result.AddError("الكمية يجب أن تكون أكبر من صفر", "Quantity");
                }

                if (string.IsNullOrWhiteSpace(operation))
                {
                    result.AddError("نوع العملية غير محدد", "Operation");
                }

                _logger.LogInformation(
                    "Stock transaction validation completed. Product: {ProductId}, Quantity: {Quantity}, Operation: {Operation}",
                    productId, quantity, operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من عملية المخزون");
                result.AddError($"خطأ في التحقق: {ex.Message}", "General");
            }

            return result;
        }
    }

    /// <summary>
    /// نتيجة التحقق من منطق الأعمال
    /// </summary>
    public class BusinessValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<ValidationError> Errors { get; } = new();
        public List<ValidationError> Warnings { get; } = new();

        public void AddError(string message, string propertyName = "")
        {
            Errors.Add(new ValidationError { Message = message, PropertyName = propertyName });
        }

        public void AddWarning(string message, string propertyName = "")
        {
            Warnings.Add(new ValidationError { Message = message, PropertyName = propertyName });
        }

        public string GetErrorMessage()
        {
            if (IsValid) return string.Empty;
            return string.Join(Environment.NewLine, Errors.Select(e => e.Message));
        }

        public string GetWarningMessage()
        {
            if (!Warnings.Any()) return string.Empty;
            return string.Join(Environment.NewLine, Warnings.Select(w => w.Message));
        }
    }

    /// <summary>
    /// خطأ التحقق
    /// </summary>
    public class ValidationError
    {
        public string Message { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
    }
}
