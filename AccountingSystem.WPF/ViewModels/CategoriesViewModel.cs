// File: CategoriesViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using AccountingSystem.Models;
using AccountingSystem.Data;
using AccountingSystem.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.ViewModels;

/// <summary>
/// ViewModel for managing product categories
/// </summary>
public sealed class CategoriesViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;

    // Properties for search and filter
    private string _searchText = string.Empty;
    private bool _showInactiveOnly = false;

    // Parent filter (اختياري حسب وجود ParentCategoryId في الكيان)
    private bool _enableParentFilter = false;
    private int? _parentFilterId = null;

    // Collections
    private ObservableCollection<Category> _categories = new();
    private readonly CollectionViewSource _categoriesViewSource;

    // Selected item
    private Category? _selectedCategory;

    // Commands
    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleActiveCommand { get; }

    public CategoriesViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Setup collection view for filtering and sorting
        _categoriesViewSource = new CollectionViewSource { Source = _categories };
        _categoriesViewSource.Filter += OnCategoriesFilter;
        _categoriesViewSource.SortDescriptions.Add(
            new SortDescription(nameof(Category.CategoryName), ListSortDirection.Ascending));

        // Initialize commands
        RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
        AddCommand = new RelayCommand(async () => await AddCategoryAsync());
        EditCommand = new RelayCommand<Category>(async (category) => await EditCategoryAsync(category));
        DeleteCommand = new RelayCommand<Category>(async (category) => await DeleteCategoryAsync(category),
            category => category != null);
        ToggleActiveCommand = new RelayCommand<Category>(async (category) => await ToggleActiveCategoryAsync(category),
            category => category != null);

        // Load initial data
        _ = Task.Run(RefreshDataAsync);
    }

    #region Properties

    public ObservableCollection<Category> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public ICollectionView CategoriesView => _categoriesViewSource.View;

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _categoriesViewSource.View.Refresh();
            }
        }
    }

    public bool ShowInactiveOnly
    {
        get => _showInactiveOnly;
        set
        {
            if (SetProperty(ref _showInactiveOnly, value))
            {
                _categoriesViewSource.View.Refresh();
            }
        }
    }

    // ====== Parent Filter (اختياري) ======
    public bool EnableParentFilter
    {
        get => _enableParentFilter;
        set
        {
            if (SetProperty(ref _enableParentFilter, value))
                _categoriesViewSource.View.Refresh();
        }
    }

    public int? ParentFilterId
    {
        get => _parentFilterId;
        set
        {
            if (SetProperty(ref _parentFilterId, value))
                _categoriesViewSource.View.Refresh();
        }
    }
    // =====================================

    public int TotalCount => _categories.Count;
    public int ActiveCount => _categories.Count(c => c.IsActive);
    public int InactiveCount => _categories.Count(c => !c.IsActive);

    #endregion

    #region Private Methods

    private async Task RefreshDataAsync()
    {
        try
        {
            var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
            categories = categories.OrderBy(c => c.CategoryName).ToList();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _categories.Clear();
                foreach (var category in categories)
                    _categories.Add(category);

                _categoriesViewSource.View.Refresh();
                UpdateCounts();
            });
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"خطأ في تحميل البيانات:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    private async Task AddCategoryAsync()
    {
        try
        {
            var newCategory = new Category
            {
                CategoryName = "",
                Description = "",
                IsActive = true
            };

            var editWindow = new CategoryEditWindow(newCategory)
            {
                Owner = Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                var name = (newCategory.CategoryName ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("اسم الفئة لا يمكن أن يكون فارغًا.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicate names (case-insensitive)
                var exists = await _unitOfWork.Repository<Category>()
                    .AnyAsync(c => (c.CategoryName ?? "").ToLowerInvariant() == name.ToLowerInvariant());

                if (exists)
                {
                    MessageBox.Show("اسم الفئة موجود بالفعل. يرجى اختيار اسم آخر.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                newCategory.CategoryName = name;

                await _unitOfWork.Repository<Category>().AddAsync(newCategory);
                await _unitOfWork.SaveChangesAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _categories.Add(newCategory);
                    SelectedCategory = newCategory;
                    _categoriesViewSource.View.Refresh();
                    UpdateCounts();
                });

                MessageBox.Show("تم إضافة الفئة بنجاح.", "نجحت العملية",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إضافة الفئة:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task EditCategoryAsync(Category? category)
    {
        if (category == null) return;

        try
        {
            // Create a copy for editing
            var editCategory = new Category
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive
            };

            var editWindow = new CategoryEditWindow(editCategory)
            {
                Owner = Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                var name = (editCategory.CategoryName ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("اسم الفئة لا يمكن أن يكون فارغًا.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicate names (excluding current category, case-insensitive)
                var exists = await _unitOfWork.Repository<Category>()
                    .AnyAsync(c => c.CategoryId != category.CategoryId &&
                                   (c.CategoryName ?? "").ToLowerInvariant() == name.ToLowerInvariant());

                if (exists)
                {
                    MessageBox.Show("اسم الفئة موجود بالفعل. يرجى اختيار اسم آخر.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update the original category
                category.CategoryName = name;
                category.Description = editCategory.Description;
                category.IsActive = editCategory.IsActive;

                _unitOfWork.Repository<Category>().Update(category);
                await _unitOfWork.SaveChangesAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedCategory = category; // keep selection
                    _categoriesViewSource.View.Refresh();
                    UpdateCounts();
                });

                MessageBox.Show("تم تحديث الفئة بنجاح.", "نجحت العملية",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحديث الفئة:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteCategoryAsync(Category? category)
    {
        if (category == null) return;

        try
        {
            // تأكيد أول
            var result = MessageBox.Show(
                $"هل تريد حذف الفئة '{category.CategoryName}'؟\n\nملاحظة: إذا كانت مرتبطة بمنتجات، سيتم تعطيلها بدلاً من الحذف.",
                "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // لو الفئة مفعّلة، اطلب تأكيد أقوى (Yes/No/Cancel)
            if (category.IsActive)
            {
                var confirmActive = MessageBox.Show(
                    $"الفئة '{category.CategoryName}' مفعّلة.\nهل تريد بالتأكيد حذفها؟\n\nملاحظة: يُفضّل التعطيل بدل الحذف للحفاظ على سلامة البيانات.",
                    "تحذير: حذف فئة مفعّلة", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                if (confirmActive == MessageBoxResult.Cancel || confirmActive == MessageBoxResult.No)
                    return;
            }

            // Check if category has associated products
            var hasProducts = await _unitOfWork.Repository<Product>()
                .AnyAsync(p => p.CategoryId == category.CategoryId);

            if (hasProducts)
            {
                // Deactivate instead of delete
                category.IsActive = false;
                _unitOfWork.Repository<Category>().Update(category);
                await _unitOfWork.SaveChangesAsync();

                MessageBox.Show("تم تعطيل الفئة بنجاح بدلاً من الحذف لأنها مرتبطة بمنتجات.", "تم التعطيل",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Safe to delete
                _unitOfWork.Repository<Category>().Remove(category);
                await _unitOfWork.SaveChangesAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _categories.Remove(category);
                    if (SelectedCategory == category)
                        SelectedCategory = null;
                });

                MessageBox.Show("تم حذف الفئة بنجاح.", "نجحت العملية",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _categoriesViewSource.View.Refresh();
                UpdateCounts();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حذف الفئة:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ToggleActiveCategoryAsync(Category? category)
    {
        if (category == null) return;

        try
        {
            category.IsActive = !category.IsActive;
            _unitOfWork.Repository<Category>().Update(category);
            await _unitOfWork.SaveChangesAsync();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _categoriesViewSource.View.Refresh();
                UpdateCounts();
            });

            var status = category.IsActive ? "تفعيل" : "تعطيل";
            MessageBox.Show($"تم {status} الفئة بنجاح.", "نجحت العملية",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تغيير حالة الفئة:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCategoriesFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not Category category)
        {
            e.Accepted = false;
            return;
        }

        // Filter by active/inactive status
        if (ShowInactiveOnly && category.IsActive)
        {
            e.Accepted = false;
            return;
        }

        // ===== فلترة حسب الأب (اختياري/Reflection) =====
        if (EnableParentFilter && ParentFilterId.HasValue)
        {
            try
            {
                PropertyInfo? p = typeof(Category).GetProperty("ParentCategoryId", BindingFlags.Public | BindingFlags.Instance);
                if (p != null)
                {
                    var parentVal = p.GetValue(category);
                    int? parentId = parentVal as int?;
                    if (!parentId.HasValue || parentId.Value != ParentFilterId.Value)
                    {
                        e.Accepted = false;
                        return;
                    }
                }
                // لو الخاصية غير موجودة، نتجاهل فلترة الأب تلقائياً
            }
            catch
            {
                // أي خطأ في الانعكاس → تجاهل فلترة الأب بدون كسر الواجهة
            }
        }
        // ================================================

        // Filter by search text (آمن ضد Null وبلا حساسية ثقافية)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            var name = (category.CategoryName ?? string.Empty).ToLowerInvariant();
            var desc = (category.Description ?? string.Empty).ToLowerInvariant();

            var matches = name.Contains(searchLower) || desc.Contains(searchLower);
            if (!matches)
            {
                e.Accepted = false;
                return;
            }
        }

        e.Accepted = true;
    }

    private void UpdateCounts()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(InactiveCount));
    }

    #endregion
}
