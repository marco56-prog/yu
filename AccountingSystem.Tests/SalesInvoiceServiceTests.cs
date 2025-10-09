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

        // Corrected logic:
        // SubTotal is the gross total before item discounts: 10 * 100 = 1000
        // DiscountAmount is the sum of item discounts: 50
        // Taxable amount is SubTotal - DiscountAmount: 1000 - 50 = 950
        // TaxAmount is calculated on the taxable amount: 950 * 0.14 = 133
        // TotalAmount (NetTotal) is taxable amount + tax: 950 + 133 = 1083
        Assert.Equal(1000m, createdInvoice.SubTotal);
        Assert.Equal(50m, createdInvoice.DiscountAmount);
        Assert.Equal(133m, createdInvoice.TaxAmount);
        Assert.Equal(1083m, createdInvoice.TotalAmount);
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

    [Fact]
    public async Task CreateSalesInvoice_ShouldCalculateNetTotalCorrectly_WhenItemDiscountExists()
    {
        // Arrange
        var customer = await _context.Customers.FirstAsync();
        var product = await _context.Products.FirstAsync();

        var invoice = new SalesInvoice
        {
            CustomerId = customer.CustomerId,
            InvoiceDate = DateTime.Now,
            PaidAmount = 0, // No payment
            CreatedBy = "TestUser"
        };

        invoice.Items.Add(new SalesInvoiceItem
        {
            ProductId = product.ProductId,
            Quantity = 10,
            UnitPrice = 100, // TotalPrice = 1000
            DiscountAmount = 50 // Item discount
        });

        invoice.Items.Add(new SalesInvoiceItem
        {
            ProductId = product.ProductId,
            Quantity = 5,
            UnitPrice = 80, // TotalPrice = 400
            DiscountAmount = 20 // Item discount
        });

        // Expected calculations:
        // SubTotal should be sum of TotalPrices: (10 * 100) + (5 * 80) = 1400
        // TotalDiscount on invoice should be sum of item discounts: 50 + 20 = 70
        // NetTotal = SubTotal - TotalDiscount = 1400 - 70 = 1330
        // RemainingAmount = 1330 (since PaidAmount is 0)

        // Buggy calculation that is currently in place:
        // Item1 NetAmount = 1000 - 50 = 950
        // Item2 NetAmount = 400 - 20 = 380
        // SubTotal (buggy) = 950 + 380 = 1330
        // TotalDiscount = 50 + 20 = 70
        // NetTotal (buggy) = SubTotal - TotalDiscount = 1330 - 70 = 1260 (INCORRECT)

        // Act
        var result = await _salesInvoiceService.CreateSalesInvoiceAsync(invoice);

        // Assert
        Assert.True(result.IsSuccess);
        var createdInvoice = result.Data;

        // These assertions will fail with the current buggy logic, proving the bug exists.
        Assert.Equal(1400m, createdInvoice.SubTotal);
        Assert.Equal(70m, createdInvoice.DiscountAmount);
        Assert.Equal(1330m, createdInvoice.NetTotal);
        Assert.Equal(1330m, createdInvoice.RemainingAmount);
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}