using Xunit;
using Microsoft.EntityFrameworkCore;
using AccountingSystem.Data;
using AccountingSystem.Models;
using System.Linq;

namespace AccountingSystem.Tests
{
    public class AccountingCorrectnessTests
    {
        private AccountingDbContext _context;

        public AccountingCorrectnessTests()
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseInMemoryDatabase(databaseName: "AccountingTestDb")
                .Options;
            _context = new AccountingDbContext(options);
        }

        [Fact]
        public void TestInvoiceTotals()
        {
            // Arrange
            var invoice = new SalesInvoice
            {
                InvoiceNumber = "INV-001",
                CustomerId = 1,
                InvoiceDate = System.DateTime.Now,
                Items =
                {
                    new SalesInvoiceItem { ProductId = 1, Quantity = 2, UnitPrice = 10, LineTotal = 20 },
                    new SalesInvoiceItem { ProductId = 2, Quantity = 3, UnitPrice = 5, LineTotal = 15 }
                },
                TotalAmount = 35
            };
            _context.SalesInvoices.Add(invoice);
            _context.SaveChanges();

            // Act
            var savedInvoice = _context.SalesInvoices.Include(i => i.Items).First();
            var calculatedTotal = savedInvoice.Items.Sum(item => item.LineTotal);

            // Assert
            Assert.Equal(savedInvoice.TotalAmount, calculatedTotal);
        }

        [Fact]
        public void TestPurchaseInvoiceTotals()
        {
            // Arrange
            var purchaseInvoice = new PurchaseInvoice
            {
                InvoiceNumber = "PINV-001",
                SupplierId = 1,
                InvoiceDate = System.DateTime.Now,
                Items =
                {
                    new PurchaseInvoiceItem { ProductId = 1, Quantity = 2, UnitCost = 10, LineTotal = 20 },
                    new PurchaseInvoiceItem { ProductId = 2, Quantity = 3, UnitCost = 5, LineTotal = 15 }
                },
                TotalAmount = 35
            };
            _context.PurchaseInvoices.Add(purchaseInvoice);
            _context.SaveChanges();

            // Act
            var savedInvoice = _context.PurchaseInvoices.Include(i => i.Items).First();
            var calculatedTotal = savedInvoice.Items.Sum(item => item.LineTotal);

            // Assert
            Assert.Equal(savedInvoice.TotalAmount, calculatedTotal);
        }
    }
}