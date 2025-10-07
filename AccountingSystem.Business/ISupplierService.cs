using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;
using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    // واجهة خدمة الموردين
    public interface ISupplierService
    {
        Task<IEnumerable<Supplier>> GetAllSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int supplierId);
        Task<Supplier> CreateSupplierAsync(Supplier supplier);
        Task<Supplier> UpdateSupplierAsync(Supplier supplier);
        Task<bool> DeleteSupplierAsync(int supplierId);
        Task<bool> ExistsAsync(int supplierId);
        Task<IEnumerable<Supplier>> SearchSuppliersAsync(string searchTerm);
        Task<decimal> GetSupplierBalanceAsync(int supplierId);
    }

    // تنفيذ خدمة الموردين
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SupplierService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Supplier>> GetAllSuppliersAsync()
        {
            return await _unitOfWork.Repository<Supplier>().FindAsync(s => s.IsActive);
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int supplierId)
        {
            return await _unitOfWork.Repository<Supplier>().GetByIdAsync(supplierId);
        }

        public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
        {
            // تنظيف الحقول النصية
            supplier.SupplierName = (supplier.SupplierName ?? string.Empty).Trim();
            supplier.Address      = (supplier.Address      ?? string.Empty).Trim();
            supplier.Phone        = (supplier.Phone        ?? string.Empty).Trim();
            supplier.Email        = (supplier.Email        ?? string.Empty).Trim();

            // التحقق من صحة البيانات باستخدام ValidationService
            var validationResult = ValidationService.ValidateSupplier(
                supplier.SupplierName, 
                supplier.Email, 
                supplier.Phone);

            if (!validationResult.IsValid)
                throw new InvalidOperationException(validationResult.ErrorMessage);

            // منع تكرار الاسم
            var duplicate = await _unitOfWork.Repository<Supplier>()
                .SingleOrDefaultAsync(s => s.IsActive && s.SupplierName.Equals(supplier.SupplierName, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
                throw new InvalidOperationException("اسم المورّد موجود بالفعل.");

            supplier.CreatedDate = DateTime.Now;
            supplier.IsActive = true;

            await _unitOfWork.Repository<Supplier>().AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();

            return supplier;
        }

        public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
        {
            var repo = _unitOfWork.Repository<Supplier>();
            var existing = await repo.GetByIdAsync(supplier.SupplierId);
            if (existing == null)
                throw new InvalidOperationException("المورّد غير موجود.");

            // تنظيف الحقول
            var newName  = (supplier.SupplierName ?? string.Empty).Trim();
            var newAddr  = (supplier.Address      ?? string.Empty).Trim();
            var newPhone = (supplier.Phone        ?? string.Empty).Trim();
            var newEmail = (supplier.Email        ?? string.Empty).Trim();

            // التحقق من صحة البيانات باستخدام ValidationService
            var validationResult = ValidationService.ValidateSupplier(newName, newEmail, newPhone);
            if (!validationResult.IsValid)
                throw new InvalidOperationException(validationResult.ErrorMessage);

            // منع تكرار الاسم لغير نفس المورّد
            var duplicate = await _unitOfWork.Repository<Supplier>()
                .SingleOrDefaultAsync(s => s.IsActive &&
                                           s.SupplierId != supplier.SupplierId &&
                                           s.SupplierName.Equals(newName, StringComparison.OrdinalIgnoreCase));
            if (duplicate != null)
                throw new InvalidOperationException("اسم المورّد موجود بالفعل.");

            // تحديث الحقول
            existing.SupplierName = newName;
            existing.Address      = newAddr;
            existing.Phone        = newPhone;
            existing.Email        = newEmail;
            existing.IsActive     = supplier.IsActive;

            repo.Update(existing);
            await _unitOfWork.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> DeleteSupplierAsync(int supplierId)
        {
            var repo = _unitOfWork.Repository<Supplier>();
            var supplier = await repo.GetByIdAsync(supplierId);
            if (supplier == null) return false;

            // منع الحذف إن وُجدت فواتير مشتريات مرتبطة
            var hasPurchaseInvoices = await _unitOfWork.Repository<PurchaseInvoice>()
                .AnyAsync(p => p.SupplierId == supplierId);

            if (hasPurchaseInvoices)
            {
                // إلغاء التفعيل بدل الحذف للحفاظ على التكامل المرجعي
                if (supplier.IsActive)
                {
                    supplier.IsActive = false;
                    repo.Update(supplier);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            else
            {
                repo.Remove(supplier);
                await _unitOfWork.SaveChangesAsync();
            }

            return true;
        }

        public async Task<IEnumerable<Supplier>> SearchSuppliersAsync(string searchTerm)
        {
            var term = (searchTerm ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(term))
                return await GetAllSuppliersAsync();

            // بحث مبسّط بالاسم/الهاتف/الإيميل
            return await _unitOfWork.Repository<Supplier>()
                .FindAsync(s => s.IsActive &&
                                (
                                    (s.SupplierName != null && s.SupplierName.Contains(term)) ||
                                    (s.Phone        != null && s.Phone.Contains(term)) ||
                                    (s.Email        != null && s.Email.Contains(term))
                                ));
        }

        public async Task<bool> ExistsAsync(int supplierId)
        {
            return await _unitOfWork.Repository<Supplier>().AnyAsync(s => s.SupplierId == supplierId);
        }

        public async Task<decimal> GetSupplierBalanceAsync(int supplierId)
        {
            var supplier = await _unitOfWork.Repository<Supplier>().GetByIdAsync(supplierId);
            return supplier?.Balance ?? 0m;
        }

    }
}
