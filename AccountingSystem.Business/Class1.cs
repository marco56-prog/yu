using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business;

// خدمة إدارة العملاء
public interface ICustomerService
{
    Task<IEnumerable<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(int id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(int id);
    Task<decimal> GetCustomerBalanceAsync(int customerId);
    Task UpdateCustomerBalanceAsync(int customerId, decimal amount, string description, string referenceType = "", int? referenceId = null);
}

// تنفيذ خدمة إدارة العملاء
public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INumberSequenceService _numberSequenceService;

    public CustomerService(IUnitOfWork unitOfWork, INumberSequenceService numberSequenceService)
    {
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        // إرجاع العملاء النشطين فقط
        return await _unitOfWork.Repository<Customer>().FindAsync(c => c.IsActive);
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _unitOfWork.Repository<Customer>().GetByIdAsync(id);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        // توحيد وتهيئة المدخلات
        customer.CustomerName = (customer.CustomerName ?? string.Empty).Trim();
        customer.Address = (customer.Address ?? string.Empty).Trim();
        customer.Phone = (customer.Phone ?? string.Empty).Trim();
        customer.Email = (customer.Email ?? string.Empty).Trim();

        // التحقق من تكرار الاسم (حسّاس لحالة الأحرف بشكل مُوحّد)
        var nameNorm = customer.CustomerName;
        var existingCustomer = await _unitOfWork.Repository<Customer>()
            .SingleOrDefaultAsync(c => c.IsActive && c.CustomerName.Equals(nameNorm, StringComparison.OrdinalIgnoreCase));

        if (existingCustomer != null)
            throw new InvalidOperationException("اسم العميل موجود بالفعل.");

        customer.CreatedDate = DateTime.Now;
        customer.IsActive = true;
        customer.Balance = 0;

        await _unitOfWork.Repository<Customer>().AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        var repo = _unitOfWork.Repository<Customer>();
        var existingCustomer = await repo.GetByIdAsync(customer.CustomerId);
        if (existingCustomer == null)
            throw new InvalidOperationException("العميل غير موجود.");

        // توحيد وتهيئة المدخلات
        var newName = (customer.CustomerName ?? string.Empty).Trim();
        var newAddr = (customer.Address ?? string.Empty).Trim();
        var newPhone = (customer.Phone ?? string.Empty).Trim();
        var newEmail = (customer.Email ?? string.Empty).Trim();

        // منع التكرار لنفس الاسم لعميل آخر نشط
        var duplicateCustomer = await _unitOfWork.Repository<Customer>()
            .SingleOrDefaultAsync(c => c.IsActive && c.CustomerId != customer.CustomerId && c.CustomerName.Equals(newName, StringComparison.OrdinalIgnoreCase));

        if (duplicateCustomer != null)
            throw new InvalidOperationException("اسم العميل موجود بالفعل.");

        existingCustomer.CustomerName = newName;
        existingCustomer.Address = newAddr;
        existingCustomer.Phone = newPhone;
        existingCustomer.Email = newEmail;

        repo.Update(existingCustomer);
        await _unitOfWork.SaveChangesAsync();

        return existingCustomer;
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        var repo = _unitOfWork.Repository<Customer>();
        var customer = await repo.GetByIdAsync(id);
        if (customer == null) return false;

        // منع الحذف لو هناك فواتير مرتبطة
        var hasSalesInvoices = await _unitOfWork.Repository<SalesInvoice>()
            .AnyAsync(s => s.CustomerId == id);
        if (hasSalesInvoices)
            throw new InvalidOperationException("لا يمكن حذف العميل لوجود فواتير مرتبطة به.");

        // منع الحذف لو الرصيد غير صفري
        if (customer.Balance != 0m)
            throw new InvalidOperationException("لا يمكن حذف العميل ورصيده غير صفري.");

        // إلغاء تفعيل بدل الحذف الفعلي
        customer.IsActive = false;
        repo.Update(customer);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<decimal> GetCustomerBalanceAsync(int customerId)
    {
        var customer = await _unitOfWork.Repository<Customer>().GetByIdAsync(customerId);
        return customer?.Balance ?? 0m;
    }

    public async Task UpdateCustomerBalanceAsync(int customerId, decimal amount, string description, string referenceType = "", int? referenceId = null)
    {
        var repo = _unitOfWork.Repository<Customer>();
        var customer = await repo.GetByIdAsync(customerId);
        if (customer == null)
            throw new InvalidOperationException("العميل غير موجود.");

        // في حالة عدم وجود تغيير فعلي
        if (amount == 0m) return;

        _unitOfWork.BeginTransaction();
        try
        {
            // تحديث رصيد العميل (موجب = مدين عليه / سالب = دائن له حسب نظامك)
            customer.Balance = customer.Balance + amount;
            repo.Update(customer);

            // إضافة قيد حركة للعميل
            var transaction = new CustomerTransaction
            {
                CustomerId = customerId,
                Customer = customer,
                TransactionNumber = await _numberSequenceService.GenerateCashTransactionNumberAsync(),
                TransactionType = amount > 0 ? TransactionType.Income : TransactionType.Expense,
                Amount = Math.Abs(amount),
                Description = (description ?? string.Empty).Trim(),
                ReferenceType = referenceType ?? string.Empty,
                ReferenceId = referenceId,
                TransactionDate = DateTime.Now,
                CreatedBy = 1 // TODO: استبدالها بالمستخدم الحالي
            };

            await _unitOfWork.Repository<CustomerTransaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            _unitOfWork.RollbackTransaction();
            throw;
        }
    }
}
