using System;
using System.ComponentModel;

namespace AccountingSystem.WPF.ViewModels
{
    // Minimal placeholder SettingsViewModel used by App.xaml.cs
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _appTheme = "Light";
        public string AppTheme
        {
            get => _appTheme;
            set
            {
                if (_appTheme == value) return;
                _appTheme = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AppTheme)));
            }
        }
    }

}
