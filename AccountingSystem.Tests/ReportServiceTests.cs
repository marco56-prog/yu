using Xunit;
using Microsoft.EntityFrameworkCore;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.Business;
using System.Threading.Tasks;
using System;

namespace AccountingSystem.Tests
{
    public class ReportServiceTests
    {
        private AccountingDbContext _context;
        private ReportService _reportService;

        public ReportServiceTests()
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseInMemoryDatabase(databaseName: "AccountingTestDb_Reports")
                .Options;
            _context = new AccountingDbContext(options);
            _reportService = new ReportService(_context);
        }

        [Fact]
        public async Task GenerateSalesReportAsync_WithData_ReturnsCorrectTotals()
        {
            // Arrange
            var customer = new Customer { CustomerName = "Test Customer" };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var invoice1 = new SalesInvoice
            {
                InvoiceNumber = "INV-002",
                CustomerId = customer.CustomerId,
                InvoiceDate = new DateTime(2023, 1, 15),
                IsPosted = true,
                SubTotal = 100,
                TaxAmount = 10,
                DiscountAmount = 5,
                NetTotal = 105,
                TotalAmount = 105,
                PaidAmount = 105,
                RemainingAmount = 0,
                Status = InvoiceStatus.Posted
            };
            var invoice2 = new SalesInvoice
            {
                InvoiceNumber = "INV-003",
                CustomerId = customer.CustomerId,
                InvoiceDate = new DateTime(2023, 1, 20),
                IsPosted = true,
                SubTotal = 200,
                TaxAmount = 20,
                DiscountAmount = 10,
                NetTotal = 210,
                TotalAmount = 210,
                PaidAmount = 210,
                RemainingAmount = 0,
                Status = InvoiceStatus.Posted
            };
            _context.SalesInvoices.AddRange(invoice1, invoice2);
            await _context.SaveChangesAsync();

            // Act
            var report = await _reportService.GenerateSalesReportAsync(new DateTime(2023, 1, 1), new DateTime(2023, 1, 31));

            // Assert
            Assert.Equal(2, report.InvoiceCount);
            Assert.Equal(300, report.TotalSales);
            Assert.Equal(30, report.TotalTax);
            Assert.Equal(15, report.TotalDiscount);
            Assert.Equal(315, report.NetSales);
        }
    }
}