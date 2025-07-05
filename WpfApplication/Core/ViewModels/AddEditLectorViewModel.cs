using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;
using VideoLecturer.Core.Models;
using WpfApplication.Core.Models;
using WpfApplication.Core.Services;
using WpfApplication.Views;
using static WpfApplication.Core.ViewModels.ProjectViewModel;
using Application = System.Windows.Application;

namespace WpfApplication.Core.ViewModels
{
    public class AddEditLectorViewModel : BaseViewModel
    {
        private string _name;
        private string _imagePath;
        private string _voicePath;

        public string WindowTitle => IsEditMode ? "Редактирование лектора" : "Добавление лектора";
        public bool IsEditMode => _lectorToEdit != null;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        public string VoicePath
        {
            get => _voicePath;
            set => SetProperty(ref _voicePath, value);
        }

        public LectorProfile _lectorToEdit { get; set; }

        private ILectorManager _lectorManager;

        public ICommand SelectImageCommand { get; }
        public ICommand SelectVoiceCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private readonly string _lectorBasePath;

        public AddEditLectorViewModel(string lectorPath, ILectorManager lectorManager, LectorProfile lectorToEdit = null)
        {
            _lectorManager = lectorManager;
            SelectImageCommand = new RelayCommand(SelectImage);
            SelectVoiceCommand = new RelayCommand(SelectVoice);
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            _lectorBasePath = lectorPath;
            _lectorToEdit = lectorToEdit;
            if (!Directory.Exists(_lectorBasePath))
            {
                Directory.CreateDirectory(_lectorBasePath);
            }

            if (IsEditMode)
            {
                Name = lectorToEdit.Name;
                ImagePath = lectorToEdit.ImagePath;
                VoicePath = lectorToEdit.VoicePath;
            }
        }

        private void SelectImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png",
                Title = "Выберите фотографию лектора"
            };

            if (dialog.ShowDialog() == true)
            {
                ImagePath = dialog.FileName;
            }
        }

        private void SelectVoice()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Аудио файлы|*.wav",
                Title = "Выберите образец голоса"
            };

            if (dialog.ShowDialog() == true)
            {
                VoicePath = dialog.FileName;
            }
        }

        private bool CanSave() => !string.IsNullOrWhiteSpace(Name) &&
                                !string.IsNullOrWhiteSpace(ImagePath) &&
                                !string.IsNullOrWhiteSpace(VoicePath);

        private void Save()
        {
            try
            {
                if (IsEditMode)
                {
                    // Для редактирования - обновляем существующие файлы
                    string lectorFolder = Path.GetDirectoryName(_lectorToEdit.ImagePath);

                    // Обновляем изображение, если оно изменилось
                    if (ImagePath != _lectorToEdit.ImagePath)
                    {
                        File.Delete(_lectorToEdit.ImagePath);
                        string newImagePath = Path.Combine(lectorFolder, Path.GetFileName(ImagePath));
                        File.Copy(ImagePath, newImagePath, true);
                        _lectorToEdit.ImagePath = newImagePath;
                    }

                    // Обновляем аудио, если оно изменилось
                    if (VoicePath != _lectorToEdit.VoicePath)
                    {
                        File.Delete(_lectorToEdit.VoicePath);
                        string newVoicePath = Path.Combine(lectorFolder, Path.GetFileName(VoicePath));
                        File.Copy(VoicePath, newVoicePath, true);
                        _lectorToEdit.VoicePath = newVoicePath;
                    }

                    _lectorToEdit.Name = Name;
                    _lectorManager.UpdateLector(_lectorToEdit);
                }
                else
                {
                    string lectorFolder = Path.Combine(_lectorBasePath, Guid.NewGuid().ToString());
                    Directory.CreateDirectory(lectorFolder);

                    string newImagePath = Path.Combine(lectorFolder, Path.GetFileName(ImagePath));
                    File.Copy(ImagePath, newImagePath);

                    string newVoicePath = Path.Combine(lectorFolder, Path.GetFileName(VoicePath));
                    File.Copy(VoicePath, newVoicePath);

                    var newLector = new LectorProfile
                    {
                        Id = Guid.Parse(lectorFolder.Split("\\").Last()),
                        Name = Name,
                        ImagePath = newImagePath,
                        VoicePath = newVoicePath
                    };
                    _lectorManager.AddLector(newLector);
                }
                (Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this) as AddEditLectorWindow)?.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при сохранении лектора: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Cancel()
        {
            // Закрываем окно с результатом false
            (Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this) as AddEditLectorWindow)?.Close();
        }
    }
}