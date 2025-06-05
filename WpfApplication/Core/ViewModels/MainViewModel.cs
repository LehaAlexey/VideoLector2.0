using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using VideoLecturer.Core.Models;
using VideoLecturer.Core.Services;
using WpfApplication.Core.Services;
using WpfApplication.Views;
using static WpfApplication.Core.ViewModels.ProjectViewModel;

namespace WpfApplication.Core.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IProjectManager _projectManager;
        private readonly ILectorManager _lectorManager;
        private readonly IPythonApiService _pythonApi;
        private ProjectViewModel _currentProject;

        public event Action<ProjectViewModel> ActiveProjectChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ProjectViewModel> Projects { get; } = new();

        public ProjectViewModel CurrentProject
        {
            get => _currentProject;
            set
            {
                if (_currentProject != value)
                {
                    _currentProject = value;
                    ActiveProjectChanged?.Invoke(_currentProject);
                    OnPropertyChanged();
                }
            }
        }

        public ICommand CreateProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand CloseProjectCommand { get; }
        public ICommand AddLectorCommand { get; }
        public ICommand ManageLectorsCommand { get; }
        public ICommand LoadProjectsCommand { get; }

        public MainViewModel(IProjectManager projectManager,
                           ILectorManager lectorManager,
                           IPythonApiService pythonApi)
        {
            _projectManager = projectManager;
            _lectorManager = lectorManager;
            _pythonApi = pythonApi;

            // Загрузка проектов
            foreach (var project in _projectManager.GetAllProjects())
            {
                var projectVm = new ProjectViewModel(project, _projectManager, _lectorManager, _pythonApi);
                Projects.Add(projectVm);
                projectVm.RequestClose += () => CloseProject(projectVm);
            }
            CurrentProject = Projects.FirstOrDefault();
            // Инициализация команд
            CreateProjectCommand = new RelayCommand(CreateNewProject);
            SaveProjectCommand = new RelayCommand(SaveCurrentProject);
            CloseProjectCommand = new RelayCommand<ProjectViewModel>(CloseProject);
            AddLectorCommand = new RelayCommand(OpenAddLectorWindow);
            ManageLectorsCommand = new RelayCommand(OpenLectorManager);
            LoadProjectsCommand = new RelayCommand(RestoreAllProjects);
        }

        private void RestoreAllProjects()
        {
            Projects.Clear();
            foreach (var project in _projectManager.GetAllProjects())
            {
                var projectVm = new ProjectViewModel(project, _projectManager, _lectorManager, _pythonApi);
                Projects.Add(projectVm);
                projectVm.RequestClose += () => CloseProject(projectVm);
                CurrentProject = null;
            }
        }

        private void OpenAddLectorWindow()
        {
            var window = new AddEditLectorWindow(_lectorManager)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        private void OpenLectorManager()
        {
            var window = new LectorManagerWindow(_lectorManager)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        private void SaveCurrentProject()
        {
            if (CurrentProject != null)
            {
                try
                {
                    CurrentProject.SaveProject();
                    MessageBox.Show("Проект успешно сохранен", "Сохранение",
                                 MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                                 MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateNewProject()
        {
            var project = _projectManager.CreateNewProject();
            var projectVm = new ProjectViewModel(project, _projectManager, _lectorManager, _pythonApi);
            projectVm.RequestClose += () => CloseProject(projectVm);
            Projects.Add(projectVm);
        }

        private void CloseProject(ProjectViewModel project)
        {
            if (project != null && Projects.Contains(project))
            {
                Projects.Remove(project);
                if (CurrentProject == project)
                {
                    CurrentProject = null;
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}