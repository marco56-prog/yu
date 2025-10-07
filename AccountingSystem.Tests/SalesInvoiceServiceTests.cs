using AccountingSystem.Business;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.Tests;

public class SalesInvoiceServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AccountingDbContext _context;
    private readonly SalesInvoiceService _salesInvoiceService;

    public SalesInvoiceServiceTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<AccountingDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // Register number sequence service used by SalesInvoiceService
        services.AddScoped<AccountingSystem.Data.INumberSequenceService, AccountingSystem.Data.NumberSequenceService>();
    // Register business services required by SalesInvoiceService
    services.AddScoped<AccountingSystem.Business.IProductService, AccountingSystem.Business.ProductService>();
    services.AddScoped<AccountingSystem.Business.IPriceHistoryService, AccountingSystem.Business.PriceHistoryService>();
    services.AddScoped<AccountingSystem.Business.ICustomerService, AccountingSystem.Business.CustomerService>();
        services.AddScoped<SalesInvoiceService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AccountingDbContext>();
        _salesInvoiceService = _serviceProvider.GetRequiredService<SalesInvoiceService>();
        
        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        // إضافة عميل تجريبي
        var customer = new Customer
        {
            CustomerName = "عميل تجريبي",
            Phone = "123456789",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "TestUser"
        };
        _context.Customers.Add(customer);

        // إضافة منتج تجريبي
        var product = new Product
        {
            ProductName = "منتج للبيع",
            ProductCode = "SALE001",
            PurchasePrice = 100m,
            SalePrice = 150m,
            CurrentStock = 100m,
            MinimumStock = 10m,
            Unit = "قطعة",
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "TestUser"
        };
        // ensure there is a unit and set as product main unit
        var unit = new Unit
        {
            UnitName = "قطعة",
            UnitSymbol = "pcs",
            IsActive = true
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        product.MainUnitId = unit.UnitId;
        _context.Products.Add(product);

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateSalesInvoice_ShouldCreateInvoiceSuccessfully()
    {
        // Arrange
        var customer = await _context.Customers.FirstAsync();
        var product = await _context.Products.FirstAsync();

        var invoice = new SalesInvoice
        {
            CustomerId = customer.CustomerId,
            InvoiceDate = DateTime.Now,
            Notes = "فاتورة تجريبية",
            IsDraft = false,
            CreatedBy = "TestUser"
        };

        var invoiceItem = new SalesInvoiceItem
        {
            ProductId = product.ProductId,
            Quantity = 5m,
            UnitPrice = 150m,
            Discount = 0m
        };
        
        invoice.SalesInvoiceItems.Add(invoiceItem);

        // Act
        var result = await _salesInvoiceService.CreateSalesInvoiceAsync(invoice);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Data.SalesInvoiceId);
        Assert.NotNull(result.Data.InvoiceNumber);
        
        // التحقق من تحديث المخزون
        var updatedProduct = await _context.Products.FindAsync(product.ProductId);
        Assert.Equal(95m, updatedProduct!.CurrentStock); // 100 - 5 = 95
    }

    [Fact]
    public async Task CreateSalesInvoice_ShouldCalculateTotalsCorrectly()
    {
        // Arrange
        var customer = await _context.Customers.FirstAsync();
        var product = await _context.Products.FirstAsync();

        var invoice = new SalesInvoice
        {
            CustomerId = customer.CustomerId,
            InvoiceDate = DateTime.Now,
            TaxRate = 14m, // ضريبة 14%
            CreatedBy = "TestUser"
        };

        var invoiceItem = new SalesInvoiceItem
        {
            ProductId = product.ProductId,
            Quantity = 10m,
            UnitPrice = 100m,
            Discount = 50m // خصم 50 جنيه
        };
        
        invoice.SalesInvoiceItems.Add(invoiceItem);

        // Act
        var result = await _salesInvoiceService.CreateSalesInvoiceAsync(invoice);

        // Assert
        Assert.True(result.IsSuccess);
        
        var createdInvoice = result.Data;
        Assert.Equal(950m, createdInvoice.SubTotal); // (10 * 100) - 50 = 950
        Assert.Equal(133m, createdInvoice.TaxAmount); // 950 * 0.14 = 133
        Assert.Equal(1083m, createdInvoice.TotalAmount); // 950 + 133 = 1083
    }

    [Fact]
    public async Task CreateSalesInvoice_ShouldFailWhenInsufficientStock()
    {
        // Arrange
        var customer = await _context.Customers.FirstAsync();
        var product = await _context.Products.FirstAsync();

        var invoice = new SalesInvoice
        {
            CustomerId = customer.CustomerId,
            InvoiceDate = DateTime.Now,
            CreatedBy = "TestUser"
        };

        var invoiceItem = new SalesInvoiceItem
        {
            ProductId = product.ProductId,
            Quantity = 200m, // كمية أكبر من المخزون (100)
            UnitPrice = 150m,
            Discount = 0m
        };
        
        invoice.SalesInvoiceItems.Add(invoiceItem);

        // Act
        var result = await _salesInvoiceService.CreateSalesInvoiceAsync(invoice);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("المخزون غير كافي", result.Message);
    }

    [Fact]
    public async Task GetSalesInvoicesByDateRange_ShouldReturnCorrectInvoices()
    {
        // Arrange
        var customer = await _context.Customers.FirstAsync();
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(1);

        // إنشاء فاتورة في النطاق المحدد
        var invoiceInRange = new SalesInvoice
        {
            CustomerId = customer.CustomerId,
            InvoiceDate = DateTime.Today.AddHours(10),
            InvoiceNumber = "INV001",
            SubTotal = 500m,
            TaxAmount = 70m,
            TotalAmount = 570m,
            CreatedAt = DateTime.Now,
            CreatedBy = "TestUser"
        };

        // إنشاء فاتورة خارج النطاق
        var invoiceOutOfRange = new SalesInvoice
        {
            CustomerId = customer.CustomerId,
            InvoiceDate = DateTime.Today.AddDays(-5),
            InvoiceNumber = "INV002",
            SubTotal = 300m,
            TaxAmount = 42m,
            TotalAmount = 342m,
            CreatedAt = DateTime.Now,
            CreatedBy = "TestUser"
        };

        _context.SalesInvoices.AddRange(invoiceInRange, invoiceOutOfRange);
        await _context.SaveChangesAsync();

        // Act
        var result = await _salesInvoiceService.GetSalesInvoicesByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("INV001", result.Data.First().InvoiceNumber);
    }

    [Fact]
    public async Task SaveAsDraft_ShouldCreateDraftInvoice()
    {
        // Arrange
        var customer = await _context.Customers.FirstAsync();
        var product = await _context.Products.FirstAsync();

        var invoice = new SalesInvoice
        {
            CustomerId = customer.CustomerId,
            InvoiceDate = DateTime.Now,
            Notes = "فاتورة مسودة",
            IsDraft = true,
            CreatedBy = "TestUser"
        };

        var invoiceItem = new SalesInvoiceItem
        {
            ProductId = product.ProductId,
            Quantity = 3m,
            UnitPrice = 150m,
            Discount = 0m
        };
        
        invoice.SalesInvoiceItems.Add(invoiceItem);

        // Act
        var result = await _salesInvoiceService.SaveAsDraftAsync(invoice);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data.IsDraft);
        
        // التحقق من عدم تحديث المخزون للمسودات
        var product_after = await _context.Products.FindAsync(product.ProductId);
        Assert.Equal(100m, product_after!.CurrentStock); // لم يتغير
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}