using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccountingSystem.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AccountingDbContext>
{
    public AccountingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountingDbContext>();

        var cs = Environment.GetEnvironmentVariable("ACCOUNTING_DB_CS")
                 ?? "Server=(localdb)\\mssqllocaldb;Database=AccountingSystemDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

        optionsBuilder.UseSqlServer(cs);
        return new AccountingDbContext(optionsBuilder.Options);
    }
}
