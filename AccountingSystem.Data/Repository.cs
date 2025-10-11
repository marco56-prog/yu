using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
using AccountingSystem.Models;

namespace AccountingSystem.Data;

// الواجهة العامة للمستودع (بدون تغيير)
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    IQueryable<T> Where(Expression<Func<T, bool>> predicate);
}

// تنفيذ المستودع العام
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AccountingDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AccountingDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // ملاحظة: FindAsync يعيد كيان متعقَّب من EF، وهو مناسب للتحديث لاحقًا
    public virtual async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    // قراءات بدون تتبّع للأداء؛ استخدم Update/Attach للتعديل
    public virtual async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.AsNoTracking().ToListAsync();

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync();

    public virtual async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AsNoTracking().SingleOrDefaultAsync(predicate);

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AnyAsync(predicate);

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        => predicate == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);

    public virtual async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        => await _dbSet.AddRangeAsync(entities);

    public virtual void Update(T entity)
        => _dbSet.Update(entity);

    public virtual void UpdateRange(IEnumerable<T> entities)
        => _dbSet.UpdateRange(entities);

    public virtual void Remove(T entity)
        => _dbSet.Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities)
        => _dbSet.RemoveRange(entities);

    public virtual IQueryable<T> Where(Expression<Func<T, bool>> predicate)
        => _dbSet.Where(predicate);
}

// واجهة وحدة العمل مع جميع الخصائص المطلوبة
public interface IUnitOfWork : IDisposable
{
    AccountingDbContext Context { get; }
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task<int> SaveAsync(); // إضافة SaveAsync كما هو مستخدم في الكود
    Task<int> CompleteAsync();
    void BeginTransaction();
    Task CommitTransactionAsync();
    void RollbackTransaction();

    // خصائص Repository للوصول المباشر
    IRepository<Customer> Customers { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<Product> Products { get; }
    IRepository<SalesInvoice> SalesInvoices { get; }
    IRepository<PurchaseInvoice> PurchaseInvoices { get; }
    IRepository<StockMovement> StockMovements { get; }
    IRepository<SalesInvoiceItem> SalesInvoiceItems { get; }
    IRepository<PurchaseInvoiceItem> PurchaseInvoiceItems { get; }
    IRepository<AuditLog> AuditLogs { get; }
}

// تنفيذ وحدة العمل مع جميع الخصائص المطلوبة
public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly AccountingDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;

    public AccountingDbContext Context => _context;

    // خصائص Repository للوصول المباشر
    private IRepository<Customer>? _customers;
    private IRepository<Supplier>? _suppliers;
    private IRepository<Product>? _products;
    private IRepository<SalesInvoice>? _salesInvoices;
    private IRepository<PurchaseInvoice>? _purchaseInvoices;
    private IRepository<StockMovement>? _stockMovements;
    private IRepository<SalesInvoiceItem>? _salesInvoiceItems;
    private IRepository<PurchaseInvoiceItem>? _purchaseInvoiceItems;
    private IRepository<AuditLog>? _auditLogs;

    public IRepository<Customer> Customers => _customers ??= Repository<Customer>();
    public IRepository<Supplier> Suppliers => _suppliers ??= Repository<Supplier>();
    public IRepository<Product> Products => _products ??= Repository<Product>();
    public IRepository<SalesInvoice> SalesInvoices => _salesInvoices ??= Repository<SalesInvoice>();
    public IRepository<PurchaseInvoice> PurchaseInvoices => _purchaseInvoices ??= Repository<PurchaseInvoice>();
    public IRepository<StockMovement> StockMovements => _stockMovements ??= Repository<StockMovement>();
    public IRepository<SalesInvoiceItem> SalesInvoiceItems => _salesInvoiceItems ??= Repository<SalesInvoiceItem>();
    public IRepository<PurchaseInvoiceItem> PurchaseInvoiceItems => _purchaseInvoiceItems ??= Repository<PurchaseInvoiceItem>();
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= Repository<AuditLog>();

    public UnitOfWork(AccountingDbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var repo))
            return (IRepository<T>)repo;

        var repository = new Repository<T>(_context);
        _repositories[typeof(T)] = repository;
        return repository;
    }

    // حفظ التغييرات
    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    // حفظ التغييرات (alias لـ SaveChangesAsync) - مطلوب من الكود
    public async Task<int> SaveAsync()
        => await SaveChangesAsync();

    // حفظ التغييرات (alias لـ SaveChangesAsync)
    public async Task<int> CompleteAsync()
        => await SaveChangesAsync();

    // بدء معاملة واحدة نشطة فقط
    public void BeginTransaction()
    {
        if (_transaction != null)
            throw new InvalidOperationException("هناك معاملة نشطة بالفعل. يجب إنهاؤها قبل بدء معاملة جديدة.");

        // InMemory provider does not support transactions - treat as no-op in tests
        if (_context.Database.IsInMemory())
        {
            // No transaction will be created for InMemory provider
            _transaction = null;
            return;
        }

        _transaction = _context.Database.BeginTransaction();
    }

    // إنهاء المعاملة الحالية بالالتزام
    public async Task CommitTransactionAsync()
    {
        // If no transaction was created (e.g. InMemory provider), treat commit as no-op
        if (_transaction == null)
            return;

        try
        {
            await _transaction.CommitAsync();
        }
        catch
        {
            // لو فشل الالتزام لأي سبب، نضمن التراجع
            try { await _transaction.RollbackAsync(); } catch { /* تجاهل */ }
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    // تراجع عن المعاملة الحالية
    public void RollbackTransaction()
    {
        if (_transaction == null)
            return;

        try
        {
            _transaction.Rollback();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    // التخلص المُعتاد
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // التخلص غير المتزامن (اختياري)
    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
