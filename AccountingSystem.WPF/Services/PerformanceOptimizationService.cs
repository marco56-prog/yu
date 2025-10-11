using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using AccountingSystem.Models;
using AccountingSystem.Business;
using AccountingSystem.WPF.Helpers;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة تحسين الأداء مع Memory Caching وLazy Loading
    /// </summary>
    public interface IPerformanceOptimizationService
    {
        Task<List<Customer>> GetCachedCustomersAsync();
        Task<List<Product>> GetCachedProductsAsync();
        Task<Customer?> GetCustomerByIdAsync(int customerId);
        Task<Product?> GetProductByIdAsync(int productId);
        void InvalidateCache(string cacheKey);
        void InvalidateAllCaches();
        Task PreloadDataAsync();
    }

    public class PerformanceOptimizationService : IPerformanceOptimizationService
    {
        private const string ComponentName = "PerformanceOptimizationService";
        private const string CustomersKey = "AllCustomers";
        private const string ProductsKey = "AllProducts";
        private const string CustomerPrefix = "Customer_";
        private const string ProductPrefix = "Product_";

        private readonly IMemoryCache _cache;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly ConcurrentDictionary<string, object> _lockObjects;

        public PerformanceOptimizationService(
            IMemoryCache cache,
            ICustomerService customerService,
            IProductService productService)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _lockObjects = new ConcurrentDictionary<string, object>();
        }

        public async Task<List<Customer>> GetCachedCustomersAsync()
        {
            const string key = CustomersKey;

            if (_cache.TryGetValue(key, out List<Customer>? cachedCustomers) && cachedCustomers != null)
            {
                ComprehensiveLogger.LogPerformanceOperation("تم استرجاع العملاء من الكاش", ComponentName, cachedCustomers.Count);
                return cachedCustomers;
            }

            var lockObject = _lockObjects.GetOrAdd(key, _ => new object());

            lock (lockObject)
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(key, out cachedCustomers) && cachedCustomers != null)
                    return cachedCustomers;

                ComprehensiveLogger.LogPerformanceOperation("بدء تحميل العملاء من قاعدة البيانات", ComponentName);

                var customers = Task.Run(async () => await _customerService.GetAllCustomersAsync()).Result;

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.High
                };

                _cache.Set(key, customers, cacheOptions);

                ComprehensiveLogger.LogPerformanceOperation("تم تحميل وحفظ العملاء في الكاش", ComponentName, customers.Count());
                return customers.ToList();
            }
        }

        public async Task<List<Product>> GetCachedProductsAsync()
        {
            const string key = ProductsKey;

            if (_cache.TryGetValue(key, out List<Product>? cachedProducts) && cachedProducts != null)
            {
                ComprehensiveLogger.LogPerformanceOperation("تم استرجاع المنتجات من الكاش", ComponentName, cachedProducts.Count);
                return cachedProducts;
            }

            var lockObject = _lockObjects.GetOrAdd(key, _ => new object());

            lock (lockObject)
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(key, out cachedProducts) && cachedProducts != null)
                    return cachedProducts;

                ComprehensiveLogger.LogPerformanceOperation("بدء تحميل المنتجات من قاعدة البيانات", ComponentName);

                var products = Task.Run(async () => await _productService.GetAllProductsAsync()).Result;

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                    SlidingExpiration = TimeSpan.FromMinutes(7),
                    Priority = CacheItemPriority.High
                };

                _cache.Set(key, products, cacheOptions);

                ComprehensiveLogger.LogPerformanceOperation("تم تحميل وحفظ المنتجات في الكاش", ComponentName, products.Count());
                return products.ToList();
            }
        }

        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            string key = CustomerPrefix + customerId;

            if (_cache.TryGetValue(key, out Customer? cachedCustomer))
                return cachedCustomer;

            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(customerId);

                if (customer != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                        SlidingExpiration = TimeSpan.FromMinutes(10),
                        Priority = CacheItemPriority.Normal
                    };

                    _cache.Set(key, customer, cacheOptions);
                }

                return customer;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل تحميل العميل {customerId}", ex, ComponentName);
                return null;
            }
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            string key = ProductPrefix + productId;

            if (_cache.TryGetValue(key, out Product? cachedProduct))
                return cachedProduct;

            try
            {
                var product = await _productService.GetProductByIdAsync(productId);

                if (product != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20),
                        SlidingExpiration = TimeSpan.FromMinutes(8),
                        Priority = CacheItemPriority.Normal
                    };

                    _cache.Set(key, product, cacheOptions);
                }

                return product;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل تحميل المنتج {productId}", ex, ComponentName);
                return null;
            }
        }

        public void InvalidateCache(string cacheKey)
        {
            try
            {
                _cache.Remove(cacheKey);
                ComprehensiveLogger.LogPerformanceOperation($"تم إلغاء الكاش: {cacheKey}", ComponentName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل إلغاء الكاش: {cacheKey}", ex, ComponentName);
            }
        }

        public void InvalidateAllCaches()
        {
            try
            {
                if (_cache is MemoryCache memoryCache)
                {
                    memoryCache.Dispose();
                }

                InvalidateCache(CustomersKey);
                InvalidateCache(ProductsKey);

                ComprehensiveLogger.LogPerformanceOperation("تم إلغاء جميع الكاشات", ComponentName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل إلغاء جميع الكاشات", ex, ComponentName);
            }
        }

        public async Task PreloadDataAsync()
        {
            try
            {
                ComprehensiveLogger.LogPerformanceOperation("بدء تحميل البيانات مسبقاً", ComponentName);

                // تحميل البيانات الأساسية في الخلفية
                var customersTask = GetCachedCustomersAsync();
                var productsTask = GetCachedProductsAsync();

                await Task.WhenAll(customersTask, productsTask);

                ComprehensiveLogger.LogPerformanceOperation("تم تحميل البيانات مسبقاً بنجاح", ComponentName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تحميل البيانات مسبقاً", ex, ComponentName);
            }
        }
    }
}