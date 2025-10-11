using System;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Models
{
    /// <summary>
    /// نموذج عنصر المنتج للبحث
    /// </summary>
    public class ProductSearchItem
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal SellPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal CurrentStock { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        /// <summary>
        /// إنشاء عنصر بحث من منتج خام
        /// </summary>
        public static ProductSearchItem FromRaw(Product product)
        {
            return new ProductSearchItem
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                CategoryName = product.Category?.CategoryName ?? "غير محدد",
                UnitName = product.MainUnit?.UnitName ?? "قطعة",
                Barcode = product.Barcode,
                SellPrice = product.SellPrice,
                CostPrice = product.CostPrice,
                CurrentStock = product.CurrentStock,
                IsActive = product.IsActive,
                Description = product.Description ?? string.Empty
            };
        }

        /// <summary>
        /// إنشاء عنصر بحث مع معلومات المخزن
        /// </summary>
        public static ProductSearchItem FromRaw(Product product, int? warehouseId, string? warehouseName = null)
        {
            var item = FromRaw(product);
            item.WarehouseId = warehouseId;
            item.WarehouseName = warehouseName;
            return item;
        }

        /// <summary>
        /// عرض النص الكامل للمنتج
        /// </summary>
        public override string ToString()
        {
            return $"{ProductCode} - {ProductName} ({UnitName})";
        }

        /// <summary>
        /// نص البحث الكامل
        /// </summary>
        public string SearchText => $"{ProductCode} {ProductName} {CategoryName} {Barcode} {Description}";

        /// <summary>
        /// إنشاء عنصر من منتج (للتوافق مع الكود القديم)
        /// </summary>
        public static ProductSearchItem FromProduct(Product product)
        {
            return FromRaw(product);
        }

        /// <summary>
        /// إنشاء عنصر بمعاملات مفصلة (للتوافق مع الكود القديم)
        /// </summary>
        public static ProductSearchItem Create(int productId, string productCode, string productName,
            string unitName, decimal price, decimal stock)
        {
            return new ProductSearchItem
            {
                ProductId = productId,
                ProductCode = productCode,
                ProductName = productName,
                UnitName = unitName,
                SellPrice = price,
                CurrentStock = stock,
                CategoryName = "عام",
                IsActive = true,
                Description = string.Empty
            };
        }

        /// <summary>
        /// خاصية السعر (للتوافق مع الكود القديم)
        /// </summary>
        public decimal Price => SellPrice;
    }
}