using System.Windows;
using WpfApplication.Core.Services;
using WpfApplication.Core.ViewModels;

namespace WpfApplication.Views
{
    public partial class LectorManagerWindow : Window
    {
        public LectorManagerWindow(ILectorManager lectorManager)
        {
            InitializeComponent();
            DataContext = new LectorManagerViewModel(lectorManager);
        }
    }
}