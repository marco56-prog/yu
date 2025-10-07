using System.ComponentModel;

namespace AccountingSystem.WPF.ViewModels
{
    public class SidebarMenuItemViewModel : INotifyPropertyChanged
    {
        private string _icon = string.Empty;
        private string _title = string.Empty;
        private System.Windows.Input.ICommand? _command;

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

        public System.Windows.Input.ICommand? Command
        {
            get => _command;
            set
            {
                _command = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DashboardCardViewModel : INotifyPropertyChanged
    {
        private string _icon = string.Empty;
        private string _title = string.Empty;
        private string _value = string.Empty;
        private string _subtitle = string.Empty;
        private string _tooltip = string.Empty;
        private string _valueColor = "Black";

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

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        public string Subtitle
        {
            get => _subtitle;
            set
            {
                _subtitle = value;
                OnPropertyChanged();
            }
        }

        public string Tooltip
        {
            get => _tooltip;
            set
            {
                _tooltip = value;
                OnPropertyChanged();
            }
        }

        public string ValueColor
        {
            get => _valueColor;
            set
            {
                _valueColor = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class QuickActionViewModel : INotifyPropertyChanged
    {
        private string _icon = string.Empty;
        private string _title = string.Empty;
        private string _tooltip = string.Empty;
        private System.Windows.Input.ICommand? _command;
        private object? _commandParameter;

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

        public string Tooltip
        {
            get => _tooltip;
            set
            {
                _tooltip = value;
                OnPropertyChanged();
            }
        }

        public System.Windows.Input.ICommand? Command
        {
            get => _command;
            set
            {
                _command = value;
                OnPropertyChanged();
            }
        }

        public object? CommandParameter
        {
            get => _commandParameter;
            set
            {
                _commandParameter = value;
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