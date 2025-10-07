using System.ComponentModel;
using System.Runtime.CompilerServices;
using AccountingSystem.WPF.Services;

namespace AccountingSystem.WPF.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;
        private SidebarViewModel _sidebarViewModel;

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            InitializeViewModels();
        }

        public SidebarViewModel SidebarViewModel
        {
            get => _sidebarViewModel;
            set => SetProperty(ref _sidebarViewModel, value);
        }

        private void InitializeViewModels()
        {
            SidebarViewModel = new SidebarViewModel(_navigationService);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}