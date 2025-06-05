using System.Windows;
using VideoLecturer.Core.Models;
using WpfApplication.Core.Services;
using WpfApplication.Core.ViewModels;

namespace WpfApplication.Views
{
    public partial class AddEditLectorWindow : Window
    {
        public AddEditLectorWindow(ILectorManager lectorManager, LectorProfile lectorToEdit = null)
        {
            InitializeComponent();
            var path = lectorManager.GetPath();
            DataContext = new AddEditLectorViewModel(path, lectorManager, lectorToEdit);
        }
    }
}