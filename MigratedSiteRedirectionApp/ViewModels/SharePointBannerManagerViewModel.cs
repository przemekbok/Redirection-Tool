using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MigratedSiteRedirectionApp.ViewModels
{
    public class SharePointBannerManagerViewModel : INotifyPropertyChanged
    {
        private string _siteUrl;
        private string _bannerMessage;
        private string _jsCode;
        private string _selectedMode;
        private ICommand _applyActionCommand;

        public SharePointBannerManagerViewModel()
        {
            // Initialize with default values
            SiteUrl = "https://glob.1sharepoint.roche.com/team/xyz";
            BannerMessage = "Important Notice: Scheduled maintenance will occur on [Date]. Please check the status page for updates.";
            JsCode = "// Enter JavaScript code for banner redirection here...";
            
            // Initialize available modes
            AvailableModes = new ObservableCollection<string>
            {
                "Select a mode...",
                "Add Banner",
                "Update Banner",
                "Remove Banner",
                "Test Banner"
            };
            
            SelectedMode = AvailableModes[0];
            
            // Initialize command
            ApplyActionCommand = new RelayCommand(ExecuteApplyAction, CanExecuteApplyAction);
        }

        public string SiteUrl
        {
            get => _siteUrl;
            set
            {
                if (_siteUrl != value)
                {
                    _siteUrl = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string BannerMessage
        {
            get => _bannerMessage;
            set
            {
                if (_bannerMessage != value)
                {
                    _bannerMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string JsCode
        {
            get => _jsCode;
            set
            {
                if (_jsCode != value)
                {
                    _jsCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (_selectedMode != value)
                {
                    _selectedMode = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ObservableCollection<string> AvailableModes { get; }

        public ICommand ApplyActionCommand
        {
            get => _applyActionCommand;
            set
            {
                _applyActionCommand = value;
                OnPropertyChanged();
            }
        }

        private bool CanExecuteApplyAction(object parameter)
        {
            // Validate that required fields are filled
            return !string.IsNullOrWhiteSpace(SiteUrl) &&
                   !string.IsNullOrWhiteSpace(BannerMessage) &&
                   SelectedMode != "Select a mode..." &&
                   !string.IsNullOrWhiteSpace(SelectedMode);
        }

        private void ExecuteApplyAction(object parameter)
        {
            // TODO: Implement the actual action based on the selected mode
            // For now, we'll just show a placeholder
            string message = $"Action '{SelectedMode}' will be applied to:\n" +
                           $"Site: {SiteUrl}\n" +
                           $"Banner Message: {BannerMessage.Substring(0, Math.Min(50, BannerMessage.Length))}...\n" +
                           $"JS Code: {(string.IsNullOrWhiteSpace(JsCode) ? "None" : "Provided")}";
            
            System.Windows.MessageBox.Show(message, "Apply Action", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    // CommandManager helper for WPF
    public static class CommandManager
    {
        public static event EventHandler RequerySuggested
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public static void InvalidateRequerySuggested()
        {
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }
}