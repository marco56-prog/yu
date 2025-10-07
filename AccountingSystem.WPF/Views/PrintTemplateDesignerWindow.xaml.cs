using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.Win32;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccountingSystem.WPF.Views
{
    public partial class PrintTemplateDesignerWindow : Window, INotifyPropertyChanged
    {
        private readonly IUnitOfWork _unitOfWork;
        private PrintTemplate _currentTemplate;
        private ObservableCollection<PrintTemplate> _templates = new();
        private ObservableCollection<TemplateElement> _templateElements = new();

        // خصائص ربط البيانات
        public ObservableCollection<PrintTemplate> Templates
        {
            get => _templates;
            set
            {
                _templates = value;
                OnPropertyChanged(nameof(Templates));
            }
        }

        public ObservableCollection<TemplateElement> TemplateElements
        {
            get => _templateElements;
            set
            {
                _templateElements = value;
                OnPropertyChanged(nameof(TemplateElements));
            }
        }

        public PrintTemplateDesignerWindow(IUnitOfWork unitOfWork)
        {
            InitializeComponent();
            
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _currentTemplate = new PrintTemplate();
            
            DataContext = this;
            
            // إعداد المجموعات
            Templates = new ObservableCollection<PrintTemplate>();
            TemplateElements = new ObservableCollection<TemplateElement>();
            
            // ربط البيانات
            lstTemplates.ItemsSource = Templates;
            // designCanvas.ItemsSource = TemplateElements; // Canvas doesn't have ItemsSource
            
            // إعداد أنواع القوالب
            SetupTemplateTypes();
            
            Loaded += async (s, e) => await LoadTemplatesAsync();
        }

        private void SetupTemplateTypes()
        {
            cmbTemplateType.Items.Clear();
            cmbTemplateType.Items.Add("فاتورة بيع");
            cmbTemplateType.Items.Add("فاتورة شراء");
            cmbTemplateType.Items.Add("إيصال نقطة البيع");
            cmbTemplateType.Items.Add("إيصال حراري");
            cmbTemplateType.Items.Add("تقرير مبيعات");
            cmbTemplateType.Items.Add("تقرير مخزون");
            cmbTemplateType.Items.Add("شيك");
            cmbTemplateType.Items.Add("مخصص");
            
            cmbTemplateType.SelectedIndex = 0;
        }

        private async Task LoadTemplatesAsync()
        {
            try
            {
                lblStatus.Text = "جارٍ تحميل القوالب...";
                
                Templates.Clear();
                
                // تحميل القوالب المحفوظة من مجلد التطبيق
                var templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrintTemplates");
                
                if (!Directory.Exists(templatesPath))
                {
                    Directory.CreateDirectory(templatesPath);
                }

                // إضافة قوالب افتراضية إذا لم تكن موجودة
                await CreateDefaultTemplatesAsync(templatesPath);
                
                // تحميل القوالب من الملفات
                var templateFiles = Directory.GetFiles(templatesPath, "*.json");
                
                foreach (var file in templateFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var template = JsonSerializer.Deserialize<PrintTemplate>(json);
                        if (template != null)
                        {
                            Templates.Add(template);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"خطأ في تحميل قالب {file}: {ex.Message}");
                    }
                }
                
                // اختيار القالب الأول افتراضياً
                if (Templates.Any())
                {
                    lstTemplates.SelectedItem = Templates.First();
                    await LoadTemplateAsync(Templates.First());
                }
                
                lblStatus.Text = $"تم تحميل {Templates.Count} قالب";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل القوالب: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "خطأ في تحميل القوالب";
            }
        }

        private async Task CreateDefaultTemplatesAsync(string templatesPath)
        {
            try
            {
                // قالب فاتورة البيع الافتراضي
                var salesInvoiceTemplate = CreateDefaultSalesInvoiceTemplate();
                await SaveTemplateAsync(salesInvoiceTemplate, templatesPath);
                
                // قالب إيصال نقطة البيع
                var posReceiptTemplate = CreateDefaultPOSReceiptTemplate();
                await SaveTemplateAsync(posReceiptTemplate, templatesPath);
                
                // قالب التقرير
                var reportTemplate = CreateDefaultReportTemplate();
                await SaveTemplateAsync(reportTemplate, templatesPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إنشاء القوالب الافتراضية: {ex.Message}");
            }
        }

        private PrintTemplate CreateDefaultSalesInvoiceTemplate()
        {
            return new PrintTemplate
            {
                Id = 1,
                Name = "فاتورة بيع افتراضية",
                Type = TemplateTypes.SalesInvoice,
                Width = 210, // A4
                Height = 297,
                BackgroundColor = "#FFFFFF",
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now,
                IsActive = true,
                IsDefault = true,
                Elements = new List<TemplateElement>
                {
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "{CompanyName}",
                        X = 50, Y = 20, Width = 100, Height = 15,
                        FontFamily = "Arial", FontSize = 18, FontWeight = "Bold",
                        TextAlign = "Center", Color = "#000000"
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "فاتورة بيع",
                        X = 50, Y = 40, Width = 100, Height = 12,
                        FontFamily = "Arial", FontSize = 16, FontWeight = "Bold",
                        TextAlign = "Center", Color = "#2E86AB"
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "رقم الفاتورة: {InvoiceNumber}",
                        X = 15, Y = 60, Width = 60, Height = 8,
                        FontFamily = "Arial", FontSize = 10, TextAlign = "Left"
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "التاريخ: {InvoiceDate}",
                        X = 130, Y = 60, Width = 60, Height = 8,
                        FontFamily = "Arial", FontSize = 10, TextAlign = "Right"
                    },
                    new TemplateElement
                    {
                        Type = "Line",
                        X = 15, Y = 75, Width = 180, Height = 1,
                        Color = "#CCCCCC"
                    },
                    new TemplateElement
                    {
                        Type = "Table",
                        Content = "{InvoiceItems}",
                        X = 15, Y = 85, Width = 180, Height = 100,
                        FontFamily = "Arial", FontSize = 9
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "الإجمالي: {TotalAmount} ج.م",
                        X = 130, Y = 200, Width = 65, Height = 10,
                        FontFamily = "Arial", FontSize = 12, FontWeight = "Bold",
                        TextAlign = "Right", Color = "#2E86AB"
                    }
                }
            };
        }

        private PrintTemplate CreateDefaultPOSReceiptTemplate()
        {
            return new PrintTemplate
            {
                Id = 2,
                Name = "إيصال نقطة البيع",
                Type = "إيصال نقطة البيع",
                Description = "قالب إيصال نقطة البيع الحراري 80mm",
                Width = 80,
                Height = 200,
                Orientation = "Portrait",
                Margins = new TemplateMargins { Top = 5, Right = 3, Bottom = 5, Left = 3 },
                BackgroundColor = "#FFFFFF",
                Elements = new List<TemplateElement>
                {
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "{CompanyName}",
                        X = 5, Y = 5, Width = 70, Height = 8,
                        FontFamily = "Arial", FontSize = 12, FontWeight = "Bold",
                        TextAlign = "Center"
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "{CompanyAddress}",
                        X = 5, Y = 15, Width = 70, Height = 6,
                        FontFamily = "Arial", FontSize = 8, TextAlign = "Center"
                    },
                    new TemplateElement
                    {
                        Type = "Line",
                        X = 5, Y = 25, Width = 70, Height = 1,
                        Color = "#000000"
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "إيصال رقم: {InvoiceNumber}",
                        X = 5, Y = 30, Width = 70, Height = 6,
                        FontFamily = "Arial", FontSize = 8, TextAlign = "Center"
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "{InvoiceDate} {InvoiceTime}",
                        X = 5, Y = 38, Width = 70, Height = 6,
                        FontFamily = "Arial", FontSize = 8, TextAlign = "Center"
                    },
                    new TemplateElement
                    {
                        Type = "Line",
                        X = 5, Y = 48, Width = 70, Height = 1,
                        Color = "#000000"
                    },
                    new TemplateElement
                    {
                        Type = "Table",
                        Content = "{InvoiceItems}",
                        X = 5, Y = 55, Width = 70, Height = 80,
                        FontFamily = "Arial", FontSize = 8
                    },
                    new TemplateElement
                    {
                        Type = "Line",
                        X = 5, Y = 140, Width = 70, Height = 1,
                        Color = "#000000"
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "الإجمالي: {TotalAmount} ج.م",
                        X = 5, Y = 145, Width = 70, Height = 8,
                        FontFamily = "Arial", FontSize = 10, FontWeight = "Bold",
                        TextAlign = "Center"
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "شكراً لزيارتكم",
                        X = 5, Y = 160, Width = 70, Height = 8,
                        FontFamily = "Arial", FontSize = 10, TextAlign = "Center"
                    }
                },
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                IsDefault = true
            };
        }

        private PrintTemplate CreateDefaultReportTemplate()
        {
            return new PrintTemplate
            {
                Id = 3,
                Name = "تقرير مبيعات افتراضي",
                Type = "تقرير مبيعات",
                Description = "قالب افتراضي لتقارير المبيعات",
                Width = 210, // A4
                Height = 297,
                Orientation = "Portrait",
                Margins = new TemplateMargins { Top = 20, Right = 15, Bottom = 20, Left = 15 },
                BackgroundColor = "#FFFFFF",
                Elements = new List<TemplateElement>
                {
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "تقرير المبيعات",
                        X = 50, Y = 20, Width = 100, Height = 15,
                        FontFamily = "Arial", FontSize = 20, FontWeight = "Bold",
                        TextAlign = "Center", Color = "#2E86AB"
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "من: {FromDate} إلى: {ToDate}",
                        X = 15, Y = 40, Width = 180, Height = 10,
                        FontFamily = "Arial", FontSize = 12, TextAlign = "Center"
                    },
                    new TemplateElement
                    {
                        Type = "Line",
                        X = 15, Y = 55, Width = 180, Height = 1,
                        Color = "#CCCCCC"
                    },
                    new TemplateElement
                    {
                        Type = "Table",
                        Content = "{ReportData}",
                        X = 15, Y = 65, Width = 180, Height = 150,
                        FontFamily = "Arial", FontSize = 9
                    },
                    new TemplateElement
                    {
                        Type = "Text",
                        Content = "إجمالي المبيعات: {TotalSales} ج.م",
                        X = 100, Y = 230, Width = 95, Height = 12,
                        FontFamily = "Arial", FontSize = 14, FontWeight = "Bold",
                        TextAlign = "Right", Color = "#28A745"
                    }
                },
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                IsDefault = true
            };
        }

        #region معالجات الأحداث

        private async void btnNewTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentTemplate = new PrintTemplate
                {
                    Id = 0,
                    Name = "قالب جديد",
                    Type = cmbTemplateType.SelectedItem?.ToString() ?? "مخصص",
                    Width = 210,
                    Height = 297,
                    Orientation = "Portrait",
                    Margins = new TemplateMargins { Top = 20, Right = 15, Bottom = 20, Left = 15 },
                    BackgroundColor = "#FFFFFF",
                    Elements = new List<TemplateElement>(),
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };
                
                await LoadTemplateAsync(_currentTemplate);
                lblStatus.Text = "تم إنشاء قالب جديد";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء قالب جديد: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentTemplate == null)
                {
                    MessageBox.Show("لا يوجد قالب للحفظ", "تنبيه", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // تحديث بيانات القالب
                _currentTemplate.Name = txtTemplateName.Text?.Trim() ?? "قالب بدون اسم";
                _currentTemplate.Description = txtTemplateDescription.Text?.Trim() ?? "";
                _currentTemplate.Type = cmbTemplateType.SelectedItem?.ToString() ?? "مخصص";
                _currentTemplate.ModifiedDate = DateTime.Now;
                
                // حفظ القالب
                var templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrintTemplates");
                await SaveTemplateAsync(_currentTemplate, templatesPath);
                
                // تحديث القائمة
                var existingTemplate = Templates.FirstOrDefault(t => t.Id == _currentTemplate.Id);
                if (existingTemplate != null)
                {
                    var index = Templates.IndexOf(existingTemplate);
                    Templates[index] = _currentTemplate;
                }
                else
                {
                    Templates.Add(_currentTemplate);
                }
                
                lblStatus.Text = "تم حفظ القالب بنجاح";
                MessageBox.Show("تم حفظ القالب بنجاح!", "نجحت العملية", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ القالب: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void lstTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTemplates.SelectedItem is PrintTemplate selectedTemplate)
            {
                await LoadTemplateAsync(selectedTemplate);
            }
        }

        private void btnAddText_Click(object sender, RoutedEventArgs e)
        {
            AddTemplateElement("Text", "نص جديد");
        }

        private void btnAddImage_Click(object sender, RoutedEventArgs e)
        {
            AddTemplateElement("Image", "صورة");
        }

        private void btnAddLine_Click(object sender, RoutedEventArgs e)
        {
            AddTemplateElement("Line", "خط");
        }

        private void btnAddTable_Click(object sender, RoutedEventArgs e)
        {
            AddTemplateElement("Table", "{TableData}");
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // إنشاء نافذة معاينة
                var previewWindow = new PrintPreviewWindow(_currentTemplate, _unitOfWork);
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في المعاينة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnExportTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentTemplate == null)
                {
                    MessageBox.Show("لا يوجد قالب للتصدير", "تنبيه", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var saveDialog = new SaveFileDialog
                {
                    Filter = "ملفات القوالب|*.json",
                    FileName = $"{_currentTemplate.Name}.json"
                };
                
                if (saveDialog.ShowDialog() == true)
                {
                    var json = JsonSerializer.Serialize(_currentTemplate, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    await File.WriteAllTextAsync(saveDialog.FileName, json);
                    
                    MessageBox.Show("تم تصدير القالب بنجاح!", "نجحت العملية", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تصدير القالب: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnImportTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "ملفات القوالب|*.json",
                    Multiselect = false
                };
                
                if (openDialog.ShowDialog() == true)
                {
                    var json = await File.ReadAllTextAsync(openDialog.FileName);
                    var template = JsonSerializer.Deserialize<PrintTemplate>(json);
                    
                    if (template != null)
                    {
                        template.Id = 0; // سيتم تعيين معرف جديد من قاعدة البيانات
                        template.CreatedDate = DateTime.Now;
                        template.ModifiedDate = DateTime.Now;
                        
                        Templates.Add(template);
                        lstTemplates.SelectedItem = template;
                        await LoadTemplateAsync(template);
                        
                        MessageBox.Show("تم استيراد القالب بنجاح!", "نجحت العملية", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في استيراد القالب: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region دوال مساعدة

        private async Task LoadTemplateAsync(PrintTemplate template)
        {
            try
            {
                _currentTemplate = template;
                
                // تحديث واجهة المستخدم
                txtTemplateName.Text = template.Name;
                txtTemplateDescription.Text = template.Description;
                cmbTemplateType.SelectedItem = template.Type;
                
                // تحديث عناصر القالب
                TemplateElements.Clear();
                
                if (template.Elements != null)
                {
                    foreach (var element in template.Elements)
                    {
                        TemplateElements.Add(element);
                    }
                }
                
                // تحديث الخصائص
                UpdateTemplateProperties(template);
                
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل القالب: {ex.Message}");
            }
        }

        private void UpdateTemplateProperties(PrintTemplate template)
        {
            try
            {
                // تحديث خصائص القالب في الواجهة
                if (template.Margins != null)
                {
                    // يمكن إضافة عناصر التحكم في الهوامش هنا
                }
                
                // تحديث لوحة التصميم
                designCanvas.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(template.BackgroundColor ?? "#FFFFFF"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحديث خصائص القالب: {ex.Message}");
            }
        }

        private void AddTemplateElement(string type, string content)
        {
            try
            {
                var element = new TemplateElement
                {
                    Id = 0,
                    Type = type,
                    Content = content,
                    X = 50, Y = 50, Width = 100, Height = 20,
                    FontFamily = "Arial", FontSize = 12,
                    Color = "#000000",
                    TextAlign = "Left"
                };
                
                TemplateElements.Add(element);
                
                // إضافة العنصر للقالب الحالي
                if (_currentTemplate?.Elements != null)
                {
                    _currentTemplate.Elements.Add(element);
                }
                
                lblStatus.Text = $"تم إضافة عنصر {type}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة العنصر: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveTemplateAsync(PrintTemplate template, string templatesPath)
        {
            try
            {
                var filePath = Path.Combine(templatesPath, $"{template.Id}.json");
                var json = JsonSerializer.Serialize(template, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"فشل في حفظ القالب: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}