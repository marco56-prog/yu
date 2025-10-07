using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AccountingSystem.WPF.ViewModels
{
    /// <summary>
    /// نموذج عنصر قائمة التنقل
    /// </summary>
    public class MenuItemVm : INotifyPropertyChanged
    {
        private bool _isExpanded;

        /// <summary>
        /// عنوان العنصر
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// مفتاح الأيقونة في الموارد
        /// </summary>
        public string? IconKey { get; set; }

        /// <summary>
        /// اختصار لوحة المفاتيح
        /// </summary>
        public string? Shortcut { get; set; }

        /// <summary>
        /// المسار المستهدف للتنقل
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// هل هذا العنصر مجموعة أم عنصر فردي
        /// </summary>
        public bool IsGroup { get; set; }

        /// <summary>
        /// هل المجموعة مفتوحة أم مغلقة
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// العناصر الفرعية (للمجموعات)
        /// </summary>
        public ObservableCollection<MenuItemVm> Children { get; } = new();

        /// <summary>
        /// أمر النقر على العنصر
        /// </summary>
        public ICommand? Command { get; set; }

        /// <summary>
        /// أمر توسيع/طي المجموعة
        /// </summary>
        public ICommand? ToggleCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}