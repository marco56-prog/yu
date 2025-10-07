using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// Interaction logic for BarcodeReaderWindow.xaml
    /// </summary>
    public partial class BarcodeReaderWindow : Window
    {
        // حالة الكاميرا (محاكاة)
        private bool _cameraOn = false;

        // آخر باركود مقروء
        private string? _lastBarcode = null;

        // عداد المسوح
        private int _scannedCount = 0;

        // “السلة” (محلية للتجربة)
        private readonly List<CartItem> _cart = new();

        // بيانات منتجات مبدئية (بدّلها بمصدر بياناتك / خدمة)
        private readonly Dictionary<string, ProductInfo> _products = new(StringComparer.OrdinalIgnoreCase)
        {
            ["6223000000012"] = new("شوكولاتة بالحليب 45 جم", "6223000000012", 17.50m, 125, "حلويات"),
            ["6223000000029"] = new("قهوة محمصة 250 جم", "6223000000029", 85.00m, 18, "مشروبات ساخنة"),
            ["6223000000036"] = new("جبنة رومي 1 كجم", "6223000000036", 245.00m, 6, "ألبان"),
            ["6223000000043"] = new("سائل تنظيف 750 مل", "6223000000043", 39.90m, 42, "منظفات"),
            ["8901030001234"] = new("مكرونة إسباجتي 400 جم", "8901030001234", 24.00m, 73, "مواد غذائية"),
        };

        // Token لإيقاف محاكاة حزمة الكاميرا
        private CancellationTokenSource? _cameraLoopCts;

        public BarcodeReaderWindow()
        {
            InitializeComponent();
            Loaded += BarcodeReaderWindow_Loaded;
            Unloaded += BarcodeReaderWindow_Unloaded;
        }

        // Constructor with parameters for compatibility
        public BarcodeReaderWindow(object unitOfWork, object parentWindow) : this()
        {
            // Store references if needed for future use
            // kept for compatibility
        }

        private void BarcodeReaderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // بداية الحالة الافتراضية
            SetStatus(disconnected: true);
            UpdateScannedCountText();
            ShowProductPanel(false);
        }

        private void BarcodeReaderWindow_Unloaded(object? sender, RoutedEventArgs e)
        {
            StopCameraSimulation();
        }

        #region UI Helpers

        private void SetStatus(bool disconnected)
        {
            lblStatus.Text = disconnected ? "🔴 غير متصل" : "🟢 متصل";
            lblStatus.Foreground = new SolidColorBrush(disconnected ? Colors.Orange : Colors.LimeGreen);
            btnToggleCamera.Content = disconnected ? "📷 تشغيل الكاميرا" : "⏹️ إيقاف الكاميرا";
            NoCameraMessage.Visibility = disconnected ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateScannedCountText()
        {
            lblScannedCount.Text = $"تم المسح: {_scannedCount}";
        }

        private void SetLastScanned(string? code)
        {
            _lastBarcode = code;
            lblLastScanned.Text = $"آخر مسح: {(string.IsNullOrWhiteSpace(code) ? "لا يوجد" : code)}";
        }

        private void ShowProductPanel(bool show)
        {
            ProductInfoPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FillProductPanel(ProductInfo info)
        {
            lblProductName.Text = info.Name;
            lblProductCode.Text = info.Code;
            lblProductPrice.Text = $"{info.Price:N2} ج.م";
            lblProductStock.Text = $"{info.Stock}";
            lblProductCategory.Text = info.Category ?? "غير محدد";
        }

        private void BeepIfEnabled()
        {
            if (chkSoundEnabled.IsChecked == true)
            {
                SystemSounds.Asterisk.Play();
            }
        }

        private int GetDesiredQuantity()
        {
            if (int.TryParse(txtQuantity.Text, out var q) && q > 0) return q;
            return 1;
        }

        #endregion

        #region Camera Simulation (replace with real scanner later)

        private void StartCameraSimulation()
        {
            StopCameraSimulation();
            _cameraLoopCts = new CancellationTokenSource();
            var token = _cameraLoopCts.Token;

            // محاكاة “فريمات” الكاميرا + قراءة باركود عند الضغط على مفتاح Enter داخل النافذة
            // (لو حبيت، تقدر تبدّل ده بمكتبة ZXing مع VideoCapture حقيقية)
            this.KeyDown += BarcodeReaderWindow_KeyDown;

            async Task Loop()
            {
                // مجرد مؤشر أن الكاميرا فعالة؛ لا نفعل شيئًا كل فترة قصيرة.
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(100, token);
                    }
                }
                catch (TaskCanceledException) { /* Ignored */ }
            }

            _ = Loop();
        }

        private void StopCameraSimulation()
        {
            this.KeyDown -= BarcodeReaderWindow_KeyDown;

            if (_cameraLoopCts != null)
            {
                _cameraLoopCts.Cancel();
                _cameraLoopCts.Dispose();
                _cameraLoopCts = null;
            }
        }

        // محاكاة: عند الضغط على Enter، نعتبر أن باركود اتقرى من “الكيبورد ويدجت”
        private void BarcodeReaderWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // لو فيه Scanner بيضخ نص متتابع وينهي بـ Enter – هنا تقدر تجمع النصوص في Buffer.
            // للتبسيط: بنفتح إدخال يدوي مباشرة عند F2، أو بنهمل.
            if (e.Key == System.Windows.Input.Key.F2)
            {
                ShowManualDialog(true);
                e.Handled = true;
            }
        }

        #endregion

        #region Lookup & Cart

        private bool TryFindProduct(string barcode, out ProductInfo info)
        {
            return _products.TryGetValue(barcode.Trim(), out info!);
        }

        private void AddToCart(ProductInfo info, int quantity)
        {
            var existing = _cart.FirstOrDefault(c => c.Code.Equals(info.Code, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                existing = new CartItem
                {
                    Code = info.Code,
                    Name = info.Name,
                    Price = info.Price,
                    Quantity = 0
                };
                _cart.Add(existing);
            }

            existing.Quantity += quantity;
        }

        private void RemoveLastFromCart()
        {
            if (_cart.Count == 0) return;

            // أبسط تطبيق: احذف آخر عنصر اتضاف (أو قلّل كميته)
            var last = _cart.Last();
            if (last.Quantity > 1) last.Quantity -= 1;
            else _cart.RemoveAt(_cart.Count - 1);
        }

        #endregion

        #region Scan Flow

        private void OnBarcodeScanned(string barcode)
        {
            SetLastScanned(barcode);

            if (TryFindProduct(barcode, out var info))
            {
                FillProductPanel(info);
                ShowProductPanel(true);
                BeepIfEnabled();

                _scannedCount++;
                UpdateScannedCountText();

                if (chkAutoAdd.IsChecked == true)
                {
                    var q = GetDesiredQuantity();
                    AddToCart(info, q);
                }
            }
            else
            {
                ShowProductPanel(false);
                MessageBox.Show($"لم يتم العثور على منتج للباركود: {barcode}", "غير موجود", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Events (names preserved exactly as in XAML)

        private void btnToggleCamera_Click(object sender, RoutedEventArgs e)
        {
            _cameraOn = !_cameraOn;
            if (_cameraOn)
            {
                SetStatus(disconnected: false);
                StartCameraSimulation();
            }
            else
            {
                StopCameraSimulation();
                SetStatus(disconnected: true);
            }
        }

        private void btnManualEntry_Click(object sender, RoutedEventArgs e)
        {
            ShowManualDialog(true);
        }

        private void btnManualSearch_Click(object sender, RoutedEventArgs e)
        {
            var code = txtManualBarcode.Text?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("من فضلك أدخل رقم باركود صحيح.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowManualDialog(false);
            OnBarcodeScanned(code);
        }

        private void btnCancelManual_Click(object sender, RoutedEventArgs e)
        {
            ShowManualDialog(false);
        }

        private void ManualEntryDialog_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // لمس أي مكان في الخلفية يغلق الحوار
            ShowManualDialog(false);
        }

        private void btnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(lblProductCode.Text))
            {
                MessageBox.Show("لا يوجد منتج محدد للإضافة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryFindProduct(lblProductCode.Text, out var info))
            {
                MessageBox.Show("تعذر استرجاع بيانات المنتج المحدد.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var q = GetDesiredQuantity();
            AddToCart(info, q);
            BeepIfEnabled();

            if (chkAutoAdd.IsChecked != true)
            {
                MessageBox.Show($"تمت إضافة {q} × {info.Name} إلى السلة.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnClearLast_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_lastBarcode))
            {
                MessageBox.Show("لا يوجد مسح سابق.", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RemoveLastFromCart();

            _scannedCount = Math.Max(0, _scannedCount - 1);
            UpdateScannedCountText();

            SetLastScanned(null);
            ShowProductPanel(false);
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder لإعدادات القارئ/الكاميرا
            MessageBox.Show("إعدادات القارئ – يمكنك لاحقًا إضافة اختيار الكاميرا، دقة الفيديو، صيغة ZXing، إلخ.", "إعدادات", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Manual Dialog

        private void ShowManualDialog(bool show)
        {
            ManualEntryDialog.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show)
            {
                txtManualBarcode.Text = "";
                txtManualBarcode.Focus();
            }
        }

        #endregion

        #region Models

        private sealed record ProductInfo(string Name, string Code, decimal Price, int Stock, string? Category);

        private sealed class CartItem
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public decimal Total => Price * Quantity;
        }

        #endregion
    }
}
