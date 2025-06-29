using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Office2010.Excel;
using FFMpegCore;
using Microsoft.Win32;
using OpenXmlPowerTools;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VideoLecturer;
using VideoLecturer.Core.Models;
using VideoLecturer.Core.Services;
using VideoLecturer.Infrastructure.Storage;
using WpfApplication.Core.Models;
using WpfApplication.Core.Services;

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
        private ObservableCollection<SlideViewModel> _slides;
        public ObservableCollection<SlideViewModel> Slides
        {
            get => _slides;
            set
            {
                _slides = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentSlide));
            }
        }

        private int _currentSlideIndex;
        public int CurrentSlideIndex
        {
            get => _currentSlideIndex;
            set
            {
                if (_currentSlideIndex != value && value >= 0 && value < Slides.Count)
                {
                    _currentSlideIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentSlide));
                }
            }
        }
        public SlideViewModel CurrentSlide => (Slides != null && Slides.Count > 0 && CurrentSlideIndex >= 0)
    ? Slides[CurrentSlideIndex]
    : null;
        public int TotalSlides => Slides.Count;
        public string Name
        {
            get => _project.Name;
            set
            {
                _project.Name = value;
                OnPropertyChanged();
            }
        }
        private VideoFragmentViewModel _selectedFragment;
        public VideoFragmentViewModel SelectedFragment
        {
            get => _selectedFragment;
            set
            {
                if (_selectedFragment != value)
                {
                    _selectedFragment = value;
                    OnPropertyChanged();

                    // Переключаем слайд при выборе фрагмента
                    if (value != null && Slides.Count > 0)
                    {
                        CurrentSlideIndex = value.SlideNumber - 1;
                    }
                }
            }
        }
        public DateTime CreationDate => _project.CreationDate;
        public string TextFilePath => _project.TextFilePath;
        public string pptxFilePath => _project.PptxFilePath;

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

        // Команды
        public ICommand SelectTextCommand { get; }
        public ICommand SelectPptxCommand { get; }
        public ICommand GenerateAllCommand { get; }
        public ICommand CancelGenerationCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ExportCommand { get; }

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
            _slides = new ObservableCollection<SlideViewModel>();
            if (!string.IsNullOrEmpty(_project.TextFilePath))
                InitializeFragments();

            AvailableLectors = new ObservableCollection<LectorProfile>(_lectorManager.AllLectors);

            // Инициализация команд
            SelectTextCommand = new RelayCommand(SelectTextFile);
            SelectTextCommand = new RelayCommand(SelectTextFile);
            SelectPptxCommand = new RelayCommand(SelectPptxFile);
            ExportCommand = new AsyncRelayCommand(ExportProjectAsync);
            GenerateAllCommand = new AsyncRelayCommand(GenerateAllFragmentsAsync);
            CancelGenerationCommand = new RelayCommand(CancelGeneration);
            SaveCommand = new RelayCommand(SaveProject);
            DeleteCommand = new RelayCommand(DeleteProject);
            InitializeFragments();
            InitializeSlides();
        }
        public double ExportProgress { get; private set; }
        public bool IsExporting { get; private set; }
        private async Task ExportProjectAsync()
        {
            var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Выберите папку для сохранения файла";
            folderDialog.ShowNewFolderButton = true;
            string output; // Путь к выходному файлу
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                output = folderDialog.SelectedPath;
            }
            else
            { return; }
            var tempConcatFile = Path.Combine((_projectManager as ProjectStorage)._storagePath, $"temp\\concat_list.txt"); // Путь к файлу для конкатенации
            

            Directory.CreateDirectory("temp");

            using (var writer = new StreamWriter(tempConcatFile))
            {
                for (int i = 0; i < Fragments.Count; i++)
                {
                    var video = Path.Combine((_projectManager as ProjectStorage)._fragmentsPath, _project.Id.ToString(), "video_fragments", $"video_fragment_0_{i + 1}_1.mp4");
                    var slide = Path.Combine((_projectManager as ProjectStorage)._storagePath, _project.Id.ToString(), "slides", "slides", $"Slide{i + 1}.png");
                    var tempSlide = Path.Combine((_projectManager as ProjectStorage)._storagePath, $"temp\\slides\\slide_{i+1}.mp4");
                    var tempOverlay = Path.Combine((_projectManager as ProjectStorage)._storagePath, $"temp\\overlays\\overlay_{i + 1}.mp4");

                    // Получение длительности видео
                    var duration = await ExecuteCommandAsync($"ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{video}\"");

                    // Создание временного слайда
                    await ExecuteCommandAsync($"ffmpeg -loop 1 -i \"{slide}\" -c:v libx264 -t {duration} -vf \"scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2\" -pix_fmt yuv420p -y \"{tempSlide}\"");

                    // Наложение видео на слайд
                    await ExecuteCommandAsync($"ffmpeg -i \"{tempSlide}\" -i \"{video}\" -filter_complex \"[0:v][1:v]overlay=0:0:shortest=1[outv]\" -map \"[outv]\" -map \"1:a\" -c:v libx264 -c:a aac -y \"{tempOverlay}\"");

                    // Запись в файл для конкатенации
                    await writer.WriteLineAsync($"file '{tempOverlay}'");

                    // Удаление временного слайда
                    File.Delete(tempSlide);
                }
            }

            // Конкатенация всех фрагментов в один выходной файл
            await ExecuteCommandAsync($"ffmpeg -f concat -safe 0 -i \"{tempConcatFile}\" -c copy -y \"{output}\"");
        }
        private async Task<string> ExecuteCommandAsync(string command)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Error executing command: {error}");
                }

                return output;
            }
        }


        public void InitializeSlides()
        {
            var newSlides = new ObservableCollection<SlideViewModel>();

            if (!string.IsNullOrEmpty(pptxFilePath) && File.Exists(pptxFilePath))
            {
                string slidesFolder = Path.Combine(
                    (_projectManager as ProjectStorage)._storagePath,
                    _project.Id.ToString(),
                    "slides");

                Directory.CreateDirectory(slidesFolder);

                for (int i = 0; i < Fragments.Count; i++)
                {
                    newSlides.Add(new SlideViewModel(
                        initialSlideNumber: i + 1,
                        videoPath: Fragments[i].VideoPath,
                        pptxPath: pptxFilePath,
                        position: Position,
                        slidesFolder: slidesFolder
                    ));
                }
            }

            Slides = newSlides;
            CurrentSlideIndex = Slides.Count > 0 ? 0 : -1;
        }

        // Обновите метод при изменении позиции видео
        public VideoPosition Position
        {
            get => _project.Position;
            set
            {
                if (_project.Position != value)
                {
                    _project.Position = value;
                    foreach (var slide in Slides)
                    {
                        slide.Position = value;
                    }
                    OnPropertyChanged();
                }
            }
        }
        private void SelectTextFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Текстовые файлы|*.txt" };
            if (dialog.ShowDialog() == true)
            {
                _project.TextFilePath = dialog.FileName;
                OnPropertyChanged(nameof(TextFilePath));
                InitializeFragments();
            }
        }

        private void SelectPptxFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Pptx файлы|*.pptx" };
            if (dialog.ShowDialog() == true)
            {
                // Освобождаем ресурсы слайдов
                foreach (var slide in Slides)
                {
                    slide.Dispose();
                    slide.UpdateCurrentImage();
                }

                _project.PptxFilePath = dialog.FileName;
                OnPropertyChanged(nameof(pptxFilePath));

                string slidesDir = Path.Combine((_projectManager as ProjectStorage)._storagePath,
                                              _project.Id.ToString(), "slides");

                try
                {
                    if (Directory.Exists(slidesDir))
                    {
                        Directory.Delete(slidesDir, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при удалении слайдов: {ex.Message}");
                }
                
                InitializeSlides();
            }
        }

        private async Task GenerateAllFragmentsAsync()
        {
            if (SelectedLector == null || string.IsNullOrEmpty(TextFilePath))
            {
                System.Windows.MessageBox.Show("Выберите лектора и текстовый файл");
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

                // Получаем все файлы фрагментов
                var videoFiles = Directory.GetFiles(fragmentDir, "video_fragment_*.mp4")
                    .OrderBy(f => f) // Сортируем для правильной последовательности
                    .ToList();

                // Группируем файлы по основному индексу (video_fragment_X_*.mp4 -> группируем по X)
                var groupedFiles = videoFiles
                    .GroupBy(f =>
                    {
                        var fileName = Path.GetFileNameWithoutExtension(f);
                        var parts = fileName.Split('_');
                        return int.Parse(parts[2]); // Берем основной индекс (X из video_fragment_X_Y.mp4)
                    })
                    .OrderBy(g => g.Key);

                // Очищаем текущие фрагменты
                Fragments.Clear();

                // Обрабатываем каждую группу
                foreach (var group in groupedFiles)
                {
                    var filesInGroup = group.OrderBy(f => f).ToList();

                    if (filesInGroup.Count == 1)
                    {
                        // Если файл один в группе - просто добавляем
                        var fragment = new VideoFragment(group.Key, filesInGroup[0]);
                        Fragments.Add(new VideoFragmentViewModel(fragment));
                    }
                    else
                    {
                        // Если файлов несколько - объединяем
                        var outputFile = Path.Combine(fragmentDir, $"video_fragment_{group.Key}.mp4");

                        // Создаем список файлов для конкатенации
                        var listFile = Path.Combine(fragmentDir, $"concat_list_{group.Key}.txt");
                        File.WriteAllLines(listFile, filesInGroup.Select(f => $"file '{f}'"));

                        // Используем FFmpeg для объединения
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "ffmpeg",
                                Arguments = $"-f concat -safe 0 -i \"{listFile}\" -c copy \"{outputFile}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardError = true
                            }
                        };

                        process.Start();
                        await process.WaitForExitAsync();

                        if (process.ExitCode == 0)
                        {
                            // Удаляем временные файлы
                            File.Delete(listFile);
                            foreach (var file in filesInGroup)
                            {
                                File.Delete(file);
                            }

                            // Добавляем объединенный фрагмент
                            var fragment = new VideoFragment(group.Key, outputFile);
                            Fragments.Add(new VideoFragmentViewModel(fragment));
                        }
                        else
                        {
                            var error = await process.StandardError.ReadToEndAsync();
                            throw new Exception($"Ошибка объединения фрагментов: {error}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                System.Windows.MessageBox.Show("Генерация отменена");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка генерации: {ex.Message}");
            }

        }
        public void SaveProject()
        {
            _projectManager.SaveProject(_project);
        }

        private void DeleteProject()
        {
            if (System.Windows.MessageBox.Show("Удалить проект?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    _projectManager.DeleteProject(_project.Id);
                    RequestClose?.Invoke();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при удалении проекта: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        public void InitializeFragments()
        {
            Fragments.Clear();
            if (!string.IsNullOrEmpty(_project.TextFilePath) && File.Exists(_project.TextFilePath))
            {
                var lines = File.ReadAllText(_project.TextFilePath).Split("_;"); //взят из конфига второкурсников (их модулей, config.py)
                for (int i = 0; i < lines.Length; i++)
                {
                    var tmp = Path.Combine((_projectManager as ProjectStorage)._fragmentsPath, _project.Id.ToString(), "video_fragments", $"video_fragment_0_{i + 1}_1.mp4");
                    Fragments.Add(new VideoFragmentViewModel(
                        new VideoFragment(i + 1, tmp)));
                }
            }
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