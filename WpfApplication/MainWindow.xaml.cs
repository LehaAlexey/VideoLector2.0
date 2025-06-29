using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VideoLecturer.Infrastructure.PythonAPI;
using VideoLecturer.Infrastructure.Storage;
using VideoLecturer.Views;
using WpfApplication.Core.Services;
using WpfApplication.Core.ViewModels;

namespace VideoLecturer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MainViewModel mainVm)
            {
                mainVm.ActiveProjectChanged += OnActiveProjectChanged;
            }
            if (e.OldValue is MainViewModel oldVm)
            {
                oldVm.ActiveProjectChanged -= OnActiveProjectChanged;
            }
        }
        private void OnActiveProjectChanged(ProjectViewModel newProject)
        {
            // Находим ProjectView в визуальном дереве
            var projectView = FindVisualChild<ProjectView>(this);
            if (projectView != null)
            {
                projectView.DataContext = newProject;
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
            return null;
        }
    }
}