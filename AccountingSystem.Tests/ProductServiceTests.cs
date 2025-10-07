using AccountingSystem.Business;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.Tests;

public class ProductServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AccountingDbContext _context;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        var services = new ServiceCollection();
        
        // إعداد قاعدة بيانات في الذاكرة للاختبارات
        services.AddDbContext<AccountingDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // Register number sequence service used by ProductService
        services.AddScoped<AccountingSystem.Data.INumberSequenceService, AccountingSystem.Data.NumberSequenceService>();
    // Also register supporting services used in ProductService constructor
    services.AddScoped<AccountingSystem.Business.IPriceHistoryService, AccountingSystem.Business.PriceHistoryService>();
    services.AddScoped<AccountingSystem.Business.IProductService, AccountingSystem.Business.ProductService>();
    services.AddScoped<ProductService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AccountingDbContext>();
        _productService = _serviceProvider.GetRequiredService<ProductService>();
    }

    [Fact]
    public async Task CreateProduct_ShouldCreateProductSuccessfully()
    {
        // Arrange
        var product = new Product
        {
            ProductName = "منتج تجريبي",
            ProductCode = "TEST001",
            PurchasePrice = 100m,
            SalePrice = 150m,
            CurrentStock = 50m,
            MinimumStock = 10m,
            Unit = "قطعة",
            IsActive = true
        };

        // Act
        var result = await _productService.CreateProductAsync(product);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Data.ProductId);
        
        var createdProduct = await _context.Products.FindAsync(result.Data.ProductId);
        Assert.NotNull(createdProduct);
        Assert.Equal("منتج تجريبي", createdProduct.ProductName);
        Assert.Equal("TEST001", createdProduct.ProductCode);
    }

    [Fact]
    public async Task GetProductByCode_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var product = new Product
        {
            ProductName = "منتج للبحث",
            ProductCode = "SEARCH001",
            PurchasePrice = 200m,
            SalePrice = 300m,
            CurrentStock = 25m,
            MinimumStock = 5m,
            Unit = "كيلو",
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "TestUser"
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetProductByCodeAsync("SEARCH001");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("منتج للبحث", result.Data.ProductName);
        Assert.Equal("SEARCH001", result.Data.ProductCode);
    }

    [Fact]
    public async Task GetProductByCode_ShouldReturnFailure_WhenProductNotExists()
    {
        // Act
        var result = await _productService.GetProductByCodeAsync("NOTEXIST");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Contains("لم يتم العثور على المنتج", result.Message);
    }

    [Fact]
    public async Task UpdateStock_ShouldUpdateStockCorrectly()
    {
        // Arrange
        var product = new Product
        {
            ProductName = "منتج للمخزون",
            ProductCode = "STOCK001",
            PurchasePrice = 50m,
            SalePrice = 75m,
            CurrentStock = 100m,
            MinimumStock = 20m,
            Unit = "قطعة",
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "TestUser"
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.UpdateStockAsync(product.ProductId, 150m, "TestUser");

        // Assert
        Assert.True(result.IsSuccess);
        
        var updatedProduct = await _context.Products.FindAsync(product.ProductId);
        Assert.NotNull(updatedProduct);
        Assert.Equal(150m, updatedProduct.CurrentStock);
    }

    [Fact]
    public async Task CreateProduct_ShouldValidateRequiredFields()
    {
        // Arrange - منتج بدون اسم
        var product = new Product
        {
            ProductName = "", // اسم فارغ
            ProductCode = "INVALID001",
            PurchasePrice = 100m,
            SalePrice = 150m
        };

        // Act & Assert
        var result = await _productService.CreateProductAsync(product);
        Assert.False(result.IsSuccess);
        Assert.Contains("اسم المنتج مطلوب", result.Message);
    }

    [Fact]
    public async Task GetLowStockProducts_ShouldReturnProductsBelowMinimum()
    {
        // Arrange
        var lowStockProduct = new Product
        {
            ProductName = "منتج مخزون قليل",
            ProductCode = "LOW001",
            CurrentStock = 5m,
            MinimumStock = 10m,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "TestUser"
        };

        var normalStockProduct = new Product
        {
            ProductName = "منتج مخزون طبيعي",
            ProductCode = "NORMAL001", 
            CurrentStock = 50m,
            MinimumStock = 10m,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "TestUser"
        };

        _context.Products.AddRange(lowStockProduct, normalStockProduct);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetLowStockProductsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("منتج مخزون قليل", result.Data.First().ProductName);
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}