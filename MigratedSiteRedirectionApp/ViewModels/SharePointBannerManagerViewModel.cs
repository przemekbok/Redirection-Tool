using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MigratedSiteRedirectionApp.Logic;

namespace MigratedSiteRedirectionApp.ViewModels
{
    public class SharePointBannerManagerViewModel : INotifyPropertyChanged
    {
        private string _siteUrl;
        private string _bannerMessage;
        private string _jsCode;
        private bool _isProcessing;
        private ICommand _applyActionCommand;
        private readonly BannerManager _bannerManager;

        public SharePointBannerManagerViewModel()
        {
            // Initialize with default values
            SiteUrl = "https://glob.1sharepoint.roche.com/team/xyz";
            BannerMessage = "Important Notice: Scheduled maintenance will occur on [Date]. Please check the status page for updates.";
            JsCode = "// Enter JavaScript code for banner redirection here...";
            
            // Initialize banner manager
            _bannerManager = new BannerManager();
            
            // Initialize command
            ApplyActionCommand = new AsyncRelayCommand(ExecuteApplyActionAsync, CanExecuteApplyAction);
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

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    OnPropertyChanged();
                }
            }
        }

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
            // Can execute if not currently processing
            return !IsProcessing;
        }

        private async Task ExecuteApplyActionAsync(object parameter)
        {
            IsProcessing = true;

            try
            {
                // Apply banner using the BannerManager
                var result = await _bannerManager.ApplyBannerAsync(SiteUrl, BannerMessage, JsCode);

                if (result.IsSuccess)
                {
                    MessageBox.Show(
                        result.Message,
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        result.ErrorMessage,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // AsyncRelayCommand implementation for async operations
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _executeAsync;
        private readonly Predicate<object> _canExecute;

        public AsyncRelayCommand(Func<object, Task> executeAsync, Predicate<object> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
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

        public async void Execute(object parameter)
        {
            await _executeAsync(parameter);
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