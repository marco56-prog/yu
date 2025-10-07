using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AccountingSystem.WPF.ViewModels
{
    // Non-generic RelayCommand with convenient overloads
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Parameterless action
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : new Predicate<object?>(_ => canExecute())) { }

        // Async parameterless action (fire-and-forget)
        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
            : this(async _ => await executeAsync().ConfigureAwait(false), canExecute == null ? null : new Predicate<object?>(_ => canExecute())) { }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }

    // Generic RelayCommand<T> implementation used across many ViewModels
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Parameterless wrapper for convenience
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : new Predicate<T?>(_ => canExecute())) { }

        // Async wrapper
        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
            : this(async _ => await executeAsync().ConfigureAwait(false), canExecute == null ? null : new Predicate<T?>(_ => canExecute())) { }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            if (parameter is T t) return _canExecute(t);
            // handle null parameter
            return _canExecute(default);
        }

        public void Execute(object? parameter)
        {
            if (parameter is T t)
                _execute(t);
            else
                _execute(default);
        }
    }
}