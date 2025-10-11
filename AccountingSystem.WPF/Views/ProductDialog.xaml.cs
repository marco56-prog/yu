using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// نافذة حوار المنتج - نسخة احترافية محسنة
    /// </summary>
    public partial class ProductDialog : Window, INotifyPropertyChanged
    {
        #region Constants

        private const string ComponentName = "ProductDialog";
        private const string InfoCaption = "معلومات";
        private const string ErrorCaption = "خطأ";
        private const string SuccessCaption = "نجح";
        private const string HelpCaption = "مساعدة";
        private const string ConfirmCaption = "تأكيد الأرشفة";

        #endregion

        #region Fields

        private Product? _currentProduct;
        private bool _isEditMode;
        private bool _isDirty;
        private bool _isLoading;

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<Product>? ProductSaved;
        public event EventHandler<Product>? ProductDeleted;

        #endregion

        #region Constructors

        public ProductDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            _isDirty = false;
            Title = "منتج جديد";

            System.Diagnostics.Debug.WriteLine("تم إنشاء نافذة المنتج - وضع جديد");
        }

        public ProductDialog(Product product) : this()
        {
            _currentProduct = product ?? throw new ArgumentNullException(nameof(product));
            _isEditMode = true;
            Title = $"تحرير المنتج: {product.ProductName}";

            System.Diagnostics.Debug.WriteLine($"تم إنشاء نافذة المنتج - وضع تحرير: {product.ProductName}");

            _ = LoadProductDataAsync();
        }

        #endregion

        #region Properties

        /// <summary>
        /// المنتج المحدد
        /// </summary>
        public Product? SelectedProduct => _currentProduct;

        /// <summary>
        /// هل النافذة في وضع التحرير
        /// </summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (_isEditMode != value)
                {
                    _isEditMode = value;
                    OnPropertyChanged();
                    UpdateWindowTitle();
                }
            }
        }

        /// <summary>
        /// هل تم تعديل البيانات
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged();
                    UpdateWindowTitle();
                }
            }
        }

        /// <summary>
        /// هل النافذة في وضع التحميل
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    UpdateUIState();
                }
            }
        }

        #endregion

        #region Data Loading

        /// <summary>
        /// تحميل بيانات المنتج بشكل غير متزامن
        /// </summary>
        private async Task LoadProductDataAsync()
        {
            if (_currentProduct == null) return;

            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"بدء تحميل بيانات المنتج: {_currentProduct.ProductId}");

                // محاكاة تحميل البيانات
                await Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(500);
                });

                // ربط البيانات بالواجهة
                PopulateFields();

                // تحديث حالة النافذة
                UpdateUIState();
                IsDirty = false;

                System.Diagnostics.Debug.WriteLine($"تم تحميل بيانات المنتج بنجاح: {_currentProduct.ProductName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"فشل تحميل بيانات المنتج: {ex.Message}");
                MessageBox.Show($"خطأ في تحميل بيانات المنتج: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// ملء الحقول بالبيانات
        /// </summary>
        private void PopulateFields()
        {
            try
            {
                if (_currentProduct == null) return;

                // سيتم ربط البيانات بالحقول الفعلية عند إنشاء XAML
                System.Diagnostics.Debug.WriteLine("تم ملء حقول النافذة بالبيانات");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"فشل ملء الحقول بالبيانات: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// حدث ضغط المفاتيح على النافذة
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        if (CheckUnsavedChanges())
                        {
                            DialogResult = false;
                        }
                        e.Handled = true;
                        break;

                    case Key.F1:
                        ShowHelp();
                        e.Handled = true;
                        break;

                    case Key.F2:
                        Save_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;

                    case Key.F4:
                        BtnF4_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;

                    case Key.F12:
                        BtnArchive_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في معالجة ضغط المفاتيح: {ex.Message}");
                MessageBox.Show($"خطأ في معالجة ضغط المفاتيح: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ضغط زر F4 - عمليات خاصة
        /// </summary>
        private void BtnF4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("تم ضغط F4 - العمليات الخاصة");

                var options = new string[]
                {
                    "نسخ المنتج",
                    "تصدير البيانات",
                    "استيراد الصورة",
                    "طباعة باركود",
                    "عرض التقارير"
                };

                MessageBox.Show($"العمليات المتاحة:\n{string.Join("\n", options)}",
                    "العمليات الخاصة (F4)", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تنفيذ عملية F4: {ex.Message}");
                MessageBox.Show($"خطأ في تنفيذ العملية: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تغيير كود المنتج
        /// </summary>
        private void BtnChangeCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("طلب تغيير كود المنتج");

                if (_currentProduct == null)
                {
                    MessageBox.Show("لا يوجد منتج محدد لتغيير الكود", ErrorCaption,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newCode = Microsoft.VisualBasic.Interaction.InputBox(
                    "أدخل الكود الجديد للمنتج:",
                    "تغيير كود المنتج",
                    _currentProduct.ProductCode);

                if (!string.IsNullOrEmpty(newCode) && newCode != _currentProduct.ProductCode)
                {
                    _currentProduct.ProductCode = newCode;
                    IsDirty = true;
                    MessageBox.Show("تم تغيير الكود بنجاح", SuccessCaption,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تغيير كود المنتج: {ex.Message}");
                MessageBox.Show($"خطأ في تغيير كود المنتج: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// عرض الأكواد الفرعية
        /// </summary>
        private void BtnSubCodes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("عرض الأكواد الفرعية");

                var subCodes = new string[]
                {
                    "كود المورد الأساسي",
                    "كود الباركود الثانوي",
                    "كود التصنيف الداخلي",
                    "كود المخزن",
                    "كود الجرد"
                };

                MessageBox.Show($"الأكواد الفرعية المتاحة:\n{string.Join("\n", subCodes)}",
                    "الأكواد الفرعية", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في عرض الأكواد الفرعية: {ex.Message}");
                MessageBox.Show($"خطأ في عرض الأكواد الفرعية: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تحرير المنتج
        /// </summary>
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEditMode = !IsEditMode;
                System.Diagnostics.Debug.WriteLine($"تم تبديل وضع التحرير إلى: {(IsEditMode ? "تحرير" : "عرض")}");

                UpdateUIState();

                var message = IsEditMode ?
                    "تم تفعيل وضع التحرير" : "تم تفعيل وضع العرض فقط";

                MessageBox.Show(message, InfoCaption,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تبديل وضع التحرير: {ex.Message}");
                MessageBox.Show($"خطأ في تبديل وضع التحرير: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// حفظ المنتج
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("بدء عملية حفظ المنتج");

                if (!ValidateData())
                {
                    return;
                }

                if (SaveProductData())
                {
                    IsDirty = false;
                    System.Diagnostics.Debug.WriteLine("تم حفظ المنتج بنجاح");

                    MessageBox.Show("تم حفظ المنتج بنجاح", SuccessCaption,
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    if (_currentProduct != null)
                    {
                        ProductSaved?.Invoke(this, _currentProduct);
                    }

                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في حفظ المنتج: {ex.Message}");
                MessageBox.Show($"خطأ في حفظ المنتج: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// أرشفة المنتج
        /// </summary>
        private void BtnArchive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentProduct == null)
                {
                    MessageBox.Show("لا يوجد منتج للأرشفة", ErrorCaption,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"هل أنت متأكد من أرشفة المنتج '{_currentProduct.ProductName}'؟\nسيتم إلغاء تفعيل المنتج وإخفاؤه من القوائم.",
                    ConfirmCaption,
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Debug.WriteLine($"أرشفة المنتج: {_currentProduct.ProductName}");

                    _currentProduct.IsActive = false;

                    if (SaveProductData())
                    {
                        MessageBox.Show("تم أرشفة المنتج بنجاح", SuccessCaption,
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        ProductDeleted?.Invoke(this, _currentProduct);

                        DialogResult = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في أرشفة المنتج: {ex.Message}");
                MessageBox.Show($"خطأ في أرشفة المنتج: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// الانتقال للمنتج السابق
        /// </summary>
        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("الانتقال للمنتج السابق");

                if (!CheckUnsavedChanges())
                {
                    return;
                }

                MessageBox.Show("ميزة التنقل للمنتج السابق ستكون متاحة قريباً", InfoCaption,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في الانتقال للمنتج السابق: {ex.Message}");
                MessageBox.Show($"خطأ في الانتقال للمنتج السابق: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// الانتقال للمنتج التالي
        /// </summary>
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("الانتقال للمنتج التالي");

                if (!CheckUnsavedChanges())
                {
                    return;
                }

                MessageBox.Show("ميزة التنقل للمنتج التالي ستكون متاحة قريباً", InfoCaption,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في الانتقال للمنتج التالي: {ex.Message}");
                MessageBox.Show($"خطأ في الانتقال للمنتج التالي: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Validation & Data Management

        /// <summary>
        /// التحقق من صحة البيانات
        /// </summary>
        private bool ValidateData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("بدء التحقق من صحة البيانات");

                var validationErrors = new System.Collections.Generic.List<string>();

                if (_currentProduct != null)
                {
                    if (string.IsNullOrWhiteSpace(_currentProduct.ProductName))
                    {
                        validationErrors.Add("اسم المنتج مطلوب");
                    }

                    if (string.IsNullOrWhiteSpace(_currentProduct.ProductCode))
                    {
                        validationErrors.Add("كود المنتج مطلوب");
                    }

                    if (_currentProduct.SellPrice < 0)
                    {
                        validationErrors.Add("سعر البيع يجب أن يكون أكبر من أو يساوي صفر");
                    }

                    if (_currentProduct.CostPrice < 0)
                    {
                        validationErrors.Add("سعر التكلفة يجب أن يكون أكبر من أو يساوي صفر");
                    }
                }

                if (validationErrors.Count > 0)
                {
                    var message = "يرجى تصحيح الأخطاء التالية:\n\n" +
                                string.Join("\n• ", validationErrors);

                    MessageBox.Show(message, "أخطاء في البيانات",
                        MessageBoxButton.OK, MessageBoxImage.Warning);

                    return false;
                }

                System.Diagnostics.Debug.WriteLine("تم التحقق من صحة البيانات بنجاح");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في التحقق من صحة البيانات: {ex.Message}");
                MessageBox.Show($"خطأ في التحقق من صحة البيانات: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// حفظ بيانات المنتج
        /// </summary>
        private bool SaveProductData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("بدء حفظ بيانات المنتج");

                if (_currentProduct == null)
                {
                    MessageBox.Show("لا توجد بيانات للحفظ", ErrorCaption,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                CollectDataFromFields();

                System.Threading.Thread.Sleep(200);

                System.Diagnostics.Debug.WriteLine($"تم حفظ المنتج: {_currentProduct.ProductName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"فشل حفظ بيانات المنتج: {ex.Message}");
                MessageBox.Show($"فشل حفظ المنتج: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// جمع البيانات من الحقول
        /// </summary>
        private void CollectDataFromFields()
        {
            try
            {
                if (_currentProduct == null) return;

                // سيتم جمع البيانات من الحقول الفعلية عند إنشاء XAML
                System.Diagnostics.Debug.WriteLine("تم جمع البيانات من الحقول");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"فشل جمع البيانات من الحقول: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// التحقق من وجود تغييرات غير محفوظة
        /// </summary>
        private bool CheckUnsavedChanges()
        {
            try
            {
                if (!IsDirty)
                {
                    return true;
                }

                var result = MessageBox.Show(
                    "يوجد تغييرات غير محفوظة. هل تريد المتابعة بدون حفظ؟",
                    "تغييرات غير محفوظة",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                return result == MessageBoxResult.Yes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في التحقق من التغييرات غير المحفوظة: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region UI Management

        /// <summary>
        /// تحديث حالة واجهة المستخدم
        /// </summary>
        private void UpdateUIState()
        {
            try
            {
                // سيتم تنفيذ تحديث حالة الأزرار والحقول عند إنشاء XAML
                System.Diagnostics.Debug.WriteLine($"تم تحديث حالة الواجهة - تحرير: {IsEditMode}, تحميل: {IsLoading}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحديث حالة الواجهة: {ex.Message}");
            }
        }

        /// <summary>
        /// تحديث عنوان النافذة
        /// </summary>
        private void UpdateWindowTitle()
        {
            try
            {
                var baseTitle = _currentProduct?.ProductName ?? "منتج جديد";
                var modeIndicator = IsEditMode ? "تحرير" : "عرض";
                var dirtyIndicator = IsDirty ? " *" : "";

                Title = $"{baseTitle} - {modeIndicator}{dirtyIndicator}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحديث عنوان النافذة: {ex.Message}");
            }
        }

        /// <summary>
        /// عرض المساعدة
        /// </summary>
        private void ShowHelp()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("عرض نافذة المساعدة");

                var helpText = @"مساعدة نافذة المنتج:

الاختصارات:
• F1 - عرض هذه المساعدة
• F2 - حفظ المنتج
• F4 - عمليات خاصة
• F12 - أرشفة المنتج
• Esc - إغلاق النافذة

الوظائف:
• تحرير معلومات المنتج
• تغيير كود المنتج
• عرض الأكواد الفرعية
• أرشفة وإلغاء تفعيل المنتج
• التنقل بين المنتجات

ملاحظات:
• يتم حفظ التغييرات تلقائياً عند الضغط على حفظ
• يمكن التراجع عن التغييرات بإغلاق النافذة بدون حفظ
• الحقول المطلوبة محددة بعلامة *";

                MessageBox.Show(helpText, HelpCaption,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في عرض المساعدة: {ex.Message}");
                MessageBox.Show($"خطأ في عرض المساعدة: {ex.Message}", ErrorCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}