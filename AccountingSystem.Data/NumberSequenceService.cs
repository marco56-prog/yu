using System.Data;
using Microsoft.EntityFrameworkCore;
using AccountingSystem.Models;

namespace AccountingSystem.Data;

// واجهة خدمة توليد الأرقام التلقائية
public interface INumberSequenceService
{
    Task<string> GetNextNumberAsync(string sequenceType);
    Task<string> GenerateCustomerCodeAsync();
    Task<string> GenerateSupplierCodeAsync();
    Task<string> GenerateProductCodeAsync();
    Task<string> GenerateSalesInvoiceNumberAsync();
    Task<string> GeneratePurchaseInvoiceNumberAsync();
    Task<string> GenerateCashTransactionNumberAsync();
}

// تنفيذ خدمة توليد الأرقام التلقائية
public class NumberSequenceService : INumberSequenceService
{
    private readonly AccountingDbContext _context;

    public NumberSequenceService(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetNextNumberAsync(string sequenceType)
    {
        if (string.IsNullOrWhiteSpace(sequenceType))
            throw new ArgumentException("sequenceType لا يمكن أن يكون فارغاً.", nameof(sequenceType));

        // استراتيجية إعادة المحاولة (SQL Server transient faults)
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Fast-path for InMemory provider: it doesn't support transactions
            if (_context.Database.IsInMemory())
            {
                var seq = await _context.NumberSequences
                    .FirstOrDefaultAsync(s => s.SequenceType == sequenceType);


                if (seq == null)
                {
                    seq = new NumberSequence
                    {
                        SequenceType = sequenceType,
                        CurrentNumber = 0,
                        Prefix = GetDefaultPrefix(sequenceType),
                        NumberLength = 6,
                        UpdatedDate = DateTime.UtcNow
                    };

                    _context.NumberSequences.Add(seq);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        // Retry: try to read existing row; if still missing create it again.
                        seq = await _context.NumberSequences
                            .FirstOrDefaultAsync(s => s.SequenceType == sequenceType);

                        if (seq == null)
                        {
                            // Another safe-guard: create again and save
                            seq = new NumberSequence
                            {
                                SequenceType = sequenceType,
                                CurrentNumber = 0,
                                Prefix = GetDefaultPrefix(sequenceType),
                                NumberLength = 6,
                                UpdatedDate = DateTime.UtcNow
                            };

                            _context.NumberSequences.Add(seq);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                seq.CurrentNumber++;
                seq.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return BuildNumber(seq, seq.CurrentNumber);
            }

            // عزل Serializable يمنع السباق على السجل/النطاق
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            // جرّب تجيب التسلسل
            var seq2 = await _context.NumberSequences
                .FirstOrDefaultAsync(s => s.SequenceType == sequenceType);

            // إنشاء أول مرة مع معالجة احتمال تضارب المفتاح الفريد
            if (seq2 == null)
            {
                seq2 = new NumberSequence
                {
                    SequenceType = sequenceType,
                    CurrentNumber = 0,
                    Prefix = GetDefaultPrefix(sequenceType),
                    NumberLength = 6,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.NumberSequences.Add(seq2);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // If another process inserted concurrently, try to read it; if still missing create again.
                    seq2 = await _context.NumberSequences
                        .FirstOrDefaultAsync(s => s.SequenceType == sequenceType);

                    if (seq2 == null)
                    {
                        seq2 = new NumberSequence
                        {
                            SequenceType = sequenceType,
                            CurrentNumber = 0,
                            Prefix = GetDefaultPrefix(sequenceType),
                            NumberLength = 6,
                            UpdatedDate = DateTime.UtcNow
                        };

                        _context.NumberSequences.Add(seq2);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // زوّد الرقم واحفظ
            seq2.CurrentNumber++;
            seq2.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await tx.CommitAsync();

            return BuildNumber(seq2, seq2.CurrentNumber);
        });
    }

    public Task<string> GenerateCustomerCodeAsync()
        => GetNextNumberAsync("Customer");

    public Task<string> GenerateSupplierCodeAsync()
        => GetNextNumberAsync("Supplier");

    public Task<string> GenerateProductCodeAsync()
        => GetNextNumberAsync("Product");

    public Task<string> GenerateSalesInvoiceNumberAsync()
        => GetNextNumberAsync("SalesInvoice");

    public Task<string> GeneratePurchaseInvoiceNumberAsync()
        => GetNextNumberAsync("PurchaseInvoice");

    public Task<string> GenerateCashTransactionNumberAsync()
        => GetNextNumberAsync("CashTransaction");

    // ================= Helpers =================

    private static string BuildNumber(NumberSequence seq, int next)
    {
        var padLen = Math.Max(1, seq.NumberLength);
        var numberPart = next.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(padLen, '0');
        var prefix = seq.Prefix ?? string.Empty;
        var suffix = seq.Suffix ?? string.Empty; // في حالتك قد تكون null
        return $"{prefix}{numberPart}{suffix}";
    }

    private static string GetDefaultPrefix(string sequenceType)
    {
        // أول لحد 3 حروف من النوع (حروف فقط) كـ Prefix افتراضي
        var letters = new string((sequenceType ?? string.Empty)
            .Where(char.IsLetter)
            .Take(3)
            .ToArray());

        if (string.IsNullOrEmpty(letters))
        {
            // fallback بسيط
            letters = (sequenceType ?? "NS").Trim();
            if (letters.Length == 0) letters = "NS";
            letters = letters.Length <= 3 ? letters : letters[..3];
        }

        return letters.ToUpperInvariant();
    }
}
