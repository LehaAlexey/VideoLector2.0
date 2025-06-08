using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using VideoLecturer;
using VideoLecturer.Core.Models;
using VideoLecturer.Core.Services;
using VideoLecturer.Infrastructure.Storage;
using WpfApplication.Core.Models;
using WpfApplication.Core.Services;
using WpfApplication.Core.ViewModels;

namespace WpfApplication.Core.ViewModels
{
    public partial class ProjectViewModel : INotifyPropertyChanged
    {
        private readonly Project _project;
        private readonly IProjectManager _projectManager;
        private readonly ILectorManager _lectorManager;
        private readonly IPythonApiService _pythonApi;
        private CancellationTokenSource _generationCts;

        public event Action RequestClose;

        public string Name
        {
            get => _project.Name;
            set
            {
                _project.Name = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreationDate => _project.CreationDate;
        public string TextFilePath => _project.TextFilePath;
        public string PdfFilePath => _project.PdfFilePath;

        public ObservableCollection<VideoFragmentViewModel> Fragments { get; } = new();
        public IEnumerable<LectorProfile> AvailableLectors { get; }

        public LectorProfile SelectedLector
        {
            get => _lectorManager.GetLectorById(_project.LectorId);
            set
            {
                _project.LectorId = value?.Id ?? Guid.Empty;
                OnPropertyChanged();
            }
        }

        private VideoPosition _position;
        public VideoPosition Position
        {
            get => _project.Position;
            set
            {
                if (_project.Position != value)
                {
                    _project.Position = value;
                    OnPropertyChanged();
                }
            }
        }
        // Команды
        public ICommand SelectTextCommand { get; }
        public ICommand SelectPdfCommand { get; }
        public ICommand GenerateAllCommand { get; }
        public ICommand CancelGenerationCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public bool IsBottomRight
        {
            get => Position == VideoPosition.BottomRight;
            set { if (value) Position = VideoPosition.BottomRight; }
        }

        public bool IsBottomLeft
        {
            get => Position == VideoPosition.BottomLeft;
            set { if (value) Position = VideoPosition.BottomLeft; }
        }

        public bool IsTopRight
        {
            get => Position == VideoPosition.TopRight;
            set { if (value) Position = VideoPosition.TopRight; }
        }

        public bool IsTopLeft
        {
            get => Position == VideoPosition.TopLeft;
            set { if (value) Position = VideoPosition.TopLeft; }
        }

        public ProjectViewModel(Project project,
                                  IProjectManager projectManager,
                                  ILectorManager lectorManager,
                                  IPythonApiService pythonApi)
        {
            _project = project;
            _projectManager = projectManager;
            _lectorManager = lectorManager;
            _pythonApi = pythonApi;
            
            if(!string.IsNullOrEmpty(_project.TextFilePath))
                LoadFragments();

            AvailableLectors = new ObservableCollection<LectorProfile>(_lectorManager.AllLectors);

            // Инициализация команд
            SelectTextCommand = new RelayCommand(SelectTextFile);
            SelectTextCommand = new RelayCommand(SelectTextFile);
            SelectPdfCommand = new RelayCommand(SelectPdfFile);
            GenerateAllCommand = new AsyncRelayCommand(GenerateAllFragmentsAsync);
            CancelGenerationCommand = new RelayCommand(CancelGeneration);
            SaveCommand = new RelayCommand(SaveProject);
            DeleteCommand = new RelayCommand(DeleteProject);
            LoadFragments();
        }

        private void SelectTextFile()
        {
            var dialog = new OpenFileDialog { Filter = "Текстовые файлы|*.txt" };
            if (dialog.ShowDialog() == true)
            {
                _project.TextFilePath = dialog.FileName;
                OnPropertyChanged(nameof(TextFilePath));
                LoadFragments();
            }
        }

        private void SelectPdfFile()
        {
            var dialog = new OpenFileDialog { Filter = "PDF файлы|*.pdf" };
            if (dialog.ShowDialog() == true)
            {
                _project.PdfFilePath = dialog.FileName;
                OnPropertyChanged(nameof(PdfFilePath));
            }
        }

        private async Task GenerateAllFragmentsAsync()
        {
            if (SelectedLector == null || string.IsNullOrEmpty(TextFilePath))
            {
                MessageBox.Show("Выберите лектора и текстовый файл");
                return;
            }

            _generationCts = new CancellationTokenSource();

            try
            {
                var slideTexts = File.ReadAllText(_project.TextFilePath);


                var generationId = Guid.NewGuid();


                var fragmentDir = await _pythonApi.GenerateVideoFragment(
                    (_projectManager as ProjectStorage)._fragmentsPath,
                    SelectedLector.ImagePath,
                    SelectedLector.VoicePath,
                    slideTexts,
                    _project.Id);

                var videoFiles = Directory.GetFiles(fragmentDir, "video_fragment_*.mp4");
                var i = 0;
                foreach (var videoFile in videoFiles)
                {
                    var fragment = new VideoFragment(i + 1, videoFile);
                    Fragments.Add(new VideoFragmentViewModel(fragment));
                    i++;
                }
                
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Генерация отменена");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации: {ex.Message}");
            }
        }
        public void SaveProject()
        {
            _projectManager.SaveProject(_project);
        }

        private void DeleteProject()
        {
            if (MessageBox.Show("Удалить проект?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    _projectManager.DeleteProject(_project.Id);
                    RequestClose?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении проекта: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        public void LoadFragments()
        {
            Fragments.Clear();
            if (!string.IsNullOrEmpty(_project.TextFilePath) && File.Exists(_project.TextFilePath))
            {
                var lines = File.ReadAllText(_project.TextFilePath).Split("_;"); //взят из конфига второкурсников (их модулей, config.py)
                for (int i = 0; i < lines.Length; i++)
                {
                    Fragments.Add(new VideoFragmentViewModel(
                        new VideoFragment(i + 1, lines[i])));
                }
            }
        }

        private void CloseProject()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.Windows
                    .OfType<MainWindow>()
                    .FirstOrDefault(w => w.DataContext == this);

                window?.Close();
            });
        }

        private void CancelGeneration()
        {
            _pythonApi.CancelGeneration();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            SaveProject();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}