using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Data;

internal static class DatabaseExtensions
{
    public static bool IsInMemory(this DatabaseFacade database)
    {
        var provider = database.ProviderName ?? string.Empty;
        return provider.Contains("InMemory", StringComparison.OrdinalIgnoreCase);
    }
}
