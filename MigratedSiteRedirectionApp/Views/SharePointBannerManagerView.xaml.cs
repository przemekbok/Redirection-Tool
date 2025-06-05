using System.Windows;
using MigratedSiteRedirectionApp.ViewModels;

namespace MigratedSiteRedirectionApp.Views
{
    /// <summary>
    /// Interaction logic for SharePointBannerManagerView.xaml
    /// </summary>
    public partial class SharePointBannerManagerView : Window
    {
        public SharePointBannerManagerView()
        {
            InitializeComponent();
            DataContext = new SharePointBannerManagerViewModel();
        }
    }
}