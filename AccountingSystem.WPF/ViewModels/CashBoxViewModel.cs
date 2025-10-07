// File: CashBoxViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
using AccountingSystem.Models;
using AccountingSystem.Data;
using AccountingSystem.WPF.Views;
using AccountingSystem.Business;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.WPF.ViewModels;

/// <summary>
/// ViewModel for managing cash box transactions and balances
/// </summary>
public sealed class CashBoxViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;

    // Properties for filters
    private CashBox? _selectedCashBox;
    private DateTime _fromDate = DateTime.Today.AddDays(-30);
    private DateTime _toDate = DateTime.Today;
    private TransactionType? _selectedTransactionType;
    private string _searchText = string.Empty;
    private string _status = "جاهز";
    private string _countDisplay = "";

    // Collections
    private ObservableCollection<CashBox> _cashBoxes = new();
    private readonly ObservableCollection<CashTransactionViewModel> _transactions = new();
    private readonly CollectionViewSource _transactionsViewSource;

    // Summary properties
    private decimal _totalIncome;
    private decimal _totalExpense;
    private decimal _netAmount;
    private decimal _selectedCashBoxBalance;

    // Concurrency guards
    private int _isRefreshingData = 0;
    private int _isRefreshingTransactions = 0;

    // Commands
    public ICommand RefreshCommand { get; }
    public ICommand AddIncomeCommand { get; }
    public ICommand AddExpenseCommand { get; }
    public ICommand TransferCommand { get; }
    public ICommand EditTransactionCommand { get; }
    public ICommand DeleteTransactionCommand { get; }
    public ICommand FilterCommand { get; }

    public CashBoxViewModel(IUnitOfWork unitOfWork, ICustomerService customerService, ISupplierService supplierService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));

        // Setup collection view for filtering and sorting
        _transactionsViewSource = new CollectionViewSource { Source = _transactions };
        _transactionsViewSource.Filter += OnTransactionsFilter;
        _transactionsViewSource.SortDescriptions.Add(
            new SortDescription(nameof(CashTransactionViewModel.TransactionDate), ListSortDirection.Descending));

        // Initialize commands
        RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
        AddIncomeCommand = new RelayCommand(async () => await AddTransactionAsync(TransactionType.Income));
        AddExpenseCommand = new RelayCommand(async () => await AddTransactionAsync(TransactionType.Expense));
        TransferCommand = new RelayCommand(async () => await ShowTransferDialogAsync());
        EditTransactionCommand = new RelayCommand<CashTransactionViewModel>(async (transaction) =>
            await EditTransactionAsync(transaction));
        DeleteTransactionCommand = new RelayCommand<CashTransactionViewModel>(async (transaction) =>
            await DeleteTransactionAsync(transaction), transaction => transaction != null);
        FilterCommand = new RelayCommand(async () => await RefreshTransactionsAsync());

        // Load initial data
        _ = Task.Run(RefreshDataAsync);
    }

    #region Properties

    public ObservableCollection<CashBox> CashBoxes
    {
        get => _cashBoxes;
        set => SetProperty(ref _cashBoxes, value);
    }

    public ICollectionView TransactionsView => _transactionsViewSource.View;

    public CashBox? SelectedCashBox
    {
        get => _selectedCashBox;
        set
        {
            if (SetProperty(ref _selectedCashBox, value))
            {
                _ = Task.Run(async () =>
                {
                    await RefreshTransactionsAsync();
                    await UpdateCashBoxBalanceAsync();
                });
            }
        }
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
            {
                _ = Task.Run(RefreshTransactionsAsync);
            }
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value))
            {
                _ = Task.Run(RefreshTransactionsAsync);
            }
        }
    }

    public TransactionType? SelectedTransactionType
    {
        get => _selectedTransactionType;
        set
        {
            if (SetProperty(ref _selectedTransactionType, value))
            {
                _transactionsViewSource.View.Refresh();
                UpdateSummary();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _transactionsViewSource.View.Refresh();
            }
        }
    }

    public decimal TotalIncome
    {
        get => _totalIncome;
        set => SetProperty(ref _totalIncome, value);
    }

    public decimal TotalExpense
    {
        get => _totalExpense;
        set => SetProperty(ref _totalExpense, value);
    }

    public decimal NetAmount
    {
        get => _netAmount;
        set => SetProperty(ref _netAmount, value);
    }

    public decimal SelectedCashBoxBalance
    {
        get => _selectedCashBoxBalance;
        set => SetProperty(ref _selectedCashBoxBalance, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string CountDisplay
    {
        get => _countDisplay;
        set => SetProperty(ref _countDisplay, value);
    }

    public string CurrentBalanceDisplay => $"{SelectedCashBoxBalance:N2} ج.م";

    public string LastTransactionDisplay
    {
        get
        {
            var last = _transactionsViewSource.View.Cast<CashTransactionViewModel>()
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();
            if (last == null) return "لا توجد حركات";
            var date = last.TransactionDate.Date;
            if (date == DateTime.Today) return "اليوم";
            if (date == DateTime.Today.AddDays(-1)) return "أمس";
            return last.TransactionDate.ToString("yyyy/MM/dd HH:mm");
        }
    }

    public ObservableCollection<CashTransactionViewModel> Transactions => _transactions;

    // Transaction types for ComboBox
    public List<TransactionTypeItem> TransactionTypeOptions { get; } = new()
    {
        new(null, "جميع الأنواع"),
        new(TransactionType.Income, "دخل"),
        new(TransactionType.Expense, "مصروف"),
        new(TransactionType.Transfer, "تحويل")
    };

    #endregion

    #region Private Methods

    private async Task RefreshDataAsync()
    {
        if (Interlocked.Exchange(ref _isRefreshingData, 1) == 1) return;

        try
        {
            await RefreshCashBoxesAsync();
            await RefreshTransactionsAsync();
            await UpdateCashBoxBalanceAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _isRefreshingData, 0);
        }
    }

    private async Task RefreshCashBoxesAsync()
    {
        try
        {
            var allBoxes = await _unitOfWork.Repository<CashBox>().FindAsync(c => c.IsActive);
            var cashBoxes = allBoxes.OrderBy(c => c.CashBoxName).ToList();

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                CashBoxes.Clear();
                foreach (var cashBox in cashBoxes)
                    CashBoxes.Add(cashBox);

                if (SelectedCashBox == null && CashBoxes.Count > 0)
                    SelectedCashBox = CashBoxes[0];
            });
        }
        catch (Exception ex)
        {
            // Log error and show message to user
            System.Diagnostics.Debug.WriteLine($"Error refreshing cash boxes: {ex.Message}");
        }
    }

    private async Task RefreshTransactionsAsync()
    {
        if (SelectedCashBox == null) return;
        if (Interlocked.Exchange(ref _isRefreshingTransactions, 1) == 1) return;

        try
        {
            var toInclusive = ToDate.Date.AddDays(1).AddTicks(-1); // يشمل نهاية اليوم
            var list = await _unitOfWork.Repository<CashTransaction>()
                .FindAsync(t => t.CashBoxId == SelectedCashBox.CashBoxId &&
                                 t.TransactionDate >= FromDate.Date &&
                                 t.TransactionDate <= toInclusive);

            var transactions = list.OrderByDescending(t => t.TransactionDate).ToList();

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                _transactions.Clear();
                foreach (var transaction in transactions)
                {
                    _transactions.Add(new CashTransactionViewModel(transaction));
                }

                _transactionsViewSource.View.Refresh();
                UpdateSummary();
                OnPropertyChanged(nameof(LastTransactionDisplay));
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing transactions: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _isRefreshingTransactions, 0);
        }
    }

    private async Task UpdateCashBoxBalanceAsync()
    {
        if (SelectedCashBox == null) return;

        try
        {
            var currentBox = await _unitOfWork.Repository<CashBox>()
                .GetByIdAsync(SelectedCashBox.CashBoxId);

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                SelectedCashBoxBalance = currentBox?.CurrentBalance ?? 0;
                OnPropertyChanged(nameof(CurrentBalanceDisplay));
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating cash box balance: {ex.Message}");
        }
    }

    private void OnTransactionsFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not CashTransactionViewModel transaction)
        {
            e.Accepted = false;
            return;
        }

        // Filter by transaction type
        if (SelectedTransactionType.HasValue &&
            transaction.TransactionType != SelectedTransactionType.Value)
        {
            e.Accepted = false;
            return;
        }

        // Filter by search text (آمن ضد null)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            var number = (transaction.TransactionNumber ?? string.Empty).ToLowerInvariant();
            var desc = (transaction.Description ?? string.Empty).ToLowerInvariant();

            var matches = number.Contains(searchLower) || desc.Contains(searchLower);

            if (!matches)
            {
                e.Accepted = false;
                return;
            }
        }

        e.Accepted = true;
    }

    private void UpdateSummary()
    {
        var visibleTransactions = _transactionsViewSource.View.Cast<CashTransactionViewModel>().ToList();

        TotalIncome = visibleTransactions
            .Where(t => t.TransactionType == TransactionType.Income)
            .Sum(t => t.Amount);

        TotalExpense = visibleTransactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .Sum(t => t.Amount);

        NetAmount = TotalIncome - TotalExpense;
        CountDisplay = $"إجمالي الحركات: {visibleTransactions.Count}";

        // Update other display properties
        OnPropertyChanged(nameof(CurrentBalanceDisplay));
    }

    private async Task AddTransactionAsync(TransactionType type)
    {
        try
        {
            if (SelectedCashBox == null)
            {
                MessageBox.Show("يرجى اختيار خزنة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new CashTransactionDialog(type, SelectedCashBox)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                // Generate transaction number first
                var lastTransaction = await _unitOfWork.Repository<CashTransaction>()
                    .FindAsync(t => t.CashBoxId == SelectedCashBox.CashBoxId);
                var nextNum = lastTransaction.Any() ? lastTransaction.Max(t => t.CashTransactionId) + 1 : 1;

                var transaction = new CashTransaction
                {
                    CashBoxId = SelectedCashBox.CashBoxId,
                    CashBox = SelectedCashBox,
                    TransactionDate = DateTime.Now,
                    TransactionType = type,
                    Amount = dialog.Amount,
                    Description = dialog.Description,
                    TransactionNumber = $"CASH-{SelectedCashBox.CashBoxId:D2}-{nextNum:D6}",
                    CreatedBy = 1 // Current user ID
                };

                _unitOfWork.BeginTransaction();
                try
                {
                    await _unitOfWork.Repository<CashTransaction>().AddAsync(transaction);

                    // Update cash box balance
                    var cashBox = await _unitOfWork.Repository<CashBox>().GetByIdAsync(SelectedCashBox.CashBoxId);
                    if (cashBox != null)
                    {
                        if (type == TransactionType.Income)
                            cashBox.CurrentBalance += dialog.Amount;
                        else
                            cashBox.CurrentBalance -= dialog.Amount;

                        _unitOfWork.Repository<CashBox>().Update(cashBox);
                    }

                    // Link to customer/supplier if specified
                    if (!string.IsNullOrEmpty(dialog.CustomerSupplierName))
                    {
                        await LinkToCustomerOrSupplierAsync(transaction, dialog.CustomerSupplierName, dialog.Amount, type);
                    }

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    await RefreshDataAsync();
                    Status = $"تم إضافة {(type == TransactionType.Income ? "دخل" : "مصروف")} بمبلغ {dialog.Amount:N2} ج.م";
                }
                catch
                {
                    _unitOfWork.RollbackTransaction();
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إضافة الحركة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LinkToCustomerOrSupplierAsync(CashTransaction transaction, string name, decimal amount, TransactionType type)
    {
        try
        {
            // Try to find customer first
            var customers = await _customerService.GetAllCustomersAsync();
            var customer = customers.FirstOrDefault(c => c.CustomerName.Contains(name, StringComparison.OrdinalIgnoreCase));

            if (customer != null)
            {
                // Update customer balance (income = payment from customer reduces their debt)
                var balanceChange = type == TransactionType.Income ? -amount : amount;
                await _customerService.UpdateCustomerBalanceAsync(
                    customer.CustomerId,
                    balanceChange,
                    $"حركة نقدية - {transaction.TransactionNumber}",
                    "CashTransaction",
                    transaction.CashTransactionId);
                return;
            }

            // Try suppliers
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            var supplier = suppliers.FirstOrDefault(s => s.SupplierName.Contains(name, StringComparison.OrdinalIgnoreCase));

            if (supplier != null)
            {
                // Update supplier balance (expense = payment to supplier reduces our debt to them)
                var existingSupplier = await _unitOfWork.Repository<Supplier>().GetByIdAsync(supplier.SupplierId);
                if (existingSupplier != null)
                {
                    var balanceChange = type == TransactionType.Expense ? -amount : amount;
                    existingSupplier.Balance += balanceChange;
                    _unitOfWork.Repository<Supplier>().Update(existingSupplier);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error linking transaction to customer/supplier: {ex.Message}");
        }
    }

    private async Task ShowTransferDialogAsync()
    {
        try
        {
            if (SelectedCashBox == null)
            {
                MessageBox.Show("يرجى اختيار خزنة المصدر أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new CashTransferDialog(CashBoxes.ToList(), SelectedCashBox)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && dialog.TargetCashBox != null)
            {
                await PerformTransferAsync(SelectedCashBox, dialog.TargetCashBox, dialog.Amount, dialog.Description);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في التحويل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task PerformTransferAsync(CashBox fromBox, CashBox toBox, decimal amount, string description)
    {
        if (fromBox.CurrentBalance < amount)
        {
            MessageBox.Show("الرصيد غير كافي للتحويل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _unitOfWork.BeginTransaction();
        try
        {
            var transferNum = $"TR-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";

            // Create transfer-out for source box
            var expenseTransaction = new CashTransaction
            {
                CashBoxId = fromBox.CashBoxId,
                CashBox = fromBox,
                TransactionDate = DateTime.Now,
                TransactionType = TransactionType.Transfer,
                Amount = amount,
                Description = $"تحويل إلى {toBox.CashBoxName} - {description}",
                TransactionNumber = $"{transferNum}-OUT",
                CreatedBy = 1
            };

            // Create transfer-in for target box
            var incomeTransaction = new CashTransaction
            {
                CashBoxId = toBox.CashBoxId,
                CashBox = toBox,
                TransactionDate = DateTime.Now,
                TransactionType = TransactionType.Transfer,
                Amount = amount,
                Description = $"تحويل من {fromBox.CashBoxName} - {description}",
                TransactionNumber = $"{transferNum}-IN",
                CreatedBy = 1
            };

            await _unitOfWork.Repository<CashTransaction>().AddAsync(expenseTransaction);
            await _unitOfWork.Repository<CashTransaction>().AddAsync(incomeTransaction);

            // Update balances
            fromBox.CurrentBalance -= amount;
            toBox.CurrentBalance += amount;

            _unitOfWork.Repository<CashBox>().Update(fromBox);
            _unitOfWork.Repository<CashBox>().Update(toBox);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            await RefreshDataAsync();
            Status = $"تم تحويل {amount:N2} ج.م من {fromBox.CashBoxName} إلى {toBox.CashBoxName}";
        }
        catch
        {
            _unitOfWork.RollbackTransaction();
            throw;
        }
    }

    private async Task EditTransactionAsync(CashTransactionViewModel? transaction)
    {
        if (transaction == null) return;

        MessageBox.Show($"تعديل الحركة رقم: {transaction.TransactionNumber}\n\nهذه الوظيفة قيد التطوير...",
            "تعديل حركة", MessageBoxButton.OK, MessageBoxImage.Information);
        await Task.CompletedTask;
    }

    private async Task DeleteTransactionAsync(CashTransactionViewModel? transaction)
    {
        if (transaction == null) return;

        var result = MessageBox.Show(
            $"هل تريد حذف الحركة رقم: {transaction.TransactionNumber}؟\nالمبلغ: {transaction.Amount:N2} ج.م\nالوصف: {transaction.Description}",
            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var cashTransaction = await _unitOfWork.Repository<CashTransaction>()
                    .GetByIdAsync(transaction.CashTransactionId);
                if (cashTransaction != null)
                {
                    // Reverse the balance change (تعامل خاص مع التحويل)
                    var cashBox = await _unitOfWork.Repository<CashBox>().GetByIdAsync(cashTransaction.CashBoxId);
                    if (cashBox != null)
                    {
                        if (cashTransaction.TransactionType == TransactionType.Income)
                        {
                            cashBox.CurrentBalance -= cashTransaction.Amount;
                        }
                        else if (cashTransaction.TransactionType == TransactionType.Expense)
                        {
                            cashBox.CurrentBalance += cashTransaction.Amount;
                        }
                        else if (cashTransaction.TransactionType == TransactionType.Transfer)
                        {
                            // استخدم لاحقة رقم الحركة لتحديد الاتجاه
                            var number = cashTransaction.TransactionNumber ?? string.Empty;
                            if (number.EndsWith("-OUT", StringComparison.OrdinalIgnoreCase))
                            {
                                // كان إنقاص من المصدر → نرجّع الرصيد
                                cashBox.CurrentBalance += cashTransaction.Amount;
                            }
                            else if (number.EndsWith("-IN", StringComparison.OrdinalIgnoreCase))
                            {
                                // كان زيادة في الهدف → ننقص الرصيد
                                cashBox.CurrentBalance -= cashTransaction.Amount;
                            }
                            else
                            {
                                // في حال عدم وجود لاحقة واضحة، لا نغيّر الرصيد احترازيًا
                                System.Diagnostics.Debug.WriteLine("Transfer delete without IN/OUT suffix; skipped balance adjust.");
                            }
                        }

                        _unitOfWork.Repository<CashBox>().Update(cashBox);
                    }

                    _unitOfWork.Repository<CashTransaction>().Remove(cashTransaction);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    await RefreshDataAsync();
                    Status = $"تم حذف الحركة رقم: {transaction.TransactionNumber}";
                }
            }
            catch (Exception ex)
            {
                _unitOfWork.RollbackTransaction();
                MessageBox.Show($"خطأ في حذف الحركة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion
}

/// <summary>
/// ViewModel wrapper for CashTransaction entity
/// </summary>
public sealed class CashTransactionViewModel : BaseViewModel
{
    private readonly CashTransaction _transaction;

    public CashTransactionViewModel(CashTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public int CashTransactionId => _transaction.CashTransactionId;
    public string TransactionNumber => _transaction.TransactionNumber;
    public DateTime TransactionDate => _transaction.TransactionDate;
    public TransactionType TransactionType => _transaction.TransactionType;
    public decimal Amount => _transaction.Amount;
    public string Description => _transaction.Description;
    public string CashBoxName => _transaction.CashBox?.CashBoxName ?? "غير محدد";

    // Display properties with Arabic text
    public string TransactionTypeDisplay => TransactionType switch
    {
        TransactionType.Income => "دخل",
        TransactionType.Expense => "مصروف",
        TransactionType.Transfer => "تحويل",
        _ => "غير محدد"
    };

    public string AmountDisplay => $"{Amount:N2} ج.م";
    public string DateDisplay => TransactionDate.ToString("yyyy/MM/dd HH:mm");
}

/// <summary>
/// Helper class for transaction type ComboBox
/// </summary>
public sealed class TransactionTypeItem
{
    public TransactionTypeItem(TransactionType? value, string display)
    {
        Value = value;
        Display = display;
    }

    public TransactionType? Value { get; }
    public string Display { get; }
}
