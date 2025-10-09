using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models
{
    // =========================
    // Core Accounting Models
    // =========================

    /// <summary>
    /// Defines the fundamental types of accounts in a double-entry accounting system.
    /// </summary>
    public enum AccountType
    {
        Asset = 1,
        Liability = 2,
        Equity = 3,
        Revenue = 4,
        Expense = 5
    }

    /// <summary>
    /// Represents a single account in the Chart of Accounts.
    /// </summary>
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        public AccountType Type { get; set; }

        [StringLength(20)]
        public string? Code { get; set; }

        public int? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public virtual Account? ParentAccount { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        public virtual ICollection<Account> ChildAccounts { get; set; } = new List<Account>();
        public virtual ICollection<JournalVoucherDetail> JournalVoucherDetails { get; set; } = new List<JournalVoucherDetail>();
    }

    /// <summary>
    /// Represents the status of a journal voucher.
    /// </summary>
    public enum VoucherStatus
    {
        Draft = 1,
        Posted = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Represents the header of a journal entry (a voucher).
    /// </summary>
    public class JournalVoucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public required string VoucherNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public VoucherStatus Status { get; set; } = VoucherStatus.Draft;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }

        public DateTime? PostedDate { get; set; }
        public string? PostedBy { get; set; }

        public virtual ICollection<JournalVoucherDetail> Details { get; set; } = new List<JournalVoucherDetail>();
    }

    /// <summary>
    /// Represents a single line (debit or credit) in a JournalVoucher.
    /// </summary>
    public class JournalVoucherDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JournalVoucherId { get; set; }
        [ForeignKey("JournalVoucherId")]
        public virtual JournalVoucher? JournalVoucher { get; set; }

        [Required]
        public int AccountId { get; set; }
        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Debit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Credit { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }
    }
}