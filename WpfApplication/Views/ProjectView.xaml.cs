using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VideoLecturer.Core.Models;
using WpfApplication.Core.ViewModels;

namespace VideoLecturer.Views
{
    /// <summary>
    /// Логика взаимодействия для ProjectView.xaml
    /// </summary>
    public partial class ProjectView : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
                DependencyProperty.Register(
                    "ViewModel",
                    typeof(ProjectViewModel),
                    typeof(ProjectView),
                    new PropertyMetadata(null, OnViewModelChanged));
        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProjectView view)
            {
                view.DataContext = e.NewValue;
            }
        }
        public ProjectViewModel ViewModel
        {
            get => (ProjectViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel mainVm)
            {
                mainVm.ActiveProjectChanged += OnActiveProjectChanged;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel mainVm)
            {
                mainVm.ActiveProjectChanged -= OnActiveProjectChanged;
            }
        }
        private void OnActiveProjectChanged(ProjectViewModel newProject)
        {
            // Принудительно обновляем DataContext
            DataContext = newProject;

            // Дополнительные действия при смене проекта
            if (newProject != null)
            {
                newProject.LoadFragments(); // Например, перезагружаем фрагменты
            }
        }
        public ProjectView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            //Loaded += (s, e) => { DataContext = ViewModel; };
        }
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }
}
