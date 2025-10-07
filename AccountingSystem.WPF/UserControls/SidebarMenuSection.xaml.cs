using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using AccountingSystem.WPF.ViewModels;

namespace AccountingSystem.WPF.UserControls
{
    public partial class SidebarMenuSection : UserControl, INotifyPropertyChanged
    {
        private string _icon = string.Empty;
        private string _title = string.Empty;
        private bool _isExpanded = true;
        private ObservableCollection<SidebarMenuItemViewModel> _menuItems = new();

        public SidebarMenuSection()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SidebarMenuItemViewModel> MenuItems
        {
            get => _menuItems;
            set
            {
                _menuItems = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}